namespace FinanzasInteligentes.Api.Contratos.FinanzasFamiliares;

public sealed record GrupoFamiliarRequest(string Nombre);
public sealed record PatchGruposFamiliaresByGrupoIdRequest(string Nombre);
public sealed record PostGruposFamiliaresByGrupoIdEliminacionesRequest(Guid VerificacionOtpId);
public sealed record PatchGruposFamiliaresByGrupoIdIntegrantesByIntegranteIdRequest(string Rol);
public sealed record InvitacionFamiliarRequest(
    string? Correo,
    Guid? IdentificadorUsuario,
    string Rol,
    string? Alias = null);
public sealed record AceptacionInvitacionRequest(string Codigo);
public sealed record PostGruposFamiliaresByGrupoIdCuentasCompartidasRequest(Guid CuentaId);
public sealed record CategoriaFamiliarRequest(string Nombre, string Tipo, string Icono, string Color);
public sealed record CategoriaFamiliarPatchRequest(
    string? Nombre = null, string? Tipo = null, string? Icono = null, string? Color = null);
public sealed record MovimientoFamiliarRequest(
    Guid CuentaId,
    string Tipo,
    long Monto,
    string Descripcion,
    DateOnly Fecha,
    IReadOnlyCollection<Guid>? CategoriaIds = null);
public sealed record PatchGruposFamiliaresByGrupoIdMovimientosByMovimientoIdRequest(
    string? Descripcion = null,
    IReadOnlyCollection<Guid>? CategoriaIds = null);
public sealed record OperacionCajaRequest(
    string Tipo,
    long Monto,
    string Descripcion,
    Guid CuentaPrivadaId,
    Guid? MovimientoFamiliarId = null,
    Guid? VerificacionOtpId = null);
public sealed record PresupuestoFamiliarRequest(
    string Nombre,
    long Monto,
    string Periodo,
    IReadOnlyCollection<Guid> CategoriaIds);
public sealed record PresupuestoFamiliarPatchRequest(
    string? Nombre = null,
    long? Monto = null,
    string? Periodo = null,
    IReadOnlyCollection<Guid>? CategoriaIds = null);
