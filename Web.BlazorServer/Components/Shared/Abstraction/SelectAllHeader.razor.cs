using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Web.BlazorServer.Components.Base;

namespace Web.BlazorServer.Components.Shared.Abstraction;

public partial class SelectAllHeader : BaseComponent
{
    [Parameter] public bool? Value { get; set; }
    [Parameter] public EventCallback<bool?> OnChange { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool ShowCheckbox { get; set; } = true;
    [Parameter] public bool ShowButton { get; set; } = true;
    [Parameter] public string SelectText { get; set; } = "Select All";
    [Parameter] public string DeselectText { get; set; } = "Deselect All";
    [Parameter] public string CheckboxName { get; set; } = $"select_all_{Guid.NewGuid():N}";

    async Task OnCheckboxChange(bool? value)
    {
        // Radzen's tri-state cycles through null; normalize so consumers only ever
        // receive an explicit true (select all) or false (deselect all).
        bool? next = value is null ? (Value == true ? false : true) : value;

        if (OnChange.HasDelegate)
            await OnChange.InvokeAsync(next);
    }

    async Task OnButtonClick(MouseEventArgs _)
    {
        bool? next = Value == true ? false : true;

        if (OnChange.HasDelegate)
            await OnChange.InvokeAsync(next);
    }

    public static bool? Compute(int eligibleCount, int selectedCount)
    {
        if (eligibleCount <= 0) return false;
        if (selectedCount <= 0) return false;
        if (selectedCount >= eligibleCount) return true;
        return null;
    }
}
