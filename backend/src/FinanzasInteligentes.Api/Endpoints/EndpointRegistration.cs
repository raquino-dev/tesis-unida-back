using FinanzasInteligentes.Api.Endpoints.Analitica;
using FinanzasInteligentes.Api.Endpoints.Documentos;
using FinanzasInteligentes.Api.Endpoints.FinanzasFamiliares;
using FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;
using FinanzasInteligentes.Api.Endpoints.Identidad;
using FinanzasInteligentes.Api.Endpoints.Piloto;
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
        endpoints.MapInstrumentosPiloto();
        endpoints.MapSeguridad();
        endpoints.MapCuentas();
        endpoints.MapCategorias();
        endpoints.MapTarjetasCredito();
        endpoints.MapMovimientos();
        endpoints.MapMovimientosRecurrentes();
        endpoints.MapSincronizacion();
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
            capacidades = new[]
            {
                "identidad", "cuentas", "categorias", "tarjetas-credito",
                "movimientos", "movimientos-recurrentes", "sincronizacion",
                "transferencias", "planificacion", "analitica", "reportes",
                "documentos", "exportaciones", "seguridad-dispositivos",
                "familias", "suscripciones", "piloto"
            }
        })).AllowAnonymous().WithTags("Configuración");

        return endpoints;
    }
}
