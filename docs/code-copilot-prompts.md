let's create a small test for multiplayer character sync through Spacetime DB.

using the reference implementation of Unity and Spacetime DB in 'reference-implementations/Blackholio', implement this feature in the Unity project 'Solarville' and a Spacetime DB server written in Rust in 'Solarville_server':
- in the folder 'Assets/_Scripts' create Spacetime client scripts and player controller scripts that synchronize the 3d movement of a player character prefab. See these scripts as a reference:
    - 'reference-implementations/Blackholio/client-unity/Assets/Scripts/GameManager.cs'
    - 'reference-implementations/Blackholio/client-unity/Assets/Scripts/PlayerController.cs'
- in the folder 'Solarville_server', implement the Spactime DB service to synchronize 3d movement in Rust. See these scripts as reference:
    - reference-implementations/Blackholio/server-rust/src/lib.rs