#!/bin/bash

# Core logic script for generating FILE_CONTENTS.txt.
# It lists tracked and key untracked files and outputs their content.

# --- Configuration ---
SEPARATOR="===================================================================================================="

# --- Function to generate the content ---
generate_content() {
  # --- Content Header (Goes to stdout / FILE_CONTENTS.txt) ---
  echo "PROJECT FILE SUMMARY (Generated: $(date))"
  echo "$SEPARATOR"
  echo "This summary includes tracked files and relevant untracked files, excluding common build/dependency directories."
  echo "$SEPARATOR"

  # Get the list of files:
  # Filter out common directories to prevent excessive output:
  FILE_LIST=$(git ls-files -c -o --exclude-standard 2>/dev/null |
    grep -v 'node_modules/' |
    grep -v 'build/' |
    grep -v 'dist/')

  if [ -z "$FILE_LIST" ]; then
    echo "No files found matching the criteria."
    return 0
  fi

  # Read the file list line by line
  IFS=$'\n'
  for file_path in $FILE_LIST; do
    if [ -z "$file_path" ]; then
      continue
    fi

    # Print path header (to stdout / FILE_CONTENTS.txt)
    echo ""
    echo "$SEPARATOR"
    echo "$file_path"
    echo "$SEPARATOR"
    echo ""

    # Print the file content (to stdout / FILE_CONTENTS.txt)
    if [ -f "$file_path" ]; then
      cat "$file_path"
    else
      echo "--- WARNING: File not found or not readable ---"
    fi
    echo ""
  done
}

# Execute the main function
generate_content
