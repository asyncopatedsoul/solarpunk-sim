import socket
import json

def receive_snapshots():
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect(('localhost', 12345))
    buffer = b''
    while True:
        data = sock.recv(1024)
        if not data:
            break
        buffer += data
        while b'\n' in buffer:
            line, buffer = buffer.split(b'\n', 1)
            try:
                snapshot = json.loads(line.decode('utf-8'))
                print("--- Received Snapshot ---")
                print(json.dumps(snapshot, indent=4))
                print("--- End of Snapshot ---")
            except json.JSONDecodeError:
                print(f"Received non-JSON data: {line.decode('utf-8')}")

if __name__ == "__main__":
    receive_snapshots()