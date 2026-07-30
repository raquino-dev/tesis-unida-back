using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;
using System.Text.Json;

namespace FinanzasInteligentes.Dominio.Seguridad;

public sealed class Dispositivo : MutableEntity
{
    private Dispositivo()
    { }

    public Guid UsuarioId { get; private set; }
    public string IdentificadorInstalacion { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string Plataforma { get; private set; } = string.Empty;
    public string VersionSistema { get; private set; } = string.Empty;
    public string VersionAplicacion { get; private set; } = string.Empty;
    public string TokenPushProtegido { get; private set; } = string.Empty;
    public string ZonaHoraria { get; private set; } = string.Empty;
    public bool Confiable { get; private set; } = true;
    public DateTimeOffset UltimoAccesoEn { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevocadoEn { get; private set; }

    public static Dispositivo Crear(
        Guid usuarioId, string identificador, string nombre, string plataforma,
        string versionSistema, string versionAplicacion, string tokenProtegido,
        string zonaHoraria)
    {
        if (string.IsNullOrWhiteSpace(identificador) || identificador.Length > 200)
            throw new DomainException("identificador_invalido", "El identificador de instalación no es válido.");
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 120)
            throw new DomainException("nombre_invalido", "El nombre del dispositivo no es válido.");
        if (plataforma is not ("android" or "ios"))
            throw new DomainException("plataforma_invalida", "La plataforma debe ser android o ios.");
        if (string.IsNullOrWhiteSpace(tokenProtegido))
            throw new DomainException("token_push_invalido", "El token push es requerido.");
        return new()
        {
            UsuarioId = usuarioId,
            IdentificadorInstalacion = identificador.Trim(),
            Nombre = nombre.Trim(),
            Plataforma = plataforma,
            VersionSistema = versionSistema.Trim(),
            VersionAplicacion = versionAplicacion.Trim(),
            TokenPushProtegido = tokenProtegido,
            ZonaHoraria = zonaHoraria.Trim()
        };
    }

    public void Actualizar(
        string? nombre, string? versionAplicacion, string? tokenProtegido, string? zonaHoraria)
    {
        if (nombre is null && versionAplicacion is null && tokenProtegido is null && zonaHoraria is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");
        if (nombre is not null)
        {
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 120)
                throw new DomainException("nombre_invalido", "El nombre no es válido.");
            Nombre = nombre.Trim();
        }
        if (versionAplicacion is not null) VersionAplicacion = versionAplicacion.Trim();
        if (tokenProtegido is not null) TokenPushProtegido = tokenProtegido;
        if (zonaHoraria is not null) ZonaHoraria = zonaHoraria.Trim();
        UltimoAccesoEn = DateTimeOffset.UtcNow;
        Touch();
    }

    public void Revocar()
    {
        RevocadoEn ??= DateTimeOffset.UtcNow;
        TokenPushProtegido = string.Empty;
        Confiable = false;
        Touch();
    }
}

public sealed class EventoSeguridad : Entity
{
    private EventoSeguridad()
    { }

    public Guid UsuarioId { get; private set; }
    public Guid? DispositivoId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
    public bool Exitoso { get; private set; }
    public string OrigenAproximado { get; private set; } = "No disponible";
    public string Dispositivo { get; private set; } = "Desconocido";
    public DateTimeOffset OcurridoEn { get; private set; } = DateTimeOffset.UtcNow;

    public static EventoSeguridad Crear(
        Guid usuarioId, string tipo, string descripcion, bool exitoso,
        Guid? dispositivoId = null, string? dispositivo = null, string? origen = null) =>
        new()
        {
            UsuarioId = usuarioId,
            Tipo = tipo,
            Descripcion = descripcion,
            Exitoso = exitoso,
            DispositivoId = dispositivoId,
            Dispositivo = dispositivo ?? "Desconocido",
            OrigenAproximado = origen ?? "No disponible"
        };
}

public sealed class EventoAuditoria : Entity
{
    private EventoAuditoria()
    { }

    public Guid? UsuarioId { get; private set; }
    public Guid? GrupoFamiliarId { get; private set; }
    public string Accion { get; private set; } = string.Empty;
    public string Recurso { get; private set; } = string.Empty;
    public Guid RecursoId { get; private set; }
    public JsonDocument? DatosAnteriores { get; private set; }
    public JsonDocument DatosPosteriores { get; private set; } = JsonDocument.Parse("{}");
    public string CorrelationId { get; private set; } = string.Empty;
    public DateTimeOffset OcurridoEn { get; private set; } = DateTimeOffset.UtcNow;

    public static EventoAuditoria Crear(
        Guid? usuarioId, Guid? grupoId, string accion, string recurso, Guid recursoId,
        object? anteriores, object posteriores, string correlationId) =>
        new()
        {
            UsuarioId = usuarioId,
            GrupoFamiliarId = grupoId,
            Accion = accion,
            Recurso = recurso,
            RecursoId = recursoId,
            DatosAnteriores = anteriores is null ? null : JsonSerializer.SerializeToDocument(anteriores),
            DatosPosteriores = JsonSerializer.SerializeToDocument(posteriores),
            CorrelationId = correlationId
        };
}