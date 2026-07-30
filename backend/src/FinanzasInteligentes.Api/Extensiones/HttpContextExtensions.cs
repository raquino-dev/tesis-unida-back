using System.Security.Claims;

namespace FinanzasInteligentes.Api.Extensiones;

public static class HttpContextExtensions
{
    public static Guid UsuarioId(this HttpContext context)
    {
        var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("El token no contiene un usuario válido.");
    }

    public static Guid SesionId(this HttpContext context)
    {
        var value = context.User.FindFirstValue("sid");
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("El token no contiene una sesión válida.");
    }
}

public static class ETagExtensions
{
    public static string Formatear(long version) => $"\"{version}\"";

    public static bool TryObtenerVersionIfMatch(this HttpRequest request, out long version)
    {
        version = 0;
        var value = request.Headers.IfMatch.FirstOrDefault();
        return value is not null &&
            long.TryParse(value.Trim().Trim('"'), out version) &&
            version > 0;
    }

    public static IResult IfMatchInvalido(HttpContext context) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "If-Match requerido",
            detail: "Debe enviar un ETag numérico válido en If-Match.",
            instance: context.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["codigo"] = "if_match_requerido",
                ["correlationId"] = context.TraceIdentifier
            });
}