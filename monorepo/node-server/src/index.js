const express = require('express');
const { PythonShell } = require('python-shell');
const path = require('path');
const { Client } = require('@clockworklabs/spacetimedb-sdk');

const app = express();
const port = process.env.PORT || 3001;

// Import our modules
const ProcessManager = require('./process-manager/processManager');
const SpacetimeClient = require('./spacetime-client/spacetimeClient');

// Initialize the SpacetimeDB client
const spacetimeClient = new SpacetimeClient('localhost:3000', 'robotics-simulation-db');

// Initialize the process manager
const processManager = new ProcessManager(spacetimeClient);

// Express middleware
app.use(express.json());

// API routes
app.get('/api/status', (req, res) => {
  res.json({
    status: 'ok',
    activeProcesses: processManager.getActiveProcessCount(),
    connectedToSpacetime: spacetimeClient.isConnected()
  });
});

// Start a Python process
app.post('/api/process/start', async (req, res) => {
  const { codeId } = req.body;
  
  if (!codeId) {
    return res.status(400).json({ error: 'Missing required parameter: codeId' });
  }
  
  try {
    const processId = await processManager.startProcess(codeId);
    res.json({ processId, status: 'started' });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

// Stop a Python process
app.post('/api/process/stop', async (req, res) => {
  const { codeId } = req.body;
  
  if (!codeId) {
    return res.status(400).json({ error: 'Missing required parameter: codeId' });
  }
  
  try {
    await processManager.stopProcess(codeId);
    res.json({ status: 'stopped' });
  } catch (error) {
    res.status(500).json({ error: error.message });
  }
});

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

// Connect to SpacetimeDB and start listening for code changes
async function init() {
  try {
    console.log('Connecting to SpacetimeDB...');
    await spacetimeClient.connect();
    
    // Subscribe to code changes
    spacetimeClient.subscribeToCodeChanges((codeId, isRunning) => {
      if (isRunning) {
        processManager.startProcess(codeId).catch(console.error);
      } else {
        processManager.stopProcess(codeId).catch(console.error);
      }
    });
    
    // Start the server
    app.listen(port, () => {
      console.log(`Node.js server running on port ${port}`);
      console.log('Connected to SpacetimeDB and ready to manage Python processes');
    });
  } catch (error) {
    console.error('Failed to initialize:', error);
    process.exit(1);
  }
}

// Handle graceful shutdown
process.on('SIGINT', async () => {
  console.log('Shutting down...');
  await processManager.stopAllProcesses();
  process.exit(0);
});

// Start the server
init();
