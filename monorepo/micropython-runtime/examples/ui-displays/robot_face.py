# Robot Face Display Example
# This script demonstrates a simple robot face using LVGL for MicroPython

import lvgl as lv
import time
import math
import json

# Global variables
screen_width = 320
screen_height = 240
emotion = "neutral"  # Current emotion state
eye_blink_counter = 0
eye_blink_interval = 50  # Number of iterations between blinks
mouth_position = 0  # 0 to 100 (percentage of smile/frown)

# Colors
COLOR_BG = lv.color_hex(0x222222)  # Dark background
COLOR_FACE = lv.color_hex(0xFFA726)  # Orange
COLOR_EYE = lv.color_hex(0x42A5F5)  # Blue
COLOR_MOUTH = lv.color_hex(0xE0E0E0)  # Light grey

# LVGL objects
scr = None
left_eye = None
right_eye = None
mouth = None

def create_robot_face():
    """Create the robot face UI elements"""
    global scr, left_eye, right_eye, mouth
    
    # Create a screen
    scr = lv.scr_act()
    scr.set_style_bg_color(COLOR_BG, 0)
    
    # Create face background
    face_bg = lv.obj(scr)
    face_bg.set_size(240, 240)
    face_bg.center()
    face_bg.set_style_radius(120, 0)  # Make it circular
    face_bg.set_style_bg_color(COLOR_FACE, 0)
    face_bg.set_style_border_width(4, 0)
    face_bg.set_style_border_color(lv.color_hex(0xE65100), 0)  # Darker orange border
    
    # Create left eye
    left_eye = lv.obj(face_bg)
    left_eye.set_size(50, 50)
    left_eye.set_pos(60, 70)
    left_eye.set_style_radius(25, 0)  # Make it circular
    left_eye.set_style_bg_color(COLOR_EYE, 0)
    
    # Create right eye
    right_eye = lv.obj(face_bg)
    right_eye.set_size(50, 50)
    right_eye.set_pos(130, 70)
    right_eye.set_style_radius(25, 0)  # Make it circular
    right_eye.set_style_bg_color(COLOR_EYE, 0)
    
    # Create mouth (initially neutral)
    mouth = lv.line(face_bg)
    
    # Set neutral mouth points
    update_mouth(0)  # 0 = neutral

def update_mouth(position):
    """
    Update the mouth based on position
    
    Parameters:
    position (int): -100 to 100, where -100 is full frown, 0 is neutral, 100 is full smile
    """
    global mouth
    
    # Clamp position to -100 to 100 range
    position = max(-100, min(100, position))
    
    # Calculate mouth curve based on position
    center_y = 160
    curve_amount = position / 100 * 20  # Max curve is 20 pixels
    
    # Create mouth points
    points = [
        {"x": 70, "y": center_y + curve_amount},  # Left point
        {"x": 120, "y": center_y - curve_amount},  # Middle point
        {"x": 170, "y": center_y + curve_amount}   # Right point
    ]
    
    # Convert points to LVGL format
    lv_points = [lv.point_t({"x": p["x"], "y": p["y"]}) for p in points]
    
    # Set line points
    mouth.set_points(lv_points, len(lv_points))
    
    # Style the line
    mouth.set_style_line_width(5, 0)
    mouth.set_style_line_color(COLOR_MOUTH, 0)
    mouth.set_style_line_rounded(True, 0)

def blink_eyes():
    """Simulate eye blinking"""
    global left_eye, right_eye
    
    # Save original size
    orig_height = 50
    
    # Animate eyes closing
    for i in range(10):
        new_height = int(orig_height * (10 - i) / 10)
        left_eye.set_height(new_height)
        right_eye.set_height(new_height)
        left_eye.set_y(70 + (orig_height - new_height) // 2)
        right_eye.set_y(70 + (orig_height - new_height) // 2)
        time.sleep(0.01)
    
    # Brief pause with eyes closed
    time.sleep(0.05)
    
    # Animate eyes opening
    for i in range(10):
        new_height = int(orig_height * i / 10)
        left_eye.set_height(new_height)
        right_eye.set_height(new_height)
        left_eye.set_y(70 + (orig_height - new_height) // 2)
        right_eye.set_y(70 + (orig_height - new_height) // 2)
        time.sleep(0.01)

def set_emotion(new_emotion):
    """
    Set the robot's emotion
    
    Parameters:
    new_emotion (str): Emotion name ("happy", "sad", "surprised", "angry", "neutral")
    """
    global emotion, mouth_position
    
    emotion = new_emotion
    
    if emotion == "happy":
        mouth_position = 80
    elif emotion == "sad":
        mouth_position = -80
    elif emotion == "surprised":
        # Make eyes bigger for surprised
        left_eye.set_size(60, 60)
        left_eye.set_pos(55, 65)
        right_eye.set_size(60, 60)
        right_eye.set_pos(125, 65)
        mouth_position = 0  # 'O' shaped mouth for surprised
    elif emotion == "angry":
        mouth_position = -50
    else:  # neutral
        # Reset eye size
        left_eye.set_size(50, 50)
        left_eye.set_pos(60, 70)
        right_eye.set_size(50, 50)
        right_eye.set_pos(130, 70)
        mouth_position = 0
    
    # Update mouth
    update_mouth(mouth_position)
    
    # Output emotion state for external systems
    print(json.dumps({"emotion": emotion}))

def setup():
    """Initial setup"""
    # Create the robot face UI
    create_robot_face()
    
    # Set initial emotion
    set_emotion("neutral")
    
    print(json.dumps({"status": "Robot face initialized"}))

def loop():
    """Main loop function"""
    global eye_blink_counter
    
    # Increment the blink counter
    eye_blink_counter += 1
    
    # Blink periodically
    if eye_blink_counter >= eye_blink_interval:
        blink_eyes()
        eye_blink_counter = 0
    
    # Add subtle mouth movement based on time
    wave = math.sin(time.time() * 2) * 10
    update_mouth(mouth_position + wave)
    
    # Handle any incoming commands
    if lv.is_ui_update_needed():
        lv.ui_update()
    
    # Wait a bit to control animation speed
    # In a real micropython environment, we would use sleep to control the timing
    # Since this is simulated, the Node.js process will control the calling of loop()
    # time.sleep(0.1)  # 100ms delay (commented out as it's handled externally)

# Function to receive and process commands from external systems
def process_command(command_str):
    """
    Process a command received as a string
    
    Parameters:
    command_str (str): Command string in JSON format
    """
    try:
        command = json.loads(command_str)
        
        if "emotion" in command:
            set_emotion(command["emotion"])
        
        if "blink" in command and command["blink"]:
            blink_eyes()
        
        if "mouth_position" in command:
            update_mouth(command["mouth_position"])
            
    except Exception as e:
        print(json.dumps({"error": str(e)}))

# This function would be called by the communication system
def receive_external_command(command_str):
    process_command(command_str)

# Note: The setup() and loop() functions will be called by the micropython_runner.py script
# The setup() function is called once at the start
# The loop() function is called repeatedly
