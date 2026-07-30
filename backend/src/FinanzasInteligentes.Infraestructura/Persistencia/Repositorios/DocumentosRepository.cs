using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Documentos;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class DocumentosRepository(FinanzasDbContext db) : IDocumentosRepository
{
    public async Task<IReadOnlyCollection<DocumentoFinanciero>> ListarDocumentos(
        Guid usuarioId, CancellationToken ct) =>
        await db.DocumentosFinancieros.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .OrderByDescending(x => x.CreadoEn).ThenByDescending(x => x.Id).ToListAsync(ct);

    public Task<DocumentoFinanciero?> ObtenerDocumento(
        Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct)
    {
        IQueryable<DocumentoFinanciero> query = db.DocumentosFinancieros;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.Id == id && x.EliminadoEn == null, ct);
    }

    public Task<DocumentoFinanciero?> ObtenerDocumentoPorIdempotencia(
        Guid usuarioId, string hash, CancellationToken ct) =>
        db.DocumentosFinancieros.AsNoTracking().SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.HashIdempotencia == hash &&
                x.EliminadoEn == null, ct);

    public Task<int> ContarDocumentos(Guid usuarioId, CancellationToken ct) =>
        db.DocumentosFinancieros.AsNoTracking().CountAsync(
            x => x.UsuarioId == usuarioId && x.EliminadoEn == null, ct);

    public Task<bool> ExisteDocumentoConHash(
        Guid usuarioId, string sha256, CancellationToken ct) =>
        db.DocumentosFinancieros.AsNoTracking().AnyAsync(
            x => x.UsuarioId == usuarioId && x.Sha256 == sha256 && x.EliminadoEn == null, ct);

    public Task<ProcesamientoDocumental?> ObtenerProcesamiento(
        Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct)
    {
        IQueryable<ProcesamientoDocumental> query = db.ProcesamientosDocumentales;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id &&
            db.DocumentosFinancieros.Any(d => d.Id == x.DocumentoId &&
                d.UsuarioId == usuarioId && d.EliminadoEn == null), ct);
    }

    public Task<ProcesamientoDocumental?> ObtenerUltimoProcesamiento(
        Guid documentoId, CancellationToken ct) =>
        db.ProcesamientosDocumentales.AsNoTracking()
            .Where(x => x.DocumentoId == documentoId)
            .OrderByDescending(x => x.CreadoEn).FirstOrDefaultAsync(ct);

    public Task<ProcesamientoDocumental?> ObtenerProcesamientoPorHash(
        Guid documentoId, string hash, CancellationToken ct) =>
        db.ProcesamientosDocumentales.AsNoTracking().SingleOrDefaultAsync(
            x => x.DocumentoId == documentoId && x.HashIdempotencia == hash, ct);

    public async Task<IReadOnlyCollection<Exportacion>> ListarExportaciones(
        Guid usuarioId, CancellationToken ct) =>
        await db.Exportaciones.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId && x.EliminadoEn == null)
            .OrderByDescending(x => x.CreadoEn).ThenByDescending(x => x.Id).ToListAsync(ct);

    public Task<Exportacion?> ObtenerExportacion(
        Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct)
    {
        IQueryable<Exportacion> query = db.Exportaciones;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.Id == id && x.EliminadoEn == null, ct);
    }

    public Task<Exportacion?> ObtenerExportacionPorHash(
        Guid usuarioId, string hash, CancellationToken ct) =>
        db.Exportaciones.AsNoTracking().SingleOrDefaultAsync(
            x => x.UsuarioId == usuarioId && x.HashSolicitud == hash && x.EliminadoEn == null, ct);

    public void Agregar(DocumentoFinanciero entity) => db.DocumentosFinancieros.Add(entity);

    public void Agregar(ProcesamientoDocumental entity) => db.ProcesamientosDocumentales.Add(entity);

    public void Agregar(Exportacion entity) => db.Exportaciones.Add(entity);
}
