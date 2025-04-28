import code
import json
import inspect
import types
import importlib.util
import sys
import traceback
from io import StringIO
import contextlib
import os
import multiprocessing

# --- Configuration ---
FILE_PATH = "./firmware/robot.py"  #
# FILE_PATH = "../firmware/robot.py"  # Replace with the path to your user's Python file
#Replace with the path to your user's Python file
WATCHED_VARS = ['robot_x', 'robot_y', 'robot_speed']  # List of global variable names to watch

# --- Global Variables ---
global_state = {}  # To store the tracked global variables
main_thread_queue = multiprocessing.Queue()  # Queue for communication with main thread
repl_locals = {}  # REPL's local namespace

# --- Helper Functions ---
def load_module_from_file(file_path):
    """Load a Python module from a file path."""
    spec = importlib.util.spec_from_file_location("user_module", file_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def track_globals(module):
    """Track global variables in the module and return a dictionary."""
    tracked_vars = {}
    for name in WATCHED_VARS:
        if hasattr(module, name):
            obj = getattr(module, name)
            try:
                if isinstance(obj, (int, float, str, bool, list, dict, tuple, set)):
                    tracked_vars[name] = obj
                elif hasattr(obj, '__dict__'):
                    obj_dict = {}
                    for attr, value in obj.__dict__.items():
                        if isinstance(value, (int, float, str, bool, list, dict, tuple, set)):
                            obj_dict[attr] = value
                    if obj_dict:
                        tracked_vars[name] = obj_dict
            except:
                pass  # Ignore if serialization fails
    return tracked_vars

def print_global_state(locals_dict):
    """Prints the global state as a JSON string."""
    global global_state
    tracked_state = {}
    for name in WATCHED_VARS:
        if name in locals_dict:
            try:
                # Safely serialize the variable
                tracked_state[name] = json.dumps(locals_dict[name])
            except TypeError:
                tracked_state[name] = str(locals_dict[name])  # Or handle differently
        elif name in global_state:
            tracked_state[name] = global_state[name] # Access from global store if available
    print(json.dumps({"global_state": tracked_state}, indent=4))


# --- Main Execution ---

if __name__ == "__main__":
    # 1. Load the User's Module
    try:
        user_module = load_module_from_file(FILE_PATH)
        print(f"Loaded module: {user_module}")
    except Exception as e:
        print(json.dumps({"error": f"Failed to load module: {e}", "traceback": traceback.format_exc()}))
        sys.exit(1)

    # 2. Prepare the REPL Environment
    # Initialize the REPL's local namespace with globals from the user's module and helper functions
    repl_locals = globals().copy() # Start with globals from this script
    repl_locals.update(vars(user_module)) # Add globals from user's module
    repl_locals['print_global_state'] = print_global_state
    repl_locals['track_globals'] = track_globals
    
    # Optionally restrict builtins for security (use with caution!)
    # repl_locals['__builtins__'] = {'print': print, 'len': len, 'int': int, 'float': float, 'str': str, 'bool': bool, 'list': list, 'dict': dict, 'tuple': tuple, 'set': set, 'range': range}
    
    # 3. Override __setattr__ to Track Changes
    original_setattr = __builtins__.setattr  # Store the original setattr

    def custom_setattr(obj, name, value):
        print(f"Setting {obj} {name} to {value}")
        """Custom setattr to track changes to watched variables."""
        original_setattr(obj, name, value)
        if name in WATCHED_VARS:
            print_global_state(repl_locals) # Print state on change

    __builtins__.setattr = custom_setattr

    # 4. Run the REPL
    console = code.InteractiveConsole(locals=repl_locals)
    console.interact("Interactive console with global variable watching")

    # 5. Restore original setattr (VERY IMPORTANT for cleanup)
    __builtins__.setattr = original_setattr