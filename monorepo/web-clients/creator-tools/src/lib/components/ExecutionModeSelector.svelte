<script>
  export let selectedMode = 'single_process';
  export let onChange = (mode) => {};
  
  const modes = [
    { id: 'single_process', label: 'Single Process', description: 'Execute code in a single Python process' },
    { id: 'multi_process', label: 'Multi-Process', description: 'Execute code in multiple Python processes' },
    { id: 'multi_thread', label: 'Multi-Thread', description: 'Execute code in multiple threads within a single Python process' },
    { id: 'pyodide', label: 'Pyodide (Browser)', description: 'Execute code in the browser using Pyodide WebAssembly' }
  ];
  
  function handleModeChange(event) {
    selectedMode = event.target.value;
    onChange(selectedMode);
  }
</script>

<div class="mode-selector">
  <h3>Execution Mode</h3>
  <div class="mode-options">
    {#each modes as mode}
      <label class="mode-option">
        <input 
          type="radio" 
          name="executionMode" 
          value={mode.id} 
          checked={selectedMode === mode.id}
          on:change={handleModeChange}
        />
        <div class="mode-info">
          <span class="mode-label">{mode.label}</span>
          <span class="mode-description">{mode.description}</span>
        </div>
      </label>
    {/each}
  </div>
</div>

<style>
  .mode-selector {
    margin-bottom: 20px;
    padding: 10px;
    border: 1px solid #ccc;
    border-radius: 4px;
    background-color: #f8f8f8;
  }
  
  h3 {
    margin-top: 0;
    margin-bottom: 10px;
    font-size: 16px;
  }
  
  .mode-options {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }
  
  .mode-option {
    display: flex;
    align-items: flex-start;
    padding: 8px;
    border-radius: 4px;
    background-color: white;
    cursor: pointer;
    transition: background-color 0.2s;
  }
  
  .mode-option:hover {
    background-color: #f0f0f0;
  }
  
  .mode-info {
    display: flex;
    flex-direction: column;
    margin-left: 8px;
  }
  
  .mode-label {
    font-weight: bold;
  }
  
  .mode-description {
    font-size: 12px;
    color: #666;
  }
  
  input[type="radio"] {
    margin-top: 4px;
  }
</style> 