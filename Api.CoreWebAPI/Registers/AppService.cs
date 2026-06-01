namespace Api.CoreWebAPI.Registers;

public static class AppService
{
    public static void AppServices(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = string.Empty;
            options.DocumentTitle = "Direc API Reference";
        });

        // app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
