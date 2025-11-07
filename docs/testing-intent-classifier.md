# Testing Intent Classifier

## Quick Verification Checklist

### ✅ Phase 1: Direct MCP Operations (Instant, No AI)

Test these commands - they should execute **instantly** (50-100ms) with **no AI call**:

#### Scene Hierarchy
- [ ] `list objects` → Should display hierarchy instantly
- [ ] `show hierarchy` → Should display hierarchy instantly
- [ ] `get scene` → Should display hierarchy instantly
- [ ] `what is in the scene` → Should display hierarchy instantly

#### Object Selection
- [ ] `select Player` → Should select "Player" object
- [ ] `select Cube` → Should select "Cube" object
- [ ] `find MainCamera` → Should select "MainCamera" object

#### Object Creation
- [ ] `create cube` → Should create cube instantly
- [ ] `create sphere` → Should create sphere instantly
- [ ] `create a cylinder` → Should create cylinder instantly
- [ ] `create plane` → Should create plane instantly

#### Transform Operations
- [ ] `move Cube to 5,0,5` → Should move cube instantly
- [ ] `scale Player by 2` → Should scale player 2x instantly
- [ ] `rotate Cube by 45` → Should rotate cube instantly

#### Console Operations
- [ ] `show logs` → Should display console logs instantly
- [ ] `get console logs` → Should display console logs instantly
- [ ] `clear console` → Should clear console instantly

#### Scene Management
- [ ] `list scenes` → Should list all scenes instantly
- [ ] `save scene` → Should save current scene instantly

#### Play Mode
- [ ] `play` → Should enter play mode instantly
- [ ] `enter play mode` → Should enter play mode instantly
- [ ] `stop` → Should exit play mode instantly
- [ ] `exit play mode` → Should exit play mode instantly

#### Assets
- [ ] `list assets` → Should list assets instantly
- [ ] `refresh assets` → Should refresh asset database instantly
- [ ] `cleanup scene` → Should clean scene instantly

---

### 🤖 Phase 2: AI Reasoning (1-2s, No Tools)

Test these commands - they should use **AI without tools** (cheaper, faster):

#### Code Generation
- [ ] `write a player controller script` → Should generate code with AI (no tools)
- [ ] `create a jump script` → Should generate code with AI (no tools)
- [ ] `implement A* pathfinding` → Should generate code with AI (no tools)

#### Explanations
- [ ] `explain coroutines` → Should use AI to explain (no tools)
- [ ] `what is a ScriptableObject` → Should use AI to explain (no tools)
- [ ] `how does the physics system work` → Should use AI to explain (no tools)

#### Code Review/Refactoring
- [ ] `refactor this function` → Should use AI (no tools)
- [ ] `fix this bug` → Should use AI (no tools)
- [ ] `optimize this code` → Should use AI (no tools)

---

### 🔀 Phase 3: Ambiguous (2-3s, AI + Tools)

Test these commands - they **might need tools**, AI decides:

- [ ] `add physics to the cube` → AI might use MCP tools
- [ ] `make the player jump higher` → AI might use MCP tools
- [ ] `fix the lighting` → AI might use MCP tools

---

## Expected Console Logs

### Direct MCP (Phase 1)
```
[GameSmith] Intent classified as: DirectMCP
[GameSmith] Executing MCP tool directly: unity_get_hierarchy
```

### AI Reasoning (Phase 2)
```
[GameSmith] Intent classified as: RequiresAI
[GameSmith] Sending to AI for reasoning (no tools needed)
```

### Ambiguous (Phase 3)
```
[GameSmith] Intent classified as: AmbiguousWithTools
[GameSmith] Sending to AI with 33 MCP tools (ambiguous intent)
```

---

## Performance Benchmarks

### Expected Timings

| Command Type | Expected Latency | Expected Cost |
|--------------|------------------|---------------|
| Direct MCP | 50-100ms | $0.00 |
| AI Reasoning | 1-2s | $0.01 |
| Ambiguous | 2-3s | $0.03 |

### Monitoring

Watch Unity console for these metrics:
1. Intent classification type
2. Tool execution path
3. Response time
4. Token usage (if available)

---

## Common Issues & Solutions

### Issue: Direct MCP not working
**Symptom:** Commands like "list objects" go to AI instead of direct execution
**Fix:** Check console logs - ensure MCP server is connected

### Issue: All commands go to AI
**Symptom:** Every command shows "Sending to AI with 33 tools"
**Fix:** Verify IntentClassifier.cs is compiled and loaded correctly

### Issue: MCP server not connected
**Symptom:** "❌ MCP tools not available" message
**Fix:** Restart Unity Editor or manually start MCP server

---

## Regression Testing

After implementing intent classification, verify:

1. **Previous functionality still works**
   - [ ] Chat history persists
   - [ ] Settings window still accessible
   - [ ] Model switching works
   - [ ] Clear chat works

2. **MCP integration works**
   - [ ] MCP server auto-starts
   - [ ] Tool execution completes
   - [ ] Results display correctly
   - [ ] Error handling works

3. **AI responses work**
   - [ ] Code generation works
   - [ ] Explanations work
   - [ ] Multi-turn conversations work
   - [ ] Tool use (for ambiguous intents) works

---

## Success Criteria

✅ **Phase 1 commands execute in <200ms**
✅ **Phase 2 commands don't send tool definitions**
✅ **Phase 3 commands work with AI decision-making**
✅ **Console logs show correct intent classification**
✅ **No regressions in existing functionality**
✅ **User experience feels instant for Unity operations**

---

## Test Results

### Date: _________
### Tester: _________

| Test Category | Pass/Fail | Notes |
|---------------|-----------|-------|
| Direct MCP - Hierarchy | ☐ | |
| Direct MCP - Selection | ☐ | |
| Direct MCP - Creation | ☐ | |
| Direct MCP - Transform | ☐ | |
| Direct MCP - Console | ☐ | |
| Direct MCP - Scenes | ☐ | |
| Direct MCP - Play Mode | ☐ | |
| Direct MCP - Assets | ☐ | |
| AI Reasoning - Code Gen | ☐ | |
| AI Reasoning - Explanations | ☐ | |
| Ambiguous - AI + Tools | ☐ | |
| Regression Tests | ☐ | |

### Overall Assessment:
- [ ] All tests passed
- [ ] Minor issues (list below)
- [ ] Major issues (list below)

### Issues Found:
```
[List any issues here]
```

### Performance Metrics:
- Average Direct MCP latency: _____ ms
- Average AI Reasoning latency: _____ s
- Average Ambiguous latency: _____ s

---

**Last Updated:** 2025-01-07
