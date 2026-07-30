using FinanzasInteligentes.Dominio.FinanzasFamiliares;

namespace FinanzasInteligentes.Aplicacion.Abstracciones;

public interface IFamiliasRepository
{
    Task<IReadOnlyCollection<GrupoFamiliar>> ListarGrupos(Guid usuarioId, CancellationToken ct);

    Task<GrupoFamiliar?> ObtenerGrupo(Guid grupoId, bool soloLectura, CancellationToken ct);

    Task<IntegranteFamiliar?> ObtenerMembresia(Guid grupoId, Guid usuarioId, bool soloLectura, CancellationToken ct);

    Task<IReadOnlyCollection<IntegranteFamiliar>> ListarIntegrantes(Guid grupoId, CancellationToken ct);

    Task<IntegranteFamiliar?> ObtenerIntegrante(Guid grupoId, Guid integranteId, bool soloLectura, CancellationToken ct);

    Task<bool> UsuarioEsIntegrante(Guid grupoId, Guid usuarioId, CancellationToken ct);

    Task<IReadOnlyCollection<InvitacionFamiliar>> ListarInvitaciones(Guid grupoId, CancellationToken ct);

    Task<InvitacionFamiliar?> ObtenerInvitacion(Guid grupoId, Guid invitacionId, bool soloLectura, CancellationToken ct);

    Task<InvitacionFamiliar?> ObtenerInvitacionPorHashToken(string hashToken, bool soloLectura, CancellationToken ct);

    Task<IReadOnlyCollection<CuentaCompartida>> ListarCuentasCompartidas(Guid grupoId, CancellationToken ct);

    Task<CuentaCompartida?> ObtenerCuentaCompartida(Guid grupoId, Guid cuentaId, bool soloLectura, CancellationToken ct);

    Task<IReadOnlyCollection<CategoriaFamiliar>> ListarCategorias(Guid grupoId, CancellationToken ct);

    Task<CategoriaFamiliar?> ObtenerCategoria(Guid grupoId, Guid categoriaId, bool soloLectura, CancellationToken ct);

    Task<IReadOnlyCollection<CategoriaFamiliar>> ObtenerCategorias(
        Guid grupoId, IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<IReadOnlyCollection<MovimientoFamiliar>> ListarMovimientos(Guid grupoId, CancellationToken ct);

    Task<MovimientoFamiliar?> ObtenerMovimiento(Guid grupoId, Guid movimientoId, bool soloLectura, CancellationToken ct);

    Task<CajaCompartida?> ObtenerCaja(Guid grupoId, bool soloLectura, CancellationToken ct);

    Task<IReadOnlyCollection<OperacionCaja>> ListarOperacionesCaja(Guid grupoId, CancellationToken ct);

    Task<OperacionCaja?> ObtenerOperacionCaja(Guid grupoId, Guid operacionId, CancellationToken ct);

    Task<IReadOnlyCollection<PresupuestoFamiliar>> ListarPresupuestos(Guid grupoId, CancellationToken ct);

    Task<PresupuestoFamiliar?> ObtenerPresupuesto(Guid grupoId, Guid presupuestoId, bool soloLectura, CancellationToken ct);

    Task<EliminacionGrupoFamiliar?> ObtenerEliminacion(Guid grupoId, Guid eliminacionId, CancellationToken ct);

    Task<bool> ExisteEliminacionPendiente(Guid grupoId, CancellationToken ct);

    void Agregar(GrupoFamiliar entity);

    void Agregar(IntegranteFamiliar entity);

    void Agregar(InvitacionFamiliar entity);

    void Agregar(CuentaCompartida entity);

    void Agregar(CategoriaFamiliar entity);

    void Agregar(MovimientoFamiliar entity);

    void Agregar(CajaCompartida entity);

    void Agregar(OperacionCaja entity);

    void Agregar(PresupuestoFamiliar entity);

    void Agregar(EliminacionGrupoFamiliar entity);
}