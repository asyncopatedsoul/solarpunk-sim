# Design Solution: Inspecting Python Variables in Pyodide Processes

## Problem Statement

How can we inspect and react to changes in Python variables in player-defined Python scripts executing in interactive Pyodide processes managed by a NodeJS server, while still allowing the player to interact with a REPL for each process?

## Solution Overview

The solution employs a multi-layered approach that combines:

1. **Reactive variable monitoring** using both Python-side notifications and JavaScript-side polling
2. **WebSocket-based REPL interface** for interactive access to Pyodide processes
3. **Process and session management** for multiple concurrent users
4. **SpacetimeDB integration** for state persistence and synchronization

## Detailed Design

### 1. Variable Monitoring System

The core of the solution is a dual-approach variable monitoring system:

#### Python-Side Monitoring

We use a custom Python dictionary class (`MonitoredDict`) that wraps the global namespace. This class overrides the `__setitem__` method to notify JavaScript whenever a variable is set:

```python
class MonitoredDict(dict):
    def __setitem__(self, key, value):
        # Call the original __setitem__
        super().__setitem__(key, value)
        # Notify JavaScript about the change
        self._notify_js(key, value)
```

The `_notify_js` function is implemented on the JavaScript side and connected to the Python instance, creating a direct communication channel for variable changes.

#### JavaScript-Side Polling

As a safety net, the system also implements polling-based monitoring that checks specified variables at a configurable interval. This ensures that variables are always in sync, even if the Python-side notification mechanism fails for any reason.

### 2. Process Management Architecture

The solution uses a tiered architecture:

1. **PyodideProcess**: Manages a single Pyodide instance, including variable monitoring, code execution, and event emission.

2. **PyodideManager**: Coordinates multiple Pyodide processes, handling creation, destruction, and event forwarding.

3. **PyodideREPL**: Provides an interactive REPL interface for a Pyodide process, with command history and output formatting.

4. **PyodideServer**: Exposes process management and REPL functionality over WebSockets, allowing multiple clients to connect.

5. **SpacetimeDBIntegration**: Synchronizes Pyodide process state with SpacetimeDB for persistence and cross-client synchronization.

### 3. REPL Interaction Flow

The REPL interaction follows this pattern:

1. Client connects to the PyodideServer via WebSocket (Socket.IO)
2. Client creates a new session or joins an existing one
3. Server creates/retrieves a Pyodide process and REPL
4. Client sends code to execute via the WebSocket
5. Server executes the code in the Pyodide process
6. Results are sent back to the client
7. Variable changes are automatically monitored and pushed to the client

This flow maintains full REPL interaction while providing real-time variable monitoring.

### 4. Variable Synchronization with SpacetimeDB

The SpacetimeDBIntegration component provides:

1. **Bidirectional Synchronization**: Changes in Pyodide processes are reflected in SpacetimeDB and vice versa.

2. **Mapping System**: Maps between Pyodide processes and SpacetimeDB entities (code and state).

3. **State Management**: Manages the lifecycle of processes and their corresponding SpacetimeDB entities.

### 5. WebSocket Server Implementation

The WebSocket server provides:

1. **Session Management**: Creating, joining, and destroying sessions.

2. **REPL Access**: Executing code, getting/setting variables, and receiving results.

3. **Event Broadcasting**: Pushing variable changes and other events to connected clients.

4. **Resource Cleanup**: Handling disconnections and ensuring proper resource cleanup.

## Key Advantages

1. **Reactive Monitoring**: Real-time notification of variable changes without excessive polling.

2. **Multiple Process Support**: Manage many Pyodide processes concurrently.

3. **Interactive REPL**: Full REPL capabilities preserved for each process.

4. **Persistent State**: SpacetimeDB integration ensures state is preserved and shareable.

5. **Clean Architecture**: Separation of concerns makes the system maintainable and extensible.

## Implementation Challenges and Solutions

### Challenge 1: Monitoring Python Variables Reactively

**Solution**: Combined approach using both Python-side notifications (overriding dictionary methods) and JavaScript-side polling (checking variables periodically).

### Challenge 2: Maintaining REPL Interaction While Monitoring

**Solution**: Separated REPL functionality (PyodideREPL) from process management (PyodideProcess), allowing both to operate independently.

### Challenge 3: Managing Multiple Processes and Sessions

**Solution**: Created manager classes (PyodideManager, PyodideServer) to coordinate processes and sessions.

### Challenge 4: Integrating with SpacetimeDB

**Solution**: Implemented dedicated integration layer (SpacetimeDBIntegration) to handle mapping and synchronization.

### Challenge 5: Performance and Resource Management

**Solution**: Implemented efficient polling intervals, resource cleanup, and optimized data transfer between components.

## Future Enhancements

1. **Direct Observable Variables**: Implement a more efficient variable observation mechanism without polling.

2. **Enhanced Type Support**: Better handling of complex Python types in JavaScript.

3. **Process Isolation**: Improved isolation between Pyodide processes for security.

4. **Performance Optimizations**: Reduce memory usage and improve execution speed.

5. **Extended SpacetimeDB Integration**: More comprehensive state synchronization with SpacetimeDB.

## Conclusion

This solution provides a comprehensive approach to inspecting and reacting to Python variables in Pyodide processes while maintaining REPL functionality. The architecture is modular, scalable, and integrates well with the existing SpacetimeDB ecosystem.

By combining reactive monitoring, WebSocket-based REPL, and SpacetimeDB integration, the system delivers a seamless experience for both developers and end-users interacting with Python code in the browser.
