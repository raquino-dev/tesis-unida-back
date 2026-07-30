# Pruebas negativas, concurrencia, carga y seguridad

## Automatizadas

- política de contraseñas: longitud, complejidad, datos personales y contraseñas comunes;
- estados y roles de usuario: valores inválidos, anonimización y reactivación;
- contrato OpenAPI y ausencia de secretos;
- concurrencia de entidades mediante versiones/ETag;
- restricciones únicas e idempotencia mediante PostgreSQL.

## Carga

Instalar k6 y ejecutar contra un ambiente de prueba con datos no productivos:

```powershell
$env:BASE_URL = "https://api-pruebas.example.com"
$env:ACCESS_TOKEN = "TOKEN_DE_USUARIO_DE_PRUEBA"
k6 run backend/tests/load/k6-smoke.js
```

El script usa diez usuarios virtuales, exige menos de 1 % de errores y p95 inferior a
3 segundos. El resultado debe guardarse como artefacto del piloto.

## Matriz negativa pendiente de ambiente

1. Usuario A intenta leer/modificar recursos de usuario B: 404 o 403.
2. Integrante intenta operaciones exclusivas de propietario/administrador: 403.
3. Usuario inactivo intenta login y refresh: 401.
4. Token expirado/revocado/reutilizado: 401.
5. OTP incorrecto, expirado, agotado o usado: 422/403 sin filtrar el código.
6. Dos solicitudes con la misma clave idempotente: un solo efecto.
7. Dos actualizaciones con el mismo ETag: una tiene éxito y otra obtiene 412.
8. Compra Google Play inexistente, expirada o de otro producto: rechazo sin activar plan.
9. Archivo con extensión permitida pero contenido inválido: rechazo.
10. Respuestas 500 no contienen stack trace, SQL, secretos o datos personales.

Para declarar estos puntos verificados deben ejecutarse contra PostgreSQL/Supabase real y
adjuntar salida, fecha, versión y correlation IDs.
