# Arquitectura técnica del backend

## 1. Objetivo y alcance

La arquitectura debe soportar todos los endpoints documentados sin trasladar todavía la complejidad operativa de microservicios al proyecto. La aplicación Flutter nunca debe conectarse directamente a PostgreSQL o S3; toda autorización y regla de negocio se ejecuta en la API.

La solución se divide por capacidades de negocio:

| Módulo | Responsabilidad |
|---|---|
| `Identidad` | Usuarios, perfil, preferencias, sesiones y contraseñas |
| `Seguridad` | Dispositivos, OTP, biometría, controles de riesgo |
| `FinanzasPersonales` | Cuentas, tarjetas, categorías, movimientos, transferencias y recurrencias |
| `Familia` | Grupos, roles, invitaciones, cuentas compartidas y caja |
| `Documentos` | Archivos, OCR, PDF y XML SIFEN |
| `Analitica` | Dashboard, reportes, alertas, score y predicciones |
| `Suscripciones` | Planes, suscripciones y capacidades habilitadas |
| `Notificaciones` | Preferencias, correos, push y registro de entregas |
| `Auditoria` | Trazabilidad de operaciones sensibles |

## 2. Topología de despliegue

```mermaid
flowchart LR
    App["Flutter Android"] -->|HTTPS| CF["Cloudflare"]
    CF --> Nginx["Nginx TLS"]
    Nginx --> API["API .NET 10"]
    API --> PG[("PostgreSQL / Supabase")]
    API --> S3[("S3 privado")]
    API -->|outbox / trabajos| PG
    Worker["Worker .NET 10"] -->|reclama trabajos| PG
    Worker --> S3
    Worker --> Textract["Textract"]
    Worker --> SES["SES"]
    Worker --> FCM["Firebase FCM"]
    App --> Play["Google Play Billing"]
    API --> PlayApi["Google Play Developer API"]
```

En desarrollo local se levantan `api`, `worker` y PostgreSQL mediante Docker Compose; el
almacenamiento local implementa el mismo puerto que S3. En el piloto, API, Worker y Nginx
se ejecutan en una instancia Lightsail de North Virginia y consumen Supabase Pro, S3,
Textract y SES en `us-east-1`. La API atiende solicitudes breves; el Worker procesa:

- OCR y lectura de XML SIFEN;
- exportaciones;
- correo y notificaciones;
- generación de movimientos recurrentes;
- alertas, score y predicciones;
- limpieza de tokens, trabajos y archivos huérfanos.

PostgreSQL funciona inicialmente como cola durable. Una cola externa sólo se incorpora si el volumen, la latencia o la operación lo justifican.

### 2.1 Exposición y operación del piloto

- Nginx es el único punto de entrada al backend y termina TLS;
- Cloudflare administra dominio y protección externa básica;
- Docker Compose administra los contenedores de aplicación;
- systemd inicia Compose tras un reinicio y aplica una política controlada de reinicio;
- Supabase no se expone a Flutter: sólo API/Worker reciben credenciales;
- S3 utiliza buckets privados y URLs prefirmadas;
- SES envía invitaciones, recuperación y notificaciones;
- backups, monitor externo y simulacro de reinicio generan evidencia para RNF-19, RNF-21 y RNF-24.

## 3. Estructura propuesta de la solución

```text
backend/
├── FinanzasInteligentes.sln
├── Directory.Build.props
├── src/
│   ├── FinanzasInteligentes.Api/
│   ├── FinanzasInteligentes.Worker/
│   ├── FinanzasInteligentes.BuildingBlocks/
│   ├── FinanzasInteligentes.Dominio/
│   │   └── {Identidad, FinanzasPersonales, Familia, ...}/
│   ├── FinanzasInteligentes.Aplicacion/
│   │   └── <Módulo>/<Recurso>/<CasoDeUso>/
│   └── FinanzasInteligentes.Infraestructura/
│       ├── Persistencia/
│       └── Adaptadores/
├── tests/
│   ├── FinanzasInteligentes.UnitTests/
│   ├── FinanzasInteligentes.IntegrationTests/
│   ├── FinanzasInteligentes.ArchitectureTests/
│   └── FinanzasInteligentes.ContractTests/
└── deploy/
    ├── compose.yaml
    └── env.example
```

Responsabilidades por capa:

- **Dominio:** entidades, objetos de valor, invariantes y eventos de dominio; no referencia EF, HTTP ni proveedores.
- **Aplicación:** casos de uso, comandos, consultas, DTO, autorización por caso de uso y puertos.
- **Infraestructura:** EF Core, S3, JWT y adaptadores externos.
- **Endpoints:** rutas, binding, códigos HTTP, ProblemDetails y OpenAPI.

Los módulos no acceden directamente a tablas de otro módulo. La interacción síncrona se hace mediante interfaces de aplicación y la asíncrona mediante eventos almacenados en outbox.

`FinanzasInteligentes.Infraestructura` compone el `DbContext`, repositorios, autenticación, adaptadores y el historial de migraciones; no contiene reglas de negocio. Los módulos se organizan como verticales dentro de Dominio, Aplicación y Endpoints para evitar una proliferación prematura de ensamblados.

## 4. Persistencia

### 4.1 DbContext y esquemas

Para la primera versión se utiliza un solo `FinanzasDbContext` y un historial lineal de migraciones. Esto permite que un movimiento, una transferencia o una operación de caja sean atómicos incluso cuando afectan varias tablas.

Las configuraciones `IEntityTypeConfiguration<T>` se agrupan por módulo y asignan estas tablas:

| Esquema | Propietario |
|---|---|
| `identidad` | Identidad |
| `seguridad` | Seguridad |
| `finanzas` | FinanzasPersonales |
| `familia` | Familia |
| `documentos` | Documentos |
| `analitica` | Analitica |
| `suscripciones` | Suscripciones |
| `notificaciones` | Notificaciones |
| `auditoria` | Auditoria |
| `infra` | Idempotencia, outbox, consumos y trabajos |

No se recomienda un `DbContext` por módulo durante esta etapa: complicaría migraciones y transacciones sin aportar aislamiento operativo real. La separación futura sigue siendo posible porque los esquemas y propietarios ya están definidos.

### 4.2 Convenciones de datos

- nombres físicos en `snake_case`;
- claves `uuid`, creadas como UUID v7 en .NET;
- fecha/hora `timestamptz` y siempre UTC;
- fecha civil `date`;
- moneda ISO 4217 en `char(3)`; la primera versión sólo acepta `PYG`;
- importes PYG en `bigint`, expresados en guaraníes enteros;
- porcentajes y confianza en `numeric(8,6)`;
- documentos variables controlados en `jsonb`;
- correo en `citext`;
- concurrencia optimista con columna `version bigint`;
- `creado_en` y `actualizado_en` en entidades mutables;
- `eliminado_en` sólo donde el borrado lógico tiene valor real.

Los registros contables, operaciones de caja y auditoría son inmutables. Un error se corrige con anulación o movimiento compensatorio, conservando su relación con el original.

### 4.3 Saldos y valores derivados

| Dato | Estrategia |
|---|---|
| Saldo de cuenta | Snapshot en `finanzas.cuentas`, actualizado en la misma transacción que el movimiento |
| Saldo de caja | Snapshot en `familia.cajas`, actualizado junto con la operación inmutable |
| Gasto de presupuesto | Calculado desde movimientos confirmados; no se guarda un contador editable |
| Progreso de meta | Suma de aportes válidos; puede cachearse |
| Dashboard y reportes | Consulta o caché Redis; PostgreSQL sigue siendo la fuente de verdad |
| Score y predicción | Snapshot versionado generado por Worker |

Las actualizaciones de saldos bloquean la fila de cuenta o caja dentro de la transacción para evitar sobregiros concurrentes.

## 5. Transacciones y consistencia

### 5.1 Límites transaccionales

- **Movimiento:** insertar movimiento, actualizar saldo, asociar categorías/documento y escribir outbox.
- **Transferencia:** insertar transferencia y sus dos movimientos vinculados, actualizar ambos saldos y escribir outbox.
- **Aporte o retiro de caja:** validar OTP cuando corresponda, insertar operación, actualizar caja/cuenta y escribir auditoría/outbox.
- **Aporte a meta:** insertar aporte y, si existe una cuenta origen, crear el movimiento correspondiente en la misma transacción.
- **Invitación aceptada:** consumir token y crear membresía de manera atómica.

Alertas, notificaciones, dashboard, score, proyecciones y exportaciones se actualizan después mediante eventos. Su retraso no revierte la operación financiera principal.

### 5.2 Patrón outbox

La misma transacción del cambio de negocio agrega un registro a `infra.outbox_eventos`. Cada consumidor durable registra su avance en `infra.consumos_evento`, cuya clave única es `(evento_id, consumidor)`. El Worker:

1. reclama un lote corto con `FOR UPDATE SKIP LOCKED`, asigna un lease y confirma la transacción;
2. ejecuta cada consumidor fuera de la transacción de reclamo;
3. registra atómicamente el consumo o el trabajo derivado usando una clave de deduplicación;
4. reintenta con espera exponencial cuando vence el lease;
5. marca el evento como procesado sólo cuando todos sus consumidores obligatorios terminaron;
6. mueve los fallos permanentes a estado `fallido`.

No se publica un evento antes de confirmar la transacción. La entrega interna es **al menos una vez**; el efecto funcional se vuelve idempotente mediante `consumos_evento`, restricciones únicas de negocio y claves de deduplicación de proveedor. No se promete semántica física “exactamente una vez” frente a sistemas externos que no ofrezcan idempotencia.

## 6. Seguridad

- access token JWT de 10 a 15 minutos;
- refresh token aleatorio, rotativo y almacenado sólo como hash;
- contraseñas con Argon2id o el `PasswordHasher` vigente de ASP.NET Core, nunca cifrado reversible;
- refresh y tokens aleatorios de alta entropía almacenados mediante SHA-256;
- OTP y códigos cortos almacenados mediante HMAC-SHA-256 con una clave secreta versionada fuera de la base de datos; nunca mediante un hash rápido sin clave;
- secretos únicamente en variables de entorno o secret manager;
- rate limiting por IP, usuario y propósito;
- autorización familiar comprobada en cada caso de uso;
- URLs S3 prefirmadas y de corta duración;
- cifrado TLS en tránsito y cifrado gestionado por proveedor en reposo;
- auditoría sin contraseñas, tokens, OTP ni contenido binario.

La conexión PostgreSQL usada por la API no se entrega al cliente. En el piloto Supabase funciona exclusivamente como PostgreSQL administrado: cualquier clave con privilegios elevados permanece en backend y los roles públicos no reciben acceso directo a los esquemas.

## 7. Idempotencia y concurrencia

Las rutas que el contrato OpenAPI marque con `Idempotency-Key` guardan el método, la plantilla de ruta, el sujeto autenticado —o una huella pública controlada—, el hash canónico de la solicitud y el resultado:

- misma clave y mismo cuerpo: devuelve el resultado anterior;
- misma clave y cuerpo diferente: `409 Conflict`;
- operación todavía en proceso: `409 Conflict` con código `idempotencia_en_proceso`;
- claves vencidas: se eliminan por Worker según la retención.

El resultado persistido incluye código HTTP, cuerpo, `Content-Type` y los encabezados reproducibles necesarios, como `Location` y `ETag`. No se almacenan `Set-Cookie`, credenciales ni encabezados hop-by-hop. Los `5xx` transitorios no quedan registrados como resultado exitoso reutilizable.

El `ETag` expone `version`. Un `If-Match` desactualizado produce `412 Precondition Failed`; la actualización SQL incluye `WHERE id = @id AND version = @version`.

## 8. Archivos y procesos

La carga crea primero metadatos en PostgreSQL y luego confirma el objeto privado en S3. El nombre de objeto es opaco y no contiene datos personales. Se registra SHA-256 para integridad y deduplicación.

Estados del archivo:

```text
pendiente -> cargando -> disponible -> eliminado
                    \-> fallido
```

Estados de cada procesamiento:

```text
pendiente -> procesando -> completado
                       \-> incompleto
                       \-> fallido
```

Si una transacción falla después de cargar un objeto, un trabajo de limpieza elimina el archivo huérfano. Nunca se guarda un archivo completo en PostgreSQL.

## 9. Observabilidad y operación

- logs JSON con `correlationId`, `usuarioId` pseudonimizado, módulo y duración;
- OpenTelemetry para trazas y métricas;
- health checks separados: `/salud/vivo` y `/salud/listo`;
- métricas de latencia, errores, conexiones, cola pendiente, reintentos y tiempo de OCR;
- `ProblemDetails` uniforme y sin detalles internos;
- backups automáticos de PostgreSQL y prueba periódica de restauración;
- migraciones aplicadas por un job de despliegue, no por cada réplica de API al iniciar.

## 10. Pruebas obligatorias

| Tipo | Qué valida |
|---|---|
| Unitarias | Invariantes, permisos, cálculos y transiciones de estado |
| Integración | EF Core contra PostgreSQL real mediante contenedor |
| Contrato | Rutas, JSON, códigos y ProblemDetails de `docs/api` |
| Arquitectura | Dependencias permitidas y propiedad de módulos |
| Concurrencia | Idempotencia, ETag, doble gasto y aceptación doble de invitación |
| Seguridad | Acceso entre usuarios/grupos, expiración, rotación y rate limit |
| Migraciones | Base vacía a última versión y actualización desde la versión anterior |

SQLite no sustituye PostgreSQL en pruebas de integración porque no reproduce `citext`, índices parciales, `jsonb`, bloqueos ni semántica de concurrencia.

El archivo [`openapi.yaml`](../api/openapi.yaml) versionado es la fuente de verdad del contrato HTTP. Las pruebas de contrato deben validarlo junto con las rutas y ejemplos documentados.

## 11. Criterios para extraer un microservicio

No se separa un módulo por anticipación. La extracción se evalúa sólo si aparecen uno o más motivos medibles:

- carga o escalado muy diferente;
- requisitos propios de disponibilidad o cumplimiento;
- equipo independiente y ciclos de despliegue distintos;
- tecnología de persistencia realmente diferente;
- fallos de ese módulo afectan al resto con frecuencia.

Los primeros candidatos serían Documentos/OCR, Notificaciones y Analítica. Identidad sólo se separaría si se adopta un proveedor o servicio de identidad dedicado.
