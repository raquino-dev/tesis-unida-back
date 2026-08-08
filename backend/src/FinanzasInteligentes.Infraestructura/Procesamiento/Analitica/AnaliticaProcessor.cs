using FinanzasInteligentes.Dominio.Analitica;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using FinanzasInteligentes.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace FinanzasInteligentes.Infraestructura.Procesamiento.Analitica;

public sealed class AnaliticaProcessor(FinanzasDbContext db) : IAnaliticaProcessor
{
    public async Task<int> Procesar(CancellationToken cancellationToken)
    {
        var periodo = DateTimeOffset.UtcNow.ToString("yyyy-MM");
        var cuentas = await db.Cuentas.AsNoTracking()
            .Where(x => x.EliminadoEn == null && x.SaldoActual < 0)
            .ToListAsync(cancellationToken);
        var creadas = 0;
        foreach (var cuenta in cuentas)
        {
            var clave = $"saldo-negativo:{cuenta.Id}:{periodo}";
            if (await db.AlertasFinancieras.AnyAsync(
                x => x.UsuarioId == cuenta.UsuarioId && x.ClaveDeduplicacion == clave,
                cancellationToken)) continue;
            var alerta = AlertaFinanciera.Crear(
                cuenta.UsuarioId, "saldo-negativo", "critica",
                "Cuenta con saldo negativo",
                $"La cuenta {cuenta.Nombre} tiene un saldo de {cuenta.SaldoActual:N0} PYG.",
                "El saldo disponible quedó por debajo de cero.",
                "Saldo actual de la cuenta.",
                "Puede generar intereses, rechazos o falta de liquidez.",
                "Regularice la cuenta y revise sus próximos movimientos.",
                clave);
            db.AlertasFinancieras.Add(alerta);
            AgregarNotificacion(alerta);
            creadas++;
        }

        var inicioMes = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var movimientos = await db.Movimientos.AsNoTracking()
            .Where(x => x.Estado == "confirmado" && x.Fecha >= inicioMes)
            .ToListAsync(cancellationToken);
        foreach (var grupo in movimientos.GroupBy(x => x.UsuarioId))
        {
            var ingresos = grupo.Where(x => x.Tipo == "ingreso").Sum(x => x.Monto);
            var gastos = grupo.Where(x => x.Tipo == "gasto").Sum(x => x.Monto);
            if (gastos <= ingresos || gastos == 0) continue;
            var clave = $"gastos-superiores:{periodo}";
            if (await db.AlertasFinancieras.AnyAsync(
                x => x.UsuarioId == grupo.Key && x.ClaveDeduplicacion == clave,
                cancellationToken)) continue;
            var alerta = AlertaFinanciera.Crear(
                grupo.Key, "gastos-superiores", "advertencia",
                "Gastos superiores a ingresos",
                "Los gastos confirmados del mes superan a los ingresos.",
                $"Gastos: {gastos:N0} PYG; ingresos: {ingresos:N0} PYG.",
                "Movimientos confirmados del mes actual.",
                "El balance mensual proyectado es negativo.",
                "Revise gastos no esenciales y ajuste su presupuesto.",
                clave);
            db.AlertasFinancieras.Add(alerta);
            AgregarNotificacion(alerta);
            creadas++;
        }

        if (creadas > 0) await db.SaveChangesAsync(cancellationToken);
        return creadas;
    }

    private void AgregarNotificacion(AlertaFinanciera alerta) =>
        db.EventosOutbox.Add(EventoOutbox.Crear(
            "alerta.financiera-creada",
            "alerta-financiera",
            alerta.Id,
            new
            {
                alerta.UsuarioId,
                AlertaId = alerta.Id,
                alerta.Titulo,
                alerta.Mensaje
            },
            null));
}
