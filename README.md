# ecosim
 

spacetime publish --server local solarville
spacetime logs solarville
spacetime start


pnpm create vite@latest . -- --template react-ts
cd client
pnpm install

solarville_clients/typescript_client/src

mkdir -p client/src/module_bindings
spacetime generate --lang typescript --out-dir client/src/module_bindings --project-path server

spacetime generate --lang typescript --out-dir ../solarville_clients/react_client/src/module_bindings

spacetime publish --server local solarville