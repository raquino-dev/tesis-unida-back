using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.FinanzasPersonales;

public sealed class Movimiento : Entity
{
    private Movimiento()
    { }

    public Guid UsuarioId { get; private set; }
    public Guid CuentaId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public long Monto { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public string Descripcion { get; private set; } = string.Empty;
    public DateOnly Fecha { get; private set; }
    public string Estado { get; private set; } = "confirmado";
    public string Origen { get; private set; } = "manual";
    public DateTimeOffset? AnuladoEn { get; private set; }
    public string? MotivoAnulacion { get; private set; }
    public Guid? RecurrenciaId { get; private set; }
    public DateOnly? PeriodoRecurrencia { get; private set; }
    public Guid? TransferenciaId { get; private set; }
    public Guid? DocumentoId { get; private set; }
    public long Version { get; private set; } = 1;
    public ICollection<Categoria> Categorias { get; private set; } = new List<Categoria>();

    public static Movimiento Crear(
        Guid usuarioId,
        Guid cuentaId,
        string tipo,
        long monto,
        string descripcion,
        DateOnly fecha,
        string origen = "manual",
        Guid? recurrenciaId = null,
        DateOnly? periodoRecurrencia = null,
        Guid? transferenciaId = null,
        Guid? documentoId = null,
        Guid? id = null)
    {
        if (monto <= 0) throw new DomainException("monto_invalido", "El monto debe ser positivo.");
        if (tipo is not ("ingreso" or "gasto"))
            throw new DomainException("tipo_movimiento_invalido", "El tipo debe ser ingreso o gasto.");
        if (origen is not ("manual" or "documento" or "recurrencia" or "transferencia" or "meta" or "caja"))
            throw new DomainException("origen_movimiento_invalido", "El origen del movimiento no es válido.");
        if ((recurrenciaId is null) != (periodoRecurrencia is null))
            throw new DomainException(
                "recurrencia_invalida", "La recurrencia y su periodo deben informarse juntos.");
        if (origen == "recurrencia" && recurrenciaId is null)
            throw new DomainException(
                "recurrencia_invalida", "Un movimiento recurrente requiere su recurrencia.");
        if (origen == "transferencia" && transferenciaId is null)
            throw new DomainException(
                "transferencia_invalida", "El movimiento requiere su transferencia.");
        if (transferenciaId is not null && recurrenciaId is not null)
            throw new DomainException(
                "origen_ambiguo", "Un movimiento no puede pertenecer a dos operaciones.");

        return new Movimiento
        {
            Id = id ?? Guid.CreateVersion7(),
            UsuarioId = usuarioId,
            CuentaId = cuentaId,
            Tipo = tipo,
            Monto = monto,
            Descripcion = descripcion.Trim(),
            Fecha = fecha,
            Origen = origen,
            RecurrenciaId = recurrenciaId,
            PeriodoRecurrencia = periodoRecurrencia,
            TransferenciaId = transferenciaId,
            DocumentoId = documentoId
        };
    }

    public void Anular(string motivo)
    {
        if (Estado == "anulado") throw new DomainException("movimiento_anulado", "El movimiento ya fue anulado.");
        Estado = "anulado";
        AnuladoEn = DateTimeOffset.UtcNow;
        MotivoAnulacion = motivo.Trim();
        Version = checked(Version + 1);
    }

    public void AsignarCategoriasIniciales(IReadOnlyCollection<Categoria> categorias)
    {
        Categorias.Clear();
        foreach (var categoria in categorias.DistinctBy(x => x.Id))
            Categorias.Add(categoria);
    }

    public void Actualizar(
        string? descripcion,
        IReadOnlyCollection<Categoria>? categorias,
        Guid? documentoId = null)
    {
        if (descripcion is null && categorias is null && documentoId is null)
            throw new DomainException(
                "actualizacion_vacia", "Debe indicar al menos un campo para actualizar.");
        if (Estado == "anulado")
            throw new DomainException(
                "movimiento_anulado", "Un movimiento anulado no puede modificarse.");

        if (descripcion is not null)
        {
            if (descripcion.Length > 300)
                throw new DomainException(
                    "descripcion_invalida", "La descripción supera los 300 caracteres.");
            Descripcion = descripcion.Trim();
        }

        if (categorias is not null)
        {
            Categorias.Clear();
            foreach (var categoria in categorias.DistinctBy(x => x.Id))
                Categorias.Add(categoria);
        }

        if (documentoId is not null)
            DocumentoId = documentoId;

        Version = checked(Version + 1);
    }
}
