# Unity GameSmith - Free Alternative to Unity Muse

> **Privacy-first, open-source AI code generation for Unity**

Forge legendary game code with AI - for free. No subscriptions, runs locally, works offline. The indie developer's alternative to Unity Muse Chat.

**🆓 100% Free** | **🔒 Privacy-First** | **📚 10+ Templates** | **🤖 Multi-AI Support**

## 🎨 Two Versions Available

### 1. Enhanced Window (Recommended)
**Menu:** `Tools → Unity GameSmith (Enhanced)`

Modern Sketchfab-inspired UI with:
- 🔍 **Template Library** - Browse 10+ pre-built code templates
- 🌟 **Favorites System** - Star and quick-access your favorites
- 📑 **Tabs** - AI Generator, Template Library, Favorites
- 🎯 **Search & Filter** - Find templates by keyword or category
- 📄 **Pagination** - Easy navigation through templates
- 🎨 **Grid Layout** - Beautiful card-based display

### 2. Classic Window
**Menu:** `Tools → Unity GameSmith`

Simplified interface focusing on:
- AI-powered code generation
- Quick action buttons
- Natural language commands

## Features

- 🤖 **AI Integration**: Connect to AI agents (Ollama, OpenAI, etc.) for code generation
- 🎮 **2D Shooter Templates**: Generate complete game systems
- 👤 **Player Systems**: Movement, health, and shooting mechanics
- 🤺 **Enemy AI**: Intelligent enemy behaviors and attacks
- 💥 **Projectile System**: Physics-based combat mechanics
- 📊 **Level Management**: Wave-based spawning and progression
- 🎨 **UI System**: Health bars, scores, and game screens
- 🔧 **Editor Integration**: Work directly in Unity Editor
- 📚 **Template Library**: 10+ pre-built, production-ready templates (Enhanced only)
- ⭐ **Favorites**: Save templates for quick access (Enhanced only)

## Installation

### Method 1: Unity Package Manager (Recommended)

1. Open Unity Editor
2. Window → Package Manager
3. Click "+" → Add package from disk
4. Navigate to and select `package.json` in this folder
5. Unity will import the package automatically

### Method 2: Manual Installation

1. Copy the `UnityPackage` folder to your Unity project's `Packages` directory
2. Unity will automatically detect and import the package

## Setup

### 1. Configure AI Agent

1. Open Unity Editor
2. Go to **Tools → Unity GameSmith**
3. In the AI Configuration section, set:
   - **API URL**: Your AI agent endpoint (e.g., `http://localhost:11434/api/generate` for Ollama)
   - **Model**: AI model name (e.g., `codellama`, `gpt-4`, etc.)
   - **API Key**: Optional, for services like OpenAI
   - **Timeout**: Request timeout in seconds (default: 120)
   - **Temperature**: AI creativity level 0-1 (default: 0.7)
4. Click **Save Config**

### 2. Set Up AI Backend (Example: Ollama)

```bash
# Install Ollama
curl -fsSL https://ollama.com/install.sh | sh

# Download a code model
ollama pull codellama

# Start Ollama server (usually runs automatically)
ollama serve
```

## Usage

### Quick Start (Enhanced Window)

1. Open **Tools → Unity GameSmith (Enhanced)** in Unity Editor
2. Choose your workflow:
   - **Template Library Tab**: Browse and use pre-built templates
   - **AI Generator Tab**: Generate custom code with AI
   - **Favorites Tab**: Access your starred templates

### Template Library (Enhanced Window Only)

Browse 10+ ready-to-use templates:
- Search by keyword
- Filter by category (Player, Enemy, UI, etc.)
- View complexity ratings
- Copy code or generate scripts instantly
- Add to favorites for quick access

### Quick Actions (Both Windows)

Use **Quick Actions** buttons for common tasks:
   - Create 2D Shooter Project
   - Setup Player
   - Create Enemy AI
   - Add Projectile System
   - Setup Level System
   - Create Game UI

### Natural Language Commands

Enter commands in the command box:

```
"Create a player with 5 movement speed and health of 150"
"Generate an enemy that shoots projectiles at the player"
"Make a boss enemy with 500 health that teleports"
```

### Menu Items

Access generators directly from the menu:

- **Tools → Unity GameSmith → Generate → Player System**
- **Tools → Unity GameSmith → Generate → Enemy System**
- **Tools → Unity GameSmith → Generate → Projectile System**
- **Tools → Unity GameSmith → Generate → Level System**
- **Tools → Unity GameSmith → Generate → UI System**

## Generated Scripts

All scripts are generated in `Assets/Scripts/` and include:

### Player System
- `PlayerController.cs` - Movement and rotation
- `PlayerHealth.cs` - Health management
- `PlayerShooting.cs` - Shooting mechanics

### Enemy System
- `EnemyAI.cs` - Chase and attack AI
- `EnemyHealth.cs` - Enemy health
- `EnemyAttack.cs` - Attack behavior

### Projectile System
- `Projectile.cs` - Bullet behavior
- `DamageDealer.cs` - Collision damage

### Level System
- `LevelManager.cs` - Level progression
- `WaveSpawner.cs` - Enemy spawning
- `SpawnPoint.cs` - Spawn locations

### UI System
- `HealthBar.cs` - Player health display
- `ScoreDisplay.cs` - Score tracking
- `GameOverScreen.cs` - Game over UI

## Configuration File

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

## Supported AI Providers

- **Ollama** (Local): `http://localhost:11434/api/generate`
- **OpenAI**: `https://api.openai.com/v1/chat/completions`
- **Anthropic Claude**: Via API gateway
- **Custom APIs**: Any OpenAI-compatible endpoint

## Troubleshooting

### Connection Failed
- Ensure AI backend is running (e.g., `ollama serve`)
- Check API URL is correct
- Verify firewall isn't blocking connections

### No Scripts Generated
- Check Unity Console for errors
- Verify `Assets/Scripts/` folder exists
- Check file permissions

### AI Not Responding
- Increase timeout in configuration
- Try a different model
- Check AI backend logs

## Logs

View detailed logs at: `Logs/AIAgent.log` in your project root

## Examples

### Create a Complete 2D Shooter

1. Click **Create 2D Shooter Project**
2. Wait for AI to generate all systems
3. Scripts will be created in `Assets/Scripts/`
4. Attach scripts to GameObjects as needed

### Custom Enemy Type

```
Natural Language Command:
"Create a flying enemy that shoots fireballs every 2 seconds and has 75 health"
```

The AI will generate a custom enemy script based on your requirements.

## Requirements

- Unity 2021.3 or later
- AI backend (Ollama, OpenAI API, etc.)
- Internet connection (for cloud AI) or local AI setup

## Support

- GitHub Issues: https://github.com/muammar-yacoob/unity-gamesmith/issues
- Documentation: https://github.com/muammar-yacoob/unity-gamesmith

## License

MIT License - See LICENSE file for details

## Credits

Created by Spark Games
https://github.com/muammar-yacoob
