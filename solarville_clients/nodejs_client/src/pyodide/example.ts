import { PyodideServer } from './PyodideServer';
import { PyodideProcess } from './PyodideProcess';
import { SpacetimeDBIntegration } from './SpacetimeDBIntegration';
import { DbConnection } from '../module_bindings';

/**
 * Example of using the Pyodide integration
 */
async function main() {
  try {
    console.log('Starting Pyodide integration example...');
    
    // Connect to SpacetimeDB
    console.log('Connecting to SpacetimeDB...');
    const spacetimeClient = DbConnection.builder()
      .withUri('ws://127.0.0.1:3000')
      .withModuleName('solarville')
      .withToken('')
      .build();
      
    // Create a Pyodide server
    console.log('Creating Pyodide server...');
    const server = new PyodideServer({
      port: 3200,
      debug: true
    });
    
    // Get the PyodideManager
    const manager = server.getManager();
    
    // Create SpacetimeDB integration
    console.log('Creating SpacetimeDB integration...');
    const integration = new SpacetimeDBIntegration({
      manager,
      spacetimeClient,
      syncIntervalMs: 1000
    });
    
    // Start the server
    console.log('Starting Pyodide server...');
    await server.start();
    
    // Start the SpacetimeDB integration
    console.log('Starting SpacetimeDB integration...');
    integration.startSync();
    
    // Create a sample Pyodide process
    console.log('Creating sample Pyodide process...');
    const process = manager.createProcess({
      initialCode: `
# Initialize variables
left_motor_speed = 0.0
right_motor_speed = 0.0
error_message = ""

# Define helper functions
def set_motors(left, right):
    global left_motor_speed, right_motor_speed
    left_motor_speed = max(min(left, 1.0), -1.0)
    right_motor_speed = max(min(right, 1.0), -1.0)
    print(f"Motors set to: left={left_motor_speed}, right={right_motor_speed}")

def setup():
    print("Setting up...")
    set_motors(0.0, 0.0)
    
def loop():
    # This function would be called periodically in a real implementation
    pass

# Run setup
setup()
`,
      watchedVariables: ['left_motor_speed', 'right_motor_speed', 'error_message']
    });
    
    // Set up handlers for variable changes
    process.on('variableChange', (variable, value, oldValue) => {
      console.log(`Variable changed in process ${process.id}: ${variable} = ${value} (was ${oldValue})`);
    });
    
    // Wait for the process to be ready
    process.on('ready', async () => {
      console.log(`Process ${process.id} is ready`);
      
      // Run some sample code
      console.log('Running sample code...');
      await process.runCode('set_motors(0.5, 0.3)');
      
      // Get variables
      const leftSpeed = process.getVariable('left_motor_speed');
      const rightSpeed = process.getVariable('right_motor_speed');
      
      console.log(`Motor speeds: left=${leftSpeed}, right=${rightSpeed}`);
      
      // Test setting variables directly
      console.log('Setting variables directly...');
      process.setVariable('left_motor_speed', 0.8);
      
      // Test watching additional variables
      console.log('Watching additional variables...');
      process.watchVariable('counter');
      
      // Create a variable to watch
      console.log('Creating a variable to watch...');
      await process.runCode('counter = 0');
      
      // Update the variable periodically
      console.log('Starting counter update loop...');
      let count = 0;
      const intervalId = setInterval(async () => {
        count++;
        await process.runCode(`counter = ${count}`);
        
        // Stop after 10 updates
        if (count >= 10) {
          clearInterval(intervalId);
          console.log('Counter update loop completed');
          
          // Clean up after a delay
          setTimeout(() => {
            cleanUp(server, integration);
          }, 2000);
        }
      }, 1000);
    });
    
    // Handle process errors
    process.on('error', (error) => {
      console.error(`Error in process ${process.id}:`, error);
    });
    
    // Handle Ctrl+C
    process.on('SIGINT', () => {
      cleanUp(server, integration);
    });
    
  } catch (error) {
    console.error('Error in example:', error);
  }
}

/**
 * Clean up resources
 */
function cleanUp(server: PyodideServer, integration: SpacetimeDBIntegration) {
  console.log('Cleaning up...');
  
  // Stop the SpacetimeDB integration
  integration.stopSync();
  
  // Stop the server
  server.stop().then(() => {
    console.log('Server stopped');
    process.exit(0);
  }).catch((error) => {
    console.error('Error stopping server:', error);
    process.exit(1);
  });
}

// Run the example
main().catch(console.error);
