import express from 'express';
import http from 'http';
import { Server as SocketIOServer, Socket } from 'socket.io';
import { PyodideManager } from './PyodideManager';
import { PyodideREPL } from './PyodideREPL';
import { v4 as uuidv4 } from 'uuid';

/**
 * Server configuration options
 */
export interface PyodideServerConfig {
  port?: number;
  corsOrigin?: string | string[];
  debug?: boolean;
}

/**
 * Active session information
 */
export interface ActiveSession {
  id: string;
  replId: string;
  processId: string;
  socketId: string;
  createdAt: Date;
  lastActivity: Date;
}

/**
 * Server for managing Pyodide processes and REPLs over WebSockets
 */
export class PyodideServer {
  private app: express.Application;
  private server: http.Server;
  private io: SocketIOServer;
  private manager: PyodideManager;
  private repls: Map<string, PyodideREPL> = new Map();
  private sessions: Map<string, ActiveSession> = new Map();
  private config: PyodideServerConfig;
  
  /**
   * Create a new PyodideServer
   */
  constructor(config: PyodideServerConfig = {}) {
    this.config = {
      port: config.port || 3200,
      corsOrigin: config.corsOrigin || '*',
      debug: config.debug || false
    };
    
    // Create Express app
    this.app = express();
    this.server = http.createServer(this.app);
    
    // Create Socket.IO server
    this.io = new SocketIOServer(this.server, {
      cors: {
        origin: this.config.corsOrigin,
        methods: ['GET', 'POST']
      }
    });
    
    // Create PyodideManager
    this.manager = new PyodideManager();
    
    // Set up routes and socket handlers
    this.setupRoutes();
    this.setupSocketHandlers();
  }
  
  /**
   * Set up Express routes
   */
  private setupRoutes(): void {
    // Root route for info
    this.app.get('/', (req, res) => {
      res.json({
        name: 'Pyodide Server',
        status: 'running',
        sessions: this.getSessions()
      });
    });
    
    // Get active sessions
    this.app.get('/sessions', (req, res) => {
      res.json(this.getSessions());
    });
  }
  
  /**
   * Set up Socket.IO event handlers
   */
  private setupSocketHandlers(): void {
    this.io.on('connection', (socket: Socket) => {
      this.logDebug(`Client connected: ${socket.id}`);
      
      // Create a new session when requested
      socket.on('create_session', (data: { initialCode?: string, watchedVariables?: string[] }, callback) => {
        try {
          const session = this.createSession(socket, data);
          callback({ success: true, session });
        } catch (error) {
          callback({ success: false, error: String(error) });
        }
      });
      
      // Join an existing session
      socket.on('join_session', (data: { sessionId: string }, callback) => {
        try {
          const session = this.joinSession(socket, data.sessionId);
          callback({ success: true, session });
        } catch (error) {
          callback({ success: false, error: String(error) });
        }
      });
      
      // Handle disconnection
      socket.on('disconnect', () => {
        this.logDebug(`Client disconnected: ${socket.id}`);
        this.handleDisconnect(socket);
      });
    });
    
    // Set up manager event handlers
    this.manager.on('variableChange', (process, variable, value, oldValue) => {
      this.logDebug(`Variable changed in process ${process.id}: ${variable} = ${value}`);
      
      // Find all sessions for this process
      const sessions = Array.from(this.sessions.values())
        .filter(session => session.processId === process.id);
      
      // Notify all connected sockets for these sessions
      for (const session of sessions) {
        const socket = this.io.sockets.sockets.get(session.socketId);
        if (socket) {
          socket.emit('variable_change', {
            variable,
            value,
            oldValue,
            processId: process.id
          });
        }
      }
    });
  }
  
  /**
   * Create a new session for a socket
   */
  private createSession(socket: Socket, options: { initialCode?: string, watchedVariables?: string[] }): ActiveSession {
    // Generate session ID
    const sessionId = uuidv4();
    
    // Create a Pyodide process
    const process = this.manager.createProcess({
      id: `process-${sessionId}`,
      initialCode: options.initialCode,
      watchedVariables: options.watchedVariables
    });
    
    // Create a REPL for the process
    const repl = new PyodideREPL({
      process,
      socket
    });
    
    // Store the REPL
    const replId = `repl-${sessionId}`;
    this.repls.set(replId, repl);
    
    // Create session
    const session: ActiveSession = {
      id: sessionId,
      replId,
      processId: process.id,
      socketId: socket.id,
      createdAt: new Date(),
      lastActivity: new Date()
    };
    
    // Store the session
    this.sessions.set(sessionId, session);
    
    // Set up socket event handlers for the session
    this.setupSessionHandlers(socket, sessionId);
    
    this.logDebug(`Created new session: ${sessionId}`);
    
    return session;
  }
  
  /**
   * Join an existing session
   */
  private joinSession(socket: Socket, sessionId: string): ActiveSession {
    // Check if the session exists
    const session = this.sessions.get(sessionId);
    if (!session) {
      throw new Error(`Session ${sessionId} not found`);
    }
    
    // Get the REPL
    const repl = this.repls.get(session.replId);
    if (!repl) {
      throw new Error(`REPL for session ${sessionId} not found`);
    }
    
    // Update the socket for the REPL
    repl.setSocket(socket);
    
    // Update the session
    session.socketId = socket.id;
    session.lastActivity = new Date();
    
    // Set up socket event handlers for the session
    this.setupSessionHandlers(socket, sessionId);
    
    this.logDebug(`Socket ${socket.id} joined session ${sessionId}`);
    
    return session;
  }
  
  /**
   * Set up socket event handlers for a session
   */
  private setupSessionHandlers(socket: Socket, sessionId: string): void {
    const session = this.sessions.get(sessionId);
    if (!session) return;
    
    const repl = this.repls.get(session.replId);
    if (!repl) return;
    
    // Execute code
    socket.on('execute_code', async (data: { code: string }, callback) => {
      try {
        // Update last activity
        session.lastActivity = new Date();
        
        // Execute the code
        const result = await repl.execute(data.code);
        callback({ success: true, result });
      } catch (error) {
        callback({ success: false, error: String(error) });
      }
    });
    
    // Get variable value
    socket.on('get_variable', (data: { variable: string }, callback) => {
      try {
        // Update last activity
        session.lastActivity = new Date();
        
        // Get the variable
        const value = repl.getProcess().getVariable(data.variable);
        callback({ success: true, variable: data.variable, value });
      } catch (error) {
        callback({ success: false, error: String(error) });
      }
    });
    
    // Set variable value
    socket.on('set_variable', (data: { variable: string, value: any }, callback) => {
      try {
        // Update last activity
        session.lastActivity = new Date();
        
        // Set the variable
        repl.getProcess().setVariable(data.variable, data.value);
        callback({ success: true });
      } catch (error) {
        callback({ success: false, error: String(error) });
      }
    });
    
    // Watch a variable
    socket.on('watch_variable', (data: { variable: string }, callback) => {
      try {
        // Update last activity
        session.lastActivity = new Date();
        
        // Watch the variable
        repl.getProcess().watchVariable(data.variable);
        callback({ success: true });
      } catch (error) {
        callback({ success: false, error: String(error) });
      }
    });
    
    // Unwatch a variable
    socket.on('unwatch_variable', (data: { variable: string }, callback) => {
      try {
        // Update last activity
        session.lastActivity = new Date();
        
        // Unwatch the variable
        repl.getProcess().unwatchVariable(data.variable);
        callback({ success: true });
      } catch (error) {
        callback({ success: false, error: String(error) });
      }
    });
    
    // Close session
    socket.on('close_session', (callback) => {
      try {
        this.closeSession(sessionId);
        callback({ success: true });
      } catch (error) {
        callback({ success: false, error: String(error) });
      }
    });
  }
  
  /**
   * Handle socket disconnection
   */
  private handleDisconnect(socket: Socket): void {
    // Find sessions for this socket
    const sessionsToRemove = Array.from(this.sessions.entries())
      .filter(([_, session]) => session.socketId === socket.id);
    
    // Remove socket from REPLs but don't destroy sessions (allow reconnection)
    for (const [sessionId, session] of sessionsToRemove) {
      const repl = this.repls.get(session.replId);
      if (repl) {
        repl.removeSocket();
      }
      
      this.logDebug(`Socket ${socket.id} disconnected from session ${sessionId}`);
    }
  }
  
  /**
   * Close a session and clean up resources
   */
  public closeSession(sessionId: string): void {
    const session = this.sessions.get(sessionId);
    if (!session) {
      throw new Error(`Session ${sessionId} not found`);
    }
    
    // Remove the REPL
    const repl = this.repls.get(session.replId);
    if (repl) {
      this.repls.delete(session.replId);
    }
    
    // Destroy the process
    this.manager.destroyProcess(session.processId);
    
    // Remove the session
    this.sessions.delete(sessionId);
    
    this.logDebug(`Closed session: ${sessionId}`);
  }
  
  /**
   * Get all active sessions
   */
  public getSessions(): ActiveSession[] {
    return Array.from(this.sessions.values());
  }
  
  /**
   * Start the server
   */
  public start(): Promise<void> {
    return new Promise<void>((resolve) => {
      this.server.listen(this.config.port, () => {
        console.log(`Pyodide server started on port ${this.config.port}`);
        resolve();
      });
    });
  }
  
  /**
   * Stop the server
   */
  public stop(): Promise<void> {
    return new Promise<void>((resolve, reject) => {
      // Clean up all sessions
      for (const sessionId of this.sessions.keys()) {
        try {
          this.closeSession(sessionId);
        } catch (error) {
          console.error(`Error closing session ${sessionId}:`, error);
        }
      }
      
      // Close the server
      this.server.close((error) => {
        if (error) {
          reject(error);
        } else {
          resolve();
        }
      });
    });
  }
  
  /**
   * Log debug messages
   */
  private logDebug(message: string): void {
    if (this.config.debug) {
      console.log(`[PyodideServer] ${message}`);
    }
  }
  
  /**
   * Get the PyodideManager instance
   */
  public getManager(): PyodideManager {
    return this.manager;
  }
  
  /**
   * Get the Express app
   */
  public getApp(): express.Application {
    return this.app;
  }
  
  /**
   * Get the HTTP server
   */
  public getServer(): http.Server {
    return this.server;
  }
  
  /**
   * Get the Socket.IO server
   */
  public getIO(): SocketIOServer {
    return this.io;
  }
}
