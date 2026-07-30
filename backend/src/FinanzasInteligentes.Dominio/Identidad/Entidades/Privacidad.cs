using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.Identidad;

public sealed class PoliticaPrivacidad : Entity
{
    private PoliticaPrivacidad() { }

    public string VersionPolitica { get; private set; } = string.Empty;
    public string Titulo { get; private set; } = string.Empty;
    public string UrlDocumento { get; private set; } = string.Empty;
    public DateTimeOffset VigenteDesde { get; private set; }
    public bool Activa { get; private set; }
}

public sealed class ConsentimientoPrivacidad : Entity
{
    private ConsentimientoPrivacidad() { }

    public Guid UsuarioId { get; private set; }
    public Guid PoliticaId { get; private set; }
    public string VersionPolitica { get; private set; } = string.Empty;
    public string Finalidad { get; private set; } = string.Empty;
    public DateTimeOffset AceptadoEn { get; private set; }
    public DateTimeOffset? RevocadoEn { get; private set; }

    public static ConsentimientoPrivacidad Crear(
        Guid usuarioId, PoliticaPrivacidad politica, string finalidad)
    {
        if (!politica.Activa)
            throw new DomainException("politica_no_vigente", "La política indicada no está vigente.");
        if (string.IsNullOrWhiteSpace(finalidad))
            throw new DomainException("finalidad_invalida", "Debe indicar la finalidad del consentimiento.");
        return new()
        {
            UsuarioId = usuarioId,
            PoliticaId = politica.Id,
            VersionPolitica = politica.VersionPolitica,
            Finalidad = finalidad.Trim(),
            AceptadoEn = DateTimeOffset.UtcNow
        };
    }

    public void Revocar()
    {
        if (RevocadoEn is not null) return;
        RevocadoEn = DateTimeOffset.UtcNow;
    }
}
