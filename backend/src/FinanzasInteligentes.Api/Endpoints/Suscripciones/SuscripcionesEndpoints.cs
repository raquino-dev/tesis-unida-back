using FinanzasInteligentes.Api.Contratos.Suscripciones;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Suscripciones;
using FinanzasInteligentes.Aplicacion.Suscripciones.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.Suscripciones;

public static class SuscripcionesEndpoints
{
    private const string Tag = "Planes y suscripciones";

    public static IEndpointRouteBuilder MapSuscripciones(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/planes-suscripcion", ListarPlanes)
            .WithName("getPlanesSuscripcion")
            .WithTags(Tag)
            .AllowAnonymous()
            .Produces<IReadOnlyCollection<PlanSuscripcionResponse>>();
        endpoints.MapGet("/suscripcion", Obtener)
            .WithName("getSuscripcion")
            .WithTags(Tag)
            .Produces<SuscripcionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapPost("/suscripciones", Crear)
            .WithName("postSuscripciones")
            .WithTags(Tag)
            .Produces<SuscripcionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapPost("/suscripcion/cancelaciones", Cancelar)
            .WithName("postSuscripcionCancelaciones")
            .WithTags(Tag)
            .Produces<SuscripcionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapPost("/restauraciones-suscripcion", Restaurar)
            .WithName("postRestauracionesSuscripcion")
            .WithTags(Tag)
            .Produces<SuscripcionResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
        endpoints.MapGet("/suscripcion/transacciones", ListarTransacciones)
            .WithName("getSuscripcionTransacciones")
            .WithTags(Tag)
            .Produces<IReadOnlyCollection<TransaccionSuscripcionResponse>>();
        endpoints.MapPut("/suscripcion/plan", CambiarPlan)
            .WithName("putSuscripcionPlan")
            .WithTags(Tag)
            .Produces<SuscripcionResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
        endpoints.MapPost("/webhooks/google-play/rtdn", ProcesarRtdn)
            .WithName("postGooglePlayRtdn")
            .WithTags("Webhooks")
            .AllowAnonymous()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
        return endpoints;
    }

    private static async Task<IResult> ListarPlanes(
        SuscripcionesHandler handler,
        CancellationToken ct) =>
        Results.Ok(await handler.ListarPlanes(ct));

    private static async Task<IResult> Obtener(
        SuscripcionesHandler handler,
        HttpContext context,
        CancellationToken ct)
    {
        var response = await handler.Obtener(context.UsuarioId(), ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Crear(
        SuscripcionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        SuscripcionesHandler handler,
        HttpContext context,
        CancellationToken ct)
    {
        var response = await handler.Crear(
            context.UsuarioId(), request.PlanCodigo, request.Proveedor,
            request.Comprobante, request.VerificacionOtpId,
            context.TraceIdentifier, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Created($"/api/v1/suscripcion", response);
    }

    private static async Task<IResult> Cancelar(
        PostSuscripcionCancelacionesRequest? request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromHeader(Name = "If-Match")] string ifMatch,
        SuscripcionesHandler handler,
        HttpContext context,
        CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.Cancelar(
            context.UsuarioId(), version, request?.Motivo,
            context.TraceIdentifier, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Restaurar(
        PostRestauracionesSuscripcionRequest request,
        SuscripcionesHandler handler,
        HttpContext context,
        CancellationToken ct)
    {
        var response = await handler.Restaurar(
            context.UsuarioId(), request.Proveedor, request.Comprobante,
            context.TraceIdentifier, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Accepted("/api/v1/suscripcion", response);
    }

    private static async Task<IResult> ListarTransacciones(
        SuscripcionesHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.ListarTransacciones(context.UsuarioId(), ct));

    private static async Task<IResult> CambiarPlan(
        CambiarPlanSuscripcionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        SuscripcionesHandler handler, HttpContext context, CancellationToken ct)
    {
        if (!context.Request.TryObtenerVersionIfMatch(out var version))
            return ETagExtensions.IfMatchInvalido(context);
        var response = await handler.CambiarPlan(
            context.UsuarioId(), version, request.PlanCodigo, request.Proveedor,
            request.Comprobante, request.VerificacionOtpId,
            context.TraceIdentifier, ct);
        context.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ProcesarRtdn(
        GooglePubSubPushRequest request,
        [FromHeader(Name = "Authorization")] string authorization,
        SuscripcionesHandler handler, HttpContext context, CancellationToken ct)
    {
        await handler.ProcesarNotificacionGooglePlay(
            authorization, request.Message.Data, context.TraceIdentifier, ct);
        return Results.NoContent();
    }
}
