import { PythonShell } from 'python-shell';
import fs from 'fs';
import path from 'path';
import os from 'os';
import express from 'express';
import http from 'http';
import { Server, Socket } from 'socket.io';
import { fileURLToPath } from 'url';

// Configuration
// const __filename = fileURLToPath(import.meta.url);
// const __dirname = path.dirname(__filename);
const TEMP_DIR = path.join(os.tmpdir(), 'solarville-microprocessor');
const PORT = 3100;

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
}

interface OutputMessage {
  type: 'ready' | 'output' | 'error' | 'motor_state' | 'debug' | 'exit';
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
}

// Active REPL sessions
const replSessions = new Map<string, ReplSession>();

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

    // Handle client disconnect
    socket.on('disconnect', () => {
      console.log('Client disconnected:', socket.id);
      const session = replSessions.get(socket.id);
      if (session) {
        if (session.pyshell) {
          session.pyshell.kill();
        }
        if (session.tempFile && fs.existsSync(session.tempFile)) {
          fs.unlinkSync(session.tempFile);
        }
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
        const command = `set_motors(${left}, ${right})`;
        session.pyshell.send(command);
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

  // Options for PythonShell
  let options = {
    // mode: "text",
    pythonPath: 'python3',
    pythonOptions: ['-u'],  // unbuffered stdout
    scriptPath: TEMP_DIR,
  };

  // Create PythonShell instance
  const pyshell = new PythonShell(tempFile, options);

  // Handle output from Python
  pyshell.on('message', (message: string) => {
    try {
      const data = JSON.parse(message) as OutputMessage;
      socket.emit('repl_output', data);

      // Handle motor state updates
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
        }
      }
    } catch (error) {
      socket.emit('repl_output', {
        type: 'output',
        message: message
      });
    }
  });

  // Handle errors
  pyshell.on('error', (error: Error) => {
    socket.emit('repl_output', {
      type: 'error',
      message: `Python error: ${error.message}`
    });
  });

  // Handle process exit
  pyshell.on('close', () => {
    socket.emit('repl_output', {
      type: 'exit',
      message: 'Python process exited'
    });

    // Clean up
    if (fs.existsSync(tempFile)) {
      fs.unlinkSync(tempFile);
    }

    // Remove session
    replSessions.delete(socket.id);
  });

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
    codeId: session.codeId
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