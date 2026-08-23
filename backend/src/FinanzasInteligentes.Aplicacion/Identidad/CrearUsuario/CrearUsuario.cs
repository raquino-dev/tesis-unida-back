using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Identidad.Mapeos;
using FinanzasInteligentes.Aplicacion.Identidad.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.Identidad;

namespace FinanzasInteligentes.Aplicacion.Identidad.CrearUsuario;

public sealed record CrearUsuarioCommand(
    string Correo,
    string Nombre,
    string Contrasena,
    string? Alias = null,
    string Moneda = "PYG",
    string? Idioma = "es",
    string? ZonaHoraria = "America/Asuncion",
    bool AceptaTerminos = false,
    string VersionPolitica = "1.0");

public sealed class CrearUsuarioHandler(
    IIdentidadRepository identidad,
    IPasswordService passwords,
    IPoliticaContrasena politicaContrasena,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<UsuarioResponse> Handle(CrearUsuarioCommand command, CancellationToken cancellationToken)
    {
        if (!command.AceptaTerminos)
            throw new DomainException("terminos_no_aceptados", "Debe aceptar los términos.");
        var politica = await identidad.ObtenerPoliticaPorVersion(
            command.VersionPolitica, cancellationToken)
            ?? throw new DomainException(
                "politica_no_vigente", "La versión de la política no está vigente.");

        if (command.Moneda != "PYG")
            throw new DomainException("moneda_no_soportada", "La primera versión sólo admite PYG.");

        politicaContrasena.Validar(command.Contrasena, command.Correo, command.Nombre);

        var correo = command.Correo.Trim().ToLowerInvariant();
        if (await identidad.ExisteCorreo(correo, cancellationToken))
            throw new ConflictException("correo_duplicado", "El correo ya está registrado.");

        var alias = command.Alias is null ? null : Usuario.NormalizarAlias(command.Alias);
        if (alias is not null && await identidad.ExisteAlias(alias, null, cancellationToken))
            throw new ConflictException("alias_duplicado", "El alias ya está en uso.");

        var usuario = Usuario.Crear(
            correo,
            command.Nombre,
            passwords.Hash(command.Contrasena),
            command.Idioma ?? "es",
            command.ZonaHoraria ?? "America/Asuncion",
            alias);

        identidad.Agregar(usuario);
        identidad.Agregar(ConsentimientoPrivacidad.Crear(
            usuario.Id, politica, "tratamiento-datos-servicio"));
        identidad.Agregar(ConsentimientoPrivacidad.Crear(
            usuario.Id, politica, "aceptacion-terminos-servicio"));

        await unidadDeTrabajo.GuardarCambios(cancellationToken);

        return usuario.ToResponse();
    }
}
