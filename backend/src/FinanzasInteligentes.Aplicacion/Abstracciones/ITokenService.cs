using FinanzasInteligentes.Dominio.Identidad;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface ITokenService
{
    TokenEmitido Emitir(Usuario usuario, Guid sesionId);
}

public sealed record TokenEmitido(
    string AccessToken, string RefreshToken, string HashRefreshToken,
    int ExpiraEnSegundos, DateTimeOffset RefreshTokenExpiraEn);
