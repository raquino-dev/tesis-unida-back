namespace FinanzasInteligentes.Api.Configuracion;

public sealed class DataProtectionStorageOptions
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; init; } = "FinanzasInteligentes.Api";
    public string KeysPath { get; init; } = "data-protection-keys";
    public string? CertificatePath { get; init; }
    public string? CertificatePassword { get; init; }
}
