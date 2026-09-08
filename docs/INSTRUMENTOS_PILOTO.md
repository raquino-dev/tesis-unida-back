# Instrumentos del piloto

Los cuestionarios del piloto se administran como datos versionados en el esquema PostgreSQL `piloto`; la aplicación no contiene preguntas fijas. Las respuestas existentes nunca se sobrescriben para acomodar un cambio de instrumento.

## Instrumentos principales

La migración `M0021_InstrumentosPiloto` creó los instrumentos `preuso` y `postuso`, versión `1.0`.

- `preuso`: ocho ítems de escala y tres preguntas abiertas. Los diez referentes adultos lo completaron antes de su primer uso de la aplicación; constituye la medición basal del diseño preexperimental.
- `postuso`: instrumento final administrado después de los 28 días de observación correspondientes a cada unidad familiar.

Cada respuesta se vincula tanto al instrumento como a su versión. Un participante solo puede responder una vez cada versión activa.

## Bloque complementario inicial

La migración `M0023_AlineacionPilotoTesis` incorpora `preuso-complementario`, versión `1.0`, solicitado por tutoría durante la fase inicial del piloto. Se mantiene como instrumento separado porque los participantes ya habían completado el preuso principal antes de comenzar a utilizar la aplicación.

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

## Cambiar preguntas sin una nueva AAB

No se deben editar ni eliminar preguntas de una versión que ya tenga respuestas. Para publicar un ajuste:

1. Crear una nueva fila en `piloto.instrumentos` con un código/versionado que preserve la semántica de la medición anterior.
2. Insertar las preguntas de esa versión en `piloto.preguntas`.
3. Activar la versión correspondiente sin alterar respuestas históricas.

La aplicación obtiene los instrumentos desde la API; por ello, los cambios de preguntas versionadas no requieren compilar una nueva AAB si el código del instrumento ya está soportado por el cliente.

## Endpoints autenticados

- `GET /api/v1/instrumentos-piloto/preuso`
- `GET /api/v1/instrumentos-piloto/preuso-complementario`
- `GET /api/v1/instrumentos-piloto/postuso`
- `POST /api/v1/instrumentos-piloto/{codigo}/respuestas`

El `GET` informa si el usuario autenticado ya respondió. El `POST` valida preguntas, obligatoriedad y rango de escala antes de persistir.
