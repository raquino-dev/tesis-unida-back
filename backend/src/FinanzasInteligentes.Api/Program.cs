using FinanzasInteligentes.Api.Dependencies;
using FinanzasInteligentes.Api.Endpoints;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Api.Middleware;
using FinanzasInteligentes.Aplicacion;
using FinanzasInteligentes.Infraestructura;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
builder.SetDefaultCulture();
builder.Services
    .AddAplicacion()
    .AddInfraestructura(builder.Configuration)
    .AddPresentacion(builder.Configuration)
    .AddProductionSecurity(builder.Configuration, builder.Environment);

var app = builder.Build();
Log.Information("Iniciando la aplicación {ApplicationName} en el entorno {EnvironmentName}", app.Environment.ApplicationName, app.Environment.EnvironmentName);

app.UseConfiguredForwardedHeaders();
app.UseRouting();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseMiddleware<AuditoriaHttpMiddleware>();
app.UseCustomSerilogRequestLogging();
app.UseAuthentication();
app.UseMiddleware<UsuarioActivoMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOpenApiDocumentation();

app.MapGet("/salud/vivo", () => Results.Ok(new { estado = "saludable" })).AllowAnonymous().WithTags("Salud");

app.MapHealthChecks("/salud/listo", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
}).AllowAnonymous().WithTags("Salud");

app.MapGet("/", () => Results.Redirect("/swagger/index.html")).AllowAnonymous().ExcludeFromDescription();

app.MapGroup("/api/v1")
    .RequireAuthorization()
    .MapApiEndpoints();

await app.RunAsync();
