<template>
    <div>
        <p>{{ foo }} : {{ message }}</p>
        <button @click="changeMessage">Change Message</button>
    </div>
    <div>
        <dynamicComponent :templateString="myTemplateString" :scriptString="scriptString" />
    </div>
</template>

<script>
import { ref } from 'vue';
import dynamicComponent from './dynamicComponent.vue';

let templateString = `<div> \
      <button @click="sendMessage">Send Message</button>\
    </div>\
`;
let scriptString = `
    return {\
      methods: {\
        sendMessage() {\
        console.log(this);\
            this.repl("Hello from child!");\
            console.log("Message sent!");\
          this.$emit("my-event", "Hello from child!");\
        }\
      }\
    }\
    `;

export default {
    name: 'MyComponent',
    props: {
        foo: {
            type: String,
            default: 'foo',
        },
    },
    components: {
        dynamicComponent
    },
    setup() {
        const message = ref('Hello, Vue 3!');

        const changeMessage = () => {
            message.value = 'Message changed!';
        };

        return {
            message,
            changeMessage,
        };
    },
    data() {
        return {
            myTemplateString: templateString,
            scriptString: scriptString,
        };
    }
};
</script>

<style scoped>
p {
    font-size: 18px;
    color: #333;
}

button {
    padding: 10px 20px;
    background-color: #42b983;
    color: white;
    border: none;
    cursor: pointer;
}
</style>