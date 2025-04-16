import React, { useState, useEffect, useRef } from 'react';
import { io, Socket } from 'socket.io-client';
import { DbConnection } from '../module_bindings';

// Styles
const styles = {
  container: {
    display: 'flex',
    flexDirection: 'column' as const,
    height: '100%',
    border: '1px solid #ccc',
    borderRadius: '4px',
    overflow: 'hidden',
  },
  output: {
    flex: 1,
    backgroundColor: '#f0f0f0',
    padding: '10px',
    fontFamily: 'monospace',
    whiteSpace: 'pre-wrap',
    overflowY: 'auto' as const,
    margin: 0,
  },
  input: {
    display: 'flex',
    borderTop: '1px solid #ccc',
  },
  textarea: {
    flex: 1,
    padding: '8px',
    fontFamily: 'monospace',
    border: 'none',
    outline: 'none',
    resize: 'none',
  },
  button: {
    padding: '0 15px',
    backgroundColor: '#4CAF50',
    color: 'white',
    border: 'none',
    cursor: 'pointer',
  },
  disabled: {
    backgroundColor: '#cccccc',
    cursor: 'not-allowed',
  },
  motorControls: {
    display: 'flex',
    padding: '10px',
    borderTop: '1px solid #ccc',
    justifyContent: 'space-between',
  },
  controlGroup: {
    display: 'flex',
    flexDirection: 'column' as const,
    alignItems: 'center',
  },
  slider: {
    width: '100px',
    margin: '5px 0',
  },
  label: {
    fontFamily: 'sans-serif',
    fontSize: '12px',
  },
  value: {
    fontFamily: 'monospace',
    fontSize: '14px',
    fontWeight: 'bold',
  },
  saveButtonContainer: {
    padding: '10px',
    borderTop: '1px solid #ccc',
  },
  saveButton: {
    padding: '8px 15px',
    backgroundColor: '#2196F3',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    width: '100%',
  },
};

// Output line types
type OutputLine = {
  type: 'ready' | 'output' | 'error' | 'motor_state' | 'debug' | 'exit' | 'system';
  message: string;
  timestamp: number;
};

// Props for the component
interface ReplInterfaceProps {
  dbConnection: DbConnection | null;
  height?: string;
}

const REPL_SERVER_URL = 'http://localhost:3100';

const ReplInterface: React.FC<ReplInterfaceProps> = ({ dbConnection, height = '500px' }) => {
  const [socket, setSocket] = useState<Socket | null>(null);
  const [connected, setConnected] = useState(false);
  const [outputLines, setOutputLines] = useState<OutputLine[]>([
    { type: 'system', message: 'Connecting to Python REPL server...', timestamp: Date.now() },
  ]);
  const [inputValue, setInputValue] = useState('');
  const [leftMotor, setLeftMotor] = useState(0);
  const [rightMotor, setRightMotor] = useState(0);
  const [scriptName, setScriptName] = useState('My Script');
  const [currentCodeId, setCurrentCodeId] = useState<number | null>(null);
  const outputRef = useRef<HTMLPreElement>(null);

  // Auto-scroll output to bottom
  useEffect(() => {
    if (outputRef.current) {
      outputRef.current.scrollTop = outputRef.current.scrollHeight;
    }
  }, [outputLines]);

  // Connect to REPL server
  useEffect(() => {
    const newSocket = io(REPL_SERVER_URL);

    newSocket.on('connect', () => {
      setConnected(true);
      setOutputLines(prev => [...prev, {
        type: 'system',
        message: 'Connected to Python REPL server',
        timestamp: Date.now()
      }]);
    });

    newSocket.on('disconnect', () => {
      setConnected(false);
      setOutputLines(prev => [...prev, {
        type: 'system',
        message: 'Disconnected from Python REPL server',
        timestamp: Date.now()
      }]);
    });

    newSocket.on('repl_output', (data) => {
      setOutputLines(prev => [...prev, {
        type: data.type,
        message: data.type === 'motor_state' 
          ? `Motors: Left ${data.left_motor_speed.toFixed(2)}, Right ${data.right_motor_speed.toFixed(2)}`
          : data.message,
        timestamp: Date.now()
      }]);

      // Update motor sliders if motor state is received
      if (data.type === 'motor_state') {
        setLeftMotor(data.left_motor_speed);
        setRightMotor(data.right_motor_speed);
      }
    });

    setSocket(newSocket);

    return () => {
      newSocket.disconnect();
    };
  }, []);

  // Execute code
  const executeCode = () => {
    if (!socket || !connected || !inputValue.trim()) return;

    // Add the code to the output
    setOutputLines(prev => [...prev, {
      type: 'system',
      message: `>>> ${inputValue}`,
      timestamp: Date.now()
    }]);

    // Send the code to the server
    socket.emit('execute', { code: inputValue, codeId: currentCodeId });

    // Clear the input
    setInputValue('');
  };

  // Handle key press in textarea
  const handleKeyPress = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      executeCode();
    }
  };

  // Update motor values
  const updateMotors = () => {
    if (!socket || !connected) return;
    socket.emit('set_motors', { 
      left: leftMotor, 
      right: rightMotor, 
      codeId: currentCodeId 
    });
  };

  // Save script to database
  const saveScript = async () => {
    if (!dbConnection || !inputValue.trim()) {
      setOutputLines(prev => [...prev, {
        type: 'error',
        message: 'Cannot save: Not connected to database or empty script',
        timestamp: Date.now()
      }]);
      return;
    }

    try {
      const codeId = await dbConnection.reducers.save_microprocess_code(
        scriptName,
        `repl_script_${Date.now()}.py`,
        inputValue
      );

      setCurrentCodeId(codeId);
      setOutputLines(prev => [...prev, {
        type: 'system',
        message: `Script saved with ID: ${codeId}`,
        timestamp: Date.now()
      }]);
    } catch (error) {
      setOutputLines(prev => [...prev, {
        type: 'error',
        message: `Error saving script: ${error instanceof Error ? error.message : String(error)}`,
        timestamp: Date.now()
      }]);
    }
  };

  // Start the script
  const startScript = async () => {
    if (!dbConnection || currentCodeId === null) {
      setOutputLines(prev => [...prev, {
        type: 'error',
        message: 'Cannot start: Not connected to database or no script saved',
        timestamp: Date.now()
      }]);
      return;
    }

    try {
      await dbConnection.reducers.start_microprocess(currentCodeId);
      setOutputLines(prev => [...prev, {
        type: 'system',
        message: `Script started with ID: ${currentCodeId}`,
        timestamp: Date.now()
      }]);
    } catch (error) {
      setOutputLines(prev => [...prev, {
        type: 'error',
        message: `Error starting script: ${error instanceof Error ? error.message : String(error)}`,
        timestamp: Date.now()
      }]);
    }
  };

  // Stop the script
  const stopScript = async () => {
    if (!dbConnection || currentCodeId === null) {
      setOutputLines(prev => [...prev, {
        type: 'error',
        message: 'Cannot stop: Not connected to database or no script running',
        timestamp: Date.now()
      }]);
      return;
    }

    try {
      await dbConnection.reducers.stop_microprocess(currentCodeId);
      setOutputLines(prev => [...prev, {
        type: 'system',
        message: `Script stopped with ID: ${currentCodeId}`,
        timestamp: Date.now()
      }]);
    } catch (error) {
      setOutputLines(prev => [...prev, {
        type: 'error',
        message: `Error stopping script: ${error instanceof Error ? error.message : String(error)}`,
        timestamp: Date.now()
      }]);
    }
  };

  // Render with color-coded output
  const getLineStyle = (type: string) => {
    switch (type) {
      case 'error': return { color: 'red' };
      case 'ready': return { color: 'green', fontWeight: 'bold' };
      case 'system': return { color: 'blue', fontStyle: 'italic' };
      case 'debug': return { color: 'purple' };
      case 'exit': return { color: 'orange' };
      default: return {};
    }
  };

  return (
    <div style={{ ...styles.container, height }}>
      <pre
        ref={outputRef}
        style={styles.output}
      >
        {outputLines.map((line, i) => (
          <div key={i} style={getLineStyle(line.type)}>
            {line.message}
          </div>
        ))}
      </pre>
      
      <div style={styles.motorControls}>
        <div style={styles.controlGroup}>
          <div style={styles.label}>Left Motor</div>
          <input
            type="range"
            min="-1"
            max="1"
            step="0.1"
            value={leftMotor}
            onChange={(e) => setLeftMotor(parseFloat(e.target.value))}
            onMouseUp={updateMotors}
            style={styles.slider}
          />
          <div style={styles.value}>{leftMotor.toFixed(1)}</div>
        </div>
        
        <div style={styles.controlGroup}>
          <div style={styles.label}>Right Motor</div>
          <input
            type="range"
            min="-1"
            max="1"
            step="0.1"
            value={rightMotor}
            onChange={(e) => setRightMotor(parseFloat(e.target.value))}
            onMouseUp={updateMotors}
            style={styles.slider}
          />
          <div style={styles.value}>{rightMotor.toFixed(1)}</div>
        </div>
        
        <div style={styles.controlGroup}>
          <input
            type="text"
            value={scriptName}
            onChange={(e) => setScriptName(e.target.value)}
            placeholder="Script Name"
            style={{ margin: '5px 0' }}
          />
          <div style={{ display: 'flex', gap: '5px' }}>
            <button 
              onClick={saveScript} 
              style={styles.saveButton} 
              disabled={!connected || !inputValue.trim()}
            >
              Save
            </button>
            <button 
              onClick={startScript} 
              style={{...styles.saveButton, backgroundColor: '#4CAF50'}} 
              disabled={!connected || currentCodeId === null}
            >
              Start
            </button>
            <button 
              onClick={stopScript} 
              style={{...styles.saveButton, backgroundColor: '#f44336'}} 
              disabled={!connected || currentCodeId === null}
            >
              Stop
            </button>
          </div>
        </div>
      </div>
      
      <div style={styles.input}>
        <textarea
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyPress={handleKeyPress}
          placeholder="Enter Python code here..."
          style={styles.textarea}
          rows={5}
          disabled={!connected}
        />
        <button
          onClick={executeCode}
          style={{
            ...styles.button,
            ...((!connected || !inputValue.trim()) ? styles.disabled : {})
          }}
          disabled={!connected || !inputValue.trim()}
        >
          Run
        </button>
      </div>
    </div>
  );
};

export default ReplInterface;