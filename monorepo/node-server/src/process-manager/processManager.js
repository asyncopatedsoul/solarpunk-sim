const { PythonShell } = require('python-shell');
const path = require('path');
const fs = require('fs');

class ProcessManager {
  constructor(spacetimeClient) {
    this.spacetimeClient = spacetimeClient;
    this.processes = new Map(); // Map of codeId -> { process, status }
    this.micropythonRuntimePath = path.join(__dirname, '../../../micropython-runtime/firmware');
  }
  
  /**
   * Start a Python process for a given code ID
   * @param {number} codeId - The ID of the code to run
   * @returns {Promise<number>} - The process ID
   */
  async startProcess(codeId) {
    // Check if process is already running
    if (this.processes.has(codeId) && this.processes.get(codeId).process) {
      throw new Error(`Process for code ${codeId} is already running`);
    }
    
    try {
      // Get the code from SpacetimeDB
      const code = await this.spacetimeClient.getCode(codeId);
      
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
        args: [tempFilePath]
      };
      
      // Create and start the Python process
      const process = new PythonShell('micropython_runner.py', options);
      
      // Set up the process status
      const processStatus = {
        process,
        status: 'running',
        codeId,
        startTime: Date.now(),
        lastUpdateTime: Date.now(),
        error: null,
        leftMotorSpeed: 0,
        rightMotorSpeed: 0
      };
      
      this.processes.set(codeId, processStatus);
      
      // Handle process output
      process.on('message', (message) => {
        try {
          const data = JSON.parse(message);
          processStatus.lastUpdateTime = Date.now();
          
          // Update motor speeds if included in the message
          if (data.leftMotorSpeed !== undefined) {
            processStatus.leftMotorSpeed = data.leftMotorSpeed;
          }
          
          if (data.rightMotorSpeed !== undefined) {
            processStatus.rightMotorSpeed = data.rightMotorSpeed;
          }
          
          // Update SpacetimeDB with the new state
          this.spacetimeClient.updateMicroprocessState(
            codeId,
            processStatus.leftMotorSpeed,
            processStatus.rightMotorSpeed,
            processStatus.error || '',
            true
          );
        } catch (error) {
          console.error(`Error processing message from Python process ${codeId}:`, error);
        }
      });
      
      // Handle process errors
      process.on('error', (error) => {
        processStatus.error = error.message;
        processStatus.status = 'error';
        
        // Update SpacetimeDB with the error
        this.spacetimeClient.updateMicroprocessState(
          codeId,
          processStatus.leftMotorSpeed,
          processStatus.rightMotorSpeed,
          error.message,
          false
        );
        
        console.error(`Error in Python process ${codeId}:`, error);
      });
      
      // Handle process exit
      process.on('close', (code) => {
        if (code !== 0) {
          processStatus.status = 'error';
          processStatus.error = `Process exited with code ${code}`;
          
          // Update SpacetimeDB with the error
          this.spacetimeClient.updateMicroprocessState(
            codeId,
            processStatus.leftMotorSpeed,
            processStatus.rightMotorSpeed,
            processStatus.error,
            false
          );
        } else {
          processStatus.status = 'stopped';
          
          // Update SpacetimeDB with the stopped state
          this.spacetimeClient.updateMicroprocessState(
            codeId,
            0,
            0,
            '',
            false
          );
        }
        
        processStatus.process = null;
        
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
    
    if (!processStatus || !processStatus.process) {
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
            this.spacetimeClient.updateMicroprocessState(
              codeId,
              0,
              0,
              '',
              false
            );
            
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
      rightMotorSpeed: processStatus.rightMotorSpeed
    };
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

module.exports = ProcessManager;
