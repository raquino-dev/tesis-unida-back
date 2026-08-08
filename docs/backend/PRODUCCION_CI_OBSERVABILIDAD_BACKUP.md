# Producción, CI/CD, observabilidad y recuperación

## Estado

El pipeline CI compila, ejecuta las pruebas y construye las tres imágenes. El despliegue
productivo se describe mediante `backend/deploy/compose.production.yaml`; las credenciales
no forman parte del repositorio.

## Preparación

1. Contratar Supabase Pro y Lightsail en North Virginia (`us-east-1`).
2. Copiar `env.production.example` a `.env.production`, completar los valores fuera de
   Git y aplicar modo `600`.
3. Colocar `data-protection.pfx`, `google-service-account.json`, `supabase-ca.crt`,
   `tls-fullchain.pem` y `tls-privkey.pem` en
   `backend/deploy/secrets/`.
4. Crear el bucket documental S3 privado con versionado, bloqueo de acceso público,
   cifrado y las políticas IAM mínimas `deploy/aws/api-iam-policy.json` y
   `deploy/aws/worker-iam-policy.json`.
5. Verificar el dominio/remitente en Amazon SES y solicitar la salida del sandbox.
6. Crear la cuenta de servicio de Google, habilitar Android Publisher API y otorgarle en
   Play Console los permisos mínimos de pedidos y suscripciones.
   Crear además un tópico Pub/Sub para Real-time Developer Notifications, una suscripción
   push autenticada con OIDC y usar como audiencia
   `https://SU-DOMINIO/api/v1/webhooks/google-play/rtdn`.
7. Crear roles separados de Supabase para migrador, API y Worker. API/Worker usan el
   session pooler; migrador y backup usan conexión directa.
8. Configurar Cloudflare en Full (strict), proxy sólo para el dominio de API y firewall
   Lightsail con 22 restringido a IP administrativa, 80/443 públicos.

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

```bash
cd backend/deploy
./scripts/validate-config.sh
./scripts/deploy.sh
```

El migrador debe terminar correctamente antes de que API y Worker inicien. Sólo Nginx
publica 80/443; Kestrel no expone puertos del host. Para reinicio automático copie
`deploy/systemd/finanzas-pilot.service` a `/etc/systemd/system/`, recargue systemd y
habilite la unidad.

El workflow `pilot-deploy.yml` prueba el backend, publica tres imágenes inmutables en GHCR
y despliega sólo después de la aprobación del Environment `pilot`. Ese Environment requiere:
`PILOT_HOST`, `PILOT_USER`, `PILOT_SSH_PRIVATE_KEY`, `PILOT_SSH_KNOWN_HOSTS` y
`GHCR_READ_TOKEN`.

Para volver a una versión anterior:

```bash
cd /opt/finanzas/deploy
IMAGE_REGISTRY=ghcr.io/PROPIETARIO ./scripts/rollback.sh SHA_ANTERIOR
```

El rollback de aplicación no revierte migraciones destructivas. Durante el piloto las
migraciones deben ser compatibles hacia atrás; una reversión de datos requiere el
procedimiento de restauración.

## Observabilidad mínima

- logs estructurados a stdout y rotación del driver Docker;
- `X-Correlation-Id` propagado en respuestas y eventos;
- `/salud/vivo` para liveness y `/salud/listo` para readiness PostgreSQL;
- alertas externas por caída, tasa de HTTP 5xx, latencia p95 y crecimiento del outbox;
- no registrar contraseñas, tokens, OTP, comprobantes ni cuerpos HTTP.

Crashlytics registra fallos fatales sólo en builds con `USE_REAL_API=true`. Antes del
piloto debe configurarse un monitor HTTPS externo sobre `/salud/listo` y conservar
capturas de latencia, errores, disponibilidad y reinicios. Nunca se adjuntan cuerpos,
tokens ni documentos financieros a Crashlytics.

## Supabase: backup y restauración

Supabase Pro realiza backups diarios. Como segunda copia, un cron nocturno ejecuta
`scripts/backup-postgres-to-s3.sh`: genera un dump lógico, lo comprime, cifra con
AES-256/PBKDF2 y lo guarda con cifrado S3 en un bucket distinto al documental.

Procedimiento trimestral de simulacro:

1. Seleccionar un dump y crear una base aislada de recuperación.
2. Ejecutar `restore-postgres-from-s3.sh URL CONFIRMAR_RESTAURACION` apuntando únicamente
   a esa base.
3. Reaplicar contraseñas de roles personalizados.
4. Ejecutar migraciones y pruebas de humo.
5. Reconciliar cantidades y totales de usuarios, cuentas, movimientos y suscripciones.
6. Verificar aparte objetos y versionado S3.
7. Registrar RPO, RTO, responsable y resultado en la trazabilidad RNF.

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
