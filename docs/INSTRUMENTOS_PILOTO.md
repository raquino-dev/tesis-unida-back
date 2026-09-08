# Instrumentos del piloto

Los cuestionarios del piloto se administran como datos versionados en el esquema PostgreSQL `piloto`; la aplicación no contiene preguntas fijas. Las respuestas existentes nunca se sobrescriben para acomodar un cambio de instrumento.

## Instrumentos principales

La migración `M0021_InstrumentosPiloto` creó `preuso` y `postuso`, versión `1.0`.

- `preuso` v1.0: ocho ítems de escala y tres preguntas abiertas. Los diez referentes adultos lo completaron antes de su primer uso; constituye la medición basal del diseño preexperimental y se conserva sin modificaciones retrospectivas.
- `postuso` v1.0: versión histórica inicial, conservada para trazabilidad.

La migración `M0023_AlineacionPilotoTesis` publica `postuso` v2.0 como versión activa para el cierre de los 28 días. La versión 2.0 coincide con el instrumento final documentado en la tesis y contiene diez ítems Likert y cuatro preguntas abiertas. La versión anterior no se elimina y cualquier respuesta histórica mantiene su `VersionInstrumento` original.

Cada respuesta se vincula tanto al instrumento como a su versión. Un participante solo puede responder una vez cada versión activa.

## Bloque complementario inicial

`M0023_AlineacionPilotoTesis` incorpora además `preuso-complementario`, versión `1.0`, solicitado por tutoría durante la fase inicial del piloto. Se mantiene como instrumento separado porque los participantes ya habían completado el preuso principal antes de comenzar a utilizar la aplicación.

Su análisis es **descriptivo** y no se presenta como medición basal pre/post. Contiene once ítems Likert de 1 a 5 distribuidos en tres dimensiones:

### Intención de compra

1. Consideraría contratar un plan Premium si las funcionalidades ofrecidas aportan valor a la organización de mis finanzas personales o familiares.
2. Es probable que considere adquirir una suscripción si la aplicación satisface mis necesidades de control financiero.
3. Tener acceso previo a una versión gratuita influiría positivamente en mi decisión de contratar posteriormente un plan Premium.

### Cuestiones técnicas del sistema

4. Considero importante que una aplicación financiera permita realizar operaciones básicas aun cuando no exista conexión a Internet.
5. Considero importante que una aplicación financiera proteja el acceso a la información mediante controles adicionales de seguridad cuando sea necesario.
6. Considero importante poder revisar y corregir la información detectada automáticamente de un comprobante antes de registrarla definitivamente.
7. Considero importante que las principales operaciones de una aplicación financiera respondan de forma rápida y estable.
8. Considero importante que la información personal y la información compartida con un grupo familiar permanezcan claramente separadas.

### Disposición de compra

9. Estaría dispuesto/a a pagar Gs. 39.000 mensuales por una versión Premium si considero útiles sus funcionalidades.
10. Estaría dispuesto/a a pagar Gs. 390.000 anuales por una versión Premium si considero útiles sus funcionalidades.
11. Considero razonable pagar por una aplicación de control financiero cuando ofrece funciones adicionales de automatización, análisis, seguridad y gestión familiar.

## Postuso v2.0

Escala 1–5:

1. La aplicación fue fácil de aprender.
2. Pude registrar gastos con menos esfuerzo que mediante mi procedimiento habitual.
3. La captura documental redujo la carga manual.
4. Los reportes me ayudaron a comprender mis gastos.
5. Las proyecciones y explicaciones fueron comprensibles.
6. La separación entre información privada y familiar fue clara.
7. Los controles de seguridad me generaron confianza.
8. Los mensajes de error y validación fueron claros.
9. La aplicación puede ayudarme a mejorar el control financiero.
10. Utilizaría la aplicación de forma continua.

Preguntas abiertas:

- ¿Qué funcionalidad le resultó más útil?
- ¿Qué parte fue confusa o difícil?
- ¿Qué mejoraría antes de usarla regularmente?
- ¿Qué preocupación de seguridad o privacidad mantiene?

## Regla de versionado

No se editan ni eliminan preguntas de una versión que ya tenga respuestas. Un cambio se publica como una nueva versión o como un código separado cuando la semántica de la medición es distinta. De esta manera, los datos históricos siguen siendo interpretables y reproducibles.

## Endpoints autenticados

- `GET /api/v1/instrumentos-piloto/preuso`
- `GET /api/v1/instrumentos-piloto/preuso-complementario`
- `GET /api/v1/instrumentos-piloto/postuso`
- `POST /api/v1/instrumentos-piloto/{codigo}/respuestas`

El `GET` informa si el usuario autenticado ya respondió. El `POST` valida preguntas, obligatoriedad y rango de escala antes de persistir.
