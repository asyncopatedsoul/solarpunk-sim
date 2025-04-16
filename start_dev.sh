#!/bin/bash

# Start the Node.js client
cd solarville_clients/nodejs_client
npm run dev &
NODEJS_PID=$!

# Start the React client
cd ../react_client
npm run dev &
REACT_PID=$!

# Handle cleanup on exit
cleanup() {
  echo "Shutting down..."
  kill $NODEJS_PID
  kill $REACT_PID
  exit 0
}

trap cleanup INT TERM

# Keep script running
echo "Started both clients. Press Ctrl+C to stop."
wait
