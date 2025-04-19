# ecosim
 

spacetime publish --server local solarville
spacetime logs solarville
spacetime start


pnpm create vite@latest . -- --template react-ts
cd client
pnpm install

solarville_clients/typescript_client/src

mkdir -p client/src/module_bindings

## in solarville_server

spacetime generate --lang csharp --out-dir client/src/module_bindings --project-path server

### for Unity client
spacetime generate --lang csharp --out-dir ../Solarville/Assets/_Scripts/autogen

### for NodeJS client
spacetime generate --lang typescript --out-dir ../solarville_clients/nodejs_client/src/module_bindings

### for ReactJS client
spacetime generate --lang typescript --out-dir ../solarville_clients/react_client/src/module_bindings

spacetime publish --server local solarville

## Run full system
- open Unity Project: Solarville
- run SpacetimeSB service
```bash
spacetime start
```
- run NodeJS client
```bash
cd solarville_clients/nodejs_client
npm run dev
```
- run ReactJS client
```bash
cd solarville_clients/react_client
npm run dev
```
http://localhost:5173/
