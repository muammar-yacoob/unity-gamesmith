# Unity GameSmith - Installation Guide

## Prerequisites

- Unity 2021.3 LTS or later
- AI backend (one of):
  - **Ollama** (recommended for local/free use)
  - **OpenAI API** key
  - **Anthropic Claude** API key
  - Any OpenAI-compatible API endpoint

## Installation Methods

### Method 1: Unity Package Manager (Recommended)

1. Open your Unity project
2. Open Package Manager: `Window → Package Manager`
3. Click the `+` button in the top-left
4. Select `Add package from disk...`
5. Navigate to and select `UnityPackage/package.json`
6. Unity will import the package automatically

### Method 2: Manual Copy

1. Copy the entire `UnityPackage` folder
2. Paste it into your project's `Packages` directory:
   ```
   YourProject/
   └── Packages/
       └── UnityPackage/  <-- Paste here
   ```
3. Unity will detect and import it automatically

### Method 3: Git Submodule (Advanced)

```bash
cd YourProject/Packages/
git submodule add https://github.com/muammar-yacoob/unity-gamesmith.git unity-gamesmith
```

## Post-Installation

### 1. Verify Installation

Open Unity Editor and check:
- Menu bar has `Tools → Unity GameSmith` menu
- Menu has `Tools → Unity GameSmith → Generate` submenu

### 2. Open AI Agent Window

```
Unity Editor → Tools → Unity GameSmith
```

You should see a window with:
- AI Configuration section
- Quick Actions buttons
- Natural Language command interface

### 3. Setup AI Backend

#### Option A: Ollama (Local, Free)

**Install Ollama:**
```bash
# macOS/Linux
curl -fsSL https://ollama.com/install.sh | sh

# Windows
Download from https://ollama.com/download
```

**Download a model:**
```bash
ollama pull codellama
# or
ollama pull llama2
# or
ollama pull deepseek-coder
```

**Configure in Unity:**
- API URL: `http://localhost:11434/api/generate`
- Model: `codellama` (or whichever you pulled)
- API Key: (leave empty)
- Click **Save Config**

#### Option B: OpenAI

**Configure in Unity:**
- API URL: `https://api.openai.com/v1/chat/completions`
- Model: `gpt-4` or `gpt-3.5-turbo`
- API Key: Your OpenAI API key
- Click **Save Config**

#### Option C: Custom API

**Configure in Unity:**
- API URL: Your API endpoint
- Model: Your model name
- API Key: Your API key (if required)
- Click **Save Config**

### 4. Test Installation

In the Unity GameSmith window:
1. Click **Setup Player** button
2. Wait for processing
3. Check `Assets/Scripts/` folder
4. You should see:
   - `PlayerController.cs`
   - `PlayerHealth.cs`
   - `PlayerShooting.cs`

If scripts appear, installation is successful!

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

You can edit this file directly or use the Unity Editor interface.

## Troubleshooting

### Package Not Showing

**Problem:** Package Manager doesn't show the package

**Solution:**
1. Check `Packages/manifest.json` includes reference
2. Try `Assets → Reimport All`
3. Restart Unity Editor

### Menu Items Missing

**Problem:** `Tools → Unity GameSmith` menu doesn't appear

**Solution:**
1. Check Console for compilation errors
2. Verify all `.cs` files are in `UnityPackage/Editor/` folder
3. Try `Assets → Refresh`

### AI Connection Failed

**Problem:** "Connection failed" error when using AI features

**Solution:**
1. **Ollama users:** Ensure Ollama is running:
   ```bash
   ollama serve
   ```
2. **OpenAI users:** Verify API key is correct
3. Check firewall isn't blocking connections
4. Test URL in browser or curl:
   ```bash
   curl http://localhost:11434/api/generate
   ```

### Scripts Not Generating

**Problem:** Scripts don't appear in `Assets/Scripts/`

**Solution:**
1. Check Unity Console for errors
2. Verify `Assets/Scripts/` folder exists (create it if not)
3. Check folder permissions
4. Try `Assets → Refresh`

### Slow Response

**Problem:** AI takes too long to respond

**Solution:**
1. Increase timeout in configuration
2. Try a different/faster model
3. For Ollama: ensure model is already pulled
4. Check your internet connection (for cloud APIs)

## Uninstallation

To remove Unity GameSmith:

1. Delete `UnityPackage` folder from `Packages/`
2. Delete `AIAgentConfig.json` from project root
3. Delete any generated scripts you don't want

Unity will automatically unload the package.

## Upgrading

To upgrade to a new version:

1. Delete old `UnityPackage` folder
2. Copy new `UnityPackage` folder
3. Restart Unity Editor
4. Configuration file will be preserved

## System Requirements

### Minimum
- Unity 2021.3 LTS
- 4GB RAM
- 1GB free disk space
- Internet connection (for cloud AI APIs)

### Recommended
- Unity 2022.3 LTS or later
- 8GB RAM
- SSD storage
- For Ollama: 8GB RAM + GPU for faster generation

## Getting Help

- **Documentation:** See `README.md`
- **GitHub Issues:** https://github.com/muammar-yacoob/unity-gamesmith/issues
- **Discord:** Check README for server link
- **Logs:** Check `Logs/AIAgent.log` in your project

## Next Steps

After installation:
1. Read `README.md` for usage examples
2. Try Quick Actions buttons
3. Experiment with natural language commands
4. Generate your first complete game system!

---

Happy game development! 🎮✨
