namespace FinanzasInteligentes.Api.Contratos.Seguridad;

public sealed record PostDispositivosRequest(
    string IdentificadorInstalacion,
    string Nombre,
    string Plataforma,
    string VersionSistema,
    string VersionAplicacion,
    string TokenPush,
    string ZonaHoraria);

public sealed record DispositivoPatchRequest(
    string? Nombre,
    string? VersionAplicacion,
    string? TokenPush,
    string? ZonaHoraria);