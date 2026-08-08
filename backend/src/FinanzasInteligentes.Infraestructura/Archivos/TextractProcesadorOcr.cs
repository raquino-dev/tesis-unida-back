using System.Globalization;
using Amazon;
using Amazon.Textract;
using Amazon.Textract.Model;
using FinanzasInteligentes.Aplicacion.Abstracciones;
using Microsoft.Extensions.Options;

namespace FinanzasInteligentes.Infraestructura.Archivos;

public sealed class TextractProcesadorOcr :
    IProcesadorOcrDocumento, IDisposable
{
    private readonly TextractOptions options;
    private AmazonTextractClient? client;

    public TextractProcesadorOcr(IOptions<TextractOptions> options)
    {
        this.options = options.Value;
    }

    public bool Habilitado => options.Habilitado;

    public async Task<ResultadoOcrDocumento> Procesar(
        Stream contenido,
        CancellationToken ct)
    {
        if (!Habilitado)
            throw new InvalidOperationException(
                "Amazon Textract no está habilitado.");

        using var buffer = new MemoryStream();
        await contenido.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        client ??= new AmazonTextractClient(
            RegionEndpoint.GetBySystemName(options.Region));
        var response = await client.AnalyzeExpenseAsync(
            new AnalyzeExpenseRequest
            {
                Document = new Document
                {
                    Bytes = buffer
                }
            },
            ct);

        var fields = response.ExpenseDocuments
            .SelectMany(x => x.SummaryFields)
            .Where(x => x.Type?.Text is not null)
            .GroupBy(x => x.Type.Text, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(
                    field => field.ValueDetection?.Confidence ?? 0)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        var amountField = First(
            fields, "TOTAL", "AMOUNT_DUE", "SUBTOTAL");
        var dateField = First(
            fields, "INVOICE_RECEIPT_DATE", "DUE_DATE");
        var vendorField = First(
            fields, "VENDOR_NAME", "RECEIVER_NAME");
        var amount = ParseAmount(amountField?.ValueDetection?.Text);
        var date = ParseDate(dateField?.ValueDetection?.Text);
        var vendor = vendorField?.ValueDetection?.Text?.Trim();
        var confidences = new[]
        {
            amountField?.ValueDetection?.Confidence,
            dateField?.ValueDetection?.Confidence,
            vendorField?.ValueDetection?.Confidence
        }.Where(x => x.HasValue).Select(x => x!.Value / 100d).ToArray();
        var warnings = new List<string>();
        if (amount is null) warnings.Add(
            "Amazon Textract no detectó un total confiable.");
        if (date is null) warnings.Add(
            "Amazon Textract no detectó una fecha válida.");
        if (string.IsNullOrWhiteSpace(vendor)) warnings.Add(
            "Amazon Textract no detectó el comercio.");

        return new(
            amount,
            date,
            string.IsNullOrWhiteSpace(vendor) ? null : vendor,
            confidences.Length == 0 ? 0 : confidences.Average(),
            warnings);
    }

    private static ExpenseField? First(
        IReadOnlyDictionary<string, ExpenseField> fields,
        params string[] types)
    {
        foreach (var type in types)
            if (fields.TryGetValue(type, out var field)) return field;
        return null;
    }

    private static long? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        var lastSeparator = Math.Max(
            trimmed.LastIndexOf(','),
            trimmed.LastIndexOf('.'));
        var hasDecimals =
            lastSeparator >= 0 &&
            trimmed[(lastSeparator + 1)..].Count(char.IsDigit) == 2;
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (hasDecimals && digits.Length > 2)
            digits = digits[..^2];
        return long.TryParse(
            digits, NumberStyles.None, CultureInfo.InvariantCulture,
            out var amount) && amount > 0
            ? amount
            : null;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var formats = new[]
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy",
            "yyyy-MM-dd", "dd/MM/yy", "d/M/yy"
        };
        return DateOnly.TryParseExact(
            value.Trim(), formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    public void Dispose() => client?.Dispose();
}
