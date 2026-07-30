using FinanzasInteligentes.BuildingBlocks;
using FinanzasInteligentes.Dominio.Excepciones;
using System.Text.RegularExpressions;

namespace FinanzasInteligentes.Dominio.FinanzasPersonales;

public sealed partial class TarjetaCredito : MutableEntity
{
    private TarjetaCredito()
    { }

    public Guid UsuarioId { get; private set; }
    public Guid CuentaPagoId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Emisor { get; private set; } = string.Empty;
    public string UltimosCuatro { get; private set; } = string.Empty;
    public long DiaCierre { get; private set; }
    public long DiaVencimiento { get; private set; }
    public long LimiteCredito { get; private set; }
    public long SaldoUtilizado { get; private set; }
    public string Moneda { get; private set; } = "PYG";
    public string Color { get; private set; } = string.Empty;
    public DateTimeOffset? EliminadoEn { get; private set; }

    public long CreditoDisponible => Math.Max(0, LimiteCredito - SaldoUtilizado);

    public static TarjetaCredito Crear(
        Guid usuarioId,
        string nombre,
        string emisor,
        string ultimosCuatro,
        Guid cuentaPagoId,
        long diaCierre,
        long diaVencimiento,
        long limiteCredito,
        string moneda,
        string color)
    {
        Validar(usuarioId, nombre, emisor, ultimosCuatro, cuentaPagoId, diaCierre,
            diaVencimiento, limiteCredito, moneda, color);
        return new()
        {
            UsuarioId = usuarioId,
            Nombre = nombre.Trim(),
            Emisor = emisor.Trim(),
            UltimosCuatro = ultimosCuatro,
            CuentaPagoId = cuentaPagoId,
            DiaCierre = diaCierre,
            DiaVencimiento = diaVencimiento,
            LimiteCredito = limiteCredito,
            Moneda = moneda,
            Color = color
        };
    }

    public void Actualizar(
        string? nombre,
        string? emisor,
        string? ultimosCuatro,
        Guid? cuentaPagoId,
        long? diaCierre,
        long? diaVencimiento,
        long? limiteCredito,
        string? moneda,
        string? color)
    {
        if (nombre is null && emisor is null && ultimosCuatro is null && cuentaPagoId is null &&
            diaCierre is null && diaVencimiento is null && limiteCredito is null &&
            moneda is null && color is null)
            throw new DomainException(
                "actualizacion_vacia", "Debe indicar al menos un campo para actualizar.");

        var nuevoNombre = nombre ?? Nombre;
        var nuevoEmisor = emisor ?? Emisor;
        var nuevosUltimosCuatro = ultimosCuatro ?? UltimosCuatro;
        var nuevaCuentaPagoId = cuentaPagoId ?? CuentaPagoId;
        var nuevoDiaCierre = diaCierre ?? DiaCierre;
        var nuevoDiaVencimiento = diaVencimiento ?? DiaVencimiento;
        var nuevoLimite = limiteCredito ?? LimiteCredito;
        var nuevaMoneda = moneda ?? Moneda;
        var nuevoColor = color ?? Color;

        Validar(UsuarioId, nuevoNombre, nuevoEmisor, nuevosUltimosCuatro, nuevaCuentaPagoId,
            nuevoDiaCierre, nuevoDiaVencimiento, nuevoLimite, nuevaMoneda, nuevoColor);

        Nombre = nuevoNombre.Trim();
        Emisor = nuevoEmisor.Trim();
        UltimosCuatro = nuevosUltimosCuatro;
        CuentaPagoId = nuevaCuentaPagoId;
        DiaCierre = nuevoDiaCierre;
        DiaVencimiento = nuevoDiaVencimiento;
        LimiteCredito = nuevoLimite;
        Moneda = nuevaMoneda;
        Color = nuevoColor;
        Touch();
    }

    public void Eliminar()
    {
        if (EliminadoEn is not null) return;
        EliminadoEn = DateTimeOffset.UtcNow;
        Touch();
    }

    private static void Validar(
        Guid usuarioId,
        string nombre,
        string emisor,
        string ultimosCuatro,
        Guid cuentaPagoId,
        long diaCierre,
        long diaVencimiento,
        long limiteCredito,
        string moneda,
        string color)
    {
        if (usuarioId == Guid.Empty)
            throw new DomainException("usuario_invalido", "El usuario es requerido.");
        if (cuentaPagoId == Guid.Empty)
            throw new DomainException("cuenta_pago_invalida", "La cuenta de pago es requerida.");
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 120)
            throw new DomainException("nombre_invalido", "El nombre no es válido.");
        if (string.IsNullOrWhiteSpace(emisor) || emisor.Trim().Length > 120)
            throw new DomainException("emisor_invalido", "El emisor no es válido.");
        if (!UltimosCuatroRegex().IsMatch(ultimosCuatro))
            throw new DomainException(
                "ultimos_cuatro_invalidos", "Los últimos cuatro deben contener cuatro dígitos.");
        if (diaCierre is < 1 or > 31 || diaVencimiento is < 1 or > 31)
            throw new DomainException("dia_invalido", "Los días deben estar entre 1 y 31.");
        if (limiteCredito < 0)
            throw new DomainException(
                "limite_credito_invalido", "El límite de crédito no puede ser negativo.");
        if (moneda != "PYG")
            throw new DomainException("moneda_no_soportada", "La primera versión sólo admite PYG.");
        if (!ColorRegex().IsMatch(color))
            throw new DomainException("color_invalido", "El color debe tener el formato #RRGGBB.");
    }

    [GeneratedRegex(@"^\d{4}$", RegexOptions.CultureInvariant)]
    private static partial Regex UltimosCuatroRegex();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorRegex();
}