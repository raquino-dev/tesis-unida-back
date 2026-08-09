# Seguridad de correo del piloto

## Incidente y causa raíz

El evento `suscripcion.activada` enviaba primero el correo y luego el push. Si el
push fallaba, el evento seguía `pendiente`; el worker lo volvía a ejecutar cada
cinco segundos y SES recibía el mismo correo una y otra vez. El diseño tampoco
aplicaba espera entre reintentos ni un máximo de intentos.

## Barreras obligatorias

1. **Freno externo de SES.** El estado de cuenta `SendingEnabled` debe ser
   `false` durante una incidencia. Detiene todo envío saliente, aun si la
   aplicación presenta un defecto.
2. **Interruptor interno.** `Correo__Habilitado=false` en
   `compose.production.yaml` evita que el worker invoque SES. Se mantiene
   independiente del freno de SES.
3. **Reserva por evento y canal.** La tabla `infra.outbox_entregas` posee una
   restricción única `(evento_outbox_id, canal)`. La reserva se confirma antes
   de invocar SES. Por seguridad se prefiere perder una notificación ambigua a
   cobrar o enviar un duplicado.
4. **Límite por destinatario.** Para las pruebas del piloto, el worker bloquea
   y marca fallido el correo número 31 al mismo destinatario dentro de una hora.
   No existe un límite global: los usuarios no compiten entre sí. El valor se
   puede ajustar con
   `CORREO_MAXIMO_ENVIOS_POR_DESTINATARIO_HORA` del entorno de despliegue.
5. **Reintentos finitos.** Un evento falla como máximo cinco veces y usa
   esperas de 1 min, 5 min, 15 min, 1 h y 6 h. Luego queda en `fallido` con
   `ultimo_error`; nunca se reintenta indefinidamente.
6. **Permiso de push.** La migración M0020 concede al rol de worker acceso de
   solo lectura a `seguridad.dispositivos`, eliminando la falla que inició el
   reintento de la suscripción.

## Recuperación controlada

No reactivar SES hasta que M0020 esté desplegada y se verifique que el worker
no tiene eventos `pendiente` asociados al incidente.

El archivo de despliegue también debe conservar `CORREO_HABILITADO=false`.
Solo después de validar la migración y una única prueba se cambia a `true` y se
recrea el worker.

```bash
# Estado de SES (us-east-1)
aws sesv2 get-account --region us-east-1 --query SendingEnabled --output text

# Mantenerlo detenido
aws sesv2 put-account-sending-attributes --no-sending-enabled --region us-east-1

# Reactivar solo después de la verificación
aws sesv2 put-account-sending-attributes --sending-enabled --region us-east-1
```

Antes de reactivar, revisar:

```sql
select estado, tipo, intentos, ultimo_error, creado_en
from infra.outbox_eventos
where estado in ('pendiente', 'fallido')
order by creado_en desc;

select canal, estado, reservada_en, enviada_en
from infra.outbox_entregas
order by reservada_en desc;
```

La reactivación debe hacerse primero con una única prueba funcional de OTP y
observando que aparece una sola fila `correo` en `outbox_entregas`.
