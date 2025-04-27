import { PyodideProcess } from './PyodideProcess';
import { EventEmitter } from 'events';
import { v4 as uuidv4 } from 'uuid';

/**
 * Options for creating a new Pyodide process
 */
export interface CreateProcessOptions {
  id?: string;
  initialCode?: string;
  watchedVariables?: string[];
  onVariableChange?: (process: PyodideProcess, variable: string, value: any, oldValue: any) => void;
}

/**
 * Manages multiple Pyodide processes
 */
export class PyodideManager extends EventEmitter {
  private processes: Map<string, PyodideProcess> = new Map();
  
  constructor() {
    super();
  }
  
  /**
   * Create a new Pyodide process
   */
  public createProcess(options: CreateProcessOptions = {}): PyodideProcess {
    // Generate a unique ID if not provided
    const id = options.id || uuidv4();
    
    // Check if a process with the given ID already exists
    if (this.processes.has(id)) {
      throw new Error(`A process with ID ${id} already exists`);
    }
    
    // Create the onVariableChange handler that emits from the manager
    const onVariableChange = (variable: string, value: any, oldValue: any) => {
      const process = this.processes.get(id);
      if (process) {
        // Emit from this specific process
        if (options.onVariableChange) {
          options.onVariableChange(process, variable, value, oldValue);
        }
        
        // Emit from the manager
        this.emit('variableChange', process, variable, value, oldValue);
      }
    };
    
    // Create the process
    const process = new PyodideProcess({
      id,
      initialCode: options.initialCode,
      watchedVariables: options.watchedVariables,
      onVariableChange
    });
    
    // Store the process
    this.processes.set(id, process);
    
    // Set up event forwarding
    process.on('ready', () => {
      this.emit('processReady', process);
    });
    
    process.on('error', (error) => {
      this.emit('processError', process, error);
    });
    
    // Return the process
    return process;
  }
  
  /**
   * Get a process by ID
   */
  public getProcess(id: string): PyodideProcess | undefined {
    return this.processes.get(id);
  }
  
  /**
   * Get all processes
   */
  public getAllProcesses(): Map<string, PyodideProcess> {
    return new Map(this.processes);
  }
  
  /**
   * Destroy a process by ID
   */
  public destroyProcess(id: string): boolean {
    const process = this.processes.get(id);
    if (!process) {
      return false;
    }
    
    // Destroy the process
    process.destroy();
    
    // Remove from the map
    this.processes.delete(id);
    
    // Emit the event
    this.emit('processDestroyed', id);
    
    return true;
  }
  
  /**
   * Destroy all processes
   */
  public destroyAllProcesses(): void {
    // Get all process IDs
    const ids = Array.from(this.processes.keys());
    
    // Destroy each process
    for (const id of ids) {
      this.destroyProcess(id);
    }
  }
  
  /**
   * Add a variable to watch for a specific process
   */
  public watchVariable(processId: string, variable: string): boolean {
    const process = this.processes.get(processId);
    if (!process) {
      return false;
    }
    
    process.watchVariable(variable);
    return true;
  }
  
  /**
   * Add a variable to watch for all processes
   */
  public watchVariableAll(variable: string): void {
    for (const process of this.processes.values()) {
      process.watchVariable(variable);
    }
  }
  
  /**
   * Remove a variable from the watch list for a specific process
   */
  public unwatchVariable(processId: string, variable: string): boolean {
    const process = this.processes.get(processId);
    if (!process) {
      return false;
    }
    
    process.unwatchVariable(variable);
    return true;
  }
  
  /**
   * Remove a variable from the watch list for all processes
   */
  public unwatchVariableAll(variable: string): void {
    for (const process of this.processes.values()) {
      process.unwatchVariable(variable);
    }
  }
  
  /**
   * Get a variable value from a specific process
   */
  public getVariable(processId: string, variable: string): any {
    const process = this.processes.get(processId);
    if (!process) {
      throw new Error(`Process ${processId} not found`);
    }
    
    return process.getVariable(variable);
  }
  
  /**
   * Set a variable value in a specific process
   */
  public setVariable(processId: string, variable: string, value: any): void {
    const process = this.processes.get(processId);
    if (!process) {
      throw new Error(`Process ${processId} not found`);
    }
    
    process.setVariable(variable, value);
  }
  
  /**
   * Run code in a specific process
   */
  public async runCode(processId: string, code: string): Promise<any> {
    const process = this.processes.get(processId);
    if (!process) {
      throw new Error(`Process ${processId} not found`);
    }
    
    return process.runCode(code);
  }
}
