#!/usr/bin/env ruby

require "find"
require "pathname"
require "psych"

ROOT = Pathname.new(__dir__).join("..", "..").expand_path
IGNORED_DIRECTORIES = [
  ".git",
  "bin",
  "obj"
].freeze
YAML_EXTENSIONS = [
  ".yaml",
  ".yml"
].freeze

def ignored_path?(path)
  relative_parts = Pathname.new(path).relative_path_from(ROOT).each_filename.to_a

  relative_parts.any? { |part| IGNORED_DIRECTORIES.include?(part) }
end

yaml_files = []

Find.find(ROOT.to_s) do |path|
  if File.directory?(path) && ignored_path?(path)
    Find.prune
    next
  end

  next unless File.file?(path)
  next unless YAML_EXTENSIONS.include?(File.extname(path))

  yaml_files << Pathname.new(path).relative_path_from(ROOT).to_s
end

if yaml_files.empty?
  warn "No YAML files found."
  exit 1
end

yaml_files.sort.each do |relative_path|
  Psych.parse_file(ROOT.join(relative_path).to_s)
  puts "ok #{relative_path}"
end
