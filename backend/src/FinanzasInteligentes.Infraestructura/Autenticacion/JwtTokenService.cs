using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Identidad;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Autenticacion;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public TokenEmitido Emitir(Usuario usuario, Guid sesionId)
    {
        var ahora = DateTimeOffset.UtcNow;

        var expira = ahora.AddMinutes(_options.AccessTokenMinutes);

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim("role", usuario.Rol),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new Claim("sid", sesionId.ToString())
        };

        var token = new JwtSecurityToken(
            _options.Issuer,
            _options.Audience,
            claims,
            ahora.UtcDateTime,
            expira.UtcDateTime,
            credenciales);

        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hashRefresh = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

        return new TokenEmitido(
            new JwtSecurityTokenHandler().WriteToken(token),
            refreshToken,
            hashRefresh,
            checked(_options.AccessTokenMinutes * 60),
            ahora.AddDays(_options.RefreshTokenDays));
    }
}
