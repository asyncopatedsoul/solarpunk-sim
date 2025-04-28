# Python Execution Demo
# This is a simple example of Python code that can be executed
import time
# Global variables that will be tracked
left_motor_speed = 0.5
right_motor_speed = 0.25
sensor_data = {"temperature": 22.5, "humidity": 65}
robot_state = "running"
loop = True
loop_count = 0

# Define a simple function
def calculate_average(a, b):
    return (a + b) / 2

def set_motor_speed(left_speed, right_speed):
    global left_motor_speed, right_motor_speed
    left_motor_speed = left_speed
    right_motor_speed = right_speed

# Main function that will be executed
def main():
    global left_motor_speed, right_motor_speed, loop, loop_count
    
    # Some calculation example
    left_motor_speed = calculate_average(0.3, 0.7)
    right_motor_speed = calculate_average(0.1, 0.4)
    
    # Print some output for the REPL
    print(f"Motors set to: L={left_motor_speed}, R={right_motor_speed}")
    print(f"Current sensor data: {sensor_data}")
    
    # You can see this value in the Global State viewer
    result = [i * i for i in range(5)]
    print(f"Calculated squares: {result}")
    
    while loop:
        print(f"Looping... {loop_count}")
        loop_count += 1
        time.sleep(1)
