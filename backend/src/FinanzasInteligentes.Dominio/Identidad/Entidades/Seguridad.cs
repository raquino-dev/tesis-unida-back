using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;
using System.Security.Cryptography;

namespace FinanzasInteligentes.Dominio.Identidad;

public sealed class DesafioOtp : MutableEntity
{
    private DesafioOtp()
    { }

    public Guid UsuarioId { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public string Canal { get; private set; } = string.Empty;
    public string HashCodigo { get; private set; } = string.Empty;
    public string Destino { get; private set; } = string.Empty;
    public DateTimeOffset ExpiraEn { get; private set; }
    public int IntentosRestantes { get; private set; } = 5;
    public DateTimeOffset? VerificadoEn { get; private set; }

    public static DesafioOtp Crear(
        Guid usuarioId, string motivo, string canal, string hashCodigo,
        string destino, DateTimeOffset expiraEn)
    {
        if (motivo is not ("cambio-contrasena" or "eliminacion-perfil" or
            "eliminacion-grupo-familiar" or "operacion-sensible"))
            throw new DomainException("motivo_otp_invalido", "El motivo OTP no es válido.");
        if (canal != "correo")
            throw new DomainException("canal_otp_invalido", "Sólo se admite el canal correo.");
        return new()
        {
            UsuarioId = usuarioId,
            Motivo = motivo,
            Canal = canal,
            HashCodigo = hashCodigo,
            Destino = destino,
            ExpiraEn = expiraEn
        };
    }

    public void Verificar(string hashCodigo)
    {
        if (VerificadoEn is not null)
            throw new DomainException("otp_utilizado", "El desafío OTP ya fue verificado.");
        if (ExpiraEn <= DateTimeOffset.UtcNow)
            throw new DomainException("otp_expirado", "El desafío OTP expiró.");
        if (IntentosRestantes <= 0)
            throw new DomainException("otp_bloqueado", "El desafío OTP no admite más intentos.");
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(HashCodigo), Convert.FromHexString(hashCodigo)))
        {
            IntentosRestantes--;
            Touch();
            throw new DomainException("otp_invalido", "El código OTP no es válido.");
        }
        VerificadoEn = DateTimeOffset.UtcNow;
        Touch();
    }
}

public sealed class EliminacionPerfil : MutableEntity
{
    private EliminacionPerfil()
    { }

    public Guid UsuarioId { get; private set; }
    public string Estado { get; private set; } = "pendiente";
    public DateTimeOffset? CompletadoEn { get; private set; }
    public string? ErrorCodigo { get; private set; }

    public static EliminacionPerfil Crear(Guid usuarioId) => new() { UsuarioId = usuarioId };

    public void Completar()
    {
        if (Estado != "pendiente") return;
        Estado = "completado";
        CompletadoEn = DateTimeOffset.UtcNow;
        Touch();
    }
}

public sealed class VerificacionOtp : Entity
{
    private VerificacionOtp()
    { }

    public Guid UsuarioId { get; private set; }
    public Guid DesafioId { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? ConsumidoEn { get; private set; }

    public static VerificacionOtp Crear(DesafioOtp desafio, DateTimeOffset expiraEn) => new()
    {
        UsuarioId = desafio.UsuarioId,
        DesafioId = desafio.Id,
        Motivo = desafio.Motivo,
        ExpiraEn = expiraEn
    };

    public void Consumir(string motivo)
    {
        if (ConsumidoEn is not null || ExpiraEn <= DateTimeOffset.UtcNow || Motivo != motivo)
            throw new DomainException("verificacion_otp_invalida", "La verificación OTP no es válida.");
        ConsumidoEn = DateTimeOffset.UtcNow;
    }
}

public sealed class RecuperacionContrasena : Entity
{
    private RecuperacionContrasena()
    { }

    public Guid UsuarioId { get; private set; }
    public string HashToken { get; private set; } = string.Empty;
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? ConsumidoEn { get; private set; }
    public int IntentosRestantes { get; private set; } = 5;

    public static RecuperacionContrasena Crear(
        Guid usuarioId, string hashToken, DateTimeOffset expiraEn) =>
        new() { UsuarioId = usuarioId, HashToken = hashToken, ExpiraEn = expiraEn };

    public void Consumir()
    {
        if (ConsumidoEn is not null || ExpiraEn <= DateTimeOffset.UtcNow)
            throw new DomainException("token_recuperacion_invalido", "El token no es válido o expiró.");
        ConsumidoEn = DateTimeOffset.UtcNow;
    }

    public void VerificarYConsumir(string hashCodigo)
    {
        if (ConsumidoEn is not null)
            throw new DomainException("codigo_recuperacion_utilizado", "El código de recuperación ya fue utilizado.");
        if (ExpiraEn <= DateTimeOffset.UtcNow)
            throw new DomainException("codigo_recuperacion_expirado", "El código de recuperación expiró.");
        if (IntentosRestantes <= 0)
            throw new DomainException("codigo_recuperacion_bloqueado", "El código de recuperación no admite más intentos.");
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(HashToken), Convert.FromHexString(hashCodigo)))
        {
            IntentosRestantes--;
            throw new DomainException("codigo_recuperacion_invalido", "El código de recuperación no es válido.");
        }

        ConsumidoEn = DateTimeOffset.UtcNow;
    }
}
