<script>
  import { onMount } from 'svelte';
  
  const demos = [
    { title: 'Python Executor', description: 'Interactive Python code execution with multiple execution modes', path: '/demo/python-executor' }
  ];
</script>

<svelte:head>
  <title>Creator Tools Demos</title>
</svelte:head>

<div class="container">
  <h1>Creator Tools Demos</h1>
  
  <div class="demo-list">
    {#each demos as demo}
      <a href={demo.path} class="demo-card">
        <h2>{demo.title}</h2>
        <p>{demo.description}</p>
      </a>
    {/each}
  </div>
</div>

<style>
  .container {
    max-width: 800px;
    margin: 0 auto;
    padding: 20px;
  }
  
  h1 {
    margin-bottom: 30px;
    color: #333;
  }
  
  .demo-list {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 20px;
  }
  
  .demo-card {
    display: block;
    padding: 20px;
    border-radius: 8px;
    background-color: #f8f8f8;
    color: #333;
    text-decoration: none;
    transition: transform 0.2s, box-shadow 0.2s;
    box-shadow: 0 2px 5px rgba(0,0,0,0.1);
  }
  
  .demo-card:hover {
    transform: translateY(-3px);
    box-shadow: 0 5px 15px rgba(0,0,0,0.1);
  }
  
  .demo-card h2 {
    margin-top: 0;
    margin-bottom: 10px;
    color: #0275d8;
  }
  
  .demo-card p {
    margin: 0;
    color: #666;
  }
</style>
