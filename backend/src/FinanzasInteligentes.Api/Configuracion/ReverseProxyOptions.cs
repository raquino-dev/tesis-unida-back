namespace FinanzasInteligentes.Api.Configuracion;

public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    public bool Enabled { get; init; }
    public string[] KnownProxies { get; init; } = [];
}
