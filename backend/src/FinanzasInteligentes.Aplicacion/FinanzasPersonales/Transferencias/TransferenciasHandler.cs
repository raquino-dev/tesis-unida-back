using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Transferencias;

public sealed record CuentaTransferenciaResponse(Guid Id, string Nombre);
public sealed record TransferenciaResponse(
    Guid Id, CuentaTransferenciaResponse CuentaOrigen, CuentaTransferenciaResponse CuentaDestino,
    long Monto, DateOnly Fecha, string Descripcion, string Estado,
    DateTimeOffset CreadoEn, long Version);
public sealed record PaginacionTransferenciaResponse(string? SiguienteCursor, bool HayMas, long Limite);
public sealed record PaginaTransferenciaResponse(
    IReadOnlyCollection<TransferenciaResponse> Datos, PaginacionTransferenciaResponse Paginacion);

public sealed class TransferenciasHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PaginaTransferenciaResponse> Listar(
        Guid usuarioId, string? cursor, long limite, Guid? cuentaOrigenId,
        Guid? cuentaDestinoId, DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        if (limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");
        if (desde > hasta)
            throw new DomainException("periodo_invalido", "La fecha desde no puede superar a hasta.");
        Guid? cursorId = null;
        if (cursor is not null)
        {
            if (!Guid.TryParse(cursor, out var parsed))
                throw new DomainException("cursor_invalido", "El cursor no es válido.");
            cursorId = parsed;
        }

        var items = (await finanzas.ListarTransferencias(usuarioId, ct))
            .Where(x => cuentaOrigenId is null || x.CuentaOrigenId == cuentaOrigenId)
            .Where(x => cuentaDestinoId is null || x.CuentaDestinoId == cuentaDestinoId)
            .Where(x => desde is null || x.Fecha >= desde)
            .Where(x => hasta is null || x.Fecha <= hasta)
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) < 0)
            .Take(checked((int)limite + 1)).ToArray();
        var hayMas = items.Length > limite;
        var pagina = items.Take(checked((int)limite)).ToArray();
        var response = new List<TransferenciaResponse>();
        foreach (var item in pagina) response.Add(await Map(item, ct));
        return new(response, new(hayMas ? pagina[^1].Id.ToString() : null, hayMas, limite));
    }

    public async Task<TransferenciaResponse> Crear(
        Guid usuarioId, Guid origenId, Guid destinoId, long monto, DateOnly fecha,
        string descripcion, string idempotencyKey, string correlationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
            throw new DomainException("idempotencia_invalida", "Idempotency-Key es requerido.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey)));
        var repetida = await finanzas.ObtenerTransferenciaPorIdempotencia(usuarioId, hash, ct);
        if (repetida is not null)
        {
            if (repetida.CuentaOrigenId != origenId || repetida.CuentaDestinoId != destinoId ||
                repetida.Monto != monto || repetida.Fecha != fecha ||
                repetida.Descripcion != descripcion.Trim())
                throw new ConflictException(
                    "idempotencia_en_conflicto",
                    "La clave de idempotencia ya fue utilizada con otros datos.");
            return await Map(repetida, ct);
        }
        if (origenId == destinoId)
            throw new ConflictException(
                "misma_cuenta", "Las cuentas de origen y destino deben ser distintas.");

        await using var transaccion = await unidadDeTrabajo.IniciarTransaccion(ct);
        var origen = await finanzas.ObtenerCuenta(usuarioId, origenId, false, ct)
            ?? throw new NotFoundException("cuenta_origen_no_encontrada", "La cuenta de origen no existe.");
        var destino = await finanzas.ObtenerCuenta(usuarioId, destinoId, false, ct)
            ?? throw new NotFoundException("cuenta_destino_no_encontrada", "La cuenta de destino no existe.");
        if (origen.SaldoActual < monto)
            throw new ConflictException("saldo_insuficiente", "La cuenta de origen no tiene saldo suficiente.");

        var transferencia = Transferencia.Iniciar(
            usuarioId, origenId, destinoId, monto, fecha, descripcion, hash);
        var egreso = Movimiento.Crear(
            usuarioId, origenId, "gasto", monto, descripcion, fecha,
            "transferencia", transferenciaId: transferencia.Id);
        var ingreso = Movimiento.Crear(
            usuarioId, destinoId, "ingreso", monto, descripcion, fecha,
            "transferencia", transferenciaId: transferencia.Id);
        origen.AplicarMovimiento("gasto", monto);
        destino.AplicarMovimiento("ingreso", monto);
        transferencia.Confirmar(egreso.Id, ingreso.Id);
        finanzas.Agregar(transferencia);
        finanzas.Agregar(egreso);
        finanzas.Agregar(ingreso);
        finanzas.Agregar(EventoOutbox.Crear(
            "transferencia.confirmada", "transferencia", transferencia.Id,
            new { transferencia.Id, transferencia.Monto }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        await transaccion.Confirmar(ct);
        return Map(transferencia, origen, destino);
    }

    public async Task<TransferenciaResponse> Obtener(
        Guid usuarioId, Guid transferenciaId, CancellationToken ct) =>
        await Map(await Existente(usuarioId, transferenciaId, true, ct), ct);

    public async Task Anular(
        Guid usuarioId, Guid transferenciaId, long version,
        string correlationId, CancellationToken ct)
    {
        await using var transaccion = await unidadDeTrabajo.IniciarTransaccion(ct);
        var transferencia = await Existente(usuarioId, transferenciaId, false, ct);
        if (transferencia.Version != version)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión del recurso está desactualizada.");
        if (transferencia.Estado != "confirmada")
            throw new ConflictException("transferencia_anulada", "La transferencia ya fue anulada.");
        var origen = await finanzas.ObtenerCuenta(
            usuarioId, transferencia.CuentaOrigenId, false, ct)
            ?? throw new NotFoundException("cuenta_origen_no_encontrada", "La cuenta de origen no existe.");
        var destino = await finanzas.ObtenerCuenta(
            usuarioId, transferencia.CuentaDestinoId, false, ct)
            ?? throw new NotFoundException("cuenta_destino_no_encontrada", "La cuenta de destino no existe.");
        var egreso = await finanzas.ObtenerMovimiento(
            usuarioId, transferencia.MovimientoEgresoId!.Value, false, ct)
            ?? throw new ConflictException("transferencia_inconsistente", "No se encontró el movimiento de egreso.");
        var ingreso = await finanzas.ObtenerMovimiento(
            usuarioId, transferencia.MovimientoIngresoId!.Value, false, ct)
            ?? throw new ConflictException("transferencia_inconsistente", "No se encontró el movimiento de ingreso.");
        origen.RevertirMovimiento("gasto", transferencia.Monto);
        destino.RevertirMovimiento("ingreso", transferencia.Monto);
        egreso.Anular("Transferencia anulada.");
        ingreso.Anular("Transferencia anulada.");
        transferencia.Anular();
        finanzas.Agregar(EventoOutbox.Crear(
            "transferencia.anulada", "transferencia", transferencia.Id,
            new { transferencia.Id }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        await transaccion.Confirmar(ct);
    }

    private async Task<Transferencia> Existente(
        Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct) =>
        await finanzas.ObtenerTransferencia(usuarioId, id, soloLectura, ct)
        ?? throw new NotFoundException("transferencia_no_encontrada", "La transferencia no existe.");

    private async Task<TransferenciaResponse> Map(Transferencia x, CancellationToken ct)
    {
        var origen = await finanzas.ObtenerCuenta(x.UsuarioId, x.CuentaOrigenId, true, ct)
            ?? throw new NotFoundException("cuenta_origen_no_encontrada", "La cuenta de origen no existe.");
        var destino = await finanzas.ObtenerCuenta(x.UsuarioId, x.CuentaDestinoId, true, ct)
            ?? throw new NotFoundException("cuenta_destino_no_encontrada", "La cuenta de destino no existe.");
        return Map(x, origen, destino);
    }

    private static TransferenciaResponse Map(Transferencia x, Cuenta origen, Cuenta destino) =>
        new(x.Id, new(origen.Id, origen.Nombre), new(destino.Id, destino.Nombre), x.Monto,
            x.Fecha, x.Descripcion, x.Estado, x.CreadoEn, x.Version);
}