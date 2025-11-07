# Unity MCP Integration Setup

This project now includes the Unity MCP (Model Context Protocol) server, which provides real-time control of Unity Editor through AI-powered workflows.

## 🚀 Features

Unity MCP enables you to:
- **Control Unity Editor**: Select, move, rotate, and scale objects with natural language
- **Batch Operations**: Align, distribute, duplicate objects with undo support
- **Automated Testing**: Enter play mode and run test scenarios programmatically
- **Scene Management**: Load, save, inspect scene hierarchy in real-time

## 📋 Prerequisites

- Node.js >= 18.0.0
- Unity 2022.3 LTS or later
- Claude Desktop or any MCP client

## 🛠️ Installation

### Step 1: Install Dependencies

The Unity MCP package is already included in the project. To ensure all dependencies are installed:

```bash
npm install
```

### Step 2: Configure Your MCP Client

The `.mcp.json` configuration file is already set up in this project. If you're using Claude Desktop or Claude Code, add this configuration to your client:

**For Claude Desktop:**
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Linux: `~/.config/Claude/claude_desktop_config.json`

Add or update:
```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "npx",
      "args": ["-y", "@spark-apps/unity-mcp"]
    }
  }
}
```

**For Local Development:**
You can also run the MCP server directly from this project:
```bash
npx @spark-apps/unity-mcp
```

### Step 3: Unity Editor Integration

**Required one-time setup per Unity project:**

1. Open your MCP client (e.g., Claude Desktop)
2. Tell Claude:
   ```
   Setup Unity MCP in my Unity project
   ```
3. This installs 4 C# scripts to `Assets/Editor/UnityMCP/`:
   - `MCPEditorServer.cs` - HTTP server
   - `EditorCommandHandler.cs` - Command processor with undo support
   - `PlayModeHandler.cs` - Play mode automation
   - `SceneHandler.cs` - Scene operations
4. Restart Unity Editor
5. Verify in Console: `[Unity MCP] Server started on port 8080`

## 🎮 Available Tools

### Safe Operations (Read-only)
- **Setup Unity MCP**: Install editor integration into Unity project
- **Select Objects**: Select by name, tag, or pattern with framing
- **Find Objects**: Find by component type or pattern
- **List Scenes**: List all scenes in build settings
- **Get Hierarchy**: Get complete scene hierarchy
- **Find In Scene**: Find objects in current scene
- **Play Mode Status**: Get play mode status and logs

### Modifications (With Undo Support)
- **Transform Objects**: Move, rotate, scale objects
- **Align Objects**: Align left/right/top/bottom/center
- **Distribute Objects**: Distribute evenly along axis
- **Duplicate Objects**: Clone objects with undo support
- **Enter/Exit Play Mode**: Control play mode programmatically
- **Run Test**: Execute automated test scenarios
- **Set Time Scale**: Slow motion or fast forward
- **Load/Save Scene**: Manage scenes programmatically

### Destructive Operations (Use with Caution)
- **Delete Objects**: Delete objects with undo support
- **Cleanup Scene**: Remove missing scripts and empty objects

## 💬 Example Commands

### Object Manipulation
- "Select all objects with tag 'Enemy' and align them horizontally"
- "Move the Player object to position (0, 5, 10)"
- "Distribute selected objects evenly along the x axis"
- "Find all objects with Camera component"
- "Duplicate selected object 5 times"

### Automated Testing
- "Enter play mode and move Player to (10, 0, 0) for 5 seconds"
- "Set time scale to 0.5 for slow motion"
- "Run a test that destroys the Boss after 2 seconds"
- "Check play mode status and show test logs"

### Scene Operations
- "List all scenes in the project"
- "Load the MainMenu scene"
- "Show me the complete hierarchy of the current scene"
- "Find all objects with Rigidbody component"
- "Clean up scene by removing missing scripts"

## 🐛 Troubleshooting

### MCP Server Not Showing
1. Verify Node.js is installed: `node --version`
2. Check config file path is correct
3. Ensure JSON syntax is valid
4. Restart MCP client completely

### Unity Editor Not Responding
1. Ensure Unity Editor is open
2. Check `Assets/Editor/UnityMCP/` scripts are installed
3. Verify Console for `[Unity MCP] Server started on port 8080`
4. Check no errors in Unity Console

### Port Already in Use
1. Default port is `8080`
2. Check what's using it:
   - Mac/Linux: `lsof -i :8080`
   - Windows: `netstat -ano | findstr :8080`
3. Stop conflicting process or change port in Unity scripts

## 🔗 Resources

- [Unity MCP GitHub](https://github.com/muammar-yacoob/unity-mcp)
- [Unity MCP NPM Package](https://www.npmjs.com/package/@spark-apps/unity-mcp)
- [Model Context Protocol Documentation](https://modelcontextprotocol.io/)

## 📝 License

Unity MCP is MIT licensed by [Muammar Yacoob](https://github.com/muammar-yacoob).