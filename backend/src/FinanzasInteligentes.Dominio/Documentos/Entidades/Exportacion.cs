using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.Documentos;

public sealed class Exportacion : MutableEntity
{
    private Exportacion()
    { }

    public Guid UsuarioId { get; private set; }
    public string Ambito { get; private set; } = "privado";
    public Guid? GrupoFamiliarId { get; private set; }
    public string Formato { get; private set; } = string.Empty;
    public DateOnly Desde { get; private set; }
    public DateOnly Hasta { get; private set; }
    public string? TipoMovimiento { get; private set; }
    public Guid? CategoriaId { get; private set; }
    public Guid? CuentaId { get; private set; }
    public string Documento { get; private set; } = "cualquiera";
    public string HashSolicitud { get; private set; } = string.Empty;
    public string Estado { get; private set; } = "pendiente";
    public string? ClaveObjeto { get; private set; }
    public DateTimeOffset? FinalizadoEn { get; private set; }
    public long CantidadMovimientos { get; private set; }
    public long TotalIngresos { get; private set; }
    public long TotalGastos { get; private set; }
    public long TotalTransferido { get; private set; }
    public DateTimeOffset? ExpiraEn { get; private set; }
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static Exportacion Crear(
        Guid usuarioId, string formato, string ambito, Guid? grupoId,
        DateOnly desde, DateOnly hasta, string? tipo, Guid? categoriaId,
        Guid? cuentaId, string documento, string hash)
    {
        if (formato is not ("csv" or "xlsx" or "pdf"))
            throw new DomainException("formato_invalido", "El formato debe ser csv, xlsx o pdf.");
        if (desde > hasta)
            throw new DomainException("rango_invalido", "La fecha desde no puede superar a hasta.");
        if ((ambito == "privado" && grupoId is not null) ||
            (ambito == "familiar" && grupoId is null) ||
            ambito is not ("privado" or "familiar"))
            throw new DomainException("ambito_invalido", "El ámbito no es válido.");
        return new()
        {
            UsuarioId = usuarioId,
            Formato = formato,
            Ambito = ambito,
            GrupoFamiliarId = grupoId,
            Desde = desde,
            Hasta = hasta,
            TipoMovimiento = tipo,
            CategoriaId = categoriaId,
            CuentaId = cuentaId,
            Documento = documento,
            HashSolicitud = hash
        };
    }

    public void Iniciar()
    { Estado = "procesando"; Touch(); }

    public void Completar(
        string clave, long cantidad, long ingresos, long gastos, long transferido)
    {
        ClaveObjeto = clave; CantidadMovimientos = cantidad;
        TotalIngresos = ingresos; TotalGastos = gastos; TotalTransferido = transferido;
        Estado = "completado"; FinalizadoEn = DateTimeOffset.UtcNow;
        ExpiraEn = DateTimeOffset.UtcNow.AddDays(7); Touch();
    }

    public void Eliminar()
    {
        if (Estado is "pendiente" or "procesando")
            throw new DomainException("procesamiento_activo", "No puede eliminar una exportación activa.");
        EliminadoEn = DateTimeOffset.UtcNow; Estado = "eliminado"; Touch();
    }
}