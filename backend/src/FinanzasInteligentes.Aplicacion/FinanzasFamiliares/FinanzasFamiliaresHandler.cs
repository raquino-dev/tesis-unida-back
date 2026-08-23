using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasFamiliares.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasFamiliares;
using FinanzasInteligentes.Dominio.Identidad;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Aplicacion.FinanzasFamiliares;

public sealed class FinanzasFamiliaresHandler(
    IFamiliasRepository familias,
    IFinanzasRepository finanzas,
    IIdentidadRepository identidad,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PaginaGrupoFamiliarResponse> ListarGrupos(
        Guid usuarioId, string? cursor, long limite, CancellationToken ct)
    {
        var grupos = Paginar(await familias.ListarGrupos(usuarioId, ct), cursor, limite, out var siguiente);
        var responses = new List<GrupoFamiliarResponse>();
        foreach (var grupo in grupos)
            responses.Add(await MapGrupo(grupo, usuarioId, ct));
        return new(responses, new(siguiente));
    }

    public async Task<GrupoFamiliarResponse> CrearGrupo(
        Guid usuarioId, string nombre, CancellationToken ct)
    {
        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var grupo = GrupoFamiliar.Crear(nombre);
        familias.Agregar(grupo);
        familias.Agregar(IntegranteFamiliar.Crear(grupo.Id, usuarioId, "propietario"));
        familias.Agregar(CajaCompartida.Crear(grupo.Id));
        familias.Agregar(CategoriaFamiliar.Crear(
            grupo.Id, "Alimentación", "gasto", "restaurant", "#F59E0B"));
        familias.Agregar(CategoriaFamiliar.Crear(
            grupo.Id, "Servicios", "gasto", "home", "#3B82F6"));
        familias.Agregar(CategoriaFamiliar.Crear(
            grupo.Id, "Ingresos", "ingreso", "payments", "#10B981"));
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
        return new(grupo.Id, grupo.Nombre, "propietario", 1, 0, grupo.CreadoEn, grupo.Version);
    }

    public async Task<GrupoFamiliarResponse> ObtenerGrupo(
        Guid usuarioId, Guid grupoId, CancellationToken ct)
    {
        var grupo = await GrupoExistente(grupoId, true, ct);
        await RequerirMembresia(grupoId, usuarioId, ct);
        return await MapGrupo(grupo, usuarioId, ct);
    }

    public async Task<GrupoFamiliarResponse> ActualizarGrupo(
        Guid usuarioId, Guid grupoId, long version, string nombre, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var grupo = await GrupoExistente(grupoId, false, ct);
        VerificarVersion(grupo.Version, version);
        grupo.Actualizar(nombre);
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapGrupo(grupo, usuarioId, ct);
    }

    public async Task<ProcesoGrupoResponse> SolicitarEliminacion(
        Guid usuarioId, Guid grupoId, long version, Guid verificacionOtpId,
        string correlationId, CancellationToken ct)
    {
        var membresia = await RequerirMembresia(grupoId, usuarioId, ct);
        if (membresia.Rol != "propietario")
            throw new ForbiddenException(
                "solo_propietario", "Sólo el propietario puede eliminar el grupo.");
        var grupo = await GrupoExistente(grupoId, true, ct);
        VerificarVersion(grupo.Version, version);
        if (await familias.ExisteEliminacionPendiente(grupoId, ct))
            throw new ConflictException(
                "eliminacion_pendiente", "Ya existe una eliminación pendiente.");
        if (!await identidad.ConsumirVerificacionOtp(
            usuarioId, verificacionOtpId, "eliminacion-grupo-familiar", ct))
            throw new ForbiddenException(
                "verificacion_otp_invalida", "La verificación OTP no es válida.");

        var eliminacion = EliminacionGrupoFamiliar.Crear(grupoId);
        familias.Agregar(eliminacion);
        identidad.Agregar(EventoOutbox.Crear(
            "grupo-familiar.eliminacion-solicitada", "grupo-familiar", eliminacion.Id,
            new { eliminacion.Id, GrupoFamiliarId = grupoId }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return MapProceso(eliminacion);
    }

    public async Task<ProcesoGrupoResponse> ObtenerEliminacion(
        Guid usuarioId, Guid grupoId, Guid eliminacionId, CancellationToken ct)
    {
        if (await familias.ObtenerMembresia(grupoId, usuarioId, true, ct) is null)
            throw new ForbiddenException("no_es_integrante", "No integra el grupo familiar.");
        var eliminacion = await familias.ObtenerEliminacion(grupoId, eliminacionId, ct)
            ?? throw new NotFoundException(
                "eliminacion_no_encontrada", "La eliminación no existe.");
        return MapProceso(eliminacion);
    }

    public async Task<PaginaIntegranteFamiliarResponse> ListarIntegrantes(
        Guid usuarioId, Guid grupoId, string? cursor, long limite, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var items = Paginar(await familias.ListarIntegrantes(grupoId, ct), cursor, limite, out var siguiente);
        var response = new List<IntegranteFamiliarResponse>();
        foreach (var item in items) response.Add(await MapIntegrante(item, ct));
        return new(response, new(siguiente));
    }

    public async Task<IntegranteFamiliarResponse> ActualizarIntegrante(
        Guid usuarioId, Guid grupoId, Guid integranteId, long version, string rol, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var integrante = await familias.ObtenerIntegrante(grupoId, integranteId, false, ct)
            ?? throw new NotFoundException("integrante_no_encontrado", "El integrante no existe.");
        VerificarVersion(integrante.Version, version);
        integrante.CambiarRol(rol);
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapIntegrante(integrante, ct);
    }

    public async Task EliminarIntegrante(
        Guid usuarioId, Guid grupoId, Guid integranteId, long version, CancellationToken ct)
    {
        var actor = await RequerirMembresia(grupoId, usuarioId, ct);
        var integrante = await familias.ObtenerIntegrante(grupoId, integranteId, false, ct)
            ?? throw new NotFoundException("integrante_no_encontrado", "El integrante no existe.");
        if (actor.Rol == "integrante" && actor.Id != integrante.Id)
            throw new ForbiddenException("rol_insuficiente", "No puede eliminar a otro integrante.");
        VerificarVersion(integrante.Version, version);
        integrante.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    public async Task<PaginaInvitacionFamiliarResponse> ListarInvitaciones(
        Guid usuarioId, Guid grupoId, string? cursor, long limite, string? estado, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var query = await familias.ListarInvitaciones(grupoId, ct);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(x => x.Estado == estado).ToArray();
        var items = Paginar(query, cursor, limite, out var siguiente);
        var respuestas = new List<InvitacionFamiliarResponse>();
        foreach (var invitacion in items)
        {
            string? aliasDestino = null;
            if (invitacion.UsuarioDestinoId is not null)
                aliasDestino = (await identidad.ObtenerUsuario(
                    invitacion.UsuarioDestinoId.Value, true, ct))?.Alias;
            respuestas.Add(MapInvitacion(invitacion, aliasDestino));
        }
        return new(respuestas, new(siguiente));
    }

    public async Task<(CrearInvitacionFamiliarResponse Response, string Token)> CrearInvitacion(
        Guid usuarioId, Guid grupoId, string? correo, Guid? destinoId,
        string? alias, string rol, string correlationId, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var destinosIndicados = new[]
        {
            !string.IsNullOrWhiteSpace(correo),
            destinoId is not null,
            !string.IsNullOrWhiteSpace(alias)
        }.Count(x => x);
        if (destinosIndicados != 1)
            throw new DomainException(
                "destino_invitacion_invalido",
                "Indicá únicamente el alias del usuario que querés invitar.");

        Usuario? destino = null;
        if (destinoId is not null)
            destino = await identidad.ObtenerUsuario(destinoId.Value, true, ct)
                ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        else if (!string.IsNullOrWhiteSpace(alias))
        {
            var aliasNormalizado = Usuario.NormalizarAlias(alias);
            destino = await identidad.BuscarUsuarioPorAlias(aliasNormalizado, ct)
                ?? throw new NotFoundException(
                    "alias_no_encontrado", "No encontramos un usuario con ese alias.");
            destinoId = destino.Id;
            correo = null;
        }
        else if (!string.IsNullOrWhiteSpace(correo))
            destino = await identidad.BuscarUsuarioPorCorreo(correo.Trim().ToLowerInvariant(), ct);
        if (destino?.Id == usuarioId)
            throw new ConflictException(
                "autoinvitacion_no_permitida", "No podés invitarte a tu propio grupo.");
        if (destino is not null && await familias.UsuarioEsIntegrante(grupoId, destino.Id, ct))
            throw new ConflictException("usuario_ya_integrante", "El usuario ya integra el grupo.");
        var invitaciones = await familias.ListarInvitaciones(grupoId, ct);
        if (destino is not null && invitaciones.Any(x =>
            x.Estado == "pendiente" && x.UsuarioDestinoId == destino.Id && x.ExpiraEn > DateTimeOffset.UtcNow))
            throw new ConflictException(
                "invitacion_duplicada", "Ya existe una invitación pendiente para ese usuario.");

        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var invitacion = InvitacionFamiliar.Crear(
            grupoId, correo, destinoId, rol, Hash(token), Hash(codigo));
        familias.Agregar(invitacion);
        identidad.Agregar(EventoOutbox.Crear(
            "grupo-familiar.invitacion-creada", "invitacion-familiar", invitacion.Id,
            new
            {
                invitacion.Id,
                Token = token,
                Codigo = codigo,
                Correo = destino?.Correo ?? invitacion.Correo,
                Alias = destino?.Alias
            }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return (new(
            invitacion.Id, grupoId, invitacion.Correo, invitacion.UsuarioDestinoId, destino?.Alias,
            invitacion.Rol, invitacion.Estado, invitacion.ExpiraEn, invitacion.Version, codigo), token);
    }

    public async Task<InvitacionPublicaResponse> ObtenerInvitacionPublica(string token, CancellationToken ct)
    {
        var invitacion = await familias.ObtenerInvitacionPorHashToken(Hash(token), true, ct)
            ?? throw new NotFoundException("invitacion_no_encontrada", "La invitación no existe.");
        var grupo = await GrupoExistente(invitacion.GrupoFamiliarId, true, ct);
        var destino = invitacion.Correo ?? invitacion.UsuarioDestinoId?.ToString() ?? string.Empty;
        return new(new(grupo.Id, grupo.Nombre), Enmascarar(destino),
            invitacion.Rol, invitacion.Estado, invitacion.ExpiraEn);
    }

    public async Task<IntegranteFamiliarResponse> AceptarInvitacion(
        Guid usuarioId, string token, string codigo, CancellationToken ct)
    {
        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var invitacion = await familias.ObtenerInvitacionPorHashToken(Hash(token), false, ct)
            ?? throw new NotFoundException("invitacion_no_encontrada", "La invitación no existe.");
        var usuario = await identidad.ObtenerUsuario(usuarioId, true, ct)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        if (invitacion.UsuarioDestinoId is not null && invitacion.UsuarioDestinoId != usuarioId ||
            invitacion.Correo is not null &&
            !string.Equals(invitacion.Correo, usuario.Correo, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException(
                "destino_invitacion_invalido", "La invitación pertenece a otro usuario.");
        if (await familias.UsuarioEsIntegrante(invitacion.GrupoFamiliarId, usuarioId, ct))
            throw new ConflictException("usuario_ya_integrante", "El usuario ya integra el grupo.");
        invitacion.Aceptar(Hash(codigo));
        var integrante = IntegranteFamiliar.Crear(
            invitacion.GrupoFamiliarId, usuarioId, invitacion.Rol);
        familias.Agregar(integrante);
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
        return await MapIntegrante(integrante, ct);
    }

    public async Task CancelarInvitacion(
        Guid usuarioId, Guid grupoId, Guid invitacionId, long version, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var invitacion = await familias.ObtenerInvitacion(grupoId, invitacionId, false, ct)
            ?? throw new NotFoundException("invitacion_no_encontrada", "La invitación no existe.");
        VerificarVersion(invitacion.Version, version);
        invitacion.Cancelar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    public async Task<PaginaCuentaCompartidaResponse> ListarCuentas(
        Guid usuarioId, Guid grupoId, string? cursor, long limite, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var items = Paginar(await familias.ListarCuentasCompartidas(grupoId, ct), cursor, limite, out var siguiente);
        var response = new List<CuentaCompartidaResponse>();
        foreach (var item in items) response.Add(await MapCuentaCompartida(item, ct));
        return new(response, new(siguiente));
    }

    public async Task<CuentaCompartidaResponse> CompartirCuenta(
        Guid usuarioId, Guid grupoId, Guid cuentaId, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        _ = await finanzas.ObtenerCuenta(usuarioId, cuentaId, true, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        if (await familias.ObtenerCuentaCompartida(grupoId, cuentaId, true, ct) is not null)
            throw new ConflictException("cuenta_ya_compartida", "La cuenta ya está compartida.");
        var entity = CuentaCompartida.Crear(grupoId, cuentaId, usuarioId);
        familias.Agregar(entity);
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapCuentaCompartida(entity, ct);
    }

    public async Task DesvincularCuenta(
        Guid usuarioId, Guid grupoId, Guid cuentaId, long version, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerCuentaCompartida(grupoId, cuentaId, false, ct)
            ?? throw new NotFoundException("cuenta_compartida_no_encontrada", "La cuenta no está compartida.");
        VerificarVersion(entity.Version, version);
        entity.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    public async Task<PaginaCategoriaFamiliarResponse> ListarCategorias(
        Guid usuarioId, Guid grupoId, string? cursor, long limite, string? tipo, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var query = await familias.ListarCategorias(grupoId, ct);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(x => x.Tipo == tipo).ToArray();
        var items = Paginar(query, cursor, limite, out var siguiente);
        return new(items.Select(MapCategoria).ToArray(), new(siguiente));
    }

    public async Task<CategoriaFamiliarResponse> CrearCategoria(
        Guid usuarioId, Guid grupoId, string nombre, string tipo,
        string icono, string color, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var entity = CategoriaFamiliar.Crear(grupoId, nombre, tipo, icono, color);
        familias.Agregar(entity);
        await unidadDeTrabajo.GuardarCambios(ct);
        return MapCategoria(entity);
    }

    public async Task<CategoriaFamiliarResponse> ActualizarCategoria(
        Guid usuarioId, Guid grupoId, Guid categoriaId, long version,
        string? nombre, string? tipo, string? icono, string? color, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerCategoria(grupoId, categoriaId, false, ct)
            ?? throw new NotFoundException("categoria_no_encontrada", "La categoría no existe.");
        VerificarVersion(entity.Version, version);
        entity.Actualizar(nombre, tipo, icono, color);
        await unidadDeTrabajo.GuardarCambios(ct);
        return MapCategoria(entity);
    }

    public async Task EliminarCategoria(
        Guid usuarioId, Guid grupoId, Guid categoriaId, long version, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerCategoria(grupoId, categoriaId, false, ct)
            ?? throw new NotFoundException("categoria_no_encontrada", "La categoría no existe.");
        VerificarVersion(entity.Version, version);
        entity.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    public async Task<PaginaMovimientoFamiliarResponse> ListarMovimientos(
        Guid usuarioId, Guid grupoId, string? cursor, long limite, string? tipo,
        Guid? categoriaId, Guid? cuentaId, Guid? integranteId, DateOnly? desde, DateOnly? hasta,
        string? texto, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        IEnumerable<MovimientoFamiliar> query = await familias.ListarMovimientos(grupoId, ct);
        if (tipo is not null) query = query.Where(x => x.Tipo == tipo);
        if (categoriaId is not null) query = query.Where(x => x.CategoriaIds.Contains(categoriaId.Value));
        if (cuentaId is not null) query = query.Where(x => x.CuentaId == cuentaId);
        if (integranteId is not null)
        {
            var integrante = await familias.ObtenerIntegrante(grupoId, integranteId.Value, true, ct)
                ?? throw new NotFoundException("integrante_no_encontrado", "El integrante no existe.");
            query = query.Where(x => x.UsuarioId == integrante.UsuarioId);
        }
        if (desde is not null) query = query.Where(x => x.Fecha >= desde);
        if (hasta is not null) query = query.Where(x => x.Fecha <= hasta);
        if (!string.IsNullOrWhiteSpace(texto))
            query = query.Where(x => x.Descripcion.Contains(texto, StringComparison.OrdinalIgnoreCase));
        var items = Paginar(query.ToArray(), cursor, limite, out var siguiente);
        return new(items.Select(MapMovimiento).ToArray(), new(siguiente));
    }

    public async Task<MovimientoFamiliarResponse> CrearMovimiento(
        Guid usuarioId, Guid grupoId, Guid cuentaId, string tipo, long monto,
        string descripcion, DateOnly fecha, IReadOnlyCollection<Guid>? categoriaIds,
        CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var compartida = await familias.ObtenerCuentaCompartida(grupoId, cuentaId, true, ct);
        if (compartida is null)
            throw new NotFoundException("cuenta_no_compartida", "La cuenta no está compartida.");
        var cuenta = await finanzas.ObtenerCuenta(
            compartida.CompartidaPorUsuarioId, cuentaId, false, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        await ValidarCategorias(grupoId, categoriaIds, ct);
        var entity = MovimientoFamiliar.Crear(
            grupoId, usuarioId, cuentaId, tipo, monto, descripcion, fecha, categoriaIds);
        cuenta.AplicarMovimiento(tipo, monto);
        familias.Agregar(entity);
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
        return MapMovimiento(entity);
    }

    public async Task<MovimientoFamiliarResponse> ObtenerMovimiento(
        Guid usuarioId, Guid grupoId, Guid movimientoId, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerMovimiento(grupoId, movimientoId, true, ct)
            ?? throw new NotFoundException("movimiento_no_encontrado", "El movimiento no existe.");
        return MapMovimiento(entity);
    }

    public async Task<MovimientoFamiliarResponse> ActualizarMovimiento(
        Guid usuarioId, Guid grupoId, Guid movimientoId, long version,
        string? descripcion, IReadOnlyCollection<Guid>? categoriaIds, CancellationToken ct)
    {
        var actor = await RequerirMembresia(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerMovimiento(grupoId, movimientoId, false, ct)
            ?? throw new NotFoundException("movimiento_no_encontrado", "El movimiento no existe.");
        if (entity.UsuarioId != usuarioId && actor.Rol == "integrante")
            throw new ForbiddenException("rol_insuficiente", "No puede modificar este movimiento.");
        VerificarVersion(entity.Version, version);
        await ValidarCategorias(grupoId, categoriaIds, ct);
        entity.Actualizar(descripcion, categoriaIds);
        await unidadDeTrabajo.GuardarCambios(ct);
        return MapMovimiento(entity);
    }

    public async Task EliminarMovimiento(
        Guid usuarioId, Guid grupoId, Guid movimientoId, long version, CancellationToken ct)
    {
        var actor = await RequerirMembresia(grupoId, usuarioId, ct);
        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var entity = await familias.ObtenerMovimiento(grupoId, movimientoId, false, ct)
            ?? throw new NotFoundException("movimiento_no_encontrado", "El movimiento no existe.");
        if (entity.UsuarioId != usuarioId && actor.Rol == "integrante")
            throw new ForbiddenException("rol_insuficiente", "No puede anular este movimiento.");
        VerificarVersion(entity.Version, version);
        var compartida = await familias.ObtenerCuentaCompartida(
            grupoId, entity.CuentaId, true, ct)
            ?? throw new NotFoundException("cuenta_no_compartida", "La cuenta no está compartida.");
        var cuenta = await finanzas.ObtenerCuenta(
            compartida.CompartidaPorUsuarioId, entity.CuentaId, false, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        entity.Anular();
        cuenta.RevertirMovimiento(entity.Tipo, entity.Monto);
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
    }

    public async Task<CajaCompartidaResponse> ObtenerCaja(
        Guid usuarioId, Guid grupoId, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var caja = await familias.ObtenerCaja(grupoId, true, ct)
            ?? throw new NotFoundException("caja_no_encontrada", "La caja no existe.");
        var inicioMes = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var operaciones = (await familias.ListarOperacionesCaja(grupoId, ct))
            .Where(x => x.CreadoEn >= inicioMes);
        return new(grupoId, caja.Saldo,
            operaciones.Where(x => x.Tipo == "aporte").Sum(x => x.Monto),
            operaciones.Where(x => x.Tipo == "retiro").Sum(x => x.Monto), caja.Version);
    }

    public async Task<PaginaOperacionCajaResponse> ListarOperacionesCaja(
        Guid usuarioId, Guid grupoId, string? cursor, long limite, string? tipo,
        Guid? integranteId, DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        IEnumerable<OperacionCaja> query = await familias.ListarOperacionesCaja(grupoId, ct);
        if (tipo is not null) query = query.Where(x => x.Tipo == tipo);
        if (integranteId is not null)
        {
            var integrante = await familias.ObtenerIntegrante(grupoId, integranteId.Value, true, ct)
                ?? throw new NotFoundException("integrante_no_encontrado", "El integrante no existe.");
            query = query.Where(x => x.UsuarioId == integrante.UsuarioId);
        }
        if (desde is not null) query = query.Where(x => DateOnly.FromDateTime(x.CreadoEn.Date) >= desde);
        if (hasta is not null) query = query.Where(x => DateOnly.FromDateTime(x.CreadoEn.Date) <= hasta);
        var items = Paginar(query.ToArray(), cursor, limite, out var siguiente);
        var response = new List<OperacionCajaResponse>();
        foreach (var item in items) response.Add(await MapOperacion(item, ct));
        return new(response, new(siguiente));
    }

    public async Task<OperacionCajaResponse> CrearOperacionCaja(
        Guid usuarioId, Guid grupoId, string tipo, long monto, string descripcion,
        Guid cuentaId, Guid? verificacionOtpId, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        if (tipo == "retiro" && (verificacionOtpId is null ||
            !await identidad.ConsumirVerificacionOtp(
                usuarioId, verificacionOtpId.Value, "operacion-sensible", ct)))
            throw new ForbiddenException(
                "verificacion_otp_invalida", "El retiro requiere una verificación OTP.");
        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var cuenta = await finanzas.ObtenerCuenta(usuarioId, cuentaId, false, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta privada no existe.");
        var caja = await familias.ObtenerCaja(grupoId, false, ct)
            ?? throw new NotFoundException("caja_no_encontrada", "La caja no existe.");
        var saldos = caja.Aplicar(tipo, monto);
        cuenta.AplicarMovimiento(tipo == "aporte" ? "gasto" : "ingreso", monto);
        var operacion = OperacionCaja.Crear(
            grupoId, usuarioId, cuentaId, tipo, monto, descripcion, saldos.Anterior, saldos.Posterior);
        familias.Agregar(operacion);
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
        return await MapOperacion(operacion, ct);
    }

    public async Task<OperacionCajaResponse> ObtenerOperacionCaja(
        Guid usuarioId, Guid grupoId, Guid operacionId, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerOperacionCaja(grupoId, operacionId, ct)
            ?? throw new NotFoundException("operacion_no_encontrada", "La operación no existe.");
        return await MapOperacion(entity, ct);
    }

    public async Task<PaginaPresupuestoFamiliarResponse> ListarPresupuestos(
        Guid usuarioId, Guid grupoId, string? cursor, long limite, string? periodo,
        Guid? categoriaId, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        IEnumerable<PresupuestoFamiliar> query = await familias.ListarPresupuestos(grupoId, ct);
        if (periodo is not null) query = query.Where(x => x.Periodo == periodo);
        if (categoriaId is not null) query = query.Where(x => x.CategoriaIds.Contains(categoriaId.Value));
        var items = Paginar(query.ToArray(), cursor, limite, out var siguiente);
        var response = new List<PresupuestoFamiliarResponse>();
        foreach (var item in items) response.Add(await MapPresupuesto(item, ct));
        return new(response, new(siguiente));
    }

    public async Task<PresupuestoFamiliarResponse> CrearPresupuesto(
        Guid usuarioId, Guid grupoId, string nombre, long monto, string periodo,
        IReadOnlyCollection<Guid> categoriaIds, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        await ValidarCategorias(grupoId, categoriaIds, ct);
        var entity = PresupuestoFamiliar.Crear(grupoId, nombre, monto, periodo, categoriaIds);
        familias.Agregar(entity);
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapPresupuesto(entity, ct);
    }

    public async Task<PresupuestoFamiliarResponse> ObtenerPresupuesto(
        Guid usuarioId, Guid grupoId, Guid presupuestoId, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerPresupuesto(grupoId, presupuestoId, true, ct)
            ?? throw new NotFoundException("presupuesto_no_encontrado", "El presupuesto no existe.");
        return await MapPresupuesto(entity, ct);
    }

    public async Task<PresupuestoFamiliarResponse> ActualizarPresupuesto(
        Guid usuarioId, Guid grupoId, Guid presupuestoId, long version,
        string? nombre, long? monto, string? periodo,
        IReadOnlyCollection<Guid>? categoriaIds, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerPresupuesto(grupoId, presupuestoId, false, ct)
            ?? throw new NotFoundException("presupuesto_no_encontrado", "El presupuesto no existe.");
        VerificarVersion(entity.Version, version);
        await ValidarCategorias(grupoId, categoriaIds, ct);
        entity.Actualizar(nombre, monto, periodo, categoriaIds);
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapPresupuesto(entity, ct);
    }

    public async Task EliminarPresupuesto(
        Guid usuarioId, Guid grupoId, Guid presupuestoId, long version, CancellationToken ct)
    {
        await RequerirAdministrador(grupoId, usuarioId, ct);
        var entity = await familias.ObtenerPresupuesto(grupoId, presupuestoId, false, ct)
            ?? throw new NotFoundException("presupuesto_no_encontrado", "El presupuesto no existe.");
        VerificarVersion(entity.Version, version);
        entity.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    public async Task<DashboardFamiliarResponse> ObtenerDashboard(
        Guid usuarioId, Guid grupoId, DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var rango = ResolverRango(desde, hasta);
        var movimientos = (await familias.ListarMovimientos(grupoId, ct))
            .Where(x => x.Estado != "anulado" && x.Fecha >= rango.Desde && x.Fecha <= rango.Hasta).ToArray();
        var ingresos = movimientos.Where(x => x.Tipo == "ingreso").Sum(x => x.Monto);
        var gastos = movimientos.Where(x => x.Tipo == "gasto").Sum(x => x.Monto);
        var presupuestos = await familias.ListarPresupuestos(grupoId, ct);
        var total = presupuestos.Sum(x => x.Monto);
        var categorias = await CalcularCategorias(grupoId, movimientos, gastos, ct);
        return new("familiar", rango, ingresos, gastos, ingresos - gastos,
            total, Math.Max(0, total - gastos), CalcularScore(ingresos, gastos),
            categorias, [], []);
    }

    public async Task<ReporteFamiliarResponse> ObtenerReporte(
        Guid usuarioId, Guid grupoId, string? rangoNombre, DateOnly? desde, DateOnly? hasta,
        string? tipo, Guid? categoriaId, Guid? integranteId, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var rango = ResolverRango(desde, hasta);
        IEnumerable<MovimientoFamiliar> query = await familias.ListarMovimientos(grupoId, ct);
        query = query.Where(x => x.Estado != "anulado" && x.Fecha >= rango.Desde && x.Fecha <= rango.Hasta);
        if (tipo is not null) query = query.Where(x => x.Tipo == tipo);
        if (categoriaId is not null) query = query.Where(x => x.CategoriaIds.Contains(categoriaId.Value));
        if (integranteId is not null)
        {
            var integrante = await familias.ObtenerIntegrante(grupoId, integranteId.Value, true, ct)
                ?? throw new NotFoundException("integrante_no_encontrado", "El integrante no existe.");
            query = query.Where(x => x.UsuarioId == integrante.UsuarioId);
        }
        var movimientos = query.ToArray();
        var ingresos = movimientos.Where(x => x.Tipo == "ingreso").Sum(x => x.Monto);
        var gastos = movimientos.Where(x => x.Tipo == "gasto").Sum(x => x.Monto);
        var distribucion = await CalcularCategorias(grupoId, movimientos, gastos, ct);
        var tendencia = movimientos.GroupBy(x => x.Fecha.ToString("yyyy-MM"))
            .OrderBy(x => x.Key)
            .Select(x => new TendenciaFamiliarResponse(
                x.Key, x.Where(m => m.Tipo == "ingreso").Sum(m => m.Monto),
                x.Where(m => m.Tipo == "gasto").Sum(m => m.Monto))).ToArray();
        return new("familiar", rangoNombre ?? "mes", rango.Desde, rango.Hasta,
            ingresos, gastos, ingresos - gastos, distribucion, tendencia, []);
    }

    public async Task<ProyeccionFamiliarResponse> ObtenerProyeccion(
        Guid usuarioId, Guid grupoId, string? periodo, CancellationToken ct)
    {
        await RequerirMembresia(grupoId, usuarioId, ct);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var desdeHistorial = hoy.AddMonths(-3);
        var movimientos = (await familias.ListarMovimientos(grupoId, ct))
            .Where(x => x.Estado != "anulado" && x.Fecha >= desdeHistorial).ToArray();
        var gastos = movimientos.Where(x => x.Tipo == "gasto").Sum(x => x.Monto);
        var ingresos = movimientos.Where(x => x.Tipo == "ingreso").Sum(x => x.Monto);
        var mensual = gastos / 3;
        var categorias = await CalcularCategorias(grupoId, movimientos, gastos, ct);
        return new(
            "familiar", 3, movimientos.Length < 12, mensual, ingresos / 3 - mensual,
            categorias.FirstOrDefault()?.Nombre ?? "Sin datos",
            ingresos >= gastos ? "bajo" : "alto", "gastos-v1",
            new(hoy, hoy.AddMonths(periodo == "anual" ? 12 : 6)),
            categorias.Select(x => new CategoriaProyeccionResponse(x.Nombre, x.Monto / 3, 0)).ToArray(),
            [], DateTimeOffset.UtcNow);
    }

    private async Task<GrupoFamiliarResponse> MapGrupo(
        GrupoFamiliar grupo, Guid usuarioId, CancellationToken ct)
    {
        var membresia = await RequerirMembresia(grupo.Id, usuarioId, ct);
        var miembros = await familias.ListarIntegrantes(grupo.Id, ct);
        var cuentas = await familias.ListarCuentasCompartidas(grupo.Id, ct);
        return new(grupo.Id, grupo.Nombre, membresia.Rol, miembros.Count, cuentas.Count,
            grupo.CreadoEn, grupo.Version);
    }

    private async Task<IntegranteFamiliarResponse> MapIntegrante(
        IntegranteFamiliar integrante, CancellationToken ct)
    {
        var usuario = await identidad.ObtenerUsuario(integrante.UsuarioId, true, ct)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        return new(integrante.Id, new(usuario.Id, usuario.Nombre, usuario.Correo),
            integrante.Rol, integrante.CreadoEn, integrante.Version);
    }

    private async Task<CuentaCompartidaResponse> MapCuentaCompartida(
        CuentaCompartida entity, CancellationToken ct)
    {
        var cuenta = await finanzas.ObtenerCuenta(
            entity.CompartidaPorUsuarioId, entity.CuentaId, true, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        var usuario = await identidad.ObtenerUsuario(entity.CompartidaPorUsuarioId, true, ct)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        return new(entity.GrupoFamiliarId, new(cuenta.Id, cuenta.Nombre, cuenta.Tipo, cuenta.Moneda),
            new(usuario.Id, usuario.Nombre), entity.CreadoEn, entity.Version);
    }

    private async Task<OperacionCajaResponse> MapOperacion(OperacionCaja entity, CancellationToken ct)
    {
        var usuario = await identidad.ObtenerUsuario(entity.UsuarioId, true, ct)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        return new(entity.Id, entity.Tipo, entity.Monto, entity.Descripcion,
            new(usuario.Id, usuario.Nombre), entity.CreadoEn,
            entity.SaldoAnterior, entity.SaldoPosterior);
    }

    private async Task<PresupuestoFamiliarResponse> MapPresupuesto(
        PresupuestoFamiliar entity, CancellationToken ct)
    {
        var categorias = await familias.ObtenerCategorias(entity.GrupoFamiliarId, entity.CategoriaIds, ct);
        var movimientos = await familias.ListarMovimientos(entity.GrupoFamiliarId, ct);
        var gastado = movimientos.Where(x => x.Estado != "anulado" && x.Tipo == "gasto" &&
            x.CategoriaIds.Intersect(entity.CategoriaIds).Any()).Sum(x => x.Monto);
        var progreso = entity.Monto == 0 ? 0 : Math.Min(1, (double)gastado / entity.Monto);
        return new(entity.Id, "familiar", entity.Nombre, entity.Monto, gastado,
            Math.Max(0, entity.Monto - gastado), progreso, "activo",
            progreso < .8 ? "saludable" : progreso < 1 ? "atencion" : "excedido",
            entity.Periodo, categorias.Select(x => new CategoriaPresupuestoResponse(x.Id, x.Nombre)).ToArray(),
            entity.Version);
    }

    private async Task<IReadOnlyCollection<CategoriaAnaliticaResponse>> CalcularCategorias(
        Guid grupoId, IReadOnlyCollection<MovimientoFamiliar> movimientos, long totalGastos, CancellationToken ct)
    {
        var categorias = await familias.ListarCategorias(grupoId, ct);
        return categorias.Select(c =>
        {
            var monto = movimientos.Where(x => x.Tipo == "gasto" && x.CategoriaIds.Contains(c.Id))
                .Sum(x => x.Monto);
            return new CategoriaAnaliticaResponse(
                c.Id, c.Nombre, monto, totalGastos == 0 ? 0 : (double)monto / totalGastos);
        }).Where(x => x.Monto > 0).OrderByDescending(x => x.Monto).Take(5).ToArray();
    }

    private async Task<GrupoFamiliar> GrupoExistente(Guid grupoId, bool soloLectura, CancellationToken ct) =>
        await familias.ObtenerGrupo(grupoId, soloLectura, ct)
            ?? throw new NotFoundException("grupo_no_encontrado", "El grupo familiar no existe.");

    private async Task<IntegranteFamiliar> RequerirMembresia(
        Guid grupoId, Guid usuarioId, CancellationToken ct)
    {
        _ = await GrupoExistente(grupoId, true, ct);
        return await familias.ObtenerMembresia(grupoId, usuarioId, true, ct)
            ?? throw new ForbiddenException("no_es_integrante", "No integra el grupo familiar.");
    }

    private async Task<IntegranteFamiliar> RequerirAdministrador(
        Guid grupoId, Guid usuarioId, CancellationToken ct)
    {
        var membresia = await RequerirMembresia(grupoId, usuarioId, ct);
        if (membresia.Rol == "integrante")
            throw new ForbiddenException(
                "rol_insuficiente", "Se requiere rol administrador o propietario.");
        return membresia;
    }

    private async Task ValidarCategorias(
        Guid grupoId, IReadOnlyCollection<Guid>? ids, CancellationToken ct)
    {
        if (ids is null) return;
        var distintos = ids.Distinct().ToArray();
        var encontrados = await familias.ObtenerCategorias(grupoId, distintos, ct);
        if (encontrados.Count != distintos.Length)
            throw new NotFoundException(
                "categoria_no_encontrada", "Una o más categorías no existen.");
    }

    private static IReadOnlyCollection<T> Paginar<T>(
        IReadOnlyCollection<T> source, string? cursor, long limite, out string? siguiente)
        where T : FinanzasInteligentes.BuildingBlocks.Entity
    {
        if (limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");
        Guid? cursorId = null;
        if (cursor is not null)
        {
            if (!Guid.TryParse(cursor, out var id))
                throw new DomainException("cursor_invalido", "El cursor no es válido.");
            cursorId = id;
        }
        var items = source.OrderBy(x => x.Id)
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) > 0)
            .Take(checked((int)limite + 1)).ToArray();
        var tieneSiguiente = items.Length > limite;
        var pagina = items.Take(checked((int)limite)).ToArray();
        siguiente = tieneSiguiente ? pagina[^1].Id.ToString() : null;
        return pagina;
    }

    private static InvitacionFamiliarResponse MapInvitacion(
        InvitacionFamiliar x, string? aliasDestino) =>
        new(x.Id, x.Correo, x.UsuarioDestinoId, aliasDestino,
            x.Rol, x.Estado, x.ExpiraEn, x.Version);

    private static CategoriaFamiliarResponse MapCategoria(CategoriaFamiliar x) =>
        new(x.Id, x.GrupoFamiliarId, x.Nombre, x.Tipo, x.Icono, x.Color, x.Version);

    private static MovimientoFamiliarResponse MapMovimiento(MovimientoFamiliar x) =>
        new(x.Id, "familiar", x.CuentaId, x.Tipo, x.Monto, "PYG", x.Descripcion,
            x.Fecha, x.CategoriaIds, x.UsuarioId, x.Estado, x.CreadoEn, x.Version);

    private static ProcesoGrupoResponse MapProceso(EliminacionGrupoFamiliar x) =>
        new(x.Id, x.Estado, x.CreadoEn, x.CompletadoEn, x.ErrorCodigo,
            $"/api/v1/grupos-familiares/{x.GrupoFamiliarId}/eliminaciones/{x.Id}", x.Version);

    private static void VerificarVersion(long actual, long esperada)
    {
        if (actual != esperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión del recurso está desactualizada.");
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Enmascarar(string value) =>
        value.Contains('@')
            ? $"{value[0]}***{value[value.IndexOf('@')..]}"
            : value.Length <= 4 ? "***" : $"{value[..2]}***{value[^2..]}";

    private static PeriodoAnaliticoResponse ResolverRango(DateOnly? desde, DateOnly? hasta)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = desde ?? new DateOnly(hoy.Year, hoy.Month, 1);
        var fin = hasta ?? inicio.AddMonths(1).AddDays(-1);
        if (fin < inicio) throw new DomainException("rango_invalido", "El rango no es válido.");
        return new(inicio, fin);
    }

    private static long CalcularScore(long ingresos, long gastos) =>
        ingresos <= 0 ? 0 : Math.Clamp((long)Math.Round(100d * (ingresos - gastos) / ingresos), 0, 100);
}
