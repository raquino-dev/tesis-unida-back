using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Identidad;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.UnitTests.Identidad;

public sealed class SeguridadTests
{
    [Fact]
    public void OtpCorrectoGeneraVerificacionConsumibleUnaSolaVez()
    {
        var desafio = CrearDesafio("123456");
        desafio.Verificar(Hash("123456"));
        var verificacion = VerificacionOtp.Crear(
            desafio, DateTimeOffset.UtcNow.AddMinutes(10));

        verificacion.Consumir("cambio-contrasena");

        Assert.Throws<DomainException>(() => verificacion.Consumir("cambio-contrasena"));
    }

    [Fact]
    public void OtpIncorrectoReduceIntentos()
    {
        var desafio = CrearDesafio("123456");

        var exception = Assert.Throws<DomainException>(() => desafio.Verificar(Hash("000000")));

        Assert.Equal("otp_invalido", exception.Code);
        Assert.Equal(4, desafio.IntentosRestantes);
    }

    [Fact]
    public void RecuperacionSoloPuedeConsumirseUnaVez()
    {
        var recuperacion = RecuperacionContrasena.Crear(
            Guid.CreateVersion7(), Hash("token"), DateTimeOffset.UtcNow.AddMinutes(30));

        recuperacion.Consumir();

        Assert.Throws<DomainException>(recuperacion.Consumir);
    }

    [Fact]
    public void EliminacionPerfilCompletaElProceso()
    {
        var eliminacion = EliminacionPerfil.Crear(Guid.CreateVersion7());

        eliminacion.Completar();

        Assert.Equal("completado", eliminacion.Estado);
        Assert.NotNull(eliminacion.CompletadoEn);
        Assert.Equal(2, eliminacion.Version);
    }

    [Fact]
    public void AnonimizarUsuarioEliminaDatosPersonales()
    {
        var usuario = Usuario.Crear(
            "usuario@example.com", "Usuario", "hash-seguro");
        usuario.ActualizarPerfil(null, null, "Asunción", null);

        usuario.Anonimizar();

        Assert.Equal("eliminado", usuario.Estado);
        Assert.Equal("Usuario eliminado", usuario.Nombre);
        Assert.Equal(string.Empty, usuario.Ubicacion);
        Assert.StartsWith($"anon-{usuario.Id:N}@", usuario.Correo);
        Assert.NotNull(usuario.AnonimizadoEn);
        Assert.Equal(3, usuario.Version);
    }

    private static DesafioOtp CrearDesafio(string codigo) =>
        DesafioOtp.Crear(
            Guid.CreateVersion7(), "cambio-contrasena", "correo",
            Hash(codigo), "usuario@example.com", DateTimeOffset.UtcNow.AddMinutes(5));

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
