# API de Finanzas Inteligentes

Contrato propuesto para el backend .NET 10 que reemplazará los repositorios mock de la aplicación Flutter.

## Documentos disponibles

- [Alcance formal y requisitos](../ALCANCE_Y_REQUISITOS.md)
- [Convenciones REST, seguridad y errores](CONVENCIONES.md)
- [Catálogo de endpoints](ENDPOINTS.md)
- [Contratos JSON y enumeraciones](CONTRATOS.md)
- [Trazabilidad de requisitos funcionales](TRAZABILIDAD_RF.md)
- [Trazabilidad de requisitos no funcionales](TRAZABILIDAD_RNF.md)
- [Especificación OpenAPI 3.1](openapi.yaml)
- [Arquitectura, migraciones y plan del backend](../backend/README.md)

## Contrato ejecutable

`openapi.yaml` es la fuente de verdad versionada del contrato HTTP. Se genera a partir del catálogo y los contratos revisados para evitar divergencias:

```bash
ruby scripts/generate_openapi.rb
ruby scripts/validate_openapi.rb
ruby scripts/validate_documentation.rb
```

La validación comprueba las 132 operaciones, los parámetros de ruta, la unicidad de `operationId`, la trazabilidad y todas las referencias locales.

## Decisiones adoptadas

| Tema | Decisión |
|---|---|
| URL base | `/api/v1` |
| Idioma | Recursos en español, sin tildes |
| Formato de rutas | Plural y `kebab-case` |
| Acciones | Se representan como recursos: `sesiones`, `aportes`, `desafios-otp`, `procesamientos-documentales` |
| JSON | `camelCase`, UTF-8 |
| Identificadores | UUID v7 como texto |
| Fechas y horas | ISO 8601 en UTC, por ejemplo `2026-07-22T18:30:00Z` |
| Fechas sin hora | `YYYY-MM-DD` |
| Zona horaria | IANA, inicialmente `America/Asuncion` |
| Importes PYG | Enteros `Int64`; PYG no utiliza decimales |
| Porcentajes | Decimal entre `0` y `1` |
| Autenticación | Bearer JWT de corta duración y refresh token rotativo |
| Errores | `application/problem+json`, compatible con `ProblemDetails` |
| Paginación | Cursor opaco con `limite` máximo 100 |
| Concurrencia | `ETag` y `If-Match` en actualizaciones sensibles |
| Idempotencia | `Idempotency-Key` en altas financieras y operaciones críticas |
| Rol principal familiar | `propietario`, equivalente al administrador principal del alcance |
| Exportaciones obligatorias | PDF y Excel (`xlsx`); CSV es adicional |

## Alcance

El alcance materializado en OpenAPI y trazabilidad cubre RF-01 a RF-21 y los módulos descritos para Flutter:

- usuarios, perfil, sesiones y contraseñas;
- seguridad, OTP, biometría y auditoría;
- cuentas, tarjetas, categorías y transferencias internas;
- movimientos privados, recurrentes y documentos;
- OCR de imágenes/PDF e importación XML SIFEN;
- presupuestos y metas de ahorro;
- grupos familiares, miembros, invitaciones y caja compartida;
- dashboards, reportes, exportaciones, alertas, predicciones y score;
- planes y suscripciones.

Las transferencias entre cuentas y la caja familiar son registros contables internos. No ejecutan transferencias bancarias ni administran fondos reales.

## Decisiones pendientes del equipo

1. Proveedor final de biometría/attestation y mecanismo de vinculación de dispositivos.
2. Proveedor de pagos o si las suscripciones seguirán sólo como feature flags durante el piloto.
3. Límites máximos de archivos, retención documental y política de eliminación en S3.
4. Estructura exacta de los XML SIFEN y reglas tributarias que serán validadas.
5. Política de recálculo de presupuestos, score, alertas y predicciones.
6. Si un usuario podrá pertenecer a más de un grupo familiar en versiones futuras. Este contrato asume uno.
7. Política legal de anonimización y retención al eliminar una cuenta.
