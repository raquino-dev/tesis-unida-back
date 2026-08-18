namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record CrearCategoriaRequest(
    string Nombre,
    string Tipo,
    string? Icono = null,
    string? Color = null,
    Guid? Id = null);
