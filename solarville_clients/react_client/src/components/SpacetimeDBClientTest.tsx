// https://github.com/clockworklabs/spacetimedb-typescript-sdk/blob/main/examples/quickstart-chat/src/App.tsx


import React, { useEffect, useState } from 'react';
import './App.css';
import {
  DbConnection,
  ErrorContext,
  EventContext,
  Player,
  MicroprocessCode,
  MicroprocessState
} from '../module_bindings';
import { Identity } from '@clockworklabs/spacetimedb-sdk';

export type PrettyMessage = {
  senderName: string;
  text: string;
};

function usePlayers(conn: DbConnection | null): Map<string, Player> {
// function usePlayers(conn: DbConnection | null): Player[] {
//   const [players, setPlayers] = useState<Player[]>([]);
const [players, setPlayers] = useState<Map<string, Player>>(new Map());

  useEffect(() => {
    if (!conn) return;
    const onInsert = (_ctx: EventContext, player: Player) => {
    //   setPlayers(prev => [...prev, player]);
    setPlayers(prev => new Map(prev.set(player.identity.toHexString(), player)));
    };
    conn.db.player.onInsert(onInsert);

    const onUpdate = (_ctx: EventContext, oldPlayer: Player, newPlayer: Player) => {
        setPlayers(prev => {
          prev.delete(oldPlayer.identity.toHexString());
          return new Map(prev.set(newPlayer.identity.toHexString(), newPlayer));
        });
      };
      conn.db.player.onUpdate(onUpdate);

    const onDelete = (_ctx: EventContext, player: Player) => {
    //   setPlayers(prev =>
    //     prev.filter(
    //       p =>
    //         p.playerId !== player.playerId
    //     )
    //   );
    setPlayers(prev => {
        prev.delete(player.identity.toHexString());
        return new Map(prev);
      });
    };
    conn.db.player.onDelete(onDelete);

    return () => {
      conn.db.player.removeOnInsert(onInsert);
      conn.db.player.removeOnUpdate(onUpdate);
      conn.db.player.removeOnDelete(onDelete);
    };
  }, [conn]);

  return players;
}

// function useUsers(conn: DbConnection | null): Map<string, User> {
//   const [users, setUsers] = useState<Map<string, User>>(new Map());

//   useEffect(() => {
//     if (!conn) return;
//     const onInsert = (_ctx: EventContext, user: User) => {
//       setUsers(prev => new Map(prev.set(user.identity.toHexString(), user)));
//     };
//     conn.db.user.onInsert(onInsert);

//     const onUpdate = (_ctx: EventContext, oldUser: User, newUser: User) => {
//       setUsers(prev => {
//         prev.delete(oldUser.identity.toHexString());
//         return new Map(prev.set(newUser.identity.toHexString(), newUser));
//       });
//     };
//     conn.db.user.onUpdate(onUpdate);

//     const onDelete = (_ctx: EventContext, user: User) => {
//       setUsers(prev => {
//         prev.delete(user.identity.toHexString());
//         return new Map(prev);
//       });
//     };
//     conn.db.user.onDelete(onDelete);

//     return () => {
//       conn.db.user.removeOnInsert(onInsert);
//       conn.db.user.removeOnUpdate(onUpdate);
//       conn.db.user.removeOnDelete(onDelete);
//     };
//   }, [conn]);

//   return users;
// }

function SpacetimeDBClient() {
  const [newName, setNewName] = useState('');
  const [settingName, setSettingName] = useState(false);
  const [systemMessage, setSystemMessage] = useState('');
  const [newMessage, setNewMessage] = useState('');
  const [connected, setConnected] = useState<boolean>(false);
  const [identity, setIdentity] = useState<Identity | null>(null);
  const [conn, setConn] = useState<DbConnection | null>(null);

  useEffect(() => {
    const subscribeToQueries = (conn: DbConnection, queries: string[]) => {
      for (const query of queries) {
        conn
          ?.subscriptionBuilder()
          .onApplied(() => {
            console.log('SDK client cache initialized.');
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
    //   conn.reducers.onSendMessage(() => {
    //     console.log('Message sent.');
    //   });

      subscribeToQueries(conn, ['SELECT * FROM players', 'SELECT * FROM microprocess_code', 'SELECT * FROM microprocess_state']);
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
        .withUri('ws://localhost:3000')
        .withModuleName('solarville')
        .withToken(localStorage.getItem('auth_token') || '')
        .onConnect(onConnect)
        .onDisconnect(onDisconnect)
        .onConnectError(onConnectError)
        .build()
    );
  }, []);

  useEffect(() => {
    if (!conn) return;
    conn.db.user.onInsert((_ctx, user) => {
      if (user.online) {
        const name = user.name || user.identity.toHexString().substring(0, 8);
        setSystemMessage(prev => prev + `\n${name} has connected.`);
      }
    });
    conn.db.user.onUpdate((_ctx, oldUser, newUser) => {
      const name =
        newUser.name || newUser.identity.toHexString().substring(0, 8);
      if (oldUser.online === false && newUser.online === true) {
        setSystemMessage(prev => prev + `\n${name} has connected.`);
      } else if (oldUser.online === true && newUser.online === false) {
        setSystemMessage(prev => prev + `\n${name} has disconnected.`);
      }
    });
  }, [conn]);

//   const messages = useMessages(conn);
  const players = usePlayers(conn);

//   const prettyMessages: PrettyMessage[] = messages
//     .sort((a, b) => (a.sent > b.sent ? 1 : -1))
//     .map(message => ({
//       senderName:
//         users.get(message.sender.toHexString())?.name ||
//         message.sender.toHexString().substring(0, 8),
//       text: message.text,
//     }));

  if (!conn || !connected || !identity) {
    return (
      <div className="App">
        <h1>Connecting...</h1>
      </div>
    );
  }

//   const name =
//     users.get(identity?.toHexString())?.name ||
//     identity?.toHexString().substring(0, 8) ||
//     '';

  const onSubmitPlayer = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSettingName(false);
    // conn.reducers.setName(newName);
  };

//   const onMessageSubmit = (e: React.FormEvent<HTMLFormElement>) => {
//     e.preventDefault();
//     setNewMessage('');
//     conn.reducers.sendMessage(newMessage);
//   };

  return (
    <div className="SpacetimeDB">
      <div className="profile">
        <h1>Profile</h1>
        {/* {!settingName ? (
          <>
            <p>{name}</p>
            <button
              onClick={() => {
                setSettingName(true);
                setNewName(name);
              }}
            >
              Edit Name
            </button>
          </>
        ) : (
          <form onSubmit={onSubmitNewName}>
            <input
              type="text"
              aria-label="name input"
              value={newName}
              onChange={e => setNewName(e.target.value)}
            />
            <button type="submit">Submit</button>
          </form>
        )} */}
      </div>
      <div className="message">
        {/* <h1>Messages</h1>
        {prettyMessages.length < 1 && <p>No messages</p>}
        <div>
          {prettyMessages.map((message, key) => (
            <div key={key}>
              <p>
                <b>{message.senderName}</b>
              </p>
              <p>{message.text}</p>
            </div>
          ))}
        </div> */}
      </div>
      <div className="system" style={{ whiteSpace: 'pre-wrap' }}>
        <h1>System</h1>
        <div>
          <p>{systemMessage}</p>
        </div>
      </div>
      <div className="new-message">
        {/* <form
          onSubmit={onMessageSubmit}
          style={{
            display: 'flex',
            flexDirection: 'column',
            width: '50%',
            margin: '0 auto',
          }}
        >
          <h3>New Message</h3>
          <textarea
            aria-label="message input"
            value={newMessage}
            onChange={e => setNewMessage(e.target.value)}
          ></textarea>
          <button type="submit">Send</button>
        </form> */}
      </div>
    </div>
  );
}

export default App;