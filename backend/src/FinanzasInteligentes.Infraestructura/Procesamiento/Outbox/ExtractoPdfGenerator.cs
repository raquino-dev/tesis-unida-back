using System.Globalization;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Procesamiento.Outbox;

internal sealed record ExtractoPdfMetadata(
    DateOnly Desde,
    DateOnly Hasta,
    string Cuenta,
    DateOnly GeneradoEl,
    long Ingresos,
    long Gastos);

internal sealed record ExtractoPdfMovimiento(
    DateOnly Fecha,
    string Concepto,
    string Categoria,
    string Metodo,
    string Tipo,
    long Monto,
    long IngresoAnalitico,
    long GastoAnalitico);

internal static class ExtractoPdfGenerator
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double Margin = 42;
    private const double RowHeight = 18;

    private static readonly PdfColor Navy = new(0.063, 0.165, 0.263);
    private static readonly PdfColor Green = new(0, 0.608, 0.408);
    private static readonly PdfColor Red = new(0.941, 0.267, 0.392);
    private static readonly PdfColor Muted = new(0.420, 0.510, 0.604);
    private static readonly PdfColor Border = new(0.843, 0.894, 0.875);
    private static readonly PdfColor PaleGreen = new(0.937, 0.973, 0.957);
    private static readonly PdfColor Alternate = new(0.969, 0.980, 0.976);

    public static byte[] Crear(
        ExtractoPdfMetadata metadata,
        IReadOnlyCollection<ExtractoPdfMovimiento> movimientos)
    {
        var ordenados = movimientos.OrderByDescending(x => x.Fecha).ToArray();
        var categorias = movimientos
            .Where(x => x.GastoAnalitico != 0)
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Categoria) ? "Sin categoría" : x.Categoria)
            .Select(x => new CategoriaTotal(x.Key, x.Sum(y => y.GastoAnalitico)))
            .Where(x => x.Monto > 0)
            .OrderByDescending(x => x.Monto)
            .Take(5)
            .ToArray();

        var firstPageCapacity = categorias.Length == 0
            ? 18
            : Math.Max(10, 17 - categorias.Length);
        var firstPageRows = ordenados.Take(firstPageCapacity).ToArray();
        var continuaciones = ordenados.Skip(firstPageCapacity).Chunk(34).ToArray();
        var totalPaginas = 1 + continuaciones.Length;
        var paginas = new List<string>
        {
            CrearPrimeraPagina(metadata, categorias, firstPageRows, ordenados.Length, totalPaginas)
        };
        for (var index = 0; index < continuaciones.Length; index++)
            paginas.Add(CrearPaginaContinuacion(
                metadata, continuaciones[index], index + 2, totalPaginas));

        return ConstruirDocumento(paginas);
    }

    private static string CrearPrimeraPagina(
        ExtractoPdfMetadata metadata,
        IReadOnlyCollection<CategoriaTotal> categorias,
        IReadOnlyCollection<ExtractoPdfMovimiento> movimientos,
        int cantidadMovimientos,
        int totalPaginas)
    {
        var canvas = new PdfCanvas();
        Encabezado(canvas, metadata);

        canvas.Text(Margin, 730, "Extracto financiero", 24, Navy, true);
        canvas.Text(Margin, 708,
            "Un resumen claro de tus movimientos durante el período seleccionado.", 11, Navy);

        InfoBand(canvas, metadata);
        Summary(canvas, metadata, cantidadMovimientos);

        var y = 520d;
        canvas.Text(Margin, y, "Principales categorías de gasto", 15, Navy, true);
        y -= 26;
        if (categorias.Count == 0)
        {
            canvas.FillRect(Margin, y - 20, PageWidth - Margin * 2, 28, Alternate);
            canvas.Text(Margin + 10, y - 10, "No hay gastos para el período seleccionado.", 9, Muted);
            y -= 44;
        }
        else
        {
            var total = metadata.Gastos > 0 ? metadata.Gastos : categorias.Sum(x => x.Monto);
            foreach (var categoria in categorias)
            {
                var porcentaje = total == 0 ? 0 : Math.Clamp((double)categoria.Monto / total, 0, 1);
                canvas.Text(Margin, y, Recortar(categoria.Nombre, 24), 8.5, Navy);
                canvas.FillRect(176, y - 2, 160 * porcentaje, 10, Green);
                canvas.RightText(PageWidth - Margin, y + 2, FormatearMonto(categoria.Monto), 8, Muted);
                canvas.RightText(PageWidth - Margin, y - 10, $"{Math.Round(porcentaje * 100)}%", 8, Muted);
                y -= 28;
            }
        }

        y -= 8;
        canvas.Text(Margin, y, "Detalle de movimientos", 15, Navy, true);
        canvas.Text(Margin, y - 18,
            "Los movimientos se muestran del más reciente al más antiguo.", 8.5, Muted);
        Tabla(canvas, movimientos, y - 42);
        Pie(canvas, 1, totalPaginas);
        return canvas.ToString();
    }

    private static string CrearPaginaContinuacion(
        ExtractoPdfMetadata metadata,
        IReadOnlyCollection<ExtractoPdfMovimiento> movimientos,
        int pagina,
        int totalPaginas)
    {
        var canvas = new PdfCanvas();
        Encabezado(canvas, metadata);
        canvas.Text(Margin, 730, "Detalle de movimientos", 18, Navy, true);
        canvas.Text(Margin, 710,
            $"Período {FormatearFecha(metadata.Desde)} - {FormatearFecha(metadata.Hasta)}", 9, Muted);
        Tabla(canvas, movimientos, 680);
        Pie(canvas, pagina, totalPaginas);
        return canvas.ToString();
    }

    private static void Encabezado(PdfCanvas canvas, ExtractoPdfMetadata metadata)
    {
        canvas.FillCircle(57, 784, 14, Green);
        canvas.Line(48, 776, 66, 793, new PdfColor(0.129, 0.788, 0.549), 1.5);
        canvas.Text(82, 791, "Finanza", 12, Navy, true);
        canvas.Text(82, 777, "Pequeños hábitos, grandes logros.", 8.5, Muted);
        canvas.RightText(PageWidth - Margin, 791, "Generado el", 8.5, Muted);
        canvas.RightText(PageWidth - Margin, 777, FormatearFecha(metadata.GeneradoEl), 8.5, Muted);
    }

    private static void InfoBand(PdfCanvas canvas, ExtractoPdfMetadata metadata)
    {
        const double y = 640;
        const double height = 50;
        var width = (PageWidth - Margin * 2) / 3;
        canvas.FillRect(Margin, y, PageWidth - Margin * 2, height, PaleGreen);
        canvas.StrokeRect(Margin, y, PageWidth - Margin * 2, height, Border, 0.6);
        canvas.Line(Margin + width, y, Margin + width, y + height, Border, 0.6);
        canvas.Line(Margin + width * 2, y, Margin + width * 2, y + height, Border, 0.6);
        InfoCell(canvas, Margin + 10, y + 32, "Período",
            $"{FormatearFecha(metadata.Desde)} - {FormatearFecha(metadata.Hasta)}");
        InfoCell(canvas, Margin + width + 10, y + 32, "Cuenta", Recortar(metadata.Cuenta, 28));
        InfoCell(canvas, Margin + width * 2 + 10, y + 32, "Moneda", "Guaraníes (PYG)");
    }

    private static void InfoCell(PdfCanvas canvas, double x, double y, string label, string value)
    {
        canvas.Text(x, y, label, 10, Navy);
        canvas.Text(x, y - 16, value, 9.5, Navy);
    }

    private static void Summary(PdfCanvas canvas, ExtractoPdfMetadata metadata, int count)
    {
        const double y = 565;
        const double height = 52;
        var width = (PageWidth - Margin * 2) / 4;
        var balance = metadata.Ingresos - metadata.Gastos;
        SummaryCell(canvas, Margin, y, width, height, "Ingresos", FormatearMonto(metadata.Ingresos), Green);
        SummaryCell(canvas, Margin + width, y, width, height, "Gastos", FormatearMonto(metadata.Gastos), Red);
        SummaryCell(canvas, Margin + width * 2, y, width, height, "Balance",
            FormatearMontoFirmado(balance), balance >= 0 ? Green : Red);
        SummaryCell(canvas, Margin + width * 3, y, width, height, "Movimientos", count.ToString(CultureInfo.InvariantCulture), Navy);
    }

    private static void SummaryCell(PdfCanvas canvas, double x, double y, double width,
        double height, string label, string value, PdfColor valueColor)
    {
        canvas.StrokeRect(x, y, width, height, Border, 0.6);
        canvas.Text(x + 10, y + 33, label, 8.5, Muted);
        canvas.Text(x + 10, y + 13, Recortar(value, 18), 13, valueColor);
    }

    private static void Tabla(
        PdfCanvas canvas,
        IReadOnlyCollection<ExtractoPdfMovimiento> movimientos,
        double top)
    {
        var widths = new[] { 56d, 126d, 82d, 105d, 62d, 80d };
        var headers = new[] { "Fecha", "Concepto", "Categoría", "Cuenta / método", "Tipo", "Monto" };
        var x = Margin;
        canvas.FillRect(Margin, top - RowHeight, widths.Sum(), RowHeight, PaleGreen);
        for (var i = 0; i < headers.Length; i++)
        {
            if (i == headers.Length - 1)
                canvas.RightText(x + widths[i] - 5, top - 12, headers[i], 7.2, Navy, true);
            else
                canvas.Text(x + 5, top - 12, headers[i], 7.2, Navy, true);
            x += widths[i];
        }

        var y = top - RowHeight;
        var index = 0;
        foreach (var movimiento in movimientos)
        {
            y -= RowHeight;
            if (index++ % 2 == 1) canvas.FillRect(Margin, y, widths.Sum(), RowHeight, Alternate);
            canvas.Line(Margin, y, Margin + widths.Sum(), y, Border, 0.45);
            x = Margin;
            var tipo = TipoVisible(movimiento);
            var positivo = movimiento.IngresoAnalitico > 0 ||
                           movimiento.Tipo.Equals("Reintegro", StringComparison.OrdinalIgnoreCase);
            var monto = $"{(positivo ? "+" : "-")}{FormatearMonto(movimiento.Monto)}";
            var values = new[]
            {
                FormatearFecha(movimiento.Fecha), Recortar(movimiento.Concepto, 25),
                Recortar(movimiento.Categoria, 17), Recortar(movimiento.Metodo, 21), tipo
            };
            for (var i = 0; i < values.Length; i++)
            {
                canvas.Text(x + 5, y + 6, values[i], 7.2, Muted);
                x += widths[i];
            }
            canvas.RightText(Margin + widths.Sum() - 5, y + 6, monto, 7.2,
                positivo ? Green : Red, true);
        }

        if (movimientos.Count == 0)
        {
            canvas.FillRect(Margin, y - 30, widths.Sum(), 30, Alternate);
            canvas.Text(Margin + 10, y - 18, "No hay movimientos para mostrar.", 9, Muted);
        }
    }

    private static string TipoVisible(ExtractoPdfMovimiento movimiento)
    {
        if (movimiento.Tipo.Equals("pago", StringComparison.OrdinalIgnoreCase)) return "Pago tarjeta";
        if (movimiento.Tipo.Equals("reintegro", StringComparison.OrdinalIgnoreCase)) return "Reintegro";
        return movimiento.Tipo.Equals("ingreso", StringComparison.OrdinalIgnoreCase) ? "Ingreso" : "Gasto";
    }

    private static void Pie(PdfCanvas canvas, int pagina, int total)
    {
        canvas.Line(Margin, 38, PageWidth - Margin, 38, Border, 0.6);
        canvas.Text(Margin, 23, "Documento generado por Finanza - Uso informativo", 8, Muted);
        canvas.RightText(PageWidth - Margin, 23, $"Página {pagina} de {total}", 8, Muted);
    }

    private static string FormatearFecha(DateOnly value) => value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string FormatearMonto(long value) =>
        $"Gs. {Math.Abs(value).ToString("N0", new CultureInfo("es-PY"))}";

    private static string FormatearMontoFirmado(long value) =>
        $"{(value < 0 ? "-" : string.Empty)}{FormatearMonto(value)}";

    private static string Recortar(string? value, int max)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "Sin detalle" : value.Trim();
        return normalized.Length <= max ? normalized : $"{normalized[..Math.Max(1, max - 3)]}...";
    }

    private static byte[] ConstruirDocumento(IReadOnlyList<string> paginas)
    {
        var objects = new List<string>();
        var pageIds = Enumerable.Range(0, paginas.Count).Select(i => 3 + i * 2).ToArray();
        var fontRegularId = 3 + paginas.Count * 2;
        var fontBoldId = fontRegularId + 1;
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add($"<< /Type /Pages /Kids [{string.Join(' ', pageIds.Select(x => $"{x} 0 R"))}] /Count {paginas.Count} >>");
        for (var i = 0; i < paginas.Count; i++)
        {
            var streamId = pageIds[i] + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] /Resources << /Font << /F1 {fontRegularId} 0 R /F2 {fontBoldId} 0 R >> >> /Contents {streamId} 0 R >>");
            var length = Encoding.Latin1.GetByteCount(paginas[i]);
            objects.Add($"<< /Length {length} >>\nstream\n{paginas[i]}\nendstream");
        }
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");

        var pdf = new StringBuilder("%PDF-1.7\n%âãÏÓ\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.Latin1.GetByteCount(pdf.ToString()));
            pdf.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }
        var xref = Encoding.Latin1.GetByteCount(pdf.ToString());
        pdf.Append("xref\n0 ").Append(objects.Count + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) pdf.Append(offset.ToString("D10")).Append(" 00000 n \n");
        pdf.Append("trailer << /Size ").Append(objects.Count + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.Latin1.GetBytes(pdf.ToString());
    }

    private sealed record CategoriaTotal(string Nombre, long Monto);
    private readonly record struct PdfColor(double R, double G, double B);

    private sealed class PdfCanvas
    {
        private readonly StringBuilder _stream = new();

        public void Text(double x, double y, string value, double size, PdfColor color, bool bold = false)
        {
            _stream.Append("BT /").Append(bold ? "F2" : "F1").Append(' ')
                .Append(N(size)).Append(" Tf ").Append(Color(color)).Append(" rg 1 0 0 1 ")
                .Append(N(x)).Append(' ').Append(N(y)).Append(" Tm (")
                .Append(Escape(value)).Append(") Tj ET\n");
        }

        public void RightText(double right, double y, string value, double size, PdfColor color, bool bold = false)
        {
            var width = value.Length * size * 0.49;
            Text(Math.Max(Margin, right - width), y, value, size, color, bold);
        }

        public void FillRect(double x, double y, double width, double height, PdfColor color) =>
            _stream.Append(Color(color)).Append(" rg ").Append(N(x)).Append(' ').Append(N(y)).Append(' ')
                .Append(N(width)).Append(' ').Append(N(height)).Append(" re f\n");

        public void StrokeRect(double x, double y, double width, double height, PdfColor color, double lineWidth) =>
            _stream.Append(Color(color)).Append(" RG ").Append(N(lineWidth)).Append(" w ")
                .Append(N(x)).Append(' ').Append(N(y)).Append(' ').Append(N(width)).Append(' ')
                .Append(N(height)).Append(" re S\n");

        public void FillCircle(double x, double y, double radius, PdfColor color)
        {
            var c = radius * 0.5522847498;
            _stream.Append(Color(color)).Append(" rg ")
                .Append(N(x + radius)).Append(' ').Append(N(y)).Append(" m ")
                .Append(N(x + radius)).Append(' ').Append(N(y + c)).Append(' ')
                .Append(N(x + c)).Append(' ').Append(N(y + radius)).Append(' ')
                .Append(N(x)).Append(' ').Append(N(y + radius)).Append(" c ")
                .Append(N(x - c)).Append(' ').Append(N(y + radius)).Append(' ')
                .Append(N(x - radius)).Append(' ').Append(N(y + c)).Append(' ')
                .Append(N(x - radius)).Append(' ').Append(N(y)).Append(" c ")
                .Append(N(x - radius)).Append(' ').Append(N(y - c)).Append(' ')
                .Append(N(x - c)).Append(' ').Append(N(y - radius)).Append(' ')
                .Append(N(x)).Append(' ').Append(N(y - radius)).Append(" c ")
                .Append(N(x + c)).Append(' ').Append(N(y - radius)).Append(' ')
                .Append(N(x + radius)).Append(' ').Append(N(y - c)).Append(' ')
                .Append(N(x + radius)).Append(' ').Append(N(y)).Append(" c f\n");
        }

        public void Line(double x1, double y1, double x2, double y2, PdfColor color, double width) =>
            _stream.Append(Color(color)).Append(" RG ").Append(N(width)).Append(" w ")
                .Append(N(x1)).Append(' ').Append(N(y1)).Append(" m ")
                .Append(N(x2)).Append(' ').Append(N(y2)).Append(" l S\n");

        public override string ToString() => _stream.ToString();

        private static string Escape(string value)
        {
            var normalized = value.Replace('–', '-').Replace('—', '-').Replace('’', '\'');
            var result = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                var current = character <= 255 ? character : '?';
                if (current is '\\' or '(' or ')') result.Append('\\');
                result.Append(current);
            }
            return result.ToString();
        }

        private static string Color(PdfColor value) => $"{N(value.R)} {N(value.G)} {N(value.B)}";
        private static string N(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
