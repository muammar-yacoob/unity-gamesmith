# Getting Started with GameSmith

Welcome! GameSmith is your **AI-powered Unity assistant** that helps you build games faster.

## 🚀 Zero-Setup Installation

### 1. Import the Package

**Via Unity Package Manager:**
```
Window → Package Manager → + → Add package from git URL
```
Enter: `https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage`

**Or Manual Install:**
1. Download from [GitHub](https://github.com/muammar-yacoob/unity-gamesmith)
2. Copy `UnityPackage` folder to your project's `Packages` folder
3. Restart Unity

### 2. Automatic Setup ✨

**On first launch, GameSmith automatically:**
- ✓ Creates settings file (`ProjectSettings/GameSmithSettings.json`)
- ✓ Loads AI provider configurations
- ✓ Shows a friendly welcome window
- ✓ Initializes with smart defaults

**No manual configuration needed!**

### 3. Choose Your AI Provider

GameSmith supports multiple AI providers. Choose one:

#### 🟦 Claude (Anthropic) - **Recommended**
- **Best for:** Code generation, reasoning, complex tasks
- **Free tier:** Yes ($5 free credits)
- **Get API key:** [console.anthropic.com](https://console.anthropic.com/account/keys)

#### 🔷 Gemini (Google)
- **Best for:** Fast responses, general tasks
- **Free tier:** Yes (generous limits)
- **Get API key:** [aistudio.google.com](https://aistudio.google.com/app/apikey)

#### 🟩 Ollama - **100% Free & Private**
- **Best for:** Complete privacy, offline work, no API costs
- **Free tier:** 100% free forever
- **Setup:** [Download Ollama](https://ollama.ai) → Run `ollama pull llama2`
- **No API key needed!**

#### 🟧 OpenAI
- **Best for:** GPT-4 access
- **Free tier:** No (pay-per-use)
- **Get API key:** [platform.openai.com](https://platform.openai.com/api-keys)

#### 🟪 OpenRouter
- **Best for:** Access to multiple models, competitive pricing
- **Free tier:** Yes (limited)
- **Get API key:** [openrouter.ai](https://openrouter.ai/keys)

### 4. Quick Setup (2 minutes)

1. **Open GameSmith:**
   - Menu: `Tools → GameSmith → Open Window`
   - Keyboard: `Ctrl+Shift+G` (Windows) or `Cmd+Shift+G` (Mac)

2. **Click the error message** (if you see one) or use:
   - Menu: `Tools → GameSmith → Configure Settings`

3. **Enter your API key:**
   - Select your provider (e.g., "Claude")
   - Paste your API key
   - Press Enter to verify
   - ✓ Green dot = Ready!

4. **Start chatting!**
   - Type your question
   - Press Enter to send
   - Get AI-powered help instantly

## 💡 Your First Conversation

Try these example prompts:

```
Create a player controller with WASD movement and jumping

Explain what this script does [select a script in your project]

Write a health system with damage and healing

Help me fix this error: NullReferenceException in PlayerController.cs:42
```

## 📁 Files Created (Auto-Managed)

GameSmith creates only 2 files in your project:

```
YourProject/
├── ProjectSettings/
│   └── GameSmithSettings.json    # Your API keys & preferences (⚠️ Don't commit!)
└── Assets/Resources/GameSmith/
    └── ChatHistory.asset          # Your conversation history
```

**That's it!** No clutter, no complicated setup.

## 🔒 Privacy & Security

### API Keys Security
- ✓ Stored in `ProjectSettings/` (Unity auto-ignores in version control)
- ✓ Never included in builds
- ✓ Never uploaded to GitHub (if you use recommended .gitignore)

### Add to `.gitignore`:
```gitignore
# GameSmith - Don't commit API keys!
ProjectSettings/GameSmithSettings.json
Assets/Resources/GameSmith/ChatHistory.asset
Assets/Resources/GameSmith/ChatHistory.asset.meta
```

### Code Privacy
- **Ollama:** 100% local, your code never leaves your machine
- **Other providers:** Your code is sent to their API (check their privacy policies)
- **GameSmith itself:** Zero telemetry, 100% open source

## ⌨️ Keyboard Shortcuts

| Action | Windows/Linux | Mac |
|--------|--------------|-----|
| Open GameSmith | `Ctrl+Shift+G` | `Cmd+Shift+G` |
| Send message | `Enter` | `Enter` |
| New line | `Shift+Enter` | `Shift+Enter` |

## 🎯 Pro Tips

### 1. Use Ollama for Privacy
```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Pull a model
ollama pull llama2

# GameSmith will auto-detect running Ollama models!
```

### 2. Create Custom Rules
- Create a text file with Unity-specific guidelines
- In GameSmith settings, assign it to "Rules TextAsset"
- GameSmith will follow your custom rules!

### 3. Multiple Providers
- Configure multiple AI providers
- Switch between them instantly via the dropdown
- Compare responses from different models

### 4. Temperature Settings
- **0.0-0.3:** Focused, deterministic (good for code)
- **0.4-0.7:** Balanced (recommended)
- **0.8-2.0:** Creative, varied responses

## 🆘 Troubleshooting

### "API Key Error" message
**Solution:** Click the error message to open settings, then:
1. Verify your API key is correct
2. Check you have credits/quota remaining
3. Ensure correct provider is selected

### GameSmith window not appearing
**Solution:**
- Menu: `Tools → GameSmith → Open Window`
- Or use keyboard shortcut: `Ctrl+Shift+G`

### Welcome window disappeared
**Solution:**
- Menu: `Tools → GameSmith → Show Welcome Window`
- Or: `Tools → GameSmith → Reset First-Time Setup` (then restart Unity)

### Settings not saving
**Solution:**
- Check file permissions on `ProjectSettings/` folder
- Ensure Unity has write access
- Check Console for errors

### Ollama not detecting models
**Solution:**
1. Ensure Ollama is running: `ollama serve`
2. Check models are installed: `ollama list`
3. Pull a model if needed: `ollama pull llama2`
4. Restart GameSmith window

## 📚 Learn More

- **Documentation:** [github.com/muammar-yacoob/unity-gamesmith](https://github.com/muammar-yacoob/unity-gamesmith)
- **Report Issues:** [GitHub Issues](https://github.com/muammar-yacoob/unity-gamesmith/issues)
- **Unity MCP:** [unity-mcp package](https://www.npmjs.com/package/@spark-apps/unity-mcp)

## 🎉 You're Ready!

Open GameSmith (`Ctrl+Shift+G`) and start building amazing games with AI assistance!

---

Made with ❤️ by [Muammar Yacoob](https://github.com/muammar-yacoob)
Free & Open Source • MIT License
