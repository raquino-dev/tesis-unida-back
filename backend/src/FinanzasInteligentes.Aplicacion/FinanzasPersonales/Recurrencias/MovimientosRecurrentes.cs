using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Recurrencias;

public sealed record CategoriaRecurrenteResponse(Guid Id, string Nombre);
public sealed record CuentaRecurrenteResponse(Guid Id, string Nombre);
public sealed record MovimientoRecurrenteResponse(
    Guid Id,
    string Tipo,
    long Monto,
    IReadOnlyCollection<CategoriaRecurrenteResponse> Categorias,
    CuentaRecurrenteResponse Cuenta,
    string Descripcion,
    DateOnly FechaInicio,
    DateOnly? FechaFin,
    string Frecuencia,
    long? CantidadOcurrencias,
    long OcurrenciasCompletadas,
    DateOnly ProximaEjecucion,
    string Estado,
    long Version);
public sealed record PaginacionRecurrenteResponse(
    string? SiguienteCursor, bool HayMas, long Limite);
public sealed record PaginaMovimientoRecurrenteResponse(
    IReadOnlyCollection<MovimientoRecurrenteResponse> Datos,
    PaginacionRecurrenteResponse Paginacion);

public sealed class MovimientosRecurrentesHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<PaginaMovimientoRecurrenteResponse> Listar(
        Guid usuarioId, string? cursor, long limite,
        string? estado, string? tipo, CancellationToken ct)
    {
        ValidarLimite(limite);
        IEnumerable<MovimientoRecurrente> query =
            await finanzas.ListarMovimientosRecurrentes(usuarioId, ct);
        if (estado is not null) query = query.Where(x => x.Estado == estado);
        if (tipo is not null) query = query.Where(x => x.Tipo == tipo);
        var pagina = Paginar(query.ToArray(), cursor, limite, out var siguiente, out var hayMas);
        var respuestas = new List<MovimientoRecurrenteResponse>();
        foreach (var entity in pagina)
            respuestas.Add(await Map(entity, ct));
        return new(respuestas, new(siguiente, hayMas, limite));
    }

    public async Task<MovimientoRecurrenteResponse> Crear(
        Guid usuarioId, Guid cuentaId, string tipo, long monto,
        IReadOnlyCollection<Guid> categoriaIds, string descripcion,
        DateOnly fechaInicio, DateOnly? fechaFin, string frecuencia,
        long? cantidadOcurrencias, string correlationId, Guid? id,
        CancellationToken ct)
    {
        if (id is { } requestedId &&
            await finanzas.ObtenerMovimientoRecurrente(
                usuarioId, requestedId, true, ct) is { } existing)
            return await Map(existing, ct);

        var cuenta = await finanzas.ObtenerCuenta(usuarioId, cuentaId, true, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        var categorias = await ObtenerCategorias(usuarioId, categoriaIds, tipo, ct);
        if (await finanzas.ExisteMovimientoRecurrenteDuplicado(
            usuarioId, cuentaId, descripcion.Trim(), null, ct))
            throw new ConflictException(
                "recurrencia_duplicada",
                "Ya existe una recurrencia activa con la misma cuenta y descripción.");

        var entity = MovimientoRecurrente.Crear(
            usuarioId, cuentaId, tipo, monto, descripcion, fechaInicio,
            fechaFin, frecuencia, cantidadOcurrencias, categorias, id);
        finanzas.Agregar(entity);
        finanzas.Agregar(EventoOutbox.Crear(
            "movimiento-recurrente.creado", "movimiento-recurrente",
            entity.Id, new { entity.Id, entity.ProximaEjecucion }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(entity, cuenta);
    }

    public async Task<MovimientoRecurrenteResponse> Obtener(
        Guid usuarioId, Guid recurrenteId, CancellationToken ct)
    {
        var entity = await Existente(usuarioId, recurrenteId, true, ct);
        return await Map(entity, ct);
    }

    public async Task<MovimientoRecurrenteResponse> Actualizar(
        Guid usuarioId, Guid recurrenteId, long version,
        Guid? cuentaId, string? tipo, long? monto,
        IReadOnlyCollection<Guid>? categoriaIds, string? descripcion,
        DateOnly? fechaInicio, DateOnly? fechaFin, bool fechaFinEspecificada,
        string? frecuencia, long? cantidadOcurrencias,
        bool cantidadOcurrenciasEspecificada, string? estado, CancellationToken ct)
    {
        var entity = await Existente(usuarioId, recurrenteId, false, ct);
        VerificarVersion(entity.Version, version);
        var cuentaFinalId = cuentaId ?? entity.CuentaId;
        var cuenta = await finanzas.ObtenerCuenta(usuarioId, cuentaFinalId, true, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        IReadOnlyCollection<Categoria>? categorias = null;
        if (categoriaIds is not null)
            categorias = await ObtenerCategorias(
                usuarioId, categoriaIds, tipo ?? entity.Tipo, ct);
        else if (tipo is not null && entity.Categorias.Any(
            x => x.Tipo != tipo && x.Tipo != "ambos"))
            throw new DomainException(
                "categoria_incompatible",
                "Debe reemplazar las categorías al cambiar el tipo.");

        var descripcionFinal = descripcion ?? entity.Descripcion;
        if (await finanzas.ExisteMovimientoRecurrenteDuplicado(
            usuarioId, cuentaFinalId, descripcionFinal.Trim(), entity.Id, ct))
            throw new ConflictException(
                "recurrencia_duplicada",
                "Ya existe una recurrencia activa con la misma cuenta y descripción.");

        var hayCambiosDeContenido =
            cuentaId is not null || tipo is not null || monto is not null ||
            categoriaIds is not null || descripcion is not null || fechaInicio is not null ||
            fechaFinEspecificada || frecuencia is not null || cantidadOcurrenciasEspecificada;
        if (hayCambiosDeContenido)
            entity.Actualizar(
                cuentaId, tipo, monto, descripcion, fechaInicio,
                fechaFin, fechaFinEspecificada, frecuencia,
                cantidadOcurrencias, cantidadOcurrenciasEspecificada, categorias);
        if (estado is not null)
            entity.CambiarEstado(estado);
        if (!hayCambiosDeContenido && estado is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(entity, cuenta);
    }

    public async Task Eliminar(
        Guid usuarioId, Guid recurrenteId, long version, CancellationToken ct)
    {
        var entity = await Existente(usuarioId, recurrenteId, false, ct);
        VerificarVersion(entity.Version, version);
        entity.Eliminar();
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    private async Task<MovimientoRecurrenteResponse> Map(
        MovimientoRecurrente entity, CancellationToken ct)
    {
        var cuenta = await finanzas.ObtenerCuenta(
            entity.UsuarioId, entity.CuentaId, true, ct)
            ?? throw new NotFoundException("cuenta_no_encontrada", "La cuenta no existe.");
        return Map(entity, cuenta);
    }

    private static MovimientoRecurrenteResponse Map(
        MovimientoRecurrente entity, Cuenta cuenta) =>
        new(
            entity.Id, entity.Tipo, entity.Monto,
            entity.Categorias.Select(x => new CategoriaRecurrenteResponse(x.Id, x.Nombre)).ToArray(),
            new(cuenta.Id, cuenta.Nombre), entity.Descripcion,
            entity.FechaInicio, entity.FechaFin, entity.Frecuencia,
            entity.CantidadOcurrencias, entity.OcurrenciasCompletadas,
            entity.ProximaEjecucion, entity.Estado, entity.Version);

    private async Task<IReadOnlyCollection<Categoria>> ObtenerCategorias(
        Guid usuarioId, IReadOnlyCollection<Guid> ids, string tipo, CancellationToken ct)
    {
        if (ids.Count == 0)
            throw new DomainException(
                "categorias_requeridas", "Debe seleccionar al menos una categoría.");
        var distintos = ids.Distinct().ToArray();
        var categorias = await finanzas.ObtenerCategorias(usuarioId, distintos, ct);
        if (categorias.Count != distintos.Length)
            throw new NotFoundException(
                "categoria_no_encontrada", "Una o más categorías no existen.");
        if (categorias.Any(x => x.Tipo != tipo && x.Tipo != "ambos"))
            throw new DomainException(
                "categoria_incompatible", "Una categoría no corresponde al tipo del movimiento.");
        return categorias;
    }

    private async Task<MovimientoRecurrente> Existente(
        Guid usuarioId, Guid recurrenteId, bool soloLectura, CancellationToken ct) =>
        await finanzas.ObtenerMovimientoRecurrente(
            usuarioId, recurrenteId, soloLectura, ct)
            ?? throw new NotFoundException(
                "recurrencia_no_encontrada", "El movimiento recurrente no existe.");

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

    private static MovimientoRecurrente[] Paginar(
        MovimientoRecurrente[] elementos, string? cursor, long limite,
        out string? siguiente, out bool hayMas)
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
}
