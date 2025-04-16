# Solarville Microprocessor Simulation

This system implements a simulated microprocessor environment that allows players to write Python code to control vehicles in the game. The code is executed in a real Python environment managed by a Node.js process, creating a realistic simulation of embedded systems programming.

## System Architecture

The microprocessor simulation consists of three main components:

1. **Spacetime DB Server** - The central database that stores code and state
2. **Node.js Client** - Manages Python processes and updates state
3. **Unity Client** - Visualizes the vehicles and provides a code editor

### Data Flow

1. A player writes Python code in the Unity client's editor
2. The code is saved to the Spacetime DB in the `microprocess_code` table
3. The Node.js client detects the new code and creates a Python process to execute it
4. The Python process outputs motor states (left/right motor speeds)
5. The Node.js client captures this output and updates the `microprocess_state` table
6. The Unity client receives the state updates and moves the vehicle accordingly

## Components

### Spacetime DB Tables

- **microprocess_code**: Stores Python code with metadata
  - `code_id`: Unique identifier for the code
  - `owner_id`: Player ID of the owner
  - `name`: Name of the script
  - `file_path`: Path to the file (for organization)
  - `code_content`: The actual Python code
  - `last_updated`: Timestamp of the last update

- **microprocess_state**: Stores the current state of the microprocessor
  - `state_id`: Unique identifier for the state
  - `code_id`: Reference to the associated code
  - `left_motor_speed`: Speed of the left motor (-1.0 to 1.0)
  - `right_motor_speed`: Speed of the right motor (-1.0 to 1.0)
  - `error_message`: Any error that occurred during execution
  - `last_updated`: Timestamp of the last update
  - `is_running`: Whether the code is currently running

### Node.js Client

The Node.js client (`solarville_clients/nodejs_client`) is responsible for:

1. Monitoring the Spacetime DB for new code and state changes
2. Creating and managing Python processes to execute the code
3. Capturing Python output and updating state in the Spacetime DB
4. Handling errors and cleanup of processes

### Unity Client

The Unity client components include:

1. **MicroprocessorManager.cs**: Manages the creation and updating of vehicles
2. **VehicleController.cs**: Controls vehicle movement based on motor speeds
3. **CodeEditorUI.cs**: Provides a UI for editing and managing Python code

## Python Programming Interface

The Python code runs in a simulated environment that provides the following functions:

- `set_left_motor(speed)`: Set the speed of the left motor (-1.0 to 1.0)
- `set_right_motor(speed)`: Set the speed of the right motor (-1.0 to 1.0)
- `set_motors(left, right)`: Set the speeds of both motors simultaneously

Each Python script should define:

- `setup()`: Function called once when the script starts
- `loop()`: Function called repeatedly (approximately 10 times per second)

Example script:

```python
def setup():
    """Initialize the vehicle"""
    print("Starting up...")
    set_motors(0, 0)  # Start with motors stopped

def loop():
    """Main control loop"""
    # Move forward
    set_motors(0.7, 0.7)
    
    # You could add more complex behavior here!
```

## Setup and Usage

### Setting Up the Spacetime DB Server

1. Navigate to the `Solarville_server` directory
2. Run the server with `spacetime serve`

### Starting the Node.js Client

1. Navigate to the `solarville_clients/nodejs_client` directory
2. Run `npm install` to install dependencies
3. Start the client with `npm start`

### Using the Unity Client

1. Open the Solarville Unity project
2. Make sure the GameManager component is pointing to the correct server URL
3. Run the game and connect to the server
4. Press the "Code Editor" button to open the code editor
5. Write or select a script, then press "Save" and "Run"
6. Watch your vehicle move according to your code!

## Advanced Usage

### Manual Testing

You can manually test the Node.js client by running:

```
node src/index.js save "TestScript" "/test/script.py" "examples/simple_car.py"
node src/index.js start <code_id>
node src/index.js stop <code_id>
```

### Advanced Python Scripts

You can create more complex behaviors using Python:

- Line following using simulated sensors
- Obstacle avoidance
- Autonomous exploration
- Time-based maneuvers
- Procedural movement patterns

Example scripts are provided in the `solarville_clients/nodejs_client/examples` directory.

## Troubleshooting

- **Vehicle not moving**: Check the state in Spacetime DB and make sure the Python process is running
- **Python errors**: Check the error message in the microprocess_state table
- **Connection issues**: Make sure the Spacetime DB server is running and accessible

## Extending the System

The microprocessor simulation can be extended with:

- Additional sensor types (proximity, light, etc.)
- More vehicle types with different movement mechanics
- Interactive environments that respond to the vehicles
- Multi-vehicle coordination
- Educational tutorials and challenges