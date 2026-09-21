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

def operations(document, swagger: false)
  document.fetch("paths").each_with_object(Set.new) do |(raw_path, definition), result|
    path = swagger ? raw_path.sub(%r{\A/api/v1(?=/|\z)}, "") : raw_path
    next if swagger && IGNORED_SWAGGER_PATHS.include?(path)

    definition.each_key do |method|
      result << [method.upcase, path] if HTTP_METHODS.include?(method.downcase)
    end
  end
end

documented = operations(contract)
implemented = operations(swagger, swagger: true)
missing = implemented - documented
obsolete = documented - implemented

unless missing.empty? && obsolete.empty?
  warn "El OpenAPI versionado no coincide con el Swagger generado por la aplicación."
  unless missing.empty?
    warn "\nFaltan en docs/api/openapi.yaml:"
    missing.sort.each { |method, path| warn "  #{method} #{path}" }
  end
  unless obsolete.empty?
    warn "\nSobran en docs/api/openapi.yaml:"
    obsolete.sort.each { |method, path| warn "  #{method} #{path}" }
  end
  exit 1
end

puts "OpenAPI y Swagger coinciden: #{documented.length} operaciones."
