# Unity WebGL Testing Guide

Complete guide for testing Unity GameSmith WebGL builds using Chrome DevTools MCP for automated testing, performance profiling, and debugging.

---

## 📋 Table of Contents

1. [Prerequisites](#prerequisites)
2. [Building WebGL Projects](#building-webgl-projects)
3. [Local Testing Setup](#local-testing-setup)
4. [Automated Testing with MCP](#automated-testing-with-mcp)
5. [Performance Profiling](#performance-profiling)
6. [Debugging WebGL Issues](#debugging-webgl-issues)
7. [Best Practices](#best-practices)
8. [Common Issues](#common-issues)

---

## Prerequisites

### Required Software
- ✅ Unity 2021.3 LTS or later
- ✅ Node.js 16+ (for local server)
- ✅ Google Chrome (latest version)
- ✅ Claude Code with Chrome DevTools MCP configured

### Unity WebGL Module
Install via Unity Hub:
```
Unity Hub → Installs → [Your Unity Version] → Add Modules → WebGL Build Support
```

---

## Building WebGL Projects

### 1. Configure Build Settings

#### Player Settings (Edit → Project Settings → Player)
```
WebGL Settings:
├── Resolution and Presentation
│   ├── Default Canvas Width: 1280
│   ├── Default Canvas Height: 720
│   └── Run In Background: ✓
├── Publishing Settings
│   ├── Compression Format: Brotli (best compression)
│   ├── Enable Exceptions: Explicitly Thrown (for debugging)
│   └── Data Caching: ✓
└── Other Settings
    ├── Color Space: Linear
    ├── Auto Graphics API: ✓
    └── Managed Stripping Level: Medium
```

### 2. Optimize for WebGL

#### Code Stripping
```csharp
// Tag methods to prevent stripping
[UnityEngine.Scripting.Preserve]
public void ImportantMethod() { }
```

#### Asset Optimization
- **Textures**: Use ASTC/ETC2 compression
- **Audio**: Use MP3/Vorbis compression
- **Meshes**: Keep poly count under 10k per object
- **Materials**: Use Standard shader or URP/Lit

### 3. Build the Project

#### Via Unity Editor
```
File → Build Settings
├── Platform: WebGL
├── Compression: Brotli
└── Development Build: ✓ (for testing)
Click "Build" and select output folder
```

#### Via Command Line
```bash
# Windows
"C:\Program Files\Unity\Hub\Editor\2021.3.x\Editor\Unity.exe" -quit -batchmode -projectPath "." -executeMethod BuildScript.BuildWebGL

# macOS/Linux
/Applications/Unity/Hub/Editor/2021.3.x/Unity.app/Contents/MacOS/Unity -quit -batchmode -projectPath "." -executeMethod BuildScript.BuildWebGL
```

**Build Script Example** (`Assets/Editor/BuildScript.cs`):
```csharp
using UnityEditor;

public class BuildScript
{
    public static void BuildWebGL()
    {
        string[] scenes = { "Assets/Scenes/MainScene.unity" };
        BuildPipeline.BuildPlayer(scenes, "Build/WebGL", BuildTarget.WebGL, BuildOptions.None);
    }
}
```

---

## Local Testing Setup

### Option 1: Python Simple Server (Recommended)

#### Python 3
```bash
cd Build/WebGL
python -m http.server 8000
```

#### Python 2
```bash
cd Build/WebGL
python -m SimpleHTTPServer 8000
```

Access at: `http://localhost:8000`

### Option 2: Node.js http-server

```bash
npm install -g http-server
cd Build/WebGL
http-server -p 8000
```

### Option 3: Unity Editor
```
File → Build Settings → Build And Run
```
Opens in default browser automatically.

---

## Automated Testing with MCP

### Basic Testing Commands

#### Launch and Test
```
"Launch Chrome and test the WebGL build at http://localhost:8000"
```

#### Check for Errors
```
"Monitor console logs and check for WebGL errors"
```

#### Verify Game Functionality
```
"Test player movement and shooting system in the WebGL build"
```

### Example Test Session

```
Developer: "Test the WebGL build at http://localhost:8000"

Claude (via Chrome DevTools MCP):
1. Launching headless Chrome with GPU acceleration...
2. Loading http://localhost:8000...
3. Waiting for Unity WebGL to initialize...
4. Monitoring console logs...

Results:
✅ Build loaded successfully (3.2s)
✅ No console errors
✅ Average FPS: 58
⚠️ Warning: UnityLoader took 2.1s to initialize
📊 Memory usage: 145 MB
```

---

## Performance Profiling

### CPU Profiling

#### Start Profiling
```
"Profile CPU usage for 10 seconds while the game runs"
```

**Metrics to Monitor**:
- Frame render time (target: <16.67ms for 60 FPS)
- Script execution time
- Garbage collection frequency
- Physics simulation cost

### Memory Profiling

#### Check Memory Usage
```
"Monitor memory usage and check for memory leaks"
```

**Key Metrics**:
- Initial memory allocation
- Memory growth over time
- Garbage collection pressure
- Total heap size

#### Detect Memory Leaks
```
"Run the game for 30 seconds and check if memory keeps growing"
```

### GPU Profiling

#### Check Rendering Performance
```
"Profile rendering performance and check draw calls"
```

**Metrics**:
- Draw calls per frame (target: <500)
- Triangles rendered (target: <100k)
- SetPass calls (target: <100)
- Shader compilation time

### Network Profiling

#### Monitor Asset Loading
```
"Profile network activity and asset loading times"
```

**Check**:
- Initial load time
- Asset bundle sizes
- Compression effectiveness
- Slow loading assets

---

## Debugging WebGL Issues

### Common Errors

#### 1. Infinite Load / White Screen

**Symptoms**: Build loads forever, no Unity logo appears

**Debug Commands**:
```
"Check console logs for WebGL initialization errors"
"Inspect network tab for failed asset loads"
```

**Common Causes**:
- Missing or corrupted build files
- CORS issues with local server
- Browser blocking WebAssembly
- Insufficient memory

**Solutions**:
```csharp
// Add to index.html for better error reporting
<script>
window.addEventListener('error', function(e) {
    console.error('Unity WebGL Error:', e);
});
</script>
```

#### 2. Low FPS / Performance Issues

**Debug Commands**:
```
"Profile the game and show me what's causing slow FPS"
"Take CPU timeline snapshot during gameplay"
```

**Optimization Checklist**:
- [ ] Reduce draw calls (use batching)
- [ ] Optimize scripts (avoid Update() loops)
- [ ] Compress textures
- [ ] Use object pooling
- [ ] Limit physics calculations

#### 3. Memory Errors / Crashes

**Debug Commands**:
```
"Monitor heap memory and find what's allocating"
"Check for memory leaks in the game"
```

**Fix Strategies**:
```csharp
// Manual garbage collection (use sparingly)
System.GC.Collect();

// Object pooling for frequently instantiated objects
public class ObjectPool<T> where T : Component
{
    private Queue<T> pool = new Queue<T>();

    public T Get()
    {
        return pool.Count > 0 ? pool.Dequeue() : null;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

#### 4. Input Not Working

**Debug Commands**:
```
"Test keyboard and mouse input in the WebGL build"
"Check if Unity is receiving input events"
```

**Solutions**:
- Ensure canvas has focus (click on it)
- Check Input System package version
- Verify browser permissions

---

## Best Practices

### 1. Development Builds

**Always test with Development Build first**:
```
Build Settings → Development Build: ✓
Build Settings → Autoconnect Profiler: ✓
```

Benefits:
- Better error messages
- Console logging works
- Unity Profiler connection
- Debug symbols included

### 2. Compression

**Test both compressed and uncompressed**:
```
Brotli: Best compression, slower builds
Gzip: Good compression, faster builds
Disabled: Fastest builds, largest files
```

### 3. Test Matrix

Test on multiple browsers:
- ✅ Chrome (primary)
- ✅ Firefox
- ✅ Edge
- ✅ Safari (macOS/iOS only)

### 4. Performance Targets

**Mobile**: 30 FPS minimum
**Desktop**: 60 FPS target
**Memory**: <500 MB peak usage
**Load Time**: <10 seconds on 4G

### 5. Automated Testing Script

**Example Test Suite**:
```
"Run the following tests on the WebGL build:
1. Check initial load time (target: <5s)
2. Monitor FPS for 30 seconds (target: >50 FPS)
3. Check for console errors
4. Verify memory doesn't exceed 300 MB
5. Test player input responsiveness
6. Take screenshots every 10 seconds
7. Generate performance report"
```

---

## Common Issues

### Issue: Build Size Too Large

**Symptoms**: Build folder >100 MB

**Solutions**:
```
1. Enable code stripping: Medium or High
2. Compress textures: ASTC/ETC2
3. Use Asset Bundles for large assets
4. Enable Brotli compression
5. Remove unused assets
```

### Issue: Slow Loading

**Symptoms**: Long wait on "Loading..." screen

**Debug**:
```
"Profile asset loading and show bottlenecks"
```

**Optimization**:
- Reduce initial scene complexity
- Use loading screens with progress bars
- Lazy load assets
- Enable caching

**Example Loading Bar**:
```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WebGLLoader : MonoBehaviour
{
    public Slider progressBar;

    void Start()
    {
        StartCoroutine(LoadSceneAsync());
    }

    IEnumerator LoadSceneAsync()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync("MainScene");

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            progressBar.value = progress;
            yield return null;
        }
    }
}
```

### Issue: CORS Errors

**Symptoms**: Assets fail to load with CORS errors

**Solutions**:
```bash
# Python server with CORS
python -m http.server 8000 --bind 0.0.0.0

# Node.js http-server with CORS
http-server -p 8000 --cors
```

### Issue: WebAssembly Not Supported

**Symptoms**: "WebAssembly is not supported" error

**Debug**:
```
"Check browser compatibility for WebAssembly"
```

**Solutions**:
- Update browser to latest version
- Try different browser
- Check browser flags/settings
- Verify https:// (some browsers require secure context)

---

## Advanced Testing

### Continuous Integration

**GitHub Actions Example** (`.github/workflows/webgl-test.yml`):
```yaml
name: WebGL Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2

      - name: Build WebGL
        uses: game-ci/unity-builder@v2
        with:
          targetPlatform: WebGL

      - name: Test with Chrome DevTools MCP
        run: |
          npx -y http-server Build/WebGL -p 8000 &
          npx -y @chromedevtools/chrome-devtools-mcp test
```

### Load Testing

**Test Multiple Players**:
```
"Simulate 10 concurrent players and measure server load"
```

### Visual Regression Testing

**Screenshot Comparison**:
```
"Take screenshot of the main menu and compare with baseline"
```

---

## Resources

- [Unity WebGL Documentation](https://docs.unity3d.com/Manual/webgl.html)
- [Chrome DevTools MCP GitHub](https://github.com/ChromeDevTools/chrome-devtools-mcp)
- [WebGL Optimization Guide](https://docs.unity3d.com/Manual/webgl-performance.html)
- [Browser Compatibility](https://caniuse.com/wasm)

---

**Next**: Return to [MCP Integration Guide](mcp-integration.md) for complete workflow.
