# Remote Control Car Implementation

This document explains the architecture and implementation of the remote control car interface in the SolarVille simulation environment.

## Overview

The remote control car implementation consists of several components:

1. **Vue Component** - A frontend interface for controlling the car with keyboard and touch inputs
2. **REPL WebSocket Server** - Handles communication between the frontend and Python firmware
3. **Python Firmware Simulation** - Simulates the car's behavior and physics
4. **SpacetimeDB Integration** - Manages state synchronization across the application

## Components

### Vue Component (`RemoteControlCar.vue`)

The Vue component provides an intuitive interface for controlling the simulated car:

- Keyboard controls: W/S for forward/backward, A/D for steering left/right, spacebar for brake
- Touch-compatible virtual joysticks for mobile devices
- Visual overlay showing active inputs
- Real-time display of motor speeds

This component is integrated into the React application using the Veaury library, which allows seamless use of Vue components within React.

### REPL WebSocket Server

The REPL server manages bidirectional communication between the frontend and Python firmware:

- Creates a new microprocess state in SpacetimeDB when a WebSocket connection is established
- Translates control inputs from the frontend into commands for the Python firmware
- Processes motor state updates from the firmware and broadcasts them to clients
- Updates the SpacetimeDB state to maintain synchronization
- Cleans up resources and marks the state as inactive when a connection closes

### Python Firmware Simulation

The Python firmware (`remote_control_car.py`) simulates the behavior of a remote-controlled car:

- Processes motor control commands
- Simulates physics including movement, rotation, and battery drain
- Reports the car's state back to the REPL server
- Provides a realistic simulation of real-world limitations (max speed, turning radius, etc.)

### SpacetimeDB Integration

SpacetimeDB is used to maintain consistent state across the application:

- Each REPL session creates a microprocess state entry
- Motor state updates are synchronized in real-time
- Multiple clients can observe the same car's behavior
- Unity visualization can read the state from SpacetimeDB
- States are marked as inactive when sessions end

## Communication Flow

1. User interacts with the RemoteControlCar Vue component (keyboard/touch)
2. Input is translated into motor commands (`set_motors(left, right)`)
3. Commands are sent via WebSocket to the REPL server
4. REPL server forwards commands to the Python firmware
5. Python firmware updates the car's state and reports back
6. REPL server receives state updates and:
   - Broadcasts updates to connected clients
   - Updates the SpacetimeDB state
7. Unity visualization reads the state from SpacetimeDB and renders the car

## Implementation Details

### State Management

Each remote control session follows this lifecycle:

1. **Connection**: When a WebSocket connection is established:
   - A new REPL session is created
   - A microprocess code entry is created in SpacetimeDB with placeholder code
   - A microprocess state entry is created and marked as running

2. **Operation**: During the session:
   - Motor commands update the session's state
   - The state is synchronized with SpacetimeDB
   - The Python firmware simulates the car's behavior

3. **Disconnection**: When the WebSocket connection is closed:
   - The REPL session is terminated
   - The microprocess state is marked as not running in SpacetimeDB
   - Resources are cleaned up

### Firmware Message Format

The Python firmware uses a simple protocol to communicate with the REPL server:

- Motor state updates: `MOTOR_STATE:left,right`
  - Example: `MOTOR_STATE:0.75,-0.5` (left motor at 75% forward, right motor at 50% reverse)

### SpacetimeDB Schema

The microprocess state in SpacetimeDB includes:

- `state_id`: Unique identifier
- `code_id`: Reference to the microprocess code
- `left_motor_speed`: Current speed of the left motor (-1.0 to 1.0)
- `right_motor_speed`: Current speed of the right motor (-1.0 to 1.0)
- `error_message`: Error information (if any)
- `last_updated`: Timestamp of the last update
- `is_running`: Whether the microprocess is currently active

## Future Improvements

Potential enhancements for the remote control system:

1. Additional sensors (proximity, line following, etc.)
2. Customizable car parameters (max speed, acceleration, etc.)
3. Multiple car types with different behavior characteristics
4. Obstacle courses and structured challenges
5. Multiplayer racing capabilities

## Usage

1. Start the SpacetimeDB server
2. Run the Node.js REPL server
3. Launch the React client application
4. Navigate to the "Remote Control Car" tab
5. Click the button to open the fullscreen interface
6. Control the car using keyboard or touch inputs

The car's state will be synchronized across all connected clients and visualized in Unity.