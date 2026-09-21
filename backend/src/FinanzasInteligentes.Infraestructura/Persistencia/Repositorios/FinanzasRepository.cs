using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Analitica;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class FinanzasRepository(FinanzasDbContext db) : IFinanzasRepository
{
    public async Task<IReadOnlyCollection<Cuenta>> ListarCuentas(Guid usuarioId, CancellationToken cancellationToken) =>
        await db.Cuentas.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .OrderBy(x => x.CreadoEn).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<Cuenta?> ObtenerCuenta(
        Guid usuarioId,
        Guid cuentaId,
        bool soloLectura,
        CancellationToken cancellationToken)
    {
        IQueryable<Cuenta> query = db.Cuentas;

        if (soloLectura) query = query.AsNoTracking();

        return query.SingleOrDefaultAsync(
            x => x.Id == cuentaId && x.UsuarioId == usuarioId && x.EliminadoEn == null,
            cancellationToken);
    }

    public async Task<bool> CuentaEstaEnUso(
        Guid cuentaId,
        CancellationToken cancellationToken) =>
        await db.Movimientos.AnyAsync(x => x.CuentaId == cuentaId, cancellationToken) ||
        await db.TarjetasCredito.AnyAsync(
            x => x.CuentaPagoId == cuentaId && x.EliminadoEn == null,
            cancellationToken);

    public void Agregar(Cuenta cuenta) => db.Cuentas.Add(cuenta);

    public async Task<IReadOnlyCollection<Categoria>> ListarCategorias(
        Guid usuarioId,
        CancellationToken cancellationToken) =>
        await db.Categorias.AsNoTracking()
            .Where(x => (x.UsuarioId == null || x.UsuarioId == usuarioId) && x.EliminadoEn == null)
            .OrderByDescending(x => x.EsPredeterminada).ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

    public Task<Categoria?> ObtenerCategoria(
        Guid usuarioId,
        Guid categoriaId,
        bool soloLectura,
        CancellationToken cancellationToken)
    {
        IQueryable<Categoria> query = db.Categorias;
        if (soloLectura) query = query.AsNoTracking();

        return query.SingleOrDefaultAsync(
            x => x.Id == categoriaId && (x.UsuarioId == null || x.UsuarioId == usuarioId),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Categoria>> ObtenerCategorias(
        Guid usuarioId,
        IReadOnlyCollection<Guid> categoriaIds,
        CancellationToken cancellationToken) =>
        await db.Categorias
            .Where(x => categoriaIds.Contains(x.Id) &&
                (x.UsuarioId == null || x.UsuarioId == usuarioId) &&
                x.EliminadoEn == null)
            .ToListAsync(cancellationToken);

    public void Agregar(Categoria categoria) => db.Categorias.Add(categoria);

    public async Task<IReadOnlyCollection<TarjetaCredito>> ListarTarjetasCredito(
        Guid usuarioId,
        CancellationToken cancellationToken) =>
        await db.TarjetasCredito.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<TarjetaCredito?> ObtenerTarjetaCredito(
        Guid usuarioId,
        Guid tarjetaId,
        bool soloLectura,
        CancellationToken cancellationToken,
        bool incluirEliminadas = false)
    {
        IQueryable<TarjetaCredito> query = db.TarjetasCredito;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == tarjetaId && x.UsuarioId == usuarioId &&
                (incluirEliminadas || x.EliminadoEn == null),
            cancellationToken);
    }

    public Task<bool> ExisteTarjetaCreditoConAlias(
        Guid usuarioId,
        string alias,
        Guid? exceptoTarjetaId,
        CancellationToken cancellationToken) =>
        db.TarjetasCredito.AnyAsync(
            x => x.UsuarioId == usuarioId && x.Alias == alias &&
                x.EliminadoEn == null &&
                (exceptoTarjetaId == null || x.Id != exceptoTarjetaId),
            cancellationToken);

    public void Agregar(TarjetaCredito tarjeta) => db.TarjetasCredito.Add(tarjeta);

    public async Task<IReadOnlyCollection<Movimiento>> ListarMovimientos(
        Guid usuarioId,
        CancellationToken cancellationToken) =>
        await db.Movimientos.AsNoTracking().Include(x => x.Categorias)
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Movimiento>> ListarMovimientosPagina(
        Guid usuarioId, Guid? cuentaId, DateOnly? desde, DateOnly? hasta,
        DateOnly? cursorFecha, Guid? cursorId, int limite, CancellationToken cancellationToken)
    {
        var query = db.Movimientos.AsNoTracking().Include(x => x.Categorias)
            .Where(x => x.UsuarioId == usuarioId && x.Estado != "anulado");
        if (cuentaId is { } account)
            query = query.Where(x => x.CuentaId == account &&
                (x.TarjetaCreditoId == null || x.OperacionTarjeta == "pago"));
        if (desde is { } start) query = query.Where(x => x.Fecha >= start);
        if (hasta is { } end) query = query.Where(x => x.Fecha <= end);
        if (cursorFecha is { } date && cursorId is { } id)
            query = query.Where(x => x.Fecha < date ||
                (x.Fecha == date && x.Id.CompareTo(id) < 0));
        return await query.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Take(limite).ToListAsync(cancellationToken);
    }

    public Task<Movimiento?> ObtenerMovimiento(
        Guid usuarioId,
        Guid movimientoId,
        bool soloLectura,
        CancellationToken cancellationToken)
    {
        IQueryable<Movimiento> query = db.Movimientos.Include(x => x.Categorias);
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == movimientoId && x.UsuarioId == usuarioId,
            cancellationToken);
    }

    public void Agregar(Movimiento movimiento) => db.Movimientos.Add(movimiento);

    public async Task<IReadOnlyCollection<MovimientoRecurrente>> ListarMovimientosRecurrentes(
        Guid usuarioId,
        CancellationToken cancellationToken) =>
        await db.MovimientosRecurrentes.AsNoTracking().Include(x => x.Categorias)
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<MovimientoRecurrente?> ObtenerMovimientoRecurrente(
        Guid usuarioId,
        Guid recurrenteId,
        bool soloLectura,
        CancellationToken cancellationToken)
    {
        IQueryable<MovimientoRecurrente> query =
            db.MovimientosRecurrentes.Include(x => x.Categorias);
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == recurrenteId && x.UsuarioId == usuarioId && x.EliminadoEn == null,
            cancellationToken);
    }

    public Task<bool> ExisteMovimientoRecurrenteDuplicado(
        Guid usuarioId,
        Guid cuentaId,
        string descripcion,
        Guid? exceptoRecurrenteId,
        CancellationToken cancellationToken) =>
        db.MovimientosRecurrentes.AnyAsync(
            x => x.UsuarioId == usuarioId && x.CuentaId == cuentaId &&
                x.Descripcion == descripcion && x.Estado == "activa" &&
                x.EliminadoEn == null &&
                (exceptoRecurrenteId == null || x.Id != exceptoRecurrenteId),
            cancellationToken);

    public void Agregar(MovimientoRecurrente movimientoRecurrente) =>
        db.MovimientosRecurrentes.Add(movimientoRecurrente);

    public async Task<IReadOnlyCollection<Transferencia>> ListarTransferencias(
        Guid usuarioId, CancellationToken cancellationToken) =>
        await db.Transferencias.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<Transferencia?> ObtenerTransferencia(
        Guid usuarioId, Guid transferenciaId, bool soloLectura,
        CancellationToken cancellationToken)
    {
        IQueryable<Transferencia> query = db.Transferencias;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.Id == transferenciaId, cancellationToken);
    }

    public Task<Transferencia?> ObtenerTransferenciaPorIdempotencia(
        Guid usuarioId, string hashIdempotencia, CancellationToken cancellationToken) =>
        db.Transferencias.AsNoTracking().SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.HashIdempotencia == hashIdempotencia,
            cancellationToken);

    public void Agregar(Transferencia transferencia) => db.Transferencias.Add(transferencia);

    public async Task<IReadOnlyCollection<AlertaFinanciera>> ListarAlertas(
        Guid usuarioId, CancellationToken cancellationToken) =>
        await db.AlertasFinancieras.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.CreadoEn).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<AlertaFinanciera?> ObtenerAlerta(
        Guid usuarioId, Guid alertaId, bool soloLectura, CancellationToken cancellationToken)
    {
        IQueryable<AlertaFinanciera> query = db.AlertasFinancieras;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.Id == alertaId, cancellationToken);
    }

    public void Agregar(AlertaFinanciera alerta) => db.AlertasFinancieras.Add(alerta);

    public async Task<IReadOnlyCollection<Presupuesto>> ListarPresupuestos(
        Guid usuarioId,
        CancellationToken cancellationToken) =>
        await db.Presupuestos.AsNoTracking().Include(x => x.Categorias)
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<Presupuesto?> ObtenerPresupuesto(
        Guid usuarioId,
        Guid presupuestoId,
        bool soloLectura,
        CancellationToken cancellationToken)
    {
        IQueryable<Presupuesto> query = db.Presupuestos.Include(x => x.Categorias);
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == presupuestoId && x.UsuarioId == usuarioId && x.EliminadoEn == null,
            cancellationToken);
    }

    public Task<bool> ExistePresupuestoSolapado(
        Guid usuarioId,
        string periodo,
        IReadOnlyCollection<Guid> categoriaIds,
        Guid? exceptoPresupuestoId,
        CancellationToken cancellationToken) =>
        db.Presupuestos.AnyAsync(
            x => x.UsuarioId == usuarioId && x.Periodo == periodo &&
                x.Estado == "activo" && x.EliminadoEn == null &&
                (exceptoPresupuestoId == null || x.Id != exceptoPresupuestoId) &&
                x.Categorias.Any(c => categoriaIds.Contains(c.Id)),
            cancellationToken);

    public void Agregar(Presupuesto presupuesto) => db.Presupuestos.Add(presupuesto);

    public async Task<IReadOnlyCollection<MetaAhorro>> ListarMetas(
        Guid usuarioId,
        CancellationToken cancellationToken)
    {
        var grupos = db.IntegrantesFamiliares
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .Select(x => x.GrupoFamiliarId);
        return await db.MetasAhorro.AsNoTracking()
            .Where(x => x.EliminadoEn == null &&
                (x.UsuarioId == usuarioId ||
                 (x.GrupoFamiliarId != null && grupos.Contains(x.GrupoFamiliarId.Value))))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<MetaAhorro?> ObtenerMeta(
        Guid metaId,
        bool soloLectura,
        CancellationToken cancellationToken)
    {
        IQueryable<MetaAhorro> query = db.MetasAhorro;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == metaId && x.EliminadoEn == null,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<AporteMeta>> ListarAportes(
        Guid metaId,
        CancellationToken cancellationToken) =>
        await db.AportesMeta.AsNoTracking()
            .Where(x => x.MetaAhorroId == metaId)
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<AporteMeta?> ObtenerAportePorIdempotencia(
        Guid metaId,
        string hashIdempotencia,
        CancellationToken cancellationToken) =>
        db.AportesMeta.AsNoTracking().SingleOrDefaultAsync(
            x => x.MetaAhorroId == metaId && x.HashIdempotencia == hashIdempotencia,
            cancellationToken);

    public void Agregar(MetaAhorro meta) => db.MetasAhorro.Add(meta);

    public void Agregar(AporteMeta aporte) => db.AportesMeta.Add(aporte);

    public void Agregar(EventoOutbox evento) => db.EventosOutbox.Add(evento);
}
