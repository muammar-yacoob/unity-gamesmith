# Unity GameSmith - Free Alternative to Unity Muse

> **Privacy-first, open-source AI code generation for Unity**

Stop paying $30/month for Unity Muse. Use free local AI (Ollama) or bring your own API. Browse 10+ battle-tested templates offline. Your code, your machine, zero telemetry.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](https://opensource.org/licenses/MIT)
[![Unity](https://img.shields.io/badge/Unity-2021.3+-blue?style=flat-square)](https://unity.com)
[![Free Alternative](https://img.shields.io/badge/Unity_Muse-Free_Alternative-green?style=flat-square)](https://github.com/muammar-yacoob/unity-gamesmith)
[![GitHub Stars](https://img.shields.io/github/stars/muammar-yacoob/unity-gamesmith?style=social)](https://github.com/muammar-yacoob/unity-gamesmith)

## 🆓 Why GameSmith Over Unity Muse?

| Feature | Unity Muse Chat | Unity GameSmith |
|---------|----------------|-----------------|
| **Cost** | $30/month subscription | **100% Free** |
| **Privacy** | Sends code to Unity cloud | **Runs locally, your machine** |
| **Offline** | Requires internet | **Works offline** |
| **AI Choice** | Unity's AI only | **Ollama, OpenAI, Claude, custom** |
| **Templates** | No template library | **10+ curated templates** |
| **Open Source** | Closed source | **MIT License** |
| **Learning** | Black box | **Study & customize templates** |
| **Enterprise** | Cloud-dependent | **On-premise deployment** |

## ⚡ Quick Start (60 Seconds)

**No signup, no credit card, no subscription. Just install and code.**

```bash
# 1. Install package in Unity via Git URL
Window → Package Manager → + → Add from git URL
https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage

# 2. Install free local AI (optional, for offline use)
curl -fsSL https://ollama.com/install.sh | sh
ollama pull codellama

# 3. Open in Unity and generate code
Tools → Unity GameSmith (Enhanced) → Template Library
```

**That's it. Free forever. No strings attached.** ✨

## ✨ What is Unity GameSmith?

Unity GameSmith is a Unity Editor tool that brings AI-powered code generation and a searchable template library directly into your Unity workflow. Inspired by the Unity Sketchfab Browser, it features a modern, intuitive interface for rapid game development - like a blacksmith forging legendary code for your games.

### 🎨 Two Interface Options

#### 1. Enhanced Window (Recommended)
**Menu:** `Tools → Unity GameSmith (Enhanced)`

Modern interface with Sketchfab-inspired design:
- 🔍 **Template Library** - Browse 10+ pre-built code templates
- 🌟 **Favorites System** - Star and save your favorites
- 📑 **Tab Navigation** - AI Generator, Template Library, Favorites
- 🎯 **Search & Filter** - Find templates by keyword or category
- 📄 **Pagination** - Navigate through results easily
- 🎴 **Grid Layout** - Beautiful card-based display
- 🤖 **AI Generation** - Natural language code generation

#### 2. Classic Window
**Menu:** `Tools → Unity GameSmith`

Streamlined interface focusing on:
- AI-powered code generation
- Quick action buttons
- Natural language commands
- Essential features only

## 🚀 Key Features

### 🆓 **Free & Privacy-First**
- **100% Free** - No subscriptions, no trials, no paywalls
- **Local AI** - Run Ollama locally, your code never leaves your machine
- **Zero Telemetry** - No tracking, no analytics, no data collection
- **Open Source** - MIT License, inspect and modify everything

### 🤖 **Multi-AI Support**
- **Ollama** (Free, local) - CodeLlama, DeepSeek Coder, Llama 2
- **OpenAI** - GPT-4, GPT-3.5 Turbo
- **Anthropic Claude** - Via API gateway
- **Custom APIs** - Any OpenAI-compatible endpoint
- **Your Choice** - Not locked into vendor AI

### 📚 **Template Library (Works Offline)**
- **10+ Production Templates** - Battle-tested code patterns
- **Instant Access** - No internet required for templates
- **Searchable** - Find by keyword, category, complexity
- **Favorites System** - Quick-access your most-used
- **Copy or Generate** - Use as-is or customize with AI

### 🎮 **Game Systems**
- **2D Shooter Mechanics** - Complete player, enemy, projectile systems
- **Player Controllers** - Movement, health, shooting, dash abilities
- **Enemy AI** - Chase, attack, spawning patterns
- **Level Management** - Wave-based progression
- **UI Components** - Health bars, score displays, game over screens

## 👥 Who Should Use GameSmith?

### Perfect For:
- 🎓 **Students & Learners** - Free tools, learn from templates
- 💼 **Indie Developers** - Can't afford $30/month subscriptions
- 🔒 **Privacy-Conscious Teams** - Keep code on your infrastructure
- 🏢 **Enterprise Studios** - Need on-premise, air-gapped solutions
- 🌍 **International Developers** - Limited access to Unity Muse
- 🧪 **Experimenters** - Want to try different AI models
- 📚 **Educators** - Teaching game development without subscriptions

### Use Cases:
- **Offline Development** - Work on planes, trains, remote locations
- **Corporate Networks** - Restricted internet, firewall policies
- **Proprietary Projects** - Code cannot leave your network
- **Cost-Sensitive Projects** - Budget constraints, no ongoing fees
- **AI Research** - Compare models, customize prompts
- **Learning Resources** - Study proven patterns from templates

## 📦 Installation

Choose your preferred installation method:

### Option 1: One-Click Install (Easiest) ⚡

**Coming Soon:** Once published to OpenUPM, installation will be as simple as clicking a link!

[![Install via OpenUPM](https://img.shields.io/badge/Install-OpenUPM-blue?style=for-the-badge)](https://package-installer.needle.tools/v1/installer/OpenUPM/com.spark-games.unity-gamesmith?registry=https://package.openupm.com)

*Note: Currently pending OpenUPM approval. Use Git URL method below in the meantime.*

### Option 2: Git URL (Recommended) 🔗

1. **Open Unity Package Manager**
   - `Window → Package Manager`

2. **Add package from Git URL**
   - Click `+` button → `Add package from git URL...`
   - Enter: `https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage`
   - Click `Add`

3. **Unity imports automatically**
   - Package will appear in Package Manager
   - Editor scripts will be compiled
   - Menu items will appear under `Tools`

### Option 3: OpenUPM CLI (For Advanced Users) 🚀

**Coming Soon:** Once published to OpenUPM:

```bash
# Install via OpenUPM CLI
openupm add com.spark-games.unity-gamesmith
```

### Option 4: Install from Disk (Local Development) 💻

1. **Clone this repository**
   ```bash
   git clone https://github.com/muammar-yacoob/unity-gamesmith.git
   ```

2. **Open Unity Package Manager**
   - `Window → Package Manager`

3. **Add package from disk**
   - Click `+` button → `Add package from disk...`
   - Navigate to `unity-gamesmith/UnityPackage/package.json`
   - Click `Open`

### Option 5: Manual Installation (Not Recommended) 📁

1. Clone or download this repository
2. Copy the `UnityPackage` folder to your project's `Packages` folder
3. Rename to `com.spark-games.unity-gamesmith`
4. Unity will auto-detect and import the package

---

**For OpenUPM submission status and detailed setup instructions, see [OPENUPM_SETUP.md](OPENUPM_SETUP.md)**

## ⚙️ Setup

### 1. Configure AI Backend

#### Option A: Ollama (100% Free, Privacy-First) ⭐ RECOMMENDED

**Why Ollama?**
- ✅ **Completely Free** - No API costs, no subscriptions
- ✅ **Runs Locally** - Your code never leaves your machine
- ✅ **Works Offline** - No internet required after model download
- ✅ **No Telemetry** - Zero data collection or tracking
- ✅ **Commercial Use** - Free for personal and commercial projects

**Install Ollama:**
```bash
# macOS/Linux
curl -fsSL https://ollama.com/install.sh | sh

# Windows
# Download from https://ollama.com/download
```

**Download a model (one-time, runs locally forever):**
```bash
# Best for code generation (recommended)
ollama pull codellama

# Alternative: Specialized code model
ollama pull deepseek-coder

# Alternative: General purpose
ollama pull llama2
```

**Configure in Unity:**
1. Open `Tools → Unity GameSmith (Enhanced)`
2. AI Configuration section:
   - **API URL:** `http://localhost:11434/api/generate`
   - **Model:** `codellama` (or model you pulled)
   - **API Key:** (leave empty)
   - **Timeout:** 120 seconds
   - **Temperature:** 0.7
3. Click **💾 Save Config**

#### Option B: OpenAI

**Configure in Unity:**
1. Get API key from https://platform.openai.com/api-keys
2. Open `Tools → Unity GameSmith (Enhanced)`
3. AI Configuration:
   - **API URL:** `https://api.openai.com/v1/chat/completions`
   - **Model:** `gpt-4` or `gpt-3.5-turbo`
   - **API Key:** Your OpenAI API key
   - **Timeout:** 120 seconds
   - **Temperature:** 0.7
4. Click **💾 Save Config**

#### Option C: Custom API

Any OpenAI-compatible API endpoint works:
- Local LLM servers (LM Studio, etc.)
- Cloud providers
- Custom deployments

### 2. Verify Installation

1. Check Unity Editor menu:
   - `Tools → Unity GameSmith (Enhanced)` ✅
   - `Tools → Unity GameSmith` ✅

2. Check Package Manager:
   - `Window → Package Manager`
   - Look for "Unity GameSmith" in list

3. Test template library:
   - Open Enhanced Window
   - Click **Template Library** tab
   - Should see 10+ templates

## 💡 Usage

### Enhanced Window Workflow

#### Browse Template Library
1. Open `Tools → Unity GameSmith (Enhanced)`
2. Click **Template Library** tab
3. Use search bar to find templates
4. Filter by category (Player, Enemy, UI, etc.)
5. Click template cards to:
   - 📋 **Copy Code** - Instant clipboard
   - ✨ **Use Template** - Generate in `Assets/Scripts/`
   - ⭐ **Favorite** - Add to Favorites tab

#### Use AI Generation
1. Open `Tools → Unity GameSmith (Enhanced)`
2. Click **AI Generator** tab
3. Use **Quick Actions** or
4. Type natural language command:
   ```
   "Create a player with dash ability and 150 health"
   "Generate a boss enemy that shoots in patterns"
   "Make a shield system that absorbs 3 hits"
   ```
5. Click **🚀 Execute Command**

#### Manage Favorites
1. Star templates in Template Library
2. Switch to **Favorites** tab
3. Quick access to your most-used templates

### Classic Window Workflow

1. Open `Tools → Unity GameSmith`
2. Configure AI in settings
3. Use Quick Actions or natural language commands
4. Scripts generated in `Assets/Scripts/`

## 📚 Available Templates

| Template | Category | Complexity | Description |
|----------|----------|-----------|-------------|
| 2D Player Controller | Player | ⭐⭐ | WASD movement with mouse aiming |
| Chase Enemy AI | Enemy | ⭐⭐ | Detection and player pursuit |
| Shooting System | Projectile | ⭐⭐ | Projectile-based weapons |
| Health System | Player | ⭐ | Damage and healing management |
| Wave Spawner | Level | ⭐⭐⭐ | Enemy waves with difficulty scaling |
| Health Bar UI | UI | ⭐ | Dynamic health display |
| Camera Follow | Camera | ⭐⭐ | Smooth player tracking |
| Dash Ability | Player | ⭐⭐ | Quick dash with cooldown |
| Power-up Pickup | Power-ups | ⭐⭐ | Collectible items |
| Particle Effect | Effects | ⭐ | Trigger particle systems |

## 🎯 Quick Start Example

```
1. Install package in Unity
2. Open Tools → Unity GameSmith (Enhanced)
3. Click Template Library tab
4. Search "player"
5. Click ⭐ on "2D Player Controller"
6. Click ✨ Use Template
7. PlayerController.cs created in Assets/Scripts/
8. Attach to Player GameObject
9. Press Play! 🎮
```

## 📂 Project Structure

```
unity-gamesmith/
├── .github/              # GitHub workflows
├── .git/                # Git repository
├── .gitignore           # Git ignore rules
├── LICENSE              # MIT license
├── README.md            # This file
├── CHANGELOG.md         # Version history
└── UnityPackage/        # Unity package content
    ├── package.json     # Package manifest
    ├── README.md        # Package documentation
    ├── INSTALLATION.md  # Setup guide
    ├── ENHANCED_FEATURES.md  # Feature documentation
    ├── Editor/          # Editor scripts (12 C# files)
    │   ├── EnhancedAIAgentWindow.cs
    │   ├── AITemplateLibrary.cs
    │   ├── UnityAIAgentWindow.cs
    │   ├── AIAgentConfig.cs
    │   ├── AIAgentClient.cs
    │   └── ...
    ├── Resources/       # Unity resources
    ├── Runtime/         # Runtime scripts (empty for now)
    ├── Templates/       # Template files
    └── Documentation/   # Extended docs
        ├── MIGRATION_NOTES.md
        └── INTEGRATION_COMPLETE.md
```

## 🔧 Configuration File

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

Edit this file or use the in-editor UI to configure.

## 🐛 Troubleshooting

### Package Not Showing
- Check `Packages/manifest.json` includes the package
- Try `Assets → Reimport All`
- Restart Unity Editor

### Menu Items Missing
- Check Console for compilation errors
- Verify all `.cs` files are in `Editor/` folder
- Try `Assets → Refresh`

### AI Connection Failed
- **Ollama:** Ensure server is running (`ollama serve`)
- **OpenAI:** Verify API key is correct
- Check firewall settings
- Test endpoint with curl

### Scripts Not Generating
- Check `Assets/Scripts/` folder exists
- Verify folder has write permissions
- Look for errors in Console
- Try `Assets → Refresh`

## 📊 Supported AI Providers

| Provider | Type | API URL | Cost |
|----------|------|---------|------|
| **Ollama** | Local | `http://localhost:11434/api/generate` | Free |
| **OpenAI** | Cloud | `https://api.openai.com/v1/chat/completions` | Paid |
| **Anthropic** | Cloud | Via proxy/gateway | Paid |
| **LM Studio** | Local | `http://localhost:1234/v1/chat/completions` | Free |
| **Custom** | Any | Your endpoint | Varies |

## ❓ FAQ - Unity Muse vs GameSmith

### Is GameSmith really free?
**Yes, 100% free.** MIT licensed open-source. No hidden costs, no trials, no premium tiers. With Ollama, you pay $0 forever.

### How is this different from Unity Muse Chat?
**Key differences:**
- **Cost:** GameSmith is free. Muse is $30/month.
- **Privacy:** GameSmith runs locally. Muse sends code to Unity's cloud.
- **Offline:** GameSmith works offline. Muse requires internet.
- **AI Choice:** GameSmith supports any AI. Muse locks you into Unity's AI.
- **Templates:** GameSmith has a template library. Muse doesn't.

### Will Unity Muse replace this?
**No.** GameSmith serves different needs:
- **Students/Learners:** Can't afford subscriptions
- **Privacy-focused:** Don't want code on external servers
- **Offline workers:** Need tools without internet
- **Enterprise:** Require on-premise solutions
- **Multi-AI users:** Want to experiment with different models

### Can I use both Unity Muse and GameSmith?
**Absolutely!** They complement each other. Use Muse for Unity-specific API questions, GameSmith for proven game patterns and privacy.

### Does GameSmith send my code anywhere?
**Not if you use Ollama.** With local AI, everything stays on your machine. With cloud APIs (OpenAI, Claude), only prompts are sent - you control what data leaves.

### How good is the free Ollama vs paid OpenAI?
**Surprisingly good!** CodeLlama and DeepSeek Coder are specifically trained for code generation. For game scripts, they often match GPT-4 quality at $0 cost.

### Is this legal for commercial projects?
**Yes.** MIT license allows commercial use. Ollama models are open-source. Your generated code is yours.

### Can my company use this in air-gapped networks?
**Yes.** Perfect for secure environments. Install Ollama, download models once, then completely offline. No external connections needed.

## 🎓 Requirements

- **Unity:** 2021.3 LTS or later
- **Platform:** Windows, macOS, Linux
- **AI Backend:** Ollama (recommended) or OpenAI API
- **.NET:** Standard 2.1 or 4.x

## 🤝 Contributing

Contributions welcome!

1. Fork the repository
2. Create feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open Pull Request

### Adding Templates

Edit `UnityPackage/Editor/AITemplateLibrary.cs`:

```csharp
new CodeTemplate
{
    id = "your_template",
    name = "Your Template",
    description = "What it does",
    category = "Category",
    tags = new[] { "tag1", "tag2" },
    complexity = 2,
    code = @"// Your C# code"
}
```

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details

## 🙏 Credits

- **Author:** Muammar Yacoob
- **GitHub:** https://github.com/muammar-yacoob
- **Inspired by:** Unity Sketchfab Browser

## 🔗 Links

- **GitHub:** https://github.com/muammar-yacoob/unity-gamesmith
- **Issues:** https://github.com/muammar-yacoob/unity-gamesmith/issues
- **Ollama:** https://ollama.com
- **Unity:** https://unity.com

## 🌟 Support the Free Alternative

**Help keep GameSmith free for everyone:**

⭐ **Star the repo** - Boost visibility, help others discover free tools
🐛 **Report issues** - Make it better for the community
🤝 **Contribute templates** - Share your game patterns
📢 **Spread the word** - Tell indie devs about free alternatives
💰 **Optional tips:** [Support development](https://tip.md/muammar-yacoob)

**Community over corporations. Free tools for all developers.**

---

**Built with ❤️ for indie developers, students, and teams who deserve free, privacy-first tools**

*Why pay $30/month when open-source can do it better?*

## 📜 Version History

See [CHANGELOG.md](CHANGELOG.md) for version history and updates.

## 🎥 Demo & Tutorials

Coming soon! Watch this space for:
- Video tutorials
- Setup guides
- Example projects
- Best practices
