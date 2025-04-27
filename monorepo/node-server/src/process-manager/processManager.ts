import { PythonShell } from 'python-shell';
import path from 'path';
import fs from 'fs';
// import WebSocket from 'ws';
import * as WebSocket from 'ws';
import { Server, Socket } from 'socket.io';
import { DbConnection } from '../module_bindings';
import http from 'http';

// Define interfaces
interface ProcessStatus {
    process: PythonShell | null;
    status: 'running' | 'stopped' | 'error';
    codeId: number;
    startTime: number;
    lastUpdateTime: number;
    error: string | null;
    leftMotorSpeed: number;
    rightMotorSpeed: number;
    executionMode: string;
    code?: string;
}

interface CodeData {
    codeId: number;
    name: string;
    codeContent: string;
    filePath: string;
}

interface WebSocketMessage {
    type: string;
    [key: string]: any;
}

interface ProcessStatusResponse {
    codeId: number;
    status: 'running' | 'stopped' | 'error';
    startTime: number;
    lastUpdateTime: number;
    error: string | null;
    leftMotorSpeed: number;
    rightMotorSpeed: number;
    executionMode: string;
}

// Execution modes
export const EXECUTION_MODES = {
    SINGLE_PROCESS: 'single_process',
    MULTI_PROCESS: 'multi_process',
    MULTI_THREAD: 'multi_thread',
    PYODIDE: 'pyodide'
};

class ProcessManager {
    // Class properties
    public spacetimeClient: DbConnection | null;
    private processes: Map<number, ProcessStatus>;
    private micropythonRuntimePath: string;
    private websocketClients: Map<string, WebSocket>;
    private globalState: Map<number, any>;

    constructor(spacetimeClient: DbConnection | null) {
        this.spacetimeClient = spacetimeClient;
        this.processes = new Map(); // Map of codeId -> { process, status }
        this.micropythonRuntimePath = path.join(__dirname, '../../../micropython-runtime/firmware');
        this.websocketClients = new Map(); // Map of clientId -> websocket
        this.globalState = new Map(); // Map of processId -> global state
    }

    /**
     * Initialize websocket server for Python REPL and process state updates
     * @param server - HTTP server instance to attach the WebSocket server to
     */
    initializeWebsocketServer(server: http.Server): void {
        // WebSocket.Server is directly accessible from the WebSocket object
        // @ts-ignore: TypeScript doesn't properly recognize WebSocket.Server
        const wss = new WebSocket.Server({ noServer: false, server });

        wss.on('connection', (ws: WebSocket) => {
            const clientId = Date.now().toString();
            this.websocketClients.set(clientId, ws);
            console.log(`Client connected: ${clientId}`);
            console.log(`Websocket clients: ${this.websocketClients.size}`);

            ws.on('message', async (message: Buffer | string) => {
                try {
                    const data = JSON.parse(message.toString()) as WebSocketMessage;

                    if (data.type === 'repl_command') {
                        // Handle REPL command
                        if (this.processes.has(data.codeId)) {
                            const processStatus = this.processes.get(data.codeId);
                            if (processStatus && processStatus.process) {
                                processStatus.process.send(JSON.stringify({
                                    type: 'repl_command',
                                    command: data.command
                                }));
                            } else {
                                ws.send(JSON.stringify({
                                    type: 'repl_response',
                                    error: 'Process is not running'
                                }));
                            }
                        } else {
                            ws.send(JSON.stringify({
                                type: 'repl_response',
                                error: `No process found for code ${data.codeId}`
                            }));
                        }
                    }
                } catch (error) {
                    console.error('Error processing WebSocket message:', error);
                    ws.send(JSON.stringify({
                        type: 'error',
                        message: error instanceof Error ? error.message : String(error)
                    }));
                }
            });

            ws.on('close', () => {
                this.websocketClients.delete(clientId);
            });

            // Send the current state of all processes
            const initialState: Record<number, ProcessStatusResponse> = {};
            for (const [codeId, _] of this.processes) {
                initialState[codeId] = this.getProcessStatus(codeId);
            }

            ws.send(JSON.stringify({
                type: 'initial_state',
                state: initialState
            }));
        });
    }

    /**
     * Broadcast process status updates to all connected WebSocket clients
     * @param codeId - The ID of the code
     * @param status - The status to broadcast
     */
    broadcastStatus(codeId: number, status: ProcessStatusResponse): void {
        for (const ws of this.websocketClients.values()) {
            if (ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify({
                    type: 'process_status',
                    codeId,
                    status
                }));
            }
        }
    }

    /**
     * Broadcast Python REPL output to all connected WebSocket clients
     * @param codeId - The ID of the code
     * @param output - The output to broadcast
     */
    broadcastReplOutput(codeId: number, output: string): void {
        for (const ws of this.websocketClients.values()) {
            if (ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify({
                    type: 'repl_output',
                    codeId,
                    output
                }));
            }
        }
    }

    /**
     * Broadcast global state updates to all connected WebSocket clients
     * @param codeId - The ID of the code
     * @param state - The global state to broadcast
     */
    broadcastGlobalState(codeId: number, state: any): void {
        this.globalState.set(codeId, state);

        for (const ws of this.websocketClients.values()) {
            if (ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify({
                    type: 'global_state',
                    codeId,
                    state
                }));
            }
        }
    }

    /**
     * Get code content from SpacetimeDB
     * @param codeId - The ID of the code to get
     * @returns The code object
     */
    async getCodeFromDB(codeId: number): Promise<CodeData> {
        if (!this.spacetimeClient) {
            throw new Error('Not connected to SpacetimeDB');
        }

        // @ts-ignore: Property access is not properly typed
        const code = this.spacetimeClient.db.microprocessCode.codeId.find(codeId);
        if (!code) {
            throw new Error(`Code with ID ${codeId} not found`);
        }

        return {
            codeId: code.codeId,
            name: code.name,
            codeContent: code.codeContent,
            filePath: code.filePath
        };
    }

    /**
     * Update the microprocess state in SpacetimeDB
     */
    async updateMicroprocessState(
        codeId: number,
        leftMotorSpeed: number,
        rightMotorSpeed: number,
        errorMessage: string,
        isRunning: boolean,
        executionMode: string
    ): Promise<void> {
        if (!this.spacetimeClient) {
            throw new Error('Not connected to SpacetimeDB');
        }

        try {
            // @ts-ignore: Method access is not properly typed
            await this.spacetimeClient.reducers.updateMicroprocessState(
                codeId,
                leftMotorSpeed,
                rightMotorSpeed,
                errorMessage,
                isRunning,
                executionMode
            );
        } catch (error) {
            console.error(`Failed to update microprocess state for code ${codeId}:`, error);
        }
    }

    /**
     * Start a Python process for a given code ID with specified execution mode
     * @param codeId - The ID of the code to run
     * @param executionMode - The execution mode to use (single_process, multi_process, multi_thread, pyodide)
     * @returns The process ID
     */
    async startProcess(codeId: number, executionMode: string = EXECUTION_MODES.SINGLE_PROCESS): Promise<number> {
        console.log(`Starting process for code ${codeId} with execution mode ${executionMode}`);
        // Check if process is already running
        if (this.processes.has(codeId) && this.processes.get(codeId)?.process) {
            throw new Error(`Process for code ${codeId} is already running`);
        }

        try {
            // Get the code from SpacetimeDB
            //   const code = await this.getCodeFromDB(codeId);
            let code = {}
            let codeContent = `# Python Execution Demo
# This is a simple example of Python code that can be executed

# Global variables that will be tracked
left_motor_speed = 0.5
right_motor_speed = 0.25
sensor_data = {"temperature": 22.5, "humidity": 65}
robot_state = "running"

# Define a simple function
def calculate_average(a, b):
    return (a + b) / 2

def set_motor_speed(left_speed, right_speed):
    global left_motor_speed, right_motor_speed
    left_motor_speed = left_speed
    right_motor_speed = right_speed

# Main function that will be executed
def main():
    global left_motor_speed, right_motor_speed
    
    # Some calculation example
    left_motor_speed = calculate_average(0.3, 0.7)
    right_motor_speed = calculate_average(0.1, 0.4)
    
    # Print some output for the REPL
    print(f"Motors set to: L={left_motor_speed}, R={right_motor_speed}")
    print(f"Current sensor data: {sensor_data}")
    
    # You can see this value in the Global State viewer
    result = [i * i for i in range(5)]
    print(f"Calculated squares: {result}")
`;

            if (!code) {
                throw new Error(`Code with ID ${codeId} not found`);
            }

            // Create a temporary file with the code
            const tempFilePath = path.join(__dirname, `../../scripts/temp/${codeId}.py`);
            fs.mkdirSync(path.dirname(tempFilePath), { recursive: true });
            // fs.writeFileSync(tempFilePath, code.codeContent);
            fs.writeFileSync(tempFilePath, codeContent);


            // Set up the Python options
            const options = {
                mode: 'text' as const,
                pythonPath: 'python3',
                pythonOptions: ['-u'], // Unbuffered output
                scriptPath: this.micropythonRuntimePath,
                args: [tempFilePath, executionMode]
            };
            console.log(`Options: ${JSON.stringify(options)}`);

            // For Pyodide execution, we don't start a Python process
            if (executionMode === EXECUTION_MODES.PYODIDE) {
                // For Pyodide, we'll just store the code and let the web client handle execution
                const processStatus: ProcessStatus = {
                    process: null,
                    status: 'stopped',
                    codeId,
                    startTime: Date.now(),
                    lastUpdateTime: Date.now(),
                    error: null,
                    leftMotorSpeed: 0,
                    rightMotorSpeed: 0,
                    executionMode: EXECUTION_MODES.PYODIDE,
                    // code: code.codeContent
                    code: codeContent
                };

                this.processes.set(codeId, processStatus);

                // Update SpacetimeDB with the initial state
                // await this.updateMicroprocessState(
                //     codeId,
                //     0,
                //     0,
                //     '',
                //     false,
                //     EXECUTION_MODES.PYODIDE
                // );

                this.broadcastStatus(codeId, this.getProcessStatus(codeId));

                return codeId;
            }
            // const pythonRunnerPath = 'process_runner.py'
            const pythonRunnerPath = 'thread_runner.py'

            // Start the Python process
            const pyshell = new PythonShell(pythonRunnerPath, options);

            const processStatus: ProcessStatus = {
                process: pyshell,
                status: 'running',
                codeId,
                startTime: Date.now(),
                lastUpdateTime: Date.now(),
                error: null,
                leftMotorSpeed: 0,
                rightMotorSpeed: 0,
                executionMode
            };

            this.processes.set(codeId, processStatus);

            // Update SpacetimeDB with the initial state
            //   await this.updateMicroprocessState(
            //     codeId,
            //     0,
            //     0,
            //     '',
            //     true,
            //     executionMode
            //   );

            // Handle Python process output
            pyshell.on('message', (message: string) => {
                try {
                    // Try to parse the message as JSON
                    const data = JSON.parse(message);

                    if (data.type === 'motor_state') {
                        // Update motor state
                        if (processStatus) {
                            processStatus.leftMotorSpeed = data.left_motor_speed;
                            processStatus.rightMotorSpeed = data.right_motor_speed;
                            processStatus.lastUpdateTime = Date.now();

                            // Update SpacetimeDB with the new state
                            this.updateMicroprocessState(
                                codeId,
                                data.left_motor_speed,
                                data.right_motor_speed,
                                '',
                                true,
                                executionMode
                            ).catch(console.error);
                        }
                    } else if (data.type === 'global_state') {
                        // Update global state
                        this.broadcastGlobalState(codeId, data.state);
                    } else {
                        // Broadcast as REPL output
                        this.broadcastReplOutput(codeId, message);
                    }
                } catch (error) {
                    // Not JSON, broadcast as REPL output
                    this.broadcastReplOutput(codeId, message);
                }

                // Broadcast the updated status
                this.broadcastStatus(codeId, this.getProcessStatus(codeId));
            });

            // Handle Python process errors
            pyshell.on('stderr', (stderr: string) => {
                if (processStatus) {
                    const now = Date.now();
                    processStatus.lastUpdateTime = now;

                    // Broadcast error as REPL output
                    this.broadcastReplOutput(codeId, `ERROR: ${stderr}`);
                }
            });

            // Handle Python process exit
            pyshell.on('close', async () => {
                if (processStatus) {
                    processStatus.status = 'stopped';
                    processStatus.process = null;
                    processStatus.lastUpdateTime = Date.now();

                    // Update SpacetimeDB with the stopped state
                    await this.updateMicroprocessState(
                        codeId,
                        0,
                        0,
                        'Process exited',
                        false,
                        executionMode
                    );

                    // Broadcast the updated status
                    this.broadcastStatus(codeId, this.getProcessStatus(codeId));

                    console.log(`Process for code ${codeId} exited`);
                }
            });

            // Handle Python process errors
            pyshell.on('error', async (error: Error) => {
                if (processStatus) {
                    processStatus.status = 'error';
                    processStatus.error = error.message;
                    processStatus.process = null;
                    processStatus.lastUpdateTime = Date.now();

                    // Update SpacetimeDB with the error state
                    await this.updateMicroprocessState(
                        codeId,
                        0,
                        0,
                        error.message,
                        false,
                        executionMode
                    );

                    // Broadcast the updated status
                    this.broadcastStatus(codeId, this.getProcessStatus(codeId));

                    console.error(`Process for code ${codeId} encountered an error:`, error);
                }
            });

            // Broadcast the initial status
            this.broadcastStatus(codeId, this.getProcessStatus(codeId));

            console.log(`Started process for code ${codeId} with execution mode ${executionMode}`);

            return codeId;
        } catch (error) {
            console.error(`Failed to start process for code ${codeId}:`, error);

            // Update the process status to error
            const processStatus: ProcessStatus = {
                process: null,
                status: 'error',
                codeId,
                startTime: Date.now(),
                lastUpdateTime: Date.now(),
                error: error instanceof Error ? error.message : String(error),
                leftMotorSpeed: 0,
                rightMotorSpeed: 0,
                executionMode
            };

            this.processes.set(codeId, processStatus);

            // Update SpacetimeDB with the error state
            await this.updateMicroprocessState(
                codeId,
                0,
                0,
                error instanceof Error ? error.message : String(error),
                false,
                executionMode
            );

            // Broadcast the error status
            this.broadcastStatus(codeId, this.getProcessStatus(codeId));

            throw error;
        }
    }

    /**
     * Stop a Python process for a given code ID
     * @param codeId - The ID of the code to stop
     */
    async stopProcess(codeId: number): Promise<void> {
        // Check if the process exists
        if (!this.processes.has(codeId)) {
            throw new Error(`No process found for code ${codeId}`);
        }

        const processStatus = this.processes.get(codeId);

        if (!processStatus) {
            throw new Error(`Process status not found for code ${codeId}`);
        }

        // If the process is already stopped, just return
        if (processStatus.status === 'stopped' || !processStatus.process) {
            return;
        }

        try {
            // Terminate the Python process
            processStatus.process.kill();
            processStatus.process = null;
            processStatus.status = 'stopped';
            processStatus.lastUpdateTime = Date.now();

            // Update SpacetimeDB with the stopped state
            await this.updateMicroprocessState(
                codeId,
                0,
                0,
                '',
                false,
                processStatus.executionMode
            );

            // Broadcast the updated status
            this.broadcastStatus(codeId, this.getProcessStatus(codeId));

            console.log(`Stopped process for code ${codeId}`);
        } catch (error) {
            console.error(`Failed to stop process for code ${codeId}:`, error);

            processStatus.status = 'error';
            processStatus.error = error instanceof Error ? error.message : String(error);
            processStatus.process = null;
            processStatus.lastUpdateTime = Date.now();

            // Update SpacetimeDB with the error state
            await this.updateMicroprocessState(
                codeId,
                0,
                0,
                error instanceof Error ? error.message : String(error),
                false,
                processStatus.executionMode
            );

            // Broadcast the error status
            this.broadcastStatus(codeId, this.getProcessStatus(codeId));

            throw error;
        }
    }

    /**
     * Stop all running processes
     */
    async stopAllProcesses(): Promise<void> {
        const promises: Promise<void>[] = [];

        for (const [codeId, processStatus] of this.processes) {
            if (processStatus.status === 'running' && processStatus.process) {
                promises.push(this.stopProcess(codeId));
            }
        }

        await Promise.all(promises);
    }

    /**
     * Get the status of a process
     * @param codeId - The ID of the code
     * @returns The process status
     */
    getProcessStatus(codeId: number): ProcessStatusResponse {
        if (!this.processes.has(codeId)) {
            throw new Error(`No process found for code ${codeId}`);
        }

        const processStatus = this.processes.get(codeId);

        if (!processStatus) {
            throw new Error(`Process status not found for code ${codeId}`);
        }

        return {
            codeId: processStatus.codeId,
            status: processStatus.status,
            startTime: processStatus.startTime,
            lastUpdateTime: processStatus.lastUpdateTime,
            error: processStatus.error,
            leftMotorSpeed: processStatus.leftMotorSpeed,
            rightMotorSpeed: processStatus.rightMotorSpeed,
            executionMode: processStatus.executionMode
        };
    }

    /**
     * Get the global state of a process
     * @param codeId - The ID of the code
     * @returns The global state
     */
    getProcessGlobalState(codeId: number): any {
        if (!this.globalState.has(codeId)) {
            return {};
        }

        return this.globalState.get(codeId);
    }

    /**
     * Get the count of active processes
     * @returns The count of active processes
     */
    getActiveProcessCount(): number {
        let count = 0;

        for (const processStatus of this.processes.values()) {
            if (processStatus.status === 'running') {
                count++;
            }
        }

        return count;
    }

    /**
     * Accessor for the execution modes
     */
    static get EXECUTION_MODES() {
        return EXECUTION_MODES;
    }
}

export default ProcessManager; 