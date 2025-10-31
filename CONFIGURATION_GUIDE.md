# GameSmith Configuration Guide

Complete visual guide to setting up GameSmith with your AI provider.

## 🎯 Where to Configure

### Method 1: Welcome Window (First Time)

When you first import GameSmith, a welcome window appears automatically:

```
┌─────────────────────────────────────────┐
│   ⚒️ Welcome to GameSmith               │
│                                         │
│   [Status: Configuration Needed]        │
│                                         │
│   [⚙️ Configure Settings First]  ← CLICK THIS
│                                         │
│   [View Documentation]                  │
└─────────────────────────────────────────┘
```

**Click "Configure Settings First"** → Opens the Inspector with GameSmith settings

### Method 2: Unity Menu

```
Unity Menu Bar
    ↓
Tools
    ↓
GameSmith
    ↓
Configure Settings  ← Click this
```

This opens the GameSmith configuration in the Unity Inspector.

### Method 3: Error Message

If you see an error in the chat window:

```
┌─────────────────────────────────────┐
│  ❌ API Key Error                    │
│                                     │
│  Please configure your API key      │
│                                     │
│  💡 Click here to configure         │  ← Click anywhere on this error
└─────────────────────────────────────┘
```

**Click the error bubble** → Opens settings automatically

## 📝 The Inspector Configuration Panel

After clicking "Configure Settings", you'll see this in the Unity Inspector:

```
┌─────────────────────────────────────────────┐
│  GameSmith AI Configuration                 │
│                                             │
│  👋 Welcome! Select a provider below...     │
│                                             │
│  ═══ General Settings ═══                   │
│                                             │
│  Active Provider: [Claude           ▼]     │  ← 1. Select provider
│                                             │
│  Selected Model:  [Claude 4.5 Sonnet ▼]    │  ← 2. Choose model
│                                             │
│  ─── Model Parameters ───                   │
│  Temperature: [━━━━○━━━━] 0.7              │
│  Max Tokens:  [━━━━━○━━━] 4096             │
│                                             │
│  ─── Unity Rules ───                        │
│  Rules TextAsset: [None (TextAsset)]       │
│                                             │
│  ═══ API Configuration ═══                  │
│                                             │
│  API Key: [●●●●●●●●●●●●●●●●] ●             │  ← 3. Paste key here
│                                             │
│           Get API Key ↗                     │  ← 4. Click to get key
│                                             │
│  ✓ API connection verified successfully     │
└─────────────────────────────────────────────┘
```

## 🔑 Step-by-Step Setup

### Step 1: Select Your Provider

**In the Inspector → General Settings:**

Click the **"Active Provider"** dropdown:
- **Claude** (Anthropic) - Best for code generation
- **Gemini** (Google) - Fast and free
- **OpenAI** - GPT-4 access
- **OpenRouter** - Multiple models
- **Ollama** - 100% free, runs locally

**👉 Recommendation:** Start with **Claude** for best code generation results.

### Step 2: Get Your API Key

**In the Inspector → API Configuration:**

1. Look for the blue link: **"Get API Key ↗"**
2. Click it → Opens the provider's website in your browser
3. Sign up (free) and create an API key
4. Copy the API key to your clipboard

**Free API Keys:**
- **Claude:** [console.anthropic.com/account/keys](https://console.anthropic.com/account/keys) - $5 free credits
- **Gemini:** [aistudio.google.com/app/apikey](https://aistudio.google.com/app/apikey) - Generous free tier
- **OpenAI:** [platform.openai.com/api-keys](https://platform.openai.com/api-keys) - Pay per use
- **OpenRouter:** [openrouter.ai/keys](https://openrouter.ai/keys) - Free trial
- **Ollama:** No API key needed! Just install Ollama

### Step 3: Enter Your API Key

**In the Inspector → API Configuration:**

1. Find the **"API Key"** field (shows dots: ●●●●●●●●)
2. Click in the field
3. Paste your API key (`Ctrl+V` or `Cmd+V`)
4. **Press Enter** (important!)

**Status Indicators:**
- 🔴 **Red dot** = Not configured or error
- 🟡 **Yellow dot** = Verifying...
- 🟢 **Green dot** = Success! You're ready!

### Step 4: Start Using GameSmith

Once you see the **green dot ✓**:

1. Open GameSmith: `Tools → GameSmith → Open Window`
2. Or use keyboard shortcut: `Ctrl+Shift+G` (Windows/Linux) or `Cmd+Shift+G` (Mac)
3. Type your question and press Enter
4. Get AI-powered responses instantly!

## 🎨 Visual Reference

### Configuration Location in Unity

```
Unity Editor
    ├── Menu Bar
    │   └── Tools → GameSmith → Configure Settings
    │
    ├── Project Window
    │   └── Assets/Resources/GameSmith/EditorConfig.asset (optional)
    │
    └── Inspector Window  ← Configuration appears here!
        └── [GameSmith AI Configuration panel]
```

### Inspector Panel Sections

```
┌─ GameSmith AI Configuration ────────────┐
│                                         │
│  ┌─ General Settings ────────────────┐ │  What it does:
│  │ Active Provider   [Dropdown]      │ │  - Choose AI service
│  │ Selected Model    [Dropdown]      │ │  - Pick model version
│  │ Temperature       [Slider]        │ │  - Adjust creativity
│  │ Max Tokens        [Slider]        │ │  - Response length
│  │ Unity Rules       [TextAsset]     │ │  - Custom guidelines
│  └───────────────────────────────────┘ │
│                                         │
│  ┌─ API Configuration ───────────────┐ │  What it does:
│  │ API Key          [Password field] │ │  - Your secret key
│  │ Status indicator [● Green/Red]    │ │  - Connection status
│  │ Get API Key ↗    [Clickable link] │ │  - Get free key
│  │ Help messages    [If errors]      │ │  - Troubleshooting
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

## 🔄 Changing Providers Later

Want to try a different AI provider? Easy!

1. Open: `Tools → GameSmith → Configure Settings`
2. Change **"Active Provider"** dropdown
3. Enter new API key for that provider
4. Press Enter to verify
5. Done! Start chatting with the new provider

**You can configure multiple providers** and switch between them instantly.

## 💡 Pro Tips

### Save Time with Keyboard Shortcut
- Configure Settings: `Tools → GameSmith → Configure Settings`
- Open Window: `Ctrl+Shift+G` (or `Cmd+Shift+G` on Mac)

### Multiple API Keys
The system stores API keys for all providers. Configure them once:
- **Claude** for complex code generation
- **Gemini** for quick questions
- **Ollama** for privacy-sensitive work

Then switch between them via the dropdown!

### Lost Your Settings?
Settings are stored in:
- `ProjectSettings/GameSmithSettings.json`

If deleted, just re-enter your API key - takes 30 seconds.

### Share Project (Without API Keys)
Your API keys are stored separately and **not** included when you:
- Commit to Git
- Share the project
- Build the game

Each person uses their own API key.

## ❓ Troubleshooting

### "I clicked Configure Settings but nothing happened"
**Solution:** Look at the **Inspector** tab (usually on the right side of Unity). The configuration panel appears there, not in a popup window.

### "I entered my API key but the dot is still red"
**Solution:** Make sure to **press Enter** after pasting the key. This triggers verification.

### "Where is the Inspector tab?"
**Solution:**
- Menu: `Window → General → Inspector`
- Or press `Ctrl+3` (Windows/Linux) or `Cmd+3` (Mac)

### "The Inspector shows something else"
**Solution:** Click the "Configure Settings" button again, or find the `EditorConfig` asset in `Assets/Resources/GameSmith/` and click it.

## 📍 Quick Reference Card

| Action | Location | How |
|--------|----------|-----|
| First setup | Welcome window | Appears automatically |
| Open settings | Menu | `Tools → GameSmith → Configure Settings` |
| View settings | Inspector | After clicking "Configure Settings" |
| Enter API key | Inspector | Paste key → Press Enter |
| Check status | Inspector | Look for colored dot (🔴/🟡/🟢) |
| Get API key | Inspector | Click "Get API Key ↗" link |
| Open GameSmith | Menu/Keyboard | `Tools → GameSmith` or `Ctrl+Shift+G` |

---

**Still stuck?** Check [GETTING_STARTED.md](GETTING_STARTED.md) or [report an issue](https://github.com/muammar-yacoob/unity-gamesmith/issues)
