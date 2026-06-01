using Microsoft.AspNetCore.Components;
using Radzen;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class AppContent
{
    #region Parameters
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Parameter] public Orientation Orientation { get; set; } = Orientation.Vertical;

    [Parameter] public JustifyContent JustfyContent { get; set; } = JustifyContent.Normal;

    [Parameter] public AlignItems AlignItems { get; set; } = AlignItems.Normal;

    [Parameter] public string Gap { get; set; } = "0";

    [Parameter] public RenderFragment? ChildContent { get; set; }
    #endregion Parameters

}
