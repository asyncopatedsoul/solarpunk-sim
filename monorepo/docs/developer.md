# Developer Guide

# General
- when editing the server, publish changes to server:
spacetime publish --project-path . solarville
- if changes to the table schemas, easiest to clear the database 
```
# stop server
spacetime server clear
# restart server
```
- or could implement incremental migration
https://spacetimedb.com/docs/how-to/incremental-migrations
- when editing the server, update the spacetimeDB bindings for all clients
```
cd Solarville_server
spacetime generate --lang typescript --out-dir ../monorepo/node-server/src/module_bindings
```
- run the SpacetimeDB service
```
spactime start
```
- get server logs
```
# tail logs
spacetime logs -f solarville 
# logs to file
spacetime logs solarville > spacetimedb.log

```
- inspect database schema and reducers
```
spacetime describe --json solarville > describe_solarville.json
```
JSON Grid Viewer extension 
https://marketplace.cursorapi.com/items?itemName=DutchIgor.json-viewer
- query the spacetimeDB database
https://spacetimedb.com/docs/sql
https://spacetimedb.com/docs/cli-reference#spacetime-sql
```
spacetime sql solarville --interactive
```

## Testing Python runners

> Options: {"mode":"text","pythonPath":"python3","pythonOptions":["-u"],"scriptPath":"/Users/michael.garrido/Documents/GitHub/solarpunk-sim/monorepo/micropython-runtime/firmware","args":["/Users/michael.garrido/Documents/GitHub/solarpunk-sim/monorepo/node-server/scripts/temp/123.py","single_process"]}
```
python monorepo/micropython-runtime/firmware/process_runner.py monorepo/node-server/scripts/temp/123.py single_process

python monorepo/micropython-runtime/firmware/thread_runner.py monorepo/node-server/scripts/temp/123.py single_process

```
run the repl process on Mac Terminal 
to gracefully exit the repl process, control-c KeyboardInterrupt then control-d exit Interactive console


set_motor_speed(0, 0)

ps aux | grep "python.*repl_runner_socket.py"
ps aux | grep "python.*repl_runner_websocket2.py"

ps aux | grep "python.*repl_websocket_server.py"

git rm --cached -r folder