import React, { useEffect, useState } from 'react';
import reactLogo from './assets/react.svg';
import viteLogo from '/vite.svg';
import './App.css';

import { DbConnection, ErrorContext, MicroprocessCode, MicroprocessState } from './module_bindings';
import { Identity } from '@clockworklabs/spacetimedb-sdk';
import ReplInterface from './components/ReplInterface';

import Basic from './components/EmbeddedVue';

function App() {
  console.log('App.tsx loaded');
  
  const [connected, setConnected] = useState<boolean>(false);
  const [identity, setIdentity] = useState<Identity | null>(null);
  const [conn, setConn] = useState<DbConnection | null>(null);
  const [microprocessCodes, setMicroprocessCodes] = useState<MicroprocessCode[]>([]);
  const [microprocessStates, setMicroprocessStates] = useState<MicroprocessState[]>([]);
  const [selectedTab, setSelectedTab] = useState<'repl' | 'codes'>('repl');

  useEffect(() => {
    const subscribeToQueries = (conn: DbConnection, queries: string[]) => {
      let count = 0;
      for (const query of queries) {
        conn
          ?.subscriptionBuilder()
          .onApplied(() => {
            count++;
            if (count === queries.length) {
              console.log('SDK client cache initialized.');
            }
          })
          .subscribe(query);
      }
    };

    const onConnect = (
      conn: DbConnection,
      identity: Identity,
      token: string
    ) => {
      setIdentity(identity);
      setConnected(true);
      localStorage.setItem('auth_token', token);
      console.log(
        'Connected to SpacetimeDB with identity:',
        identity.toHexString()
      );

      // Subscribe to our tables
      subscribeToQueries(conn, [
        'SELECT * FROM microprocess_code', 
        'SELECT * FROM microprocess_state'
      ]);

      // Set up listeners for our tables
      conn.db.microprocessCode.onInsert((code) => {
        console.log('Microprocess code inserted:', code);
        // setMicroprocessCodes(prev => [...prev, code]);
      });

      conn.db.microprocessCode.onUpdate((oldCode, newCode) => {
        console.log('Microprocess code updated:', newCode);
        // setMicroprocessCodes(prev => 
        //   prev.map(code => code.codeId === newCode.codeId ? newCode : code)
        // );
      });

      conn.db.microprocessCode.onDelete((code) => {
        console.log('Microprocess code deleted:', code);
        // setMicroprocessCodes(prev => 
        //   prev.filter(c => c.codeId !== code.codeId)
        // );
      });

      conn.db.microprocessState.onInsert((state) => {
        console.log('Microprocess state inserted:', state);
        // setMicroprocessStates(prev => [...prev, state]);
      });

      conn.db.microprocessState.onUpdate((oldState, newState) => {
        console.log('Microprocess state updated:', newState);
        // setMicroprocessStates(prev => 
        //   prev.map(state => state.codeId === newState.codeId ? newState : state)
        // );
      });

      // Initialize the local state with existing data
      // setMicroprocessCodes(conn.db.microprocessCode.filter(() => true));
      // setMicroprocessStates(conn.db.microprocessState.filter(() => true));
    };

    const onDisconnect = () => {
      console.log('Disconnected from SpacetimeDB');
      setConnected(false);
    };

    const onConnectError = (_ctx: ErrorContext, err: Error) => {
      console.log('Error connecting to SpacetimeDB:', err);
    };

    setConn(
      DbConnection.builder()
        .withUri('ws://127.0.0.1:3000')
        .withModuleName('solarville')
        .withToken(localStorage.getItem('auth_token') || '')
        .onConnect(onConnect)
        .onDisconnect(onDisconnect)
        .onConnectError(onConnectError)
        .build()
    );
  }, []);

  // Styles for the app
  const styles = {
    container: {
      maxWidth: '1000px',
      margin: '0 auto',
      padding: '20px',
    },
    header: {
      display: 'flex',
      alignItems: 'center',
      marginBottom: '20px',
    },
    logo: {
      height: '40px',
      marginRight: '10px',
    },
    title: {
      fontSize: '24px',
      margin: 0,
    },
    connectionStatus: {
      marginLeft: 'auto',
      padding: '5px 10px',
      borderRadius: '4px',
      fontSize: '14px',
      color: 'white',
      backgroundColor: connected ? '#4CAF50' : '#f44336',
    },
    tabs: {
      display: 'flex',
      marginBottom: '20px',
      borderBottom: '1px solid #ccc',
    },
    tab: {
      padding: '10px 20px',
      cursor: 'pointer',
      borderBottom: '2px solid transparent',
    },
    activeTab: {
      borderBottom: '2px solid #2196F3',
      fontWeight: 'bold',
    },
    codeList: {
      display: 'grid',
      gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))',
      gap: '15px',
    },
    codeCard: {
      border: '1px solid #ccc',
      borderRadius: '4px',
      padding: '15px',
      backgroundColor: '#f9f9f9',
    },
    codeHeader: {
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center',
      marginBottom: '10px',
    },
    codeName: {
      margin: 0,
      fontSize: '18px',
      fontWeight: 'bold',
    },
    codeButtons: {
      display: 'flex',
      gap: '5px',
    },
    runButton: {
      padding: '5px 10px',
      backgroundColor: '#4CAF50',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      cursor: 'pointer',
    },
    stopButton: {
      padding: '5px 10px',
      backgroundColor: '#f44336',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      cursor: 'pointer',
    },
    editButton: {
      padding: '5px 10px',
      backgroundColor: '#2196F3',
      color: 'white',
      border: 'none',
      borderRadius: '4px',
      cursor: 'pointer',
    },
    codeStatus: {
      display: 'flex',
      justifyContent: 'space-between',
      marginTop: '10px',
      fontSize: '14px',
      color: '#666',
    },
    running: {
      color: '#4CAF50',
      fontWeight: 'bold',
    },
    stopped: {
      color: '#f44336',
    },
    noScripts: {
      textAlign: 'center' as const,
      padding: '20px',
      color: '#666',
    },
  };

  // Function to start a microprocess
  const startMicroprocess = async (codeId: number) => {
    if (!conn) return;
    
    try {
      conn.reducers.startMicroprocess(codeId);
      console.log(`Started microprocess ${codeId}`);
    } catch (error) {
      console.error('Error starting microprocess:', error);
    }
  };

  // Function to stop a microprocess
  const stopMicroprocess = async (codeId: number) => {
    if (!conn) return;
    
    try {
      conn.reducers.stopMicroprocess(codeId);
      console.log(`Stopped microprocess ${codeId}`);
    } catch (error) {
      console.error('Error stopping microprocess:', error);
    }
  };

  // Render microprocess code list
  const renderCodeList = () => {
    if (microprocessCodes.length === 0) {
      return (
        <div style={styles.noScripts}>
          <p>No scripts available. Create one in the REPL tab!</p>
        </div>
      );
    }

    return (
      <div style={styles.codeList}>
        {microprocessCodes.map(code => {
          const state = microprocessStates.find(s => s.codeId === code.codeId);
          const isRunning = state?.isRunning || false;
          
          return (
            <div key={code.codeId} style={styles.codeCard}>
              <div style={styles.codeHeader}>
                <h3 style={styles.codeName}>{code.name}</h3>
                <div style={styles.codeButtons}>
                  {!isRunning ? (
                    <button 
                      style={styles.runButton}
                      onClick={() => startMicroprocess(code.codeId)}
                    >
                      Run
                    </button>
                  ) : (
                    <button 
                      style={styles.stopButton}
                      onClick={() => stopMicroprocess(code.codeId)}
                    >
                      Stop
                    </button>
                  )}
                </div>
              </div>
              
              <pre style={{ 
                maxHeight: '100px', 
                overflow: 'auto', 
                backgroundColor: '#f0f0f0',
                padding: '5px',
                fontSize: '12px'
              }}>
                {code.codeContent.length > 200 
                  ? `${code.codeContent.substring(0, 200)}...` 
                  : code.codeContent}
              </pre>
              
              <div style={styles.codeStatus}>
                <span style={isRunning ? styles.running : styles.stopped}>
                  {isRunning ? 'Running' : 'Stopped'}
                </span>
                {state && (
                  <span>
                    L: {state.leftMotorSpeed.toFixed(1)} | 
                    R: {state.rightMotorSpeed.toFixed(1)}
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    );
  };

  return (
    <div style={styles.container}>
      <div style={styles.header}>
        <img src={reactLogo} style={styles.logo} alt="React logo" />
        <h1 style={styles.title}>Solarville Microprocess Manager</h1>
        <div style={styles.connectionStatus}>
          {connected ? 'Connected' : 'Disconnected'}
        </div>
      </div>
      <Basic />
      <div style={styles.tabs}>
        <div 
          style={{
            ...styles.tab,
            ...(selectedTab === 'repl' ? styles.activeTab : {})
          }}
          onClick={() => setSelectedTab('repl')}
        >
          Python REPL
        </div>
        <div 
          style={{
            ...styles.tab,
            ...(selectedTab === 'codes' ? styles.activeTab : {})
          }}
          onClick={() => setSelectedTab('codes')}
        >
          Saved Scripts ({microprocessCodes.length})
        </div>
      </div>

      {selectedTab === 'repl' ? (
        <ReplInterface dbConnection={conn} height="500px" />
      ) : (
        renderCodeList()
      )}
    </div>
  );
}

export default App;