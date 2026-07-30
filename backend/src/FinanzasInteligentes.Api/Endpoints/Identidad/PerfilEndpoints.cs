using FinanzasInteligentes.Api.Contratos.Identidad;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Identidad.ActualizarPerfil;
using FinanzasInteligentes.Aplicacion.Identidad.EliminarPerfil;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Aplicacion.Identidad.ObtenerPerfil;
using FinanzasInteligentes.Aplicacion.Identidad.Preferencias;

namespace FinanzasInteligentes.Api.Endpoints.Identidad;

public static class PerfilEndpoints
{
    public static IEndpointRouteBuilder MapPerfil(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/perfil", Obtener).WithTags("Perfil");
        endpoints.MapPatch("/perfil", Actualizar).WithTags("Perfil");
        endpoints.MapGet("/perfil/preferencias", ObtenerPreferencias).WithTags("Preferencias");
        endpoints.MapPut("/perfil/preferencias", ActualizarPreferencias).WithTags("Preferencias");
        endpoints.MapPost("/eliminaciones-perfil", SolicitarEliminacion)
            .WithName("postEliminacionesPerfil")
            .WithTags("Perfil")
            .Produces<ProcesoAsyncResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapGet("/eliminaciones-perfil/{eliminacionId:guid}", ObtenerEliminacion)
            .WithName("getEliminacionesPerfilByEliminacionId")
            .WithTags("Perfil")
            .Produces<ProcesoAsyncResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        return endpoints;
    }

    private static async Task<IResult> Obtener(
        ObtenerPerfilHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new ObtenerPerfilQuery(context.UsuarioId()),
            cancellationToken);

        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);

        return Results.Ok(response);
    }

    private static async Task<IResult> Actualizar(
        ActualizarPerfilRequest request, ActualizarPerfilHandler handler,
        HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.Handle(new(
            context.UsuarioId(), version, request.Nombre, request.Idioma,
            request.Ubicacion, request.ZonaHoraria), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ObtenerPreferencias(
        ObtenerPreferenciasHandler handler, HttpContext context, CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new(context.UsuarioId()), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ActualizarPreferencias(
        ActualizarPreferenciasRequest request, ActualizarPreferenciasHandler handler,
        HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.Handle(new(
            context.UsuarioId(), version, request.Tema, request.Idioma,
            request.NotificacionesPush, request.ResumenSemanal), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> SolicitarEliminacion(
        EliminarPerfilRequest request,
        SolicitarEliminacionPerfilHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.Handle(new(
            context.UsuarioId(), request.Contrasena,
            request.VerificacionOtpId, context.TraceIdentifier), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(resultado.Version);
        return Results.Accepted(resultado.Response.UrlEstado, resultado.Response);
    }

    private static async Task<IResult> ObtenerEliminacion(
        Guid eliminacionId,
        ObtenerEliminacionPerfilHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var resultado = await handler.Handle(
            new(context.UsuarioId(), eliminacionId), cancellationToken);
        context.Response.Headers.ETag = ETagExtensions.Formatear(resultado.Version);
        return Results.Ok(resultado.Response);
    }
}