import express, { Request, Response, RequestHandler } from 'express';
import { Server, Socket } from 'socket.io';

import { PythonShell } from 'python-shell';
import path from 'path';
import http from 'http';
import cors from 'cors';
import { Identity } from '@clockworklabs/spacetimedb-sdk';
import { 
  DbConnection, 
  ErrorContext, 
  EventContext, 
  Player, 
  MicroprocessCode, 
  MicroprocessState,
  ReducerEventContext
} from './module_bindings';

// Import ProcessManager
import ProcessManager from './process-manager/processManager';

const app = express();
const port = process.env.PORT || 3001;

// Create HTTP server
const server = http.createServer(app);
const router = express.Router();

// Configure SpacetimeDB connection
const SERVER_URL = 'ws://127.0.0.1:3000';
const MODULE_NAME = 'solarville';

// Initialize the process manager (SpacetimeClient will be set later)
const processManager = new ProcessManager(null);

// Express middleware
app.use(express.json());
app.use(cors()); // Enable CORS for all routes

// Initialize WebSocket server
processManager.initializeWebsocketServer(server);

// API route types
interface StatusResponse {
  status: string;
  activeProcesses: number;
  connectedToSpacetime: boolean;
}

interface ProcessStartRequest {
  codeId: number;
  executionMode?: string;
}

interface ProcessStartResponse {
  processId: number;
  status: string;
  executionMode?: string;
}

interface ProcessStopRequest {
  codeId: number;
}

interface ProcessStopResponse {
  status: string;
}

interface ErrorResponse {
  error: string;
}

interface ExecutionModesResponse {
  modes: string[];
}

// API routes
app.get('/api/status', (_req, res) => {
  res.json({
    status: 'ok',
    activeProcesses: processManager.getActiveProcessCount(),
    connectedToSpacetime: processManager.spacetimeClient ? true : false
  });
});

// Start a Python process with specified execution mode
app.post('/api/process/start', (async (req, res) => {
  const { codeId, executionMode } = req.body as ProcessStartRequest;
  
  if (!codeId) {
    return res.status(400).json({ error: 'Missing required parameter: codeId' });
  }
  
  try {
    const processId = await processManager.startProcess(codeId, executionMode);
    res.json({ processId, status: 'started', executionMode });
  } catch (error) {
    res.status(500).json({ error: error instanceof Error ? error.message : String(error) });
  }
}) as RequestHandler);

// Stop a Python process
app.post('/api/process/stop', (async (req, res) => {
  const { codeId } = req.body as ProcessStopRequest;
  
  if (!codeId) {
    return res.status(400).json({ error: 'Missing required parameter: codeId' });
  }
  
  try {
    await processManager.stopProcess(codeId);
    res.json({ status: 'stopped' });
  } catch (error) {
    res.status(500).json({ error: error instanceof Error ? error.message : String(error) });
  }
}) as RequestHandler);

// Get process status
app.get('/api/process/:codeId', (req, res) => {
  const { codeId } = req.params;
  
  try {
    const status = processManager.getProcessStatus(parseInt(codeId));
    res.json(status);
  } catch (error) {
    res.status(404).json({ error: 'Process not found' });
  }
});

// Get all available execution modes
app.get('/api/execution-modes', (_req, res) => {
  res.json({
    modes: Object.values(ProcessManager.EXECUTION_MODES)
  });
});

// Get global state for a process
app.get('/api/process/:codeId/state', (req, res) => {
  const { codeId } = req.params;
  
  try {
    const state = processManager.getProcessGlobalState(parseInt(codeId));
    res.json(state);
  } catch (error) {
    res.status(404).json({ error: 'Process not found or no state available' });
  }
});

/**
 * Handle successful connection to SpacetimeDB
 */
function handleConnect(conn: DbConnection, identity: Identity, token: string): void {
  console.log('Connected to SpacetimeDB!');
  console.log('Identity:', identity.toHexString());

  // Set up subscriptions for all tables
  const queries: string[] = [
    'SELECT * FROM microprocess_code',
    'SELECT * FROM microprocess_state',
    // 'SELECT * FROM robot',
    // 'SELECT * FROM game_object',
    // 'SELECT * FROM player',
    // 'SELECT * FROM scenario'
  ];

  subscribeToQueries(conn, queries);
  
  // Update the SpacetimeClient in the ProcessManager
  processManager.spacetimeClient = conn;
  
  // We need to register for updates to the MicroprocessState table
  // and start/stop processes as needed, but TypeScript doesn't recognize
  // the API correctly, so we'll add a listener to monitor the state
  // in the processManager itself
  // conn.on(ReducerEvent.COMMITTED, (event: MicroprocessStateEvent) => {
  //   if (event.reducerName === 'update_microprocess_state') {
  //     const args = event.args;
  //     if (args.code_id && args.is_running !== undefined) {
  //       // Handle microprocess state update
  //       const codeId = args.code_id;
  //       const isRunning = args.is_running;
  //       const executionMode = args.execution_mode || ProcessManager.EXECUTION_MODES.SINGLE_PROCESS;
        
  //       // Start or stop process based on is_running state
  //       if (isRunning) {
  //         processManager.startProcess(codeId, executionMode).catch(console.error);
  //       } else {
  //         processManager.stopProcess(codeId).catch(console.error);
  //       }
  //     }
  //   }
  // });
  
  // Start the server once connected
  server.listen(port, () => {
    console.log(`Node.js server running on port ${port}`);
    console.log('Connected to SpacetimeDB and ready to manage Python processes');
    console.log(`WebSocket server initialized for REPL communication on ws://localhost:${port}`);
  });
}

const handleDisconnect = (): void => {
  console.log('Disconnected from SpacetimeDB');
  
  // Clean up processes on disconnect
  processManager.stopAllProcesses().catch(console.error);
  
  // Clear the SpacetimeClient reference
  processManager.spacetimeClient = null;
  
  // Try to reconnect after a delay
  setTimeout(initializeSpacetimeClient, 5000);
};

const handleConnectError = (ctx: ErrorContext, err: Error): void => {
  console.error('Error connecting to SpacetimeDB:', err);
  
  // Try to reconnect after a delay
  setTimeout(initializeSpacetimeClient, 5000);
};

/**
 * Subscribe to multiple queries
 */
const subscribeToQueries = (conn: DbConnection, queries: string[]): void => {
  let count = 0;
  for (const query of queries) {
    try {
      // Use type assertion to work around TypeScript errors
      const subscriptionBuilder = conn.subscriptionBuilder();
      
      // @ts-ignore: API not recognized correctly
      subscriptionBuilder.onApplied(() => {
        count++;
        if (count === queries.length) {
          console.log('SDK client cache initialized.');
        }
      });
      
      // @ts-ignore: API not recognized correctly
      subscriptionBuilder.subscribe(query);
    } catch (error) {
      console.error('Error subscribing to query:', query, error);
    }
  }
};

/**
 * Initialize the SpacetimeDB client connection
 */
async function initializeSpacetimeClient(): Promise<void> {
  console.log('Connecting to SpacetimeDB...');
  try {
    DbConnection.builder()
      .withUri(SERVER_URL)
      .withModuleName(MODULE_NAME)
      .withToken('')
      .onConnect(handleConnect)
      .onDisconnect(handleDisconnect)
      .onConnectError(handleConnectError)
      .build();
  } catch (error) {
    console.error('Failed to connect to SpacetimeDB:', error);
    
    // Try to reconnect after a delay
    setTimeout(initializeSpacetimeClient, 5000);
  }
}

// Handle graceful shutdown
process.on('SIGINT', async () => {
  console.log('Shutting down...');
  await processManager.stopAllProcesses();
  process.exit(0);
});

// Start the application
function init(): void {
  initializeSpacetimeClient();
}

// Start the application
init();
