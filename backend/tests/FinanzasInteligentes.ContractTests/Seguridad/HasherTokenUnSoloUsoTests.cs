using FinanzasInteligentes.Infraestructura.Seguridad;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.ContractTests.Seguridad;

public sealed class HasherTokenUnSoloUsoTests
{
    [Fact]
    public void UsaHmacVersionadoYDeterministico()
    {
        var hasher = new HasherTokenUnSoloUso(Options.Create(new SeguridadOptions
        {
            TokenPushKey = "secreto-independiente-con-mas-de-32-bytes"
        }));

        var primero = hasher.Hash("123456");
        var segundo = hasher.Hash("123456");
        var shaSinClave = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes("123456")));

        Assert.Equal("hmac-sha256-v1", hasher.Version);
        Assert.Equal(primero, segundo);
        Assert.Equal(64, primero.Length);
        Assert.NotEqual(shaSinClave, primero);
        Assert.NotEqual(primero, hasher.Hash("654321"));
    }
}
