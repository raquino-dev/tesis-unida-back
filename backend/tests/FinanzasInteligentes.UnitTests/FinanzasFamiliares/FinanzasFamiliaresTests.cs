using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasFamiliares;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.UnitTests.FinanzasFamiliares;

public sealed class FinanzasFamiliaresTests
{
    [Fact]
    public void PropietarioNoPuedeSerEliminadoNiDegradado()
    {
        var integrante = IntegranteFamiliar.Crear(
            Guid.CreateVersion7(), Guid.CreateVersion7(), "propietario");

        Assert.Throws<DomainException>(() => integrante.CambiarRol("integrante"));
        Assert.Throws<DomainException>(integrante.Eliminar);
    }

    [Fact]
    public void CajaMantieneSaldoYRechazaSobregiro()
    {
        var caja = CajaCompartida.Crear(Guid.CreateVersion7());

        var aporte = caja.Aplicar("aporte", 1_000_000);
        var retiro = caja.Aplicar("retiro", 250_000);

        Assert.Equal((0, 1_000_000), aporte);
        Assert.Equal((1_000_000, 750_000), retiro);
        Assert.Equal(750_000, caja.Saldo);
        Assert.Throws<DomainException>(() => caja.Aplicar("retiro", 800_000));
    }

    [Fact]
    public void InvitacionSoloAceptaCodigoCorrectoUnaVez()
    {
        var codigo = Hash("482915");
        var invitacion = InvitacionFamiliar.Crear(
            Guid.CreateVersion7(), "familiar@example.com", null, "integrante",
            Hash("token"), codigo);

        invitacion.Aceptar(codigo);

        Assert.Equal("aceptada", invitacion.Estado);
        Assert.Throws<DomainException>(() => invitacion.Aceptar(codigo));
    }

    [Fact]
    public void PresupuestoCalculaActualizacionVersionada()
    {
        var presupuesto = PresupuestoFamiliar.Crear(
            Guid.CreateVersion7(), "Hogar", 2_000_000, "mensual", []);

        presupuesto.Actualizar(null, 2_500_000, null, null);

        Assert.Equal(2_500_000, presupuesto.Monto);
        Assert.Equal(2, presupuesto.Version);
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}