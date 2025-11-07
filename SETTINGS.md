# GameSmith Settings

GameSmith uses a **simple JSON file** to store all your settings and API keys.

## Settings File Location

```
YourProject/
└── ProjectSettings/
    └── GameSmithSettings.json
```

## Settings Structure

```json
{
  "activeProvider": "Claude",
  "selectedModel": "claude-sonnet-4-20250514",
  "temperature": 0.7,
  "maxTokens": 4096,
  "rulesAssetPath": "",
  "apiKeys": [
    {
      "provider": "Claude",
      "apiKey": "sk-ant-..."
    },
    {
      "provider": "OpenAI",
      "apiKey": "sk-..."
    }
  ]
}
```

## Why JSON?

**Simple & Clean:**
- Easy to read and edit
- No complicated ScriptableObject serialization
- Works perfectly with .gitignore
- Can be backed up or shared easily (without API keys)

**Secure:**
- Stored in `ProjectSettings/` (Unity auto-ignores this folder)
- API keys separate from project assets
- Easy to exclude from version control

**Flexible:**
- Add/remove providers easily
- Change settings without Unity's asset pipeline
- No asset database dependencies

## Security Best Practices

### ⚠️ NEVER Commit This File

Add to your `.gitignore`:

```gitignore
ProjectSettings/GameSmithSettings.json
```

### Sharing Projects

When sharing your project:
1. **Exclude** `GameSmithSettings.json` from version control
2. Each team member sets up their own API keys
3. Share provider/model configurations via `providers.json` (safe to commit)

### Backup Without API Keys

To backup settings without exposing API keys:

```json
{
  "activeProvider": "Claude",
  "selectedModel": "claude-sonnet-4-20250514",
  "temperature": 0.7,
  "maxTokens": 4096,
  "rulesAssetPath": "Assets/Rules/UnityRules.txt",
  "apiKeys": []
}
```

## Manual Configuration

You can manually edit the JSON file while Unity is closed:

1. Close Unity
2. Open `ProjectSettings/GameSmithSettings.json` in a text editor
3. Edit settings as needed
4. Save and reopen Unity

## Available Providers

- **Claude** (Anthropic)
- **Gemini** (Google)
- **OpenAI**
- **OpenRouter**
- **Ollama** (Local, no API key needed)

## Provider Models

Models are defined in `UnityPackage/Editor/providers.json`:
- Claude: Sonnet 4.5, Opus 4.1
- Gemini: 2.5 Flash, 2.5 Pro
- OpenAI: GPT-4 Turbo, GPT-3.5
- OpenRouter: Multiple providers
- Ollama: Dynamic (fetched from local server)

## Troubleshooting

**Settings not saving?**
- Check file permissions on `ProjectSettings/` folder
- Ensure Unity has write access
- Check Unity console for errors

**Lost API keys?**
- Restore from backup (if you have one)
- Re-enter in GameSmith configuration window
- Settings are auto-saved when changed

**Settings reset after Git pull?**
- Ensure `GameSmithSettings.json` is in `.gitignore`
- Check it's not being tracked by Git
- Use `git rm --cached ProjectSettings/GameSmithSettings.json`
