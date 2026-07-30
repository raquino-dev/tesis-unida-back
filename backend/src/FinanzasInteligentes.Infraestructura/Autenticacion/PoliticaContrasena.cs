using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Excepciones;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.Infraestructura.Autenticacion;

public sealed class PoliticaContrasenaOptions
{
    public const string SectionName = "Contrasenas";
    public int LongitudMinima { get; init; } = 12;
    public int LongitudMaxima { get; init; } = 128;
    public bool RequerirMayuscula { get; init; } = true;
    public bool RequerirMinuscula { get; init; } = true;
    public bool RequerirNumero { get; init; } = true;
    public bool RequerirCaracterEspecial { get; init; } = true;
}

public sealed class PoliticaContrasena(IOptions<PoliticaContrasenaOptions> options) : IPoliticaContrasena
{
    private static readonly HashSet<string> Comunes = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "password123", "12345678", "123456789", "qwerty123",
        "admin123", "contraseña", "contrasena", "finanzas123"
    };

    private readonly PoliticaContrasenaOptions _options = options.Value;

    public void Validar(string contrasena, string? correo = null, string? nombre = null)
    {
        if (string.IsNullOrWhiteSpace(contrasena) ||
            contrasena.Length < _options.LongitudMinima ||
            contrasena.Length > _options.LongitudMaxima)
            Fallar($"La contraseña debe tener entre {_options.LongitudMinima} y {_options.LongitudMaxima} caracteres.");
        if (_options.RequerirMayuscula && !contrasena.Any(char.IsUpper))
            Fallar("La contraseña debe contener al menos una letra mayúscula.");
        if (_options.RequerirMinuscula && !contrasena.Any(char.IsLower))
            Fallar("La contraseña debe contener al menos una letra minúscula.");
        if (_options.RequerirNumero && !contrasena.Any(char.IsDigit))
            Fallar("La contraseña debe contener al menos un número.");
        if (_options.RequerirCaracterEspecial && !contrasena.Any(x => !char.IsLetterOrDigit(x)))
            Fallar("La contraseña debe contener al menos un carácter especial.");
        var comunNormalizada = new string(contrasena.Where(char.IsLetterOrDigit).ToArray());
        if (Comunes.Contains(contrasena) || Comunes.Contains(comunNormalizada))
            Fallar("La contraseña es demasiado común.");

        var parteLocal = correo?.Split('@', 2)[0];
        if ((!string.IsNullOrWhiteSpace(parteLocal) && parteLocal.Length >= 4 &&
             contrasena.Contains(parteLocal, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(nombre) && nombre.Length >= 4 &&
             contrasena.Contains(nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
            Fallar("La contraseña no debe contener el nombre ni el correo del usuario.");
    }

    private static void Fallar(string detalle) =>
        throw new DomainException("contrasena_invalida", detalle);
}
