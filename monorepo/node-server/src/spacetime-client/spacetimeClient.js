const { Client, AlgebraicType, Identity } = require('@clockworklabs/spacetimedb-sdk');

class SpacetimeClient {
  constructor(address, moduleName) {
    this.address = address;
    this.moduleName = moduleName;
    this.client = null;
    this.connected = false;
    this.identity = null;
    this.tables = {
      microprocessCode: null,
      microprocessState: null,
      robot: null,
      gameObject: null,
      player: null,
      scenario: null
    };
  }
  
  /**
   * Connect to the SpacetimeDB server
   * @returns {Promise<void>}
   */
  async connect() {
    return new Promise((resolve, reject) => {
      try {
        // Create a new client
        this.client = new Client();
        
        // Set up event listeners
        this.client.on('connect', () => {
          console.log('Connected to SpacetimeDB');
          this.connected = true;
          this.identity = this.client.getIdentity();
          resolve();
        });
        
        this.client.on('disconnect', () => {
          console.log('Disconnected from SpacetimeDB');
          this.connected = false;
        });
        
        this.client.on('error', (error) => {
          console.error('SpacetimeDB client error:', error);
          reject(error);
        });
        
        // Connect to the SpacetimeDB server
        this.client.connect({
          address: this.address,
          moduleName: this.moduleName,
          identity: this.client.generateIdentity()
        }).then(() => {
          // Register the process manager as a player
          this.client.callReducer('register_player', {
            name: 'Python Process Manager'
          });
          
          // Set up table references
          this.setupTables();
        }).catch(reject);
      } catch (error) {
        reject(error);
      }
    });
  }
  
  /**
   * Set up table references
   */
  setupTables() {
    // Get table references
    this.tables.microprocessCode = this.client.getTable('MicroprocessCode');
    this.tables.microprocessState = this.client.getTable('MicroprocessState');
    this.tables.robot = this.client.getTable('Robot');
    this.tables.gameObject = this.client.getTable('GameObject');
    this.tables.player = this.client.getTable('Player');
    this.tables.scenario = this.client.getTable('Scenario');
  }
  
  /**
   * Check if the client is connected to SpacetimeDB
   * @returns {boolean} - True if connected, false otherwise
   */
  isConnected() {
    return this.connected;
  }
  
  /**
   * Get the Identity of the client
   * @returns {Identity} - The Identity object
   */
  getIdentity() {
    return this.identity;
  }
  
  /**
   * Get the code for a given code ID
   * @param {number} codeId - The ID of the code to get
   * @returns {Promise<Object>} - The code object
   */
  async getCode(codeId) {
    return new Promise((resolve, reject) => {
      try {
        // Check if connected
        if (!this.connected) {
          throw new Error('Not connected to SpacetimeDB');
        }
        
        // Get the code from the MicroprocessCode table
        const codes = this.tables.microprocessCode.getRows();
        const code = codes.find(c => c.code_id === codeId);
        
        if (!code) {
          resolve(null);
        } else {
          resolve({
            codeId: code.code_id,
            ownerId: code.owner_id,
            name: code.name,
            filePath: code.file_path,
            codeContent: code.code_content,
            lastUpdated: code.last_updated
          });
        }
      } catch (error) {
        reject(error);
      }
    });
  }
  
  /**
   * Update the state of a microprocessor
   * @param {number} codeId - The ID of the code
   * @param {number} leftMotorSpeed - The speed of the left motor
   * @param {number} rightMotorSpeed - The speed of the right motor
   * @param {string} errorMessage - Any error message
   * @param {boolean} isRunning - Whether the code is running
   * @returns {Promise<void>}
   */
  async updateMicroprocessState(codeId, leftMotorSpeed, rightMotorSpeed, errorMessage, isRunning) {
    return new Promise((resolve, reject) => {
      try {
        // Check if connected
        if (!this.connected) {
          throw new Error('Not connected to SpacetimeDB');
        }
        
        // Call the update_microprocess_state reducer
        this.client.callReducer('update_microprocess_state', {
          code_id: codeId,
          left_motor_speed: leftMotorSpeed,
          right_motor_speed: rightMotorSpeed,
          error_message: errorMessage,
          is_running: isRunning
        }).then(resolve).catch(reject);
      } catch (error) {
        reject(error);
      }
    });
  }
  
  /**
   * Subscribe to code changes in SpacetimeDB
   * @param {Function} callback - The callback function to call when code state changes
   */
  subscribeToCodeChanges(callback) {
    // Check if connected
    if (!this.connected) {
      throw new Error('Not connected to SpacetimeDB');
    }
    
    // Subscribe to the start_microprocess reducer
    this.client.onReducer('start_microprocess', (ctx, args) => {
      const codeId = args.code_id;
      callback(codeId, true);
    });
    
    // Subscribe to the stop_microprocess reducer
    this.client.onReducer('stop_microprocess', (ctx, args) => {
      const codeId = args.code_id;
      callback(codeId, false);
    });
  }
  
  /**
   * Create a new microprocessor code
   * @param {string} name - The name of the code
   * @param {string} codeContent - The content of the code
   * @param {string} filePath - The file path of the code
   * @returns {Promise<number>} - The ID of the created code
   */
  async createCode(name, codeContent, filePath) {
    return new Promise((resolve, reject) => {
      try {
        // Check if connected
        if (!this.connected) {
          throw new Error('Not connected to SpacetimeDB');
        }
        
        // Call the add_microprocess_code reducer
        this.client.callReducer('add_microprocess_code', {
          name,
          code_content: codeContent,
          file_path: filePath
        }).then(result => {
          resolve(result.code_id);
        }).catch(reject);
      } catch (error) {
        reject(error);
      }
    });
  }
  
  /**
   * Update an existing microprocessor code
   * @param {number} codeId - The ID of the code to update
   * @param {string} name - The name of the code
   * @param {string} codeContent - The content of the code
   * @param {string} filePath - The file path of the code
   * @returns {Promise<void>}
   */
  async updateCode(codeId, name, codeContent, filePath) {
    return new Promise((resolve, reject) => {
      try {
        // Check if connected
        if (!this.connected) {
          throw new Error('Not connected to SpacetimeDB');
        }
        
        // Call the update_microprocess_code reducer
        this.client.callReducer('update_microprocess_code', {
          code_id: codeId,
          name,
          code_content: codeContent,
          file_path: filePath
        }).then(resolve).catch(reject);
      } catch (error) {
        reject(error);
      }
    });
  }
  
  /**
   * Get all microprocessor codes
   * @returns {Promise<Array>} - An array of code objects
   */
  async getAllCodes() {
    return new Promise((resolve, reject) => {
      try {
        // Check if connected
        if (!this.connected) {
          throw new Error('Not connected to SpacetimeDB');
        }
        
        // Get all codes from the MicroprocessCode table
        const codes = this.tables.microprocessCode.getRows();
        
        resolve(codes.map(code => ({
          codeId: code.code_id,
          ownerId: code.owner_id,
          name: code.name,
          filePath: code.file_path,
          codeContent: code.code_content,
          lastUpdated: code.last_updated
        })));
      } catch (error) {
        reject(error);
      }
    });
  }
}

module.exports = SpacetimeClient;
