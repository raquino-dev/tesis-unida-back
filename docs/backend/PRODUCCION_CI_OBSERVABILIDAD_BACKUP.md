# Producción, CI/CD, observabilidad y recuperación

## Estado

El pipeline CI compila, ejecuta las pruebas y construye las tres imágenes. El despliegue
productivo se describe mediante `backend/deploy/compose.production.yaml`; las credenciales
no forman parte del repositorio.

## Preparación

1. Copiar `env.production.example` a `.env.production` y completar los valores fuera de Git.
2. Colocar `data-protection.pfx` y `google-play-service-account.json` en
   `backend/deploy/secrets/`.
3. Verificar el dominio/remitente en Amazon SES y solicitar la salida del sandbox.
4. Crear la cuenta de servicio de Google, habilitar Android Publisher API y otorgarle en
   Play Console los permisos mínimos de pedidos y suscripciones.
   Crear además un tópico Pub/Sub para Real-time Developer Notifications, una suscripción
   push autenticada con OIDC y usar como audiencia
   `https://SU-DOMINIO/api/v1/webhooks/google-play/rtdn`.
5. Crear roles separados de Supabase para migrador, API y Worker.

## Administrador inicial

Después de registrar normalmente la primera cuenta, el migrador debe promoverla una única
vez desde SQL Editor de Supabase:

```sql
UPDATE identidad.usuarios
SET rol = 'administrador', actualizado_en = now(), version = version + 1
WHERE correo = 'administrador@example.com' AND anonimizado_en IS NULL;
```

Reemplace el correo, compruebe que se actualizó exactamente una fila y vuelva a iniciar
sesión para obtener un JWT con el claim `role=administrador`. Desde ese momento los cambios
de estado y rol se realizan mediante `/api/v1/administracion/usuarios`.

## Despliegue

```powershell
docker compose --env-file .env.production -f compose.production.yaml config
docker compose --env-file .env.production -f compose.production.yaml build
docker compose --env-file .env.production -f compose.production.yaml run --rm migrator
docker compose --env-file .env.production -f compose.production.yaml up -d api worker
docker compose --env-file .env.production -f compose.production.yaml ps
```

La API solo publica `127.0.0.1:8080`; Nginx o el proxy TLS es el único punto público.

## Observabilidad mínima

- logs estructurados a stdout y captura por la plataforma;
- `X-Correlation-Id` propagado en respuestas y eventos;
- `/salud/vivo` para liveness y `/salud/listo` para readiness PostgreSQL;
- alertas externas por caída, tasa de HTTP 5xx, latencia p95 y crecimiento del outbox;
- no registrar contraseñas, tokens, OTP, comprobantes ni cuerpos HTTP.

Antes del piloto debe conectarse un colector de logs/métricas y conservar capturas de
latencia, errores, disponibilidad y reinicios.

## Supabase: backup y restauración

Supabase realiza backups diarios gestionados en planes Pro/Team/Enterprise y permite PITR
como complemento. Esto cubre PostgreSQL, no los objetos almacenados fuera de la base.

Procedimiento trimestral de simulacro:

1. confirmar en Dashboard la fecha del último punto recuperable;
2. crear un proyecto aislado de recuperación;
3. restaurar el backup o duplicar el proyecto;
4. reaplicar contraseñas de roles personalizados;
5. ejecutar migraciones y pruebas de humo;
6. reconciliar cantidades y totales de usuarios, cuentas, movimientos y suscripciones;
7. restaurar por separado los objetos de almacenamiento;
8. registrar tiempos reales, RPO, RTO, responsable y resultado en la trazabilidad RNF.

Una copia gestionada que nunca fue restaurada no constituye evidencia de recuperación.

## Suscripciones Google Play

- `POST /api/v1/suscripciones`: registra una compra inicial verificada.
- `PUT /api/v1/suscripcion/plan`: aplica upgrade/downgrade después de que Flutter complete
  el flujo de reemplazo en Google Play.
- `GET /api/v1/suscripcion/transacciones`: devuelve compras, cambios, cancelaciones,
  restauraciones y sincronizaciones RTDN.
- `POST /api/v1/webhooks/google-play/rtdn`: recibe Pub/Sub, valida firma OIDC, audiencia,
  cuenta de servicio, paquete, token, producto, estado y vencimiento.

El Worker genera avisos deduplicados siete, tres y un día antes, el día del vencimiento y
después del vencimiento. SES respeta la preferencia de correo del usuario.
