import fs from "node:fs/promises";
import path from "node:path";

const HTTP_METHODS = new Set(["get", "post", "put", "patch", "delete"]);
const TAG_ORDER = [
  "Salud",
  "Usuarios",
  "Sesiones",
  "Perfil",
  "Preferencias",
  "Seguridad OTP",
  "Contraseñas",
  "Cuentas financieras",
  "Categorías",
  "Tarjetas de crédito",
  "Movimientos privados",
  "Movimientos recurrentes",
  "Transferencias internas entre cuentas",
  "Presupuestos privados",
  "Metas de ahorro",
  "Predicciones, alertas y score",
  "Dashboards y reportes",
  "Documentos, OCR y XML SIFEN",
  "Exportaciones",
  "Seguridad, OTP, biometría y auditoría",
  "Endpoints operativos del cliente",
  "Finanzas familiares",
  "Planes y suscripciones",
  "Configuración"
];

const RESPONSE_VARIABLES = new Map([
  ["POST /api/v1/usuarios", "usuarioId"],
  ["POST /api/v1/sesiones", "sesionId"],
  ["POST /api/v1/cuentas", "cuentaId"],
  ["POST /api/v1/categorias", "categoriaId"],
  ["POST /api/v1/tarjetas-credito", "tarjetaId"],
  ["POST /api/v1/movimientos", "movimientoId"],
  ["POST /api/v1/movimientos-recurrentes", "recurrenteId"],
  ["POST /api/v1/transferencias", "transferenciaId"],
  ["POST /api/v1/presupuestos", "presupuestoId"],
  ["POST /api/v1/metas-ahorro", "metaId"],
  ["POST /api/v1/documentos", "documentoId"],
  ["POST /api/v1/exportaciones", "exportacionId"],
  ["POST /api/v1/dispositivos", "dispositivoId"],
  ["POST /api/v1/grupos-familiares", "grupoId"],
  ["POST /api/v1/grupos-familiares/{grupoId}/invitaciones", "invitacionId"],
  ["POST /api/v1/grupos-familiares/{grupoId}/categorias", "categoriaFamiliarId"],
  ["POST /api/v1/grupos-familiares/{grupoId}/movimientos", "movimientoFamiliarId"],
  ["POST /api/v1/grupos-familiares/{grupoId}/caja-compartida/operaciones", "operacionCajaId"],
  ["POST /api/v1/grupos-familiares/{grupoId}/presupuestos", "presupuestoFamiliarId"],
  ["POST /api/v1/grupos-familiares/{grupoId}/metas-ahorro", "metaFamiliarId"]
]);

const ID_ALIASES = new Map([
  ["alertaId", "alertaId"],
  ["aporteId", "aporteId"],
  ["categoriaId", "categoriaId"],
  ["cuentaId", "cuentaId"],
  ["dispositivoId", "dispositivoId"],
  ["documentoId", "documentoId"],
  ["exportacionId", "exportacionId"],
  ["grupoFamiliarId", "grupoId"],
  ["grupoId", "grupoId"],
  ["integranteId", "integranteId"],
  ["invitacionId", "invitacionId"],
  ["metaId", "metaId"],
  ["movimientoId", "movimientoId"],
  ["operacionId", "operacionCajaId"],
  ["presupuestoId", "presupuestoId"],
  ["procesamientoId", "procesamientoId"],
  ["recurrenteId", "recurrenteId"],
  ["sesionId", "sesionId"],
  ["tarjetaId", "tarjetaId"],
  ["transferenciaId", "transferenciaId"],
  ["usuarioId", "usuarioId"],
  ["verificacionOtpId", "verificacionOtpId"]
]);

const source = process.argv[2] ?? "http://localhost:8080/swagger/v1/swagger.json";
const output = process.argv[3] ??
  path.resolve("docs/postman/FinanzasInteligentes-API-Completa.postman_collection.json");
const document = await readOpenApi(source);
const schemas = document.components?.schemas ?? {};
const discoveredVariables = new Set([
  "accessToken",
  "refreshToken",
  "ifMatch",
  "testEmail",
  "testPassword",
  "currentDate"
]);
const folders = new Map();
let operationCount = 0;

for (const [route, pathItem] of Object.entries(document.paths ?? {})) {
  const sharedParameters = pathItem.parameters ?? [];
  for (const [method, operation] of Object.entries(pathItem)) {
    if (!HTTP_METHODS.has(method)) continue;
    operationCount++;
    const tag = operation.tags?.[0] ?? "Sin tag";
    if (!folders.has(tag)) folders.set(tag, []);
    folders.get(tag).push(createRequest(route, method, operation, sharedParameters));
  }
}

const orderedTags = [
  ...TAG_ORDER.filter(tag => folders.has(tag)),
  ...[...folders.keys()].filter(tag => !TAG_ORDER.includes(tag)).sort()
];
const collection = {
  info: {
    _postman_id: "e35c3983-46fb-4e09-9ee8-104cb77fbdc2",
    name: "Finanzas Inteligentes API - Catálogo completo",
    description:
      `Colección generada desde NSwag con ${operationCount} operaciones. ` +
      "Los DELETE se omiten en Collection Runner mientras allowDestructive no sea true. " +
      "Para un flujo automático inicial use también la colección FinanzasInteligentes-Paso9.",
    schema: "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  auth: {
    type: "bearer",
    bearer: [{ key: "token", value: "{{accessToken}}", type: "string" }]
  },
  event: [{
    listen: "prerequest",
    script: {
      type: "text/javascript",
      exec: [
        "if (!pm.collectionVariables.get('currentDate')) {",
        "    pm.collectionVariables.set('currentDate', new Date().toISOString().slice(0, 10));",
        "    const future = new Date();",
        "    future.setFullYear(future.getFullYear() + 1);",
        "    pm.collectionVariables.set('futureDate', future.toISOString().slice(0, 10));",
        "}"
      ]
    }
  }],
  item: orderedTags.map(tag => ({
    name: tag,
    description: `${folders.get(tag).length} operaciones generadas desde Swagger.`,
    item: folders.get(tag)
  })),
  variable: createVariables()
};

await fs.mkdir(path.dirname(output), { recursive: true });
await fs.writeFile(output, `${JSON.stringify(collection, null, 2)}\n`, "utf8");
console.log(`Colección generada: ${output}`);
console.log(`Operaciones: ${operationCount}; carpetas: ${orderedTags.length}; variables: ${collection.variable.length}`);

function createRequest(route, method, operation, sharedParameters) {
  const upperMethod = method.toUpperCase();
  const parameters = [...sharedParameters, ...(operation.parameters ?? [])]
    .map(resolveParameter);
  const url = createUrl(route, parameters);
  const headers = parameters
    .filter(parameter => parameter.in === "header")
    .map(parameter => ({
      key: parameter.name,
      value: headerValue(parameter.name),
      type: "text",
      disabled: !parameter.required
    }));
  const bodyResult = createBody(operation.requestBody);
  if (bodyResult.contentType && !headers.some(header => header.key.toLowerCase() === "content-type")) {
    headers.push({ key: "Content-Type", value: bodyResult.contentType, type: "text" });
  }

  const documentedCodes = Object.keys(operation.responses ?? {})
    .filter(code => /^\d{3}$/.test(code))
    .map(Number);
  const responseVariable = RESPONSE_VARIABLES.get(`${upperMethod} ${route}`);
  if (responseVariable) discoveredVariables.add(responseVariable);
  const requestEvents = [];
  if (upperMethod === "DELETE") {
    requestEvents.push({
      listen: "prerequest",
      script: {
        type: "text/javascript",
        exec: [
          "if (pm.collectionVariables.get('allowDestructive') !== 'true') {",
          "    console.warn('Solicitud DELETE omitida. Configure allowDestructive=true para habilitarla.');",
          "    pm.execution.skipRequest();",
          "}"
        ]
      }
    });
  }
  if (route === "/api/v1/usuarios" && upperMethod === "POST") {
    requestEvents.push({
      listen: "prerequest",
      script: {
        type: "text/javascript",
        exec: [
          "const unique = `${Date.now()}-${Math.floor(Math.random() * 100000)}`;",
          "pm.collectionVariables.set('testEmail', `postman.${unique}@example.com`);"
        ]
      }
    });
  }
  requestEvents.push({
    listen: "test",
    script: {
      type: "text/javascript",
      exec: createTestScript(documentedCodes, route, upperMethod, responseVariable)
    }
  });

  const request = {
    method: upperMethod,
    header: headers,
    url,
    description: createDescription(operation, documentedCodes, upperMethod)
  };
  if (!operation.security?.length) request.auth = { type: "noauth" };
  if (bodyResult.body) request.body = bodyResult.body;

  return {
    name: `${upperMethod} ${operation.summary || route}`,
    event: requestEvents,
    request,
    response: []
  };
}

function createUrl(route, parameters) {
  let renderedRoute = route;
  for (const match of route.matchAll(/\{([^}]+)\}/g)) {
    const variable = variableName(match[1]);
    discoveredVariables.add(variable);
    renderedRoute = renderedRoute.replace(match[0], `{{${variable}}}`);
  }
  const query = parameters
    .filter(parameter => parameter.in === "query")
    .map(parameter => ({
      key: parameter.name,
      value: parameterValue(parameter),
      disabled: !parameter.required
    }));
  const rawQuery = query.filter(item => !item.disabled)
    .map(item => `${encodeURIComponent(item.key)}=${item.value}`).join("&");
  return {
    raw: `{{baseUrl}}${renderedRoute}${rawQuery ? `?${rawQuery}` : ""}`,
    host: ["{{baseUrl}}"],
    path: renderedRoute.split("/").filter(Boolean),
    ...(query.length ? { query } : {})
  };
}

function createBody(requestBody) {
  if (!requestBody) return {};
  const resolvedBody = resolveReference(requestBody);
  const entries = Object.entries(resolvedBody.content ?? {});
  if (!entries.length) return {};
  const [contentType, media] =
    entries.find(([type]) => type === "application/json") ??
    entries.find(([type]) => type === "multipart/form-data") ??
    entries[0];
  const schema = resolveSchema(media.schema ?? {});

  if (contentType === "multipart/form-data") {
    const formdata = [];
    for (const [name, propertySchema] of Object.entries(schema.properties ?? {})) {
      const property = resolveSchema(propertySchema);
      if (property.format === "binary") {
        formdata.push({ key: name, type: "file", src: [] });
      } else {
        formdata.push({
          key: name,
          type: "text",
          value: stringifyFormValue(exampleValue(
            property, name, schema["x-context-name"] ?? schema.title ?? "")),
          disabled: property.nullable === true
        });
      }
    }
    return { body: { mode: "formdata", formdata } };
  }

  if (contentType.includes("json")) {
    return {
      contentType,
      body: {
        mode: "raw",
        raw: JSON.stringify(exampleValue(
          schema, "", schema["x-context-name"] ?? schema.title ?? ""), null, 2),
        options: { raw: { language: "json" } }
      }
    };
  }

  return {
    contentType,
    body: {
      mode: "raw",
      raw: typeof media.example === "string" ? media.example : ""
    }
  };
}

function exampleValue(inputSchema, propertyName, contextName) {
  const schema = resolveSchema(inputSchema);
  if (schema.example !== undefined) return schema.example;
  if (schema.default !== undefined) return schema.default;
  if (schema.enum?.length) return schema.enum[0];
  if (schema.oneOf?.length) return exampleValue(schema.oneOf[0], propertyName, contextName);
  if (schema.allOf?.length) {
    return Object.assign({}, ...schema.allOf.map(item => exampleValue(item, propertyName, contextName)));
  }
  if (schema.type === "object" || schema.properties) {
    const result = {};
    const properties = Object.entries(schema.properties ?? {});
    const included = properties.filter(([, property]) => !resolveSchema(property).nullable);
    const selected = included.length ? included : properties.slice(0, 1);
    for (const [name, property] of selected) {
      result[name] = exampleValue(property, name, schema.title ?? contextName);
    }
    return result;
  }
  if (schema.type === "array") {
    return [exampleValue(schema.items ?? {}, singular(propertyName), contextName)];
  }
  if (schema.type === "boolean") return booleanExample(propertyName);
  if (schema.type === "integer" || schema.type === "number") return numberExample(propertyName);
  if (schema.format === "date") {
    return /objetivo|fin/i.test(propertyName) ? "{{futureDate}}" : "{{currentDate}}";
  }
  if (schema.format === "date-time") return "2026-07-26T12:00:00Z";
  if (schema.format === "time") return "12:00:00";
  if (schema.format === "guid" || schema.format === "uuid" || /Id$/i.test(propertyName)) {
    const variable = variableName(propertyName);
    discoveredVariables.add(variable);
    return `{{${variable}}}`;
  }
  return stringExample(propertyName, contextName);
}

function stringExample(name, context) {
  const normalized = name.toLowerCase();
  const typeContext = context.toLowerCase();
  if (normalized === "correo") return "{{testEmail}}";
  if (normalized.includes("contrasena")) return "{{testPassword}}";
  if (normalized === "moneda") return "PYG";
  if (normalized === "idioma") return "es";
  if (normalized === "zonahoraria") return "America/Asuncion";
  if (normalized === "ambito") return "privado";
  if (normalized === "tema") return "sistema";
  if (normalized === "formato") return "pdf";
  if (normalized === "frecuencia") return "mensual";
  if (normalized === "periodo" || normalized === "rango") return "mes";
  if (normalized === "rol") return "integrante";
  if (normalized === "plancodigo") return "premium-mensual";
  if (normalized === "proveedor") return "interno";
  if (normalized === "comprobante") return `postman-${Date.now()}`;
  if (normalized === "plataforma") return "windows";
  if (normalized === "versionaplicacion") return "0.1.0";
  if (normalized === "versionsistema" || normalized === "versionso") return "11";
  if (normalized === "identificador" || normalized.includes("identificadorinstalacion")) return "postman-local-01";
  if (normalized === "tipo") {
    if (typeContext.includes("cuenta")) return "cuenta-ahorro";
    if (typeContext.includes("categoria")) return "ambos";
    if (typeContext.includes("procesamiento")) return "ocr";
    if (typeContext.includes("document")) return "imagen";
    if (typeContext.includes("operacioncaja")) return "aporte";
    return "gasto";
  }
  if (normalized === "color") return "#2563EB";
  if (normalized === "icono") return "wallet";
  if (normalized.includes("descripcion")) return "Operación generada desde Postman";
  if (normalized.includes("nombre")) return "Recurso Postman";
  if (normalized === "codigo") return "000000";
  if (normalized === "canal") return "correo";
  if (normalized === "motivo") return "cambio-contrasena";
  if (normalized === "ultimoscuatro") return "1234";
  if (normalized.includes("token")) return "REEMPLAZAR_TOKEN";
  return `REEMPLAZAR_${name || "VALOR"}`;
}

function numberExample(name) {
  const normalized = name.toLowerCase();
  if (normalized.includes("monto") || normalized.includes("saldo") || normalized.includes("limite")) return 100000;
  if (normalized.includes("dia")) return 15;
  if (normalized.includes("cantidad")) return 1;
  return 1;
}

function booleanExample(name) {
  const normalized = name.toLowerCase();
  if (normalized.includes("acepta")) return true;
  if (normalized.includes("incluida")) return true;
  if (normalized.includes("recordar")) return true;
  return false;
}

function createTestScript(codes, route, method, responseVariable) {
  const lines = [
    `const documentedCodes = ${JSON.stringify(codes)};`,
    "pm.test('El código HTTP está documentado en Swagger', function () {",
    "    pm.expect(documentedCodes).to.include(pm.response.code);",
    "});",
    "const etag = pm.response.headers.get('ETag');",
    "if (etag) pm.collectionVariables.set('ifMatch', etag);"
  ];
  if (responseVariable) {
    lines.push(
      "if (pm.response.code >= 200 && pm.response.code < 300 && pm.response.text()) {",
      "    const json = pm.response.json();",
      `    if (json.id) pm.collectionVariables.set('${responseVariable}', json.id);`,
      "}"
    );
  }
  if (route === "/api/v1/sesiones" && method === "POST") {
    lines.push(
      "if (pm.response.code >= 200 && pm.response.code < 300) {",
      "    const json = pm.response.json();",
      "    if (json.accessToken) pm.collectionVariables.set('accessToken', json.accessToken);",
      "    if (json.refreshToken) pm.collectionVariables.set('refreshToken', json.refreshToken);",
      "}"
    );
  }
  return lines;
}

function createDescription(operation, codes, method) {
  const notes = [
    `operationId: ${operation.operationId ?? "sin-operationId"}`,
    `Respuestas documentadas: ${codes.join(", ") || "ninguna"}.`
  ];
  if (method === "DELETE") {
    notes.push("Seguridad: Collection Runner la omite salvo que allowDestructive=true.");
  }
  if (operation.description) notes.push(operation.description);
  return notes.join("\n\n");
}

function createVariables() {
  for (const parameter of collectPathParameters()) {
    discoveredVariables.add(variableName(parameter));
  }
  const defaults = new Map([
    ["baseUrl", "http://localhost:8080"],
    ["testPassword", "PruebaLocal-2026!"],
    ["allowDestructive", "false"],
    ["currentDate", ""],
    ["futureDate", ""],
    ["testEmail", ""],
    ["accessToken", ""],
    ["refreshToken", ""],
    ["ifMatch", "\"1\""]
  ]);
  return [...new Set([...defaults.keys(), ...[...discoveredVariables].sort()])]
    .map(key => ({ key, value: defaults.get(key) ?? "", type: "string" }));
}

function collectPathParameters() {
  const names = [];
  for (const route of Object.keys(document.paths ?? {})) {
    for (const match of route.matchAll(/\{([^}]+)\}/g)) names.push(match[1]);
  }
  return names;
}

function parameterValue(parameter) {
  const schema = resolveSchema(parameter.schema ?? {});
  if (schema.default !== undefined) return String(schema.default);
  if (schema.example !== undefined) return String(schema.example);
  if (parameter.name.toLowerCase() === "limite") return "20";
  if (parameter.name.toLowerCase() === "desde") return "{{currentDate}}";
  if (parameter.name.toLowerCase() === "hasta") return "{{currentDate}}";
  if (/Id$/i.test(parameter.name)) {
    const variable = variableName(parameter.name);
    discoveredVariables.add(variable);
    return `{{${variable}}}`;
  }
  return stringExample(parameter.name, "");
}

function headerValue(name) {
  const normalized = name.toLowerCase();
  if (normalized === "if-match") return "{{ifMatch}}";
  if (normalized.includes("idempot")) return "{{$guid}}";
  if (normalized.includes("correlation")) return "{{$guid}}";
  return `REEMPLAZAR_${name}`;
}

function variableName(name) {
  return ID_ALIASES.get(name) ?? name;
}

function singular(name) {
  if (name === "categoriaIds") return "categoriaId";
  if (name === "cuentaIds") return "cuentaId";
  return name.endsWith("s") ? name.slice(0, -1) : name;
}

function stringifyFormValue(value) {
  return typeof value === "string" ? value : JSON.stringify(value);
}

function resolveParameter(parameter) {
  return resolveReference(parameter);
}

function resolveSchema(schema) {
  const contextName = schema?.$ref?.split("/").at(-1);
  const referenced = resolveReference(schema);
  const resolved = contextName
    ? { ...referenced, "x-context-name": contextName }
    : referenced;
  if (resolved.oneOf?.length === 1) {
    const selected = resolveSchema(resolved.oneOf[0]);
    return { ...selected, nullable: resolved.nullable ?? selected.nullable };
  }
  return resolved;
}

function resolveReference(value) {
  if (!value?.$ref) return value ?? {};
  const parts = value.$ref.replace(/^#\//, "").split("/");
  return parts.reduce((current, part) => current?.[part], document) ?? {};
}

async function readOpenApi(location) {
  if (/^https?:\/\//i.test(location)) {
    const response = await fetch(location);
    if (!response.ok) throw new Error(`No se pudo leer Swagger: HTTP ${response.status}`);
    return response.json();
  }
  return JSON.parse(await fs.readFile(path.resolve(location), "utf8"));
}
