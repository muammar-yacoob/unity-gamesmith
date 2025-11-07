# Unity GameSmith

**AI-powered code generation and template library for Unity Editor**  
🎮 Free, open-source alternative to Unity Muse

---

![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)
![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3+-blue.svg?style=for-the-badge)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![AI Powered](https://img.shields.io/badge/AI-Powered-cc6600?style=for-the-badge)

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

## 📥 Installation

### Prerequisites

**⚠️ UniTask is required** - Install it first before installing GameSmith:

1. In Unity, open `Window → Package Manager → + → Add package from git URL`
2. Enter: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
3. Wait for Unity to import UniTask

### Install GameSmith

#### Option 1: One-Click Install (Recommended)

[![Install via Needle Package Installer](https://img.shields.io/badge/Install-Unity%20Package-blue?style=for-the-badge&logo=unity)](https://package-installer.needle.tools/v1/installer/github.com/muammar-yacoob/unity-gamesmith?upmPackagePath=/UnityPackage&registry=https://github.com/muammar-yacoob/unity-gamesmith.git)

Click the button above to install directly in Unity Editor.

#### Option 2: Manual Git URL

In Unity Package Manager (UPM): `Window → Package Manager → + → Add package from git URL`
```
https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage
```

After installation, access via **Tools → Game Smith** (Alt+G) in Unity Editor.

---

## ⚙️ Configuration

1. Open `Tools → Game Smith` (Alt+G)
2. Select AI provider from dropdown:
   - **Ollama** (Local, Free) ⭐ Recommended for privacy
   - **Claude** (Best quality)
   - **OpenAI** (Excellent quality)
   - **Custom** (Any OpenAI-compatible endpoint)
3. Click **Edit Config** to open the scriptable object
4. Configure your selected provider:
   - **API URL** - Endpoint URL
   - **Model** - Model name
   - **API Key** - Your API key (if required)
5. Save the configuration

<details>
<summary><b>📝 Provider Examples</b></summary>

**Ollama (Local):**
- API URL: `http://localhost:11434/api/generate`
- Model: `codellama`
- API Key: (leave empty)
- Setup: `ollama pull codellama`

**Claude:**
- API URL: `https://api.anthropic.com/v1/messages`
- Model: `claude-sonnet-4` or `claude-3-5-sonnet-20241022`
- API Key: Get from [console.anthropic.com](https://console.anthropic.com/)

**OpenAI:**
- API URL: `https://api.openai.com/v1/chat/completions`
- Model: `gpt-4` or `gpt-3.5-turbo`
- API Key: Get from [platform.openai.com](https://platform.openai.com/api-keys)

**Custom:**
- Works with LM Studio, LocalAI, and any OpenAI-compatible endpoint
- Configure URL, model, and key as needed

</details>

---

## 💡 Usage

<details>
<summary><b>📚 Template Library</b></summary>

1. `Tools → Game Smith` (Alt+G)
2. Click **Template Library** tab
3. Search/filter templates
4. Click **Use Template** to generate code

Scripts generate to `Assets/Scripts/`

</details>

<details>
<summary><b>🤖 AI Generation</b></summary>

1. `Tools → Game Smith` (Alt+G)
2. Click **AI Generator** tab
3. Enter command and click **Execute Command**

Scripts generate to `Assets/Scripts/`

</details>

---

## 💬 Example Commands

<details>
<summary><b>🎮 Player Systems</b></summary>

* _"Create a 2D player with WASD movement and jump"_
* _"Generate a player with dash ability and cooldown"_
* _"Make a character with wall jump mechanics"_
* _"Create a player with stamina system"_

</details>

<details>
<summary><b>👾 Enemy AI</b></summary>

* _"Generate boss enemy with 3 attack patterns"_
* _"Create chase enemy that patrols waypoints"_
* _"Make flying enemy that shoots projectiles"_
* _"Generate enemy spawner with wave system"_

</details>

<details>
<summary><b>⚔️ Combat & Weapons</b></summary>

* _"Create a shooting system with bullet spread"_
* _"Generate melee combat with combo system"_
* _"Make a health system with shields and armor"_
* _"Create weapon switching system"_

</details>

<details>
<summary><b>🎨 UI & Effects</b></summary>

* _"Generate dynamic health bar with smooth transitions"_
* _"Create damage number popup effect"_
* _"Make particle effect on enemy death"_
* _"Generate pause menu with settings"_

</details>

---

## 📦 Available Templates

### 🎯 Mechanics (Core Game Rules)

| Template | Description |
|---------|-------------|
| ![](https://img.shields.io/badge/🎮%20-0078D4?style=for-the-badge) ![2D Player Controller](https://img.shields.io/badge/2D%20Player%20Controller%20-4A9EFF?style=for-the-badge) | WASD movement + mouse aim |
| ![](https://img.shields.io/badge/👾%20-DC143C?style=for-the-badge) ![Chase Enemy AI](https://img.shields.io/badge/Chase%20Enemy%20AI%20-E74856?style=for-the-badge) | Detection and pursuit |
| ![](https://img.shields.io/badge/🔫%20-FF6B35?style=for-the-badge) ![Shooting System](https://img.shields.io/badge/Shooting%20System%20-FF8C42?style=for-the-badge) | Projectile weapons |
| ![](https://img.shields.io/badge/❤️%20-2ECC71?style=for-the-badge) ![Health System](https://img.shields.io/badge/Health%20System%20-27AE60?style=for-the-badge) | Damage/healing |
| ![](https://img.shields.io/badge/⚡%20-FFD700?style=for-the-badge) ![Dash Ability](https://img.shields.io/badge/Dash%20Ability%20-FFC107?style=for-the-badge) | Dash with cooldown |

### 🔄 Dynamics (Runtime Behavior)

| Template | Description |
|---------|-------------|
| ![](https://img.shields.io/badge/🌊%20-8E44AD?style=for-the-badge) ![Wave Spawner](https://img.shields.io/badge/Wave%20Spawner%20-9B59B6?style=for-the-badge) | Enemy wave spawning |
| ![](https://img.shields.io/badge/🎁%20-7CB342?style=for-the-badge) ![Power-up Pickup](https://img.shields.io/badge/Power--up%20Pickup%20-8BC34A?style=for-the-badge) | Collectibles |

### 🎨 Aesthetics (Visual & Audio Feedback)

| Template | Description |
|---------|-------------|
| ![](https://img.shields.io/badge/📊%20-E91E63?style=for-the-badge) ![Health Bar UI](https://img.shields.io/badge/Health%20Bar%20UI%20-F06292?style=for-the-badge) | Dynamic health display |
| ![](https://img.shields.io/badge/📷%20-00BCD4?style=for-the-badge) ![Camera Follow](https://img.shields.io/badge/Camera%20Follow%20-26C6DA?style=for-the-badge) | Smooth tracking |
| ![](https://img.shields.io/badge/✨%20-AB47BC?style=for-the-badge) ![Particle Effect](https://img.shields.io/badge/Particle%20Effect%20-BA68C8?style=for-the-badge) | Particle triggers |

---

## 🔌 MCP Integration (Advanced)

Unity GameSmith supports **Model Context Protocol (MCP)** for extended AI capabilities:

### 🎯 Taskmaster MCP
AI-powered task management and PRD parsing for structured project planning.

**Features**:
- Parse Product Requirements Documents into actionable tasks
- Dependency-aware task tracking
- Complexity analysis and effort estimation
- Progress tracking with AI assistance

### 🌐 Chrome DevTools MCP
Automated WebGL testing and performance profiling for Unity builds.

**Features**:
- Automated browser testing with GPU acceleration
- Performance profiling (CPU, memory, GPU)
- Console log monitoring and debugging
- Visual regression testing with screenshots

### 📚 Documentation
- [MCP Integration Guide](docs/mcp-integration.md) - Complete setup and usage guide
- [WebGL Testing Guide](docs/webgl-testing.md) - Detailed testing workflows

**Configuration**: `.mcp.json` is pre-configured in the repository.

---

## 📋 Requirements

- Unity 2021.3 LTS or later
- .NET Standard 2.1 or 4.x
- **UniTask** - Required dependency (async/await support)
- AI backend: Ollama (recommended) or API key
- **Optional**: Node.js 16+ (for MCP servers)

---

## 🔧 Troubleshooting

<details>
<summary><b>"Cysharp" or "UniTask" errors</b></summary>

You need to install UniTask first:
1. `Window → Package Manager → + → Add package from git URL`
2. Enter: `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
3. Wait for import to complete
4. GameSmith should compile successfully

</details>

<details>
<summary><b>Package not showing</b></summary>

- Check `Packages/manifest.json`
- `Assets → Reimport All`
- Restart Unity

</details>

<details>
<summary><b>AI connection failed</b></summary>

- Ollama: Ensure `ollama serve` is running
- OpenAI: Verify API key
- Check firewall settings

</details>

<details>
<summary><b>Scripts not generating</b></summary>

- Create `Assets/Scripts/` folder
- Check write permissions
- View Console for errors

</details>

---

## 🌱 Support & Contributions

⭐ **Star the repo** & I power up like Mario 🍄  
☕ **Devs run on coffee** - [Buy me one?](https://www.buymeacoffee.com/muammaryacoob)  
💰 **Crypto tips welcome** - [Tip in crypto](https://muammar-yacoob.github.io/crypto-tip/)  
🤝 **Contributions are welcome** - 🍴 Fork, improve, PR!  
🎥 **Need help?** [YouTube Setup Tutorial](https://youtube.com/@muammaryacoob) • [Discord](https://discord.gg/muammaryacoob)

## 💖 Sponsor

Your support helps maintain and improve the tool. please consider sponsoring the project.

---

**Made with ❤️ for Unity Developers** • [Privacy Policy](https://github.com/muammar-yacoob/unity-gamesmith) • [Terms of Service](https://github.com/muammar-yacoob/unity-gamesmith)
