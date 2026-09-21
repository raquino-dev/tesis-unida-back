using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.AnularMovimiento;

public sealed record AnularMovimientoCommand(
    Guid UsuarioId,
    Guid MovimientoId,
    long VersionEsperada,
    string CorrelationId);

public sealed class AnularMovimientoHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(AnularMovimientoCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await unidadDeTrabajo.IniciarTransaccion(cancellationToken);

        var movimiento = await finanzas.ObtenerMovimiento(
            command.UsuarioId, command.MovimientoId, false, cancellationToken)
            ?? throw new NotFoundException("movimiento_no_encontrado", "El movimiento no existe.");

        if (movimiento.Version != command.VersionEsperada)
            throw new PreconditionFailedException("etag_desactualizado", "La versión del movimiento está desactualizada.");

        var cuenta = await finanzas.ObtenerCuenta(
            command.UsuarioId, movimiento.CuentaId, false, cancellationToken)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");

        movimiento.Anular("Anulación solicitada por el usuario.");

        if (movimiento.TarjetaCreditoId is { } tarjetaId)
        {
            var tarjeta = await finanzas.ObtenerTarjetaCredito(
                command.UsuarioId, tarjetaId, false, cancellationToken,
                incluirEliminadas: true)
                ?? throw new NotFoundException("tarjeta_no_encontrada", "La tarjeta no existe.");
            tarjeta.RevertirMovimiento(movimiento.Tipo, movimiento.Monto);
            if (movimiento.EsPagoTarjeta)
                cuenta.RevertirMovimiento("gasto", movimiento.Monto);
        }
        else
            cuenta.RevertirMovimiento(movimiento.Tipo, movimiento.Monto);

        finanzas.Agregar(EventoOutbox.Crear(
            "movimiento.anulado", "movimiento", movimiento.Id, new { movimiento.Id }, command.CorrelationId));

        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        await transaction.Confirmar(cancellationToken);
    }
}
