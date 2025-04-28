const { PythonShell } = require('python-shell');
const readline = require('readline');

const PYTHON_SCRIPT_PATH = '../firmware/repl_runner.py'; // Replace with your main script path

async function runPythonRepl() {
  let pyshell = new PythonShell(PYTHON_SCRIPT_PATH, {
    mode: 'text', // Ensure text mode for proper line ending handling
    pythonOptions: ['-u'], // Unbuffered output for immediate feedback
  });

  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
  });

  // Handle output from Python
  pyshell.on('message', (message) => {
    try {
      const data = JSON.parse(message);
      if (data.global_state) {
        console.log('--- Global State Snapshot ---');
        console.log(JSON.stringify(data.global_state, null, 2)); // Pretty-print JSON
        console.log('--- End of Snapshot ---');
      } else if (data.type === 'error') {
        console.error('Python Error:', data.error);
        if (data.traceback) {
          console.error(data.traceback);
        }
      } else if (data.type === 'repl_output') {
        console.log(data.output);
      } else {
        console.log(message); // Other output
      }
    } catch (error) {
      console.log(message); // If not JSON, just print
    }
  });

  pyshell.on('stderr', (stderr) => {
    console.error('Python Stderr:', stderr);
  });

  pyshell.end((err, code, signal) => {
    if (err) {
      console.error('Pyshell ended with error:', err);
    }
    console.log('Pyshell finished. Exit code:', code);
    rl.close();
  });

  // Forward input to Python
  rl.on('line', (input) => {
    pyshell.send(input);
  });

  // Handle Ctrl+C gracefully
  rl.on('SIGINT', () => {
    console.log('Received SIGINT. Sending exit command to Python...');
    pyshell.send('exit_repl()'); // Send the exit command
    // Don't immediately close rl; let pyshell.end handle it after Python exits.
  });
}

runPythonRepl().catch(err => {
  console.error('Error running Python REPL:', err);
});