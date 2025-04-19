// import { defineConfig } from 'vite'
// import react from '@vitejs/plugin-react'

// // https://vite.dev/config/
// export default defineConfig({
//   plugins: [react()],
// })

import { defineConfig } from 'vite'

// If mode cjs(vite.config.cjs), should use `veaury/vite/cjs/index.cjs`
// If mode esm(vite.config.mjs), should use `veaury/vite/esm/index.mjs`
// import veauryVitePlugins from 'veaury/vite/esm/index.mjs'
// 'veaury/types/veaury.d.ts'
// If the configuration file of vite has a `.js` suffix(vite.config.js), it is recommended to import it in the following way.
import veauryVitePlugins from 'veaury/vite/index.js'

export default defineConfig({
  plugins: [
    // Turn off react plugin
    // react(),
    // When the type of veauryVitePlugins is set to react, 
    // only jsx in .vue files and files in a directory named "vue_app" will be parsed using vue jsx, 
    // jsx in other files will be parsed with react jsx
    veauryVitePlugins({
      type: 'react',
      // Configuration of @vitejs/plugin-vue
      // vueOptions: {...},
      // Configuration of @vitejs/plugin-react
      // reactOptions: {...}, 
      // Configuration of @vitejs/plugin-vue-jsx
      // vueJsxOptions: {...}
    })
  ],
  resolve: {
    alias: {
      'vue': "vue/dist/vue.esm-bundler.js"
    }
  }
})
