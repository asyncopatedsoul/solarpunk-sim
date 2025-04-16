import { io } from 'socket.io-client';

// "undefined" means the URL will be computed from the `window.location` object
// const URL = process.env.NODE_ENV === 'production' ? undefined : 'http://localhost:4000';
const REPL_SERVER_URL = 'http://localhost:3100';

export const socket = io(REPL_SERVER_URL,{
    autoConnect: false
  });