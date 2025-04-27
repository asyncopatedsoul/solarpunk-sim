# Robot Race Scenario Setup
# This script sets up a robot racing scenario in the Unity environment

import json
import random
import math

# Define the scenario dimensions
ARENA_WIDTH = 30
ARENA_LENGTH = 50
WALL_HEIGHT = 2

# Race track parameters
TRACK_WIDTH = 5
CURVE_RADIUS = 8

# Number of obstacles to place randomly
NUM_OBSTACLES = 15

# Unity GameObjects that will be created
created_objects = []
player_robots = []

def setup():
    """Initialize the racing scenario"""
    print(json.dumps({"status": "Setting up Robot Race scenario"}))
    
    # Clear any existing objects
    clear_environment()
    
    # Create the arena
    create_arena()
    
    # Create the race track
    create_race_track()
    
    # Add obstacles
    add_obstacles()
    
    # Set up starting positions
    setup_starting_positions()
    
    # Create finish line
    create_finish_line()
    
    # Spawn connected player robots
    spawn_player_robots()
    
    # Set up camera positions
    setup_camera_positions()
    
    print(json.dumps({
        "status": "Robot Race scenario setup complete",
        "created_objects": len(created_objects),
        "player_robots": len(player_robots)
    }))

def clear_environment():
    """Clear existing objects in the environment"""
    # In a real implementation, this would call a SpacetimeDB reducer
    # to clear existing game objects
    print(json.dumps({"action": "clear_environment"}))

def create_arena():
    """Create the racing arena walls and floor"""
    # Create floor
    spawn_object(
        name="Arena Floor",
        prefab_name="GroundPlane",
        position=(0, 0, 0),
        rotation=(0, 0, 0),
        scale=(ARENA_WIDTH, 1, ARENA_LENGTH),
        is_static=True,
        properties={
            "material": "RaceTrackMaterial",
            "physicsLayer": "Ground"
        }
    )
    
    # Create walls
    # North wall
    spawn_object(
        name="North Wall",
        prefab_name="Wall",
        position=(0, WALL_HEIGHT/2, ARENA_LENGTH/2),
        rotation=(0, 0, 0),
        scale=(ARENA_WIDTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )
    
    # South wall
    spawn_object(
        name="South Wall",
        prefab_name="Wall",
        position=(0, WALL_HEIGHT/2, -ARENA_LENGTH/2),
        rotation=(0, 0, 0),
        scale=(ARENA_WIDTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )
    
    # East wall
    spawn_object(
        name="East Wall",
        prefab_name="Wall",
        position=(ARENA_WIDTH/2, WALL_HEIGHT/2, 0),
        rotation=(0, 90, 0),
        scale=(ARENA_LENGTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )
    
    # West wall
    spawn_object(
        name="West Wall",
        prefab_name="Wall",
        position=(-ARENA_WIDTH/2, WALL_HEIGHT/2, 0),
        rotation=(0, 90, 0),
        scale=(ARENA_LENGTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )

def create_race_track():
    """Create the race track with visual indicators"""
    # Create the outer track markers
    create_track_markers()
    
    # Create the track texture on the ground
    # (This would apply a texture to the ground plane in a real implementation)
    print(json.dumps({
        "action": "apply_texture",
        "target": "Arena Floor",
        "texture": "RaceTrack"
    }))
    
    # Add checkpoints along the track
    create_checkpoints()

def create_track_markers():
    """Create visual markers for the race track"""
    # Place cones or other markers along the track
    # This creates a circular track in the arena
    
    num_markers = 40
    for i in range(num_markers):
        angle = (2 * math.pi * i) / num_markers
        
        # Outside track marker
        outer_radius = min(ARENA_WIDTH, ARENA_LENGTH) * 0.4
        x = outer_radius * math.cos(angle)
        z = outer_radius * math.sin(angle)
        
        spawn_object(
            name=f"OuterMarker_{i}",
            prefab_name="TrackCone",
            position=(x, 0.5, z),
            rotation=(0, angle * 180 / math.pi, 0),
            scale=(1, 1, 1),
            is_static=True,
            properties={"color": "Red"}
        )
        
        # Inside track marker
        inner_radius = outer_radius - TRACK_WIDTH
        x = inner_radius * math.cos(angle)
        z = inner_radius * math.sin(angle)
        
        spawn_object(
            name=f"InnerMarker_{i}",
            prefab_name="TrackCone",
            position=(x, 0.5, z),
            rotation=(0, angle * 180 / math.pi, 0),
            scale=(1, 1, 1),
            is_static=True,
            properties={"color": "Blue"}
        )

def create_checkpoints():
    """Create checkpoint triggers around the track"""
    # Place invisible checkpoint triggers for lap counting and progress tracking
    num_checkpoints = 8
    track_radius = min(ARENA_WIDTH, ARENA_LENGTH) * 0.4 - TRACK_WIDTH/2
    
    for i in range(num_checkpoints):
        angle = (2 * math.pi * i) / num_checkpoints
        x = track_radius * math.cos(angle)
        z = track_radius * math.sin(angle)
        
        spawn_object(
            name=f"Checkpoint_{i}",
            prefab_name="CheckpointTrigger",
            position=(x, 1, z),
            rotation=(0, angle * 180 / math.pi + 90, 0),
            scale=(TRACK_WIDTH, 2, 1),
            is_static=True,
            properties={
                "checkpointIndex": i,
                "isVisible": False,  # Invisible trigger collider
                "triggerType": "Checkpoint"
            }
        )
    
    # Create the start/finish line
    spawn_object(
        name="StartFinishLine",
        prefab_name="StartFinishLine",
        position=(0, 0.1, track_radius),
        rotation=(0, 0, 0),
        scale=(TRACK_WIDTH, 0.1, 1),
        is_static=True,
        properties={
            "checkpointIndex": 0,
            "isStart": True,
            "triggerType": "StartFinish"
        }
    )

def add_obstacles():
    """Add random obstacles on and off the track"""
    # Place obstacles like rocks, barriers, ramps, etc.
    safe_zone_radius = 5  # Keep this area clear for starting grid
    
    for i in range(NUM_OBSTACLES):
        # Choose a random position in the arena
        # Making sure it's not in the safe zone at start/finish
        valid_position = False
        pos_x, pos_z = 0, 0
        
        while not valid_position:
            pos_x = random.uniform(-ARENA_WIDTH/2 + 2, ARENA_WIDTH/2 - 2)
            pos_z = random.uniform(-ARENA_LENGTH/2 + 2, ARENA_LENGTH/2 - 2)
            
            # Check distance from start area
            distance_from_start = math.sqrt(pos_x**2 + (pos_z - safe_zone_radius)**2)
            if distance_from_start > safe_zone_radius:
                valid_position = True
        
        # Choose random obstacle type
        obstacle_types = ["Rock", "Barrier", "Ramp", "OilSlick", "PowerUp"]
        obstacle_type = random.choice(obstacle_types)
        
        # Set properties based on obstacle type
        properties = {"obstacleType": obstacle_type}
        scale = (1, 1, 1)
        
        if obstacle_type == "Rock":
            scale = (random.uniform(0.5, 1.5), random.uniform(0.5, 1.5), random.uniform(0.5, 1.5))
            properties["damageOnCollision"] = random.uniform(5, 15)
        elif obstacle_type == "Barrier":
            scale = (random.uniform(2, 4), random.uniform(1, 2), random.uniform(0.5, 1))
        elif obstacle_type == "Ramp":
            scale = (random.uniform(1.5, 3), random.uniform(0.5, 1), random.uniform(2, 4))
        elif obstacle_type == "OilSlick":
            scale = (random.uniform(2, 4), 0.1, random.uniform(2, 4))
            properties["frictionMultiplier"] = 0.2
        elif obstacle_type == "PowerUp":
            scale = (1, 1, 1)
            properties["powerUpType"] = random.choice(["Speed", "Shield", "Repair"])
            properties["respawnTime"] = 15  # Seconds to respawn after collected
        
        spawn_object(
            name=f"Obstacle_{i}_{obstacle_type}",
            prefab_name=f"Obstacle{obstacle_type}",
            position=(pos_x, scale[1]/2, pos_z),
            rotation=(0, random.uniform(0, 360), 0),
            scale=scale,
            is_static=(obstacle_type != "PowerUp"),  # PowerUps can be collected
            properties=properties
        )

def setup_starting_positions():
    """Set up the starting grid for robot racers"""
    # Create start positions in a grid pattern behind the start line
    num_rows = 3
    num_cols = 4
    spacing_x = 2
    spacing_z = 3
    start_z = -5  # Starting line Z position
    
    start_positions = []
    
    for row in range(num_rows):
        for col in range(num_cols):
            pos_x = (col - (num_cols - 1) / 2) * spacing_x
            pos_z = start_z - row * spacing_z
            
            # Create a start position marker
            spawn_object(
                name=f"StartPosition_{row}_{col}",
                prefab_name="StartPositionMarker",
                position=(pos_x, 0.1, pos_z),
                rotation=(0, 0, 0),
                scale=(1, 0.1, 1),
                is_static=True,
                properties={
                    "startIndex": row * num_cols + col,
                    "isVisible": True
                }
            )
            
            # Save the start position for later robot spawning
            start_positions.append({
                "index": row * num_cols + col,
                "position": (pos_x, 0, pos_z),
                "rotation": (0, 0, 0)
            })
    
    # Store start positions in a global variable for the game manager
    print(json.dumps({
        "action": "set_start_positions",
        "positions": start_positions
    }))

def create_finish_line():
    """Create the finish line with visual markers"""
    # Already covered in create_checkpoints, but could add more visual elements
    track_radius = min(ARENA_WIDTH, ARENA_LENGTH) * 0.4 - TRACK_WIDTH/2
    
    # Add finish line overhead banner
    spawn_object(
        name="FinishBanner",
        prefab_name="FinishBanner",
        position=(0, 5, track_radius),
        rotation=(0, 0, 0),
        scale=(TRACK_WIDTH + 4, 2, 1),
        is_static=True,
        properties={
            "bannerText": "FINISH"
        }
    )
    
    # Add checkered pattern on ground
    spawn_object(
        name="CheckeredGround",
        prefab_name="CheckeredDecal",
        position=(0, 0.05, track_radius),
        rotation=(90, 0, 0),
        scale=(TRACK_WIDTH, 3, 1),
        is_static=True,
        properties={}
    )

def spawn_player_robots():
    """Spawn robots for all connected players"""
    # In a real implementation, this would get the connected players from SpacetimeDB
    # and spawn a robot for each at the starting positions
    
    # Mock-up of getting player list from SpacetimeDB
    print(json.dumps({"action": "query_players"}))
    
    # Mock player data (in real implementation, this would come from SpacetimeDB)
    mock_players = [
        {"playerId": "player_1", "name": "Player 1", "robotType": "StandardBot"},
        {"playerId": "player_2", "name": "Player 2", "robotType": "SpeedBot"},
        {"playerId": "player_3", "name": "Player 3", "robotType": "HeavyBot"},
        {"playerId": "player_4", "name": "Player 4", "robotType": "StandardBot"}
    ]
    
    # Assign start positions to players
    for i, player in enumerate(mock_players):
        if i >= 12:  # Max 12 starting positions
            break
            
        # Determine start position based on index
        row = i // 4
        col = i % 4
        pos_x = (col - 1.5) * 2  # 4 columns, centered
        pos_z = -5 - row * 3     # First row at z=-5, 3 units between rows
        
        # Spawn the robot
        robot_id = spawn_robot(
            player_id=player["playerId"],
            robot_type=player["robotType"],
            name=f"{player['name']}'s Robot",
            position=(pos_x, 0.5, pos_z),
            rotation=(0, 0, 0)
        )
        
        # Add to player robots list
        player_robots.append({
            "robotId": robot_id,
            "playerId": player["playerId"],
            "name": player["name"],
            "startPosition": i
        })
    
    # Store the player robot list for the game manager
    print(json.dumps({
        "action": "set_player_robots",
        "robots": player_robots
    }))

def setup_camera_positions():
    """Set up camera positions for spectating the race"""
    # Create overhead camera position
    spawn_object(
        name="OverheadCamera",
        prefab_name="CameraPosition",
        position=(0, 30, 0),
        rotation=(90, 0, 0),
        scale=(1, 1, 1),
        is_static=True,
        properties={
            "cameraType": "Overhead",
            "priority": 10
        }
    )
    
    # Create finish line camera
    track_radius = min(ARENA_WIDTH, ARENA_LENGTH) * 0.4 - TRACK_WIDTH/2
    spawn_object(
        name="FinishCamera",
        prefab_name="CameraPosition",
        position=(TRACK_WIDTH, 3, track_radius - 10),
        rotation=(20, 180, 0),
        scale=(1, 1, 1),
        is_static=True,
        properties={
            "cameraType": "FinishLine",
            "priority": 20
        }
    )
    
    # Create track corner cameras
    for i in range(4):
        angle = (2 * math.pi * i) / 4
        track_radius = min(ARENA_WIDTH, ARENA_LENGTH) * 0.4 - TRACK_WIDTH/2
        
        camera_distance = track_radius + 5
        x = camera_distance * math.cos(angle)
        z = camera_distance * math.sin(angle)
        
        # Calculate rotation to look at center
        look_angle = (angle + math.pi) * 180 / math.pi
        
        spawn_object(
            name=f"CornerCamera_{i}",
            prefab_name="CameraPosition",
            position=(x, 4, z),
            rotation=(15, look_angle, 0),
            scale=(1, 1, 1),
            is_static=True,
            properties={
                "cameraType": "Corner",
                "cornerIndex": i,
                "priority": 15
            }
        )

# Helper Functions

def spawn_object(name, prefab_name, position, rotation, scale, is_static, properties=None):
    """Spawn a game object in the Unity environment"""
    # In a real implementation, this would call a SpacetimeDB reducer
    # to create a new game object
    
    obj_id = len(created_objects) + 1
    
    # Prepare the object data
    object_data = {
        "action": "spawn_object",
        "objectId": obj_id,
        "name": name,
        "prefabName": prefab_name,
        "position": {
            "x": position[0],
            "y": position[1],
            "z": position[2]
        },
        "rotation": {
            "x": rotation[0],
            "y": rotation[1],
            "z": rotation[2]
        },
        "scale": {
            "x": scale[0],
            "y": scale[1],
            "z": scale[2]
        },
        "isStatic": is_static
    }
    
    # Add any additional properties
    if properties:
        object_data["properties"] = properties
    
    # Send the spawn command
    print(json.dumps(object_data))
    
    # Add to created objects list
    created_objects.append(obj_id)
    
    return obj_id

def spawn_robot(player_id, robot_type, name, position, rotation):
    """Spawn a robot for a player"""
    # In a real implementation, this would call a SpacetimeDB reducer
    # to create a new robot associated with a player
    
    robot_id = len(created_objects) + 1
    
    # Prepare the robot data
    robot_data = {
        "action": "spawn_robot",
        "robotId": robot_id,
        "playerId": player_id,
        "robotType": robot_type,
        "name": name,
        "position": {
            "x": position[0],
            "y": position[1],
            "z": position[2]
        },
        "rotation": {
            "x": rotation[0],
            "y": rotation[1],
            "z": rotation[2]
        }
    }
    
    # Set robot-specific properties based on type
    if robot_type == "StandardBot":
        robot_data["properties"] = {
            "maxSpeed": 10,
            "acceleration": 5,
            "handling": 5,
            "durability": 100
        }
    elif robot_type == "SpeedBot":
        robot_data["properties"] = {
            "maxSpeed": 15,
            "acceleration": 8,
            "handling": 4,
            "durability": 70
        }
    elif robot_type == "HeavyBot":
        robot_data["properties"] = {
            "maxSpeed": 8,
            "acceleration": 3,
            "handling": 3,
            "durability": 150
        }
    
    # Send the spawn command
    print(json.dumps(robot_data))
    
    # Add to created objects list
    created_objects.append(robot_id)
    
    return robot_id

# Note: The setup() function will be called once by the game manager when the scenario is loaded
# In a real implementation, all print statements with JSON would be replaced with
# calls to SpacetimeDB reducers to create and manage game objects
