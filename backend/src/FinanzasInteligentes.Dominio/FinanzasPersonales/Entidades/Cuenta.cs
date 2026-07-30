using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;

namespace FinanzasInteligentes.Dominio.FinanzasPersonales;

public sealed class Cuenta : MutableEntity
{
    private Cuenta()
    { }

    public Guid UsuarioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public string Moneda { get; private set; } = "PYG";
    public long SaldoActual { get; private set; }
    public long SaldoInicial { get; private set; }
    public string? Color { get; private set; }
    public string? Icono { get; private set; }
    public bool IncluidaEnTotal { get; private set; } = true;
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static Cuenta Crear(
        Guid usuarioId,
        string nombre,
        string tipo,
        long saldoInicial,
        string? color = null,
        string? icono = null,
        bool incluidaEnTotal = true)
    {
        if (usuarioId == Guid.Empty) throw new DomainException("usuario_invalido", "El usuario es requerido.");
        if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("nombre_invalido", "El nombre es requerido.");
        if (!TiposPermitidos.Contains(tipo)) throw new DomainException("tipo_cuenta_invalido", "El tipo de cuenta no es válido.");

        return new Cuenta
        {
            UsuarioId = usuarioId,
            Nombre = nombre.Trim(),
            Tipo = tipo,
            SaldoInicial = saldoInicial,
            SaldoActual = saldoInicial,
            Color = color,
            Icono = icono,
            IncluidaEnTotal = incluidaEnTotal
        };
    }

    public void AplicarMovimiento(string tipo, long monto)
    {
        if (monto <= 0) throw new DomainException("monto_invalido", "El monto debe ser positivo.");
        SaldoActual = tipo switch
        {
            "ingreso" => checked(SaldoActual + monto),
            "gasto" => checked(SaldoActual - monto),
            _ => throw new DomainException("tipo_movimiento_invalido", "El tipo debe ser ingreso o gasto.")
        };
        Touch();
    }

    public void RevertirMovimiento(string tipo, long monto) =>
        AplicarMovimiento(tipo == "ingreso" ? "gasto" : "ingreso", monto);

    public void Actualizar(
        string? nombre,
        string? tipo,
        long? saldoInicial,
        string? color,
        string? icono,
        bool? incluidaEnTotal)
    {
        if (nombre is null && tipo is null && saldoInicial is null && color is null &&
            icono is null && incluidaEnTotal is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo para actualizar.");

        if (nombre is not null)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new DomainException("nombre_invalido", "El nombre es requerido.");
            Nombre = nombre.Trim();
        }

        if (tipo is not null)
        {
            if (!TiposPermitidos.Contains(tipo))
                throw new DomainException("tipo_cuenta_invalido", "El tipo de cuenta no es válido.");
            Tipo = tipo;
        }

        if (saldoInicial is not null)
        {
            SaldoActual = checked(SaldoActual + saldoInicial.Value - SaldoInicial);
            SaldoInicial = saldoInicial.Value;
        }

        if (color is not null) Color = color;
        if (icono is not null) Icono = icono;
        if (incluidaEnTotal is not null) IncluidaEnTotal = incluidaEnTotal.Value;

        Touch();
    }

    public void Eliminar()
    {
        EliminadoEn ??= DateTimeOffset.UtcNow;
        Touch();
    }

    private static readonly HashSet<string> TiposPermitidos =
    [
        "efectivo", "cuenta-corriente", "cuenta-ahorro", "tarjeta-debito", "billetera-digital", "otra"
    ];
}

public sealed class Categoria : MutableEntity
{
    private Categoria()
    { }

    public Guid? UsuarioId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = "ambos";
    public string? Icono { get; private set; }
    public string? Color { get; private set; }
    public bool EsPredeterminada { get; private set; }
    public DateTimeOffset? EliminadoEn { get; private set; }

    public static Categoria Crear(Guid usuarioId, string nombre, string tipo, string? icono = null, string? color = null)
    {
        if (usuarioId == Guid.Empty) throw new DomainException("usuario_invalido", "El usuario es requerido.");
        if (string.IsNullOrWhiteSpace(nombre)) throw new DomainException("nombre_invalido", "El nombre es requerido.");
        if (!new[] { "ingreso", "gasto", "ambos" }.Contains(tipo))
            throw new DomainException("tipo_categoria_invalido", "El tipo de categoría no es válido.");
        return new Categoria
        {
            UsuarioId = usuarioId,
            Nombre = nombre.Trim(),
            Tipo = tipo,
            Icono = icono,
            Color = color
        };
    }

    public void Actualizar(string? nombre, string? tipo, string? icono, string? color)
    {
        if (EsPredeterminada)
            throw new DomainException("categoria_predefinida", "Una categoría predefinida no puede modificarse.");
        if (nombre is null && tipo is null && icono is null && color is null)
            throw new DomainException("actualizacion_vacia", "Debe indicar al menos un campo para actualizar.");

        if (nombre is not null)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new DomainException("nombre_invalido", "El nombre es requerido.");
            Nombre = nombre.Trim();
        }

        if (tipo is not null)
        {
            if (tipo is not ("ingreso" or "gasto" or "ambos"))
                throw new DomainException("tipo_categoria_invalido", "El tipo de categoría no es válido.");
            Tipo = tipo;
        }

        if (icono is not null) Icono = icono;
        if (color is not null) Color = color;

        Touch();
    }

    public void Eliminar()
    {
        if (EsPredeterminada)
            throw new DomainException("categoria_predefinida", "Una categoría predefinida no puede eliminarse.");
        EliminadoEn ??= DateTimeOffset.UtcNow;
        Touch();
    }
}