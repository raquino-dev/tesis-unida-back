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

El primer incremento recomendado en `PLAN_IMPLEMENTACION.md` está iniciado y compilable:

- fundación de API, Worker, persistencia, JWT, correlación, ProblemDetails, rate limiting y health checks;
- registro, inicio de sesión y consulta de perfil;
- altas y consultas de cuentas y categorías;
- altas, consultas y anulaciones de movimientos;
- transacción de movimiento + saldo + outbox;
- concurrencia optimista mediante `ETag`/`If-Match`;
- migración EF inicial para los esquemas `identidad`, `finanzas` e `infra`;
- PostgreSQL, Redis y MinIO preparados para desarrollo local mediante Docker Compose.

Los módulos Familia, Documentos, Analítica, Suscripciones, Notificaciones, OTP/biometría y las operaciones restantes del OpenAPI continúan en las etapas 1–7 del plan. No se exponen rutas ficticias: una ruta se incorpora cuando posee migración, dominio, caso de uso y prueba.

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

Este Compose es el ambiente local. El piloto sustituye PostgreSQL local por Supabase y MinIO por Amazon S3, añade Amazon SES y despliega API/Worker en Hetzner CX33 detrás de Nginx y Cloudflare, con inicio supervisado por systemd. Esas piezas permanecen pendientes según [`TRAZABILIDAD_RNF.md`](../docs/api/TRAZABILIDAD_RNF.md).

## Decisiones de seguridad

- La clave JWT y las contraseñas de infraestructura sólo se reciben por configuración externa.
- Las contraseñas de usuario se guardan mediante `PasswordHasher`.
- Los refresh tokens se generan con entropía criptográfica y sólo se persiste SHA-256.
- Las consultas filtran siempre por el usuario autenticado.
- Los cambios financieros y el outbox comparten una transacción.
