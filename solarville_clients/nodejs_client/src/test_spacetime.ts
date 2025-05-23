// src/index.ts

// nodemon --config nodemon.dev.json

import { DbConnection, ErrorContext, EventContext, Player, MicroprocessCode, MicroprocessState } from './module_bindings';
import { Identity } from '@clockworklabs/spacetimedb-sdk';

function greet(name: string): string {
  return `Hello, ${name}!`;
}
console.log(greet("World"));

// reference implementation of the SpacetimeDB SDK
// https://github.com/clockworklabs/spacetimedb-typescript-sdk/blob/main/examples/quickstart-chat/src/App.tsx

// SpacetimeDB docs
// https://spacetimedb.com/docs/sdks/typescript#callback-oninsert
// https://spacetimedb.com/docs/sdks/typescript#unique-constraint-index-access

// const [connected, setConnected] = useState<boolean>(false);
// const [identity, setIdentity] = useState<Identity | null>(null);
// const [conn, setConn] = useState<DbConnection | null>(null);

let connected = false;
let identity: Identity | null = null;
let conn: DbConnection | null = null;
let player: Player | null = null;

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

let players: Player[] = [];
let microprocesses: MicroprocessCode[] = [];
let microprocessStates: MicroprocessState[] = [];

function usePlayers(conn: DbConnection | null): Player[] {

  // let players: Player[] = [];
  if (!conn) return [];

  const onInsert = (_ctx: EventContext, player: Player) => {
    // console.log('Player inserted:', player);
    players.push(player);
    // console.log('Players:', players);
  };
  conn.db.player.onInsert(onInsert);

  const onDelete = (_ctx: EventContext, player: Player) => {
    // console.log('Player deleted:', player);
    players = players.filter((p) => p.playerId !== player.playerId);
    // console.log('Players:', players);

  };
  conn.db.player.onDelete(onDelete);

  conn.db.player.removeOnInsert(onInsert);
  conn.db.player.removeOnDelete(onDelete);


  return players;
}

const onConnect = (
  conn: DbConnection,
  _identity: Identity,
  token: string
) => {
  // setIdentity(identity);
  // setConnected(true);
  identity = _identity;
  // localStorage.setItem('auth_token', token);
  console.log(
    'Connected to SpacetimeDB with identity:',
    identity.toHexString()
  );
  // conn.reducers.onSendMessage(() => {
  //   console.log('Message sent.');
  // });

  subscribeToQueries(conn, ['SELECT * FROM player', 'SELECT * FROM microprocess_code', 'SELECT * FROM microprocess_state']);

  usePlayers(conn);
};

const onDisconnect = () => {
  console.log('Disconnected from SpacetimeDB');
  // setConnected(false);
  connected = false;
};

const onConnectError = (_ctx: ErrorContext, err: Error) => {
  console.log('Error connecting to SpacetimeDB:', err);
};

// setConn(
conn = DbConnection.builder()
  .withUri('ws://127.0.0.1:3000')
  .withModuleName('solarville')
  // .withToken(localStorage.getItem('auth_token') || '')
  .withToken('')
  .onConnect(onConnect)
  .onDisconnect(onDisconnect)
  .onConnectError(onConnectError)
  .build()
// );
