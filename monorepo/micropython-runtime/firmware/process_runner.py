#!/usr/bin/env python3
import sys
import json
import multiprocessing
import traceback
import time
import importlib.util
import types
import inspect
from io import StringIO
import contextlib
import os

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

def execute_code_in_process(file_path, process_id, global_state, output_queue):
    """Execute the code in a process and capture output."""
    output_buffer = StringIO()
    print(f"Executing code at {file_path} in process {process_id}")
    try:
        # Redirect stdout to capture output
        with contextlib.redirect_stdout(output_buffer):
            # Load and execute the user module
            module = load_module_from_file(file_path)
            
            # Add process information to the module
            module.process_id = process_id
            
            # Execute the code
            if hasattr(module, 'main'):
                module.main()
            
            # Track global variables
            process_globals = track_globals(module)
            
            # Store the global state for this process
            global_state[f"process_{process_id}"] = process_globals
            
            # If the module has defined left_motor_speed and right_motor_speed, send a state update
            if hasattr(module, 'left_motor_speed') and hasattr(module, 'right_motor_speed'):
                output_queue.put(json.dumps({
                    "type": "state_update",
                    "leftMotorSpeed": module.left_motor_speed,
                    "rightMotorSpeed": module.right_motor_speed
                }))
    
    except Exception as e:
        # Put the error in the output queue
        traceback_str = traceback.format_exc()
        output_queue.put(json.dumps({
            "type": "error",
            "error": str(e),
            "traceback": traceback_str
        }))
    
    # Put any output in the queue
    if output_buffer.getvalue():
        output_queue.put(json.dumps({
            "type": "repl_output",
            "output": output_buffer.getvalue()
        }))

def main():
    """Main entry point for the process runner."""
    if len(sys.argv) < 2:
        print(json.dumps({
            "type": "error",
            "error": "No file path provided"
        }))
        sys.exit(1)
    
    file_path = sys.argv[1]
    
    try:
        # Create multiple processes to execute the code
        # num_processes = min(os.cpu_count() or 2, 3)  # Default number of processes (max 3)
        num_processes = 1
        processes = []
        
        for i in range(num_processes):
            process = multiprocessing.Process(
                target=execute_code_in_process, 
                args=(file_path, i, global_state, output_queue)
            )
            process.daemon = True
            processes.append(process)
            process.start()
        
        # Set up REPL command handling and process monitoring
        while any(p.is_alive() for p in processes):
            # Check if there's any output in the queue
            try:
                while not output_queue.empty():
                    output = output_queue.get_nowait()
                    print(output)
                
                # Send the global state update periodically
                print(json.dumps({
                    "type": "global_state",
                    "state": dict(global_state)
                }))
                
                # Check for input commands
                try:
                    command = input()
                    if command:
                        print(f"Received command: {command}")
                        # Try to parse the command as JSON
                        try:
                            command_data = json.loads(command)
                            if command_data.get("type") == "repl_command":
                                # We can't directly execute commands in other processes,
                                # but we can report that this isn't supported
                                print(json.dumps({
                                    "type": "repl_output",
                                    "output": "REPL commands in multi-process mode are not supported. Use multi-thread mode for REPL interaction."
                                }))
                        except json.JSONDecodeError:
                            # Not a valid JSON command, ignore
                            pass
                except EOFError:
                    # End of input, exit the loop
                    break
            except Exception as e:
                print(json.dumps({
                    "type": "error",
                    "error": f"Error in main process: {str(e)}"
                }))
            
            # Sleep a bit to reduce CPU usage
            time.sleep(0.1)
        
        # Wait for all processes to complete
        for process in processes:
            process.join(timeout=1.0)
            if process.is_alive():
                process.terminate()
        
        # Send the final global state
        print(json.dumps({
            "type": "global_state",
            "state": dict(global_state)
        }))
    
    except Exception as e:
        traceback_str = traceback.format_exc()
        print(json.dumps({
            "type": "error",
            "error": str(e),
            "traceback": traceback_str
        }))
        sys.exit(1)

if __name__ == "__main__":
    # Global variables MUST be created within this block on some platforms
    manager = multiprocessing.Manager()
    global_state = manager.dict()
    output_queue = multiprocessing.Queue()
    print(f"Global state: {global_state}")
    main()