# MCP Integration Guide

Unity GameSmith integrates with **Model Context Protocol (MCP)** servers to extend AI-powered development capabilities with external tools and services.

---

## 🎯 Overview

This project uses **two MCP servers**:

1. **Taskmaster MCP** - AI-powered task management and PRD parsing
2. **Chrome DevTools MCP** - Automated WebGL testing and performance profiling

---

## 🛠️ Taskmaster MCP

### Purpose
AI-powered task management system that parses Product Requirements Documents (PRDs) into structured, dependency-aware task lists.

### Features
- ✅ **PRD Parsing** - Converts PRD documents into actionable tasks
- ✅ **Dependency Tracking** - Understands task dependencies and execution order
- ✅ **Smart Planning** - AI analyzes requirements and suggests implementation phases
- ✅ **Progress Tracking** - Maintains task status and completion tracking
- ✅ **Complexity Analysis** - Estimates task complexity and effort

### Configuration

```json
{
  "taskmaster": {
    "type": "stdio",
    "command": "npx",
    "args": ["-y", "claude-task-master"],
    "cwd": "${workspaceFolder}",
    "description": "AI-powered task management for project planning, PRD parsing, and dependency-aware task tracking"
  }
}
```

### Directory Structure

```
.taskmaster/
├── docs/                    # Product Requirements Documents
│   └── prd_*.txt           # Individual PRD files
├── templates/               # PRD templates
│   └── example_prd.txt     # Template for creating new PRDs
└── tasks/                   # Generated task lists (gitignored)
    └── *.json              # Task tracking files
```

### Workflow Example

#### 1. Create a PRD
Use the template at `.taskmaster/templates/example_prd.txt` to create a new PRD:

```bash
cp .taskmaster/templates/example_prd.txt .taskmaster/docs/prd_my_feature.txt
# Edit prd_my_feature.txt with your requirements
```

#### 2. Let AI Parse the PRD
In Claude Code, ask:
```
"Parse the PRD at .taskmaster/docs/prd_my_feature.txt and create a task list"
```

Taskmaster will:
- Extract all requirements and acceptance criteria
- Break down implementation phases into subtasks
- Identify dependencies between tasks
- Generate a structured task list

#### 3. Track Progress
As you complete tasks:
```
"Mark task 'Implement core functionality' as completed"
```

Taskmaster maintains state and updates dependent tasks automatically.

### PRD Best Practices

**Use the 14-section template** for consistency:
1. Executive Summary
2. Problem Statement
3. Goals & Success Metrics
4. User Stories
5. Technical Requirements
6. Feature Specification
7. Implementation Phases
8. Dependencies & Integration
9. Testing Strategy
10. Documentation Requirements
11. Risks & Mitigations
12. Timeline & Milestones
13. Open Questions
14. References

**See Example**: `.taskmaster/docs/prd_animation_system.txt`

---

## 🌐 Chrome DevTools MCP

### Purpose
Automated browser testing for Unity WebGL builds with Puppeteer integration, performance profiling, and debugging capabilities.

### Features
- ✅ **Automated Testing** - Launch and test WebGL builds in Chrome
- ✅ **Performance Profiling** - CPU, memory, and GPU metrics
- ✅ **GPU Acceleration** - Uses Vulkan for hardware rendering
- ✅ **Network Monitoring** - Track asset loading times
- ✅ **Console Logging** - Capture Unity Debug.Log output
- ✅ **Screenshots** - Visual regression testing

### Configuration

```json
{
  "chrome-devtools": {
    "type": "stdio",
    "command": "npx",
    "args": [
      "-y",
      "@chromedevtools/chrome-devtools-mcp",
      "--",
      "--headless=new",
      "--use-angle=vulkan",
      "--enable-features=Vulkan",
      "--no-sandbox"
    ],
    "description": "Chrome DevTools for Unity WebGL testing, performance profiling, and automated browser testing"
  }
}
```

### WebGL Testing Workflow

#### 1. Build WebGL Project
In Unity:
```
File → Build Settings → WebGL → Build
```

#### 2. Serve the Build
```bash
cd Build
python -m http.server 8000
```

#### 3. Test with Chrome DevTools MCP
In Claude Code:
```
"Test the WebGL build at http://localhost:8000 and profile performance"
```

Chrome DevTools MCP will:
- Launch headless Chrome with GPU acceleration
- Load your WebGL build
- Profile FPS, memory usage, and load times
- Check for console errors
- Generate performance report

#### 4. Analyze Results
```
"Show me the console logs and any WebGL errors"
"What's the average FPS and memory usage?"
"Take a screenshot of the game running"
```

### GPU Flags Explained

**`--use-angle=vulkan`**: Use Vulkan for graphics rendering (better performance)
**`--enable-features=Vulkan`**: Enable Vulkan support in Chrome
**`--no-sandbox`**: Required for headless mode with GPU (WSL/Docker compatibility)

---

## 🚀 Quick Start

### 1. Install Claude Code
If not already installed:
```bash
npm install -g @anthropic-ai/claude-code
```

### 2. Verify MCP Configuration
The `.mcp.json` file should already be configured:
```bash
cat .mcp.json
```

### 3. Start Claude Code
```bash
claude-code
```

Claude Code will automatically load both MCP servers.

### 4. Test Taskmaster
```
"Show me the PRD template and create a new PRD for a pause menu system"
```

### 5. Test Chrome DevTools (after WebGL build)
```
"Start Chrome and test http://localhost:8000"
```

---

## 📋 Common Use Cases

### Planning a New Feature
```
1. Copy PRD template
2. Fill out requirements
3. Ask Claude: "Parse this PRD and create an implementation plan"
4. Track progress as you complete tasks
```

### Testing WebGL Build
```
1. Build WebGL in Unity
2. Serve locally
3. Ask Claude: "Test WebGL build and check for errors"
4. Review performance metrics
5. Fix issues and retest
```

### Debugging WebGL Issues
```
"Monitor console logs for errors in the WebGL build"
"Profile memory usage - are there any leaks?"
"Test loading time for all assets"
```

---

## 🔧 Troubleshooting

### Taskmaster Issues

**MCP server not loading**:
- Ensure Node.js is installed: `node --version`
- Test npx: `npx -y claude-task-master --version`

**PRDs not being parsed**:
- Check PRD file exists: `ls .taskmaster/docs/`
- Verify file format matches template

### Chrome DevTools Issues

**Chrome fails to start**:
- Check Chrome is installed: `google-chrome --version`
- On WSL: Install Chrome in Windows and use `chrome.exe`

**GPU acceleration disabled**:
- Verify Vulkan support: `vulkaninfo` (Linux)
- Try without GPU flags for testing (slower):
  ```json
  "args": ["-y", "@chromedevtools/chrome-devtools-mcp", "--", "--headless=new"]
  ```

**WebGL build not loading**:
- Verify server is running: `curl http://localhost:8000`
- Check browser console for CORS errors
- Ensure correct Unity Player Settings for WebGL

---

## 📚 Additional Resources

- [Taskmaster MCP GitHub](https://github.com/eyaltoledano/claude-task-master)
- [Chrome DevTools MCP GitHub](https://github.com/ChromeDevTools/chrome-devtools-mcp)
- [MCP Protocol Specification](https://modelcontextprotocol.io/)
- [Unity WebGL Best Practices](https://docs.unity3d.com/Manual/webgl-building.html)

---

**Next**: See [WebGL Testing Guide](webgl-testing.md) for detailed testing workflows.
