namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record ActualizarCuentaRequest(
    string? Nombre = null,
    string? Tipo = null,
    string? Moneda = null,
    long? SaldoInicial = null,
    string? Color = null,
    string? Icono = null,
    bool? IncluidaEnTotal = null);