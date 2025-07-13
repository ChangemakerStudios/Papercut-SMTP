const PROXY_CONFIG = [
  {
    context: ['/api/**'],
    target: 'http://localhost:37408',
    secure: false,
    changeOrigin: true,
    logLevel: 'debug'
  }
];

module.exports = PROXY_CONFIG; 