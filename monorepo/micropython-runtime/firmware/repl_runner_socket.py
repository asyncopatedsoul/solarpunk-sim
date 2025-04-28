import code
import multiprocessing
import json
import time
import socket
import threading
import traceback
import sys
import importlib.util
import inspect
import types
from io import StringIO
import contextlib
import os

# --- Configuration ---
LIBRARY_PATH = "robot.py"
FILE_PATH = "custom_robot.py"  # Replace with the path to your user's Python file
WATCHED_VARS = ['robot_x', 'robot_y', 'robot_speed']  # List of global variable names to watch
SNAPSHOT_PORT = 12345  # Port for sending snapshots

# --- Global Variables ---
global_state = {}  # To store the tracked global variables
repl_locals = {}  # REPL's local namespace
snapshot_sender_queue = multiprocessing.Queue()  # Queue for snapshots
snapshot_receiver_queue = multiprocessing.Queue()  # Queue for snapshots


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


def send_snapshot(data):
    """Sends a JSON snapshot of the global state."""
    snapshot_sender_queue.put(json.dumps({"global_state": data}))


def repl_thread_function():
    """Runs the interactive REPL."""
    global watched_vars, repl_locals

    try:
        library_module = load_module_from_file(LIBRARY_PATH)
        user_module = load_module_from_file(FILE_PATH)
    except Exception as e:
        print(json.dumps({"type": "error", "error": f"Failed to load module: {e}", "traceback": traceback.format_exc()}))
        sys.exit(1)

    repl_locals = globals().copy()  # Start with globals from this script
    repl_locals.update(vars(library_module)) # Add globals from user's module
    repl_locals.update(vars(user_module))  # Add globals from user's module
    repl_locals['track_globals'] = track_globals

    original_setattr = __builtins__.setattr

    def custom_setattr(obj, name, value):
        print(f"Setting {obj} {name} to {value}")
        original_setattr(obj, name, value)
        send_snapshot(track_globals(user_module))
        
        # if name in WATCHED_VARS:
        #     send_snapshot(track_globals(user_module))

    __builtins__.setattr = custom_setattr

    console = code.InteractiveConsole(locals=repl_locals)
    console.interact("Interactive console with global variable watching")

    __builtins__.setattr = original_setattr


def snapshot_sender(queue):
    """Sends snapshots via socket."""
    try:
        sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        sock.bind(('localhost', SNAPSHOT_PORT))
        sock.listen(1)
        conn, addr = sock.accept()
        print(f"Connection from {addr}")

        while True:
            data = queue.get()
            conn.sendall(data.encode('utf-8') + b'\n')  # Send with delimiter

    except Exception as e:
        traceback.print_exc()
        print(f"Snapshot sender error: {e}")
    finally:
        if sock:
            sock.close()


if __name__ == "__main__":
    # Start the snapshot sender process
    sender_process = multiprocessing.Process(target=snapshot_sender, args=(snapshot_sender_queue,))
    sender_process.daemon = True
    sender_process.start()

    # Run the REPL in the main thread
    repl_thread = threading.Thread(target=repl_thread_function)
    repl_thread.start()
    repl_thread.join()

    print("REPL finished. Cleaning up...")
    sender_process.terminate()  # Ensure cleanup

    print("Script finished.")