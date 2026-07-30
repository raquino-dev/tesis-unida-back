using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.FinanzasPersonales;

public sealed class Presupuesto : MutableEntity
{
    private Presupuesto()
    { }

    public Guid UsuarioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public long Monto { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public string Periodo { get; private set; } = "mensual";
    public string Estado { get; private set; } = "activo";
    public DateTimeOffset? EliminadoEn { get; private set; }
    public ICollection<Categoria> Categorias { get; private set; } = new List<Categoria>();

    public static Presupuesto Crear(
        Guid usuarioId,
        string nombre,
        long monto,
        string periodo,
        IReadOnlyCollection<Categoria> categorias)
    {
        Validar(nombre, monto, periodo, categorias);
        var presupuesto = new Presupuesto
        {
            UsuarioId = usuarioId,
            Nombre = nombre.Trim(),
            Monto = monto,
            Periodo = periodo
        };
        presupuesto.ReemplazarCategorias(categorias);
        return presupuesto;
    }

    public void Actualizar(
        string? nombre,
        long? monto,
        string? periodo,
        IReadOnlyCollection<Categoria>? categorias)
    {
        if (nombre is null && monto is null && periodo is null && categorias is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");

        Validar(nombre ?? Nombre, monto ?? Monto, periodo ?? Periodo, categorias ?? Categorias.ToArray());
        if (nombre is not null) Nombre = nombre.Trim();
        if (monto is not null) Monto = monto.Value;
        if (periodo is not null) Periodo = periodo;
        if (categorias is not null) ReemplazarCategorias(categorias);
        Touch();
    }

    public void Eliminar()
    {
        EliminadoEn ??= DateTimeOffset.UtcNow;
        Touch();
    }

    private void ReemplazarCategorias(IEnumerable<Categoria> categorias)
    {
        Categorias.Clear();
        foreach (var categoria in categorias.DistinctBy(x => x.Id))
            Categorias.Add(categoria);
    }

    private static void Validar(
        string nombre,
        long monto,
        string periodo,
        IReadOnlyCollection<Categoria> categorias)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 120)
            throw new DomainException("nombre_invalido", "El nombre del presupuesto no es válido.");
        if (monto <= 0)
            throw new DomainException("monto_invalido", "El monto debe ser positivo.");
        if (periodo is not ("semanal" or "mensual" or "anual"))
            throw new DomainException("periodo_invalido", "El periodo no es válido.");
        if (categorias.Count == 0)
            throw new DomainException("categorias_requeridas", "Debe seleccionar al menos una categoría.");
    }
}

public sealed class MetaAhorro : MutableEntity
{
    private MetaAhorro()
    { }

    public string Ambito { get; private set; } = "privado";
    public Guid? UsuarioId { get; private set; }
    public Guid? GrupoFamiliarId { get; private set; }
    public Guid CreadoPor { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public long MontoObjetivo { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public DateOnly FechaObjetivo { get; private set; }
    public Guid CuentaId { get; private set; }
    public string Estado { get; private set; } = "activa";
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static MetaAhorro Crear(
        Guid actorUsuarioId,
        string ambito,
        Guid? grupoFamiliarId,
        string nombre,
        long montoObjetivo,
        DateOnly fechaObjetivo,
        Guid cuentaId)
    {
        Validar(nombre, montoObjetivo, fechaObjetivo);
        if (ambito is not ("privado" or "familiar"))
            throw new DomainException("ambito_invalido", "El ámbito debe ser privado o familiar.");
        if ((ambito == "privado" && grupoFamiliarId is not null) ||
            (ambito == "familiar" && grupoFamiliarId is null))
            throw new DomainException("propietario_invalido", "El propietario de la meta es ambiguo.");
        if (cuentaId == Guid.Empty)
            throw new DomainException("cuenta_invalida", "La cuenta es requerida.");

        return new()
        {
            Ambito = ambito,
            UsuarioId = ambito == "privado" ? actorUsuarioId : null,
            GrupoFamiliarId = grupoFamiliarId,
            CreadoPor = actorUsuarioId,
            Nombre = nombre.Trim(),
            MontoObjetivo = montoObjetivo,
            FechaObjetivo = fechaObjetivo,
            CuentaId = cuentaId
        };
    }

    public void Actualizar(string? nombre, long? montoObjetivo, DateOnly? fechaObjetivo)
    {
        if (nombre is null && montoObjetivo is null && fechaObjetivo is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");
        Validar(nombre ?? Nombre, montoObjetivo ?? MontoObjetivo, fechaObjetivo ?? FechaObjetivo);
        if (nombre is not null) Nombre = nombre.Trim();
        if (montoObjetivo is not null) MontoObjetivo = montoObjetivo.Value;
        if (fechaObjetivo is not null) FechaObjetivo = fechaObjetivo.Value;
        Touch();
    }

    public void Eliminar()
    {
        EliminadoEn ??= DateTimeOffset.UtcNow;
        Estado = "eliminada";
        Touch();
    }

    private static void Validar(string nombre, long montoObjetivo, DateOnly fechaObjetivo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 120)
            throw new DomainException("nombre_invalido", "El nombre de la meta no es válido.");
        if (montoObjetivo <= 0)
            throw new DomainException("monto_objetivo_invalido", "El monto objetivo debe ser positivo.");
        if (fechaObjetivo <= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new DomainException("fecha_objetivo_invalida", "La fecha objetivo debe ser futura.");
    }
}

public sealed class AporteMeta : Entity
{
    private AporteMeta()
    { }

    public Guid MetaAhorroId { get; private set; }
    public long Monto { get; private set; }
    public Guid CuentaOrigenId { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public Guid AportadoPor { get; private set; }
    public DateOnly Fecha { get; private set; }
    public string HashIdempotencia { get; private set; } = string.Empty;

    public static AporteMeta Crear(
        Guid metaAhorroId,
        long monto,
        Guid cuentaOrigenId,
        string descripcion,
        Guid aportadoPor,
        string hashIdempotencia)
    {
        if (monto <= 0)
            throw new DomainException("monto_invalido", "El aporte debe ser positivo.");
        if (cuentaOrigenId == Guid.Empty)
            throw new DomainException("cuenta_invalida", "La cuenta de origen es requerida.");
        if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Trim().Length > 300)
            throw new DomainException("descripcion_invalida", "La descripción no es válida.");
        if (hashIdempotencia.Length != 64)
            throw new DomainException("idempotencia_invalida", "La clave de idempotencia no es válida.");
        return new()
        {
            MetaAhorroId = metaAhorroId,
            Monto = monto,
            CuentaOrigenId = cuentaOrigenId,
            Descripcion = descripcion.Trim(),
            AportadoPor = aportadoPor,
            Fecha = DateOnly.FromDateTime(DateTime.UtcNow),
            HashIdempotencia = hashIdempotencia
        };
    }
}