pub mod math;

use math::DbVector3;
// use rand::Rng;
// use spacetimedb::{spacetimedb_lib::ScheduleAt, Identity, ReducerContext, Table};
use spacetimedb::{spacetimedb_lib::ScheduleAt, Identity, ReducerContext, Table, Timestamp, TimeDuration};

#[spacetimedb::table(name = world_config, public)]
pub struct WorldConfig {
    #[primary_key]
    pub id: u32,
    pub world_name: String,
}

#[spacetimedb::table(name = player, public)]
#[spacetimedb::table(name = logged_out_player)]
#[derive(Debug, Clone)]
pub struct Player {
    #[primary_key]
    pub identity: Identity,
    #[unique]
    #[auto_inc]
    pub player_id: u32,
    pub name: String,
    pub position: DbVector3,
    pub rotation: DbVector3,
    pub last_update: spacetimedb::Timestamp,
}

// Tables for the microprocessor simulation
#[spacetimedb::table(name = microprocess_code, public)]
#[derive(Debug, Clone)]
pub struct MicroprocessCode {
    #[primary_key]
    #[auto_inc]
    pub code_id: u32,
    #[index(btree)]
    pub owner_id: u32, // References player_id
    pub name: String,
    pub file_path: String,
    pub code_content: String,
    pub last_updated: spacetimedb::Timestamp,
}

#[spacetimedb::table(name = microprocess_state, public)]
#[derive(Debug, Clone)]
pub struct MicroprocessState {
    #[primary_key]
    #[auto_inc]
    pub state_id: u32,
    #[index(btree)]
    pub code_id: u32, // References microprocess_code.code_id
    pub left_motor_speed: f32,  // Speed from -1.0 to 1.0
    pub right_motor_speed: f32, // Speed from -1.0 to 1.0
    pub error_message: String,
    pub last_updated: spacetimedb::Timestamp,
    pub is_running: bool,
}

#[spacetimedb::table(name = update_player_timer, scheduled(update_player_positions))]
pub struct UpdatePlayerTimer {
    #[primary_key]
    #[auto_inc]
    scheduled_id: u64,
    scheduled_at: spacetimedb::ScheduleAt,
}

#[spacetimedb::reducer(init)]
pub fn init(ctx: &ReducerContext) -> Result<(), String> {
    log::info!("Initializing Solarville Server...");
    ctx.db.world_config().try_insert(WorldConfig {
        id: 0,
        world_name: "Solarville".to_string(),
    })?;

    // Set up the timer for player position updates
    ctx.db.update_player_timer().try_insert(UpdatePlayerTimer {
        scheduled_id: 0,
        scheduled_at: ScheduleAt::Interval(std::time::Duration::from_millis(100).into()),
    })?;

    Ok(())
}

#[spacetimedb::reducer(client_connected)]
pub fn connect(ctx: &ReducerContext) -> Result<(), String> {
    log::info!("Client connected: {:?}", ctx.sender);

    if let Some(player) = ctx.db.logged_out_player().identity().find(&ctx.sender) {
        ctx.db.player().insert(player.clone());
        ctx.db
            .logged_out_player()
            .identity()
            .delete(&player.identity);
    } else {
        ctx.db.player().try_insert(Player {
            identity: ctx.sender,
            player_id: 0,
            name: String::new(),
            position: DbVector3::new(0.0, 0.0, 0.0),
            rotation: DbVector3::new(0.0, 0.0, 0.0),
            last_update: ctx.timestamp,
        })?;
    }
    Ok(())
}

#[spacetimedb::reducer(client_disconnected)]
pub fn disconnect(ctx: &ReducerContext) -> Result<(), String> {
    log::info!("Client disconnected: {:?}", ctx.sender);
    
    // Remove the player when they disconnect
    if let Some(player) = ctx.db.player().identity().find(&ctx.sender) {
        ctx.db.player().identity().delete(&ctx.sender);
        log::info!("Player removed: {}", player.name);
    }

    Ok(())
}

#[spacetimedb::reducer]
pub fn register_player(ctx: &ReducerContext, name: String, position: DbVector3, rotation: DbVector3) -> Result<(), String> {
    log::info!("Registering player with name: {}", name);
    
    // If the player already exists, update their info
    if let Some(mut player) = ctx.db.player().identity().find(&ctx.sender) {
        player.name = name;
        player.position = position;
        player.rotation = rotation;
        player.last_update = ctx.timestamp;
        ctx.db.player().identity().update(player);
        // log::info!("Updated existing player: {}", name);
    } else {
        // Create a new player
        ctx.db.player().try_insert(Player {
            identity: ctx.sender,
            player_id: 0, // Will be auto-incremented
            name,
            position,
            rotation,
            last_update: ctx.timestamp,
        })?;
        log::info!("Created new player");
    }

    Ok(())
}

#[spacetimedb::reducer]
pub fn update_player_position(ctx: &ReducerContext, position: DbVector3, rotation: DbVector3) -> Result<(), String> {
    // Find the player and update their position and rotation
    if let Some(mut player) = ctx.db.player().identity().find(&ctx.sender) {
        player.position = position;
        player.rotation = rotation;
        player.last_update = ctx.timestamp;
        ctx.db.player().identity().update(player);
    } else {
        return Err("Player not found".to_string());
    }

    Ok(())
}

#[spacetimedb::reducer]
pub fn update_player_positions(_ctx: &ReducerContext, _timer: UpdatePlayerTimer) -> Result<(), String> {
    // This scheduled reducer runs periodically to broadcast position updates
    // In a more complex implementation, we might do server-side validation or physics here
    Ok(())
}

#[spacetimedb::reducer]
pub fn update_microprocess_code(ctx: &ReducerContext, name: String, file_path: String, code_content: String) -> Result<(), String> {
    // Get the player ID
    let player = ctx.db.player().identity().find(&ctx.sender).ok_or("Player not found")?;
    
    // Check if code with this name already exists for this player
    let existing_code = ctx.db.microprocess_code()
        .iter()
        .find(|code| code.owner_id == player.player_id && code.name == name);
    
    if let Some(mut existing_code) = existing_code {
        // Update existing code
        existing_code.file_path = file_path;
        existing_code.code_content = code_content;
        existing_code.last_updated = ctx.timestamp;
        ctx.db.microprocess_code().code_id().update(existing_code.clone());
        log::info!("Updated microprocessor code: {}", name);
        
        // Update any associated state
        // if let Some(mut state) = ctx.db.microprocess_state().code_id().eq(&existing_code.code_id) {
        if let Some(mut state) = ctx.db.microprocess_state().iter()
            .find(|state| state.code_id == existing_code.code_id) {
            state.is_running = false; // Reset running state when code is updated
            state.error_message = "Code updated, awaiting execution".to_string();
            state.last_updated = ctx.timestamp;
            ctx.db.microprocess_state().state_id().update(state);
        }
        
        Ok(())
    } else {
        // Create new code
        let code = ctx.db.microprocess_code().try_insert(MicroprocessCode {
            code_id: 0, // Auto-incremented
            owner_id: player.player_id,
            name,
            file_path,
            code_content,
            last_updated: ctx.timestamp,
        })?;
        
        // Create initial state entry
        ctx.db.microprocess_state().try_insert(MicroprocessState {
            state_id: 0, // Auto-incremented
            code_id: code.code_id,
            left_motor_speed: 0.0,
            right_motor_speed: 0.0,
            error_message: "Awaiting execution".to_string(),
            last_updated: ctx.timestamp,
            is_running: false,
        })?;
        
        log::info!("Created new microprocessor code: {} with ID: {}", code.name, code.code_id);
        Ok(())
    }
}

#[spacetimedb::reducer]
pub fn update_microprocess_state(
    ctx: &ReducerContext, 
    code_id: u32, 
    left_motor_speed: f32, 
    right_motor_speed: f32, 
    error_message: String, 
    is_running: bool
) -> Result<(), String> {
    // Ensure code exists
    let code = ctx.db.microprocess_code().iter()
    .find(|code| code.code_id == code_id).ok_or("Code not found")?;
    
    // Clamp motor speeds between -1.0 and 1.0
    let left_speed = left_motor_speed.max(-1.0).min(1.0);
    let right_speed = right_motor_speed.max(-1.0).min(1.0);
    
    // Update state
    if let Some(mut state) = ctx.db.microprocess_state().iter()
            .find(|state| state.code_id == code_id) {
        state.left_motor_speed = left_speed;
        state.right_motor_speed = right_speed;
        state.error_message = error_message;
        state.last_updated = ctx.timestamp;
        state.is_running = is_running;
        ctx.db.microprocess_state().state_id().update(state);
        log::info!("Updated microprocessor state for code_id: {}", code_id);
    } else {
        // Create new state if it doesn't exist
        ctx.db.microprocess_state().try_insert(MicroprocessState {
            state_id: 0, // Auto-incremented
            code_id,
            left_motor_speed: left_speed,
            right_motor_speed: right_speed,
            error_message,
            last_updated: ctx.timestamp,
            is_running,
        })?;
        log::info!("Created new microprocessor state for code_id: {}", code_id);
    }
    
    Ok(())
}

#[spacetimedb::reducer]
pub fn start_microprocess(ctx: &ReducerContext, code_id: u32) -> Result<(), String> {
    // Check if code exists and belongs to caller
    let code = ctx.db.microprocess_code().iter()
    .find(|code| code.code_id == code_id).ok_or("Code not found")?;
    let player = ctx.db.player().identity().find(&ctx.sender).ok_or("Player not found")?;
    
    if code.owner_id != player.player_id {
        return Err("You don't own this code".to_string());
    }
    
    // Update state to indicate it should start running
    if let Some(mut state) = ctx.db.microprocess_state().iter()
            .find(|state| state.code_id == code_id) {
        state.is_running = true;
        state.error_message = "Starting execution".to_string();
        state.last_updated = ctx.timestamp;
        ctx.db.microprocess_state().state_id().update(state);
        log::info!("Started microprocessor execution for code_id: {}", code_id);
    } else {
        // Create new state if it doesn't exist
        ctx.db.microprocess_state().try_insert(MicroprocessState {
            state_id: 0, // Auto-incremented
            code_id,
            left_motor_speed: 0.0,
            right_motor_speed: 0.0,
            error_message: "Starting execution".to_string(),
            last_updated: ctx.timestamp,
            is_running: true,
        })?;
        log::info!("Created and started microprocessor state for code_id: {}", code_id);
    }
    
    Ok(())
}

#[spacetimedb::reducer]
pub fn stop_microprocess(ctx: &ReducerContext, code_id: u32) -> Result<(), String> {
    // Check if code exists and belongs to caller
    let code = ctx.db.microprocess_code().iter()
    .find(|code| code.code_id == code_id).ok_or("Code not found")?;
    let player = ctx.db.player().identity().find(&ctx.sender).ok_or("Player not found")?;
    
    if code.owner_id != player.player_id {
        return Err("You don't own this code".to_string());
    }
    
    // Update state to indicate it should stop running
    if let Some(mut state) = ctx.db.microprocess_state().iter()
            .find(|state| state.code_id == code_id) {
        state.is_running = false;
        state.left_motor_speed = 0.0;
        state.right_motor_speed = 0.0;
        state.error_message = "Execution stopped".to_string();
        state.last_updated = ctx.timestamp;
        ctx.db.microprocess_state().state_id().update(state);
        log::info!("Stopped microprocessor execution for code_id: {}", code_id);
    } else {
        return Err("Microprocessor state not found".to_string());
    }
    
    Ok(())
}