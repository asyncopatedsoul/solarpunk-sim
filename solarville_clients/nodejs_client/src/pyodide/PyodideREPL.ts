import { PyodideProcess } from './PyodideProcess';
import { EventEmitter } from 'events';
import { Socket } from 'socket.io';

/**
 * Output message type for REPL
 */
export interface ReplOutput {
  type: 'ready' | 'output' | 'error' | 'variable_change' | 'debug' | 'exit' | 'system';
  message?: string;
  variable?: string;
  value?: any;
  oldValue?: any;
}

/**
 * Options for creating a PyodideREPL
 */
export interface PyodideReplOptions {
  process: PyodideProcess;
  socket?: Socket;
  historySize?: number;
}

/**
 * Manages an interactive REPL for a Pyodide process
 */
export class PyodideREPL extends EventEmitter {
  private process: PyodideProcess;
  private socket: Socket | null = null;
  private history: string[] = [];
  private historyIndex: number = -1;
  private historySize: number;
  
  /**
   * Create a new PyodideREPL
   */
  constructor(options: PyodideReplOptions) {
    super();
    this.process = options.process;
    this.socket = options.socket || null;
    this.historySize = options.historySize || 100;
    
    // Set up event listeners
    this.setupEventListeners();
  }
  
  /**
   * Setup event listeners
   */
  private setupEventListeners(): void {
    // Listen for variable changes
    this.process.on('variableChange', (variable: string, value: any, oldValue: any) => {
      const output: ReplOutput = {
        type: 'variable_change',
        variable,
        value,
        oldValue
      };
      
      // Emit locally
      this.emit('output', output);
      
      // Send to socket if available
      if (this.socket) {
        this.socket.emit('repl_output', output);
      }
    });
    
    // Listen for process ready
    this.process.on('ready', () => {
      const output: ReplOutput = {
        type: 'ready',
        message: `Pyodide process ${this.process.id} is ready`
      };
      
      // Emit locally
      this.emit('output', output);
      
      // Send to socket if available
      if (this.socket) {
        this.socket.emit('repl_output', output);
      }
    });
    
    // Listen for process errors
    this.process.on('error', (error: Error) => {
      const output: ReplOutput = {
        type: 'error',
        message: `Error in Pyodide process: ${error.message}`
      };
      
      // Emit locally
      this.emit('output', output);
      
      // Send to socket if available
      if (this.socket) {
        this.socket.emit('repl_output', output);
      }
    });
    
    // Set up socket event listeners if socket is available
    if (this.socket) {
      this.setupSocketListeners();
    }
  }
  
  /**
   * Set up socket event listeners
   */
  private setupSocketListeners(): void {
    if (!this.socket) return;
    
    // Execute code when 'execute' event is received
    this.socket.on('execute', async (data: { code: string }) => {
      await this.execute(data.code);
    });
    
    // Get variable value when 'get_variable' event is received
    this.socket.on('get_variable', (data: { variable: string }) => {
      try {
        const value = this.process.getVariable(data.variable);
        this.socket?.emit('variable_value', {
          variable: data.variable,
          value
        });
      } catch (error) {
        const output: ReplOutput = {
          type: 'error',
          message: `Error getting variable ${data.variable}: ${error}`
        };
        this.socket?.emit('repl_output', output);
      }
    });
    
    // Set variable value when 'set_variable' event is received
    this.socket.on('set_variable', (data: { variable: string, value: any }) => {
      try {
        this.process.setVariable(data.variable, data.value);
        this.socket?.emit('variable_set', {
          variable: data.variable,
          value: data.value
        });
      } catch (error) {
        const output: ReplOutput = {
          type: 'error',
          message: `Error setting variable ${data.variable}: ${error}`
        };
        this.socket?.emit('repl_output', output);
      }
    });
    
    // Watch variable when 'watch_variable' event is received
    this.socket.on('watch_variable', (data: { variable: string }) => {
      this.process.watchVariable(data.variable);
      this.socket?.emit('variable_watched', {
        variable: data.variable
      });
    });
    
    // Unwatch variable when 'unwatch_variable' event is received
    this.socket.on('unwatch_variable', (data: { variable: string }) => {
      this.process.unwatchVariable(data.variable);
      this.socket?.emit('variable_unwatched', {
        variable: data.variable
      });
    });
    
    // Handle socket disconnection
    this.socket.on('disconnect', () => {
      this.socket = null;
    });
  }
  
  /**
   * Execute code in the REPL
   */
  public async execute(code: string): Promise<any> {
    // Add to history
    this.addToHistory(code);
    
    try {
      // Run the code
      const result = await this.process.runCode(code);
      
      // Create output object
      const output: ReplOutput = {
        type: 'output',
        message: result !== undefined ? String(result) : undefined,
        value: result
      };
      
      // Emit locally
      this.emit('output', output);
      
      // Send to socket if available
      if (this.socket) {
        this.socket.emit('repl_output', output);
      }
      
      return result;
    } catch (error) {
      // Create error output object
      const output: ReplOutput = {
        type: 'error',
        message: `Error: ${error}`
      };
      
      // Emit locally
      this.emit('output', output);
      
      // Send to socket if available
      if (this.socket) {
        this.socket.emit('repl_output', output);
      }
      
      throw error;
    }
  }
  
  /**
   * Add code to history
   */
  private addToHistory(code: string): void {
    // Don't add empty code
    if (!code.trim()) return;
    
    // Don't add duplicate of the last command
    if (this.history.length > 0 && this.history[this.history.length - 1] === code) {
      return;
    }
    
    // Add to history
    this.history.push(code);
    
    // Trim history if it exceeds max size
    if (this.history.length > this.historySize) {
      this.history.shift();
    }
    
    // Reset history index
    this.historyIndex = this.history.length;
  }
  
  /**
   * Get previous command from history
   */
  public getPreviousCommand(): string | null {
    if (this.history.length === 0) {
      return null;
    }
    
    // Decrement history index
    if (this.historyIndex > 0) {
      this.historyIndex--;
    }
    
    return this.history[this.historyIndex];
  }
  
  /**
   * Get next command from history
   */
  public getNextCommand(): string | null {
    if (this.history.length === 0) {
      return null;
    }
    
    // Increment history index
    if (this.historyIndex < this.history.length - 1) {
      this.historyIndex++;
      return this.history[this.historyIndex];
    } else {
      // At the end of history, return empty string
      this.historyIndex = this.history.length;
      return '';
    }
  }
  
  /**
   * Set the socket for this REPL
   */
  public setSocket(socket: Socket): void {
    this.socket = socket;
    this.setupSocketListeners();
  }
  
  /**
   * Remove the socket
   */
  public removeSocket(): void {
    this.socket = null;
  }
  
  /**
   * Get the process for this REPL
   */
  public getProcess(): PyodideProcess {
    return this.process;
  }
}
