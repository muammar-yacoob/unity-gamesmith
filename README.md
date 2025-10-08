# Unity GameSmith

AI-powered code generation and template library for Unity Editor. Free, open-source alternative to Unity Muse.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3+-blue.svg)](https://unity.com)

## Features

- **AI Code Generation** - Natural language to Unity C# scripts
- **Template Library** - 10+ pre-built game systems (player, enemy, UI, etc.)
- **Multi-AI Support** - Ollama (local/free), OpenAI, Claude, custom endpoints
- **Privacy-First** - Run locally with Ollama, no telemetry
- **Works Offline** - Templates and local AI work without internet

## Installation

### Via Git URL (Recommended)

1. Open Unity Package Manager: `Window → Package Manager`
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage`

### From Disk

1. Clone: `git clone https://github.com/muammar-yacoob/unity-gamesmith.git`
2. In Unity: `Window → Package Manager → + → Add package from disk`
3. Select `unity-gamesmith/UnityPackage/package.json`

## Quick Start

```bash
# 1. Install Ollama (optional, for free local AI)
curl -fsSL https://ollama.com/install.sh | sh
ollama pull codellama

# 2. In Unity: Window → Package Manager → + → Add from git URL
# https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage

# 3. Open: Tools → Unity GameSmith (Enhanced)
```

## Configuration

### Ollama (Local, Free)

1. Open `Tools → Unity GameSmith (Enhanced)`
2. Configure:
   - **API URL:** `http://localhost:11434/api/generate`
   - **Model:** `codellama`
   - **API Key:** (leave empty)
3. Click **Save Config**

### OpenAI

1. Get API key from https://platform.openai.com/api-keys
2. Configure:
   - **API URL:** `https://api.openai.com/v1/chat/completions`
   - **Model:** `gpt-4` or `gpt-3.5-turbo`
   - **API Key:** Your key
3. Click **Save Config**

### Custom API

Any OpenAI-compatible endpoint works (LM Studio, custom servers, etc.)

## Usage

### Template Library

1. `Tools → Unity GameSmith (Enhanced)`
2. Click **Template Library** tab
3. Search/filter templates
4. Click **Use Template** to generate code

### AI Generation

1. `Tools → Unity GameSmith (Enhanced)`
2. Click **AI Generator** tab
3. Enter command:
   ```
   "Create a player with dash ability"
   "Generate boss enemy with attack patterns"
   "Make a health system with shields"
   ```
4. Click **Execute Command**

Scripts generate to `Assets/Scripts/`

## Available Templates

| Template | Category | Description |
|----------|----------|-------------|
| 2D Player Controller | Player | WASD movement + mouse aim |
| Chase Enemy AI | Enemy | Detection and pursuit |
| Shooting System | Projectile | Projectile weapons |
| Health System | Player | Damage/healing |
| Wave Spawner | Level | Enemy wave spawning |
| Health Bar UI | UI | Dynamic health display |
| Camera Follow | Camera | Smooth tracking |
| Dash Ability | Player | Dash with cooldown |
| Power-up Pickup | Power-ups | Collectibles |
| Particle Effect | Effects | Particle triggers |

## AI Providers

| Provider | Type | Cost |
|----------|------|------|
| Ollama | Local | Free |
| OpenAI | Cloud | Paid |
| LM Studio | Local | Free |
| Custom | Any | Varies |

## Requirements

- Unity 2021.3 LTS or later
- .NET Standard 2.1 or 4.x
- AI backend: Ollama (recommended) or API key

## Troubleshooting

**Package not showing:**
- Check `Packages/manifest.json`
- `Assets → Reimport All`
- Restart Unity

**AI connection failed:**
- Ollama: Ensure `ollama serve` is running
- OpenAI: Verify API key
- Check firewall settings

**Scripts not generating:**
- Create `Assets/Scripts/` folder
- Check write permissions
- View Console for errors

## Contributing

1. Fork repository
2. Create branch: `git checkout -b feature/name`
3. Commit: `git commit -m 'Add feature'`
4. Push: `git push origin feature/name`
5. Open Pull Request

## License

MIT License - see [LICENSE](LICENSE)

## Links

- [GitHub Repository](https://github.com/muammar-yacoob/unity-gamesmith)
- [Issues](https://github.com/muammar-yacoob/unity-gamesmith/issues)
- [Ollama](https://ollama.com)
