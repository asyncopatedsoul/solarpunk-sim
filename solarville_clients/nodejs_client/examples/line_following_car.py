# Line Following Remote Control Car Example
# This Python script demonstrates a simulated line-following car
# that can also be manually controlled.

import time
import math
import random

# Global variables for car state
elapsed_time = 0
car_mode = "idle"  # Can be "idle", "manual", or "line_follow"

# Simulated sensor values for line following
# In a real implementation, these would come from actual sensors
left_sensor = 0.0
center_sensor = 1.0
right_sensor = 0.0

# PID controller variables for line following
last_error = 0.0
integral = 0.0

# Simulated line pattern (changes over time)
# This simulates a line that curves left and right
def simulate_line_sensors():
    global left_sensor, center_sensor, right_sensor, elapsed_time
    
    # Create a pattern that changes over time (moves the line left and right)
    t = elapsed_time * 0.5
    position = math.sin(t) * 0.8  # Value between -0.8 and 0.8
    
    # Set sensor values based on where the "line" is
    # Each sensor has a value from 0.0 (no line) to 1.0 (line detected)
    center_range = 0.3
    side_range = 0.3
    
    left_sensor = max(0.0, min(1.0, 1.0 - abs(position + 0.5) / side_range))
    center_sensor = max(0.0, min(1.0, 1.0 - abs(position) / center_range))
    right_sensor = max(0.0, min(1.0, 1.0 - abs(position - 0.5) / side_range))
    
    # Add some noise
    left_sensor += random.uniform(-0.05, 0.05)
    center_sensor += random.uniform(-0.05, 0.05)
    right_sensor += random.uniform(-0.05, 0.05)
    
    # Clamp values
    left_sensor = max(0.0, min(1.0, left_sensor))
    center_sensor = max(0.0, min(1.0, center_sensor))
    right_sensor = max(0.0, min(1.0, right_sensor))

def setup():
    """
    Required setup function that runs once when the script starts.
    """
    global car_mode
    print("Line Following Car Firmware Starting...")
    
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
    
    # Simulate sensor readings
    simulate_line_sensors()
    
    # Handle different car modes
    if car_mode == "idle":
        # Do nothing
        set_motors(0, 0)
    elif car_mode == "manual":
        # Manual mode is handled by external commands
        pass
    elif car_mode == "line_follow":
        # Follow the line using PID control
        follow_line()

def follow_line():
    """
    Follow a line using PID control based on simulated sensor readings.
    """
    global last_error, integral
    
    # Calculate error: positive means line is to the right, negative means line is to the left
    error = right_sensor - left_sensor
    
    # PID constants
    kp = 0.7  # Proportional gain
    ki = 0.01  # Integral gain
    kd = 0.3   # Derivative gain
    
    # Calculate PID components
    p_term = kp * error
    integral += error * 0.1  # dt = 0.1 seconds
    i_term = ki * integral
    derivative = (error - last_error) / 0.1  # dt = 0.1 seconds
    d_term = kd * derivative
    
    # Calculate control output
    control = p_term + i_term + d_term
    
    # Save error for next iteration
    last_error = error
    
    # Base speed
    base_speed = 0.5
    
    # Calculate motor speeds
    left_speed = base_speed - control
    right_speed = base_speed + control
    
    # Clamp motor speeds
    left_speed = max(-1.0, min(1.0, left_speed))
    right_speed = max(-1.0, min(1.0, right_speed))
    
    # Set motor speeds
    set_motors(left_speed, right_speed)

# Functions to be called by the user or other systems

def set_car_mode(mode):
    """
    Set the car's operation mode.
    
    Args:
        mode (str): One of "idle", "manual", or "line_follow"
    """
    global car_mode, integral, last_error
    valid_modes = ["idle", "manual", "line_follow"]
    
    if mode in valid_modes:
        car_mode = mode
        
        # Reset PID when changing modes
        if mode == "line_follow":
            integral = 0.0
            last_error = 0.0
            
        print(f"Car mode changed to: {mode}")
    else:
        print(f"Invalid mode: {mode}. Must be one of {valid_modes}")

def manual_control(left, right):
    """
    Manually control the car's motors.
    Only works in "manual" mode.
    
    Args:
        left (float): Left motor speed (-1.0 to 1.0)
        right (float): Right motor speed (-1.0 to 1.0)
    """
    if car_mode == "manual":
        set_motors(left, right)
    else:
        print("Cannot control manually: car is not in manual mode")

def emergency_stop():
    """
    Immediately stop the car and set to idle mode.
    """
    global car_mode
    car_mode = "idle"
    set_motors(0, 0)
    print("EMERGENCY STOP ACTIVATED")

def get_sensor_values():
    """
    Return the current simulated sensor values.
    
    Returns:
        tuple: (left_sensor, center_sensor, right_sensor)
    """
    return (left_sensor, center_sensor, right_sensor)

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

print("Line following car firmware loaded.")
print("Available functions:")
print("- set_car_mode(mode): Set car mode to 'idle', 'manual', or 'line_follow'")
print("- manual_control(left, right): Manually control motors (when in manual mode)")
print("- emergency_stop(): Immediately stop the car")
print("- get_sensor_values(): Get current sensor readings")
