# Solarville Microprocess REPL Interface

This feature adds a Read-Eval-Print-Loop (REPL) interface to the React client application, allowing users to write, test, save, and execute Python code for microprocesses.

## Components

1. **ReplInterface Component**
   - Interactive Python code editor
   - Real-time motor control sliders
   - Save, start, and stop script functionality
   - Integrated output console displaying execution results

2. **App.tsx Updates**
   - Tabbed interface to switch between REPL and saved scripts
   - Script listing with status indicators and controls
   - Code preview with motor state information

## Technologies Used

- **React** for the UI components
- **Socket.io** for real-time communication with the Python REPL server
- **SpacetimeDB** for script storage and state management

## How It Works

1. **Python REPL Communication**
   - Connects to the REPL server via WebSockets (port 3100)
   - Sends Python code for execution
   - Displays execution output in real-time
   - Allows for interactive control of motor values

2. **Script Management**
   - Save scripts to the database with unique code IDs
   - Start/stop execution through SpacetimeDB reducers
   - View state updates and motor values in real-time

## Getting Started

1. Run the Node.js client to start the REPL server:
   ```
   cd solarville_clients/nodejs_client
   npm run dev
   ```

2. In another terminal, run the React client:
   ```
   cd solarville_clients/react_client
   npm run dev
   ```

3. Or use the provided script to start both:
   ```
   ./start_dev.sh
   ```

4. Navigate to the React app (usually at http://localhost:5173)
