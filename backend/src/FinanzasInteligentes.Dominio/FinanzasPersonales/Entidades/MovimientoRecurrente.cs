using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.FinanzasPersonales;

public sealed class MovimientoRecurrente : MutableEntity
{
    private MovimientoRecurrente()
    { }

    public Guid UsuarioId { get; private set; }
    public Guid CuentaId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public long Monto { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public string Descripcion { get; private set; } = string.Empty;
    public DateOnly FechaInicio { get; private set; }
    public DateOnly? FechaFin { get; private set; }
    public string Frecuencia { get; private set; } = string.Empty;
    public long? CantidadOcurrencias { get; private set; }
    public long OcurrenciasCompletadas { get; private set; }
    public DateOnly ProximaEjecucion { get; private set; }
    public string Estado { get; private set; } = "activa";
    public DateTimeOffset? UltimaEjecucionEn { get; private set; }
    public DateTimeOffset? EliminadoEn { get; private set; }
    public ICollection<Categoria> Categorias { get; private set; } = new List<Categoria>();

    public static MovimientoRecurrente Crear(
        Guid usuarioId,
        Guid cuentaId,
        string tipo,
        long monto,
        string descripcion,
        DateOnly fechaInicio,
        DateOnly? fechaFin,
        string frecuencia,
        long? cantidadOcurrencias,
        IReadOnlyCollection<Categoria> categorias)
    {
        Validar(
            tipo, monto, descripcion, fechaInicio, fechaFin,
            frecuencia, cantidadOcurrencias, 0, categorias);
        var entity = new MovimientoRecurrente
        {
            UsuarioId = usuarioId,
            CuentaId = cuentaId,
            Tipo = tipo,
            Monto = monto,
            Descripcion = descripcion.Trim(),
            FechaInicio = fechaInicio,
            FechaFin = fechaFin,
            Frecuencia = frecuencia,
            CantidadOcurrencias = cantidadOcurrencias,
            ProximaEjecucion = fechaInicio
        };
        entity.ReemplazarCategorias(categorias);
        return entity;
    }

    public void Actualizar(
        Guid? cuentaId,
        string? tipo,
        long? monto,
        string? descripcion,
        DateOnly? fechaInicio,
        DateOnly? fechaFin,
        bool actualizarFechaFin,
        string? frecuencia,
        long? cantidadOcurrencias,
        bool actualizarCantidadOcurrencias,
        IReadOnlyCollection<Categoria>? categorias)
    {
        if (cuentaId is null && tipo is null && monto is null && descripcion is null &&
            fechaInicio is null && !actualizarFechaFin && frecuencia is null &&
            !actualizarCantidadOcurrencias && categorias is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");
        if (Estado != "activa")
            throw new DomainException(
                "recurrencia_inactiva", "Una recurrencia inactiva no puede modificarse.");

        var inicio = fechaInicio ?? FechaInicio;
        var fin = actualizarFechaFin ? fechaFin : FechaFin;
        var cantidad = actualizarCantidadOcurrencias ? cantidadOcurrencias : CantidadOcurrencias;
        var frecuenciaFinal = frecuencia ?? Frecuencia;
        Validar(
            tipo ?? Tipo, monto ?? Monto, descripcion ?? Descripcion,
            inicio, fin, frecuenciaFinal, cantidad, OcurrenciasCompletadas,
            categorias ?? Categorias.ToArray());

        if (cuentaId is not null) CuentaId = cuentaId.Value;
        if (tipo is not null) Tipo = tipo;
        if (monto is not null) Monto = monto.Value;
        if (descripcion is not null) Descripcion = descripcion.Trim();
        FechaInicio = inicio;
        FechaFin = fin;
        Frecuencia = frecuenciaFinal;
        CantidadOcurrencias = cantidad;
        if (categorias is not null) ReemplazarCategorias(categorias);
        ProximaEjecucion = CalcularEjecucion(FechaInicio, Frecuencia, OcurrenciasCompletadas);
        Touch();
    }

    public void RegistrarEjecucion(DateTimeOffset ahora)
    {
        if (Estado != "activa")
            throw new DomainException("recurrencia_inactiva", "La recurrencia no está activa.");
        OcurrenciasCompletadas = checked(OcurrenciasCompletadas + 1);
        UltimaEjecucionEn = ahora;
        var siguiente = CalcularEjecucion(FechaInicio, Frecuencia, OcurrenciasCompletadas);
        var finalizadaPorCantidad =
            CantidadOcurrencias is not null && OcurrenciasCompletadas >= CantidadOcurrencias;
        var finalizadaPorFecha = FechaFin is not null && siguiente > FechaFin;
        if (finalizadaPorCantidad || finalizadaPorFecha)
            Estado = "finalizada";
        else
            ProximaEjecucion = siguiente;
        Touch();
    }

    public void CambiarEstado(string estado)
    {
        if (estado is not ("activa" or "pausada" or "finalizada"))
            throw new DomainException("estado_invalido", "El estado de la recurrencia no es válido.");
        if (Estado == "finalizada" && estado != "finalizada")
            throw new DomainException(
                "recurrencia_finalizada", "Una recurrencia finalizada no puede reanudarse.");
        if (Estado == estado) return;
        Estado = estado;
        Touch();
    }

    public void Eliminar()
    {
        EliminadoEn ??= DateTimeOffset.UtcNow;
        Estado = "finalizada";
        Touch();
    }

    public static DateOnly CalcularEjecucion(
        DateOnly fechaInicio, string frecuencia, long indice)
    {
        if (indice < 0 || indice > int.MaxValue)
            throw new DomainException("ocurrencias_invalidas", "La cantidad de ocurrencias no es válida.");
        var cantidad = checked((int)indice);
        return frecuencia switch
        {
            "diaria" => fechaInicio.AddDays(cantidad),
            "semanal" => fechaInicio.AddDays(checked(cantidad * 7)),
            "quincenal" => fechaInicio.AddDays(checked(cantidad * 15)),
            "mensual" => fechaInicio.AddMonths(cantidad),
            "anual" => fechaInicio.AddYears(cantidad),
            _ => throw new DomainException("frecuencia_invalida", "La frecuencia no es válida.")
        };
    }

    private void ReemplazarCategorias(IEnumerable<Categoria> categorias)
    {
        Categorias.Clear();
        foreach (var categoria in categorias.DistinctBy(x => x.Id))
            Categorias.Add(categoria);
    }

    private static void Validar(
        string tipo,
        long monto,
        string descripcion,
        DateOnly fechaInicio,
        DateOnly? fechaFin,
        string frecuencia,
        long? cantidadOcurrencias,
        long ocurrenciasCompletadas,
        IReadOnlyCollection<Categoria> categorias)
    {
        if (tipo is not ("ingreso" or "gasto"))
            throw new DomainException("tipo_movimiento_invalido", "El tipo no es válido.");
        if (monto <= 0)
            throw new DomainException("monto_invalido", "El monto debe ser positivo.");
        if (string.IsNullOrWhiteSpace(descripcion) || descripcion.Trim().Length > 300)
            throw new DomainException("descripcion_invalida", "La descripción no es válida.");
        if (fechaFin is not null && fechaFin < fechaInicio)
            throw new DomainException("rango_invalido", "La fecha final no puede preceder al inicio.");
        if (frecuencia is not ("diaria" or "semanal" or "quincenal" or "mensual" or "anual"))
            throw new DomainException("frecuencia_invalida", "La frecuencia no es válida.");
        if (cantidadOcurrencias is <= 0 || cantidadOcurrencias <= ocurrenciasCompletadas)
            throw new DomainException(
                "ocurrencias_invalidas",
                "La cantidad de ocurrencias debe superar las ya completadas.");
        if (categorias.Count == 0)
            throw new DomainException(
                "categorias_requeridas", "Debe seleccionar al menos una categoría.");
        if (categorias.Any(x => x.Tipo != tipo && x.Tipo != "ambos"))
            throw new DomainException(
                "categoria_incompatible", "Una categoría no corresponde al tipo del movimiento.");
    }
}
