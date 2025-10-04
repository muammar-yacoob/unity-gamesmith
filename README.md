# ⚡ Unity MCP

**🎮 Automate Unity development • 🤖 AI-powered workflows • 🚀 Natural language game creation**

[![npm version](https://img.shields.io/npm/v/@spark-apps/unity-mcp?style=flat-square)](https://www.npmjs.com/package/@spark-apps/unity-mcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](https://opensource.org/licenses/MIT)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/muammar-yacoob?style=social)](https://github.com/sponsors/muammar-yacoob)
[![Report Bug](https://img.shields.io/badge/Report-Bug-red?style=flat-square)](https://github.com/muammar-yacoob/unity-mcp/issues)
[![GitHub Stars](https://img.shields.io/github/stars/muammar-yacoob/unity-mcp?style=social)](https://github.com/muammar-yacoob/unity-mcp)

## ✨ What It Does

Control Unity game engine through natural language via Claude Desktop:

| Feature                   | Description                                              |
| ------------------------- | -------------------------------------------------------- |
| 🎮 Project Creation       | Initialize new Unity projects with 2D shooter templates  |
| 🏃 Player Movement        | Create player controllers with WASD/Arrow key controls   |
| 🔫 Shooting Mechanics     | Implement projectile systems and enemy interactions      |
| 📊 Level System           | Build progressive level systems with difficulty scaling  |
| 🎨 Scene Management       | Create and configure game scenes programmatically        |
| 🔧 Component Setup        | Add and configure Unity components via natural language  |

## 🚀 Quick Setup

### 📋 Prerequisites

- **Node.js** >= 18.0.0
- **Unity** 2022.3 LTS or later
- **Claude Desktop** app

### 📥 Installation

```bash
npm install -g @spark-apps/unity-mcp
```

### ⚙️ Configure Claude Desktop

1. **Open Claude Desktop settings** and locate the MCP configuration file:
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json`
   - **Windows**: `%APPDATA%/Claude/claude_desktop_config.json`
   - **Linux**: `~/.config/Claude/claude_desktop_config.json`

2. **Add Unity MCP server** to the configuration:

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

3. **Restart Claude Desktop**

4. **Verify installation**: Look for the 🔌 icon in Claude Desktop. Click it to see "unity-mcp" listed as a connected server.

## 🛠️ Available Tools

|                                                                            | Tool           | Description                                              |
| -------------------------------------------------------------------------- | -------------- | -------------------------------------------------------- |
| ![🎮](https://img.shields.io/badge/🎮-Project_Init-blue?style=flat-square) | **create_project**       | Create new Unity 2D shooter projects          |
| ![🏃](https://img.shields.io/badge/🏃-Player-green?style=flat-square)   | **setup_player**        | Create player with movement and shooting |
| ![👾](https://img.shields.io/badge/👾-Enemies-orange?style=flat-square) | **create_enemy** | Generate enemy prefabs with AI behavior                       |
| ![📊](https://img.shields.io/badge/📊-Levels-purple?style=flat-square)     | **setup_level**     | Create progressive level systems    |
| ![🔫](https://img.shields.io/badge/🔫-Weapons-teal?style=flat-square)          | **add_weapon**        | Add weapons and projectile systems     |
| ![🎯](https://img.shields.io/badge/🎯-Collision-indigo?style=flat-square)   | **setup_collision**     | Configure collision and physics      |

## 💬 Example Commands in Claude Desktop

<details>
<summary><strong>🎮 Project Creation</strong></summary>

> "Create a new 2D shooter game project called 'Space Invaders Clone'"

> "Initialize a basic top-down shooter"

> "Set up a side-scrolling shooter project"

</details>

<details>
<summary><strong>🏃 Player Setup</strong></summary>

> "Create a player that can move with WASD and shoot with spacebar"

> "Add a player controller with 5 units per second movement speed"

> "Set up player with health system and shooting mechanics"

</details>

<details>
<summary><strong>👾 Enemy System</strong></summary>

> "Create an enemy that moves toward the player and has 3 health"

> "Add a boss enemy with 50 health and special attacks"

> "Generate 5 different enemy types with varying behaviors"

</details>

<details>
<summary><strong>📊 Level Progression</strong></summary>

> "Create a 5-level progression system with increasing difficulty"

> "Add a level manager that spawns more enemies each level"

> "Set up level transitions with victory screens"

</details>

## 🔧 Using with Unity

### Workflow Integration

1. **Install Unity** (2022.3 LTS or later)
2. **Open Claude Desktop**
3. **Ask Claude to create your game** using natural language
4. **Open the generated project** in Unity Editor
5. **Test and iterate** with Claude's help

### Supported Unity Versions

- **Unity 2022.3 LTS** (recommended)
- **Unity 2023.x** (supported)
- **Unity 6** (supported)

## 🐛 Troubleshooting

### MCP Server Not Showing in Claude Desktop

1. Verify Node.js is installed: `node --version`
2. Check configuration file path is correct
3. Ensure JSON syntax is valid (use a JSON validator)
4. Restart Claude Desktop completely

### Need Help?

- 📖 [Documentation](https://github.com/muammar-yacoob/unity-mcp)
- 🐛 [Report Bug](https://github.com/muammar-yacoob/unity-mcp/issues)
- 💬 [Discussions](https://github.com/muammar-yacoob/unity-mcp/discussions)

## 📝 License

MIT © [Muammar Yacoob](https://github.com/muammar-yacoob)

## 🌟 Support

If you find this MCP server helpful:

- ⭐ Star the repository
- 💖 [Sponsor the project](https://github.com/sponsors/muammar-yacoob)
- 🐛 Report bugs and suggest features

---

**Built with ❤️ for the Unity and AI automation community**
