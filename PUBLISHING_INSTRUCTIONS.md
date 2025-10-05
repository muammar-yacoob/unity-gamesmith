# Publishing Instructions for @spark-apps/unity-mcp

## ✅ Pre-publish Checklist (COMPLETED)

All preparation steps have been completed:

- ✅ Package renamed to `@spark-apps/unity-mcp`
- ✅ Version updated to `0.2.0`
- ✅ Built production bundle with TypeScript
- ✅ Added CLI shebang to `dist/index.js`
- ✅ Copied README.md and LICENSE to package
- ✅ Configured `package.json` with:
  - `bin` entry for CLI usage
  - `files` array for npm publish
  - Repository, bugs, homepage URLs
  - Enhanced keywords (sketchfab, cinemachine, 3d-character)
- ✅ Package contents verified (92 files, 211.8 kB unpacked)
- ✅ All commits pushed to git

## 📦 Package Details

**Name:** `@spark-apps/unity-mcp`
**Version:** `0.2.0`
**Size:** 43.6 kB (compressed), 211.8 kB (unpacked)
**Files:** 92 files including:
- All Unity C# templates (16 .hbs files)
- All service modules (TypeScript transpiled)
- Type definitions (.d.ts files)
- README.md and LICENSE

## 🚀 Publishing Steps

### 1. Log in to npm

```bash
cd /mnt/d/MCPs/Unity-MCP/packages/mcp-server
npm login
```

You'll be prompted for:
- **Username:** (your npm username)
- **Password:** (your npm password)
- **Email:** (your npm email)
- **OTP:** (if you have 2FA enabled)

### 2. Verify you're logged in

```bash
npm whoami
```

Should display your npm username.

### 3. Final pre-publish check (optional)

```bash
npm pack --dry-run
```

This shows what will be published without actually publishing.

### 4. Publish to npm

```bash
npm publish --access public
```

Since this is a scoped package (`@spark-apps/`), you need `--access public` to make it publicly available.

### 5. Verify publication

After publishing, verify at:
- https://www.npmjs.com/package/@spark-apps/unity-mcp
- Or run: `npm view @spark-apps/unity-mcp`

## 🎯 Post-publish Steps

### 1. Test installation

```bash
# In a new directory
npm install -g @spark-apps/unity-mcp

# Verify CLI works
unity-mcp --help

# Test with Claude Desktop
# Add to claude_desktop_config.json:
{
  "mcpServers": {
    "unity-mcp": {
      "command": "npx",
      "args": ["-y", "@spark-apps/unity-mcp"]
    }
  }
}
```

### 2. Create GitHub Release

```bash
git tag v0.2.0
git push origin v0.2.0
```

Then create a release on GitHub with release notes highlighting:
- ✨ 3D character import from Sketchfab
- 📹 Cinemachine camera follow system
- 🎨 Complete UI system with HUD, menus, screens
- All existing 2D shooter features

### 3. Update Social Media / Announcements

Share on:
- GitHub Discussions
- Twitter/X (if applicable)
- Discord servers (if applicable)
- Model Context Protocol community

## 🔄 Future Releases

For subsequent releases:

1. Update version in `package.json`:
   ```bash
   npm version patch  # 0.2.0 -> 0.2.1
   npm version minor  # 0.2.0 -> 0.3.0
   npm version major  # 0.2.0 -> 1.0.0
   ```

2. Build and test:
   ```bash
   bun run build
   npm pack --dry-run
   ```

3. Commit and publish:
   ```bash
   git add .
   git commit -m "chore: release vX.Y.Z"
   git push
   npm publish --access public
   ```

## 🆘 Troubleshooting

### "401 Unauthorized" error
- Run `npm login` again
- Check your credentials
- Ensure 2FA code is correct (if enabled)

### "403 Forbidden" error
- Package name might be taken
- Check if you have permission to publish under `@spark-apps` scope
- Try `npm publish --access public`

### Package not appearing on npm
- Wait a few minutes (can take 5-10 minutes to index)
- Clear npm cache: `npm cache clean --force`
- Try: `npm view @spark-apps/unity-mcp`

### CLI not working after install
- Check shebang is present: `head -1 node_modules/@spark-apps/unity-mcp/dist/index.js`
- Should show: `#!/usr/bin/env node`
- Verify executable: `ls -la $(which unity-mcp)`

## 📊 Package Stats

Monitor your package:
- npm downloads: https://npm-stat.com/charts.html?package=@spark-apps/unity-mcp
- npm trends: https://npmtrends.com/@spark-apps/unity-mcp
- bundlephobia: https://bundlephobia.com/package/@spark-apps/unity-mcp

---

**Ready to publish!** Just run the commands in the "Publishing Steps" section above.
