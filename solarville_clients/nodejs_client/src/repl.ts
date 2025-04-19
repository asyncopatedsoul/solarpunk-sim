import { PythonShell } from 'python-shell';
import fs from 'fs';
import path from 'path';
import os from 'os';
import express from 'express';
import http from 'http';
import { Server, Socket } from 'socket.io';
import { fileURLToPath } from 'url';
import { v4 as uuidv4 } from 'uuid'; // Make sure to install this via npm

// Configuration
// const __filename = fileURLToPath(import.meta.url);
// const __dirname = path.dirname(__filename);
// const TEMP_DIR = path.join(os.tmpdir(), 'solarville-microprocessor');
const TEMP_DIR = path.join(__dirname, 'temp');
const PORT = 3100;

// Reference to SpacetimeDB client (will be set by index.mjs)
let spacetimeDbClient = null;

// Create temp dir if it doesn't exist
if (!fs.existsSync(TEMP_DIR)) {
  fs.mkdirSync(TEMP_DIR, { recursive: true });
}

// Define interfaces
interface MotorState {
  left: number;
  right: number;
}

interface ReplSession {
  pyshell: PythonShell;
  tempFile: string;
  sessionId: string;
  createdAt: number;
  motorState: MotorState;
  codeId?: number;
  stateId?: number; // SpacetimeDB state ID
}

interface OutputMessage {
  type: 'ready' | 'output' | 'error' | 'motor_state' | 'debug' | 'exit' | 'system';
  message?: string;
  left_motor_speed?: number;
  right_motor_speed?: number;
}

interface ExecuteData {
  code: string;
  codeId?: number;
}

interface MotorControlData {
  left: number;
  right: number;
  codeId?: number;
}

interface ActiveSession {
  id: string;
  createdAt: number;
  motorState: MotorState;
  codeId?: number;
  stateId?: number;
}

// Active REPL sessions
const replSessions = new Map<string, ReplSession>();

/**
 * Set the SpacetimeDB client reference
 */
export function setSpacetimeDbClient(client: any): void {
  spacetimeDbClient = client;
  console.log('SpacetimeDB client set in REPL server');
}

/**
 * Create a new microprocess state for a REPL session in SpacetimeDB
 */
async function createMicroprocessState(sessionId: string): Promise<number | null> {
  if (!spacetimeDbClient) {
    console.error('Cannot create microprocess state: SpacetimeDB client not set');
    return null;
  }

  try {
    // Generate a temporary name for the code
    const tempName = `REPL_Session_${sessionId.substring(0, 8)}`;
    const tempFileName = `repl_${sessionId}.py`;
    
    // Create a placeholder Python code for the remote control
    const tempCodeContent = `
# Auto-generated REPL session for remote control interface
# SessionID: ${sessionId}
# Created: ${new Date().toISOString()}

def set_motors(left, right):
    # This function is implemented by the REPL server
    # and will control the motors of the simulated car
    pass
`;

    // Save the code to SpacetimeDB 
    const codeId = await spacetimeDbClient.reducers.add_microprocess_code(
      tempName,
      tempFileName,
      tempCodeContent
    );

    if (!codeId) {
      console.error('Failed to create microprocess code');
      return null;
    }

    console.log(`Created microprocess code with ID: ${codeId}`);

    // Create an initial state for this code
    await spacetimeDbClient.reducers.update_microprocess_state(
      codeId,
      0.0, // left motor speed
      0.0, // right motor speed
      "", // error message
      true // is running
    );

    // Get the state ID
    const state = spacetimeDbClient.tables.microprocess_state.findBy('code_id', codeId);
    if (!state) {
      console.error('Failed to find created microprocess state');
      return null;
    }

    console.log(`Created microprocess state with ID: ${state.state_id}`);
    
    // Store the code ID in the session
    const session = replSessions.get(sessionId);
    if (session) {
      session.codeId = codeId;
      session.stateId = state.state_id;
    }

    return state.state_id;
  } catch (error) {
    console.error('Error creating microprocess state:', error);
    return null;
  }
}

/**
 * Delete a microprocess state from SpacetimeDB
 */
async function deleteMicroprocessState(session: ReplSession): Promise<boolean> {
  if (!spacetimeDbClient || !session.codeId) {
    console.error('Cannot delete microprocess state: SpacetimeDB client not set or no code ID');
    return false;
  }

  try {
    // First update the state to not running
    await spacetimeDbClient.reducers.update_microprocess_state(
      session.codeId,
      0.0, // left motor speed
      0.0, // right motor speed
      "Session closed", // error message
      false // is running
    );

    // We could add a reducer to delete the state completely, but for now
    // we'll just mark it as not running to keep the history

    console.log(`Marked microprocess state for code ID ${session.codeId} as not running`);
    return true;
  } catch (error) {
    console.error('Error deleting microprocess state:', error);
    return false;
  }
}

/**
 * Initialize REPL server
 */
export function initializeReplServer(): { server: http.Server, io: Server } {
  const app = express();
  const server = http.createServer(app);
  const io = new Server(server, {
    cors: {
      origin: "*",
      methods: ["GET", "POST"]
    }
  });

  // Serve simple info page
  app.get('/', (req, res) => {
    res.send('Solarville Python REPL Server is running');
  });

  // Socket.io connection handler
  io.on('connection', (socket: Socket) => {
    console.log('Client connected:', socket.id);

    // Create a new REPL session for this client
    const session = createReplSession(socket);
    replSessions.set(socket.id, session);
    
    // Create a new microprocess state in SpacetimeDB
    if (spacetimeDbClient) {
      createMicroprocessState(socket.id)
        .then(stateId => {
          if (stateId) {
            console.log(`Created SpacetimeDB state with ID ${stateId} for socket ${socket.id}`);
            // Send confirmation to client
            socket.emit('repl_output', {
              type: 'system',
              message: `Connected to SpacetimeDB with state ID ${stateId}`
            });
          } else {
            console.error(`Failed to create SpacetimeDB state for socket ${socket.id}`);
          }
        })
        .catch(error => {
          console.error(`Error creating SpacetimeDB state for socket ${socket.id}:`, error);
        });
    } else {
      console.warn('SpacetimeDB client not set, skipping state creation');
    }

    // Handle client disconnect
    socket.on('disconnect', () => {
      console.log('Client disconnected:', socket.id);
      const session = replSessions.get(socket.id);
      if (session) {
        // Delete the microprocess state in SpacetimeDB
        if (spacetimeDbClient && session.codeId) {
          deleteMicroprocessState(session)
            .then(success => {
              if (success) {
                console.log(`Successfully marked SpacetimeDB state for code ID ${session.codeId} as not running`);
              } else {
                console.error(`Failed to update SpacetimeDB state for code ID ${session.codeId}`);
              }
            })
            .catch(error => {
              console.error(`Error updating SpacetimeDB state:`, error);
            });
        }

        // Clean up Python process
        if (session.pyshell) {
          session.pyshell.kill();
        }
        
        // Clean up temp file
        if (session.tempFile && fs.existsSync(session.tempFile)) {
          fs.unlinkSync(session.tempFile);
        }
        
        // Remove from session map
        replSessions.delete(socket.id);
      }
    });

    // Handle code execution requests
    socket.on('execute', (data: ExecuteData) => {
      const { code, codeId } = data;
      executeCode(socket, code, codeId);
    });

    // Handle motor control requests
    socket.on('set_motors', (data: MotorControlData) => {
      const { left, right, codeId } = data;
      const session = replSessions.get(socket.id);
      if (session && session.pyshell) {
        // Update the motor state in the session
        session.motorState = { left, right };
        
        // Send the command to the Python process
        const command = `set_motors(${left}, ${right})`;
        session.pyshell.send(command);
        
        // Update the state in SpacetimeDB
        if (spacetimeDbClient && session.codeId) {
          spacetimeDbClient.reducers.update_microprocess_state(
            session.codeId,
            left,
            right,
            "", // No error
            true // Still running
          ).catch(error => {
            console.error(`Error updating motor state in SpacetimeDB for code ID ${session.codeId}:`, error);
          });
        }
      }
    });
  });

  // Start server
  server.listen(PORT, () => {
    console.log(`REPL server running on http://localhost:${PORT}`);
  });

  return { server, io };
}

/**
 * Create a new Python REPL session
 */
function createReplSession(socket: Socket): ReplSession {
  const sessionId = socket.id;

  const tempFile = path.join(TEMP_DIR, `repl_${sessionId}_${Date.now()}.py`);
  console.log(`Creating REPL session for ${sessionId} at ${tempFile}`);
  // Create initial REPL environment file
  const replSetupCode = `
import sys
import json
import time
import traceback

# State variables
left_motor_speed = 0.0
right_motor_speed = 0.0
debugging_mode = False

# Helper functions
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
    state = {
        "type": "motor_state",
        "left_motor_speed": left_motor_speed,
        "right_motor_speed": right_motor_speed
    }
    print(json.dumps(state), flush=True)

def debug(*args):
    if debugging_mode:
        print(json.dumps({
            "type": "debug",
            "message": " ".join(str(arg) for arg in args)
        }), flush=True)

print(json.dumps({"type": "ready", "message": "Python REPL ready!"}), flush=True)
`;

  fs.writeFileSync(tempFile, replSetupCode);
  console.log(tempFile);

  // Options for PythonShell
  let options = {
    // mode: "text",
    pythonPath: 'python3',
    pythonOptions: [
      // '-u', // unbuffered stdout,
      '-i' // interactive mode
    ],
    // scriptPath: TEMP_DIR,
  };

  PythonShell.defaultOptions = {
    mode: "text",
  }
  // Create PythonShell instance
  const pyshell = new PythonShell(tempFile, options);

  // Handle output from Python
  pyshell.on('message', (message: string) => {
    console.log(`Received message from Python: ${message}`);

    // Check for special firmware message format from the remote control car
    if (message.startsWith("MOTOR_STATE:")) {
      try {
        const values = message.split(":")[1].split(",");
        const leftSpeed = parseFloat(values[0]);
        const rightSpeed = parseFloat(values[1]);
        
        // Create a properly formatted message for the client
        const motorStateMsg: OutputMessage = {
          type: 'motor_state',
          left_motor_speed: leftSpeed,
          right_motor_speed: rightSpeed
        };
        socket.emit('repl_output', motorStateMsg);
        
        // Update session state
        const session = replSessions.get(socket.id);
        if (session) {
          session.motorState = {
            left: leftSpeed,
            right: rightSpeed
          };
          
          // Update SpacetimeDB state
          if (spacetimeDbClient && session.codeId) {
            spacetimeDbClient.reducers.update_microprocess_state(
              session.codeId,
              leftSpeed,
              rightSpeed,
              "", // No error
              true // Still running
            ).catch(error => {
              console.error(`Error updating motor state from firmware in SpacetimeDB for code ID ${session.codeId}:`, error);
            });
          }
        }
        
        return; // Skip the regular JSON parsing below
      } catch (error) {
        console.error('Error parsing MOTOR_STATE message:', error, message);
      }
    }

    // Regular JSON message handling
    try {
      const data = JSON.parse(message) as OutputMessage;
      socket.emit('repl_output', data);

      // Handle motor state updates from JSON messages
      if (data.type === 'motor_state' &&
          typeof data.left_motor_speed === 'number' &&
          typeof data.right_motor_speed === 'number') {
        // Store the motor state in the session for future reference
        const session = replSessions.get(socket.id);
        if (session) {
          session.motorState = {
            left: data.left_motor_speed,
            right: data.right_motor_speed
          };
          
          // Update the microprocess state in SpacetimeDB if we have a codeId
          if (spacetimeDbClient && session.codeId) {
            spacetimeDbClient.reducers.update_microprocess_state(
              session.codeId,
              data.left_motor_speed,
              data.right_motor_speed,
              "", // No error
              true // Still running
            ).catch(error => {
              console.error(`Error updating motor state in SpacetimeDB for code ID ${session.codeId}:`, error);
            });
          }
        }
      }
    } catch (error) {
      console.error('Error parsing message from Python:', error);

      // If not JSON, send it as regular output
      socket.emit('repl_output', {
        type: 'output',
        message: message
      });
    }
  });

  // Handle errors
  pyshell.on('error', (error: Error) => {
    console.error('Python error:', error);
    socket.emit('repl_output', {
      type: 'error',
      message: `Python error: ${error.message}`
    });
  });

  // Handle process exit
  pyshell.on('close', () => {
    console.log(`Python process for session ${sessionId} exited`);
    socket.emit('repl_output', {
      type: 'exit',
      message: 'Python process exited'
    });

    // Clean up
    if (fs.existsSync(tempFile)) {
      // fs.unlinkSync(tempFile);
    }

    // Remove session
    // replSessions.delete(socket.id);
  });

  pyshell.send('debug()');

  return {
    pyshell,
    tempFile,
    sessionId,
    createdAt: Date.now(),
    motorState: {
      left: 0,
      right: 0
    }
  };
}

/**
 * Execute Python code in the REPL
 */
function executeCode(socket: Socket, code: string, codeId: number | null = null): void {
  console.log(`Executing code: ${code} with ID: ${codeId} for session ${socket.id}`);
  
  const session = replSessions.get(socket.id);
  if (!session || !session.pyshell) {
    socket.emit('repl_output', {
      type: 'error',
      message: 'No active Python session'
    });
    return;
  }

  try {
    // Send code to Python process
    session.pyshell.send(code);

    // Store code ID if provided (for linking with SpacetimeDB)
    if (codeId !== null) {
      session.codeId = codeId;
    }

  } catch (error) {
    const errorMessage = error instanceof Error ? error.message : String(error);
    socket.emit('repl_output', {
      type: 'error',
      message: `Failed to execute code: ${errorMessage}`
    });
  }
}

/**
 * Get all active REPL sessions
 */
export function getActiveSessions(): ActiveSession[] {
  return Array.from(replSessions.entries()).map(([id, session]) => ({
    id,
    createdAt: session.createdAt,
    motorState: session.motorState,
    codeId: session.codeId,
    stateId: session.stateId
  }));
}

/**
 * Get a specific session by ID
 */
export function getSession(id: string): ReplSession | undefined {
  return replSessions.get(id);
}

/**
 * Close a specific session by ID
 */
export function closeSession(id: string): boolean {
  const session = replSessions.get(id);
  if (session) {
    if (session.pyshell) {
      session.pyshell.kill();
    }
    if (session.tempFile && fs.existsSync(session.tempFile)) {
      fs.unlinkSync(session.tempFile);
    }
    replSessions.delete(id);
    return true;
  }
  return false;
}

/**
 * Close all sessions
 */
export function closeAllSessions(): boolean {
  for (const id of replSessions.keys()) {
    closeSession(id);
  }
  return true;
}