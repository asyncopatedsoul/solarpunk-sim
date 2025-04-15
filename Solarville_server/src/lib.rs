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