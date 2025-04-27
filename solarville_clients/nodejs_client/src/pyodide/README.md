# Pyodide Integration for Solarpunk-Sim

This module provides a complete solution for running Python code in WebAssembly using Pyodide, with support for:

1. Inspecting and reacting to changes in Python variables
2. Interactive REPL capabilities for users
3. Integration with SpacetimeDB for state persistence
4. WebSocket-based communication with clients

## Architecture

The system consists of several components:

### PyodideProcess

`PyodideProcess` manages a single Pyodide instance and provides:

- Running Python code
- Watching variables for changes
- Getting/setting variable values
- Event emission for variable changes and other lifecycle events

### PyodideManager

`PyodideManager` manages multiple Pyodide processes:

- Creating and destroying processes
- Forwarding events from processes
- Central management of all Pyodide instances

### PyodideREPL

`PyodideREPL` provides an interactive REPL for a Pyodide process:

- Command execution
- Command history
- Socket.IO integration for remote clients
- Output formatting

### PyodideServer

`PyodideServer` provides a web server for managing Pyodide processes and REPLs:

- WebSocket (Socket.IO) communication
- Session management
- HTTP API for process management

### SpacetimeDBIntegration

`SpacetimeDBIntegration` connects PyodideManager with SpacetimeDB:

- Syncing process state with SpacetimeDB
- Handling state changes in SpacetimeDB
- Mapping between processes and SpacetimeDB entities

## How Variable Observation Works

The system uses two mechanisms for observing variable changes in Python:

1. **Python-side Monitoring**: A custom dictionary class wraps the Python globals and notifies JavaScript when variables are set.

2. **JavaScript-side Polling**: For safety, the system also polls for changes to variables at a configurable interval.

When a variable changes, the system:

1. Detects the change via notification or polling
2. Emits a `variableChange` event
3. Updates the variable state in the process
4. Syncs the change to SpacetimeDB (if integrated)

## Usage

### Basic Example

```typescript
import { PyodideManager } from './PyodideManager';

// Create a manager
const manager = new PyodideManager();

// Create a process
const process = manager.createProcess({
  initialCode: `
# Initialize variables
counter = 0
message = "Hello, World!"
`,
  watchedVariables: ['counter', 'message']
});

// Listen for variable changes
process.on('variableChange', (variable, value, oldValue) => {
  console.log(`Variable changed: ${variable} = ${value} (was ${oldValue})`);
});

// Run some code to change variables
process.runCode(`
counter += 1
message = "Updated message"
`);

// Get variable values
const counter = process.getVariable('counter');
console.log(`Counter value: ${counter}`);

// Set variables directly from JavaScript
process.setVariable('message', 'Set from JavaScript');
```

### WebSocket Server Example

```typescript
import { PyodideServer } from './PyodideServer';

// Create and start the server
const server = new PyodideServer({ port: 3200 });
server.start().then(() => {
  console.log('Pyodide server started on port 3200');
});

// The server handles WebSocket connections and session management
// Clients can connect to create/join sessions and interact with Pyodide processes
```

### SpacetimeDB Integration Example

```typescript
import { PyodideManager } from './PyodideManager';
import { SpacetimeDBIntegration } from './SpacetimeDBIntegration';

// Create a manager
const manager = new PyodideManager();

// Create SpacetimeDB integration
const integration = new SpacetimeDBIntegration({
  manager,
  spacetimeClient, // Your initialized SpacetimeDB client
  syncIntervalMs: 1000
});

// Start syncing
integration.startSync();

// Create a process (will be automatically synced with SpacetimeDB)
const process = manager.createProcess({
  watchedVariables: ['left_motor_speed', 'right_motor_speed']
});

// Changes to variables will be synced to SpacetimeDB automatically
process.setVariable('left_motor_speed', 0.5);
```

## Client-Side Integration

To connect to the Pyodide server from a client (e.g., a web browser), use the following:

```javascript
import { io } from 'socket.io-client';

// Connect to the server
const socket = io('http://localhost:3200');

// Create a new session
socket.emit('create_session', {
  initialCode: 'counter = 0',
  watchedVariables: ['counter']
}, (response) => {
  if (response.success) {
    console.log('Session created:', response.session);
    
    // Execute code
    socket.emit('execute_code', {
      code: 'counter += 1'
    }, (response) => {
      console.log('Code executed:', response);
    });
    
    // Listen for variable changes
    socket.on('variable_change', (data) => {
      console.log('Variable changed:', data);
    });
  }
});
```

## Implementation Details

### Python Monitoring

The system injects the following code to monitor Python variables:

```python
class MonitoredDict(dict):
    def __setitem__(self, key, value):
        # Call the original __setitem__
        super().__setitem__(key, value)
        # Notify JavaScript about the change
        self._notify_js(key, value)
    
    def _notify_js(self, key, value):
        # This will be overridden by JavaScript
        pass

# Create a monitored globals dict
_original_globals = globals().copy()
_monitored_globals = MonitoredDict(_original_globals)

# Override run_code to use our monitored globals
def _monitored_run_code(code, globals=None, locals=None):
    if globals is None:
        globals = _monitored_globals
    return _original_run_code(code, globals, locals)
```

This allows JavaScript to receive notifications when Python variables change.

## Best Practices

1. Always specify the variables you want to watch to avoid unnecessary overhead.
   
2. Use `runCode` for executing Python code blocks and updating variables.
   
3. Use event handlers to react to variable changes rather than polling.
   
4. When integrating with SpacetimeDB, ensure your client connection is properly initialized.
   
5. Clean up resources by destroying processes when they're no longer needed.

## Error Handling

The system provides comprehensive error handling:

1. Errors in Python code execution are caught and emitted as events.
   
2. Network disconnections are handled gracefully.
   
3. State synchronization issues are logged and recovered when possible.

## Advanced Configuration

The system supports advanced configuration options:

1. Custom WebSocket transport options
   
2. Variable watching interval configuration
   
3. Custom Python initialization code
   
4. SpacetimeDB synchronization options

## Performance Considerations

1. **Memory Usage**: Each Pyodide instance consumes significant memory (50-100MB). Monitor memory usage when running multiple instances.
   
2. **Watch Interval**: Adjust the watch interval based on your application's needs to balance responsiveness and performance.
   
3. **Variable Size**: Be cautious with large variables (arrays, objects) as they can affect transfer performance.

## Limitations

1. **WebAssembly Support**: Requires a browser with WebAssembly support.
   
2. **Python Libraries**: Only Python libraries compiled for WebAssembly are available.
   
3. **Performance**: Some operations are slower than native Python due to WebAssembly limitations.

## Future Improvements

1. **Improved Reactivity**: Direct variable observation without polling.
   
2. **Performance Optimizations**: Reducing overhead and memory usage.
   
3. **Enhanced SpacetimeDB Integration**: More comprehensive state synchronization.

## Contributing

Contributions are welcome! Please follow the project's coding standards and include tests for new features.

## License

This project is licensed under the terms of the project's main license.
