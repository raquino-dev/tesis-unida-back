using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;
using System.Security.Cryptography;

namespace FinanzasInteligentes.Dominio.FinanzasFamiliares;

public sealed class GrupoFamiliar : MutableEntity
{
    private GrupoFamiliar()
    { }

    public string Nombre { get; private set; } = string.Empty;
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static GrupoFamiliar Crear(string nombre)
    {
        ValidarNombre(nombre);
        return new() { Nombre = nombre.Trim() };
    }

    public void Actualizar(string nombre)
    {
        ValidarNombre(nombre);
        Nombre = nombre.Trim();
        Touch();
    }

    public void Eliminar()
    {
        if (EliminadoEn is not null) return;
        EliminadoEn = DateTimeOffset.UtcNow;
        Touch();
    }

    private static void ValidarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 120)
            throw new DomainException("nombre_grupo_invalido", "El nombre del grupo no es válido.");
    }
}

public sealed class IntegranteFamiliar : MutableEntity
{
    private IntegranteFamiliar()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Rol { get; private set; } = "integrante";
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static IntegranteFamiliar Crear(Guid grupoId, Guid usuarioId, string rol)
    {
        ValidarRol(rol, true);
        return new() { GrupoFamiliarId = grupoId, UsuarioId = usuarioId, Rol = rol };
    }

    public void CambiarRol(string rol)
    {
        ValidarRol(rol, false);
        if (Rol == "propietario")
            throw new DomainException(
                "propietario_inmutable", "No se puede modificar el rol del propietario.");
        Rol = rol;
        Touch();
    }

    public void Eliminar()
    {
        if (Rol == "propietario")
            throw new DomainException(
                "propietario_inmutable", "No se puede eliminar al propietario.");
        if (EliminadoEn is not null) return;
        EliminadoEn = DateTimeOffset.UtcNow;
        Touch();
    }

    private static void ValidarRol(string rol, bool permitePropietario)
    {
        if (rol is not ("administrador" or "integrante") &&
            !(permitePropietario && rol == "propietario"))
            throw new DomainException("rol_familiar_invalido", "El rol familiar no es válido.");
    }
}

public sealed class InvitacionFamiliar : MutableEntity
{
    private InvitacionFamiliar()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public string? Correo { get; private set; }
    public Guid? UsuarioDestinoId { get; private set; }
    public string Rol { get; private set; } = "integrante";
    public string HashToken { get; private set; } = string.Empty;
    public string HashCodigo { get; private set; } = string.Empty;
    public string Estado { get; private set; } = "pendiente";
    public DateTimeOffset ExpiraEn { get; private set; }

    public static InvitacionFamiliar Crear(
        Guid grupoId,
        string? correo,
        Guid? usuarioDestinoId,
        string rol,
        string hashToken,
        string hashCodigo)
    {
        if ((correo is null) == (usuarioDestinoId is null))
            throw new DomainException(
                "destino_invitacion_invalido", "Debe indicar un único destino.");
        if (rol is not ("administrador" or "integrante"))
            throw new DomainException("rol_familiar_invalido", "El rol familiar no es válido.");
        return new()
        {
            GrupoFamiliarId = grupoId,
            Correo = correo?.Trim().ToLowerInvariant(),
            UsuarioDestinoId = usuarioDestinoId,
            Rol = rol,
            HashToken = hashToken,
            HashCodigo = hashCodigo,
            ExpiraEn = DateTimeOffset.UtcNow.AddDays(7)
        };
    }

    public void Aceptar(string hashCodigo)
    {
        if (Estado != "pendiente" || ExpiraEn <= DateTimeOffset.UtcNow)
            throw new DomainException(
                "invitacion_invalida", "La invitación no está disponible.");
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(HashCodigo), Convert.FromHexString(hashCodigo)))
            throw new DomainException("codigo_invitacion_invalido", "El código no es válido.");
        Estado = "aceptada";
        Touch();
    }

    public void Cancelar()
    {
        if (Estado != "pendiente")
            throw new DomainException(
                "invitacion_invalida", "La invitación no está pendiente.");
        Estado = "cancelada";
        Touch();
    }
}

public sealed class CuentaCompartida : MutableEntity
{
    private CuentaCompartida()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public Guid CuentaId { get; private set; }
    public Guid CompartidaPorUsuarioId { get; private set; }
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static CuentaCompartida Crear(Guid grupoId, Guid cuentaId, Guid usuarioId) =>
        new() { GrupoFamiliarId = grupoId, CuentaId = cuentaId, CompartidaPorUsuarioId = usuarioId };

    public void Eliminar()
    {
        if (EliminadoEn is not null) return;
        EliminadoEn = DateTimeOffset.UtcNow;
        Touch();
    }
}

public sealed class CategoriaFamiliar : MutableEntity
{
    private CategoriaFamiliar()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = "gasto";
    public string Icono { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static CategoriaFamiliar Crear(
        Guid grupoId, string nombre, string tipo, string icono, string color)
    {
        Validar(nombre, tipo, icono, color);
        return new()
        {
            GrupoFamiliarId = grupoId,
            Nombre = nombre.Trim(),
            Tipo = tipo,
            Icono = icono.Trim(),
            Color = color
        };
    }

    public void Actualizar(string? nombre, string? tipo, string? icono, string? color)
    {
        if (nombre is null && tipo is null && icono is null && color is null)
            throw new DomainException(
                "actualizacion_vacia", "Debe indicar al menos un campo.");
        var n = nombre ?? Nombre;
        var t = tipo ?? Tipo;
        var i = icono ?? Icono;
        var c = color ?? Color;
        Validar(n, t, i, c);
        Nombre = n.Trim(); Tipo = t; Icono = i.Trim(); Color = c;
        Touch();
    }

    public void Eliminar()
    {
        EliminadoEn ??= DateTimeOffset.UtcNow;
        Touch();
    }

    private static void Validar(string nombre, string tipo, string icono, string color)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100)
            throw new DomainException("nombre_invalido", "El nombre no es válido.");
        if (tipo is not ("ingreso" or "gasto" or "ambos"))
            throw new DomainException("tipo_categoria_invalido", "El tipo no es válido.");
        if (string.IsNullOrWhiteSpace(icono) || icono.Trim().Length > 50)
            throw new DomainException("icono_invalido", "El icono no es válido.");
        if (color.Length != 7 || color[0] != '#')
            throw new DomainException("color_invalido", "El color no es válido.");
    }
}

public sealed class MovimientoFamiliar : MutableEntity
{
    private MovimientoFamiliar()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid CuentaId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public long Monto { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public DateOnly Fecha { get; private set; }
    public Guid[] CategoriaIds { get; private set; } = [];
    public string Estado { get; private set; } = "confirmado";

    public static MovimientoFamiliar Crear(
        Guid grupoId, Guid usuarioId, Guid cuentaId, string tipo, long monto,
        string descripcion, DateOnly fecha, IReadOnlyCollection<Guid>? categoriaIds)
    {
        if (tipo is not ("ingreso" or "gasto") || monto <= 0)
            throw new DomainException("movimiento_invalido", "El movimiento no es válido.");
        return new()
        {
            GrupoFamiliarId = grupoId,
            UsuarioId = usuarioId,
            CuentaId = cuentaId,
            Tipo = tipo,
            Monto = monto,
            Descripcion = descripcion.Trim(),
            Fecha = fecha,
            CategoriaIds = categoriaIds?.Distinct().ToArray() ?? []
        };
    }

    public void Actualizar(string? descripcion, IReadOnlyCollection<Guid>? categoriaIds)
    {
        if (descripcion is null && categoriaIds is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");
        if (Estado == "anulado")
            throw new DomainException("movimiento_anulado", "El movimiento fue anulado.");
        if (descripcion is not null) Descripcion = descripcion.Trim();
        if (categoriaIds is not null) CategoriaIds = categoriaIds.Distinct().ToArray();
        Touch();
    }

    public void Anular()
    {
        if (Estado == "anulado")
            throw new DomainException("movimiento_anulado", "El movimiento ya fue anulado.");
        Estado = "anulado";
        Touch();
    }
}

public sealed class CajaCompartida : MutableEntity
{
    private CajaCompartida()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public long Saldo { get; private set; }

    public static CajaCompartida Crear(Guid grupoId) => new() { GrupoFamiliarId = grupoId };

    public (long Anterior, long Posterior) Aplicar(string tipo, long monto)
    {
        if (monto <= 0 || tipo is not ("aporte" or "retiro"))
            throw new DomainException("operacion_caja_invalida", "La operación no es válida.");
        var anterior = Saldo;
        Saldo = tipo == "aporte"
            ? checked(Saldo + monto)
            : Saldo >= monto
                ? Saldo - monto
                : throw new DomainException("saldo_insuficiente", "La caja no tiene saldo suficiente.");
        Touch();
        return (anterior, Saldo);
    }
}

public sealed class OperacionCaja : Entity
{
    private OperacionCaja()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid CuentaPrivadaId { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public long Monto { get; private set; }
    public string Descripcion { get; private set; } = string.Empty;
    public long SaldoAnterior { get; private set; }
    public long SaldoPosterior { get; private set; }

    public static OperacionCaja Crear(
        Guid grupoId, Guid usuarioId, Guid cuentaId, string tipo, long monto,
        string descripcion, long anterior, long posterior) =>
        new()
        {
            GrupoFamiliarId = grupoId,
            UsuarioId = usuarioId,
            CuentaPrivadaId = cuentaId,
            Tipo = tipo,
            Monto = monto,
            Descripcion = descripcion.Trim(),
            SaldoAnterior = anterior,
            SaldoPosterior = posterior
        };
}

public sealed class PresupuestoFamiliar : MutableEntity
{
    private PresupuestoFamiliar()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public long Monto { get; private set; }
    public string Periodo { get; private set; } = "mensual";
    public Guid[] CategoriaIds { get; private set; } = [];
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static PresupuestoFamiliar Crear(
        Guid grupoId, string nombre, long monto, string periodo,
        IReadOnlyCollection<Guid> categoriaIds)
    {
        Validar(nombre, monto, periodo);
        return new()
        {
            GrupoFamiliarId = grupoId,
            Nombre = nombre.Trim(),
            Monto = monto,
            Periodo = periodo,
            CategoriaIds = categoriaIds.Distinct().ToArray()
        };
    }

    public void Actualizar(
        string? nombre, long? monto, string? periodo,
        IReadOnlyCollection<Guid>? categoriaIds)
    {
        if (nombre is null && monto is null && periodo is null && categoriaIds is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo.");
        var n = nombre ?? Nombre;
        var m = monto ?? Monto;
        var p = periodo ?? Periodo;
        Validar(n, m, p);
        Nombre = n.Trim(); Monto = m; Periodo = p;
        if (categoriaIds is not null) CategoriaIds = categoriaIds.Distinct().ToArray();
        Touch();
    }

    public void Eliminar()
    {
        EliminadoEn ??= DateTimeOffset.UtcNow;
        Touch();
    }

    private static void Validar(string nombre, long monto, string periodo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || monto <= 0)
            throw new DomainException("presupuesto_invalido", "El presupuesto no es válido.");
        if (periodo is not ("semanal" or "mensual" or "anual"))
            throw new DomainException("periodo_invalido", "El periodo no es válido.");
    }
}

public sealed class EliminacionGrupoFamiliar : MutableEntity
{
    private EliminacionGrupoFamiliar()
    { }

    public Guid GrupoFamiliarId { get; private set; }
    public string Estado { get; private set; } = "pendiente";
    public DateTimeOffset? CompletadoEn { get; private set; }
    public string? ErrorCodigo { get; private set; }

    public static EliminacionGrupoFamiliar Crear(Guid grupoId) =>
        new() { GrupoFamiliarId = grupoId };

    public void Completar()
    {
        Estado = "completado";
        CompletadoEn = DateTimeOffset.UtcNow;
        Touch();
    }
}