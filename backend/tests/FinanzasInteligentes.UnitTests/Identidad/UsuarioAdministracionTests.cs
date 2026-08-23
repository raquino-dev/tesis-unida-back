using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Identidad;

namespace FinanzasInteligentes.UnitTests.Identidad;

public sealed class UsuarioAdministracionTests
{
    [Fact]
    public void UsuarioPuedeDesactivarseYReactivarse()
    {
        var usuario = CrearUsuario();

        usuario.CambiarEstado("inactivo");
        Assert.Equal("inactivo", usuario.Estado);

        usuario.CambiarEstado("activo");
        Assert.Equal("activo", usuario.Estado);
    }

    [Fact]
    public void RechazaEstadoYRolDesconocidos()
    {
        var usuario = CrearUsuario();

        Assert.Throws<DomainException>(() => usuario.CambiarEstado("bloqueado"));
        Assert.Throws<DomainException>(() => usuario.CambiarRol("superadmin"));
    }

    [Fact]
    public void AnonimizacionRetiraRolYNoPermiteReactivar()
    {
        var usuario = CrearUsuario();
        usuario.CambiarRol("administrador");

        usuario.Anonimizar();

        Assert.Equal("eliminado", usuario.Estado);
        Assert.Equal("usuario", usuario.Rol);
        Assert.Throws<DomainException>(() => usuario.CambiarEstado("activo"));
    }

    [Fact]
    public void UsuarioConservaAliasVisibleSinArroba()
    {
        var usuario = Usuario.Crear(
            "rodrigo@example.com", "Rodrigo", "hash-seguro", alias: "@Rodrigo001");

        Assert.Equal("Rodrigo", usuario.Nombre);
        Assert.Equal("Rodrigo001", usuario.Alias);
    }

    [Theory]
    [InlineData("2rodrigo")]
    [InlineData("rodrigo.aquino")]
    [InlineData("ro drigo")]
    public void RechazaAliasConFormatoInvalido(string alias)
    {
        var exception = Assert.Throws<DomainException>(() => Usuario.Crear(
            "rodrigo@example.com", "Rodrigo", "hash-seguro", alias: alias));

        Assert.Equal("alias_invalido", exception.Code);
    }

    private static Usuario CrearUsuario() =>
        Usuario.Crear("usuario@example.com", "Usuario Prueba", "hash-seguro");
}
