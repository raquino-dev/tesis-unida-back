namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record PatchMovimientosByMovimientoIdRequest(
    string? Descripcion = null,
    IReadOnlyCollection<Guid>? CategoriaIds = null,
    Guid? DocumentoId = null);
