const { PythonShell } = require('python-shell');
const path = require('path');
const fs = require('fs');
const WebSocket = require('ws');

// Execution modes
const EXECUTION_MODES = {
  SINGLE_PROCESS: 'single_process',
  MULTI_PROCESS: 'multi_process',
  MULTI_THREAD: 'multi_thread',
  PYODIDE: 'pyodide'
};

class ProcessManager {
  constructor(spacetimeClient) {
    this.spacetimeClient = spacetimeClient;
    this.processes = new Map(); // Map of codeId -> { process, status }
    this.micropythonRuntimePath = path.join(__dirname, '../../../micropython-runtime/firmware');
    this.websocketClients = new Map(); // Map of clientId -> websocket
    this.globalState = new Map(); // Map of processId -> global state
  }
  
  /**
   * Initialize websocket server for Python REPL and process state updates
   * @param {object} server - HTTP server instance to attach the WebSocket server to
   */
  initializeWebsocketServer(server) {
    const wss = new WebSocket.Server({ server });
    
    wss.on('connection', (ws) => {
      const clientId = Date.now().toString();
      this.websocketClients.set(clientId, ws);
      
      ws.on('message', async (message) => {
        try {
          const data = JSON.parse(message);
          
          if (data.type === 'repl_command') {
            // Handle REPL command
            if (this.processes.has(data.codeId)) {
              const processStatus = this.processes.get(data.codeId);
              if (processStatus.process) {
                processStatus.process.send(JSON.stringify({
                  type: 'repl_command',
                  command: data.command
                }));
              } else {
                ws.send(JSON.stringify({
                  type: 'repl_response',
                  error: 'Process is not running'
                }));
              }
            } else {
              ws.send(JSON.stringify({
                type: 'repl_response',
                error: `No process found for code ${data.codeId}`
              }));
            }
          }
        } catch (error) {
          console.error('Error processing WebSocket message:', error);
          ws.send(JSON.stringify({
            type: 'error',
            message: error.message
          }));
        }
      });
      
      ws.on('close', () => {
        this.websocketClients.delete(clientId);
      });
      
      // Send the current state of all processes
      const initialState = {};
      for (const [codeId, processStatus] of this.processes) {
        initialState[codeId] = this.getProcessStatus(codeId);
      }
      
      ws.send(JSON.stringify({
        type: 'initial_state',
        state: initialState
      }));
    });
  }
  
  /**
   * Broadcast process status updates to all connected WebSocket clients
   * @param {number} codeId - The ID of the code
   * @param {object} status - The status to broadcast
   */
  broadcastStatus(codeId, status) {
    for (const ws of this.websocketClients.values()) {
      if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({
          type: 'process_status',
          codeId,
          status
        }));
      }
    }
  }
  
  /**
   * Broadcast Python REPL output to all connected WebSocket clients
   * @param {number} codeId - The ID of the code
   * @param {string} output - The output to broadcast
   */
  broadcastReplOutput(codeId, output) {
    for (const ws of this.websocketClients.values()) {
      if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({
          type: 'repl_output',
          codeId,
          output
        }));
      }
    }
  }
  
  /**
   * Broadcast global state updates to all connected WebSocket clients
   * @param {number} codeId - The ID of the code
   * @param {object} state - The global state to broadcast
   */
  broadcastGlobalState(codeId, state) {
    this.globalState.set(codeId, state);
    
    for (const ws of this.websocketClients.values()) {
      if (ws.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({
          type: 'global_state',
          codeId,
          state
        }));
      }
    }
  }
  
  /**
   * Get code content from SpacetimeDB
   * @param {number} codeId - The ID of the code to get
   * @returns {Promise<Object>} - The code object
   */
  async getCodeFromDB(codeId) {
    if (!this.spacetimeClient) {
      throw new Error('Not connected to SpacetimeDB');
    }
    
    const code = this.spacetimeClient.db.microprocessCode.code_id.find(codeId);
    if (!code) {
      throw new Error(`Code with ID ${codeId} not found`);
    }
    
    return {
      codeId: code.codeId,
      name: code.name,
      codeContent: code.codeContent,
      filePath: code.filePath
    };
  }
  
  /**
   * Update the microprocess state in SpacetimeDB
   * @param {number} codeId - The ID of the code
   * @param {number} leftMotorSpeed - The speed of the left motor
   * @param {number} rightMotorSpeed - The speed of the right motor
   * @param {string} errorMessage - Any error message
   * @param {boolean} isRunning - Whether the code is running
   * @param {string} executionMode - The execution mode
   */
  async updateMicroprocessState(codeId, leftMotorSpeed, rightMotorSpeed, errorMessage, isRunning, executionMode) {
    if (!this.spacetimeClient) {
      throw new Error('Not connected to SpacetimeDB');
    }
    
    try {
      await this.spacetimeClient.call('update_microprocess_state', {
        code_id: codeId,
        left_motor_speed: leftMotorSpeed,
        right_motor_speed: rightMotorSpeed,
        error_message: errorMessage,
        is_running: isRunning,
        execution_mode: executionMode
      });
    } catch (error) {
      console.error(`Failed to update microprocess state for code ${codeId}:`, error);
    }
  }
  
  /**
   * Start a Python process for a given code ID with specified execution mode
   * @param {number} codeId - The ID of the code to run
   * @param {string} executionMode - The execution mode to use (single_process, multi_process, multi_thread, pyodide)
   * @returns {Promise<number>} - The process ID
   */
  async startProcess(codeId, executionMode = EXECUTION_MODES.SINGLE_PROCESS) {
    // Check if process is already running
    if (this.processes.has(codeId) && this.processes.get(codeId).process) {
      throw new Error(`Process for code ${codeId} is already running`);
    }
    
    try {
      // Get the code from SpacetimeDB
      const code = await this.getCodeFromDB(codeId);
      
      if (!code) {
        throw new Error(`Code with ID ${codeId} not found`);
      }
      
      // Create a temporary file with the code
      const tempFilePath = path.join(__dirname, `../../scripts/temp/${codeId}.py`);
      fs.mkdirSync(path.dirname(tempFilePath), { recursive: true });
      fs.writeFileSync(tempFilePath, code.codeContent);
      
      // Set up the Python options
      const options = {
        mode: 'text',
        pythonPath: 'python3',
        pythonOptions: ['-u'], // Unbuffered output
        scriptPath: this.micropythonRuntimePath,
        args: [tempFilePath, executionMode]
      };
      
      // For Pyodide execution, we don't start a Python process
      if (executionMode === EXECUTION_MODES.PYODIDE) {
        // For Pyodide, we'll just store the code and let the web client handle execution
        const processStatus = {
          process: null,
          status: 'stopped',
          codeId,
          startTime: Date.now(),
          lastUpdateTime: Date.now(),
          error: null,
          leftMotorSpeed: 0,
          rightMotorSpeed: 0,
          executionMode: EXECUTION_MODES.PYODIDE,
          code: code.codeContent
        };
        
        this.processes.set(codeId, processStatus);
        
        // Update SpacetimeDB with the initial state
        await this.updateMicroprocessState(
          codeId,
          0,
          0,
          '',
          false,
          EXECUTION_MODES.PYODIDE
        );
        
        this.broadcastStatus(codeId, this.getProcessStatus(codeId));
        
        return codeId;
      }
      
      // Create and start the Python process with appropriate runner script
      let runnerScript = 'micropython_runner.py';
      
      if (executionMode === EXECUTION_MODES.MULTI_THREAD) {
        runnerScript = 'thread_runner.py';
      } else if (executionMode === EXECUTION_MODES.MULTI_PROCESS) {
        runnerScript = 'process_runner.py';
      }
      
      const process = new PythonShell(runnerScript, options);
      
      // Set up the process status
      const processStatus = {
        process,
        status: 'running',
        codeId,
        startTime: Date.now(),
        lastUpdateTime: Date.now(),
        error: null,
        leftMotorSpeed: 0,
        rightMotorSpeed: 0,
        executionMode
      };
      
      this.processes.set(codeId, processStatus);
      
      // Handle process output
      process.on('message', (message) => {
        try {
          const data = JSON.parse(message);
          processStatus.lastUpdateTime = Date.now();
          
          if (data.type === 'state_update') {
            // Update motor speeds if included in the message
            if (data.leftMotorSpeed !== undefined) {
              processStatus.leftMotorSpeed = data.leftMotorSpeed;
            }
            
            if (data.rightMotorSpeed !== undefined) {
              processStatus.rightMotorSpeed = data.rightMotorSpeed;
            }
            
            // Update SpacetimeDB with the new state
            this.updateMicroprocessState(
              codeId,
              processStatus.leftMotorSpeed,
              processStatus.rightMotorSpeed,
              processStatus.error || '',
              true,
              executionMode
            );
          } else if (data.type === 'repl_output') {
            // Broadcast REPL output to all connected clients
            this.broadcastReplOutput(codeId, data.output);
          } else if (data.type === 'global_state') {
            // Broadcast global state to all connected clients
            this.broadcastGlobalState(codeId, data.state);
          }
          
          // Broadcast status update
          this.broadcastStatus(codeId, this.getProcessStatus(codeId));
        } catch (error) {
          console.error(`Error processing message from Python process ${codeId}:`, error);
        }
      });
      
      // Handle process errors
      process.on('error', (error) => {
        processStatus.error = error.message;
        processStatus.status = 'error';
        
        // Update SpacetimeDB with the error
        this.updateMicroprocessState(
          codeId,
          processStatus.leftMotorSpeed,
          processStatus.rightMotorSpeed,
          error.message,
          false,
          executionMode
        );
        
        // Broadcast status update
        this.broadcastStatus(codeId, this.getProcessStatus(codeId));
        
        console.error(`Error in Python process ${codeId}:`, error);
      });
      
      // Handle process exit
      process.on('close', (code) => {
        if (code !== 0) {
          processStatus.status = 'error';
          processStatus.error = `Process exited with code ${code}`;
          
          // Update SpacetimeDB with the error
          this.updateMicroprocessState(
            codeId,
            processStatus.leftMotorSpeed,
            processStatus.rightMotorSpeed,
            processStatus.error,
            false,
            executionMode
          );
        } else {
          processStatus.status = 'stopped';
          
          // Update SpacetimeDB with the stopped state
          this.updateMicroprocessState(
            codeId,
            0,
            0,
            '',
            false,
            executionMode
          );
        }
        
        processStatus.process = null;
        
        // Broadcast status update
        this.broadcastStatus(codeId, this.getProcessStatus(codeId));
        
        // Clean up the temporary file
        try {
          fs.unlinkSync(tempFilePath);
        } catch (error) {
          console.error(`Error deleting temporary file ${tempFilePath}:`, error);
        }
      });
      
      return codeId;
    } catch (error) {
      throw new Error(`Failed to start process for code ${codeId}: ${error.message}`);
    }
  }
  
  /**
   * Stop a Python process for a given code ID
   * @param {number} codeId - The ID of the code to stop
   * @returns {Promise<void>}
   */
  async stopProcess(codeId) {
    const processStatus = this.processes.get(codeId);
    
    if (!processStatus) {
      throw new Error(`No process found for code ${codeId}`);
    }
    
    // For Pyodide execution, just update the status
    if (processStatus.executionMode === EXECUTION_MODES.PYODIDE) {
      processStatus.status = 'stopped';
      
      // Update SpacetimeDB with the stopped state
      await this.updateMicroprocessState(
        codeId,
        0,
        0,
        '',
        false,
        EXECUTION_MODES.PYODIDE
      );
      
      // Broadcast status update
      this.broadcastStatus(codeId, this.getProcessStatus(codeId));
      
      return;
    }
    
    if (!processStatus.process) {
      throw new Error(`No running process found for code ${codeId}`);
    }
    
    return new Promise((resolve, reject) => {
      try {
        processStatus.process.end((err) => {
          if (err) {
            reject(new Error(`Failed to stop process for code ${codeId}: ${err.message}`));
          } else {
            processStatus.status = 'stopped';
            processStatus.process = null;
            
            // Update SpacetimeDB with the stopped state
            this.updateMicroprocessState(
              codeId,
              0,
              0,
              '',
              false,
              processStatus.executionMode
            );
            
            // Broadcast status update
            this.broadcastStatus(codeId, this.getProcessStatus(codeId));
            
            resolve();
          }
        });
      } catch (error) {
        reject(new Error(`Failed to stop process for code ${codeId}: ${error.message}`));
      }
    });
  }
  
  /**
   * Stop all running Python processes
   * @returns {Promise<void>}
   */
  async stopAllProcesses() {
    const promises = [];
    
    for (const [codeId, processStatus] of this.processes) {
      if (processStatus.process) {
        promises.push(this.stopProcess(codeId).catch(console.error));
      }
    }
    
    await Promise.all(promises);
  }
  
  /**
   * Get the status of a Python process
   * @param {number} codeId - The ID of the code
   * @returns {Object} - The process status
   */
  getProcessStatus(codeId) {
    const processStatus = this.processes.get(codeId);
    
    if (!processStatus) {
      throw new Error(`No process found for code ${codeId}`);
    }
    
    return {
      codeId,
      status: processStatus.status,
      running: !!processStatus.process,
      startTime: processStatus.startTime,
      lastUpdateTime: processStatus.lastUpdateTime,
      error: processStatus.error,
      leftMotorSpeed: processStatus.leftMotorSpeed,
      rightMotorSpeed: processStatus.rightMotorSpeed,
      executionMode: processStatus.executionMode,
      globalState: this.globalState.get(codeId) || {}
    };
  }
  
  /**
   * Get the global state of a Python process
   * @param {number} codeId - The ID of the code
   * @returns {Object} - The global state
   */
  getProcessGlobalState(codeId) {
    return this.globalState.get(codeId) || {};
  }
  
  /**
   * Get the number of active Python processes
   * @returns {number} - The number of active processes
   */
  getActiveProcessCount() {
    let count = 0;
    
    for (const processStatus of this.processes.values()) {
      if (processStatus.process) {
        count++;
      }
    }
    
    return count;
  }
}

// Export the EXECUTION_MODES constant
ProcessManager.EXECUTION_MODES = EXECUTION_MODES;

module.exports = ProcessManager;
