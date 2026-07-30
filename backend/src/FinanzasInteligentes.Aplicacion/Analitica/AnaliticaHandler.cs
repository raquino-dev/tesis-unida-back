using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Aplicacion.Excepciones;
using FinanzasInteligentes.Dominio.Analitica;
using FinanzasInteligentes.Dominio.Excepciones;
using FinanzasInteligentes.Dominio.FinanzasPersonales;

namespace FinanzasInteligentes.Aplicacion.Analitica;

public sealed record PeriodoAnaliticaResponse(DateOnly Desde, DateOnly Hasta);
public sealed record CategoriaProyeccionResponse(string Nombre, long MontoProyectado, double Variacion);
public sealed record HistorialProyeccionResponse(string Periodo, long Proyectado, long Real);
public sealed record ProyeccionResponse(
    string Ambito, long MesesHistorial, bool Preliminar, long GastoProyectado,
    long BalanceProyectado, string CategoriaMayorCrecimiento, string NivelRiesgo,
    string VersionModelo, PeriodoAnaliticaResponse Periodo,
    IReadOnlyCollection<CategoriaProyeccionResponse> Categorias,
    IReadOnlyCollection<HistorialProyeccionResponse> Historial, DateTimeOffset GeneradoEn);
public sealed record AlertaResponse(
    Guid Id, string Titulo, string Mensaje, string Nivel, DateTimeOffset Fecha,
    string QueOcurrio, string DatosUtilizados, string Impacto,
    string Recomendacion, bool Leida, long Version);
public sealed record PaginacionAlertaResponse(string? SiguienteCursor, bool HayMas, long Limite);
public sealed record PaginaAlertaResponse(
    IReadOnlyCollection<AlertaResponse> Datos, PaginacionAlertaResponse Paginacion);
public sealed record HistorialScoreResponse(string Periodo, long Score);
public sealed record ScoreResponse(
    long Score, string Estado, string VersionAlgoritmo, PeriodoAnaliticaResponse Periodo,
    IReadOnlyCollection<string> FactoresPositivos, IReadOnlyCollection<string> FactoresNegativos,
    IReadOnlyCollection<HistorialScoreResponse> Historial,
    IReadOnlyCollection<string> Recomendaciones);

public sealed class AnaliticaHandler(IFinanzasRepository finanzas, IUnidadDeTrabajo unidadDeTrabajo)
{
    public async Task<ProyeccionResponse> Proyectar(
        Guid usuarioId, string? ambito, string? periodo, CancellationToken ct)
    {
        ambito ??= "privado";
        if (ambito != "privado")
            throw new DomainException("ambito_invalido", "Esta operación admite el ámbito privado.");
        var meses = periodo switch
        {
            null or "mensual" => 1,
            "trimestral" => 3,
            "semestral" => 6,
            "anual" => 12,
            _ => throw new DomainException("periodo_invalido", "El período no es válido.")
        };
        var movimientos = Confirmados(await finanzas.ListarMovimientos(usuarioId, ct));
        ValidarDatos(movimientos);
        var desdeHistorial = movimientos.Min(x => x.Fecha);
        var mesesHistorial = (DateOnly.FromDateTime(DateTime.UtcNow).Year - desdeHistorial.Year) * 12
            + DateOnly.FromDateTime(DateTime.UtcNow).Month - desdeHistorial.Month + 1;
        var gastos = movimientos.Where(x => x.Tipo == "gasto").ToArray();
        var ingresos = movimientos.Where(x => x.Tipo == "ingreso").ToArray();
        var promedioGasto = gastos.Sum(x => x.Monto) / Math.Max(1, mesesHistorial);
        var promedioIngreso = ingresos.Sum(x => x.Monto) / Math.Max(1, mesesHistorial);
        var categorias = gastos
            .GroupBy(x => x.Categorias.FirstOrDefault()?.Nombre ?? "Sin categoría")
            .Select(x => new CategoriaProyeccionResponse(
                x.Key, x.Sum(y => y.Monto) / Math.Max(1, mesesHistorial) * meses, 0))
            .OrderByDescending(x => x.MontoProyectado).ToArray();
        var historial = movimientos.GroupBy(x => $"{x.Fecha:yyyy-MM}")
            .OrderBy(x => x.Key)
            .Select(x => new HistorialProyeccionResponse(
                x.Key, promedioGasto, x.Where(y => y.Tipo == "gasto").Sum(y => y.Monto)))
            .ToArray();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        return new(
            ambito, mesesHistorial, mesesHistorial < 3, promedioGasto * meses,
            (promedioIngreso - promedioGasto) * meses,
            categorias.FirstOrDefault()?.Nombre ?? "Sin datos",
            promedioGasto > promedioIngreso ? "alto" : promedioGasto * 10 > promedioIngreso * 8 ? "medio" : "bajo",
            "gastos-v1", new(hoy, hoy.AddMonths(meses).AddDays(-1)),
            categorias, historial, DateTimeOffset.UtcNow);
    }

    public async Task<PaginaAlertaResponse> ListarAlertas(
        Guid usuarioId, string? cursor, long limite, string? nivel, bool? leida,
        DateOnly? desde, DateOnly? hasta, CancellationToken ct)
    {
        if (limite is < 1 or > 100)
            throw new DomainException("limite_invalido", "El límite debe estar entre 1 y 100.");
        Guid? cursorId = null;
        if (cursor is not null)
        {
            if (!Guid.TryParse(cursor, out var parsed))
                throw new DomainException("cursor_invalido", "El cursor no es válido.");
            cursorId = parsed;
        }
        var items = (await finanzas.ListarAlertas(usuarioId, ct))
            .Where(x => !x.Archivada)
            .Where(x => nivel is null || x.Nivel == nivel)
            .Where(x => leida is null || x.Leida == leida)
            .Where(x => desde is null || DateOnly.FromDateTime(x.CreadoEn.UtcDateTime) >= desde)
            .Where(x => hasta is null || DateOnly.FromDateTime(x.CreadoEn.UtcDateTime) <= hasta)
            .Where(x => cursorId is null || x.Id.CompareTo(cursorId.Value) < 0)
            .Take(checked((int)limite + 1)).ToArray();
        var hayMas = items.Length > limite;
        var pagina = items.Take(checked((int)limite)).ToArray();
        return new(pagina.Select(Map).ToArray(),
            new(hayMas ? pagina[^1].Id.ToString() : null, hayMas, limite));
    }

    public async Task<AlertaResponse> ObtenerAlerta(
        Guid usuarioId, Guid alertaId, CancellationToken ct) =>
        Map(await Existente(usuarioId, alertaId, true, ct));

    public async Task<AlertaResponse> ActualizarAlerta(
        Guid usuarioId, Guid alertaId, long version, bool? leida, bool? archivada,
        CancellationToken ct)
    {
        var alerta = await Existente(usuarioId, alertaId, false, ct);
        if (alerta.Version != version)
            throw new PreconditionFailedException(
                "etag_desactualizado", "La versión del recurso está desactualizada.");
        alerta.Actualizar(leida, archivada);
        await unidadDeTrabajo.GuardarCambios(ct);
        return Map(alerta);
    }

    public async Task<ScoreResponse> ObtenerScore(Guid usuarioId, CancellationToken ct)
    {
        var movimientos = Confirmados(await finanzas.ListarMovimientos(usuarioId, ct));
        ValidarDatos(movimientos);
        var desde = movimientos.Min(x => x.Fecha);
        var hasta = movimientos.Max(x => x.Fecha);
        var ingresos = movimientos.Where(x => x.Tipo == "ingreso").Sum(x => x.Monto);
        var gastos = movimientos.Where(x => x.Tipo == "gasto").Sum(x => x.Monto);
        var balance = ingresos - gastos;
        var score = 50L;
        var positivos = new List<string>();
        var negativos = new List<string>();
        var recomendaciones = new List<string>();
        if (balance >= 0) { score += 25; positivos.Add("Balance acumulado no negativo."); }
        else { score -= 25; negativos.Add("Los gastos superan a los ingresos."); }
        if (ingresos > 0 && gastos * 100 / ingresos <= 80)
        { score += 15; positivos.Add("El gasto es igual o menor al 80% de los ingresos."); }
        else { negativos.Add("El margen de ahorro es reducido."); recomendaciones.Add("Defina un presupuesto mensual."); }
        var cuentas = await finanzas.ListarCuentas(usuarioId, ct);
        if (cuentas.All(x => x.SaldoActual >= 0))
        { score += 10; positivos.Add("No hay cuentas con saldo negativo."); }
        else { score -= 10; negativos.Add("Hay cuentas con saldo negativo."); recomendaciones.Add("Priorice regularizar saldos negativos."); }
        score = Math.Clamp(score, 0, 100);
        if (recomendaciones.Count == 0) recomendaciones.Add("Mantenga el seguimiento periódico de sus finanzas.");
        var estado = score >= 80 ? "excelente" : score >= 60 ? "saludable" : score >= 40 ? "en-observacion" : "critico";
        return new(score, estado, "score-v1", new(desde, hasta), positivos, negativos,
            [new($"{hasta:yyyy-MM}", score)], recomendaciones);
    }

    private static Movimiento[] Confirmados(IReadOnlyCollection<Movimiento> movimientos) =>
        movimientos.Where(x => x.Estado == "confirmado").ToArray();

    private static void ValidarDatos(Movimiento[] movimientos)
    {
        if (movimientos.Length < 3)
            throw new DomainException(
                "datos_insuficientes", "Se requieren al menos tres movimientos confirmados.");
    }

    private async Task<AlertaFinanciera> Existente(
        Guid usuarioId, Guid alertaId, bool soloLectura, CancellationToken ct) =>
        await finanzas.ObtenerAlerta(usuarioId, alertaId, soloLectura, ct)
        ?? throw new NotFoundException("alerta_no_encontrada", "La alerta no existe.");

    private static AlertaResponse Map(AlertaFinanciera x) =>
        new(x.Id, x.Titulo, x.Mensaje, x.Nivel, x.CreadoEn, x.QueOcurrio,
            x.DatosUtilizados, x.Impacto, x.Recomendacion, x.Leida, x.Version);
}