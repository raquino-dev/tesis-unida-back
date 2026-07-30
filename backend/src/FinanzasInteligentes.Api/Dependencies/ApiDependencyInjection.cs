using FinanzasInteligentes.Api.Middleware;
using FinanzasInteligentes.Api.OpenApi;
using FinanzasInteligentes.Infraestructura.Autenticacion;
using FinanzasInteligentes.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using NSwag.Generation.Processors.Security;
using System.Text;
using System.Threading.RateLimiting;

namespace FinanzasInteligentes.Api.Dependencies;

public sealed class LimiteSolicitudesOptions
{
    public const string SectionName = "LimiteSolicitudes";
    public int SolicitudesPorVentana { get; init; } = 120;
    public int VentanaSegundos { get; init; } = 60;
}

public static class ApiDependencyInjection
{
    public static IServiceCollection AddPresentacion(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(settings =>
        {
            settings.DocumentName = "v1";
            settings.Title = "Finanzas Inteligentes API";
            settings.Version = "v1";
            settings.AddSecurity("Bearer", [], new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                BearerFormat = "JWT",
                Description = "Ingrese el token JWT."
            });
            settings.OperationProcessors.Add(
                new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
            settings.OperationProcessors.Add(new AllowAnonymousOperationProcessor());
            settings.OperationProcessors.Add(new MultipartDocumentoOperationProcessor());
        });

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization(options =>
            options.AddPolicy("administrador", policy =>
                policy.RequireAuthenticatedUser().RequireRole("administrador")));

        var limite = configuration.GetSection(LimiteSolicitudesOptions.SectionName)
            .Get<LimiteSolicitudesOptions>() ?? new LimiteSolicitudesOptions();
        if (limite.SolicitudesPorVentana is < 10 or > 10_000 ||
            limite.VentanaSegundos is < 1 or > 3_600)
            throw new InvalidOperationException("La configuración de límite de solicitudes no es válida.");
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst("sub")?.Value ??
                    context.Connection.RemoteIpAddress?.ToString() ??
                    "anonimo",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limite.SolicitudesPorVentana,
                        Window = TimeSpan.FromSeconds(limite.VentanaSegundos),
                        QueueLimit = 0
                    }));
        });

        services.AddHealthChecks().AddDbContextCheck<FinanzasDbContext>("postgresql", tags: ["ready"]);

        return services;
    }
}
