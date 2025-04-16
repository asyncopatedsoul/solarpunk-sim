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