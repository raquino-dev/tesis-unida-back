using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;
using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.TarjetasCredito;

public sealed record ListarTarjetasCreditoQuery(Guid UsuarioId, string? Cursor, long Limite);
public sealed record ObtenerTarjetaCreditoQuery(Guid UsuarioId, Guid TarjetaId);
public sealed record CrearTarjetaCreditoCommand(
    Guid UsuarioId,
    string CorrelationId,
    string Alias,
    Guid CuentaPagoId,
    long DiaCierre,
    long DiaVencimiento,
    long LimiteCredito,
    string Moneda,
    string Color,
    Guid? Id = null);
public sealed record ActualizarTarjetaCreditoCommand(
    Guid UsuarioId,
    Guid TarjetaId,
    long VersionEsperada,
    string? Alias,
    Guid? CuentaPagoId,
    long? DiaCierre,
    long? DiaVencimiento,
    long? LimiteCredito,
    string? Moneda,
    string? Color);
public sealed record EliminarTarjetaCreditoCommand(
    Guid UsuarioId, Guid TarjetaId, long VersionEsperada);

public sealed class ListarTarjetasCreditoHandler(IFinanzasRepository finanzas)
{
    public async Task<PaginaTarjetaCreditoResponse> Handle(
        ListarTarjetasCreditoQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");

        Guid? cursorId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!Guid.TryParse(query.Cursor, out var id))
                throw new DomainException("cursor_invalido", "El cursor no es válido.");
            cursorId = id;
        }

        var tarjetas = await finanzas.ListarTarjetasCredito(query.UsuarioId, cancellationToken);
        var pagina = tarjetas
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) > 0)
            .Take(checked((int)query.Limite + 1))
            .ToArray();
        var tieneSiguiente = pagina.Length > query.Limite;
        var seleccionadas = pagina.Take(checked((int)query.Limite)).ToArray();
        var respuestas = new List<TarjetaCreditoResponse>(seleccionadas.Length);

        foreach (var tarjeta in seleccionadas)
        {
            var cuenta = await finanzas.ObtenerCuenta(
                query.UsuarioId, tarjeta.CuentaPagoId, true, cancellationToken)
                ?? throw new NotFoundException(
                    "cuenta_pago_no_encontrada", "La cuenta de pago no existe.");
            respuestas.Add(tarjeta.ToResponse(cuenta));
        }

        return new(
            respuestas,
            new(tieneSiguiente ? seleccionadas[^1].Id.ToString() : null));
    }
}

public sealed class ObtenerTarjetaCreditoHandler(IFinanzasRepository finanzas)
{
    public async Task<TarjetaCreditoResponse> Handle(
        ObtenerTarjetaCreditoQuery query,
        CancellationToken cancellationToken)
    {
        var tarjeta = await finanzas.ObtenerTarjetaCredito(
            query.UsuarioId, query.TarjetaId, true, cancellationToken)
            ?? throw new NotFoundException("tarjeta_no_encontrada", "La tarjeta no existe.");
        var cuenta = await ObtenerCuentaPago(
            finanzas, query.UsuarioId, tarjeta.CuentaPagoId, cancellationToken);
        return tarjeta.ToResponse(cuenta);
    }

    internal static async Task<Cuenta> ObtenerCuentaPago(
        IFinanzasRepository finanzas,
        Guid usuarioId,
        Guid cuentaPagoId,
        CancellationToken cancellationToken) =>
        await finanzas.ObtenerCuenta(usuarioId, cuentaPagoId, true, cancellationToken)
            ?? throw new NotFoundException(
                "cuenta_pago_no_encontrada", "La cuenta de pago no existe.");
}

public sealed class CrearTarjetaCreditoHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<TarjetaCreditoResponse> Handle(
        CrearTarjetaCreditoCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Id is not null)
        {
            var existente = await finanzas.ObtenerTarjetaCredito(
                command.UsuarioId, command.Id.Value, true, cancellationToken);
            if (existente is not null)
            {
                var cuentaExistente = await ObtenerTarjetaCreditoHandler.ObtenerCuentaPago(
                    finanzas, command.UsuarioId, existente.CuentaPagoId, cancellationToken);
                return existente.ToResponse(cuentaExistente);
            }
        }
        var cuenta = await ObtenerTarjetaCreditoHandler.ObtenerCuentaPago(
            finanzas, command.UsuarioId, command.CuentaPagoId, cancellationToken);
        if (await finanzas.ExisteTarjetaCreditoConAlias(
            command.UsuarioId, command.Alias.Trim(), null, cancellationToken))
            throw new ConflictException(
                "tarjeta_duplicada", "Ya existe una tarjeta con ese alias.");

        var tarjeta = TarjetaCredito.Crear(
            command.UsuarioId, command.Alias,
            command.CuentaPagoId, command.DiaCierre, command.DiaVencimiento,
            command.LimiteCredito, command.Moneda, command.Color, command.Id);
        finanzas.Agregar(tarjeta);
        finanzas.Agregar(EventoOutbox.Crear(
            "tarjeta-credito.creada", "tarjeta-credito", tarjeta.Id,
            new { tarjeta.Id }, command.CorrelationId));
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return tarjeta.ToResponse(cuenta);
    }
}

public sealed class ActualizarTarjetaCreditoHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<TarjetaCreditoResponse> Handle(
        ActualizarTarjetaCreditoCommand command,
        CancellationToken cancellationToken)
    {
        var tarjeta = await finanzas.ObtenerTarjetaCredito(
            command.UsuarioId, command.TarjetaId, false, cancellationToken)
            ?? throw new NotFoundException("tarjeta_no_encontrada", "La tarjeta no existe.");
        if (tarjeta.Version != command.VersionEsperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión de la tarjeta está desactualizada.");
        if (command.Alias is not null &&
            await finanzas.ExisteTarjetaCreditoConAlias(
                command.UsuarioId, command.Alias.Trim(), tarjeta.Id, cancellationToken))
            throw new ConflictException(
                "tarjeta_duplicada", "Ya existe una tarjeta con ese alias.");

        var cuentaId = command.CuentaPagoId ?? tarjeta.CuentaPagoId;
        var cuenta = await ObtenerTarjetaCreditoHandler.ObtenerCuentaPago(
            finanzas, command.UsuarioId, cuentaId, cancellationToken);
        tarjeta.Actualizar(
            command.Alias, command.CuentaPagoId,
            command.DiaCierre, command.DiaVencimiento, command.LimiteCredito,
            command.Moneda, command.Color);
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
        return tarjeta.ToResponse(cuenta);
    }
}

public sealed class EliminarTarjetaCreditoHandler(
    IFinanzasRepository finanzas,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task Handle(
        EliminarTarjetaCreditoCommand command,
        CancellationToken cancellationToken)
    {
        var tarjeta = await finanzas.ObtenerTarjetaCredito(
            command.UsuarioId, command.TarjetaId, false, cancellationToken)
            ?? throw new NotFoundException("tarjeta_no_encontrada", "La tarjeta no existe.");
        if (tarjeta.Version != command.VersionEsperada)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión de la tarjeta está desactualizada.");
        tarjeta.Eliminar();
        await unidadDeTrabajo.GuardarCambios(cancellationToken);
    }
}
