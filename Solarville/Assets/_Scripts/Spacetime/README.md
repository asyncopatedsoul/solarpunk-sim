# Solarville Multiplayer Character Sync System

This implementation provides multiplayer character synchronization using Spacetime DB. It allows multiple players to connect to a shared world and see each other moving in real-time.

## Setup Instructions

### 1. Server Setup

Before running the Unity client, you need to start the Spacetime DB server:

1. Navigate to the `Solarville_server` directory
2. Run the following commands:

```bash
cargo generate   # Generate necessary files
spacetime serve  # Start the local server
```

The server will start on `http://127.0.0.1:3000` by default.

### 2. Unity Client Setup

1. Open the Solarville Unity project
2. Make sure you have the following prefabs created:
   - `PlayerCharacter` prefab with the `PlayerController` component attached
   - A simple UI for connecting (or use the `NetworkManager` script's UI features)

3. Set up a new scene with:
   - Add a GameObject with the `GameManager` component
   - Add a GameObject with the `NetworkManager` component
   - Add a Camera GameObject with the `CameraController` component

4. Configure the components:
   - In `GameManager`, set the server URL and module name (default: `http://127.0.0.1:3000` and `solarville_server`)
   - In `GameManager`, assign the player prefab and a container transform for player instances
   - In `NetworkManager`, hook up the UI elements and reference the `GameManager`
   - In `CameraController`, you can leave the target blank as it will automatically find the local player

### 3. Testing the Multiplayer System

1. Build the Unity project twice to create two separate executables (or run multiple instances of the editor)
2. Launch both instances
3. In each instance, enter a different player name and connect
4. You should see both player characters in the game world, with each instance controlling one character
5. Movement of both characters should be synchronized across instances

## How It Works

1. **Connection**: When the game starts, it attempts to connect to the Spacetime DB server
2. **Registration**: Upon connection, the player registers with a name and initial position
3. **Synchronization**: Local player movements are sent to the server at regular intervals
4. **Updates**: All connected clients receive player position updates from the server
5. **Interpolation**: Remote players are smoothly interpolated between update positions for fluid movement

## Components

- **GameManager**: Handles connection to Spacetime DB and manages the player list
- **PlayerController**: Controls player movement and synchronizes position with the server
- **NetworkManager**: Provides UI for connection and name entry
- **CameraController**: Follows the local player with smooth camera movement
- **AuthToken**: Helper class for managing authentication tokens

## Known Limitations

- No prediction or lag compensation (this is a simple implementation)
- Limited error handling for network failures
- No automatic reconnection if the connection is lost
- Player prefab must be manually created in Unity