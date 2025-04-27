import { loadPyodide, PyodideInterface } from 'pyodide';
import * as fs from 'fs';
import * as path from 'path';
import { EventEmitter } from 'events';

/**
 * Options for creating a PyodideProcess
 */
interface PyodideProcessOptions {
  id: string;
  initialCode?: string;
  watchedVariables?: string[];
  onVariableChange?: (variable: string, value: any, oldValue: any) => void;
}

/**
 * Variable state information
 */
interface VariableState {
  name: string;
  value: any;
  type: string;
  lastUpdated: Date;
}

/**
 * Manages a single Pyodide process with REPL capabilities
 * and variable monitoring
 */
export class PyodideProcess extends EventEmitter {
  private pyodide: PyodideInterface | null = null;
  private isReady: boolean = false;
  private variables: Map<string, VariableState> = new Map();
  private watchedVariables: Set<string> = new Set();
  private watchIntervalId: NodeJS.Timeout | null = null;
  private watchIntervalMs: number = 100; // Default watch interval
  
  public readonly id: string;
  
  /**
   * Creates a new PyodideProcess
   */
  constructor(options: PyodideProcessOptions) {
    super();
    this.id = options.id;
    
    // Set up watched variables
    if (options.watchedVariables) {
      options.watchedVariables.forEach(v => this.watchedVariables.add(v));
    }
    
    // Set up variable change callback
    if (options.onVariableChange) {
      this.on('variableChange', options.onVariableChange);
    }
    
    // Initialize Pyodide
    this.initialize(options.initialCode);
  }
  
  /**
   * Initialize Pyodide
   */
  private async initialize(initialCode?: string): Promise<void> {
    try {
      console.log(`Initializing Pyodide process ${this.id}...`);
      this.pyodide = await loadPyodide({
        indexURL: "https://cdn.jsdelivr.net/pyodide/v0.27.5/full/",
      });
      
      // Setup the monitoring system in Python
      await this.setupMonitoring();
      
      // Run initial code if provided
      if (initialCode) {
        await this.runCode(initialCode);
      }
      
      this.isReady = true;
      this.emit('ready');
      
      // Start watching variables
      this.startWatching();
      
      console.log(`Pyodide process ${this.id} initialized successfully`);
    } catch (error) {
      console.error(`Error initializing Pyodide process ${this.id}:`, error);
      this.emit('error', error);
    }
  }
  
  /**
   * Set up the variable monitoring system in Python
   */
  private async setupMonitoring(): Promise<void> {
    if (!this.pyodide) return;
    
    // Create a Python monitoring module that will track variable changes
    await this.pyodide.runPythonAsync(`
import sys
from pyodide.code import run_code

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

# Override the globals() function
def _monitored_globals_fn():
    return _monitored_globals

# Store the original run_code function
_original_run_code = run_code

# Override run_code to use our monitored globals
def _monitored_run_code(code, globals=None, locals=None):
    if globals is None:
        globals = _monitored_globals
    return _original_run_code(code, globals, locals)

# Replace the run_code function
run_code = _monitored_run_code

# Fix the sys module's reference to globals
sys.modules['__main__'].__dict__ = _monitored_globals
    `);
    
    // Connect the Python _notify_js function to our JavaScript handler
    this.pyodide.globals.get('_monitored_globals')._notify_js = (key: string, value: any) => {
      this.handleVariableChange(key, value);
    };
  }
  
  /**
   * Handle a variable change notification from Python
   */
  private handleVariableChange(variable: string, newValue: any): void {
    // Skip if this variable is not being watched
    if (!this.watchedVariables.has(variable)) return;
    
    // Get the current state of the variable (if any)
    const oldState = this.variables.get(variable);
    const oldValue = oldState?.value;
    
    // Get the Python type of the variable
    let type = "unknown";
    if (this.pyodide) {
      try {
        type = this.pyodide.runPython(`type(${variable}).__name__`);
      } catch (error) {
        console.warn(`Error getting type for ${variable}:`, error);
      }
    }
    
    // Update the variable state
    const newState: VariableState = {
      name: variable,
      value: newValue,
      type,
      lastUpdated: new Date()
    };
    this.variables.set(variable, newState);
    
    // Emit the change event
    this.emit('variableChange', variable, newValue, oldValue);
  }
  
  /**
   * Start watching variables
   */
  private startWatching(): void {
    if (this.watchIntervalId) {
      clearInterval(this.watchIntervalId);
    }
    
    this.watchIntervalId = setInterval(() => {
      this.checkWatchedVariables();
    }, this.watchIntervalMs);
  }
  
  /**
   * Stop watching variables
   */
  public stopWatching(): void {
    if (this.watchIntervalId) {
      clearInterval(this.watchIntervalId);
      this.watchIntervalId = null;
    }
  }
  
  /**
   * Check all watched variables for changes
   */
  private async checkWatchedVariables(): Promise<void> {
    if (!this.pyodide || !this.isReady) return;
    
    for (const variable of this.watchedVariables) {
      try {
        // Check if the variable exists in the Python globals
        const exists = this.pyodide.runPython(`'${variable}' in globals()`);
        if (!exists) continue;
        
        // Get the current value
        const value = this.pyodide.globals.get(variable);
        
        // Get the current state
        const currentState = this.variables.get(variable);
        
        // If there's no current state or the value has changed, update it
        if (!currentState || JSON.stringify(value) !== JSON.stringify(currentState.value)) {
          this.handleVariableChange(variable, value);
        }
      } catch (error) {
        console.warn(`Error checking variable ${variable}:`, error);
      }
    }
  }
  
  /**
   * Run Python code in this process
   */
  public async runCode(code: string): Promise<any> {
    if (!this.pyodide || !this.isReady) {
      throw new Error('Pyodide is not ready');
    }
    
    try {
      // Run the code and return the result
      const result = await this.pyodide.runPythonAsync(code);
      
      // Check all watched variables for changes
      await this.checkWatchedVariables();
      
      return result;
    } catch (error) {
      console.error(`Error running code in Pyodide process ${this.id}:`, error);
      this.emit('error', error);
      throw error;
    }
  }
  
  /**
   * Add a variable to watch
   */
  public watchVariable(variable: string): void {
    this.watchedVariables.add(variable);
    
    // Immediately check the variable if Pyodide is ready
    if (this.pyodide && this.isReady) {
      this.checkWatchedVariables();
    }
  }
  
  /**
   * Remove a variable from the watch list
   */
  public unwatchVariable(variable: string): void {
    this.watchedVariables.delete(variable);
  }
  
  /**
   * Get the current value of a variable
   */
  public getVariable(variable: string): any {
    if (!this.pyodide || !this.isReady) {
      throw new Error('Pyodide is not ready');
    }
    
    try {
      return this.pyodide.globals.get(variable);
    } catch (error) {
      console.error(`Error getting variable ${variable}:`, error);
      throw error;
    }
  }
  
  /**
   * Set a variable value
   */
  public setVariable(variable: string, value: any): void {
    if (!this.pyodide || !this.isReady) {
      throw new Error('Pyodide is not ready');
    }
    
    try {
      this.pyodide.globals.set(variable, value);
      
      // Check the variable for changes
      this.checkWatchedVariables();
    } catch (error) {
      console.error(`Error setting variable ${variable}:`, error);
      throw error;
    }
  }
  
  /**
   * Get all variables
   */
  public getAllVariables(): Map<string, VariableState> {
    return new Map(this.variables);
  }
  
  /**
   * Clean up resources
   */
  public destroy(): void {
    this.stopWatching();
    this.removeAllListeners();
    
    // Clear variables
    this.variables.clear();
    this.watchedVariables.clear();
    
    // TODO: Clean up Pyodide resources when API supports it
    this.pyodide = null;
    this.isReady = false;
    
    console.log(`Pyodide process ${this.id} destroyed`);
  }
}
