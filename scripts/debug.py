import os
import sys
import pprint
import inspect
import types
from pprint import PrettyPrinter
from io import StringIO

class EarlyExitWriter(StringIO):
    def __init__(self):
        super().__init__()
        self.line_count = 0

    def write(self, s: str):
        if '\n' in s:
            if self.line_count == 0:
                self.line_count += 1
                # Only write up to the first newline
                first_line, _ = s.split('\n', 1)
                super().write(first_line)
            else:
                raise StopIteration("Multiline detected")
        super().write(s)

class EarlyExitPrettyPrinter(PrettyPrinter):
    def __init__(self, width=80, depth=None, indent=1, compact=False, sort_dicts=True, **kwargs):
        # Explicitly call PrettyPrinter.__init__ with all the standard parameters
        self.string_io = EarlyExitWriter()

        # Call the parent constructor with explicit parameters
        PrettyPrinter.__init__(
            self,
            width=width,
            depth=depth,
            indent=indent,
            compact=compact,
            sort_dicts=sort_dicts if sys.version_info >= (3, 8) else None,
            stream=self.string_io,
            **kwargs
        )

    def pformat(self, object):
        try:
            super().pprint(object)
            return False, self.string_io.getvalue()
        except StopIteration:
            return True, self.string_io.getvalue()

class FunctionPrinter:
    """Class to handle pretty printing of functions with their closures"""

    @staticmethod
    def get_function_details(func):
        """Extract details from a function including closures"""
        if not callable(func) or not isinstance(func, types.FunctionType):
            return None
        details = {
            'name': func.__name__,
            'module': func.__module__,
            'docstring': func.__doc__,
            'signature': str(inspect.signature(func)),
            'source_file': inspect.getfile(func),
            'line_number': inspect.getsourcelines(func)[1],
        }

        # Get closure variables if they exist
        if func.__closure__:
            closure_vars = {}
            # Get the names of free variables
            free_vars = func.__code__.co_freevars

            # Match free variable names with their values in the closure
            for i, name in enumerate(free_vars):
                value = func.__closure__[i].cell_contents
                closure_vars[name] = value

            details['closure'] = closure_vars

        return details

    @staticmethod
    def pformat(func, width=80):
        """Format a function with its details"""
        details = FunctionPrinter.get_function_details(func)
        if not details:
            return f"<non-function object: {type(func).__name__}>"

        # Format the function details
        lines = [f"<function {details['name']} at {hex(id(func))}>"]
        lines.append(f"  Signature: {details['signature']}")

        if details.get('docstring'):
            doc = details['docstring'].strip()
            if len(doc) > width - 10:  # Truncate long docstrings
                doc = doc[:width - 13] + "..."
            lines.append(f"  Docstring: {doc}")

        lines.append(f"  Defined in: {details['source_file']}:{details['line_number']}")

        # Add closure variables if they exist
        if 'closure' in details:
            lines.append("  Closure variables:")
            for name, value in details['closure'].items():
                # Format the value, handling multiline representations
                value_str = pprint.pformat(value, width=width-10)
                if '\n' in value_str:
                    # For multiline values, add indentation
                    value_lines = value_str.split('\n')
                    value_str = value_lines[0] + '...'
                lines.append(f"    {name} = {value_str}")

        return '\n'.join(lines)

def lazy_pformat(obj, width=None, depth=None, compact=None, sort_dicts=None, indent=None):
    """
    Checks if an object would be formatted as multiple lines by pprint.

    Returns (is_multiline, partial_result) where:
        - is_multiline: boolean indicating if formatting would use multiple lines
        - partial_result: the formatted string (complete if single line, partial if multiline)

    All formatting parameters match pprint.PrettyPrinter defaults unless specified.
    """
    # Special handling for functions
    if isinstance(obj, types.FunctionType):
        formatted = FunctionPrinter.pformat(obj, width=width or 80)
        return '\n' in formatted, formatted.split('\n', 1)[0] if '\n' in formatted else formatted

    # Create kwargs dict with only specified parameters
    printer_kwargs = {}
    if width is not None:
        printer_kwargs['width'] = width
    if depth is not None:
        printer_kwargs['depth'] = depth
    if compact is not None:
        printer_kwargs['compact'] = compact
    if indent is not None:
        printer_kwargs['indent'] = indent

    # sort_dicts parameter was added in Python 3.8
    if sys.version_info >= (3, 8) and sort_dicts is not None:
        printer_kwargs['sort_dicts'] = sort_dicts

    # Create printer with all the specified or default parameters
    printer = EarlyExitPrettyPrinter(**printer_kwargs)

    # Use our custom pformat method that returns the multiline status
    return printer.pformat(obj)

def pformat_function(func, width=80):
    """Pretty format a function with its closure variables"""
    return FunctionPrinter.pformat(func, width)

def log(*messages, delimiter: str = ' '):
    processed_messages: list[str] = []
    for message in messages:
        width=120
        if isinstance(message, str):
            processed_messages.append(message)
        elif isinstance(message, types.FunctionType):
            formatted = pformat_function(message, width=width)
            processed_messages.append(f"\n{formatted}")
        else:
            (is_multiline, pformat_object) = lazy_pformat(message, width=width)
            if is_multiline:
                # Process the entire message on its own line if multiline
                # and increase the indent by one
                processed_messages.append(f"\n{pprint.pformat(message, width=width)}")
            else:
                processed_messages.append(pformat_object)
    processed_message: str = (
        delimiter
            .join(processed_messages)
            .replace("\n", "\n\t")
            .rstrip()
    )
    print(
        f"["
            f"{os.path.relpath(inspect.getframeinfo(inspect.currentframe().f_back).filename)}:"
            f"{inspect.currentframe().f_back.f_code.co_name}:"
            f"{inspect.currentframe().f_back.f_lineno}"
        f"]",
        processed_message)
    if messages:
        if (0 < len(messages) <= 1):
            return messages[0]
        else:
            return messages
