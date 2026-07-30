# Catálogo completo de endpoints

Base: `/api/v1`. Los nombres indicados en Request/Response corresponden a [CONTRATOS.md](CONTRATOS.md). Paginación, cabeceras, autorización y errores siguen [CONVENCIONES.md](CONVENCIONES.md).

## 1. Usuarios, perfil y sesiones

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `POST` | `/usuarios` | `CrearUsuarioRequest` | `201 UsuarioResponse`; `Location: /usuarios/{id}` | `201`, `400`, `409 correo_duplicado`, `422`, `429` |
| `GET` | `/perfil` | — | `200 UsuarioResponse` | `200`, `401` |
| `PATCH` | `/perfil` | `{nombre?, idioma?, ubicacion?, zonaHoraria?}`; `If-Match` | `200 UsuarioResponse` | `200`, `400`, `401`, `412`, `422` |
| `POST` | `/eliminaciones-perfil` | `{contrasena, verificacionOtpId}`; `Idempotency-Key` | `202 {id, estado, creadoEn, urlEstado}` | `202`, `401`, `403`, `409 propietario_grupo`, `422`, `429` |
| `GET` | `/eliminaciones-perfil/{eliminacionId}` | — | `200 ProcesoAsyncResponse` | `200`, `401`, `404` |
| `POST` | `/sesiones` | `CrearSesionRequest` | `201 SesionResponse` | `201`, `400`, `401 credenciales_invalidas`, `403 cuenta_bloqueada`, `429` |
| `GET` | `/sesiones` | filtros de paginación | `200 Pagina<SesionActivaResponse>` | `200`, `401` |
| `POST` | `/sesiones/renovaciones` | `RenovarSesionRequest` | `201 SesionResponse` con refresh token rotado | `201`, `400`, `401`, `409 token_reutilizado`, `429` |
| `DELETE` | `/sesiones/{sesionId}` | — | `204` | `204`, `401`, `404` |
| `POST` | `/recuperaciones-contrasena` | `RecuperacionContrasenaRequest` | `202` sin revelar si el correo existe | `202`, `400`, `429`, `503` |
| `POST` | `/restablecimientos-contrasena` | `RestablecimientoContrasenaRequest` | `204` | `204`, `400`, `409 token_utilizado`, `410 token_expirado`, `422`, `429` |
| `PUT` | `/perfil/contrasena` | `CambiarContrasenaRequest` | `204`; revoca otras sesiones | `204`, `400`, `401`, `403 otp_requerido`, `422`, `429` |
| `GET` | `/perfil/preferencias` | — | `200 PreferenciasResponse` | `200`, `401` |
| `PUT` | `/perfil/preferencias` | `{tema, idioma, notificacionesPush, resumenSemanal}`; `If-Match` | `200 PreferenciasResponse` | `200`, `400`, `401`, `412`, `422` |

## 2. Seguridad, OTP, biometría y auditoría

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `POST` | `/desafios-otp` | `DesafioOtpRequest`; `Idempotency-Key` | `201 DesafioOtpResponse` | `201`, `400`, `401`, `409`, `429`, `503` |
| `POST` | `/verificaciones-otp` | `VerificacionOtpRequest` | `201 {id, valida, expiraEn}` | `201`, `400`, `401`, `409 desafio_consumido`, `410`, `422 codigo_invalido`, `429` |
| `POST` | `/credenciales-biometricas` | `{desafioId, identificadorDispositivo, clavePublica, atestacion}` | `201 CredencialBiometricaResponse` | `201`, `400`, `401`, `409`, `410`, `422` |
| `GET` | `/credenciales-biometricas` | paginación | `200 Pagina<CredencialBiometricaResponse>` | `200`, `401` |
| `DELETE` | `/credenciales-biometricas/{credencialId}` | `If-Match` | `204` | `204`, `401`, `404`, `412` |
| `POST` | `/desafios-biometricos` | `{proposito, dispositivoId, credencialId?}` | `201 DesafioBiometricoResponse` | `201`, `400`, `401`, `404`, `409`, `429` |
| `POST` | `/verificaciones-biometricas` | `VerificacionBiometricaRequest` | `201 {id, valida, expiraEn}` | `201`, `400`, `401`, `403`, `422`, `429` |
| `GET` | `/eventos-seguridad` | `tipo`, `desde`, `hasta`, paginación | `200 Pagina<EventoSeguridadResponse>` | `200`, `400`, `401` |
| `GET` | `/eventos-auditoria` | filtros por recurso, usuario, grupo y fecha, paginación | `200 Pagina<EventoAuditoriaResponse>` | `200`, `400`, `401`, `403` |

Los eventos de auditoría son creados exclusivamente por el servidor; no existe un `POST` público.

## 3. Cuentas financieras

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/cuentas` | `activas?`, `tipo?`, paginación | `200 Pagina<CuentaResponse>` | `200`, `401` |
| `POST` | `/cuentas` | `CuentaRequest` | `201 CuentaResponse` | `201`, `400`, `401`, `409 nombre_duplicado`, `422` |
| `GET` | `/cuentas/{cuentaId}` | — | `200 CuentaResponse` | `200`, `401`, `404` |
| `PATCH` | `/cuentas/{cuentaId}` | campos modificables de `CuentaRequest`; `If-Match` | `200 CuentaResponse` | `200`, `400`, `401`, `404`, `409`, `412`, `422` |
| `DELETE` | `/cuentas/{cuentaId}` | `If-Match` | `204` | `204`, `401`, `404`, `409 cuenta_en_uso`, `412` |

## 4. Categorías

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/categorias` | `tipo?`, `predefinida?`, paginación | `200 Pagina<CategoriaResponse>` | `200`, `401` |
| `POST` | `/categorias` | `CategoriaRequest` | `201 CategoriaResponse` | `201`, `400`, `401`, `409 nombre_duplicado`, `422` |
| `GET` | `/categorias/{categoriaId}` | — | `200 CategoriaResponse` | `200`, `401`, `404` |
| `PATCH` | `/categorias/{categoriaId}` | campos modificables; `If-Match` | `200 CategoriaResponse` | `200`, `400`, `401`, `403 categoria_predefinida`, `404`, `409`, `412`, `422` |
| `DELETE` | `/categorias/{categoriaId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `409 categoria_en_uso`, `412` |

## 5. Tarjetas de crédito

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/tarjetas-credito` | paginación | `200 Pagina<TarjetaCreditoResponse>` | `200`, `401` |
| `POST` | `/tarjetas-credito` | `TarjetaCreditoRequest` | `201 TarjetaCreditoResponse` | `201`, `400`, `401`, `404 cuenta_no_encontrada`, `409`, `422` |
| `GET` | `/tarjetas-credito/{tarjetaId}` | — | `200 TarjetaCreditoResponse` | `200`, `401`, `404` |
| `PATCH` | `/tarjetas-credito/{tarjetaId}` | campos modificables; `If-Match` | `200 TarjetaCreditoResponse` | `200`, `400`, `401`, `404`, `409`, `412`, `422` |
| `DELETE` | `/tarjetas-credito/{tarjetaId}` | `If-Match` | `204` | `204`, `401`, `404`, `409 tarjeta_en_uso`, `412` |

## 6. Movimientos privados

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/movimientos` | filtros comunes y paginación | `200 Pagina<MovimientoResponse>` | `200`, `400`, `401` |
| `POST` | `/movimientos` | `MovimientoRequest`; `Idempotency-Key` | `201 MovimientoResponse` | `201`, `400`, `401`, `404`, `409 idempotencia/conflicto`, `422` |
| `GET` | `/movimientos/{movimientoId}` | — | `200 MovimientoResponse` | `200`, `401`, `404` |
| `PATCH` | `/movimientos/{movimientoId}` | `{descripcion?, categoriaIds?}`; `If-Match` | `200 MovimientoResponse` | `200`, `400`, `401`, `404`, `409`, `412`, `422` |
| `DELETE` | `/movimientos/{movimientoId}` | `If-Match` | `204` | `204`, `401`, `404`, `409`, `412` |

Monto, tipo, cuenta y fecha son inmutables. Una corrección contable anula el movimiento y crea otro mediante dos operaciones idempotentes relacionadas. Saldos y outbox se actualizan en la transacción principal; presupuestos, score y proyecciones se recalculan con consistencia eventual.

## 7. Documentos, OCR y XML SIFEN

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `POST` | `/documentos-financieros` | `multipart/form-data`; `Idempotency-Key`, `X-Content-SHA256` | `202 DocumentoFinancieroResponse`; `Location` al procesamiento | `202`, `400`, `401`, `403 capacidad_premium`, `409 duplicado`, `413`, `415`, `422`, `503` |
| `GET` | `/documentos-financieros` | `tipo?`, `estado?`, fechas, paginación | `200 Pagina<DocumentoFinancieroResponse>` | `200`, `400`, `401` |
| `GET` | `/documentos-financieros/{documentoId}` | — | `200 DocumentoFinancieroResponse` | `200`, `401`, `404` |
| `DELETE` | `/documentos-financieros/{documentoId}` | `If-Match` | `204`; limpieza S3 asíncrona | `204`, `401`, `404`, `409 documento_en_uso`, `412` |
| `POST` | `/documentos-financieros/{documentoId}/descargas` | — | `201 {url, expiraEn}` | `201`, `401`, `404`, `409 no_disponible`, `429`, `503` |
| `GET` | `/procesamientos-documentales/{procesamientoId}` | — | `200 ProcesamientoDocumentalResponse` | `200`, `401`, `404` |
| `POST` | `/documentos-financieros/{documentoId}/procesamientos-documentales` | `{tipo: ocr/sifen}`; `Idempotency-Key` | `202 ProcesamientoDocumentalResponse` | `202`, `400`, `401`, `403`, `404`, `409 procesamiento_activo`, `422`, `429`, `503` |
| `PATCH` | `/procesamientos-documentales/{procesamientoId}` | correcciones de `datosDetectados`; `If-Match` | `200 ProcesamientoDocumentalResponse` | `200`, `400`, `401`, `404`, `409 estado_incompatible`, `412`, `422` |

Después de revisar/corregir, el cliente crea el movimiento con `POST /movimientos` enviando `documentoId`. No se utiliza un endpoint con verbo “confirmar”.

## 8. Movimientos recurrentes

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/movimientos-recurrentes` | `estado?`, `tipo?`, paginación | `200 Pagina<MovimientoRecurrenteResponse>` | `200`, `400`, `401` |
| `POST` | `/movimientos-recurrentes` | `MovimientoRecurrenteRequest` | `201 MovimientoRecurrenteResponse` | `201`, `400`, `401`, `404`, `409`, `422` |
| `GET` | `/movimientos-recurrentes/{recurrenteId}` | — | `200 MovimientoRecurrenteResponse` | `200`, `401`, `404` |
| `PATCH` | `/movimientos-recurrentes/{recurrenteId}` | campos modificables; `If-Match` | `200 MovimientoRecurrenteResponse` | `200`, `400`, `401`, `404`, `412`, `422` |
| `DELETE` | `/movimientos-recurrentes/{recurrenteId}` | `If-Match` | `204` | `204`, `401`, `404`, `412` |

## 9. Transferencias internas entre cuentas

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/transferencias` | cuentas, fechas y paginación | `200 Pagina<TransferenciaResponse>` | `200`, `400`, `401` |
| `POST` | `/transferencias` | `TransferenciaRequest`; `Idempotency-Key` | `201 TransferenciaResponse` | `201`, `400`, `401`, `404`, `409 misma_cuenta/saldo_insuficiente`, `422` |
| `GET` | `/transferencias/{transferenciaId}` | — | `200 TransferenciaResponse` | `200`, `401`, `404` |
| `DELETE` | `/transferencias/{transferenciaId}` | `If-Match` | `204`; genera anulaciones enlazadas | `204`, `401`, `404`, `409 estado_incompatible`, `412` |

Las transferencias son registros lógicos internos; no representan operaciones bancarias reales.

## 10. Presupuestos privados

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/presupuestos` | `periodo?`, `estado?`, `categoriaId?`, paginación | `200 Pagina<PresupuestoResponse>` | `200`, `400`, `401` |
| `POST` | `/presupuestos` | `PresupuestoRequest` | `201 PresupuestoResponse` | `201`, `400`, `401`, `404`, `409 nombre_duplicado/solapamiento`, `422` |
| `GET` | `/presupuestos/{presupuestoId}` | — | `200 PresupuestoResponse` | `200`, `401`, `404` |
| `PATCH` | `/presupuestos/{presupuestoId}` | campos modificables; `If-Match` | `200 PresupuestoResponse` | `200`, `400`, `401`, `404`, `409`, `412`, `422` |
| `DELETE` | `/presupuestos/{presupuestoId}` | `If-Match` | `204` | `204`, `401`, `404`, `412` |
| `GET` | `/resumen-presupuestario` | `desde?`, `hasta?` | `200 {total, gastado, disponible, progreso, presupuestos}` | `200`, `400`, `401` |

## 11. Metas de ahorro

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/metas-ahorro` | `ambito?`, `grupoFamiliarId?`, paginación | `200 Pagina<MetaAhorroResponse>` | `200`, `400`, `401`, `403` |
| `POST` | `/metas-ahorro` | `MetaAhorroRequest` | `201 MetaAhorroResponse` | `201`, `400`, `401`, `403`, `404`, `409`, `422` |
| `GET` | `/metas-ahorro/{metaId}` | — | `200 MetaAhorroResponse` | `200`, `401`, `403`, `404` |
| `PATCH` | `/metas-ahorro/{metaId}` | nombre, objetivo y fecha; `If-Match` | `200 MetaAhorroResponse` | `200`, `400`, `401`, `403`, `404`, `412`, `422` |
| `DELETE` | `/metas-ahorro/{metaId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `409 saldo_existente`, `412` |
| `GET` | `/metas-ahorro/{metaId}/aportes` | paginación | `200 Pagina<AporteMetaResponse>` | `200`, `401`, `403`, `404` |
| `POST` | `/metas-ahorro/{metaId}/aportes` | `AporteMetaRequest`; `Idempotency-Key` | `201 AporteMetaResponse` | `201`, `400`, `401`, `403`, `404`, `409 saldo_insuficiente`, `422` |

## 12. Grupos familiares y miembros

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/grupos-familiares` | paginación | `200 Pagina<GrupoFamiliarResponse>` | `200`, `401` |
| `POST` | `/grupos-familiares` | `GrupoFamiliarRequest` | `201 GrupoFamiliarResponse`; creador propietario/administrador principal | `201`, `400`, `401`, `409 grupo_existente`, `422` |
| `GET` | `/grupos-familiares/{grupoId}` | — | `200 GrupoFamiliarResponse` | `200`, `401`, `403`, `404` |
| `PATCH` | `/grupos-familiares/{grupoId}` | `{nombre}`; `If-Match` | `200 GrupoFamiliarResponse` | `200`, `400`, `401`, `403`, `404`, `412`, `422` |
| `POST` | `/grupos-familiares/{grupoId}/eliminaciones` | `{verificacionOtpId}`; `Idempotency-Key`, `If-Match` | `202 {id, estado, creadoEn, urlEstado}` | `202`, `401`, `403 propietario_requerido`, `404`, `409`, `412`, `422` |
| `GET` | `/grupos-familiares/{grupoId}/eliminaciones/{eliminacionId}` | — | `200 ProcesoAsyncResponse` | `200`, `401`, `403`, `404` |
| `GET` | `/grupos-familiares/{grupoId}/integrantes` | paginación | `200 Pagina<IntegranteFamiliarResponse>` | `200`, `401`, `403`, `404` |
| `PATCH` | `/grupos-familiares/{grupoId}/integrantes/{integranteId}` | `{rol}`; `If-Match` | `200 IntegranteFamiliarResponse` | `200`, `400`, `401`, `403`, `404`, `409 ultimo_propietario`, `412`, `422` |
| `DELETE` | `/grupos-familiares/{grupoId}/integrantes/{integranteId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `409 propietario/no_transferible`, `412` |

## 13. Invitaciones familiares

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/grupos-familiares/{grupoId}/invitaciones` | `estado?`, paginación | `200 Pagina<InvitacionFamiliarResponse>` | `200`, `401`, `403`, `404` |
| `POST` | `/grupos-familiares/{grupoId}/invitaciones` | `InvitacionFamiliarRequest`; `Idempotency-Key` | `202 CrearInvitacionFamiliarResponse` | `202`, `400`, `401`, `403`, `404`, `409 invitacion_existente`, `422`, `429`, `503` |
| `GET` | `/invitaciones-familiares/{token}` | token público opaco | `200 InvitacionPublicaResponse` | `200`, `404`, `410 invitacion_vencida`, `429` |
| `POST` | `/invitaciones-familiares/{token}/aceptaciones` | `AceptacionInvitacionRequest`; autenticación requerida | `201 IntegranteFamiliarResponse` | `201`, `400`, `401`, `404`, `409 ya_integrante`, `410`, `422` |
| `DELETE` | `/grupos-familiares/{grupoId}/invitaciones/{invitacionId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `409 estado_incompatible`, `412` |

## 14. Cuentas y movimientos familiares

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/grupos-familiares/{grupoId}/cuentas-compartidas` | paginación | `200 Pagina<CuentaCompartidaResponse>` | `200`, `401`, `403`, `404` |
| `POST` | `/grupos-familiares/{grupoId}/cuentas-compartidas` | `{cuentaId}` | `201 CuentaCompartidaResponse` | `201`, `400`, `401`, `403`, `404`, `409 ya_compartida`, `422` |
| `DELETE` | `/grupos-familiares/{grupoId}/cuentas-compartidas/{cuentaId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `409 cuenta_en_uso`, `412` |
| `GET` | `/grupos-familiares/{grupoId}/categorias` | `tipo?`, paginación | `200 Pagina<CategoriaFamiliarResponse>` | `200`, `400`, `401`, `403`, `404` |
| `POST` | `/grupos-familiares/{grupoId}/categorias` | `CategoriaRequest` | `201 CategoriaFamiliarResponse` | `201`, `400`, `401`, `403`, `404`, `409 nombre_duplicado`, `422` |
| `PATCH` | `/grupos-familiares/{grupoId}/categorias/{categoriaId}` | campos modificables; `If-Match` | `200 CategoriaFamiliarResponse` | `200`, `400`, `401`, `403`, `404`, `409`, `412`, `422` |
| `DELETE` | `/grupos-familiares/{grupoId}/categorias/{categoriaId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `409 categoria_en_uso`, `412` |
| `GET` | `/grupos-familiares/{grupoId}/movimientos` | filtros familiares y paginación | `200 Pagina<MovimientoResponse>` | `200`, `400`, `401`, `403`, `404` |
| `POST` | `/grupos-familiares/{grupoId}/movimientos` | `MovimientoRequest`; `Idempotency-Key` | `201 MovimientoResponse` | `201`, `400`, `401`, `403`, `404`, `409 cuenta_no_compartida`, `422` |
| `GET` | `/grupos-familiares/{grupoId}/movimientos/{movimientoId}` | — | `200 MovimientoResponse` | `200`, `401`, `403`, `404` |
| `PATCH` | `/grupos-familiares/{grupoId}/movimientos/{movimientoId}` | `{descripcion?, categoriaIds?}`; `If-Match` | `200 MovimientoResponse` | `200`, `400`, `401`, `403`, `404`, `412`, `422` |
| `DELETE` | `/grupos-familiares/{grupoId}/movimientos/{movimientoId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `412` |

## 15. Caja compartida

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/grupos-familiares/{grupoId}/caja-compartida` | — | `200 CajaCompartidaResponse` | `200`, `401`, `403`, `404` |
| `GET` | `/grupos-familiares/{grupoId}/operaciones-caja` | `tipo?`, integrante, fechas, paginación | `200 Pagina<OperacionCajaResponse>` | `200`, `400`, `401`, `403`, `404` |
| `POST` | `/grupos-familiares/{grupoId}/operaciones-caja` | `OperacionCajaRequest`; `Idempotency-Key` | `201 OperacionCajaResponse` | `201`, `400`, `401`, `403`, `404`, `409 saldo_insuficiente`, `422 otp_requerido` |
| `GET` | `/grupos-familiares/{grupoId}/operaciones-caja/{operacionId}` | — | `200 OperacionCajaResponse` | `200`, `401`, `403`, `404` |

Las operaciones de caja son inmutables. Una corrección se representa con una nueva operación compensatoria y ambas quedan auditadas.

## 16. Presupuestos familiares

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/grupos-familiares/{grupoId}/presupuestos` | periodo, estado, categoría y paginación | `200 Pagina<PresupuestoResponse>` | `200`, `400`, `401`, `403`, `404` |
| `POST` | `/grupos-familiares/{grupoId}/presupuestos` | `PresupuestoRequest` | `201 PresupuestoResponse` | `201`, `400`, `401`, `403`, `404`, `409`, `422` |
| `GET` | `/grupos-familiares/{grupoId}/presupuestos/{presupuestoId}` | — | `200 PresupuestoResponse` | `200`, `401`, `403`, `404` |
| `PATCH` | `/grupos-familiares/{grupoId}/presupuestos/{presupuestoId}` | campos modificables; `If-Match` | `200 PresupuestoResponse` | `200`, `400`, `401`, `403`, `404`, `409`, `412`, `422` |
| `DELETE` | `/grupos-familiares/{grupoId}/presupuestos/{presupuestoId}` | `If-Match` | `204` | `204`, `401`, `403`, `404`, `412` |

## 17. Dashboards y reportes

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/tableros-financieros` | `ambito=privado`, `desde?`, `hasta?` | `200 DashboardResponse` | `200`, `400`, `401` |
| `GET` | `/grupos-familiares/{grupoId}/tableros-financieros` | fechas | `200 DashboardResponse` | `200`, `400`, `401`, `403`, `404` |
| `GET` | `/reportes-financieros` | `rango`, `desde?`, `hasta?`, filtros | `200 ReporteResponse` | `200`, `400`, `401`, `422 rango_invalido` |
| `GET` | `/grupos-familiares/{grupoId}/reportes-financieros` | rango y filtros | `200 ReporteResponse` | `200`, `400`, `401`, `403`, `404`, `422` |

`desde` y `hasta` son obligatorios cuando `rango=personalizado`.

## 18. Exportaciones

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/exportaciones` | estado, formato, fechas y paginación | `200 Pagina<ExportacionResponse>` | `200`, `400`, `401` |
| `POST` | `/exportaciones` | `ExportacionRequest`; `Idempotency-Key` | `202 ExportacionResponse`; `Location` | `202`, `400`, `401`, `403 capacidad_premium`, `404`, `409`, `422`, `429`, `503` |
| `GET` | `/exportaciones/{exportacionId}` | — | `200 ExportacionResponse` | `200`, `401`, `403`, `404` |
| `POST` | `/exportaciones/{exportacionId}/descargas` | — | `201 {url, expiraEn}` | `201`, `401`, `403`, `404`, `409 no_completada`, `429`, `503` |
| `DELETE` | `/exportaciones/{exportacionId}` | `If-Match` | `204` | `204`, `401`, `404`, `409 procesamiento_activo`, `412` |

## 19. Predicciones, alertas y score

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/proyecciones-gastos` | `ambito=privado`, `periodo?` | `200 ProyeccionResponse` | `200`, `400`, `401`, `403 capacidad_premium`, `422 datos_insuficientes` |
| `GET` | `/grupos-familiares/{grupoId}/proyecciones-gastos` | periodo | `200 ProyeccionResponse` | `200`, `400`, `401`, `403`, `404`, `422` |
| `GET` | `/alertas-financieras` | nivel, leída, fechas y paginación | `200 Pagina<AlertaResponse>` | `200`, `400`, `401` |
| `GET` | `/alertas-financieras/{alertaId}` | — | `200 AlertaResponse` | `200`, `401`, `404` |
| `PATCH` | `/alertas-financieras/{alertaId}` | `{leida?, archivada?}`; `If-Match` | `200 AlertaResponse` | `200`, `400`, `401`, `404`, `412` |
| `GET` | `/score-financiero` | — | `200 ScoreResponse` | `200`, `401`, `422 datos_insuficientes` |

No se exponen endpoints públicos para crear predicciones o alertas: las genera el backend mediante procesos internos y eventos de dominio.

## 20. Planes y suscripciones

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/planes-suscripcion` | — | `200 PlanSuscripcionResponse[]` | `200` |
| `GET` | `/suscripcion` | — | `200 SuscripcionResponse` | `200`, `401` |
| `POST` | `/suscripciones` | `SuscripcionRequest`; `Idempotency-Key` | `201 SuscripcionResponse` | `201`, `400`, `401`, `403 otp_requerido`, `409 ya_activa`, `422 comprobante_invalido`, `503` |
| `POST` | `/suscripcion/cancelaciones` | `{motivo?}`; `Idempotency-Key`, `If-Match` | `200 SuscripcionResponse` con fin de vigencia | `200`, `401`, `404`, `409`, `412`, `503` |
| `POST` | `/restauraciones-suscripcion` | `{proveedor, comprobante}` | `202 SuscripcionResponse` | `202`, `400`, `401`, `404`, `409`, `422`, `503` |

## 21. Endpoints operativos del cliente

| Método | Endpoint | Request | Response exitosa | Códigos |
|---|---|---|---|---|
| `GET` | `/configuracion-cliente` | cabeceras de plataforma/versión | `200 {versionMinima, versionRecomendada, mantenimiento, capacidades}` | `200` |
| `POST` | `/dispositivos` | token push, plataforma, versión | `201 DispositivoResponse` | `201`, `400`, `401`, `409`, `422` |
| `PATCH` | `/dispositivos/{dispositivoId}` | token push y preferencias; `If-Match` | `200 DispositivoResponse` | `200`, `400`, `401`, `404`, `412`, `422` |
| `DELETE` | `/dispositivos/{dispositivoId}` | `If-Match` | `204` | `204`, `401`, `404`, `412` |

Estos recursos permiten notificaciones, invalidación de sesiones y compatibilidad mínima de la app sin introducir lógica financiera en el cliente.
