#!/usr/bin/env ruby
# frozen_string_literal: true

require "yaml"

ROOT = File.expand_path("..", __dir__)
OPENAPI_PATH = File.join(ROOT, "docs/api/openapi.yaml")
ENDPOINTS_PATH = File.join(ROOT, "docs/api/ENDPOINTS.md")
HTTP_METHODS = %w[get post put patch delete options head trace].freeze
PUBLIC_OPERATIONS = [
  ["POST", "/usuarios"],
  ["POST", "/sesiones"],
  ["POST", "/sesiones/renovaciones"],
  ["POST", "/recuperaciones-contrasena"],
  ["POST", "/restablecimientos-contrasena"],
  ["GET", "/invitaciones-familiares/{token}"],
  ["GET", "/planes-suscripcion"],
  ["GET", "/configuracion-cliente"]
].freeze

document = YAML.safe_load(File.read(OPENAPI_PATH), aliases: false)
errors = []

errors << "openapi debe ser 3.1.x" unless document["openapi"]&.start_with?("3.1.")
errors << "info ausente" unless document["info"].is_a?(Hash)
errors << "paths ausente" unless document["paths"].is_a?(Hash)
errors << "components ausente" unless document["components"].is_a?(Hash)

operations = []
document.fetch("paths", {}).each do |path, path_item|
  errors << "path inválido: #{path}" unless path.start_with?("/")
  path_item.each do |method, operation|
    next unless HTTP_METHODS.include?(method)

    operations << [method.upcase, path, operation]
    errors << "#{method.upcase} #{path}: operationId ausente" if operation["operationId"].to_s.empty?
    errors << "#{method.upcase} #{path}: responses ausente" unless operation["responses"].is_a?(Hash)
    errors << "#{method.upcase} #{path}: GET no debe declarar requestBody" if method == "get" && operation.key?("requestBody")
    errors << "#{method.upcase} #{path}: falta respuesta 500" unless operation.fetch("responses", {}).key?("500")
    if operation.dig("responses", "204", "content")
      errors << "#{method.upcase} #{path}: una respuesta 204 no puede tener contenido"
    end
    errors << "#{method.upcase} #{path}: trazabilidad RF ausente" unless operation["x-requisitos-funcionales"].is_a?(Array)
    errors << "#{method.upcase} #{path}: etapa inválida" unless operation["x-etapa"].is_a?(Integer) && operation["x-etapa"].positive?
    tests = operation["x-pruebas-contrato"]
    errors << "#{method.upcase} #{path}: pruebas ausentes" unless tests.is_a?(Array) && !tests.empty?
    if operation["x-requisitos-funcionales"].empty? && operation["x-capacidad"].to_s.empty?
      errors << "#{method.upcase} #{path}: sin RF ni capacidad"
    end

    expected_params = path.scan(/\{([^}]+)\}/).flatten.sort
    actual_params = Array(operation["parameters"]).filter_map do |parameter|
      parameter["name"] if parameter.is_a?(Hash) && parameter["in"] == "path"
    end.sort
    errors << "#{method.upcase} #{path}: parámetros #{actual_params.inspect}, esperados #{expected_params.inspect}" unless actual_params == expected_params
  end
end

actual_public = operations.filter_map do |method, path, operation|
  [method, path] if operation["security"] == []
end
unless actual_public.sort == PUBLIC_OPERATIONS.sort
  errors << "operaciones públicas inesperadas: #{actual_public.sort.inspect}; esperadas: #{PUBLIC_OPERATIONS.sort.inspect}"
end

operation_ids = operations.map { |_method, _path, operation| operation["operationId"] }
duplicates = operation_ids.tally.select { |_id, count| count > 1 }.keys
errors << "operationId duplicados: #{duplicates.join(', ')}" unless duplicates.empty?

catalog_operations = File.read(ENDPOINTS_PATH).lines.filter_map do |line|
  match = line.match(/^\| `(GET|POST|PATCH|PUT|DELETE)` \| `([^`]+)`/)
  [match[1], match[2]] if match
end
spec_operations = operations.map { |method, path, _operation| [method, path] }
missing = catalog_operations - spec_operations
extra = spec_operations - catalog_operations
errors << "operaciones faltantes: #{missing.inspect}" unless missing.empty?
errors << "operaciones extra: #{extra.inspect}" unless extra.empty?

def resolve_pointer(document, reference)
  return nil unless reference.start_with?("#/")

  reference.delete_prefix("#/").split("/").reduce(document) do |current, token|
    break nil unless current.is_a?(Hash)

    current[token.gsub("~1", "/").gsub("~0", "~")]
  end
end

walk = lambda do |value, location|
  case value
  when Hash
    if value["$ref"] && resolve_pointer(document, value["$ref"]).nil?
      errors << "#{location}: referencia inexistente #{value['$ref']}"
    end
    value.each { |key, child| walk.call(child, "#{location}/#{key}") }
  when Array
    value.each_with_index { |child, index| walk.call(child, "#{location}/#{index}") }
  end
end
walk.call(document, "#")

invitation_schema = document.dig("components", "schemas", "InvitacionFamiliarRequest")
unless invitation_schema.is_a?(Hash) &&
       invitation_schema["required"] == ["rol"] &&
       invitation_schema["oneOf"].is_a?(Array) &&
       invitation_schema["oneOf"].length == 2
  errors << "InvitacionFamiliarRequest debe exigir rol y exactamente uno entre correo e identificadorUsuario"
end

invitation_roles = invitation_schema&.dig("properties", "rol", "enum")
unless invitation_roles == %w[administrador integrante]
  errors << "InvitacionFamiliarRequest sólo puede asignar los roles administrador o integrante"
end

invitation_response_schema = document.dig("components", "schemas", "InvitacionFamiliarResponse")
unless invitation_response_schema.is_a?(Hash) &&
       invitation_response_schema["required"]&.include?("correo") &&
       invitation_response_schema["required"]&.include?("usuarioDestino") &&
       invitation_response_schema["oneOf"].is_a?(Array) &&
       invitation_response_schema["oneOf"].length == 2
  errors << "InvitacionFamiliarResponse debe devolver un único destino normalizado"
end

if errors.empty?
  schemas = document.dig("components", "schemas")&.length || 0
  puts "OpenAPI válido estructuralmente: #{operations.length} operaciones, #{schemas} esquemas, #{operation_ids.length} operationId únicos"
else
  warn errors.join("\n")
  exit 1
end
