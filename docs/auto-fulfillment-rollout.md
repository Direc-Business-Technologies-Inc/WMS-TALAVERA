# STR auto-fulfillment rollout

## Supported policy

Only one active exact category/from/to rule with Auto Pack **Yes** and Auto Ship
**Yes** is eligible. Other flag combinations do not create automatic fulfillments.
Eligible orders create Shipped (`C`) item fulfillments, subject to complete-line,
inventory, bin, UOM and concurrent-posting checks.

The worker persists a route activation time on the first successful polling pass
that sees a valid, unique Yes/Yes rule. Orders must be approved **strictly after**
that time. This is not the time the environment variable was edited or the rule
was saved. Existing route activation times survive API restarts; restarting does
not establish a new cutoff. Historical orders are not backfilled.

## API process environment

Configure these in the API host's environment, then restart the API. A repository
`.env` file alone does not configure the process unless the host loads it.

```dotenv
WMS_STR_AUTO_ENABLED=false
WMS_STR_AUTO_APPROVED_STATUS_ID=<Approved list-entry internal ID>
WMS_STR_AUTO_YES_LIST_VALUE_ID=<Yes list-entry internal ID>
WMS_STR_AUTO_SUBSIDIARY_IDS=<comma-separated permitted subsidiary IDs>
WMS_STR_AUTO_APPROVAL_TIME_FIELD=<approval timestamp custbody_ script ID>
WMS_STR_AUTO_APPROVAL_TIME_IS_UTC_VERIFIED=false
WMS_STR_AUTO_TEMPLATE_APPLICATION_VERIFIED=false
WMS_STR_AUTO_CATEGORY_STATUS_POLICY_VERIFIED=false
WMS_STR_AUTO_POLL_SECONDS=120
```

Set `WMS_STR_AUTO_LINE_STATUS_FIELD` to a verified `custcol_` inventory-status
field, or verify the category-based policy and set
`WMS_STR_AUTO_CATEGORY_STATUS_POLICY_VERIFIED=true`. A supplied malformed line
field is a configuration error even with the category fallback enabled.

The approval timestamp must be stored on the transfer order when its custom
approval status transitions to Approved. Verify how SuiteQL renders that field
against UTC before setting the UTC verification switch. A status list field,
created date, last-modified date or System Notes screenshot cannot replace it.
Do not invent a field ID if the approval process does not currently store one.

The supplied account sample establishes Yes = `1`, Approved = `4` (from the
sample's status and display), subsidiary = `1`, and category/from/to = `1/1/207`.
Verify those mappings in the target deployment. Template `160` belongs to the
rule, not an environment variable.

## Verification

1. Apply migration `20261030063331_StrAutoFulfillmentProcessing` to the API's addon
   database using the deployment's normal migration procedure.
2. Resolve the previously identified claim-concurrency and fulfillment-contract
   review findings before production enablement. Readiness flags assert account
   configuration; they do not prove posting correctness.
3. Verify template application and the inventory-status/bin contract in sandbox.
4. Set verified switches and ENABLED to true, restart the API, and call the
   authenticated `GET /api/StockTransferRequest/AutoFulfillment/status` endpoint.
   `Ready` must be true. Otherwise its `ReadinessIssues` list and startup log name
   the missing settings without exposing credential values.
5. Wait for route activation before approving a new sandbox STR. Confirm exactly
   one Shipped IF with correct quantities, bins, status and label behavior.
6. Check `GET /api/StockTransferRequest/AutoFulfillment/outcomes`. Confirm older
   orders remain unfulfilled and No/Yes, Yes/No and No/No rules do not post.
7. Confirm a second poll/restart does not create a duplicate IF. Investigate
   `Posting` or `OutcomeUncertain` against NetSuite before any reconciliation;
   do not delete processing records to force a retry.
