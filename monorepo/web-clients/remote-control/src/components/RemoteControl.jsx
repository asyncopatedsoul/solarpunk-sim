import React, { useState, useEffect } from 'react';
import { Client } from '@clockworklabs/spacetimedb-sdk';

import './RemoteControl.css';

const RemoteControl = ({ robotId }) => {
  // State
  const [connected, setConnected] = useState(false);
  const [client, setClient] = useState(null);
  const [leftMotorSpeed, setLeftMotorSpeed] = useState(0);
  const [rightMotorSpeed, setRightMotorSpeed] = useState(0);
  const [isManualControl, setIsManualControl] = useState(true);
  const [availableRobots, setAvailableRobots] = useState([]);
  const [selectedRobotId, setSelectedRobotId] = useState(robotId || null);
  const [connectionStatus, setConnectionStatus] = useState('Disconnected');

  // Initialize SpacetimeDB client
  useEffect(() => {
    const initializeClient = async () => {
      try {
        // Create a new client
        const newClient = new Client();
        
        // Set event handlers
        newClient.on('connect', () => {
          console.log('Connected to SpacetimeDB');
          setConnected(true);
          setConnectionStatus('Connected');
        });
        
        newClient.on('disconnect', () => {
          console.log('Disconnected from SpacetimeDB');
          setConnected(false);
          setConnectionStatus('Disconnected');
        });
        
        newClient.on('error', (error) => {
          console.error('SpacetimeDB client error:', error);
          setConnectionStatus(`Error: ${error.message}`);
        });
        
        // Connect to SpacetimeDB
        const serverAddress = process.env.REACT_APP_SPACETIME_ADDRESS || 'localhost:3000';
        const moduleName = process.env.REACT_APP_SPACETIME_MODULE || 'robotics-simulation-db';
        
        await newClient.connect({
          address: serverAddress,
          moduleName: moduleName,
          identity: localStorage.getItem('spacetimeIdentity')
            ? JSON.parse(localStorage.getItem('spacetimeIdentity'))
            : newClient.generateIdentity()
        });
        
        // Save identity
        localStorage.setItem('spacetimeIdentity', JSON.stringify(newClient.getIdentity()));
        
        // Set client
        setClient(newClient);
        
        // Register as remote control
        newClient.callReducer('register_player', {
          name: 'Remote Control Interface'
        });
        
        // Subscribe to robot table
        const robotTable = newClient.getTable('Robot');
        
        robotTable.onInsert((robot) => {
          setAvailableRobots(prevRobots => {
            // Check if robot already exists
            const exists = prevRobots.some(r => r.robotId === robot.robotId);
            
            if (exists) {
              return prevRobots;
            }
            
            return [...prevRobots, {
              robotId: robot.robotId,
              name: robot.name,
              type: robot.robotType
            }];
          });
        });
        
        robotTable.onUpdate((oldRobot, newRobot) => {
          setAvailableRobots(prevRobots => {
            return prevRobots.map(robot => {
              if (robot.robotId === newRobot.robotId) {
                return {
                  robotId: newRobot.robotId,
                  name: newRobot.name,
                  type: newRobot.robotType
                };
              }
              
              return robot;
            });
          });
        });
        
        robotTable.onDelete((robot) => {
          setAvailableRobots(prevRobots => {
            return prevRobots.filter(r => r.robotId !== robot.robotId);
          });
        });
        
        // Subscribe to microprocess state table
        const stateTable = newClient.getTable('MicroprocessState');
        
        stateTable.onInsert((state) => {
          if (state.codeId && selectedRobotId) {
            // Find the robot
            const robot = newClient.getTable('Robot').getRows()
              .find(r => r.robotId === selectedRobotId);
            
            // Check if this state is for our selected robot's controller
            if (robot && robot.controllerCodeId === state.codeId) {
              // Update motor speeds
              setLeftMotorSpeed(state.leftMotorSpeed);
              setRightMotorSpeed(state.rightMotorSpeed);
            }
          }
        });
        
        stateTable.onUpdate((oldState, newState) => {
          if (newState.codeId && selectedRobotId) {
            // Find the robot
            const robot = newClient.getTable('Robot').getRows()
              .find(r => r.robotId === selectedRobotId);
            
            // Check if this state is for our selected robot's controller
            if (robot && robot.controllerCodeId === newState.codeId) {
              // Update motor speeds
              setLeftMotorSpeed(newState.leftMotorSpeed);
              setRightMotorSpeed(newState.rightMotorSpeed);
            }
          }
        });
      } catch (error) {
        console.error('Error initializing SpacetimeDB client:', error);
        setConnectionStatus(`Initialization Error: ${error.message}`);
      }
    };
    
    initializeClient();
    
    // Clean up
    return () => {
      if (client) {
        client.disconnect();
      }
    };
  }, [robotId]);
  
  // Handle robot selection
  const handleRobotSelect = (event) => {
    const robotId = Number(event.target.value);
    setSelectedRobotId(robotId);
  };
  
  // Handle manual control toggle
  const handleControlModeToggle = () => {
    setIsManualControl(!isManualControl);
    
    if (isManualControl) {
      // Switching to automated mode, stop any manual control
      setLeftMotorSpeed(0);
      setRightMotorSpeed(0);
      sendMotorUpdate(0, 0);
    }
  };
  
  // Handle left motor speed change
  const handleLeftMotorChange = (event) => {
    const speed = parseFloat(event.target.value);
    setLeftMotorSpeed(speed);
    
    if (isManualControl) {
      sendMotorUpdate(speed, rightMotorSpeed);
    }
  };
  
  // Handle right motor speed change
  const handleRightMotorChange = (event) => {
    const speed = parseFloat(event.target.value);
    setRightMotorSpeed(speed);
    
    if (isManualControl) {
      sendMotorUpdate(leftMotorSpeed, speed);
    }
  };
  
  // Send motor update to SpacetimeDB
  const sendMotorUpdate = (leftSpeed, rightSpeed) => {
    if (!client || !connected || !selectedRobotId) {
      return;
    }
    
    try {
      // Get the robot to find its controller code ID
      const robot = client.getTable('Robot').getRows()
        .find(r => r.robotId === selectedRobotId);
      
      if (!robot || !robot.controllerCodeId) {
        console.warn('No controller code found for robot');
        return;
      }
      
      // Update the microprocess state
      client.callReducer('update_microprocess_state', {
        codeId: robot.controllerCodeId,
        leftMotorSpeed: leftSpeed,
        rightMotorSpeed: rightSpeed,
        errorMessage: '',
        isRunning: true
      });
    } catch (error) {
      console.error('Error updating motor speeds:', error);
    }
  };
  
  // Handle joystick input (simplified)
  const handleJoystickMove = (x, y) => {
    if (!isManualControl) return;
    
    // Convert joystick position to motor speeds
    // Forward/backward (y) affects both motors
    // Left/right (x) affects the difference between motors
    const left = y + x;
    const right = y - x;
    
    // Clamp values to [-1, 1]
    const leftClamped = Math.max(-1, Math.min(1, left));
    const rightClamped = Math.max(-1, Math.min(1, right));
    
    // Update state
    setLeftMotorSpeed(leftClamped);
    setRightMotorSpeed(rightClamped);
    
    // Send update
    sendMotorUpdate(leftClamped, rightClamped);
  };
  
  // Handle preset buttons
  const handlePreset = (preset) => {
    if (!isManualControl) return;
    
    let left = 0;
    let right = 0;
    
    switch (preset) {
      case 'forward':
        left = 0.7;
        right = 0.7;
        break;
      case 'backward':
        left = -0.7;
        right = -0.7;
        break;
      case 'left':
        left = -0.5;
        right = 0.5;
        break;
      case 'right':
        left = 0.5;
        right = -0.5;
        break;
      case 'stop':
      default:
        left = 0;
        right = 0;
        break;
    }
    
    // Update state
    setLeftMotorSpeed(left);
    setRightMotorSpeed(right);
    
    // Send update
    sendMotorUpdate(left, right);
  };
  
  return (
    <div className="remote-control">
      <div className="remote-control-header">
        <h1>Robot Remote Control</h1>
        <div className="connection-status">
          <span className={`status-dot ${connected ? 'connected' : 'disconnected'}`}></span>
          <span>{connectionStatus}</span>
        </div>
      </div>
      
      <div className="robot-selection">
        <label htmlFor="robot-select">Select Robot:</label>
        <select 
          id="robot-select" 
          value={selectedRobotId || ''}
          onChange={handleRobotSelect}
          disabled={!connected || availableRobots.length === 0}
        >
          <option value="">-- Select a Robot --</option>
          {availableRobots.map((robot) => (
            <option key={robot.robotId} value={robot.robotId}>
              {robot.name} ({robot.type})
            </option>
          ))}
        </select>
      </div>
      
      <div className="control-mode">
        <label>
          <input
            type="checkbox"
            checked={isManualControl}
            onChange={handleControlModeToggle}
            disabled={!connected || !selectedRobotId}
          />
          Manual Control
        </label>
      </div>
      
      <div className="control-panel">
        <div className="motor-controls">
          <div className="motor-control left">
            <label htmlFor="left-motor">Left Motor: {leftMotorSpeed.toFixed(2)}</label>
            <input
              id="left-motor"
              type="range"
              min="-1"
              max="1"
              step="0.1"
              value={leftMotorSpeed}
              onChange={handleLeftMotorChange}
              disabled={!connected || !selectedRobotId || !isManualControl}
            />
          </div>
          
          <div className="motor-control right">
            <label htmlFor="right-motor">Right Motor: {rightMotorSpeed.toFixed(2)}</label>
            <input
              id="right-motor"
              type="range"
              min="-1"
              max="1"
              step="0.1"
              value={rightMotorSpeed}
              onChange={handleRightMotorChange}
              disabled={!connected || !selectedRobotId || !isManualControl}
            />
          </div>
        </div>
        
        <div className="preset-buttons">
          <button 
            onClick={() => handlePreset('forward')}
            disabled={!connected || !selectedRobotId || !isManualControl}
          >
            Forward
          </button>
          <button 
            onClick={() => handlePreset('backward')}
            disabled={!connected || !selectedRobotId || !isManualControl}
          >
            Backward
          </button>
          <button 
            onClick={() => handlePreset('left')}
            disabled={!connected || !selectedRobotId || !isManualControl}
          >
            Turn Left
          </button>
          <button 
            onClick={() => handlePreset('right')}
            disabled={!connected || !selectedRobotId || !isManualControl}
          >
            Turn Right
          </button>
          <button 
            onClick={() => handlePreset('stop')}
            disabled={!connected || !selectedRobotId || !isManualControl}
            className="stop-button"
          >
            STOP
          </button>
        </div>
      </div>
      
      <div className="joystick-control">
        <div 
          className="joystick"
          // A real implementation would use a proper joystick component
          // This is just a placeholder for the structure
        >
          <div className="joystick-handle"></div>
        </div>
      </div>

      <div className="robot-status">
        <h2>Robot Status</h2>
        <div className="status-info">
          <p>Left Motor: {leftMotorSpeed.toFixed(2)}</p>
          <p>Right Motor: {rightMotorSpeed.toFixed(2)}</p>
          <p>Control Mode: {isManualControl ? 'Manual' : 'Automated'}</p>
        </div>
      </div>
    </div>
  );
};

export default RemoteControl;
