using FinanzasInteligentes.Aplicacion.Abstracciones;
using FinanzasInteligentes.Dominio.Infraestructura.Entidades;
using FinanzasInteligentes.Infraestructura.Persistencia;
using FinanzasInteligentes.Infraestructura.Correo;
using FinanzasInteligentes.Infraestructura.Notificaciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace FinanzasInteligentes.Infraestructura.Procesamiento.Outbox;

public interface IOutboxProcessor
{
    Task<int> ProcesarLote(CancellationToken cancellationToken);
}

public sealed class OutboxProcessor(
    FinanzasDbContext db,
    IArchivoStorage storage,
    IProcesadorOcrDocumento ocr,
    ICorreoSender correo,
    IPushNotificationSender push,
    Microsoft.Extensions.Options.IOptions<CorreoOptions> correoOptions,
    ILogger<OutboxProcessor> logger) : IOutboxProcessor
{
    private const int MaximoIntentosOutbox = 5;

    public async Task<int> ProcesarLote(CancellationToken cancellationToken)
    {
        var eventos = await db.EventosOutbox
            .Where(x => x.Estado == "pendiente" && x.DisponibleEn <= DateTimeOffset.UtcNow)
            .OrderBy(x => x.CreadoEn)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var evento in eventos)
        {
            logger.LogInformation(
                "Procesando {Tipo} de {AgregadoTipo}/{AgregadoId}",
                evento.Tipo,
                evento.AgregadoTipo,
                evento.AgregadoId);

            try
            {
                if (evento.Tipo == "perfil.eliminacion-solicitada")
                    await ProcesarEliminacionPerfil(evento.AgregadoId, cancellationToken);
                else if (evento.Tipo == "grupo-familiar.eliminacion-solicitada")
                    await ProcesarEliminacionGrupo(evento.AgregadoId, cancellationToken);
                else if (evento.Tipo == "documento.procesamiento-solicitado")
                    await ProcesarDocumento(evento.AgregadoId, cancellationToken);
                else if (evento.Tipo == "exportacion.solicitada")
                    await ProcesarExportacion(evento.AgregadoId, cancellationToken);
                else if (evento.Tipo == "otp.solicitado")
                    await EnviarOtp(evento, cancellationToken);
                else if (evento.Tipo == "contrasena.recuperacion-solicitada")
                    await EnviarRecuperacion(evento, cancellationToken);
                else if (evento.Tipo is "suscripcion.activada" or "suscripcion.cancelada" or "suscripcion.restaurada")
                    await EnviarSuscripcion(evento, cancellationToken);
                else if (evento.Tipo == "suscripcion.aviso-vencimiento")
                    await EnviarAvisoVencimiento(evento, cancellationToken);
                else if (evento.Tipo == "alerta.financiera-creada")
                    await EnviarAlerta(evento.Payload.RootElement, cancellationToken);

                evento.MarcarProcesado(
                    evento.Tipo is "otp.solicitado" or
                        "contrasena.recuperacion-solicitada");
            }
            catch (LimiteCorreoExcedidoException exception)
            {
                evento.MarcarFallido(exception.Message);
                logger.LogCritical(exception,
                    "Se bloqueó el correo del evento {EventoId} por el circuito de seguridad.",
                    evento.Id);
            }
            catch (Exception exception)
            {
                evento.ReprogramarError(exception, MaximoIntentosOutbox);
                logger.LogError(exception,
                    "Falló el evento outbox {EventoId}; intento {Intentos}/{MaximoIntentos}.",
                    evento.Id, evento.Intentos, MaximoIntentosOutbox);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
        return eventos.Count;
    }

    private async Task EnviarOtp(EventoOutbox evento, CancellationToken ct)
    {
        var payload = evento.Payload.RootElement;
        var destinatario = payload.GetProperty("Correo").GetString()!;
        var codigo = payload.GetProperty("Codigo").GetString()!;
        var texto = $"Tu código de verificación es {codigo}. Expira en 5 minutos.";
        await EnviarCorreoUnaVez(evento, destinatario, "Código de verificación", texto,
            $"<p>Tu código de verificación es <strong>{System.Net.WebUtility.HtmlEncode(codigo)}</strong>.</p><p>Expira en 5 minutos.</p>", ct);
    }

    private async Task EnviarRecuperacion(EventoOutbox evento, CancellationToken ct)
    {
        var payload = evento.Payload.RootElement;
        var destinatario = payload.GetProperty("Correo").GetString()!;
        // Los eventos pendientes del formato anterior contenían un token largo
        // que ya no puede consumirse con el contrato nuevo. Se descartan sin
        // reintentos para no bloquear el outbox ni enviar instrucciones inválidas.
        if (!payload.TryGetProperty("RecuperacionId", out var idProperty) ||
            !payload.TryGetProperty("Codigo", out var codigoProperty))
            return;
        var recuperacionId = idProperty.GetGuid();
        var codigo = codigoProperty.GetString()!;
        var baseUrl = correoOptions.Value.UrlAplicacion.TrimEnd('/');
        var url = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(
            $"{baseUrl}/reset-password", new Dictionary<string, string?>
            {
                ["recoveryId"] = recuperacionId.ToString(),
                ["code"] = codigo
            });
        var codigoSeguro = System.Net.WebUtility.HtmlEncode(codigo);
        var urlSegura = System.Net.WebUtility.HtmlEncode(url);
        var texto =
            $"Código de recuperación: {codigo}. " +
            $"También puedes abrir este enlace: {url}. Expira en 30 minutos.";
        await EnviarCorreoUnaVez(evento, destinatario, "Restablecer contraseña", texto,
            $"<p>Solicitaste restablecer tu contraseña.</p>" +
            $"<p>Tu código de recuperación es:</p>" +
            $"<p style=\"font-size:24px;font-weight:bold;letter-spacing:6px\">{codigoSeguro}</p>" +
            $"<p><a href=\"{urlSegura}\">Abrir Finanzas Inteligentes</a></p>" +
            $"<p>El código expira en 30 minutos, admite cinco intentos y solo puede utilizarse una vez.</p>", ct);
    }

    private async Task EnviarSuscripcion(
        EventoOutbox evento, CancellationToken ct)
    {
        var tipo = evento.Tipo;
        var payload = evento.Payload.RootElement;
        var usuarioId = payload.GetProperty("UsuarioId").GetGuid();
        var usuario = await db.Usuarios.AsNoTracking().Include(x => x.Preferencias)
            .SingleOrDefaultAsync(x => x.Id == usuarioId, ct);
        if (usuario is null) return;
        var accion = tipo switch
        {
            "suscripcion.activada" => "activada",
            "suscripcion.cancelada" => "cancelada",
            _ => "restaurada"
        };
        var texto = $"Tu suscripción fue {accion}.";
        if (usuario.Preferencias.NotificacionesCorreo)
            await EnviarCorreoUnaVez(evento, usuario.Correo, $"Suscripción {accion}", texto, $"<p>{texto}</p>", ct);
        await push.Enviar(
            usuarioId,
            $"Suscripción {accion}",
            texto,
            new Dictionary<string, string>
            {
                ["tipo"] = "suscripcion",
                ["route"] = "/subscription"
            },
            ct);
    }

    private async Task EnviarAvisoVencimiento(
        EventoOutbox evento, CancellationToken ct)
    {
        var payload = evento.Payload.RootElement;
        var usuarioId = payload.GetProperty("UsuarioId").GetGuid();
        var usuario = await db.Usuarios.AsNoTracking().Include(x => x.Preferencias)
            .SingleOrDefaultAsync(x => x.Id == usuarioId, ct);
        if (usuario is null) return;
        var tipo = payload.GetProperty("Tipo").GetString();
        var fin = payload.GetProperty("FinPeriodoEn").GetDateTimeOffset();
        var mensaje = tipo switch
        {
            "vence-7-dias" => "Tu suscripción vence en 7 días.",
            "vence-3-dias" => "Tu suscripción vence en 3 días.",
            "vence-1-dia" => "Tu suscripción vence mañana.",
            "vence-hoy" => "Tu suscripción vence hoy.",
            _ => "Tu suscripción ha vencido."
        };
        var texto = $"{mensaje} Fecha del periodo: {fin:yyyy-MM-dd}.";
        if (usuario.Preferencias.NotificacionesCorreo)
            await EnviarCorreoUnaVez(evento,
                usuario.Correo, "Información de tu suscripción", texto,
                $"<p>{mensaje}</p><p>Fecha del periodo: <strong>{fin:yyyy-MM-dd}</strong>.</p>", ct);
        await push.Enviar(
            usuarioId,
            "Información de tu suscripción",
            texto,
            new Dictionary<string, string>
            {
                ["tipo"] = "suscripcion",
                ["route"] = "/subscription"
            },
            ct);
    }

    private async Task EnviarCorreoUnaVez(
        EventoOutbox evento,
        string destinatario,
        string asunto,
        string texto,
        string html,
        CancellationToken ct)
    {
        const string canal = "correo";
        if (await db.EntregasOutbox.AnyAsync(x =>
            x.EventoOutboxId == evento.Id && x.Canal == canal, ct))
        {
            logger.LogWarning(
                "Se omitió un correo duplicado del evento {EventoId}; ya fue reservado o enviado.",
                evento.Id);
            return;
        }

        var ahora = DateTimeOffset.UtcNow;
        var opciones = correoOptions.Value;
        var destinatarioHash = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(destinatario.Trim().ToUpperInvariant()))).ToLowerInvariant();
        var desdeHora = ahora.AddHours(-1);
        var enviadosDestinatario = await db.EntregasOutbox.CountAsync(x =>
            x.Canal == canal && x.DestinatarioHash == destinatarioHash &&
            x.ReservadaEn >= desdeHora, ct);
        if (enviadosDestinatario >= opciones.MaximoEnviosPorDestinatarioHora)
            throw new LimiteCorreoExcedidoException(
                "Límite horario por destinatario alcanzado.");

        // La reserva se confirma antes de SES. Si la llamada externa es ambigua,
        // priorizamos no repetir ni cobrar un correo duplicado.
        var entrega = EntregaOutbox.Reservar(evento.Id, canal, destinatarioHash);
        db.EntregasOutbox.Add(entrega);
        await db.SaveChangesAsync(ct);

        var enviado = await correo.Enviar(destinatario, asunto, texto, html, ct);
        if (enviado)
            entrega.MarcarEnviada();
        else
            entrega.MarcarOmitida();
        await db.SaveChangesAsync(ct);
    }

    private sealed class LimiteCorreoExcedidoException(string message) : Exception(message);

    private Task EnviarAlerta(
        System.Text.Json.JsonElement payload, CancellationToken ct)
    {
        var usuarioId = payload.GetProperty("UsuarioId").GetGuid();
        var alertaId = payload.GetProperty("AlertaId").GetGuid();
        return push.Enviar(
            usuarioId,
            payload.GetProperty("Titulo").GetString()!,
            payload.GetProperty("Mensaje").GetString()!,
            new Dictionary<string, string>
            {
                ["tipo"] = "alerta-financiera",
                ["alertaId"] = alertaId.ToString(),
                ["route"] = $"/alerts/{alertaId}"
            },
            ct);
    }

    private async Task ProcesarDocumento(Guid id, CancellationToken ct)
    {
        var proceso = await db.ProcesamientosDocumentales.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (proceso is null || proceso.Estado is not ("pendiente" or "procesando")) return;
        var documento = await db.DocumentosFinancieros.AsNoTracking()
            .SingleAsync(x => x.Id == proceso.DocumentoId, ct);
        if (proceso.Estado == "pendiente")
        {
            proceso.Iniciar();
            await db.SaveChangesAsync(ct);
        }
        if (proceso.Tipo == "sifen")
        {
            await using var stream = await storage.Abrir(documento.ClaveObjeto, ct);
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = 10_485_760
            };
            using var reader = XmlReader.Create(stream, settings);
            var xml = await XDocument.LoadAsync(reader, LoadOptions.None, ct);
            string? Valor(string local) =>
                xml.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName.Equals(
                        local, StringComparison.OrdinalIgnoreCase))?.Value
                ?? xml.Descendants()
                    .Attributes()
                    .FirstOrDefault(x => x.Name.LocalName.Equals(
                        local, StringComparison.OrdinalIgnoreCase))?.Value;
            var totalTexto = Valor("dTotGralOpe") ?? Valor("total");
            long.TryParse(totalTexto?.Split('.')[0], out var monto);
            proceso.Completar(new
            {
                monto = monto > 0 ? monto : (long?)null,
                fecha = Valor("dFeEmiDE"),
                comercio = Valor("dNomEmi"),
                categoriaSugeridaId = (Guid?)null,
                cdcSifen = Valor("Id") ?? Valor("CDC")
            }, monto > 0 ? 0.95 : 0.6, monto > 0 ? [] : ["No se detectó el total del comprobante."]);
        }
        else
        {
            if (!ocr.Habilitado)
            {
                proceso.Completar(new
                {
                    monto = (long?)null,
                    fecha = (DateOnly?)null,
                    comercio = (string?)null,
                    categoriaSugeridaId = (Guid?)null,
                    cdcSifen = (string?)null
                }, 0, "OCR deshabilitado; complete los datos manualmente.");
            }
            else
            {
                await using var stream = await storage.Abrir(
                    documento.ClaveObjeto, ct);
                var resultado = await ocr.Procesar(stream, ct);
                proceso.Completar(new
                {
                    monto = resultado.Monto,
                    fecha = resultado.Fecha,
                    comercio = resultado.Comercio,
                    categoriaSugeridaId = (Guid?)null,
                    cdcSifen = (string?)null
                }, resultado.Confianza, resultado.Advertencias.ToArray());
            }
        }
    }

    private async Task ProcesarExportacion(Guid id, CancellationToken ct)
    {
        var exportacion = await db.Exportaciones.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (exportacion is null || exportacion.Estado is not ("pendiente" or "procesando")) return;
        if (exportacion.Estado == "pendiente")
        {
            exportacion.Iniciar();
            await db.SaveChangesAsync(ct);
        }
        var filas = exportacion.Ambito == "privado"
            ? (await db.Movimientos.AsNoTracking().Where(x =>
                x.UsuarioId == exportacion.UsuarioId && x.Estado == "confirmado" &&
                x.Fecha >= exportacion.Desde && x.Fecha <= exportacion.Hasta &&
                (exportacion.TipoMovimiento == null ||
                 (exportacion.TipoMovimiento == "ingreso" && x.Tipo == "ingreso" &&
                  x.TransferenciaId == null && x.TarjetaCreditoId == null) ||
                 (exportacion.TipoMovimiento == "gasto" &&
                  ((x.Tipo == "gasto" && x.TransferenciaId == null) ||
                   x.OperacionTarjeta == "reintegro"))) &&
                (exportacion.CuentaId == null ||
                 (x.CuentaId == exportacion.CuentaId &&
                  (x.TarjetaCreditoId == null || x.OperacionTarjeta == "pago"))) &&
                (exportacion.CategoriaId == null || x.Categorias.Any(c => c.Id == exportacion.CategoriaId)) &&
                (exportacion.Documento == "cualquiera" ||
                 (exportacion.Documento == "con-documento" && x.DocumentoId != null) ||
                (exportacion.Documento == "sin-documento" && x.DocumentoId == null)))
                .OrderBy(x => x.Fecha).Select(x => new ExportRow(
                    x.Fecha, x.Tipo, x.Monto, x.Descripcion, x.TransferenciaId != null,
                    x.OperacionTarjeta,
                    x.Tipo == "ingreso" && x.TransferenciaId == null &&
                    x.TarjetaCreditoId == null ? x.Monto : 0,
                    x.TransferenciaId != null || x.OperacionTarjeta == "pago"
                        ? 0
                        : x.OperacionTarjeta == "reintegro"
                            ? -x.Monto
                            : x.Tipo == "gasto" ? x.Monto : 0))
                .ToListAsync(ct))
            : (await db.MovimientosFamiliares.AsNoTracking().Where(x =>
                x.GrupoFamiliarId == exportacion.GrupoFamiliarId &&
                x.Estado == "confirmado" && x.Fecha >= exportacion.Desde &&
                x.Fecha <= exportacion.Hasta &&
                (exportacion.TipoMovimiento == null || x.Tipo == exportacion.TipoMovimiento) &&
                (exportacion.CuentaId == null || x.CuentaId == exportacion.CuentaId) &&
                (exportacion.CategoriaId == null || x.CategoriaIds.Contains(exportacion.CategoriaId.Value)))
                .OrderBy(x => x.Fecha).Select(x => new ExportRow(
                    x.Fecha, x.Tipo, x.Monto, x.Descripcion, false, null,
                    x.Tipo == "ingreso" ? x.Monto : 0,
                    x.Tipo == "gasto" ? x.Monto : 0)).ToListAsync(ct));
        var clave = $"exportaciones/{exportacion.UsuarioId:N}/{exportacion.Id:N}.{exportacion.Formato}";
        await using var bytes = new MemoryStream(CrearArchivo(exportacion.Formato, filas));
        await storage.Guardar(clave, bytes, ct);
        exportacion.Completar(
            clave, filas.Count,
            filas.Sum(x => x.IngresoAnalitico),
            filas.Sum(x => x.GastoAnalitico),
            filas.Where(x => x.EsTransferencia).Sum(x => x.Monto));
    }

    private static byte[] CrearArchivo(string formato, IReadOnlyCollection<ExportRow> filas) =>
        formato switch
        {
            "xlsx" => CrearXlsx(filas),
            "pdf" => CrearPdf(filas),
            _ => Encoding.UTF8.GetBytes(CrearCsv(filas))
        };

    private static string CrearCsv(IEnumerable<ExportRow> filas)
    {
        var contenido = new StringBuilder(
            "fecha,tipo,monto,descripcion,operacion_tarjeta\r\n");
        foreach (var fila in filas)
            contenido.Append(fila.Fecha).Append(',').Append(fila.Tipo).Append(',').Append(fila.Monto)
                .Append(",\"").Append(fila.Descripcion.Replace("\"", "\"\"")).Append("\",")
                .AppendLine(fila.OperacionTarjeta ?? string.Empty);
        return contenido.ToString();
    }

    private static byte[] CrearXlsx(IEnumerable<ExportRow> filas)
    {
        using var output = new MemoryStream();
        using (var zip = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            Escribir(zip, "[Content_Types].xml",
                """<?xml version="1.0"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            Escribir(zip, "_rels/.rels",
                """<?xml version="1.0"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            Escribir(zip, "xl/workbook.xml",
                """<?xml version="1.0"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Movimientos" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            Escribir(zip, "xl/_rels/workbook.xml.rels",
                """<?xml version="1.0"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");
            var xml = new StringBuilder("""<?xml version="1.0"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
            var row = 1;
            void AgregarFila(params string[] values)
            {
                xml.Append("<row r=\"").Append(row++).Append("\">");
                foreach (var value in values)
                    xml.Append("<c t=\"inlineStr\"><is><t>").Append(SecurityElement.Escape(value))
                        .Append("</t></is></c>");
                xml.Append("</row>");
            }
            AgregarFila("Fecha", "Tipo", "Monto", "Descripción", "Operación tarjeta");
            foreach (var fila in filas)
                AgregarFila(
                    fila.Fecha.ToString(), fila.Tipo, fila.Monto.ToString(), fila.Descripcion,
                    fila.OperacionTarjeta ?? string.Empty);
            xml.Append("</sheetData></worksheet>");
            Escribir(zip, "xl/worksheets/sheet1.xml", xml.ToString());
        }
        return output.ToArray();
    }

    private static void Escribir(ZipArchive zip, string nombre, string contenido)
    {
        using var writer = new StreamWriter(
            zip.CreateEntry(nombre).Open(), new UTF8Encoding(false));
        writer.Write(contenido);
    }

    private static byte[] CrearPdf(IEnumerable<ExportRow> filas)
    {
        var lineas = new[]
            { "Reporte financiero", "Fecha | Tipo | Monto | Descripcion | Operacion tarjeta" }
            .Concat(filas.Take(35).Select(x =>
                $"{x.Fecha} | {x.Tipo} | {x.Monto} | {x.Descripcion} | {x.OperacionTarjeta}"));
        var stream = new StringBuilder("BT /F1 9 Tf 40 800 Td ");
        foreach (var linea in lineas)
            stream.Append('(').Append(linea.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)"))
                .Append(") Tj 0 -18 Td ");
        stream.Append("ET");
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream.ToString())} >>\nstream\n{stream}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        var pdf = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append(i + 1).Append(" 0 obj\n").Append(objects[i]).Append("\nendobj\n");
        }
        var xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append("xref\n0 ").Append(objects.Length + 1).Append("\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) pdf.Append(offset.ToString("D10")).Append(" 00000 n \n");
        pdf.Append("trailer << /Size ").Append(objects.Length + 1)
            .Append(" /Root 1 0 R >>\nstartxref\n").Append(xref).Append("\n%%EOF");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private sealed record ExportRow(
        DateOnly Fecha,
        string Tipo,
        long Monto,
        string Descripcion,
        bool EsTransferencia,
        string? OperacionTarjeta,
        long IngresoAnalitico,
        long GastoAnalitico);

    private async Task ProcesarEliminacionPerfil(
        Guid eliminacionId,
        CancellationToken cancellationToken)
    {
        var eliminacion = await db.EliminacionesPerfil
            .SingleOrDefaultAsync(x => x.Id == eliminacionId, cancellationToken);
        if (eliminacion is null || eliminacion.Estado != "pendiente")
            return;

        var usuario = await db.Usuarios
            .SingleOrDefaultAsync(x => x.Id == eliminacion.UsuarioId, cancellationToken);
        if (usuario is null)
            throw new InvalidOperationException(
                $"No existe el usuario de la eliminación de perfil {eliminacionId}.");

        usuario.Anonimizar();
        await db.Sesiones
            .Where(x => x.UsuarioId == usuario.Id && x.RevocadoEn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevocadoEn, DateTimeOffset.UtcNow),
                cancellationToken);
        eliminacion.Completar();
    }

    private async Task ProcesarEliminacionGrupo(
        Guid eliminacionId,
        CancellationToken cancellationToken)
    {
        var eliminacion = await db.EliminacionesGrupos
            .SingleOrDefaultAsync(x => x.Id == eliminacionId, cancellationToken);
        if (eliminacion is null || eliminacion.Estado != "pendiente")
            return;
        var grupo = await db.GruposFamiliares
            .SingleOrDefaultAsync(x => x.Id == eliminacion.GrupoFamiliarId, cancellationToken);
        if (grupo is null)
            throw new InvalidOperationException(
                $"No existe el grupo de la eliminación {eliminacionId}.");
        grupo.Eliminar();
        eliminacion.Completar();
    }
}
