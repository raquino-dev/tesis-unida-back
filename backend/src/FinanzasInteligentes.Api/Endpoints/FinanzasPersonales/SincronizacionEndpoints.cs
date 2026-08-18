using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Sincronizacion;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.FinanzasPersonales;

public static class SincronizacionEndpoints
{
    public static IEndpointRouteBuilder MapSincronizacion(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/sincronizacion", Listar).WithTags("Sincronización");
        return endpoints;
    }

    private static async Task<IResult> Listar(
        [FromQuery] long desde,
        [FromQuery] int limite,
        ListarCambiosSincronizacionHandler handler,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var response = await handler.Handle(
            new(context.UsuarioId(), desde, limite == 0 ? 200 : limite),
            cancellationToken);

        return Results.Ok(new
        {
            cambios = response.Cambios.Select(x => new
            {
                secuencia = x.Secuencia,
                tipoEntidad = x.TipoEntidad,
                entidadId = x.EntidadId,
                operacion = x.Operacion,
                version = x.Version,
                ocurridoEn = x.OcurridoEn
            }),
            siguienteCursor = response.SiguienteCursor,
            hayMas = response.HayMas
        });
    }
}
