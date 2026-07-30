# Modelo de datos y migraciones PostgreSQL

## 1. Estrategia

Las migraciones son incrementales, inmutables una vez desplegadas y se aplican en el orden de este documento. Cada etapa deja una base utilizable; no es necesario crear desde el inicio tablas de módulos que todavía no se desarrollan.

Se utiliza un único historial EF Core:

```text
infra.__ef_migrations_history
```

En el piloto PostgreSQL es administrado por Supabase, pero continúa tratándose como una base PostgreSQL del backend. Flutter no utiliza acceso directo, claves públicas ni tablas de los esquemas funcionales.

La tabla debe configurarse explícitamente:

```csharp
options.UseNpgsql(connectionString, npgsql =>
    npgsql.MigrationsHistoryTable("__ef_migrations_history", "infra"));
```

Convención de nombres:

```text
M0001_ExtensionesYEsquemas
M0002_InfraestructuraProcesamiento
M0010_IdentidadUsuarios
```

El prefijo numérico reserva espacio entre capacidades y hace visible la dependencia. El nombre real generado por EF puede conservar el timestamp delante, pero debe terminar con el nombre indicado.

## 2. Reglas generales

### 2.1 Columnas comunes

Las entidades mutables con identidad propia incluyen:

| Columna | Tipo | Regla |
|---|---|---|
| `id` | `uuid` | PK; UUID v7 generado por .NET |
| `creado_en` | `timestamptz` | `NOT NULL` |
| `actualizado_en` | `timestamptz` | `NOT NULL` |
| `version` | `bigint` | `NOT NULL DEFAULT 1`; concurrencia/ETag |

En el catálogo detallado, cuando `id` ya aparece enumerado, la expresión “columnas comunes” se refiere sólo a las columnas comunes restantes y no crea un segundo identificador.

Las entidades uno a uno con PK compartida —por ejemplo, preferencias de usuario— no agregan `id`: usan la FK del propietario como PK y sólo comparten timestamps y `version`.

`eliminado_en timestamptz NULL` se añade a categorías personalizadas, cuentas, tarjetas, recurrencias, presupuestos, metas y archivos. Los usuarios usan `anonimizado_en` y estados de eliminación, porque su ciclo de vida no equivale a un borrado lógico común. Los índices de unicidad deben excluir filas eliminadas cuando se permita recrear el recurso.

Las tablas inmutables usan `id`, `creado_en` y, cuando corresponda, `anulado_en`, `anulado_por` y `motivo_anulacion`.

### 2.2 Restricciones transversales

- importes: `CHECK (monto > 0)`;
- saldo/límite configurable: puede ser negativo sólo si la regla del producto lo permite;
- moneda: `CHECK (moneda = 'PYG')` en la primera versión; adoptar otra moneda exige definir y migrar su unidad mínima antes de relajar el check;
- porcentaje/confianza: `CHECK (valor BETWEEN 0 AND 1)`;
- correo: `citext`;
- zona horaria: texto IANA validado en aplicación;
- estados y tipos: `text` con `CHECK`, no enum nativo PostgreSQL, para facilitar evolución;
- los valores físicos compuestos usan `snake_case`; la API los serializa en `kebab-case` según `docs/api/CONTRATOS.md`;
- `jsonb`: sólo para payloads variables, snapshots y respuestas de proveedores;
- FKs de negocio: `RESTRICT` por defecto;
- FKs de tablas dependientes puras: `CASCADE` cuando no destruye historial financiero.

### 2.3 Propiedad privada o familiar

Los recursos que pueden tener ambos ámbitos usan:

```sql
ambito text NOT NULL CHECK (ambito IN ('privado', 'familiar')),
usuario_id uuid NULL,
grupo_familiar_id uuid NULL,
CONSTRAINT ck_propietario_ambito CHECK (
  (ambito = 'privado'  AND usuario_id IS NOT NULL AND grupo_familiar_id IS NULL) OR
  (ambito = 'familiar' AND usuario_id IS NULL AND grupo_familiar_id IS NOT NULL)
)
```

Este patrón aplica a presupuestos, metas, documentos y exportaciones. El usuario que ejecutó una acción se guarda aparte como `creado_por`.

## 3. Catálogo ordenado

| Orden | Migración | Etapa | Propósito |
|---:|---|---:|---|
| 1 | `M0001_ExtensionesYEsquemas` | 0 | Extensiones, esquemas y permisos base |
| 2 | `M0002_InfraestructuraProcesamiento` | 0 | Idempotencia, outbox, consumos y trabajos |
| 3 | `M0010_IdentidadUsuarios` | 1 | Usuarios y preferencias |
| 4 | `M0011_IdentidadSesionesYTokens` | 1 | Sesiones, refresh y recuperación |
| 5 | `M0012_SeguridadDispositivosYOtp` | 1 | Dispositivos, biometría y OTP |
| 6 | `M0013_AuditoriaEventos` | 1 | Eventos de seguridad y auditoría |
| 7 | `M0020_FinanzasCategoriasYCuentas` | 2 | Catálogo y cuentas privadas |
| 8 | `M0021_FinanzasTarjetas` | 2 | Tarjetas de crédito |
| 9 | `M0022_FinanzasMovimientos` | 2 | Registro financiero privado |
| 10 | `M0023_FinanzasTransferenciasYRecurrencias` | 2 | Transferencias y plantillas |
| 11 | `M0030_FamiliaGruposEInvitaciones` | 3 | Grupos, integrantes e invitaciones |
| 12 | `M0031_FamiliaCuentasYMovimientos` | 3 | Finanzas compartidas |
| 13 | `M0032_FamiliaCaja` | 3 | Caja y operaciones inmutables |
| 14 | `M0033_FinanzasPresupuestosYMetas` | 4 | Presupuestos/metas privados y familiares |
| 15 | `M0040_DocumentosArchivosYProcesamientos` | 5 | Metadatos, OCR y relación con movimientos |
| 16 | `M0041_DocumentosSifen` | 5 | Datos normalizados de comprobantes |
| 17 | `M0042_DocumentosExportaciones` | 5 | Exportaciones asíncronas |
| 18 | `M0050_AnaliticaAlertasYScore` | 6 | Alertas y snapshots de score |
| 19 | `M0051_AnaliticaPredicciones` | 6 | Ejecuciones y resultados predictivos |
| 20 | `M0060_SuscripcionesPlanes` | 7 | Planes, capacidades y suscripciones |
| 21 | `M0061_NotificacionesEntregas` | 7 | Preferencias y entregas |
| 22 | `M0070_EndurecimientoIndicesYRetencion` | 8 | Índices finales, retención y anonimización |

## 4. Detalle por migración

### M0001_ExtensionesYEsquemas

Crea:

```sql
CREATE EXTENSION IF NOT EXISTS citext;

CREATE SCHEMA IF NOT EXISTS infra;
CREATE SCHEMA IF NOT EXISTS identidad;
CREATE SCHEMA IF NOT EXISTS seguridad;
CREATE SCHEMA IF NOT EXISTS finanzas;
CREATE SCHEMA IF NOT EXISTS familia;
CREATE SCHEMA IF NOT EXISTS documentos;
CREATE SCHEMA IF NOT EXISTS analitica;
CREATE SCHEMA IF NOT EXISTS suscripciones;
CREATE SCHEMA IF NOT EXISTS notificaciones;
CREATE SCHEMA IF NOT EXISTS auditoria;
```

También crea `infra.__ef_migrations_history` por configuración de EF. UUID v7 se genera en la aplicación; no se depende de una función de extensión para la PK.

Antes de aplicar esta migración, el despliegue provisiona tres roles de grupo sin contraseña: `finanzas_migrador`, `finanzas_api` y `finanzas_worker`. Las identidades concretas de cada ambiente reciben membresía mediante infraestructura como código o administración de la base; las contraseñas nunca se incluyen en una migración.

La migración:

- revoca `CREATE` sobre `public` y todo acceso de roles públicos a los esquemas de la aplicación;
- asigna la propiedad de los esquemas al rol migrador;
- concede a API y Worker sólo `USAGE` de los esquemas que necesitan;
- configura `ALTER DEFAULT PRIVILEGES` del migrador para que las tablas y secuencias futuras no queden públicas;
- exige que cada migración posterior conceda por tabla `SELECT`, `INSERT`, `UPDATE` o `DELETE` únicamente según la matriz de acceso mantenida en despliegue;
- falla en el preflight si `citext` no puede instalarse con la identidad de migración.

La API y el Worker nunca son propietarios de tablas ni reciben `CREATE`, `ALTER`, `DROP` o `TRUNCATE`.

Matriz mínima de privilegios:

| Rol | Escritura permitida |
|---|---|
| `finanzas_api` | tablas de negocio usadas por casos de uso; `infra.idempotencias`; sólo `INSERT` en outbox/trabajos |
| `finanzas_worker` | outbox, consumos y trabajos; tablas de resultado de procesos; escrituras financieras únicamente mediante los casos de uso internos previstos |
| ambos | `SELECT` sólo sobre las tablas necesarias; nunca `DELETE` sobre registros financieros, caja o auditoría |

El job de migración asume `finanzas_migrador` antes de que EF cree o consulte el historial. M0001 asigna explícitamente a ese rol la propiedad de `infra.__ef_migrations_history`, incluso si el proveedor creó la tabla de historial durante el bootstrap.

Reversión: sólo en entorno local vacío. En ambientes compartidos no se eliminan esquemas automáticamente.

### M0002_InfraestructuraProcesamiento

#### `infra.idempotencias`

| Columna | Tipo | Notas |
|---|---|---|
| `id` | `uuid` | PK |
| `usuario_id` | `uuid NULL` | Nulo en rutas públicas |
| `sujeto_publico_hash` | `char(64) NULL` | HMAC de sujeto limitado para rutas públicas |
| `clave` | `varchar(100)` | Valor de `Idempotency-Key` |
| `metodo` | `varchar(10)` | Método HTTP |
| `ruta` | `varchar(300)` | Plantilla normalizada |
| `hash_solicitud` | `char(64)` | SHA-256 del cuerpo canónico |
| `estado` | `text` | `procesando`, `completado`, `fallido` |
| `codigo_respuesta` | `smallint NULL` | HTTP guardado |
| `cuerpo_respuesta` | `jsonb NULL` | Respuesta reproducible |
| `tipo_contenido` | `varchar(100) NULL` | `Content-Type` reproducible |
| `encabezados_respuesta` | `jsonb NULL` | Sólo `Location`, `ETag` y encabezados permitidos |
| `creado_en` / `expira_en` | `timestamptz` | Retención |

La clave siempre se asocia también con `metodo` y `ruta` al comparar el hash de solicitud. Los índices son:

- único `(usuario_id, clave)` donde `usuario_id IS NOT NULL`;
- único `(sujeto_publico_hash, clave)` donde `usuario_id IS NULL`;
- check que exige exactamente uno entre `usuario_id` y `sujeto_publico_hash`.

`sujeto_publico_hash` es un HMAC de una huella limitada y versionada; no se basa solamente en IP ni pretende identificar permanentemente al cliente.

#### `infra.outbox_eventos`

`id`, `tipo`, `version_evento`, `agregado_tipo`, `agregado_id`, `payload jsonb`, `correlation_id`, `ocurrido_en`, `disponible_en`, `procesado_en`, `intentos`, `ultimo_error`, `estado`, `bloqueado_por`, `bloqueado_hasta`.

Índices:

- `(estado, disponible_en, ocurrido_en)` donde `estado = 'pendiente'`;
- `(agregado_tipo, agregado_id)`;
- `correlation_id`.

#### `infra.consumos_evento`

`evento_id`, `consumidor`, `estado`, `intentos`, `disponible_en`, `bloqueado_por`, `bloqueado_hasta`, `procesado_en`, `ultimo_error`, `creado_en`.

PK `(evento_id, consumidor)`. Cada consumidor se registra antes de ejecutar efectos derivados. Si produce un trabajo o cambio interno, el registro del consumo y ese cambio se confirman en la misma transacción. Los efectos externos usan además una clave idempotente aceptada por el proveedor o una tabla de entrega con restricción única.

#### `infra.trabajos`

`id`, `tipo`, `payload jsonb`, `clave_deduplicacion`, `sujeto_tipo`, `sujeto_id`, `solicitado_por NULL`, `estado`, `prioridad`, `intentos`, `max_intentos`, `disponible_en`, `bloqueado_por`, `bloqueado_hasta`, `ultimo_error`, `creado_en`, `completado_en`.

Restricción única parcial sobre `clave_deduplicacion` para trabajos activos. Índices de consumo `(estado, prioridad DESC, disponible_en)` y de consulta autorizada `(sujeto_tipo, sujeto_id, creado_en DESC)`. Los endpoints nunca exponen el `payload` ni permiten consultar un trabajo sin revalidar el propietario o grupo asociado.

### M0010_IdentidadUsuarios

#### `identidad.usuarios`

`id`, `correo citext`, `nombre`, `hash_contrasena`, `moneda char(3)`, `idioma`, `zona_horaria`, `ubicacion`, `correo_verificado_en`, `estado`, `terminos_aceptados_en`, `ultimo_acceso_en`, `anonimizado_en`, columnas comunes.

Reglas:

- `UNIQUE (correo)` donde `anonimizado_en IS NULL`;
- estado: `pendiente`, `activo`, `bloqueado`, `eliminacion_pendiente`, `anonimizado`;
- el hash de contraseña nunca aparece en consultas de lectura.

#### `identidad.preferencias`

PK/FK `usuario_id`; `tema`, `notificaciones_push`, `notificaciones_correo`, `biometria_habilitada`, `configuracion jsonb`, `creado_en`, `actualizado_en`, `version`. No tiene una columna `id` independiente. Relación uno a uno con borrado en cascada durante anonimización controlada.

Seed mínimo: ningún usuario. Las categorías predeterminadas se crean en M0020.

Esta migración agrega la FK opcional `infra.idempotencias.usuario_id -> identidad.usuarios.id`. Las filas públicas continúan con `usuario_id NULL`.

### M0011_IdentidadSesionesYTokens

#### `identidad.sesiones`

`id`, `usuario_id`, `dispositivo_id NULL`, `hash_refresh_token char(64)`, `familia_token`, `emitido_en`, `expira_en`, `usado_en`, `revocado_en`, `motivo_revocacion`, `sesion_rotada_desde_id NULL`, `ip_hash`, `user_agent`, `creado_en`.

Índices: hash de refresh único; `(usuario_id, revocado_en, expira_en)`; sesión anterior única para impedir doble rotación.

#### `identidad.tokens_recuperacion`

`id`, `usuario_id`, `hash_token`, `expira_en`, `consumido_en`, `intentos`, `creado_en`. El token es de un solo uso.

#### `identidad.verificaciones_correo`

Misma estructura de token, con propósito y vencimiento independiente.

La FK opcional de sesión a dispositivo se agrega en M0012, cuando exista la tabla.

### M0012_SeguridadDispositivosYOtp

#### `seguridad.dispositivos`

`id`, `usuario_id`, `identificador_instalacion`, `plataforma`, `modelo`, `version_so`, `version_app`, `push_token_cifrado`, `confianza`, `ultimo_acceso_en`, `revocado_en`, columnas comunes.

Único activo `(usuario_id, identificador_instalacion)`.

#### `seguridad.credenciales_biometricas`

`id`, `usuario_id`, `dispositivo_id`, `proveedor`, `identificador_credencial`, `clave_publica`, `attestation jsonb`, `registrada_en`, `revocada_en`.

No se almacena información biométrica; sólo prueba criptográfica o identificador del proveedor.

#### `seguridad.desafios_biometricos`

`id`, `usuario_id`, `dispositivo_id`, `credencial_id NULL`, `proposito`, `nonce_hash`, `expira_en`, `consumido_en`, `creado_en`, `correlation_id`.

Propósitos iniciales: `registro`, `verificacion`. El nonce aleatorio se entrega una vez, se guarda sólo como hash y queda ligado al usuario, dispositivo, propósito y, cuando existe, credencial. Un índice parcial impide más de un desafío activo equivalente.

#### `seguridad.desafios_otp`

`id`, `usuario_id`, `proposito`, `destino_hmac`, `codigo_hmac`, `version_clave_hmac`, `estado`, `intentos`, `max_intentos`, `expira_en`, `verificado_en`, `creado_en`, `correlation_id`.

Índice parcial por `(usuario_id, proposito, creado_en DESC)` para desafíos activos. El rate limit se apoya además en Redis.

El código y el destino se protegen con HMAC-SHA-256 y una clave versionada almacenada en el secret manager. Esto evita que una copia de la base permita probar fuera de línea todo el espacio reducido de un OTP. La comparación es de tiempo constante y la rotación conserva temporalmente las versiones necesarias para desafíos aún vigentes.

Esta migración agrega la FK `identidad.sesiones.dispositivo_id -> seguridad.dispositivos.id`.

### M0013_AuditoriaEventos

#### `seguridad.eventos`

Eventos visibles al usuario: `usuario_id`, `tipo`, `severidad`, `descripcion`, `ip_hash`, `origen_aproximado NULL`, `dispositivo_id`, `datos jsonb`, `ocurrido_en`, `leido_en`. La IP completa no se conserva ni se devuelve; el origen aproximado se calcula al registrar el evento y contiene sólo ciudad/región/país cuando la política lo permite.

#### `auditoria.eventos`

Registro técnico inmutable: `id`, `actor_usuario_id NULL`, `actor_tipo`, `modulo`, `accion`, `recurso_tipo`, `recurso_id`, `resultado`, `datos_anteriores jsonb`, `datos_nuevos jsonb`, `correlation_id`, `ip_hash`, `ocurrido_en`.

Índices: `(actor_usuario_id, ocurrido_en DESC)`, `(recurso_tipo, recurso_id, ocurrido_en)`, `correlation_id`, y BRIN sobre `ocurrido_en` cuando el volumen lo amerite.

No se incluyen secretos ni archivos. Los snapshots deben filtrar datos sensibles antes de persistirse.

### M0020_FinanzasCategoriasYCuentas

#### `finanzas.categorias`

`id`, `usuario_id NULL`, `nombre`, `icono`, `color`, `tipo`, `es_predeterminada`, `orden`, `eliminado_en`, columnas comunes.

Reglas:

- predeterminada implica `usuario_id IS NULL`;
- personalizada implica `usuario_id IS NOT NULL`;
- tipo: `ingreso`, `gasto`, `ambos`;
- único predeterminado por nombre/tipo;
- único activo `(usuario_id, lower(nombre), tipo)` para personalizadas.
- clave candidata única `(id, es_predeterminada)` para permitir que las relaciones familiares exijan categorías predeterminadas mediante FK compuesta.

Seed versionado con las categorías usadas por la app: alimentación, transporte, vivienda, salud, educación, ocio, servicios, salario, ahorro y otros. Los IDs deben ser constantes para todos los ambientes.

#### `finanzas.cuentas`

`id`, `usuario_id`, `nombre`, `tipo`, `moneda`, `saldo_actual bigint`, `saldo_inicial bigint`, `color`, `icono`, `incluida_en_total`, `eliminado_en`, columnas comunes.

Tipos físicos iniciales: `efectivo`, `cuenta_corriente`, `cuenta_ahorro`, `tarjeta_debito`, `billetera_digital`, `otra`; la API los expone en `kebab-case`. Las tarjetas de crédito permanecen en su tabla propia. Índice `(usuario_id, eliminado_en, orden)`; `orden smallint` se incluye en la tabla.

### M0021_FinanzasTarjetas

#### `finanzas.tarjetas_credito`

`id`, `usuario_id`, `cuenta_pago_id NULL`, `nombre`, `emisor`, `ultimos_cuatro char(4)`, `limite_credito bigint`, `saldo_utilizado bigint`, `dia_cierre smallint`, `dia_vencimiento smallint`, `moneda`, `color`, `eliminado_en`, columnas comunes.

Checks para últimos cuatro dígitos, días 1–31, límite positivo y saldo no negativo. No se guarda PAN, CVV ni fecha completa de tarjeta.

### M0022_FinanzasMovimientos

#### `finanzas.movimientos`

`id`, `usuario_id`, `cuenta_id`, `tipo`, `monto`, `moneda`, `descripcion`, `fecha date`, `hora time NULL`, `estado`, `origen`, `transferencia_id NULL`, `recurrencia_id NULL`, `movimiento_anulado_id NULL`, `creado_por`, `anulado_en`, `motivo_anulacion`, `creado_en`, `version`.

En M0022, `transferencia_id` y `recurrencia_id` se crean como columnas nullable sin FK porque las tablas referenciadas aún no existen. Las FKs e índices se agregan explícitamente en M0023.

Tipos: `ingreso`, `gasto`. Estados: `confirmado`, `anulado`. Orígenes: `manual`, `documento`, `recurrencia`, `transferencia`, `meta`, `caja`.

Índices:

- `(usuario_id, fecha DESC, id DESC)`;
- `(cuenta_id, fecha DESC)`;
- GIN de búsqueda sólo si se adopta `tsvector`; inicialmente B-tree y búsqueda acotada;
- únicos parciales para claves de generación recurrente cuando se añadan en M0023.

#### `finanzas.movimiento_categorias`

PK compuesta `(movimiento_id, categoria_id)`, `proporcion numeric(8,6)`, `monto bigint NULL`. La suma de asignaciones se valida en dominio; una restricción diferida o trigger puede añadirse si se permiten divisiones múltiples.

Regla transaccional de alta:

1. bloquear cuenta;
2. validar propiedad y estado;
3. insertar movimiento/categorías;
4. actualizar `saldo_actual` según tipo;
5. insertar evento outbox;
6. confirmar.

El endpoint DELETE no borra la fila: la marca anulada y revierte el saldo una sola vez.

### M0023_FinanzasTransferenciasYRecurrencias

#### `finanzas.transferencias`

`id`, `usuario_id`, `cuenta_origen_id`, `cuenta_destino_id`, `monto`, `moneda`, `descripcion`, `fecha`, `movimiento_egreso_id NULL`, `movimiento_ingreso_id NULL`, `estado`, `anulada_en`, `creado_en`, `version`.

Checks: cuentas distintas, monto positivo y movimientos distintos. Ambos movimientos se crean en una sola transacción.

Orden transaccional de creación:

1. bloquear las cuentas en orden determinista por UUID;
2. insertar la transferencia en estado `procesando`, todavía sin IDs de movimientos;
3. insertar los dos movimientos con `transferencia_id`;
4. completar ambos IDs en la transferencia y cambiarla a `confirmada`;
5. verificar antes del commit que una transferencia confirmada tenga exactamente dos movimientos, uno de gasto y otro de ingreso, mediante trigger de restricción diferible;
6. actualizar saldos y escribir outbox.

M0023 agrega las FKs desde movimientos a transferencias/recurrencias y desde transferencias a sus movimientos. Las FKs circulares son `DEFERRABLE INITIALLY DEFERRED`; las columnas de movimientos permanecen nullable para movimientos que no provienen de esas operaciones.

#### `finanzas.recurrencias`

`id`, `usuario_id`, `cuenta_id`, `tipo`, `monto`, `moneda`, `descripcion`, `frecuencia`, `intervalo`, `dia_mes NULL`, `dia_semana NULL`, `fecha_inicio`, `fecha_fin NULL`, `proxima_ejecucion`, `zona_horaria`, `estado`, `ultima_ejecucion_en`, `eliminado_en`, columnas comunes.

Frecuencias: `diaria`, `semanal`, `quincenal`, `mensual`, `anual`. `quincenal` significa cada 15 días civiles desde `fecha_inicio`, no “dos veces por mes”. Estado: `activa`, `pausada`, `finalizada`.

#### `finanzas.recurrencia_categorias`

PK `(recurrencia_id, categoria_id)`.

Se agrega a movimientos `periodo_recurrencia date NULL` y un índice único parcial `(recurrencia_id, periodo_recurrencia)` para impedir generación doble.

### M0030_FamiliaGruposEInvitaciones

#### `familia.grupos`

`id`, `nombre`, `moneda`, `zona_horaria`, `estado`, `creado_por`, columnas comunes.

#### `familia.integrantes`

`id`, `grupo_id`, `usuario_id`, `rol`, `estado`, `ingresado_en`, `salio_en`, columnas comunes.

Único activo `(grupo_id, usuario_id)`. Rol: `propietario`, `administrador`, `integrante`. Un índice único parcial garantiza un propietario activo por grupo.

Para la regla actual de un grupo por usuario se agrega único parcial `(usuario_id)` donde `estado = 'activo'`. Esa restricción se puede retirar en una migración futura sin rediseñar las demás tablas.

#### `familia.invitaciones`

`id`, `grupo_id`, `correo_destino citext NULL`, `usuario_destino_id uuid NULL`, `rol`, `hash_token`, `codigo_hmac`, `version_clave_hmac`, `estado`, `invitada_por`, `expira_en`, `aceptada_por NULL`, `aceptada_en NULL`, `creado_en`, `version`.

Un check exige exactamente uno entre correo y usuario de destino. Índice único de token; únicos parciales `(grupo_id, correo_destino)` y `(grupo_id, usuario_destino_id)` para invitaciones pendientes. Estados: `pendiente`, `aceptada`, `revocada`, `vencida`.

### M0031_FamiliaCuentasYMovimientos

#### `familia.cuentas_compartidas`

`grupo_id`, `cuenta_id`, `compartida_por`, `compartida_en`, `version`. PK `(grupo_id, cuenta_id)`.

`cuenta_id` referencia `finanzas.cuentas`: compartir una cuenta no la duplica ni transfiere su propiedad. La API autoriza al propietario antes de compartirla y el grupo obtiene acceso sólo mientras exista esta relación.

#### `familia.categorias`

`id`, `grupo_id`, `nombre`, `icono`, `color`, `tipo`, `orden`, `eliminado_en`, columnas comunes. Único activo `(grupo_id, lower(nombre), tipo)` y clave candidata `(id, grupo_id)` para FKs de mismo grupo.

Las categorías personalizadas de un usuario nunca se reutilizan directamente en movimientos familiares. El grupo puede usar categorías predeterminadas globales o categorías propias, evitando exponer información privada y conservando el historial si el creador abandona el grupo.

#### `familia.movimientos`

Equivalente al movimiento privado, con `grupo_id`, `cuenta_id`, `creado_por` y `responsable_usuario_id NULL`. La FK compuesta `(grupo_id, cuenta_id) -> familia.cuentas_compartidas(grupo_id, cuenta_id)` garantiza que la cuenta esté compartida con ese grupo. Declara además la clave candidata `(id, grupo_id)`. Incluye estado/anulación y actualiza `finanzas.cuentas.saldo_actual` de forma atómica.

#### Categorías de movimientos familiares

Se usan dos tablas sin referencias ambiguas:

- `familia.movimiento_categorias_predeterminadas`, PK `(movimiento_id, categoria_id)`. Incluye el discriminador constante `es_predeterminada = true` y una FK a la clave candidata `(id, es_predeterminada)` de `finanzas.categorias`;
- `familia.movimiento_categorias_propias`, PK `(movimiento_id, categoria_id)`. Incluye `grupo_id` y dos FKs compuestas que exigen que movimiento y categoría pertenezcan al mismo grupo.

Índices principales: `(grupo_id, fecha DESC, id DESC)`, `(cuenta_id, fecha DESC)`, `(responsable_usuario_id, fecha DESC)`.

### M0032_FamiliaCaja

#### `familia.cajas`

`id`, `grupo_id`, `moneda`, `saldo_actual`, `estado`, columnas comunes. Único `(grupo_id)`.

#### `familia.operaciones_caja`

Registro inmutable: `id`, `caja_id`, `tipo`, `monto`, `descripcion`, `actor_usuario_id`, `cuenta_id NULL`, `movimiento_privado_id NULL`, `movimiento_familiar_id NULL`, `verificacion_otp_id NULL`, `operacion_compensada_id NULL`, `saldo_anterior`, `saldo_resultante`, `creado_en`.

Tipos: `aporte`, `retiro`, `ajuste_positivo`, `ajuste_negativo`, `compensacion`. Se exige una sola cuenta origen/destino cuando aplique. Los retiros bloquean la caja y validan saldo antes de insertar.

Índices: `(caja_id, creado_en DESC, id DESC)`, `actor_usuario_id`, y únicos sobre movimientos asociados no nulos.

### M0033_FinanzasPresupuestosYMetas

#### `finanzas.presupuestos`

`id`, `ambito`, `usuario_id NULL`, `grupo_familiar_id NULL`, `nombre`, `monto_limite`, `moneda`, `periodicidad`, `fecha_inicio`, `fecha_fin NULL`, `alerta_porcentaje`, `estado`, `creado_por`, `eliminado_en`, columnas comunes.

Estados persistidos: `activo`, `pausado`, `finalizado`. La salud `saludable`, `en_riesgo` o `excedido` es derivada del gasto confirmado y no se persiste como estado editable.

#### `finanzas.presupuesto_categorias`

PK `(presupuesto_id, categoria_id)`.

No se persiste “gastado”: se calcula desde movimientos confirmados dentro de las fechas y categorías. Puede cachearse usando el ID y la versión del presupuesto.

#### `finanzas.metas_ahorro`

`id`, ámbito/propietario, `nombre`, `monto_objetivo`, `moneda`, `fecha_objetivo NULL`, `cuenta_id NULL`, `estado`, `creado_por`, `eliminado_en`, columnas comunes.

La propiedad de la cuenta se garantiza sin checks entre tablas:

- `finanzas.cuentas` declara único `(id, usuario_id)`;
- la FK `(cuenta_id, usuario_id) -> finanzas.cuentas(id, usuario_id)` valida metas privadas;
- la FK `(grupo_familiar_id, cuenta_id) -> familia.cuentas_compartidas(grupo_id, cuenta_id)` valida metas familiares;
- por la semántica `MATCH SIMPLE`, la FK que no corresponde al ámbito se omite al contener un componente nulo;
- el check local de ámbito continúa exigiendo exactamente un propietario.

#### `finanzas.aportes_meta`

`id`, `meta_id`, `monto`, `fecha`, `descripcion`, `aportado_por`, `movimiento_privado_id NULL`, `movimiento_familiar_id NULL`, `aporte_compensado_id NULL`, `creado_en`.

Los aportes son inmutables. La coincidencia entre aporte, meta y movimiento se valida con FKs compuestas donde sea posible y con un trigger de restricción diferible para la regla polimórfica restante; no se usan `CHECK` que consulten otras tablas. Índices por propietario/estado y `(meta_id, fecha DESC)`.

### M0040_DocumentosArchivosYProcesamientos

#### `documentos.archivos`

`id`, ámbito/propietario, `creado_por`, `bucket`, `clave_objeto`, `nombre_original`, `mime`, `tamano_bytes`, `sha256 char(64)`, `estado`, `cargado_en`, `eliminado_en`, columnas comunes.

Único `(bucket, clave_objeto)`; índice por SHA-256 y propietario. Nunca se persiste URL prefirmada.

#### `documentos.procesamientos`

`id`, `archivo_id`, `tipo`, `estado`, `proveedor`, `version_modelo`, `confianza`, `resultado jsonb`, `error_codigo`, `error_detalle_controlado`, `iniciado_en`, `completado_en`, `creado_en`, `version`.

Tipos: `ocr`, `sifen`. Único activo `(archivo_id, tipo)`. Los reintentos actualizan la misma ejecución y aumentan su contador; una nueva ejecución deliberada requiere otra `version_modelo` o un identificador explícito de reprocesamiento. El resultado bruto se retiene sólo según la política documental.

#### Relaciones con movimientos

Tablas `documentos.movimientos_privados` y `documentos.movimientos_familiares`, ambas con PK compuesta `(archivo_id, movimiento_id)`. Así se evita que Documentos altere la tabla propiedad de Finanzas y se permite adjuntar varios comprobantes.

### M0041_DocumentosSifen

#### `documentos.comprobantes_sifen`

`id`, `archivo_id`, `procesamiento_id`, `cdc`, `ruc_emisor`, `razon_social_emisor`, `numero_documento`, `fecha_emision`, `moneda`, `total`, `total_iva`, `datos_adicionales jsonb`, `creado_en`.

`cdc` único cuando no es nulo. Checks de importes no negativos y moneda. Los ítems normalizados se guardan en `documentos.comprobante_items`: `id`, `comprobante_id`, `codigo`, `descripcion`, `cantidad numeric(18,6)`, `precio_unitario bigint`, `total bigint`, `tasa_iva numeric(8,6)`.

La validación XML debe deshabilitar entidades externas y limitar tamaño/profundidad para evitar XXE y agotamiento de recursos.

### M0042_DocumentosExportaciones

#### `documentos.exportaciones`

`id`, ámbito/propietario, `solicitada_por`, `formato`, `filtros jsonb`, `hash_solicitud char(64)`, `clave_deduplicacion`, `estado`, `archivo_id NULL`, `expira_en`, `error_codigo`, columnas comunes.

Formatos iniciales: `csv`, `xlsx`, `pdf`. Único `(solicitada_por, clave_deduplicacion)` durante la retención e índices `(solicitada_por, creado_en DESC)` y `(estado, creado_en)` para limpieza. Reusar una clave con filtros/formato diferentes produce conflicto. El Worker genera el archivo y enlaza `archivo_id`.

### M0050_AnaliticaAlertasYScore

#### `analitica.alertas`

`id`, `usuario_id`, `grupo_familiar_id NULL`, `tipo`, `severidad`, `titulo`, `mensaje`, `recurso_tipo`, `recurso_id`, `datos jsonb`, `estado`, `detectada_en`, `leida_en`, `descartada_en`, `version`.

#### `analitica.scores`

`id`, `usuario_id`, `valor smallint`, `version_algoritmo`, `factores jsonb`, `periodo_desde`, `periodo_hasta`, `calculado_en`.

Check de rango `0–100`, conforme al contrato actual. Índice `(usuario_id, calculado_en DESC)`; los snapshots anteriores se conservan para explicar evolución.

### M0051_AnaliticaPredicciones

#### `analitica.ejecuciones_prediccion`

`id`, `usuario_id`, `tipo`, `estado`, `version_modelo`, `periodo_desde`, `periodo_hasta`, `datos_entrada_version`, `iniciada_en`, `completada_en`, `error_codigo`, `creado_en`.

#### `analitica.predicciones`

`id`, `ejecucion_id`, `categoria_id NULL`, `fecha_objetivo`, `monto_estimado`, `intervalo_inferior`, `intervalo_superior`, `confianza`, `explicacion jsonb`.

Índices de unicidad:

- `(ejecucion_id, categoria_id, fecha_objetivo)` donde `categoria_id IS NOT NULL`;
- `(ejecucion_id, fecha_objetivo)` donde `categoria_id IS NULL`.

Así la proyección agregada sin categoría tampoco puede duplicarse. Las proyecciones son snapshots reproducibles y no modifican movimientos.

### M0060_SuscripcionesPlanes

#### `suscripciones.planes`

`id`, `codigo`, `nombre`, `descripcion`, `precio bigint`, `moneda`, `periodicidad`, `estado`, columnas comunes. `codigo` único.

#### `suscripciones.capacidades`

`id`, `codigo`, `descripcion`; `codigo` único.

#### `suscripciones.plan_capacidades`

PK `(plan_id, capacidad_id)`, `limite bigint NULL`, `configuracion jsonb`.

#### `suscripciones.suscripciones`

`id`, `usuario_id`, `plan_id`, `proveedor`, `referencia_proveedor`, `estado`, `inicio_en`, `fin_periodo_en`, `cancelada_en`, `version`, timestamps.

Restricciones:

- estados: `pendiente`, `activa`, `en_gracia`, `vencida`, `cancelada`;
- único `(proveedor, referencia_proveedor)` cuando la referencia no sea nula;
- como máximo una suscripción en estado `activa` o `en_gracia` por usuario mediante índice único parcial `(usuario_id)`;
- check de intervalo `fin_periodo_en > inicio_en`;
- toda transición conserva historial; una renovación actualiza el período con control de versión o crea un evento de período asociado, según el proveedor elegido.

#### `suscripciones.comprobantes_proveedor`

`id`, `suscripcion_id`, `proveedor`, `id_evento_proveedor`, `hash_recibo`, `payload_verificado jsonb`, `recibido_en`. Son únicos `(proveedor, id_evento_proveedor)` y, cuando exista, `(proveedor, hash_recibo)` para idempotencia.

Durante el piloto sin pagos, el proveedor puede ser `interno` y las capacidades se administran mediante seed/configuración.

### M0061_NotificacionesEntregas

#### `notificaciones.preferencias_entrega`

PK/FK `usuario_id`, reglas granulares por tipo, `horario_silencioso_desde/hasta`, `zona_horaria`, `creado_en`, `actualizado_en`, `version`. No tiene `id` independiente. Los flags generales continúan en `identidad.preferencias`; esta tabla sólo agrega reglas propias de entrega.

#### `notificaciones.entregas`

`id`, `usuario_id`, `canal`, `tipo`, `plantilla`, `destino_hash`, `datos jsonb`, `clave_deduplicacion`, `estado`, `intentos`, `proveedor_id`, `disponible_en`, `enviada_en`, `ultimo_error`, `creado_en`.

Único por clave de deduplicación; índice `(estado, disponible_en)`. El destino real se obtiene de Identidad/Seguridad sólo al enviar y no se repite innecesariamente en logs.

### M0070_EndurecimientoIndicesYRetencion

Se aplica después de medir consultas con datos representativos:

- índices faltantes detectados con `EXPLAIN (ANALYZE, BUFFERS)`;
- BRIN de tablas cronológicas voluminosas;
- políticas de limpieza de idempotencia, trabajos, tokens, archivos y entregas;
- funciones o procedimiento de anonimización;
- auditoría automatizada de propietarios, membresías y privilegios para detectar cualquier deriva respecto de M0001;
- validación `NOT VALID` seguida de `VALIDATE CONSTRAINT` para restricciones incorporadas sobre tablas grandes;
- límites de longitud y checks que hayan quedado pendientes durante el piloto.

No se crean índices “por si acaso”: cada índice aumenta costo de escritura y almacenamiento.

## 5. Migraciones de datos y seeds

Los seeds de referencia se versionan dentro de migraciones:

- categorías predeterminadas con UUID constantes;
- plan gratuito y capacidades iniciales;
- plantillas de notificación si se almacenan en base;
- versiones iniciales de algoritmo, sólo si son catálogos.

No se insertan usuarios demo ni movimientos mock en migraciones. Los datos de demostración pertenecen a un proyecto de seed local separado y nunca se ejecutan en producción.

Una modificación masiva sigue el patrón **expandir–migrar–contraer**:

1. agregar columna nueva nullable;
2. desplegar código que escribe ambos formatos;
3. completar datos por lotes;
4. cambiar lecturas al nuevo formato;
5. volver `NOT NULL` y retirar la columna vieja en otro despliegue.

## 6. Ejecución con EF Core

Comandos de referencia desde `backend/`:

```bash
dotnet ef migrations add M0001_ExtensionesYEsquemas \
  --project src/FinanzasInteligentes.Infraestructura \
  --startup-project src/FinanzasInteligentes.Api

dotnet ef migrations script 0 M0001_ExtensionesYEsquemas \
  --idempotent \
  --output artifacts/sql/M0001.sql

dotnet ef database update \
  --project src/FinanzasInteligentes.Infraestructura \
  --startup-project src/FinanzasInteligentes.Api
```

Debe existir un único ensamblado propietario de migraciones.

Flujo de despliegue:

1. compilar y ejecutar pruebas;
2. generar SQL idempotente;
3. revisar SQL y plan de bloqueo;
4. crear backup o snapshot;
5. ejecutar migración con un job único;
6. verificar historial, health check y consultas críticas;
7. desplegar API/Worker compatibles.

No se usa `Database.Migrate()` al iniciar cada réplica.

## 7. Política de reversión

- En desarrollo local se permite `database update <migracion_anterior>`.
- En ambientes con datos se prefiere una migración correctiva hacia adelante.
- Antes de una migración destructiva se realiza backup y prueba de restauración.
- Una reversión de binarios sólo es segura si la migración es compatible con ambas versiones.
- El método `Down` no debe prometer recuperación de datos eliminados.

## 8. Verificación automatizada

Cada pull request de backend debe:

1. crear un PostgreSQL vacío;
2. aplicar todas las migraciones;
3. validar constraints, índices y seeds esperados;
4. ejecutar pruebas de integración;
5. restaurar un dump de la versión previa;
6. aplicar sólo las migraciones nuevas;
7. repetir pruebas de humo.

Casos críticos: alta idempotente de movimiento, transferencia concurrente, doble retiro, doble aceptación de invitación, rotación doble de refresh token, anulación repetida y deduplicación de recurrencias.
