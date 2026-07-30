using System.Text.Json;
using FinanzasInteligentes.Aplicacion.Documentos;
using Microsoft.AspNetCore.Http;

namespace FinanzasInteligentes.Api.Contratos.Documentos;

public sealed class DocumentoFinancieroForm
{
    public required IFormFile Archivo { get; init; }
    public required string Tipo { get; init; }
    public required string Ambito { get; init; }
    public Guid? GrupoFamiliarId { get; init; }
}

public sealed record PostDocumentosFinancierosByDocumentoIdProcesamientosDocumentalesRequest(string Tipo);
public sealed record CorreccionProcesamientoDocumentalRequest(JsonDocument DatosDetectados);
public sealed record ExportacionRequest(
    string Formato, string Ambito, Guid? GrupoFamiliarId,
    DateOnly Desde, DateOnly Hasta, FiltrosExportacionRequest Filtros);
