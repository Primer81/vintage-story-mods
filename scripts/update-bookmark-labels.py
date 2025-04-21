import json
import os
import argparse
from debug import log

def read_json_file(file_path):
    """Read and parse a JSON file."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except Exception as e:
        log(f"Error reading JSON file {file_path}: {e}")
        return None

def write_json_file(file_path, data):
    """Write data to a JSON file."""
    try:
        with open(file_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2)
        log(f"Successfully updated bookmark labels in {file_path}")
        return True
    except Exception as e:
        log(f"Error writing to JSON file {file_path}: {e}")
        return False

def read_file_lines(file_path):
    """Read all lines from a file."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            return f.readlines()
    except Exception as e:
        log(f"Error reading file {file_path}: {e}")
        return None

def update_bookmark_label(bookmark, file_lines, force=False):
    """Update a bookmark's label if it's empty or if force is True."""
    if force or not bookmark.get("label"):
        line_number = bookmark.get("line", 0)

        if 0 <= line_number < len(file_lines):
            bookmark["label"] = file_lines[line_number].strip()
            return True
        else:
            return False
    return False

def process_file_bookmarks(file_entry, base_dir, force=False):
    """Process all bookmarks for a single file."""
    relative_path = file_entry.get("path")
    # Construct absolute path relative to the parent directory of bookmarks.json
    absolute_path = os.path.join(base_dir, relative_path)
    bookmarks = file_entry.get("bookmarks", [])

    if not os.path.exists(absolute_path):
        log(f"Warning: File not found: {absolute_path}")
        return 0

    file_lines = read_file_lines(absolute_path)
    if file_lines is None:
        return 0

    updated_count = 0
    for bookmark in bookmarks:
        if update_bookmark_label(bookmark, file_lines, force):
            updated_count += 1
        elif not bookmark.get("label") or (force and bookmark.get("line", 0) >= len(file_lines)):
            line_number = bookmark.get("line", 0)
            log(f"Warning: Invalid line number {line_number} in file {absolute_path}")

    return updated_count

def update_bookmark_labels(json_data, json_file_path, force=False):
    """Update bookmark labels in the JSON data."""
    # Get the parent directory of the bookmarks.json file
    base_dir = os.path.dirname(os.path.dirname(os.path.abspath(json_file_path)))
    log(f"Using base directory: {base_dir}")

    mode = "all" if force else "empty"
    log(f"Updating {mode} bookmark labels")

    total_updated = 0

    for file_entry in json_data.get("files", []):
        total_updated += process_file_bookmarks(file_entry, base_dir, force)

    log(f"Total bookmarks updated: {total_updated}")
    return json_data

def parse_arguments():
    """Parse command line arguments."""
    parser = argparse.ArgumentParser(description='Update bookmark labels in a JSON file.')
    parser.add_argument('json_file', help='Path to the JSON file containing bookmarks')
    parser.add_argument('-f', '--force', action='store_true',
                        help='Force update all labels regardless of current value')
    return parser.parse_args()

def main():
    args = parse_arguments()
    json_file_path = args.json_file
    force = args.force

    json_data = read_json_file(json_file_path)
    if json_data:
        updated_data = update_bookmark_labels(json_data, json_file_path, force)
        write_json_file(json_file_path, updated_data)

if __name__ == "__main__":
    main()
