# Robot Race Game Manager
# This script manages the robot race simulation, tracking player progress,
# handling game events, and determining the winner

import json
import time
import math

# Game configuration
MAX_LAPS = 3
RACE_TIMEOUT_SECONDS = 300  # 5 minutes max race time
COUNTDOWN_SECONDS = 3
FINISH_COOLDOWN = 10  # Time in seconds after race ends before resetting

# Game state
game_state = "WAITING"  # WAITING, COUNTDOWN, RACING, FINISHED
race_start_time = 0
race_end_time = 0
player_data = {}
checkpoint_count = 8  # Number of checkpoints per lap

# Robot data maps
robot_positions = {}  # Map of robot_id -> (x, y, z)
robot_checkpoints = {}  # Map of robot_id -> last checkpoint passed
robot_laps = {}  # Map of robot_id -> laps completed
robot_lap_times = {}  # Map of robot_id -> list of lap times
robot_finish_times = {}  # Map of robot_id -> finish time
leaderboard = []  # Ordered list of robot_ids by race position

# Game events log
game_events = []

def setup():
    """Initialize the race manager"""
    global game_state
    
    print(json.dumps({"status": "Initializing Robot Race Game Manager"}))
    
    # Reset game state
    game_state = "WAITING"
    initialize_game_data()
    
    # Subscribe to game events
    subscribe_to_events()
    
    print(json.dumps({"status": "Robot Race Game Manager initialized"}))

def initialize_game_data():
    """Initialize game data structures"""
    global player_data, robot_positions, robot_checkpoints, robot_laps
    global robot_lap_times, robot_finish_times, leaderboard, game_events
    
    # Clear all data
    player_data = {}
    robot_positions = {}
    robot_checkpoints = {}
    robot_laps = {}
    robot_lap_times = {}
    robot_finish_times = {}
    leaderboard = []
    game_events = []
    
    # Load player data from SpacetimeDB (mock implementation)
    load_player_data()

def subscribe_to_events():
    """Subscribe to game events from SpacetimeDB"""
    # In a real implementation, this would set up callbacks for SpacetimeDB events
    print(json.dumps({"action": "subscribe_events", "events": [
        "checkpoint_trigger",
        "robot_position_update",
        "robot_collision",
        "robot_damage",
        "powerup_collected"
    ]}))

def load_player_data():
    """Load player and robot data from SpacetimeDB"""
    # In a real implementation, this would query SpacetimeDB tables
    # Mock data for demonstration
    mock_robots = [
        {"robotId": 101, "playerId": "player_1", "name": "Player 1", "robotType": "StandardBot"},
        {"robotId": 102, "playerId": "player_2", "name": "Player 2", "robotType": "SpeedBot"},
        {"robotId": 103, "playerId": "player_3", "name": "Player 3", "robotType": "HeavyBot"},
        {"robotId": 104, "playerId": "player_4", "name": "Player 4", "robotType": "StandardBot"}
    ]
    
    # Initialize data for each robot
    for robot in mock_robots:
        robot_id = robot["robotId"]
        player_data[robot_id] = {
            "robotId": robot_id,
            "playerId": robot["playerId"],
            "playerName": robot["name"],
            "robotType": robot["robotType"],
            "status": "READY"  # READY, RACING, FINISHED, DNF (Did Not Finish)
        }
        
        robot_positions[robot_id] = (0, 0, 0)
        robot_checkpoints[robot_id] = -1  # No checkpoints passed initially
        robot_laps[robot_id] = 0
        robot_lap_times[robot_id] = []
        robot_finish_times[robot_id] = None
    
    # Initialize leaderboard with all robots
    leaderboard = [robot["robotId"] for robot in mock_robots]

def loop():
    """Main game loop - runs continuously during the race"""
    global game_state, race_start_time, race_end_time
    
    # Handle different game states
    if game_state == "WAITING":
        # Check if enough players are ready to start
        if check_players_ready():
            start_countdown()
    
    elif game_state == "COUNTDOWN":
        # Countdown is handled by events, nothing to do here
        pass
    
    elif game_state == "RACING":
        # Update race timer
        current_time = time.time()
        race_duration = current_time - race_start_time
        
        # Check if race has timed out
        if race_duration > RACE_TIMEOUT_SECONDS:
            end_race("TIMEOUT")
        
        # Update leaderboard
        update_leaderboard()
        
        # Broadcast race status
        if int(race_duration) % 5 == 0:  # Every 5 seconds
            broadcast_race_status(race_duration)
    
    elif game_state == "FINISHED":
        # Check if enough time has passed to reset the race
        if time.time() - race_end_time > FINISH_COOLDOWN:
            reset_race()

def check_players_ready():
    """Check if enough players are ready to start the race"""
    ready_count = sum(1 for player in player_data.values() if player["status"] == "READY")
    
    # Need at least 2 players to start
    return ready_count >= 2

def start_countdown():
    """Start the race countdown"""
    global game_state
    
    game_state = "COUNTDOWN"
    print(json.dumps({"action": "start_countdown", "seconds": COUNTDOWN_SECONDS}))
    
    # Add event to log
    add_game_event("COUNTDOWN_START", {
        "countdown": COUNTDOWN_SECONDS
    })
    
    # In a real implementation, this would set a timer or wait for events
    # For this example, we'll simulate the countdown
    for i in range(COUNTDOWN_SECONDS, 0, -1):
        print(json.dumps({"action": "countdown_tick", "seconds_left": i}))
        # This would trigger UI updates in the Unity client
        # time.sleep(1) - commented out as in the simulation we don't actually sleep
    
    # Start the race
    start_race()

def start_race():
    """Start the race after countdown completes"""
    global game_state, race_start_time
    
    game_state = "RACING"
    race_start_time = time.time()
    
    # Update all players to racing status
    for player_id in player_data:
        player_data[player_id]["status"] = "RACING"
    
    # Broadcast race start event
    print(json.dumps({
        "action": "race_started",
        "timestamp": race_start_time,
        "player_count": len(player_data)
    }))
    
    # Add event to log
    add_game_event("RACE_START", {
        "timestamp": race_start_time,
        "player_count": len(player_data)
    })
    
    # Trigger race start in Unity client
    print(json.dumps({"action": "trigger_race_start"}))

def end_race(reason="COMPLETED"):
    """End the race"""
    global game_state, race_end_time
    
    game_state = "FINISHED"
    race_end_time = time.time()
    race_duration = race_end_time - race_start_time
    
    # Update any remaining players as DNF
    for robot_id, player in player_data.items():
        if player["status"] == "RACING":
            player["status"] = "DNF"
    
    # Determine final results
    finalize_results()
    
    # Broadcast race end event
    print(json.dumps({
        "action": "race_ended",
        "reason": reason,
        "duration": race_duration,
        "winners": get_podium_results()
    }))
    
    # Add event to log
    add_game_event("RACE_END", {
        "timestamp": race_end_time,
        "duration": race_duration,
        "reason": reason,
        "winners": get_podium_results()
    })
    
    # Trigger race end in Unity client
    print(json.dumps({"action": "trigger_race_end", "reason": reason}))

def reset_race():
    """Reset the race to waiting state"""
    global game_state
    
    game_state = "WAITING"
    
    # Reset all game data
    initialize_game_data()
    
    # Add event to log
    add_game_event("RACE_RESET", {
        "timestamp": time.time()
    })
    
    # Trigger race reset in Unity client
    print(json.dumps({"action": "trigger_race_reset"}))

def update_leaderboard():
    """Update the race leaderboard based on current positions"""
    global leaderboard
    
    # Sort robots by laps completed (descending) and then by last checkpoint (descending)
    # This gives a rough estimate of race position
    leaderboard = sorted(
        robot_laps.keys(),
        key=lambda robot_id: (
            robot_laps[robot_id],  # Laps completed
            robot_checkpoints[robot_id],  # Last checkpoint
            -calculate_distance_to_next_checkpoint(robot_id)  # Negative distance to next checkpoint
        ),
        reverse=True
    )
    
    # Broadcast updated leaderboard periodically
    print(json.dumps({
        "action": "update_leaderboard",
        "leaderboard": [{
            "position": i + 1,
            "robotId": robot_id,
            "playerName": player_data[robot_id]["playerName"],
            "laps": robot_laps[robot_id],
            "checkpoint": robot_checkpoints[robot_id]
        } for i, robot_id in enumerate(leaderboard)]
    }))

def calculate_distance_to_next_checkpoint(robot_id):
    """Calculate distance from robot to next checkpoint"""
    # In a real implementation, this would use the actual positions
    # Here we just return a random value for demonstration
    import random
    return random.uniform(0, 100)

def broadcast_race_status(race_duration):
    """Broadcast current race status"""
    # Create status summary
    status = {
        "game_state": game_state,
        "race_duration": race_duration,
        "leader": leaderboard[0] if leaderboard else None,
        "player_count": len(player_data),
        "finished_count": sum(1 for player in player_data.values() if player["status"] == "FINISHED"),
        "top_players": [{
            "position": i + 1,
            "robotId": robot_id,
            "playerName": player_data[robot_id]["playerName"],
            "laps": robot_laps[robot_id],
            "checkpoint": robot_checkpoints[robot_id]
        } for i, robot_id in enumerate(leaderboard[:3])]  # Top 3 players
    }
    
    # Broadcast status update
    print(json.dumps({
        "action": "race_status_update",
        "status": status
    }))

def handle_checkpoint_trigger(robot_id, checkpoint_index):
    """Handle a robot passing through a checkpoint"""
    # Validate robot exists
    if robot_id not in robot_checkpoints:
        print(json.dumps({"error": f"Unknown robot ID: {robot_id}"}))
        return
    
    # Get the current checkpoint and lap data
    current_checkpoint = robot_checkpoints[robot_id]
    current_laps = robot_laps[robot_id]
    
    # Check if this is a valid checkpoint progression
    if checkpoint_index == (current_checkpoint + 1) % checkpoint_count:
        # Valid next checkpoint
        robot_checkpoints[robot_id] = checkpoint_index
        
        # Check if completed a lap (crossed start/finish line)
        if checkpoint_index == 0 and current_checkpoint != -1:
            complete_lap(robot_id)
    else:
        # Invalid checkpoint (might be going backward or skipped a checkpoint)
        print(json.dumps({
            "warning": f"Invalid checkpoint progression for robot {robot_id}: {current_checkpoint} -> {checkpoint_index}"
        }))
    
    # Add event to log
    add_game_event("CHECKPOINT_TRIGGER", {
        "robotId": robot_id,
        "checkpointIndex": checkpoint_index,
        "timestamp": time.time()
    })
    
    # Update leaderboard
    update_leaderboard()

def complete_lap(robot_id):
    """Handle a robot completing a lap"""
    global robot_laps, robot_lap_times
    
    # Increment lap counter
    robot_laps[robot_id] += 1
    current_lap = robot_laps[robot_id]
    
    # Calculate lap time
    current_time = time.time()
    lap_start_time = race_start_time
    
    if len(robot_lap_times[robot_id]) > 0:
        # Use the last lap end time as the start time for this lap
        lap_start_time = race_start_time + sum(robot_lap_times[robot_id])
    
    lap_time = current_time - lap_start_time
    robot_lap_times[robot_id].append(lap_time)
    
    # Broadcast lap completion
    print(json.dumps({
        "action": "lap_completed",
        "robotId": robot_id,
        "playerName": player_data[robot_id]["playerName"],
        "lap": current_lap,
        "lapTime": lap_time,
        "totalTime": current_time - race_start_time
    }))
    
    # Add event to log
    add_game_event("LAP_COMPLETED", {
        "robotId": robot_id,
        "playerName": player_data[robot_id]["playerName"],
        "lap": current_lap,
        "lapTime": lap_time,
        "timestamp": current_time
    })
    
    # Check if race completed
    if current_lap >= MAX_LAPS:
        finish_race(robot_id)

def finish_race(robot_id):
    """Handle a robot finishing the race"""
    # Update player status
    player_data[robot_id]["status"] = "FINISHED"
    
    # Record finish time
    finish_time = time.time() - race_start_time
    robot_finish_times[robot_id] = finish_time
    
    # Broadcast finish event
    print(json.dumps({
        "action": "robot_finished",
        "robotId": robot_id,
        "playerName": player_data[robot_id]["playerName"],
        "position": get_finish_position(robot_id),
        "finishTime": finish_time,
        "lapTimes": robot_lap_times[robot_id]
    }))
    
    # Add event to log
    add_game_event("ROBOT_FINISHED", {
        "robotId": robot_id,
        "playerName": player_data[robot_id]["playerName"],
        "position": get_finish_position(robot_id),
        "finishTime": finish_time,
        "timestamp": time.time()
    })
    
    # Check if all robots have finished or if this is the winner
    check_race_completion()

def get_finish_position(robot_id):
    """Get the finishing position of a robot"""
    # Count how many robots have already finished
    finished_count = sum(1 for rid, player in player_data.items() 
                     if player["status"] == "FINISHED" and 
                     rid != robot_id and 
                     robot_finish_times.get(rid, float('inf')) < robot_finish_times[robot_id])
    
    # Position is 1-based
    return finished_count + 1

def check_race_completion():
    """Check if the race is complete (all finished or winner determined)"""
    # Count finished robots
    finished_count = sum(1 for player in player_data.values() if player["status"] == "FINISHED")
    
    # End race if all robots have finished
    if finished_count == len(player_data):
        end_race("ALL_FINISHED")
    
    # End race if the first place robot has finished and certain time has passed
    finished_robots = [rid for rid, player in player_data.items() if player["status"] == "FINISHED"]
    if finished_robots:
        # At least one robot has finished
        first_finish_time = min(robot_finish_times[rid] for rid in finished_robots)
        current_duration = time.time() - race_start_time
        
        # If 30 seconds have passed since first robot finished, end the race
        if current_duration - first_finish_time > 30:
            end_race("WINNER_DETERMINED")

def finalize_results():
    """Finalize race results for display and record keeping"""
    # Sort finished robots by finish time
    finished_robots = [rid for rid, player in player_data.items() if player["status"] == "FINISHED"]
    finished_robots.sort(key=lambda rid: robot_finish_times[rid])
    
    # Sort DNF robots by position in race (laps, checkpoints)
    dnf_robots = [rid for rid, player in player_data.items() if player["status"] == "DNF"]
    dnf_robots.sort(key=lambda rid: (robot_laps[rid], robot_checkpoints[rid]), reverse=True)
    
    # Combine for final leaderboard
    final_leaderboard = finished_robots + dnf_robots
    
    # Create final results
    final_results = []
    for i, robot_id in enumerate(final_leaderboard):
        position = i + 1
        status = player_data[robot_id]["status"]
        finish_time = robot_finish_times.get(robot_id, None)
        
        result = {
            "position": position,
            "robotId": robot_id,
            "playerName": player_data[robot_id]["playerName"],
            "robotType": player_data[robot_id]["robotType"],
            "status": status,
            "laps": robot_laps[robot_id],
            "lapTimes": robot_lap_times[robot_id],
            "totalTime": finish_time,
            "bestLap": min(robot_lap_times[robot_id]) if robot_lap_times[robot_id] else None
        }
        
        final_results.append(result)
    
    # Record final results
    print(json.dumps({
        "action": "record_final_results",
        "results": final_results
    }))
    
    # In a real implementation, this would store results in SpacetimeDB

def get_podium_results():
    """Get the top 3 finishers for podium display"""
    # Get finished robots sorted by finish time
    finished_robots = [rid for rid, player in player_data.items() if player["status"] == "FINISHED"]
    finished_robots.sort(key=lambda rid: robot_finish_times[rid])
    
    # Get top 3 (or fewer if not enough finishers)
    podium = []
    for i, robot_id in enumerate(finished_robots[:3]):
        podium.append({
            "position": i + 1,
            "robotId": robot_id,
            "playerName": player_data[robot_id]["playerName"],
            "finishTime": robot_finish_times[robot_id],
            "bestLap": min(robot_lap_times[robot_id]) if robot_lap_times[robot_id] else None
        })
    
    return podium

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

def handle_robot_position_update(robot_id, position):
    """Handle robot position updates from Unity"""
    # Update stored position
    robot_positions[robot_id] = position
    
    # In a real implementation, this would analyze position for gameplay events
    # like detecting if a robot is off-track

def handle_robot_collision(robot_id1, robot_id2, collision_force):
    """Handle collisions between robots"""
    # Log the collision event
    add_game_event("ROBOT_COLLISION", {
        "robotId1": robot_id1,
        "robotId2": robot_id2,
        "force": collision_force,
        "timestamp": time.time()
    })
    
    # In a real implementation, this might apply damage or penalties

def handle_robot_damage(robot_id, damage_amount, source):
    """Handle a robot taking damage"""
    # Log the damage event
    add_game_event("ROBOT_DAMAGE", {
        "robotId": robot_id,
        "amount": damage_amount,
        "source": source,
        "timestamp": time.time()
    })
    
    # In a real implementation, this would update robot health
    # and potentially disable robots that take too much damage

def handle_powerup_collected(robot_id, powerup_type, powerup_id):
    """Handle a robot collecting a powerup"""
    # Log the powerup collection
    add_game_event("POWERUP_COLLECTED", {
        "robotId": robot_id,
        "powerupType": powerup_type,
        "powerupId": powerup_id,
        "timestamp": time.time()
    })
    
    # Apply powerup effects based on type
    if powerup_type == "Speed":
        # In a real implementation, this would boost the robot's speed
        print(json.dumps({
            "action": "apply_speed_boost",
            "robotId": robot_id,
            "duration": 5  # 5 seconds of speed boost
        }))
    elif powerup_type == "Shield":
        # In a real implementation, this would make the robot temporarily invulnerable
        print(json.dumps({
            "action": "apply_shield",
            "robotId": robot_id,
            "duration": 3  # 3 seconds of shield
        }))
    elif powerup_type == "Repair":
        # In a real implementation, this would repair damage
        print(json.dumps({
            "action": "repair_robot",
            "robotId": robot_id,
            "amount": 50  # Repair 50 damage points
        }))

def handle_player_disconnect(player_id):
    """Handle a player disconnecting from the game"""
    # Find the robot for this player
    disconnected_robot_id = None
    for robot_id, player in player_data.items():
        if player["playerId"] == player_id:
            disconnected_robot_id = robot_id
            break
    
    if disconnected_robot_id:
        # Mark the robot as DNF if the race is in progress
        if game_state == "RACING":
            player_data[disconnected_robot_id]["status"] = "DNF"
            
            # Add event to log
            add_game_event("PLAYER_DISCONNECT", {
                "playerId": player_id,
                "robotId": disconnected_robot_id,
                "timestamp": time.time()
            })
            
            # Check if race should end
            check_race_completion()
        
        # In a real implementation, this might keep the robot running on AI
        # or remove it from the game depending on the rules

# Event handler functions - these would be called by SpacetimeDB callbacks in a real implementation
# They are declared here but would need to be connected to SpacetimeDB events

def on_checkpoint_trigger(data):
    """Callback for checkpoint trigger events from Unity"""
    robot_id = data["robotId"]
    checkpoint_index = data["checkpointIndex"]
    handle_checkpoint_trigger(robot_id, checkpoint_index)

def on_robot_position_update(data):
    """Callback for robot position updates from Unity"""
    robot_id = data["robotId"]
    position = (data["position"]["x"], data["position"]["y"], data["position"]["z"])
    handle_robot_position_update(robot_id, position)

def on_robot_collision(data):
    """Callback for robot collision events from Unity"""
    robot_id1 = data["robotId1"]
    robot_id2 = data["robotId2"]
    collision_force = data["force"]
    handle_robot_collision(robot_id1, robot_id2, collision_force)

def on_robot_damage(data):
    """Callback for robot damage events from Unity"""
    robot_id = data["robotId"]
    damage_amount = data["amount"]
    source = data["source"]
    handle_robot_damage(robot_id, damage_amount, source)

def on_powerup_collected(data):
    """Callback for powerup collection events from Unity"""
    robot_id = data["robotId"]
    powerup_type = data["powerupType"]
    powerup_id = data["powerupId"]
    handle_powerup_collected(robot_id, powerup_type, powerup_id)

def on_player_disconnect(data):
    """Callback for player disconnect events from SpacetimeDB"""
    player_id = data["playerId"]
    handle_player_disconnect(player_id)

# Note: The setup() and loop() functions will be called by the game manager framework
# All print statements with JSON would be replaced with calls to SpacetimeDB reducers
# Event handlers would be registered with SpacetimeDB event system
