# Sincronización offline incremental

## Contrato

`GET /api/v1/sincronizacion?desde={cursor}&limite={1..500}` devuelve únicamente cambios privados del usuario autenticado:

```json
{
  "cambios": [
    {
      "secuencia": 101,
      "tipoEntidad": "movimiento",
      "entidadId": "019...",
      "operacion": "actualizado",
      "version": 2,
      "ocurridoEn": "2026-08-16T01:00:00Z"
    }
  ],
  "siguienteCursor": 101,
  "hayMas": false
}
```

`operacion` vale `actualizado` o `eliminado`. La segunda opción es el tombstone que permite retirar una fila de otro dispositivo. La secuencia es global y monótona, pero las filas siempre se filtran por `UsuarioId` antes de salir de la API. `siguienteCursor` avanza únicamente hasta la última fila efectivamente entregada, para no saltar páginas ni cambios intercalados de otros usuarios.

## Registro de cambios

`FinanzasDbContext.SaveChangesAsync` inspecciona altas y modificaciones de:

- cuenta;
- categoría privada;
- movimiento privado;
- presupuesto;
- meta de ahorro privada;
- movimiento recurrente;
- tarjeta de crédito representada por alias.

El cambio se inserta en `sincronizacion.cambios` dentro de la misma transacción de `SaveChanges`, evitando publicar un cursor para una escritura que no se confirmó. La migración versionada es `M0022_SincronizacionOffline`.

Las altas aceptan opcionalmente el UUID definitivo producido por el cliente. Si el mismo usuario repite un alta con ese UUID, los handlers devuelven la entidad existente. Esto hace recuperables respuestas perdidas sin crear duplicados.

## Despliegue y diagnóstico

El migrador debe ejecutarse antes de arrancar API y worker. Validaciones posteriores:

```bash
curl -fsS "https://api.rodrigoaquino.com/api/v1/sincronizacion?desde=0&limite=10" \
  -H "Authorization: Bearer TOKEN"
```

En PostgreSQL:

```sql
select secuencia, usuario_id, tipo_entidad, entidad_id, operacion, version, ocurrido_en
from sincronizacion.cambios
order by secuencia desc
limit 20;
```

No se registran operaciones de Billing, OCR, push ni colaboración familiar en esta cola; esos módulos continúan requiriendo conexión.
