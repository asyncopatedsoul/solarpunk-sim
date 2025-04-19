import React, { useState } from 'react';
import { applyVueInReact } from 'veaury';
import RemoteControlCarComponent from './RemoteControlCar.vue';

// Use HOC 'applyVueInReact' to convert Vue component to React
const RemoteControlCar = applyVueInReact(RemoteControlCarComponent);

const RemoteControlCarWrapper: React.FC = () => {
  const [showFullscreen, setShowFullscreen] = useState(false);

  // Toggle fullscreen mode
  const toggleFullscreen = () => {
    setShowFullscreen(!showFullscreen);
  };

  // Handle close event from Vue component
  const handleClose = () => {
    setShowFullscreen(false);
  };

  // Styles
  const buttonStyle = {
    padding: '10px 20px',
    backgroundColor: '#4CAF50',
    color: 'white',
    border: 'none',
    borderRadius: '4px',
    cursor: 'pointer',
    margin: '10px 0',
    fontSize: '16px',
  };

  const wrapperStyle = {
    height: '100%',
    width: '100%',
  };

  // If fullscreen, render the component in fullscreen mode
  if (showFullscreen) {
    return (
      <RemoteControlCar 
        isFullscreen={true} 
        onClose={handleClose}
        style={wrapperStyle}
      />
    );
  }

  // Otherwise, render a button to open the fullscreen mode
  return (
    <div>
      <button 
        style={buttonStyle}
        onClick={toggleFullscreen}
      >
        Open Remote Control Car Interface
      </button>
    </div>
  );
};

export default RemoteControlCarWrapper;