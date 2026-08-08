# Backend Finanzas Inteligentes

Solución .NET 10 basada en el contrato de `docs/api/openapi.yaml` y en la arquitectura de monolito modular relevada.

El alcance canónico se encuentra en [`docs/ALCANCE_Y_REQUISITOS.md`](../docs/ALCANCE_Y_REQUISITOS.md), con trazabilidad funcional y no funcional en `docs/api`.

## Contenido

| Proyecto | Responsabilidad |
|---|---|
| `FinanzasInteligentes.Api` | API REST `/api/v1`, JWT, rate limiting, ProblemDetails y health checks |
| `FinanzasInteligentes.Worker` | Procesamiento durable del outbox |
| `FinanzasInteligentes.Aplicacion` | Casos de uso, comandos, consultas, mapeos y puertos |
| `FinanzasInteligentes.Dominio` | Entidades e invariantes de identidad, finanzas e infraestructura |
| `FinanzasInteligentes.Infraestructura` | EF Core, PostgreSQL, repositorios, JWT, hashing y migraciones |
| `FinanzasInteligentes.BuildingBlocks` | Primitivas compartidas sin dependencias de infraestructura |
| `FinanzasInteligentes.UnitTests` | Pruebas de invariantes |
| `FinanzasInteligentes.IntegrationTests` | Configuración del modelo PostgreSQL e integraciones |
| `FinanzasInteligentes.ArchitectureTests` | Dependencias permitidas entre capas |
| `FinanzasInteligentes.ContractTests` | Cobertura básica del OpenAPI relevado |

La solución está en [`FinanzasInteligentes.sln`](FinanzasInteligentes.sln).

## Organización del código

La solución usa capas a nivel de ensamblado y módulos/casos de uso dentro de cada capa:

```text
src/
├── FinanzasInteligentes.Api/
│   ├── Endpoints/{Identidad,FinanzasPersonales}/
│   ├── Middleware/
│   ├── Dependencies/
│   └── Program.cs
├── FinanzasInteligentes.Aplicacion/
│   ├── Identidad/{CrearUsuario,CrearSesion,ObtenerPerfil}/
│   ├── FinanzasPersonales/{Cuentas,Categorias,Movimientos}/
│   ├── Abstracciones/
│   └── Excepciones/
├── FinanzasInteligentes.Dominio/
│   ├── Identidad/Entidades/
│   ├── FinanzasPersonales/Entidades/
│   └── Excepciones/
└── FinanzasInteligentes.Infraestructura/
    ├── Autenticacion/
    └── Persistencia/{Repositorios,Migraciones}/
```

Los endpoints sólo realizan binding HTTP y delegan en un handler. Los handlers coordinan repositorios y transacciones; el dominio conserva invariantes, e Infraestructura implementa EF Core y proveedores.

## Estado funcional

La solución está compilable y contiene las verticales de backend previstas por el
contrato. Entre las capacidades verificadas localmente se encuentran:

- fundación de API, Worker, persistencia, JWT, correlación, ProblemDetails, rate limiting y health checks;
- registro, inicio de sesión, refresh rotativo, revocación y consulta de perfil;
- OTP, recuperación/cambio de contraseña, sesiones, dispositivos y auditoría;
- altas y consultas de cuentas y categorías;
- altas, consultas y anulaciones de movimientos;
- transacción de movimiento + saldo + outbox;
- concurrencia optimista mediante `ETag`/`If-Match`;
- migración EF inicial para los esquemas `identidad`, `finanzas` e `infra`;
- PostgreSQL preparado para desarrollo local mediante Docker Compose y almacenamiento
  intercambiable local/S3;
- verificación de Google Play Billing, RTDN y envío push FCM HTTP v1 mediante outbox.

La activación de credenciales reales de proveedores, la evidencia sobre PostgreSQL
desplegado y las validaciones del piloto continúan según el roadmap. El detalle del bloque de
seguridad está en
[`IDENTIDAD_Y_SEGURIDAD.md`](../docs/backend/IDENTIDAD_Y_SEGURIDAD.md).

## Requisitos

- .NET SDK 10.0.201 o compatible.
- PostgreSQL 16+.
- Docker Compose para el entorno completo.

## Compilar y probar

```bash
dotnet tool restore
dotnet build FinanzasInteligentes.sln
dotnet test FinanzasInteligentes.sln
```

## Ejecutar localmente

Definir configuración mediante variables de entorno; no se versionan secretos:

```bash
export ConnectionStrings__PostgreSql='Host=localhost;Port=5432;Database=finanzas;Username=finanzas_api_local;Password=...'
export Jwt__SigningKey='un-secreto-aleatorio-de-al-menos-32-bytes'
export Archivos__SigningKey='otro-secreto-aleatorio-de-al-menos-32-bytes'
export Seguridad__TokenPushKey='un-tercer-secreto-aleatorio-de-al-menos-32-bytes'
dotnet run --project src/FinanzasInteligentes.Api
```

Para el Worker del piloto, Google Play y Firebase usan Application Default
Credentials de una cuenta de servicio, configurada fuera del repositorio:

```bash
export GOOGLE_APPLICATION_CREDENTIALS='/ruta/segura/cuenta-servicio.json'
export GooglePlay__Habilitado='true'
export GooglePlay__PackageName='com.tesis.finanzasinteligentes'
export GooglePlay__Productos__premium-mensual='premium_monthly'
export GooglePlay__Productos__premium-anual='premium_yearly'
export Firebase__Habilitado='true'
export Firebase__ProjectId='id-del-proyecto-firebase'
```

La cuenta necesita permisos mínimos para Android Publisher y FCM HTTP v1.
`Seguridad__TokenPushKey` cifra los tokens FCM en PostgreSQL y debe conservarse
estable entre API y Worker.

Aplicar migraciones con una identidad separada:

```bash
export FINANZAS_MIGRACIONES_POSTGRESQL='Host=localhost;Port=5432;Database=finanzas;Username=finanzas_migrador;Password=...'
dotnet ef database update --project src/FinanzasInteligentes.Infraestructura
```

Endpoints operativos:

- `GET /salud/vivo`
- `GET /salud/listo`
- `GET /openapi/v1.yaml`

## Docker Compose

```bash
cd deploy
cp env.example .env
# Reemplazar todos los valores de ejemplo.
docker compose up --build
```

El servicio `migrator` termina antes de iniciar API y Worker. API y Worker usan roles sin permisos DDL.

La configuración de HTTPS detrás de Nginx, encabezados reenviados y claves persistentes/cifradas de ASP.NET Core se explica en [`HTTPS_PROXY_Y_DATA_PROTECTION.md`](../docs/backend/HTTPS_PROXY_Y_DATA_PROTECTION.md).

El inventario de secretos, su inicialización local y las reglas de rotación se encuentran en [`SECRETOS_Y_CONFIGURACION.md`](../docs/backend/SECRETOS_Y_CONFIGURACION.md).

Este Compose es el ambiente local. El piloto sustituye PostgreSQL local por Supabase,
usa Amazon S3, Textract y SES en `us-east-1`, y despliega API/Worker en AWS Lightsail
North Virginia detrás de Nginx y Cloudflare, con inicio supervisado por systemd. La
definición ejecutable está en `deploy/compose.production.yaml`.

## Decisiones de seguridad

- La clave JWT y las contraseñas de infraestructura sólo se reciben por configuración externa.
- Las contraseñas de usuario se guardan mediante `PasswordHasher`.
- Los refresh tokens se generan con entropía criptográfica y sólo se persiste SHA-256.
- Los códigos OTP y tokens de recuperación se persisten mediante HMAC-SHA-256 v1.
- Una sesión revocada deja de autorizar peticiones aunque su JWT no haya expirado.
- Las consultas filtran siempre por el usuario autenticado.
- Los cambios financieros y el outbox comparten una transacción.
