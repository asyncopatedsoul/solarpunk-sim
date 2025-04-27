use spacetimedb::{spacetimedb, Identity, Reducer, ReducerContext, Timestamp};
use serde::{Deserialize, Serialize};

// Vector3 type for positions and rotations
#[spacetimedb(table)]
#[derive(Clone, Debug, PartialEq)]
pub struct Vector3 {
    pub x: f32,
    pub y: f32,
    pub z: f32,
}

// MicroprocessCode table stores user-defined Python scripts
#[spacetimedb(table)]
#[derive(Clone, Debug, PartialEq)]
pub struct MicroprocessCode {
    #[primarykey]
    pub code_id: u64,
    pub owner_id: Identity,
    pub name: String,
    pub file_path: String,
    pub code_content: String,
    pub last_updated: Timestamp,
}

// MicroprocessState table stores the current state of running scripts
#[spacetimedb(table)]
#[derive(Clone, Debug, PartialEq)]
pub struct MicroprocessState {
    #[primarykey]
    pub state_id: u64,
    pub code_id: u64,
    pub left_motor_speed: f32,
    pub right_motor_speed: f32,
    pub error_message: String,
    pub last_updated: Timestamp,
    pub is_running: bool,
}

// Robot table stores information about robots in the simulation
#[spacetimedb(table)]
#[derive(Clone, Debug, PartialEq)]
pub struct Robot {
    #[primarykey]
    pub robot_id: u64,
    pub name: String,
    pub owner_id: Identity,
    pub position: Vector3,
    pub rotation: Vector3,
    pub robot_type: String,
    pub controller_code_id: Option<u64>,
}

// GameObject table stores information about objects in the environment
#[spacetimedb(table)]
#[derive(Clone, Debug, PartialEq)]
pub struct GameObject {
    #[primarykey]
    pub object_id: u64,
    pub name: String,
    pub creator_id: Option<Identity>,
    pub position: Vector3,
    pub rotation: Vector3,
    pub scale: Vector3,
    pub prefab_name: String,
    pub is_static: bool,
}

// Player table stores information about connected players
#[spacetimedb(table)]
#[derive(Clone, Debug, PartialEq)]
pub struct Player {
    #[primarykey]
    pub identity: Identity,
    pub name: String,
    pub last_connected: Timestamp,
}

// Scenario table stores information about game scenarios
#[spacetimedb(table)]
#[derive(Clone, Debug, PartialEq)]
pub struct Scenario {
    #[primarykey]
    pub scenario_id: u64,
    pub name: String,
    pub creator_id: Identity,
    pub description: String,
    pub setup_code_id: u64,
    pub game_logic_code_id: u64,
    pub is_active: bool,
}

// Reducers

#[spacetimedb(reducer)]
pub fn add_microprocess_code(ctx: ReducerContext, name: String, code_content: String, file_path: String) -> u64 {
    let code_id = ctx.get_unique_id();
    MicroprocessCode::insert(MicroprocessCode {
        code_id,
        owner_id: ctx.sender,
        name,
        file_path,
        code_content,
        last_updated: ctx.timestamp,
    });
    code_id
}

#[spacetimedb(reducer)]
pub fn update_microprocess_state(ctx: ReducerContext, code_id: u64, left_motor_speed: f32, right_motor_speed: f32, error_message: String, is_running: bool) {
    // Find existing state or create a new one
    let state_id = match MicroprocessState::filter_by_code_id(&code_id).iter().next() {
        Some(state) => state.state_id,
        None => ctx.get_unique_id(),
    };
    
    MicroprocessState::insert(MicroprocessState {
        state_id,
        code_id,
        left_motor_speed,
        right_motor_speed,
        error_message,
        last_updated: ctx.timestamp,
        is_running,
    });
}

#[spacetimedb(reducer)]
pub fn start_microprocess(ctx: ReducerContext, code_id: u64) {
    // Validate code exists and belongs to sender
    if let Some(code) = MicroprocessCode::filter_by_code_id(&code_id).iter().next() {
        if code.owner_id == ctx.sender {
            // Update state to indicate code is running
            update_microprocess_state(ctx, code_id, 0.0, 0.0, String::new(), true);
        }
    }
}

#[spacetimedb(reducer)]
pub fn stop_microprocess(ctx: ReducerContext, code_id: u64) {
    // Validate code exists and belongs to sender
    if let Some(code) = MicroprocessCode::filter_by_code_id(&code_id).iter().next() {
        if code.owner_id == ctx.sender {
            // Update state to indicate code is not running
            update_microprocess_state(ctx, code_id, 0.0, 0.0, String::new(), false);
        }
    }
}

#[spacetimedb(reducer)]
pub fn register_player(ctx: ReducerContext, name: String) {
    Player::insert(Player {
        identity: ctx.sender,
        name,
        last_connected: ctx.timestamp,
    });
}

#[spacetimedb(reducer)]
pub fn spawn_game_object(ctx: ReducerContext, name: String, position: Vector3, rotation: Vector3, scale: Vector3, prefab_name: String, is_static: bool) -> u64 {
    let object_id = ctx.get_unique_id();
    GameObject::insert(GameObject {
        object_id,
        name,
        creator_id: Some(ctx.sender),
        position,
        rotation,
        scale,
        prefab_name,
        is_static,
    });
    object_id
}

#[spacetimedb(reducer)]
pub fn update_robot_position(ctx: ReducerContext, robot_id: u64, position: Vector3, rotation: Vector3) {
    // Find the robot
    if let Some(mut robot) = Robot::filter_by_robot_id(&robot_id).iter().next().cloned() {
        // Check if the sender is the owner
        if robot.owner_id == ctx.sender {
            robot.position = position;
            robot.rotation = rotation;
            Robot::insert(robot);
        }
    }
}
