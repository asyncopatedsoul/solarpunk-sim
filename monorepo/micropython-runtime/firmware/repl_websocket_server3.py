import asyncio
import websockets
import json

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


async def send_periodic_messages():
    """Sends a message to all clients every X seconds."""
    while True:
        message = {"type": "heartbeat", "time": time.time()}  # Include timestamp
        await broadcast_message(message)
        await asyncio.sleep(5)  # Send every 5 seconds (adjust as needed)


async def main():
    async with websockets.serve(handler, "localhost", 8765):
        print("WebSocket server started on ws://localhost:8765")
        await asyncio.gather(send_periodic_messages(), asyncio.Future())  # Run forever


if __name__ == "__main__":
    import asyncio
    import time  # Import the time module

    asyncio.run(main())