# my_robot_code.py
# robot_x = 0
# robot_y = 10
# robot_speed = 5.0

# def move_robot(x, y):
#     global robot_x, robot_y
#     robot_x = x
#     robot_y = y
#     print(f"Moved robot to {robot_x}, {robot_y}")

# def increase_speed(amount):
#     global robot_speed
#     robot_speed += amount
#     print(f"Increased speed to {robot_speed}")

# def

import json
import asyncio

class Robot:
    def __init__(self, name="robot"):
        self.name = name
        self.snapshot_queue = None
        self.asyncio_loop = None
        # actuators
        self.actuators = {
            "motor1": 0,
            "motor2": 0,
            "motor3": 0,
            "motor4": 0,
        }
        # sensors
        self.sensors = {
            "battery": 100,
            "light": 0,
            "distance": 0,
            "temperature": 0,
            "humidity": 0,
            "pressure": 0,
            "altitude": 0,
            "orientation": [0, 0, 0],
            "acceleration": [0, 0, 0],
        }
        self.state_cache = {
            "actuators": {
                "motor1": 0,
                "motor2": 0,
                "motor3": 0,
                "motor4": 0,
            },
            "sensors": {
                "battery": 100,
                "light": 0,
                "distance": 0,
                "temperature": 0,
                "humidity": 0,
                "pressure": 0,
                "altitude": 0,
                "orientation": [0, 0, 0],
                "acceleration": [0, 0, 0],
            }
        }
        self.state_changes_local = {
            "actuators": {
                "motor1": None,
                "motor2": None,
                "motor3": None,
                "motor4": None,
            },
        }
        # this is the latest state from the server
        self.state_remote = {
            "sensors": {
                "battery": 100,
                "light": 0,
                "distance": 0,
                "temperature": 0,
                "humidity": 0,
                "pressure": 0,
                "altitude": 0,
                "orientation": [0, 0, 0],
                "acceleration": [0, 0, 0],
            }
        }
        print("Robot initialized")

        
    def set_motor(self, motor, speed):
        self.state_changes_local["actuators"][motor] = speed
        self.push_state_to_server()
        self.actuators[motor] = speed
        if self.snapshot_queue and self.asyncio_loop:
            asyncio.run_coroutine_threadsafe(self.snapshot_queue.put(self.actuators), self.asyncio_loop)
    
    def check_state_change(self):
        # print("check_state_change")
        # print(self.state_changes_local)
        # print(self.state_remote)
        # print(self.state_cache)
        # what are the local changes that need to be sent to the server?
        # what are the differences between the state_remote and the state_cache?
        pass    
    
    def push_state_to_server(self):
        message = json.dumps(self.state_changes_local)
        # send message to server
        if self.snapshot_queue:
            # self.socket.send(message)
            self.snapshot_queue.put(message)
        pass
        
    # async on message from server
    # or during main loop check_state_change
    def set_state_from_server(self, state):
        print("set_state_from_server: ", state)
        self.state_remote = state
        self.actuators['motor1'] = state['actuators']['motor1']
        # self.sensors = state["sensors"]
        # self.actuators = state["actuators"]
# my_robot = Robot("Rover")