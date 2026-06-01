using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using Shared.Kernel;
using Sprache;
using System.ComponentModel;
using Web.BlazorServer.Defaults;
using Web.BlazorServer.Handlers.Repositories.Administration.Authorization;
using Web.BlazorServer.Handlers.Repositories.Administration.Role;
using Web.BlazorServer.Handlers.Repositories.System;
using Web.BlazorServer.Services.Implementation;
using Web.BlazorServer.Services.Repositories;
using Web.BlazorServer.ViewModels.Abstraction;
using Web.BlazorServer.ViewModels.Administration.Role;
using Web.BlazorServer.ViewModels.Enums;
using Web.BlazorServer.ViewModels.System;

namespace Web.BlazorServer.Components.Pages.Administrator.Authorization;

public partial class AuthorizationPage
{

    #region Primitives
    AuthorizationType ActiveTab { get; set; } = AuthorizationType.Role;
    #endregion Primitives

    #region Overrides

    #endregion Overrides

}

public enum AuthorizationType
{
    [Description("Role Authorization")]
    Role,
    [Description("User Authorization")]
    User
}
