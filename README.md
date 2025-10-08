# Unity AI Agent - Editor Tool

> **AI-powered Unity Editor tool for automated game development**

Create complete 2D shooters, player systems, enemy AI, and game mechanics using natural language commands and AI agents - all from within the Unity Editor.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](https://opensource.org/licenses/MIT)
[![GitHub Stars](https://img.shields.io/github/stars/muammar-yacoob/unity-mcp?style=social)](https://github.com/muammar-yacoob/unity-mcp)

## 🎮 What is Unity AI Agent?

Unity AI Agent is a Unity Editor extension that integrates AI capabilities directly into your Unity workflow. Instead of being an external code generator, it works **inside Unity** to help you create game systems through:

- Natural language commands
- AI-powered code generation
- Pre-built game system templates
- One-click component generation

## ✨ Features

- 🤖 **AI Integration** - Connect to Ollama, OpenAI, or any AI API
- 🎯 **Editor Window** - Beautiful Unity Editor interface
- 🎮 **2D Shooter Templates** - Complete game systems ready to use
- 👤 **Player Systems** - Movement, health, shooting mechanics
- 🤺 **Enemy AI** - Intelligent chase, attack, and combat behaviors
- 💥 **Projectile System** - Physics-based projectiles and damage
- 📊 **Level Management** - Wave-based spawning and progression
- 🎨 **UI System** - Health bars, scores, game over screens
- 🔧 **Script Generation** - Creates production-ready C# code

## 🚀 Quick Start

### Installation

1. **Import the Unity Package**
   ```
   - Copy the UnityPackage folder to your project's Packages directory
   - Unity will automatically detect and import it
   ```

2. **Open Unity AI Agent**
   ```
   Unity Editor → Tools → Unity AI Agent
   ```

3. **Configure AI Backend**
   ```
   - API URL: http://localhost:11434/api/generate (for Ollama)
   - Model: codellama
   - Click "Save Config"
   ```

4. **Start Creating!**
   - Use Quick Actions buttons
   - Or enter natural language commands
   - Or use menu items: Tools → Unity AI Agent → Generate

### Setup Ollama (Recommended Local AI)

```bash
# Install Ollama
curl -fsSL https://ollama.com/install.sh | sh

# Download codellama model
ollama pull codellama

# Server runs automatically
```

## 📖 Usage Examples

### Quick Actions (One-Click)

Open **Tools → Unity AI Agent** and click:
- **Create 2D Shooter Project** - Complete game template
- **Setup Player** - Movement, health, shooting
- **Create Enemy AI** - Chase and attack behavior
- **Add Projectile System** - Bullets and damage
- **Setup Level System** - Waves and spawning
- **Create Game UI** - Health, score, menus

### Natural Language Commands

```
"Create a player with dash ability and 150 health"
"Generate a boss enemy that shoots in a circle pattern"
"Make a power-up that gives the player triple shot"
"Create a shield system that absorbs 3 hits"
```

### Menu Integration

All generators are accessible via:
```
Tools → Unity AI Agent → Generate → [System Name]
```

## 📂 Generated Scripts

Scripts are created in `Assets/Scripts/` with production-ready code:

### Player System
- `PlayerController.cs` - WASD movement, mouse aim
- `PlayerHealth.cs` - Health with events
- `PlayerShooting.cs` - Shooting mechanics

### Enemy System
- `EnemyAI.cs` - Chase/attack AI with detection ranges
- `EnemyHealth.cs` - Health with death events
- `EnemyAttack.cs` - Attack behavior with cooldowns

### Projectile System
- `Projectile.cs` - Bullet physics and lifetime
- `DamageDealer.cs` - Collision damage handling

### Level System
- `LevelManager.cs` - Level progression
- `WaveSpawner.cs` - Enemy wave spawning
- `SpawnPoint.cs` - Spawn location markers

### UI System
- `HealthBar.cs` - Dynamic health display
- `ScoreDisplay.cs` - Score tracking
- `GameOverScreen.cs` - Game over UI

## ⚙️ Configuration

The tool creates `AIAgentConfig.json` in your project root:

```json
{
  "apiUrl": "http://localhost:11434/api/generate",
  "model": "codellama",
  "apiKey": "",
  "timeout": 120,
  "temperature": 0.7
}
```

### Supported AI Providers

| Provider | API URL | Notes |
|----------|---------|-------|
| **Ollama** (Local) | `http://localhost:11434/api/generate` | Free, runs locally |
| **OpenAI** | `https://api.openai.com/v1/chat/completions` | Requires API key |
| **Anthropic Claude** | Via proxy | Requires setup |
| **Custom** | Any OpenAI-compatible endpoint | Your own API |

## 🎯 Workflow Example

### Creating a Complete 2D Shooter

1. **Open Unity AI Agent Window**
   - Tools → Unity AI Agent

2. **Generate Player System**
   - Click "Setup Player" button
   - Scripts created in Assets/Scripts/
   - Attach to Player GameObject

3. **Create Enemy**
   - Click "Create Enemy AI"
   - Drag EnemyAI.cs to Enemy prefab
   - Set detection and attack ranges

4. **Add Projectiles**
   - Click "Add Projectile System"
   - Create Projectile prefab
   - Assign to PlayerShooting

5. **Setup Levels**
   - Click "Setup Level System"
   - Add WaveSpawner to scene
   - Configure spawn points and enemy prefabs

6. **Add UI**
   - Click "Create Game UI"
   - Build UI canvas with health bar
   - Attach HealthBar.cs script

## 🔧 Troubleshooting

### AI Connection Issues
```
❌ Connection failed
✅ Solution: Ensure AI backend is running (ollama serve)
```

### Scripts Not Generating
```
❌ No scripts created
✅ Solution: Check Assets/Scripts/ folder exists and has write permissions
```

### Slow AI Response
```
❌ Timeout error
✅ Solution: Increase timeout in config, or use faster model
```

### Compilation Errors
```
❌ Generated code doesn't compile
✅ Solution: Check Unity version compatibility, use better AI model
```

## 📊 Logs

View detailed logs at: `Logs/AIAgent.log` in project root

All operations are logged with timestamps for debugging.

## 🎓 Advanced Usage

### Custom Prompts

Modify prompts in `UnityAIAgentWindow.cs`:

```csharp
private string GeneratePrompt(string userCommand)
{
    return $@"Your custom prompt template here...
    {userCommand}
    ...additional instructions...";
}
```

### Extend Generators

Create new generators by following the pattern in existing files:
- Create new class in `Editor/` folder
- Use `[MenuItem]` attribute for menu integration
- Call `ScriptGeneratorUtility.CreateScript()`

## 📋 Requirements

- Unity 2021.3 or later
- AI backend (Ollama recommended)
- .NET 4.x or .NET Standard 2.1

## 🌱 Support & Contributions

⭐ **Star the repo** & I power up like Mario 🍄
☕ **Devs run on coffee** - Buy me one?
💰 **Crypto tips welcome** - [Tip in crypto](https://tip.md/muammar-yacoob)
🤝 **Contributions are welcome** - Fork, improve, PR!

## 📄 License

MIT License - see LICENSE file

## 🔗 Links

- **GitHub**: https://github.com/muammar-yacoob/unity-mcp
- **Issues**: https://github.com/muammar-yacoob/unity-mcp/issues
- **Ollama**: https://ollama.com

## 🙏 Credits

Created by **Spark Apps**
- GitHub: https://github.com/muammar-yacoob

---

**Note**: This tool generates code as a starting point. Always review and test generated scripts before using in production.

## 📜 Previous MCP Version

This repository previously contained an MCP (Model Context Protocol) server for external Unity automation. That code has been replaced with this Unity Editor extension for better integration and workflow. The MCP server code is archived in git history if needed.

---

**Built with ❤️ for the Unity and AI automation community**
