import { PythonShell } from 'python-shell';
import fs from 'fs';
import path from 'path';
import os from 'os';
import express from 'express';
import http from 'http';
import { Server } from 'socket.io';
import { fileURLToPath } from 'url';

// Configuration
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const TEMP_DIR = path.join(os.tmpdir(), 'solarville-microprocessor');
const PORT = 3100;

// Create temp dir if it doesn't exist
if (!fs.existsSync(TEMP_DIR)) {
  fs.mkdirSync(TEMP_DIR, { recursive: true });
}

// Active REPL sessions
const replSessions = new Map();

/**
 * Initialize REPL server
 */
export function initializeReplServer() {
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
  io.on('connection', (socket) => {
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
    socket.on('execute', (data) => {
      const { code, codeId } = data;
      executeCode(socket, code, codeId);
    });
    
    // Handle motor control requests
    socket.on('set_motors', (data) => {
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
function createReplSession(socket) {
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
  const options = {
    mode: 'text',
    pythonPath: 'python3',
    pythonOptions: ['-u'],  // unbuffered stdout
    scriptPath: TEMP_DIR,
  };

  // Create PythonShell instance
  const pyshell = new PythonShell(tempFile, options);

  // Handle output from Python
  pyshell.on('message', (message) => {
    try {
      const data = JSON.parse(message);
      socket.emit('repl_output', data);
      
      // Handle motor state updates
      if (data.type === 'motor_state') {
        // Store the motor state in the session for future reference
        replSessions.get(socket.id).motorState = {
          left: data.left_motor_speed,
          right: data.right_motor_speed
        };
      }
    } catch (error) {
      socket.emit('repl_output', {
        type: 'output',
        message: message
      });
    }
  });

  // Handle errors
  pyshell.on('error', (error) => {
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
function executeCode(socket, code, codeId = null) {
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
    if (codeId) {
      session.codeId = codeId;
    }
    
  } catch (error) {
    socket.emit('repl_output', {
      type: 'error',
      message: `Failed to execute code: ${error.message}`
    });
  }
}

/**
 * Get all active REPL sessions
 */
export function getActiveSessions() {
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
export function getSession(id) {
  return replSessions.get(id);
}

/**
 * Close a specific session by ID
 */
export function closeSession(id) {
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
export function closeAllSessions() {
  for (const id of replSessions.keys()) {
    closeSession(id);
  }
  return true;
}
