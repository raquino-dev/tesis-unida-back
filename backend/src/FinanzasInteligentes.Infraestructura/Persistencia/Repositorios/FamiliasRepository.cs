using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.FinanzasFamiliares;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class FamiliasRepository(FinanzasDbContext db) : IFamiliasRepository
{
    public async Task<IReadOnlyCollection<GrupoFamiliar>> ListarGrupos(Guid usuarioId, CancellationToken ct)
    {
        var ids = db.IntegrantesFamiliares
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .Select(x => x.GrupoFamiliarId);
        return await db.GruposFamiliares.AsNoTracking()
            .Where(x => ids.Contains(x.Id) && x.EliminadoEn == null)
            .OrderBy(x => x.Id).ToListAsync(ct);
    }

    public Task<GrupoFamiliar?> ObtenerGrupo(Guid grupoId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<GrupoFamiliar> query = db.GruposFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == grupoId && x.EliminadoEn == null, ct);
    }

    public Task<IntegranteFamiliar?> ObtenerMembresia(
        Guid grupoId, Guid usuarioId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<IntegranteFamiliar> query = db.IntegrantesFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.GrupoFamiliarId == grupoId && x.UsuarioId == usuarioId && x.EliminadoEn == null, ct);
    }

    public async Task<IReadOnlyCollection<IntegranteFamiliar>> ListarIntegrantes(Guid grupoId, CancellationToken ct) =>
        await db.IntegrantesFamiliares.AsNoTracking()
            .Where(x => x.GrupoFamiliarId == grupoId && x.EliminadoEn == null)
            .OrderBy(x => x.Id).ToListAsync(ct);

    public Task<IntegranteFamiliar?> ObtenerIntegrante(
        Guid grupoId, Guid integranteId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<IntegranteFamiliar> query = db.IntegrantesFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == integranteId && x.GrupoFamiliarId == grupoId && x.EliminadoEn == null, ct);
    }

    public Task<bool> UsuarioEsIntegrante(Guid grupoId, Guid usuarioId, CancellationToken ct) =>
        db.IntegrantesFamiliares.AnyAsync(
            x => x.GrupoFamiliarId == grupoId && x.UsuarioId == usuarioId && x.EliminadoEn == null, ct);

    public async Task<IReadOnlyCollection<InvitacionFamiliar>> ListarInvitaciones(Guid grupoId, CancellationToken ct) =>
        await db.InvitacionesFamiliares.AsNoTracking()
            .Where(x => x.GrupoFamiliarId == grupoId).OrderByDescending(x => x.CreadoEn).ToListAsync(ct);

    public Task<InvitacionFamiliar?> ObtenerInvitacion(
        Guid grupoId, Guid invitacionId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<InvitacionFamiliar> query = db.InvitacionesFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == invitacionId && x.GrupoFamiliarId == grupoId, ct);
    }

    public Task<InvitacionFamiliar?> ObtenerInvitacionPorHashToken(
        string hashToken, bool soloLectura, CancellationToken ct)
    {
        IQueryable<InvitacionFamiliar> query = db.InvitacionesFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.HashToken == hashToken, ct);
    }

    public async Task<IReadOnlyCollection<CuentaCompartida>> ListarCuentasCompartidas(Guid grupoId, CancellationToken ct) =>
        await db.CuentasCompartidas.AsNoTracking()
            .Where(x => x.GrupoFamiliarId == grupoId && x.EliminadoEn == null)
            .OrderBy(x => x.Id).ToListAsync(ct);

    public Task<CuentaCompartida?> ObtenerCuentaCompartida(
        Guid grupoId, Guid cuentaId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<CuentaCompartida> query = db.CuentasCompartidas;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.GrupoFamiliarId == grupoId && x.CuentaId == cuentaId && x.EliminadoEn == null, ct);
    }

    public async Task<IReadOnlyCollection<CategoriaFamiliar>> ListarCategorias(Guid grupoId, CancellationToken ct) =>
        await db.CategoriasFamiliares.AsNoTracking()
            .Where(x => x.GrupoFamiliarId == grupoId && x.EliminadoEn == null)
            .OrderBy(x => x.Nombre).ToListAsync(ct);

    public Task<CategoriaFamiliar?> ObtenerCategoria(
        Guid grupoId, Guid categoriaId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<CategoriaFamiliar> query = db.CategoriasFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == categoriaId && x.GrupoFamiliarId == grupoId && x.EliminadoEn == null, ct);
    }

    public async Task<IReadOnlyCollection<CategoriaFamiliar>> ObtenerCategorias(
        Guid grupoId, IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        await db.CategoriasFamiliares
            .Where(x => x.GrupoFamiliarId == grupoId && ids.Contains(x.Id) && x.EliminadoEn == null)
            .ToListAsync(ct);

    public async Task<IReadOnlyCollection<MovimientoFamiliar>> ListarMovimientos(Guid grupoId, CancellationToken ct) =>
        await db.MovimientosFamiliares.AsNoTracking()
            .Where(x => x.GrupoFamiliarId == grupoId)
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id).ToListAsync(ct);

    public Task<MovimientoFamiliar?> ObtenerMovimiento(
        Guid grupoId, Guid movimientoId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<MovimientoFamiliar> query = db.MovimientosFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == movimientoId && x.GrupoFamiliarId == grupoId, ct);
    }

    public Task<CajaCompartida?> ObtenerCaja(Guid grupoId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<CajaCompartida> query = db.CajasCompartidas;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.GrupoFamiliarId == grupoId, ct);
    }

    public async Task<IReadOnlyCollection<OperacionCaja>> ListarOperacionesCaja(Guid grupoId, CancellationToken ct) =>
        await db.OperacionesCaja.AsNoTracking().Where(x => x.GrupoFamiliarId == grupoId)
            .OrderByDescending(x => x.CreadoEn).ToListAsync(ct);

    public Task<OperacionCaja?> ObtenerOperacionCaja(Guid grupoId, Guid operacionId, CancellationToken ct) =>
        db.OperacionesCaja.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == operacionId && x.GrupoFamiliarId == grupoId, ct);

    public async Task<IReadOnlyCollection<PresupuestoFamiliar>> ListarPresupuestos(Guid grupoId, CancellationToken ct) =>
        await db.PresupuestosFamiliares.AsNoTracking()
            .Where(x => x.GrupoFamiliarId == grupoId && x.EliminadoEn == null)
            .OrderBy(x => x.Id).ToListAsync(ct);

    public Task<PresupuestoFamiliar?> ObtenerPresupuesto(
        Guid grupoId, Guid presupuestoId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<PresupuestoFamiliar> query = db.PresupuestosFamiliares;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == presupuestoId && x.GrupoFamiliarId == grupoId && x.EliminadoEn == null, ct);
    }

    public Task<EliminacionGrupoFamiliar?> ObtenerEliminacion(
        Guid grupoId, Guid eliminacionId, CancellationToken ct) =>
        db.EliminacionesGrupos.AsNoTracking().SingleOrDefaultAsync(
            x => x.Id == eliminacionId && x.GrupoFamiliarId == grupoId, ct);

    public Task<bool> ExisteEliminacionPendiente(Guid grupoId, CancellationToken ct) =>
        db.EliminacionesGrupos.AnyAsync(
            x => x.GrupoFamiliarId == grupoId && x.Estado == "pendiente", ct);

    public void Agregar(GrupoFamiliar x) => db.GruposFamiliares.Add(x);

    public void Agregar(IntegranteFamiliar x) => db.IntegrantesFamiliares.Add(x);

    public void Agregar(InvitacionFamiliar x) => db.InvitacionesFamiliares.Add(x);

    public void Agregar(CuentaCompartida x) => db.CuentasCompartidas.Add(x);

    public void Agregar(CategoriaFamiliar x) => db.CategoriasFamiliares.Add(x);

    public void Agregar(MovimientoFamiliar x) => db.MovimientosFamiliares.Add(x);

    public void Agregar(CajaCompartida x) => db.CajasCompartidas.Add(x);

    public void Agregar(OperacionCaja x) => db.OperacionesCaja.Add(x);

    public void Agregar(PresupuestoFamiliar x) => db.PresupuestosFamiliares.Add(x);

    public void Agregar(EliminacionGrupoFamiliar x) => db.EliminacionesGrupos.Add(x);
}