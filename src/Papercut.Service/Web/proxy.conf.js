const PROXY_CONFIG = [
  {
    context: ['/api/**'],
    target: 'http://localhost:37408',
    secure: false,
    changeOrigin: true,
    logLevel: 'debug'
  },
  {
    context: ['/hubs/**'],
    target: 'http://localhost:37408',
    secure: false,
    changeOrigin: true,
    ws: true, // Enable WebSocket support for SignalR
    logLevel: 'debug'
  }
];

module.exports = PROXY_CONFIG; 