[//]: # (Constants)

[license-link]: ../../blob/main/LICENSE
[stars-link]: ../../stargazers
[issues-link]: ../../issues
[discord-link]: https://discord.gg/muammaryacoob
[coffee-link]: https://www.buymeacoffee.com/muammaryacoob
[release-link]: ../../releases
[fork-link]: ../../fork

# Unity GameSmith

<div align="center">

**🎮 AI chat for Unity Editor · 🔌 MCP tooling · ⚙️ Zero configuration**

[![Unity Package](https://img.shields.io/badge/Unity%20Package-UPM-222?style=flat-square&logo=unity)][release-link]
[![MIT](https://img.shields.io/badge/License-MIT-blueviolet?style=flat-square)][license-link]
[![Discord](https://img.shields.io/badge/Discord-Join-blue?logo=discord&logoColor=white&style=flat-square)][discord-link]
[![GitHub Sponsors](https://img.shields.io/github/sponsors/muammar-yacoob?label=Sponsor&logo=github-sponsors&logoColor=white&color=hotpink&style=flat-square)][coffee-link]
[![GitHub Stars](https://img.shields.io/github/stars/muammar-yacoob/unity-gamesmith?style=social)][stars-link]

</div>

---

### 🔌 Unity MCP Powered

Unity GameSmith bundles the open-source [unity-mcp](https://github.com/muammar-yacoob/unity-mcp) server, giving the editor access to 30+ Model Context Protocol tools for scene management, automation, and testing directly from the dockable chat window.

---

## 🚀 Features

| Feature | Description |
|---------|-------------|
| ![](https://img.shields.io/badge/💬%20-5865F2?style=for-the-badge) **AI Assistant Chat** | Dockable chat panel with retry, history, and one-click tool results inside the Unity Editor |
| ![](https://img.shields.io/badge/🔌%20-1ABC9C?style=for-the-badge) **Unity MCP Tools** | 30+ editor operations exposed via MCP (hierarchy queries, scene save/load, playmode control, asset utilities) |
| ![](https://img.shields.io/badge/⚙️%20-ED4245?style=for-the-badge) **Unified Settings** | Modern settings window to switch providers (Ollama, Claude, OpenAI, custom) and manage configuration |
| ![](https://img.shields.io/badge/🛡️%20-2ECC71?style=for-the-badge) **Zero Configuration** | Auto-generates settings, history, and MCP wiring the first time you open the window |

---

## 📦 Installation

### Option 1: One-Click (Needle Package Installer)

[![Install via Needle Package Installer](https://img.shields.io/badge/Install-Unity%20Package-blue?style=for-the-badge&logo=unity)](https://package-installer.needle.tools/v1/installer/github.com/muammar-yacoob/unity-gamesmith?upmPackagePath=/UnityPackage&registry=https://github.com/muammar-yacoob/unity-gamesmith.git)

### Option 2: Git URL (Unity Package Manager)

`Window → Package Manager → + → Add package from git URL`
```
https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage
```

After installation open **Tools → Game Smith → GameSmith AI** (Alt/⌥ + G).

---

## ⚙️ Configuration

1. Open the GameSmith AI window (Tools → Game Smith → GameSmith AI).
2. Click the settings gear to pick an AI provider:
   - **Ollama** – Local, private inference
   - **Claude** – Anthropic’s API
   - **OpenAI** – GPT-4/3.5 family
   - **Custom** – Any OpenAI-compatible endpoint
3. Provide API keys or local endpoints as required.
4. Unity automatically stores preferences in `ProjectSettings/GameSmithSettings.json` (already git-ignored).

---

## 💡 Usage

- **Chat-driven workflow** – Ask natural language questions (“list objects in the scene”, “save the current scene”, “create a todo list from hierarchies”).
- **Automatic tool execution** – When MCP tools are available, the assistant runs them instantly and displays the output in-line.
- **Retry and history** – Use the chat footer buttons to retry or clear the conversation.

### Common MCP Commands

| Command | Result |
|---------|--------|
| `list objects` | Calls `unity_get_hierarchy` and prints the scene tree |
| `save scene` | Invokes `unity_save_scene` |
| `enter play mode` / `exit play mode` | Controls the editor play state |
| `get console logs` | Streams the latest Unity console output |

*All MCP tooling is backed by the bundled unity-mcp server, so no external setup is needed.*

---

## 📋 Requirements

- Unity **2021.3 LTS** or newer
- Node.js 16+ (only required if you want to run MCP servers separately)
- Optional AI backend: Ollama (local) or external API keys (Claude, OpenAI, custom)

---

## 🤝 Support & Contributions

- ⭐ Star the repo if GameSmith speeds up your workflow
- ☕ [Buy me a coffee](https://www.buymeacoffee.com/muammaryacoob)
- 💬 Join the discussion on [Discord](https://discord.gg/muammaryacoob)
- 🐞 Issues & feature requests: [GitHub Issues](https://github.com/muammar-yacoob/unity-gamesmith/issues)

---

Made with ❤️ for Unity developers. The bundled MCP tooling is powered by the open-source [unity-mcp](https://github.com/muammar-yacoob/unity-mcp).
