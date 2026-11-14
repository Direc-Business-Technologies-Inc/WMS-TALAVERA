using System.Text.Json;
using Application.DataTransferObjects.Others;
using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Integration.NS.DataTransferObjects.StockTransferRequest;
using Integration.NS.Implementations.Transactions;
using Integration.NS.Services;
using Integration.NS.Tests.Fakes;
using Integration.NS.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Shared.Entities;
using Shared.Libraries.Utilities;
using Xunit;

namespace Integration.NS.Tests.Implementations.Transactions;

/// <summary>
/// Feature 2026-10-09: STR "Requested By" is captured from a NetSuite employee
/// dropdown, persisted to the standard NetSuite <c>employee</c> field, read back
/// as id + name, and surfaced as a list-grid column.
///
/// Coverage limitation: there is no test project for <c>Web.BlazorServer</c> or
/// <c>Application.UseCases</c>, so the STRForm markup (T-4.2) and the
/// HandleSubmit Requested By guard (T-4.3) have NO automated coverage. AC-1,
/// AC-2, AC-5, AC-7 and AC-11 therefore require manual verification. Do not
/// invent a test project to cover them.
/// </summary>
public class StockTransferRequestIntegrationTests
{
    [Fact]
    public async Task Date_range_uses_native_trandate_on_all_three_str_lists()
    {
        var (sut, client) = CreateCapturing();
        foreach (var list in new Func<DataGridIntent, Task>[]
        {
            async intent => { await sut.GetTransferOrderList(intent); },
            async intent => { await sut.GetIntercompanyTransferOrderList(intent); },
            async intent => { await sut.GetReturnsList(intent); }
        })
        {
            var intent = new DataGridIntent { Take = 10 };
            intent.Filters.Add(DataGridFilterUtilities.GreaterThanOrEqual("Date", new DateTime(2026, 10, 29)));
            await list(intent);

            Assert.NotNull(client.LastQuery);
            Assert.Contains("t.trandate >= TO_DATE('2026-10-29T00:00:00'", client.LastQuery);
            Assert.DoesNotContain("TO_DATE(t.trandate", client.LastQuery);
            Assert.DoesNotContain("WHERE TO_CHAR(t.trandate", client.LastQuery);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Print_count_filter_is_applied_before_paging_on_all_three_transaction_lists(bool printed)
    {
        var (sut, client) = CreateCapturing();
        var expectedPredicate = printed
            ? "t.custbody_wms_print_count >= 1"
            : "t.custbody_wms_print_count IS NULL";

        foreach (var list in new Func<DataGridIntent, Task>[]
        {
            async intent => { await sut.GetTransferOrderList(intent); },
            async intent => { await sut.GetIntercompanyTransferOrderList(intent); },
            async intent => { await sut.GetReturnsList(intent); }
        })
        {
            var intent = new DataGridIntent { Take = 10 };
            intent.Filters.Add(printed
                ? DataGridFilterUtilities.GreaterThanOrEqual("t.custbody_wms_print_count", 1)
                : DataGridFilterUtilities.IsNull("t.custbody_wms_print_count"));

            await list(intent);

            Assert.NotNull(client.LastQuery);
            Assert.Contains(expectedPredicate, client.LastQuery);
        }
    }

    private static (StockTransferRequestIntegration Sut, CapturingNetSuiteClient Client) CreateCapturing()
    {
        var client = new CapturingNetSuiteClient();
        var sut = new StockTransferRequestIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService());
        return (sut, client);
    }

    private static (StockTransferRequestIntegration Sut, RestCapturingNetSuiteClient Client) CreateRestCapturing()
    {
        var client = new RestCapturingNetSuiteClient();
        var sut = new StockTransferRequestIntegration(
            client,
            new HttpContextAccessor(),
            new SuiteQLQueryBuilderFactoryService());
        return (sut, client);
    }

    private static StockTransferRequestInfoDTO SampleCreateDto(EmployeeNsDTO? requestedBy) => new()
    {
        Subsidiary = new SubsidiaryDTO { Id = 1, Name = "DBTI" },
        SourceLocation = new LocationDTO { Id = 10, Name = "Main" },
        DestinationLocation = new LocationDTO { Id = 11, Name = "Out" },
        TransferCategory = TransferCategory.Transfer,
        RequestedBy = requestedBy,
        PreparedById = 77,
        Lines = []
    };

    private static EmployeeNsDTO SampleEmployee(int nsId = 42) => new()
    {
        NsId = nsId,
        FirstName = "Juan",
        LastName = "Dela Cruz"
    };

    private static JsonElement PayloadOf(string? body)
    {
        Assert.False(string.IsNullOrWhiteSpace(body), "no REST payload was captured");
        return JsonDocument.Parse(body!).RootElement;
    }

    // ---------------------------------------------------------------------
    // T-6.1 — P-1, AC-3: create payload carries the employee reference.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Create_payload_includes_employee_id_when_requested_by_set()
    {
        var (sut, client) = CreateRestCapturing();

        await sut.CreateStockTransferRequest(SampleCreateDto(SampleEmployee(42)));

        var payload = PayloadOf(client.LastBody);
        Assert.Equal(
            JsonValueKind.Object,
            payload.GetProperty("employee").ValueKind);
        Assert.Equal("42", payload.GetProperty("employee").GetProperty("id").GetString());
    }

    // ---------------------------------------------------------------------
    // T-6.2 — P-2: WhenWritingNull must OMIT the key, never send `"employee": null`.
    // NetSuite treats an explicit null on a mandatory field as a document-level
    // rejection, so omission and null are not interchangeable here.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Create_payload_omits_employee_key_when_requested_by_null()
    {
        var (sut, client) = CreateRestCapturing();

        await sut.CreateStockTransferRequest(SampleCreateDto(null));

        var payload = PayloadOf(client.LastBody);
        Assert.False(payload.TryGetProperty("employee", out _));
    }

    // ---------------------------------------------------------------------
    // T-6.3 — P-1, AC-6: update shares CreateSTRPayload, so it must emit the
    // employee too. Skipping this would silently leave the field unchanged.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Update_payload_includes_employee_id()
    {
        var (sut, client) = CreateRestCapturing();
        var dto = SampleCreateDto(SampleEmployee(99));
        dto.Id = 555;

        await sut.UpdateStockTransferRequest(dto);

        var payload = PayloadOf(client.LastBody);
        Assert.Equal("99", payload.GetProperty("employee").GetProperty("id").GetString());
        Assert.Equal(HttpMethod.Patch, client.LastMethod);
    }

    // ---------------------------------------------------------------------
    // T-6.4 — P-3, AC-4: the header read must select the id AND the name.
    // Name-only is the defect Prepared By already exhibits; id-only would leave
    // the dropdown unable to render without a second round-trip.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Header_query_selects_employee_id_and_joins_re_alias()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetStockTransferRequest("TO-1");

        Assert.NotNull(client.LastQuery);
        Assert.Contains("re.firstname AS RequestedByFirstName", client.LastQuery);
        Assert.Contains("re.lastname AS RequestedByLastName", client.LastQuery);
        Assert.Contains("t.employee AS RequestedById", client.LastQuery);
        Assert.Contains("LEFT JOIN employee re ON re.id = t.employee", client.LastQuery);
    }

    // The header read must compose EmployeeNsDTO from the flat NS columns so
    // EmployeeNsVM.FullName is computable on load.
    [Fact]
    public async Task Header_result_maps_requested_by_id_and_name()
    {
        var (sut, client) = CreateCapturing();
        client.NextResult = new StockTransferRequestHeaderNSDTO
        {
            Id = 1,
            RequestedById = 42,
            RequestedByFirstName = "Juan",
            RequestedByLastName = "Dela Cruz"
        };

        var result = await sut.GetStockTransferRequest("TO-1");

        Assert.NotNull(result);
        Assert.NotNull(result!.RequestedBy);
        Assert.Equal(42, result.RequestedBy!.NsId);
        Assert.Equal("Juan", result.RequestedBy.FirstName);
        Assert.Equal("Dela Cruz", result.RequestedBy.LastName);
    }

    // A legacy record with no employee must surface null, not a zero-id stub,
    // so the update-form guard can detect it.
    [Fact]
    public async Task Header_result_leaves_requested_by_null_when_not_set()
    {
        var (sut, client) = CreateCapturing();
        client.NextResult = new StockTransferRequestHeaderNSDTO { Id = 1, RequestedById = 0 };

        var result = await sut.GetStockTransferRequest("TO-1");

        Assert.NotNull(result);
        Assert.Null(result!.RequestedBy);
    }

    // ---------------------------------------------------------------------
    // T-6.5 — P-3, P-10: `e` stays bound to custbody_dbti_prepared_by. Rebinding
    // it would repoint Prepared By at the new field and silently change
    // Prepared By behavior, which is frozen by Requirement 5.1.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Header_query_keeps_e_alias_on_prepared_by()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetStockTransferRequest("TO-1");

        Assert.NotNull(client.LastQuery);
        Assert.Contains("LEFT JOIN employee e ON e.id = t.custbody_dbti_prepared_by", client.LastQuery);
        Assert.Contains("CONCAT(e.firstname,CONCAT(' ',e.lastname)) AS PreparedBy", client.LastQuery);
        Assert.DoesNotContain("LEFT JOIN employee e ON e.id = t.employee", client.LastQuery);
    }

    // ---------------------------------------------------------------------
    // T-6.6 — P-4, AC-8: Transfer Order tab.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Transfer_order_list_query_selects_requested_by()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        Assert.Contains(
            "CONCAT(re.firstname,CONCAT(' ',re.lastname)) AS RequestedBy",
            client.LastQuery);
        Assert.Contains("LEFT JOIN employee re ON re.id = t.employee", client.LastQuery);
        Assert.Contains("t.recordtype = 'transferorder'", client.LastQuery);
    }

    // ---------------------------------------------------------------------
    // T-6.7 — P-4, AC-8: Intercompany tab. Missing either list query fails the
    // column silently on that tab only, which is why all three are asserted.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Intercompany_list_query_selects_requested_by()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetIntercompanyTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        Assert.Contains(
            "CONCAT(re.firstname,CONCAT(' ',re.lastname)) AS RequestedBy",
            client.LastQuery);
        Assert.Contains("LEFT JOIN employee re ON re.id = t.employee", client.LastQuery);
        Assert.Contains("t.recordtype = 'intercompanytransferorder'", client.LastQuery);
    }

    // ---------------------------------------------------------------------
    // T-6.8 — P-4, AC-8: Returns tab.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Returns_list_query_selects_requested_by()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetReturnsList(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        Assert.Contains(
            "CONCAT(re.firstname,CONCAT(' ',re.lastname)) AS RequestedBy",
            client.LastQuery);
        Assert.Contains("LEFT JOIN employee re ON re.id = t.employee", client.LastQuery);
    }

    // The Select() call registers every alias in PropertyMap, which is what makes
    // grid filtering/sorting on RequestedBy work server-side. Without the
    // projection the filter would resolve to a raw column NetSuite cannot sort
    // (AC-9), or silently match nothing.
    [Fact]
    public async Task List_queries_register_requested_by_in_the_sort_and_filter_map()
    {
        var (sut, client) = CreateCapturing();

        var intent = new DataGridIntent { Take = 10 };
        intent.Sorts.Add(DataGridSortUtilities.Ascending("RequestedBy"));
        await sut.GetTransferOrderList(intent);

        Assert.NotNull(client.LastQuery);
        Assert.Contains(
            "ORDER BY CONCAT(re.firstname,CONCAT(' ',re.lastname)) ASC",
            client.LastQuery);
    }

    // ---------------------------------------------------------------------
    // T-6.9 — P-8, AC-10: editing without touching Requested By must re-send the
    // stored employee. Because the key is simply omitted when null, a draft that
    // failed to carry the value forward would clear it in NetSuite.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Update_payload_retains_employee_when_unchanged()
    {
        var (sut, client) = CreateRestCapturing();
        var dto = SampleCreateDto(SampleEmployee(42));
        dto.Id = 555;

        await sut.UpdateStockTransferRequest(dto);

        var payload = PayloadOf(client.LastBody);
        Assert.Equal("42", payload.GetProperty("employee").GetProperty("id").GetString());
    }

    // Requirement 5.2 — the update path must NOT start sending Prepared By.
    // It is intentionally never populated by the handler on update.
    [Fact]
    public async Task Update_payload_omits_prepared_by()
    {
        var (sut, client) = CreateRestCapturing();
        var dto = SampleCreateDto(SampleEmployee(42));
        dto.Id = 555;
        dto.PreparedById = null;

        await sut.UpdateStockTransferRequest(dto);

        var payload = PayloadOf(client.LastBody);
        Assert.False(payload.TryGetProperty("custbody_dbti_prepared_by", out _));
    }

    // ---------------------------------------------------------------------
    // PERF 2026-10-09 — the purchase-category predicate
    // (purchase_category != 3 OR purchase_subcategory IN (4, 5, 9)) resolved in
    // NetSuite as a ~45s full scan (Q0=46s, Q5-without-it=1-2s, Q6-date-bound=47-48s).
    // It is therefore NOT sent to NetSuite; the equivalent predicate runs in
    // memory over the selective base set. NULL semantics mirror SQL 3VL:
    // NULL != 3 is UNKNOWN (kept only if the other arm is TRUE).
    //
    // 2026-10-15 — the whitelist widened from [5] to [4, 5, 9] to match the
    // Receiving > Purchase Orders listing, which had already been widened and
    // from which this grid had silently drifted.
    // ---------------------------------------------------------------------
    [Fact]
    public async Task Transfer_order_list_query_omits_purchase_category_or_group()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        // The DF display projections stay (grid columns), but the scan-driving
        // WHERE predicate must be gone.
        Assert.DoesNotContain("!= 3", client.LastQuery);
        Assert.DoesNotContain("purchase_subcategory = 5", client.LastQuery);
        var where = client.LastQuery.Split("WHERE", StringSplitOptions.RemoveEmptyEntries).Last();
        Assert.DoesNotContain("custbody_dbti_purchase_category", where);
        Assert.DoesNotContain("custbody_dbti_purchase_subcategory", where);
    }

    [Fact]
    public async Task Intercompany_list_query_omits_purchase_category_or_group()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetIntercompanyTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        Assert.DoesNotContain("!= 3", client.LastQuery);
        Assert.DoesNotContain("purchase_subcategory = 5", client.LastQuery);
        var where = client.LastQuery.Split("WHERE", StringSplitOptions.RemoveEmptyEntries).Last();
        Assert.DoesNotContain("custbody_dbti_purchase_category", where);
        Assert.DoesNotContain("custbody_dbti_purchase_subcategory", where);
    }

    [Fact]
    public async Task Transfer_order_list_query_selects_raw_purchase_category_ids()
    {
        var (sut, client) = CreateCapturing();

        await sut.GetTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.NotNull(client.LastQuery);
        Assert.Contains("AS PurchaseCategoryId", client.LastQuery);
        Assert.Contains("AS PurchaseSubcategoryId", client.LastQuery);
    }

    private static StockTransferRequestDataGridNSDTO GridRow(
        string refNo, int? categoryId, int? subcategoryId) => new()
        {
            ReferenceNumber = refNo,
            PurchaseCategoryId = categoryId,
            PurchaseSubcategoryId = subcategoryId,
            StatusId = 3,
            StatusName = "Pending Approval",
        };

    [Fact]
    public async Task Transfer_order_list_applies_purchase_category_filter_in_memory()
    {
        var (sut, client) = CreateCapturing();
        client.NextItems =
        [
            GridRow("TO-1", 1, 1),   // trade -> keep
            GridRow("TO-2", 3, 5),   // non-trade, store supplies -> keep
            GridRow("TO-3", 3, 7),   // non-trade, non-whitelisted sub -> drop
            GridRow("TO-4", null, 5),// NULL != 3 unknown, sub = 5 true -> keep
            GridRow("TO-5", null, null), // both unknown -> drop
        ];

        var (data, count) = await sut.GetTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.Equal(3, count);
        Assert.Equal(["TO-1", "TO-2", "TO-4"], data.Select(d => d.ReferenceNumber).Order());
    }

    /// <summary>
    /// 2026-10-15 fix: the non-trade whitelist widened from [5] to [4, 5, 9],
    /// matching the Receiving &gt; Purchase Orders listing. Non-trade Office
    /// Supplies and Uniforms STRs were previously invisible on this grid.
    /// </summary>
    [Fact]
    public async Task Transfer_order_list_keeps_every_non_trade_whitelisted_subcategory()
    {
        var (sut, client) = CreateCapturing();
        client.NextItems =
        [
            GridRow("TO-OFFICE", 3, 4),   // office supplies -> now visible
            GridRow("TO-STORE", 3, 5),   // store supplies -> visible before too
            GridRow("TO-UNIFORM", 3, 9),  // uniforms -> now visible
            GridRow("TO-OTHER", 3, 7),   // still hidden
        ];

        var (data, count) = await sut.GetTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.Equal(3, count);
        Assert.Equal(
            ["TO-OFFICE", "TO-STORE", "TO-UNIFORM"],
            data.Select(d => d.ReferenceNumber).Order());
    }

    /// <summary>
    /// End-to-end run of the SHARED truth table through the actual integration
    /// method. This is what proves the in-memory path is wired to the shared
    /// rule and to the right DTO properties, rather than merely agreeing with it
    /// by coincidence.
    /// </summary>
    [Fact]
    public async Task Transfer_order_list_matches_the_shared_purchase_category_truth_table()
    {
        var (sut, client) = CreateCapturing();
        var cases = PurchaseCategoryRuleTestCases.All;
        client.NextItems = [.. cases.Select((c, i) => GridRow(
            PurchaseCategoryRuleTestCases.ReferenceNumbers[i], c.CategoryId, c.SubcategoryId))];

        var (data, count) = await sut.GetTransferOrderList(new DataGridIntent { Take = 100 });

        var expected = PurchaseCategoryRuleTestCases.ReferenceNumbers
            .Where((_, i) => cases[i].ExpectedToPass);

        Assert.Equal(expected.Count(), count);
        Assert.Equal(expected.Order(), data.Select(d => d.ReferenceNumber).Order());
    }

    [Fact]
    public async Task Intercompany_list_applies_purchase_category_filter_in_memory()
    {
        var (sut, client) = CreateCapturing();
        client.NextItems =
        [
            GridRow("ICTO-1", 2, 2), // keep
            GridRow("ICTO-2", 3, 1), // drop
        ];

        var (data, count) = await sut.GetIntercompanyTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.Equal(1, count);
        Assert.Single(data);
        Assert.Equal("ICTO-1", data.Single().ReferenceNumber);
    }

    /// <summary>
    /// The ICTO grid shares <c>PassesPurchaseCategoryFilter</c> with the TO
    /// grid, so the widened whitelist must apply to it identically.
    /// </summary>
    [Fact]
    public async Task Intercompany_list_keeps_every_non_trade_whitelisted_subcategory()
    {
        var (sut, client) = CreateCapturing();
        client.NextItems =
        [
            GridRow("ICTO-OFFICE", 3, 4),
            GridRow("ICTO-UNIFORM", 3, 9),
            GridRow("ICTO-OTHER", 3, 7),
        ];

        var (data, count) = await sut.GetIntercompanyTransferOrderList(new DataGridIntent { Take = 10 });

        Assert.Equal(2, count);
        Assert.Equal(["ICTO-OFFICE", "ICTO-UNIFORM"], data.Select(d => d.ReferenceNumber).Order());
    }

    [Fact]
    public async Task Transfer_order_list_pages_the_filtered_set_not_the_base_set()
    {
        var (sut, client) = CreateCapturing();
        client.NextItems = Enumerable.Range(1, 15)
            .Select(i => GridRow($"TO-{i:00}", 1, 1))
            .Cast<object>()
            .ToList();

        var (page2, count) = await sut.GetTransferOrderList(new DataGridIntent { Take = 10, Skip = 10 });

        Assert.Equal(15, count);
        Assert.Equal(5, page2.Count());
    }
}
