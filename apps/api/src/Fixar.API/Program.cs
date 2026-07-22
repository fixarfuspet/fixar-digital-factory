using Fixar.API.Extensions;
using Fixar.Application;
using Fixar.Infrastructure;
using Serilog;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

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
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FIXAR_DEV_TEST_PASSWORD")))
        {
            using var scope = app.Services.CreateScope();
            var initialiser = scope.ServiceProvider.GetRequiredService<Fixar.Infrastructure.Persistence.ApplicationDbContextInitialiser>();
            await initialiser.SeedAsync();
            await initialiser.SeedDevelopmentTestUsersAsync();
        }
        app.UseSwaggerDocumentation();
    }

    app.UseMiddleware<Fixar.API.Middleware.RequestContextMiddleware>();
    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    app.UseHttpsRedirection();

    app.UseCors(ApiServiceExtensions.CorsPolicyName);
    app.UseRateLimiter();

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
