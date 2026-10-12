#!/usr/bin/env python3
"""Genera un DER DBML y Mermaid desde el snapshot vigente de Entity Framework Core."""

from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass, field
from pathlib import Path


ENTITY_BLOCK = re.compile(
    r'^ {12}modelBuilder\.Entity\("([^"]+)", b =>\r?\n'
    r'^ {16}\{\r?\n(.*?)^ {16}\}\);',
    re.MULTILINE | re.DOTALL,
)
PROPERTY = re.compile(
    r'b\.Property<([^\r\n]+?)>\("([^"]+)"\)(.*?);',
    re.DOTALL,
)

SCHEMA_COLORS = {
    "analitica": "#2A9D8F",
    "auditoria": "#7E57C2",
    "documentos": "#247BA0",
    "familias": "#4C956C",
    "finanzas": "#00A676",
    "identidad": "#136F63",
    "infra": "#607D8B",
    "piloto": "#E76F51",
    "seguridad": "#5C6BC0",
    "sincronizacion": "#78909C",
    "suscripciones": "#F4A261",
}


@dataclass
class Column:
    property_name: str
    name: str
    store_type: str
    nullable: bool
    generated: bool
    primary_key: bool = False
    unique: bool = False


@dataclass
class Index:
    name: str
    columns: list[str]
    unique: bool
    primary_key: bool = False


@dataclass
class Table:
    entity_name: str
    schema: str
    name: str
    columns: list[Column]
    property_to_column: dict[str, str]
    indexes: list[Index] = field(default_factory=list)

    @property
    def qualified_name(self) -> str:
        return f"{self.schema}.{self.name}"


@dataclass
class ForeignKey:
    name: str
    dependent: Table
    dependent_columns: list[str]
    principal: Table
    principal_columns: list[str]
    delete_action: str


@dataclass
class DatabaseSchema:
    tables: list[Table]
    foreign_keys: list[ForeignKey]

    def for_schema(self, schema_name: str) -> "DatabaseSchema":
        tables = [table for table in self.tables if table.schema == schema_name]
        table_names = {table.qualified_name for table in tables}
        foreign_keys = [
            foreign_key
            for foreign_key in self.foreign_keys
            if foreign_key.dependent.qualified_name in table_names
            and foreign_key.principal.qualified_name in table_names
        ]
        return DatabaseSchema(tables, foreign_keys)


def quoted_values(source: str) -> list[str]:
    return re.findall(r'"([^"]+)"', source)


def method_call(body: str, method: str) -> re.Match[str] | None:
    return re.search(rf'b\.{method}\((.*?)\)(.*?);', body, re.DOTALL)


def method_calls(body: str, method: str) -> list[re.Match[str]]:
    return list(re.finditer(rf'b\.{method}\((.*?)\)(.*?);', body, re.DOTALL))


def parse_snapshot(snapshot_path: Path) -> DatabaseSchema:
    source = snapshot_path.read_text(encoding="utf-8-sig")
    blocks: dict[str, list[str]] = {}
    for entity_name, body in ENTITY_BLOCK.findall(source):
        blocks.setdefault(entity_name, []).append(body)

    tables_by_entity: dict[str, Table] = {}
    for entity_name, entity_blocks in blocks.items():
        definition = next((body for body in entity_blocks if "b.Property<" in body and "b.ToTable(" in body), None)
        if definition is None:
            continue

        table_call = method_call(definition, "ToTable")
        if table_call is None:
            continue
        table_args = quoted_values(table_call.group(1))
        if not table_args:
            continue
        table_name = table_args[0]
        schema_name = table_args[1] if len(table_args) > 1 else "public"

        columns: list[Column] = []
        property_to_column: dict[str, str] = {}
        for clr_type, property_name, chain in PROPERTY.findall(definition):
            column_name_match = re.search(r'\.HasColumnName\("([^"]+)"\)', chain)
            store_type_match = re.search(r'\.HasColumnType\("([^"]+)"\)', chain)
            column_name = column_name_match.group(1) if column_name_match else property_name
            store_type = store_type_match.group(1) if store_type_match else normalize_clr_type(clr_type)
            reference_type = clr_type in {"string", "byte[]", "JsonDocument"}
            nullable = clr_type.endswith("?") or (reference_type and ".IsRequired()" not in chain)
            columns.append(
                Column(
                    property_name=property_name,
                    name=column_name,
                    store_type=store_type,
                    nullable=nullable,
                    generated=".ValueGeneratedOnAdd()" in chain,
                )
            )
            property_to_column[property_name] = column_name

        table = Table(entity_name, schema_name, table_name, columns, property_to_column)
        key_call = method_call(definition, "HasKey")
        if key_call:
            key_properties = quoted_values(key_call.group(1))
            key_columns = [property_to_column[name] for name in key_properties]
            for column in columns:
                column.primary_key = column.name in key_columns
            table.indexes.append(
                Index(
                    name=f"pk_{table_name}",
                    columns=key_columns,
                    unique=True,
                    primary_key=True,
                )
            )

        for sequence, index_call in enumerate(method_calls(definition, "HasIndex"), start=1):
            properties = quoted_values(index_call.group(1))
            index_columns = [property_to_column[name] for name in properties]
            chain = index_call.group(2)
            explicit_name = re.search(r'\.HasDatabaseName\("([^"]+)"\)', chain)
            index_name = explicit_name.group(1) if explicit_name else f"ix_{table_name}_{'_'.join(index_columns)}"
            is_unique = ".IsUnique()" in chain
            table.indexes.append(Index(index_name, index_columns, is_unique))
            if is_unique and len(index_columns) == 1:
                for column in columns:
                    if column.name == index_columns[0]:
                        column.unique = True

        tables_by_entity[entity_name] = table

    foreign_keys: list[ForeignKey] = []
    seen_foreign_keys: set[tuple[str, str, tuple[str, ...]]] = set()
    for entity_name, entity_blocks in blocks.items():
        dependent = tables_by_entity.get(entity_name)
        if dependent is None:
            continue
        for body in entity_blocks:
            for relation in re.finditer(
                r'b\.HasOne\("([^"]+)", null\)(.*?);',
                body,
                re.DOTALL,
            ):
                principal_entity = relation.group(1)
                chain = relation.group(2)
                principal = tables_by_entity.get(principal_entity)
                if principal is None:
                    continue
                foreign_key_match = re.search(r'\.HasForeignKey\((.*?)\)', chain, re.DOTALL)
                if foreign_key_match is None:
                    continue
                properties = quoted_values(foreign_key_match.group(1))
                if properties and "." in properties[0]:
                    properties = properties[1:]
                dependent_columns = [dependent.property_to_column[name] for name in properties]
                principal_key = next(index for index in principal.indexes if index.primary_key)
                signature = (dependent.qualified_name, principal.qualified_name, tuple(dependent_columns))
                if signature in seen_foreign_keys:
                    continue
                seen_foreign_keys.add(signature)
                delete_match = re.search(r'\.OnDelete\(DeleteBehavior\.([A-Za-z]+)\)', chain)
                delete_action = normalize_delete_action(delete_match.group(1) if delete_match else "NoAction")
                foreign_keys.append(
                    ForeignKey(
                        name=f"fk_{dependent.name}_{principal.name}_{'_'.join(dependent_columns)}",
                        dependent=dependent,
                        dependent_columns=dependent_columns,
                        principal=principal,
                        principal_columns=principal_key.columns,
                        delete_action=delete_action,
                    )
                )

    tables = sorted(tables_by_entity.values(), key=lambda table: (table.schema, table.name))
    foreign_keys.sort(key=lambda foreign_key: (foreign_key.dependent.qualified_name, foreign_key.name))
    return DatabaseSchema(tables, foreign_keys)


def normalize_clr_type(clr_type: str) -> str:
    return {
        "Guid": "uuid",
        "string": "text",
        "bool": "boolean",
        "int": "integer",
        "long": "bigint",
        "decimal": "numeric",
        "DateTime": "timestamp without time zone",
        "DateTimeOffset": "timestamp with time zone",
        "DateOnly": "date",
        "JsonDocument": "jsonb",
        "byte[]": "bytea",
    }.get(clr_type.rstrip("?"), clr_type.rstrip("?"))


def normalize_delete_action(value: str) -> str:
    return {
        "Cascade": "cascade",
        "Restrict": "restrict",
        "SetNull": "set null",
    }.get(value, "no action")


def dbml_identifier(value: str) -> str:
    if re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", value):
        return value
    return '"' + value.replace('"', '""') + '"'


def dbml_type(value: str) -> str:
    if re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*(\([0-9, ]+\))?(\[\])?", value):
        return value
    return '"' + value.replace('"', '\\"') + '"'


def dbml_endpoint(table: Table, columns: list[str]) -> str:
    prefix = f"{dbml_identifier(table.schema)}.{dbml_identifier(table.name)}."
    if len(columns) == 1:
        return prefix + dbml_identifier(columns[0])
    return prefix + "(" + ", ".join(dbml_identifier(column) for column in columns) + ")"


def write_dbml(schema: DatabaseSchema) -> str:
    lines = [
        "Project Finanza {",
        "  database_type: 'PostgreSQL'",
        "  Note: 'DER generado automáticamente desde FinanzasDbContextModelSnapshot'",
        "}",
        "",
    ]
    for table in schema.tables:
        color = SCHEMA_COLORS.get(table.schema, "#009B72")
        lines.append(f"Table {dbml_identifier(table.schema)}.{dbml_identifier(table.name)} [headerColor: {color}] {{")
        primary_key = next(index for index in table.indexes if index.primary_key)
        for column in table.columns:
            settings: list[str] = []
            if column.primary_key and len(primary_key.columns) == 1:
                settings.append("pk")
            if not column.nullable:
                settings.append("not null")
            if column.generated and re.fullmatch(r"smallint|integer|bigint|serial|bigserial", column.store_type, re.I):
                settings.append("increment")
            suffix = f" [{', '.join(settings)}]" if settings else ""
            lines.append(f"  {dbml_identifier(column.name)} {dbml_type(column.store_type)}{suffix}")

        remaining_indexes = [
            index
            for index in table.indexes
            if not (index.primary_key and len(index.columns) == 1)
        ]
        if remaining_indexes:
            lines.extend(["", "  indexes {"])
            for index in remaining_indexes:
                settings = [f"name: '{index.name}'"]
                if index.primary_key:
                    settings.append("pk")
                elif index.unique:
                    settings.append("unique")
                columns = ", ".join(dbml_identifier(column) for column in index.columns)
                lines.append(f"    ({columns}) [{', '.join(settings)}]")
            lines.append("  }")
        lines.extend(["}", ""])

    for foreign_key in schema.foreign_keys:
        dependent = dbml_endpoint(foreign_key.dependent, foreign_key.dependent_columns)
        principal = dbml_endpoint(foreign_key.principal, foreign_key.principal_columns)
        lines.append(
            f"Ref {dbml_identifier(foreign_key.name)}: {dependent} > {principal} "
            f"[delete: {foreign_key.delete_action}]"
        )
    lines.append("")

    schema_names = sorted({table.schema for table in schema.tables})
    for schema_name in schema_names:
        lines.append(f"TableGroup {dbml_identifier(schema_name)} {{")
        for table in schema.tables:
            if table.schema == schema_name:
                lines.append(f"  {dbml_identifier(table.schema)}.{dbml_identifier(table.name)}")
        lines.extend(["}", ""])
    return "\n".join(lines)


def mermaid_name(value: str) -> str:
    return re.sub(r"[^A-Za-z0-9_-]", "_", value)


def mermaid_entity(table: Table) -> str:
    return mermaid_name(f"{table.schema}__{table.name}")


def mermaid_type(value: str, nullable: bool) -> str:
    normalized = re.sub(r"[^A-Za-z0-9_()\[\]-]", "_", value)
    if not normalized[0].isalpha():
        normalized = "type_" + normalized
    return normalized + ("?" if nullable else "")


def write_mermaid(schema: DatabaseSchema) -> str:
    lines = [
        "---",
        "title: Finanza - Diagrama entidad relación",
        "config:",
        "  layout: elk",
        "---",
        "erDiagram",
        "  direction LR",
    ]
    for table in schema.tables:
        lines.append(f'  {mermaid_entity(table)}["{table.qualified_name}"] {{')
        for column in table.columns:
            keys = []
            if column.primary_key:
                keys.append("PK")
            if column.unique:
                keys.append("UK")
            key_suffix = " " + ",".join(keys) if keys else ""
            nullability = "nullable" if column.nullable else "required"
            lines.append(
                f"    {mermaid_type(column.store_type, column.nullable)} "
                f'{mermaid_name(column.name)}{key_suffix} "{nullability}"'
            )
        lines.append("  }")
    for foreign_key in schema.foreign_keys:
        lines.append(
            f"  {mermaid_entity(foreign_key.principal)} ||--o{{ "
            f'{mermaid_entity(foreign_key.dependent)} : "{foreign_key.name}"'
        )
    return "\n".join(lines) + "\n"


def write_table_overview(schema: DatabaseSchema) -> str:
    """Genera una vista de arquitectura con tablas y relaciones, sin columnas."""
    lines = [
        "---",
        "title: Finanza - Mapa de tablas",
        "config:",
        "  layout: elk",
        "  flowchart:",
        "    curve: basis",
        "---",
        "flowchart LR",
    ]

    for schema_name in sorted({table.schema for table in schema.tables}):
        lines.append(f'  subgraph schema_{mermaid_name(schema_name)}["{schema_name}"]')
        lines.append("    direction TB")
        for table in schema.tables:
            if table.schema == schema_name:
                lines.append(f'    {mermaid_entity(table)}["{table.name}"]')
        lines.append("  end")

    relationship_pairs = sorted(
        {
            (mermaid_entity(foreign_key.dependent), mermaid_entity(foreign_key.principal))
            for foreign_key in schema.foreign_keys
        }
    )
    for dependent, principal in relationship_pairs:
        lines.append(f"  {dependent} --> {principal}")

    for schema_name in sorted({table.schema for table in schema.tables}):
        color = SCHEMA_COLORS.get(schema_name, "#009B72")
        class_name = f"table_{mermaid_name(schema_name)}"
        lines.append(
            f"  classDef {class_name} fill:#ffffff,stroke:{color},stroke-width:2px,color:#0B2239"
        )
        entities = ",".join(
            mermaid_entity(table) for table in schema.tables if table.schema == schema_name
        )
        lines.append(f"  class {entities} {class_name}")

    lines.append("  linkStyle default stroke:#8AA39B,stroke-width:1px")
    return "\n".join(lines) + "\n"


def write_catalog(schema: DatabaseSchema) -> str:
    lines = [
        "# Catálogo de tablas de Finanza",
        "",
        "> Generado automáticamente desde `FinanzasDbContextModelSnapshot`. No editar manualmente.",
        "",
        f"- **Tablas:** {len(schema.tables)}",
        f"- **Columnas:** {sum(len(table.columns) for table in schema.tables)}",
        f"- **Relaciones:** {len(schema.foreign_keys)}",
        f"- **Índices:** {sum(len(table.indexes) for table in schema.tables)}",
        "",
    ]
    for schema_name in sorted({table.schema for table in schema.tables}):
        lines.extend([f"## Esquema `{schema_name}`", ""])
        for table in schema.tables:
            if table.schema != schema_name:
                continue
            lines.extend(
                [
                    f"### `{table.qualified_name}`",
                    "",
                    "| Campo | Tipo PostgreSQL | Nulo | Claves |",
                    "|---|---|:---:|---|",
                ]
            )
            for column in table.columns:
                keys = []
                if column.primary_key:
                    keys.append("PK")
                if column.unique:
                    keys.append("UK")
                lines.append(
                    f"| `{column.name}` | `{column.store_type}` | "
                    f"{'Sí' if column.nullable else 'No'} | {', '.join(keys)} |"
                )
            lines.append("")
    return "\n".join(lines)


def write_html(mermaid_source: str, title: str = "Finanza · Diagrama entidad relación") -> str:
    escaped = mermaid_source.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
    return f"""<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{title}</title>
  <style>
    body {{ margin: 0; font-family: system-ui, sans-serif; color: #0b2239; background: #f4fffb; }}
    header {{ position: sticky; top: 0; z-index: 1; padding: 16px 24px; background: #006b4f; color: white; }}
    header h1 {{ margin: 0; font-size: 20px; }}
    header p {{ margin: 4px 0 0; opacity: .85; }}
    main {{ min-width: max-content; padding: 24px; }}
    .mermaid {{ background: white; border: 1px solid #cde9df; border-radius: 16px; padding: 20px; }}
  </style>
</head>
<body>
  <header><h1>{title}</h1><p>Generado desde el snapshot vigente de Entity Framework Core</p></header>
  <main><pre class="mermaid">{escaped}</pre></main>
  <script type="module">
    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
    mermaid.initialize({{ startOnLoad: true, layout: 'elk', theme: 'neutral', securityLevel: 'loose' }});
  </script>
</body>
</html>
"""


def write_readme(stats: dict[str, object]) -> str:
    schemas = ", ".join(f"`{name}`" for name in stats["schemas"])
    return f"""# Diagrama entidad relación de Finanza

Estos archivos se generan automáticamente desde el snapshot vigente de Entity Framework Core.

## Inventario

- **Tablas:** {stats["tables"]}
- **Columnas:** {stats["columns"]}
- **Relaciones:** {stats["relationships"]}
- **Índices:** {stats["indexes"]}
- **Esquemas:** {schemas}

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
"""


def validate(schema: DatabaseSchema) -> None:
    if not schema.tables:
        raise ValueError("El snapshot no contiene tablas mapeadas.")
    qualified_names = [table.qualified_name for table in schema.tables]
    if len(qualified_names) != len(set(qualified_names)):
        raise ValueError("Se detectaron tablas duplicadas.")
    for table in schema.tables:
        if not table.columns:
            raise ValueError(f"La tabla {table.qualified_name} no tiene columnas.")
        primary_keys = [index for index in table.indexes if index.primary_key]
        if len(primary_keys) != 1:
            raise ValueError(f"La tabla {table.qualified_name} no tiene exactamente una clave primaria.")
    for foreign_key in schema.foreign_keys:
        if len(foreign_key.dependent_columns) != len(foreign_key.principal_columns):
            raise ValueError(f"La relación {foreign_key.name} tiene cardinalidad de columnas inválida.")


def main() -> None:
    repository_root = Path(__file__).resolve().parents[1]
    default_snapshot = (
        repository_root
        / "backend/src/FinanzasInteligentes.Infraestructura/Persistencia/Migraciones/FinanzasDbContextModelSnapshot.cs"
    )
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--snapshot",
        type=Path,
        default=default_snapshot,
        help="Ruta a FinanzasDbContextModelSnapshot.cs",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=repository_root / "docs/database",
        help="Directorio de salida",
    )
    args = parser.parse_args()

    schema = parse_snapshot(args.snapshot.resolve())
    validate(schema)
    output = args.output.resolve()
    schemas_output = output / "schemas"
    schemas_output.mkdir(parents=True, exist_ok=True)

    complete_mermaid = write_mermaid(schema)
    table_overview = write_table_overview(schema)
    (output / "finanza-completo.dbml").write_text(write_dbml(schema), encoding="utf-8")
    (output / "finanza-completo.mmd").write_text(complete_mermaid, encoding="utf-8")
    (output / "finanza-completo.html").write_text(write_html(complete_mermaid), encoding="utf-8")
    (output / "finanza-tablas.mmd").write_text(table_overview, encoding="utf-8")
    (output / "finanza-tablas.html").write_text(
        write_html(table_overview, "Finanza · Mapa de tablas"),
        encoding="utf-8",
    )
    (output / "catalogo-tablas.md").write_text(write_catalog(schema), encoding="utf-8")

    for schema_name in sorted({table.schema for table in schema.tables}):
        partial = schema.for_schema(schema_name)
        mermaid = write_mermaid(partial)
        (schemas_output / f"{schema_name}.dbml").write_text(write_dbml(partial), encoding="utf-8")
        (schemas_output / f"{schema_name}.mmd").write_text(mermaid, encoding="utf-8")
        (schemas_output / f"{schema_name}.html").write_text(write_html(mermaid), encoding="utf-8")

    stats = {
        "tables": len(schema.tables),
        "columns": sum(len(table.columns) for table in schema.tables),
        "relationships": len(schema.foreign_keys),
        "indexes": sum(len(table.indexes) for table in schema.tables),
        "schemas": sorted({table.schema for table in schema.tables}),
    }
    (output / "schema-stats.json").write_text(json.dumps(stats, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    (output / "README.md").write_text(write_readme(stats), encoding="utf-8")
    print(
        f"DER generado en {output}: {stats['tables']} tablas, {stats['columns']} columnas, "
        f"{stats['relationships']} relaciones y {stats['indexes']} índices."
    )


if __name__ == "__main__":
    main()
