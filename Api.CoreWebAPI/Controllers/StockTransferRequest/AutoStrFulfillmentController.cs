using Application.DataTransferObjects.Transactions.StockTransferRequest;
using Application.UseCases.Repositories.Domain.Transaction.StockTransferRequest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.CoreWebAPI.Controllers.StockTransferRequest;

/// <summary>Read-only operational record of automatic STR outcomes; no blind retry endpoint.</summary>
[ApiController]
[Authorize(AuthenticationSchemes = "Bearer")]
[Route("api/StockTransferRequest/AutoFulfillment")]
public sealed class AutoStrFulfillmentController(IStrAutoFulfillmentStore store) : ControllerBase
{
    [HttpGet("outcomes")]
    public Task<IReadOnlyList<StrAutoFulfillmentOutcomeDTO>> GetOutcomes(CancellationToken ct) =>
        store.RecentOutcomesAsync(100, ct);
}
