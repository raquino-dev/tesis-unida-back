# Trazabilidad de requisitos funcionales

Esta matriz conecta cada requisito con operaciones HTTP, criterio verificable, prueba prevista y etapa. `diseñado` indica que existe contrato y plan; `parcial`, que sólo una parte del flujo o de sus criterios está implementada; `implementado`, que falta evidencia final; y `verificado`, que la prueba asociada fue ejecutada satisfactoriamente.

| RF | Operaciones principales | Criterio verificable mínimo | Prueba | Etapa | Estado |
|---|---|---|---|---:|---|
| RF-01 Registro e inicio de sesión | `POST /usuarios`, `POST /sesiones`, `POST /sesiones/renovaciones` | Registro único, credenciales no enumerables y refresh rotativo con detección de reutilización | `CT-RF-01` | 1 | parcial |
| RF-02 Recuperación y cambio de contraseña | recuperaciones, restablecimientos y `PUT /perfil/contrasena` | Token de un solo uso, vencimiento, rate limit y revocación de otras sesiones | `CT-RF-02` | 1 | diseñado |
| RF-03 Creación de grupos y propietario automático | `POST /grupos-familiares` | Creador queda como único propietario activo y administrador principal | `CT-RF-03` | 3 | diseñado |
| RF-04 Invitación, exclusión y eliminación del grupo | invitaciones, integrantes y eliminaciones anidadas | Invitación de un uso, sin doble membresía y sin eliminar o degradar al propietario fuera de una transferencia válida | `CT-RF-04` | 3 | diseñado |
| RF-05 Ingresos/gastos privados y familiares | movimientos privados y familiares | Movimiento, saldo y outbox son atómicos; no existe acceso cruzado | `CT-RF-05` | 2–3 | parcial |
| RF-06 Registro manual | `POST /movimientos` | Reintento con la misma clave no duplica el movimiento | `CT-RF-06` | 2 | parcial |
| RF-07 Imagen/PDF con OCR | documentos y procesamientos OCR | MIME/tamaño válidos, archivo privado y procesamiento reintentable | `CT-RF-07` | 5 | diseñado |
| RF-08 XML SIFEN | documentos `xml-sifen` y procesamiento `sifen` | XML sin entidades externas, CDC deduplicado y datos normalizados | `CT-RF-08` | 5 | diseñado |
| RF-09 Corrección OCR/XML | `PATCH /procesamientos-documentales/{id}`, luego movimiento | Corrección conserva original y exige ETag vigente | `CT-RF-09` | 5 | diseñado |
| RF-10 Categorías | categorías privadas y familiares | Predeterminadas inmutables; categorías familiares no filtran datos privados | `CT-RF-10` | 2–3 | parcial |
| RF-11 Presupuestos | presupuestos privados y familiares | Sin solapamiento inválido; gasto derivado de movimientos confirmados | `CT-RF-11` | 4 | diseñado |
| RF-12 Metas | metas y aportes | Ámbito/cuenta coherentes y aporte/movimiento atómicos | `CT-RF-12` | 4 | diseñado |
| RF-13 Caja compartida | caja y operaciones anidadas al grupo | Saldo bloqueado, retiro con OTP y corrección por compensación | `CT-RF-13` | 3 | diseñado |
| RF-14 Dashboards/reportes | tableros y reportes privados/familiares | Totales reconcilian con movimientos y caché no es fuente de verdad | `CT-RF-14` | 6 | diseñado |
| RF-15 Exportación | exportaciones y descargas | PDF/XLSX obligatorios y CSV adicional; deduplicados y con URL privada temporal | `CT-RF-15` | 5 | diseñado |
| RF-16 Proyecciones | proyecciones privadas/familiares | Versión y período visibles; datos insuficientes producen `422` | `CT-RF-16` | 6 | diseñado |
| RF-17 Alertas | alertas financieras | Regla reproducible, explicación y actualización con ETag | `CT-RF-17` | 6 | diseñado |
| RF-18 Biometría | credenciales, desafíos y verificaciones biométricas | Nonce emitido por servidor, ligado a dispositivo y de un solo uso | `CT-RF-18` | 1 | diseñado |
| RF-19 OTP adaptativo | desafíos y verificaciones OTP | HMAC versionado, expiración, intentos limitados y consumo único | `CT-RF-19` | 1 | diseñado |
| RF-20 Seguridad y auditoría | eventos de seguridad/auditoría | Sólo recursos autorizados; sin secretos ni IP completa | `CT-RF-20` | 1 | diseñado |
| RF-21 Free/Premium | planes, suscripciones, restauraciones y cancelaciones | Recibos idempotentes, una suscripción activa y acceso hasta fin de período | `CT-RF-21` | 7 | diseñado |

## Cobertura adicional

| Capacidad | Operaciones | Prueba | Etapa |
|---|---|---|---:|
| Cuentas | `/cuentas` | `CT-CUENTAS` | 2 |
| Tarjetas de crédito | `/tarjetas-credito` | `CT-TARJETAS` | 2 |
| Transferencias internas | `/transferencias` | `CT-TRANSFERENCIAS` | 2 |
| Movimientos recurrentes | `/movimientos-recurrentes` | `CT-RECURRENCIAS` | 2 |
| Score financiero | `/score-financiero` | `CT-SCORE` | 6 |
| Preferencias | `/perfil/preferencias` | `CT-PREFERENCIAS` | 1 |
| Dispositivos | `/dispositivos` | `CT-DISPOSITIVOS` | 1 |
| Compatibilidad del cliente | `/configuracion-cliente` | `CT-CONFIG-CLIENTE` | 1 |

## Trazabilidad inversa

Cada operación de `openapi.yaml` debe declarar:

```yaml
x-requisitos-funcionales: [RF-05, RF-06]
x-etapa: 2
x-pruebas-contrato: [CT-RF-05, CT-RF-06]
```

El pipeline falla si una operación no tiene RF/capacidad asociada, si un RF no aparece en ninguna operación o si una prueba declarada no existe.

## Definition of Done por operación

1. Esquema OpenAPI versionado con campos, formatos, límites y ejemplos.
2. Autorización por propietario, membresía y rol.
3. Pruebas unitarias, integración PostgreSQL y contrato.
4. OpenAPI generado por ASP.NET Core comparado sin diferencias incompatibles contra `docs/api/openapi.yaml`.
5. `ProblemDetails`, códigos de dominio y `correlationId` estables.
6. ETag/`If-Match` e idempotencia cuando correspondan.
7. Auditoría para operaciones críticas sin secretos ni PII innecesaria.
8. Transacción u outbox para efectos secundarios.
9. Métricas de latencia, errores y dependencias externas.
10. Estado actualizado a `implementado` y luego `verificado` sólo cuando las pruebas asociadas pasan.
