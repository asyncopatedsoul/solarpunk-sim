<script>
  import { onMount, onDestroy } from 'svelte';

  export let codeId = null;
  export let websocketUrl = 'ws://localhost:3001';

  let socket;
  let replOutput = '';
  let commandInput = '';
  let connected = false;
  let errorMessage = '';

  function connect() {
    if (socket && socket.readyState === WebSocket.OPEN) {
      return;
    }

    try {
      socket = new WebSocket(websocketUrl);
      
      socket.onopen = () => {
        connected = true;
        errorMessage = '';
        replOutput += '> Connected to Python REPL server\n';
      };
      
      socket.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          
          if (data.type === 'repl_output' && data.codeId === codeId) {
            replOutput += data.output;
            scrollToBottom();
          } else if (data.type === 'error') {
            errorMessage = data.message;
          }
        } catch (e) {
          console.error('Error parsing WebSocket message:', e);
        }
      };
      
      socket.onclose = () => {
        connected = false;
        replOutput += '> Disconnected from Python REPL server\n';
      };
      
      socket.onerror = (error) => {
        connected = false;
        errorMessage = 'WebSocket error occurred';
        console.error('WebSocket error:', error);
      };
    } catch (error) {
      errorMessage = `Failed to connect: ${error.message}`;
      console.error('Error creating WebSocket:', error);
    }
  }

  function sendCommand() {
    if (!socket || socket.readyState !== WebSocket.OPEN || !commandInput.trim()) {
      return;
    }
    
    try {
      socket.send(JSON.stringify({
        type: 'repl_command',
        codeId,
        command: commandInput
      }));
      
      replOutput += `> ${commandInput}\n`;
      commandInput = '';
      scrollToBottom();
    } catch (error) {
      errorMessage = `Failed to send command: ${error.message}`;
      console.error('Error sending command:', error);
    }
  }

  function scrollToBottom() {
    setTimeout(() => {
      const outputElement = document.getElementById('repl-output');
      if (outputElement) {
        outputElement.scrollTop = outputElement.scrollHeight;
      }
    }, 0);
  }

  function handleKeyDown(event) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      sendCommand();
    }
  }

  function clearOutput() {
    replOutput = '> REPL output cleared\n';
  }

  $: if (codeId && !socket) {
    connect();
  }

  onMount(() => {
    if (codeId) {
      connect();
    }
  });

  onDestroy(() => {
    if (socket) {
      socket.close();
    }
  });
</script>

<div class="repl-container">
  <div class="repl-header">
    <h3>Python REPL Console</h3>
    <button on:click={clearOutput} class="clear-button">Clear</button>
  </div>
  
  {#if errorMessage}
    <div class="error-message">{errorMessage}</div>
  {/if}
  
  <div id="repl-output" class="repl-output">{replOutput}</div>
  
  <div class="repl-input">
    <textarea
      bind:value={commandInput}
      on:keydown={handleKeyDown}
      placeholder="Enter Python command..."
      disabled={!connected || !codeId}
    ></textarea>
    <button on:click={sendCommand} disabled={!connected || !codeId}>Run</button>
  </div>
</div>

<style>
  .repl-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    border: 1px solid #ccc;
    border-radius: 4px;
    overflow: hidden;
  }

  .repl-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0 10px;
    background-color: #f0f0f0;
    border-bottom: 1px solid #ccc;
  }

  .repl-header h3 {
    margin: 10px 0;
    font-size: 16px;
  }

  .clear-button {
    background-color: #f0f0f0;
    border: 1px solid #ccc;
    border-radius: 4px;
    padding: 4px 8px;
    cursor: pointer;
  }

  .error-message {
    color: red;
    padding: 5px 10px;
    background-color: #ffeeee;
    border-bottom: 1px solid #ffcccc;
  }

  .repl-output {
    flex-grow: 1;
    padding: 10px;
    background-color: #1e1e1e;
    color: #d4d4d4;
    font-family: 'Source Code Pro', monospace;
    font-size: 14px;
    line-height: 1.5;
    white-space: pre-wrap;
    overflow-y: auto;
    min-height: 200px;
  }

  .repl-input {
    display: flex;
    border-top: 1px solid #ccc;
  }

  .repl-input textarea {
    flex-grow: 1;
    padding: 10px;
    font-family: 'Source Code Pro', monospace;
    font-size: 14px;
    border: none;
    resize: none;
    height: 60px;
  }

  .repl-input button {
    padding: 0 20px;
    background-color: #4285f4;
    color: white;
    border: none;
    cursor: pointer;
  }

  .repl-input button:disabled {
    background-color: #cccccc;
    cursor: not-allowed;
  }
</style> 