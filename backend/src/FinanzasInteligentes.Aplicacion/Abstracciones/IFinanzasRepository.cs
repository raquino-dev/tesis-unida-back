using FinanzasInteligentes.Dominio.Analitica;
using FinanzasInteligentes.Dominio.FinanzasPersonales;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IFinanzasRepository
{
    Task<IReadOnlyCollection<Cuenta>> ListarCuentas(Guid usuarioId, CancellationToken cancellationToken);

    Task<Cuenta?> ObtenerCuenta(Guid usuarioId, Guid cuentaId, bool soloLectura, CancellationToken cancellationToken);

    Task<bool> CuentaEstaEnUso(Guid cuentaId, CancellationToken cancellationToken);

    void Agregar(Cuenta cuenta);

    Task<IReadOnlyCollection<Categoria>> ListarCategorias(Guid usuarioId, CancellationToken cancellationToken);

    Task<Categoria?> ObtenerCategoria(
        Guid usuarioId,
        Guid categoriaId,
        bool soloLectura,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Categoria>> ObtenerCategorias(
        Guid usuarioId,
        IReadOnlyCollection<Guid> categoriaIds,
        CancellationToken cancellationToken);

    void Agregar(Categoria categoria);

    Task<IReadOnlyCollection<TarjetaCredito>> ListarTarjetasCredito(
        Guid usuarioId,
        CancellationToken cancellationToken);

    Task<TarjetaCredito?> ObtenerTarjetaCredito(
        Guid usuarioId,
        Guid tarjetaId,
        bool soloLectura,
        CancellationToken cancellationToken);

    Task<bool> ExisteTarjetaCreditoConAlias(
        Guid usuarioId,
        string alias,
        Guid? exceptoTarjetaId,
        CancellationToken cancellationToken);

    void Agregar(TarjetaCredito tarjeta);

    Task<IReadOnlyCollection<Movimiento>> ListarMovimientos(Guid usuarioId, CancellationToken cancellationToken);

    Task<Movimiento?> ObtenerMovimiento(Guid usuarioId, Guid movimientoId, bool soloLectura, CancellationToken cancellationToken);

    void Agregar(Movimiento movimiento);

    Task<IReadOnlyCollection<MovimientoRecurrente>> ListarMovimientosRecurrentes(
        Guid usuarioId, CancellationToken cancellationToken);

    Task<MovimientoRecurrente?> ObtenerMovimientoRecurrente(
        Guid usuarioId, Guid recurrenteId, bool soloLectura, CancellationToken cancellationToken);

    Task<bool> ExisteMovimientoRecurrenteDuplicado(
        Guid usuarioId, Guid cuentaId, string descripcion,
        Guid? exceptoRecurrenteId, CancellationToken cancellationToken);

    void Agregar(MovimientoRecurrente movimientoRecurrente);

    Task<IReadOnlyCollection<Transferencia>> ListarTransferencias(
        Guid usuarioId, CancellationToken cancellationToken);

    Task<Transferencia?> ObtenerTransferencia(
        Guid usuarioId, Guid transferenciaId, bool soloLectura, CancellationToken cancellationToken);

    Task<Transferencia?> ObtenerTransferenciaPorIdempotencia(
        Guid usuarioId, string hashIdempotencia, CancellationToken cancellationToken);

    void Agregar(Transferencia transferencia);

    Task<IReadOnlyCollection<AlertaFinanciera>> ListarAlertas(
        Guid usuarioId, CancellationToken cancellationToken);

    Task<AlertaFinanciera?> ObtenerAlerta(
        Guid usuarioId, Guid alertaId, bool soloLectura, CancellationToken cancellationToken);

    void Agregar(AlertaFinanciera alerta);

    Task<IReadOnlyCollection<Presupuesto>> ListarPresupuestos(
        Guid usuarioId, CancellationToken cancellationToken);

    Task<Presupuesto?> ObtenerPresupuesto(
        Guid usuarioId, Guid presupuestoId, bool soloLectura, CancellationToken cancellationToken);

    Task<bool> ExistePresupuestoSolapado(
        Guid usuarioId, string periodo, IReadOnlyCollection<Guid> categoriaIds,
        Guid? exceptoPresupuestoId, CancellationToken cancellationToken);

    void Agregar(Presupuesto presupuesto);

    Task<IReadOnlyCollection<MetaAhorro>> ListarMetas(
        Guid usuarioId, CancellationToken cancellationToken);

    Task<MetaAhorro?> ObtenerMeta(
        Guid metaId, bool soloLectura, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AporteMeta>> ListarAportes(
        Guid metaId, CancellationToken cancellationToken);

    Task<AporteMeta?> ObtenerAportePorIdempotencia(
        Guid metaId, string hashIdempotencia, CancellationToken cancellationToken);

    void Agregar(MetaAhorro meta);

    void Agregar(AporteMeta aporte);

    void Agregar(EventoOutbox evento);
}
