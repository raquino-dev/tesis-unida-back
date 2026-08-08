#!/usr/bin/env ruby
# frozen_string_literal: true

require "json"
require "yaml"

ROOT = File.expand_path("..", __dir__)
ENDPOINTS_PATH = File.join(ROOT, "docs/api/ENDPOINTS.md")
CONTRACTS_PATH = File.join(ROOT, "docs/api/CONTRATOS.md")
OUTPUT_PATH = File.join(ROOT, "docs/api/openapi.yaml")
MUTABLE_RESPONSE_SCHEMAS = %w[
  ProcesoAsyncResponse UsuarioResponse PreferenciasResponse CredencialBiometricaResponse
  CuentaResponse CategoriaResponse TarjetaCreditoResponse MovimientoResponse
  DocumentoFinancieroResponse ProcesamientoDocumentalResponse MovimientoRecurrenteResponse
  TransferenciaResponse PresupuestoResponse MetaAhorroResponse GrupoFamiliarResponse
  InvitacionFamiliarResponse CrearInvitacionFamiliarResponse IntegranteFamiliarResponse
  CategoriaFamiliarResponse CuentaCompartidaResponse CajaCompartidaResponse
  ExportacionResponse AlertaResponse SuscripcionResponse DispositivoResponse
].freeze

def camelize(parts)
  first, *rest = parts
  [first, *rest.map { |part| part[0].upcase + part[1..] }].join
end

def operation_id(method, path)
  parts = path.split("/").reject(&:empty?).flat_map do |segment|
    if segment.start_with?("{")
      ["by", segment.delete("{}")]
    else
      segment.split("-")
    end
  end
  camelize([method.downcase, *parts])
end

def schema_for(value, key = nil)
  schema =
    case value
    when Hash
      properties = value.to_h { |child_key, child| [child_key, schema_for(child, child_key)] }
      required = value.reject { |_child_key, child| child.nil? }.keys
      result = { "type" => "object", "additionalProperties" => false, "properties" => properties }
      result["required"] = required unless required.empty?
      result
    when Array
      { "type" => "array", "items" => value.empty? ? {} : schema_for(value.first) }
    when Integer
      { "type" => "integer", "format" => "int64", "example" => value }
    when Float
      { "type" => "number", "format" => "double", "example" => value }
    when TrueClass, FalseClass
      { "type" => "boolean", "example" => value }
    when NilClass
      { "type" => %w[string null] }
    else
      string_schema(value, key)
    end

  if key
    schema["minimum"] = 1 if key == "monto" || key.end_with?("Monto")
    if key.match?(/porcentaje|progreso|confianza/i) && schema["type"] == "number"
      schema["minimum"] = 0
      schema["maximum"] = 1
    end
  end
  schema
end

def string_schema(value, key)
  schema = { "type" => "string" }
  case value
  when /\A\d{4}-\d{2}-\d{2}\z/
    schema["format"] = "date"
    schema["example"] = value
  when /\A\d{4}-\d{2}-\d{2}T/
    schema["format"] = "date-time"
    schema["example"] = value
  when /\Ahttps?:\/\//
    schema["format"] = "uri"
    schema["example"] = value
  else
    if key&.match?(/(?:^id$|Id$)/)
      schema["format"] = "uuid"
    elsif key&.match?(/correo/i)
      schema["format"] = "email"
      schema["example"] = value
    elsif key&.match?(/contrasena/i)
      schema["minLength"] = 12
      schema["maxLength"] = 128
      schema["writeOnly"] = true
    elsif key&.match?(/token|comprobante|firma|atestacion/i)
      schema["writeOnly"] = true
    elsif key == "moneda"
      schema["enum"] = ["PYG"]
      schema["example"] = "PYG"
    elsif key == "color"
      schema["pattern"] = "^#[0-9A-Fa-f]{6}$"
      schema["example"] = value
    else
      schema["example"] = value
    end
  end
  schema
end

def contract_sections(markdown)
  sections = {}
  current = nil
  markdown.each_line do |line|
    if (match = line.match(/^### (.+)$/))
      current = match[1]
      sections[current] = +""
    elsif current
      sections[current] << line
    end
  end
  sections
end

def json_examples(section)
  section.scan(/```json\s*\n(.*?)\n```/m).filter_map do |body|
    JSON.parse(body.first)
  rescue JSON::ParserError
    nil
  end
end

def add_example_schema(schemas, name, sections, heading, index)
  examples = json_examples(sections.fetch(heading))
  schemas[name] = schema_for(examples.fetch(index))
end

def inline_request_schema(name, cell)
  body = cell[/\{([^}]+)\}/, 1]
  return { "type" => "object", "additionalProperties" => false, "minProperties" => 1 } unless body

  properties = {}
  required = []
  body.split(",").each do |raw|
    token = raw.strip
    key_part, values = token.split(":", 2)
    optional = key_part.end_with?("?")
    key = key_part.delete_suffix("?").strip
    schema =
      if values
        enum = values.strip.split("/").map(&:strip)
        { "type" => "string", "enum" => enum }
      elsif key.match?(/^(leida|archivada|mantenimiento|activa|valida)$/i)
        { "type" => "boolean" }
      elsif key.end_with?("Ids")
        { "type" => "array", "items" => { "type" => "string", "format" => "uuid" }, "uniqueItems" => true }
      elsif key.end_with?("Id")
        { "type" => "string", "format" => "uuid" }
      elsif key.match?(/^(monto|limite|saldo)/i)
        { "type" => "integer", "format" => "int64" }
      elsif key.match?(/^(atestacion|datosDetectados)$/i)
        { "type" => "object", "additionalProperties" => true }
      else
        { "type" => "string" }
      end
    schema["writeOnly"] = true if key.match?(/contrasena|token|codigo|atestacion|clavePrivada|comprobante/i)
    properties[key] = schema
    required << key unless optional
  end
  result = {
    "type" => "object",
    "additionalProperties" => false,
    "properties" => properties,
    "minProperties" => 1
  }
  result["required"] = required unless required.empty?
  result
end

def partial_schema(base_schema, allowed = nil)
  properties = Marshal.load(Marshal.dump(base_schema.fetch("properties", {})))
  properties.select! { |key, _value| allowed.include?(key) } if allowed
  {
    "type" => "object",
    "additionalProperties" => false,
    "minProperties" => 1,
    "properties" => properties
  }
end

def patch_base_for(path)
  cases = [
    [%r{\A/cuentas/}, "CuentaRequest", nil],
    [%r{/categorias/}, "CategoriaRequest", nil],
    [%r{\A/tarjetas-credito/}, "TarjetaCreditoRequest", nil],
    [%r{\A/movimientos-recurrentes/}, "MovimientoRecurrenteRequest", nil],
    [%r{/presupuestos/}, "PresupuestoRequest", nil],
    [%r{\A/metas-ahorro/}, "MetaAhorroRequest", %w[nombre montoObjetivo fechaObjetivo]],
    [%r{\A/dispositivos/}, "DispositivoRequest", %w[nombre tokenPush zonaHoraria versionAplicacion]]
  ]
  cases.find { |regex, _schema, _allowed| path.match?(regex) }&.drop(1)
end

def trace_for(path)
  cases = [
    [/biometric|biometr/, %w[RF-18], 1, %w[CT-RF-18]],
    [/desafios-otp|verificaciones-otp/, %w[RF-19], 1, %w[CT-RF-19]],
    [/eventos-seguridad|eventos-auditoria/, %w[RF-20], 1, %w[CT-RF-20]],
    [/recuperaciones-contrasena|restablecimientos-contrasena|perfil\/contrasena/, %w[RF-02], 1, %w[CT-RF-02]],
    [/perfil\/preferencias/, [], 1, %w[CT-PREFERENCIAS]],
    [/perfil|eliminaciones-perfil/, [], 1, %w[CT-PERFIL]],
    [/usuarios|sesiones/, %w[RF-01], 1, %w[CT-RF-01]],
    [/documentos-financieros.*procesamientos|procesamientos-documentales/, %w[RF-07 RF-08 RF-09], 5, %w[CT-RF-07 CT-RF-08 CT-RF-09]],
    [/documentos-financieros/, %w[RF-07 RF-08], 5, %w[CT-RF-07 CT-RF-08]],
    [/categorias/, %w[RF-10], 2, %w[CT-RF-10]],
    [/presupuestos|resumen-presupuestario/, %w[RF-11], 4, %w[CT-RF-11]],
    [/metas-ahorro/, %w[RF-12], 4, %w[CT-RF-12]],
    [/caja-compartida|operaciones-caja/, %w[RF-13], 3, %w[CT-RF-13]],
    [/tableros-financieros|reportes-financieros/, %w[RF-14], 6, %w[CT-RF-14]],
    [/exportaciones/, %w[RF-15], 5, %w[CT-RF-15]],
    [/proyecciones-gastos/, %w[RF-16], 6, %w[CT-RF-16]],
    [/alertas-financieras/, %w[RF-17], 6, %w[CT-RF-17]],
    [/planes-suscripcion|suscripcion/, %w[RF-21], 7, %w[CT-RF-21]],
    [/invitaciones|integrantes|grupos-familiares.*eliminaciones/, %w[RF-04], 3, %w[CT-RF-04]],
    [/grupos-familiares$/, %w[RF-03], 3, %w[CT-RF-03]],
    [/cuentas-compartidas/, %w[RF-04], 3, %w[CT-RF-04]],
    [/movimientos-recurrentes/, [], 2, %w[CT-RECURRENCIAS]],
    [/movimientos/, %w[RF-05 RF-06], 2, %w[CT-RF-05 CT-RF-06]],
    [/grupos-familiares/, %w[RF-03 RF-04], 3, %w[CT-RF-03 CT-RF-04]],
    [/cuentas/, [], 2, %w[CT-CUENTAS]],
    [/tarjetas-credito/, [], 2, %w[CT-TARJETAS]],
    [/transferencias/, [], 2, %w[CT-TRANSFERENCIAS]],
    [/score-financiero/, [], 6, %w[CT-SCORE]],
    [/dispositivos/, [], 1, %w[CT-DISPOSITIVOS]],
    [/configuracion-cliente/, [], 1, %w[CT-CONFIG-CLIENTE]]
  ]
  rf, stage, tests = cases.find { |regex, *_rest| path.match?(regex) }&.drop(1) || [[], 0, ["CT-SIN-ASIGNAR"]]
  [rf, stage, tests]
end

def public_operation?(method, path)
  [
    ["POST", "/usuarios"],
    ["POST", "/sesiones"],
    ["POST", "/sesiones/renovaciones"],
    ["POST", "/recuperaciones-contrasena"],
    ["POST", "/restablecimientos-contrasena"],
    ["GET", "/invitaciones-familiares/{token}"],
    ["GET", "/planes-suscripcion"],
    ["GET", "/configuracion-cliente"]
  ].include?([method, path])
end

def query_parameters(path, request_cell)
  parameters = []
  if request_cell.include?("paginación")
    parameters << { "$ref" => "#/components/parameters/Cursor" }
    parameters << { "$ref" => "#/components/parameters/Limite" }
  end

  fields = case path
           when "/eventos-seguridad"
             %w[tipo desde hasta]
           when "/eventos-auditoria"
             %w[recurso usuarioId grupoFamiliarId desde hasta]
           when "/cuentas"
             %w[activas tipo]
           when %r{\A(?:/grupos-familiares/\{grupoId\})?/categorias\z}
             %w[tipo predefinida]
           when %r{\A(?:/grupos-familiares/\{grupoId\})?/movimientos\z}
             %w[texto tipo categoriaId cuentaId integranteId desde hasta documento]
           when "/documentos-financieros"
             %w[tipo estado desde hasta]
           when "/movimientos-recurrentes"
             %w[estado tipo]
           when "/transferencias"
             %w[cuentaOrigenId cuentaDestinoId desde hasta]
           when %r{\A(?:/grupos-familiares/\{grupoId\})?/presupuestos\z}
             %w[periodo estado categoriaId]
           when "/resumen-presupuestario"
             %w[desde hasta]
           when "/metas-ahorro"
             %w[ambito grupoFamiliarId]
           when %r{/invitaciones\z}
             %w[estado]
           when %r{/operaciones-caja\z}
             %w[tipo integranteId desde hasta]
           when "/tableros-financieros"
             %w[ambito desde hasta]
           when %r{/tableros-financieros\z}
             %w[desde hasta]
           when "/reportes-financieros"
             %w[rango desde hasta tipo categoriaId cuentaId]
           when %r{/reportes-financieros\z}
             %w[rango desde hasta tipo categoriaId integranteId]
           when "/exportaciones"
             %w[estado formato desde hasta]
           when "/proyecciones-gastos"
             %w[ambito periodo]
           when %r{/proyecciones-gastos\z}
             %w[periodo]
           when "/alertas-financieras"
             %w[nivel leida desde hasta]
           else
             []
           end
  fields.uniq.each do |field|
    schema =
      if field.end_with?("Id")
        { "type" => "string", "format" => "uuid" }
      elsif %w[activas predefinida leida documento].include?(field)
        { "type" => "boolean" }
      elsif %w[desde hasta].include?(field)
        { "type" => "string", "format" => "date" }
      else
        { "type" => "string" }
      end
    parameters << { "name" => field, "in" => "query", "required" => false, "schema" => schema }
  end
  parameters
end

contracts = File.read(CONTRACTS_PATH)
sections = contract_sections(contracts)
schemas = {}

schema_sources = {
  "ProcesoAsyncResponse" => ["ProcesoAsyncResponse", 0],
  "CrearUsuarioRequest" => ["CrearUsuarioRequest", 0],
  "UsuarioResponse" => ["UsuarioResponse", 0],
  "CrearSesionRequest" => ["CrearSesionRequest / SesionResponse", 0],
  "SesionResponse" => ["CrearSesionRequest / SesionResponse", 1],
  "SesionActivaResponse" => ["SesionActivaResponse", 0],
  "RenovarSesionRequest" => ["RenovarSesionRequest", 0],
  "RecuperacionContrasenaRequest" => ["RecuperacionContrasenaRequest / RestablecimientoContrasenaRequest", 0],
  "RestablecimientoContrasenaRequest" => ["RecuperacionContrasenaRequest / RestablecimientoContrasenaRequest", 1],
  "CambiarContrasenaRequest" => ["CambiarContrasenaRequest", 0],
  "DesafioOtpRequest" => ["DesafioOtpRequest / DesafioOtpResponse / VerificacionOtpRequest", 0],
  "DesafioOtpResponse" => ["DesafioOtpRequest / DesafioOtpResponse / VerificacionOtpRequest", 1],
  "VerificacionOtpRequest" => ["DesafioOtpRequest / DesafioOtpResponse / VerificacionOtpRequest", 2],
  "DesafioBiometricoResponse" => ["DesafioBiometricoResponse / VerificacionBiometricaRequest", 0],
  "VerificacionBiometricaRequest" => ["DesafioBiometricoResponse / VerificacionBiometricaRequest", 1],
  "EventoSeguridadResponse" => ["EventoSeguridadResponse", 0],
  "PreferenciasResponse" => ["PreferenciasResponse", 0],
  "CredencialBiometricaResponse" => ["CredencialBiometricaResponse", 0],
  "EventoAuditoriaResponse" => ["EventoAuditoriaResponse", 0],
  "CuentaRequest" => ["CuentaRequest / CuentaResponse", 0],
  "CuentaResponse" => ["CuentaRequest / CuentaResponse", 1],
  "CategoriaRequest" => ["CategoriaRequest / CategoriaResponse", 0],
  "CategoriaResponse" => ["CategoriaRequest / CategoriaResponse", 1],
  "TarjetaCreditoRequest" => ["TarjetaCreditoRequest / TarjetaCreditoResponse", 0],
  "TarjetaCreditoResponse" => ["TarjetaCreditoRequest / TarjetaCreditoResponse", 1],
  "MovimientoRequest" => ["MovimientoRequest", 0],
  "MovimientoResponse" => ["MovimientoResponse", 0],
  "DocumentoFinancieroResponse" => ["DocumentoFinancieroResponse", 0],
  "ProcesamientoDocumentalResponse" => ["ProcesamientoDocumentalResponse", 0],
  "MovimientoRecurrenteRequest" => ["MovimientoRecurrenteRequest", 0],
  "MovimientoRecurrenteResponse" => ["MovimientoRecurrenteResponse", 0],
  "TransferenciaRequest" => ["TransferenciaRequest / TransferenciaResponse", 0],
  "TransferenciaResponse" => ["TransferenciaRequest / TransferenciaResponse", 1],
  "PresupuestoRequest" => ["PresupuestoRequest / PresupuestoResponse", 0],
  "PresupuestoResponse" => ["PresupuestoRequest / PresupuestoResponse", 1],
  "MetaAhorroRequest" => ["MetaAhorroRequest / MetaAhorroResponse", 0],
  "MetaAhorroResponse" => ["MetaAhorroRequest / MetaAhorroResponse", 1],
  "AporteMetaRequest" => ["AporteMetaRequest / AporteMetaResponse", 0],
  "AporteMetaResponse" => ["AporteMetaRequest / AporteMetaResponse", 1],
  "GrupoFamiliarRequest" => ["GrupoFamiliarRequest / GrupoFamiliarResponse", 0],
  "GrupoFamiliarResponse" => ["GrupoFamiliarRequest / GrupoFamiliarResponse", 1],
  "InvitacionFamiliarRequest" => ["InvitacionFamiliarRequest / InvitacionFamiliarResponse / CrearInvitacionFamiliarResponse", 0],
  "InvitacionFamiliarResponse" => ["InvitacionFamiliarRequest / InvitacionFamiliarResponse / CrearInvitacionFamiliarResponse", 1],
  "InvitacionPublicaResponse" => ["InvitacionPublicaResponse", 0],
  "AceptacionInvitacionRequest" => ["AceptacionInvitacionRequest", 0],
  "IntegranteFamiliarResponse" => ["IntegranteFamiliarResponse", 0],
  "CategoriaFamiliarResponse" => ["CategoriaFamiliarResponse", 0],
  "CuentaCompartidaResponse" => ["CuentaCompartidaResponse", 0],
  "OperacionCajaRequest" => ["OperacionCajaRequest / OperacionCajaResponse", 0],
  "OperacionCajaResponse" => ["OperacionCajaRequest / OperacionCajaResponse", 1],
  "CajaCompartidaResponse" => ["CajaCompartidaResponse", 0],
  "DashboardResponse" => ["DashboardResponse", 0],
  "ReporteResponse" => ["ReporteResponse", 0],
  "ExportacionRequest" => ["ExportacionRequest / ExportacionResponse", 0],
  "ExportacionResponse" => ["ExportacionRequest / ExportacionResponse", 1],
  "ProyeccionResponse" => ["ProyeccionResponse", 0],
  "AlertaResponse" => ["AlertaResponse", 0],
  "ScoreResponse" => ["ScoreResponse", 0],
  "PlanSuscripcionResponse" => ["PlanSuscripcionResponse", 0],
  "SuscripcionRequest" => ["SuscripcionRequest / SuscripcionResponse", 0],
  "SuscripcionResponse" => ["SuscripcionRequest / SuscripcionResponse", 1],
  "DispositivoRequest" => ["DispositivoRequest / DispositivoResponse", 0],
  "DispositivoResponse" => ["DispositivoRequest / DispositivoResponse", 1]
}
schema_sources.each { |name, (heading, index)| add_example_schema(schemas, name, sections, heading, index) }

invitacion_request = schemas.fetch("InvitacionFamiliarRequest")
invitacion_request["required"] = ["rol"]
invitacion_request["properties"]["correo"] = {
  "type" => ["string", "null"],
  "format" => "email",
  "example" => "familiar@correo.com"
}
invitacion_request["properties"]["identificadorUsuario"] = {
  "type" => ["string", "null"],
  "format" => "uuid"
}
invitacion_request["properties"]["rol"] = {
  "type" => "string",
  "enum" => %w[administrador integrante],
  "example" => "integrante"
}
invitacion_request["oneOf"] = [
  {
    "required" => ["correo"],
    "properties" => {
      "correo" => { "type" => "string", "format" => "email" },
      "identificadorUsuario" => { "type" => "null" }
    }
  },
  {
    "required" => ["identificadorUsuario"],
    "properties" => {
      "correo" => { "type" => "null" },
      "identificadorUsuario" => { "type" => "string", "format" => "uuid" }
    }
  }
]

invitacion_response = schemas.fetch("InvitacionFamiliarResponse")
invitacion_response["properties"]["correo"] = {
  "type" => ["string", "null"],
  "format" => "email",
  "example" => "familiar@correo.com"
}
invitacion_response["properties"]["usuarioDestino"] = {
  "type" => ["string", "null"],
  "format" => "uuid"
}
invitacion_response["properties"]["rol"] = {
  "type" => "string",
  "enum" => %w[administrador integrante],
  "example" => "integrante"
}
invitacion_response["required"] |= %w[correo usuarioDestino]
invitacion_response["oneOf"] = [
  {
    "properties" => {
      "correo" => { "type" => "string", "format" => "email" },
      "usuarioDestino" => { "type" => "null" }
    }
  },
  {
    "properties" => {
      "correo" => { "type" => "null" },
      "usuarioDestino" => { "type" => "string", "format" => "uuid" }
    }
  }
]

schemas["CrearInvitacionFamiliarResponse"] = Marshal.load(Marshal.dump(schemas["InvitacionFamiliarResponse"]))
schemas["CrearInvitacionFamiliarResponse"]["properties"]["codigo"] = {
  "type" => "string", "pattern" => "^\\d{6}$", "writeOnly" => true
}
schemas["CrearInvitacionFamiliarResponse"]["required"] << "codigo"

schemas["ProblemDetails"] = {
  "type" => "object",
  "additionalProperties" => false,
  "required" => %w[type title status codigo correlationId],
  "properties" => {
    "type" => { "type" => "string", "format" => "uri" },
    "title" => { "type" => "string" },
    "status" => { "type" => "integer", "minimum" => 400, "maximum" => 599 },
    "detail" => { "type" => ["string", "null"] },
    "instance" => { "type" => ["string", "null"] },
    "codigo" => { "type" => "string", "pattern" => "^[a-z0-9_]+$" },
    "correlationId" => { "type" => "string" },
    "errores" => {
      "type" => ["object", "null"],
      "additionalProperties" => { "type" => "array", "items" => { "type" => "string" } }
    }
  }
}
schemas["Paginacion"] = {
  "type" => "object",
  "additionalProperties" => false,
  "required" => %w[siguienteCursor hayMas limite],
  "properties" => {
    "siguienteCursor" => { "type" => ["string", "null"] },
    "hayMas" => { "type" => "boolean" },
    "limite" => { "type" => "integer", "minimum" => 1, "maximum" => 100 }
  }
}
schemas["DescargaResponse"] = {
  "type" => "object", "additionalProperties" => false, "required" => %w[url expiraEn],
  "properties" => {
    "url" => { "type" => "string", "format" => "uri" },
    "expiraEn" => { "type" => "string", "format" => "date-time" }
  }
}
schemas["VerificacionSeguridadResponse"] = {
  "type" => "object", "additionalProperties" => false, "required" => %w[id valida expiraEn],
  "properties" => {
    "id" => { "type" => "string", "format" => "uuid" },
    "valida" => { "type" => "boolean" },
    "expiraEn" => { "type" => "string", "format" => "date-time" }
  }
}
schemas["ConfiguracionClienteResponse"] = {
  "type" => "object", "additionalProperties" => false,
  "required" => %w[versionMinima versionRecomendada mantenimiento capacidades],
  "properties" => {
    "versionMinima" => { "type" => "string" },
    "versionRecomendada" => { "type" => "string" },
    "mantenimiento" => { "type" => "boolean" },
    "capacidades" => { "type" => "array", "items" => { "type" => "string" } }
  }
}
schemas["ResumenPresupuestarioResponse"] = {
  "type" => "object", "additionalProperties" => false,
  "required" => %w[total gastado disponible progreso presupuestos],
  "properties" => {
    "total" => { "type" => "integer", "format" => "int64" },
    "gastado" => { "type" => "integer", "format" => "int64" },
    "disponible" => { "type" => "integer", "format" => "int64" },
    "progreso" => { "type" => "number", "minimum" => 0 },
    "presupuestos" => { "type" => "array", "items" => { "$ref" => "#/components/schemas/PresupuestoResponse" } }
  }
}

endpoints = File.read(ENDPOINTS_PATH)
operations = []
tag = nil
endpoints.each_line do |line|
  tag = line.sub(/^## \d+\. /, "").strip if line.start_with?("## ")
  next unless line.match?(/^\| `(GET|POST|PATCH|PUT|DELETE)`/)

  cells = line.split("|").map(&:strip)
  operations << {
    method: cells[1].delete("`"),
    path: cells[2].delete("`"),
    request: cells[3],
    success: cells[4],
    codes: cells[5],
    tag: tag
  }
end

page_types = operations.filter_map { |op| op[:success][/Pagina<([A-Z][A-Za-z]+Response)>/, 1] }.uniq
page_types.each do |type|
  schemas["Pagina#{type}"] = {
    "type" => "object",
    "additionalProperties" => false,
    "required" => %w[datos paginacion],
    "properties" => {
      "datos" => { "type" => "array", "items" => { "$ref" => "#/components/schemas/#{type}" } },
      "paginacion" => { "$ref" => "#/components/schemas/Paginacion" }
    }
  }
end

components = {
  "securitySchemes" => {
    "bearerAuth" => { "type" => "http", "scheme" => "bearer", "bearerFormat" => "JWT" }
  },
  "parameters" => {
    "CorrelationId" => {
      "name" => "X-Correlation-Id", "in" => "header", "required" => false,
      "schema" => { "type" => "string", "maxLength" => 100 }
    },
    "IdempotencyKey" => {
      "name" => "Idempotency-Key", "in" => "header", "required" => true,
      "schema" => { "type" => "string", "minLength" => 8, "maxLength" => 100 }
    },
    "IfMatch" => {
      "name" => "If-Match", "in" => "header", "required" => true,
      "schema" => { "type" => "string", "pattern" => "^\\\".+\\\"$" }
    },
    "Cursor" => {
      "name" => "cursor", "in" => "query", "required" => false,
      "schema" => { "type" => "string", "maxLength" => 500 }
    },
    "Limite" => {
      "name" => "limite", "in" => "query", "required" => false,
      "schema" => { "type" => "integer", "minimum" => 1, "maximum" => 100, "default" => 20 }
    }
  },
  "headers" => {
    "CorrelationId" => {
      "description" => "Identificador de correlación de la solicitud.",
      "schema" => { "type" => "string" }
    },
    "ETag" => {
      "description" => "Versión opaca del recurso.",
      "schema" => { "type" => "string" }
    },
    "Location" => {
      "description" => "URI del recurso creado o aceptado.",
      "schema" => { "type" => "string", "format" => "uri-reference" }
    }
  },
  "schemas" => schemas
}

problem_descriptions = {
  "400" => "Solicitud o parámetros inválidos.",
  "401" => "Autenticación ausente o inválida.",
  "403" => "Permiso insuficiente.",
  "404" => "Recurso inexistente o no visible.",
  "409" => "Conflicto de estado o duplicado.",
  "410" => "Token o recurso efímero vencido/consumido.",
  "412" => "ETag desactualizado.",
  "413" => "Contenido demasiado grande.",
  "415" => "Tipo de contenido no soportado.",
  "422" => "Regla de negocio no satisfecha.",
  "429" => "Límite de solicitudes excedido.",
  "500" => "Error interno no controlado.",
  "503" => "Dependencia temporalmente no disponible."
}
components["responses"] = problem_descriptions.to_h do |code, description|
  ["Problem#{code}", {
    "description" => description,
    "headers" => { "X-Correlation-Id" => { "$ref" => "#/components/headers/CorrelationId" } },
    "content" => {
      "application/problem+json" => { "schema" => { "$ref" => "#/components/schemas/ProblemDetails" } }
    }
  }]
end

paths = {}
operations.each do |entry|
  method = entry[:method]
  path = entry[:path]
  op_id = operation_id(method, path)
  rf, stage, tests = trace_for(path)
  operation = {
    "tags" => [entry[:tag]],
    "summary" => "#{method} #{path}",
    "operationId" => op_id,
    "x-requisitos-funcionales" => rf,
    "x-etapa" => stage,
    "x-pruebas-contrato" => tests,
    "security" => public_operation?(method, path) ? [] : [{ "bearerAuth" => [] }],
    "parameters" => [{ "$ref" => "#/components/parameters/CorrelationId" }],
    "responses" => {}
  }
  operation["x-capacidad"] = tests.first if rf.empty?

  path.scan(/\{([^}]+)\}/).flatten.each do |parameter|
    schema = parameter == "token" ? { "type" => "string", "minLength" => 32 } : { "type" => "string", "format" => "uuid" }
    operation["parameters"] << {
      "name" => parameter, "in" => "path", "required" => true, "schema" => schema
    }
  end
  operation["parameters"].concat(query_parameters(path, entry[:request]))
  operation["parameters"] << { "$ref" => "#/components/parameters/IdempotencyKey" } if entry[:request].include?("Idempotency-Key")
  operation["parameters"] << { "$ref" => "#/components/parameters/IfMatch" } if entry[:request].include?("If-Match")
  if entry[:request].include?("X-Content-SHA256")
    operation["parameters"] << {
      "name" => "X-Content-SHA256", "in" => "header", "required" => true,
      "schema" => { "type" => "string", "pattern" => "^[0-9a-fA-F]{64}$" }
    }
  end

  if %w[POST PATCH PUT].include?(method) && entry[:request] != "—"
    media_type = "application/json"
    request_schema = nil
    if entry[:request].include?("multipart/form-data")
      media_type = "multipart/form-data"
      request_schema = {
        "type" => "object", "additionalProperties" => false,
        "required" => %w[archivo tipo ambito],
        "properties" => {
          "archivo" => { "type" => "string", "format" => "binary" },
          "tipo" => { "type" => "string", "enum" => %w[imagen pdf xml-sifen] },
          "ambito" => { "type" => "string", "enum" => %w[privado familiar] },
          "grupoFamiliarId" => { "type" => ["string", "null"], "format" => "uuid" }
        }
      }
    elsif method == "PATCH" && path.start_with?("/procesamientos-documentales/")
      schema_name = "CorreccionProcesamientoDocumentalRequest"
      schemas[schema_name] ||= {
        "type" => "object",
        "additionalProperties" => false,
        "required" => ["datosDetectados"],
        "properties" => {
          "datosDetectados" => {
            "type" => "object",
            "minProperties" => 1,
            "additionalProperties" => true
          }
        }
      }
      request_schema = { "$ref" => "#/components/schemas/#{schema_name}" }
    elsif method == "PATCH" && (base = patch_base_for(path))
      base_name, allowed = base
      schema_name = "#{base_name.delete_suffix('Request')}PatchRequest"
      schemas[schema_name] ||= partial_schema(schemas.fetch(base_name), allowed)
      if schema_name == "MovimientoRecurrentePatchRequest"
        schemas[schema_name]["properties"]["estado"] = {
          "type" => "string",
          "enum" => %w[activa pausada finalizada]
        }
      end
      request_schema = { "$ref" => "#/components/schemas/#{schema_name}" }
    elsif (dto = entry[:request][/`([A-Z][A-Za-z]+Request)`/, 1])
      request_schema = { "$ref" => "#/components/schemas/#{dto}" }
    else
      schema_name = "#{op_id[0].upcase + op_id[1..]}Request"
      schemas[schema_name] ||= inline_request_schema(schema_name, entry[:request])
      request_schema = { "$ref" => "#/components/schemas/#{schema_name}" }
    end
    operation["requestBody"] = {
      "required" => path != "/suscripcion/cancelaciones",
      "content" => { media_type => { "schema" => request_schema } }
    }
  end

  success_code = entry[:success][/\b(200|201|202|204)\b/, 1] || "200"
  success_response = {
    "description" => success_code == "204" ? "Operación completada sin cuerpo." : "Operación exitosa.",
    "headers" => { "X-Correlation-Id" => { "$ref" => "#/components/headers/CorrelationId" } }
  }
  has_location = success_code == "201" ||
    (success_code == "202" && (
      entry[:success].include?("Location") ||
      entry[:success].include?("Response") ||
      entry[:success].include?("{id, estado")
    ))
  success_response["headers"]["Location"] = { "$ref" => "#/components/headers/Location" } if has_location

  response_schema = nil
  if (page_type = entry[:success][/Pagina<([A-Z][A-Za-z]+Response)>/, 1])
    response_schema = { "$ref" => "#/components/schemas/Pagina#{page_type}" }
  elsif entry[:success].include?("Response[]")
    dto = entry[:success][/([A-Z][A-Za-z]+Response)\[\]/, 1]
    response_schema = { "type" => "array", "items" => { "$ref" => "#/components/schemas/#{dto}" } }
  elsif (dto = entry[:success][/([A-Z][A-Za-z]+Response)/, 1])
    response_schema = { "$ref" => "#/components/schemas/#{dto}" }
  elsif entry[:success].include?("{url, expiraEn}")
    response_schema = { "$ref" => "#/components/schemas/DescargaResponse" }
  elsif entry[:success].include?("{id, valida, expiraEn}")
    response_schema = { "$ref" => "#/components/schemas/VerificacionSeguridadResponse" }
  elsif path == "/resumen-presupuestario"
    response_schema = { "$ref" => "#/components/schemas/ResumenPresupuestarioResponse" }
  elsif path == "/configuracion-cliente"
    response_schema = { "$ref" => "#/components/schemas/ConfiguracionClienteResponse" }
  elsif success_code == "202" && entry[:success].include?("{id, estado")
    response_schema = { "$ref" => "#/components/schemas/ProcesoAsyncResponse" }
  end

  if response_schema && success_code != "204"
    success_response["content"] = { "application/json" => { "schema" => response_schema } }
    response_component = response_schema["$ref"]&.split("/")&.last
    if MUTABLE_RESPONSE_SCHEMAS.include?(response_component)
      success_response["headers"]["ETag"] = { "$ref" => "#/components/headers/ETag" }
    end
  end
  operation["responses"][success_code] = success_response

  codes = entry[:codes].scan(/\b(?:400|401|403|404|409|410|412|413|415|422|429|500|503)\b/).uniq
  codes.each { |code| operation["responses"][code] = { "$ref" => "#/components/responses/Problem#{code}" } }
  operation["responses"]["500"] ||= { "$ref" => "#/components/responses/Problem500" }

  paths[path] ||= {}
  paths[path][method.downcase] = operation
end

document = {
  "openapi" => "3.1.0",
  "jsonSchemaDialect" => "https://json-schema.org/draft/2020-12/schema",
  "info" => {
    "title" => "API de Finanzas Inteligentes",
    "version" => "1.0.0-draft",
    "description" => "Contrato ejecutable derivado de CONVENCIONES.md, ENDPOINTS.md, CONTRATOS.md y TRAZABILIDAD_RF.md."
  },
  "servers" => [
    { "url" => "https://api.finanzas.example/api/v1", "description" => "Producción (referencial)" },
    { "url" => "http://localhost:8080/api/v1", "description" => "Desarrollo local" }
  ],
  "security" => [{ "bearerAuth" => [] }],
  "tags" => operations.map { |operation| operation[:tag] }.uniq.map { |name| { "name" => name } },
  "paths" => paths,
  "components" => components
}

File.write(OUTPUT_PATH, YAML.dump(document, line_width: -1))
puts "Generado #{OUTPUT_PATH}: #{operations.length} operaciones, #{schemas.length} esquemas"
