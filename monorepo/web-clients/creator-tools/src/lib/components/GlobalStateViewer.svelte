<script>
  import { onMount, onDestroy } from 'svelte';

  export let codeId = null;
  export let websocketUrl = 'ws://localhost:3001';

  let socket;
  let globalState = {};
  let connected = false;
  let errorMessage = '';
  let expandedKeys = new Set();

  function connect() {
    if (socket && socket.readyState === WebSocket.OPEN) {
      return;
    }

    try {
      socket = new WebSocket(websocketUrl);
      
      socket.onopen = () => {
        connected = true;
        errorMessage = '';
      };
      
      socket.onmessage = (event) => {
        try {
          const data = JSON.parse(event.data);
          
          if (data.type === 'global_state' && data.codeId === codeId) {
            globalState = data.state;
          } else if (data.type === 'error') {
            errorMessage = data.message;
          } else if (data.type === 'initial_state' && data.state[codeId]) {
            globalState = data.state[codeId].globalState || {};
          }
        } catch (e) {
          console.error('Error parsing WebSocket message:', e);
        }
      };
      
      socket.onclose = () => {
        connected = false;
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

  function toggleExpand(key) {
    if (expandedKeys.has(key)) {
      expandedKeys.delete(key);
    } else {
      expandedKeys.add(key);
    }
    expandedKeys = expandedKeys; // Trigger reactivity
  }

  function isObject(value) {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
  }

  function isArray(value) {
    return Array.isArray(value);
  }

  function formatValue(value) {
    if (value === null) return 'null';
    if (value === undefined) return 'undefined';
    if (typeof value === 'string') return `"${value}"`;
    return String(value);
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

<div class="state-viewer-container">
  <div class="state-viewer-header">
    <h3>Python Global State</h3>
  </div>
  
  {#if errorMessage}
    <div class="error-message">{errorMessage}</div>
  {/if}
  
  <div class="state-viewer-content">
    {#if Object.keys(globalState).length === 0}
      <div class="empty-state">No global state available</div>
    {:else}
      <div class="state-tree">
        {#each Object.entries(globalState) as [key, value]}
          <div class="state-item">
            {#if isObject(value) || isArray(value)}
              <div class="state-key expandable" on:click={() => toggleExpand(key)}>
                <span class="expand-icon">{expandedKeys.has(key) ? '▼' : '►'}</span>
                <span class="key-name">{key}</span>
                <span class="type-indicator">{isArray(value) ? 'Array' : 'Object'}</span>
              </div>
              
              {#if expandedKeys.has(key)}
                <div class="nested-items">
                  {#each Object.entries(value) as [nestedKey, nestedValue]}
                    <div class="state-item nested">
                      <div class="state-key">
                        <span class="key-name">{nestedKey}:</span>
                        <span class="state-value">{formatValue(nestedValue)}</span>
                      </div>
                    </div>
                  {/each}
                </div>
              {/if}
            {:else}
              <div class="state-key">
                <span class="key-name">{key}:</span>
                <span class="state-value">{formatValue(value)}</span>
              </div>
            {/if}
          </div>
        {/each}
      </div>
    {/if}
  </div>
</div>

<style>
  .state-viewer-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    border: 1px solid #ccc;
    border-radius: 4px;
    overflow: hidden;
  }

  .state-viewer-header {
    padding: 0 10px;
    background-color: #f0f0f0;
    border-bottom: 1px solid #ccc;
  }

  .state-viewer-header h3 {
    margin: 10px 0;
    font-size: 16px;
  }

  .error-message {
    color: red;
    padding: 5px 10px;
    background-color: #ffeeee;
    border-bottom: 1px solid #ffcccc;
  }

  .state-viewer-content {
    flex-grow: 1;
    padding: 10px;
    background-color: #f8f8f8;
    overflow-y: auto;
  }

  .empty-state {
    color: #888;
    font-style: italic;
    padding: 20px;
    text-align: center;
  }

  .state-tree {
    font-family: 'Source Code Pro', monospace;
    font-size: 14px;
  }

  .state-item {
    margin: 4px 0;
  }

  .state-key {
    padding: 4px 8px;
    border-radius: 4px;
    background-color: #fff;
    display: flex;
    align-items: center;
  }

  .expandable {
    cursor: pointer;
  }

  .expandable:hover {
    background-color: #f0f0f0;
  }

  .key-name {
    font-weight: bold;
    margin-right: 8px;
  }

  .expand-icon {
    margin-right: 8px;
    font-size: 10px;
    width: 12px;
    display: inline-block;
  }

  .nested-items {
    margin-left: 20px;
    border-left: 1px dashed #ccc;
    padding-left: 10px;
  }

  .state-value {
    color: #0275d8;
  }

  .type-indicator {
    margin-left: auto;
    color: #888;
    font-size: 12px;
    background-color: #eee;
    padding: 2px 6px;
    border-radius: 10px;
  }
</style> 