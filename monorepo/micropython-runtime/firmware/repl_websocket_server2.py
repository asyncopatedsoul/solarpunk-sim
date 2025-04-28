import asyncio
import websockets
import json
import code  # For the interactive console
import traceback

# Global dictionary to store connected clients
connected_clients = {}


async def send_message_to_client(websocket, message):
    """Sends a JSON message to a specific client."""
    try:
        await websocket.send(json.dumps(message))
    except (websockets.exceptions.ConnectionClosedOK, websockets.exceptions.ConnectionClosedError):
        print(f"Connection closed while trying to send: {message}")


async def broadcast_message(message):
    """Sends a JSON message to all connected clients."""
    for client in connected_clients:
        await send_message_to_client(connected_clients[client], message)


async def handler(websocket):
    """Handles WebSocket connections and messages."""
    client_id = id(websocket)  # Unique ID for each client
    connected_clients[client_id] = websocket
    print(f"Client {client_id} connected")

    try:
        # Send a connection confirmation
        await send_message_to_client(websocket, {"type": "connected", "message": "Welcome to the server!"})

        async for message in websocket:
            try:
                data = json.loads(message)
                print(f"Received from {client_id}: {data}")
                # Echo the received message (for demonstration)
                await send_message_to_client(websocket, {"type": "echo", "data": data})

            except json.JSONDecodeError:
                print(f"Invalid JSON received from {client_id}: {message}")
                await send_message_to_client(websocket, {"type": "error", "message": "Invalid JSON format"})

    except (websockets.exceptions.ConnectionClosedOK, websockets.exceptions.ConnectionClosedError):
        print(f"Client {client_id} disconnected")
    except Exception as e:
        print(f"Error handling connection with {client_id}: {e}")
        traceback.print_exc()  # Print detailed error info
    finally:
        if client_id in connected_clients:
            del connected_clients[client_id]


# --- Interactive Console Functions ---
def send_to_all(message):
    """Sends a message to all connected clients."""
    asyncio.create_task(broadcast_message({"type": "server_message", "message": message}))
    print(f"(Console) Sent to all: {message}")


def send_to_one(client_id, message):
    """Sends a message to a specific client (by ID)."""
    if client_id in connected_clients:
        asyncio.create_task(send_message_to_client(connected_clients[client_id], {"type": "server_message", "message": message}))
        print(f"(Console) Sent to {client_id}: {message}")
    else:
        print(f"(Console) Client {client_id} not found.")


def list_clients():
    """Lists all connected client IDs."""
    print("(Console) Connected Clients:", list(connected_clients.keys()))


# --- Main Function and Setup ---
async def main():
    print("Starting WebSocket server...")
    async with websockets.serve(handler, "localhost", 8765):
        print("WebSocket server started on ws://localhost:8765")

        # Set up the interactive console's namespace
        console_locals = {
            "send_to_all": send_to_all,
            "send_to_one": send_to_one,
            "list_clients": list_clients,
            "clients": connected_clients,  # Provide access to the clients dict
            "asyncio": asyncio,  # Give console access to asyncio
        }
        console = code.InteractiveConsole(locals=console_locals)
        console.interact("WebSocket Server Console")  # Start the console


if __name__ == "__main__":
    asyncio.run(main())