using FinanzasInteligentes.Dominio.Documentos;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IDocumentosRepository
{
    Task<IReadOnlyCollection<DocumentoFinanciero>> ListarDocumentos(Guid usuarioId, CancellationToken ct);

    Task<DocumentoFinanciero?> ObtenerDocumento(Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct);

    Task<DocumentoFinanciero?> ObtenerDocumentoPorIdempotencia(Guid usuarioId, string hash, CancellationToken ct);
    Task<int> ContarDocumentos(Guid usuarioId, CancellationToken ct);
    Task<bool> ExisteDocumentoConHash(Guid usuarioId, string sha256, CancellationToken ct);

    Task<ProcesamientoDocumental?> ObtenerProcesamiento(Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct);

    Task<ProcesamientoDocumental?> ObtenerUltimoProcesamiento(Guid documentoId, CancellationToken ct);

    Task<ProcesamientoDocumental?> ObtenerProcesamientoPorHash(Guid documentoId, string hash, CancellationToken ct);

    Task<IReadOnlyCollection<Exportacion>> ListarExportaciones(Guid usuarioId, CancellationToken ct);

    Task<Exportacion?> ObtenerExportacion(Guid usuarioId, Guid id, bool soloLectura, CancellationToken ct);

    Task<Exportacion?> ObtenerExportacionPorHash(Guid usuarioId, string hash, CancellationToken ct);

    void Agregar(DocumentoFinanciero entity);

    void Agregar(ProcesamientoDocumental entity);

    void Agregar(Exportacion entity);
}

public interface IValidadorDocumento
{
    long TamanoMaximoBytes { get; }
    int CantidadMaximaPorUsuario { get; }
    void Validar(string tipo, string mime, string nombre, ReadOnlyMemory<byte> contenido);
}
