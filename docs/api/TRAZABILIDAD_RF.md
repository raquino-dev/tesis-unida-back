# Trazabilidad de requisitos funcionales

Esta matriz alinea los **RF-01 a RF-26** de la tesis con la implementación actual. Los estados distinguen implementación de evidencia E2E:

- `implementado`: existe implementación trazable en código/contrato y cobertura técnica asociada;
- `verificado-auto`: además, existe una prueba automatizada ejecutada satisfactoriamente en CI;
- la evidencia contra servicios desplegados y participantes se registra por separado y no se sustituye con mocks o dobles.

| RF | Operaciones/capacidad principal | Criterio verificable mínimo | Evidencia técnica asociada | Estado |
|---|---|---|---|---|
| RF-01 Registro e inicio de sesión | `POST /usuarios`, `POST /sesiones`, renovaciones | Registro único, credenciales seguras, refresh rotativo | unit/contract/integration + cliente API | verificado-auto |
| RF-02 Recuperación y cambio de contraseña | recuperaciones, restablecimientos, `PUT /perfil/contrasena` | token temporal, rate limit, revocación | unit/contract + cliente API | verificado-auto |
| RF-03 Creación de grupos | `POST /grupos-familiares` | creador queda propietario | unit/contract/aceptación | verificado-auto |
| RF-04 Membresía y administración familiar | invitaciones, integrantes, transferencia/eliminación | autorización por rol y propiedad | unit/contract/aceptación | verificado-auto |
| RF-05 Ingresos/gastos privados y familiares | movimientos privados/familiares | separación de ámbitos y consistencia | unit/contract/aceptación | verificado-auto |
| RF-06 Registro manual | `/movimientos` | validación e idempotencia cuando corresponda | unit/contract/aceptación | verificado-auto |
| RF-07 Imagen/PDF con OCR | documentos/procesamientos | archivo privado, validación y procesamiento | unit/contract; E2E Textract separado | implementado |
| RF-08 XML SIFEN | documentos `xml-sifen` | parser seguro, CDC deduplicado | unit/contract/cliente API | verificado-auto |
| RF-09 Corrección OCR/XML | procesamiento + movimiento | revisión humana y control de versión | unit/contract/cliente API | verificado-auto |
| RF-10 Categorías | privadas/familiares | predeterminadas y personalizadas sin fuga de ámbito | unit/contract/aceptación | verificado-auto |
| RF-11 Presupuestos | privados/familiares | reglas de periodo/categoría y cálculo coherente | unit/contract/cliente API | verificado-auto |
| RF-12 Metas de ahorro | metas/aportes | ámbito/cuenta coherentes y trazabilidad | unit/contract/cliente API | verificado-auto |
| RF-13 Caja compartida | caja/operaciones | rol autorizado, OTP cuando corresponda y saldo consistente | unit/contract/aceptación | verificado-auto |
| RF-14 Dashboards/reportes | tableros/reportes | totales coherentes con movimientos | unit/contract/cliente API | verificado-auto |
| RF-15 Exportación | exportaciones/descargas | PDF/XLSX y acceso privado | unit/contract/aceptación; E2E S3 separado | verificado-auto |
| RF-16 Proyecciones | proyecciones privadas/familiares | explicabilidad y marca preliminar con <3 meses | unit/contract/cliente API | verificado-auto |
| RF-17 Alertas/recomendaciones | alertas, score, push | regla reproducible y explicación | unit/contract; E2E FCM separado | implementado |
| RF-18 Biometría | protección local/dispositivo | autenticación biométrica en dispositivo compatible | pruebas cliente con doble; dispositivo real separado | implementado |
| RF-19 OTP adaptativo | desafíos/verificaciones OTP | expiración, intentos, consumo único | unit/contract/cliente API | verificado-auto |
| RF-20 Seguridad y auditoría | eventos/auditoría | operaciones críticas trazables sin secretos | unit/contract/arquitectura | verificado-auto |
| RF-21 Free/Premium | planes/suscripciones/Billing | derechos consistentes; compra de prueba sin cobro real | unit/contract; E2E Google Play separado | implementado |
| RF-22 Cuentas financieras | `/cuentas` | ownership, saldo y concurrencia | unit/contract/cliente API | verificado-auto |
| RF-23 Tarjetas por alias | `/tarjetas-credito` | alias sin PAN/CVV ni otros datos del plástico | unit/contract/cliente API | verificado-auto |
| RF-24 Transferencias contables | `/transferencias` | origen/destino propios, atomicidad e idempotencia | unit/contract/cliente API | verificado-auto |
| RF-25 Movimientos recurrentes | `/movimientos-recurrentes`, Worker | una ejecución por periodo, reintentos idempotentes | unit/contract/cliente API | verificado-auto |
| RF-26 Offline y sincronización | SQLite cifrada, outbox local, `GET /sincronizacion`, M0022 | cola persistente, idempotencia, `If-Match`, cursor y tombstones | `offline_store_test`, UUID y backend sync; E2E modo avión separado | verificado-auto |

## Cobertura complementaria

| Capacidad | Operación principal | Evidencia |
|---|---|---|
| Score financiero | `/score-financiero` | pruebas automatizadas + evidencia de explicación |
| Preferencias | `/perfil/preferencias` | contrato/cliente |
| Dispositivos | `/dispositivos` | contrato/cliente |
| Compatibilidad del cliente | `/configuracion-cliente` | contrato |
| Instrumentos del piloto | `/instrumentos-piloto/*` | M0021/M0023 + persistencia versionada |
| Consentimiento | `/privacidad/consentimientos` | aceptación versionada por finalidad |

## Evidencia automatizada vs. E2E

Una prueba que utiliza `MockClient`, repositorios mock o servicios sustituidos demuestra comportamiento de cliente/contrato, **no** funcionamiento extremo a extremo del proveedor desplegado. S3, Textract, SES, FCM, Google Play Billing, backup/restore y sincronización multi-dispositivo deben conservar evidencia E2E independiente.

## Trazabilidad inversa

Las operaciones del contrato API conservan metadatos de requisito/capacidad y pruebas cuando corresponda. La documentación de RF-22 a RF-26 no se mantiene como una lista “adicional”: desde esta versión forman parte del alcance formal de la tesis.

## Definition of Done por operación

1. Contrato HTTP versionado con campos, límites y errores.
2. Autorización por ownership, membresía y rol.
3. Pruebas unitarias/contrato/integración según corresponda.
4. `ProblemDetails`, códigos de dominio y `correlationId` estables.
5. ETag/`If-Match` e idempotencia cuando correspondan.
6. Auditoría para operaciones críticas sin secretos ni PII innecesaria.
7. Transacción u outbox para efectos secundarios.
8. Evidencia de proveedor/despliegue separada cuando la funcionalidad depende de un servicio externo.
9. Documentación y tesis alineadas con la versión entregada.
