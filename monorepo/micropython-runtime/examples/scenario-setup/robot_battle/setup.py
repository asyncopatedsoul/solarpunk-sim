# Robot Battle Arena Scenario Setup
# This script sets up a robot battle arena in the Unity environment

import json
import random
import math

# Define the arena dimensions
ARENA_WIDTH = 40
ARENA_LENGTH = 40
WALL_HEIGHT = 4

# Battle arena features
NUM_OBSTACLES = 20
NUM_WEAPONS = 10
NUM_HEALTH_PACKS = 8
SPAWN_PROTECTION_TIME = 5  # Seconds of invulnerability after spawning

# Unity GameObjects that will be created
created_objects = []
spawn_points = []
weapon_spawns = []
player_robots = []

def setup():
    """Initialize the battle arena scenario"""
    print(json.dumps({"status": "Setting up Robot Battle Arena scenario"}))
    
    # Clear any existing objects
    clear_environment()
    
    # Create the arena
    create_arena()
    
    # Add obstacles and cover
    add_obstacles_and_cover()
    
    # Add weapon pickup locations
    add_weapon_pickups()
    
    # Add health packs
    add_health_packs()
    
    # Set up spawn points
    setup_spawn_points()
    
    # Create environmental hazards
    create_hazards()
    
    # Spawn connected player robots
    spawn_player_robots()
    
    # Set up camera positions
    setup_camera_positions()
    
    print(json.dumps({
        "status": "Robot Battle Arena scenario setup complete",
        "created_objects": len(created_objects),
        "spawn_points": len(spawn_points),
        "weapon_spawns": len(weapon_spawns),
        "player_robots": len(player_robots)
    }))

def clear_environment():
    """Clear existing objects in the environment"""
    # In a real implementation, this would call a SpacetimeDB reducer
    # to clear existing game objects
    print(json.dumps({"action": "clear_environment"}))

def create_arena():
    """Create the battle arena walls and floor"""
    # Create floor
    spawn_object(
        name="Arena Floor",
        prefab_name="ArenaFloor",
        position=(0, 0, 0),
        rotation=(0, 0, 0),
        scale=(ARENA_WIDTH, 1, ARENA_LENGTH),
        is_static=True,
        properties={
            "material": "MetalFloor",
            "physicsLayer": "Ground"
        }
    )
    
    # Create walls
    # North wall
    spawn_object(
        name="North Wall",
        prefab_name="ArenaWall",
        position=(0, WALL_HEIGHT/2, ARENA_LENGTH/2),
        rotation=(0, 0, 0),
        scale=(ARENA_WIDTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )
    
    # South wall
    spawn_object(
        name="South Wall",
        prefab_name="ArenaWall",
        position=(0, WALL_HEIGHT/2, -ARENA_LENGTH/2),
        rotation=(0, 0, 0),
        scale=(ARENA_WIDTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )
    
    # East wall
    spawn_object(
        name="East Wall",
        prefab_name="ArenaWall",
        position=(ARENA_WIDTH/2, WALL_HEIGHT/2, 0),
        rotation=(0, 90, 0),
        scale=(ARENA_LENGTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )
    
    # West wall
    spawn_object(
        name="West Wall",
        prefab_name="ArenaWall",
        position=(-ARENA_WIDTH/2, WALL_HEIGHT/2, 0),
        rotation=(0, 90, 0),
        scale=(ARENA_LENGTH, WALL_HEIGHT, 1),
        is_static=True,
        properties={"physicsLayer": "Wall"}
    )
    
    # Add arena floor details
    add_floor_details()

def add_floor_details():
    """Add visual details to the arena floor"""
    # Center circle
    spawn_object(
        name="Center Circle",
        prefab_name="FloorDecal",
        position=(0, 0.05, 0),
        rotation=(90, 0, 0),
        scale=(10, 10, 1),
        is_static=True,
        properties={
            "decalType": "Circle",
            "color": "Red"
        }
    )
    
    # Grid lines
    for i in range(-3, 4):
        # Vertical grid lines
        spawn_object(
            name=f"GridLine_V_{i}",
            prefab_name="FloorDecal",
            position=(i * 5, 0.05, 0),
            rotation=(90, 0, 0),
            scale=(0.2, ARENA_LENGTH - 2, 1),
            is_static=True,
            properties={
                "decalType": "Line",
                "color": "Gray"
            }
        )
        
        # Horizontal grid lines
        spawn_object(
            name=f"GridLine_H_{i}",
            prefab_name="FloorDecal",
            position=(0, 0.05, i * 5),
            rotation=(90, 90, 0),
            scale=(0.2, ARENA_WIDTH - 2, 1),
            is_static=True,
            properties={
                "decalType": "Line",
                "color": "Gray"
            }
        )
    
    # Corner markings
    for x in [-1, 1]:
        for z in [-1, 1]:
            spawn_object(
                name=f"Corner_Marking_{x}_{z}",
                prefab_name="FloorDecal",
                position=(x * (ARENA_WIDTH/2 - 5), 0.05, z * (ARENA_LENGTH/2 - 5)),
                rotation=(90, 0, 0),
                scale=(4, 4, 1),
                is_static=True,
                properties={
                    "decalType": "Warning",
                    "color": "Yellow"
                }
            )

def add_obstacles_and_cover():
    """Add obstacles and cover to the battle arena"""
    # Define different types of obstacles
    obstacle_types = [
        {
            "prefab": "BarrierBlock",
            "count": 5,
            "min_scale": (2, 2, 2),
            "max_scale": (4, 3, 2),
            "destructible": False
        },
        {
            "prefab": "CrateStack",
            "count": 8,
            "min_scale": (1.5, 1.5, 1.5),
            "max_scale": (2.5, 2.5, 2.5),
            "destructible": True
        },
        {
            "prefab": "MetalBarrier",
            "count": 4,
            "min_scale": (3, 1.5, 1),
            "max_scale": (5, 2, 1),
            "destructible": False
        },
        {
            "prefab": "ExplosiveBarrel",
            "count": 3,
            "min_scale": (1, 1, 1),
            "max_scale": (1.2, 1.2, 1.2),
            "destructible": True
        }
    ]
    
    # Track placed obstacle positions to avoid overlap
    obstacle_positions = []
    
    # Place obstacles of each type
    for obstacle_type in obstacle_types:
        for i in range(obstacle_type["count"]):
            # Try to find a valid position without too much overlap
            position = get_random_position(obstacle_positions, min_distance=4)
            
            # Generate random scale within defined range
            min_scale = obstacle_type["min_scale"]
            max_scale = obstacle_type["max_scale"]
            scale = (
                random.uniform(min_scale[0], max_scale[0]),
                random.uniform(min_scale[1], max_scale[1]),
                random.uniform(min_scale[2], max_scale[2])
            )
            
            # Random rotation (around y-axis only for most obstacles)
            rotation = (0, random.uniform(0, 360), 0)
            
            # Spawn the obstacle
            obstacle_id = spawn_object(
                name=f"{obstacle_type['prefab']}_{i}",
                prefab_name=obstacle_type["prefab"],
                position=(position[0], scale[1]/2, position[1]),  # Position on ground
                rotation=rotation,
                scale=scale,
                is_static=not obstacle_type["destructible"],
                properties={
                    "destructible": obstacle_type["destructible"],
                    "health": 100 if obstacle_type["destructible"] else -1,
                    "damageOnContact": 10 if obstacle_type["prefab"] == "ExplosiveBarrel" else 0,
                    "explodes": obstacle_type["prefab"] == "ExplosiveBarrel"
                }
            )
            
            # Record position for collision avoidance
            obstacle_positions.append(position)
    
    # Create a central structure for intense battles
    create_central_structure()

def create_central_structure():
    """Create a central structure in the arena for cover and strategic gameplay"""
    # Center platform
    spawn_object(
        name="Center Platform",
        prefab_name="Platform",
        position=(0, 1, 0),
        rotation=(0, 0, 0),
        scale=(8, 0.5, 8),
        is_static=True,
        properties={"physicsLayer": "Ground"}
    )
    
    # Ramps to platform (from four directions)
    for angle in [0, 90, 180, 270]:
        rad_angle = math.radians(angle)
        offset_x = 5 * math.sin(rad_angle)
        offset_z = 5 * math.cos(rad_angle)
        
        spawn_object(
            name=f"Ramp_{angle}",
            prefab_name="Ramp",
            position=(offset_x, 0.5, offset_z),
            rotation=(0, angle, 0),
            scale=(2, 1, 4),
            is_static=True,
            properties={"physicsLayer": "Ground"}
        )
    
    # Central cover structure on the platform
    spawn_object(
        name="Central Cover",
        prefab_name="CoverStructure",
        position=(0, 2, 0),
        rotation=(0, 45, 0),
        scale=(3, 2, 3),
        is_static=True,
        properties={
            "climbable": True,
            "physicsLayer": "Cover"
        }
    )
    
    # Power-up on top of central structure
    spawn_object(
        name="Center Powerup",
        prefab_name="SuperWeapon",
        position=(0, 3.5, 0),
        rotation=(0, 0, 0),
        scale=(1, 1, 1),
        is_static=False,
        properties={
            "powerupType": "SuperWeapon",
            "respawnTime": 60,  # 1 minute respawn
            "glowColor": "Purple"
        }
    )

def add_weapon_pickups():
    """Add weapon pickup locations around the arena"""
    global weapon_spawns
    
    # Define weapon types with properties
    weapon_types = [
        {
            "type": "LaserGun",
            "prefab": "WeaponPickup",
            "count": 3,
            "respawn_time": 20,
            "color": "Red"
        },
        {
            "type": "RocketLauncher",
            "prefab": "WeaponPickup",
            "count": 2,
            "respawn_time": 45,
            "color": "Orange"
        },
        {
            "type": "ShockGun",
            "prefab": "WeaponPickup",
            "count": 3,
            "respawn_time": 30,
            "color": "Blue"
        },
        {
            "type": "MineLayer",
            "prefab": "WeaponPickup",
            "count": 2,
            "respawn_time": 40,
            "color": "Green"
        }
    ]
    
    # Track weapon spawn positions
    existing_positions = []
    
    # Place weapons around the arena
    for weapon_type in weapon_types:
        for i in range(weapon_type["count"]):
            # Find a valid position that's not too close to other weapons
            position = get_random_position(existing_positions, min_distance=10)
            
            # Spawn the weapon pickup
            weapon_id = spawn_object(
                name=f"{weapon_type['type']}Pickup_{i}",
                prefab_name=weapon_type["prefab"],
                position=(position[0], 1, position[1]),  # Floating slightly above ground
                rotation=(0, random.uniform(0, 360), 0),
                scale=(1, 1, 1),
                is_static=False,
                properties={
                    "weaponType": weapon_type["type"],
                    "respawnTime": weapon_type["respawn_time"],
                    "glowColor": weapon_type["color"],
                    "floatHeight": 1
                }
            )
            
            # Record position
            existing_positions.append(position)
            
            # Save weapon spawn data for game manager
            weapon_spawns.append({
                "id": weapon_id,
                "type": weapon_type["type"],
                "position": (position[0], 1, position[1]),
                "respawnTime": weapon_type["respawn_time"]
            })
    
    # Send weapon spawn data to game manager
    print(json.dumps({
        "action": "set_weapon_spawns",
        "spawns": weapon_spawns
    }))

def add_health_packs():
    """Add health pack pickups to the arena"""
    # Track existing positions to avoid overlap
    existing_positions = []
    
    # Get positions from already placed objects
    for spawn in weapon_spawns:
        existing_positions.append((spawn["position"][0], spawn["position"][2]))
    
    # Place health packs
    for i in range(NUM_HEALTH_PACKS):
        # Find valid position
        position = get_random_position(existing_positions, min_distance=8)
        
        # Spawn health pack
        spawn_object(
            name=f"HealthPack_{i}",
            prefab_name="HealthPickup",
            position=(position[0], 0.5, position[1]),  # Just above ground
            rotation=(0, 0, 0),
            scale=(1, 1, 1),
            is_static=False,
            properties={
                "healAmount": 50,
                "respawnTime": 30,
                "glowColor": "Green",
                "floatHeight": 0.5
            }
        )
        
        # Record position
        existing_positions.append(position)

def setup_spawn_points():
    """Set up spawn points for robots"""
    global spawn_points
    
    # Create spawn points near corners but not too close to walls
    corner_offsets = 15  # Distance from arena center to spawn point
    
    # Four corner spawn areas
    for x in [-1, 1]:
        for z in [-1, 1]:
            # Create a spawn point
            spawn_pos = (x * corner_offsets, 0.5, z * corner_offsets)
            
            spawn_id = spawn_object(
                name=f"SpawnPoint_{x}_{z}",
                prefab_name="SpawnPoint",
                position=spawn_pos,
                rotation=(0, 45 if (x == z) else -45, 0),  # Face toward center
                scale=(1, 1, 1),
                is_static=True,
                properties={
                    "spawnProtectionTime": SPAWN_PROTECTION_TIME,
                    "respawnDelay": 5,  # 5 seconds before respawn
                    "glowColor": "Blue" if x == 1 else "Red"  # Team colors
                }
            )
            
            # Record spawn point data
            spawn_points.append({
                "id": spawn_id,
                "position": spawn_pos,
                "team": "Blue" if x == 1 else "Red"
            })
    
    # Send spawn point data to game manager
    print(json.dumps({
        "action": "set_spawn_points",
        "spawns": spawn_points
    }))

def create_hazards():
    """Create environmental hazards in the arena"""
    # Add lava pits
    create_lava_pits()
    
    # Add trap areas
    create_trap_areas()
    
    # Add moving elements
    create_moving_elements()

def create_lava_pits():
    """Create lava/damage floor areas"""
    # Create a few damage zones on the ground
    for i in range(3):
        # Random position away from center and spawn points
        radius = random.uniform(8, 15)
        angle = random.uniform(0, 2 * math.pi)
        
        x = radius * math.cos(angle)
        z = radius * math.sin(angle)
        
        # Random size for the damage zone
        size = random.uniform(3, 5)
        
        spawn_object(
            name=f"DamageZone_{i}",
            prefab_name="DamageFloor",
            position=(x, 0.1, z),  # Slightly above the floor
            rotation=(0, 0, 0),
            scale=(size, 0.1, size),
            is_static=True,
            properties={
                "damagePerSecond": 10,
                "effectType": "Lava",
                "effectColor": "Orange"
            }
        )

def create_trap_areas():
    """Create trap areas that trigger when robots enter"""
    # Create a couple of trap triggers in strategic locations
    for i in range(2):
        # Position in narrow passages or strategic points
        if i == 0:
            # Near center but off to one side
            position = (7, 0.1, 0)
        else:
            # In another strategic location
            position = (0, 0.1, -10)
        
        spawn_object(
            name=f"TrapTrigger_{i}",
            prefab_name="TrapTrigger",
            position=position,
            rotation=(0, 0, 0),
            scale=(4, 0.1, 4),
            is_static=True,
            properties={
                "trapType": "Spikes" if i == 0 else "Turret",
                "damage": 30 if i == 0 else 5,
                "triggerDelay": 0.5,
                "resetDelay": 5,
                "visible": False  # Invisible trigger
            }
        )

def create_moving_elements():
    """Create moving platforms or obstacles"""
    # Create a rotating platform in one area
    spawn_object(
        name="RotatingPlatform",
        prefab_name="MovingPlatform",
        position=(-10, 0.5, 10),
        rotation=(0, 0, 0),
        scale=(6, 0.5, 1.5),
        is_static=False,
        properties={
            "movementType": "Rotate",
            "rotationAxis": "Y",
            "rotationSpeed": 20,  # Degrees per second
            "physicsLayer": "MovingPlatform"
        }
    )
    
    # Create a moving platform that goes back and forth
    spawn_object(
        name="LinearPlatform",
        prefab_name="MovingPlatform",
        position=(10, 0.5, -10),
        rotation=(0, 0, 0),
        scale=(4, 0.5, 4),
        is_static=False,
        properties={
            "movementType": "Linear",
            "moveAxis": "X",
            "moveDistance": 6,
            "moveSpeed": 2,  # Units per second
            "physicsLayer": "MovingPlatform"
        }
    )

def setup_camera_positions():
    """Set up camera positions for spectating the battle"""
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
    
    # Create corner cameras
    for i, (x, z) in enumerate([(-1, -1), (-1, 1), (1, -1), (1, 1)]):
        spawn_object(
            name=f"CornerCamera_{i}",
            prefab_name="CameraPosition",
            position=(x * (ARENA_WIDTH/2 - 5), 10, z * (ARENA_LENGTH/2 - 5)),
            rotation=(30, -x * z * 45, 0),  # Point toward center
            scale=(1, 1, 1),
            is_static=True,
            properties={
                "cameraType": "Corner",
                "cornerIndex": i,
                "priority": 5
            }
        )
    
    # Create action cameras near interesting features
    spawn_object(
        name="CenterCamera",
        prefab_name="CameraPosition",
        position=(0, 8, -15),
        rotation=(20, 0, 0),
        scale=(1, 1, 1),
        is_static=True,
        properties={
            "cameraType": "Action",
            "priority": 15
        }
    )

def spawn_player_robots():
    """Spawn robots for all connected players"""
    global player_robots
    
    # In a real implementation, this would get the connected players from SpacetimeDB
    # Mock-up of getting player list from SpacetimeDB
    print(json.dumps({"action": "query_players"}))
    
    # Mock player data (in real implementation, this would come from SpacetimeDB)
    mock_players = [
        {"playerId": "player_1", "name": "Player 1", "team": "Red", "robotType": "AssaultBot"},
        {"playerId": "player_2", "name": "Player 2", "team": "Blue", "robotType": "SniperBot"},
        {"playerId": "player_3", "name": "Player 3", "team": "Red", "robotType": "TankBot"},
        {"playerId": "player_4", "name": "Player 4", "team": "Blue", "robotType": "ScoutBot"}
    ]
    
    # Sort spawn points by team
    red_spawns = [sp for sp in spawn_points if sp["team"] == "Red"]
    blue_spawns = [sp for sp in spawn_points if sp["team"] == "Blue"]
    
    # Spawn robots at appropriate team spawn points
    for player in mock_players:
        # Select spawn point based on team
        team_spawns = red_spawns if player["team"] == "Red" else blue_spawns
        spawn_point = random.choice(team_spawns)
        
        # Spawn the robot
        robot_id = spawn_robot(
            player_id=player["playerId"],
            robot_type=player["robotType"],
            name=f"{player['name']}'s Robot",
            position=spawn_point["position"],
            team=player["team"]
        )
        
        # Add to player robots list
        player_robots.append({
            "robotId": robot_id,
            "playerId": player["playerId"],
            "name": player["name"],
            "team": player["team"],
            "robotType": player["robotType"],
            "spawnPoint": spawn_point["id"]
        })
    
    # Store the player robot list for the game manager
    print(json.dumps({
        "action": "set_player_robots",
        "robots": player_robots
    }))

# Helper Functions

def get_random_position(existing_positions, min_distance=5):
    """
    Get a random position that's at least min_distance away from all existing positions
    Returns (x, z) coordinates
    """
    max_attempts = 50
    attempts = 0
    
    while attempts < max_attempts:
        # Generate random position within arena
        buffer = 5  # Distance from walls
        x = random.uniform(-ARENA_WIDTH/2 + buffer, ARENA_WIDTH/2 - buffer)
        z = random.uniform(-ARENA_LENGTH/2 + buffer, ARENA_LENGTH/2 - buffer)
        
        # Check distance from center (avoid central structure)
        distance_from_center = math.sqrt(x * x + z * z)
        if distance_from_center < 10:  # Central area size
            attempts += 1
            continue
        
        # Check distance from existing positions
        too_close = False
        for pos in existing_positions:
            dx = x - pos[0]
            dz = z - pos[1]
            distance = math.sqrt(dx * dx + dz * dz)
            if distance < min_distance:
                too_close = True
                break
        
        if not too_close:
            return (x, z)
        
        attempts += 1
    
    # If we failed to find a good position, just pick a random one
    x = random.uniform(-ARENA_WIDTH/2 + 5, ARENA_WIDTH/2 - 5)
    z = random.uniform(-ARENA_LENGTH/2 + 5, ARENA_LENGTH/2 - 5)
    return (x, z)

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

def spawn_robot(player_id, robot_type, name, position, team):
    """Spawn a robot for a player"""
    # In a real implementation, this would call a SpacetimeDB reducer
    # to create a new robot associated with a player
    
    robot_id = len(created_objects) + 1
    
    # Set robot properties based on type
    properties = {
        "team": team,
        "spawnProtection": SPAWN_PROTECTION_TIME
    }
    
    if robot_type == "AssaultBot":
        properties.update({
            "maxHealth": 100,
            "speed": 8,
            "turnRate": 180,
            "startingWeapon": "Blaster",
            "specialAbility": "ShieldBoost"
        })
    elif robot_type == "SniperBot":
        properties.update({
            "maxHealth": 75,
            "speed": 6,
            "turnRate": 120,
            "startingWeapon": "LaserRifle",
            "specialAbility": "Cloak"
        })
    elif robot_type == "TankBot":
        properties.update({
            "maxHealth": 200,
            "speed": 5,
            "turnRate": 90,
            "startingWeapon": "HeavyCannon",
            "specialAbility": "ArmorBoost"
        })
    elif robot_type == "ScoutBot":
        properties.update({
            "maxHealth": 80,
            "speed": 12,
            "turnRate": 240,
            "startingWeapon": "PulsePistol",
            "specialAbility": "SpeedBoost"
        })
    
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
            "x": 0,
            "y": 0,
            "z": 0
        },
        "properties": properties
    }
    
    # Send the spawn command
    print(json.dumps(robot_data))
    
    # Add to created objects list
    created_objects.append(robot_id)
    
    return robot_id

# Note: The setup() function will be called once by the game manager when the scenario is loaded
# In a real implementation, all print statements with JSON would be replaced with
# calls to SpacetimeDB reducers to create and manage game objects
