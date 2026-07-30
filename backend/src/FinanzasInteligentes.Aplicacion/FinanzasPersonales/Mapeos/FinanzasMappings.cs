using FinanzasInteligentes.Aplicacion.FinanzasPersonales.Modelos;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.Aplicacion.FinanzasPersonales.Mapeos;

public static class FinanzasMappings
{
    public static CuentaResponse ToResponse(this Cuenta cuenta) =>
        new(
            cuenta.Id, cuenta.Nombre, cuenta.Tipo, cuenta.Moneda, cuenta.SaldoActual,
            cuenta.SaldoInicial, cuenta.Color, cuenta.Icono, cuenta.IncluidaEnTotal,
            cuenta.EliminadoEn is not null, cuenta.Version);

    public static CategoriaResponse ToResponse(this Categoria categoria) =>
        new(
            categoria.Id, categoria.Nombre, categoria.Tipo, categoria.Icono, categoria.Color,
            categoria.EsPredeterminada, false, categoria.Version);

    public static MovimientoResponse ToResponse(this Movimiento movimiento) =>
        new(
            movimiento.Id, "privado", movimiento.CuentaId, movimiento.Tipo, movimiento.Monto,
            movimiento.Moneda, movimiento.Descripcion, movimiento.Fecha, movimiento.Estado,
            movimiento.CreadoEn, movimiento.Version);

    public static TarjetaCreditoResponse ToResponse(
        this TarjetaCredito tarjeta,
        Cuenta cuentaPago) =>
        new(
            tarjeta.Id, tarjeta.Nombre, tarjeta.Emisor, tarjeta.UltimosCuatro,
            new(cuentaPago.Id, cuentaPago.Nombre, cuentaPago.Tipo),
            tarjeta.DiaCierre, tarjeta.DiaVencimiento, tarjeta.LimiteCredito,
            tarjeta.SaldoUtilizado, tarjeta.CreditoDisponible, tarjeta.Moneda,
            tarjeta.Color, tarjeta.Version);
}