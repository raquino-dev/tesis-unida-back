using FinanzasInteligentes.Api.Endpoints.Analitica;
using FinanzasInteligentes.Api.Endpoints.Documentos;
using FinanzasInteligentes.Api.Endpoints.FinanzasFamiliares;
using FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;
using FinanzasInteligentes.Api.Endpoints.Identidad;
using FinanzasInteligentes.Api.Endpoints.Seguridad;
using FinanzasInteligentes.Api.Endpoints.Suscripciones;

namespace FinanzasInteligentes.Api.Endpoints;

public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapUsuarios();
        endpoints.MapAdministracionUsuarios();
        endpoints.MapSesiones();
        endpoints.MapPerfil();
        endpoints.MapPrivacidad();
        endpoints.MapSeguridad();
        endpoints.MapCuentas();
        endpoints.MapCategorias();
        endpoints.MapTarjetasCredito();
        endpoints.MapMovimientos();
        endpoints.MapMovimientosRecurrentes();
        endpoints.MapTransferencias();
        endpoints.MapPlanificacion();
        endpoints.MapAnalitica();
        endpoints.MapDashboardYReportes();
        endpoints.MapDocumentos();
        endpoints.MapExportaciones();
        endpoints.MapSeguridadYDispositivos();
        endpoints.MapFinanzasFamiliares();
        endpoints.MapSuscripciones();

        endpoints.MapGet("/configuracion-cliente", () => Results.Ok(new
        {
            versionMinima = "0.1.0",
            versionRecomendada = "0.1.0",
            mantenimiento = false,
            capacidades = new[] { "identidad", "cuentas", "categorias", "movimientos" }
        })).AllowAnonymous().WithTags("Configuración");

        return endpoints;
    }
}
