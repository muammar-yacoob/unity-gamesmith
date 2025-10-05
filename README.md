# ⚡ Unity MCP: AI-Powered Game Development

**🎮 Automate Unity development • 🤖 AI-powered workflows • 🚀 Natural language game creation**

[![npm version](https://img.shields.io/npm/v/@spark-apps/unity-mcp?style=flat-square)](https://www.npmjs.com/package/@spark-apps/unity-mcp)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](https://opensource.org/licenses/MIT)
[![GitHub Sponsors](https://img.shields.io/github/sponsors/muammar-yacoob?style=social)](https://github.com/sponsors/muammar-yacoob)
[![Report Bug](https://img.shields.io/badge/Report-Bug-red?style=flat-square)](https://github.com/muammar-yacoob/unity-mcp/issues)
[![GitHub Stars](https://img.shields.io/github/stars/muammar-yacoob/unity-mcp?style=social)](https://github.com/muammar-yacoob/unity-mcp)

[discord-link]: https://discord.gg/5skXfKRytR

## ✨ What It Does

Control the Unity game engine through natural language using AI coding assistants like Claude Desktop. This Model Context Protocol (MCP) server enables you to rapidly prototype, build, and manage Unity projects by simply describing your game ideas.

| Feature                   | Description                                              |
| ------------------------- | -------------------------------------------------------- |
| 🎮 Project Creation       | Initialize new Unity projects with templates             |
| 🏃 Player Movement        | Create player controllers with customizable inputs       |
| 🔫 Shooting Mechanics     | Implement projectile systems and combat interactions     |
| 📊 Level System           | Build progressive level systems with difficulty scaling  |
| 🎨 UI System              | Generate game HUDs, menus, and screens                   |
| 🔧 Component Setup        | Add and configure Unity components via natural language  |
| 🎭 3D Character Import    | Search and import rigged 3D models from Sketchfab       |
| 📹 Cinemachine Camera     | Automatic camera follow system for 3D characters         |
| 🎬 Scene Management       | Create and configure game scenes programmatically        |

## 🚀 Quick Setup

### 📋 Prerequisites

- **Node.js** >= 18.0.0
- **Unity** 2022.3 LTS or later
- **AI Coding Assistant** (e.g., Claude Desktop, Cursor, VS Code with MCP support)

### 📥 Installation

```bash
npm install -g @spark-apps/unity-mcp
```

### ⚙️ Configure Your AI Assistant

1. **Locate your AI assistant's MCP configuration file** (e.g., `claude_desktop_config.json` for Claude Desktop, `.cursor/mcp.json` for Cursor):
   - **macOS**: `~/Library/Application Support/Claude/claude_desktop_config.json` or `~/.cursor/mcp.json`
   - **Windows**: `%APPDATA%/Claude/claude_desktop_config.json` or `%APPDATA%/Cursor/.cursor/mcp.json`
   - **Linux**: `~/.config/Claude/claude_desktop_config.json` or `~/.config/cursor/.cursor/mcp.json`

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

3. **Restart your AI assistant** for changes to take effect.

4. **Verify installation**: Look for the 🔌 icon or a similar indicator in your AI assistant. Click it to see "unity-mcp" listed as a connected server.

## 🛠️ Available Tools

|                                                                            | Tool                       | Description                                              |
| -------------------------------------------------------------------------- | -------------------------- | -------------------------------------------------------- |
| ![🎮](https://img.shields.io/badge/🎮-Project_Init-blue?style=flat-square) | **create_unity_project**   | Create new Unity projects with specified templates       |
| ![🏃](https://img.shields.io/badge/🏃-Player-green?style=flat-square)   | **setup_player**           | Create player character with movement, shooting, health  |
| ![🔫](https://img.shields.io/badge/🔫-Projectiles-teal?style=flat-square) | **create_projectile_system** | Add weapon and projectile mechanics                   |
| ![👾](https://img.shields.io/badge/👾-Enemies-orange?style=flat-square) | **create_enemy**           | Generate enemy prefabs with AI behavior and attributes   |
| ![📊](https://img.shields.io/badge/📊-Levels-purple?style=flat-square)     | **setup_level_system**     | Create wave-based level progression and difficulty       |
| ![🎨](https://img.shields.io/badge/🎨-UI-pink?style=flat-square)     | **create_game_ui**         | Build HUD, menus, and other game screens                 |
| ![🎯](https://img.shields.io/badge/🎯-Collision-indigo?style=flat-square) | **setup_collision_system** | Configure physics layers, collision matrix, damage       |
| ![🎭](https://img.shields.io/badge/🎭-3D_Character-red?style=flat-square)   | **import_3d_character**    | Import rigged 3D models from Sketchfab with animations   |
| ![🎬](https://img.shields.io/badge/🎬-Scene-yellow?style=flat-square)   | **setup_scene_structure**  | Create organized scene hierarchy with camera & lighting  |

## 💬 Example Commands in Your AI Assistant

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

<details>
<summary><strong>🎭 3D Character Import</strong></summary>

> "Import a 3D character from Sketchfab with walk animation"

> "Search Sketchfab for a female character and set up controls"

> "Create a 3D character controller with Cinemachine camera"

> "Add a robot character with WASD movement and jump"

</details>
---

## 🌱 Support & Contributions

⭐ **Star the repo** & I power up like Mario 🍄  
☕ **Devs run on coffee** - [Buy me one?][coffee-link]  
💰 **Crypto tips welcome** - [Tip in crypto](https://tip.md/muammar-yacoob)  
🤝 **Contributions are welcome** - [🍴 Fork][fork-link], improve, PR!  
🎥 **Need help?** <img src="https://img.icons8.com/color/20/youtube-play.png" alt="YouTube" width="20" height="20" style="vertical-align: middle;"> [Setup Tutorial][vid-link] • <img src="https://img.icons8.com/color/20/discord--v2.png" alt="Discord" width="20" height="20" style="vertical-align: middle;"> [Join Discord][discord-link]

## 💖 Sponsor
Your support helps maintain and improve the tool. please consider [sponsoring the project][stars-link]. 


---

**Built with ❤️ for the Unity and AI automation community**
