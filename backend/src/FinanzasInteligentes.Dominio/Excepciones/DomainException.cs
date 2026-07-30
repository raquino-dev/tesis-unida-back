namespace FinanzasInteligentes.Dominio.Excepciones;

public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}