<script>
	import { onMount } from 'svelte';
	import { PythonEditor, PythonREPL, GlobalStateViewer, ExecutionModeSelector } from '$lib';

	export const ssr = false;

	let code = `# Python Execution Demo
# This is a simple example of Python code that can be executed

# Global variables that will be tracked
left_motor_speed = 0.5
right_motor_speed = 0.25
sensor_data = {"temperature": 22.5, "humidity": 65}
robot_state = "running"

# Define a simple function
def calculate_average(a, b):
    return (a + b) / 2

# Main function that will be executed
def main():
    global left_motor_speed, right_motor_speed
    
    # Some calculation example
    left_motor_speed = calculate_average(0.3, 0.7)
    right_motor_speed = calculate_average(0.1, 0.4)
    
    # Print some output for the REPL
    print(f"Motors set to: L={left_motor_speed}, R={right_motor_speed}")
    print(f"Current sensor data: {sensor_data}")
    
    # You can see this value in the Global State viewer
    result = [i * i for i in range(5)]
    print(f"Calculated squares: {result}")
`;

	let codeId = 123; // This would normally come from the database
	let selectedMode = 'single_process';
	let isRunning = false;
	let apiBaseUrl = 'http://localhost:3001';
	let websocketUrl = 'ws://localhost:3001';
	let errorMessage = '';

    onMount(async () => {
        console.log('Mounted python-executor');
    });

	async function startExecution() {
		try {
			const response = await fetch(`${apiBaseUrl}/api/process/start`, {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify({
					codeId,
					executionMode: selectedMode
				})
			});

			if (!response.ok) {
				const error = await response.json();
				throw new Error(error.error || 'Failed to start process');
			}

			isRunning = true;
			errorMessage = '';
		} catch (error) {
			errorMessage = error.message;
			console.error('Error starting process:', error);
		}
	}

	async function stopExecution() {
		try {
			const response = await fetch(`${apiBaseUrl}/api/process/stop`, {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify({
					codeId
				})
			});

			if (!response.ok) {
				const error = await response.json();
				throw new Error(error.error || 'Failed to stop process');
			}

			isRunning = false;
			errorMessage = '';
		} catch (error) {
			errorMessage = error.message;
			console.error('Error stopping process:', error);
		}
	}

	function handleModeChange(mode) {
		selectedMode = mode;
	}

	function handleCodeChange(newCode) {
		code = newCode;
		// In a real app, we would save the code to the database
	}
</script>

<svelte:head>
	<title>Python Execution Demo</title>
</svelte:head>

<div class="container">
	<h1>Python Execution Demo</h1>

	{#if errorMessage}
		<div class="error-message">{errorMessage}</div>
	{/if}

	<div class="execution-controls">
		<ExecutionModeSelector {selectedMode} onChange={handleModeChange} />

		<div class="button-group">
			<button on:click={startExecution} disabled={isRunning} class="start-button">
				Start Execution
			</button>
			<button on:click={stopExecution} disabled={!isRunning} class="stop-button">
				Stop Execution
			</button>
		</div>
	</div>

	<div class="panels">
		<div class="panel editor-panel">
			<h2>Python Code Editor</h2>
			<div class="editor-container">
				<PythonEditor {code} onChange={handleCodeChange} />
			</div>
		</div>

		<div class="panel repl-panel">
			<h2>Python REPL</h2>
			<div class="repl-container">
				<PythonREPL {codeId} {websocketUrl} />
			</div>
		</div>

		<div class="panel state-panel">
			<h2>Global State</h2>
			<div class="state-container">
				<GlobalStateViewer {codeId} {websocketUrl} />
			</div>
		</div>
	</div>
</div>

<style>
	.container {
		max-width: 1200px;
		margin: 0 auto;
		padding: 20px;
	}

	h1 {
		margin-bottom: 20px;
		color: #333;
	}

	h2 {
		margin-top: 0;
		margin-bottom: 10px;
		font-size: 18px;
		color: #333;
	}

	.error-message {
		padding: 10px;
		margin-bottom: 20px;
		background-color: #ffeeee;
		border: 1px solid #ffcccc;
		border-radius: 4px;
		color: #cc0000;
	}

	.execution-controls {
		display: flex;
		flex-direction: column;
		margin-bottom: 20px;
	}

	.button-group {
		display: flex;
		gap: 10px;
		margin-bottom: 20px;
	}

	button {
		padding: 10px 20px;
		border: none;
		border-radius: 4px;
		font-weight: bold;
		cursor: pointer;
	}

	button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}

	.start-button {
		background-color: #4caf50;
		color: white;
	}

	.stop-button {
		background-color: #f44336;
		color: white;
	}

	.panels {
		display: grid;
		grid-template-columns: 1fr 1fr;
		grid-template-rows: 1fr 1fr;
		gap: 20px;
		height: 800px;
	}

	.panel {
		display: flex;
		flex-direction: column;
		border: 1px solid #ddd;
		border-radius: 4px;
		padding: 15px;
		background-color: white;
	}

	.editor-panel {
		grid-column: 1;
		grid-row: 1 / span 2;
	}

	.repl-panel {
		grid-column: 2;
		grid-row: 1;
	}

	.state-panel {
		grid-column: 2;
		grid-row: 2;
	}

	.editor-container,
	.repl-container,
	.state-container {
		flex-grow: 1;
		overflow: hidden;
	}
</style>
