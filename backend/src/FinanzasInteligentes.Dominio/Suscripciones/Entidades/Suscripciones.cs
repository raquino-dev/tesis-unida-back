using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.Suscripciones;

public sealed class PlanSuscripcion : Entity
{
    private PlanSuscripcion()
    { }

    public string Codigo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;
    public long Precio { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public string Periodo { get; private set; } = "mensual";
    public string[] Capacidades { get; private set; } = [];
    public bool Destacado { get; private set; }
    public bool Activo { get; private set; } = true;
}

public sealed class Suscripcion : MutableEntity
{
    private Suscripcion()
    { }

    public Guid UsuarioId { get; private set; }
    public Guid PlanId { get; private set; }
    public string Proveedor { get; private set; } = string.Empty;
    public string HashComprobante { get; private set; } = string.Empty;
    public string Estado { get; private set; } = "activa";
    public DateTimeOffset IniciadaEn { get; private set; }
    public DateTimeOffset? CanceladaEn { get; private set; }
    public DateTimeOffset FinPeriodoEn { get; private set; }
    public string? MotivoCancelacion { get; private set; }
    public Guid? SuscripcionAnteriorId { get; private set; }
    public Guid? ReemplazadaPorId { get; private set; }

    public static Suscripcion Crear(
        Guid usuarioId,
        Guid planId,
        string proveedor,
        string hashComprobante,
        DateTimeOffset finPeriodoEn)
    {
        var ahora = DateTimeOffset.UtcNow;
        if (usuarioId == Guid.Empty || planId == Guid.Empty || finPeriodoEn <= ahora)
            throw new DomainException("suscripcion_invalida", "La suscripción no es válida.");
        if (proveedor is not ("interno" or "google-play" or "app-store"))
            throw new DomainException("proveedor_invalido", "El proveedor no es compatible.");
        return new()
        {
            UsuarioId = usuarioId,
            PlanId = planId,
            Proveedor = proveedor,
            HashComprobante = hashComprobante,
            IniciadaEn = ahora,
            FinPeriodoEn = finPeriodoEn
        };
    }

    public void Cancelar(string? motivo)
    {
        if (Estado != "activa")
            throw new DomainException(
                "suscripcion_no_cancelable", "La suscripción no se puede cancelar.");
        Estado = "cancelada";
        CanceladaEn = DateTimeOffset.UtcNow;
        MotivoCancelacion = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        Touch();
    }

    public void Restaurar(DateTimeOffset finPeriodoEn)
    {
        if (Estado == "activa")
            throw new DomainException("suscripcion_ya_activa", "La suscripción ya está activa.");
        if (finPeriodoEn <= DateTimeOffset.UtcNow)
            throw new DomainException("periodo_invalido", "El periodo restaurado no es válido.");
        Estado = "activa";
        CanceladaEn = null;
        MotivoCancelacion = null;
        FinPeriodoEn = finPeriodoEn;
        Touch();
    }

    public void ReemplazarPor(Guid nuevaSuscripcionId)
    {
        if (Estado is not ("activa" or "en_gracia" or "cancelada"))
            throw new DomainException("suscripcion_no_reemplazable", "La suscripción no se puede reemplazar.");
        Estado = "reemplazada";
        ReemplazadaPorId = nuevaSuscripcionId;
        CanceladaEn = DateTimeOffset.UtcNow;
        MotivoCancelacion = "Cambio de plan";
        Touch();
    }

    public void VincularAnterior(Guid suscripcionAnteriorId)
    {
        if (suscripcionAnteriorId == Guid.Empty || suscripcionAnteriorId == Id)
            throw new DomainException("suscripcion_anterior_invalida", "La suscripción anterior no es válida.");
        SuscripcionAnteriorId = suscripcionAnteriorId;
        Touch();
    }

    public void SincronizarProveedor(string estado, DateTimeOffset finPeriodoEn)
    {
        if (estado is not ("activa" or "en_gracia" or "cancelada" or "expirada" or "pausada"))
            throw new DomainException("estado_suscripcion_invalido", "El estado del proveedor no es válido.");
        if (finPeriodoEn < IniciadaEn)
            throw new DomainException("periodo_invalido", "El periodo informado por el proveedor no es válido.");
        Estado = estado;
        FinPeriodoEn = finPeriodoEn;
        if (estado == "cancelada") CanceladaEn ??= DateTimeOffset.UtcNow;
        Touch();
    }
}

public sealed class TransaccionSuscripcion : Entity
{
    private TransaccionSuscripcion() { }

    public Guid UsuarioId { get; private set; }
    public Guid SuscripcionId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string Estado { get; private set; } = string.Empty;
    public string Proveedor { get; private set; } = string.Empty;
    public string ReferenciaExternaHash { get; private set; } = string.Empty;
    public long Monto { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public DateTimeOffset OcurridoEn { get; private set; }

    public static TransaccionSuscripcion Crear(
        Guid usuarioId, Guid suscripcionId, string tipo, string estado,
        string proveedor, string referenciaHash, long monto, string moneda) =>
        new()
        {
            UsuarioId = usuarioId,
            SuscripcionId = suscripcionId,
            Tipo = tipo,
            Estado = estado,
            Proveedor = proveedor,
            ReferenciaExternaHash = referenciaHash,
            Monto = monto,
            Moneda = moneda,
            OcurridoEn = DateTimeOffset.UtcNow
        };
}

public sealed class AvisoSuscripcion : Entity
{
    private AvisoSuscripcion() { }

    public Guid UsuarioId { get; private set; }
    public Guid SuscripcionId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public DateTimeOffset PeriodoFinEn { get; private set; }
    public DateTimeOffset GeneradoEn { get; private set; }

    public static AvisoSuscripcion Crear(
        Guid usuarioId, Guid suscripcionId, string tipo, DateTimeOffset periodoFinEn) =>
        new()
        {
            UsuarioId = usuarioId,
            SuscripcionId = suscripcionId,
            Tipo = tipo,
            PeriodoFinEn = periodoFinEn,
            GeneradoEn = DateTimeOffset.UtcNow
        };
}
