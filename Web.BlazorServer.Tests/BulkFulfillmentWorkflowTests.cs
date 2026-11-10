using System.Reflection;
using System.Security.Claims;
using Application.DataTransferObjects.Transactions.SupplierReturn;
using Application.UseCases.Commands.Transaction.SupplierReturn;
using Bunit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Radzen;
using Web.BlazorServer.Components.Base;
using Web.BlazorServer.Components.Pages.Transaction.SupplierReturn.Components;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Handlers.Implementations.Transaction.SupplierReturn;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Transaction.SupplierReturn;
using Xunit;

namespace Web.BlazorServer.Tests;

public class BulkFulfillmentWorkflowTests : TestContext
{
    private readonly ISupplierReturnHandler handler = Substitute.For<ISupplierReturnHandler>();
    private readonly IToastService toast = Substitute.For<IToastService>();
    private readonly DialogService dialogs;

    public BulkFulfillmentWorkflowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<DialogService>();
        dialogs = Services.GetRequiredService<DialogService>();
    }

    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Dictionary<int, SupplierReturnDataGridVM> Selection(SupplierReturnsDataGrid grid) =>
        (Dictionary<int, SupplierReturnDataGridVM>)typeof(SupplierReturnsDataGrid).GetField("SelectedRows", Private)!.GetValue(grid)!;
    private static Task Open(SupplierReturnsDataGrid grid) =>
        (Task)typeof(SupplierReturnsDataGrid).GetMethod("OpenBulkFulfillConfirmation", Private)!.Invoke(grid, null)!;

    private SupplierReturnsDataGrid Grid()
    {
        var grid = new SupplierReturnsDataGrid();
        typeof(SupplierReturnsDataGrid).GetProperty("returnHandler", Private)!.SetValue(grid, handler);
        typeof(BaseComponent).GetProperty("DialogService", Private)!.SetValue(grid, dialogs);
        typeof(BaseComponent).GetProperty("ToastService", Private)!.SetValue(grid, toast);
        Selection(grid).Add(1234, new() { Id = 1234, ReferenceNumber = "RTS-1234" });
        Selection(grid).Add(5678, new() { Id = 5678, ReferenceNumber = "RTS-5678" });
        return grid;
    }

    [Fact]
    public async Task Cancel_and_minimum_selection_create_nothing()
    {
        var grid = Grid();
        Selection(grid).Remove(5678);
        await Open(grid);
        Selection(grid).Add(5678, new() { Id = 5678 });
        var open = Open(grid);
        dialogs.Close(false);
        await open;
        Assert.Equal(2, Selection(grid).Count);
        await handler.DidNotReceiveWithAnyArgs().SubmitBulkFulfillmentAsync(default!);
    }

    [Theory]
    [InlineData(BulkFulfillmentOutcome.Submitted, 0)]
    [InlineData(BulkFulfillmentOutcome.Rejected, 2)]
    [InlineData(BulkFulfillmentOutcome.Uncertain, 2)]
    public async Task Guard_spans_dialog_and_submission_and_selection_follows_outcome(BulkFulfillmentOutcome outcome, int remaining)
    {
        var pending = new TaskCompletionSource<BulkFulfillmentResultDTO>();
        handler.SubmitBulkFulfillmentAsync(Arg.Any<SubmitBulkFulfillmentDTO>()).Returns(pending.Task);
        var grid = Grid();
        var first = Open(grid);
        await Open(grid); // second click while dialog is open
        dialogs.Close(true);
        await Open(grid); // second click while HTTP request is pending
        await handler.Received(1).SubmitBulkFulfillmentAsync(Arg.Is<SubmitBulkFulfillmentDTO>(data => data.ReturnIds.SequenceEqual(new[] { 1234, 5678 })));
        pending.SetResult(new(outcome, "Result", outcome == BulkFulfillmentOutcome.Submitted ? 42 : null));
        await first;
        Assert.Equal(remaining, Selection(grid).Count);
        Assert.False((bool)typeof(SupplierReturnsDataGrid).GetField("BulkSubmissionBusy", Private)!.GetValue(grid)!);
    }

    [Fact]
    public async Task Search_changes_display_only_and_confirmation_returns_true()
    {
        var confirmation = dialogs.OpenAsync<BulkFulfillConfirmation>("Confirm");
        SupplierReturnDataGridVM[] rows = [
            new() { Id = 1234, ReferenceNumber = "RTS-1234", VendorName = "Vendor", CategoryName = "Category", PreparedBy = "User" },
            new() { Id = 5678, ReferenceNumber = "RTS-5678", VendorName = "Vendor", CategoryName = "Category", PreparedBy = "User" }];
        var cut = RenderComponent<BulkFulfillConfirmation>(parameters => parameters.Add(component => component.Rows, rows));
        cut.Find("input").Change("RTS-1234");
        Assert.Contains("2 selected; 1 matching", cut.Markup);
        Assert.Equal(2, cut.Instance.Rows.Count);
        cut.FindAll("button").Single(button => button.TextContent.Contains("Submit Request")).Click();
        Assert.True((bool)await confirmation);
    }

    [Theory]
    [InlineData(false, "[7]", false)]
    [InlineData(true, "[]", false)]
    [InlineData(true, "invalid", false)]
    [InlineData(true, "[7]", true)]
    public async Task Presentation_enforces_permission_and_trusted_scope(bool permitted, string scope, bool dispatched)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("com.direcbusiness.wms.nsAllowedSubsidiaries", scope)], "test"));
        var auth = new AppAuthenticationService(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } });
        var authorization = Substitute.For<IAuthorizationService>();
        authorization.AuthorizeAsync(principal, null, "permission.OSRN.CREATE").Returns(permitted ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<SubmitBulkFulfillmentCmd>(), Arg.Any<CancellationToken>())
            .Returns(new BulkFulfillmentResultDTO(BulkFulfillmentOutcome.Submitted, "Submitted", 42));
        var result = await new SupplierReturnHandler(auth, sender, authorization).SubmitBulkFulfillmentAsync(new([1234, 5678]));
        Assert.Equal(dispatched ? BulkFulfillmentOutcome.Submitted : BulkFulfillmentOutcome.Rejected, result.Outcome);
        Assert.Equal(dispatched ? 1 : 0, sender.ReceivedCalls().Count());
        if (dispatched)
            await sender.Received(1).Send(Arg.Is<SubmitBulkFulfillmentCmd>(cmd => cmd.AllowedSubsidiaries.SequenceEqual(new[] { 7 })), Arg.Any<CancellationToken>());
    }
}
