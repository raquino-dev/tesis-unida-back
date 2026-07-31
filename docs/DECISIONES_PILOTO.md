# Decisiones aprobadas para el piloto

**Estado:** aprobado  
**Fecha de decisión:** 29 de julio de 2026  
**Ámbito:** aplicación Android Finanzas Inteligentes y backend .NET

Este documento cierra las decisiones de alcance y proveedores del hito 0. Cualquier
ampliación posterior debe registrar su impacto en calendario, pruebas, privacidad y
evidencia académica antes de incorporarse.

## 1. Diseño del piloto

| Aspecto | Decisión |
|---|---|
| Lugar | Asunción, Paraguay |
| Participantes | 10 personas adultas |
| Duración | 28 días consecutivos |
| Plataforma | Android 10 o superior |
| Distribución | Google Play Internal Testing |
| Datos | Datos financieros reales aportados voluntariamente; sin conexión bancaria |
| Instrumentos | Consentimiento, encuesta previa, tareas guiadas, métricas de uso y encuesta posterior |
| Moneda | PYG |
| Abandono | El participante puede abandonar y solicitar borrado o anonimización |

La evaluación no pretende obtener inferencia estadística poblacional. Busca comprobar
factibilidad técnica, usabilidad, utilidad percibida y comportamiento del sistema en
un grupo piloto controlado.

## 2. Alcance funcional obligatorio

### Identidad y seguridad

- registro, inicio y cierre de sesión, recuperación y cambio de contraseña;
- JWT y refresh token rotativo;
- OTP para operaciones sensibles o de riesgo;
- biometría local para reautorización en el dispositivo;
- dispositivos, sesiones, eventos de seguridad y auditoría;
- consentimiento, retiro del piloto y eliminación o anonimización.

### Finanzas personales

- cuentas, categorías, ingresos y gastos;
- tarjetas de crédito como instrumentos manuales de planificación;
- transferencias contables entre cuentas propias;
- movimientos recurrentes con ejecución automática e idempotente;
- presupuestos y metas de ahorro;
- filtros, historial y conciliación de saldos.

### Tarjetas sin datos del plástico

Una tarjeta se identifica visualmente mediante un **alias elegido por el usuario**.
Puede registrar límite, saldo utilizado, cierre, vencimiento, cuenta de pago, color,
estado y movimientos asociados.

Está prohibido solicitar, transmitir o persistir:

- emisor;
- últimos cuatro dígitos;
- PAN o número completo;
- nombre impreso;
- fecha de expiración;
- CVV;
- token bancario;
- credenciales o cualquier otro dato del plástico.

El alias debe ser único entre las tarjetas activas del mismo usuario. No existe
integración con bancos, emisores ni redes de tarjetas.

### Finanzas familiares

- grupos con propietario, administradores e integrantes;
- invitaciones y control de membresía;
- cuentas, movimientos, categorías, presupuestos y metas familiares;
- caja común lógica con aportes, retiros y trazabilidad;
- separación verificable entre información privada y familiar.

### Documentos y exportación

- carga privada de imágenes y PDF;
- OCR de comprobantes con revisión y corrección humana;
- importación de XML SIFEN mediante parser propio;
- creación del movimiento sólo después de la confirmación;
- exportaciones PDF y XLSX, con CSV adicional.

### Analítica

- dashboard y reportes personales y familiares;
- comparación de gasto real contra presupuesto;
- proyecciones explicables, marcadas como preliminares con menos de tres meses;
- alertas y recomendaciones;
- indicador de salud financiera de 0 a 100, versionado y explicable;
- factores positivos, negativos, recomendaciones e historial.

El indicador de salud financiera no es un score crediticio, no consulta centrales de
riesgo, no determina elegibilidad para productos financieros y no sustituye asesoría
profesional. Con datos insuficientes debe indicar `provisional` o `datos insuficientes`.

### Notificaciones

- centro de alertas dentro de la aplicación como fuente canónica;
- notificaciones push por presupuesto, recurrencia, actividad familiar, procesamiento
  documental, suscripción y resumen semanal;
- consentimiento y preferencia de activación;
- renovación y revocación del token del dispositivo;
- navegación al contexto correcto al abrir una notificación.

No se incluyen campañas publicitarias ni segmentación comercial.

### Suscripciones

- una suscripción mensual Premium mediante Google Play Billing;
- producto y plan configurados en Google Play Console;
- flujo real de compra usando instrumentos de pago de prueba;
- compra aprobada, rechazada y pendiente;
- renovación, cancelación y restauración;
- verificación del `purchaseToken` mediante Google Play Developer API desde el backend;
- reconocimiento de compras y sincronización de cambios de estado;
- Real-time Developer Notifications para eventos del ciclo de vida.

El piloto no cobrará dinero real. La integración, verificación y gestión de derechos
son reales; solamente el instrumento de pago pertenece al ambiente de prueba.

## 3. Proveedores aprobados

| Capacidad | Proveedor/tecnología | Modalidad del piloto |
|---|---|---|
| Aplicación | Flutter | Android 10+ |
| Backend | ASP.NET Core .NET 10 | API y Worker |
| Base de datos | Supabase PostgreSQL Pro | Administrada, backup diario |
| Archivos | Amazon S3 | Bucket privado, cifrado y URL prefirmada |
| OCR | Amazon Textract AnalyzeExpense | Sujeto a prueba de 30–50 comprobantes paraguayos |
| XML SIFEN | Parser propio | Sin entidades externas |
| Correo | Amazon SES | Acceso de producción antes del piloto |
| Push | Firebase Cloud Messaging | API HTTP v1 |
| Facturación | Google Play Billing | License testers y productos de prueba |
| Caché | Redis | Servicio privado accesible sólo por API/Worker |
| Servidor | Hetzner CX33, Ubuntu 24.04 | Docker Compose y systemd |
| Proxy/TLS | Nginx y Cloudflare | HTTPS y origen restringido |
| Distribución | Google Play Internal Testing | Cuentas Google autorizadas |

Textract se confirma definitivamente si la prueba local demuestra una extracción útil
de total, fecha y comercio. Si no supera el criterio definido en la sección 5, debe
compararse con un segundo proveedor antes de desarrollar adaptaciones específicas.

## 4. Exclusiones

- panel web administrativo o de usuario;
- aplicación iOS;
- conexión directa con bancos, billeteras, emisores o redes de tarjetas;
- movimientos reales de dinero;
- cobros reales durante el piloto;
- datos del plástico de tarjetas;
- score crediticio o consulta de centrales de riesgo;
- machine learning opaco o entrenamiento de modelos complejos;
- campañas comerciales;
- soporte multi-moneda;
- publicación abierta al público;
- alta disponibilidad distribuida y motor antifraude avanzado.

## 5. Criterios de éxito

### Participación y usabilidad

| Indicador | Objetivo |
|---|---:|
| Participantes que completan encuesta inicial, uso y encuesta final | al menos 8 de 10 |
| Duración observada | 28 días |
| Reducción de la mediana de tiempo para registrar un gasto | al menos 30 % respecto a la medición inicial |
| Flujos del guion completados sin defecto bloqueante | 100 % |

### Funcionalidad externa

| Capacidad | Criterio |
|---|---|
| Billing | compra aprobada, rechazada, pendiente, renovación, cancelación y restauración verificadas |
| Push | al menos 90 % de mensajes de prueba aceptados por FCM para dispositivos registrados; la alerta interna siempre queda disponible |
| OCR | total correcto en al menos 90 %, fecha en 85 % y comercio en 80 % del conjunto de validación |
| OCR corregible | 100 % de documentos permiten corregir o rechazar antes de crear un movimiento |
| SIFEN | 100 % del conjunto válido se importa y los XML maliciosos o duplicados se rechazan |
| Score | mismo conjunto de datos y versión produce el mismo resultado y explica sus factores |

El tiempo total de OCR y la tasa de corrección humana se registran aparte de la
exactitud de cada campo.

### Rendimiento, operación y seguridad

| Indicador | Objetivo |
|---|---:|
| Autenticación | ≤ 2 s |
| Consultas principales | ≤ 3 s |
| OCR promedio | ≤ 5 s para el conjunto del piloto |
| Recuperación autorizada de archivo | ≤ 4 s |
| Usuarios simultáneos | 10 sin degradación crítica |
| Disponibilidad | ≥ 95 %, excluyendo mantenimiento programado |
| Backup | al menos cada 24 h, con restauración ensayada |
| Recuperación después de reinicio | ≤ 10 min |
| Operaciones críticas auditadas | 100 % |
| Accesos exitosos entre usuarios o grupos no autorizados | 0 |
| Datos del plástico solicitados, transmitidos o persistidos | 0 |

## 6. Evidencia mínima

- versión, commit, ambiente y período exacto;
- consentimiento anonimizado y encuestas pre/post;
- resultados del guion funcional por participante;
- capturas o registros anonimizados de Billing, push, OCR y SIFEN;
- resultados de latencia, carga, disponibilidad, backup y restauración;
- pruebas negativas de aislamiento y autorización;
- incidentes, defectos, correcciones y decisión de cierre;
- versión del algoritmo del indicador financiero y variables empleadas.

La plantilla de recolección está en
[`api/EVIDENCIA_PILOTO_TEMPLATE.md`](api/EVIDENCIA_PILOTO_TEMPLATE.md).

## 7. Acciones externas pendientes

- crear o confirmar la cuenta de Google Play Console y el perfil de pagos;
- reservar definitivamente el Application ID;
- configurar Internal Testing, license testers, producto y plan mensual;
- crear proyecto Firebase y credenciales de servicio para FCM HTTP v1;
- crear Supabase Pro y probar restauración;
- habilitar AWS, bucket S3, Textract y salida de sandbox de SES;
- adquirir/configurar Hetzner, dominio, DNS y Cloudflare;
- ejecutar la evaluación OCR con 30–50 comprobantes anonimizados;
- aprobar consentimiento, política de privacidad, retención y encuestas;
- reclutar diez participantes con cuenta Google y Android 10+.

Las credenciales y documentos financieros de participantes nunca se incluyen en Git,
capturas públicas o evidencia académica sin anonimización.

## 8. Impacto técnico ya identificado

La decisión de tarjetas por alias reemplaza el diseño anterior. El 30 de julio
de 2026 se retiraron `Emisor` y `UltimosCuatro` de:

- entidad y casos de uso del backend;
- requests, responses y OpenAPI;
- mapeo de Entity Framework y una nueva migración;
- pruebas y ejemplos contractuales.

Las migraciones históricas se conservan como evidencia de evolución. La migración
`M0019_TarjetasSoloAlias` elimina las columnas vigentes y renombra `nombre` a
`alias`. El contrato móvil/API de tarjetas, transferencias y recurrencias quedó
integrado; aún debe validarse la migración y la ejecución automática del Worker
sobre PostgreSQL real antes de habilitar estas capacidades en el piloto.
