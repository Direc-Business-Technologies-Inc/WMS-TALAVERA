using Mapster;
using Microsoft.AspNetCore.Components.Forms;
using Radzen.Blazor;
using System.Reflection;

namespace Web.BlazorServer.Components.Base;

public abstract partial class BaseForm<TItem> : BaseComponent, IDisposable where TItem : class, new()
{
    
    public TItem FormData { get; set; } = default!;
    public TItem FormDataClone { get; set; } = default!;
    public RadzenButton SubmitBtn { get; set; } = default!;
    public RadzenTemplateForm<TItem> TemplateForm { get; set; } = default!;
    public EditContext EditContext { get; set; } = default!;


    protected override void OnInitialized()
    {
        FormData = new TItem();
        FormDataClone = new TItem();
        EditContext = new EditContext(FormData);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            EditContext.OnFieldChanged += HandleFormChange;
        }
    }

    public virtual void HandleFormChange(object? sender, FieldChangedEventArgs args)
    {
        if (EditContext.IsModified())
        {
            UnsavedChangesService.MarkDirty();
        }
    }

    protected void OnFieldChanged(string propertyName)
    {
        var field = new FieldIdentifier(FormData, propertyName);
        EditContext.NotifyFieldChanged(field);
    }

    public async Task ResetFormContext()
    {
        UnhookFormChangedHandler();

        EditContext = new EditContext(FormData);
        HookFormChangeHandler();

        await InvokeAsync(StateHasChanged);
    }

    void HookFormChangeHandler()
    {
        if (EditContext is not null)
        {
            EditContext.OnFieldChanged += HandleFormChange;
        }
    }

    void UnhookFormChangedHandler()
    {
        if (EditContext is not null)
        {
            EditContext.OnFieldChanged -= HandleFormChange;
        }
    }

    public void NotifyAllFieldsChanged()
    {
        if (EditContext == null || FormData == null)
            return;

        NotifyObjectFields(FormData, string.Empty);

        if (!EditContext.Validate())
            return;
    }

    public void AdaptToClone() => FormDataClone = FormData.Adapt<TItem>();
    public void AdaptToForm() => FormData = FormDataClone.Adapt<TItem>();

    public virtual void Dispose()
    {
        UnhookFormChangedHandler();
    }

    void NotifyObjectFields(object instance, string parentPath)
    {
        var type = instance.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            var fieldPath = string.IsNullOrEmpty(parentPath)
                ? prop.Name
                : $"{parentPath}.{prop.Name}";

            var fieldIdentifier = new FieldIdentifier(FormData, fieldPath);
            EditContext.NotifyFieldChanged(fieldIdentifier);

            var value = prop.GetValue(instance);
            if (value != null &&
                prop.PropertyType.IsClass &&
                prop.PropertyType != typeof(string))
            {
                NotifyObjectFields(value, fieldPath);
            }
        }
    }


    protected abstract Task InitializeEditing();
    protected abstract Task CancelEditing();
    protected abstract Task HandleSubmit();
}
