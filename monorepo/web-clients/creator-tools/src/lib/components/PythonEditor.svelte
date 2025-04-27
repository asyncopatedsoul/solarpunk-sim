<script>
  import { onMount, onDestroy } from 'svelte';
  import { CodeJar } from 'codejar';
  import { withLineNumbers } from 'codejar/linenumbers';
  import Prism from 'prismjs';
  import 'prismjs/components/prism-python';
  import 'prismjs/themes/prism.css';

  export let code = '';
  export let readOnly = false;
  export let onChange = (value) => {};

  let editor;
  let jar;

  onMount(() => {
    const highlight = (editor) => {
      editor.textContent = editor.textContent;
      Prism.highlightElement(editor);
    };

    jar = CodeJar(editor, withLineNumbers(highlight), {
      tab: '    ', // 4 spaces
      indentOn: /[:(\[]$/,
      spellcheck: false,
      addClosing: true,
      catchTab: true,
      preserveIdent: true,
      braces: false
    });

    jar.updateCode(code);
    jar.onUpdate((newCode) => {
      code = newCode;
      onChange(newCode);
    });

    if (readOnly) {
      editor.setAttribute('contenteditable', 'false');
      editor.style.opacity = '0.8';
      editor.style.userSelect = 'text';
    }
  });

  onDestroy(() => {
    if (jar) {
      jar.destroy();
    }
  });

  $: if (jar) {
    // Update code in the editor when the code prop changes
    if (jar.toString() !== code) {
      jar.updateCode(code);
    }
  }
</script>

<div class="editor-container">
  <div bind:this={editor} class="editor language-python"></div>
</div>

<style>
  .editor-container {
    width: 100%;
    height: 100%;
    overflow: auto;
    border: 1px solid #ccc;
    border-radius: 4px;
    background-color: #f5f5f5;
  }

  .editor {
    min-height: 100%;
    padding: 10px;
    font-family: 'Source Code Pro', monospace;
    font-size: 14px;
    line-height: 1.5;
    tab-size: 4;
    white-space: pre;
  }

  /* Line number colors */
  :global(.codejar-linenumbers) {
    color: #888;
    background-color: #f0f0f0;
    padding-right: 8px;
    margin-right: 8px;
    border-right: 1px solid #ddd;
  }
</style> 