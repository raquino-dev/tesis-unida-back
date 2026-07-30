# Convenciones REST, seguridad y errores

## Autorización

Todos los endpoints requieren `Authorization: Bearer <accessToken>`, excepto:

- creación de usuario, creación/renovación de sesión y recuperación/restablecimiento de contraseña;
- consulta pública de una invitación mediante token opaco;
- planes de suscripción y configuración pública de compatibilidad del cliente;
- liveness y readiness.

Aceptar una invitación requiere autenticación: el token autoriza la invitación, pero la identidad del integrante siempre proviene del JWT.

El backend obtiene el usuario desde el JWT. No debe aceptar `usuarioId` enviado por el cliente para decidir propiedad. En recursos familiares debe validar la membresía y el rol en cada solicitud.

Tokens opacos presentes en rutas —por ejemplo invitaciones— se consideran secretos: el proxy y la aplicación deben redactarlos en logs, responder `Cache-Control: no-store` y no incluirlos en métricas, trazas ni `Referer`.

Refresh tokens, comprobantes de tienda, OTP, firmas y atestaciones tampoco se registran. De comprobantes de suscripción sólo se persisten el hash, el identificador del proveedor y el resultado verificado mínimo.

Roles familiares:

| Rol | Permisos principales |
|---|---|
| `propietario` | Administración total, eliminación del grupo, roles, caja y presupuestos |
| `administrador` | Invitaciones, miembros no propietarios, cuentas compartidas, caja y presupuestos |
| `integrante` | Consulta del grupo y alta de movimientos/aportes permitidos |

`propietario` es el administrador principal mencionado en RF-03/RF-04. El creador recibe este rol automáticamente y cada grupo conserva exactamente un propietario activo. `administrador` es una delegación operativa y no puede eliminar el grupo ni sustituir al propietario fuera del flujo explícito de transferencia.

## Cabeceras

```http
Authorization: Bearer eyJ...
Content-Type: application/json
Accept: application/json
X-Correlation-Id: 019...
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
If-Match: "7"
```

- `X-Correlation-Id`: opcional desde el cliente; el servidor lo genera si falta y lo devuelve en la respuesta y dentro de `ProblemDetails`.
- `Idempotency-Key`: obligatoria en creación de movimientos, transferencias, aportes/retiros, exportaciones, invitaciones, desafíos OTP, procesamientos documentales y suscripciones.
- `If-Match`: obligatoria en `PATCH`, `PUT` que reemplazan representaciones y eliminaciones con riesgo de conflicto. Operaciones de seguridad como cambiar contraseña usan verificación reciente en lugar de versión del perfil.
- Cada lectura o escritura de un recurso mutable devuelve `ETag`; el valor se trata como opaco aunque inicialmente derive de `version`.

## Respuesta paginada

```json
{
  "datos": [],
  "paginacion": {
    "siguienteCursor": "eyJpZCI6Ii4uLiJ9",
    "hayMas": true,
    "limite": 20
  }
}
```

Parámetros comunes: `cursor`, `limite`, `orden=fecha:desc`.

## ProblemDetails

```json
{
  "type": "https://api.finanzas.example/problemas/saldo-insuficiente",
  "title": "Saldo insuficiente",
  "status": 409,
  "detail": "La caja compartida no posee saldo suficiente.",
  "instance": "/api/v1/grupos-familiares/019.../operaciones-caja",
  "codigo": "saldo_insuficiente",
  "correlationId": "019...",
  "errores": {
    "monto": ["El monto debe ser mayor que cero."]
  }
}
```

## Códigos HTTP

| Código | Uso |
|---|---|
| `200 OK` | Consulta o modificación con cuerpo |
| `201 Created` | Recurso creado; incluir `Location` |
| `202 Accepted` | OCR, exportación, correo u otro procesamiento asíncrono |
| `204 No Content` | Eliminación o modificación sin cuerpo |
| `400 Bad Request` | JSON, parámetros o formato inválido |
| `401 Unauthorized` | Token ausente, inválido o vencido |
| `403 Forbidden` | Usuario autenticado sin permiso suficiente |
| `404 Not Found` | Recurso inexistente o no visible para el usuario |
| `409 Conflict` | Duplicado, saldo insuficiente o estado incompatible |
| `410 Gone` | Token, invitación o desafío existió pero venció o fue consumido |
| `412 Precondition Failed` | `ETag` desactualizado |
| `413 Content Too Large` | Archivo por encima del límite |
| `415 Unsupported Media Type` | MIME/extensión no permitida |
| `422 Unprocessable Content` | Validación de negocio |
| `429 Too Many Requests` | Rate limit, OTP o intentos de autenticación |
| `500 Internal Server Error` | Error no controlado |
| `503 Service Unavailable` | S3, SES, Redis u otra dependencia temporalmente no disponible |

## Filtros de movimientos

```text
?texto=supermercado
&tipo=gasto
&categoriaId=019...
&cuentaId=019...
&desde=2026-07-01
&hasta=2026-07-31
&documento=con-documento
&cursor=...
&limite=20
```

## Archivos

- Carga: `multipart/form-data`.
- MIME iniciales: `image/jpeg`, `image/png`, `application/pdf`, `application/xml`, `text/xml`.
- La API almacena el archivo en S3 y devuelve metadatos, nunca una ruta interna.
- La descarga se solicita mediante un subrecurso `/descargas` y devuelve una URL prefirmada de corta duración; las respuestas normales del documento o exportación no incluyen URLs.
- El cliente envía `X-Content-SHA256` en base hexadecimal; es obligatorio para cargas idempotentes.
- En `multipart/form-data`, el hash de solicitud idempotente se calcula sobre SHA-256, tipo, ámbito, grupo y demás metadatos normalizados, no sobre fronteras multipart.

## Procesos asíncronos

Los recursos de procesamiento asíncrono utilizan: `pendiente`, `procesando`, `completado`, `incompleto`, `fallido`. Los archivos usan un ciclo separado: `pendiente`, `cargando`, `disponible`, `fallido`, `eliminado`.

Una respuesta `202` incluye `Location` y un recurso consultable:

```json
{
  "id": "019...",
  "estado": "pendiente",
  "creadoEn": "2026-07-22T18:30:00Z",
  "urlEstado": "/api/v1/procesamientos-documentales/019..."
}
```
