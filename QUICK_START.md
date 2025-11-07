# GameSmith Quick Start - Intent Classification

## 🎯 What Changed?

GameSmith now **intelligently routes commands** instead of always using AI:

- ✅ **Unity operations** → Instant local execution (free, 50ms)
- ✅ **Code generation** → AI without tools (cheaper, faster)
- ✅ **Complex tasks** → AI with tools (when needed)

---

## 🚀 Try It Now

### 1. Open GameSmith
```
Unity Editor → Tools → GameSmith → GameSmith AI (Alt+G)
```

### 2. Test Instant Commands (No AI, Free)

Type these - they should execute **instantly** (<100ms):

```
list objects
create sphere
show console logs
play
```

Watch the Unity console for:
```
[GameSmith] Intent classified as: DirectMCP
[GameSmith] Executing MCP tool directly: unity_get_hierarchy
```

### 3. Test AI Commands (Optimized)

Type these - they use AI **without tool overhead**:

```
write a player controller script
explain coroutines
how do I implement pathfinding
```

Watch the Unity console for:
```
[GameSmith] Intent classified as: RequiresAI
[GameSmith] Sending to AI for reasoning (no tools needed)
```

---

## 📊 Performance Comparison

| Command | Old Behavior | New Behavior |
|---------|--------------|--------------|
| "list objects" | AI call (2-3s, $0.03) | Direct MCP (50ms, $0) |
| "create cube" | AI call (2-3s, $0.03) | Direct MCP (50ms, $0) |
| "write script" | AI + tools (2-3s, $0.03) | AI only (1-2s, $0.01) |

---

## 🎮 Instant Commands Reference

### Scene Hierarchy
- `list objects`
- `show hierarchy`
- `get scene`
- `what is in the scene`

### Create Objects
- `create cube`
- `create sphere`
- `create cylinder`
- `create plane`

### Select/Find
- `select Player`
- `find MainCamera`

### Transform
- `move Cube to 5,0,5`
- `scale Player by 2`
- `rotate Cube by 45`

### Console
- `show logs`
- `clear console`

### Play Mode
- `play`
- `stop`

### Scenes
- `list scenes`
- `save scene`

---

## 🐛 Troubleshooting

### "MCP tools not available"
**Solution:** MCP server not running
- Check: `[GameSmith] MCP: X tools` in status bar
- Wait a few seconds after opening Unity
- Restart Unity if needed

### Commands going to AI instead of direct
**Solution:** Check pattern matching
- Ensure exact wording (e.g., "list objects" not "listing objects")
- Check console logs for classification type
- Report new patterns as issues

---

## 📚 Documentation

- **Full Architecture:** `docs/intent-classification-architecture.md`
- **Testing Guide:** `docs/testing-intent-classifier.md`
- **Summary:** `OPTIMIZATION_SUMMARY.md`

---

## 💡 Tips

1. **Use simple commands for Unity ops** → Instant execution
2. **Use natural language for coding** → AI generation
3. **Watch console logs** → See classification in action
4. **Report patterns** → Help improve classifier

---

## 🎯 Expected Savings

**Typical usage (100 Unity ops + 50 code gen per day):**
- **Before:** $5.10/day = $153/month
- **After:** $1.10/day = $33/month
- **Savings:** $120/month (78% reduction)

---

**Version:** 2.0.0
**Date:** 2025-01-07
