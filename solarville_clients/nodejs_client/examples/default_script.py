# Simple Remote Control Car Script
# This Python script controls the movement of your vehicle

def setup():
    """
    Required setup function that runs once when the script starts.
    """
    print(""Remote Control Car Starting..."")
    set_motors(0, 0)  # Start with motors stopped

def loop():
    """
    Main loop function that runs repeatedly.
    This function is called approximately 10 times per second.
    """
    # Example: Move forward
    set_motors(0.5, 0.5)
    
    # You can modify this code to create different behaviors!
    # Examples:
    # - set_motors(0.7, 0.7)    # Move forward at 70% speed
    # - set_motors(-0.5, -0.5)  # Move backward at 50% speed
    # - set_motors(-0.3, 0.7)   # Turn left
    # - set_motors(0.7, -0.3)   # Turn right

# Note: The functions set_motors, set_left_motor, and set_right_motor
# are provided by the system and control the vehicle's movement.