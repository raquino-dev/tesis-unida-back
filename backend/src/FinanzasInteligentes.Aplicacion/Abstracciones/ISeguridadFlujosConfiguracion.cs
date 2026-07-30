namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface ISeguridadFlujosConfiguracion
{
    int OtpMinutos { get; }
    int VerificacionOtpMinutos { get; }
    int RecuperacionContrasenaMinutos { get; }
}
