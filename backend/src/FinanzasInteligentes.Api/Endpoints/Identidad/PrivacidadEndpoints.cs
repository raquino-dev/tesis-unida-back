using FinanzasInteligentes.Api.Contratos.Identidad;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Identidad.Privacidad;

namespace FinanzasInteligentes.Api.Endpoints.Identidad;

public static class PrivacidadEndpoints
{
    public static IEndpointRouteBuilder MapPrivacidad(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/privacidad/politica-vigente", ObtenerPolitica)
            .AllowAnonymous().WithTags("Privacidad");
        endpoints.MapGet("/privacidad/consentimientos", Listar)
            .WithTags("Privacidad");
        endpoints.MapPost("/privacidad/consentimientos", Aceptar)
            .WithTags("Privacidad");
        endpoints.MapDelete("/privacidad/consentimientos/{consentimientoId:guid}", Revocar)
            .WithTags("Privacidad");
        return endpoints;
    }

    private static async Task<IResult> ObtenerPolitica(
        PrivacidadHandler handler, CancellationToken ct) =>
        Results.Ok(await handler.ObtenerPolitica(ct));

    private static async Task<IResult> Listar(
        PrivacidadHandler handler, HttpContext context, CancellationToken ct) =>
        Results.Ok(await handler.Listar(context.UsuarioId(), ct));

    private static async Task<IResult> Aceptar(
        AceptarConsentimientoRequest request, PrivacidadHandler handler,
        HttpContext context, CancellationToken ct)
    {
        var response = await handler.Aceptar(
            context.UsuarioId(), request.VersionPolitica, request.Finalidad,
            context.TraceIdentifier, ct);
        return Results.Created($"/api/v1/privacidad/consentimientos/{response.Id}", response);
    }

    private static async Task<IResult> Revocar(
        Guid consentimientoId, PrivacidadHandler handler,
        HttpContext context, CancellationToken ct)
    {
        await handler.Revocar(context.UsuarioId(), consentimientoId, context.TraceIdentifier, ct);
        return Results.NoContent();
    }
}
