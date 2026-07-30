namespace FinanzasInteligentes.Api.Contratos.FinanzasPersonales;

public sealed record CrearCuentaRequest(
    string Nombre,
    string Tipo,
    long SaldoInicial = 0,
    string Moneda = "PYG",
    string? Color = null,
    string? Icono = null,
    bool IncluidaEnTotal = true);