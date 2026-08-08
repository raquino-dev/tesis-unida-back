using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Seguridad;

public sealed class HasherTokenUnSoloUso : IHasherTokenUnSoloUso
{
    public string Version => "hmac-sha256-v1";

    private readonly byte[] key;

    public HasherTokenUnSoloUso(IOptions<SeguridadOptions> options)
    {
        var rootKey = Encoding.UTF8.GetBytes(options.Value.TokenPushKey);
        key = HMACSHA256.HashData(
            rootKey, Encoding.UTF8.GetBytes("finanzas-inteligentes:token-un-solo-uso:v1"));
    }

    public string Hash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Convert.ToHexStringLower(
            HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value)));
    }
}
