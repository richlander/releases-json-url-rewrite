#!/bin/bash

# Check if the input file is provided
if [ $# -ne 1 ]; then
  echo "Usage: $0 <input-json-file>"
  exit 1
fi

# Check if the file exists
JSON_FILE="$1"
if [ ! -f "$JSON_FILE" ]; then
  echo "File not found: $JSON_FILE"
  exit 1
fi

# Loop through each URL in the JSON file
jq -r '[.. | objects | select(has("url")) | .url] | .[]' "$JSON_FILE" | while read -r url; do
  echo -n "$url: "
  # Get the HTTP response code
  code=$(curl -s -o /dev/null -w "%{http_code}" -I "$url")
  echo "$code"

  # If the response code is 404, exit the script with an error
  if [ "$code" -ne 200 ]; then
    echo "Error: URL $url returned 404. Exiting..."
    exit 1
  fi
done
