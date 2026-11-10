using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Application.UseCases.Repositories.Integration.Transaction.SupplierReturn;
using Integration.NS.Implementations.Others.Caching;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Web.BlazorServer.Controllers;
using Web.BlazorServer.Services.Implementation;
using Xunit;

namespace Web.BlazorServer.Tests;

public class BulkFulfillmentCallbackTests
{
    private readonly InMemoryBulkFulfillmentTrackingRepository store = new();

    [Fact]
    public async Task Callback_before_receipt_only_reaches_owner_until_dismissed_and_restart_loses_state()
    {
        var userId = Guid.NewGuid();
        var requestId = await store.BeginAsync(userId, [1, 2], default);
        var evt = new BulkFulfillmentEventDTO(Guid.NewGuid(), 301, 1, "Done", "IFs created");
        Assert.Equal(BulkFulfillmentEventReceipt.Accepted, await store.ReceiveAsync(evt, default));
        Assert.Empty(await store.GetUnreadAsync(userId, default));
        await store.CompleteSubmissionAsync(requestId, new(BulkFulfillmentOutcome.Submitted, "Submitted", 301), default);

        var notification = Assert.Single(await store.GetUnreadAsync(userId, default));
        Assert.Equal("Done", notification.Status);
        Assert.Empty(await store.GetUnreadAsync(Guid.NewGuid(), default));
        await store.DismissAsync(Guid.NewGuid(), requestId, 1, default);
        Assert.Single(await store.GetUnreadAsync(userId, default));
        await store.DismissAsync(userId, requestId, 1, default);
        Assert.Empty(await store.GetUnreadAsync(userId, default));
        Assert.Empty(await new InMemoryBulkFulfillmentTrackingRepository().GetUnreadAsync(userId, default));
    }

    [Fact]
    public async Task Duplicate_delivery_is_idempotent_conflicts_rejected_and_late_events_cannot_revert_latest_status()
    {
        var user = Guid.NewGuid();
        var request = await store.BeginAsync(user, [1, 2], default);
        await store.CompleteSubmissionAsync(request, new(BulkFulfillmentOutcome.Submitted, "Submitted", 301), default);
        var failed = new BulkFulfillmentEventDTO(Guid.NewGuid(), 301, 1, "Failed");
        var done = new BulkFulfillmentEventDTO(Guid.NewGuid(), 301, 2, "Done");
        Assert.Equal(BulkFulfillmentEventReceipt.Accepted, await store.ReceiveAsync(done, default));
        Assert.Equal(BulkFulfillmentEventReceipt.Accepted, await store.ReceiveAsync(failed, default));
        Assert.Equal(BulkFulfillmentEventReceipt.Duplicate, await store.ReceiveAsync(done, default));
        Assert.Equal(BulkFulfillmentEventReceipt.Conflict, await store.ReceiveAsync(done with { Status = "Failed" }, default));
        Assert.Equal(BulkFulfillmentEventReceipt.Conflict, await store.ReceiveAsync(done with { EventId = Guid.NewGuid() }, default));
        Assert.Equal("Done", Assert.Single(await store.GetUnreadAsync(user, default)).Status);
        await store.DismissAsync(user, request, 2, default);
        Assert.Equal(BulkFulfillmentEventReceipt.Duplicate, await store.ReceiveAsync(done, default));
        Assert.Empty(await store.GetUnreadAsync(user, default));
        await store.ReceiveAsync(new(Guid.NewGuid(), 301, 3, "Failed"), default);
        Assert.Equal(3, Assert.Single(await store.GetUnreadAsync(user, default)).Sequence);
    }

    [Theory]
    [InlineData("Done", 1, 301, true)]
    [InlineData("Failed", 1, 301, true)]
    [InlineData("Processing", 1, 301, false)]
    [InlineData("Done", 0, 301, false)]
    [InlineData("Done", 1, 0, false)]
    public async Task Command_validates_contract_before_saving(string status, long sequence, int bfId, bool valid)
    {
        var repository = Substitute.For<IBulkFulfillmentTrackingRepository>();
        repository.ReceiveAsync(Arg.Any<BulkFulfillmentEventDTO>(), Arg.Any<CancellationToken>()).Returns(BulkFulfillmentEventReceipt.Accepted);
        var sut = new ReceiveBulkFulfillmentEventCmdHandler(repository);
        var result = await sut.Handle(new(new(Guid.NewGuid(), bfId, sequence, status)), default);
        Assert.Equal(valid ? BulkFulfillmentEventReceipt.Accepted : BulkFulfillmentEventReceipt.Invalid, result);
        Assert.Equal(valid ? 1 : 0, repository.ReceivedCalls().Count());
    }

    [Theory]
    [InlineData(null, true, 503)]
    [InlineData("wrong-key", true, 401)]
    [InlineData("configured", false, 400)]
    [InlineData("configured", true, 202)]
    public async Task Endpoint_requires_configured_machine_key_and_https(string? provided, bool https, int expected)
    {
        var key = new string('x', 40); // Test-only credential.
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["NetSuiteWebhook:ApiKey"] = provided is null ? null : key
        }).Build();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReceiveBulkFulfillmentEventCmd>(), Arg.Any<CancellationToken>()).Returns(BulkFulfillmentEventReceipt.Accepted);
        using var cache = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var controller = new NetSuiteBulkFulfillmentController(config, sender, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Scheme = https ? "https" : "http";
        controller.Request.Headers["X-NetSuite-Webhook-Key"] = provided == "configured" ? key : provided;
        var result = await controller.Receive(new(Guid.NewGuid(), 301, 1, "Done"), default);
        Assert.Equal(expected, result switch { ObjectResult obj => obj.StatusCode, StatusCodeResult status => status.StatusCode, _ => 0 });
        Assert.Equal(expected == 202 ? 1 : 0, sender.ReceivedCalls().Count());
    }

    [Fact]
    public async Task Endpoint_does_not_acknowledge_failed_event_acceptance()
    {
        var key = new string('x', 40);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["NetSuiteWebhook:ApiKey"] = key }).Build();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReceiveBulkFulfillmentEventCmd>(), Arg.Any<CancellationToken>())
            .Returns<BulkFulfillmentEventReceipt>(_ => throw new InvalidOperationException("Notification service unavailable"));
        using var cache = new GridCacheStore(100, TimeSpan.FromSeconds(60));
        var controller = new NetSuiteBulkFulfillmentController(config, sender, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        controller.Request.Scheme = "https";
        controller.Request.Headers["X-NetSuite-Webhook-Key"] = key;
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Receive(new(Guid.NewGuid(), 301, 1, "Done"), default));
    }

    [Fact]
    public async Task Missing_user_or_tracking_failure_prevents_the_external_submission()
    {
        var integration = Substitute.For<ISupplierReturnIntegration>();
        var tracking = Substitute.For<IBulkFulfillmentTrackingRepository>();
        var sut = new SubmitBulkFulfillmentCmdHandler(integration, tracking, NullLogger<SubmitBulkFulfillmentCmdHandler>.Instance);
        Assert.Equal(BulkFulfillmentOutcome.Rejected, (await sut.Handle(new(new([1, 2]), [7]), default)).Outcome);
        tracking.BeginAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns<Guid>(_ => throw new InvalidOperationException("Notification service unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(new(new([1, 2]), [7], Guid.NewGuid()), default));
        Assert.Empty(integration.ReceivedCalls());
    }
}
