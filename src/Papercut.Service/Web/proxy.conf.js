// changeOrigin stays off: the service blocks cross-origin requests, and it recognizes
// ng serve as same-origin only if Host still matches the page's Origin
const PROXY_CONFIG = [
  {
    context: ['/api/**'],
    target: 'http://127.0.0.1:8080',
    secure: false,
    changeOrigin: false,
    logLevel: 'debug'
  },
  {
    context: ['/hubs/**'],
    target: 'http://127.0.0.1:8080',
    secure: false,
    changeOrigin: false,
    ws: true, // Enable WebSocket support for SignalR
    logLevel: 'debug'
  }
];

module.exports = PROXY_CONFIG;
