import code
import multiprocessing
import json
import time
import threading
import traceback
import sys
import importlib.util
import inspect
import types
from io import StringIO
import contextlib
import os
import asyncio
import websockets

# --- Configuration ---
LIBRARY_PATH = "robot.py"
FILE_PATH = "custom_robot.py"
WATCHED_VARS = ['robot_x', 'robot_y', 'robot_speed']
WS_URI = "ws://localhost:8765"  # Replace with your WebSocket server URI

# --- Global Variables ---
global_state = {}
repl_locals = {}
snapshot_send_queue = asyncio.Queue()
snapshot_recv_queue = asyncio.Queue()
running = True
asyncio_loop = None
ws = None
ws_connected = False
snapshot_count = 0

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
                pass
    return tracked_vars


def track_locals():
    tracked_vars = {}
    for name in repl_locals.keys():
        if name in repl_locals.keys():
            tracked_vars[name] = repl_locals[name]
    return tracked_vars


async def send_snapshot(data):
    """Sends a JSON snapshot of the global state via WebSocket."""
    global ws, ws_connected, asyncio_loop, snapshot_count
    if asyncio_loop and not asyncio_loop.is_closed():  # Check if loop is running
        try:
            # if ws is None:
            #     await connect_websocket()  # Ensure connection before sending
            if ws_connected:
                await ws.send(json.dumps({"type": "state_update", "state": data, "snapshot_count": snapshot_count}))
                snapshot_count += 1
            else:
                print("send_snapshot: WebSocket not connected.")
        except Exception as e:
            print(f"Error sending snapshot: {e}")
            ws_connected = False
            # await connect_websocket()  # Attempt to reconnect
    else:
        print("send_snapshot: asyncio loop is not running.")


async def receive_snapshots():
    """Receives snapshots from the WebSocket and puts them in the queue."""
    global running, asyncio_loop, ws, ws_connected

    if asyncio_loop and not asyncio_loop.is_closed():  # Check if loop is running
        try:
            # await connect_websocket()  # Ensure connection before receiving
            while running and not asyncio_loop.is_closed():
                if ws_connected:
                    try:
                        message = await ws.recv()
                        if message is None:
                            print("WebSocket connection closed.")
                            break
                        data = json.loads(message)
                        print("received data: ", data)
                        if data['type'] == 'state_update':
                            await snapshot_recv_queue.put(data['state'])
                    except websockets.exceptions.ConnectionClosedOK:
                        print("WebSocket connection closed normally.")
                        break
                    except websockets.exceptions.ConnectionClosedError:
                        print("WebSocket connection closed with error.")
                        ws_connected = False
                        await connect_websocket()  # Attempt to reconnect
                    except Exception as e:
                        print(f"Error receiving snapshots: {e}")
                        traceback.print_exc()
                else:
                    await asyncio.sleep(1)  # Wait a bit before retrying connection
        except Exception as e:
            print(f"receive_snapshots outer loop exception: {e}")
            traceback.print_exc()
        finally:
            print("receive_snapshots: Exiting.")
    else:
        print("receive_snapshots: asyncio loop is not running.")


async def process_send_queue():
    """Processes the queue of snapshots to send."""
    global running, asyncio_loop

    if asyncio_loop and not asyncio_loop.is_closed():  # Check if loop is running
        try:
            while running and not asyncio_loop.is_closed():
                try:
                    data = await snapshot_send_queue.get()
                    await send_snapshot(data)
                except Exception as e:
                    print(f"Error processing send queue: {e}")
        except Exception as e:
            print(f"process_send_queue exception: {e}")
            traceback.print_exc()
        finally:
            print("process_send_queue: Exiting.")
    else:
        print("process_send_queue: asyncio loop is not running.")


async def connect_websocket():
    """Connects to the WebSocket server."""
    global ws, ws_connected, asyncio_loop
    retry_delay = 1
    while asyncio_loop and not asyncio_loop.is_closed():  # Check if loop is running
        try:
            ws = await websockets.connect(WS_URI)
            print(f"Connected to WebSocket at {WS_URI}")
            ws_connected = True
            break
        except Exception as e:
            print(f"WebSocket connection failed: {e}. Retrying in {retry_delay} seconds...")
            await asyncio.sleep(retry_delay)
            retry_delay = min(retry_delay * 2, 10)  # Exponential backoff
        if asyncio_loop.is_closed():
            print("connect_websocket: asyncio loop is closed, exiting")
            break
    if asyncio_loop is None or asyncio_loop.is_closed():
        print("connect_websocket: asyncio loop is not running, connection attempt aborted.")


def repl_thread_function():
    """Runs the interactive REPL."""
    global watched_vars, repl_locals, snapshot_send_queue, asyncio_loop

    try:
        library_module = load_module_from_file(LIBRARY_PATH)

        # now load the user module with the modified Robot class
        user_module = load_module_from_file(FILE_PATH)

    except Exception as e:
        print(json.dumps({"type": "error", "error": f"Failed to load module: {e}", "traceback": traceback.format_exc()}))
        sys.exit(1)

    def track_self():
        if asyncio_loop and not asyncio_loop.is_closed():  # Check if loop is running
            asyncio.run_coroutine_threadsafe(snapshot_send_queue.put(user_module.tracked_vars), asyncio_loop)
        else:
            print("track_self: asyncio loop is not running.")

    repl_locals = globals().copy()
    repl_locals.update(vars(library_module))
    repl_locals.update(vars(user_module))

    # injecting methods into the class
    # class_name = "Robot"
    #  # Get the class
    # target_class = getattr(module, class_name)
    # # Inject the method
    # setattr(target_class, method_name, method_func)

    # bind the snapshot_queue and asyncio_loop to the Robot class
    target_class = getattr(library_module, 'Robot')
    original_init = target_class.__init__  # Store the original
    print("target_class: ", target_class)
    print("original_init: ", original_init)

    def extended_init(self, *args, **kwargs):
        original_init(self, *args, **kwargs)  # Call the original
        # new_init_func(self, *args, **kwargs)  # Call the new
        self.snapshot_queue = snapshot_send_queue
        self.asyncio_loop = asyncio_loop
        print("extended Robot initialized")

    target_class.__init__ = extended_init  # Replace with extended
    repl_locals['Robot'] = target_class

    repl_locals['track_globals'] = track_globals
    repl_locals['user_module'] = user_module
    repl_locals['track_self'] = track_self

    original_setattr = __builtins__.setattr

    def custom_setattr(obj, name, value):
        original_setattr(obj, name, value)
        # Send snapshot via asyncio queue
        if asyncio_loop and not asyncio_loop.is_closed():  # Check if loop is running
            asyncio.run_coroutine_threadsafe(snapshot_send_queue.put(track_globals(user_module)), asyncio_loop)
        else:
            print("custom_setattr: asyncio loop is not running.")
        # asyncio.run_coroutine_threadsafe(snapshot_send_queue.put(track_locals()), asyncio_loop)
        # asyncio.run_coroutine_threadsafe(snapshot_send_queue.put(user_module.tracked_vars), asyncio_loop)

    # __builtins__.setattr = custom_setattr

    console = code.InteractiveConsole(locals=repl_locals)
    console.interact("Interactive console with global variable watching")

    # __builtins__.setattr = original_setattr


def run_asyncio_loop():
    """Runs the asyncio event loop."""
    global asyncio_loop, running
    asyncio_loop = asyncio.new_event_loop()
    asyncio.set_event_loop(asyncio_loop)

    tasks = [
        receive_snapshots(),
        process_send_queue(),
    ]
    asyncio.run_coroutine_threadsafe(connect_websocket(), asyncio_loop)  # Start connect_websocket

    try:
        asyncio_loop.run_until_complete(asyncio.gather(*tasks))
    except Exception as e:
        print(f"Error in asyncio loop: {e}")
        traceback.print_exc()
    finally:
        print("Closing asyncio loop.")
        asyncio_loop.close()
        running = False


if __name__ == "__main__":
    # Start the asyncio loop in a separate thread
    asyncio_thread = threading.Thread(target=run_asyncio_loop)
    asyncio_thread.daemon = True
    asyncio_thread.start()

    # Run the REPL in the main thread
    repl_thread = threading.Thread(target=repl_thread_function)
    repl_thread.start()
    repl_thread.join()

    print("REPL finished. Cleaning up...")
    running = False  # Signal to stop asyncio tasks
    if asyncio_loop and not asyncio_loop.is_closed():
        asyncio.run_coroutine_threadsafe(asyncio_loop.stop(), asyncio_loop)  # Stop the loop safely
    asyncio_thread.join()  # Wait for asyncio thread to finish

    print("Script finished.")