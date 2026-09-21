using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Movimientos.ActualizarMovimiento;

public sealed record ActualizarMovimientoCommand(
    Guid UsuarioId,
    Guid MovimientoId,
    long VersionEsperada,
    string? Descripcion,
    IReadOnlyCollection<Guid>? CategoriaIds,
    Guid? DocumentoId,
    string CorrelationId,
    Guid? CuentaId = null,
    string? Tipo = null,
    long? Monto = null,
    DateOnly? Fecha = null,
    TimeOnly? Hora = null);

public sealed class ActualizarMovimientoHandler(
    IFinanzasRepository finanzas,
    IDocumentosRepository documentos,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<MovimientoResponse> Handle(
        ActualizarMovimientoCommand command,
        CancellationToken cancellationToken)
    {
        var movimiento = await finanzas.ObtenerMovimiento(
            command.UsuarioId, command.MovimientoId, false, cancellationToken)
            ?? throw new NotFoundException(
                "movimiento_no_encontrado", "El movimiento no existe.");
        if (movimiento.Version != command.VersionEsperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión del movimiento está desactualizada.");

        IReadOnlyCollection<Categoria>? categorias = null;
        if (command.CategoriaIds is not null)
        {
            var ids = command.CategoriaIds.Distinct().ToArray();
            categorias = await finanzas.ObtenerCategorias(
                command.UsuarioId, ids, cancellationToken);
            if (categorias.Count != ids.Length)
                throw new NotFoundException(
                    "categoria_no_encontrada", "Una o más categorías no existen.");
        }

        if (command.DocumentoId is not null &&
            await documentos.ObtenerDocumento(
                command.UsuarioId, command.DocumentoId.Value, true, cancellationToken) is null)
            throw new NotFoundException(
                "documento_no_encontrado", "El comprobante no existe.");

        var cuentaOriginalId = movimiento.CuentaId;
        var tipoOriginal = movimiento.Tipo;
        var montoOriginal = movimiento.Monto;
        var cambiaImporte = command.CuentaId is not null || command.Tipo is not null || command.Monto is not null;
        await using var transaction = await unidadDeTrabajo.IniciarTransaccion(cancellationToken);
        Cuenta? cuentaOriginal = null;
        Cuenta? cuentaNueva = null;
        if (cambiaImporte && movimiento.TarjetaCreditoId is null)
        {
            cuentaOriginal = await finanzas.ObtenerCuenta(
                command.UsuarioId, cuentaOriginalId, false, cancellationToken)
                ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta original no existe.");
            cuentaNueva = command.CuentaId is null || command.CuentaId == cuentaOriginalId
                ? cuentaOriginal
                : await finanzas.ObtenerCuenta(command.UsuarioId, command.CuentaId.Value, false, cancellationToken)
                    ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta nueva no existe.");
        }
        if (movimiento.TarjetaCreditoId is not null && command.CuentaId is not null &&
            command.CuentaId != cuentaOriginalId)
            throw new DomainException("cuenta_tarjeta_inmutable", "La cuenta de pago de una tarjeta no puede cambiarse desde el movimiento.");

        movimiento.Actualizar(command.Descripcion, categorias, command.DocumentoId,
            command.CuentaId, command.Tipo, command.Monto, command.Fecha, command.Hora);
        if (cambiaImporte)
        {
            if (movimiento.TarjetaCreditoId is { } tarjetaId)
            {
                var tarjeta = await finanzas.ObtenerTarjetaCredito(
                    command.UsuarioId, tarjetaId, false, cancellationToken,
                    incluirEliminadas: true)
                    ?? throw new NotFoundException("tarjeta_no_encontrada", "La tarjeta no existe.");
                tarjeta.RevertirMovimiento(tipoOriginal, montoOriginal);
                tarjeta.AplicarMovimiento(movimiento.Tipo, movimiento.Monto);
                if (movimiento.EsPagoTarjeta)
                {
                    var cuentaPago = await finanzas.ObtenerCuenta(
                        command.UsuarioId, cuentaOriginalId, false, cancellationToken)
                        ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta de pago no existe.");
                    cuentaPago.RevertirMovimiento("gasto", montoOriginal);
                    cuentaPago.AplicarMovimiento("gasto", movimiento.Monto);
                }
            }
            else
            {
                cuentaOriginal!.RevertirMovimiento(tipoOriginal, montoOriginal);
                cuentaNueva!.AplicarMovimiento(movimiento.Tipo, movimiento.Monto);
            }
        }
        finanzas.Agregar(EventoOutbox.Crear(
            "movimiento.actualizado", "movimiento", movimiento.Id,
            new { movimiento.Id }, command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        await transaction.Confirmar(cancellationToken);
        return movimiento.ToResponse();
    }
}
