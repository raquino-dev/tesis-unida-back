using FinanzasInteligentes.Api.Contratos.Documentos;
using FinanzasInteligentes.Api.Extensiones;
using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Documentos;
using FinanzasInteligentes.Dominio.Excepciones;
using Microsoft.AspNetCore.Mvc;

namespace FinanzasInteligentes.Api.Endpoints.Documentos;

public static class DocumentosEndpoints
{
    private const string Tag = "Documentos, OCR y XML SIFEN";

    public static IEndpointRouteBuilder MapDocumentos(this IEndpointRouteBuilder e)
    {
        e.MapGet("/documentos-financieros", Listar).WithName("getDocumentosFinancieros").WithTags(Tag)
            .Produces<PaginaDocumentoFinancieroResponse>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(500);

        e.MapPost("/documentos-financieros", Crear).WithName("postDocumentosFinancieros").WithTags(Tag)
            .DisableAntiforgery().Accepts<DocumentoFinancieroForm>("multipart/form-data")
            .Produces<DocumentoFinancieroResponse>(202).ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(403).ProducesProblem(409).ProducesProblem(413)
            .ProducesProblem(415).ProducesProblem(422).ProducesProblem(503).ProducesProblem(500);

        e.MapGet("/documentos-financieros/{documentoId:guid}", Obtener)
            .WithName("getDocumentosFinancierosByDocumentoId").WithTags(Tag)
            .Produces<DocumentoFinancieroResponse>().ProducesProblem(401).ProducesProblem(404).ProducesProblem(500);

        e.MapDelete("/documentos-financieros/{documentoId:guid}", Eliminar)
            .WithName("deleteDocumentosFinancierosByDocumentoId").WithTags(Tag)
            .Produces(204).ProducesProblem(401).ProducesProblem(404).ProducesProblem(409).ProducesProblem(412);

        e.MapPost("/documentos-financieros/{documentoId:guid}/descargas", Descargar)
            .WithName("postDocumentosFinancierosByDocumentoIdDescargas").WithTags(Tag)
            .Produces<DescargaResponse>(201).ProducesProblem(401).ProducesProblem(404)
            .ProducesProblem(409).ProducesProblem(429).ProducesProblem(503);

        e.MapPost("/documentos-financieros/{documentoId:guid}/procesamientos-documentales", CrearProceso)
            .WithName("postDocumentosFinancierosByDocumentoIdProcesamientosDocumentales").WithTags(Tag)
            .Produces<ProcesamientoDocumentalResponse>(202).ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(403).ProducesProblem(404).ProducesProblem(409).ProducesProblem(422);

        e.MapGet("/procesamientos-documentales/{procesamientoId:guid}", ObtenerProceso)
            .WithName("getProcesamientosDocumentalesByProcesamientoId").WithTags(Tag)
            .Produces<ProcesamientoDocumentalResponse>().ProducesProblem(401).ProducesProblem(404);

        e.MapPatch("/procesamientos-documentales/{procesamientoId:guid}", Corregir)
            .WithName("patchProcesamientosDocumentalesByProcesamientoId").WithTags(Tag)
            .Produces<ProcesamientoDocumentalResponse>().ProducesProblem(400).ProducesProblem(401)
            .ProducesProblem(404).ProducesProblem(409).ProducesProblem(412).ProducesProblem(422);

        e.MapGet("/descargas/{**clave}", ObtenerArchivo).AllowAnonymous().ExcludeFromDescription();

        return e;
    }

    private static async Task<IResult> Listar(
        string? cursor, long? limite, string? tipo, string? estado,
        DateOnly? desde, DateOnly? hasta, DocumentosHandler h, HttpContext c, CancellationToken ct) =>
        Results.Ok(await h.Listar(c.UsuarioId(), cursor, limite ?? 20, tipo, estado, desde, hasta, ct));

    [Consumes("multipart/form-data")]
    private static async Task<IResult> Crear(
        [FromQuery(Name = "tipo")] string? tipoFiltro, [FromQuery] string? estado,
        [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        [FromHeader(Name = "X-Content-SHA256")] string sha,
        DocumentosHandler h, HttpContext c, CancellationToken ct)
    {
        var form = await c.Request.ReadFormAsync(ct);
        var archivo = form.Files.GetFile("archivo")
            ?? throw new DomainException("archivo_requerido", "La parte archivo es requerida.");
        var tipoCarga = form["tipo"].ToString();
        var ambito = form["ambito"].ToString();
        Guid? grupoFamiliarId = Guid.TryParse(
            form["grupoFamiliarId"].ToString(), out var grupo) ? grupo : null;
        await using var stream = archivo.OpenReadStream();
        var response = await h.Crear(
            c.UsuarioId(), stream, archivo.FileName, archivo.ContentType,
            archivo.Length, tipoCarga, ambito, grupoFamiliarId,
            sha, idempotencyKey, c.TraceIdentifier, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        c.Response.Headers.Location = $"/api/v1/documentos-financieros/{response.Id}";
        return Results.Accepted(c.Response.Headers.Location, response);
    }

    private static async Task<IResult> Obtener(
        Guid documentoId, DocumentosHandler h, HttpContext c, CancellationToken ct)
    {
        var response = await h.Obtener(c.UsuarioId(), documentoId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Eliminar(
        Guid documentoId, [FromHeader(Name = "If-Match")] string ifMatch,
        DocumentosHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var version)) return ETagExtensions.IfMatchInvalido(c);
        await h.Eliminar(c.UsuarioId(), documentoId, version, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Descargar(
        Guid documentoId, DocumentosHandler h, HttpContext c, CancellationToken ct)
    {
        var response = await h.DescargarDocumento(c.UsuarioId(), documentoId, ct);
        c.Response.Headers.Location = response.Url;
        return Results.Created(response.Url, response);
    }

    private static async Task<IResult> CrearProceso(
        Guid documentoId,
        PostDocumentosFinancierosByDocumentoIdProcesamientosDocumentalesRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        DocumentosHandler h, HttpContext c, CancellationToken ct)
    {
        var response = await h.CrearProceso(
            c.UsuarioId(), documentoId, request.Tipo, idempotencyKey, c.TraceIdentifier, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        c.Response.Headers.Location = $"/api/v1/procesamientos-documentales/{response.Id}";
        return Results.Accepted(c.Response.Headers.Location, response);
    }

    private static async Task<IResult> ObtenerProceso(
        Guid procesamientoId, DocumentosHandler h, HttpContext c, CancellationToken ct)
    {
        var response = await h.ObtenerProceso(c.UsuarioId(), procesamientoId, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> Corregir(
        Guid procesamientoId, CorreccionProcesamientoDocumentalRequest request,
        [FromHeader(Name = "If-Match")] string ifMatch,
        DocumentosHandler h, HttpContext c, CancellationToken ct)
    {
        if (!c.Request.TryObtenerVersionIfMatch(out var version)) return ETagExtensions.IfMatchInvalido(c);
        var response = await h.Corregir(c.UsuarioId(), procesamientoId, version, request.DatosDetectados, ct);
        c.Response.Headers.ETag = ETagExtensions.Formatear(response.Version);
        return Results.Ok(response);
    }

    private static async Task<IResult> ObtenerArchivo(
        string clave, long expira, string firma, IArchivoStorage storage, CancellationToken ct)
    {
        if (!storage.ValidarUrl(clave, expira, firma)) return Results.Unauthorized();
        try { return Results.Stream(await storage.Abrir(clave, ct), "application/octet-stream"); }
        catch (FileNotFoundException) { return Results.NotFound(); }
    }
}