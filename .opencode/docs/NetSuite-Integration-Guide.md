# NetSuite Integration Guide: 30 Users, 5 Concurrent Requests

**Audience:** developers building or maintaining the on-prem C# .NET web app that talks directly to NetSuite.
**Goal:** transactions post *almost instantly* for about 30 concurrent users, even though the NetSuite license allows only **5 concurrent API requests**.

> **Assumptions.** The code samples target ASP.NET Core (.NET 8). If the app is on ASP.NET MVC (.NET Framework 4.x), the same patterns apply: register the singletons with your DI container, or use static instances. Values in `<ANGLE_BRACKETS>` are placeholders you must fill in. Test everything in the **Sandbox** account before Production.

---

## Table of Contents

1. [The Problem in Plain Terms](#1-the-problem-in-plain-terms)
2. [Design Rules (Read These First)](#2-design-rules-read-these-first)
3. [Target Architecture](#3-target-architecture)
4. [Implementation Steps](#4-implementation-steps)
   - [Step 1: Configuration & Authentication](#step-1-configuration--authentication)
   - [Step 2: The NetSuite Gateway (Concurrency Gate + Retry)](#step-2-the-netsuite-gateway-concurrency-gate--retry)
   - [Step 3: Reference Data Cache](#step-3-reference-data-cache)
   - [Step 3b: Making Data Fetching Fast](#step-3b-making-data-fetching-fast)
   - [Step 4: Pre-Validation](#step-4-pre-validation)
   - [Step 5: The "Create Transaction" RESTlet](#step-5-the-create-transaction-restlet)
   - [Step 6: Pending-Post Journal (Crash Safety Without a Database)](#step-6-pending-post-journal-crash-safety-without-a-database)
   - [Step 7: The Posting Endpoint (Putting It Together)](#step-7-the-posting-endpoint-putting-it-together)
   - [Step 8: Slim Down the User Event Scripts](#step-8-slim-down-the-user-event-scripts)
5. [Monitoring & Tuning](#5-monitoring--tuning)
6. [Load Testing Before Go-Live](#6-load-testing-before-go-live)
7. [Do / Don't Checklist](#7-do--dont-checklist)
8. [Glossary](#8-glossary)

---

## 1. The Problem in Plain Terms

- NetSuite limits how many API requests can run **at the same moment**. Our account allows **5**.
- The limit covers **simultaneous requests, not users**. A slot is only taken while a call is running.
- When a 6th request arrives while 5 are running, NetSuite **rejects** it (HTTP `429` or a concurrency error). It does not wait in line.
- That gives us two levers:
  1. **Control the traffic** so we never send more than NetSuite allows, and we wait briefly instead of failing.
  2. **Shorten each call** so slots free up faster.

### Throughput math

| Average save time | Slots used for writes | Posts per minute (max) |
|---|---|---|
| 1.5 s | 4 | ~160 |
| 3 s | 4 | ~80 |
| 7 s (heavy User Event scripts) | 4 | ~34 ← users will feel this at peak |

**Takeaway:** making each save faster matters more than anything else. A 7 s save becoming a 1.5 s save more than quadruples capacity.

### Why we do NOT use Map/Reduce for the transaction itself

Map/Reduce jobs go into a queue and can start seconds to minutes later. Users need the transaction number **right away**, so the record itself is created **synchronously** through a RESTlet. Map/Reduce and scheduled scripts are used only for the follow-up work (see [Step 8](#step-8-slim-down-the-user-event-scripts)).

---

## 2. Design Rules (Read These First)

1. **Every NetSuite call goes through `NetSuiteGateway`.** No exceptions, and no `new HttpClient()` anywhere else.
2. **Never use more than 4 slots.** One slot stays free for CSV imports, other integrations, and admins.
3. **Posts get priority over reads.** Reads (searches and lookups) may use at most 2 slots, so a burst of searches can never block a sale.
4. **One business transaction = one HTTP call.** The RESTlet creates the record and returns the ID and number in the same response.
5. **Every transaction carries an `externalId`.** This makes retries safe, because the same post can never create a duplicate.
6. **Reference data comes from the in-memory cache**, never from NetSuite on every keystroke.
7. **Validate before you call.** A rejected call still uses up a slot.
8. **User Event scripts do only what the user must see right now.** Everything else is deferred.

---

## 3. Target Architecture

```
 Browser (30 users)
      │
      ▼
 ┌──────────────────────────── C# Web App (IIS, on-prem) ────────────────────────────┐
 │                                                                                    │
 │  Controllers ──► Validator ──► (uses) ReferenceDataCache  ◄── CacheRefreshService  │
 │      │                                (in memory)              (background, reads) │
 │      ▼                                                                             │
 │  PendingPostJournal  (App_Data/pending/*.json, crash safety)                       │
 │      │                                                                             │
 │      ▼                                                                             │
 │  NetSuiteGateway                                                                   │
 │   ├─ TotalGate   = 4 slots   (everything)                                          │
 │   ├─ ReadGate    = 2 slots   (reads also need this, so writes always get ≥ 2)      │
 │   ├─ TBA OAuth 1.0 signing (fresh nonce per attempt)                               │
 │   └─ Retry w/ backoff on 429 / concurrency errors / timeouts                       │
 │                                                                                    │
 │  JournalRecoveryService (background): re-sends anything left in the journal        │
 └──────────────────────────────────────┬─────────────────────────────────────────────┘
                                        │ HTTPS (keep-alive, single shared HttpClient)
                                        ▼
 ┌──────────────────────────────── NetSuite ──────────────────────────────────────────┐
 │  RESTlet: dbti_rl_create_transaction  → record.create (standard mode) → save       │
 │      │                                                                             │
 │      ▼                                                                             │
 │  User Event scripts (slimmed): beforeSubmit sets "needs follow-up" flag            │
 │      │                                                                             │
 │      ▼                                                                             │
 │  Map/Reduce: dbti_mr_followup_processing → heavy work, clears flag  (async)        │
 │                                                                                    │
 │  SuiteQL REST endpoint ← cache refresh reads                                       │
 └────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Implementation Steps

### Step 1: Configuration & Authentication

Use **Token-Based Authentication (TBA)**. It needs no login call and no session, and every request is signed on its own. Do **not** use SOAP `login`/`logout` sessions, which are slow and use up concurrency.

**NetSuite setup (admin task):**
1. *Setup > Company > Enable Features > SuiteCloud*: enable **Token-Based Authentication**, **REST Web Services**, and **Server SuiteScript**.
2. *Setup > Integration > Manage Integrations > New*: check **Token-Based Authentication**. Save the **Consumer Key/Secret** (they are shown only once).
3. Create a role with only the permissions the app needs, and assign it to an integration user.
4. *Setup > Users/Roles > Access Tokens > New*: choose the integration, user, and role. Save the **Token ID/Secret**.

**`appsettings.json`:** keep the secrets out of source control. Use environment variables, DPAPI-protected config, or Windows Credential Manager.

```json
{
  "NetSuite": {
    "AccountId": "<1234567_SB1>",
    "ConsumerKey": "<from-env>",
    "ConsumerSecret": "<from-env>",
    "TokenId": "<from-env>",
    "TokenSecret": "<from-env>",
    "CreateTransactionRestletScriptId": "<customscript_dbti_rl_create_transaction>",
    "CreateTransactionRestletDeployId": "<customdeploy_dbti_rl_create_transaction>",
    "MaxConcurrentRequests": 4,
    "MaxConcurrentReads": 2,
    "GateWaitTimeoutSeconds": 15
  }
}
```

```csharp
public sealed class NetSuiteOptions
{
    public string AccountId { get; set; } = "";
    public string ConsumerKey { get; set; } = "";
    public string ConsumerSecret { get; set; } = "";
    public string TokenId { get; set; } = "";
    public string TokenSecret { get; set; } = "";
    public string CreateTransactionRestletScriptId { get; set; } = "";
    public string CreateTransactionRestletDeployId { get; set; } = "";
    public int MaxConcurrentRequests { get; set; } = 4;
    public int MaxConcurrentReads { get; set; } = 2;
    public int GateWaitTimeoutSeconds { get; set; } = 15;

    /// <summary>"1234567_SB1" → "1234567-sb1" (URL host form).</summary>
    public string AccountHost => AccountId.ToLowerInvariant().Replace('_', '-');
    /// <summary>"1234567_SB1" → "1234567_SB1" (OAuth realm form, upper-case).</summary>
    public string Realm => AccountId.ToUpperInvariant();

    public Uri RestletUri => new(
        $"https://{AccountHost}.restlets.api.netsuite.com/app/site/hosting/restlet.nl" +
        $"?script={CreateTransactionRestletScriptId}&deploy={CreateTransactionRestletDeployId}");

    public Uri SuiteQlUri(int limit, int offset) => new(
        $"https://{AccountHost}.suitetalk.api.netsuite.com/services/rest/query/v1/suiteql" +
        $"?limit={limit}&offset={offset}");
}
```

**TBA signer (OAuth 1.0, HMAC-SHA256):**

```csharp
using System.Security.Cryptography;
using System.Text;

public sealed class TbaSigner
{
    private readonly NetSuiteOptions _o;
    public TbaSigner(NetSuiteOptions o) => _o = o;

    // IMPORTANT: call once PER ATTEMPT. Nonce + timestamp must be fresh on every retry.
    public string BuildAuthorizationHeader(HttpMethod method, Uri uri)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        var parameters = new List<KeyValuePair<string, string>>
        {
            new("oauth_consumer_key", _o.ConsumerKey),
            new("oauth_token", _o.TokenId),
            new("oauth_signature_method", "HMAC-SHA256"),
            new("oauth_timestamp", timestamp),
            new("oauth_nonce", nonce),
            new("oauth_version", "1.0"),
        };

        // Query-string params (e.g. script=, deploy=, limit=) MUST be part of the signature.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            parameters.Add(new(Uri.UnescapeDataString(kv[0]),
                               kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : ""));
        }

        var paramString = string.Join("&", parameters
            .Select(p => (K: Enc(p.Key), V: Enc(p.Value)))
            .OrderBy(p => p.K, StringComparer.Ordinal)
            .ThenBy(p => p.V, StringComparer.Ordinal)
            .Select(p => $"{p.K}={p.V}"));

        var baseUrl = $"{uri.Scheme}://{uri.Host.ToLowerInvariant()}{uri.AbsolutePath}";
        var baseString = $"{method.Method.ToUpperInvariant()}&{Enc(baseUrl)}&{Enc(paramString)}";
        var key = $"{Enc(_o.ConsumerSecret)}&{Enc(_o.TokenSecret)}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(baseString)));

        return "OAuth " +
            $"realm=\"{_o.Realm}\", " +
            $"oauth_consumer_key=\"{Enc(_o.ConsumerKey)}\", " +
            $"oauth_token=\"{Enc(_o.TokenId)}\", " +
            $"oauth_signature_method=\"HMAC-SHA256\", " +
            $"oauth_timestamp=\"{timestamp}\", " +
            $"oauth_nonce=\"{nonce}\", " +
            $"oauth_version=\"1.0\", " +
            $"oauth_signature=\"{Enc(signature)}\"";
    }

    // RFC 3986 encoding (Uri.EscapeDataString is RFC 3986-compliant on .NET Core / .NET 5+).
    private static string Enc(string s) => Uri.EscapeDataString(s);
}
```

> **Common pitfall:** `INVALID_LOGIN_ATTEMPT` almost always means the signature base string is wrong. The usual causes are query params left out of the signature, a realm that isn't upper-case, or the server clock being off by more than a few minutes. Keep the on-prem server's clock synced with NTP.

---

### Step 2: The NetSuite Gateway (Concurrency Gate + Retry)

This is the **most important class** in the app. It guarantees that:
- no more than 4 requests are ever in flight;
- reads never use more than 2 slots, so posts always have at least 2;
- a concurrency rejection is retried after a short wait instead of reaching the user.

```csharp
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

public enum NsPriority { Write, Read }

public sealed class GateTimeoutException : Exception
{
    public GateTimeoutException() : base("NetSuite is busy; request was not sent.") { }
}

public sealed class NetSuiteGateway
{
    private readonly HttpClient _http;
    private readonly TbaSigner _signer;
    private readonly NetSuiteOptions _o;
    private readonly ILogger<NetSuiteGateway> _log;
    private readonly SemaphoreSlim _totalGate;
    private readonly SemaphoreSlim _readGate;

    private const int MaxAttempts = 4;

    public NetSuiteGateway(HttpClient http, TbaSigner signer, NetSuiteOptions o, ILogger<NetSuiteGateway> log)
    {
        _http = http; _signer = signer; _o = o; _log = log;
        _totalGate = new SemaphoreSlim(o.MaxConcurrentRequests, o.MaxConcurrentRequests);
        _readGate  = new SemaphoreSlim(o.MaxConcurrentReads,    o.MaxConcurrentReads);
    }

    public async Task<string> SendAsync(HttpMethod method, Uri uri, string? jsonBody,
                                        NsPriority priority, CancellationToken ct,
                                        IDictionary<string, string>? extraHeaders = null)
    {
        var gateTimeout = TimeSpan.FromSeconds(_o.GateWaitTimeoutSeconds);
        var waitSw = Stopwatch.StartNew();

        // Reads must pass BOTH gates; writes only the total gate.
        if (priority == NsPriority.Read && !await _readGate.WaitAsync(gateTimeout, ct))
            throw new GateTimeoutException();
        try
        {
            if (!await _totalGate.WaitAsync(gateTimeout, ct))
                throw new GateTimeoutException();
            try
            {
                var gateWaitMs = waitSw.ElapsedMilliseconds;
                return await SendWithRetryAsync(method, uri, jsonBody, extraHeaders, gateWaitMs, ct);
            }
            finally { _totalGate.Release(); }
        }
        finally
        {
            if (priority == NsPriority.Read) _readGate.Release();
        }
    }

    private async Task<string> SendWithRetryAsync(HttpMethod method, Uri uri, string? jsonBody,
        IDictionary<string, string>? extraHeaders, long gateWaitMs, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            // Build a NEW request each attempt (fresh OAuth nonce/timestamp).
            using var req = new HttpRequestMessage(method, uri);
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(_signer.BuildAuthorizationHeader(method, uri));
            if (extraHeaders != null)
                foreach (var h in extraHeaders) req.Headers.TryAddWithoutValidation(h.Key, h.Value);
            if (jsonBody != null)
                req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var sw = Stopwatch.StartNew();
            try
            {
                using var resp = await _http.SendAsync(req, ct);
                var body = await resp.Content.ReadAsStringAsync(ct);

                _log.LogInformation("NS {Method} {Path} -> {Status} in {Ms}ms (gate wait {GateMs}ms, attempt {Attempt})",
                    method, uri.AbsolutePath, (int)resp.StatusCode, sw.ElapsedMilliseconds, gateWaitMs, attempt);

                if (resp.IsSuccessStatusCode) return body;

                if (IsRetryable(resp.StatusCode, body) && attempt < MaxAttempts)
                {
                    await Task.Delay(Backoff(attempt), ct);
                    continue;
                }
                throw new NetSuiteException((int)resp.StatusCode, body);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested && attempt < MaxAttempts)
            {
                // HttpClient timeout. Safe to retry ONLY because posts are idempotent (externalId).
                _log.LogWarning("NS {Path} timed out on attempt {Attempt}; retrying", uri.AbsolutePath, attempt);
                await Task.Delay(Backoff(attempt), ct);
            }
            catch (HttpRequestException ex) when (attempt < MaxAttempts)
            {
                _log.LogWarning(ex, "NS {Path} network error on attempt {Attempt}; retrying", uri.AbsolutePath, attempt);
                await Task.Delay(Backoff(attempt), ct);
            }
        }
    }

    private static bool IsRetryable(HttpStatusCode status, string body) =>
        status == HttpStatusCode.TooManyRequests ||
        status == HttpStatusCode.ServiceUnavailable ||
        body.Contains("REQUEST_LIMIT_EXCEEDED", StringComparison.OrdinalIgnoreCase) ||
        body.Contains("CONCURRENCY", StringComparison.OrdinalIgnoreCase);

    // 250ms, 500ms, 1000ms (+ up to 200ms jitter so retries don't all fire together)
    private static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt - 1) + Random.Shared.Next(0, 200));
}

public sealed class NetSuiteException : Exception
{
    public int StatusCode { get; }
    public string Body { get; }
    public NetSuiteException(int statusCode, string body)
        : base($"NetSuite returned {statusCode}: {body}") { StatusCode = statusCode; Body = body; }
}
```

**Registration (`Program.cs`):** the gateway **must be a singleton**, otherwise every request gets its own gate and the limit means nothing.

```csharp
var nsOptions = builder.Configuration.GetSection("NetSuite").Get<NetSuiteOptions>()!;
builder.Services.AddSingleton(nsOptions);
builder.Services.AddSingleton<TbaSigner>();

builder.Services.AddHttpClient("netsuite", c =>
{
    // Generous: the server keeps working even if we give up, so a short timeout only causes confusion.
    c.Timeout = TimeSpan.FromSeconds(120);
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),   // pick up DNS changes
    MaxConnectionsPerServer = nsOptions.MaxConcurrentRequests + 2,
});

// ONE gateway for the whole app, so the semaphores are shared by every request.
builder.Services.AddSingleton(sp => new NetSuiteGateway(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("netsuite"),
    sp.GetRequiredService<TbaSigner>(),
    nsOptions,
    sp.GetRequiredService<ILogger<NetSuiteGateway>>()));
```

> **.NET Framework note:** use one `static readonly HttpClient` and one `static` `NetSuiteGateway` instance. Set `ServicePointManager.DefaultConnectionLimit` to at least 6.

---

### Step 3: Reference Data Cache

Items, locations, customers, subsidiaries, and price levels change rarely but are read all the time. Load them **into memory at startup** and refresh them in the background. User screens read **only from the cache**.

```csharp
public sealed record ItemInfo(string Id, string ItemId, string Name, bool IsInactive);
public sealed record LocationInfo(string Id, string Name, bool IsInactive);

public sealed class ReferenceDataCache
{
    // Swapped atomically: readers never see a half-loaded dictionary.
    private volatile IReadOnlyDictionary<string, ItemInfo> _items = new Dictionary<string, ItemInfo>();
    private volatile IReadOnlyDictionary<string, LocationInfo> _locations = new Dictionary<string, LocationInfo>();

    public DateTimeOffset LastRefreshed { get; private set; }
    public bool IsWarm => LastRefreshed != default;

    public bool TryGetItem(string id, out ItemInfo item) => _items.TryGetValue(id, out item!);
    public bool TryGetLocation(string id, out LocationInfo loc) => _locations.TryGetValue(id, out loc!);
    public IEnumerable<ItemInfo> SearchItems(string text, int max = 20) =>
        _items.Values.Where(i => !i.IsInactive &&
            (i.ItemId.Contains(text, StringComparison.OrdinalIgnoreCase) ||
             i.Name.Contains(text, StringComparison.OrdinalIgnoreCase))).Take(max);

    public void Replace(IEnumerable<ItemInfo> items, IEnumerable<LocationInfo> locations)
    {
        _items = items.ToDictionary(i => i.Id);
        _locations = locations.ToDictionary(l => l.Id);
        LastRefreshed = DateTimeOffset.UtcNow;
    }
}
```

**SuiteQL loader.** One query per entity, paged, asking only for the columns you use:

```csharp
using System.Text.Json;

public sealed class SuiteQlClient
{
    private readonly NetSuiteGateway _gw;
    private readonly NetSuiteOptions _o;
    public SuiteQlClient(NetSuiteGateway gw, NetSuiteOptions o) { _gw = gw; _o = o; }

    public async Task<List<JsonElement>> QueryAllAsync(string sql, CancellationToken ct)
    {
        var rows = new List<JsonElement>();
        const int pageSize = 1000;
        for (var offset = 0; ; offset += pageSize)
        {
            var body = JsonSerializer.Serialize(new { q = sql });
            var json = await _gw.SendAsync(HttpMethod.Post, _o.SuiteQlUri(pageSize, offset), body,
                NsPriority.Read, ct, new Dictionary<string, string> { ["Prefer"] = "transient" });

            using var doc = JsonDocument.Parse(json);
            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
                rows.Add(item.Clone());
            if (!doc.RootElement.GetProperty("hasMore").GetBoolean()) break;
        }
        return rows;
    }
}
```

```csharp
public sealed class CacheRefreshService : BackgroundService
{
    private readonly SuiteQlClient _ql;
    private readonly ReferenceDataCache _cache;
    private readonly ILogger<CacheRefreshService> _log;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public CacheRefreshService(SuiteQlClient ql, ReferenceDataCache cache, ILogger<CacheRefreshService> log)
    { _ql = ql; _cache = cache; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var items = (await _ql.QueryAllAsync(
                    "SELECT id, itemid, displayname, isinactive FROM item WHERE isinactive = 'F'", ct))
                    .Select(r => new ItemInfo(S(r, "id"), S(r, "itemid"), S(r, "displayname"), S(r, "isinactive") == "T"));

                var locations = (await _ql.QueryAllAsync(
                    "SELECT id, name, isinactive FROM location WHERE isinactive = 'F'", ct))
                    .Select(r => new LocationInfo(S(r, "id"), S(r, "name"), S(r, "isinactive") == "T"));

                _cache.Replace(items, locations);
                _log.LogInformation("Reference cache refreshed");
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Keep serving the old cache; try again next cycle.
                _log.LogError(ex, "Reference cache refresh failed");
            }
            await Task.Delay(Interval, ct);
        }
    }

    private static string S(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : "";
}
```

```csharp
builder.Services.AddSingleton<ReferenceDataCache>();
builder.Services.AddSingleton<SuiteQlClient>();
builder.Services.AddHostedService<CacheRefreshService>();
```

**Rules for the cache:**
- **Inventory quantities are NOT cached.** They change with every sale, and NetSuite checks them when the record is saved anyway.
- IIS app-pool recycles clear memory. The service reloads on startup. Set the app pool's **Start Mode = AlwaysRunning** and turn on **Preload Enabled**, so the first user doesn't wait for the warm-up.
- If a value a user needs isn't in the cache (for example, a brand-new item), allow a **single** on-demand lookup with `NsPriority.Read`, and add the result to the cache.

> The loader above is the simple version. Step 3b replaces the full 15-minute reload with snapshots and delta refreshes. Build Step 3 first, then upgrade it.

---

### Step 3b: Making Data Fetching Fast

**Target:** most screens load in **under 100 ms** because they never wait on NetSuite. The few that must reach NetSuite make **one** call, not several.

| Source | Typical latency |
|---|---|
| App memory (cache) | < 5 ms |
| Browser cache (no request at all) | 0 ms |
| One SuiteQL / RESTlet call to NetSuite | 300–1,500 ms, plus gate wait at peak |
| Several sequential NetSuite calls | Add them up. This is what makes screens slow |

#### 3b.1 Sort data by how often it changes

Every piece of data a screen shows belongs in one of these tiers. Pick the tier first, then the technique.

| Tier | Examples | Strategy | Freshness |
|---|---|---|---|
| **Static** | Subsidiaries, locations, units, payment terms, tax codes | Full load at startup, then nightly | Hours |
| **Semi-static** | Items, customers, vendors, price levels | Disk snapshot + **delta refresh** every 1–2 min | 1–2 min |
| **Volatile** | Inventory available per location | Short-lived cache (30 s) + **single-flight** + stale-while-revalidate | 30 s |
| **Live** | A specific transaction's status, open orders for one customer | On-demand, **one** call, single-flight | Real time |

#### 3b.2 Delta refresh (fetch only what changed)

Re-downloading 20,000 items every 15 minutes wastes a read slot for many seconds. Instead, ask for **only the rows changed since the last sync**. A delta is usually a handful of rows and takes well under a second, so it can run every minute.

```csharp
public sealed class DeltaSync<T> where T : class
{
    private readonly SuiteQlClient _ql;
    private readonly string _selectSql;         // e.g. "SELECT id, itemid, displayname, isinactive, lastmodifieddate FROM item"
    private readonly Func<JsonElement, T> _map;
    private readonly Func<T, string> _key;

    private ConcurrentDictionary<string, T> _rows = new();
    private DateTime _lastSyncLocal = DateTime.MinValue;   // in the NetSuite account's timezone

    // lastmodifieddate is in account time and has minute precision: overlap to be safe.
    private static readonly TimeSpan Overlap = TimeSpan.FromMinutes(5);

    public DeltaSync(SuiteQlClient ql, string selectSql, Func<JsonElement, T> map, Func<T, string> key)
    { _ql = ql; _selectSql = selectSql; _map = map; _key = key; }

    public IReadOnlyDictionary<string, T> Rows => _rows;

    public async Task SyncAsync(Func<DateTime> nowInAccountTz, CancellationToken ct)
    {
        var started = nowInAccountTz();
        var where = _lastSyncLocal == DateTime.MinValue
            ? ""
            : $" WHERE lastmodifieddate >= TO_DATE('{(_lastSyncLocal - Overlap):yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')";

        foreach (var row in await _ql.QueryAllKeysetAsync(_selectSql + where, ct))
        {
            var item = _map(row);
            _rows[_key(item)] = item;       // upsert: inactivated rows arrive with isinactive = 'T'
        }
        _lastSyncLocal = started;
    }

    public void LoadSnapshot(IEnumerable<T> rows, DateTime lastSyncLocal)
    {
        _rows = new ConcurrentDictionary<string, T>(rows.ToDictionary(_key));
        _lastSyncLocal = lastSyncLocal;
    }

    public void ResetForFullReload() => _lastSyncLocal = DateTime.MinValue;   // run nightly: catches hard deletes
}
```

- Delta refresh **every 1–2 minutes**, plus a **full reload nightly** to catch hard-deleted records, which deltas can't see.
- Filter inactive rows **when reading from the cache**, not in the SQL. Otherwise an item that was just made inactive never reaches the cache, and the cache keeps treating it as active.

#### 3b.3 Disk snapshot: instant startup after an app-pool recycle

Without a database, every IIS recycle means a cold cache and a full reload from NetSuite. Save the cache to disk after each sync, and load it **first** on startup.

```csharp
public sealed class CacheSnapshotStore
{
    private readonly string _dir;
    public CacheSnapshotStore(IWebHostEnvironment env)
    {
        _dir = Path.Combine(env.ContentRootPath, "App_Data", "cache");
        Directory.CreateDirectory(_dir);
    }

    public async Task SaveAsync<T>(string name, IEnumerable<T> rows, DateTime lastSync)
    {
        var path = Path.Combine(_dir, name + ".json");
        var tmp = path + ".tmp";
        await using (var fs = File.Create(tmp))
            await JsonSerializer.SerializeAsync(fs, new Snapshot<T>(lastSync, rows.ToList()));
        File.Move(tmp, path, overwrite: true);
    }

    public async Task<Snapshot<T>?> LoadAsync<T>(string name)
    {
        var path = Path.Combine(_dir, name + ".json");
        if (!File.Exists(path)) return null;
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Snapshot<T>>(fs);
    }
}

public sealed record Snapshot<T>(DateTime LastSync, List<T> Rows);
```

**Startup sequence:** load the snapshot → mark the cache **warm** (users can work immediately) → run a delta sync to catch up on changes made while the app was down. A cold start goes from minutes to seconds.

#### 3b.4 Keyset paging instead of OFFSET

The `QueryAllAsync` in Step 3 pages with `offset`. Large offsets get slower with every page, and the SuiteQL REST endpoint caps how deep offset paging can go. Page by **ID** instead:

```csharp
// Add to SuiteQlClient. The SQL must include "id" and must NOT contain ORDER BY.
public async Task<List<JsonElement>> QueryAllKeysetAsync(string sql, CancellationToken ct)
{
    var rows = new List<JsonElement>();
    const int pageSize = 1000;
    long lastId = 0;
    var hasWhere = sql.Contains(" WHERE ", StringComparison.OrdinalIgnoreCase);

    while (true)
    {
        var pageSql = $"{sql} {(hasWhere ? "AND" : "WHERE")} id > {lastId} ORDER BY id";
        var body = JsonSerializer.Serialize(new { q = pageSql });
        var json = await _gw.SendAsync(HttpMethod.Post, _o.SuiteQlUri(pageSize, 0), body,
            NsPriority.Read, ct, new Dictionary<string, string> { ["Prefer"] = "transient" });

        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");
        foreach (var item in items.EnumerateArray()) rows.Add(item.Clone());
        if (items.GetArrayLength() < pageSize) break;
        lastId = long.Parse(items[items.GetArrayLength() - 1].GetProperty("id").GetString()!);
    }
    return rows;
}
```

#### 3b.5 Single-flight: 10 identical requests become 1 NetSuite call

When 10 cashiers open the same location's stock screen at once, without single-flight that's 10 identical NetSuite calls competing for 2 read slots. With single-flight, the first request makes the call and the other 9 wait for **the same result**.

```csharp
using System.Collections.Concurrent;

public sealed class SingleFlight<T>
{
    private readonly ConcurrentDictionary<string, Lazy<Task<T>>> _inFlight = new();

    public async Task<T> RunAsync(string key, Func<Task<T>> fetch)
    {
        var lazy = _inFlight.GetOrAdd(key, _ => new Lazy<Task<T>>(fetch));
        try { return await lazy.Value; }
        finally { _inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<T>>>(key, lazy)); }
    }
}
```

#### 3b.6 Volatile data: short cache + stale-while-revalidate

For inventory availability: if the cached value is **fresh** (under 30 s old), return it. If it's **stale but usable** (under 5 min), return it **immediately** and refresh in the background. Only make the user wait when there's nothing usable at all.

```csharp
public sealed class InventoryCache
{
    private sealed record Entry(IReadOnlyDictionary<string, decimal> AvailableByItem, DateTimeOffset LoadedAt);

    private readonly SuiteQlClient _ql;
    private readonly ConcurrentDictionary<long, Entry> _byLocation = new();
    private readonly SingleFlight<Entry> _flight = new();
    private static readonly TimeSpan Fresh = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxStale = TimeSpan.FromMinutes(5);

    public InventoryCache(SuiteQlClient ql) => _ql = ql;

    public async Task<IReadOnlyDictionary<string, decimal>> GetAvailableAsync(long locationId)
    {
        if (_byLocation.TryGetValue(locationId, out var e))
        {
            var age = DateTimeOffset.UtcNow - e.LoadedAt;
            if (age < Fresh) return e.AvailableByItem;
            if (age < MaxStale)
            {
                _ = RefreshAsync(locationId);          // fire-and-forget, deduplicated by single-flight
                return e.AvailableByItem;
            }
        }
        return (await RefreshAsync(locationId)).AvailableByItem;
    }

    // Call after a successful post so the user sees their own change at once.
    public void Invalidate(long locationId) => _byLocation.TryRemove(locationId, out _);

    private Task<Entry> RefreshAsync(long locationId) =>
        _flight.RunAsync($"inv:{locationId}", async () =>
        {
            // locationId is a long, so it cannot carry SQL injection. NEVER concatenate raw strings into SuiteQL.
            var rows = await _ql.QueryAllAsync(
                $"SELECT item, quantityavailable FROM inventoryitemlocations " +
                $"WHERE location = {locationId} AND quantityavailable <> 0", CancellationToken.None);

            var entry = new Entry(
                rows.ToDictionary(r => r.GetProperty("item").ToString(),
                                  r => decimal.Parse(r.GetProperty("quantityavailable").ToString(),
                                                     System.Globalization.CultureInfo.InvariantCulture)),
                DateTimeOffset.UtcNow);
            _byLocation[locationId] = entry;
            return entry;
        });
}
```

> The cached availability is for **display and pre-validation only**. NetSuite checks inventory again when the record is saved, so a stale number can never cause a wrong post. At worst the user gets a clear error.

#### 3b.7 One call per screen (RESTlet "screen bundle")

If a screen needs item details, availability, and a customer's open orders, that's 3 sequential calls, each waiting for a read slot. A RESTlet runs all three queries **inside NetSuite** and returns them in **one** response, using one slot and one network round trip.

`dbti_rl_screen_data.js`

```javascript
/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 * @description Returns everything the order-entry screen needs in one round trip.
 */
define(['N/query'], (query) => {

    const run = (sql, params) => query.runSuiteQL({ query: sql, params }).asMappedResults();

    const post = (body) => {
        const locationId = Number(body.locationId);
        const customerId = Number(body.customerId);

        return {
            availability: run(
                `SELECT item, quantityavailable
                   FROM inventoryitemlocations
                  WHERE location = ? AND quantityavailable <> 0`, [locationId]),

            openOrders: customerId ? run(
                `SELECT id, tranid, trandate, status
                   FROM transaction
                  WHERE type = 'SalesOrd' AND entity = ? AND status NOT IN ('SalesOrd:G','SalesOrd:H','SalesOrd:C')
                  ORDER BY trandate DESC
                  FETCH FIRST 20 ROWS ONLY`, [customerId]) : [],
        };
    };

    return { post };
});
```

- Inside a RESTlet, `N/query` **supports bind parameters** (`?`). Always use them.
- Return **IDs**, not display names, and resolve names from the app's cache. `BUILTIN.DF()` and joins only to get names make queries slower.
- Return only the columns the screen actually shows.

#### 3b.8 Write faster SuiteQL

| Do | Avoid |
|---|---|
| Filter on `id`, `lastmodifieddate`, `location`, `entity`, `type`, `trandate` | `LIKE '%text%'` in NetSuite. Do text search **in app memory** |
| Select specific columns | `SELECT *` |
| `FETCH FIRST n ROWS ONLY` for "latest N" lists | Pulling everything and trimming in C# |
| Date ranges (`trandate >= ...`) on transaction queries | Unbounded transaction queries |
| Resolve names from the cache | `BUILTIN.DF()` on every column |

#### 3b.9 Transport: compression and warm connections

```csharp
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
    MaxConnectionsPerServer = nsOptions.MaxConcurrentRequests + 2,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,  // large JSON shrinks ~80-90%
});
```

Also turn on **response compression** between IIS and the browser (`builder.Services.AddResponseCompression()` + `app.UseResponseCompression()`, or IIS dynamic compression).

#### 3b.10 Push work to the browser

The fastest request is one that never happens.

- **Send the full item and location lists to the browser once**, with an `ETag` based on the cache version. The browser keeps a copy, and **typeahead filters locally** with zero network calls per keystroke.

```csharp
[HttpGet("api/reference/items")]
public IActionResult Items([FromServices] ReferenceDataCache cache)
{
    var etag = $"\"{cache.LastRefreshed.UtcTicks}\"";
    if (Request.Headers.IfNoneMatch == etag) return StatusCode(304);   // browser already has it
    Response.Headers.ETag = etag;
    Response.Headers.CacheControl = "private, no-cache";               // revalidate each time, cheap 304
    return Ok(cache.SearchItems("", int.MaxValue).Select(i => new { i.Id, i.ItemId, i.Name }));
}
```

- **Prefetch on login:** once the user's location is known, request that location's availability in the background before they open the order screen.
- **Use virtual scrolling** for long lists. Rendering 5,000 DOM rows is often slower than the data fetch.

#### 3b.11 Measure every read

Log these for every read: **source** (`memory` / `nsCall`), **gate wait ms**, **NetSuite ms**, **rows**, and **bytes**. In the first week, look at:
- **Screens that still call NetSuite on every load:** move them up a tier, or to a screen bundle.
- **Single-flight hit rate:** it should be high for inventory at peak.
- **Read gate wait above 500 ms:** too many live reads. Cache more aggressively, but don't raise the read gate.

---

### Step 4: Pre-Validation

Reject bad input **before** it takes up a NetSuite slot.

```csharp
public sealed record TransactionLine(string ItemId, decimal Quantity, decimal? Rate);
public sealed record PostTransactionRequest(
    string ExternalId,            // generated by the app, see Step 7
    string RecordType,            // "salesorder", "transferorder", ...
    string LocationId,
    string? EntityId,
    string? TransferLocationId,
    List<TransactionLine> Lines);

public sealed class TransactionValidator
{
    private readonly ReferenceDataCache _cache;
    public TransactionValidator(ReferenceDataCache cache) => _cache = cache;

    public List<string> Validate(PostTransactionRequest r)
    {
        var errors = new List<string>();
        if (!_cache.IsWarm) { errors.Add("System is still loading reference data. Try again in a moment."); return errors; }
        if (r.Lines.Count == 0) errors.Add("At least one line is required.");
        if (!_cache.TryGetLocation(r.LocationId, out _)) errors.Add($"Unknown location {r.LocationId}.");
        if (r.RecordType == "transferorder" && r.TransferLocationId == r.LocationId)
            errors.Add("From and To locations must differ.");

        for (var i = 0; i < r.Lines.Count; i++)
        {
            var l = r.Lines[i];
            if (!_cache.TryGetItem(l.ItemId, out var item) || item.IsInactive)
                errors.Add($"Line {i + 1}: item {l.ItemId} is unknown or inactive.");
            if (l.Quantity <= 0)
                errors.Add($"Line {i + 1}: quantity must be greater than zero.");
        }
        return errors;
    }
}
```

---

### Step 5: The "Create Transaction" RESTlet

One call creates the transaction and returns `{ id, tranId }`. Main points:
- **`isDynamic: false` (standard mode)** is much faster because NetSuite doesn't recalculate after every field.
- **Idempotent:** it first checks whether a transaction with this `externalId` already exists. If it does, it returns that transaction instead of creating a duplicate.
- **Field allowlist:** the RESTlet only sets fields we explicitly allow. Never pass arbitrary field IDs from the client.

`dbti_rl_create_transaction.js`

```javascript
/**
 * @NApiVersion 2.1
 * @NScriptType Restlet
 * @description Synchronously creates a transaction and returns its internal ID and document number.
 *              Idempotent on externalId: safe for the C# app to retry.
 */
define(['N/record', 'N/search', 'N/log'], (record, search, log) => {

    const SUPPORTED_TYPES = {
        salesorder: record.Type.SALES_ORDER,
        transferorder: record.Type.TRANSFER_ORDER,
        purchaseorder: record.Type.PURCHASE_ORDER,
        invoice: record.Type.INVOICE,
    };

    const findByExternalId = (type, externalId) => {
        const results = search.create({
            type,
            filters: [['externalidstring', 'is', externalId], 'AND', ['mainline', 'is', 'T']],
            columns: ['tranid'],
        }).run().getRange({ start: 0, end: 1 });

        return results.length ? { id: results[0].id, tranId: results[0].getValue('tranid') } : null;
    };

    const post = (body) => {
        const type = SUPPORTED_TYPES[body.recordType];
        if (!type) return { status: 'error', code: 'UNSUPPORTED_TYPE', message: `Unsupported type ${body.recordType}` };
        if (!body.externalId) return { status: 'error', code: 'MISSING_EXTERNAL_ID', message: 'externalId is required' };

        const existing = findByExternalId(type, body.externalId);
        if (existing) return { status: 'duplicate', ...existing };

        try {
            const rec = record.create({ type, isDynamic: false });

            rec.setValue({ fieldId: 'externalid', value: body.externalId });
            rec.setValue({ fieldId: 'location', value: body.locationId });
            if (body.entityId) rec.setValue({ fieldId: 'entity', value: body.entityId });
            if (body.transferLocationId) rec.setValue({ fieldId: 'transferlocation', value: body.transferLocationId });

            body.lines.forEach((line, i) => {
                rec.setSublistValue({ sublistId: 'item', fieldId: 'item', line: i, value: line.itemId });
                rec.setSublistValue({ sublistId: 'item', fieldId: 'quantity', line: i, value: line.quantity });
                if (line.rate != null) rec.setSublistValue({ sublistId: 'item', fieldId: 'rate', line: i, value: line.rate });
            });

            const id = rec.save({ enableSourcing: true, ignoreMandatoryFields: false });
            const tranId = search.lookupFields({ type, id, columns: ['tranid'] }).tranid;
            return { status: 'created', id: String(id), tranId };

        } catch (e) {
            // Duplicate race: two retries arrived at the same time; the other one won.
            const raced = findByExternalId(type, body.externalId);
            if (raced) return { status: 'duplicate', ...raced };

            log.error({ title: 'create failed', details: `${body.externalId}: ${e.name} ${e.message}` });
            return { status: 'error', code: e.name, message: e.message };
        }
    };

    return { post };
});
```

> **Date fields:** `setValue` for date fields needs a JavaScript `Date` object, not a string. Convert the value in the RESTlet (for example, `new Date(body.tranDate)`).
>
> **Deployment:** deploy with **Status = Released**, **Log Level = Error** (Debug logging slows things down), and an audience of only the integration role.

---

### Step 6: Pending-Post Journal (Crash Safety Without a Database)

There's no database, so if IIS recycles in the middle of a post we need to know what was in flight. Before calling NetSuite, write the request to disk. Delete the file after success. A background service re-sends anything left over, and because the RESTlet is idempotent, re-sending is always safe.

```csharp
using System.Text.Json;

public sealed class PendingPostJournal
{
    private readonly string _dir;
    public PendingPostJournal(IWebHostEnvironment env)
    {
        _dir = Path.Combine(env.ContentRootPath, "App_Data", "pending");
        Directory.CreateDirectory(_dir);
    }

    private string PathFor(string externalId) => Path.Combine(_dir, $"{externalId}.json");

    public async Task WriteAsync(PostTransactionRequest r)
    {
        var tmp = PathFor(r.ExternalId) + ".tmp";
        await File.WriteAllTextAsync(tmp, JsonSerializer.Serialize(r));
        File.Move(tmp, PathFor(r.ExternalId), overwrite: true);   // atomic on NTFS
    }

    public void Complete(string externalId) => File.Delete(PathFor(externalId));

    public IEnumerable<PostTransactionRequest> ReadAll(TimeSpan olderThan) =>
        Directory.EnumerateFiles(_dir, "*.json")
            .Where(f => File.GetLastWriteTimeUtc(f) < DateTime.UtcNow - olderThan)
            .Select(f => JsonSerializer.Deserialize<PostTransactionRequest>(File.ReadAllText(f))!);
}
```

```csharp
public sealed class JournalRecoveryService : BackgroundService
{
    private readonly PendingPostJournal _journal;
    private readonly TransactionPoster _poster;
    private readonly ILogger<JournalRecoveryService> _log;

    public JournalRecoveryService(PendingPostJournal j, TransactionPoster p, ILogger<JournalRecoveryService> log)
    { _journal = j; _poster = p; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // Only pick up entries older than 2 min so we don't race a live request.
            foreach (var pending in _journal.ReadAll(TimeSpan.FromMinutes(2)))
            {
                try
                {
                    var result = await _poster.SendAsync(pending, ct);
                    _journal.Complete(pending.ExternalId);
                    _log.LogInformation("Recovered {ExternalId} -> {TranId}", pending.ExternalId, result.TranId);
                }
                catch (Exception ex) { _log.LogError(ex, "Recovery failed for {ExternalId}", pending.ExternalId); }
            }
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
```

> Make sure the IIS app-pool identity has **write** permission on `App_Data\pending`, and exclude that folder from deployment cleanup.

---

### Step 7: The Posting Endpoint (Putting It Together)

```csharp
public sealed record PostResult(string Status, string? Id, string? TranId, string? Code, string? Message);

public sealed class TransactionPoster
{
    private readonly NetSuiteGateway _gw;
    private readonly NetSuiteOptions _o;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public TransactionPoster(NetSuiteGateway gw, NetSuiteOptions o) { _gw = gw; _o = o; }

    public async Task<PostResult> SendAsync(PostTransactionRequest r, CancellationToken ct)
    {
        var json = await _gw.SendAsync(HttpMethod.Post, _o.RestletUri,
            JsonSerializer.Serialize(r, Json), NsPriority.Write, ct);
        return JsonSerializer.Deserialize<PostResult>(json, Json)!;
    }
}
```

```csharp
[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly TransactionValidator _validator;
    private readonly PendingPostJournal _journal;
    private readonly TransactionPoster _poster;

    public TransactionsController(TransactionValidator v, PendingPostJournal j, TransactionPoster p)
    { _validator = v; _journal = j; _poster = p; }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] PostTransactionRequest request, CancellationToken ct)
    {
        // 1. Validate locally (no NetSuite slot used).
        var errors = _validator.Validate(request);
        if (errors.Count > 0) return BadRequest(new { errors });

        // 2. Journal first, so nothing is lost if the process dies mid-call.
        await _journal.WriteAsync(request);

        try
        {
            // 3. One synchronous call to NetSuite.
            //    Use CancellationToken.None: if the browser disconnects we still want the post to finish.
            var result = await _poster.SendAsync(request, CancellationToken.None);

            if (result.Status is "created" or "duplicate")
            {
                _journal.Complete(request.ExternalId);
                return Ok(result);   // { status, id, tranId } → show tranId to the user
            }

            // Business error from NetSuite (e.g. insufficient inventory). Don't retry; tell the user.
            _journal.Complete(request.ExternalId);
            return UnprocessableEntity(result);
        }
        catch (GateTimeoutException)
        {
            // Extremely busy: leave it in the journal; recovery service will post it.
            return Accepted(new { status = "queued", externalId = request.ExternalId,
                message = "Saved and queued. It will post to NetSuite within a minute." });
        }
    }
}
```

**Where does `externalId` come from?**
Generate it **in the browser when the form opens** (for example, `crypto.randomUUID()`, or a readable form like `WEB-<storeCode>-<yyyyMMddHHmmss>-<random4>`). Send the **same** value if the user double-clicks or resubmits. That gives you duplicate protection from start to finish, including against double-clicks.

**UI rules:**
- Disable the Submit button and show a spinner while the post is in flight.
- On `200`, show the **tranId** (e.g. `SO-10234`).
- On `202 queued`, show "Saved, posting…" and poll a status endpoint, or just tell the user it will appear shortly.
- On `422`, show NetSuite's message (e.g. "Insufficient quantity at location X").

**Registration:**

```csharp
builder.Services.AddSingleton<TransactionValidator>();
builder.Services.AddSingleton<PendingPostJournal>();
builder.Services.AddSingleton<TransactionPoster>();
builder.Services.AddHostedService<JournalRecoveryService>();
```

---

### Step 8: Slim Down the User Event Scripts

**This is where most of the speed improvement comes from.** Every User Event (UE) script deployed on Sales Order, Invoice, Transfer Order, and Purchase Order runs **inside** our RESTlet's save, and the save holds a concurrency slot the whole time. Scripts to review in this repo:

| Script | Why it matters |
|---|---|
| `files/dbti_ue_purchase_order.js` | Many load/save/search calls; review first |
| `files/dbti_ue_inctercompany_transfer_order.js` | Many load/save/search calls; review first |
| `files/dbti_ue_transfer_order.js` | Runs on every TO post |
| `files/dbti_ue_sales_order.js` | Runs on every SO post |
| `files/dbti_ue_invoice.js` | Runs on every invoice post |
| `files/dbti_server_utils.js` | Shared helpers: check for hidden `record.load`/`save` calls |

#### 8a. Sort every piece of logic into one of two groups

For each block of code in `beforeSubmit` / `afterSubmit`, ask: **"Does the user need this result before they see their transaction number?"**

| Synchronous (keep in UE) | Deferred (move to Map/Reduce) |
|---|---|
| Validation that should block the save | Updating *other* records (related POs, ICTOs, custom records) |
| Setting a field on *this* record (do it in `beforeSubmit`) | Re-saving the record |
| | Emails, notifications, logging to custom records |
| | Recalculations that are only needed for reports |

#### 8b. Patterns

**Set fields on the current record in `beforeSubmit`, never in `afterSubmit`.** Changing `newRecord` in `beforeSubmit` is free. Doing it in `afterSubmit` means a second `load` + `save`.

```javascript
// SLOW: afterSubmit
const rec = record.load({ type: ctx.newRecord.type, id: ctx.newRecord.id });
rec.setValue({ fieldId: 'custbody_x', value: 'y' });
rec.save();

// FAST: beforeSubmit
ctx.newRecord.setValue({ fieldId: 'custbody_x', value: 'y' });
```

**If you must update another record, use `submitFields`, not `load` + `save`:**

```javascript
record.submitFields({
    type: record.Type.PURCHASE_ORDER, id: poId,
    values: { custbody_dbti_linked_so: soId },
    options: { enableSourcing: false, ignoreMandatoryFields: true },
});
```

**Defer heavy work with a flag plus Map/Reduce.**

1. Create a checkbox body field: `custbody_dbti_followup_pending`.
2. In the UE's `beforeSubmit`, set the flag (this costs nothing):

```javascript
const beforeSubmit = (ctx) => {
    if (ctx.type !== ctx.UserEventType.CREATE) return;
    // ...blocking validation only...
    ctx.newRecord.setValue({ fieldId: 'custbody_dbti_followup_pending', value: true });
};
```

3. In `afterSubmit`, *try* to start the follow-up job right away (it may already be running, which is fine):

```javascript
const afterSubmit = (ctx) => {
    if (ctx.type !== ctx.UserEventType.CREATE) return;
    try {
        task.create({
            taskType: task.TaskType.MAP_REDUCE,
            scriptId: 'customscript_dbti_mr_followup_processing',
            deploymentId: 'customdeploy_dbti_mr_followup_processing',
        }).submit();
    } catch (e) {
        // Already queued/running: the scheduled deployment will pick it up.
        log.debug({ title: 'follow-up already queued', details: e.name });
    }
};
```

4. The Map/Reduce script `dbti_mr_followup_processing` does this:
   - `getInputData`: search for transactions where `custbody_dbti_followup_pending = T`.
   - `map`: does the heavy work for one transaction. **It must be idempotent**, meaning running it twice must be safe.
   - At the end, clears the flag with `record.submitFields`.
   - Has a **scheduled deployment every 15 minutes** as a safety net, plus extra deployments (copies) so `task.create` has somewhere to queue.

**Skip logic the RESTlet path doesn't need:**

```javascript
const runtime = require('N/runtime');
if (runtime.executionContext === runtime.ContextType.RESTLET) {
    // Skip UI-only behaviour (e.g. defaulting fields the app already sends).
}
```

> **Note:** User Event scripts do **not** trigger other User Event scripts when they save records. Saves from scheduled scripts, Map/Reduce scripts, and RESTlets **do** trigger them. Keep that in mind when moving logic so you don't end up with loops or double processing.

#### 8c. Measure before and after

Use **Customization > Performance > Application Performance Monitor (APM)** to compare average and 95th-percentile save times for each record type and script. **Target: under 2 s per save.**

---

## 5. Monitoring & Tuning

| What | Where | Healthy |
|---|---|---|
| Peak concurrency / rejected requests | NetSuite: *Setup > Integration > Integration Management > Integration Governance* | Rejected ≈ 0 |
| Save duration per record type | NetSuite APM | p95 < 2 s |
| **Gate wait time** (`gate wait {GateMs}ms` in our logs) | App logs | p95 < 1 s |
| Retries per call | App logs (`attempt > 1`) | Rare |
| `202 queued` responses | App logs | Almost never |
| Files left in `App_Data/pending` | Disk | Empty (or briefly 1–2) |

**Tuning guide:**
- **Gate wait is high but NetSuite shows no rejections:** saves are too slow. Work on Step 8.
- **NetSuite shows rejections:** something outside this app is also using slots (CSV imports, other integrations). Lower `MaxConcurrentRequests` to 3, or use **Integration Governance** to reserve slots for this integration.
- **Posts are fast but searches feel slow:** cache more data. Don't raise `MaxConcurrentReads` above 2.
- **Still not enough after all of the above:** each **SuiteCloud Plus** license adds 10 concurrent requests. Use the numbers above to make the business case.

---

## 6. Load Testing Before Go-Live

Run these in **Sandbox** with production-like User Event scripts deployed:

1. **Baseline:** 1 user, 20 posts. Record the average save time.
2. **Target load:** 30 virtual users posting at the same moment (for example, with [NBomber](https://nbomber.com/) or a simple `Parallel.ForEachAsync` console app calling *our* `/api/transactions`, **not** NetSuite directly).
3. **Burst:** 30 users all clicking Submit within the same second.
4. **Chaos:** recycle the IIS app pool mid-test. Confirm the journal recovers every post, with **no duplicates** (search by `externalId`).
5. **Double-submit:** send the same `externalId` 5 times at once. Exactly one transaction should exist.

**Pass criteria:** zero errors shown to users, zero duplicates, p95 end-to-end post time under 5 s during the burst.

---

## 7. Do / Don't Checklist

**Do**
- [ ] Route every NetSuite call through the singleton `NetSuiteGateway`.
- [ ] Send an `externalId` on every transaction, generated once per form.
- [ ] Build a new signed request for every retry attempt.
- [ ] Read reference data from `ReferenceDataCache`.
- [ ] Validate locally before posting.
- [ ] Set fields on the current record in `beforeSubmit`.
- [ ] Defer non-essential `afterSubmit` work to Map/Reduce.
- [ ] Log duration, gate wait, and attempt count on every call.
- [ ] Refresh semi-static data with deltas (`lastmodifieddate`) and keep a disk snapshot for fast startup.
- [ ] Wrap every live read in single-flight.
- [ ] Call `InventoryCache.Invalidate(location)` after a successful post.
- [ ] Give each screen one RESTlet "bundle" call instead of several sequential calls.
- [ ] Use bind parameters (`?`) in RESTlet SuiteQL; only build REST SuiteQL from numeric IDs.

**Don't**
- [ ] Don't `new HttpClient()` per request.
- [ ] Don't call NetSuite from a typeahead or dropdown.
- [ ] Don't reload whole tables on a timer when a delta will do.
- [ ] Don't use `LIKE '%…%'`, `SELECT *`, or `BUILTIN.DF()` everywhere in SuiteQL.
- [ ] Don't use SOAP login sessions.
- [ ] Don't `record.load` + `save` the same record in `afterSubmit`.
- [ ] Don't use `isDynamic: true` in the RESTlet unless a field truly requires it.
- [ ] Don't retry business errors (insufficient inventory, closed period). Only retry concurrency, timeout, and network errors.
- [ ] Don't set the HttpClient timeout low. NetSuite keeps processing after we give up.
- [ ] Don't leave RESTlet or UE log level on **Debug** in Production.

---

## 8. Glossary

| Term | Meaning |
|---|---|
| **Concurrency limit** | Maximum number of API requests NetSuite runs at the same moment for the account (ours: 5). |
| **Gate / semaphore** | A counter in our app that lets only N requests through at once; the rest wait their turn. |
| **TBA** | Token-Based Authentication: OAuth 1.0 request signing with consumer + token keys. No login session. |
| **RESTlet** | A SuiteScript that exposes our own REST endpoint inside NetSuite. |
| **SuiteQL** | SQL-like query language for reading NetSuite data through the REST API. |
| **User Event (UE) script** | SuiteScript that runs automatically when a record is created, edited, or viewed (`beforeLoad`, `beforeSubmit`, `afterSubmit`). |
| **Map/Reduce (MR)** | Asynchronous, queued SuiteScript for background or bulk processing. Doesn't use web-service concurrency. |
| **Idempotent** | Safe to run more than once. The result is the same as running it once. |
| **externalId** | Our own unique key stored on the NetSuite record; used to detect duplicates. |
| **APM** | Application Performance Monitor, NetSuite's tool for measuring script and record performance. |
| **p95** | 95th percentile: 95% of requests are faster than this value. |
