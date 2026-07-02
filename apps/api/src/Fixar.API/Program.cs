using Fixar.API.Extensions;
using Fixar.Application;
using Fixar.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting FIXAR OS API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Fixar.API")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        await app.InitialiseDatabaseAsync();
        app.UseSwaggerDocumentation();
    }

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseCors(ApiServiceExtensions.CorsPolicyName);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthCheckEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "FIXAR OS API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
