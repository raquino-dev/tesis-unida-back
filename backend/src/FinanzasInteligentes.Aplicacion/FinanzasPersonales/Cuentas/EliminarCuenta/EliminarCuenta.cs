using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Cuentas.EliminarCuenta;

public sealed record EliminarCuentaCommand(Guid UsuarioId, Guid CuentaId, long VersionEsperada);

public sealed class EliminarCuentaHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(EliminarCuentaCommand command, CancellationToken cancellationToken)
    {
        var cuenta = await finanzas.ObtenerCuenta(command.UsuarioId, command.CuentaId, false, cancellationToken)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");

        if (cuenta.Version != command.VersionEsperada)
            throw new PreconditionFailedException("etag_desactualizado", "La versión de la cuenta está desactualizada.");

        if (await finanzas.CuentaEstaEnUso(command.CuentaId, cancellationToken))
            throw new ConflictException(
                "cuenta_en_uso", "La cuenta posee movimientos o tarjetas asociadas.");

        cuenta.Eliminar();
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
    }
}
