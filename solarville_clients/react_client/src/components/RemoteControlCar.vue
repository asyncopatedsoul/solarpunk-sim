<template>
  <div class="remote-control-container" :class="{ 'fullscreen': isFullscreen }">
    <div class="car-controller">
      <!-- Control status overlay -->
      <div v-if="showOverlay" class="control-overlay">
        <div class="key-indicators">
          <div class="key-row">
            <div class="key-indicator" :class="{ active: controls.forward }">W</div>
          </div>
          <div class="key-row">
            <div class="key-indicator" :class="{ active: controls.left }">A</div>
            <div class="key-indicator" :class="{ active: controls.backward }">S</div>
            <div class="key-indicator" :class="{ active: controls.right }">D</div>
          </div>
          <div class="key-row">
            <div class="key-indicator brake" :class="{ active: controls.brake }">SPACE</div>
          </div>
        </div>
      </div>

      <!-- Virtual joysticks for touch devices -->
      <div class="joysticks-container">
        <div ref="leftJoystick" class="joystick left-joystick">
          <div class="joystick-base">
            <div class="joystick-thumb" :style="leftJoystickThumbStyle"></div>
          </div>
        </div>
        <div ref="rightJoystick" class="joystick right-joystick">
          <div class="joystick-base">
            <div class="joystick-thumb" :style="rightJoystickThumbStyle"></div>
          </div>
        </div>
      </div>

      <!-- UI Controls -->
      <div class="control-panel">
        <button class="control-button" @click="toggleOverlay">
          {{ showOverlay ? 'Hide Controls' : 'Show Controls' }}
        </button>
        <div class="motor-status">
          <div>Left Motor: {{ leftMotorSpeed.toFixed(2) }}</div>
          <div>Right Motor: {{ rightMotorSpeed.toFixed(2) }}</div>
        </div>
        <button v-if="isFullscreen" class="close-button" @click="closeFullscreen">
          Close
        </button>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue';
import { socket as replSocket } from '../socket';

export default {
  name: 'RemoteControlCar',
  props: {
    isFullscreen: {
      type: Boolean,
      default: false,
    },
  },
  emits: ['close'],
  setup(props, { emit }) {
    const showOverlay = ref(true);
    const leftJoystick = ref(null);
    const rightJoystick = ref(null);
    const leftJoystickActive = ref(false);
    const rightJoystickActive = ref(false);
    const leftJoystickPos = reactive({ x: 0, y: 0 });
    const rightJoystickPos = reactive({ x: 0, y: 0 });
    const leftMotorSpeed = ref(0);
    const rightMotorSpeed = ref(0);
    
    const controls = reactive({
      forward: false,
      backward: false,
      left: false,
      right: false,
      brake: false,
    });

    // Computed styles for joystick thumbs
    const leftJoystickThumbStyle = computed(() => ({
      transform: `translate(${leftJoystickPos.x}px, ${leftJoystickPos.y}px)`,
      opacity: leftJoystickActive.value ? 1 : 0.5,
    }));

    const rightJoystickThumbStyle = computed(() => ({
      transform: `translate(${rightJoystickPos.x}px, ${rightJoystickPos.y}px)`,
      opacity: rightJoystickActive.value ? 1 : 0.5,
    }));

    // Toggle control overlay
    const toggleOverlay = () => {
      showOverlay.value = !showOverlay.value;
    };

    // Close fullscreen mode
    const closeFullscreen = () => {
      emit('close');
    };

    // Update motor speeds based on controls
    const updateMotorSpeeds = () => {
      // Using keyboard controls
      if (controls.forward) {
        leftMotorSpeed.value = 1;
        rightMotorSpeed.value = 1;
      } else if (controls.backward) {
        leftMotorSpeed.value = -1;
        rightMotorSpeed.value = -1;
      } else if (!controls.forward && !controls.backward) {
        // Reset when no forward/backward keys are pressed
        leftMotorSpeed.value = 0;
        rightMotorSpeed.value = 0;
      }

      if (controls.left) {
        leftMotorSpeed.value -= 0.5;
        rightMotorSpeed.value += 0.5;
      } else if (controls.right) {
        leftMotorSpeed.value += 0.5;
        rightMotorSpeed.value -= 0.5;
      }

      if (controls.brake) {
        leftMotorSpeed.value = 0;
        rightMotorSpeed.value = 0;
      }

      // Using joysticks (if active)
      if (leftJoystickActive.value) {
        // Left joystick controls forward/backward
        const forwardValue = -leftJoystickPos.y / 50; // Normalized to -1 to 1
        leftMotorSpeed.value = forwardValue;
        rightMotorSpeed.value = forwardValue;
      }

      if (rightJoystickActive.value) {
        // Right joystick controls steering
        const steeringValue = rightJoystickPos.x / 50; // Normalized to -1 to 1
        leftMotorSpeed.value += steeringValue;
        rightMotorSpeed.value -= steeringValue;
      }

      // Clamp values between -1 and 1
      leftMotorSpeed.value = Math.max(-1, Math.min(1, leftMotorSpeed.value));
      rightMotorSpeed.value = Math.max(-1, Math.min(1, rightMotorSpeed.value));

      // Send motor values to server
      sendMotorValues();
    };

    // Send motor values to server
    const sendMotorValues = () => {
      if (replSocket.connected) {
        replSocket.emit('set_motors', {
          left: leftMotorSpeed.value,
          right: rightMotorSpeed.value,
          codeId: null // Use null for direct control
        });
      }
    };

    // Keyboard event handlers
    const handleKeyDown = (e) => {
      if (e.repeat) return; // Prevent key repeat
      
      switch (e.key.toLowerCase()) {
        case 'w':
          controls.forward = true;
          break;
        case 's':
          controls.backward = true;
          break;
        case 'a':
          controls.left = true;
          break;
        case 'd':
          controls.right = true;
          break;
        case ' ':
          controls.brake = true;
          break;
      }
      
      updateMotorSpeeds();
    };

    const handleKeyUp = (e) => {
      switch (e.key.toLowerCase()) {
        case 'w':
          controls.forward = false;
          break;
        case 's':
          controls.backward = false;
          break;
        case 'a':
          controls.left = false;
          break;
        case 'd':
          controls.right = false;
          break;
        case ' ':
          controls.brake = false;
          break;
      }
      
      updateMotorSpeeds();
    };

    // Joystick handlers
    const setupJoysticks = () => {
      if (!leftJoystick.value || !rightJoystick.value) return;

      // Left joystick (throttle)
      const leftBase = leftJoystick.value.querySelector('.joystick-base');
      const leftThumb = leftJoystick.value.querySelector('.joystick-thumb');
      
      const setupLeftJoystick = () => {
        const baseRect = leftBase.getBoundingClientRect();
        const centerX = baseRect.width / 2;
        const centerY = baseRect.height / 2;
        
        // Touch start
        leftBase.addEventListener('touchstart', (e) => {
          e.preventDefault();
          leftJoystickActive.value = true;
          
          const touch = e.touches[0];
          const rect = leftBase.getBoundingClientRect();
          const x = touch.clientX - rect.left - centerX;
          const y = touch.clientY - rect.top - centerY;
          
          updateLeftJoystick(x, y);
        });
        
        // Touch move
        leftBase.addEventListener('touchmove', (e) => {
          e.preventDefault();
          
          const touch = e.touches[0];
          const rect = leftBase.getBoundingClientRect();
          const x = touch.clientX - rect.left - centerX;
          const y = touch.clientY - rect.top - centerY;
          
          updateLeftJoystick(x, y);
        });
        
        // Touch end
        leftBase.addEventListener('touchend', () => {
          leftJoystickActive.value = false;
          leftJoystickPos.x = 0;
          leftJoystickPos.y = 0;
          updateMotorSpeeds();
        });
      };
      
      // Right joystick (steering)
      const rightBase = rightJoystick.value.querySelector('.joystick-base');
      const rightThumb = rightJoystick.value.querySelector('.joystick-thumb');
      
      const setupRightJoystick = () => {
        const baseRect = rightBase.getBoundingClientRect();
        const centerX = baseRect.width / 2;
        const centerY = baseRect.height / 2;
        
        // Touch start
        rightBase.addEventListener('touchstart', (e) => {
          e.preventDefault();
          rightJoystickActive.value = true;
          
          const touch = e.touches[0];
          const rect = rightBase.getBoundingClientRect();
          const x = touch.clientX - rect.left - centerX;
          const y = touch.clientY - rect.top - centerY;
          
          updateRightJoystick(x, y);
        });
        
        // Touch move
        rightBase.addEventListener('touchmove', (e) => {
          e.preventDefault();
          
          const touch = e.touches[0];
          const rect = rightBase.getBoundingClientRect();
          const x = touch.clientX - rect.left - centerX;
          const y = touch.clientY - rect.top - centerY;
          
          updateRightJoystick(x, y);
        });
        
        // Touch end
        rightBase.addEventListener('touchend', () => {
          rightJoystickActive.value = false;
          rightJoystickPos.x = 0;
          rightJoystickPos.y = 0;
          updateMotorSpeeds();
        });
      };
      
      setupLeftJoystick();
      setupRightJoystick();
    };
    
    // Update joystick positions with constraints
    const updateLeftJoystick = (x, y) => {
      const radius = 50;
      const distance = Math.sqrt(x * x + y * y);
      
      if (distance > radius) {
        const angle = Math.atan2(y, x);
        x = radius * Math.cos(angle);
        y = radius * Math.sin(angle);
      }
      
      leftJoystickPos.x = x;
      leftJoystickPos.y = y;
      updateMotorSpeeds();
    };
    
    const updateRightJoystick = (x, y) => {
      const radius = 50;
      const distance = Math.sqrt(x * x + y * y);
      
      if (distance > radius) {
        const angle = Math.atan2(y, x);
        x = radius * Math.cos(angle);
        y = radius * Math.sin(angle);
      }
      
      rightJoystickPos.x = x;
      rightJoystickPos.y = y;
      updateMotorSpeeds();
    };

    // Connect to socket server
    const connectToServer = () => {
      if (!replSocket.connected) {
        replSocket.connect();
      }
    };

    onMounted(() => {
      // Set up keyboard listeners
      window.addEventListener('keydown', handleKeyDown);
      window.addEventListener('keyup', handleKeyUp);
      
      // Connect to server
      connectToServer();
      
      // Setup joysticks after component mounted
      setupJoysticks();
    });

    onUnmounted(() => {
      // Clean up event listeners
      window.removeEventListener('keydown', handleKeyDown);
      window.removeEventListener('keyup', handleKeyUp);
      
      // Reset motor values on unmount
      leftMotorSpeed.value = 0;
      rightMotorSpeed.value = 0;
      sendMotorValues();
    });

    return {
      showOverlay,
      toggleOverlay,
      closeFullscreen,
      controls,
      leftJoystick,
      rightJoystick,
      leftJoystickThumbStyle,
      rightJoystickThumbStyle,
      leftMotorSpeed,
      rightMotorSpeed,
    };
  }
};
</script>

<style scoped>
.remote-control-container {
  width: 100%;
  height: 100%;
  display: flex;
  flex-direction: column;
  background-color: #212121;
  color: white;
  position: relative;
}

.remote-control-container.fullscreen {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1000;
}

.car-controller {
  flex: 1;
  display: flex;
  flex-direction: column;
  position: relative;
  overflow: hidden;
}

.control-overlay {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background-color: rgba(0, 0, 0, 0.7);
  border-radius: 12px;
  padding: 20px;
  z-index: 100;
  transition: opacity 0.3s;
}

.key-indicators {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.key-row {
  display: flex;
  justify-content: center;
  gap: 10px;
}

.key-indicator {
  width: 60px;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #333;
  border-radius: 8px;
  font-weight: bold;
  font-size: 18px;
  box-shadow: 0 4px 0 #222;
  transition: all 0.1s;
}

.key-indicator.active {
  background-color: #4CAF50;
  transform: translateY(2px);
  box-shadow: 0 2px 0 #2E7D32;
}

.key-indicator.brake {
  width: 180px;
}

.key-indicator.brake.active {
  background-color: #F44336;
  box-shadow: 0 2px 0 #C62828;
}

.joysticks-container {
  display: flex;
  justify-content: space-between;
  position: absolute;
  bottom: 100px;
  left: 0;
  right: 0;
  padding: 0 30px;
  height: 120px;
}

.joystick {
  width: 120px;
  height: 120px;
  position: relative;
}

.joystick-base {
  width: 100%;
  height: 100%;
  background-color: rgba(255, 255, 255, 0.1);
  border-radius: 50%;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
}

.joystick-thumb {
  width: 60px;
  height: 60px;
  background-color: rgba(255, 255, 255, 0.8);
  border-radius: 50%;
  position: absolute;
  transition: opacity 0.3s;
}

.control-panel {
  padding: 15px;
  background-color: #333;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.control-button {
  padding: 10px 15px;
  background-color: #4CAF50;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.close-button {
  padding: 10px 15px;
  background-color: #F44336;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.motor-status {
  display: flex;
  flex-direction: column;
  align-items: center;
  font-family: monospace;
  font-size: 14px;
}

@media (min-width: 768px) {
  .joysticks-container {
    opacity: 0.5;
  }
}

@media (max-width: 767px) {
  .control-overlay {
    display: none;
  }
}
</style>