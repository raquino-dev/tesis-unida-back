using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.Aplicacion.Analitica;

public sealed record PeriodoDashboardResponse(DateOnly Desde, DateOnly Hasta);
public sealed record CategoriaDashboardResponse(
    Guid CategoriaId, string Nombre, long Monto, double Porcentaje);
public sealed record RecurrenteDashboardResponse(string Nombre, long Monto, DateOnly Fecha);
public sealed record DashboardResponse(
    string Ambito, PeriodoDashboardResponse Periodo, long Ingresos, long Gastos,
    long Balance, long PresupuestoTotal, long PresupuestoDisponible, long ScoreFinanciero,
    IReadOnlyCollection<CategoriaDashboardResponse> CategoriasPrincipales,
    IReadOnlyCollection<RecurrenteDashboardResponse> ProximosRecurrentes,
    IReadOnlyCollection<string> AlertasDestacadas);
public sealed record TendenciaReporteResponse(string Periodo, long Ingresos, long Gastos);
public sealed record ReporteResponse(
    string Ambito, string Rango, DateOnly Desde, DateOnly Hasta,
    long Ingresos, long Gastos, long Balance,
    IReadOnlyCollection<CategoriaDashboardResponse> Distribucion,
    IReadOnlyCollection<TendenciaReporteResponse> Tendencia,
    IReadOnlyCollection<string> Observaciones);

public sealed class DashboardYReportesHandler(IFinanzasRepository finanzas)
{
    public async Task<DashboardResponse> ObtenerDashboard(
        Guid usuarioId, string? ambito, DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        ValidarAmbito(ambito);
        var periodo = ResolverPeriodoDashboard(desde, hasta);
        var movimientos = FiltrarConfirmados(
            await finanzas.ListarMovimientos(usuarioId, ct), periodo.Desde, periodo.Hasta);
        var ingresos = Sumar(movimientos, "ingreso");
        var gastos = Sumar(movimientos, "gasto");
        var presupuestos = (await finanzas.ListarPresupuestos(usuarioId, ct))
            .Where(x => x.Estado == "activo").ToArray();
        var presupuestoTotal = presupuestos.Sum(x => x.Monto);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var recurrentes = (await finanzas.ListarMovimientosRecurrentes(usuarioId, ct))
            .Where(x => x.Estado == "activa" && x.ProximaEjecucion >= hoy)
            .OrderBy(x => x.ProximaEjecucion)
            .Take(5)
            .Select(x => new RecurrenteDashboardResponse(
                x.Descripcion, x.Monto, x.ProximaEjecucion))
            .ToArray();
        var alertas = (await finanzas.ListarAlertas(usuarioId, ct))
            .Where(x => !x.Archivada && !x.Leida)
            .OrderByDescending(x => x.Nivel == "critica")
            .ThenByDescending(x => x.Nivel == "advertencia")
            .ThenByDescending(x => x.CreadoEn)
            .Take(5)
            .Select(x => x.Titulo)
            .ToArray();

        return new(
            "privado", periodo, ingresos, gastos, ingresos - gastos,
            presupuestoTotal, Math.Max(0, presupuestoTotal - gastos),
            CalcularScore(ingresos, gastos),
            CalcularDistribucion(movimientos, gastos, 5), recurrentes, alertas);
    }

    public async Task<ReporteResponse> ObtenerReporte(
        Guid usuarioId, string? rango, DateOnly? desde, DateOnly? hasta,
        string? tipo, Guid? categoriaId, Guid? cuentaId, CancellationToken ct)
    {
        var periodo = ResolverPeriodoReporte(rango, desde, hasta);
        ValidarTipo(tipo);
        IEnumerable<Movimiento> consulta = FiltrarConfirmados(
            await finanzas.ListarMovimientos(usuarioId, ct), periodo.Desde, periodo.Hasta);
        if (tipo is not null) consulta = consulta.Where(x => x.Tipo == tipo);
        if (categoriaId is not null)
            consulta = consulta.Where(x => x.Categorias.Any(c => c.Id == categoriaId));
        if (cuentaId is not null) consulta = consulta.Where(x => x.CuentaId == cuentaId);

        var movimientos = consulta.ToArray();
        var ingresos = Sumar(movimientos, "ingreso");
        var gastos = Sumar(movimientos, "gasto");
        var distribucion = CalcularDistribucion(movimientos, gastos);
        var tendencia = movimientos
            .GroupBy(x => $"{x.Fecha:yyyy-MM}")
            .OrderBy(x => x.Key)
            .Select(x => new TendenciaReporteResponse(
                x.Key, Sumar(x, "ingreso"), Sumar(x, "gasto")))
            .ToArray();

        return new(
            "privado", periodo.Rango, periodo.Desde, periodo.Hasta,
            ingresos, gastos, ingresos - gastos, distribucion, tendencia,
            CrearObservaciones(ingresos, gastos, distribucion));
    }

    public static PeriodoReporte ResolverPeriodoReporte(
        string? rango, DateOnly? desde, DateOnly? hasta)
    {
        rango ??= desde is not null || hasta is not null ? "personalizado" : "mes";
        if (rango is not ("semana" or "mes" or "trimestre" or "anio" or "personalizado"))
            throw new DomainException("rango_invalido", "El rango solicitado no es válido.");

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly inicio;
        DateOnly fin;
        if (rango == "personalizado")
        {
            if (desde is null || hasta is null)
                throw new DomainException(
                    "fechas_requeridas", "El rango personalizado requiere desde y hasta.");
            inicio = desde.Value;
            fin = hasta.Value;
        }
        else
        {
            (inicio, fin) = rango switch
            {
                "semana" => ResolverSemana(hoy),
                "trimestre" => ResolverTrimestre(hoy),
                "anio" => (new(hoy.Year, 1, 1), new(hoy.Year, 12, 31)),
                _ => (new(hoy.Year, hoy.Month, 1),
                    new DateOnly(hoy.Year, hoy.Month, 1).AddMonths(1).AddDays(-1))
            };
            inicio = desde ?? inicio;
            fin = hasta ?? fin;
        }

        ValidarOrden(inicio, fin);
        return new(rango, inicio, fin);
    }

    private static PeriodoDashboardResponse ResolverPeriodoDashboard(
        DateOnly? desde, DateOnly? hasta)
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var inicio = desde ?? new DateOnly(hoy.Year, hoy.Month, 1);
        var fin = hasta ?? inicio.AddMonths(1).AddDays(-1);
        ValidarOrden(inicio, fin);
        return new(inicio, fin);
    }

    private static (DateOnly Desde, DateOnly Hasta) ResolverSemana(DateOnly hoy)
    {
        var diasDesdeLunes = ((int)hoy.DayOfWeek + 6) % 7;
        var inicio = hoy.AddDays(-diasDesdeLunes);
        return (inicio, inicio.AddDays(6));
    }

    private static (DateOnly Desde, DateOnly Hasta) ResolverTrimestre(DateOnly hoy)
    {
        var primerMes = ((hoy.Month - 1) / 3) * 3 + 1;
        var inicio = new DateOnly(hoy.Year, primerMes, 1);
        return (inicio, inicio.AddMonths(3).AddDays(-1));
    }

    private static void ValidarOrden(DateOnly desde, DateOnly hasta)
    {
        if (hasta < desde)
            throw new DomainException("rango_invalido", "La fecha hasta no puede preceder a desde.");
    }

    private static void ValidarAmbito(string? ambito)
    {
        if (ambito is not (null or "privado"))
            throw new DomainException(
                "ambito_invalido", "Esta operación admite únicamente el ámbito privado.");
    }

    private static void ValidarTipo(string? tipo)
    {
        if (tipo is not (null or "ingreso" or "gasto"))
            throw new DomainException("tipo_invalido", "El tipo debe ser ingreso o gasto.");
    }

    private static Movimiento[] FiltrarConfirmados(
        IReadOnlyCollection<Movimiento> movimientos, DateOnly desde, DateOnly hasta) =>
        movimientos.Where(x =>
            x.Estado == "confirmado" && x.Fecha >= desde && x.Fecha <= hasta).ToArray();

    private static long Sumar(IEnumerable<Movimiento> movimientos, string tipo) =>
        movimientos.Where(x => x.Tipo == tipo).Sum(x => x.Monto);

    private static CategoriaDashboardResponse[] CalcularDistribucion(
        IEnumerable<Movimiento> movimientos, long gastos, int? limite = null)
    {
        var consulta = movimientos
            .Where(x => x.Tipo == "gasto" && x.Categorias.Count > 0)
            .Select(x => new { Movimiento = x, Categoria = x.Categorias.First() })
            .GroupBy(x => new { x.Categoria.Id, x.Categoria.Nombre })
            .Select(x => new CategoriaDashboardResponse(
                x.Key.Id, x.Key.Nombre, x.Sum(y => y.Movimiento.Monto),
                gastos == 0 ? 0 : (double)x.Sum(y => y.Movimiento.Monto) / gastos))
            .OrderByDescending(x => x.Monto);
        return (limite is null ? consulta : consulta.Take(limite.Value)).ToArray();
    }

    private static string[] CrearObservaciones(
        long ingresos, long gastos, IReadOnlyCollection<CategoriaDashboardResponse> distribucion)
    {
        var observaciones = new List<string>();
        if (distribucion.FirstOrDefault() is { } principal)
            observaciones.Add($"{principal.Nombre} es la categoría con mayor gasto.");
        if (ingresos == 0 && gastos > 0)
            observaciones.Add("No se registraron ingresos en el período.");
        else if (gastos > ingresos)
            observaciones.Add("Los gastos superaron a los ingresos en el período.");
        else if (ingresos > 0)
            observaciones.Add("El período cerró con balance no negativo.");
        if (observaciones.Count == 0)
            observaciones.Add("No hay movimientos para los filtros seleccionados.");
        return observaciones.ToArray();
    }

    private static long CalcularScore(long ingresos, long gastos) =>
        ingresos <= 0
            ? 0
            : Math.Clamp((long)Math.Round(100d * (ingresos - gastos) / ingresos), 0, 100);
}

public sealed record PeriodoReporte(string Rango, DateOnly Desde, DateOnly Hasta);