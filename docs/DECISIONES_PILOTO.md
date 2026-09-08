# Decisiones consolidadas para el piloto

**Estado:** aprobado para la fase de cierre de tesis  
**Última alineación:** septiembre de 2026  
**Ámbito:** aplicación Android Finanzas Inteligentes y backend .NET

Este documento registra las decisiones consolidadas que deben permanecer coherentes con la tesis, el código, los instrumentos, la evidencia y la defensa.

## 1. Diseño del piloto

| Aspecto | Decisión |
|---|---|
| Lugar | Asunción, Paraguay |
| Muestra principal | 10 unidades familiares, cada una representada por un adulto referente (`n = 10`) |
| Incorporación | Escalonada entre el 17/08/2026 y el 31/08/2026 |
| Duración | 28 días consecutivos por unidad familiar desde su incorporación |
| Plataforma | Android 10 o superior |
| Distribución | Google Play Internal Testing |
| Datos | Datos aportados voluntariamente, sin conexión bancaria |
| Instrumentos | Consentimiento, preuso, bloque complementario descriptivo, tareas/métricas y postuso |
| Moneda | PYG |
| Retiro | El participante puede retirarse; la cuenta solo se elimina cuando el propio usuario ejecuta la eliminación prevista en la aplicación |

Los diez referentes completaron el preuso principal antes de su primer uso. Los demás adultos de las familias pueden participar en funciones colaborativas, sin incorporarse automáticamente como observaciones independientes del análisis pre/post.

La evaluación no pretende obtener inferencia estadística poblacional ni demostrar causalidad. Busca validar funcionamiento técnico, usabilidad y cambios observados dentro del grupo piloto.

## 2. Alcance funcional consolidado

La referencia funcional es RF-01 a RF-26. Incluye identidad y seguridad, finanzas privadas y familiares, OCR/XML SIFEN, analítica explicable, suscripciones, cuentas, tarjetas por alias, transferencias contables, recurrencias y funcionamiento offline del subconjunto privado.

### Roles

- globales: `usuario` y `administrador`;
- familiares: `propietario`, `administrador` e `integrante`.

### Tarjetas

Las tarjetas se identifican mediante alias. No se solicitan ni persisten PAN, CVV, últimos cuatro dígitos, expiración, nombre impreso, token bancario ni otros datos del plástico.

### Offline

RF-26 forma parte del alcance de defensa. El subconjunto privado compatible utiliza SQLite cifrada, outbox local, UUID definitivos, `Idempotency-Key`, `If-Match`, cursor incremental y tombstones. Las capacidades dependientes de proveedores o colaboración familiar requieren conectividad.

## 3. Suscripciones y monetización

La propuesta comercial aprobada contempla:

| Plan | Precio |
|---|---:|
| Premium mensual | Gs. 39.000 |
| Premium anual | Gs. 390.000 |

Google Play Billing se utiliza con productos mensual y anual y license testers. Durante el piloto **no existen cobros reales**. La compra, verificación y ciclo de derechos se prueban técnicamente mediante el ambiente de prueba de Google Play.

El mecanismo de pago y comprobante fue solicitado verbalmente por tutoría. El comprobante de la transacción corresponde al emitido por Google Play; la aplicación no emite factura tributaria propia durante el piloto.

## 4. Instrumentos

El preuso principal permanece como medición basal. No se modifica retrospectivamente.

Por indicación tutorial se incorpora un instrumento independiente `preuso-complementario` para:

- intención de compra;
- valoración de cuestiones técnicas;
- disposición de compra.

Este bloque se analiza descriptivamente y no se utiliza como variable pre/post porque fue incorporado después de que los participantes completaran el preuso principal.

El consentimiento se actualiza a una segunda aceptación que explicita los **28 días de observación desde la incorporación individual**. Se conserva el registro histórico de la aceptación anterior.

## 5. Análisis predictivo

Los participantes pueden registrar movimientos históricos anteriores al inicio del piloto. No es obligatorio disponer de tres meses. Cuando el histórico es inferior a tres meses, la aplicación debe identificar la proyección como preliminar y continuar mostrando una explicación comprensible.

## 6. Infraestructura real del piloto

| Capacidad | Decisión/estado operativo |
|---|---|
| Aplicación | Flutter, Android 10+ |
| Backend | ASP.NET Core .NET 10, API + Worker |
| Base de datos | Supabase PostgreSQL Pro |
| Archivos | Amazon S3 privado |
| OCR | Amazon Textract AnalyzeExpense |
| Correo | Amazon SES con envío transaccional real |
| Push | Firebase Cloud Messaging |
| Facturación | Google Play Billing en ambiente de prueba |
| Servidor | AWS Lightsail, Ubuntu 24.04, North Virginia |
| Proxy | Nginx |
| DNS | Cloudflare |
| Distribución | Google Play Internal Testing |
| Caché externa | Redis no forma parte del despliegue base |

`api.rodrigoaquino.com` corresponde al ambiente productivo del piloto. PostgreSQL permanece como fuente de verdad.

## 7. Exclusiones

- iOS durante la tesis;
- aplicación web de usuario final;
- conexión directa con bancos o billeteras;
- transferencias reales de dinero;
- datos sensibles del plástico de tarjetas;
- cobros reales durante el piloto;
- score crediticio;
- machine learning opaco;
- microservicios distribuidos o Kubernetes para el piloto;
- inferencia estadística poblacional.

## 8. Criterios de aceptación del proyecto

Los siguientes umbrales son **criterios definidos por el proyecto**, no exigencias universales ni requisitos impuestos por UNIDA:

| Indicador | Meta de referencia |
|---|---:|
| Mejora de eficiencia del registro | 30 % respecto a referencia manual |
| Tareas principales | ≥ 80 % |
| Valoraciones favorables de facilidad/utilidad/seguridad | ≥ 80 % |
| Autenticación | ≤ 2 s |
| Consultas principales | ≤ 3 s |
| OCR promedio | ≤ 5 s |
| Recuperación de archivo | ≤ 4 s |
| Concurrencia | 10 usuarios sin degradación crítica |
| Disponibilidad | ≥ 95 % durante la ventana efectivamente medida |
| Auditoría de operaciones críticas definidas | 100 % |

La meta del 30 % se utiliza como referencia del proyecto; el análisis final informa el valor observado aunque quede por debajo de esa meta.

## 9. Evidencia a producir

- versión, commit, ambiente y periodo exacto;
- aceptación del consentimiento actualizado;
- instrumentos anonimizados;
- pruebas por categoría, diferenciando dobles/mocks de E2E desplegado;
- OCR con nuevo conjunto controlado y ground truth por campo;
- S3, Textract, SES, FCM, Billing y sincronización offline;
- disponibilidad y latencia medidas prospectivamente;
- backup y restauración ensayada;
- incidentes/hotfixes y builds usados en el piloto.

Las evidencias nunca deben publicar credenciales, tokens, OTP, datos financieros identificables o comprobantes reconocibles.

## 10. Historial relevante

- La incorporación de familias se realizó entre 17/08/2026 y 31/08/2026.
- Algunos participantes iniciaron en build `0.1.0+7`; `0.1.0+8` incorporó un fix de envío de correo sin cambio funcional mayor del instrumento.
- La evaluación OCR informal previa no se utilizará como resultado académico. Se ejecutará un nuevo benchmark controlado.
- El Acta de Entrega y Conformidad ya no forma parte de los requisitos institucionales vigentes para esta tesis.
