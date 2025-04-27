#!/usr/bin/env python3
import sys
import json
import threading
import traceback
import time
import importlib.util
import types
import inspect
from io import StringIO
import contextlib

# Global variables
global_state = {}
output_buffer = StringIO()

def load_module_from_file(file_path):
    """Load a Python module from a file path."""
    spec = importlib.util.spec_from_file_location("user_module", file_path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module

def track_globals(module):
    """Track global variables in the module."""
    globals_dict = {}
    
    for name, obj in inspect.getmembers(module):
        # Skip private members and built-in methods/functions
        if name.startswith('_') or inspect.isbuiltin(obj):
            continue
        
        # Include functions, classes, and other non-module objects
        if not inspect.ismodule(obj):
            try:
                # Try to serialize the object to JSON
                if isinstance(obj, (int, float, str, bool, list, dict, tuple, set)):
                    globals_dict[name] = obj
                elif hasattr(obj, '__dict__'):
                    # For objects with __dict__, convert to dictionary
                    obj_dict = {}
                    for attr, value in obj.__dict__.items():
                        if not attr.startswith('_') and isinstance(value, (int, float, str, bool, list, dict, tuple, set)):
                            obj_dict[attr] = value
                    if obj_dict:
                        globals_dict[name] = obj_dict
            except:
                pass
    
    return globals_dict

def execute_code_in_thread(file_path, thread_id):
    """Execute the code in a thread and capture output."""
    global global_state, output_buffer
    
    try:
        # Redirect stdout to capture output
        with contextlib.redirect_stdout(output_buffer):
            # Load and execute the user module
            module = load_module_from_file(file_path)
            
            # Add thread information to the module
            module.thread_id = thread_id
            
            # Execute the code
            if hasattr(module, 'main'):
                module.main()
            
            # Track global variables
            thread_globals = track_globals(module)
            
            # Store the global state for this thread
            global_state[f"thread_{thread_id}"] = thread_globals
            
            # Send the global state update
            print(json.dumps({
                "type": "global_state",
                "state": global_state
            }))
            
            # If the module has defined left_motor_speed and right_motor_speed, send a state update
            if hasattr(module, 'left_motor_speed') and hasattr(module, 'right_motor_speed'):
                print(json.dumps({
                    "type": "state_update",
                    "leftMotorSpeed": module.left_motor_speed,
                    "rightMotorSpeed": module.right_motor_speed
                }))
    
    except Exception as e:
        # Print the error to be captured
        traceback_str = traceback.format_exc()
        print(json.dumps({
            "type": "error",
            "error": str(e),
            "traceback": traceback_str
        }))

def main():
    """Main entry point for the thread runner."""
    if len(sys.argv) < 2:
        print(json.dumps({
            "type": "error",
            "error": "No file path provided"
        }))
        sys.exit(1)
    
    file_path = sys.argv[1]
    
    try:
        # Create multiple threads to execute the code
        num_threads = 3  # Default number of threads
        threads = []
        
        for i in range(num_threads):
            thread = threading.Thread(target=execute_code_in_thread, args=(file_path, i))
            thread.daemon = True
            threads.append(thread)
            thread.start()
        
        # Set up REPL command handling
        while True:
            # Check if there's any output to send
            if output_buffer.getvalue():
                output = output_buffer.getvalue()
                output_buffer = StringIO()  # Reset the buffer
                print(json.dumps({
                    "type": "repl_output",
                    "output": output
                }))
            
            # Check for input commands
            try:
                command = input()
                if command:
                    # Try to parse the command as JSON
                    try:
                        command_data = json.loads(command)
                        if command_data.get("type") == "repl_command":
                            # Execute the command
                            try:
                                with contextlib.redirect_stdout(output_buffer):
                                    exec(command_data["command"], globals())
                                
                                # Send the new global state
                                print(json.dumps({
                                    "type": "global_state",
                                    "state": global_state
                                }))
                            except Exception as e:
                                traceback_str = traceback.format_exc()
                                print(json.dumps({
                                    "type": "repl_output",
                                    "output": f"Error: {str(e)}\n{traceback_str}"
                                }))
                    except json.JSONDecodeError:
                        # Not a valid JSON command, ignore
                        pass
            except EOFError:
                # End of input, exit the loop
                break
            
            # Sleep a bit to reduce CPU usage
            time.sleep(0.1)
    
    except Exception as e:
        traceback_str = traceback.format_exc()
        print(json.dumps({
            "type": "error",
            "error": str(e),
            "traceback": traceback_str
        }))
        sys.exit(1)

if __name__ == "__main__":
    main() 