using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.Analitica;

public sealed class AlertaFinanciera : MutableEntity
{
    private AlertaFinanciera()
    { }

    public Guid UsuarioId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Nivel { get; private set; } = string.Empty;
    public string Titulo { get; private set; } = string.Empty;
    public string Mensaje { get; private set; } = string.Empty;
    public string QueOcurrio { get; private set; } = string.Empty;
    public string DatosUtilizados { get; private set; } = string.Empty;
    public string Impacto { get; private set; } = string.Empty;
    public string Recomendacion { get; private set; } = string.Empty;
    public string ClaveDeduplicacion { get; private set; } = string.Empty;
    public bool Leida { get; private set; }
    public DateTimeOffset? LeidaEn { get; private set; }
    public bool Archivada { get; private set; }
    public DateTimeOffset? ArchivadaEn { get; private set; }

    public static AlertaFinanciera Crear(
        Guid usuarioId, string tipo, string nivel, string titulo, string mensaje,
        string queOcurrio, string datosUtilizados, string impacto,
        string recomendacion, string claveDeduplicacion)
    {
        if (nivel is not ("informativa" or "advertencia" or "critica"))
            throw new DomainException("nivel_invalido", "El nivel de alerta no es válido.");
        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(mensaje))
            throw new DomainException("alerta_invalida", "El título y el mensaje son requeridos.");
        return new()
        {
            UsuarioId = usuarioId,
            Tipo = tipo.Trim(),
            Nivel = nivel,
            Titulo = titulo.Trim(),
            Mensaje = mensaje.Trim(),
            QueOcurrio = queOcurrio.Trim(),
            DatosUtilizados = datosUtilizados.Trim(),
            Impacto = impacto.Trim(),
            Recomendacion = recomendacion.Trim(),
            ClaveDeduplicacion = claveDeduplicacion.Trim()
        };
    }

    public void Actualizar(bool? leida, bool? archivada)
    {
        if (leida is null && archivada is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");
        if (leida is not null && Leida != leida.Value)
        {
            Leida = leida.Value;
            LeidaEn = leida.Value ? DateTimeOffset.UtcNow : null;
        }
        if (archivada is not null && Archivada != archivada.Value)
        {
            Archivada = archivada.Value;
            ArchivadaEn = archivada.Value ? DateTimeOffset.UtcNow : null;
        }
        Touch();
    }
}