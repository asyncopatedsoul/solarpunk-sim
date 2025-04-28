import time
tracked_vars = {'motor1': 0, 'motor2': 0}
motor1 = 0
motor2 = 0

def set_motor(num, power):
    my_robot.set_motor(num, power)

def move_robot(x, y):
    my_robot.move_robot(x, y)

def increase_speed(speed):
    my_robot.increase_speed(speed)

def drain_power(power):
    my_robot.drain_power(power)

def init():
    global my_robot
    my_robot = Robot('Rover')