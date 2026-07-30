using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Analitica;

namespace FinanzasInteligentes.Api.Endpoints.Analitica;

public static class DashboardYReportesEndpoints
{
    private const string Tag = "Dashboards y reportes";

    public static IEndpointRouteBuilder MapDashboardYReportes(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/tableros-financieros", ObtenerDashboard)
            .WithName("getTablerosFinancieros")
            .WithTags(Tag)
            .Produces<DashboardResponse>()
            .ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);

        endpoints.MapGet("/reportes-financieros", ObtenerReporte)
            .WithName("getReportesFinancieros")
            .WithTags(Tag)
            .Produces<ReporteResponse>()
            .ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(422).ProducesProblem(500);

        return endpoints;
    }

    private static async Task<IResult> ObtenerDashboard(
        string? ambito, DateOnly? desde, DateOnly? hasta,
        DashboardYReportesHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ObtenerDashboard(
            context.UsuarioId(), ambito, desde, hasta, ct));

    private static async Task<IResult> ObtenerReporte(
        string? rango, DateOnly? desde, DateOnly? hasta, string? tipo,
        Guid? categoriaId, Guid? cuentaId, DashboardYReportesHandler handler,
        HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ObtenerReporte(
            context.UsuarioId(), rango, desde, hasta, tipo, categoriaId, cuentaId, ct));
}