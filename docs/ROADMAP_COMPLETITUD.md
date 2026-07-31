# Roadmap para completar Finanzas Inteligentes

Este roadmap convierte el alcance, RF-01..21 y RNF-01..25 en una secuencia ejecutable. “100 %” significa que cada requisito está implementado, probado y respaldado por evidencia en el ambiente del piloto; no basta con que exista en OpenAPI o en una migración.

## 1. Punto de partida

| Área | Estado comprobado | Brecha principal |
|---|---|---|
| Alcance y contrato | 132 operaciones y 116 esquemas documentados y validados | Mantener compatibilidad durante la implementación |
| Backend | Solución .NET 10 compilable; 15 rutas de negocio/configuración mapeadas | Implementar las restantes verticales, reglas y proveedores |
| Persistencia | Una migración inicial para identidad, finanzas e infraestructura | Completar migraciones M0010–M0070 y pruebas sobre PostgreSQL real |
| Seguridad | JWT, hash de contraseña, rate limit y filtros por usuario iniciales | Refresh rotativo, recuperación, OTP, biometría, auditoría y hardening |
| Procesamiento | Worker y outbox iniciales | Leases, deduplicación, reintentos, consumidores y concurrencia segura |
| Pruebas | 8 pruebas: 3 unitarias, 1 integración de modelo, 3 arquitectura y 1 contrato | Cobertura funcional, PostgreSQL, seguridad, concurrencia, carga y móvil |
| Aplicación móvil | No está incluida en este repositorio | Crear e integrar la aplicación Flutter Android 10+ |
| Infraestructura piloto | Docker Compose local con PostgreSQL, Redis y MinIO | Supabase, S3, SES, Hetzner, Nginx, Cloudflare, systemd y observabilidad |
| Evidencia RNF | Matriz y criterios definidos | Ejecutar pruebas y adjuntar resultados del ambiente objetivo |

La cobertura documental no debe usarse como porcentaje de avance de implementación. Como referencia de planificación, el producto ejecutable está todavía en la fase de fundación/MVP inicial.

## 2. Reglas de ejecución

Cada capacidad se desarrolla como una vertical:

```text
migración -> dominio -> caso de uso -> endpoint -> OpenAPI
          -> pruebas -> Flutter -> métricas/auditoría
```

Una operación queda terminada únicamente cuando:

1. aplica migraciones desde una base vacía y desde la versión anterior;
2. implementa autorización, validación e invariantes;
3. coincide con el OpenAPI y sus códigos de error;
4. incluye idempotencia, ETag, transacción u outbox cuando corresponda;
5. tiene pruebas unitarias, integración y contrato;
6. registra auditoría y métricas sin secretos;
7. está integrada en Flutter con carga, vacío, error y reintento;
8. actualiza la trazabilidad RF/RNF con evidencia ejecutada.

## 3. Roadmap de implementación

Las duraciones son estimaciones para una persona con dedicación completa. Incluyen desarrollo y pruebas técnicas, pero no esperas de aprobación de proveedores ni el período de observación del piloto.

### Hito 0 — Decisiones y gestión del proyecto

**Duración estimada:** 3–5 días.
**Estado:** decisiones de alcance y proveedores aprobadas el 29 de julio de 2026; consultar [`DECISIONES_PILOTO.md`](DECISIONES_PILOTO.md).

**Acciones**

- convertir RF, RNF y operaciones OpenAPI en backlog trazable;
- definir criterios de aceptación de usabilidad y “degradación crítica”;
- ejecutar el gate de 30–50 comprobantes para confirmar Amazon Textract;
- aplicar biometría Android como reautorización local;
- configurar Google Play Billing con license testers;
- definir retención, anonimización, borrado y recuperación;
- aprobar plantillas de correo, alertas y textos de consentimiento;
- crear repositorio/flujo Git, revisión de código y CI.

**Salida**

- ninguna decisión de producto bloquea los hitos 1–9;
- cada historia enlaza RF, operación, prueba y RNF aplicable.

### Hito 1 — Completar la fundación técnica

**Duración estimada:** 1–2 semanas.  
**RNF principales:** RNF-01, 03, 09, 11, 12, 14.

**Backend**

- separar las migraciones fundacionales de las migraciones funcionales previstas;
- completar tablas de idempotencia, trabajos, leases y consumos del outbox;
- reclamar trabajos con bloqueo PostgreSQL seguro para múltiples Workers;
- implementar reintentos con backoff, dead-letter y deduplicación por consumidor;
- validar toda la configuración al iniciar;
- añadir logs JSON, OpenTelemetry, métricas y trazas;
- ampliar readiness a PostgreSQL, Redis y almacenamiento cuando sean requeridos;
- incorporar CI para build, validación documental/OpenAPI, pruebas y análisis de dependencias.

**Pruebas obligatorias**

- migración desde base vacía en PostgreSQL real;
- API y Worker sin privilegios DDL;
- dos Workers no procesan dos veces el mismo efecto;
- caída de una dependencia afecta readiness y no liveness;
- escaneo de secretos.

**Salida**

- Etapa 0 verificada y base confiable para todas las verticales.

### Hito 2 — Identidad y seguridad completa

**Duración estimada:** 2–3 semanas.  
**RF:** RF-01, 02, 18, 19 y 20.  
**RNF:** RNF-01, 02, 20 y 23.
**Estado:** implementación e integración móvil completadas; resta validación con
SES y dispositivos Android en el ambiente del piloto. Consultar
[`backend/IDENTIDAD_Y_SEGURIDAD.md`](backend/IDENTIDAD_Y_SEGURIDAD.md).

**Capacidades**

- refresh token rotativo, familias de sesión, cierre y revocación;
- perfil, preferencias y cambio de contraseña;
- recuperación/restablecimiento sin enumeración de usuarios;
- dispositivos confiables;
- desafío y verificación OTP con HMAC versionado;
- reautorización biométrica local sin almacenar credenciales biométricas en backend;
- eventos de seguridad y auditoría;
- eliminación/anonimización de perfil;
- rate limits específicos por riesgo y operación.

**Pruebas obligatorias**

- reutilizar un refresh revoca toda su familia;
- tokens, OTP y desafíos expiran y se consumen una sola vez;
- un usuario no accede a dispositivos, sesiones o eventos ajenos;
- autenticación bajo carga responde en ≤ 2 segundos;
- el 100 % de operaciones críticas del hito produce auditoría.

**Salida**

- RF-01, RF-02, RF-18, RF-19 y RF-20 implementados;
- Flutter puede registrar, autenticar, renovar y cerrar sesión.

### Hito 3 — Finanzas personales completas

**Duración estimada:** 3–4 semanas.  
**RF:** RF-05, 06 y 10.  
**Capacidades adicionales:** cuentas, tarjetas, transferencias y recurrencias.
**Estado:** CRUD móvil/API integrado para cuentas, categorías, movimientos,
tarjetas por alias, transferencias internas y recurrencias; resta ejecutar las
pruebas de PostgreSQL/Worker y concurrencia del hito en el ambiente objetivo.

**Capacidades**

- completar CRUD y paginación estable de cuentas, categorías y movimientos;
- categorías predefinidas e inmutables;
- actualización y eliminación/anulación con ETag;
- idempotencia real en altas;
- tarjetas identificadas por alias, sin emisor, últimos cuatro, PAN, CVV ni ningún dato del plástico;
- transferencias contables internas atómicas;
- movimientos recurrentes generados por Worker;
- filtros, cursores, saldos y conciliación.

**Pruebas obligatorias**

- aislamiento entre usuarios;
- dos gastos concurrentes no pierden saldo;
- reintentar un alta no duplica el movimiento;
- transferencia genera dos movimientos o ninguno;
- una anulación revierte exactamente una vez;
- una recurrencia genera como máximo un movimiento por período.

**Salida**

- primer backend MVP completo y apto para integrar pantallas financieras.

### Hito 4 — Aplicación Flutter base e integración del MVP

**Duración estimada:** 3–5 semanas; puede comenzar cuando el hito 2 estabilice sus contratos.  
**RNF:** RNF-06 y 25.
**Estado:** aplicación Flutter conectada mediante repositorios intercambiables
mock/API; análisis estático, pruebas y APK debug verificados localmente. La
validación sobre dispositivo Android y ambiente desplegado queda para el piloto.

**Aplicación**

- crear proyecto Flutter con Application ID `com.tesis.finanzasinteligentes`;
- Android mínimo 10, configuración por ambientes y almacenamiento seguro;
- cliente HTTP generado o tipado desde OpenAPI;
- autenticación, renovación y cierre de sesión;
- cuentas, categorías y movimientos;
- repositorios mock y API intercambiables;
- manejo uniforme de ProblemDetails y correlation ID;
- estados de carga, vacío, error, reintento y sesión expirada;
- pruebas de widgets, integración y accesibilidad básica.

**Salida**

- APK instalable y flujo extremo a extremo de registro → login → movimiento;
- `flutter analyze`, `flutter test` y `flutter build apk` pasan en CI.

### Hito 5 — Finanzas familiares y caja

**Duración estimada:** 3–4 semanas.  
**RF:** RF-03, 04, 05, 10 y 13.  
**RNF:** RNF-07 y 23.
**Estado:** flujo móvil/API integrado para grupos, integrantes, invitaciones con
token y código, cuentas compartidas, categorías familiares, movimientos, caja,
retiros con OTP y presupuestos. Los grupos nuevos reciben categorías iniciales.
Restan las pruebas sobre PostgreSQL real y las pruebas de concurrencia/atomicidad,
reservadas para el cierre del flujo completo.

**Capacidades**

- grupos y propietario único;
- administradores delegados e integrantes;
- invitaciones por correo o identificador;
- aceptación, revocación, vencimiento y transferencia de propiedad;
- categorías familiares y cuentas compartidas;
- movimientos familiares;
- caja lógica, aportes, retiros y compensaciones;
- OTP en retiros/acciones sensibles;
- pantallas Flutter correspondientes.

**Pruebas obligatorias**

- ningún integrante consulta otro grupo;
- no puede quedar un grupo sin propietario;
- una invitación aceptada dos veces crea una membresía;
- compartir una cuenta no transfiere su propiedad;
- caja, movimiento y saldo cambian atómicamente.

**Salida**

- RF-03, RF-04 y RF-13 implementados; RF-05 y RF-10 completos en ambos ámbitos.

### Hito 6 — Presupuestos y metas

**Duración estimada:** 2–3 semanas.  
**RF:** RF-11 y 12.
**Estado:** presupuestos privados y familiares, resumen, metas privadas y
compartidas y aportes están integrados entre Flutter y API con ETag e
idempotencia. Las pruebas de concurrencia se ejecutarán al finalizar el flujo
completo.

**Capacidades**

- presupuestos privados y familiares por categoría;
- resumen presupuestario y salud;
- metas privadas/compartidas y aportes;
- atomicidad entre aporte, cuenta y movimiento;
- pantallas Flutter y alertas visuales básicas.

**Pruebas obligatorias**

- reglas de solapamiento;
- separación de ámbito;
- cálculos con PYG entero;
- gasto derivado exclusivamente de movimientos confirmados;
- una meta con saldo no se elimina sin compensación.

### Hito 7 — Documentos, OCR, SIFEN y exportaciones

**Duración estimada:** 4–6 semanas.  
**Estado:** contrato HTTP y flujo móvil/API integrados para carga multipart con
SHA-256, procesamiento asíncrono, XML SIFEN, revisión/corrección, asociación del
documento al movimiento, reprocesamiento, descarga segura y exportaciones PDF/XLSX.
El parser SIFEN reconoce el CDC tanto en elementos como en atributos. Resta
confirmar Amazon Textract mediante el gate de 30–50 comprobantes, configurar el
almacenamiento S3 del piloto y ejecutar las pruebas reales con API, Worker y
PostgreSQL desplegados.
**RF:** RF-07, 08, 09 y 15.  
**RNF:** RNF-03, 05, 08, 16 y 22.

**Capacidades**

- almacenamiento privado mediante interfaz S3/MinIO;
- cargas con validación de MIME, tamaño y SHA-256;
- URL prefirmada autorizada y temporal;
- procesamiento asíncrono y reintentable;
- proveedor OCR real;
- parser XML SIFEN sin entidades externas;
- corrección con conservación del resultado original;
- creación de movimientos desde datos confirmados;
- exportaciones PDF y XLSX; CSV adicional;
- limpieza de archivos huérfanos y exportaciones vencidas;
- flujos Flutter de carga, revisión, corrección y descarga.

**Pruebas obligatorias**

- acceso cruzado a archivos siempre denegado;
- OCR promedio ≤ 5 segundos para el conjunto piloto;
- recuperación autorizada de archivo ≤ 4 segundos;
- proveedor caído no pierde trabajos;
- XML malicioso o repetido se rechaza de forma segura.

### Hito 8 — Analítica explicable

**Duración estimada:** 3–4 semanas.  
**RF:** RF-14, 16 y 17.  
**RNF:** RNF-04, 10 y 17.
**Estado:** dashboard, reportes, proyección mensual, alertas explicables y
indicador de salud financiera versionado están integrados entre Flutter y API.
Flutter muestra factores positivos/negativos, recomendaciones, período y versión
del modelo; las alertas pueden marcarse como leídas y archivarse con ETag. Restan
las pruebas de reconciliación y rendimiento sobre PostgreSQL/Worker reales, la
caché Redis y la validación de las variantes analíticas familiares en el piloto.

**Capacidades**

- dashboards y reportes privados/familiares;
- comparativa real contra presupuesto;
- caché Redis e invalidación mediante outbox;
- alertas reproducibles y explicables;
- score financiero versionado;
- proyecciones mensuales/por categoría;
- marca `preliminar` con menos de tres meses;
- pantallas y gráficos Flutter.

**Pruebas obligatorias**

- totales reconcilian con movimientos;
- Redis nunca es fuente de verdad;
- consultas principales ≤ 3 segundos bajo carga piloto;
- mismos datos y versión producen resultados reproducibles;
- datos insuficientes generan la respuesta contratada.

### Hito 9 — Suscripciones y notificaciones

**Duración estimada:** 2–3 semanas.  
**RF:** RF-21.  
**RNF:** RNF-14 y 23.

**Estado local al 30 de julio de 2026:** implementación integrada. Quedan como
gates externos la configuración de Play Console/Firebase, pruebas con license
testers y evidencia en dispositivos reales.

**Capacidades**

- planes y matriz de capacidades;
- suscripción, cancelación y restauración idempotentes;
- Google Play Billing con compra de prueba, verificación backend y reconocimiento;
- sincronización de renovaciones, cancelaciones y restauraciones;
- Real-time Developer Notifications;
- notificaciones por outbox;
- Amazon SES y push móvil mediante Firebase Cloud Messaging;
- preferencias y horario silencioso.

**Pruebas obligatorias**

- una única suscripción activa/en gracia;
- nunca se confía en el plan informado por el cliente;
- compra aprobada, rechazada, pendiente, renovada, cancelada y restaurada verificadas;
- eventos repetidos no duplican efectos;
- una caída de SES/proveedor no revierte la operación principal.

### Hito 10 — Infraestructura del piloto

**Duración estimada:** 2–3 semanas, más tiempos de alta/verificación de proveedores.  
**RNF:** RNF-08, 09, 10, 12, 13, 14, 19, 21 y 24.

**Infraestructura**

- proyecto Supabase y roles mínimos para API, Worker y migrador;
- buckets S3 privados, cifrado, CORS restringido y lifecycle;
- Redis para el entorno piloto;
- Amazon SES con dominio verificado;
- VPS Hetzner CX33 endurecido;
- Docker Compose de piloto sin bases locales innecesarias;
- Nginx, TLS, Cloudflare y acceso directo a la API bloqueado;
- unidad systemd para recuperar servicios;
- gestión externa de secretos;
- métricas, logs, alertas y monitor externo;
- backups cada 24 horas y restauración ensayada.

**Salida**

- despliegue repetible desde una versión etiquetada;
- reinicio completo recupera servicio en ≤ 10 minutos;
- rollback y restauración documentados.

### Hito 11 — Endurecimiento, beta y evidencia final

**Duración estimada:** 3–5 semanas, más los 28 días del piloto.
**RF:** verificación final RF-01..21.  
**RNF:** verificación final RNF-01..25.

**Calidad y seguridad**

- pruebas end-to-end de las 132 operaciones aplicables;
- pruebas negativas de autorización entre usuarios y grupos;
- carga con diez usuarios simultáneos;
- OWASP, dependencias, secretos y revisión de logs;
- retención, anonimización y borrado;
- APK/AAB release firmado;
- distribución Google Play beta interna;
- matriz Android 10+ en dispositivos/emuladores;
- runbooks de incidentes, migraciones, rollback y restauración.

**Piloto**

- seleccionar diez participantes y registrar consentimiento;
- encuesta previa;
- guion de tareas y soporte;
- ejecución en Asunción;
- encuesta posterior e informe;
- registrar disponibilidad, latencias, errores e incidentes;
- enlazar toda evidencia en `TRAZABILIDAD_RF.md` y `TRAZABILIDAD_RNF.md`.

**Salida**

- ninguna RF permanece en `diseñado` o `parcial`;
- cada RNF tiene evidencia suficiente para estado `verificado`;
- no existen defectos críticos abiertos;
- la versión candidata puede restaurarse, desplegarse y operarse de forma repetible.

## 4. Secuencia y calendario orientativo

```text
H0 Decisiones
  └─ H1 Fundación
      ├─ H2 Identidad ── H4 Flutter MVP
      └─ H3 Finanzas ───┘
          ├─ H5 Familia ── H6 Presupuestos/metas
          └─ H7 Documentos/OCR/exportación
                    └─ H8 Analítica
H2 + H1 ──────────────── H9 Suscripciones/notificaciones
H1 en adelante ───────── H10 Infraestructura piloto
Todos ────────────────── H11 Beta, piloto y evidencia
```

Estimación secuencial para una persona: **26–40 semanas de ingeniería**, más la ventana necesaria para medir disponibilidad y realizar el piloto. Con un equipo de 2–3 personas que divida backend, Flutter y plataforma/QA, el calendario puede reducirse, pero no las pruebas ni la observación requerida.

## 5. Próximo incremento recomendado

El siguiente objetivo no debe ser implementar endpoints aislados. Debe cerrar una primera ruta extremo a extremo:

1. completar el hito 0;
2. cerrar outbox, idempotencia, telemetría y CI del hito 1;
3. implementar refresh, cierre de sesión y recuperación del hito 2;
4. completar idempotencia/concurrencia de movimientos del hito 3;
5. crear Flutter e integrar registro, login, cuentas y movimientos;
6. desplegar este corte en un ambiente de prueba similar al piloto;
7. actualizar trazabilidad con resultados reales.

Este incremento entrega una base operable y reduce el mayor riesgo actual: descubrir tarde que contrato, persistencia, seguridad y aplicación móvil no funcionan juntos.

## 6. Acciones que requieren al responsable del proyecto

- habilitar cuentas y presupuestos de Supabase, AWS, Hetzner, Cloudflare y Google Play;
- habilitar Firebase Cloud Messaging y Google Play Billing de prueba;
- aportar dominio para SES, TLS y correo;
- ejecutar el gate de Textract con 30–50 comprobantes y custodiar credenciales de prueba;
- definir responsable de privacidad y retención;
- reclutar los diez participantes;
- aprobar encuestas, consentimiento y criterios de éxito;
- disponer de dispositivos Android 10+ para validación;
- decidir tamaño del equipo y fecha objetivo.

Las credenciales nunca deben enviarse por documentación o commits; se configuran mediante un gestor de secretos o variables seguras del pipeline.

## 7. Control de completitud

El proyecto puede declararse al 100 % únicamente si se cumplen simultáneamente estos gates:

| Gate | Condición |
|---|---|
| Contrato | 132 operaciones implementadas o una decisión de alcance aprobada elimina las no aplicables |
| Funcional | RF-01..21 en estado `verificado` |
| No funcional | RNF-01..25 en estado `verificado` con evidencia |
| Backend | Migraciones, API y Worker desplegables desde cero |
| Móvil | AAB firmado, Android 10+, pruebas y distribución beta |
| Seguridad | Sin vulnerabilidades críticas ni filtración cruzada |
| Rendimiento | Límites RNF-16, 17, 18, 20 y 22 satisfechos |
| Operación | Backup, restauración, disponibilidad y recuperación comprobados |
| Piloto | Diez participantes, encuestas pre/post e informe final |
