using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.Identidad;

public sealed class Usuario : MutableEntity
{
    private Usuario(
        string correo,
        string nombre,
        string? alias,
        string hashContrasena,
        string idioma,
        string zonaHoraria)
    {
        Correo = NormalizarCorreo(correo);
        Nombre = ValidarTexto(nombre, nameof(nombre), 120);
        Alias = NormalizarAlias(alias ?? $"usuario_{Id:N}"[..20]);
        HashContrasena = hashContrasena;
        Idioma = ValidarTexto(idioma, nameof(idioma), 10);
        ZonaHoraria = ValidarTexto(zonaHoraria, nameof(zonaHoraria), 80);
        Preferencias = PreferenciasUsuario.Crear(Id);
    }

    public string Correo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string Alias { get; private set; } = string.Empty;
    public string HashContrasena { get; private set; } = string.Empty;
    public string Moneda { get; private set; } = "PYG";
    public string Idioma { get; private set; } = "es";
    public string Ubicacion { get; private set; } = string.Empty;
    public string ZonaHoraria { get; private set; } = "America/Asuncion";
    public string Estado { get; private set; } = "activo";
    public string Rol { get; private set; } = "usuario";
    public DateTimeOffset? AnonimizadoEn { get; private set; }
    public PreferenciasUsuario Preferencias { get; private set; } = null!;

    public static Usuario Crear(
        string correo,
        string nombre,
        string hashContrasena,
        string idioma = "es",
        string zonaHoraria = "America/Asuncion",
        string? alias = null) =>
        new(correo, nombre, alias, hashContrasena, idioma, zonaHoraria);

    public void ActualizarPerfil(
        string? nombre,
        string? idioma,
        string? ubicacion,
        string? zonaHoraria,
        string? alias = null)
    {
        if (nombre is null && alias is null && idioma is null && ubicacion is null && zonaHoraria is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo para actualizar.");
        if (nombre is not null) Nombre = ValidarTexto(nombre, nameof(nombre), 120);
        if (alias is not null) Alias = NormalizarAlias(alias);
        if (idioma is not null) Idioma = ValidarTexto(idioma, nameof(idioma), 10);
        if (ubicacion is not null) Ubicacion = ValidarTexto(ubicacion, nameof(ubicacion), 160);
        if (zonaHoraria is not null) ZonaHoraria = ValidarTexto(zonaHoraria, nameof(zonaHoraria), 80);
        Touch();
    }

    public void CambiarContrasena(string hashContrasena)
    {
        if (string.IsNullOrWhiteSpace(hashContrasena))
            throw new DomainException("contrasena_invalida", "La contraseña no es válida.");
        HashContrasena = hashContrasena;
        Touch();
    }

    public void CambiarEstado(string estado)
    {
        if (AnonimizadoEn is not null)
            throw new DomainException("usuario_eliminado", "Un usuario eliminado no puede cambiar de estado.");
        if (estado is not ("activo" or "inactivo"))
            throw new DomainException("estado_usuario_invalido", "El estado debe ser activo o inactivo.");
        if (Estado == estado) return;
        Estado = estado;
        Touch();
    }

    public void CambiarRol(string rol)
    {
        if (AnonimizadoEn is not null)
            throw new DomainException("usuario_eliminado", "Un usuario eliminado no puede cambiar de rol.");
        if (rol is not ("usuario" or "administrador"))
            throw new DomainException("rol_usuario_invalido", "El rol debe ser usuario o administrador.");
        if (Rol == rol) return;
        Rol = rol;
        Touch();
    }

    public void Anonimizar()
    {
        if (AnonimizadoEn is not null) return;
        Correo = $"anon-{Id:N}@invalid.local";
        Nombre = "Usuario eliminado";
        Alias = $"eliminado_{Id:N}";
        HashContrasena = Guid.NewGuid().ToString("N");
        Ubicacion = string.Empty;
        Estado = "eliminado";
        Rol = "usuario";
        AnonimizadoEn = DateTimeOffset.UtcNow;
        Touch();
    }

    private static string NormalizarCorreo(string correo)
    {
        var valor = correo.Trim().ToLowerInvariant();
        if (valor.Length is < 3 or > 320 || !valor.Contains('@'))
            throw new DomainException("correo_invalido", "El correo no es válido.");
        return valor;
    }

    public static string NormalizarAlias(string alias)
    {
        var valor = alias.Trim();
        var limpio = valor.StartsWith('@') ? valor[1..] : valor;
        if (limpio.Length is < 3 or > 24 ||
            !char.IsLetter(limpio[0]) ||
            limpio.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            throw new DomainException(
                "alias_invalido",
                "El alias debe tener entre 3 y 24 caracteres, comenzar con una letra y usar solo letras, números o guion bajo.");
        return limpio;
    }

    private static string ValidarTexto(string valor, string campo, int maximo)
    {
        var limpio = valor.Trim();
        if (limpio.Length is 0 || limpio.Length > maximo)
            throw new DomainException("campo_invalido", $"{campo} no es válido.");
        return limpio;
    }
}

public sealed class PreferenciasUsuario
{
    private PreferenciasUsuario()
    { }

    public Guid UsuarioId { get; private set; }
    public string Tema { get; private set; } = "sistema";
    public bool NotificacionesPush { get; private set; } = true;
    public bool NotificacionesCorreo { get; private set; } = true;
    public bool ResumenSemanal { get; private set; }
    public DateTimeOffset CreadoEn { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ActualizadoEn { get; private set; } = DateTimeOffset.UtcNow;
    public long Version { get; private set; } = 1;

    internal static PreferenciasUsuario Crear(Guid usuarioId) => new() { UsuarioId = usuarioId };

    public void Actualizar(string tema, bool notificacionesPush, bool resumenSemanal)
    {
        if (tema is not ("claro" or "oscuro" or "sistema"))
            throw new DomainException("tema_invalido", "El tema no es válido.");
        Tema = tema;
        NotificacionesPush = notificacionesPush;
        ResumenSemanal = resumenSemanal;
        ActualizadoEn = DateTimeOffset.UtcNow;
        Version = checked(Version + 1);
    }
}

public sealed class Sesion : Entity
{
    private Sesion()
    { }

    public Guid UsuarioId { get; private set; }
    public string HashRefreshToken { get; private set; } = string.Empty;
    public Guid FamiliaToken { get; private set; }
    public DateTimeOffset ExpiraEn { get; private set; }
    public DateTimeOffset? UsadoEn { get; private set; }
    public DateTimeOffset? RevocadoEn { get; private set; }
    public string IdentificadorDispositivo { get; private set; } = string.Empty;
    public string NombreDispositivo { get; private set; } = string.Empty;
    public string PlataformaDispositivo { get; private set; } = string.Empty;

    public static Sesion Crear(
        Guid sesionId,
        Guid usuarioId,
        string hashRefreshToken,
        DateTimeOffset expiraEn,
        string identificadorDispositivo,
        string nombreDispositivo,
        string plataformaDispositivo,
        Guid? familiaToken = null) =>
        new()
        {
            Id = sesionId,
            UsuarioId = usuarioId,
            HashRefreshToken = hashRefreshToken,
            FamiliaToken = familiaToken ?? Guid.CreateVersion7(),
            ExpiraEn = expiraEn,
            IdentificadorDispositivo = identificadorDispositivo.Trim(),
            NombreDispositivo = nombreDispositivo.Trim(),
            PlataformaDispositivo = plataformaDispositivo.Trim()
        };

    public void MarcarUsada()
    {
        if (UsadoEn is not null || RevocadoEn is not null || ExpiraEn <= DateTimeOffset.UtcNow)
            throw new DomainException("refresh_token_invalido", "El refresh token no es válido.");
        UsadoEn = DateTimeOffset.UtcNow;
    }

    public void Revocar()
    {
        RevocadoEn ??= DateTimeOffset.UtcNow;
    }
}
