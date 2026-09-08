# Alcance y requisitos del sistema

Este documento consolida el alcance formal del proyecto para la aplicación móvil Finanzas Inteligentes y funciona como referencia de producto para el contrato API, la arquitectura y el plan de implementación. Las decisiones cerradas de proveedores, piloto y criterios de éxito se encuentran en [`DECISIONES_PILOTO.md`](DECISIONES_PILOTO.md).

## 1. Objetivo y validación

El proyecto comprende el diseño e implementación de una aplicación móvil Android orientada a la gestión inteligente de gastos personales y al control financiero familiar.

La validación se realiza en Asunción durante 2026 mediante una prueba piloto con **diez unidades familiares**, cada una representada en la muestra principal por **un adulto referente** (`n = 10`). La incorporación fue escalonada entre el 17 y el 31 de agosto de 2026 y cada unidad familiar dispone de **28 días consecutivos de observación desde su incorporación**. Los demás adultos de cada familia pueden participar en las funciones colaborativas del sistema, pero no se incorporan automáticamente como unidades independientes del análisis pre/post.

Se utilizan encuestas antes y después del uso para evaluar:

- facilidad de uso;
- utilidad percibida;
- percepción de seguridad;
- apoyo en el control financiero;
- aceptación general;
- eficiencia del registro de gastos.

La medición preuso principal fue completada por los diez referentes antes de su primer uso. Un bloque complementario solicitado posteriormente por tutoría recoge intención de compra, valoración de atributos técnicos y disposición de compra; se conserva separado del pretest y se analiza descriptivamente.

## 2. Alcance funcional

La aplicación diferencia finanzas privadas y familiares e incluye:

- ingresos y gastos privados y familiares;
- cuentas financieras internas y sus saldos;
- presupuestos por categoría individuales y grupales;
- metas de ahorro privadas y compartidas;
- caja compartida lógica, con aportes, retiros y trazabilidad;
- registro manual, OCR de imágenes/PDF e importación XML SIFEN;
- validación y corrección de datos detectados antes de crear el movimiento;
- dashboards y reportes por semana, mes y rango personalizado;
- comparación entre gasto real y presupuesto;
- movimientos recurrentes;
- tarjetas identificadas únicamente mediante un alias, sin datos del plástico;
- transferencias contables entre cuentas propias, sin movimiento real de dinero;
- exportación en PDF y Excel (`xlsx`), con CSV como formato adicional;
- proyecciones mensuales y por categoría;
- alertas, recomendaciones y observaciones explicables;
- indicador de salud financiera versionado y explicable, sin finalidad crediticia;
- notificaciones push transaccionales mediante Firebase Cloud Messaging;
- marca `preliminar` cuando la proyección use menos de tres meses de histórico;
- categorías predefinidas y personalizables;
- grupos, invitaciones, integrantes y administración por roles;
- JWT, refresh tokens, biometría, OTP adaptativo y auditoría;
- capacidades gratuitas y premium con Google Play Billing en ambiente de prueba;
- operación offline para el subconjunto privado compatible, con SQLite cifrada, outbox local y sincronización incremental.

El creador de un grupo recibe el rol `propietario`. Puede delegar administración operativa mediante el rol `administrador`, pero continúa siendo el único autorizado para transferir la propiedad o eliminar el grupo. Los integrantes operan únicamente dentro de los permisos asociados a su ámbito y rol.

## 3. Plataforma e infraestructura del piloto

| Componente | Decisión |
|---|---|
| Aplicación | Flutter para Android 10 o superior |
| Backend | ASP.NET Core .NET 10 |
| Persistencia del piloto | PostgreSQL administrado mediante Supabase Pro |
| Persistencia local | SQLite cifrada para el subconjunto offline |
| Archivos | Amazon S3 privado |
| Caché | No requerida para el piloto; PostgreSQL es la fuente de verdad |
| Correo | Amazon SES |
| OCR | Amazon Textract AnalyzeExpense |
| Push | Firebase Cloud Messaging |
| Facturación | Google Play Billing con license testers; sin cobros reales en el piloto |
| Servidor | AWS Lightsail en North Virginia |
| Contenedores | Docker Compose |
| Supervisión | systemd |
| Reverse proxy | Nginx |
| DNS | Cloudflare |
| Distribución | Google Play Internal Testing |

PostgreSQL y MinIO pueden utilizarse como dependencias locales de desarrollo. Redis no se despliega mientras las mediciones no demuestren su necesidad. La aplicación Flutter nunca accede directamente a Supabase o S3: toda autorización y regla de negocio pasa por la API.

## 4. Exclusiones

Quedan fuera del alcance inicial:

- integración directa con bancos o billeteras;
- transferencias monetarias reales;
- administración de fondos bancarios;
- almacenamiento de emisor, últimos cuatro dígitos, PAN, CVV, expiración, nombre impreso, token bancario o cualquier otro dato del plástico;
- cobros reales durante el piloto;
- score crediticio o consulta de centrales de riesgo;
- aplicación para iOS;
- aplicación web para usuarios finales;
- panel web administrativo;
- entrenamiento de modelos complejos de inteligencia artificial;
- machine learning no interpretable;
- motor antifraude avanzado;
- alta disponibilidad distribuida;
- tolerancia avanzada a fallos;
- validación estadística poblacional.

Las transferencias documentadas por la API representan movimientos contables entre cuentas del mismo usuario. La caja familiar es igualmente una representación lógica interna y no mueve dinero real. iOS se considera únicamente una posible evolución posterior y no forma parte de la validación de esta tesis.

## 5. Requisitos funcionales

| ID | Requisito |
|---|---|
| RF-01 | Registrar usuarios activos e iniciar sesión mediante credenciales seguras |
| RF-02 | Recuperar y cambiar la contraseña mediante mecanismos temporales y de un solo uso |
| RF-03 | Crear grupos familiares y asignar automáticamente el rol de propietario |
| RF-04 | Invitar, aceptar, excluir integrantes, transferir propiedad y eliminar grupos según permisos |
| RF-05 | Registrar ingresos y gastos en ámbitos privado y familiar |
| RF-06 | Registrar, modificar, consultar y anular transacciones manuales |
| RF-07 | Cargar imágenes o PDF y procesar comprobantes mediante OCR |
| RF-08 | Importar y procesar archivos XML de comprobantes electrónicos SIFEN |
| RF-09 | Revisar, corregir y confirmar datos detectados antes de crear el movimiento |
| RF-10 | Gestionar categorías predefinidas y personalizables |
| RF-11 | Gestionar presupuestos por categoría privados y familiares |
| RF-12 | Gestionar metas de ahorro privadas y compartidas, con aportes y seguimiento |
| RF-13 | Gestionar una caja compartida lógica con aportes, retiros y trazabilidad |
| RF-14 | Visualizar dashboards y reportes individuales y familiares por periodos |
| RF-15 | Exportar información en PDF y XLSX, con CSV como formato adicional |
| RF-16 | Generar proyecciones mensuales y por categoría a partir del histórico |
| RF-17 | Generar alertas, recomendaciones y observaciones explicables |
| RF-18 | Permitir autenticación biométrica en dispositivos compatibles |
| RF-19 | Solicitar OTP en acciones sensibles o escenarios de riesgo alto |
| RF-20 | Registrar eventos de seguridad y auditoría de operaciones críticas |
| RF-21 | Gestionar planes gratuitos y premium, suscripciones, renovaciones y vencimientos |
| RF-22 | Gestionar cuentas financieras internas y sus saldos |
| RF-23 | Gestionar tarjetas de crédito mediante alias, sin almacenar PAN completo ni CVV |
| RF-24 | Registrar transferencias contables internas entre cuentas del mismo usuario |
| RF-25 | Configurar y ejecutar movimientos recurrentes sin duplicación por periodo |
| RF-26 | Consultar y registrar información financiera privada sin conexión, mantener una cola local cifrada y sincronizar cambios al recuperar conectividad |

La trazabilidad detallada está en [`api/TRAZABILIDAD_RF.md`](api/TRAZABILIDAD_RF.md).

## 6. Requisitos no funcionales

| ID | Requisito |
|---|---|
| RNF-01 | Protección de información personal, financiera y documental |
| RNF-02 | JWT y refresh tokens con expiración, rotación y revocación |
| RNF-03 | Integridad y consistencia entre datos, documentos y operaciones |
| RNF-04 | Rendimiento adecuado en operaciones principales |
| RNF-05 | OCR con impacto aceptable en la experiencia |
| RNF-06 | Interfaz comprensible y adaptable en Android |
| RNF-07 | Separación clara entre información privada y familiar |
| RNF-08 | Almacenamiento documental externo, privado y autorizado |
| RNF-09 | Base de datos relacional PostgreSQL |
| RNF-10 | Caché para consultas repetitivas cuando las métricas justifiquen su necesidad |
| RNF-11 | Arquitectura modular, mantenible, testeable y ampliable |
| RNF-12 | Despliegue VPS mediante contenedores y Docker Compose |
| RNF-13 | Reverse proxy para exposición controlada |
| RNF-14 | Escalabilidad progresiva |
| RNF-15 | Suficiencia funcional para el piloto |
| RNF-16 | OCR promedio no mayor a 5 segundos |
| RNF-17 | Consultas principales no mayores a 3 segundos |
| RNF-18 | Diez usuarios simultáneos sin degradación crítica |
| RNF-19 | Disponibilidad mínima del 95 %, excluyendo mantenimiento programado |
| RNF-20 | Autenticación no mayor a 2 segundos |
| RNF-21 | Backup automático al menos cada 24 horas |
| RNF-22 | Recuperación autorizada de archivos no mayor a 4 segundos |
| RNF-23 | Auditoría del 100 % de las operaciones críticas definidas |
| RNF-24 | Recuperación tras reinicio no mayor a 10 minutos |
| RNF-25 | Compatibilidad estable con Android 10 o superior |

Los umbrales son criterios de aceptación definidos para el proyecto y se verifican mediante las métricas, pruebas y evidencias documentadas en [`api/TRAZABILIDAD_RNF.md`](api/TRAZABILIDAD_RNF.md).
