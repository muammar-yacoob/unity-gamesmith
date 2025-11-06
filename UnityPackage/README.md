# Unity GameSmith

**Simple AI Chat Assistant for Unity Development**

Chat with AI directly in Unity Editor - ask questions, get code help, debug issues.

---

## 🚀 Quick Start

### 1. Add UniTask Dependency

Open your project's `Packages/manifest.json` and add:

```json
{
  "dependencies": {
    "com.cysharp.unitask": "2.3.3",
    ...your other dependencies
  },
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp.unitask",
        "com.openupm"
      ]
    }
  ]
}
```

### 2. Install GameSmith Package

In Unity Package Manager: `Window → Package Manager → + → Add package from git URL`

```
https://github.com/muammar-yacoob/unity-gamesmith.git?path=/UnityPackage
```

### 3. Open Chat Window

`Tools → GameSmith → Open Window` (or press **Alt+G**)

### 4. Configure AI Provider

Click **⚙️ Settings** button in the chat window, then:

1. **Select a Provider:**
   - **Claude** (Recommended) - Best for code
   - **OpenAI** - GPT-4o
   - **Gemini** - Google's model
   - **Ollama** - Free local AI (requires [Ollama](https://ollama.com) installed)

2. **Enter API Key** (not needed for Ollama):
   - Claude: Get key from [console.anthropic.com/settings/keys](https://console.anthropic.com/settings/keys)
   - OpenAI: Get key from [platform.openai.com/api-keys](https://platform.openai.com/api-keys)
   - Gemini: Get key from [aistudio.google.com/app/apikey](https://aistudio.google.com/app/apikey)

3. **Click "Open Chat Window"** and start chatting!

---

## 💬 Example Questions

```
"How do I make a 2D player move with WASD?"
"What's wrong with this script?" [paste code]
"Create a health system script"
"Explain Unity's Start() vs Awake()"
"How do I detect collisions in 2D?"
```

---

## 🎯 Features

- **💬 Simple Chat Interface** - Just ask questions
- **🔌 Multiple AI Providers** - Claude, OpenAI, Gemini, Ollama
- **🔒 Privacy First** - API keys stored locally, works offline with Ollama
- **📝 Chat History** - Conversations saved per project
- **⚡ Fast & Lightweight** - No bloat, just chat

---

## 🆓 Using Ollama (Free Local AI)

1. Install Ollama from [ollama.com](https://ollama.com)
2. Run in terminal: `ollama pull llama3.2`
3. Start server: `ollama serve`
4. In GameSmith settings, select "Ollama" provider
5. Chat for free - no API key needed!

---

## 📋 Requirements

- Unity 2021.3 LTS or later
- .NET Standard 2.1 or 4.x
- **UniTask** (required dependency - install via UPM)
- AI Provider: Ollama (free) OR API key (Claude/OpenAI/Gemini)

---

## 🔧 Troubleshooting

**Compilation errors about UniTask or Cysharp?**
- Make sure you added the UniTask dependency and scoped registry to `Packages/manifest.json` (see step 1 above)
- Wait for Unity to finish importing dependencies (check progress in bottom-right corner)
- Restart Unity if errors persist
- Verify `Packages/manifest.json` contains both the dependency AND scopedRegistries sections

**Package not showing?**
- Check `Packages/manifest.json` contains the package
- Try `Assets → Reimport All`
- Restart Unity

**AI not responding?**
- Check your API key is entered correctly
- For Ollama: Make sure `ollama serve` is running
- Check Unity Console for error messages

**Settings button not working?**
- Use menu: `Tools → GameSmith → Configure Settings`

---

## 🔗 Links

- [GitHub Repository](https://github.com/muammar-yacoob/unity-gamesmith)
- [Report Issues](https://github.com/muammar-yacoob/unity-gamesmith/issues)
- [Ollama](https://ollama.com) - Free local AI

---

## 📄 License

MIT License

---

**Made with ❤️ by [Muammar Yacoob](https://github.com/muammar-yacoob)**
