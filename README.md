# Finanzas Inteligentes

Backend .NET 10 y documentación técnica para la aplicación Flutter Finanzas Inteligentes. La aplicación móvil se mantiene en el repositorio hermano `raquino-dev/tesis-unida` y utiliza repositorios API en el AAB distribuido para el piloto; los dobles/mocks se conservan para desarrollo y pruebas controladas.

El [alcance formal y los requisitos](docs/ALCANCE_Y_REQUISITOS.md) definen la prueba piloto, **RF-01 a RF-26**, RNF-01 a RNF-25, plataforma Android y exclusiones.

Las [decisiones consolidadas del piloto](docs/DECISIONES_PILOTO.md) fijan muestra, duración, proveedores, alcance obligatorio, privacidad de tarjetas, suscripciones y criterios de aceptación.

Los [instrumentos del piloto](docs/INSTRUMENTOS_PILOTO.md) documentan el preuso principal, el bloque complementario solicitado por tutoría y el postuso, preservando versiones y respuestas históricas.

## Identidad Android del piloto

- Application ID: `com.tesis.finanzasinteligentes`
- Nombre visible: `Finanzas Inteligentes`
- Compatibilidad mínima: Android 10
- Distribución: Google Play Internal Testing
- Backend: `https://api.rodrigoaquino.com/api/v1`

La versión exacta de entrega se congela mediante commit/tag al cerrar la tesis. Durante el piloto se han distribuido builds `0.1.0+7` y `0.1.0+8`; los cambios entre ellas deben conservarse en la evidencia de versiones.

## Verificación

Backend:

```bash
cd backend
dotnet restore FinanzasInteligentes.sln
dotnet build FinanzasInteligentes.sln -c Release --no-restore
dotnet test FinanzasInteligentes.sln -c Release --no-build
```

Mobile, desde el repositorio hermano:

```bash
flutter pub get
flutter analyze
flutter test
flutter build apk --debug
```

La evidencia automatizada no sustituye las validaciones E2E de Supabase, S3, Textract, SES, FCM, Google Play Billing, backup/restore y sincronización multi-dispositivo.

El contrato del backend se encuentra en [docs/api/README.md](docs/api/README.md). La arquitectura y operación están documentadas en [docs/backend/README.md](docs/backend/README.md).
