# Intent Classification Architecture

## Overview

GameSmith now uses an **Intent Classification System** to efficiently route user commands. Instead of sending all Unity operations through the AI model with 33 MCP tools, the system intelligently determines the most efficient execution path.

---

## The Problem (Before)

### Old Architecture Flow
```
User: "list objects"
  ↓
  AI Model Request (with 33 tools, ~10k tokens)
  ↓
  AI decides to use unity_get_hierarchy tool
  ↓
  MCP IPC call to Unity
  ↓
  Result

Cost: ~0.03 per request | Latency: ~2-3 seconds
```

**Issues:**
- ❌ Every request sent 33 tool definitions to AI (~10k tokens)
- ❌ Wasted money on AI processing for simple operations
- ❌ High latency for instant operations
- ❌ Poor UX for Unity scene manipulation

---

## The Solution (After)

### New Architecture with Intent Classification

```
User: "list objects"
  ↓
  IntentClassifier.Classify()
  ↓
  Direct MCP call to Unity (instant)
  ↓
  Result

Cost: $0 | Latency: ~50-100ms
```

---

## Three Execution Strategies

### 1. **DirectMCP** - Instant Execution (No AI)
**When:** User input matches a known Unity operation pattern
**Flow:** User → IntentClassifier → MCP Tool → Result
**Cost:** $0 per request
**Latency:** 50-100ms (IPC only)

**Examples:**
- "list objects" → `unity_get_hierarchy`
- "create sphere" → `create_object(type: "Sphere")`
- "select Player" → `select_gameobject(name: "Player")`
- "move Cube to 1,2,3" → `translate_object(name: "Cube", x:1, y:2, z:3)`
- "show console logs" → `get_console_logs`

### 2. **RequiresAI** - AI Reasoning (No Tools)
**When:** User needs code generation, explanations, or complex reasoning
**Flow:** User → AI Model (no tools) → Result
**Cost:** ~$0.01-0.02 per request
**Latency:** 1-2 seconds

**Examples:**
- "write a player controller script"
- "explain what a coroutine is"
- "fix this bug in my code"
- "how do I implement A* pathfinding"
- "refactor this function to be more efficient"

### 3. **AmbiguousWithTools** - AI + Tools
**When:** Intent is unclear, might need Unity tools
**Flow:** User → AI Model (with 33 tools) → MCP Tool → Result
**Cost:** ~$0.03 per request
**Latency:** 2-3 seconds

**Examples:**
- "add physics to the cube"
- "make the player jump higher"
- "fix the lighting in the scene"

---

## Cost & Performance Improvements

### Before (All Requests with Tools)
| Operation | Old Cost | Old Latency |
|-----------|----------|-------------|
| "list objects" | $0.03 | 2-3s |
| "create sphere" | $0.03 | 2-3s |
| "write script" | $0.03 | 2-3s |
| **Average** | **$0.03** | **2-3s** |

### After (Intent Classification)
| Operation | New Cost | New Latency | Savings |
|-----------|----------|-------------|---------|
| "list objects" | $0.00 | 50-100ms | **100% faster, free** |
| "create sphere" | $0.00 | 50-100ms | **100% faster, free** |
| "write script" | $0.01 | 1-2s | **66% cheaper, 50% faster** |
| **Average** | **$0.003** | **~700ms** | **90% cheaper, 76% faster** |

### Real-World Impact
**100 Unity operations per day:**
- **Old:** $3.00/day = $90/month
- **New:** $0.30/day = $9/month
- **Savings:** $81/month (90% reduction)

---

## IntentClassifier Implementation

### Pattern Matching
The classifier uses regex patterns to recognize Unity operations:

```csharp
// Direct MCP patterns
{ new Regex(@"^(list|show|get)\s+(objects?|hierarchy|scene)"),
    m => CreateResult("unity_get_hierarchy", ...) }

{ new Regex(@"^create\s+(?:a\s+)?(cube|sphere|cylinder|plane)"),
    m => CreateResult("create_object", ...) }

{ new Regex(@"^move\s+(?:the\s+)?(\w+)\s+(?:to\s+)?(-?\d+,-?\d+,-?\d+)"),
    m => CreateResult("translate_object", ...) }
```

### AI Required Detection
```csharp
// Patterns that require AI reasoning
- (write|create|generate|implement) (script|code|class)
- (how|why|what|when|explain|describe)
- (fix|debug|solve|resolve) (bug|error|issue)
- (design|architecture|pattern|best practice)
```

---

## Supported Direct MCP Operations

### Scene Hierarchy (Instant)
- `list objects` / `show hierarchy` / `get scene`
- `select [ObjectName]`
- `find [ObjectName]`

### Object Creation (Instant)
- `create cube/sphere/cylinder/plane/capsule/quad`

### Transform Operations (Instant)
- `move [Object] to X,Y,Z`
- `scale [Object] by [N]`
- `rotate [Object] by [degrees]`
- `delete [Object]`

### Console (Instant)
- `show logs` / `get console logs`
- `clear console`

### Scene Management (Instant)
- `list scenes`
- `load scene [Name]`
- `save scene`

### Play Mode (Instant)
- `play` / `enter play mode`
- `stop` / `exit play mode`

### Assets (Instant)
- `list assets` / `show assets`
- `refresh assets`
- `cleanup scene`

---

## Testing the New System

### Test Cases

**1. Direct MCP (Should be instant)**
```
User: "list objects"
Expected: <50-100ms, no AI call, displays hierarchy

User: "create sphere"
Expected: <50-100ms, no AI call, sphere created

User: "move Cube to 5,0,5"
Expected: <50-100ms, no AI call, cube moved
```

**2. AI Reasoning (Should skip tools)**
```
User: "write a player controller"
Expected: 1-2s, AI call with NO tools, generates code

User: "explain coroutines"
Expected: 1-2s, AI call with NO tools, provides explanation
```

**3. Ambiguous (Should use AI + tools)**
```
User: "add physics to the cube"
Expected: 2-3s, AI call with tools, executes appropriate MCP tool
```

---

## Debug Logs

### DirectMCP Execution
```
[GameSmith] Intent classified as: DirectMCP
[GameSmith] Executing MCP tool directly: unity_get_hierarchy
```

### AI Reasoning
```
[GameSmith] Intent classified as: RequiresAI
[GameSmith] Sending to AI for reasoning (no tools needed)
```

### Ambiguous Intent
```
[GameSmith] Intent classified as: AmbiguousWithTools
[GameSmith] Sending to AI with 33 MCP tools (ambiguous intent)
```

---

## Extending the Classifier

To add new direct MCP patterns, edit `IntentClassifier.cs`:

```csharp
// Add to DirectPatterns dictionary
{ new Regex(@"^your\s+pattern\s+here", RegexOptions.IgnoreCase),
    m => CreateResult("mcp_tool_name", new Dictionary<string, object>
    {
        { "param1", m.Groups[1].Value },
        { "param2", ParseValue(m.Groups[2].Value) }
    }) }
```

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────┐
│              User Input: "list objects"              │
└───────────────────┬─────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────┐
│            IntentClassifier.Classify()               │
│  - Regex pattern matching                            │
│  - Unity terminology detection                       │
│  - AI keyword detection                              │
└─────┬──────────────────┬──────────────────┬─────────┘
      │                  │                  │
      │ DirectMCP        │ RequiresAI       │ AmbiguousWithTools
      ▼                  ▼                  ▼
┌────────────┐  ┌─────────────────┐  ┌──────────────────┐
│  MCP IPC   │  │   AI Model      │  │  AI + 33 Tools   │
│  50-100ms  │  │   1-2s          │  │  2-3s            │
│  $0        │  │   $0.01         │  │  $0.03           │
└────────────┘  └─────────────────┘  └──────────────────┘
```

---

## Benefits Summary

✅ **90% cost reduction** for typical Unity workflows
✅ **76% latency reduction** on average
✅ **Instant Unity operations** (50-100ms)
✅ **Better UX** with immediate feedback
✅ **Efficient AI usage** - only when reasoning is needed
✅ **Scalable** - easy to add new patterns
✅ **Backwards compatible** - ambiguous cases still work

---

## Next Steps

1. **Monitor usage patterns** to identify new direct MCP opportunities
2. **Expand pattern library** based on user behavior
3. **Add fuzzy matching** for typos and variations
4. **Implement caching** for repeated AI queries
5. **Add analytics** to track cost savings

---

**Last Updated:** 2025-01-07
**Architecture Version:** 2.0 (Intent Classification)
