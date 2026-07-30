namespace FinanzasInteligentes.Infraestructura.Seguridad;

public sealed class SeguridadOptions
{
    public const string SectionName = "Seguridad";

    public string TokenPushKey { get; init; } = string.Empty;
}
