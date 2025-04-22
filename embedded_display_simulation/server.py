# Python side with Flask-SocketIO
from flask import Flask, render_template
from flask_socketio import SocketIO
import numpy as np
import base64
from PIL import Image
import io
import time
import threading

app = Flask(__name__)
socketio = SocketIO(app, cors_allowed_origins="*")

def generate_frame(width=128, height=64, frame_count=0):
    # Same frame generation function as before
    buffer = np.zeros((height, width), dtype=np.uint8)
    for x in range(width):
        y = int((height/2) + (height/3) * np.sin((x + frame_count) * 0.1))
        if 0 <= y < height:
            buffer[y, x] = 255
    return buffer

def background_thread():
    frame_count = 0
    while True:
        buffer = generate_frame(frame_count=frame_count)
        
        # Convert to base64 for sending
        img = Image.fromarray(buffer)
        buffered = io.BytesIO()
        img.save(buffered, format="PNG")
        img_str = base64.b64encode(buffered.getvalue()).decode('utf-8')
        
        socketio.emit('frame_update', {'image': img_str})
        frame_count += 1
        socketio.sleep(0.033)  # ~30 FPS

@app.route('/')
def index():
    return render_template('index.html')

@socketio.on('connect')
def handle_connect():
    # Start streaming thread for each new connection
    threading.Thread(target=background_thread).start()

if __name__ == '__main__':
    socketio.run(app, debug=True)