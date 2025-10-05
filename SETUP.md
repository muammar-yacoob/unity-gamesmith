# Unity MCP Setup Guide

Complete setup instructions for the Unity Model Context Protocol server.

## Quick Start

```bash
# 1. Clone and install
git clone https://github.com/muammar-yacoob/unity-mcp.git
cd unity-mcp
npm install

# 2. Build the project
npm run build

# 3. Configure Claude Desktop (see below)

# 4. Start using in Claude Desktop!
```

## Prerequisites

### Required

1. **Node.js 18+** and **npm**
   ```bash
   # Check versions
   node --version  # Should be >= 18
   npm --version   # Should be >= 8

   # Install npm if needed
   # npm install -g npm
   ```

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/muammar-yacoob/unity-mcp.git
cd unity-mcp
```

### 2. Install Node Dependencies

```bash
npm install
```

This installs dependencies for all packages in the monorepo.

### 3. Build the Project

```bash
npm run build
```

This compiles TypeScript to JavaScript for all packages.

## Claude Desktop Configuration

### Locate Configuration File

**macOS:**
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Windows:**
```
%APPDATA%\\Claude\\claude_desktop_config.json
```

**Linux:**
```
~/.config/Claude/claude_desktop_config.json
```

### Add MCP Server Configuration

Edit the config file and add:

```json
{
  "mcpServers": {
    "unity-mcp": {
      "command": "node",
      "args": [
        "/absolute/path/to/unity-mcp/packages/mcp-server/dist/index.js"
      ],
      "env": {
        "NODE_ENV": "production"
      }
    }
  }
}
```

**Important:** Replace `/absolute/path/to/unity-mcp` with the actual absolute path to your cloned repository.

### Restart Claude Desktop

After saving the configuration, restart Claude Desktop for changes to take effect.

## Verification

### Test in Claude Desktop

In Claude Desktop, try:

```
Create a new Unity project called "my-first-game"
```

Claude should:
1. Call the `create_unity_project` tool
2. Create a new Unity project structure
3. Return the project path

Check your filesystem for the created project!

## Troubleshooting

### MCP Not Showing in Claude Desktop

**Solution:**
1. Check configuration file path is correct
2. Ensure JSON syntax is valid (use a JSON validator)
3. Verify absolute path to `dist/index.js` is correct
4. Restart Claude Desktop
5. Check Claude Desktop logs (if available)

## Development

### Running in Development Mode

```bash
# Terminal 1: Watch mode (auto-rebuild on changes)
npm run dev

# Terminal 2: Test with fastmcp
cd packages/mcp-server
npx fastmcp dev dist/index.js
```

### Making Changes

1. Edit source files in `packages/*/src/`
2. Run `npm run build` or use `npm run dev` for auto-rebuild
3. Restart Claude Desktop to pick up changes

### Adding New Tools

1. Edit `packages/mcp-server/src/core/tools.ts`
2. Rebuild: `npm run build`
3. Restart Claude Desktop

## Updating

```bash
# Pull latest changes
git pull origin main

# Reinstall dependencies (if package.json changed)
npm install

# Rebuild
npm run build

# Restart Claude Desktop
```

## Uninstalling

### Remove from Claude Desktop

Edit `claude_desktop_config.json` and remove the `unity-mcp` entry.

### Delete Files

```bash
# Remove repository
rm -rf /path/to/unity-mcp
```

## Next Steps

- Check [README.md](README.md) for project overview
- Report issues: https://github.com/muammar-yacoob/unity-mcp/issues

## Support

- GitHub Issues: https://github.com/muammar-yacoob/unity-mcp/issues
- Discussions: https://github.com/muammar-yacoob/unity-mcp/discussions
