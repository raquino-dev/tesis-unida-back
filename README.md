# Finanzas Inteligentes

Documentación de diseño para una aplicación Flutter de gestión de finanzas personales y familiares y su backend .NET. El código Flutter no forma parte todavía de esta copia del repositorio; cuando se incorpore, usará repositorios mock en memoria antes de integrar el backend.

El [alcance formal y los requisitos](docs/ALCANCE_Y_REQUISITOS.md) definen la prueba piloto, RF-01 a RF-21, RNF-01 a RNF-25, plataforma objetivo y exclusiones.

El [roadmap de completitud](docs/ROADMAP_COMPLETITUD.md) detalla los hitos, dependencias, pruebas y evidencias pendientes para llevar el proyecto al 100 % del alcance.

## Identidad Android prevista

- Application ID: `com.tesis.finanzasinteligentes`
- Nombre visible: `Finanzas Inteligentes`
- Versión: `0.1.0+1`
- Compatibilidad mínima: Android 10

## Verificación local prevista

```bash
flutter analyze
flutter test
flutter build apk --debug
```

Estos comandos sólo son verificables después de incorporar `pubspec.yaml`, `lib/`, `test/` y la configuración de plataforma del proyecto Flutter.

La configuración de firma y las instrucciones de distribución deberán documentarse en `docs/BETA_DISTRIBUTION.md` cuando se incorpore el proyecto Flutter al repositorio.

El contrato del backend .NET 10 se encuentra en [docs/api/README.md](docs/api/README.md).

La arquitectura, las migraciones PostgreSQL y el desarrollo por etapas se encuentran en [docs/backend/README.md](docs/backend/README.md).

La implementación compilable y su solución se encuentran en [backend/README.md](backend/README.md).
