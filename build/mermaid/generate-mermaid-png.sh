#!/usr/bin/env bash
DOCS_DIR="$(cd "$(dirname "$0")/../.." && pwd)/docs"

find "$DOCS_DIR" -name "*.mermaid" | while read -r file; do
    output="${file%.mermaid}.png"
    mmdc -i "$file" -o "$output"
    echo "Converted $file to $output"
done
