using FinanzasInteligentes.Api.Contratos.Identidad;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Identidad.Seguridad;

namespace FinanzasInteligentes.Api.Endpoints.Identidad;

public static class SeguridadEndpoints
{
    public static IEndpointRouteBuilder MapSeguridad(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/desafios-otp", CrearDesafio)
            .RequireRateLimiting("otp").WithTags("Seguridad OTP");
        endpoints.MapPost("/verificaciones-otp", VerificarOtp)
            .RequireRateLimiting("otp").WithTags("Seguridad OTP");
        endpoints.MapPost("/recuperaciones-contrasena", SolicitarRecuperacion)
            .RequireRateLimiting("recuperacion-contrasena")
            .AllowAnonymous().WithTags("Contraseñas");
        endpoints.MapPost("/restablecimientos-contrasena", RestablecerContrasena)
            .RequireRateLimiting("recuperacion-contrasena")
            .AllowAnonymous().WithTags("Contraseñas");
        endpoints.MapPut("/perfil/contrasena", CambiarContrasena).WithTags("Contraseñas");
        return endpoints;
    }

    private static async Task<IResult> CrearDesafio(
        DesafioOtpRequest request, CrearDesafioOtpHandler handler,
        HttpContext context, CancellationToken cancellationToken)
    {
        var response = await handler.Handle(new(
            context.UsuarioId(), request.Motivo, request.Canal, context.TraceIdentifier),
            cancellationToken);
        return Results.Created($"/api/v1/desafios-otp/{response.Id}", response);
    }

    private static async Task<IResult> VerificarOtp(
        VerificacionOtpRequest request, VerificarOtpHandler handler,
        HttpContext context, CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new(context.UsuarioId(), request.DesafioId, request.Codigo), cancellationToken);
        return Results.Created($"/api/v1/verificaciones-otp/{response.Id}", response);
    }

    private static async Task<IResult> SolicitarRecuperacion(
        RecuperacionContrasenaRequest request, SolicitarRecuperacionHandler handler,
        HttpContext context, CancellationToken cancellationToken)
    {
        await handler.Handle(new(request.Correo, context.TraceIdentifier), cancellationToken);
        return Results.Accepted();
    }

    private static async Task<IResult> RestablecerContrasena(
        RestablecimientoContrasenaRequest request, RestablecerContrasenaHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(new(request.Token, request.NuevaContrasena), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> CambiarContrasena(
        CambiarContrasenaRequest request, CambiarContrasenaHandler handler,
        HttpContext context, CancellationToken cancellationToken)
    {
        await handler.Handle(new(
            context.UsuarioId(), request.ContrasenaActual,
            request.NuevaContrasena, request.VerificacionOtpId), cancellationToken);
        return Results.NoContent();
    }
}
