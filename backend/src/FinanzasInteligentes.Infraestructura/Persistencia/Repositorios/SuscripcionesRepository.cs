using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Suscripciones;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class SuscripcionesRepository(FinanzasDbContext db) : ISuscripcionesRepository
{
    public async Task<IReadOnlyCollection<PlanSuscripcion>> ListarPlanes(CancellationToken ct) =>
        await db.PlanesSuscripcion.AsNoTracking().Where(x => x.Activo)
            .OrderBy(x => x.Precio).ThenBy(x => x.Codigo).ToListAsync(ct);

    public Task<PlanSuscripcion?> ObtenerPlanPorCodigo(string codigo, CancellationToken ct) =>
        db.PlanesSuscripcion.AsNoTracking().SingleOrDefaultAsync(
            x => x.Codigo == codigo && x.Activo, ct);

    public Task<PlanSuscripcion?> ObtenerPlan(Guid planId, CancellationToken ct) =>
        db.PlanesSuscripcion.AsNoTracking().SingleOrDefaultAsync(x => x.Id == planId, ct);

    public Task<Suscripcion?> ObtenerVigente(Guid usuarioId, bool soloLectura, CancellationToken ct)
    {
        IQueryable<Suscripcion> query = db.Suscripciones;
        if (soloLectura) query = query.AsNoTracking();
        return query.Where(x => x.UsuarioId == usuarioId &&
                (x.Estado == "activa" ||
                 x.Estado == "en_gracia" ||
                 x.Estado == "cancelada" && x.FinPeriodoEn > DateTimeOffset.UtcNow))
            .OrderByDescending(x => x.IniciadaEn).FirstOrDefaultAsync(ct);
    }

    public Task<Suscripcion?> ObtenerPorComprobante(
        string proveedor, string hashComprobante, bool soloLectura, CancellationToken ct)
    {
        IQueryable<Suscripcion> query = db.Suscripciones;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Proveedor == proveedor && x.HashComprobante == hashComprobante, ct);
    }

    public void Agregar(Suscripcion suscripcion) => db.Suscripciones.Add(suscripcion);

    public async Task<IReadOnlyCollection<TransaccionSuscripcion>> ListarTransacciones(
        Guid usuarioId, CancellationToken ct) =>
        await db.TransaccionesSuscripcion.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.OcurridoEn)
            .Take(100)
            .ToListAsync(ct);

    public Task<bool> ExisteTransaccion(
        string proveedor, string referenciaHash, string tipo, CancellationToken ct) =>
        db.TransaccionesSuscripcion.AsNoTracking().AnyAsync(
            x => x.Proveedor == proveedor &&
                 x.ReferenciaExternaHash == referenciaHash &&
                 x.Tipo == tipo, ct);

    public void Agregar(TransaccionSuscripcion transaccion) =>
        db.TransaccionesSuscripcion.Add(transaccion);
}
