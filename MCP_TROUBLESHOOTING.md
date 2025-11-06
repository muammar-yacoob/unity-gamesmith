# MCP (Model Context Protocol) Troubleshooting Guide

## Issue: "Cannot find module npm-cli.js" Error

### Problem
When trying to start the MCP server from Unity, you might encounter:
```
[MCP] Error: Cannot find module 'D:\Unity\MyAssets\GameSmith\MCP Test\node_modules\npm\bin\npm-cli.js'
[GameSmith MCP] No response received
```

### Cause
The Unity MCP client was looking for npm/node in the wrong location (inside the Unity project directory instead of the system-wide installation).

### Solution Applied
The `MCPClientAsync.cs` has been updated to:

1. **Use absolute paths to Node.js executables** on Windows (`C:\Program Files\nodejs\node.exe`)
2. **Handle multiple invocation patterns**:
   - Direct node execution with path to `index.js`
   - NPX execution with package name
   - Fallback to PATH-based resolution

3. **Improved error logging** to help diagnose issues

## Setup Requirements

### Windows
1. **Node.js Installation**: Ensure Node.js is installed at the standard location:
   - `C:\Program Files\nodejs\`
   - Verify with: `node --version` and `npm --version` in Command Prompt

2. **Unity MCP Package**: Install the unity-mcp package globally:
   ```cmd
   npm install -g @spark-apps/unity-mcp
   ```
   This will install to: `C:\Users\[YourUsername]\AppData\Roaming\npm\node_modules\@spark-apps\unity-mcp\`

3. **Verify Installation**:
   ```cmd
   node "C:\Users\[YourUsername]\AppData\Roaming\npm\node_modules\@spark-apps\unity-mcp\dist\index.js"
   ```
   You should see:
   ```
   Unity MCP Server initialized
   Server is ready to automate Unity game development
   MCP Server running on stdio
   ```

### WSL/Linux/Mac
1. **Node.js Installation**: Install via package manager or nvm
2. **Unity MCP Package**:
   ```bash
   npm install -g @spark-apps/unity-mcp
   ```
3. **Verify Installation**:
   ```bash
   node ~/.npm-global/node_modules/@spark-apps/unity-mcp/dist/index.js
   ```

## How the Fix Works

### Code Changes in MCPClientAsync.cs

1. **Detection Logic** (lines 56-65):
   - Detects if the command is for unity-mcp regardless of invocation method
   - Handles both `npx unity-mcp` and `node path/to/index.js` patterns

2. **Path Resolution** (lines 67-145):
   - If a direct path is provided, uses it as-is
   - Otherwise, attempts to find the global npm installation
   - Falls back to npx if the direct path isn't found

3. **Windows-Specific Handling**:
   - Uses full path to `node.exe` (`C:\Program Files\nodejs\node.exe`)
   - Falls back to PATH resolution if not found at standard location
   - Properly handles Windows paths without WSL path translation issues

4. **Enhanced Logging**:
   - Logs the exact command being executed
   - Captures and displays all stderr output for debugging
   - Shows warnings when commands aren't found at expected locations

## Testing the Fix

1. **In Unity**:
   - Open the GameSmith window
   - Click "Start MCP" or restart the MCP server
   - Check the Unity Console for logs

2. **Expected Output**:
   ```
   [GameSmith MCP] Starting MCP server: C:\Program Files\nodejs\node.exe C:\Users\[YourUsername]\AppData\Roaming\npm\node_modules\@spark-apps\unity-mcp\dist\index.js
   [GameSmith MCP] Using provided path: C:\Program Files\nodejs\node.exe C:\Users\[YourUsername]\AppData\Roaming\npm\node_modules\@spark-apps\unity-mcp\dist\index.js
   [MCP] Ready: [number] tools
   ```

## Common Issues and Solutions

### Issue: Node.js not in standard location
**Solution**: The code will fall back to using `node.exe` from PATH. Ensure Node.js is in your system PATH.

### Issue: unity-mcp package not found
**Solution**: Install it globally: `npm install -g @spark-apps/unity-mcp`

### Issue: Process exits immediately
**Solution**: Check stderr output in Unity Console. Common causes:
- Port already in use
- Missing dependencies
- Permission issues

### Issue: No response from server
**Solution**:
- Check if any antivirus/firewall is blocking the process
- Verify Node.js and npm are properly installed
- Try running the server manually to test

## Debug Mode

To enable verbose logging, the updated code now:
- Logs all commands before execution
- Captures all stderr output (not just errors)
- Shows file existence checks

## File Locations Reference

### Windows
- Node.js: `C:\Program Files\nodejs\`
- NPM Global Packages: `C:\Users\[Username]\AppData\Roaming\npm\node_modules\`
- Unity MCP: `C:\Users\[Username]\AppData\Roaming\npm\node_modules\@spark-apps\unity-mcp\`

### Unity Project
- MCP Client: `UnityPackage/Editor/MCPClientAsync.cs`
- GameSmith Window: `UnityPackage/Editor/GameSmithWindow.cs`
- Settings: `UnityPackage/Editor/GameSmithSettings.cs`