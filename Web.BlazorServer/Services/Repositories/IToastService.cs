namespace Web.BlazorServer.Services.Repositories;

public interface IToastService
{
    void Success(string message, string? header = null);
    void Info(string message, string? header = null);
    void Error(string message, string? header = null);
    void Warning(string message, string? header = null);

}
