using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasFamiliares;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Planificacion;

public sealed class PlanificacionFinancieraHandler(
    IFinanzasRepository finanzas,
    IFamiliasRepository familias,
    IIdentidadRepository identidad,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PaginaPresupuestoResponse> ListarPresupuestos(
        Guid usuarioId, string? cursor, long limite, string? periodo,
        string? estado, Guid? categoriaId, CancellationToken ct)
    {
        ValidarLimite(limite);
        IEnumerable<Presupuesto> query = await finanzas.ListarPresupuestos(usuarioId, ct);
        if (periodo is not null) query = query.Where(x => x.Periodo == periodo);
        if (estado is not null) query = query.Where(x => x.Estado == estado);
        if (categoriaId is not null) query = query.Where(x => x.Categorias.Any(c => c.Id == categoriaId));
        var seleccionados = Paginar(query.ToArray(), cursor, limite, out var siguiente, out var hayMas);
        var respuestas = new List<PresupuestoResponse>();
        foreach (var item in seleccionados)
            respuestas.Add(await MapPresupuesto(item, null, null, ct));
        return new(respuestas, new(siguiente, hayMas, limite));
    }

    public async Task<PresupuestoResponse> CrearPresupuesto(
        Guid usuarioId, string ambito, Guid? grupoFamiliarId, string nombre,
        long monto, string periodo, IReadOnlyCollection<Guid> categoriaIds,
        string correlationId, CancellationToken ct)
    {
        ValidarPresupuestoPrivado(ambito, grupoFamiliarId);
        var categorias = await ObtenerCategoriasPrivadas(usuarioId, categoriaIds, ct);
        if (await finanzas.ExistePresupuestoSolapado(
            usuarioId, periodo, categoriaIds, null, ct))
            throw new ConflictException(
                "presupuesto_solapado",
                "Ya existe un presupuesto activo para una de las categorías y el periodo indicados.");

        var entity = Presupuesto.Crear(usuarioId, nombre, monto, periodo, categorias);
        finanzas.Agregar(entity);
        finanzas.Agregar(EventoOutbox.Crear(
            "presupuesto.creado", "presupuesto", entity.Id, new { entity.Id }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapPresupuesto(entity, null, null, ct);
    }

    public async Task<PresupuestoResponse> ObtenerPresupuesto(
        Guid usuarioId, Guid presupuestoId, CancellationToken ct)
    {
        var entity = await PresupuestoExistente(usuarioId, presupuestoId, true, ct);
        return await MapPresupuesto(entity, null, null, ct);
    }

    public async Task<PresupuestoResponse> ActualizarPresupuesto(
        Guid usuarioId, Guid presupuestoId, long version,
        string? ambito, Guid? grupoFamiliarId, string? nombre, long? monto,
        string? periodo, IReadOnlyCollection<Guid>? categoriaIds, CancellationToken ct)
    {
        if (ambito is not null || grupoFamiliarId is not null)
            ValidarPresupuestoPrivado(ambito ?? "privado", grupoFamiliarId);
        var entity = await PresupuestoExistente(usuarioId, presupuestoId, false, ct);
        VerificarVersion(entity.Version, version);
        IReadOnlyCollection<Categoria>? categorias = null;
        if (categoriaIds is not null)
            categorias = await ObtenerCategoriasPrivadas(usuarioId, categoriaIds, ct);
        var periodoFinal = periodo ?? entity.Periodo;
        var idsFinales = categoriaIds ?? entity.Categorias.Select(x => x.Id).ToArray();
        if (await finanzas.ExistePresupuestoSolapado(
            usuarioId, periodoFinal, idsFinales, entity.Id, ct))
            throw new ConflictException(
                "presupuesto_solapado",
                "Ya existe un presupuesto activo para una de las categorías y el periodo indicados.");
        entity.Actualizar(nombre, monto, periodo, categorias);
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapPresupuesto(entity, null, null, ct);
    }

    public async Task EliminarPresupuesto(
        Guid usuarioId, Guid presupuestoId, long version, CancellationToken ct)
    {
        var entity = await PresupuestoExistente(usuarioId, presupuestoId, false, ct);
        VerificarVersion(entity.Version, version);
        entity.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    public async Task<ResumenPresupuestarioResponse> ObtenerResumen(
        Guid usuarioId, DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        if (desde is not null && hasta is not null && desde > hasta)
            throw new DomainException("rango_invalido", "La fecha desde no puede ser posterior a hasta.");
        var presupuestos = await finanzas.ListarPresupuestos(usuarioId, ct);
        var respuestas = new List<PresupuestoResponse>();
        foreach (var presupuesto in presupuestos)
            respuestas.Add(await MapPresupuesto(presupuesto, desde, hasta, ct));
        var total = respuestas.Sum(x => x.Monto);
        var gastado = respuestas.Sum(x => x.Gastado);
        return new(
            total, gastado, Math.Max(0, total - gastado),
            total == 0 ? 0 : (double)gastado / total, respuestas);
    }

    public async Task<PaginaMetaAhorroResponse> ListarMetas(
        Guid usuarioId, string? cursor, long limite, string? ambito,
        Guid? grupoFamiliarId, CancellationToken ct)
    {
        ValidarLimite(limite);
        IEnumerable<MetaAhorro> query = await finanzas.ListarMetas(usuarioId, ct);
        if (ambito is not null) query = query.Where(x => x.Ambito == ambito);
        if (grupoFamiliarId is not null) query = query.Where(x => x.GrupoFamiliarId == grupoFamiliarId);
        var seleccionadas = Paginar(query.ToArray(), cursor, limite, out var siguiente, out var hayMas);
        var respuestas = new List<MetaAhorroResponse>();
        foreach (var meta in seleccionadas)
            respuestas.Add(await MapMeta(meta, usuarioId, ct));
        return new(respuestas, new(siguiente, hayMas, limite));
    }

    public async Task<MetaAhorroResponse> CrearMeta(
        Guid usuarioId, string ambito, Guid? grupoFamiliarId, string nombre,
        long montoObjetivo, DateOnly fechaObjetivo, Guid cuentaId,
        string correlationId, CancellationToken ct)
    {
        await ValidarCuentaMeta(usuarioId, ambito, grupoFamiliarId, cuentaId, true, ct);
        var entity = MetaAhorro.Crear(
            usuarioId, ambito, grupoFamiliarId, nombre, montoObjetivo, fechaObjetivo, cuentaId);
        finanzas.Agregar(entity);
        finanzas.Agregar(EventoOutbox.Crear(
            "meta-ahorro.creada", "meta-ahorro", entity.Id, new { entity.Id }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapMeta(entity, usuarioId, ct);
    }

    public async Task<MetaAhorroResponse> ObtenerMeta(
        Guid usuarioId, Guid metaId, CancellationToken ct)
    {
        var entity = await MetaAccesible(usuarioId, metaId, true, false, ct);
        return await MapMeta(entity, usuarioId, ct);
    }

    public async Task<MetaAhorroResponse> ActualizarMeta(
        Guid usuarioId, Guid metaId, long version, string? nombre,
        long? montoObjetivo, DateOnly? fechaObjetivo, CancellationToken ct)
    {
        var entity = await MetaAccesible(usuarioId, metaId, false, true, ct);
        VerificarVersion(entity.Version, version);
        var ahorrado = (await finanzas.ListarAportes(entity.Id, ct)).Sum(x => x.Monto);
        if (montoObjetivo is not null && montoObjetivo < ahorrado)
            throw new DomainException(
                "monto_objetivo_invalido",
                "El monto objetivo no puede ser menor que el saldo ya ahorrado.");
        entity.Actualizar(nombre, montoObjetivo, fechaObjetivo);
        await unidadDeTrabajo.GuardarCambios(ct);
        return await MapMeta(entity, usuarioId, ct);
    }

    public async Task EliminarMeta(
        Guid usuarioId, Guid metaId, long version, CancellationToken ct)
    {
        var entity = await MetaAccesible(usuarioId, metaId, false, true, ct);
        VerificarVersion(entity.Version, version);
        if ((await finanzas.ListarAportes(entity.Id, ct)).Sum(x => x.Monto) > 0)
            throw new ConflictException(
                "meta_con_saldo", "Una meta con saldo no puede eliminarse sin compensación.");
        entity.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    public async Task<PaginaAporteMetaResponse> ListarAportes(
        Guid usuarioId, Guid metaId, string? cursor, long limite, CancellationToken ct)
    {
        ValidarLimite(limite);
        await MetaAccesible(usuarioId, metaId, true, false, ct);
        var aportes = await finanzas.ListarAportes(metaId, ct);
        var seleccionados = Paginar(aportes.ToArray(), cursor, limite, out var siguiente, out var hayMas);
        var saldo = aportes.Sum(x => x.Monto);
        var respuestas = new List<AporteMetaResponse>();
        foreach (var aporte in seleccionados)
            respuestas.Add(await MapAporte(aporte, saldo, ct));
        return new(respuestas, new(siguiente, hayMas, limite));
    }

    public async Task<AporteMetaResponse> CrearAporte(
        Guid usuarioId, Guid metaId, long monto, Guid cuentaOrigenId,
        string descripcion, string idempotencyKey, string correlationId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200)
            throw new DomainException(
                "idempotencia_invalida", "La clave de idempotencia no es válida.");
        var hashIdempotencia = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey)));
        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var meta = await MetaAccesible(usuarioId, metaId, false, false, ct);
        var existente = await finanzas.ObtenerAportePorIdempotencia(
            meta.Id, hashIdempotencia, ct);
        if (existente is not null)
        {
            var saldoExistente = (await finanzas.ListarAportes(meta.Id, ct)).Sum(x => x.Monto);
            return await MapAporte(existente, saldoExistente, ct);
        }
        var origen = await ObtenerCuentaDelAmbito(
            usuarioId, meta.Ambito, meta.GrupoFamiliarId, cuentaOrigenId, false, ct);
        var destino = await ObtenerCuentaDelAmbito(
            usuarioId, meta.Ambito, meta.GrupoFamiliarId, meta.CuentaId, false, ct);
        if (origen.Id == destino.Id)
            throw new ConflictException(
                "cuentas_coincidentes", "La cuenta de origen debe ser distinta de la cuenta de la meta.");
        if (origen.SaldoActual < monto)
            throw new DomainException("saldo_insuficiente", "La cuenta de origen no tiene saldo suficiente.");

        var aporte = AporteMeta.Crear(
            meta.Id, monto, cuentaOrigenId, descripcion, usuarioId, hashIdempotencia);
        origen.AplicarMovimiento("gasto", monto);
        destino.AplicarMovimiento("ingreso", monto);
        var detalle = $"Aporte a meta: {meta.Nombre}";
        if (meta.Ambito == "privado")
        {
            finanzas.Agregar(Movimiento.Crear(
                usuarioId, origen.Id, "gasto", monto, detalle, aporte.Fecha, "meta"));
            finanzas.Agregar(Movimiento.Crear(
                usuarioId, destino.Id, "ingreso", monto, detalle, aporte.Fecha, "meta"));
        }
        else
        {
            familias.Agregar(MovimientoFamiliar.Crear(
                meta.GrupoFamiliarId!.Value, usuarioId, origen.Id,
                "gasto", monto, detalle, aporte.Fecha, []));
            familias.Agregar(MovimientoFamiliar.Crear(
                meta.GrupoFamiliarId.Value, usuarioId, destino.Id,
                "ingreso", monto, detalle, aporte.Fecha, []));
        }
        finanzas.Agregar(aporte);
        finanzas.Agregar(EventoOutbox.Crear(
            "meta-ahorro.aporte-creado", "meta-ahorro", meta.Id,
            new { MetaId = meta.Id, AporteId = aporte.Id, aporte.Monto }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
        var saldo = (await finanzas.ListarAportes(meta.Id, ct)).Sum(x => x.Monto);
        return await MapAporte(aporte, saldo, ct);
    }

    private async Task<PresupuestoResponse> MapPresupuesto(
        Presupuesto entity, DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        var rango = ResolverRango(entity.Periodo, desde, hasta);
        var ids = entity.Categorias.Select(x => x.Id).ToHashSet();
        var movimientos = await finanzas.ListarMovimientos(entity.UsuarioId, ct);
        var gastado = movimientos.Where(x =>
            x.Estado == "confirmado" && x.Tipo == "gasto" &&
            x.Fecha >= rango.Desde && x.Fecha <= rango.Hasta &&
            x.Categorias.Any(c => ids.Contains(c.Id))).Sum(x => x.Monto);
        var progresoReal = entity.Monto == 0 ? 0 : (double)gastado / entity.Monto;
        return new(
            entity.Id, "privado", entity.Nombre, entity.Monto, gastado,
            Math.Max(0, entity.Monto - gastado), Math.Min(1, progresoReal),
            entity.Estado,
            progresoReal < .8 ? "saludable" : progresoReal < 1 ? "en-riesgo" : "excedido",
            entity.Periodo,
            entity.Categorias.Select(x => new CategoriaPresupuestoResponse(x.Id, x.Nombre)).ToArray(),
            entity.Version);
    }

    private async Task<MetaAhorroResponse> MapMeta(
        MetaAhorro entity, Guid usuarioId, CancellationToken ct)
    {
        var cuenta = await ObtenerCuentaDelAmbito(
            usuarioId, entity.Ambito, entity.GrupoFamiliarId, entity.CuentaId, true, ct);
        var ahorrado = (await finanzas.ListarAportes(entity.Id, ct)).Sum(x => x.Monto);
        return new(
            entity.Id, entity.Ambito, entity.GrupoFamiliarId, entity.Nombre,
            entity.MontoObjetivo, ahorrado, Math.Max(0, entity.MontoObjetivo - ahorrado),
            Math.Min(1, (double)ahorrado / entity.MontoObjetivo), entity.FechaObjetivo,
            new(cuenta.Id, cuenta.Nombre), entity.Version);
    }

    private async Task<AporteMetaResponse> MapAporte(
        AporteMeta entity, long saldo, CancellationToken ct)
    {
        var usuario = await identidad.ObtenerUsuario(entity.AportadoPor, true, ct)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        return new(
            entity.Id, entity.MetaAhorroId, entity.Monto,
            new(usuario.Id, usuario.Nombre), entity.Fecha, entity.CreadoEn, saldo);
    }

    private async Task<Presupuesto> PresupuestoExistente(
        Guid usuarioId, Guid presupuestoId, bool soloLectura, CancellationToken ct) =>
        await finanzas.ObtenerPresupuesto(usuarioId, presupuestoId, soloLectura, ct)
            ?? throw new NotFoundException(
                "presupuesto_no_encontrado", "El presupuesto no existe.");

    private async Task<MetaAhorro> MetaAccesible(
        Guid usuarioId, Guid metaId, bool soloLectura, bool requiereEdicion, CancellationToken ct)
    {
        var meta = await finanzas.ObtenerMeta(metaId, soloLectura, ct)
            ?? throw new NotFoundException("meta_no_encontrada", "La meta no existe.");
        if (meta.Ambito == "privado")
        {
            if (meta.UsuarioId != usuarioId)
                throw new NotFoundException("meta_no_encontrada", "La meta no existe.");
            return meta;
        }
        var membresia = await familias.ObtenerMembresia(
            meta.GrupoFamiliarId!.Value, usuarioId, true, ct)
            ?? throw new ForbiddenException(
                "membresia_requerida", "No pertenece al grupo familiar.");
        if (requiereEdicion && membresia.Rol == "integrante" && meta.CreadoPor != usuarioId)
            throw new ForbiddenException(
                "rol_insuficiente", "No puede modificar esta meta familiar.");
        return meta;
    }

    private async Task<IReadOnlyCollection<Categoria>> ObtenerCategoriasPrivadas(
        Guid usuarioId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            throw new DomainException("categorias_requeridas", "Debe seleccionar al menos una categoría.");
        var categorias = await finanzas.ObtenerCategorias(usuarioId, ids.Distinct().ToArray(), ct);
        if (categorias.Count != ids.Distinct().Count())
            throw new NotFoundException(
                "categoria_no_encontrada", "Una o más categorías no existen.");
        if (categorias.Any(x => x.Tipo == "ingreso"))
            throw new DomainException(
                "categoria_invalida", "Los presupuestos sólo admiten categorías de gasto.");
        return categorias;
    }

    private async Task ValidarCuentaMeta(
        Guid usuarioId, string ambito, Guid? grupoId, Guid cuentaId,
        bool soloLectura, CancellationToken ct) =>
        _ = await ObtenerCuentaDelAmbito(
            usuarioId, ambito, grupoId, cuentaId, soloLectura, ct);

    private async Task<Cuenta> ObtenerCuentaDelAmbito(
        Guid usuarioId, string ambito, Guid? grupoId, Guid cuentaId,
        bool soloLectura, CancellationToken ct)
    {
        if (ambito == "privado")
        {
            if (grupoId is not null)
                throw new DomainException("propietario_invalido", "Una meta privada no tiene grupo familiar.");
            return await finanzas.ObtenerCuenta(usuarioId, cuentaId, soloLectura, ct)
                ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        }
        if (ambito != "familiar" || grupoId is null)
            throw new DomainException("ambito_invalido", "El ámbito de la meta no es válido.");
        if (!await familias.UsuarioEsIntegrante(grupoId.Value, usuarioId, ct))
            throw new ForbiddenException(
                "membresia_requerida", "No pertenece al grupo familiar.");
        var compartida = await familias.ObtenerCuentaCompartida(
            grupoId.Value, cuentaId, true, ct)
            ?? throw new NotFoundException(
                "cuenta_no_compartida", "La cuenta no está compartida con el grupo.");
        return await finanzas.ObtenerCuenta(
            compartida.CompartidaPorUsuarioId, cuentaId, soloLectura, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
    }

    private static void ValidarPresupuestoPrivado(string ambito, Guid? grupoFamiliarId)
    {
        if (ambito != "privado" || grupoFamiliarId is not null)
            throw new DomainException(
                "ambito_invalido",
                "Estas rutas administran presupuestos privados; use las rutas familiares para ese ámbito.");
    }

    private static void VerificarVersion(long actual, long esperada)
    {
        if (actual != esperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión del recurso está desactualizada.");
    }

    private static void ValidarLimite(long limite)
    {
        if (limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");
    }

    private static T[] Paginar<T>(
        T[] elementos, string? cursor, long limite,
        out string? siguiente, out bool hayMas) where T : FinanzasInteligentes.BuildingBlocks.Entity
    {
        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!Guid.TryParse(cursor, out var id))
                throw new DomainException("cursor_invalido", "El cursor no es válido.");
            cursorId = id;
        }
        var pagina = elementos.Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) > 0)
            .OrderBy(x => x.Id).Take(checked((int)limite + 1)).ToArray();
        hayMas = pagina.Length > limite;
        var seleccionados = pagina.Take(checked((int)limite)).ToArray();
        siguiente = hayMas ? seleccionados[^1].Id.ToString() : null;
        return seleccionados;
    }

    private static (DateOnly Desde, DateOnly Hasta) ResolverRango(
        string periodo, DateOnly? desde, DateOnly? hasta)
    {
        if (desde is not null || hasta is not null)
        {
            var inicio = desde ?? DateOnly.MinValue;
            var fin = hasta ?? DateOnly.MaxValue;
            if (inicio > fin)
                throw new DomainException("rango_invalido", "El rango de fechas no es válido.");
            return (inicio, fin);
        }
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return periodo switch
        {
            "semanal" => (
                hoy.AddDays(-(((int)hoy.DayOfWeek + 6) % 7)),
                hoy.AddDays(6 - (((int)hoy.DayOfWeek + 6) % 7))),
            "mensual" => (
                new DateOnly(hoy.Year, hoy.Month, 1),
                new DateOnly(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month))),
            "anual" => (
                new DateOnly(hoy.Year, 1, 1),
                new DateOnly(hoy.Year, 12, 31)),
            _ => throw new DomainException("periodo_invalido", "El periodo no es válido.")
        };
    }
}