# Unity GameSmith Package

**AI-powered code generation and template library for Unity Editor**
Free, open-source alternative to Unity Muse

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
