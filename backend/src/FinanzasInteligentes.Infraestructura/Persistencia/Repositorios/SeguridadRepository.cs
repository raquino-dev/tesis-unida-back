using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class SeguridadRepository(FinanzasDbContext db) : ISeguridadRepository
{
    public Task<Dispositivo?> ObtenerDispositivo(
        Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct)
    {
        IQueryable<Dispositivo> query = db.Dispositivos;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.Id == id && x.RevocadoEn == null, ct);
    }

    public Task<Dispositivo?> ObtenerDispositivoPorInstalacion(
        Guid usuarioId, string identificador, CancellationToken ct) =>
        db.Dispositivos.AsNoTracking().SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId &&
                x.IdentificadorInstalacion == identificador && x.RevocadoEn == null, ct);

    public async Task<IReadOnlyCollection<EventoSeguridad>> ListarEventosSeguridad(
        Guid usuarioId, CancellationToken ct) =>
        await db.EventosSeguridad.AsNoTracking().Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.OcurridoEn).ThenByDescending(x => x.Id).ToListAsync(ct);

    public async Task<IReadOnlyCollection<EventoAuditoria>> ListarEventosAuditoria(
        Guid usuarioId, CancellationToken ct)
    {
        var grupos = db.IntegrantesFamiliares.Where(x =>
            x.UsuarioId == usuarioId && x.EliminadoEn == null).Select(x => x.GrupoFamiliarId);
        return await db.EventosAuditoria.AsNoTracking()
            .Where(x => x.UsuarioId != null && (x.UsuarioId == usuarioId ||
                (x.GrupoFamiliarId != null && grupos.Contains(x.GrupoFamiliarId.Value)))
            )
            .OrderByDescending(x => x.OcurridoEn).ThenByDescending(x => x.Id).ToListAsync(ct);
    }

    public Task RevocarSesionesDeDispositivo(
        Guid usuarioId, string identificador, CancellationToken ct) =>
        db.Sesiones.Where(x => x.UsuarioId == usuarioId &&
                x.IdentificadorDispositivo == identificador && x.RevocadoEn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevocadoEn, DateTimeOffset.UtcNow), ct);

    public void Agregar(Dispositivo entity) => db.Dispositivos.Add(entity);

    public void Agregar(EventoSeguridad entity) => db.EventosSeguridad.Add(entity);

    public void Agregar(EventoAuditoria entity) => db.EventosAuditoria.Add(entity);
}