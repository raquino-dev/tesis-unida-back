using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.FinanzasPersonales;

public sealed class Transferencia : MutableEntity
{
    private Transferencia()
    { }

    public Guid UsuarioId { get; private set; }
    public Guid CuentaOrigenId { get; private set; }
    public Guid CuentaDestinoId { get; private set; }
    public long Monto { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public string Descripcion { get; private set; } = string.Empty;
    public DateOnly Fecha { get; private set; }
    public Guid? MovimientoEgresoId { get; private set; }
    public Guid? MovimientoIngresoId { get; private set; }
    public string Estado { get; private set; } = "procesando";
    public DateTimeOffset? AnuladaEn { get; private set; }
    public string HashIdempotencia { get; private set; } = string.Empty;

    public static Transferencia Iniciar(
        Guid usuarioId,
        Guid cuentaOrigenId,
        Guid cuentaDestinoId,
        long monto,
        DateOnly fecha,
        string descripcion,
        string hashIdempotencia)
    {
        if (cuentaOrigenId == cuentaDestinoId)
            throw new DomainException(
                "misma_cuenta", "Las cuentas de origen y destino deben ser distintas.");
        if (monto <= 0)
            throw new DomainException("monto_invalido", "El monto debe ser positivo.");
        if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Trim().Length > 300)
            throw new DomainException("descripcion_invalida", "La descripción no es válida.");
        if (hashIdempotencia.Length != 64)
            throw new DomainException(
                "idempotencia_invalida", "La clave de idempotencia no es válida.");
        return new()
        {
            UsuarioId = usuarioId,
            CuentaOrigenId = cuentaOrigenId,
            CuentaDestinoId = cuentaDestinoId,
            Monto = monto,
            Fecha = fecha,
            Descripcion = descripcion.Trim(),
            HashIdempotencia = hashIdempotencia
        };
    }

    public void Confirmar(Guid movimientoEgresoId, Guid movimientoIngresoId)
    {
        if (Estado != "procesando")
            throw new DomainException(
                "estado_incompatible", "La transferencia no puede confirmarse.");
        if (movimientoEgresoId == movimientoIngresoId)
            throw new DomainException(
                "movimientos_invalidos", "Los movimientos deben ser distintos.");
        MovimientoEgresoId = movimientoEgresoId;
        MovimientoIngresoId = movimientoIngresoId;
        Estado = "confirmada";
    }

    public void Anular()
    {
        if (Estado != "confirmada")
            throw new DomainException(
                "estado_incompatible", "Sólo una transferencia confirmada puede anularse.");
        Estado = "anulada";
        AnuladaEn = DateTimeOffset.UtcNow;
        Touch();
    }
}