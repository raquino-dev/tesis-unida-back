# Trazabilidad de requisitos no funcionales

Esta matriz convierte los RNF del alcance formal en criterios medibles. Un estado `parcial` indica que existe una base técnica, pero falta evidencia suficiente para declarar cumplimiento.

## Estados

| Estado | Significado |
|---|---|
| `diseñado` | Existe una decisión o prueba prevista, sin implementación verificable |
| `parcial` | Existe implementación incompleta o aún no validada en el ambiente objetivo |
| `implementado` | La capacidad está desarrollada, pendiente de evidencia final del piloto |
| `verificado` | El criterio y su prueba/evidencia fueron ejecutados satisfactoriamente |

## Matriz

| RNF | Criterio verificable | Prueba o evidencia requerida | Etapa | Estado |
|---|---|---|---:|---|
| RNF-01 Protección de información | Ningún usuario accede a recursos privados/familiares ajenos; secretos no aparecen en logs ni respuestas | Pruebas negativas entre usuarios/grupos, revisión de logs y análisis de seguridad `CT-RNF-01` | 1–8 | parcial |
| RNF-02 JWT y refresh tokens | Access token de 10–15 min; refresh rotativo; reutilización revoca la familia | Pruebas de emisión, expiración, rotación, revocación y reutilización `CT-RNF-02` | 1 | parcial |
| RNF-03 Integridad de datos/documentos | Movimiento, saldo, documento y outbox conservan relaciones válidas o se revierten juntos | Pruebas PostgreSQL de commit/rollback, restricciones y deduplicación `CT-RNF-03` | 2–5 | parcial |
| RNF-04 Rendimiento general | Las operaciones habituales cumplen sus límites específicos durante el piloto | Informe de carga con latencia máxima, tasa de errores y ambiente `CT-RNF-04` | 8 | diseñado |
| RNF-05 Experiencia OCR | El procesamiento informa estado y no bloquea la API; el tiempo cumple RNF-16 | Lote representativo de imágenes/PDF y encuesta de percepción `CT-RNF-05` | 5–8 | diseñado |
| RNF-06 Usabilidad | Usuarios del piloto completan tareas principales y responden encuesta pre/post | Guion de tareas, incidencias, encuesta y resultados; umbral de aceptación por acordar `EV-RNF-06` | Piloto | diseñado |
| RNF-07 Separación privado/familiar | Propiedad y ámbito son inequívocos; no hay filtraciones cruzadas | Pruebas de autorización, FKs/constraints y revisión UI `CT-RNF-07` | 2–4 | parcial |
| RNF-08 Almacenamiento documental seguro | Objetos privados en S3; acceso sólo mediante URL temporal autorizada | Pruebas de carga, acceso cruzado, expiración, cifrado y eliminación `CT-RNF-08` | 5 | diseñado |
| RNF-09 Base relacional | Toda información transaccional estructurada persiste en PostgreSQL | Migración desde base vacía e integración sobre PostgreSQL/Supabase `CT-RNF-09` | 0–8 | parcial |
| RNF-10 Caché | Sólo lecturas repetitivas elegibles usan Redis; invalidación no altera la fuente de verdad | Métricas hit/miss y pruebas de invalidación/fallback `CT-RNF-10` | 6 | diseñado |
| RNF-11 Modularidad | Dominio no depende de API/Infraestructura y endpoints no acceden directamente a EF | `FinanzasInteligentes.ArchitectureTests` en cada build `CT-RNF-11` | 0–8 | verificado |
| RNF-12 VPS/containers | Todo el sistema inicia desde Docker Compose con configuración externa | `docker compose config`, arranque limpio y despliegue documentado en Hetzner `CT-RNF-12` | 0–8 | parcial |
| RNF-13 Reverse proxy | API sólo se expone mediante Nginx/TLS y conserva IP/correlation headers controlados | Prueba de configuración Nginx, TLS y acceso directo bloqueado `CT-RNF-13` | 8 | diseñado |
| RNF-14 Escalabilidad progresiva | API sin estado local durable y Worker escalable sin reclamar dos veces el mismo trabajo | Prueba con dos réplicas y outbox con lease/deduplicación `CT-RNF-14` | 0–8 | parcial |
| RNF-15 Piloto suficiente | Los diez participantes completan los flujos definidos sin bloqueo crítico | Acta del piloto, incidencias y encuestas pre/post `EV-RNF-15` | Piloto | diseñado |
| RNF-16 OCR ≤ 5 s promedio | Promedio de procesamiento no mayor a 5 s para documentos legibles del conjunto piloto | Benchmark con tamaño, calidad, proveedor, promedio y percentiles `PR-RNF-16` | 5–8 | diseñado |
| RNF-17 consultas ≤ 3 s | Cada consulta principal responde en no más de 3 s bajo carga piloto | Prueba de carga de consultas definidas con diez usuarios `PR-RNF-17` | 8 | diseñado |
| RNF-18 concurrencia mínima | Diez usuarios simultáneos completan flujos sin degradación crítica ni corrupción | Escenario concurrente con latencias, errores y reconciliación de saldos `PR-RNF-18` | 8 | diseñado |
| RNF-19 disponibilidad ≥ 95 % | Disponibilidad mensual del piloto ≥ 95 %, excluyendo mantenimiento registrado | Monitor externo, registro de incidentes y cálculo de disponibilidad `OP-RNF-19` | Piloto | diseñado |
| RNF-20 autenticación ≤ 2 s | Registro/login medidos en condiciones normales responden en no más de 2 s | Prueba de carga de autenticación con Supabase/infraestructura del piloto `PR-RNF-20` | 1–8 | diseñado |
| RNF-21 backup cada 24 h | No existe intervalo mayor a 24 h entre backups válidos de BD; archivos siguen política equivalente | Registro automático, alerta por fallo y restauración ensayada `OP-RNF-21` | 8 | diseñado |
| RNF-22 archivo ≤ 4 s | Un comprobante autorizado se recupera en no más de 4 s; uno ajeno nunca se entrega | Pruebas S3 de autorización, expiración y tiempo máximo `PR-RNF-22` | 5–8 | diseñado |
| RNF-23 auditoría 100 % crítica | Cada operación del catálogo crítico genera exactamente un evento consultable | Matriz operación-evento y reconciliación automatizada `CT-RNF-23` | 1–8 | diseñado |
| RNF-24 recuperación ≤ 10 min | Tras reiniciar el VPS, API, Worker y dependencias principales recuperan servicio en ≤ 10 min | Simulacro cronometrado con systemd/Compose y evidencia del monitor `OP-RNF-24` | 8 | diseñado |
| RNF-25 Android 10+ | Flujos críticos funcionan en Android 10 y versiones superiores sobre equipos de gama media | Matriz de dispositivos/emuladores y pruebas Flutter `MB-RNF-25` | Piloto | diseñado |

## Operaciones críticas para RNF-23

Como mínimo deben auditarse:

- registro, login, refresh, recuperación y cambio de contraseña;
- registro/revocación biométrica y validaciones OTP;
- cambios de perfil, roles, integrantes e invitaciones;
- creación, modificación, anulación o eliminación lógica financiera;
- transferencias internas y operaciones de caja;
- acceso, corrección y eliminación documental;
- exportaciones;
- cambios de suscripción;
- eliminación de usuario o grupo.

La auditoría no guarda contraseñas, tokens, OTP, firmas, atestaciones, archivos ni IP completa.

## Evidencia del piloto

Las pruebas de rendimiento deben registrar:

- fecha, versión desplegada y configuración del VPS;
- versión de aplicación Android;
- cantidad de usuarios y escenario;
- conectividad utilizada;
- volumen inicial de datos;
- latencia mínima, promedio, percentiles y máxima cuando corresponda;
- tasa de errores;
- logs, métricas y correlation IDs asociados.

Los RNF sólo cambian a `verificado` cuando la evidencia queda versionada o enlazada desde esta matriz.

## Actualización de implementación — 2026-07-26

- La aplicación Flutter existe en un repositorio externo declarado por el responsable. Su
  build, pruebas y matriz Android deben enlazarse desde la evidencia del piloto antes de
  cambiar RNF-06/RNF-25 a `verificado`.
- Las operaciones mutables HTTP generan auditoría transversal sin leer cuerpos sensibles.
- Se incorporó una política única y configurable de contraseñas y administración de
  usuarios activos/inactivos con revocación de sesiones.
- Amazon SES y Google Play Developer API quedaron integrados y configurables; su
  verificación permanece pendiente de credenciales, sandbox y evidencia del proveedor.
- CI, Compose productivo, prueba k6 y runbook Supabase están preparados.
- La plantilla ejecutable de resultados se encuentra en `EVIDENCIA_PILOTO_TEMPLATE.md`.

Esta actualización representa implementación y preparación. El piloto, la carga, la
restauración y la matriz Android aún no se han ejecutado, por lo que los RNF afectados no
se marcan artificialmente como verificados.

## Actualización técnica — suscripciones y documentos

- Google Play incorpora historial de transacciones, cambio de plan y RTDN autenticado y
  deduplicado.
- El Worker genera avisos de vencimiento 7/3/1 días, el día de vencimiento y posteriores.
- Las cargas documentales verifican firma binaria, MIME, extensión, nombre, tamaño real,
  duplicados, XML peligroso y límite configurable por usuario.
- Las duraciones de JWT refresh, OTP, verificación OTP, recuperación de contraseña,
  límites documentales y rate limit son configurables.
- Se eliminaron los comentarios `TODO/FIXME` conocidos del código productivo.
