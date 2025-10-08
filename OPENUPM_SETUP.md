# OpenUPM Setup Guide

This guide explains how to publish Unity GameSmith to OpenUPM and enable one-click installation via the Needle Package Installer.

> **Unity GameSmith** - The free, privacy-first alternative to Unity Muse Chat

## Prerequisites

- ✅ Package hosted on GitHub (already done)
- ✅ Valid Unity package structure with package.json (already done)
- ✅ Git tags for version releases (automated via semantic-release)
- ✅ MIT License (already done)

## Step 1: Verify Package Structure

Ensure your package meets OpenUPM requirements:

```
unity-gamesmith/
├── UnityPackage/
│   ├── package.json          ← Must exist
│   ├── Editor/               ← Editor scripts
│   ├── Runtime/              ← Runtime scripts (if any)
│   └── README.md             ← Package documentation
```

**✅ Status:** Package structure is compliant

## Step 2: Submit to OpenUPM

### Manual Submission

1. **Visit OpenUPM submission page:**
   - Go to https://openupm.com/
   - Click "Add Package" or "Submit Package"

2. **Fill in package information:**
   - **Repository URL:** `https://github.com/muammar-yacoob/unity-gamesmith`
   - **Package Path:** `UnityPackage` (subfolder containing package.json)
   - **Branch:** `main`
   - **Package Name:** `com.spark-games.unity-gamesmith`
   - **License:** MIT

3. **Submit for review:**
   - OpenUPM team will review the submission
   - Build pipelines will detect Git tags and publish versions

4. **Wait for approval:**
   - Check https://openupm.com/packages/com.spark-games.unity-gamesmith/
   - Typically takes 1-2 days for review

## Step 3: Automated Publishing

Once approved, OpenUPM will automatically:

1. Monitor your repository for new Git tags
2. Detect tags following semantic versioning (e.g., `v1.0.0`, `v1.1.0`)
3. Build and publish new package versions
4. Make them available on the OpenUPM registry

### Our Automated Release Workflow

The GitHub Actions workflow (`.github/workflows/release-publish.yml`) automatically:

- Creates Git tags using semantic-release
- Updates `UnityPackage/package.json` with new version
- Creates GitHub releases
- Triggers OpenUPM build pipeline

**No manual intervention needed after initial setup!**

## Step 4: Verify Publication

After approval, verify the package is live:

1. **Check OpenUPM registry:**
   ```
   https://openupm.com/packages/com.spark-games.unity-gamesmith/
   ```

2. **Test installation in Unity:**
   ```bash
   # Using OpenUPM CLI
   openupm add com.spark-games.unity-gamesmith
   ```

3. **Or use Package Manager:**
   - Add scoped registry manually (see installation docs)
   - Search for "Unity GameSmith"
   - Click Install

## Step 5: Enable Glitch/Needle Installer

Once published to OpenUPM, the one-click installer URL will be:

```
https://package-installer.needle.tools/v1/installer/OpenUPM/com.spark-games.unity-gamesmith?registry=https://package.openupm.com
```

### How It Works

1. User clicks the installer URL
2. Unity Package Manager opens automatically
3. OpenUPM registry is added to Unity project
4. Package is installed with one click

### Update README

Add installation button to README:

```markdown
### OpenUPM (One-Click Install)

Click the button to install directly in Unity:

[**Install Unity GameSmith**](https://package-installer.needle.tools/v1/installer/OpenUPM/com.spark-games.unity-gamesmith?registry=https://package.openupm.com)

*Opens Unity Package Manager and installs automatically*
```

## Version Management

### How Versioning Works

1. Developer commits with conventional commit messages:
   - `feat:` → Minor version bump (1.0.0 → 1.1.0)
   - `fix:` → Patch version bump (1.0.0 → 1.0.1)
   - `BREAKING CHANGE:` → Major version bump (1.0.0 → 2.0.0)

2. GitHub Actions workflow runs semantic-release:
   - Analyzes commit messages
   - Determines new version number
   - Updates both package.json files
   - Creates Git tag (e.g., `v1.1.0`)
   - Creates GitHub release

3. OpenUPM detects new Git tag:
   - Builds package from tagged commit
   - Publishes to OpenUPM registry
   - Makes available for installation

### Current Status

- ✅ Automated versioning configured
- ✅ Git tags created automatically
- ✅ UnityPackage/package.json synced
- ⏳ OpenUPM submission pending (manual step)

## Troubleshooting

### Package Not Appearing in OpenUPM

- Ensure Git tags exist: `git tag --list`
- Verify package.json is valid JSON
- Check Unity version compatibility (2021.3+)
- Wait for nightly build pipeline (runs every 24 hours)

### Version Not Updating

- Confirm new Git tag was created
- Check tag follows semantic versioning
- Wait for OpenUPM build cycle (can take 24 hours)
- Check OpenUPM package page for build status

### Installation Issues

- Ensure scoped registry is added correctly
- Check Unity version compatibility
- Clear Package Manager cache
- Restart Unity Editor

## Resources

- **OpenUPM Documentation:** https://openupm.com/docs/
- **Package Installer Docs:** https://package-installer.needle.tools/
- **Semantic Release:** https://semantic-release.gitbook.io/
- **Unity Package Manager:** https://docs.unity3d.com/Manual/upm-ui.html

## Next Steps

1. ⏳ **Submit package to OpenUPM** (requires manual submission)
2. ⏳ **Wait for approval** (1-2 days)
3. ⏳ **Verify package is live** on OpenUPM registry
4. ✅ **Update README** with OpenUPM installation instructions
5. ✅ **Share installer URL** with users

---

**Once OpenUPM approval is complete, users will have three installation options:**

1. **One-Click Installer** (easiest)
2. **Git URL** (direct from GitHub)
3. **OpenUPM CLI** (for advanced users)
