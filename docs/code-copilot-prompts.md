## Project Instructions
Initialize codemcp with '/Users/michael.garrido/Documents/GitHub/solarpunk-sim/'

You are managing a codebase for a full-stack simulation of embedded systems in a real-time multiplayer sandbox: firmware as python processes, state and user-generated source code synchronization through SpacetimeDB, user-generated interface to embedded system through web app, and physics/environment simulation through Unity. 

##
let's create a small test for multiplayer character sync through Spacetime DB.

using the reference implementation of Unity and Spacetime DB in 'reference-implementations/Blackholio', implement this feature in the Unity project 'Solarville' and a Spacetime DB server written in Rust in 'Solarville_server':
- in the folder 'Assets/_Scripts' create Spacetime client scripts and player controller scripts that synchronize the 3d movement of a player character prefab. See these scripts as a reference:
    - 'reference-implementations/Blackholio/client-unity/Assets/Scripts/GameManager.cs'
    - 'reference-implementations/Blackholio/client-unity/Assets/Scripts/PlayerController.cs'
- in the folder 'Solarville_server', implement the Spactime DB service to synchronize 3d movement in Rust. See these scripts as reference:
    - reference-implementations/Blackholio/server-rust/src/lib.rs

##
let's implement a simulation of a remote control car whose firmware is python code. 

here is the system architecture:
- the player writes the python code with functions for driving the motors. the python file's code and path is saved in the spacetime db 'solarville' in table 'microprocess_code' via nodejs client solarville_clients/nodejs_client/src/index.ts
- this python code as simulated firmware is retrieved from spacetime db and loaded into a python repl process spawned and managed by the nodejs process and spacetime db client
- the stdout of the python process is the state of the motors (rotation direction and speed) which is captured by the nodejs process and saved to spacetime db table 'microprocess_state' as simulated device state
- the Unity spacetime db client in Solarville/Assets/_Scripts/Spacetime/GameManager.cs listens for changes to simulated device state. It creates and binds a simulated device controller for each 'microprocess_state' spacetime db record

## 
Extend the React app in solarville_clients/react_client/src/App.tsx to implement a microprocess code editor like in Solarville/Assets/_Scripts/Spacetime/CodeEditorUI.cs. Also extend the microprocess simulation manager in solarville_clients/nodejs_client/src/index.js with an option to open a Python REPL process that has a REPL interface in the React app. 

##
create a Vue component in solarville_clients/react_client/src/components for a remote control interface to a simulated remote control car in Unity and the simulation of the remote control car's firmware in python. 

the Vue component listens for keyboard input (W/S for forward/reverse, A/D for steering left/right, space for break) and shows the active inputs in a toggled overlay view. the Vue component also provides touch compatible virtual joysticks (left joystick for forward/reverse, right joystick for steering left/right). 
all input listeners call this vue component is presented full screen from a new button in solarville_clients/react_client/src/App.tsx. provide a button to close the fullscreen view.

##
in solarville_clients/nodejs_client:
- create a new microprocess state in SpacetimeDB when REPL websocket connects
- delete the microprocess state when the REPL websocket disconnects
- on receiving message of simulated firmware in python process stdout, translate that message into updating the active microprocess state in spacetimeDB


#
Here is project context for creating a full-stack demo for integrating the following components of a robotics simulation controlled by player-defined Python scripts: 
- a SpacetimeDB server for syncing simulation state, script source code, and script execution process management
- interactive Unity game entities controlled by Python scripts, ex. player-controlled robot or AI robot
- static Unity game objects spawned/manipulated/destroyed by Python scripts, ex. obstacles, consumables
- a web app presented in a Unity-embedded web view in screen space overlay or external web browser, ex. remote control UI for any of the robots
- multiple web apps/pages presented in a Unity-embedded web view for simulating a display/touchscreen, ex. an LCD for robot's face or touchscreen for game world characters or in-world billboards or player's screen overlays showing simulation state
- a NodeJS server managing all the Python script processes

The player defines scripts that control all aspects of the simulation including:
- robot firmware on simulated microprocessor running Micropython
- setting up a simlulation scenario of dynamic and static game world entities
- managing the global simulation state for that scenario, ex. the rules and status of a minigame for robot racing or battle
- presenting UI in LVGL for Micropython for in-world displays or screen overlay widgets

With that project context, design a project structure for a monorepo implementing all the components. Add create the folders of the project structure within local path './monorepo'


#
how to setup a web app with SvelteKit, shadcn, playbook, and websocket client to manage the Python source code and multithreaded and/or multiprocess execution of that source code in the NodeJS server?

#
with the project scope defined at monorepo/docs/architecture/overview.md and the project structure at monorepo/ implement the following features across monorepo/node-server, monorepo/web-clients/creator-tools:
- in web app, provide views per Python source code and process to edit Python code executed by the NodeJS server, a Python REPL console to interact with the NodeJS-managed Python process over websocket, a reactive view of the wrapped and observed Python process global state 
- in web app, provide options for testing performance of managed Python processes:  to execute the source code in Pyodide with single thread and single process, in Python as single process per source file executed, or in Python as thread per source file executed in a single process with 
- in the NodeJS server, receive the different Python execution configurations from web app and spawn multiple processes or threads accordingly

##
update documentation at monorepo/README.md with instructions to run the web app at monorepo/web-clients/creator-tools and NodeJS server at monorepo/node-server for development and testing

##
update the implementation of SpacetimeDB client in monorepo/node-server/src/index.js to match the implementation at solarville_clients/nodejs_client/src/index.ts