import argparse
import sys

def update_ini_setting(ini_file_path, section, setting, new_value):
    """
    Updates a specific setting in a specific section of an INI file.

    Args:
        ini_file_path (str): Path to the INI file
        section (str): Section name (without brackets)
        setting (str): Setting name to update
        new_value (str): New value to set

    Returns:
        bool: True if successful, False otherwise
    """
    try:
        with open(ini_file_path, 'r') as file:
            lines = file.readlines()
    except FileNotFoundError:
        print(f"Error: File '{ini_file_path}' not found.")
        return False
    except Exception as e:
        print(f"Error reading file: {e}")
        return False

    in_target_section = False
    setting_updated = False

    for i, line in enumerate(lines):
        # Check if we're entering the target section
        if line.strip() == f'[{section}]':
            in_target_section = True
            continue

        # Check if we're leaving the section
        if in_target_section and line.strip().startswith('['):
            in_target_section = False
            continue

        # Update the setting if we're in the right section
        if in_target_section and line.strip().startswith(f'{setting} = '):
            lines[i] = f'{setting} = {new_value}\n'
            setting_updated = True
            break

    if setting_updated:
        try:
            with open(ini_file_path, 'w') as file:
                file.writelines(lines)
            print(f"Successfully updated '{setting}' to '{new_value}' in section '{section}'.")
            return True
        except Exception as e:
            print(f"Error writing to file: {e}")
            return False
    else:
        print(f"Setting '{setting}' not found in section '{section}'.")
        return False

def main():
    parser = argparse.ArgumentParser(description='Update a setting in an INI file.')
    parser.add_argument('ini_file', help='Path to the INI file')
    parser.add_argument('section', help='Section name (without brackets)')
    parser.add_argument('setting', help='Setting name to update')
    parser.add_argument('value', help='New value to set')

    args = parser.parse_args()

    success = update_ini_setting(args.ini_file, args.section, args.setting, args.value)
    sys.exit(0 if success else 1)

if __name__ == '__main__':
    main()
