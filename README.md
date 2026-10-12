# Finanzas Inteligentes

Backend .NET y documentación de diseño para la aplicación Flutter Finanzas Inteligentes. La aplicación móvil se mantiene en el repositorio hermano `practica-flutter` y admite repositorios mock o conexión HTTP con esta API.

## Manual técnico y de operación

El manual técnico documenta la construcción de **Finanza** y permite que un
tercero pueda instalar, configurar, ejecutar, mantener y auditar el sistema. Su
versión completa se incluye en los anexos del trabajo, mientras que los
repositorios conservan las instrucciones ejecutables y la documentación que
debe mantenerse alineada con cada versión publicada.

- Repositorio de la aplicación móvil:
  [https://github.com/raquino-dev/tesis-unida](https://github.com/raquino-dev/tesis-unida)
- Repositorio del backend:
  [https://github.com/raquino-dev/tesis-unida-back](https://github.com/raquino-dev/tesis-unida-back)

El manual comprende los requisitos previos de hardware y software, con las
versiones empleadas de Flutter, Dart, Android SDK, .NET 10, PostgreSQL y Docker
Compose; el procedimiento de instalación de la aplicación móvil, la API y el
Worker; la configuración mediante parámetros de compilación, variables de
entorno y credenciales externas que no se incorporan al código fuente; la
arquitectura de monolito modular y la responsabilidad de cada componente; la
estructura de PostgreSQL administrada mediante Supabase y las migraciones
versionadas `M0001`–`M0030`; y la sincronización mediante SQLite cifrada,
outbox local, idempotencia y cursores incrementales.

También documenta la seguridad aplicada mediante HTTPS/TLS, JWT, refresh tokens
rotativos, OTP, hashing de contraseñas, autorización, `ETag`/`If-Match`, mínimo
privilegio y auditoría; la integración con Amazon S3, Textract, SES, Firebase
Cloud Messaging y Google Play Billing; el resguardo y restauración de la base
de datos y de los objetos privados; los comandos de compilación, pruebas,
migración y despliegue; y los procedimientos de monitoreo, diagnóstico y
recuperación. Ninguna contraseña, clave privada, token o credencial de proveedor
forma parte del repositorio.

### Contenido disponible en los repositorios

| Contenido | Referencia |
|---|---|
| Instalación y ejecución de API y Worker | [backend/README.md](backend/README.md) |
| Contrato HTTP y OpenAPI | [docs/api/README.md](docs/api/README.md) y [docs/api/openapi.yaml](docs/api/openapi.yaml) |
| Arquitectura y componentes | [docs/backend/ARQUITECTURA_TECNICA.md](docs/backend/ARQUITECTURA_TECNICA.md) |
| Modelo de datos, migraciones y restauración | [docs/backend/MIGRACIONES_POSTGRESQL.md](docs/backend/MIGRACIONES_POSTGRESQL.md) |
| Identidad, sesiones, OTP y seguridad | [docs/backend/IDENTIDAD_Y_SEGURIDAD.md](docs/backend/IDENTIDAD_Y_SEGURIDAD.md) |
| Secretos y configuración externa | [docs/backend/SECRETOS_Y_CONFIGURACION.md](docs/backend/SECRETOS_Y_CONFIGURACION.md) |
| HTTPS, proxy y protección de claves | [docs/backend/HTTPS_PROXY_Y_DATA_PROTECTION.md](docs/backend/HTTPS_PROXY_Y_DATA_PROTECTION.md) |
| Despliegue y operación del piloto | [docs/backend/FACTIBILIDAD_Y_ARQUITECTURA_EJECUTABLE.md](docs/backend/FACTIBILIDAD_Y_ARQUITECTURA_EJECUTABLE.md) |
| Instalación y ejecución de Flutter | [README de la aplicación móvil](https://github.com/raquino-dev/tesis-unida#readme) |

Para reproducir el sistema se deben clonar ambos repositorios, restaurar sus
dependencias, configurar los valores del ambiente fuera de Git, aplicar las
migraciones con el rol migrador antes de iniciar la API y el Worker, comprobar
los endpoints de salud y ejecutar la aplicación móvil contra la URL HTTPS del
ambiente correspondiente. Antes de una migración o despliegue se realiza un
respaldo verificable; toda intervención debe registrar fecha, versión,
responsable, impacto, resultado y acciones de seguimiento.

El [alcance formal y los requisitos](docs/ALCANCE_Y_REQUISITOS.md) definen la prueba piloto, RF-01 a RF-21, RNF-01 a RNF-25, plataforma objetivo y exclusiones.

Las [decisiones aprobadas del piloto](docs/DECISIONES_PILOTO.md) fijan proveedores, alcance obligatorio, exclusiones, privacidad de tarjetas y criterios de éxito.

El [roadmap de completitud](docs/ROADMAP_COMPLETITUD.md) detalla los hitos, dependencias, pruebas y evidencias pendientes para llevar el proyecto al 100 % del alcance.

## Identidad Android prevista

- Application ID: `com.tesis.finanzasinteligentes`
- Nombre visible: `Finanzas Inteligentes`
- Versión móvil congelada para cierre: `0.1.0+9`
- Compatibilidad mínima: Android 10

## Verificación local prevista

```bash
flutter analyze
flutter test
flutter build apk --debug
```

Estos comandos se ejecutan en el repositorio hermano `practica-flutter`.

La configuración de firma y las instrucciones de distribución deberán documentarse en `docs/BETA_DISTRIBUTION.md` cuando se incorpore el proyecto Flutter al repositorio.

El contrato del backend .NET 10 se encuentra en [docs/api/README.md](docs/api/README.md).

La arquitectura, las migraciones PostgreSQL y el desarrollo por etapas se encuentran en [docs/backend/README.md](docs/backend/README.md).

La implementación compilable y su solución se encuentran en [backend/README.md](backend/README.md).
