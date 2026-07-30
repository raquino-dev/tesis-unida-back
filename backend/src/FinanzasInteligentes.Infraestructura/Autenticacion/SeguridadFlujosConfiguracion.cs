using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.Infraestructura.Autenticacion;

public sealed class SeguridadFlujosOptions
{
    public const string SectionName = "SeguridadFlujos";
    public int OtpMinutos { get; init; } = 5;
    public int VerificacionOtpMinutos { get; init; } = 10;
    public int RecuperacionContrasenaMinutos { get; init; } = 30;
}

public sealed class SeguridadFlujosConfiguracion(
    IOptions<SeguridadFlujosOptions> options) : ISeguridadFlujosConfiguracion
{
    public int OtpMinutos => options.Value.OtpMinutos;
    public int VerificacionOtpMinutos => options.Value.VerificacionOtpMinutos;
    public int RecuperacionContrasenaMinutos => options.Value.RecuperacionContrasenaMinutos;
}
