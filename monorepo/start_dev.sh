#!/bin/bash

# Start dev script for the Robotics Simulation Platform
# This script starts all the necessary services for development

# Output colors
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}Starting Robotics Simulation Platform development environment...${NC}"

# Check if spacetimedb is installed
if ! command -v spacetime &> /dev/null
then
    echo -e "${RED}SpacetimeDB CLI not found. Please install it first.${NC}"
    echo "Visit: https://spacetimedb.com/docs/getting-started"
    exit 1
fi

# Check directories exist
if [ ! -d "spacetime-server" ] || [ ! -d "node-server" ] || [ ! -d "web-clients" ]; then
    echo -e "${RED}Required directories not found. Please run this script from the project root.${NC}"
    exit 1
fi

# Start SpacetimeDB server
echo -e "${YELLOW}Starting SpacetimeDB server...${NC}"
cd spacetime-server
spacetime serve &
SPACETIME_PID=$!
cd ..

# Give SpacetimeDB a moment to start
sleep 2
echo -e "${GREEN}SpacetimeDB server started with PID ${SPACETIME_PID}${NC}"

# Start Node.js Server
echo -e "${YELLOW}Starting Node.js server...${NC}"
cd node-server
npm start &
NODE_PID=$!
cd ..
echo -e "${GREEN}Node.js server started with PID ${NODE_PID}${NC}"

# Start Web Clients (development server)
echo -e "${YELLOW}Starting Web clients development server...${NC}"
cd web-clients/remote-control
npm start &
WEB_PID=$!
cd ../..
echo -e "${GREEN}Web clients development server started with PID ${WEB_PID}${NC}"

echo -e "${BLUE}All services started successfully!${NC}"
echo -e "${YELLOW}NOTE: Unity client must be started manually from the Unity Editor${NC}"
echo -e "Press Ctrl+C to stop all services"

# Handle clean shutdown
trap "kill $SPACETIME_PID $NODE_PID $WEB_PID; echo -e '${RED}All services stopped${NC}'; exit 0" INT

# Keep script running
wait
