using System.Net;
using System.Text;

namespace FinanzasInteligentes.Infraestructura.Correo;

public sealed record ContenidoCorreoSuscripcion(
    string Asunto,
    string Texto,
    string Html);

public static class PlantillasCorreoSuscripcion
{
    public static ContenidoCorreoSuscripcion PagoProcesado(
        string nombre,
        string plan,
        DateTimeOffset fechaPago,
        DateTimeOffset proximoCobro,
        string metodoPago,
        string urlAplicacion) =>
        Crear(
            asunto: "¡Gracias por tu suscripción!",
            etiqueta: "PAGO PROCESADO",
            titulo: "¡Gracias por tu suscripción!",
            saludo: nombre,
            mensaje: "Tu pago se procesó correctamente y tu suscripción a Finanza Premium ya está activa.",
            filas:
            [
                ("Plan", plan),
                ("Fecha de pago", Fecha(fechaPago)),
                ("Método de pago", metodoPago),
                ("Próximo cobro", Fecha(proximoCobro))
            ],
            urlAplicacion: urlAplicacion,
            textoBoton: "Ir a la aplicación",
            color: "#009B65",
            fondoDestacado: "#EAF9F3");

    public static ContenidoCorreoSuscripcion Cancelacion(
        string nombre,
        string plan,
        DateTimeOffset fechaCancelacion,
        DateTimeOffset accesoHasta,
        string urlAplicacion) =>
        Crear(
            asunto: "Tu suscripción ha sido cancelada",
            etiqueta: "SUSCRIPCIÓN CANCELADA",
            titulo: "Tu suscripción ha sido cancelada",
            saludo: nombre,
            mensaje: "Confirmamos la cancelación de tu suscripción a Finanza Premium de acuerdo con tu solicitud.",
            filas:
            [
                ("Fecha de cancelación", Fecha(fechaCancelacion)),
                ("Acceso disponible hasta", Fecha(accesoHasta)),
                ("Plan", plan)
            ],
            urlAplicacion: urlAplicacion,
            textoBoton: "Volver a Finanza",
            color: "#E63E57",
            fondoDestacado: "#FFF0F2",
            cierre: "Podés volver a suscribirte cuando quieras desde la aplicación.");

    public static ContenidoCorreoSuscripcion Restauracion(
        string nombre,
        string plan,
        DateTimeOffset accesoHasta,
        string urlAplicacion) =>
        Crear(
            asunto: "Tu suscripción fue restaurada",
            etiqueta: "SUSCRIPCIÓN RESTAURADA",
            titulo: "Tu suscripción vuelve a estar activa",
            saludo: nombre,
            mensaje: "Restauramos correctamente tu suscripción a Finanza Premium.",
            filas:
            [
                ("Plan", plan),
                ("Acceso disponible hasta", Fecha(accesoHasta))
            ],
            urlAplicacion: urlAplicacion,
            textoBoton: "Ir a la aplicación",
            color: "#009B65",
            fondoDestacado: "#EAF9F3");

    private static ContenidoCorreoSuscripcion Crear(
        string asunto,
        string etiqueta,
        string titulo,
        string saludo,
        string mensaje,
        IReadOnlyCollection<(string Etiqueta, string Valor)> filas,
        string urlAplicacion,
        string textoBoton,
        string color,
        string fondoDestacado,
        string? cierre = null)
    {
        var textoFilas = string.Join(Environment.NewLine,
            filas.Select(x => $"{x.Etiqueta}: {x.Valor}"));
        var texto =
            $"Hola, {saludo}.{Environment.NewLine}{mensaje}{Environment.NewLine}" +
            $"{textoFilas}{Environment.NewLine}" +
            (cierre is null ? string.Empty : $"{cierre}{Environment.NewLine}") +
            $"Abrir Finanza: {urlAplicacion}";

        var filasHtml = new StringBuilder();
        foreach (var fila in filas)
        {
            filasHtml.Append("""
                <tr>
                  <td style="padding:8px 8px 8px 0;color:#5A6A7E;font-size:14px;line-height:20px;white-space:nowrap">{{ETIQUETA}}</td>
                  <td style="padding:8px 0;color:#122439;font-size:14px;line-height:20px;font-weight:600;text-align:right">{{VALOR}}</td>
                </tr>
                """
                .Replace("{{ETIQUETA}}", E(fila.Etiqueta))
                .Replace("{{VALOR}}", E(fila.Valor)));
        }

        var cierreHtml = cierre is null
            ? string.Empty
            : $"<p style=\"margin:20px 0 0;color:#445569;font-size:14px;line-height:21px\">{E(cierre)}</p>";
        var html = $$"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <title>{{E(asunto)}}</title>
            </head>
            <body style="margin:0;padding:0;background:#F4FBF8;font-family:Arial,Helvetica,sans-serif;color:#122439">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#F4FBF8">
                <tr><td align="center" style="padding:28px 12px">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:640px;background:#FFFFFF;border:1px solid #DCEAE5;border-radius:20px;overflow:hidden">
                    <tr><td style="padding:26px 30px 18px">
                      <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
                        <td style="width:32px;height:32px;border-radius:18px 4px 18px 4px;background:linear-gradient(135deg,#35D18A,#00784E)"></td>
                        <td style="padding-left:10px">
                          <div style="font-size:20px;line-height:22px;font-weight:800;color:#122439">Finanza</div>
                          <div style="font-size:10px;line-height:14px;color:#718094">Pequeños hábitos, grandes logros.</div>
                        </td>
                      </tr></table>
                    </td></tr>
                    <tr><td style="padding:0 30px">
                      <div style="padding:26px;border-radius:16px;background:{{fondoDestacado}}">
                        <div style="font-size:12px;line-height:18px;font-weight:800;color:{{color}};letter-spacing:.4px">{{E(etiqueta)}}</div>
                        <div style="margin-top:8px;max-width:410px;font-size:30px;line-height:35px;font-weight:800;color:#122439">{{E(titulo)}}</div>
                      </div>
                    </td></tr>
                    <tr><td style="padding:26px 30px 10px">
                      <p style="margin:0 0 8px;font-size:16px;line-height:24px;color:#122439">Hola, <strong>{{E(saludo)}}</strong> 👋</p>
                      <p style="margin:0;color:#445569;font-size:15px;line-height:23px">{{E(mensaje)}}</p>
                    </td></tr>
                    <tr><td style="padding:14px 30px">
                      <div style="padding:16px 20px;border-radius:14px;background:#F2FAF7">
                        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0">{{filasHtml}}</table>
                      </div>
                      {{cierreHtml}}
                    </td></tr>
                    <tr><td style="padding:16px 30px 26px">
                      <a href="{{E(urlAplicacion)}}" style="display:block;padding:15px 22px;border-radius:999px;background:#009B65;color:#FFFFFF;text-align:center;text-decoration:none;font-size:15px;font-weight:700">{{E(textoBoton)}}</a>
                    </td></tr>
                    <tr><td style="padding:0 30px"><div style="height:1px;background:#E5EFEA"></div></td></tr>
                    <tr><td style="padding:20px 30px 28px;color:#718094;font-size:12px;line-height:18px">
                      Si tenés alguna duda, podés responder este correo o visitar nuestro Centro de Ayuda.<br><br>
                      Gracias por ser parte de Finanza.<br><strong style="color:#445569">El equipo de Finanza ♥</strong>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        return new(asunto, texto, html);
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);

    private static string Fecha(DateTimeOffset value)
    {
        string[] meses =
        [
            "enero", "febrero", "marzo", "abril", "mayo", "junio",
            "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"
        ];
        return $"{value.Day} de {meses[value.Month - 1]} de {value.Year}";
    }
}
