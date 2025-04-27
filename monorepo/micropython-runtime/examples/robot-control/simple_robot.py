# Simple Robot Control Example
# This script demonstrates basic control of a robot with two motors

# Global variables to store state
left_motor = 0.0
right_motor = 0.0
led_state = False
counter = 0

def set_left_motor(speed):
    """
    Set the speed of the left motor
    
    Parameters:
    speed (float): Speed value between -1.0 (full reverse) and 1.0 (full forward)
    """
    global left_motor
    left_motor = max(-1.0, min(1.0, speed))  # Clamp between -1.0 and 1.0
    # Output the motor speed for the Node.js process to capture
    print(f'{{"leftMotorSpeed": {left_motor}}}')

def set_right_motor(speed):
    """
    Set the speed of the right motor
    
    Parameters:
    speed (float): Speed value between -1.0 (full reverse) and 1.0 (full forward)
    """
    global right_motor
    right_motor = max(-1.0, min(1.0, speed))  # Clamp between -1.0 and 1.0
    # Output the motor speed for the Node.js process to capture
    print(f'{{"rightMotorSpeed": {right_motor}}}')

def set_motors(left_speed, right_speed):
    """
    Set the speed of both motors at once
    
    Parameters:
    left_speed (float): Speed value for left motor between -1.0 and 1.0
    right_speed (float): Speed value for right motor between -1.0 and 1.0
    """
    set_left_motor(left_speed)
    set_right_motor(right_speed)

def toggle_led():
    """Toggle the LED state"""
    global led_state
    led_state = not led_state
    print(f'{{"ledState": {str(led_state).lower()}}}')

def read_distance_sensor():
    """
    Simulate reading from a distance sensor
    
    Returns:
    float: Distance in meters (simulated value)
    """
    # In a real robot, this would read from actual hardware
    # Here we just return a simulated value
    import random
    return random.uniform(0.1, 2.0)

def setup():
    """Initialize the robot"""
    print("Robot initialized")
    set_motors(0, 0)  # Start with motors stopped

def loop():
    """Main control loop - runs repeatedly"""
    global counter
    
    # Get simulated sensor reading
    distance = read_distance_sensor()
    
    # Simple obstacle avoidance logic
    if distance < 0.5:
        # Obstacle detected, turn
        set_motors(-0.3, 0.3)  # Turn by setting motors to opposite directions
        toggle_led()  # Blink LED when avoiding obstacle
    else:
        # No obstacle, move forward
        set_motors(0.5, 0.5)
        
        # Every 10 loops, wiggle a bit to demonstrate turns
        if counter % 10 == 0:
            set_motors(0.7, 0.3)  # Slight right turn
        elif counter % 10 == 5:
            set_motors(0.3, 0.7)  # Slight left turn
    
    # Increment the counter
    counter += 1
    
    # In a real micropython environment, we would use sleep to control the timing
    # Since this is simulated, the Node.js process will control the calling of loop()
    # time.sleep(0.1)  # 100ms delay (commented out as it's handled externally)

# Note: The setup() and loop() functions will be called by the micropython_runner.py script
# The setup() function is called once at the start
# The loop() function is called repeatedly (typically at 10Hz)
