#!/usr/bin/env ruby
# frozen_string_literal: true

require "json"
require "net/http"
require "set"
require "uri"
require "yaml"

ROOT = File.expand_path("..", __dir__)
CONTRACT_PATH = File.join(ROOT, "docs/api/openapi.yaml")
HTTP_METHODS = %w[get post put patch delete options head trace].freeze
IGNORED_SWAGGER_PATHS = %w[/salud/vivo /salud/listo].freeze

source = ARGV.fetch(0) do
  warn "Uso: ruby scripts/compare_openapi_to_swagger.rb <swagger.json|url>"
  exit 2
end

swagger_text = if source.match?(%r{\Ahttps?://})
  uri = URI(source)
  response = Net::HTTP.get_response(uri)
  abort "No se pudo leer Swagger: HTTP #{response.code}" unless response.is_a?(Net::HTTPSuccess)
  response.body
else
  File.read(source)
end

contract = YAML.safe_load_file(CONTRACT_PATH, aliases: true)
swagger = JSON.parse(swagger_text)

def operation_map(document, swagger: false)
  document.fetch("paths").each_with_object({}) do |(raw_path, definition), result|
    path = swagger ? raw_path.sub(%r{\A/api/v1(?=/|\z)}, "") : raw_path
    next if swagger && IGNORED_SWAGGER_PATHS.include?(path)

    definition.each do |method, operation|
      result[[method.upcase, path]] = operation if HTTP_METHODS.include?(method.downcase)
    end
  end
end

def resolve(document, value)
  reference = value.is_a?(Hash) ? value["$ref"] : nil
  return value unless reference&.start_with?("#/")

  reference.delete_prefix("#/").split("/").reduce(document) do |current, token|
    current.fetch(token.gsub("~1", "/").gsub("~0", "~"))
  end
end

def interface_parameters(document, operation)
  Array(operation["parameters"]).filter_map do |raw|
    parameter = resolve(document, raw)
    next unless %w[path query].include?(parameter["in"])

    [parameter["in"], parameter["name"], parameter["required"] == true]
  end.sort
end

documented_map = operation_map(contract)
implemented_map = operation_map(swagger, swagger: true)
documented = documented_map.keys.to_set
implemented = implemented_map.keys.to_set
missing = implemented - documented
obsolete = documented - implemented
drift = []

(implemented & documented).sort.each do |key|
  actual = implemented_map.fetch(key)
  expected = documented_map.fetch(key)
  if key.first == "GET"
    actual_parameters = interface_parameters(swagger, actual)
    expected_parameters = interface_parameters(contract, expected)
    if actual_parameters != expected_parameters
      drift << "#{key.join(' ')}: parámetros Swagger=#{actual_parameters.inspect}, OpenAPI=#{expected_parameters.inspect}"
    end
  end
  if actual.key?("requestBody") != expected.key?("requestBody")
    drift << "#{key.join(' ')}: presencia de requestBody diferente"
  end
  actual_public = Array(actual["security"]).empty?
  expected_public = Array(expected["security"]).empty?
  if actual_public != expected_public
    drift << "#{key.join(' ')}: autenticación diferente"
  end
end

unless missing.empty? && obsolete.empty? && drift.empty?
  warn "El OpenAPI versionado no coincide con el Swagger generado por la aplicación."
  unless missing.empty?
    warn "\nFaltan en docs/api/openapi.yaml:"
    missing.sort.each { |method, path| warn "  #{method} #{path}" }
  end
  unless obsolete.empty?
    warn "\nSobran en docs/api/openapi.yaml:"
    obsolete.sort.each { |method, path| warn "  #{method} #{path}" }
  end
  unless drift.empty?
    warn "\nDiferencias de interfaz:"
    drift.each { |difference| warn "  #{difference}" }
  end
  exit 1
end

puts "OpenAPI y Swagger coinciden: #{documented.length} operaciones."
