import { PyodideManager } from './PyodideManager';
import { PyodideProcess } from './PyodideProcess';

/**
 * Integration options for SpacetimeDB
 */
interface SpacetimeDBIntegrationOptions {
  manager: PyodideManager;
  spacetimeClient: any; // DbConnection type from SpacetimeDB client
  syncIntervalMs?: number;
}

/**
 * Integrates PyodideManager with SpacetimeDB
 */
export class SpacetimeDBIntegration {
  private manager: PyodideManager;
  private spacetimeClient: any;
  private syncIntervalId: NodeJS.Timeout | null = null;
  private syncIntervalMs: number;
  private processToCodeIdMap: Map<string, number> = new Map();
  private codeIdToProcessMap: Map<number, string> = new Map();
  
  /**
   * Create a new SpacetimeDBIntegration
   */
  constructor(options: SpacetimeDBIntegrationOptions) {
    this.manager = options.manager;
    this.spacetimeClient = options.spacetimeClient;
    this.syncIntervalMs = options.syncIntervalMs || 1000;
    
    // Set up event listeners
    this.setupEventListeners();
  }
  
  /**
   * Set up event listeners
   */
  private setupEventListeners(): void {
    // Listen for process ready events
    this.manager.on('processReady', (process: PyodideProcess) => {
      // Try to find a matching code in SpacetimeDB
      this.findOrCreateCodeForProcess(process);
    });
    
    // Listen for process destroyed events
    this.manager.on('processDestroyed', (processId: string) => {
      // Update SpacetimeDB state if needed
      this.handleProcessDestroyed(processId);
    });
    
    // Listen for variable change events
    this.manager.on('variableChange', (process: PyodideProcess, variable: string, value: any) => {
      // Sync variable changes to SpacetimeDB
      this.syncVariableToSpacetimeDB(process, variable, value);
    });
    
    // Listen for SpacetimeDB state changes
    // This would depend on the specific SpacetimeDB client implementation
    if (this.spacetimeClient.on) {
      this.spacetimeClient.on('microprocess_state/update', (oldState: any, newState: any) => {
        this.handleSpacetimeDBStateUpdate(oldState, newState);
      });
      
      this.spacetimeClient.on('microprocess_code/update', (oldCode: any, newCode: any) => {
        this.handleSpacetimeDBCodeUpdate(oldCode, newCode);
      });
    }
  }
  
  /**
   * Start syncing with SpacetimeDB
   */
  public startSync(): void {
    if (this.syncIntervalId !== null) {
      return;
    }
    
    this.syncIntervalId = setInterval(() => {
      this.syncWithSpacetimeDB();
    }, this.syncIntervalMs);
    
    console.log(`SpacetimeDB integration started syncing every ${this.syncIntervalMs}ms`);
  }
  
  /**
   * Stop syncing with SpacetimeDB
   */
  public stopSync(): void {
    if (this.syncIntervalId !== null) {
      clearInterval(this.syncIntervalId);
      this.syncIntervalId = null;
      
      console.log('SpacetimeDB integration stopped syncing');
    }
  }
  
  /**
   * Sync with SpacetimeDB
   */
  private syncWithSpacetimeDB(): void {
    try {
      // Get all processes
      const processes = this.manager.getAllProcesses();
      
      // Sync each process
      for (const [processId, process] of processes) {
        // Get the code ID for this process
        const codeId = this.processToCodeIdMap.get(processId);
        if (codeId === undefined) {
          // Try to find or create code for this process
          this.findOrCreateCodeForProcess(process);
          continue;
        }
        
        // Sync the process state
        this.syncProcessStateToSpacetimeDB(process, codeId);
      }
      
      // Check for new codes in SpacetimeDB
      this.checkForNewCodesInSpacetimeDB();
    } catch (error) {
      console.error('Error syncing with SpacetimeDB:', error);
    }
  }
  
  /**
   * Find or create code for a process in SpacetimeDB
   */
  private async findOrCreateCodeForProcess(process: PyodideProcess): Promise<number | null> {
    try {
      // Check if we already have a code ID for this process
      const existingCodeId = this.processToCodeIdMap.get(process.id);
      if (existingCodeId !== undefined) {
        return existingCodeId;
      }
      
      // Check if there's a matching code name in SpacetimeDB
      // This would depend on the specific SpacetimeDB client implementation
      let code = null;
      if (this.spacetimeClient.db && this.spacetimeClient.db.microprocessCode) {
        // Look for a code with a similar name
        code = this.spacetimeClient.db.microprocessCode.findBy('name', `Pyodide-${process.id}`);
      }
      
      if (code) {
        // Map the process to this code
        this.processToCodeIdMap.set(process.id, code.codeId);
        this.codeIdToProcessMap.set(code.codeId, process.id);
        
        console.log(`Mapped process ${process.id} to existing SpacetimeDB code ${code.codeId}`);
        return code.codeId;
      }
      
      // Create a new code in SpacetimeDB
      return this.createCodeInSpacetimeDB(process);
    } catch (error) {
      console.error(`Error finding/creating code for process ${process.id}:`, error);
      return null;
    }
  }
  
  /**
   * Create a new code in SpacetimeDB
   */
  private async createCodeInSpacetimeDB(process: PyodideProcess): Promise<number | null> {
    try {
      // Generate a name for the code
      const name = `Pyodide-${process.id}`;
      const filePath = `${name}.py`;
      
      // Generate a placeholder content
      const content = `# Pyodide process ${process.id}
# Created: ${new Date().toISOString()}

# This file represents a Pyodide process running in the browser.
# Variables will be synchronized with the Pyodide process.

def setup():
    # Setup code will be executed when the process starts
    print("Setting up Pyodide process ${process.id}")
    
def loop():
    # Loop code will be executed periodically
    pass
`;

      // Create the code in SpacetimeDB
      // This would depend on the specific SpacetimeDB client implementation
      if (!this.spacetimeClient.reducers || !this.spacetimeClient.reducers.addMicroprocessCode) {
        console.error('SpacetimeDB client does not support addMicroprocessCode');
        return null;
      }
      
      const codeId = await this.spacetimeClient.reducers.addMicroprocessCode(
        name,
        filePath,
        content
      );
      
      if (!codeId) {
        console.error('Failed to create microprocess code in SpacetimeDB');
        return null;
      }
      
      // Map the process to this code
      this.processToCodeIdMap.set(process.id, codeId);
      this.codeIdToProcessMap.set(codeId, process.id);
      
      console.log(`Created SpacetimeDB code ${codeId} for process ${process.id}`);
      
      // Create an initial state for this code
      await this.spacetimeClient.reducers.updateMicroprocessState(
        codeId,
        0.0, // left motor speed
        0.0, // right motor speed
        "", // error message
        true // is running
      );
      
      return codeId;
    } catch (error) {
      console.error(`Error creating code for process ${process.id} in SpacetimeDB:`, error);
      return null;
    }
  }
  
  /**
   * Sync a process state to SpacetimeDB
   */
  private async syncProcessStateToSpacetimeDB(process: PyodideProcess, codeId: number): Promise<void> {
    try {
      // Get variables we want to sync
      const leftMotorSpeed = this.getVariableOrDefault(process, 'left_motor_speed', 0.0);
      const rightMotorSpeed = this.getVariableOrDefault(process, 'right_motor_speed', 0.0);
      const errorMessage = this.getVariableOrDefault(process, 'error_message', '');
      
      // Update the state in SpacetimeDB
      await this.spacetimeClient.reducers.updateMicroprocessState(
        codeId,
        leftMotorSpeed,
        rightMotorSpeed,
        errorMessage,
        true // is running
      );
    } catch (error) {
      console.error(`Error syncing process ${process.id} state to SpacetimeDB:`, error);
    }
  }
  
  /**
   * Sync a variable to SpacetimeDB
   */
  private async syncVariableToSpacetimeDB(process: PyodideProcess, variable: string, value: any): Promise<void> {
    try {
      // Get the code ID for this process
      const codeId = this.processToCodeIdMap.get(process.id);
      if (codeId === undefined) {
        // Try to find or create code for this process
        await this.findOrCreateCodeForProcess(process);
        return;
      }
      
      // Handle special variables
      if (variable === 'left_motor_speed' || variable === 'right_motor_speed' || variable === 'error_message') {
        // Sync the process state
        await this.syncProcessStateToSpacetimeDB(process, codeId);
      } else {
        // For other variables, we might want to sync them to a custom state table
        // This would depend on the specific SpacetimeDB implementation
      }
    } catch (error) {
      console.error(`Error syncing variable ${variable} for process ${process.id} to SpacetimeDB:`, error);
    }
  }
  
  /**
   * Handle a process being destroyed
   */
  private async handleProcessDestroyed(processId: string): Promise<void> {
    try {
      // Get the code ID for this process
      const codeId = this.processToCodeIdMap.get(processId);
      if (codeId === undefined) {
        return;
      }
      
      // Update the state in SpacetimeDB to mark it as not running
      await this.spacetimeClient.reducers.updateMicroprocessState(
        codeId,
        0.0, // left motor speed
        0.0, // right motor speed
        "Process terminated", // error message
        false // is running
      );
      
      // Remove the mappings
      this.processToCodeIdMap.delete(processId);
      this.codeIdToProcessMap.delete(codeId);
      
      console.log(`Marked SpacetimeDB code ${codeId} as not running for destroyed process ${processId}`);
    } catch (error) {
      console.error(`Error handling destroyed process ${processId} in SpacetimeDB:`, error);
    }
  }
  
  /**
   * Handle a SpacetimeDB state update
   */
  private handleSpacetimeDBStateUpdate(oldState: any, newState: any): void {
    try {
      // Get the process ID for this code
      const processId = this.codeIdToProcessMap.get(newState.codeId);
      if (processId === undefined) {
        return;
      }
      
      // Get the process
      const process = this.manager.getProcess(processId);
      if (!process) {
        return;
      }
      
      // Check if the running state changed
      if (oldState && oldState.isRunning !== newState.isRunning) {
        if (newState.isRunning) {
          // Start the process
          console.log(`SpacetimeDB requested to start process ${processId}`);
          // The process is already running, so we don't need to do anything
        } else {
          // Stop the process
          console.log(`SpacetimeDB requested to stop process ${processId}`);
          // We might want to pause the process or destroy it
          // this.manager.destroyProcess(processId);
        }
      }
      
      // Check if motor speeds changed
      if (oldState && (oldState.leftMotorSpeed !== newState.leftMotorSpeed || oldState.rightMotorSpeed !== newState.rightMotorSpeed)) {
        // Update the variables in the process
        process.setVariable('left_motor_speed', newState.leftMotorSpeed);
        process.setVariable('right_motor_speed', newState.rightMotorSpeed);
        
        console.log(`Updated motor speeds for process ${processId} from SpacetimeDB`);
      }
    } catch (error) {
      console.error('Error handling SpacetimeDB state update:', error);
    }
  }
  
  /**
   * Handle a SpacetimeDB code update
   */
  private handleSpacetimeDBCodeUpdate(oldCode: any, newCode: any): void {
    try {
      // Get the process ID for this code
      const processId = this.codeIdToProcessMap.get(newCode.codeId);
      if (processId === undefined) {
        return;
      }
      
      // Get the process
      const process = this.manager.getProcess(processId);
      if (!process) {
        return;
      }
      
      // Check if the code content changed
      if (oldCode && oldCode.codeContent !== newCode.codeContent) {
        // Update the code in the process
        process.runCode(newCode.codeContent);
        
        console.log(`Updated code for process ${processId} from SpacetimeDB`);
      }
    } catch (error) {
      console.error('Error handling SpacetimeDB code update:', error);
    }
  }
  
  /**
   * Check for new codes in SpacetimeDB
   */
  private checkForNewCodesInSpacetimeDB(): void {
    try {
      // Get all running states from SpacetimeDB
      // This would depend on the specific SpacetimeDB client implementation
      if (!this.spacetimeClient.db || !this.spacetimeClient.db.microprocessState) {
        return;
      }
      
      // Filter states that are running and not mapped to a process
      const runningStates: any[] = [];
      for (const state of this.spacetimeClient.db.microprocessState.iter()) {
        if (state.isRunning && !this.codeIdToProcessMap.has(state.codeId)) {
          runningStates.push(state);
        }
      }
      
      // For each running state, create a new process
      for (const state of runningStates) {
        this.createProcessForCode(state.codeId);
      }
    } catch (error) {
      console.error('Error checking for new codes in SpacetimeDB:', error);
    }
  }
  
  /**
   * Create a process for a code in SpacetimeDB
   */
  private async createProcessForCode(codeId: number): Promise<PyodideProcess | null> {
    try {
      // Get the code from SpacetimeDB
      if (!this.spacetimeClient.db || !this.spacetimeClient.db.microprocessCode) {
        return null;
      }
      
      const code = this.spacetimeClient.db.microprocessCode.findBy('codeId', codeId);
      if (!code) {
        console.error(`Code ${codeId} not found in SpacetimeDB`);
        return null;
      }
      
      // Create a new process
      const process = this.manager.createProcess({
        id: `process-${codeId}`,
        initialCode: code.codeContent,
        watchedVariables: ['left_motor_speed', 'right_motor_speed', 'error_message']
      });
      
      // Map the process to this code
      this.processToCodeIdMap.set(process.id, codeId);
      this.codeIdToProcessMap.set(codeId, process.id);
      
      console.log(`Created process ${process.id} for SpacetimeDB code ${codeId}`);
      
      return process;
    } catch (error) {
      console.error(`Error creating process for code ${codeId} from SpacetimeDB:`, error);
      return null;
    }
  }
  
  /**
   * Get a variable value from a process, or return a default value if not found
   */
  private getVariableOrDefault(process: PyodideProcess, variable: string, defaultValue: any): any {
    try {
      return process.getVariable(variable);
    } catch (error) {
      return defaultValue;
    }
  }
}
