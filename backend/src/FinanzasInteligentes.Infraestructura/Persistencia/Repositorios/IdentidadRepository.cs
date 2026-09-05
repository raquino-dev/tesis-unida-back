using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Persistencia.Repositorios;

public sealed class IdentidadRepository(FinanzasDbContext db) : IIdentidadRepository
{
    public Task<bool> ExisteCorreo(string correo, CancellationToken cancellationToken) =>
        db.Usuarios.AnyAsync(x => x.Correo == correo && x.AnonimizadoEn == null, cancellationToken);

    public Task<bool> ExisteAlias(
        string alias,
        Guid? excluirUsuarioId,
        CancellationToken cancellationToken) =>
        db.Usuarios.AnyAsync(
            x => x.Alias == alias &&
                x.AnonimizadoEn == null &&
                (!excluirUsuarioId.HasValue || x.Id != excluirUsuarioId.Value),
            cancellationToken);

    public Task<Usuario?> BuscarUsuarioPorCorreo(string correo, CancellationToken cancellationToken) =>
        db.Usuarios.SingleOrDefaultAsync(
            x => x.Correo == correo && x.AnonimizadoEn == null,
            cancellationToken);

    public Task<Usuario?> BuscarUsuarioPorAlias(string alias, CancellationToken cancellationToken) =>
        db.Usuarios.AsNoTracking().SingleOrDefaultAsync(
            x => x.Alias == alias && x.AnonimizadoEn == null,
            cancellationToken);

    public Task<Usuario?> ObtenerUsuario(Guid usuarioId, bool soloLectura, CancellationToken cancellationToken)
    {
        IQueryable<Usuario> query = db.Usuarios.Include(x => x.Preferencias);
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == usuarioId, cancellationToken);
    }

    public Task<bool> UsuarioEstaActivo(Guid usuarioId, CancellationToken cancellationToken) =>
        db.Usuarios.AsNoTracking().AnyAsync(
            x => x.Id == usuarioId && x.Estado == "activo" && x.AnonimizadoEn == null,
            cancellationToken);

    public Task<bool> SesionEstaActiva(
        Guid usuarioId, Guid sesionId, CancellationToken cancellationToken) =>
        db.Sesiones.AsNoTracking().AnyAsync(
            x => x.Id == sesionId &&
                x.UsuarioId == usuarioId &&
                x.UsadoEn == null &&
                x.RevocadoEn == null &&
                x.ExpiraEn > DateTimeOffset.UtcNow,
            cancellationToken);

    public async Task<IReadOnlyCollection<Usuario>> ListarUsuarios(
        string? estado,
        string? busqueda,
        int limite,
        CancellationToken cancellationToken)
    {
        var query = db.Usuarios.AsNoTracking().Where(x => x.AnonimizadoEn == null);
        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(x => x.Estado == estado);
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();
            query = query.Where(x =>
                x.Correo.Contains(termino) ||
                x.Nombre.Contains(termino) ||
                x.Alias.Contains(termino.TrimStart('@')));
        }

        return await query.OrderBy(x => x.Correo)
            .Take(Math.Clamp(limite, 1, 100))
            .ToListAsync(cancellationToken);
    }

    public Task<PoliticaPrivacidad?> ObtenerPoliticaVigente(CancellationToken cancellationToken) =>
        db.PoliticasPrivacidad.AsNoTracking()
            .Where(x => x.Activa && x.VigenteDesde <= DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.VigenteDesde)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PoliticaPrivacidad?> ObtenerPoliticaPorVersion(
        string version, CancellationToken cancellationToken) =>
        db.PoliticasPrivacidad.AsNoTracking().SingleOrDefaultAsync(
            x => x.VersionPolitica == version && x.Activa, cancellationToken);

    public async Task<IReadOnlyCollection<ConsentimientoPrivacidad>> ListarConsentimientos(
        Guid usuarioId, CancellationToken cancellationToken) =>
        await db.ConsentimientosPrivacidad.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.AceptadoEn)
            .ToListAsync(cancellationToken);

    public Task<ConsentimientoPrivacidad?> ObtenerConsentimiento(
        Guid usuarioId, Guid consentimientoId, CancellationToken cancellationToken) =>
        db.ConsentimientosPrivacidad.SingleOrDefaultAsync(
            x => x.Id == consentimientoId && x.UsuarioId == usuarioId, cancellationToken);

    public async Task<IReadOnlyCollection<Sesion>> ListarSesiones(
        Guid usuarioId,
        CancellationToken cancellationToken) =>
        await db.Sesiones.AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId &&
                x.UsadoEn == null &&
                x.RevocadoEn == null &&
                x.ExpiraEn > DateTimeOffset.UtcNow)
            .OrderByDescending(x => x.CreadoEn)
            .ToListAsync(cancellationToken);

    public Task<Sesion?> ObtenerSesion(
        Guid usuarioId,
        Guid sesionId,
        CancellationToken cancellationToken) =>
        db.Sesiones.SingleOrDefaultAsync(
            x => x.Id == sesionId && x.UsuarioId == usuarioId,
            cancellationToken);

    public async Task<Sesion?> ConsumirSesionPorRefreshToken(
        string hashRefreshToken,
        string identificadorDispositivo,
        CancellationToken cancellationToken)
    {
        var ahora = DateTimeOffset.UtcNow;
        var conocida = await db.Sesiones.AsNoTracking().SingleOrDefaultAsync(
            x => x.HashRefreshToken == hashRefreshToken, cancellationToken);
        if (conocida is null) return null;

        if (conocida.IdentificadorDispositivo != identificadorDispositivo ||
            conocida.UsadoEn is not null ||
            conocida.RevocadoEn is not null)
        {
            await RevocarFamiliaSesiones(
                conocida.UsuarioId, conocida.FamiliaToken, cancellationToken);
            return null;
        }

        var actualizadas = await db.Sesiones
            .Where(x =>
                x.HashRefreshToken == hashRefreshToken &&
                x.IdentificadorDispositivo == identificadorDispositivo &&
                x.UsadoEn == null &&
                x.RevocadoEn == null &&
                x.ExpiraEn > ahora)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.UsadoEn, ahora),
                cancellationToken);

        if (actualizadas == 1)
            return await db.Sesiones.AsNoTracking().SingleAsync(
                x => x.HashRefreshToken == hashRefreshToken, cancellationToken);

        await RevocarFamiliaSesiones(
            conocida.UsuarioId, conocida.FamiliaToken, cancellationToken);
        return null;
    }

    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);

    public void Agregar(Sesion sesion) => db.Sesiones.Add(sesion);

    public Task<DesafioOtp?> ObtenerDesafioOtp(
        Guid usuarioId, Guid desafioId, CancellationToken cancellationToken) =>
        db.DesafiosOtp.SingleOrDefaultAsync(
            x => x.Id == desafioId && x.UsuarioId == usuarioId, cancellationToken);

    public async Task<bool> ConsumirVerificacionOtp(
        Guid usuarioId, Guid verificacionId, string motivo, CancellationToken cancellationToken)
    {
        var ahora = DateTimeOffset.UtcNow;
        return await db.VerificacionesOtp
            .Where(x => x.Id == verificacionId && x.UsuarioId == usuarioId &&
                x.Motivo == motivo && x.ConsumidoEn == null && x.ExpiraEn > ahora)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.ConsumidoEn, ahora), cancellationToken) == 1;
    }

    public Task<RecuperacionContrasena?> ObtenerRecuperacion(
        Guid recuperacionId, CancellationToken cancellationToken) =>
        db.RecuperacionesContrasena.SingleOrDefaultAsync(
            x => x.Id == recuperacionId, cancellationToken);

    public Task RevocarSesiones(Guid usuarioId, CancellationToken cancellationToken) =>
        db.Sesiones.Where(x => x.UsuarioId == usuarioId && x.RevocadoEn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevocadoEn, DateTimeOffset.UtcNow),
                cancellationToken);

    public Task RevocarFamiliaSesiones(
        Guid usuarioId, Guid familiaToken, CancellationToken cancellationToken) =>
        db.Sesiones
            .Where(x => x.UsuarioId == usuarioId &&
                x.FamiliaToken == familiaToken &&
                x.RevocadoEn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevocadoEn, DateTimeOffset.UtcNow),
                cancellationToken);

    public Task<EliminacionPerfil?> ObtenerEliminacionPerfil(
        Guid usuarioId, Guid eliminacionId, bool soloLectura, CancellationToken cancellationToken)
    {
        IQueryable<EliminacionPerfil> query = db.EliminacionesPerfil;
        if (soloLectura) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(
            x => x.Id == eliminacionId && x.UsuarioId == usuarioId, cancellationToken);
    }

    public Task<bool> ExisteEliminacionPerfilPendiente(
        Guid usuarioId, CancellationToken cancellationToken) =>
        db.EliminacionesPerfil.AnyAsync(
            x => x.UsuarioId == usuarioId && x.Estado == "pendiente", cancellationToken);

    public void Agregar(DesafioOtp desafio) => db.DesafiosOtp.Add(desafio);

    public void Agregar(VerificacionOtp verificacion) => db.VerificacionesOtp.Add(verificacion);

    public void Agregar(RecuperacionContrasena recuperacion) => db.RecuperacionesContrasena.Add(recuperacion);
    public void Agregar(ConsentimientoPrivacidad consentimiento) => db.ConsentimientosPrivacidad.Add(consentimiento);

    public void Agregar(EliminacionPerfil eliminacion) => db.EliminacionesPerfil.Add(eliminacion);

    public void Agregar(EventoOutbox evento) => db.EventosOutbox.Add(evento);
}
