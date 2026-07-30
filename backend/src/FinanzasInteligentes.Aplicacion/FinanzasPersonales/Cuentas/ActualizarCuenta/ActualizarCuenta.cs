using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.ActualizarCuenta;

public sealed record ActualizarCuentaCommand(
    Guid UsuarioId,
    Guid CuentaId,
    long VersionEsperada,
    string? Nombre,
    string? Tipo,
    string? Moneda,
    long? SaldoInicial,
    string? Color,
    string? Icono,
    bool? IncluidaEnTotal);

public sealed class ActualizarCuentaHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<CuentaResponse> Handle(
        ActualizarCuentaCommand command,
        CancellationToken cancellationToken)
    {
        var cuenta = await finanzas.ObtenerCuenta(
            command.UsuarioId, command.CuentaId, false, cancellationToken)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");

        if (cuenta.Version != command.VersionEsperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión de la cuenta está desactualizada.");
        if (command.Moneda is not null && command.Moneda != "PYG")
            throw new DomainException("moneda_no_soportada", "La primera versión sólo admite PYG.");

        cuenta.Actualizar(
            command.Nombre,
            command.Tipo,
            command.SaldoInicial,
            command.Color,
            command.Icono,
            command.IncluidaEnTotal);

        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return cuenta.ToResponse();
    }
}