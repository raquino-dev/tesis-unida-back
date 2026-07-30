using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.CrearCuenta;

public sealed record CrearCuentaCommand(
    Guid UsuarioId,
    string CorrelationId,
    string Nombre,
    string Tipo,
    long SaldoInicial = 0,
    string Moneda = "PYG",
    string? Color = null,
    string? Icono = null,
    bool IncluidaEnTotal = true);

public sealed class CrearCuentaHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<CuentaResponse> Handle(CrearCuentaCommand command, CancellationToken cancellationToken)
    {
        if (command.Moneda != "PYG")
            throw new DomainException("moneda_no_soportada", "La primera versión sólo admite PYG.");

        var cuenta = Cuenta.Crear(
            command.UsuarioId, command.Nombre, command.Tipo, command.SaldoInicial,
            command.Color, command.Icono, command.IncluidaEnTotal);

        finanzas.Agregar(cuenta);

        finanzas.Agregar(EventoOutbox.Crear(
            "cuenta.creada", "cuenta", cuenta.Id, new { cuenta.Id }, command.CorrelationId));

        await unidadDeTrabajo.GuardarCambios(cancellationToken);

        return cuenta.ToResponse();
    }
}
