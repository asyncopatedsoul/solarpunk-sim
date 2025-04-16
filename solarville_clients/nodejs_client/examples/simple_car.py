# Simple Remote Control Car Example
# This Python script demonstrates a simple remote-controlled car
# with basic autonomous behavior.

import time
import math
import random

# Global variables for car state
elapsed_time = 0
car_mode = "idle"  # Can be "idle", "forward", "backward", "left", "right", or "auto"

def setup():
    """
    Required setup function that runs once when the script starts.
    """
    global car_mode
    print("Remote Control Car Firmware Starting...")
    
    # Start in idle mode
    car_mode = "idle"
    set_motors(0, 0)

def loop():
    """
    Main loop function that runs repeatedly.
    This function is called approximately 10 times per second.
    """
    global elapsed_time, car_mode
    elapsed_time += 0.1  # Increment by 0.1 seconds each loop
    
    # Handle different car modes
    if car_mode == "idle":
        # Do nothing
        set_motors(0, 0)
    elif car_mode == "forward":
        # Move forward
        set_motors(0.7, 0.7)
    elif car_mode == "backward":
        # Move backward
        set_motors(-0.5, -0.5)
    elif car_mode == "left":
        # Turn left
        set_motors(-0.3, 0.7)
    elif car_mode == "right":
        # Turn right
        set_motors(0.7, -0.3)
    elif car_mode == "auto":
        # Autonomous mode - change directions every 3 seconds
        run_autonomous_mode()

def run_autonomous_mode():
    """
    Run the car in autonomous mode, changing directions periodically.
    """
    # Get a value between 0 and 1 that oscillates with period of 6 seconds
    phase = (elapsed_time % 6) / 6
    
    if phase < 0.25:
        # Move forward
        set_motors(0.7, 0.7)
    elif phase < 0.5:
        # Turn right
        set_motors(0.7, 0.2)
    elif phase < 0.75:
        # Move backward
        set_motors(-0.5, -0.5)
    else:
        # Turn left
        set_motors(0.2, 0.7)

# Functions to be called by the user or other systems

def set_car_mode(mode):
    """
    Set the car's operation mode.
    
    Args:
        mode (str): One of "idle", "forward", "backward", "left", "right", or "auto"
    """
    global car_mode
    valid_modes = ["idle", "forward", "backward", "left", "right", "auto"]
    
    if mode in valid_modes:
        car_mode = mode
        print(f"Car mode changed to: {mode}")
    else:
        print(f"Invalid mode: {mode}. Must be one of {valid_modes}")

def emergency_stop():
    """
    Immediately stop the car and set to idle mode.
    """
    global car_mode
    car_mode = "idle"
    set_motors(0, 0)
    print("EMERGENCY STOP ACTIVATED")

# These motor control functions are provided by the wrapper
# and will communicate the motor states back to the system
#
# def set_left_motor(speed):
#     # Implementation provided by wrapper
#     pass
#
# def set_right_motor(speed):
#     # Implementation provided by wrapper
#     pass
#
# def set_motors(left, right):
#     # Implementation provided by wrapper
#     pass

print("Remote control car firmware loaded.")
print("Available functions:")
print("- set_car_mode(mode): Set car mode to 'idle', 'forward', 'backward', 'left', 'right', or 'auto'")
print("- emergency_stop(): Immediately stop the car")
