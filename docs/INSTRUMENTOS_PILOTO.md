# Instrumentos del piloto

Los cuestionarios del piloto se administran como datos versionados en el esquema PostgreSQL `piloto`; la aplicación no contiene preguntas fijas.

## Instrumentos iniciales

La migración `M0021_InstrumentosPiloto` crea los instrumentos activos `preuso` y `postuso`, versión `1.0`.

- `preuso`: ocho ítems de escala y tres preguntas abiertas, alineados al cuestionario inicial del Apéndice A de la tesis.
- `postuso`: ocho ítems de escala y cuatro preguntas abiertas, alineados al cuestionario final del Apéndice A y a los indicadores de utilidad, facilidad, seguridad y aceptación.

Cada respuesta se vincula tanto al instrumento como a su versión. Un participante solo puede responder una vez cada versión activa.

## Cambiar preguntas sin una nueva AAB

No se deben editar ni eliminar preguntas de una versión que ya tenga respuestas. Para publicar un ajuste:

1. Crear una nueva fila en `piloto.instrumentos` con el mismo `codigo`, una `version_instrumento` mayor y `activo = false`.
2. Insertar las preguntas de esa nueva versión en `piloto.preguntas`.
3. En una única transacción, desactivar la versión anterior y activar la nueva.

La siguiente consulta de la app a `GET /api/v1/instrumentos-piloto/{codigo}` obtendrá la versión activa. El cambio no requiere compilar ni distribuir una nueva AAB.

Para conservar resultados comparables, cualquier modificación de redacción, orden, escala o obligatoriedad debe crear una versión nueva. Las respuestas anteriores siguen siendo trazables por `version_instrumento`.

## Endpoints autenticados

- `GET /api/v1/instrumentos-piloto/preuso`
- `GET /api/v1/instrumentos-piloto/postuso`
- `POST /api/v1/instrumentos-piloto/{codigo}/respuestas`

El `GET` informa si el usuario autenticado ya respondió. El `POST` valida preguntas, obligatoriedad y rango de escala antes de persistir.
