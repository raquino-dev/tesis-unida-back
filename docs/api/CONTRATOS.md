# Contratos JSON

Los campos `id`, `creadoEn`, `actualizadoEn` y `version` son generados por el servidor. Los DTO de actualización contienen únicamente campos modificables.

Los ejemplos son representativos. Campos requeridos, longitudes, formatos y restricciones se formalizan en `openapi.yaml`. Las listas paginadas usan el sobre definido en [CONVENCIONES.md](CONVENCIONES.md):

```json
{
  "datos": [],
  "paginacion": {
    "siguienteCursor": null,
    "hayMas": false,
    "limite": 20
  }
}
```

Todos los errores usan `application/problem+json`:

```json
{
  "type": "https://api.finanzas.example/problemas/validacion",
  "title": "La solicitud no es válida",
  "status": 422,
  "detail": "Uno o más campos no cumplen las reglas de negocio.",
  "instance": "/api/v1/recurso",
  "codigo": "validacion",
  "correlationId": "019b-correlation-...",
  "errores": {
    "campo": ["Descripción controlada del error."]
  }
}
```

### ProcesoAsyncResponse

```json
{
  "id": "019b-job-...",
  "estado": "pendiente",
  "creadoEn": "2026-07-22T18:30:00Z",
  "completadoEn": null,
  "errorCodigo": null,
  "urlEstado": "/api/v1/recurso/019b-job-..."
}
```

## Enumeraciones

| Nombre | Valores JSON |
|---|---|
| `TipoMovimiento` | `gasto`, `ingreso` |
| `TipoCuenta` | `efectivo`, `cuenta-corriente`, `cuenta-ahorro`, `tarjeta-debito`, `billetera-digital`, `otra` |
| `TipoCategoria` | `gasto`, `ingreso`, `ambos` |
| `PeriodoPresupuesto` | `mensual`, `trimestral`, `anual` |
| `EstadoPresupuesto` | `activo`, `pausado`, `finalizado` |
| `SaludPresupuesto` | `saludable`, `en-riesgo`, `excedido` |
| `FrecuenciaRecurrencia` | `diaria`, `semanal`, `quincenal`, `mensual`, `anual` |
| `EstadoRecurrencia` | `activa`, `pausada`, `finalizada` |
| `TipoDocumento` | `imagen`, `pdf`, `xml-sifen` |
| `EstadoArchivo` | `pendiente`, `cargando`, `disponible`, `fallido`, `eliminado` |
| `EstadoProcesamiento` | `pendiente`, `procesando`, `completado`, `incompleto`, `fallido` |
| `Ambito` | `privado`, `familiar` |
| `RolFamiliar` | `propietario`, `administrador`, `integrante` |
| `EstadoInvitacion` | `pendiente`, `aceptada`, `vencida`, `revocada` |
| `TipoOperacionCaja` | `aporte`, `retiro`, `ajuste-positivo`, `ajuste-negativo`, `compensacion`; los tres últimos son emitidos sólo por el servidor |
| `RangoReporte` | `semana`, `mes`, `trimestre`, `anio`, `personalizado` |
| `FormatoExportacion` | `csv`, `xlsx`, `pdf` |
| `NivelAlerta` | `informacion`, `advertencia`, `error`, `exito` |
| `EstadoSuscripcion` | `pendiente`, `activa`, `en-gracia`, `vencida`, `cancelada` |

En el alcance funcional, “administrador del grupo” designa al administrador principal. En el contrato ese rol se denomina `propietario`; `administrador` representa una delegación con permisos operativos limitados.

## Usuarios, sesiones y seguridad

### CrearUsuarioRequest

```json
{
  "nombre": "Rodrigo Aquino",
  "correo": "rodrigo@correo.com.py",
  "contrasena": "Una-clave-segura-2026",
  "moneda": "PYG",
  "idioma": "es-PY",
  "zonaHoraria": "America/Asuncion",
  "aceptaTerminos": true
}
```

### UsuarioResponse

```json
{
  "id": "019b1234-...",
  "nombre": "Rodrigo Aquino",
  "correo": "rodrigo@correo.com.py",
  "moneda": "PYG",
  "idioma": "es-PY",
  "ubicacion": "Asuncion, Paraguay",
  "zonaHoraria": "America/Asuncion",
  "correoVerificado": false,
  "creadoEn": "2026-07-22T18:30:00Z",
  "version": 1
}
```

### CrearSesionRequest / SesionResponse

```json
{
  "correo": "rodrigo@correo.com.py",
  "contrasena": "Una-clave-segura-2026",
  "dispositivo": {
    "identificador": "android-installation-id",
    "nombre": "Pixel 8",
    "plataforma": "android",
    "versionSistema": "14",
    "versionAplicacion": "0.1.0"
  },
  "recordarDispositivo": true
}
```

```json
{
  "id": "019b-session-...",
  "accessToken": "eyJ...",
  "refreshToken": "opaque-rotating-token",
  "tipoToken": "Bearer",
  "accessTokenExpiraEn": "2026-07-22T18:45:00Z",
  "refreshTokenExpiraEn": "2026-08-21T18:30:00Z",
  "requiereOtp": false,
  "usuario": { "id": "019b1234-...", "nombre": "Rodrigo Aquino", "correo": "rodrigo@correo.com.py" }
}
```

### SesionActivaResponse

Nunca contiene access ni refresh tokens:

```json
{
  "id": "019b-session-...",
  "dispositivo": {
    "id": "019b-device-...",
    "nombre": "Pixel 8",
    "plataforma": "android"
  },
  "emitidaEn": "2026-07-22T18:30:00Z",
  "expiraEn": "2026-08-21T18:30:00Z",
  "actual": true,
  "revocadaEn": null
}
```

### RenovarSesionRequest

```json
{ "refreshToken": "opaque-rotating-token", "identificadorDispositivo": "android-installation-id" }
```

### RecuperacionContrasenaRequest / RestablecimientoContrasenaRequest

```json
{ "correo": "rodrigo@correo.com.py" }
```

```json
{ "token": "token-enviado-por-correo", "nuevaContrasena": "Nueva-clave-2026" }
```

### CambiarContrasenaRequest

```json
{
  "contrasenaActual": "Una-clave-segura-2026",
  "nuevaContrasena": "Nueva-clave-2026",
  "verificacionOtpId": "019b-verificacion-..."
}
```

### DesafioOtpRequest / DesafioOtpResponse / VerificacionOtpRequest

```json
{ "motivo": "cambio-contrasena", "canal": "correo" }
```

```json
{
  "id": "019b-otp-...",
  "destinoEnmascarado": "ro***@correo.com.py",
  "expiraEn": "2026-07-22T18:35:00Z",
  "intentosRestantes": 5
}
```

```json
{ "desafioId": "019b-otp-...", "codigo": "123456" }
```

La verificación exitosa devuelve:

```json
{ "id": "019b-verificacion-...", "valida": true, "expiraEn": "2026-07-22T18:40:00Z" }
```

### DesafioBiometricoResponse / VerificacionBiometricaRequest

```json
{
  "id": "019b-biometric-challenge-...",
  "proposito": "verificacion",
  "dispositivoId": "019b-device-...",
  "credencialId": "019b-key-...",
  "nonce": "base64url-nonce-del-servidor",
  "expiraEn": "2026-07-22T18:32:00Z"
}
```

```json
{
  "desafioId": "019b-biometric-challenge-...",
  "identificadorDispositivo": "android-installation-id",
  "firma": "base64-signature",
  "clavePublicaId": "019b-key-..."
}
```

El desafío es de un solo uso, está ligado a usuario, credencial, dispositivo y propósito, y nunca lo elige el cliente.

### EventoSeguridadResponse

```json
{
  "id": "019b-event-...",
  "tipo": "inicio-sesion",
  "descripcion": "Inicio de sesion desde un dispositivo habitual",
  "exitoso": true,
  "origenAproximado": "Asunción, PY",
  "dispositivo": "Pixel 8",
  "ocurridoEn": "2026-07-22T18:30:00Z"
}
```

### PreferenciasResponse

```json
{
  "tema": "oscuro",
  "idioma": "es-PY",
  "moneda": "PYG",
  "zonaHoraria": "America/Asuncion",
  "notificacionesPush": true,
  "resumenSemanal": false,
  "version": 1
}
```

### CredencialBiometricaResponse

```json
{
  "id": "019b-biometric-...",
  "identificadorDispositivo": "android-installation-id",
  "nombreDispositivo": "Pixel 8",
  "algoritmo": "ES256",
  "creadaEn": "2026-07-22T18:30:00Z",
  "ultimoUsoEn": null,
  "version": 1
}
```

### EventoAuditoriaResponse

```json
{
  "id": "019b-audit-...",
  "accion": "movimiento.creado",
  "recurso": "movimiento",
  "recursoId": "019b-movement-...",
  "usuarioId": "019b-user-...",
  "grupoFamiliarId": null,
  "datosAnteriores": null,
  "datosPosteriores": { "monto": 285000, "tipo": "gasto" },
  "correlationId": "019b-correlation-...",
  "ocurridoEn": "2026-07-22T18:30:00Z"
}
```

## Cuentas, categorías y tarjetas

### CuentaRequest / CuentaResponse

```json
{
  "nombre": "Caja de ahorro",
  "tipo": "cuenta-ahorro",
  "moneda": "PYG",
  "saldoInicial": 25000000,
  "color": "#6868A6",
  "icono": "savings",
  "incluidaEnTotal": true
}
```

```json
{
  "id": "019b-account-...",
  "nombre": "Caja de ahorro",
  "tipo": "cuenta-ahorro",
  "moneda": "PYG",
  "saldoInicial": 25000000,
  "saldoActual": 26340000,
  "color": "#6868A6",
  "icono": "savings",
  "incluidaEnTotal": true,
  "eliminada": false,
  "version": 3
}
```

### CategoriaRequest / CategoriaResponse

```json
{ "nombre": "Alimentacion", "tipo": "gasto", "icono": "restaurant", "color": "#6868A6" }
```

```json
{
  "id": "019b-category-...",
  "nombre": "Alimentacion",
  "tipo": "gasto",
  "icono": "restaurant",
  "color": "#6868A6",
  "predefinida": false,
  "enUso": true,
  "version": 1
}
```

### TarjetaCreditoRequest / TarjetaCreditoResponse

```json
{
  "nombre": "Itaú Mastercard",
  "emisor": "Itaú",
  "ultimosCuatro": "1234",
  "cuentaPagoId": "019b-account-...",
  "diaCierre": 20,
  "diaVencimiento": 5,
  "limiteCredito": 15000000,
  "moneda": "PYG",
  "color": "#6868A6"
}
```

```json
{
  "id": "019b-card-...",
  "nombre": "Itaú Mastercard",
  "emisor": "Itaú",
  "ultimosCuatro": "1234",
  "cuentaPago": { "id": "019b-account-...", "nombre": "Caja de ahorro", "tipo": "cuenta-ahorro" },
  "diaCierre": 20,
  "diaVencimiento": 5,
  "limiteCredito": 15000000,
  "saldoUtilizado": 4200000,
  "creditoDisponible": 10800000,
  "moneda": "PYG",
  "color": "#6868A6",
  "version": 1
}
```

## Movimientos y documentos

### MovimientoRequest

```json
{
  "ambito": "privado",
  "tipo": "gasto",
  "monto": 285000,
  "fecha": "2026-07-22",
  "hora": "15:30:00",
  "categoriaIds": ["019b-category-..."],
  "descripcion": "Supermercado",
  "cuentaId": "019b-account-...",
  "documentoId": "019b-document-...",
  "movimientoRecurrenteId": null
}
```

Para ámbito familiar se agrega `grupoFamiliarId`; la cuenta debe estar compartida.

### MovimientoResponse

```json
{
  "id": "019b-movement-...",
  "ambito": "privado",
  "tipo": "gasto",
  "monto": 285000,
  "fecha": "2026-07-22",
  "hora": "15:30:00",
  "categorias": [{ "id": "019b-category-...", "nombre": "Alimentacion", "color": "#6868A6" }],
  "descripcion": "Supermercado",
  "cuenta": { "id": "019b-account-...", "nombre": "Debito", "tipo": "tarjeta-debito" },
  "documento": {
    "id": "019b-document-...",
    "tipo": "imagen",
    "estadoProcesamiento": "completado"
  },
  "creadoPor": { "id": "019b-user-...", "nombre": "Rodrigo Aquino" },
  "creadoEn": "2026-07-22T18:30:00Z",
  "version": 1
}
```

### DocumentoFinancieroResponse

La carga utiliza `multipart/form-data` con partes `archivo`, `tipo`, `ambito` y `grupoFamiliarId` opcional.

```json
{
  "id": "019b-document-...",
  "nombreOriginal": "factura.pdf",
  "tipo": "pdf",
  "mimeType": "application/pdf",
  "tamanoBytes": 248120,
  "sha256": "hex-sha256",
  "estadoArchivo": "disponible",
  "estadoProcesamiento": "pendiente",
  "procesamientoId": "019b-process-...",
  "creadoEn": "2026-07-22T18:30:00Z",
  "version": 1
}
```

### ProcesamientoDocumentalResponse

```json
{
  "id": "019b-process-...",
  "documentoId": "019b-document-...",
  "tipo": "ocr",
  "estado": "incompleto",
  "confianza": 0.72,
  "datosDetectados": {
    "monto": 285000,
    "fecha": "2026-07-22",
    "comercio": "Supermercado San Roque",
    "categoriaSugeridaId": "019b-category-...",
    "cdcSifen": null
  },
  "advertencias": ["La fecha posee baja confianza"],
  "iniciadoEn": "2026-07-22T18:30:01Z",
  "finalizadoEn": "2026-07-22T18:30:04Z",
  "version": 2
}
```

XML SIFEN utiliza `tipo: "sifen"` y completa `cdcSifen`, timbrado, RUC, número de comprobante e impuestos cuando estén disponibles.

### MovimientoRecurrenteRequest

```json
{
  "tipo": "gasto",
  "monto": 180000,
  "categoriaIds": ["019b-category-..."],
  "cuentaId": "019b-account-...",
  "descripcion": "Internet",
  "fechaInicio": "2026-07-01",
  "fechaFin": null,
  "frecuencia": "mensual",
  "cantidadOcurrencias": null
}
```

### MovimientoRecurrenteResponse

Agrega `id`, `ocurrenciasCompletadas`, `proximaEjecucion`, `estado` y `version`.

```json
{
  "id": "019b-recurring-...",
  "tipo": "gasto",
  "monto": 180000,
  "categorias": [{ "id": "019b-category-...", "nombre": "Servicios" }],
  "cuenta": { "id": "019b-account-...", "nombre": "Debito" },
  "descripcion": "Internet",
  "fechaInicio": "2026-07-01",
  "fechaFin": null,
  "frecuencia": "mensual",
  "cantidadOcurrencias": null,
  "ocurrenciasCompletadas": 1,
  "proximaEjecucion": "2026-08-01",
  "estado": "activa",
  "version": 2
}
```

### TransferenciaRequest / TransferenciaResponse

```json
{ "cuentaOrigenId": "019b-a1", "cuentaDestinoId": "019b-a2", "monto": 500000, "fecha": "2026-07-22", "descripcion": "Ahorro mensual" }
```

```json
{
  "id": "019b-transfer-...",
  "cuentaOrigen": { "id": "019b-a1", "nombre": "Efectivo" },
  "cuentaDestino": { "id": "019b-a2", "nombre": "Caja de ahorro" },
  "monto": 500000,
  "fecha": "2026-07-22",
  "descripcion": "Ahorro mensual",
  "estado": "confirmada",
  "creadoEn": "2026-07-22T18:30:01Z",
  "version": 1
}
```

## Presupuestos y metas

### PresupuestoRequest / PresupuestoResponse

```json
{
  "ambito": "privado",
  "grupoFamiliarId": null,
  "nombre": "Alimentacion mensual",
  "monto": 3500000,
  "periodo": "mensual",
  "categoriaIds": ["019b-category-..."]
}
```

```json
{
  "id": "019b-budget-...",
  "ambito": "privado",
  "nombre": "Alimentacion mensual",
  "monto": 3500000,
  "gastado": 1920000,
  "disponible": 1580000,
  "progreso": 0.5486,
  "estado": "activo",
  "salud": "saludable",
  "periodo": "mensual",
  "categorias": [{ "id": "019b-category-...", "nombre": "Alimentacion" }],
  "version": 1
}
```

### MetaAhorroRequest / MetaAhorroResponse

```json
{
  "ambito": "familiar",
  "grupoFamiliarId": "019b-family-...",
  "nombre": "Vacaciones familiares",
  "montoObjetivo": 9000000,
  "fechaObjetivo": "2027-01-15",
  "cuentaId": "019b-account-..."
}
```

```json
{
  "id": "019b-goal-...",
  "ambito": "familiar",
  "grupoFamiliarId": "019b-family-...",
  "nombre": "Vacaciones familiares",
  "montoObjetivo": 9000000,
  "montoAhorrado": 2250000,
  "montoRestante": 6750000,
  "progreso": 0.25,
  "fechaObjetivo": "2027-01-15",
  "cuenta": { "id": "019b-account-...", "nombre": "Caja de ahorro" },
  "version": 2
}
```

### AporteMetaRequest / AporteMetaResponse

```json
{ "monto": 250000, "cuentaOrigenId": "019b-account-...", "descripcion": "Aporte de julio" }
```

```json
{
  "id": "019b-contribution-...",
  "metaAhorroId": "019b-goal-...",
  "monto": 250000,
  "aportadoPor": { "id": "019b-user-...", "nombre": "Rodrigo Aquino" },
  "fecha": "2026-07-22",
  "creadoEn": "2026-07-22T18:30:00Z",
  "saldoMeta": 2500000
}
```

## Familia y caja compartida

### GrupoFamiliarRequest / GrupoFamiliarResponse

```json
{ "nombre": "Familia Aquino" }
```

```json
{
  "id": "019b-family-...",
  "nombre": "Familia Aquino",
  "miRol": "propietario",
  "cantidadIntegrantes": 3,
  "cantidadCuentasCompartidas": 2,
  "creadoEn": "2026-07-22T18:30:00Z",
  "version": 1
}
```

### InvitacionFamiliarRequest / InvitacionFamiliarResponse / CrearInvitacionFamiliarResponse

Se debe enviar exactamente uno entre `correo` e `identificadorUsuario`.

```json
{ "correo": "familiar@correo.com", "identificadorUsuario": null, "rol": "integrante" }
```

```json
{
  "id": "019b-invite-...",
  "grupoFamiliarId": "019b-family-...",
  "correo": "familiar@correo.com",
  "usuarioDestino": null,
  "rol": "integrante",
  "estado": "pendiente",
  "expiraEn": "2026-07-29T18:30:00Z",
  "version": 1
}
```

Sólo la respuesta inmediata de creación agrega `"codigo": "482915"`. Los listados y consultas posteriores nunca lo incluyen porque el servidor conserva únicamente su HMAC.

### InvitacionPublicaResponse

No revela correo completo ni integrantes:

```json
{
  "grupo": { "id": "019b-family-...", "nombre": "Familia Aquino" },
  "destinoEnmascarado": "fa***@correo.com",
  "rol": "integrante",
  "estado": "pendiente",
  "expiraEn": "2026-07-29T18:30:00Z"
}
```

### AceptacionInvitacionRequest

```json
{ "codigo": "482915" }
```

### IntegranteFamiliarResponse

```json
{
  "id": "019b-membership-...",
  "usuario": {
    "id": "019b-user-...",
    "nombre": "Maria Aquino",
    "correo": "maria@correo.com"
  },
  "rol": "integrante",
  "incorporadoEn": "2026-07-22T18:30:00Z",
  "version": 1
}
```

### CategoriaFamiliarResponse

Usa los mismos campos editables de `CategoriaRequest` y agrega:

```json
{
  "id": "019b-family-category-...",
  "grupoFamiliarId": "019b-family-...",
  "nombre": "Compras del hogar",
  "tipo": "gasto",
  "icono": "shopping_cart",
  "color": "#6868A6",
  "version": 1
}
```

### CuentaCompartidaResponse

```json
{
  "grupoFamiliarId": "019b-family-...",
  "cuenta": {
    "id": "019b-account-...",
    "nombre": "Caja de ahorro",
    "tipo": "cuenta-ahorro",
    "moneda": "PYG"
  },
  "compartidaPor": { "id": "019b-user-...", "nombre": "Rodrigo Aquino" },
  "compartidaEn": "2026-07-22T18:30:00Z",
  "version": 1
}
```

### OperacionCajaRequest / OperacionCajaResponse

```json
{
  "tipo": "aporte",
  "monto": 1000000,
  "descripcion": "Aporte mensual",
  "cuentaPrivadaId": "019b-account-...",
  "movimientoFamiliarId": null,
  "verificacionOtpId": null
}
```

Retiros y gastos requieren OTP y rol administrativo.

```json
{
  "id": "019b-treasury-...",
  "tipo": "aporte",
  "monto": 1000000,
  "descripcion": "Aporte mensual",
  "realizadoPor": { "id": "019b-user-...", "nombre": "Rodrigo Aquino" },
  "creadoEn": "2026-07-22T18:30:00Z",
  "saldoAnterior": 2500000,
  "saldoPosterior": 3500000
}
```

### CajaCompartidaResponse

```json
{ "grupoFamiliarId": "019b-family-...", "saldo": 3500000, "totalAportesMes": 2000000, "totalRetirosMes": 500000, "version": 8 }
```

## Analítica

### DashboardResponse

```json
{
  "ambito": "privado",
  "periodo": { "desde": "2026-07-01", "hasta": "2026-07-31" },
  "ingresos": 7900000,
  "gastos": 1383000,
  "balance": 6517000,
  "presupuestoTotal": 40500000,
  "presupuestoDisponible": 39117000,
  "scoreFinanciero": 78,
  "categoriasPrincipales": [{ "categoriaId": "019b-category-...", "nombre": "Alimentacion", "monto": 327000, "porcentaje": 0.2364 }],
  "proximosRecurrentes": [{ "nombre": "Internet", "monto": 180000, "fecha": "2026-08-01" }],
  "alertasDestacadas": ["Tu presupuesto de alimentación está cerca del límite"]
}
```

### ReporteResponse

```json
{
  "ambito": "privado",
  "rango": "mes",
  "desde": "2026-07-01",
  "hasta": "2026-07-31",
  "ingresos": 7900000,
  "gastos": 1383000,
  "balance": 6517000,
  "distribucion": [{ "categoriaId": "019b-category-...", "nombre": "Alimentacion", "monto": 327000, "porcentaje": 0.2364 }],
  "tendencia": [{ "periodo": "2026-07", "ingresos": 7900000, "gastos": 1383000 }],
  "observaciones": ["Alimentación es la categoría con mayor gasto"]
}
```

### ExportacionRequest / ExportacionResponse

```json
{
  "formato": "pdf",
  "ambito": "privado",
  "grupoFamiliarId": null,
  "desde": "2026-07-01",
  "hasta": "2026-07-31",
  "filtros": { "tipo": "gasto", "categoriaId": null, "cuentaId": null, "documento": "cualquiera" }
}
```

```json
{
  "id": "019b-export-...",
  "formato": "pdf",
  "estado": "completado",
  "creadoEn": "2026-07-22T18:30:00Z",
  "finalizadoEn": "2026-07-22T18:30:02Z",
  "cantidadMovimientos": 18,
  "totales": { "ingresos": 7900000, "gastos": 1383000, "transferido": 500000 },
  "expiraEn": "2026-07-29T18:30:00Z",
  "version": 2
}
```

La URL prefirmada sólo se obtiene mediante `POST /exportaciones/{exportacionId}/descargas`.

### ProyeccionResponse

```json
{
  "ambito": "privado",
  "mesesHistorial": 2,
  "preliminar": true,
  "gastoProyectado": 3850000,
  "balanceProyectado": 4050000,
  "categoriaMayorCrecimiento": "Transporte",
  "nivelRiesgo": "bajo",
  "versionModelo": "gastos-v1",
  "periodo": { "desde": "2026-07-01", "hasta": "2026-12-31" },
  "categorias": [{ "nombre": "Transporte", "montoProyectado": 620000, "variacion": 0.28 }],
  "historial": [{ "periodo": "2026-06", "proyectado": 3600000, "real": 3550000 }],
  "generadoEn": "2026-07-22T18:30:00Z"
}
```

### AlertaResponse

```json
{
  "id": "019b-alert-...",
  "titulo": "Gasto en transporte por encima del promedio",
  "mensaje": "Transporte representa 18% de tus gastos registrados.",
  "nivel": "advertencia",
  "fecha": "2026-07-22T18:30:00Z",
  "queOcurrio": "El gasto supera el promedio reciente.",
  "datosUtilizados": "Movimientos de los últimos 90 días.",
  "impacto": "El presupuesto podría agotarse antes de fin de mes.",
  "recomendacion": "Revisá los traslados recientes.",
  "leida": false,
  "version": 1
}
```

### ScoreResponse

```json
{
  "score": 78,
  "estado": "equilibrado",
  "versionAlgoritmo": "score-v1",
  "periodo": { "desde": "2026-04-01", "hasta": "2026-06-30" },
  "factoresPositivos": ["Balance mensual positivo"],
  "factoresNegativos": ["Aumento en gastos variables"],
  "historial": [{ "periodo": "2026-06", "score": 76 }],
  "recomendaciones": ["Mantené un fondo de emergencia"]
}
```

## Suscripciones

### PlanSuscripcionResponse

```json
{
  "id": "019b-plan-...",
  "codigo": "premium-mensual",
  "nombre": "Premium mensual",
  "precio": 45000,
  "moneda": "PYG",
  "periodo": "mensual",
  "capacidades": ["ocr", "predicciones", "exportaciones", "alertas-prioritarias"],
  "destacado": true
}
```

### SuscripcionRequest / SuscripcionResponse

```json
{ "planCodigo": "premium-mensual", "proveedor": "google-play", "comprobante": "purchase-token", "verificacionOtpId": "019b-verificacion-..." }
```

```json
{
  "id": "019b-subscription-...",
  "estado": "activa",
  "plan": { "id": "019b-plan-...", "codigo": "premium-mensual", "nombre": "Premium mensual" },
  "iniciadaEn": "2026-07-22T18:30:00Z",
  "canceladaEn": null,
  "finPeriodoEn": "2026-08-22T18:30:00Z",
  "capacidades": ["ocr", "predicciones", "exportaciones", "alertas-prioritarias"],
  "version": 1
}
```

## Cliente y dispositivos

### DispositivoRequest / DispositivoResponse

```json
{
  "identificadorInstalacion": "android-installation-id",
  "nombre": "Pixel 8",
  "plataforma": "android",
  "versionSistema": "14",
  "versionAplicacion": "0.1.0",
  "tokenPush": "fcm-token",
  "zonaHoraria": "America/Asuncion"
}
```

```json
{
  "id": "019b-device-...",
  "identificadorInstalacion": "android-installation-id",
  "nombre": "Pixel 8",
  "plataforma": "android",
  "versionAplicacion": "0.1.0",
  "confiable": true,
  "ultimoAccesoEn": "2026-07-22T18:30:00Z",
  "version": 1
}
```
