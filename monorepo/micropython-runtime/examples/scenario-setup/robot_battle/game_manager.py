# Robot Battle Arena Game Manager
# This script manages the robot battle simulation, tracking player health,
# scores, respawns, and game events

import json
import time
import math
import random

# Game configuration
MATCH_DURATION = 300  # 5 minutes per match
TEAM_DEATHMATCH = True  # Team vs Team mode
RESPAWN_DELAY = 5  # Seconds to wait before respawning
MAX_SCORE = 30  # Score to win the match
MATCH_PHASES = ["WAITING", "COUNTDOWN", "PLAYING", "GAME_OVER", "RESET"]

# Game state
game_state = "WAITING"
match_start_time = 0
match_end_time = 0
current_phase = "WAITING"
phase_end_time = 0
countdown_seconds = 5
winning_team = None
winning_player = None

# Team scores
team_scores = {
    "Red": 0,
    "Blue": 0
}

# Player data
players = {}  # Map of player_id -> player data
robots = {}   # Map of robot_id -> robot data

# Weapon and pickup respawn tracking
weapon_spawns = []
weapon_status = {}  # Map of weapon_id -> respawn time

# Game events log
game_events = []

def setup():
    """Initialize the battle arena game manager"""
    global game_state, current_phase
    
    print(json.dumps({"status": "Initializing Robot Battle Arena Game Manager"}))
    
    # Reset game state
    game_state = "WAITING"
    current_phase = "WAITING"
    initialize_game_data()
    
    # Subscribe to game events
    subscribe_to_events()
    
    print(json.dumps({"status": "Robot Battle Arena Game Manager initialized"}))

def initialize_game_data():
    """Initialize game data structures"""
    global team_scores, players, robots, weapon_status, game_events, winning_team, winning_player
    
    # Reset scores
    team_scores = {"Red": 0, "Blue": 0}
    
    # Clear all data
    players = {}
    robots = {}
    weapon_status = {}
    game_events = []
    winning_team = None
    winning_player = None
    
    # Load player data from SpacetimeDB (mock implementation)
    load_player_data()
    
    # Load weapon spawn data
    load_weapon_data()

def subscribe_to_events():
    """Subscribe to game events from SpacetimeDB"""
    # In a real implementation, this would set up callbacks for SpacetimeDB events
    print(json.dumps({"action": "subscribe_events", "events": [
        "robot_damaged",
        "robot_destroyed",
        "weapon_pickup",
        "powerup_collected",
        "player_connect",
        "player_disconnect",
        "robot_respawn"
    ]}))

def load_player_data():
    """Load player and robot data from SpacetimeDB"""
    global players, robots
    
    # In a real implementation, this would query SpacetimeDB tables
    # Mock data for demonstration
    mock_players = [
        {"playerId": "player_1", "name": "Player 1", "team": "Red", "robotId": 101, "robotType": "AssaultBot"},
        {"playerId": "player_2", "name": "Player 2", "team": "Blue", "robotId": 102, "robotType": "SniperBot"},
        {"playerId": "player_3", "name": "Player 3", "team": "Red", "robotId": 103, "robotType": "TankBot"},
        {"playerId": "player_4", "name": "Player 4", "team": "Blue", "robotId": 104, "robotType": "ScoutBot"}
    ]
    
    # Initialize player data
    for player_data in mock_players:
        player_id = player_data["playerId"]
        robot_id = player_data["robotId"]
        
        # Store player info
        players[player_id] = {
            "playerId": player_id,
            "name": player_data["name"],
            "team": player_data["team"],
            "robotId": robot_id,
            "connected": True,
            "score": 0,
            "kills": 0,
            "deaths": 0,
            "assists": 0,
            "lastRespawnTime": 0
        }
        
        # Store robot info
        robots[robot_id] = {
            "robotId": robot_id,
            "playerId": player_id,
            "team": player_data["team"],
            "type": player_data["robotType"],
            "health": get_max_health(player_data["robotType"]),
            "maxHealth": get_max_health(player_data["robotType"]),
            "state": "ALIVE",  # ALIVE, DESTROYED, RESPAWNING
            "position": (0, 0, 0),
            "currentWeapon": get_starting_weapon(player_data["robotType"]),
            "weapons": [get_starting_weapon(player_data["robotType"])],
            "powerups": [],
            "lastDamagedBy": None,
            "respawnTime": 0
        }

def load_weapon_data():
    """Load weapon spawn data from the setup script"""
    global weapon_spawns, weapon_status
    
    # In a real implementation, this would query SpacetimeDB tables
    # Mock data for demonstration
    mock_weapon_spawns = [
        {"id": 201, "type": "LaserGun", "position": (10, 1, 5), "respawnTime": 20},
        {"id": 202, "type": "RocketLauncher", "position": (-10, 1, -5), "respawnTime": 45},
        {"id": 203, "type": "ShockGun", "position": (5, 1, -10), "respawnTime": 30},
        {"id": 204, "type": "MineLayer", "position": (-5, 1, 10), "respawnTime": 40}
    ]
    
    weapon_spawns = mock_weapon_spawns
    
    # Initialize all weapons as available
    for weapon in weapon_spawns:
        weapon_status[weapon["id"]] = {
            "available": True,
            "nextRespawnTime": 0
        }

def get_max_health(robot_type):
    """Get the maximum health for a robot type"""
    if robot_type == "AssaultBot":
        return 100
    elif robot_type == "SniperBot":
        return 75
    elif robot_type == "TankBot":
        return 200
    elif robot_type == "ScoutBot":
        return 80
    else:
        return 100  # Default

def get_starting_weapon(robot_type):
    """Get the starting weapon for a robot type"""
    if robot_type == "AssaultBot":
        return "Blaster"
    elif robot_type == "SniperBot":
        return "LaserRifle"
    elif robot_type == "TankBot":
        return "HeavyCannon"
    elif robot_type == "ScoutBot":
        return "PulsePistol"
    else:
        return "Blaster"  # Default

def loop():
    """Main game loop - runs continuously during the match"""
    global current_phase, phase_end_time
    
    # Check current phase and time to transition
    current_time = time.time()
    
    if current_phase == "WAITING":
        # Check if enough players are ready to start
        if check_players_ready():
            transition_to_phase("COUNTDOWN")
    
    elif current_phase == "COUNTDOWN":
        # Check if countdown is complete
        if current_time >= phase_end_time:
            transition_to_phase("PLAYING")
    
    elif current_phase == "PLAYING":
        # Check if match time is up or score limit reached
        if current_time >= phase_end_time or check_score_limit_reached():
            transition_to_phase("GAME_OVER")
        else:
            # Update match status
            update_match_status()
            
            # Update weapon respawns
            update_weapon_respawns()
            
            # Check for respawning robots
            check_respawning_robots()
    
    elif current_phase == "GAME_OVER":
        # Check if game over period is complete
        if current_time >= phase_end_time:
            transition_to_phase("RESET")
    
    elif current_phase == "RESET":
        # Reset the game
        transition_to_phase("WAITING")

def check_players_ready():
    """Check if enough players are ready to start the match"""
    connected_count = sum(1 for player in players.values() if player["connected"])
    
    # Need at least 2 players (1 per team if team mode)
    if connected_count < 2:
        return False
    
    if TEAM_DEATHMATCH:
        # Need at least 1 player on each team
        red_players = sum(1 for player in players.values() 
                        if player["connected"] and player["team"] == "Red")
        blue_players = sum(1 for player in players.values() 
                         if player["connected"] and player["team"] == "Blue")
        
        return red_players > 0 and blue_players > 0
    
    return True

def transition_to_phase(new_phase):
    """Transition the game to a new phase"""
    global current_phase, phase_end_time, match_start_time, match_end_time
    
    current_phase = new_phase
    current_time = time.time()
    
    if new_phase == "COUNTDOWN":
        # Start countdown phase
        phase_end_time = current_time + countdown_seconds
        
        # Announce countdown
        print(json.dumps({
            "action": "announce_countdown",
            "seconds": countdown_seconds
        }))
        
        # Add event to log
        add_game_event("COUNTDOWN_START", {
            "duration": countdown_seconds
        })
    
    elif new_phase == "PLAYING":
        # Start playing phase
        match_start_time = current_time
        phase_end_time = current_time + MATCH_DURATION
        
        # Announce match start
        print(json.dumps({
            "action": "announce_match_start",
            "duration": MATCH_DURATION,
            "teams": list(team_scores.keys()) if TEAM_DEATHMATCH else None
        }))
        
        # Add event to log
        add_game_event("MATCH_START", {
            "startTime": match_start_time,
            "duration": MATCH_DURATION,
            "teams": list(team_scores.keys()) if TEAM_DEATHMATCH else None
        })
    
    elif new_phase == "GAME_OVER":
        # End playing phase
        match_end_time = current_time
        phase_end_time = current_time + 10  # 10 seconds of game over screen
        
        # Determine winner
        determine_winner()
        
        # Announce match end
        print(json.dumps({
            "action": "announce_match_end",
            "winningTeam": winning_team if TEAM_DEATHMATCH else None,
            "winningPlayer": winning_player if not TEAM_DEATHMATCH else None,
            "scores": team_scores if TEAM_DEATHMATCH else {p["name"]: p["score"] for p in players.values()},
            "matchDuration": match_end_time - match_start_time
        }))
        
        # Add event to log
        add_game_event("MATCH_END", {
            "endTime": match_end_time,
            "winningTeam": winning_team if TEAM_DEATHMATCH else None,
            "winningPlayer": winning_player if not TEAM_DEATHMATCH else None,
            "scores": team_scores if TEAM_DEATHMATCH else {p["name"]: p["score"] for p in players.values()}
        })
    
    elif new_phase == "RESET":
        # Reset the game for a new match
        initialize_game_data()
        
        # Announce reset
        print(json.dumps({
            "action": "announce_reset"
        }))
        
        # Add event to log
        add_game_event("MATCH_RESET", {})
    
    # Log phase transition
    print(json.dumps({
        "action": "phase_transition",
        "oldPhase": current_phase,
        "newPhase": new_phase,
        "phaseEndTime": phase_end_time
    }))

def check_score_limit_reached():
    """Check if score limit has been reached"""
    if TEAM_DEATHMATCH:
        # Check team scores
        for team, score in team_scores.items():
            if score >= MAX_SCORE:
                return True
    else:
        # Check individual player scores
        for player in players.values():
            if player["score"] >= MAX_SCORE:
                return True
    
    return False

def update_match_status():
    """Update and broadcast match status"""
    # Only update periodically to avoid spamming
    current_time = time.time()
    if int(current_time) % 5 != 0:  # Update every 5 seconds
        return
    
    # Calculate time remaining
    time_remaining = max(0, phase_end_time - current_time)
    
    # Create status update
    status = {
        "matchPhase": current_phase,
        "timeRemaining": time_remaining,
        "scores": team_scores if TEAM_DEATHMATCH else {p["playerId"]: p["score"] for p in players.values()},
        "playerCount": sum(1 for p in players.values() if p["connected"])
    }
    
    # Add leaderboard
    if TEAM_DEATHMATCH:
        # Team scores
        status["leaderboard"] = [
            {"team": team, "score": score} 
            for team, score in sorted(team_scores.items(), key=lambda x: x[1], reverse=True)
        ]
    else:
        # Individual scores
        status["leaderboard"] = [
            {"playerId": p["playerId"], "name": p["name"], "score": p["score"], 
             "kills": p["kills"], "deaths": p["deaths"]} 
            for p in sorted(players.values(), key=lambda x: x["score"], reverse=True)
            if p["connected"]
        ][:5]  # Top 5 players
    
    # Broadcast status update
    print(json.dumps({
        "action": "match_status_update",
        "status": status
    }))

def update_weapon_respawns():
    """Update weapon respawn timers and respawn weapons"""
    current_time = time.time()
    
    for weapon_id, status in weapon_status.items():
        if not status["available"] and current_time >= status["nextRespawnTime"]:
            # Respawn the weapon
            weapon_status[weapon_id]["available"] = True
            
            # Find weapon data
            weapon_data = next((w for w in weapon_spawns if w["id"] == weapon_id), None)
            if weapon_data:
                # Announce weapon respawn
                print(json.dumps({
                    "action": "weapon_respawn",
                    "weaponId": weapon_id,
                    "weaponType": weapon_data["type"],
                    "position": weapon_data["position"]
                }))
                
                # Add event to log
                add_game_event("WEAPON_RESPAWN", {
                    "weaponId": weapon_id,
                    "weaponType": weapon_data["type"]
                })

def check_respawning_robots():
    """Check for robots that need to respawn"""
    current_time = time.time()
    
    for robot_id, robot in robots.items():
        if robot["state"] == "RESPAWNING" and current_time >= robot["respawnTime"]:
            # Time to respawn the robot
            respawn_robot(robot_id)

def respawn_robot(robot_id):
    """Respawn a destroyed robot"""
    # Check if robot exists
    if robot_id not in robots:
        return
    
    robot = robots[robot_id]
    player_id = robot["playerId"]
    
    # Find a spawn point for this robot's team
    spawn_position = get_team_spawn_position(robot["team"])
    
    # Update robot state
    robot["state"] = "ALIVE"
    robot["health"] = robot["maxHealth"]
    robot["position"] = spawn_position
    robot["weapons"] = [get_starting_weapon(robot["type"])]
    robot["currentWeapon"] = get_starting_weapon(robot["type"])
    robot["powerups"] = []
    robot["lastDamagedBy"] = None
    
    # Update player respawn time
    if player_id in players:
        players[player_id]["lastRespawnTime"] = time.time()
    
    # Announce respawn
    print(json.dumps({
        "action": "robot_respawn",
        "robotId": robot_id,
        "playerId": player_id,
        "position": spawn_position,
        "health": robot["health"]
    }))
    
    # Add event to log
    add_game_event("ROBOT_RESPAWN", {
        "robotId": robot_id,
        "playerId": player_id,
        "team": robot["team"]
    })

def get_team_spawn_position(team):
    """Get a random spawn position for a team"""
    # In a real implementation, this would use actual spawn points from the scene
    # For demo, return a fixed position based on team
    if team == "Red":
        return (-15, 0.5, -15)  # Red team corner
    else:
        return (15, 0.5, 15)  # Blue team corner

def determine_winner():
    """Determine the winner of the match"""
    global winning_team, winning_player
    
    if TEAM_DEATHMATCH:
        # Find team with highest score
        winning_team = max(team_scores.items(), key=lambda x: x[1])[0]
        
        # Check for tie
        if list(team_scores.values()).count(team_scores[winning_team]) > 1:
            winning_team = None  # It's a tie
    else:
        # Find player with highest score
        top_player = max(players.values(), key=lambda p: p["score"])
        
        # Check for tie
        if sum(1 for p in players.values() if p["score"] == top_player["score"]) > 1:
            winning_player = None  # It's a tie
        else:
            winning_player = top_player["playerId"]

def handle_robot_damaged(robot_id, damage_amount, source_robot_id, weapon_type):
    """Handle a robot taking damage"""
    # Check if robot exists
    if robot_id not in robots:
        return
    
    robot = robots[robot_id]
    
    # Ignore if already destroyed
    if robot["state"] != "ALIVE":
        return
    
    # Update last damaged by (for assist tracking)
    robot["lastDamagedBy"] = source_robot_id
    
    # Apply damage
    robot["health"] = max(0, robot["health"] - damage_amount)
    
    # Check if robot is destroyed
    if robot["health"] <= 0:
        handle_robot_destroyed(robot_id, source_robot_id)
    else:
        # Just damaged, not destroyed
        # Announce damage
        print(json.dumps({
            "action": "robot_damaged",
            "robotId": robot_id,
            "sourceRobotId": source_robot_id,
            "damageAmount": damage_amount,
            "weaponType": weapon_type,
            "remainingHealth": robot["health"]
        }))
        
        # Add event to log
        add_game_event("ROBOT_DAMAGED", {
            "robotId": robot_id,
            "sourceRobotId": source_robot_id,
            "damageAmount": damage_amount,
            "weaponType": weapon_type,
            "remainingHealth": robot["health"]
        })

def handle_robot_destroyed(robot_id, killer_robot_id):
    """Handle a robot being destroyed"""
    # Check if robot exists
    if robot_id not in robots:
        return
    
    robot = robots[robot_id]
    player_id = robot["playerId"]
    
    # Update robot state
    robot["state"] = "RESPAWNING"
    robot["health"] = 0
    robot["respawnTime"] = time.time() + RESPAWN_DELAY
    
    # Update player stats
    if player_id in players:
        players[player_id]["deaths"] += 1
    
    # Award points to killer
    if killer_robot_id in robots:
        killer_robot = robots[killer_robot_id]
        killer_player_id = killer_robot["team"]
        
        # Update killer player stats
        if killer_player_id in players:
            players[killer_player_id]["kills"] += 1
            players[killer_player_id]["score"] += 1  # 1 point per kill
        
        # Update team score if team mode
        if TEAM_DEATHMATCH:
            killer_team = killer_robot["team"]
            team_scores[killer_team] += 1
        
        # Check for assists
        if robot["lastDamagedBy"] and robot["lastDamagedBy"] != killer_robot_id:
            assist_player_id = robots[robot["lastDamagedBy"]]["playerId"]
            if assist_player_id in players:
                players[assist_player_id]["assists"] += 1
                # Can also award partial points for assists
                players[assist_player_id]["score"] += 0.5
    
    # Announce destruction
    print(json.dumps({
        "action": "robot_destroyed",
        "robotId": robot_id,
        "playerId": player_id,
        "killerRobotId": killer_robot_id,
        "killerPlayerId": robots[killer_robot_id]["playerId"] if killer_robot_id in robots else None,
        "respawnTime": robot["respawnTime"]
    }))
    
    # Add event to log
    add_game_event("ROBOT_DESTROYED", {
        "robotId": robot_id,
        "playerId": player_id,
        "killerRobotId": killer_robot_id,
        "killerPlayerId": robots[killer_robot_id]["playerId"] if killer_robot_id in robots else None,
        "respawnTime": RESPAWN_DELAY
    })

def handle_weapon_pickup(robot_id, weapon_id, weapon_type):
    """Handle a robot picking up a weapon"""
    # Check if robot exists
    if robot_id not in robots:
        return
    
    robot = robots[robot_id]
    
    # Add weapon to robot's inventory
    if weapon_type not in robot["weapons"]:
        robot["weapons"].append(weapon_type)
    
    # Set as current weapon
    robot["currentWeapon"] = weapon_type
    
    # Mark weapon as unavailable and set respawn time
    if weapon_id in weapon_status:
        weapon_data = next((w for w in weapon_spawns if w["id"] == weapon_id), None)
        if weapon_data:
            weapon_status[weapon_id]["available"] = False
            weapon_status[weapon_id]["nextRespawnTime"] = time.time() + weapon_data["respawnTime"]
    
    # Announce pickup
    print(json.dumps({
        "action": "weapon_pickup",
        "robotId": robot_id,
        "playerId": robot["playerId"],
        "weaponId": weapon_id,
        "weaponType": weapon_type
    }))
    
    # Add event to log
    add_game_event("WEAPON_PICKUP", {
        "robotId": robot_id,
        "playerId": robot["playerId"],
        "weaponId": weapon_id,
        "weaponType": weapon_type
    })

def handle_powerup_collected(robot_id, powerup_type, powerup_id):
    """Handle a robot collecting a powerup"""
    # Check if robot exists
    if robot_id not in robots:
        return
    
    robot = robots[robot_id]
    
    # Add powerup effect
    if powerup_type == "Health":
        # Restore health
        robot["health"] = min(robot["maxHealth"], robot["health"] + 50)
    elif powerup_type == "Shield":
        # Add shield to powerups
        robot["powerups"].append({
            "type": "Shield",
            "duration": 10  # 10 seconds of shield
        })
    elif powerup_type == "DoubleDamage":
        # Add double damage to powerups
        robot["powerups"].append({
            "type": "DoubleDamage",
            "duration": 15  # 15 seconds of double damage
        })
    
    # Announce powerup collection
    print(json.dumps({
        "action": "powerup_collected",
        "robotId": robot_id,
        "playerId": robot["playerId"],
        "powerupType": powerup_type,
        "powerupId": powerup_id
    }))
    
    # Add event to log
    add_game_event("POWERUP_COLLECTED", {
        "robotId": robot_id,
        "playerId": robot["playerId"],
        "powerupType": powerup_type
    })

def handle_player_connect(player_id, name, team):
    """Handle a player connecting to the game"""
    # Check if player already exists
    if player_id in players:
        # Update connection status
        players[player_id]["connected"] = True
        
        # Add event to log
        add_game_event("PLAYER_RECONNECT", {
            "playerId": player_id,
            "name": players[player_id]["name"]
        })
    else:
        # Create new player
        players[player_id] = {
            "playerId": player_id,
            "name": name,
            "team": team,
            "robotId": None,  # Will be assigned when robot spawns
            "connected": True,
            "score": 0,
            "kills": 0,
            "deaths": 0,
            "assists": 0,
            "lastRespawnTime": 0
        }
        
        # Add event to log
        add_game_event("PLAYER_CONNECT", {
            "playerId": player_id,
            "name": name,
            "team": team
        })
    
    # Check if we should start the game
    if current_phase == "WAITING" and check_players_ready():
        transition_to_phase("COUNTDOWN")

def handle_player_disconnect(player_id):
    """Handle a player disconnecting from the game"""
    # Check if player exists
    if player_id not in players:
        return
    
    # Update connection status
    players[player_id]["connected"] = False
    
    # If player has a robot, mark it as destroyed
    robot_id = players[player_id]["robotId"]
    if robot_id and robot_id in robots:
        robots[robot_id]["state"] = "DESTROYED"
    
    # Add event to log
    add_game_event("PLAYER_DISCONNECT", {
        "playerId": player_id,
        "name": players[player_id]["name"]
    })
    
    # Check if we should end the game (not enough players)
    if current_phase == "PLAYING" and not check_players_ready():
        transition_to_phase("GAME_OVER")

def handle_robot_position_update(robot_id, position):
    """Handle a robot position update"""
    # Check if robot exists and is alive
    if robot_id in robots and robots[robot_id]["state"] == "ALIVE":
        # Update position
        robots[robot_id]["position"] = position

def add_game_event(event_type, event_data):
    """Add an event to the game event log"""
    event = {
        "type": event_type,
        "timestamp": time.time(),
        "data": event_data
    }
    
    game_events.append(event)
    
    # In a real implementation, this would store the event in SpacetimeDB
    # for history and analytics

# Event handler functions - these would be called by SpacetimeDB callbacks in a real implementation

def on_robot_damaged(data):
    """Callback for robot damage events"""
    robot_id = data["robotId"]
    damage_amount = data["damageAmount"]
    source_robot_id = data["sourceRobotId"]
    weapon_type = data["weaponType"]
    handle_robot_damaged(robot_id, damage_amount, source_robot_id, weapon_type)

def on_robot_destroyed(data):
    """Callback for robot destroyed events"""
    robot_id = data["robotId"]
    killer_robot_id = data["killerRobotId"]
    handle_robot_destroyed(robot_id, killer_robot_id)

def on_weapon_pickup(data):
    """Callback for weapon pickup events"""
    robot_id = data["robotId"]
    weapon_id = data["weaponId"]
    weapon_type = data["weaponType"]
    handle_weapon_pickup(robot_id, weapon_id, weapon_type)

def on_powerup_collected(data):
    """Callback for powerup collection events"""
    robot_id = data["robotId"]
    powerup_type = data["powerupType"]
    powerup_id = data["powerupId"]
    handle_powerup_collected(robot_id, powerup_type, powerup_id)

def on_player_connect(data):
    """Callback for player connect events"""
    player_id = data["playerId"]
    name = data["name"]
    team = data["team"]
    handle_player_connect(player_id, name, team)

def on_player_disconnect(data):
    """Callback for player disconnect events"""
    player_id = data["playerId"]
    handle_player_disconnect(player_id)

def on_robot_position_update(data):
    """Callback for robot position update events"""
    robot_id = data["robotId"]
    position = (data["position"]["x"], data["position"]["y"], data["position"]["z"])
    handle_robot_position_update(robot_id, position)

# Note: The setup() and loop() functions will be called by the game manager framework
# All print statements with JSON would be replaced with calls to SpacetimeDB reducers
# Event handlers would be registered with SpacetimeDB event system
