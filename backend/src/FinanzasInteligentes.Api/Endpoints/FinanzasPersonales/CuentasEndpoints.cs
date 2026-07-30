using FinanzasInteligentes.Api.Contratos.FinanzasPersonales;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.ActualizarCuenta;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.CrearCuenta;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.EliminarCuenta;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.ListarCuentas;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.ObtenerCuenta;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class CuentasEndpoints
{
    public static IEndpointRouteBuilder MapCuentas(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/cuentas", Listar).WithTags("Cuentas financieras");
        endpoints.MapPost("/cuentas", Crear).WithTags("Cuentas financieras");
        endpoints.MapGet("/cuentas/{cuentaId:guid}", Obtener).WithTags("Cuentas financieras");
        endpoints.MapPatch("/cuentas/{cuentaId:guid}", Actualizar).WithTags("Cuentas financieras");
        endpoints.MapDelete("/cuentas/{cuentaId:guid}", Eliminar).WithTags("Cuentas financieras");
        return endpoints;
    }

    private static async Task<IResult> Listar(
        ListarCuentasHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await handler.Handle(new ListarCuentasQuery(context.UsuarioId()), cancellationToken));
    }

    private static async Task<IResult> Crear(
        CrearCuentaRequest request,
        CrearCuentaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var command = new CrearCuentaCommand(
            context.UsuarioId(), context.TraceIdentifier, request.Nombre, request.Tipo,
            request.SaldoInicial, request.Moneda, request.Color, request.Icono, request.IncluidaEnTotal);

        var response = await handler.Handle(command, cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);

        return Results.Created($"/api/v1/cuentas/{response.Id}", response);
    }

    private static async Task<IResult> Obtener(
        Guid cuentaId,
        ObtenerCuentaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new ObtenerCuentaQuery(context.UsuarioId(), cuentaId),
            cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);

        return Results.Ok(response);
    }

    private static async Task<IResult> Eliminar(
        Guid cuentaId,
        EliminarCuentaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);

        await handler.Handle(
            new EliminarCuentaCommand(context.UsuarioId(), cuentaId, version),
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> Actualizar(
        Guid cuentaId,
        ActualizarCuentaRequest request,
        ActualizarCuentaHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);

        var response = await handler.Handle(
            new ActualizarCuentaCommand(
                context.UsuarioId(), cuentaId, version, request.Nombre, request.Tipo,
                request.Moneda, request.SaldoInicial, request.Color, request.Icono,
                request.IncluidaEnTotal),
            cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }
}
