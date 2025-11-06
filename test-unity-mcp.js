// Test script to verify unity-mcp server can be started
const { spawn } = require('child_process');
const path = require('path');

// Try to start unity-mcp server
console.log('Testing unity-mcp server...');

// Test 1: Direct node execution
const npmGlobalPath = process.platform === 'win32'
  ? path.join(process.env.APPDATA, 'npm', 'node_modules', '@spark-apps', 'unity-mcp', 'dist', 'index.js')
  : path.join(process.env.HOME, '.npm-global', 'node_modules', '@spark-apps', 'unity-mcp', 'dist', 'index.js');

console.log('Looking for unity-mcp at:', npmGlobalPath);

const fs = require('fs');
if (fs.existsSync(npmGlobalPath)) {
  console.log('✓ Found unity-mcp at expected location');

  // Try to start the server
  const nodeCmd = process.platform === 'win32' ? 'node.exe' : 'node';
  console.log(`Starting server with: ${nodeCmd} ${npmGlobalPath}`);

  const server = spawn(nodeCmd, [npmGlobalPath], {
    stdio: ['pipe', 'pipe', 'pipe']
  });

  server.stdout.on('data', (data) => {
    console.log('stdout:', data.toString());
  });

  server.stderr.on('data', (data) => {
    console.error('stderr:', data.toString());
  });

  server.on('error', (error) => {
    console.error('Failed to start server:', error);
  });

  server.on('exit', (code, signal) => {
    console.log(`Server exited with code ${code} and signal ${signal}`);
  });

  // Send initialize request after a brief delay
  setTimeout(() => {
    const initRequest = JSON.stringify({
      jsonrpc: "2.0",
      id: 1,
      method: "initialize",
      params: {
        protocolVersion: "2024-11-05",
        capabilities: { tools: {} },
        clientInfo: { name: "test-client", version: "1.0.0" }
      }
    });

    console.log('Sending initialize request...');
    server.stdin.write(initRequest + '\n');

    // Wait for response
    setTimeout(() => {
      console.log('Test complete. Press Ctrl+C to exit.');
    }, 2000);
  }, 1000);

} else {
  console.log('✗ unity-mcp not found at expected location');
  console.log('Try running: npm install -g @spark-apps/unity-mcp');
}