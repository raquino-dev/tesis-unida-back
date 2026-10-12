# Catálogo de tablas de Finanza

> Generado automáticamente desde `FinanzasDbContextModelSnapshot`. No editar manualmente.

- **Tablas:** 50
- **Columnas:** 526
- **Relaciones:** 68
- **Índices:** 142

## Esquema `analitica`

### `analitica.alertas`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `archivada` | `boolean` | No |  |
| `archivada_en` | `timestamp with time zone` | Sí |  |
| `clave_deduplicacion` | `character varying(160)` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `datos_utilizados` | `character varying(500)` | No |  |
| `impacto` | `character varying(500)` | No |  |
| `leida` | `boolean` | No |  |
| `leida_en` | `timestamp with time zone` | Sí |  |
| `mensaje` | `character varying(500)` | No |  |
| `nivel` | `character varying(20)` | No |  |
| `que_ocurrio` | `character varying(500)` | No |  |
| `recomendacion` | `character varying(500)` | No |  |
| `tipo` | `character varying(50)` | No |  |
| `titulo` | `character varying(160)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

## Esquema `auditoria`

### `auditoria.eventos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `accion` | `character varying(100)` | No |  |
| `correlation_id` | `character varying(100)` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `datos_anteriores` | `jsonb` | Sí |  |
| `datos_nuevos` | `jsonb` | No |  |
| `grupo_familiar_id` | `uuid` | Sí |  |
| `ocurrido_en` | `timestamp with time zone` | No |  |
| `recurso_tipo` | `character varying(80)` | No |  |
| `recurso_id` | `uuid` | No |  |
| `actor_usuario_id` | `uuid` | Sí |  |

## Esquema `documentos`

### `documentos.archivos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `ambito` | `character varying(20)` | No |  |
| `clave_objeto` | `character varying(300)` | No | UK |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `grupo_familiar_id` | `uuid` | Sí |  |
| `hash_idempotencia` | `character varying(64)` | No |  |
| `mime` | `character varying(100)` | No |  |
| `nombre_original` | `character varying(255)` | No |  |
| `sha256` | `character varying(64)` | No |  |
| `tamano_bytes` | `bigint` | No |  |
| `tipo` | `character varying(20)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `documentos.exportaciones`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `ambito` | `character varying(20)` | No |  |
| `cantidad_movimientos` | `bigint` | No |  |
| `categoria_id` | `uuid` | Sí |  |
| `clave_objeto` | `character varying(300)` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_id` | `uuid` | Sí |  |
| `desde` | `date` | No |  |
| `documento` | `character varying(20)` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `expira_en` | `timestamp with time zone` | Sí |  |
| `finalizado_en` | `timestamp with time zone` | Sí |  |
| `formato` | `character varying(10)` | No |  |
| `grupo_familiar_id` | `uuid` | Sí |  |
| `hash_solicitud` | `character varying(64)` | No |  |
| `hasta` | `date` | No |  |
| `tipo_movimiento` | `character varying(10)` | Sí |  |
| `total_gastos` | `bigint` | No |  |
| `total_ingresos` | `bigint` | No |  |
| `total_transferido` | `bigint` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `documentos.procesamientos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `confianza` | `double precision` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `resultado` | `jsonb` | No |  |
| `archivo_id` | `uuid` | No |  |
| `estado` | `character varying(20)` | No |  |
| `completado_en` | `timestamp with time zone` | Sí |  |
| `hash_idempotencia` | `character varying(64)` | No |  |
| `iniciado_en` | `timestamp with time zone` | Sí |  |
| `proveedor` | `character varying(50)` | No |  |
| `tipo` | `character varying(20)` | No |  |
| `version` | `bigint` | No |  |
| `version_modelo` | `character varying(50)` | No |  |

## Esquema `familias`

### `familias.cajas_compartidas`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `grupo_familiar_id` | `uuid` | No | UK |
| `saldo` | `bigint` | No |  |
| `version` | `bigint` | No |  |

### `familias.categorias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `color` | `character varying(7)` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `grupo_familiar_id` | `uuid` | No |  |
| `icono` | `character varying(50)` | No |  |
| `nombre` | `character varying(100)` | No |  |
| `tipo` | `character varying(20)` | No |  |
| `version` | `bigint` | No |  |

### `familias.cuentas_compartidas`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `compartida_por_usuario_id` | `uuid` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_id` | `uuid` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `grupo_familiar_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `familias.eliminaciones`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `completado_en` | `timestamp with time zone` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `error_codigo` | `character varying(80)` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `grupo_familiar_id` | `uuid` | No | UK |
| `version` | `bigint` | No |  |

### `familias.grupos_familiares`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `nombre` | `character varying(120)` | No |  |
| `version` | `bigint` | No |  |

### `familias.integrantes`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `grupo_familiar_id` | `uuid` | No |  |
| `rol` | `character varying(20)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `familias.invitaciones`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `correo` | `character varying(320)` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `estado` | `character varying(20)` | No |  |
| `expira_en` | `timestamp with time zone` | No |  |
| `grupo_familiar_id` | `uuid` | No |  |
| `hash_codigo` | `character varying(64)` | No |  |
| `hash_token` | `character varying(64)` | No | UK |
| `rol` | `character varying(20)` | No |  |
| `usuario_destino_id` | `uuid` | Sí |  |
| `version` | `bigint` | No |  |

### `familias.movimientos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_id` | `uuid` | No |  |
| `descripcion` | `character varying(300)` | No |  |
| `estado` | `character varying(20)` | No |  |
| `fecha` | `date` | No |  |
| `grupo_familiar_id` | `uuid` | No |  |
| `monto` | `bigint` | No |  |
| `tipo` | `character varying(10)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `familias.operaciones_caja`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_privada_id` | `uuid` | No |  |
| `descripcion` | `character varying(300)` | No |  |
| `grupo_familiar_id` | `uuid` | No |  |
| `monto` | `bigint` | No |  |
| `saldo_anterior` | `bigint` | No |  |
| `saldo_posterior` | `bigint` | No |  |
| `tipo` | `character varying(10)` | No |  |
| `usuario_id` | `uuid` | No |  |

### `familias.presupuestos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `grupo_familiar_id` | `uuid` | No |  |
| `monto` | `bigint` | No |  |
| `nombre` | `character varying(120)` | No |  |
| `periodo` | `character varying(20)` | No |  |
| `version` | `bigint` | No |  |

## Esquema `finanzas`

### `finanzas.aportes_meta`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `aportado_por` | `uuid` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_origen_id` | `uuid` | No |  |
| `descripcion` | `character varying(300)` | No |  |
| `fecha` | `date` | No |  |
| `hash_idempotencia` | `character varying(64)` | No |  |
| `meta_id` | `uuid` | No |  |
| `monto` | `bigint` | No |  |

### `finanzas.categorias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `color` | `character varying(7)` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `es_predeterminada` | `boolean` | No |  |
| `icono` | `character varying(50)` | Sí |  |
| `nombre` | `character varying(100)` | No |  |
| `tipo` | `character varying(20)` | No |  |
| `usuario_id` | `uuid` | Sí |  |
| `version` | `bigint` | No |  |

### `finanzas.cuentas`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `color` | `character varying(7)` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `icono` | `character varying(50)` | Sí |  |
| `incluida_en_total` | `boolean` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `nombre` | `character varying(120)` | No |  |
| `saldo_actual` | `bigint` | No |  |
| `saldo_inicial` | `bigint` | No |  |
| `tipo` | `character varying(40)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `finanzas.metas_ahorro`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `ambito` | `character varying(20)` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `creado_por` | `uuid` | No |  |
| `cuenta_id` | `uuid` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `fecha_objetivo` | `date` | No |  |
| `grupo_familiar_id` | `uuid` | Sí |  |
| `moneda` | `character varying(3)` | No |  |
| `monto_objetivo` | `bigint` | No |  |
| `nombre` | `character varying(120)` | No |  |
| `usuario_id` | `uuid` | Sí |  |
| `version` | `bigint` | No |  |

### `finanzas.movimientos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `anulado_en` | `timestamp with time zone` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_id` | `uuid` | No |  |
| `descripcion` | `character varying(300)` | No |  |
| `documento_id` | `uuid` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `fecha` | `date` | No |  |
| `hora` | `time without time zone` | Sí |  |
| `moneda` | `character varying(3)` | No |  |
| `monto` | `bigint` | No |  |
| `motivo_anulacion` | `character varying(300)` | Sí |  |
| `operacion_tarjeta` | `character varying(20)` | Sí |  |
| `origen` | `character varying(30)` | No |  |
| `periodo_recurrencia` | `date` | Sí |  |
| `recurrencia_id` | `uuid` | Sí |  |
| `tarjeta_credito_id` | `uuid` | Sí |  |
| `tipo` | `character varying(10)` | No |  |
| `transferencia_id` | `uuid` | Sí |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `finanzas.movimientos_categorias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `movimiento_id` | `uuid` | No | PK |
| `categoria_id` | `uuid` | No | PK |

### `finanzas.presupuesto_categorias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `presupuesto_id` | `uuid` | No | PK |
| `categoria_id` | `uuid` | No | PK |

### `finanzas.presupuestos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `monto` | `bigint` | No |  |
| `nombre` | `character varying(120)` | No |  |
| `periodo` | `character varying(20)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `finanzas.recurrencia_categorias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `recurrencia_id` | `uuid` | No | PK |
| `categoria_id` | `uuid` | No | PK |

### `finanzas.recurrencias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `cantidad_ocurrencias` | `bigint` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_id` | `uuid` | No |  |
| `descripcion` | `character varying(300)` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `fecha_fin` | `date` | Sí |  |
| `fecha_inicio` | `date` | No |  |
| `frecuencia` | `character varying(20)` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `monto` | `bigint` | No |  |
| `ocurrencias_completadas` | `bigint` | No |  |
| `proxima_ejecucion` | `date` | No |  |
| `tipo` | `character varying(10)` | No |  |
| `ultima_ejecucion_en` | `timestamp with time zone` | Sí |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `finanzas.tarjetas_credito`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `alias` | `character varying(120)` | No |  |
| `color` | `character varying(7)` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_pago_id` | `uuid` | No |  |
| `dia_cierre` | `bigint` | No |  |
| `dia_vencimiento` | `bigint` | No |  |
| `eliminado_en` | `timestamp with time zone` | Sí |  |
| `limite_credito` | `bigint` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `saldo_utilizado` | `bigint` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

### `finanzas.transferencias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `anulada_en` | `timestamp with time zone` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuenta_destino_id` | `uuid` | No |  |
| `cuenta_origen_id` | `uuid` | No |  |
| `descripcion` | `character varying(300)` | No |  |
| `estado` | `character varying(20)` | No |  |
| `fecha` | `date` | No |  |
| `hash_idempotencia` | `character varying(64)` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `monto` | `bigint` | No |  |
| `movimiento_egreso_id` | `uuid` | Sí |  |
| `movimiento_ingreso_id` | `uuid` | Sí |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

## Esquema `identidad`

### `identidad.consentimientos_privacidad`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `aceptado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `finalidad` | `character varying(120)` | No |  |
| `politica_id` | `uuid` | No |  |
| `revocado_en` | `timestamp with time zone` | Sí |  |
| `usuario_id` | `uuid` | No |  |
| `version_politica` | `character varying(30)` | No |  |

### `identidad.desafios_otp`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `canal` | `character varying(20)` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `destino` | `character varying(320)` | No |  |
| `expira_en` | `timestamp with time zone` | No |  |
| `hash_codigo` | `character varying(64)` | No |  |
| `intentos_restantes` | `integer` | No |  |
| `motivo` | `character varying(50)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `verificado_en` | `timestamp with time zone` | Sí |  |
| `version` | `bigint` | No |  |

### `identidad.eliminaciones_perfil`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `completado_en` | `timestamp with time zone` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `error_codigo` | `character varying(80)` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `usuario_id` | `uuid` | No | UK |
| `version` | `bigint` | No |  |

### `identidad.politicas_privacidad`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `activa` | `boolean` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `titulo` | `character varying(160)` | No |  |
| `url_documento` | `character varying(500)` | No |  |
| `version_politica` | `character varying(30)` | No | UK |
| `vigente_desde` | `timestamp with time zone` | No |  |

### `identidad.preferencias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `usuario_id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `notificaciones_correo` | `boolean` | No |  |
| `notificaciones_push` | `boolean` | No |  |
| `resumen_semanal` | `boolean` | No |  |
| `tema` | `character varying(20)` | No |  |
| `version` | `bigint` | No |  |

### `identidad.recuperaciones_contrasena`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `consumido_en` | `timestamp with time zone` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `expira_en` | `timestamp with time zone` | No |  |
| `hash_token` | `character varying(64)` | No |  |
| `intentos_restantes` | `integer` | No |  |
| `usuario_id` | `uuid` | No |  |

### `identidad.sesiones`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `expira_en` | `timestamp with time zone` | No |  |
| `familia_token` | `uuid` | No |  |
| `hash_refresh_token` | `character varying(64)` | No | UK |
| `identificador_dispositivo` | `character varying(200)` | No |  |
| `nombre_dispositivo` | `character varying(120)` | No |  |
| `plataforma_dispositivo` | `character varying(30)` | No |  |
| `revocado_en` | `timestamp with time zone` | Sí |  |
| `usado_en` | `timestamp with time zone` | Sí |  |
| `usuario_id` | `uuid` | No |  |

### `identidad.usuarios`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `alias` | `citext` | No | UK |
| `anonimizado_en` | `timestamp with time zone` | Sí |  |
| `correo` | `citext` | No | UK |
| `creado_en` | `timestamp with time zone` | No |  |
| `estado` | `character varying(30)` | No |  |
| `hash_contrasena` | `text` | No |  |
| `idioma` | `character varying(10)` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `nombre` | `character varying(120)` | No |  |
| `rol` | `character varying(30)` | No |  |
| `ubicacion` | `character varying(160)` | No |  |
| `version` | `bigint` | No |  |
| `zona_horaria` | `character varying(80)` | No |  |

### `identidad.verificaciones_otp`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `consumido_en` | `timestamp with time zone` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `desafio_id` | `uuid` | No | UK |
| `expira_en` | `timestamp with time zone` | No |  |
| `motivo` | `character varying(50)` | No |  |
| `usuario_id` | `uuid` | No |  |

## Esquema `infra`

### `infra.idempotencias`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `clave` | `character varying(100)` | No |  |
| `codigo_respuesta` | `smallint` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `cuerpo_respuesta` | `jsonb` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `expira_en` | `timestamp with time zone` | No |  |
| `hash_solicitud` | `character varying(64)` | No |  |
| `metodo` | `character varying(10)` | No |  |
| `ruta` | `character varying(300)` | No |  |
| `usuario_id` | `uuid` | Sí |  |

### `infra.outbox_entregas`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `canal` | `character varying(30)` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `destinatario_hash` | `character varying(64)` | No |  |
| `enviada_en` | `timestamp with time zone` | Sí |  |
| `estado` | `character varying(20)` | No |  |
| `evento_outbox_id` | `uuid` | No |  |
| `reservada_en` | `timestamp with time zone` | No |  |

### `infra.outbox_eventos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `agregado_id` | `uuid` | No |  |
| `agregado_tipo` | `text` | No |  |
| `correlation_id` | `text` | Sí |  |
| `ocurrido_en` | `timestamp with time zone` | No |  |
| `disponible_en` | `timestamp with time zone` | No |  |
| `estado` | `text` | No |  |
| `intentos` | `integer` | No |  |
| `payload` | `jsonb` | No |  |
| `procesado_en` | `timestamp with time zone` | Sí |  |
| `tipo` | `text` | No |  |
| `ultimo_error` | `character varying(1000)` | Sí |  |

## Esquema `piloto`

### `piloto.instrumentos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `activo` | `boolean` | No |  |
| `codigo` | `character varying(40)` | No | UK |
| `creado_en` | `timestamp with time zone` | No |  |
| `descripcion` | `character varying(1000)` | No |  |
| `titulo` | `character varying(160)` | No |  |
| `version_instrumento` | `character varying(30)` | No |  |

### `piloto.preguntas`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `instrumento_id` | `uuid` | No |  |
| `maximo` | `integer` | Sí |  |
| `minimo` | `integer` | Sí |  |
| `orden` | `integer` | No |  |
| `requerida` | `boolean` | No |  |
| `texto` | `character varying(1000)` | No |  |
| `tipo` | `character varying(20)` | No |  |

### `piloto.respuestas`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `instrumento_id` | `uuid` | No |  |
| `respondido_en` | `timestamp with time zone` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version_instrumento` | `character varying(30)` | No |  |

### `piloto.respuestas_detalle`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `pregunta_id` | `uuid` | No |  |
| `respuesta_id` | `uuid` | No |  |
| `valor_escala` | `integer` | Sí |  |
| `valor_texto` | `character varying(2000)` | Sí |  |

## Esquema `seguridad`

### `seguridad.dispositivos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `confiable` | `boolean` | No |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `identificador_instalacion` | `character varying(200)` | No |  |
| `nombre` | `character varying(120)` | No |  |
| `plataforma` | `character varying(20)` | No |  |
| `revocado_en` | `timestamp with time zone` | Sí |  |
| `push_token_cifrado` | `character varying(6000)` | No |  |
| `ultimo_acceso_en` | `timestamp with time zone` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |
| `version_app` | `character varying(50)` | No |  |
| `version_so` | `character varying(50)` | No |  |
| `zona_horaria` | `character varying(80)` | No |  |

### `seguridad.eventos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `descripcion` | `character varying(500)` | No |  |
| `dispositivo` | `character varying(120)` | No |  |
| `dispositivo_id` | `uuid` | Sí |  |
| `exitoso` | `boolean` | No |  |
| `ocurrido_en` | `timestamp with time zone` | No |  |
| `origen_aproximado` | `character varying(120)` | No |  |
| `tipo` | `character varying(80)` | No |  |
| `usuario_id` | `uuid` | No |  |

## Esquema `sincronizacion`

### `sincronizacion.cambios`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `secuencia` | `bigint` | No |  |
| `Secuencia` | `bigint` | No | PK |
| `entidad_id` | `uuid` | No |  |
| `ocurrido_en` | `timestamp with time zone` | No |  |
| `operacion` | `character varying(20)` | No |  |
| `tipo_entidad` | `character varying(40)` | No |  |
| `usuario_id` | `uuid` | No |  |
| `version` | `bigint` | No |  |

## Esquema `suscripciones`

### `suscripciones.avisos`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `generado_en` | `timestamp with time zone` | No |  |
| `periodo_fin_en` | `timestamp with time zone` | No |  |
| `suscripcion_id` | `uuid` | No |  |
| `tipo` | `character varying(30)` | No |  |
| `usuario_id` | `uuid` | No |  |

### `suscripciones.planes`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `activo` | `boolean` | No |  |
| `codigo` | `character varying(60)` | No | UK |
| `creado_en` | `timestamp with time zone` | No |  |
| `descripcion` | `character varying(500)` | No |  |
| `destacado` | `boolean` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `nombre` | `character varying(120)` | No |  |
| `periodicidad` | `character varying(20)` | No |  |
| `precio` | `bigint` | No |  |

### `suscripciones.suscripciones`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `actualizado_en` | `timestamp with time zone` | No |  |
| `cancelada_en` | `timestamp with time zone` | Sí |  |
| `creado_en` | `timestamp with time zone` | No |  |
| `estado` | `character varying(20)` | No |  |
| `fin_periodo_en` | `timestamp with time zone` | No |  |
| `hash_comprobante` | `character varying(64)` | No |  |
| `iniciada_en` | `timestamp with time zone` | No |  |
| `motivo_cancelacion` | `character varying(300)` | Sí |  |
| `plan_id` | `uuid` | No |  |
| `proveedor` | `character varying(30)` | No |  |
| `reemplazada_por_id` | `uuid` | Sí |  |
| `suscripcion_anterior_id` | `uuid` | Sí |  |
| `usuario_id` | `uuid` | No | UK |
| `version` | `bigint` | No |  |

### `suscripciones.transacciones`

| Campo | Tipo PostgreSQL | Nulo | Claves |
|---|---|:---:|---|
| `id` | `uuid` | No | PK |
| `creado_en` | `timestamp with time zone` | No |  |
| `estado` | `character varying(30)` | No |  |
| `moneda` | `character varying(3)` | No |  |
| `monto` | `bigint` | No |  |
| `ocurrido_en` | `timestamp with time zone` | No |  |
| `proveedor` | `character varying(30)` | No |  |
| `referencia_externa_hash` | `character varying(64)` | No |  |
| `suscripcion_id` | `uuid` | No |  |
| `tipo` | `character varying(30)` | No |  |
| `usuario_id` | `uuid` | No |  |
