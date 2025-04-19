<script>
import { h } from 'vue';

let builtInMethods = {
  repl(command) {
    console.log("repl command: ", command);
    this.$emit("repl", command);
  }
};

function executeScript(scriptString) {
  try {
    const scriptFunction = new Function(scriptString);
    // return scriptFunction();
    let config = scriptFunction();
    config.methods = {
      ...builtInMethods,
      ...config.methods
    };
    return config;

  } catch (error) {
    console.error("Error executing script:", error);
    return {};
  }
}

export default {
  props: {
    templateString: {
      type: String,
      required: true
    },
    scriptString: {
      type: String,
      required: true
    }
  },
  render() {
    return h({
      template: this.templateString, 
      ...executeScript(this.scriptString)
    //   ...new Function(this.scriptString)()
    });
  }
};
</script>