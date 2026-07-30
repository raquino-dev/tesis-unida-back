#!/usr/bin/env ruby
# frozen_string_literal: true

ROOT = File.expand_path("..", __dir__)
SCOPE_PATH = File.join(ROOT, "docs/ALCANCE_Y_REQUISITOS.md")
RF_TRACE_PATH = File.join(ROOT, "docs/api/TRAZABILIDAD_RF.md")
RNF_TRACE_PATH = File.join(ROOT, "docs/api/TRAZABILIDAD_RNF.md")

errors = []
markdown_paths = Dir.glob(File.join(ROOT, "{README.md,docs/**/*.md,backend/README.md}"))

markdown_paths.each do |path|
  contents = File.read(path, encoding: "UTF-8")
  relative_path = path.delete_prefix("#{ROOT}/")

  errors << "#{relative_path}: cantidad impar de delimitadores de código" if contents.scan(/^```/).length.odd?

  contents.scan(/\[[^\]]+\]\(([^)]+\.md)(?:#[^)]+)?\)/).flatten.each do |target|
    next if target.match?(%r{\Ahttps?://})

    resolved = File.expand_path(target, File.dirname(path))
    errors << "#{relative_path}: enlace local inexistente #{target}" unless File.file?(resolved)
  end
rescue ArgumentError => error
  errors << "#{relative_path}: no es UTF-8 válido (#{error.message})"
end

def requirement_ids(path, prefix)
  File.read(path, encoding: "UTF-8").scan(/\b#{Regexp.escape(prefix)}-(\d{2})\b/).flatten.uniq.sort
end

expected_rf = (1..21).map { |number| format("%02d", number) }
expected_rnf = (1..25).map { |number| format("%02d", number) }

{
  SCOPE_PATH => expected_rf,
  RF_TRACE_PATH => expected_rf
}.each do |path, expected|
  actual = requirement_ids(path, "RF")
  errors << "#{path.delete_prefix("#{ROOT}/")}: RF #{actual.inspect}, esperados #{expected.inspect}" unless actual == expected
end

{
  SCOPE_PATH => expected_rnf,
  RNF_TRACE_PATH => expected_rnf
}.each do |path, expected|
  actual = requirement_ids(path, "RNF")
  errors << "#{path.delete_prefix("#{ROOT}/")}: RNF #{actual.inspect}, esperados #{expected.inspect}" unless actual == expected
end

required_stack_terms = [
  "Flutter",
  ".NET 10",
  "Supabase",
  "Amazon S3",
  "Redis",
  "Amazon SES",
  "Hetzner CX33",
  "Docker Compose",
  "systemd",
  "Nginx",
  "Cloudflare",
  "Google Play"
]
scope = File.read(SCOPE_PATH, encoding: "UTF-8")
required_stack_terms.each do |term|
  errors << "docs/ALCANCE_Y_REQUISITOS.md: falta la decisión tecnológica #{term}" unless scope.include?(term)
end

if errors.empty?
  puts "Documentación válida: #{markdown_paths.length} archivos, RF-01..21 y RNF-01..25 trazados"
else
  warn errors.join("\n")
  exit 1
end
