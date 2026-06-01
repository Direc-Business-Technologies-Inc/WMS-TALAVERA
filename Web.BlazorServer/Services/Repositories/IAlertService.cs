namespace Web.BlazorServer.Services.Repositories;

public interface IAlertService
{
    Task<bool> HasUnsavedChangesAsync(string message = "Are you sure you want to proceed?", string? header = "Unsaved Changes", string confirmText = "Yes", string cancelText = "No");
    Task<bool> PromptAsync(string message = "", string? header = "Are you sure you want to proceed?", string confirmText = "Yes", string cancelText = "No");
}
