using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Infraestructura.Autenticacion;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.ContractTests.Seguridad;

public sealed class PoliticaContrasenaTests
{
    private readonly PoliticaContrasena _politica =
        new(Options.Create(new PoliticaContrasenaOptions()));

    [Theory]
    [InlineData("corta1!A")]
    [InlineData("solominusculas123!")]
    [InlineData("SOLOMAYUSCULAS123!")]
    [InlineData("SinNumerosEspecial!")]
    [InlineData("SinCaracterEspecial123")]
    [InlineData("Password123!")]
    public void RechazaContrasenasDebiles(string contrasena) =>
        Assert.Throws<DomainException>(() => _politica.Validar(contrasena));

    [Fact]
    public void AceptaContrasenaFuerteQueNoContieneDatosPersonales() =>
        _politica.Validar("Nube-Verde-9472!", "persona@example.com", "Persona");

    [Fact]
    public void RechazaContrasenaQueContieneElCorreo()
    {
        var exception = Assert.Throws<DomainException>(() =>
            _politica.Validar("Rodrigo-9472!", "rodrigo@example.com", "Persona"));
        Assert.Equal("contrasena_invalida", exception.Code);
    }
}
