# Robotics Simulation Platform

A full-stack demo for simulating embedded systems (robots and IoT devices) controlled by player-defined Python scripts.

## Overview

This project provides a comprehensive environment for learning and experimenting with embedded systems programming in a simulated setting. It enables users to:

- Write MicroPython code to control virtual robots and IoT devices
- Test their code in a realistic physics simulation
- Create custom UI displays using LVGL in MicroPython
- Collaborate with others in a multiplayer environment
- Define custom scenarios and game mechanics

The platform is built around SpacetimeDB, which provides real-time state synchronization across all components, enabling a seamless multiplayer experience.

## Architecture

The project consists of several key components:

### SpacetimeDB Server (`/spacetime-server`)

- Central database for state synchronization
- Handles all data persistence and real-time updates
- Stores robot code, state, and scenario definitions
- Written in Rust using the SpacetimeDB framework

### Node.js Server (`/node-server`)

- Manages Python processes that run robot code
- Communicates with SpacetimeDB for state updates
- Handles process lifecycle and error management
- Provides API endpoints for system control
- Supports multiple Python execution modes (single process, multi-process, multi-thread, and Pyodide)
- Provides WebSocket API for REPL and global state observation

### Unity Client (`/unity-client`)

- 3D visualization and physics simulation
- Renders robots, environments, and interactions
- Embeds web views for UI elements
- Connects to SpacetimeDB for state synchronization

### Web Clients (`/web-clients`)

- **Remote Control**: Browser-based control interface for robots
- **Display Simulation**: LVGL-based UI simulations for robot displays
- **Creator Tools**: Python code editor, REPL console, and global state visualization
- Built with React and Svelte
- Can be embedded in Unity or used standalone

### MicroPython Runtime (`/micropython-runtime`)

- Simulated environment for running MicroPython code
- Provides APIs for robot control and sensing
- Supports LVGL for UI development
- Runtime environment for user-defined scripts
- Multiple execution modes (single process, multi-process, multi-thread)

## Setup and Installation

### Prerequisites

- Node.js (v16 or later)
- Python 3.9 or later
- Unity 2022.3 or later
- Rust and Cargo
- SpacetimeDB CLI

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/your-org/robotics-simulation-platform.git
   cd robotics-simulation-platform
   ```

2. Install SpacetimeDB server:
   ```
   cd spacetime-server
   spacetime build
   ```

3. Install Node.js dependencies:
   ```
   cd ../node-server
   npm install
   ```

4. Install web client dependencies:
   ```
   cd ../web-clients
   npm install
   ```

5. Open the Unity project:
   - Launch Unity Hub
   - Add the `unity-client` directory as a project
   - Open the project and ensure all dependencies are installed

## Running the Platform

1. Start the SpacetimeDB server:
   ```
   cd spacetime-server
   spacetime serve
   ```

2. Start the Node.js server:
   ```
   cd node-server
   npm start
   ```

3. Start the web clients (for development):
   ```
   cd web-clients
   npm start
   ```

4. Launch the Unity client from the Unity Editor

Alternatively, you can use the provided script to start all components:
```
./start_dev.sh
```

## Running Creator Tools Web App and Node.js Server (Development)

For development and testing of the Python execution features, you can run just the creator-tools web app and Node.js server:

1. Start the Node.js server:
   ```bash
   cd monorepo/node-server
   npm install  # Only needed the first time or when dependencies change
   npm run dev  # Starts the server with hot-reloading using nodemon
   ```
   
   The Node.js server will run on http://localhost:3001 by default.

2. Start the creator-tools web app:
   ```bash
   cd monorepo/web-clients/creator-tools
   npm install  # Only needed the first time or when dependencies change
   npm run dev  # Starts the SvelteKit dev server
   ```
   
   The creator-tools web app will run on http://localhost:5173 by default.

3. Open your browser and navigate to http://localhost:5173/demo to access the demo pages.

4. Click on the "Python Executor" demo to test the Python code execution features:
   - Edit Python code in the editor
   - Select different execution modes (single process, multi-process, multi-thread, Pyodide)
   - Start/stop Python execution
   - Interact with the REPL console
   - Monitor the global state of the Python execution

### Execution Modes

The creator-tools support multiple execution modes for Python code:

- **Single Process**: Executes code in a single Python process (default mode)
- **Multi-Process**: Executes code in multiple Python processes (better for CPU-intensive tasks)
- **Multi-Thread**: Executes code in multiple threads within a single Python process (better for I/O-bound tasks)
- **Pyodide**: Executes code in the browser using WebAssembly (experimental)

Note: REPL commands work differently depending on the execution mode:
- In single process and multi-thread modes, REPL commands interact with the Python global context
- In multi-process mode, REPL commands are not supported (due to process isolation)
- In Pyodide mode, REPL commands execute in the browser's WebAssembly environment

## Creating Your First Robot

1. Open the Unity client or web-based code editor
2. Create a new Python script with the following structure:

```python
def setup():
    """Initialize the robot"""
    print("Robot initialized")
    set_motors(0, 0)  # Start with motors stopped

def loop():
    """Main control loop - runs repeatedly"""
    # Move forward
    set_motors(0.5, 0.5)
    
    # Check for obstacles using simulated distance sensor
    distance = read_distance_sensor()
    
    if distance < 0.5:
        # Obstacle detected, turn left
        set_motors(-0.2, 0.5)
```

3. Save the script and run it to see your robot in action
4. Modify the code and experiment with different behaviors

## Creating Custom UI Displays

For robots with displays, you can create custom UI using LVGL in MicroPython:

```python
import lvgl as lv

def setup():
    """Initialize the display"""
    # Get the screen
    scr = lv.scr_act()
    
    # Create a label
    label = lv.label(scr)
    label.set_text("Hello, Robot!")
    label.center()

def loop():
    """Update the display"""
    # This will be called repeatedly
    # Update your UI elements here
    pass
```

## Project Structure

```
/
├── spacetime-server/       # SpacetimeDB server code
│   ├── src/                # Rust source code
│   └── schema/             # Database schema definitions
│
├── node-server/            # Node.js server for managing Python processes
│   ├── src/                # JavaScript source code
│   └── scripts/            # Utility scripts
│
├── unity-client/           # Unity project for 3D visualization
│   ├── Assets/             # Unity assets
│   └── ProjectSettings/    # Unity project settings
│
├── web-clients/            # Web applications
│   ├── remote-control/     # Remote control interface
│   ├── display-simulation/ # Display simulation interfaces
│   └── creator-tools/      # Python code editor and execution tools
│
├── micropython-runtime/    # MicroPython environment
│   ├── firmware/           # Core firmware code
│   ├── lvgl/               # LVGL integration
│   └── examples/           # Example scripts
│
├── shared/                 # Shared code and utilities
│   ├── types/              # Type definitions
│   └── utils/              # Utility functions
│
└── docs/                   # Documentation
```

## Contributing

We welcome contributions to the Robotics Simulation Platform! Please see our [Contributing Guide](docs/CONTRIBUTING.md) for more information.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

This project uses the following open-source libraries:
- [SpacetimeDB](https://github.com/clockworklabs/SpacetimeDB)
- [MicroPython](https://micropython.org/)
- [LVGL](https://lvgl.io/)
- [React](https://reactjs.org/)
- [Svelte](https://svelte.dev/)
- [Unity](https://unity.com/)
- [CodeJar](https://github.com/antonmedv/codejar)
- [Prism](https://prismjs.com/)
- [Pyodide](https://pyodide.org/)
