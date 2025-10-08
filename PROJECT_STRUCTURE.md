# Unity GameSmith - Project Structure

## 📁 Repository Structure

```
unity-gamesmith/
├── .git/                    # Git repository
├── .github/                 # GitHub workflows and actions
│   └── workflows/          # CI/CD workflows
├── .gitignore              # Git ignore rules
├── .releaserc.json         # Semantic release configuration
├── CHANGELOG.md            # Version history
├── LICENSE                 # MIT License
├── README.md               # Main documentation (installation & usage)
├── PROJECT_STRUCTURE.md    # This file
└── UnityPackage/           # Unity Package (the actual deliverable)
    ├── package.json        # Unity Package manifest
    ├── README.md           # Package-specific documentation
    ├── INSTALLATION.md     # Detailed setup guide
    ├── ENHANCED_FEATURES.md # Enhanced window features
    ├── Editor/             # Unity Editor scripts (C#)
    │   ├── EnhancedAIAgentWindow.cs     # Enhanced window with templates
    │   ├── UnityAIAgentWindow.cs        # Classic window
    │   ├── AITemplateLibrary.cs         # Template system
    │   ├── AIAgentConfig.cs             # Configuration management
    │   ├── AIAgentClient.cs             # AI API client
    │   ├── AIAgentLogger.cs             # Logging utilities
    │   ├── ScriptGeneratorUtility.cs    # Script generation
    │   ├── PlayerSystemGenerator.cs     # Player system generator
    │   ├── EnemySystemGenerator.cs      # Enemy system generator
    │   ├── ProjectileSystemGenerator.cs # Projectile generator
    │   ├── LevelSystemGenerator.cs      # Level system generator
    │   └── UISystemGenerator.cs         # UI system generator
    ├── Resources/          # Unity resources (empty for now)
    ├── Runtime/            # Runtime scripts (empty for now)
    ├── Templates/          # Template files (empty for now)
    └── Documentation/      # Extended documentation
        ├── MIGRATION_NOTES.md           # Architecture migration info
        └── INTEGRATION_COMPLETE.md      # Integration summary
```

## 📝 File Descriptions

### Root Level

| File | Purpose | Keep? |
|------|---------|-------|
| `.git/` | Git repository | ✅ Yes |
| `.github/` | GitHub workflows | ✅ Yes |
| `.gitignore` | Git ignore rules | ✅ Yes |
| `.releaserc.json` | Release automation | ✅ Yes |
| `CHANGELOG.md` | Version history | ✅ Yes |
| `LICENSE` | MIT License | ✅ Yes |
| `README.md` | Main documentation | ✅ Yes |
| `PROJECT_STRUCTURE.md` | This file | ✅ Yes |

### UnityPackage/ (The Unity Package)

| File/Folder | Purpose | Type |
|-------------|---------|------|
| `package.json` | Unity package manifest | Required |
| `README.md` | Package documentation | Docs |
| `INSTALLATION.md` | Setup instructions | Docs |
| `ENHANCED_FEATURES.md` | Feature guide | Docs |
| `Editor/` | C# Editor scripts | Code |
| `Resources/` | Unity resources | Assets |
| `Runtime/` | Runtime scripts | Code |
| `Templates/` | Code templates | Assets |
| `Documentation/` | Extended docs | Docs |

## 🗑️ Removed Files (No Longer Needed)

These files were removed during cleanup as they were related to the old MCP server:

**Development Configs:**
- `package.json` (root) - Node.js MCP server package
- `tsconfig.json` - TypeScript configuration
- `biome.json` - Biome linter config
- `jest.config.js` - Jest test config
- `bun.lock` - Bun lock file
- `dependencies.json` - Custom dependencies

**Old Code:**
- `packages/` - Old MCP server TypeScript code
- `apps/` - Old CLI tools
- `node_modules/` - Node dependencies

**Development Tools:**
- `.claude/` - Claude Code configs
- `.cursor/` - Cursor IDE configs
- `.taskmaster/` - Task Master AI files
- `.mcp.json` - MCP connection config
- `.dockerignore` - Docker config
- `Dockerfile` - Docker container

**Old Documentation:**
- `CLAUDE.md` - Development instructions
- `SETUP.md` - Old setup guide
- `SUMMARY.md` - Old summary
- `PROGRESS.md` - Development progress
- `PUBLISHING_INSTRUCTIONS.md` - Old publishing guide
- `sample_release.yml` - Sample workflow

**Moved to UnityPackage/Documentation/:**
- `MIGRATION_NOTES.md` → Architecture changes
- `INTEGRATION_COMPLETE.md` → Integration summary

## 🎯 Clean Project Benefits

### For Users
- ✅ Clear installation path
- ✅ Only Unity package visible
- ✅ No confusion about what to use
- ✅ Smaller repository size
- ✅ Faster clone times

### For Developers
- ✅ Focused structure
- ✅ Easy to navigate
- ✅ Clear separation of concerns
- ✅ Standard Unity package layout
- ✅ GitHub workflows preserved

## 📦 Unity Package Standards

The `UnityPackage/` folder follows Unity's package layout conventions:

```
UnityPackage/
├── package.json          # Required: Package manifest
├── README.md             # Recommended: User documentation
├── CHANGELOG.md          # Recommended: Version history
├── LICENSE.md            # Recommended: License info
├── Editor/               # Editor-only scripts
├── Runtime/              # Runtime scripts
├── Tests/                # Unit tests (future)
├── Documentation~/       # Docs (hidden from Unity)
└── Samples~/            # Sample scenes (future)
```

## 🔄 Version Control

### Tracked by Git
- All source code
- Documentation
- Package manifest
- License files
- GitHub workflows

### Ignored by Git
- Unity meta files (generated)
- Build artifacts
- IDE settings
- Node modules (obsolete)
- Log files
- Temporary files

## 🚀 Installation Methods

### 1. Package Manager (Recommended)
Users add `UnityPackage/package.json` directly

### 2. Manual Copy
Users copy entire `UnityPackage/` to their `Packages/` folder

### 3. Git Submodule
Users add repository as submodule

### 4. Unity Asset Store (Future)
Package the `UnityPackage/` for store submission

## 📊 Project Statistics

**Repository:**
- Root files: 8
- Directories: 2 (+ UnityPackage)
- Total size: < 1 MB (without .git)

**Unity Package:**
- C# Scripts: 12 files
- Documentation: 5 markdown files
- Templates: 10 built-in
- Lines of Code: ~4,000+

## 🎓 For Contributors

### Adding Features
1. All C# code goes in `UnityPackage/Editor/`
2. Update `UnityPackage/README.md` with new features
3. Add templates to `AITemplateLibrary.cs`
4. Update `CHANGELOG.md` with changes

### Documentation
1. User docs in `UnityPackage/README.md`
2. Technical docs in `UnityPackage/Documentation/`
3. Root `README.md` is for GitHub visitors

### Testing
1. Open Unity project
2. Add package from disk
3. Test both windows
4. Verify all features work
5. Check console for errors

## 🔗 Related Files

- **Main README**: Installation and features
- **Package README**: Usage and API
- **CHANGELOG**: Version history
- **LICENSE**: MIT terms
- **ENHANCED_FEATURES**: Feature guide

## 📝 Notes

- This is now a **Unity-first** project
- Old MCP server code is in git history
- Focus is on Unity Editor integration
- GitHub workflows still active for releases
- Semantic versioning with automatic releases

---

**Last Updated**: October 2025
**Status**: ✅ Cleaned and optimized
**Purpose**: Unity Editor extension
