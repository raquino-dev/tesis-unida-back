# Backend de Finanzas Inteligentes

Guía técnica para implementar por etapas el contrato definido en [`docs/api`](../api/README.md) con .NET 10 y PostgreSQL.

La implementación inicial se encuentra en [`backend`](../../backend/README.md) e incluye la solución .NET, la migración fundacional, API, Worker y las primeras verticales de identidad y finanzas personales.

## Documentos

- [Alcance formal y requisitos](../ALCANCE_Y_REQUISITOS.md)
- [Arquitectura técnica](ARQUITECTURA_TECNICA.md)
- [Modelo de datos y migraciones PostgreSQL](MIGRACIONES_POSTGRESQL.md)
- [Plan de implementación por etapas](PLAN_IMPLEMENTACION.md)
- [Identidad, OTP, sesiones y biometría](IDENTIDAD_Y_SEGURIDAD.md)
- [Contrato REST](../api/README.md)
- [Trazabilidad no funcional](../api/TRAZABILIDAD_RNF.md)

Las convenciones, endpoints, contratos JSON, trazabilidad y la especificación [OpenAPI 3.1](../api/openapi.yaml) están disponibles en `docs/api`. `openapi.yaml` es el contrato ejecutable y puede regenerarse y validarse con los scripts versionados en `scripts`.

## Decisión principal

La primera versión será un **monolito modular**, no un conjunto de microservicios:

- una API ASP.NET Core .NET 10;
- un Worker .NET 10 desplegable por separado;
- PostgreSQL administrado en Supabase para el piloto, con un esquema por módulo;
- Redis para caché y coordinación efímera;
- Amazon S3 privado para documentos;
- Amazon SES para correo;
- patrón outbox y cola persistente en PostgreSQL al inicio.

Esta forma de despliegue mantiene simples las transacciones financieras y la operación del piloto, pero conserva límites internos que permiten extraer módulos en el futuro.

## Orden de lectura

1. Acordar las decisiones de [arquitectura](ARQUITECTURA_TECNICA.md).
2. Crear la solución y aplicar las [migraciones fundacionales](MIGRACIONES_POSTGRESQL.md).
3. Implementar cada etapa según el [plan](PLAN_IMPLEMENTACION.md).
4. Validar cada endpoint con los contratos y criterios de salida indicados.

## Estado de las decisiones

| Tema | Decisión para la primera versión |
|---|---|
| Estilo | Monolito modular |
| Persistencia | PostgreSQL 16 o superior |
| Hosting PostgreSQL del piloto | Supabase; acceso exclusivo desde backend |
| ORM | EF Core 10 |
| Contextos EF | Un `FinanzasDbContext`, configuraciones separadas por módulo |
| Historial de migración | Único y lineal |
| Consistencia financiera | Transacción fuerte |
| Analítica y notificaciones | Consistencia eventual mediante outbox |
| Acceso móvil a datos | Siempre a través de la API |
| Identificadores | UUID v7 generados por la aplicación |
| Dinero en PYG | `bigint`, sin decimales |
| Archivos | Objetos privados en S3; PostgreSQL conserva sólo metadatos |
| Desarrollo local | PostgreSQL, Redis y MinIO mediante Docker Compose |
| Despliegue piloto | Hetzner CX33, Docker Compose, systemd, Nginx y Cloudflare |
| Eliminación financiera | Anulación o compensación; no borrado físico |
