import { createClient, Identity } from '@spacetimedb/client';
import { PythonShell } from 'python-shell';
import fs from 'fs';
import path from 'path';
import os from 'os';
import { fileURLToPath } from 'url';
import { initializeReplServer, getActiveSessions, closeSession, setSpacetimeDbClient } from './repl.js';

// Configuration
const SERVER_URL = 'http://127.0.0.1:3000';
const MODULE_NAME = 'solarville_server';
const POLLING_INTERVAL_MS = 1000; // How often to check for new code to run
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// State
const runningProcesses = new Map(); // Map of code_id to { process, tempFile } objects
let spacetimeClient = null;
let replServer = null;

// Setup temp directory for Python scripts
const TEMP_DIR = path.join(os.tmpdir(), 'solarville-microprocessor');
if (!fs.existsSync(TEMP_DIR)) {
  fs.mkdirSync(TEMP_DIR, { recursive: true });
}

/**
 * Initialize the SpacetimeDB client connection
 */
async function initializeSpacetimeClient() {
  console.log('Connecting to SpacetimeDB...');
  try {
    spacetimeClient = await createClient({
      uri: SERVER_URL,
      moduleName: MODULE_NAME,
      onConnect: handleConnect,
      onDisconnect: handleDisconnect,
      onReducerError: (event) => {
        console.error('Reducer error:', event);
      },
    });
    
    // Subscribe to updates
    await spacetimeClient.subscribe();
  } catch (error) {
    console.error('Failed to connect to SpacetimeDB:', error);
  }
}

/**
 * Handle successful connection to SpacetimeDB
 */
function handleConnect(identity) {
  console.log('Connected to SpacetimeDB!');
  console.log('Identity:', identity);

  // Set the SpacetimeDB client for the REPL server
  setSpacetimeDbClient(spacetimeClient);

  // Set up event listeners for our tables
  spacetimeClient.on('microprocess_code/insert', handleCodeInsert);
  spacetimeClient.on('microprocess_code/update', handleCodeUpdate);
  spacetimeClient.on('microprocess_code/delete', handleCodeDelete);
  spacetimeClient.on('microprocess_state/update', handleStateUpdate);

  // Start polling for inactive processes that should be running
  startPolling();
}

/**
 * Handle disconnection from SpacetimeDB
 */
function handleDisconnect(reason) {
  console.log('Disconnected from SpacetimeDB:', reason);
  
  // Clean up all running processes
  stopAllPythonProcesses();
  
  // Try to reconnect after a delay
  setTimeout(initializeSpacetimeClient, 5000);
}

/**
 * Handle insertion of new microprocessor code
 */
function handleCodeInsert(code) {
  console.log(`New code inserted: ${code.name} (ID: ${code.code_id})`);
  
  // Check if this code is already supposed to be running
  const state = spacetimeClient.tables.microprocess_state.findBy('code_id', code.code_id);
  if (state && state.is_running) {
    startPythonProcess(code);
  }
}

/**
 * Handle updates to existing microprocessor code
 */
function handleCodeUpdate(oldCode, newCode) {
  console.log(`Code updated: ${newCode.name} (ID: ${newCode.code_id})`);
  
  // If this code is running, restart the process
  if (runningProcesses.has(newCode.code_id)) {
    stopPythonProcess(newCode.code_id);
    
    // Check if it should be restarted
    const state = spacetimeClient.tables.microprocess_state.findBy('code_id', newCode.code_id);
    if (state && state.is_running) {
      startPythonProcess(newCode);
    }
  }
}

/**
 * Handle deletion of microprocessor code
 */
function handleCodeDelete(code) {
  console.log(`Code deleted: ${code.name} (ID: ${code.code_id})`);
  stopPythonProcess(code.code_id);
}

/**
 * Handle updates to microprocessor state
 */
function handleStateUpdate(oldState, newState) {
  console.log(`State updated for code_id: ${newState.code_id}, Running: ${newState.is_running}`);
  
  // Find the corresponding code
  const code = spacetimeClient.tables.microprocess_code.findBy('code_id', newState.code_id);
  if (!code) {
    console.error(`Cannot find code for state with code_id: ${newState.code_id}`);
    return;
  }
  
  if (newState.is_running) {
    // Start the process if it's not already running
    if (!runningProcesses.has(newState.code_id)) {
      startPythonProcess(code);
    }
  } else {
    // Stop the process if it's running
    if (runningProcesses.has(newState.code_id)) {
      stopPythonProcess(newState.code_id);
    }
  }
}

/**
 * Start a Python process for the given code
 */
function startPythonProcess(code) {
  // If already running, don't start again
  if (runningProcesses.has(code.code_id)) {
    console.log(`Process for code_id ${code.code_id} is already running`);
    return;
  }
  
  console.log(`Starting Python process for code_id: ${code.code_id}`);
  
  try {
    // Create a temporary file for the Python code
    const tempFilePath = path.join(TEMP_DIR, `${code.code_id}_${Date.now()}.py`);
    fs.writeFileSync(tempFilePath, code.code_content);
    
    // Prepare a wrapper script that calls the user's code and reports motor states
    const wrapperCode = `
import sys
import json
import time
import traceback

# Add the directory of the script to Python's path so it can be imported
sys.path.append('${TEMP_DIR}')

# Define motor control interface
left_motor_speed = 0.0
right_motor_speed = 0.0

# Simulate motor hardware access functions
def set_left_motor(speed):
    global left_motor_speed
    left_motor_speed = max(min(speed, 1.0), -1.0)
    print_motor_state()

def set_right_motor(speed):
    global right_motor_speed
    right_motor_speed = max(min(speed, 1.0), -1.0)
    print_motor_state()

def set_motors(left, right):
    global left_motor_speed, right_motor_speed
    left_motor_speed = max(min(left, 1.0), -1.0)
    right_motor_speed = max(min(right, 1.0), -1.0)
    print_motor_state()

def print_motor_state():
    # Output the motor state as JSON for easy parsing by Node.js
    state = {
        "left_motor_speed": left_motor_speed,
        "right_motor_speed": right_motor_speed
    }
    print(json.dumps(state), flush=True)

# Load the user's code
try:
    user_code = open('${tempFilePath}', 'r').read()
    exec(user_code)
    
    # Check for required functions
    if 'setup' in globals():
        setup()
    
    # Main loop if it exists
    if 'loop' in globals():
        while True:
            try:
                loop()
            except Exception as e:
                error_state = {
                    "left_motor_speed": 0.0,
                    "right_motor_speed": 0.0,
                    "error": str(e)
                }
                print(json.dumps(error_state), flush=True)
                traceback.print_exc()
                time.sleep(1)  # Wait before retrying
            
            time.sleep(0.1)  # Run loop at 10Hz
    else:
        # If no loop function, just keep the process alive
        print("No loop function found, motors will remain at last set values")
        while True:
            print_motor_state()
            time.sleep(1)
            
except Exception as e:
    error_state = {
        "left_motor_speed": 0.0,
        "right_motor_speed": 0.0,
        "error": str(e)
    }
    print(json.dumps(error_state), flush=True)
    traceback.print_exc()
    sys.exit(1)
`;
    
    const wrapperFilePath = path.join(TEMP_DIR, `wrapper_${code.code_id}_${Date.now()}.py`);
    fs.writeFileSync(wrapperFilePath, wrapperCode);
    
    // Start the Python process
    const pyshell = new PythonShell(wrapperFilePath);
    
    // Update state in SpacetimeDB when we get output
    pyshell.on('message', (message) => {
      try {
        const state = JSON.parse(message);
        console.log(`Motor state update: Left: ${state.left_motor_speed}, Right: ${state.right_motor_speed}`);
        
        // Update state in SpacetimeDB
        spacetimeClient.reducers.update_microprocess_state(
          code.code_id,
          state.left_motor_speed || 0.0,
          state.right_motor_speed || 0.0,
          state.error || "",
          true
        );
      } catch (error) {
        console.error('Error parsing Python output:', error, message);
      }
    });
    
    // Handle errors
    pyshell.on('error', (error) => {
      console.error(`Error in Python process for code_id ${code.code_id}:`, error);
      
      // Update state in SpacetimeDB
      spacetimeClient.reducers.update_microprocess_state(
        code.code_id,
        0.0,
        0.0,
        `Error: ${error.message}`,
        false
      );
      
      // Clean up
      if (runningProcesses.has(code.code_id)) {
        const { tempFiles } = runningProcesses.get(code.code_id);
        runningProcesses.delete(code.code_id);
        
        // Clean up temporary files
        for (const file of tempFiles) {
          if (fs.existsSync(file)) {
            fs.unlinkSync(file);
          }
        }
      }
    });
    
    // Handle process termination
    pyshell.on('close', () => {
      console.log(`Python process for code_id ${code.code_id} closed`);
      
      // Update state in SpacetimeDB
      spacetimeClient.reducers.update_microprocess_state(
        code.code_id,
        0.0,
        0.0,
        "Process terminated",
        false
      );
      
      // Clean up
      if (runningProcesses.has(code.code_id)) {
        const { tempFiles } = runningProcesses.get(code.code_id);
        runningProcesses.delete(code.code_id);
        
        // Clean up temporary files
        for (const file of tempFiles) {
          if (fs.existsSync(file)) {
            fs.unlinkSync(file);
          }
        }
      }
    });
    
    // Store the process
    runningProcesses.set(code.code_id, { 
      process: pyshell, 
      tempFiles: [tempFilePath, wrapperFilePath] 
    });
    
    console.log(`Python process started for code_id: ${code.code_id}`);
    
  } catch (error) {
    console.error(`Error starting Python process for code_id ${code.code_id}:`, error);
    
    // Update state in SpacetimeDB
    spacetimeClient.reducers.update_microprocess_state(
      code.code_id,
      0.0,
      0.0,
      `Error starting process: ${error.message}`,
      false
    );
  }
}

/**
 * Stop a Python process by code_id
 */
function stopPythonProcess(codeId) {
  if (!runningProcesses.has(codeId)) {
    console.log(`No running process found for code_id: ${codeId}`);
    return;
  }
  
  console.log(`Stopping Python process for code_id: ${codeId}`);
  
  try {
    const { process, tempFiles } = runningProcesses.get(codeId);
    
    // Kill the process
    process.kill();
    
    // Clean up temporary files
    for (const file of tempFiles) {
      if (fs.existsSync(file)) {
        fs.unlinkSync(file);
      }
    }
    
    // Remove from running processes
    runningProcesses.delete(codeId);
    
    console.log(`Python process stopped for code_id: ${codeId}`);
    
  } catch (error) {
    console.error(`Error stopping Python process for code_id ${codeId}:`, error);
  }
}

/**
 * Stop all running Python processes
 */
function stopAllPythonProcesses() {
  console.log('Stopping all Python processes...');
  
  for (const codeId of runningProcesses.keys()) {
    stopPythonProcess(codeId);
  }
  
  console.log('All Python processes stopped');
}

/**
 * Start polling for microprocessor processes that should be running
 */
function startPolling() {
  console.log('Starting polling for microprocessor processes...');
  
  // Set up interval to check for processes that should be running
  setInterval(() => {
    try {
      // Get all running states
      const runningStates = spacetimeClient.tables.microprocess_state
        .filter(state => state.is_running);
        
      // Check each running state to make sure the process is running
      for (const state of runningStates) {
        if (!runningProcesses.has(state.code_id)) {
          // Process should be running but isn't
          const code = spacetimeClient.tables.microprocess_code.findBy('code_id', state.code_id);
          if (code) {
            console.log(`Starting missing process for code_id: ${state.code_id}`);
            startPythonProcess(code);
          } else {
            console.error(`Cannot find code for running state with code_id: ${state.code_id}`);
            
            // Update state to not running
            spacetimeClient.reducers.update_microprocess_state(
              state.code_id,
              0.0,
              0.0,
              "Error: Code not found",
              false
            );
          }
        }
      }
      
      // Check for processes that should not be running
      for (const codeId of runningProcesses.keys()) {
        const state = spacetimeClient.tables.microprocess_state.findBy('code_id', codeId);
        if (!state || !state.is_running) {
          // Process is running but shouldn't be
          console.log(`Stopping process that should not be running for code_id: ${codeId}`);
          stopPythonProcess(codeId);
        }
      }
    } catch (error) {
      console.error('Error in polling:', error);
    }
  }, POLLING_INTERVAL_MS);
}

/**
 * Save a Python file to SpacetimeDB
 */
async function savePythonFile(name, filePath, content) {
  if (!spacetimeClient) {
    console.error('Cannot save Python file: Not connected to SpacetimeDB');
    return null;
  }
  
  try {
    const codeId = await spacetimeClient.reducers.save_microprocess_code(
      name,
      filePath,
      content
    );
    
    console.log(`Saved Python file with code_id: ${codeId}`);
    return codeId;
  } catch (error) {
    console.error('Error saving Python file:', error);
    return null;
  }
}

/**
 * Start a Python file's execution
 */
async function startPythonFile(codeId) {
  if (!spacetimeClient) {
    console.error('Cannot start Python file: Not connected to SpacetimeDB');
    return false;
  }
  
  try {
    await spacetimeClient.reducers.start_microprocess(codeId);
    console.log(`Started Python file with code_id: ${codeId}`);
    return true;
  } catch (error) {
    console.error('Error starting Python file:', error);
    return false;
  }
}

/**
 * Stop a Python file's execution
 */
async function stopPythonFile(codeId) {
  if (!spacetimeClient) {
    console.error('Cannot stop Python file: Not connected to SpacetimeDB');
    return false;
  }
  
  try {
    await spacetimeClient.reducers.stop_microprocess(codeId);
    console.log(`Stopped Python file with code_id: ${codeId}`);
    return true;
  } catch (error) {
    console.error('Error stopping Python file:', error);
    return false;
  }
}

// Main function to initialize the client
async function main() {
  console.log('Starting Solarville Microprocessor Client...');

  // Start REPL server
  replServer = initializeReplServer();
  console.log('REPL server started');

  // Connect to SpacetimeDB
  await initializeSpacetimeClient();

  // Handle process exit
  process.on('SIGINT', async () => {
    console.log('Shutting down...');
    stopAllPythonProcesses();
    process.exit(0);
  });

  // Example usage from command line args
  if (process.argv.length > 2) {
    const command = process.argv[2];

    if (command === 'save' && process.argv.length > 5) {
      const name = process.argv[3];
      const filePath = process.argv[4];
      const content = fs.readFileSync(process.argv[5], 'utf8');

      const codeId = await savePythonFile(name, filePath, content);
      if (codeId) {
        console.log(`Saved file with code_id: ${codeId}`);
      }
    } else if (command === 'start' && process.argv.length > 3) {
      const codeId = parseInt(process.argv[3]);
      await startPythonFile(codeId);
    } else if (command === 'stop' && process.argv.length > 3) {
      const codeId = parseInt(process.argv[3]);
      await stopPythonFile(codeId);
    } else if (command === 'repl-status') {
      // Show status of REPL sessions
      console.log('Active REPL Sessions:');
      console.log(getActiveSessions());
    } else if (command === 'repl-close' && process.argv.length > 3) {
      // Close a specific REPL session
      const sessionId = process.argv[3];
      if (closeSession(sessionId)) {
        console.log(`Closed REPL session: ${sessionId}`);
      } else {
        console.log(`Session not found: ${sessionId}`);
      }
    } else {
      console.log('Usage:');
      console.log('  save <name> <path> <file> - Save a Python file to SpacetimeDB');
      console.log('  start <code_id>          - Start a Python file');
      console.log('  stop <code_id>           - Stop a Python file');
      console.log('  repl-status              - Show active REPL sessions');
      console.log('  repl-close <session_id>  - Close a specific REPL session');
    }
  }
}

// Start the client
main().catch(console.error);

// Export API for other modules
export {
  savePythonFile,
  startPythonFile,
  stopPythonFile,
  getActiveSessions,
  closeSession
};