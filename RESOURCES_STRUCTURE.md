# GameSmith Resources Structure

When you first use GameSmith in your Unity project, it will automatically create a minimal, organized structure:

```
ProjectSettings/
└── GameSmithSettings.json      # API keys & preferences (DO NOT commit!)

Assets/
└── Resources/
    └── GameSmith/
        └── ChatHistory.asset   # Conversation history
```

## File Descriptions

### ProjectSettings/GameSmithSettings.json ⚠️ IMPORTANT
Contains sensitive user settings (stored as JSON, not in version control):
- **API keys for all providers** (Claude, Gemini, OpenAI, OpenRouter, Ollama)
- Active AI provider
- Selected model
- Temperature and max tokens
- Unity rules TextAsset path

**⚠️ NEVER commit this file to version control - it contains API keys!**

### Assets/Resources/GameSmith/ChatHistory.asset
Stores your conversation history with the AI assistant:
- All user messages
- All assistant responses
- Timestamps for each message

## Notes

- **Simple & Clean**: Only 2 files created in your project
- **JSON-Based**: All settings stored in simple JSON format
- **Secure**: API keys stored separately in ProjectSettings (auto-ignored by Unity)
- **No ScriptableObject Clutter**: EditorConfig is optional, settings are in JSON
- **Auto-Creation**: Files are created automatically on first use

## .gitignore Recommendation

**IMPORTANT:** Add this to your project's `.gitignore`:

```gitignore
# GameSmith - Exclude API keys and chat history
ProjectSettings/GameSmithSettings.json       # Contains API keys!
Assets/Resources/GameSmith/ChatHistory.asset
Assets/Resources/GameSmith/ChatHistory.asset.meta
```

The `ProjectSettings/GameSmithSettings.json` file is the most important to exclude - it contains your API keys!
