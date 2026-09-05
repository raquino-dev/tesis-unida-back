# Identidad y seguridad

**Estado del código:** integrado entre API y Flutter  
**Pendiente externo:** validación final del enlace de recuperación en un dispositivo Android

## Flujos implementados

- registro e inicio de sesión;
- JWT de 15 minutos y refresh token rotativo;
- comprobación de usuario y sesión activa en cada petición autenticada;
- revocación individual y listado de sesiones;
- detección de reutilización o cambio de dispositivo del refresh token, con
  revocación de su familia;
- recuperación de contraseña sin revelar si el correo existe;
- revocación de todas las sesiones al cambiar o restablecer contraseña;
- OTP de seis dígitos, HMAC-SHA-256 v1, expiración, cinco intentos y consumo único;
- OTP obligatorio para cambio de contraseña, eliminación de perfil y otras
  operaciones sensibles;
- eventos de inicio correcto/fallido, OTP y contraseña;
- auditoría HTTP de operaciones mutables;
- límites específicos para autenticación, OTP y recuperación;
- tokens móviles almacenados mediante `flutter_secure_storage`;
- bloqueo biométrico local al iniciar o reanudar la aplicación;
- centro móvil de eventos y sesiones, con revocación remota.

La biometría no se transmite al backend ni se almacena como dato biométrico. Android
realiza la verificación local y la aplicación sólo conserva la preferencia de bloqueo.

## Correo OTP y recuperación

La API escribe eventos en outbox y el Worker los entrega mediante Amazon SES. Para el
ambiente piloto deben configurarse, como mínimo:

```text
Correo__Habilitado=true
Correo__Region=<region-aws>
Correo__Remitente=<correo-verificado>
Correo__NombreRemitente=Finanzas Inteligentes
Correo__UrlAplicacion=finanzasinteligentes://app
```

La configuración `Correo` debe estar disponible tanto para API como para Worker. El
enlace generado tendrá la forma:

```text
finanzasinteligentes://app/reset-password?recoveryId=<id>&code=<codigo>
```

El mismo correo presenta un código numérico de 6 dígitos como alternativa al enlace.
El código está asociado a un identificador opaco de solicitud, expira después de 30
minutos, se consume una sola vez y admite como máximo cinco intentos. La respuesta a
la solicitud siempre devuelve la misma estructura —incluso para correos inexistentes—
para evitar enumeración de usuarios. La pantalla móvil solicita al sistema el
autocompletado de códigos, ofrece pegado explícito desde el portapapeles y valida la
política de la contraseña mientras el usuario escribe.

No se registran códigos OTP, tokens de recuperación, contraseñas ni refresh tokens en
logs o respuestas. El outbox conserva el código o token sólo hasta entregarlo y
reemplaza inmediatamente el payload sensible por una marca de redacción.

## Límites de solicitudes

| Flujo | Límite |
|---|---:|
| Inicio/renovación de sesión | 10 por 5 minutos |
| Solicitud/verificación OTP | 5 por 5 minutos |
| Solicitud de recuperación | 3 por 15 minutos |
| Intentos de restablecimiento | 5 por 15 minutos, además de 5 por código |

La partición utiliza el usuario autenticado o, para rutas públicas, la dirección de
origen observada por la API.

## Verificación pendiente para el piloto

- solicitar salida del sandbox únicamente si el piloto necesita destinatarios no
  verificados; el dominio y el remitente ya están verificados;
- ejecutar OTP y recuperación con API, Worker y PostgreSQL desplegados;
- comprobar apertura del deep link desde Gmail en Android;
- probar biometría habilitada, cancelada y sin huella/rostro configurado;
- comprobar revocación inmediata de JWT después de cerrar sesión;
- ejecutar dos renovaciones concurrentes y confirmar revocación de la familia;
- adjuntar métricas y capturas anonimizadas a la evidencia del piloto.
