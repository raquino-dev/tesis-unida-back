# Secretos y configuración externa

La API y el Worker no contienen secretos ni conexiones de infraestructura en `appsettings.json`. Los archivos JSON versionados conservan únicamente opciones no sensibles, por ejemplo emisor JWT, duración de tokens, rutas locales y niveles de log.

## 1. Inventario

| Variable de despliegue | Clave .NET resultante | Consumidor | Propósito |
|---|---|---|---|
| `POSTGRES_PASSWORD` | `FINANZAS_MIGRACIONES_POSTGRESQL` construida por Compose | Migrator | Aplicar migraciones con permisos DDL |
| `FINANZAS_API_PASSWORD` | `ConnectionStrings__PostgreSql` construida por Compose | API | Acceso DML de la API |
| `FINANZAS_WORKER_PASSWORD` | `ConnectionStrings__PostgreSql` construida por Compose | Worker | Acceso DML del Worker |
| `JWT_SIGNING_KEY` | `Jwt__SigningKey` | API | Firmar y validar JWT |
| `ARCHIVOS_SIGNING_KEY` | `Archivos__SigningKey` | API y Worker | Firmar URLs temporales |
| `TOKEN_PUSH_ENCRYPTION_KEY` | `Seguridad__TokenPushKey` | API y Worker | Cifrar tokens push con AES-GCM |
| `MINIO_ROOT_PASSWORD` | `MINIO_ROOT_PASSWORD` | MinIO local | Administrar almacenamiento local |
| `DATA_PROTECTION_CERTIFICATE_PASSWORD` | `DataProtection__CertificatePassword` | API de producción | Abrir el PFX que cifra el anillo de claves |

Las tres claves criptográficas de aplicación deben ser diferentes. La API compara los valores y no arranca si se reutiliza una misma clave.

## 2. Por qué hay claves separadas

Cada secreto tiene un ámbito:

```text
JWT_SIGNING_KEY
  └── identidad y autenticación

ARCHIVOS_SIGNING_KEY
  └── URLs temporales de documentos

TOKEN_PUSH_ENCRYPTION_KEY
  └── cifrado de tokens de notificaciones
```

Antes, archivos y tokens push podían usar `Jwt:SigningKey` como respaldo. Esos fallbacks fueron eliminados. Si una clave se filtra, puede rotarse sin reemplazar automáticamente las otras dos.

## 3. Inicializar `.env` local

Desde la raíz del repositorio:

```powershell
.\backend\deploy\Initialize-LocalEnvironment.ps1
```

El script funciona en Windows PowerShell antiguo porque utiliza `RandomNumberGenerator.Create()` y luego el método de instancia `GetBytes`. No depende del método estático que produjo el error en versiones anteriores de PowerShell.

El script:

1. abre `backend/deploy/.env` si existe;
2. conserva todas las variables existentes;
3. detecta nombres duplicados;
4. genera con entropía criptográfica únicamente las variables faltantes;
5. no muestra los valores en pantalla;
6. crea o actualiza `.env`, que está excluido de Git y del contexto Docker.

Puede comprobar solamente los nombres, sin imprimir valores:

```powershell
Get-Content .\backend\deploy\.env |
  Where-Object { $_ -match '^[A-Za-z_][A-Za-z0-9_]*=' } |
  ForEach-Object { ($_ -split '=', 2)[0] }
```

No ejecute `Get-Content` sin ese filtrado durante una grabación, captura o sesión compartida.

## 4. Cómo llegan a .NET

ASP.NET Core traduce doble guion bajo a separador de configuración:

```text
Jwt__SigningKey
        ↓
Jwt:SigningKey
```

Compose lee las variables cortas de `.env` y construye las variables que recibe cada contenedor:

```yaml
environment:
  Jwt__SigningKey: ${JWT_SIGNING_KEY:?Configure JWT_SIGNING_KEY}
  Archivos__SigningKey: ${ARCHIVOS_SIGNING_KEY:?Configure ARCHIVOS_SIGNING_KEY}
  Seguridad__TokenPushKey: ${TOKEN_PUSH_ENCRYPTION_KEY:?Configure TOKEN_PUSH_ENCRYPTION_KEY}
```

El operador `:?` hace que `docker compose` se detenga antes de crear contenedores si falta una variable.

## 5. Ejecución local

```powershell
cd backend\deploy
docker compose config --quiet
docker compose up -d --build
docker compose ps
docker compose logs --tail 100 api worker
```

`docker compose config --quiet` valida sin imprimir la configuración interpolada. No utilice `docker compose config` sin `--quiet` en una consola compartida, porque puede mostrar conexiones y claves.

## 6. Ejecución directa sin Docker

Si se ejecuta la API mediante `dotnet run`, deben existir variables de proceso:

```powershell
$env:ConnectionStrings__PostgreSql = 'Host=localhost;Port=5432;Database=finanzas;Username=finanzas_api_local;Password=...'
$env:Jwt__SigningKey = '<secreto JWT>'
$env:Archivos__SigningKey = '<secreto archivos>'
$env:Seguridad__TokenPushKey = '<secreto token push>'

dotnet run --project backend\src\FinanzasInteligentes.Api
```

Estas asignaciones sólo viven en el proceso de PowerShell actual. No deben copiarse a documentación, commits ni capturas.

Para migraciones directas:

```powershell
$env:FINANZAS_MIGRACIONES_POSTGRESQL = 'Host=localhost;Port=5432;Database=finanzas;Username=finanzas_migrador;Password=...'

dotnet ef database update `
  --project backend\src\FinanzasInteligentes.Infraestructura
```

El factory de EF Core ya no usa una conexión predeterminada cuando falta esa variable.

## 7. Validaciones fail-fast

La aplicación se detiene antes de atender tráfico cuando:

- falta una conexión PostgreSQL;
- falta cualquiera de las tres claves;
- una clave tiene menos de 32 bytes;
- dos claves criptográficas tienen el mismo valor;
- la URL pública de archivos no es absoluta;
- producción no tiene certificado y contraseña de Data Protection.

Los mensajes indican el nombre de configuración, nunca el valor recibido.

## 8. Producción

El futuro `compose.production.yaml` no contendrá valores reales. Referenciará un archivo protegido en el VPS o secretos montados:

```text
/opt/finanzas/.env
/opt/finanzas/secrets/data-protection.pfx
```

El `.env` del VPS debe pertenecer al usuario de despliegue y tener permisos `600`. GitHub Actions sólo necesitará acceso para ordenar el despliegue; no es necesario imprimir ni regenerar secretos en cada release.

La rotación se hará por propósito:

- JWT requiere una estrategia de transición si deben seguir aceptándose tokens ya emitidos;
- cambiar la firma de archivos invalida URLs temporales existentes;
- cambiar la clave push exige volver a registrar o recifrar los tokens almacenados;
- Data Protection debe conservar claves/certificados anteriores mientras existan datos protegidos con ellos.
