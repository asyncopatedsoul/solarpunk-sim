# Battle Robot Controller Example
# This script demonstrates AI behavior for a battle robot

import json
import math
import random
import time

# Global variables
# These represent the robot's state
current_health = 100
max_health = 100
position = (0, 0, 0)  # (x, y, z)
rotation = 0  # Degrees, 0 is forward, increases clockwise
left_motor = 0
right_motor = 0
weapons = ["Blaster"]  # Start with default weapon
current_weapon = "Blaster"
target_robot = None
scan_timer = 0
last_hit_time = 0
last_state_change = 0
current_state = "PATROL"  # PATROL, PURSUIT, ATTACK, EVADE

# Environment awareness
known_obstacles = []  # List of obstacle positions
weapon_pickups = []   # List of weapon pickup positions
health_pickups = []   # List of health pickup positions
detected_robots = []  # List of detected enemy robots
team = "Red"          # Our team
team_members = []     # List of friendly robot positions

# Battle arena boundaries
ARENA_MIN_X = -20
ARENA_MAX_X = 20
ARENA_MIN_Z = -20
ARENA_MAX_Z = 20

# Configuration
PATROL_SPEED = 0.6
PURSUIT_SPEED = 0.8
ATTACK_SPEED = 0.4
EVADE_SPEED = 0.9
SCAN_INTERVAL = 1.0       # Seconds between environment scans
STATE_CHANGE_DELAY = 3.0  # Minimum seconds between state changes
HEALTH_THRESHOLD = 0.3    # Below 30% health, prioritize health pickups
PICKUP_RANGE = 2.0        # How close to be to collect a pickup
ATTACK_RANGE = 8.0        # Range for attacks
PURSUIT_RANGE = 15.0      # Max range to pursue a target
RANDOM_TURN_CHANCE = 0.05 # Chance per loop to make a random turn while patrolling

# Function definitions for motor control
def set_left_motor(speed):
    global left_motor
    left_motor = max(-1.0, min(1.0, speed))
    print(json.dumps({"leftMotorSpeed": left_motor}))

def set_right_motor(speed):
    global right_motor
    right_motor = max(-1.0, min(1.0, speed))
    print(json.dumps({"rightMotorSpeed": right_motor}))

def set_motors(left_speed, right_speed):
    set_left_motor(left_speed)
    set_right_motor(right_speed)

# Battle robot functions
def fire_weapon():
    """Fire the current weapon"""
    print(json.dumps({"action": "fireWeapon", "weapon": current_weapon}))

def switch_weapon(weapon_name):
    """Switch to a different weapon"""
    global current_weapon
    if weapon_name in weapons:
        current_weapon = weapon_name
        print(json.dumps({"action": "switchWeapon", "weapon": current_weapon}))
        return True
    return False

def scan_environment():
    """Scan for objects, obstacles, and other robots"""
    global detected_robots, known_obstacles, weapon_pickups, health_pickups, scan_timer
    
    # In a real implementation, this would use simulated sensors
    # Here we're assuming the game engine provides this data through a response
    print(json.dumps({"action": "scanEnvironment"}))
    
    # This would receive data from the game engine
    # For now, let's simulate some random detections
    scan_timer = time.time()
    
    # Simulate some enemy robot detections (this would come from Unity)
    detected_robots = []
    if random.random() < 0.7:  # 70% chance to detect an enemy
        # Generate a random position within sensor range
        distance = random.uniform(5, 20)
        angle = random.uniform(0, 2 * math.pi)
        enemy_x = position[0] + distance * math.cos(angle)
        enemy_z = position[2] + distance * math.sin(angle)
        
        # Keep within arena bounds
        enemy_x = max(ARENA_MIN_X, min(ARENA_MAX_X, enemy_x))
        enemy_z = max(ARENA_MIN_Z, min(ARENA_MAX_Z, enemy_z))
        
        detected_robots.append({
            "id": "enemy_" + str(random.randint(1, 100)),
            "position": (enemy_x, 0, enemy_z),
            "health": random.randint(10, 100),
            "team": "Blue" if team == "Red" else "Red"
        })
    
    # Simulate some pickup detections
    if random.random() < 0.4:  # 40% chance to detect a health pickup
        distance = random.uniform(10, 15)
        angle = random.uniform(0, 2 * math.pi)
        health_x = position[0] + distance * math.cos(angle)
        health_z = position[2] + distance * math.sin(angle)
        
        # Keep within arena bounds
        health_x = max(ARENA_MIN_X, min(ARENA_MAX_X, health_x))
        health_z = max(ARENA_MIN_Z, min(ARENA_MAX_Z, health_z))
        
        health_pickups = [{
            "type": "Health",
            "position": (health_x, 0.5, health_z)
        }]
    
    if random.random() < 0.3:  # 30% chance to detect a weapon pickup
        distance = random.uniform(8, 18)
        angle = random.uniform(0, 2 * math.pi)
        weapon_x = position[0] + distance * math.cos(angle)
        weapon_z = position[2] + distance * math.sin(angle)
        
        # Keep within arena bounds
        weapon_x = max(ARENA_MIN_X, min(ARENA_MAX_X, weapon_x))
        weapon_z = max(ARENA_MIN_Z, min(ARENA_MAX_Z, weapon_z))
        
        weapon_types = ["LaserGun", "RocketLauncher", "ShockGun", "MineLayer"]
        weapon_pickups = [{
            "type": random.choice(weapon_types),
            "position": (weapon_x, 0.5, weapon_z)
        }]

def update_position(pos_update):
    """Update the robot's position based on simulator data"""
    global position, rotation
    position = (pos_update["x"], pos_update["y"], pos_update["z"])
    rotation = pos_update["rotation"]

def update_health(health_update):
    """Update the robot's health"""
    global current_health, last_hit_time
    
    # Check if we took damage
    if health_update < current_health:
        last_hit_time = time.time()
        
        # If we've taken significant damage, consider evasion
        if current_health - health_update > 20:
            consider_state_change("EVADE")
    
    current_health = health_update

def process_inputs(game_data):
    """Process input data from the game engine"""
    if "position" in game_data:
        update_position(game_data["position"])
    
    if "health" in game_data:
        update_health(game_data["health"])
    
    if "weaponPickup" in game_data:
        weapons.append(game_data["weaponPickup"])
        switch_weapon(game_data["weaponPickup"])
    
    if "teamRobots" in game_data:
        team_members = game_data["teamRobots"]

def consider_state_change(new_state):
    """Consider changing the robot's state"""
    global current_state, last_state_change
    
    # Don't change states too frequently
    if time.time() - last_state_change < STATE_CHANGE_DELAY:
        return False
    
    # Save the old state for logging
    old_state = current_state
    current_state = new_state
    last_state_change = time.time()
    
    # Log state change
    print(json.dumps({
        "stateChange": {
            "from": old_state,
            "to": new_state,
            "time": last_state_change
        }
    }))
    
    return True

def choose_best_target():
    """Choose the best target from detected robots"""
    global target_robot
    
    if not detected_robots:
        target_robot = None
        return
    
    # Priority: low health enemies, then closest enemies
    best_target = None
    best_score = -1
    
    for robot in detected_robots:
        # Skip team members
        if robot["team"] == team:
            continue
        
        # Calculate distance
        dx = robot["position"][0] - position[0]
        dz = robot["position"][2] - position[2]
        distance = math.sqrt(dx * dx + dz * dz)
        
        # Calculate score based on health and distance
        # Lower health and closer distance gives higher score
        health_factor = (100 - robot["health"]) / 100
        distance_factor = (PURSUIT_RANGE - min(distance, PURSUIT_RANGE)) / PURSUIT_RANGE
        score = health_factor * 0.5 + distance_factor * 0.5
        
        if score > best_score:
            best_score = score
            best_target = robot
    
    target_robot = best_target

def calculate_direction_to_point(target_pos):
    """Calculate direction (angle in degrees) to a target position"""
    dx = target_pos[0] - position[0]
    dz = target_pos[2] - position[2]
    
    # Calculate angle in radians, then convert to degrees
    angle_rad = math.atan2(dz, dx)
    angle_deg = math.degrees(angle_rad)
    
    # Convert to robot's reference frame
    relative_angle = angle_deg - rotation
    
    # Normalize to [-180, 180]
    while relative_angle > 180:
        relative_angle -= 360
    while relative_angle < -180:
        relative_angle += 360
    
    return relative_angle

def move_towards_point(target_pos, speed_factor):
    """Move towards a target position with differential steering"""
    # Calculate relative angle to target
    relative_angle = calculate_direction_to_point(target_pos)
    
    # Convert to turn intensity (-1 to 1, where 0 is straight)
    turn_intensity = relative_angle / 90.0
    turn_intensity = max(-1.0, min(1.0, turn_intensity))
    
    # Set motor speeds for differential steering
    if turn_intensity > 0:
        # Turn right
        left_speed = speed_factor
        right_speed = speed_factor * (1 - 2 * abs(turn_intensity))
    else:
        # Turn left
        left_speed = speed_factor * (1 - 2 * abs(turn_intensity))
        right_speed = speed_factor
    
    # Apply speeds
    set_motors(left_speed, right_speed)

def patrol_behavior():
    """Patrol the arena looking for targets and pickups"""
    global scan_timer
    
    # Perform periodic environment scans
    if time.time() - scan_timer > SCAN_INTERVAL:
        scan_environment()
    
    # Check if we detected any enemies
    if detected_robots:
        choose_best_target()
        if target_robot:
            consider_state_change("PURSUIT")
            return
    
    # Check if we need health and a health pickup is available
    if current_health < max_health * HEALTH_THRESHOLD and health_pickups:
        move_towards_point(health_pickups[0]["position"], PURSUIT_SPEED)
        return
    
    # Check if a better weapon is available
    if weapon_pickups and weapon_pickups[0]["type"] not in weapons:
        move_towards_point(weapon_pickups[0]["position"], PURSUIT_SPEED)
        return
    
    # Otherwise, patrol randomly
    # Occasionally make random turns
    if random.random() < RANDOM_TURN_CHANCE:
        # Make a random turn
        turn_strength = random.uniform(-0.5, 0.5)
        set_motors(PATROL_SPEED * (1 + turn_strength), PATROL_SPEED * (1 - turn_strength))
    else:
        # Move forward with slight randomness
        left_jitter = random.uniform(-0.1, 0.1)
        right_jitter = random.uniform(-0.1, 0.1)
        set_motors(PATROL_SPEED + left_jitter, PATROL_SPEED + right_jitter)
    
    # Avoid arena boundaries
    avoid_boundaries()

def pursuit_behavior():
    """Pursue a target robot"""
    global target_robot, scan_timer
    
    # Perform periodic environment scans
    if time.time() - scan_timer > SCAN_INTERVAL:
        scan_environment()
        choose_best_target()
    
    # If no target or target lost, go back to patrolling
    if not target_robot:
        consider_state_change("PATROL")
        return
    
    # Calculate distance to target
    dx = target_robot["position"][0] - position[0]
    dz = target_robot["position"][2] - position[2]
    distance = math.sqrt(dx * dx + dz * dz)
    
    # If close enough, switch to attack mode
    if distance <= ATTACK_RANGE:
        consider_state_change("ATTACK")
        return
    
    # If too far, go back to patrolling
    if distance > PURSUIT_RANGE:
        consider_state_change("PATROL")
        return
    
    # Move towards target
    move_towards_point(target_robot["position"], PURSUIT_SPEED)
    
    # Avoid boundaries
    avoid_boundaries()

def attack_behavior():
    """Attack the target robot"""
    global target_robot, scan_timer
    
    # Perform periodic environment scans
    if time.time() - scan_timer > SCAN_INTERVAL:
        scan_environment()
        choose_best_target()
    
    # If no target or target lost, go back to patrolling
    if not target_robot:
        consider_state_change("PATROL")
        return
    
    # Calculate distance to target
    dx = target_robot["position"][0] - position[0]
    dz = target_robot["position"][2] - position[2]
    distance = math.sqrt(dx * dx + dz * dz)
    
    # If too far, switch to pursuit mode
    if distance > ATTACK_RANGE:
        consider_state_change("PURSUIT")
        return
    
    # Choose the best weapon for the distance
    choose_weapon_for_distance(distance)
    
    # Move towards target more carefully (circling behavior)
    # Calculate angle to target
    angle_to_target = calculate_direction_to_point(target_robot["position"])
    
    # Try to maintain distance by circling
    circle_angle = angle_to_target + 60  # Offset to circle around target
    
    # Convert back to steering
    if abs(circle_angle) < 45:
        # Mostly forward with slight turning
        set_motors(ATTACK_SPEED, ATTACK_SPEED * 0.7)
    elif circle_angle > 0:
        # Turn right
        set_motors(ATTACK_SPEED, -ATTACK_SPEED * 0.5)
    else:
        # Turn left
        set_motors(-ATTACK_SPEED * 0.5, ATTACK_SPEED)
    
    # Fire weapon if facing target
    if abs(angle_to_target) < 30:
        fire_weapon()
    
    # Avoid boundaries
    avoid_boundaries()

def evade_behavior():
    """Evade when under heavy attack"""
    global scan_timer, last_hit_time
    
    # Perform periodic environment scans
    if time.time() - scan_timer > SCAN_INTERVAL:
        scan_environment()
    
    # If it's been a while since we were hit, go back to patrolling
    if time.time() - last_hit_time > 5.0:
        consider_state_change("PATROL")
        return
    
    # Check if we need health and a health pickup is available
    if current_health < max_health * HEALTH_THRESHOLD and health_pickups:
        move_towards_point(health_pickups[0]["position"], EVADE_SPEED)
        return
    
    # If we know who's attacking us, move away from them
    if target_robot:
        # Calculate angle away from target
        dx = position[0] - target_robot["position"][0]
        dz = position[2] - target_robot["position"][2]
        angle_rad = math.atan2(dz, dx)
        
        # Calculate escape position
        escape_distance = 15
        escape_x = position[0] + escape_distance * math.cos(angle_rad)
        escape_z = position[2] + escape_distance * math.sin(angle_rad)
        
        # Keep within arena bounds
        escape_x = max(ARENA_MIN_X + 2, min(ARENA_MAX_X - 2, escape_x))
        escape_z = max(ARENA_MIN_Z + 2, min(ARENA_MAX_Z - 2, escape_z))
        
        # Move to escape position
        move_towards_point((escape_x, 0, escape_z), EVADE_SPEED)
    else:
        # Random evasive movement
        set_motors(
            random.uniform(-EVADE_SPEED, EVADE_SPEED),
            random.uniform(-EVADE_SPEED, EVADE_SPEED)
        )
    
    # Avoid boundaries
    avoid_boundaries()

def avoid_boundaries():
    """Avoid arena boundaries"""
    distance_to_min_x = position[0] - ARENA_MIN_X
    distance_to_max_x = ARENA_MAX_X - position[0]
    distance_to_min_z = position[2] - ARENA_MIN_Z
    distance_to_max_z = ARENA_MAX_Z - position[2]
    
    # Define boundary threshold
    boundary_threshold = 3.0
    
    # Check if we're too close to any boundary
    too_close_to_boundary = (
        distance_to_min_x < boundary_threshold or
        distance_to_max_x < boundary_threshold or
        distance_to_min_z < boundary_threshold or
        distance_to_max_z < boundary_threshold
    )
    
    if too_close_to_boundary:
        # Calculate center of arena
        center_x = (ARENA_MIN_X + ARENA_MAX_X) / 2
        center_z = (ARENA_MIN_Z + ARENA_MAX_Z) / 2
        
        # Turn towards center of arena
        move_towards_point((center_x, 0, center_z), PATROL_SPEED)
        return True
    
    return False

def choose_weapon_for_distance(distance):
    """Choose the best weapon based on target distance"""
    # Define weapon effective ranges
    weapon_ranges = {
        "ShockGun": 5,         # Short range
        "Blaster": 10,         # Medium range
        "LaserGun": 15,        # Long range
        "RocketLauncher": 12,  # Medium-long range, with splash damage
        "MineLayer": 3         # Very short range
    }
    
    # Find best weapon for current distance
    best_weapon = None
    best_range_diff = float('inf')
    
    for weapon in weapons:
        if weapon in weapon_ranges:
            range_diff = abs(weapon_ranges[weapon] - distance)
            if range_diff < best_range_diff:
                best_range_diff = range_diff
                best_weapon = weapon
    
    # Switch to best weapon if available
    if best_weapon and best_weapon != current_weapon:
        switch_weapon(best_weapon)

def setup():
    """Initialize the robot"""
    global scan_timer, current_health, max_health
    
    # Initialize timers
    scan_timer = time.time()
    
    # Get initial robot data
    print(json.dumps({"action": "getInitialData"}))
    
    # In a real implementation, we would receive this data from the game engine
    # For now, just use the defaults
    
    # Perform initial scan
    scan_environment()
    
    print(json.dumps({"status": "Robot initialized", "team": team}))

def loop():
    """Main control loop - runs repeatedly"""
    # Process any input data from the game engine
    # In a real implementation, this would be handled by events
    # For simulation, we'll just use random data
    
    # Run behavior based on current state
    if current_state == "PATROL":
        patrol_behavior()
    elif current_state == "PURSUIT":
        pursuit_behavior()
    elif current_state == "ATTACK":
        attack_behavior()
    elif current_state == "EVADE":
        evade_behavior()
    
    # Send status update
    if random.random() < 0.1:  # Only occasionally to reduce spam
        print(json.dumps({
            "status": {
                "state": current_state,
                "health": current_health,
                "position": position,
                "weapon": current_weapon
            }
        }))

# Note: The setup() and loop() functions will be called by the micropython_runner.py script
# setup() is called once at the start
# loop() is called repeatedly (typically at 10Hz)
