#!/usr/bin/env python3
"""
MicroPython Simulator Runner

This script simulates a MicroPython environment for running robot control code.
It executes the provided Python script in a controlled environment, simulating
the behavior of a MicroPython-powered robot.
"""

import sys
import time
import json
import importlib.util
import threading
import traceback
import os

# Default loop frequency (Hz)
DEFAULT_LOOP_FREQUENCY = 10
LOOP_INTERVAL = 1.0 / DEFAULT_LOOP_FREQUENCY

class MicropythonSimulator:
    def __init__(self, script_path):
        """
        Initialize the MicroPython simulator
        
        Parameters:
        script_path (str): Path to the Python script to run
        """
        self.script_path = script_path
        self.module_name = os.path.basename(script_path).replace('.py', '')
        self.module = None
        self.running = False
        self.error = None
        
        # Load the script as a module
        try:
            spec = importlib.util.spec_from_file_location(self.module_name, self.script_path)
            self.module = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(self.module)
        except Exception as e:
            self.error = f"Error loading script: {str(e)}"
            traceback.print_exc()
    
    def has_function(self, function_name):
        """
        Check if the loaded module has a specific function
        
        Parameters:
        function_name (str): Name of the function to check
        
        Returns:
        bool: True if the function exists, False otherwise
        """
        return hasattr(self.module, function_name) and callable(getattr(self.module, function_name))
    
    def run_setup(self):
        """
        Run the setup function from the loaded module
        
        Returns:
        bool: True if setup succeeded, False otherwise
        """
        if not self.module:
            return False
        
        try:
            if self.has_function('setup'):
                getattr(self.module, 'setup')()
            return True
        except Exception as e:
            self.error = f"Error in setup: {str(e)}"
            traceback.print_exc()
            return False
    
    def run_loop(self):
        """
        Run the loop function from the loaded module
        
        Returns:
        bool: True if loop succeeded, False otherwise
        """
        if not self.module:
            return False
        
        try:
            if self.has_function('loop'):
                getattr(self.module, 'loop')()
            return True
        except Exception as e:
            self.error = f"Error in loop: {str(e)}"
            traceback.print_exc()
            return False
    
    def start(self):
        """
        Start the simulation loop
        """
        if not self.module:
            print(json.dumps({"error": self.error}))
            return
        
        # Run setup
        if not self.run_setup():
            print(json.dumps({"error": self.error}))
            return
        
        # Start the main loop
        self.running = True
        self.loop_thread = threading.Thread(target=self._loop_thread)
        self.loop_thread.daemon = True
        self.loop_thread.start()
    
    def stop(self):
        """
        Stop the simulation loop
        """
        self.running = False
        if hasattr(self, 'loop_thread') and self.loop_thread.is_alive():
            self.loop_thread.join(timeout=2.0)
    
    def _loop_thread(self):
        """
        Thread function for the main loop
        """
        while self.running:
            # Run the loop function
            if not self.run_loop():
                print(json.dumps({"error": self.error}))
                self.running = False
                break
            
            # Sleep to maintain the loop frequency
            time.sleep(LOOP_INTERVAL)

def main():
    """
    Main entry point for the MicroPython simulator
    """
    # Check command line arguments
    if len(sys.argv) < 2:
        print(json.dumps({"error": "No script file specified"}))
        sys.exit(1)
    
    script_path = sys.argv[1]
    
    # Check if the script file exists
    if not os.path.isfile(script_path):
        print(json.dumps({"error": f"Script file not found: {script_path}"}))
        sys.exit(1)
    
    # Create and start the simulator
    simulator = MicropythonSimulator(script_path)
    simulator.start()
    
    try:
        # Keep the main thread running
        while simulator.running:
            time.sleep(0.1)
    except KeyboardInterrupt:
        print(json.dumps({"status": "Simulation stopped by user"}))
    finally:
        # Stop the simulator
        simulator.stop()

if __name__ == "__main__":
    main()
