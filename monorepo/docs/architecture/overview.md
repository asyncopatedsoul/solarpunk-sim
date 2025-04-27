# Robotics Simulation Platform Architecture

## System Overview

The Robotics Simulation Platform is designed as a distributed system with several interconnected components, all centered around a real-time state synchronization database (SpacetimeDB). This architecture enables a seamless multiplayer experience where multiple users can interact with the same simulated environment simultaneously.

## Core Components

### 1. SpacetimeDB Server

**Purpose**: Central state synchronization and persistence layer for the entire system.

**Key Responsibilities**:
- Store and manage all simulation state
- Synchronize state across all connected clients
- Store user-defined Python scripts
- Process state changes through reducers
- Maintain persistence of game state across sessions

**Technologies**:
- SpacetimeDB (Rust-based distributed database)
- Rust programming language
- WebSocket for realtime communication

### 2. Node.js Server

**Purpose**: Manage the execution of Python code in isolated processes.

**Key Responsibilities**:
- Create and manage Python processes for each robot
- Monitor SpacetimeDB for code changes
- Execute user code in a sandboxed environment
- Capture output from Python processes
- Update robot state in SpacetimeDB based on code execution
- Handle process errors and lifecycle management

**Technologies**:
- Node.js runtime
- Python-Shell for process management
- SpacetimeDB client SDK
- Express.js for REST API

### 3. Unity Client

**Purpose**: Provide a 3D visualization of the simulation with physics interactions.

**Key Responsibilities**:
- Render the 3D environment, robots, and objects
- Handle physics simulation using Unity's physics engine
- Embed web views for UI elements
- Connect to SpacetimeDB for state synchronization
- Provide a game interface for direct robot control
- Instantiate and update game objects based on simulation state

**Technologies**:
- Unity game engine
- C# programming language
- SpacetimeDB client SDK for Unity
- WebView integration for UI embedding

### 4. Web Clients

**Purpose**: Provide browser-based interfaces for robot control and display simulation.

**Components**:

#### Remote Control Interface
- Control robots manually
- Monitor robot state
- Switch between manual and automated control
- View simulation status

#### Display Simulation
- Simulate embedded displays (e.g., robot face)
- Provide touchscreen interfaces for in-game interaction
- Display simulation status information
- Show simulated sensor outputs

**Technologies**:
- React.js framework
- TypeScript
- SpacetimeDB client SDK for web
- CSS for UI styling

### 5. MicroPython Runtime

**Purpose**: Provide a simulated environment for MicroPython code execution.

**Key Responsibilities**:
- Execute user-defined MicroPython code
- Provide APIs for robot control (motors, sensors, etc.)
- Support LVGL for UI development
- Simulate embedded system constraints
- Report execution state back to the Node.js server

**Technologies**:
- MicroPython interpreter
- Python subprocess management
- LVGL (Lightweight and Versatile Graphics Library)
- JSON for communication with Node.js server

## Communication Flow

1. **Code Creation and Storage**:
   - User writes Python code in the Unity client or web interface
   - Code is sent to SpacetimeDB through a reducer call
   - SpacetimeDB stores the code and notifies connected clients

2. **Code Execution**:
   - Node.js server detects new or updated code in SpacetimeDB
   - Node.js server creates or updates a Python process with the code
   - Python process executes the code, simulating the robot's behavior
   - Python process outputs motor commands and other state changes

3. **State Synchronization**:
   - Node.js server captures outputs from Python processes
   - Node.js server updates state in SpacetimeDB
   - SpacetimeDB propagates state changes to all connected clients
   - Unity client and web interfaces update their displays based on state changes

4. **User Interaction**:
   - User interacts with the Unity client or web interface
   - Interactions are sent to SpacetimeDB through reducer calls
   - SpacetimeDB updates state and notifies all clients
   - Node.js server detects state changes and manages Python processes accordingly

## Data Model

### Core Tables in SpacetimeDB

1. **MicroprocessCode**:
   - `code_id`: Unique identifier
   - `owner_id`: Owner's identity
   - `name`: Script name
   - `file_path`: Virtual file path
   - `code_content`: The actual Python code
   - `last_updated`: Last modification timestamp

2. **MicroprocessState**:
   - `state_id`: Unique identifier
   - `code_id`: Reference to associated code
   - `left_motor_speed`: Speed of left motor (-1.0 to 1.0)
   - `right_motor_speed`: Speed of right motor (-1.0 to 1.0)
   - `error_message`: Any error from execution
   - `last_updated`: Last update timestamp
   - `is_running`: Whether code is executing

3. **Robot**:
   - `robot_id`: Unique identifier
   - `name`: Robot name
   - `owner_id`: Owner's identity
   - `position`: 3D position vector
   - `rotation`: 3D rotation vector
   - `robot_type`: Type of robot
   - `controller_code_id`: Reference to controlling code

4. **GameObject**:
   - `object_id`: Unique identifier
   - `name`: Object name
   - `creator_id`: Creator's identity
   - `position`: 3D position vector
   - `rotation`: 3D rotation vector
   - `scale`: 3D scale vector
   - `prefab_name`: Reference to Unity prefab
   - `is_static`: Whether object is static in physics

5. **Player**:
   - `identity`: Unique identity
   - `name`: Player name
   - `last_connected`: Last connection timestamp

6. **Scenario**:
   - `scenario_id`: Unique identifier
   - `name`: Scenario name
   - `creator_id`: Creator's identity
   - `description`: Scenario description
   - `setup_code_id`: Reference to setup code
   - `game_logic_code_id`: Reference to game logic code
   - `is_active`: Whether scenario is active

## Sequence Diagrams

### Robot Code Execution Flow

```
┌─────────┐  ┌──────────────┐  ┌───────────┐  ┌─────────────┐  ┌────────────────┐
│  User   │  │ Unity Client │  │SpacetimeDB│  │ Node Server │  │Python Process  │
└────┬────┘  └──────┬───────┘  └─────┬─────┘  └──────┬──────┘  └────────┬───────┘
     │              │                │                │                 │
     │ Write Code   │                │                │                 │
     │────────────>│                │                │                 │
     │              │                │                │                 │
     │              │  Save Code     │                │                 │
     │              │───────────────>│                │                 │
     │              │                │                │                 │
     │              │                │  Notify Code   │                 │
     │              │                │  Change        │                 │
     │              │                │───────────────>│                 │
     │              │                │                │                 │
     │              │                │                │  Create Process │
     │              │                │                │────────────────>│
     │              │                │                │                 │
     │              │                │                │<─ Output State ─│
     │              │                │                │                 │
     │              │                │  Update State  │                 │
     │              │                │<───────────────│                 │
     │              │                │                │                 │
     │              │  State Change  │                │                 │
     │              │<───────────────│                │                 │
     │              │                │                │                 │
     │ See Results  │                │                │                 │
     │<─────────────│                │                │                 │
     │              │                │                │                 │
```

### Remote Control Flow

```
┌─────────┐  ┌────────────┐  ┌───────────┐  ┌────────────┐  ┌────────┐
│  User   │  │ Web Client │  │SpacetimeDB│  │ Node Server│  │ Robot  │
└────┬────┘  └─────┬──────┘  └─────┬─────┘  └─────┬──────┘  └───┬────┘
     │             │               │               │            │
     │ Move Joystick              │               │            │
     │───────────>│               │               │            │
     │             │               │               │            │
     │             │ Update Motors │               │            │
     │             │──────────────>│               │            │
     │             │               │               │            │
     │             │               │ Notify State  │            │
     │             │               │ Change        │            │
     │             │               │──────────────>│            │
     │             │               │               │            │
     │             │               │               │ Set Motors │
     │             │               │               │──────────>│
     │             │               │               │            │
     │             │               │ Update Physics│            │
     │             │               │<─────────────────────────────
     │             │               │               │            │
     │             │ State Change  │               │            │
     │             │<──────────────│               │            │
     │             │               │               │            │
     │ See Movement│               │               │            │
     │<────────────│               │               │            │
     │             │               │               │            │
```

## Deployment Architecture

The system can be deployed in various configurations:

### Local Development
- All components run on a single machine
- SpacetimeDB runs in local mode
- Useful for individual development and testing

### Server-Client
- SpacetimeDB and Node.js server run on a server
- Unity client and web clients run on user machines
- Supports multiplayer with centralized state

### Cloud Deployment
- SpacetimeDB and Node.js server run in cloud containers
- Web clients served from static hosting
- Unity client distributed to users
- Scales to support many simultaneous users

## Security Considerations

1. **Code Execution**:
   - Python code executes in sandboxed processes
   - Resource limits prevent excessive CPU or memory usage
   - Filesystem access is restricted to safe directories

2. **Authentication**:
   - SpacetimeDB handles user identity and authentication
   - Ownership of robots and code is tracked and enforced
   - Actions are authorized based on ownership

3. **Network Security**:
   - WebSocket connections are secured with TLS
   - API endpoints require proper authentication
   - Rate limiting prevents abuse

## Performance Considerations

1. **State Synchronization**:
   - Optimized for minimal network traffic
   - Delta updates reduce bandwidth requirements
   - Local caching improves responsiveness

2. **Physics Simulation**:
   - Runs at fixed timestep for consistent behavior
   - Level of detail adjusts based on client capabilities
   - Optimization for mobile devices available

3. **Code Execution**:
   - Process pool manages resource utilization
   - Long-running operations are monitored and throttled
   - Error recovery mechanisms prevent cascading failures
