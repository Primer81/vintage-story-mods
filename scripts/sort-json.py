import json
import os
import argparse
import re
import traceback
import inspect
import pprint
from functools import reduce
from collections import OrderedDict
from debug import log
from typing import Any, Iterable

def parse_path_components(path):
    """Parse a path string into components (keys and indices)"""
    if not path:
        return []
    return re.findall(r'([^\.\[\]]+)|\[(\d+)\]', path)

def get_nested_value(obj, path):
    """Get a value from a nested object using a path with array index support"""
    if not path:
        return obj

    components = parse_path_components(path)
    # log("path = ", path)
    # log("components = ", components)
    current = obj

    for component in components:
        # log("component = ", component)
        key, _ = component
        # log("key = ", key)

        if key[0] != '[':  # It's a regular object key
            if isinstance(current, dict) and key in current:
                current = current[key]
            else:
                return None
        else:  # It's an array index
            index = int(index)
            if isinstance(current, list) and 0 <= index < len(current):
                current = current[index]
            else:
                return None

    return current

def navigate_to_parent(obj, components):
    """Navigate to the parent object of the target path"""
    if not components:
        return None, None

    current = obj

    # Navigate to the parent of the target
    for i in range(len(components) - 1):
        key, index = components[i]

        if key:  # It's a regular object key
            if key not in current or not isinstance(current[key], (dict, list)):
                current[key] = {} if components[i+1][0] else []
            current = current[key]
        elif index:  # It's an array index
            index = int(index)
            if not isinstance(current, list):
                return None, None  # Can't set an index on a non-list

            # Extend the list if needed
            while len(current) <= index:
                current.append({} if components[i+1][0] else [])

            current = current[index]

    return current, components[-1]

def set_nested_value(obj, path, value):
    """Set a value in a nested object using a path with array index support"""
    if not path:
        return

    components = parse_path_components(path)

    if not components:
        return

    parent, final_component = navigate_to_parent(obj, components)

    if parent is None:
        return

    # Set the value at the final location
    final_key, final_index = final_component

    if final_key:  # It's a regular object key
        parent[final_key] = value
    elif final_index:  # It's an array index
        final_index = int(final_index)
        if not isinstance(parent, list):
            return  # Can't set an index on a non-list

        # Extend the list if needed
        while len(parent) <= final_index:
            parent.append(None)

        parent[final_index] = value

def apply_regex(value, regex_pattern):
    """Apply regex pattern to extract substring from a string value"""
    if not isinstance(value, str) or not regex_pattern:
        return value

    log("regex_pattern = ", regex_pattern)
    log("value = ", value)
    match = re.search(regex_pattern, value)
    if match:
        log("match = ", match[0])
        # If there are capture groups, return the first one, otherwise return the whole match
        return match.group(1) if match.lastindex else match.group(0)
    return value

def get_path_to_first_sortable_value(data) -> str:
    path_to_first_sortable_value: str = (
        '.'.join(get_path_to_first_sortable_value_help(data)))
    return log(path_to_first_sortable_value)

def get_path_to_first_sortable_value_help(data) -> str:
    """Get the the path to the first sortable value"""

    if not data:
        pass

    elif isinstance(data, list):
        for path in get_path_to_first_sortable_value_help(data[0]):
            yield path

    elif isinstance(data, dict):
        first_key = sorted(data.keys())[0]
        yield first_key
        for path in get_path_to_first_sortable_value_help(data[first_key]):
            yield path

def find_matching_sort_key(items, sort_keys):
    """Find the first sort key that exists in at least one item"""
    for sort_key in sort_keys:
        if any(get_nested_value(item, sort_key) is not None for item in items):
            return sort_key
    return None

def create_sort_function_regex(key, regex_pattern):
    """Create a sorting function for the given key and regex pattern"""
    def sort_function(item: Any):
        value = get_nested_value(item, key)
        if value is not None:
            return apply_regex(value, regex_pattern)
        return None  # Items without the key will be sorted together
    return sort_function

# def create_sort_function_repr(key, regex_pattern):
#     """Create a sorting function for the given key and value representation"""
#     def sort_function(item: Any):
#         value = get_nested_value(item, key)
#         if value is not None:
#             return repr(value)
#         return None  # Items without the key will be sorted together
#     return sort_function

def create_key_sort_function(regex_pattern):
    """Create a sorting function for dictionary keys using the given regex pattern"""
    def key_function(item: tuple[Any, Any]):
        key, _ = item
        # log("key is sortable: ", key)
        return apply_regex(key, regex_pattern)
    return key_function

def sort_list(data, sort_keys, regex_patterns):
    """Sort a list of objects based on matching sort keys"""
    if not data or not isinstance(data[0], dict):
        return data

    # If no sort keys provided, use the first key in the first item
    if not sort_keys:
        first_key = get_path_to_first_sortable_value(data)
        # log("first_key = ", first_key)
        if first_key:
            sort_keys = [first_key]
        else:
            return data  # Can't determine a sort key

    # Find the first sort key that exists in at least one item
    matching_key = find_matching_sort_key(data, sort_keys)

    if matching_key:
        # Get the regex pattern for this sort key if available
        regex_pattern = regex_patterns.get(matching_key)
        log("matching_key = ", matching_key)
        log("regex_pattern = ", regex_pattern)

        # try:
            # Create the sorting function for comparable types
        sort_function = create_sort_function_regex(matching_key, regex_pattern)
        return sorted(data, key=sort_function)
        # except TypeError:
        #     # Create the sorting function for incomparable types
        #     sort_function = create_sort_function_repr(matching_key, regex_pattern)
        #     return sorted(data, key=sort_function)

    return data

def sort_dict_keys(data, regex_pattern=None):
    """Sort the keys of a dictionary, optionally using a regex pattern"""
    if regex_pattern:
        key_function = create_key_sort_function(regex_pattern)
        sorted_items = sorted(data.items(), key=key_function)
    else:
        sorted_items = sorted(data.items())

    result = OrderedDict()
    for k, v in sorted_items:
        result[k] = v

    return result

def sort_recursively(data, sort_keys=None, regex_patterns=None, sort_object_keys=False):
    """Sort all nested lists and objects recursively"""
    if regex_patterns is None:
        regex_patterns = {}

    if isinstance(data, list):
        # First sort any nested structures within each list item
        for item in data:
            if isinstance(item, (dict, list)):
                sort_recursively(item, sort_keys, regex_patterns, sort_object_keys)

        # Then sort the list itself if possible
        if data and isinstance(data[0], dict):
            # For recursive sorting, if no sort_keys provided, use the first key in the first item
            if not sort_keys:
                # log("data = ", data)
                first_key = get_path_to_first_sortable_value(data)
                # log("first_key = ", first_key)
                if first_key:
                    local_sort_keys = [first_key]
                else:
                    local_sort_keys = []
            else:
                local_sort_keys = sort_keys

            sorted_data = sort_list(data, local_sort_keys, regex_patterns)
            data[:] = sorted_data

    elif isinstance(data, dict):
        # First sort any nested structures
        for key, value in list(data.items()):
            if isinstance(value, (dict, list)):
                sort_recursively(value, sort_keys, regex_patterns, sort_object_keys)

        # Then sort the dictionary keys if requested
        if sort_object_keys:
            # If we have a regex pattern for object keys, apply it
            regex_pattern = regex_patterns.get("__object_keys__")
            sorted_dict = sort_dict_keys(data, regex_pattern)
            data.clear()
            data.update(sorted_dict)

def process_target(target, json_path, sort_keys, regex_patterns, sort_object_keys, recursive, data):
    """Process a target object or list for sorting"""
    if target is None:
        log(f"Warning: The path '{json_path}' does not exist in the JSON file. Skipping.")
        return data

    if recursive:
        # Recursively sort all nested structures
        sort_recursively(target, sort_keys, regex_patterns, sort_object_keys)
    else:
        # Handle non-recursive sorting based on the type of target
        if isinstance(target, list):
            # If no sort_keys provided, use the first key in the first item
            if not sort_keys and target and isinstance(target[0], dict):
                # log("first_key = ", first_key)
                first_key = get_path_to_first_sortable_value(target)
                if first_key:
                    sort_keys = [first_key]

            sorted_target = sort_list(target, sort_keys, regex_patterns)

            # Update the target with the sorted list
            if not json_path:
                data = sorted_target
            else:
                set_nested_value(data, json_path, sorted_target)

        elif isinstance(target, dict) and sort_object_keys:
            # Get the regex pattern for object keys if available
            regex_pattern = regex_patterns.get("__object_keys__")

            # Sort the dictionary keys
            sorted_target = sort_dict_keys(target, regex_pattern)

            # Update the target with the sorted dictionary
            if not json_path:
                data = sorted_target
            else:
                set_nested_value(data, json_path, sorted_target)

        else:
            log(f"Warning: The path '{json_path}' points to a {type(target).__name__}, not a list or object, or --sort-object-keys is not enabled. Skipping.")

    return data

def sort_json(input_file, output_file=None, json_paths=None, list_sort_keys=None,
              regex_patterns=None, sort_object_keys=False, recursive=False):
    """Sort a JSON file based on specified parameters"""
    if json_paths is None:
        json_paths = [""]
    if regex_patterns is None:
        regex_patterns = {}

    # If no output file is specified, overwrite the input file
    if output_file is None:
        output_file = input_file

    # Read the JSON file
    with open(input_file, 'r') as f:
        data = json.load(f)

    # Process each JSON path
    for json_path in json_paths:
        # Get the target to sort (list or object)
        target = data if not json_path else get_nested_value(data, json_path)
        data = process_target(target, json_path, list_sort_keys, regex_patterns, sort_object_keys, recursive, data)

    # Write the sorted data back to file
    with open(output_file, 'w') as f:
        json.dump(data, f, indent=4)

    log(f"Sorted JSON has been written to {output_file}")

def map_regex_patterns(json_paths, list_sort_keys, regex_patterns, sort_object_keys):
    """Map regex patterns to sort keys and paths"""
    regex_map = {}

    # Special handling for object keys
    if sort_object_keys and regex_patterns:
        regex_map["__object_keys__"] = regex_patterns[0]

    if not regex_patterns:
        return regex_map

    regex_index = 0
    current_regex = regex_patterns[0]

    # Process json_paths
    for i, path in enumerate(json_paths):
        # Check if we need to update the current regex
        if regex_index < len(regex_patterns) and i > 0:
            current_regex = regex_patterns[regex_index]
            regex_index += 1

        # Map the regex to the path
        regex_map[path] = current_regex

    # Process list_sort_keys
    if list_sort_keys:
        for key in list_sort_keys:
            if regex_index < len(regex_patterns):
                current_regex = regex_patterns[regex_index]
                regex_index += 1

            # Map the regex to the key
            regex_map[key] = current_regex

    return regex_map

def parse_arguments():
    """Parse command line arguments"""
    parser = argparse.ArgumentParser(description='Sort a JSON file based on specified parameters')
    parser.add_argument('input_file', help='Input JSON file to sort')
    parser.add_argument('-o', '--output-file', help='Output JSON file (defaults to overwriting input file)')
    parser.add_argument('-p', '--json-path', action='append', dest='json_paths',
                        help='Path to the list or object to sort. Supports array indices with [n] notation. (can be specified multiple times, default: root of JSON)')
    parser.add_argument('-k', '--list-sort-key', action='append', dest='list_sort_keys',
                        help='Path to the field to sort lists by. Supports array indices with [n] notation. (can be specified multiple times, default: first key in list items)')
    parser.add_argument('-x', '--regex-sort', action='append', dest='regex_patterns',
                        help='Regex pattern to extract substring for sorting. Applies to subsequent --json-path and --list-sort-key options.')
    parser.add_argument('--sort-object-keys', action='store_true',
                        help='Sort the keys of an object if the path points to an object instead of a list')
    parser.add_argument('-r', '--recursive', action='store_true',
                        help='Recursively sort all nested objects and lists')

    args = parser.parse_args()

    # Default values if not provided
    if not args.json_paths:
        args.json_paths = [""]

    # Note: We no longer set a default for list_sort_keys here
    # It will be determined dynamically based on the data

    return args

def main():
    args = parse_arguments()

    # Map regex patterns to sort keys and paths
    regex_map = map_regex_patterns(
        args.json_paths,
        args.list_sort_keys,
        args.regex_patterns,
        args.sort_object_keys
    )

    try:
        sort_json(
            log(args.input_file),
            log(args.output_file),
            log(args.json_paths),
            log(args.list_sort_keys),
            log(regex_map),
            log(args.sort_object_keys),
            log(args.recursive)
        )
    except Exception:
        traceback.print_exc()
        return 1

    return 0

if __name__ == "__main__":
    exit(main())
