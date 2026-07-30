namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record ActualizarCategoriaRequest(
    string? Nombre = null,
    string? Tipo = null,
    string? Icono = null,
    string? Color = null);