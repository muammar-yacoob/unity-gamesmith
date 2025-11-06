# Unity GameSmith MCP Test Instructions

## Prerequisites
- Unity Editor 2021.3+ installed
- Node.js and npm installed
- Ollama running locally on port 11434
- Unity project located at: `/mnt/d/Unity/MyAssets/GameSmith/MCP Test/`

## Setup Completed
1. ✅ Fixed Unity package path references in GameSmithWindow.cs
2. ✅ Configured Ollama with available models (qwen3:8b and deepseek-coder-v2:16b)
3. ✅ Created TestCubeSetup.cs script in Assets/Scripts
4. ✅ Installed @spark-apps/unity-mcp package globally (v1.0.4)

## Testing Steps

### Step 1: Open Unity Project
1. Open Unity Hub
2. Open the project at: `D:\Unity\MyAssets\GameSmith\MCP Test`
3. Wait for Unity to compile and load

### Step 2: Set Up Test Scene
1. Open `Assets/Scenes/SampleScene.unity`
2. Create an empty GameObject and attach the `TestCubeSetup.cs` script
3. Play the scene to create the test cube
4. Stop playing - the cube should be created in the scene

### Step 3: Open GameSmith Window
1. In Unity menu, go to: **Tools > GameSmith > GameSmith AI** (or press Alt+G)
2. The GameSmith window should open

### Step 4: Configure AI Provider
1. If you see "⚠ Not configured", click on it or click the Settings button
2. In the Settings window:
   - Select **Ollama** as the provider
   - The API URL should be: `http://localhost:11434/v1/chat/completions`
   - No API key is needed for Ollama
   - Select either "Qwen3 8B" or "DeepSeek Coder V2 16B" as the model
   - Click Save

### Step 5: Test Basic Chat
1. In the GameSmith window, type: "Hello, can you see the Unity project context?"
2. Press Enter or click Send
3. The AI should respond and show Unity project information

### Step 6: Test Unity-MCP Integration
1. Make sure the TestCube is in your scene (or play the scene once to create it)
2. Select the TestCube GameObject in the Hierarchy
3. In the GameSmith window, type: "Change the scale of the selected TestCube to (1, 3, 1)"
4. Press Enter
5. The AI should:
   - Recognize the Unity-MCP tools are available
   - Use the MCP tools to change the cube's scale
   - The cube should now be taller (scale: 1, 3, 1)

## Expected Results
- The GameSmith window should load without errors
- The UXML and USS files should be found at: `Assets/GameSmith/UnityPackage/Editor/`
- Ollama should connect successfully on port 11434
- The Unity-MCP server should start (check console for "[GameSmith] MCP server connected")
- The cube's scale should change from (1, 1, 1) to (1, 3, 1)

## Troubleshooting

### If GameSmith window shows "UXML not found"
- Check that files exist at: `Assets/GameSmith/UnityPackage/Editor/GameSmithWindow.uxml`
- Reimport the Unity package if needed

### If MCP server fails to start
- Check Node.js is installed: `node --version`
- Check unity-mcp is installed: `npm list -g @spark-apps/unity-mcp`
- Check console logs for specific error messages

### If Ollama doesn't connect
- Verify Ollama is running: `curl http://localhost:11434/api/tags`
- Check if models are available: `ollama list`
- Try restarting Ollama service

### If cube scale doesn't change
- Ensure the TestCube GameObject exists in the scene
- Select the cube before sending the command
- Check console for MCP tool execution logs

## Console Logs to Watch For
- `[GameSmith] Settings loaded`
- `[GameSmith] MCP server connected with X tools available`
- `[Using tool: XYZ]` when MCP tools are executed
- `TestCube created at origin` when the test cube is created

## Additional Tests
1. Try other commands:
   - "Change the TestCube's position to (2, 0, 0)"
   - "Rotate the TestCube by 45 degrees on Y axis"
   - "Change the TestCube's color to red"
2. Test with different Ollama models
3. Try creating new GameObjects through chat