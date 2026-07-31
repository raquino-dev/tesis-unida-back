using System.Security.Cryptography;
using FinanzasInteligentes.Infraestructura.Seguridad;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.ContractTests.Seguridad;

public sealed class ProtectorTokenPushTests
{
    private static ProtectorTokenPush Crear(string? key = null) =>
        new(Options.Create(new SeguridadOptions
        {
            TokenPushKey = key ?? "secreto-push-independiente-con-mas-de-32-bytes"
        }));

    [Fact]
    public void CifraConNonceAleatorioYRecuperaElToken()
    {
        const string token = "fcm-token-de-prueba";
        var protector = Crear();

        var primero = protector.Proteger(token);
        var segundo = protector.Proteger(token);

        Assert.NotEqual(token, primero);
        Assert.NotEqual(primero, segundo);
        Assert.Equal(token, protector.Desproteger(primero));
        Assert.Equal(token, protector.Desproteger(segundo));
    }

    [Fact]
    public void RechazaTokenAlterado()
    {
        var protector = Crear();
        var payload = Convert.FromBase64String(
            protector.Proteger("fcm-token-de-prueba"));
        payload[^1] ^= 0x01;

        Assert.Throws<AuthenticationTagMismatchException>(
            () => protector.Desproteger(Convert.ToBase64String(payload)));
    }
}
