#!/usr/bin/env python3
# Remote Control Car Firmware Simulation
# This script simulates a remote-controlled car's firmware
# It receives inputs from the web interface and controls the motors accordingly

import time
import logging
import random
import math

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("RemoteControlCar")

class RemoteControlCar:
    def __init__(self):
        # Initialize car state
        self.left_motor_speed = 0.0  # Range: -1.0 to 1.0
        self.right_motor_speed = 0.0  # Range: -1.0 to 1.0
        self.position_x = 0.0  # Simulated position in Unity world
        self.position_y = 0.0  # Simulated position in Unity world
        self.rotation = 0.0  # Rotation angle in radians
        self.battery_level = 100.0  # Battery percentage
        self.is_running = True
        self.debug_mode = True
        
        # Simulation parameters
        self.max_speed = 5.0  # Maximum units per second
        self.rotation_speed = 1.5  # Radians per second at full turning
        self.battery_drain_rate = 0.05  # Percentage per second when motors active
        
        logger.info("Remote Control Car initialized")
    
    def set_motor_speeds(self, left_speed, right_speed):
        """Set the speed of both motors"""
        # Clamp values between -1 and 1
        self.left_motor_speed = max(-1.0, min(1.0, left_speed))
        self.right_motor_speed = max(-1.0, min(1.0, right_speed))
        
        if self.debug_mode:
            logger.info(f"Motors set to L:{self.left_motor_speed:.1f}, R:{self.right_motor_speed:.1f}")
        
        # Report motor state change
        print(f"MOTOR_STATE:{self.left_motor_speed},{self.right_motor_speed}")
    
    def update_position(self, dt):
        """Update the car's position based on motor speeds and time delta"""
        # Calculate average forward speed and turning 
        forward_speed = (self.left_motor_speed + self.right_motor_speed) / 2.0 * self.max_speed
        turn_rate = (self.right_motor_speed - self.left_motor_speed) * self.rotation_speed
        
        # Update rotation
        self.rotation += turn_rate * dt
        
        # Normalize rotation to 0-2π
        self.rotation = self.rotation % (2 * math.pi)
        
        # Calculate movement vector based on rotation
        dx = forward_speed * math.cos(self.rotation) * dt
        dy = forward_speed * math.sin(self.rotation) * dt
        
        # Update position
        self.position_x += dx
        self.position_y += dy
        
        # Update battery level
        motor_activity = abs(self.left_motor_speed) + abs(self.right_motor_speed)
        self.battery_level -= (motor_activity / 2.0) * self.battery_drain_rate * dt
        self.battery_level = max(0.0, self.battery_level)  # Ensure battery doesn't go below 0%
        
        if self.debug_mode and random.random() < 0.1:  # Only log occasionally to avoid spam
            logger.info(f"Position: ({self.position_x:.1f}, {self.position_y:.1f}) " +
                        f"Rotation: {self.rotation:.2f} Battery: {self.battery_level:.1f}%")
    
    def run(self):
        """Main loop for the car simulation"""
        last_time = time.time()
        
        try:
            while self.is_running:
                # Calculate time delta
                current_time = time.time()
                dt = current_time - last_time
                last_time = current_time
                
                # Update car state
                self.update_position(dt)
                
                # Check for low battery
                if self.battery_level < 10.0:
                    logger.warning(f"Low battery: {self.battery_level:.1f}%")
                
                # Check for battery depletion
                if self.battery_level <= 0:
                    logger.warning("Battery depleted! Shutting down.")
                    self.is_running = False
                
                # Sleep to limit CPU usage
                time.sleep(0.05)
        
        except KeyboardInterrupt:
            logger.info("Remote Control Car simulation stopped by user")
        finally:
            self.cleanup()
    
    def cleanup(self):
        """Clean up resources and reset state"""
        self.set_motor_speeds(0, 0)  # Stop motors
        logger.info("Remote Control Car shutdown complete")
    
    def handle_command(self, command):
        """Process commands received from the interface"""
        try:
            if command.startswith("MOTORS:"):
                # Format: MOTORS:left_speed,right_speed
                values = command.split(":")[1].split(",")
                left = float(values[0])
                right = float(values[1])
                self.set_motor_speeds(left, right)
            
            elif command == "STOP":
                self.set_motor_speeds(0, 0)
            
            elif command == "DEBUG_ON":
                self.debug_mode = True
                logger.info("Debug mode enabled")
            
            elif command == "DEBUG_OFF":
                self.debug_mode = False
                logger.info("Debug mode disabled")
            
            elif command == "STATUS":
                status = {
                    "position": (self.position_x, self.position_y),
                    "rotation": self.rotation,
                    "battery": self.battery_level,
                    "motors": (self.left_motor_speed, self.right_motor_speed)
                }
                print(f"STATUS:{status}")
            
            else:
                logger.warning(f"Unknown command: {command}")
        
        except Exception as e:
            logger.error(f"Error processing command: {e}")

# REPL integration functions
def on_input(input_str):
    """Handle input from the REPL"""
    car.handle_command(input_str.strip())

def on_startup():
    """Initialize the car"""
    global car
    car = RemoteControlCar()
    # Start a separate thread for simulation updates
    import threading
    thread = threading.Thread(target=car.run, daemon=True)
    thread.start()
    print("Remote Control Car firmware started")
    return car

def set_motors(left, right):
    """Directly set motor speeds"""
    if 'car' in globals():
        car.set_motor_speeds(left, right)
    else:
        print("ERROR: Car not initialized")

# Initialize when imported
if __name__ != "__main__":
    car = on_startup()
