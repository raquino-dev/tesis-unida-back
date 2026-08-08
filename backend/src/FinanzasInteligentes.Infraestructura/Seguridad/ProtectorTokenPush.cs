using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Seguridad;

public sealed class ProtectorTokenPush : IProtectorTokenPush
{
    private readonly byte[] key;

    public ProtectorTokenPush(IOptions<SeguridadOptions> options)
    {
        var secret = options.Value.TokenPushKey;
        key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Proteger(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 4096)
            throw new ArgumentException("El token push no es válido.", nameof(token));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(token);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, tag.Length);
        aes.Encrypt(nonce, plain, cipher, tag);
        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public string Desproteger(string tokenProtegido)
    {
        var payload = Convert.FromBase64String(tokenProtegido);
        if (payload.Length < 29)
            throw new CryptographicException("El token push cifrado no es válido.");
        var nonce = payload.AsSpan(0, 12);
        var tag = payload.AsSpan(12, 16);
        var cipher = payload.AsSpan(28);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, tag.Length);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
