using System.Security.Cryptography;
using System.Text;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Integration.NS.Implementations.Others.Caching;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.BlazorServer.Controllers;

[ApiController]
[Route("api/integrations/netsuite/bulk-fulfillment-events")]
public class NetSuiteBulkFulfillmentController(IConfiguration configuration, ISender sender, GridCacheStore cache) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous] // Machine credential below; NetSuite does not have a WMS browser cookie.
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> Receive([FromBody] BulkFulfillmentEventDTO data, CancellationToken ct)
    {
        var key = configuration["NetSuiteWebhook:ApiKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        if (!Request.IsHttps) return BadRequest("HTTPS is required.");
        var supplied = Request.Headers["X-NetSuite-Webhook-Key"];
        if (supplied.Count != 1 || string.IsNullOrEmpty(supplied[0]) || !CryptographicOperations.FixedTimeEquals(
                SHA256.HashData(Encoding.UTF8.GetBytes(key)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied[0]!))))
            return Unauthorized();

        var receipt = await sender.Send(new ReceiveBulkFulfillmentEventCmd(data), ct);
        if (receipt == BulkFulfillmentEventReceipt.Invalid)
            return BadRequest("Provide an eventId, positive bulkFulfillmentId and sequence, status Done or Failed, and a message of at most 4000 characters.");
        if (receipt == BulkFulfillmentEventReceipt.Conflict)
            return Conflict("The event ID or BF sequence is already associated with a different event.");
        if (receipt == BulkFulfillmentEventReceipt.Unavailable)
            return StatusCode(StatusCodes.Status503ServiceUnavailable);

        // Invalidate after receipt, including retries. Failed batches can also contain created IFs.
        cache.Invalidate(CachedSupplierReturnIntegration.EntityName);
        cache.Invalidate("Packing");
        return Accepted();
    }
}
