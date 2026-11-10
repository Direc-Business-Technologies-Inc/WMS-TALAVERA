using Application.DataTransferObjects.Transactions.SupplierReturn;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Radzen;
using Shared.Services.Repository;
using Web.BlazorServer.Components.Security;
using Web.BlazorServer.Components.Shared.Others;
using Web.BlazorServer.Handlers.Repositories.Transaction.SupplierReturn;
using Web.BlazorServer.Helpers;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Xunit;

namespace Web.BlazorServer.Tests;

public class BulkFulfillmentToastTests : TestContext
{
    public BulkFulfillmentToastTests()
    {
        Services.AddLogging();
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddScoped<AppAuthenticationService>();
        Services.AddScoped<AuthorizationHelper>();
        Services.AddScoped<UnsavedChangesService>();
        Services.AddScoped<DialogService>();
        Services.AddSingleton(Substitute.For<IAppActionFactory>());
        Services.AddSingleton(Substitute.For<IBusyService>());
        Services.AddSingleton(Substitute.For<IToastService>());
        Services.AddSingleton(Substitute.For<IAlertService>());
        Services.AddSingleton(Substitute.For<ICurrentUserService>());
    }

    [Fact]
    public void Submission_toast_lasts_three_seconds()
    {
        var service = new NotificationService();
        new ToastService(service).FulfillmentProcessing("Bulk request #301 submitted");
        var toast = Assert.Single(service.Messages);
        Assert.Equal("Fulfillment processing", toast.Summary);
        Assert.Equal(3000, toast.Duration);
    }

    [Fact]
    public void Completion_toast_remains_until_explicit_dismissal_and_escapes_message_markup()
    {
        var handler = Substitute.For<IBulkFulfillmentNotificationsHandler>();
        IReadOnlyList<BulkFulfillmentNotificationDTO> unread = [new(Guid.NewGuid(), 301, 1, "Done", "<script>alert(1)</script>")];
        handler.GetUnreadAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(unread));
        handler.DismissAsync(Arg.Any<Guid>(), Arg.Any<long>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            unread = [];
            return Task.CompletedTask;
        });
        Services.AddSingleton(handler);
        var cut = RenderComponent<BulkFulfillmentNotifications>();
        cut.WaitForAssertion(() => Assert.Contains("#301", cut.Markup));
        Assert.Empty(cut.FindAll("script"));
        Assert.Contains("&lt;script&gt;", cut.Markup);
        Assert.Contains("Dismiss", cut.Find("button").TextContent);
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".bf-notification")));
    }
}
