# Diagrama entidad relación de Finanza

Estos archivos se generan automáticamente desde el snapshot vigente de Entity Framework Core.

## Inventario

- **Tablas:** 50
- **Columnas:** 526
- **Relaciones:** 68
- **Índices:** 142
- **Esquemas:** `analitica`, `auditoria`, `documentos`, `familias`, `finanzas`, `identidad`, `infra`, `piloto`, `seguridad`, `sincronizacion`, `suscripciones`

## Archivos

- `finanza-completo.dbml`: diagrama completo para importar en [dbdiagram.io](https://dbdiagram.io/).
- `finanza-completo.mmd`: diagrama completo en Mermaid ER.
- `finanza-completo.html`: visor Mermaid; requiere conexión para cargar la librería desde CDN.
- `finanza-tablas.mmd`: mapa general con tablas y relaciones, sin mostrar campos.
- `finanza-tablas.html`: visor del mapa general sin campos.
- `catalogo-tablas.md`: inventario navegable de tablas, campos y tipos PostgreSQL.
- `schemas/`: diagramas DBML, Mermaid y HTML separados por esquema.
- `schema-stats.json`: conteos utilizados para validar la generación.

## Regenerar

Desde la raíz del repositorio:

```bash
python3 scripts/generate_database_diagram.py
```

La herramienta valida que cada tabla tenga columnas y una clave primaria, y que las relaciones tengan el mismo número de columnas en ambos extremos.
