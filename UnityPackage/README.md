# Unity GameSmith Package

**AI-powered code generation and template library for Unity Editor**  
🎮 Free, open-source alternative to Unity Muse

---

## 🚀 Features

| Feature | Description |
|---------|-------------|
| ![](https://img.shields.io/badge/🤖%20-5865F2?style=for-the-badge) ![AI Code Generation](https://img.shields.io/badge/AI%20Code%20Generation%20-7289DA?style=for-the-badge) | Natural language to Unity C# scripts |
| ![](https://img.shields.io/badge/📚%20-57F287?style=for-the-badge) ![Template Library](https://img.shields.io/badge/Template%20Library%20-3BA55D?style=for-the-badge) | 10+ pre-built game systems (player, enemy, UI, etc.) |
| ![](https://img.shields.io/badge/🔌%20-ED4245?style=for-the-badge) ![Multi-AI Support](https://img.shields.io/badge/Multi--AI%20Support%20-E67E22?style=for-the-badge) | Claude (recommended), Ollama (local/free), OpenAI, custom endpoints |
| ![](https://img.shields.io/badge/🔒%20-5865F2?style=for-the-badge) ![Privacy-First](https://img.shields.io/badge/Privacy--First%20-3498DB?style=for-the-badge) | Run locally with Ollama, no telemetry |
| ![](https://img.shields.io/badge/📴%20-1ABC9C?style=for-the-badge) ![Works Offline](https://img.shields.io/badge/Works%20Offline%20-16A085?style=for-the-badge) | Templates and local AI work without internet |

---

## 🚀 Quick Start

After installation, access via:
- **Main Window:** `Tools → Game Smith` (Alt+G)
- **Legacy Windows:** `Tools → Legacy/` (deprecated)

---

## ⚙️ Configuration

### Ollama (Local, Free) - Recommended

1. Install Ollama: `curl -fsSL https://ollama.com/install.sh | sh`
2. Pull model: `ollama pull codellama`
3. In Unity `Tools → Game Smith`:
   - **API URL:** `http://localhost:11434/api/generate`
   - **Model:** `codellama`
   - **API Key:** (leave empty)
4. Click **Save**

### Claude (Best Quality)

1. Get API key: https://console.anthropic.com/
2. Configure:
   - **API URL:** `https://api.anthropic.com/v1/messages`
   - **Model:** `claude-sonnet-4`
   - **API Key:** Your key
3. Click **Save**

### OpenAI

1. Get API key: https://platform.openai.com/api-keys
2. Configure:
   - **API URL:** `https://api.openai.com/v1/chat/completions`
   - **Model:** `gpt-4` or `gpt-3.5-turbo`
   - **API Key:** Your key
3. Click **Save**

### Custom API

Any OpenAI-compatible endpoint works (LM Studio, etc.)

---

## 💡 Usage

### 4 Tabs Available

1. **AI Generator** - Natural language to C# scripts
2. **Template Library** - Browse 10+ pre-built templates
3. **Favorites** - Quick access to starred templates
4. **Quick Actions** - One-click system generators

### Example Commands

**AI Generator Tab:**
```
"Create a player with dash ability and 150 health"
"Generate boss enemy with attack patterns"
"Make a health system with shields"
```

**Template Library Tab:**
- Search/filter templates
- Click "Use" to generate scripts
- Star favorites for quick access

**Quick Actions Tab:**
- One-click complete systems
- Player, Enemy, Projectile, Level, UI generators

---

## 📦 Available Templates

| Template | Category | Description |
|---------|----------|-------------|
| ![](https://img.shields.io/badge/🎮%20-0078D4?style=for-the-badge) ![2D Player Controller](https://img.shields.io/badge/2D%20Player%20Controller%20-4A9EFF?style=for-the-badge) | WASD movement + mouse aim |
| ![](https://img.shields.io/badge/👾%20-DC143C?style=for-the-badge) ![Chase Enemy AI](https://img.shields.io/badge/Chase%20Enemy%20AI%20-E74856?style=for-the-badge) | Detection and pursuit |
| ![](https://img.shields.io/badge/🔫%20-FF6B35?style=for-the-badge) ![Shooting System](https://img.shields.io/badge/Shooting%20System%20-FF8C42?style=for-the-badge) | Projectile weapons |
| ![](https://img.shields.io/badge/❤️%20-2ECC71?style=for-the-badge) ![Health System](https://img.shields.io/badge/Health%20System%20-27AE60?style=for-the-badge) | Damage/healing |
| ![](https://img.shields.io/badge/🌊%20-8E44AD?style=for-the-badge) ![Wave Spawner](https://img.shields.io/badge/Wave%20Spawner%20-9B59B6?style=for-the-badge) | Enemy wave spawning |
| ![](https://img.shields.io/badge/📊%20-E91E63?style=for-the-badge) ![Health Bar UI](https://img.shields.io/badge/Health%20Bar%20UI%20-F06292?style=for-the-badge) | Dynamic health display |
| ![](https://img.shields.io/badge/📷%20-00BCD4?style=for-the-badge) ![Camera Follow](https://img.shields.io/badge/Camera%20Follow%20-26C6DA?style=for-the-badge) | Smooth tracking |
| ![](https://img.shields.io/badge/⚡%20-FFD700?style=for-the-badge) ![Dash Ability](https://img.shields.io/badge/Dash%20Ability%20-FFC107?style=for-the-badge) | Dash with cooldown |
| ![](https://img.shields.io/badge/🎁%20-7CB342?style=for-the-badge) ![Power-up Pickup](https://img.shields.io/badge/Power--up%20Pickup%20-8BC34A?style=for-the-badge) | Collectibles |
| ![](https://img.shields.io/badge/✨%20-AB47BC?style=for-the-badge) ![Particle Effect](https://img.shields.io/badge/Particle%20Effect%20-BA68C8?style=for-the-badge) | Particle triggers |

---

## 🤖 Supported AI Providers

| Provider | Type | Cost | Quality |
|----------|------|------|---------|
| **Ollama** | Local | Free | Good |
| **Claude** ⭐ | Cloud | Paid | Best |
| **OpenAI** | Cloud | Paid | Excellent |
| **LM Studio** | Local | Free | Good |
| **Custom** | Any | Varies | Varies |

---

## 📋 Requirements

- Unity 2021.3 LTS or later
- .NET Standard 2.1 or 4.x
- AI backend: Ollama (recommended) or API key

---

## 🔧 Troubleshooting

**Package not showing:**
- Check `Packages/manifest.json`
- `Assets → Reimport All`
- Restart Unity

**AI connection failed:**
- Ollama: Ensure `ollama serve` is running
- OpenAI/Claude: Verify API key
- Check firewall settings

**Scripts not generating:**
- Create `Assets/Scripts/` folder
- Check write permissions
- View Console for errors

---

## 📝 Generated Scripts Location

All scripts are created in: `Assets/Scripts/`

---

## 🔗 Links

- **GitHub:** https://github.com/muammar-yacoob/unity-gamesmith
- **Issues:** https://github.com/muammar-yacoob/unity-gamesmith/issues
- **Ollama:** https://ollama.com
- **Documentation:** See main repository README

---

## 📄 License

MIT License - See LICENSE file for details

## 🙏 Credits

- **Author:** Muammar Yacoob
- **GitHub:** https://github.com/muammar-yacoob

---

**Made with ❤️ for Unity Developers**
