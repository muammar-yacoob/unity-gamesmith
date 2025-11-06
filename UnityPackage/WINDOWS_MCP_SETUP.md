# Windows MCP Setup Guide

## Prerequisites

1. **Install Node.js for Windows**
   - Download from: https://nodejs.org/
   - Install the LTS version
   - During installation, make sure "Add to PATH" is checked

2. **Verify Installation**
   Open Command Prompt (cmd.exe) or PowerShell and run:
   ```cmd
   node --version
   npm --version
   npx --version
   ```
   All commands should return version numbers.

## Install Unity MCP Server

Open Command Prompt or PowerShell **as Administrator** and run:

```cmd
npm install -g @spark-apps/unity-mcp
```

## Verify MCP Installation

Test that the MCP server can be launched:

```cmd
npx @spark-apps/unity-mcp --version
```

This should start the MCP server and show initialization messages.

## Troubleshooting

### "npx not found"
- Make sure Node.js is installed and added to PATH
- Restart your terminal/Unity after installation

### "npm ERR! code EACCES"
- Run Command Prompt as Administrator
- Or configure npm to install global packages to user directory:
  ```cmd
  npm config set prefix %APPDATA%\npm
  ```

### MCP Server Times Out
- Check Windows Firewall isn't blocking node.exe
- Try running Unity as Administrator
- Check antivirus software isn't blocking the process

### "Cannot find module '@spark-apps/unity-mcp'"
- Reinstall the package:
  ```cmd
  npm uninstall -g @spark-apps/unity-mcp
  npm cache clean --force
  npm install -g @spark-apps/unity-mcp
  ```

## Alternative: Local Installation

If global installation doesn't work, you can install locally in your Unity project:

1. Navigate to your Unity project folder
2. Run:
   ```cmd
   npm init -y
   npm install @spark-apps/unity-mcp
   ```
3. The MCP server will be available in `node_modules/.bin/unity-mcp`

## Testing in Unity

1. Open Unity
2. Go to Tools → GameSmith → Open Window
3. The MCP status should show "✓ MCP: X tools" when connected
4. Check the Unity Console for any error messages

If you see "[GameSmith MCP] Initialization timeout", check the console for more detailed error messages about what went wrong.