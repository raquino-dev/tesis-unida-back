using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Aplicacion.Suscripciones.Modelos;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using FinanzasInteligentes.Dominio.Suscripciones;
using System.Security.Cryptography;
using System.Text;

namespace FinanzasInteligentes.Aplicacion.Suscripciones;

public sealed class SuscripcionesHandler(
    ISuscripcionesRepository suscripciones,
    IIdentidadRepository identidad,
    IValidadorComprobanteSuscripcion validador,
    IValidadorNotificacionGooglePlay notificacionesGoogle,
    IUnidadDeTrabajo unidadDeTrabajo)
{
    private static readonly Guid PlanGratisId =
        Guid.Parse("00000000-0000-0000-0000-000000000101");

    public async Task<IReadOnlyCollection<PlanSuscripcionResponse>> ListarPlanes(
        CancellationToken ct) =>
        (await suscripciones.ListarPlanes(ct)).Select(MapPlan).ToArray();

    public async Task<IReadOnlyCollection<TransaccionSuscripcionResponse>> ListarTransacciones(
        Guid usuarioId, CancellationToken ct) =>
        (await suscripciones.ListarTransacciones(usuarioId, ct))
        .Select(x => new TransaccionSuscripcionResponse(
            x.Id, x.SuscripcionId, x.Tipo, x.Estado,
            x.Proveedor, x.Monto, x.Moneda, x.OcurridoEn))
        .ToArray();

    public async Task<SuscripcionResponse> Obtener(Guid usuarioId, CancellationToken ct)
    {
        var vigente = await suscripciones.ObtenerVigente(usuarioId, true, ct);
        if (vigente is not null) return await Map(vigente, ct);

        var usuario = await identidad.ObtenerUsuario(usuarioId, true, ct)
            ?? throw new NotFoundException("usuario_no_encontrado", "El usuario no existe.");
        var plan = await suscripciones.ObtenerPlan(PlanGratisId, ct)
            ?? throw new InvalidOperationException("No está configurado el plan gratuito.");
        return new(
            usuario.Id, "activa", new(plan.Id, plan.Codigo, plan.Nombre),
            usuario.CreadoEn, null, usuario.CreadoEn.AddYears(100),
            plan.Capacidades, usuario.Version);
    }

    public async Task<SuscripcionResponse> Crear(
        Guid usuarioId,
        string planCodigo,
        string proveedor,
        string comprobante,
        Guid verificacionOtpId,
        string correlationId,
        CancellationToken ct)
    {
        var plan = await suscripciones.ObtenerPlanPorCodigo(planCodigo, ct)
            ?? throw new NotFoundException("plan_no_encontrado", "El plan no existe.");
        if (plan.Codigo == "gratis")
            throw new ConflictException("plan_gratuito", "El plan gratuito no requiere suscripción.");
        var validado = await validador.Validar(proveedor, comprobante, plan.Codigo, ct);
        var repetida = await suscripciones.ObtenerPorComprobante(
            validado.Proveedor, validado.HashComprobante, true, ct);
        if (repetida is not null)
        {
            if (repetida.UsuarioId != usuarioId)
                throw new ConflictException(
                    "comprobante_utilizado", "El comprobante ya fue utilizado.");
            return await Map(repetida, ct);
        }
        if (await suscripciones.ObtenerVigente(usuarioId, true, ct) is not null)
            throw new ConflictException("suscripcion_ya_activa", "Ya existe una suscripción vigente.");
        if (!await identidad.ConsumirVerificacionOtp(
            usuarioId, verificacionOtpId, "operacion-sensible", ct))
            throw new ForbiddenException(
                "otp_requerido", "Se requiere una verificación OTP válida.");

        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var entity = Suscripcion.Crear(
            usuarioId, plan.Id, validado.Proveedor, validado.HashComprobante,
            validado.FinPeriodoEn ?? CalcularFinPeriodo(DateTimeOffset.UtcNow, plan.Periodo));
        suscripciones.Agregar(entity);
        suscripciones.Agregar(TransaccionSuscripcion.Crear(
            usuarioId, entity.Id, "compra", "confirmada", validado.Proveedor,
            ReferenciaOperacion(validado.HashComprobante, "compra", entity.FinPeriodoEn),
            plan.Precio, plan.Moneda));
        identidad.Agregar(EventoOutbox.Crear(
            "suscripcion.activada", "suscripcion", entity.Id,
            new { entity.Id, entity.UsuarioId, plan.Codigo }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
        return Map(entity, plan);
    }

    public async Task<SuscripcionResponse> Cancelar(
        Guid usuarioId,
        long version,
        string? motivo,
        string correlationId,
        CancellationToken ct)
    {
        var entity = await suscripciones.ObtenerVigente(usuarioId, false, ct)
            ?? throw new NotFoundException("suscripcion_no_encontrada", "No existe una suscripción vigente.");
        if (entity.Version != version)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión de la suscripción está desactualizada.");
        entity.Cancelar(motivo);
        var plan = await suscripciones.ObtenerPlan(entity.PlanId, ct)
            ?? throw new NotFoundException("plan_no_encontrado", "El plan no existe.");
        suscripciones.Agregar(TransaccionSuscripcion.Crear(
            usuarioId, entity.Id, "cancelacion", "confirmada", entity.Proveedor,
            ReferenciaOperacion(entity.HashComprobante, "cancelacion", entity.FinPeriodoEn),
            0, plan.Moneda));
        identidad.Agregar(EventoOutbox.Crear(
            "suscripcion.cancelada", "suscripcion", entity.Id,
            new { entity.Id, entity.UsuarioId, entity.FinPeriodoEn }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return await Map(entity, ct);
    }

    public async Task<SuscripcionResponse> Restaurar(
        Guid usuarioId,
        string proveedor,
        string comprobante,
        string correlationId,
        CancellationToken ct)
    {
        var validado = await validador.Validar(proveedor, comprobante, null, ct);
        var entity = await suscripciones.ObtenerPorComprobante(
            validado.Proveedor, validado.HashComprobante, false, ct)
            ?? throw new NotFoundException(
                "comprobante_no_encontrado", "No existe una compra asociada al comprobante.");
        if (entity.UsuarioId != usuarioId)
            throw new ForbiddenException(
                "comprobante_no_pertenece", "El comprobante pertenece a otro usuario.");
        if (entity.Estado == "activa")
            throw new ConflictException("suscripcion_ya_activa", "La suscripción ya está activa.");
        var otra = await suscripciones.ObtenerVigente(usuarioId, true, ct);
        if (otra is not null && otra.Id != entity.Id)
            throw new ConflictException("suscripcion_ya_activa", "Ya existe otra suscripción vigente.");
        var plan = await suscripciones.ObtenerPlan(entity.PlanId, ct)
            ?? throw new NotFoundException("plan_no_encontrado", "El plan no existe.");
        var fin = validado.FinPeriodoEn ??
            (entity.FinPeriodoEn > DateTimeOffset.UtcNow
                ? entity.FinPeriodoEn
                : CalcularFinPeriodo(DateTimeOffset.UtcNow, plan.Periodo));
        entity.Restaurar(fin);
        suscripciones.Agregar(TransaccionSuscripcion.Crear(
            usuarioId, entity.Id, "restauracion", "confirmada", entity.Proveedor,
            ReferenciaOperacion(entity.HashComprobante, "restauracion", fin),
            0, plan.Moneda));
        identidad.Agregar(EventoOutbox.Crear(
            "suscripcion.restaurada", "suscripcion", entity.Id,
            new { entity.Id, entity.UsuarioId }, correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(entity, plan);
    }

    public async Task<SuscripcionResponse> CambiarPlan(
        Guid usuarioId, long version, string planCodigo, string proveedor,
        string comprobante, Guid verificacionOtpId, string correlationId, CancellationToken ct)
    {
        var actual = await suscripciones.ObtenerVigente(usuarioId, false, ct)
            ?? throw new NotFoundException("suscripcion_no_encontrada", "No existe una suscripción vigente.");
        if (actual.Version != version)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión de la suscripción está desactualizada.");
        var plan = await suscripciones.ObtenerPlanPorCodigo(planCodigo, ct)
            ?? throw new NotFoundException("plan_no_encontrado", "El plan no existe.");
        if (plan.Id == actual.PlanId)
            throw new ConflictException("plan_sin_cambios", "La suscripción ya utiliza ese plan.");
        if (!await identidad.ConsumirVerificacionOtp(
            usuarioId, verificacionOtpId, "operacion-sensible", ct))
            throw new ForbiddenException("otp_requerido", "Se requiere una verificación OTP válida.");
        var validado = await validador.Validar(proveedor, comprobante, plan.Codigo, ct);
        if (await suscripciones.ObtenerPorComprobante(
            validado.Proveedor, validado.HashComprobante, true, ct) is not null)
            throw new ConflictException("comprobante_utilizado", "El comprobante ya fue utilizado.");

        await using var tx = await unidadDeTrabajo.IniciarTransaccion(ct);
        var nueva = Suscripcion.Crear(
            usuarioId, plan.Id, validado.Proveedor, validado.HashComprobante,
            validado.FinPeriodoEn ?? CalcularFinPeriodo(DateTimeOffset.UtcNow, plan.Periodo));
        nueva.VincularAnterior(actual.Id);
        actual.ReemplazarPor(nueva.Id);
        suscripciones.Agregar(nueva);
        suscripciones.Agregar(TransaccionSuscripcion.Crear(
            usuarioId, nueva.Id, "cambio-plan", "confirmada", validado.Proveedor,
            ReferenciaOperacion(validado.HashComprobante, "cambio-plan", nueva.FinPeriodoEn),
            plan.Precio, plan.Moneda));
        identidad.Agregar(EventoOutbox.Crear(
            "suscripcion.plan-cambiado", "suscripcion", nueva.Id,
            new { nueva.Id, nueva.UsuarioId, PlanAnteriorId = actual.PlanId, PlanNuevoId = plan.Id },
            correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
        await tx.Confirmar(ct);
        return Map(nueva, plan);
    }

    public async Task ProcesarNotificacionGooglePlay(
        string tokenAutorizacion, string dataBase64, string correlationId, CancellationToken ct)
    {
        var notification = await notificacionesGoogle.Validar(
            tokenAutorizacion, dataBase64, ct);
        if (notification is null) return;
        if (await suscripciones.ExisteTransaccion(
            "google-play", notification.ReferenciaHash, "rtdn", ct))
            return;

        var validado = await validador.Validar(
            "google-play", notification.PurchaseToken, null, ct, requerirVigente: false);
        var entity = await suscripciones.ObtenerPorComprobante(
            "google-play", validado.HashComprobante, false, ct);
        if (entity is null) return;
        var plan = await suscripciones.ObtenerPlan(entity.PlanId, ct)
            ?? throw new NotFoundException("plan_no_encontrado", "El plan no existe.");
        entity.SincronizarProveedor(
            validado.Estado ?? "pendiente",
            validado.FinPeriodoEn ?? entity.FinPeriodoEn);
        suscripciones.Agregar(TransaccionSuscripcion.Crear(
            entity.UsuarioId, entity.Id, "rtdn", "confirmada", "google-play",
            notification.ReferenciaHash,
            notification.Tipo is 2 or 4 ? plan.Precio : 0,
            plan.Moneda));
        identidad.Agregar(EventoOutbox.Crear(
            "suscripcion.rtdn-procesada", "suscripcion", entity.Id,
            new { entity.Id, entity.UsuarioId, notification.Tipo, entity.Estado, entity.FinPeriodoEn },
            correlationId));
        await unidadDeTrabajo.GuardarCambios(ct);
    }

    private async Task<SuscripcionResponse> Map(Suscripcion entity, CancellationToken ct)
    {
        var plan = await suscripciones.ObtenerPlan(entity.PlanId, ct)
            ?? throw new NotFoundException("plan_no_encontrado", "El plan no existe.");
        return Map(entity, plan);
    }

    private static SuscripcionResponse Map(Suscripcion entity, PlanSuscripcion plan) =>
        new(
            entity.Id, entity.Estado, new(plan.Id, plan.Codigo, plan.Nombre),
            entity.IniciadaEn, entity.CanceladaEn, entity.FinPeriodoEn,
            plan.Capacidades, entity.Version);

    private static PlanSuscripcionResponse MapPlan(PlanSuscripcion plan) =>
        new(
            plan.Id, plan.Codigo, plan.Nombre, plan.Precio, plan.Moneda,
            plan.Periodo, plan.Capacidades, plan.Destacado);

    private static DateTimeOffset CalcularFinPeriodo(DateTimeOffset inicio, string periodo) =>
        periodo switch
        {
            "mensual" => inicio.AddMonths(1),
            "anual" => inicio.AddYears(1),
            _ => throw new InvalidOperationException($"Periodicidad no soportada: {periodo}.")
        };

    private static string ReferenciaOperacion(
        string comprobanteHash, string tipo, DateTimeOffset periodo) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{comprobanteHash}:{tipo}:{periodo:O}")));
}
