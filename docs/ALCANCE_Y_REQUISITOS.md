# Alcance y requisitos del sistema

Este documento consolida el alcance formal del proyecto para la aplicación móvil Finanzas Inteligentes y funciona como referencia de producto para el contrato API, la arquitectura y el plan de implementación. Las decisiones cerradas de proveedores, piloto y criterios de éxito se encuentran en [`DECISIONES_PILOTO.md`](DECISIONES_PILOTO.md).

## 1. Objetivo y validación

El proyecto comprende el diseño e implementación de una aplicación móvil Android orientada a la gestión inteligente de gastos personales y al control financiero familiar.

La validación se realizará en Asunción durante 2026 mediante una prueba piloto controlada de 28 días con diez usuarios adultos. Se utilizarán encuestas antes y después del uso para evaluar:

- facilidad de uso;
- utilidad percibida;
- percepción de seguridad;
- apoyo en el control financiero;
- aceptación general;
- reducción del tiempo requerido para registrar gastos.

## 2. Alcance funcional

La aplicación diferenciará finanzas privadas y familiares e incluirá:

- ingresos y gastos privados y familiares;
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
- exportación obligatoria en PDF y Excel (`xlsx`), con CSV como formato adicional;
- proyecciones mensuales y por categoría;
- alertas, recomendaciones y observaciones explicables;
- indicador de salud financiera versionado y explicable, sin finalidad crediticia;
- notificaciones push transaccionales mediante Firebase Cloud Messaging;
- marca `preliminar` cuando la proyección use menos de tres meses de histórico;
- categorías predefinidas y personalizables;
- grupos, invitaciones por identificador o correo, integrantes y eliminación;
- JWT, refresh tokens, biometría, OTP adaptativo y auditoría básica;
- capacidades gratuitas y premium con Google Play Billing en ambiente de prueba.

El creador de un grupo recibe el rol `propietario`, que representa al administrador principal mencionado en los requisitos. Puede delegar administración operativa mediante el rol `administrador`, pero continúa siendo el único autorizado para transferir la propiedad o eliminar el grupo.

## 3. Plataforma e infraestructura del piloto

| Componente | Decisión |
|---|---|
| Aplicación | Flutter para Android 10 o superior |
| Backend | ASP.NET Core .NET 10 |
| Persistencia del piloto | PostgreSQL administrado mediante Supabase |
| Archivos | Amazon S3 privado |
| Caché | Redis |
| Correo | Amazon SES |
| OCR | Amazon Textract AnalyzeExpense, condicionado a evaluación local |
| Push | Firebase Cloud Messaging |
| Facturación | Google Play Billing con license testers |
| Servidor | VPS Hetzner CX33 |
| Contenedores | Docker Compose |
| Supervisión | systemd |
| Reverse proxy | Nginx |
| DNS/protección externa | Cloudflare |
| Distribución | Google Play Internal Testing |

PostgreSQL, Redis y MinIO en Docker Compose son dependencias locales de desarrollo. MinIO sustituye a S3 únicamente en entornos locales. La aplicación Flutter nunca accede directamente a Supabase, Redis o S3: toda autorización y regla de negocio pasa por la API.

## 4. Exclusiones

Quedan fuera del alcance inicial:

- integración directa con bancos o billeteras;
- transferencias monetarias reales;
- administración de fondos bancarios;
- almacenamiento de emisor, últimos cuatro dígitos, PAN, CVV, expiración, nombre impreso, token bancario o cualquier dato del plástico;
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

Las transferencias documentadas por la API representan movimientos contables entre cuentas del mismo usuario. La caja familiar es igualmente una representación lógica interna y no mueve dinero real.

## 5. Requisitos funcionales

| ID | Requisito |
|---|---|
| RF-01 | Registro e inicio de sesión mediante credenciales seguras |
| RF-02 | Recuperación y cambio de contraseña |
| RF-03 | Creación de grupos y asignación automática del propietario/administrador principal |
| RF-04 | Invitación, exclusión de miembros y eliminación del grupo |
| RF-05 | Cuentas, tarjetas por alias, transferencias internas, recurrencias, ingresos y gastos en ámbitos privado y familiar |
| RF-06 | Registro manual de transacciones |
| RF-07 | Registro mediante imagen/PDF y OCR |
| RF-08 | Registro mediante XML de comprobantes electrónicos |
| RF-09 | Validación, corrección y completado de datos OCR/XML |
| RF-10 | Categorías predefinidas y personalizables |
| RF-11 | Presupuestos por categoría privados y familiares |
| RF-12 | Metas de ahorro privadas y compartidas |
| RF-13 | Caja compartida con aportes y retiros |
| RF-14 | Dashboards y reportes individuales y familiares |
| RF-15 | Exportaciones PDF y Excel |
| RF-16 | Proyecciones mensuales y por categoría |
| RF-17 | Alertas, push transaccional, indicador de salud financiera, recomendaciones y observaciones comprensibles |
| RF-18 | Autenticación biométrica |
| RF-19 | OTP en acciones sensibles o riesgo alto |
| RF-20 | Eventos de seguridad y auditoría básica |
| RF-21 | Capacidades gratuitas y premium mediante Google Play Billing en ambiente de prueba |

La trazabilidad detallada está en [`api/TRAZABILIDAD_RF.md`](api/TRAZABILIDAD_RF.md).

## 6. Requisitos no funcionales

| ID | Requisito |
|---|---|
| RNF-01 | Protección de información personal, financiera y documental |
| RNF-02 | JWT y refresh tokens |
| RNF-03 | Integridad y consistencia entre datos y documentos |
| RNF-04 | Rendimiento adecuado en operaciones principales |
| RNF-05 | OCR con impacto aceptable en la experiencia |
| RNF-06 | Interfaz comprensible para usuarios con conocimientos financieros básicos |
| RNF-07 | Separación clara entre información privada y familiar |
| RNF-08 | Almacenamiento documental externo y seguro |
| RNF-09 | Base de datos relacional |
| RNF-10 | Caché para consultas repetitivas cuando sea necesario |
| RNF-11 | Arquitectura modular, mantenible y ampliable |
| RNF-12 | Despliegue VPS mediante contenedores y Docker Compose |
| RNF-13 | Reverse proxy para exposición controlada |
| RNF-14 | Escalabilidad progresiva |
| RNF-15 | Suficiencia funcional para el piloto de diez usuarios |
| RNF-16 | OCR promedio no mayor a 5 segundos |
| RNF-17 | Consultas principales no mayores a 3 segundos |
| RNF-18 | Diez usuarios simultáneos sin degradación crítica |
| RNF-19 | Disponibilidad mínima del 95 %, excluyendo mantenimiento programado |
| RNF-20 | Autenticación no mayor a 2 segundos |
| RNF-21 | Backup automático al menos cada 24 horas |
| RNF-22 | Recuperación autorizada de archivos no mayor a 4 segundos |
| RNF-23 | Auditoría del 100 % de las operaciones críticas |
| RNF-24 | Recuperación tras reinicio no mayor a 10 minutos |
| RNF-25 | Compatibilidad estable con Android 10 o superior |

Las métricas, pruebas y evidencias están en [`api/TRAZABILIDAD_RNF.md`](api/TRAZABILIDAD_RNF.md).
