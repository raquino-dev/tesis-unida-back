# Diagramas de casos de uso de Finanza

Los ocho diagramas conservan los actores, identificadores y nombres de los casos de uso proporcionados. Las relaciones sin semántica explícita de los originales se revisaron aplicando:

- `<<include>>`: el caso base siempre ejecuta el comportamiento incluido.
- `<<extend>>`: el comportamiento ocurre solamente bajo una condición o elección.

## Diagramas

1. `01-identidad-acceso.puml`
2. `02-finanzas-personales.puml`
3. `03-finanzas-familiares.puml`
4. `04-documentos-ocr.puml`
5. `05-analitica-financiera.puml`
6. `06-seguridad-adaptativa.puml`
7. `07-suscripciones.puml`
8. `08-sincronizacion-offline.puml`

## Decisiones de modelado

### Identidad y acceso

- Recuperar o cambiar la contraseña incluye la validación OTP.
- La biometría y el segundo factor extienden la autenticación cuando están habilitados o son requeridos.

### Finanzas personales

- Registrar un movimiento es una extensión de su gestión general.
- Crear categorías, cuentas o recurrencias durante el registro es opcional.
- Presupuestos reutiliza la gestión de categorías y transferencias reutiliza la gestión de cuentas.

### Finanzas familiares

- Administrar integrantes puede continuar opcionalmente a la creación del grupo.
- Toda operación de caja compartida incluye el registro del movimiento familiar correspondiente.

### Documentos y OCR

- Procesar imágenes/PDF e importar XML SIFEN incluyen la revisión y confirmación del resultado.
- La exportación es una acción opcional posterior a la confirmación.

### Analítica financiera

- Proyecciones y detalle de presupuesto amplían opcionalmente la consulta del dashboard.

### Seguridad adaptativa

- Biometría y OTP amplían la autenticación únicamente cuando las condiciones de seguridad lo requieren.

### Suscripciones

- Gestionar la suscripción siempre registra auditoría.
- La validación sensible y la gestión iniciada desde una función premium son extensiones condicionadas.

### Sincronización sin conexión

- El registro offline amplía las operaciones compatibles únicamente cuando no hay conectividad.

## Renderizar

Con PlantUML instalado:

```bash
java -jar plantuml.jar -tsvg docs/uml/casos-uso/*.puml
java -jar plantuml.jar -tpng docs/uml/casos-uso/*.puml
```

