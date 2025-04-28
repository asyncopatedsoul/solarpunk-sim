# from robot import Robot
import time

tracked_vars = {
    "motor1": 0,
    "motor2": 0,
}
motor1 = 0
motor2 = 0


def set_motor(num, power):
    # global motor1, motor2
    my_robot.set_motor(num, power)
    # tracked_vars["motor" + str(num)] = power
    # if num == 1:
    #     motor1 = power
    # elif num == 2:
    #     motor2 = power
    # track_self()
    # print(locals())
    # print(globals())

def move_robot(x, y):
    my_robot.move_robot(x, y)

def increase_speed(speed):
    my_robot.increase_speed(speed)

def drain_power(power):
    my_robot.drain_power(power)
    
# while True:
#     my_robot.check_state_change()
#     time.sleep(5)

def init():
    global my_robot
    my_robot = Robot("Rover")