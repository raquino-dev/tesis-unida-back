# Colecciones Postman de Finanzas Inteligentes

## Archivos

- `FinanzasInteligentes-Paso9.postman_collection.json`: smoke test encadenado de
  registro, sesión, cuenta, categoría, movimientos, tablero y reporte.
- `FinanzasInteligentes-API-Completa.postman_collection.json`: catálogo generado
  con todas las operaciones publicadas por NSwag.
- `generate-full-collection.mjs`: generador sin dependencias externas.

## Importar y usar el catálogo completo

1. Inicie el entorno desde `backend/deploy`:

   ```powershell
   docker compose up -d
   ```

2. Importe `FinanzasInteligentes-API-Completa.postman_collection.json` en
   Postman.
3. Ejecute primero la colección `Paso9` para obtener datos relacionados y
   comprobar el flujo principal.
4. En la colección completa, revise las variables y complete los identificadores
   que requiera el módulo que desea probar.
5. Ejecute carpetas individuales. El catálogo completo no representa un único
   escenario secuencial porque varias operaciones requieren OTP, invitaciones,
   archivos o estados creados previamente.

La colección usa `http://localhost:8080` como `baseUrl` y almacena el JWT en
`accessToken`. Los parámetros query opcionales se importan desactivados.

## Solicitudes destructivas

Las operaciones `DELETE` son omitidas automáticamente por Collection Runner.
Para habilitarlas, cambie la variable de colección:

```text
allowDestructive = true
```

Vuelva a establecerla en `false` al terminar.

## Regenerar desde Swagger

Con la API iniciada, ejecute desde la raíz del repositorio:

```powershell
node docs/postman/generate-full-collection.mjs
```

También puede usar un documento OpenAPI JSON local:

```powershell
node docs/postman/generate-full-collection.mjs `
  ruta\swagger.json `
  docs\postman\FinanzasInteligentes-API-Completa.postman_collection.json
```

El generador crea carpetas por tag, autenticación Bearer, parámetros, cuerpos,
pruebas de códigos documentados, captura de `ETag` y variables de recursos
conocidos.
