# 📋 Changelog

All notable changes to **Unity GameSmith** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

# [1.4.0](https://github.com/muammar-yacoob/unity-gamesmith/compare/v1.3.0...v1.4.0) (2025-10-09)


### Features

* **mcp:** add MCP integration with Taskmaster and Chrome DevTools ([5f60fce](https://github.com/muammar-yacoob/unity-gamesmith/commit/5f60fce1654505b98674bfed048ee268b06558af))
  - Added Taskmaster MCP for PRD-based planning and task tracking
  - Added Chrome DevTools MCP for WebGL testing and performance profiling
  - Created comprehensive MCP integration documentation
  - Created detailed WebGL testing guide
  - Updated README with MCP Integration section

* **wizard:** create Project Wizard PRD for AI-powered project initialization ([prd_project_wizard.txt](.taskmaster/docs/prd_project_wizard.txt))
  - Modern multi-step wizard for project setup
  - Tech stack selection (UniTask, DOTween, Mirror, Netcode)
  - AI-generated MVP PRD from user inputs
  - Visual task dashboard with progress tracking


### Bug Fixes

* **editor:** fix Unity GUI initialization error in GameSmithWindow ([5f60fce](https://github.com/muammar-yacoob/unity-gamesmith/commit/5f60fce1654505b98674bfed048ee268b06558af))
  - Moved InitializeStyles() from OnEnable() to OnGUI()
  - Added lazy initialization to prevent GUI.skin access violation

* **upm:** resolve "update available" notification issue ([6e5efb9](https://github.com/muammar-yacoob/unity-gamesmith/commit/6e5efb90d542a66f3e88c06e9e6e85c48b3bcd9f))
  - Bumped version from 1.2.0 to 1.4.0
  - Fixed version mismatch between package.json and git tags


# [1.3.0](https://github.com/muammar-yacoob/unity-gamesmith/compare/v1.2.0...v1.3.0) (2025-10-09)


### Bug Fixes

* unify all namespaces to SparkGames.UnityGameSmith.Editor ([9c3d2bd](https://github.com/muammar-yacoob/unity-gamesmith/commit/9c3d2bdc144c2baf87f69bf560570369e4de2314))


### Features

* **editor:** simplify AI config with provider dropdown system ([3842de2](https://github.com/muammar-yacoob/unity-gamesmith/commit/3842de2edb8d885c6e022ff77a5af96ed0be38fc))

# [1.2.0](https://github.com/muammar-yacoob/unity-gamesmith/compare/v1.1.0...v1.2.0) (2025-10-08)


### Features

* consolidate editor into unified Game Smith window with SparkCore-inspired UI ([3bb9e6b](https://github.com/muammar-yacoob/unity-gamesmith/commit/3bb9e6b4d03488de689fb18cd3149921977ac5b8))

# 📋 Changelog

All notable changes to **Unity GameSmith** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

# 1.0.0 (2025-10-08)


### Bug Fixes

* add missing KiCad client methods and fix MCP type errors ([295002c](https://github.com/muammar-yacoob/unity-gamesmith/commit/295002cd60091d58ce9085dc6d057a49564e610a))
* add missing Unity meta files and update repository configuration ([3e9d52e](https://github.com/muammar-yacoob/unity-gamesmith/commit/3e9d52e5a3838afbb995c51f25596bdf141deb39))
* add start script for MCP server ([fb5ab9d](https://github.com/muammar-yacoob/unity-gamesmith/commit/fb5ab9d4f9ce8a8d411294eb6e67ef160aed4eb0))
* remove console.log breaking JSON responses and fix all ESLint errors ([ba709b2](https://github.com/muammar-yacoob/unity-gamesmith/commit/ba709b278e6430d6f4ba96963abf0a4493e8b2d2))
* use standard GITHUB_TOKEN instead of PAT_GITHUB ([cff63ed](https://github.com/muammar-yacoob/unity-gamesmith/commit/cff63edd42f35da50a228c5c9010de558c3d311c))


### Features

* add 3D character import with Sketchfab integration ([a705a47](https://github.com/muammar-yacoob/unity-gamesmith/commit/a705a47dbffcf98f68594f4b61e3e2b841cf1ad6))
* convert to Unity MCP server with comprehensive PRD ([6880e54](https://github.com/muammar-yacoob/unity-gamesmith/commit/6880e544f25feddb08a39261c0d9a3fcb3793076))
* enhance release workflow with intelligent triggering and distribution packages ([1843109](https://github.com/muammar-yacoob/unity-gamesmith/commit/1843109756579af8fe05ebadfe572aa675774e5a))
* implement create_enemy tool (Task 25) ([643400d](https://github.com/muammar-yacoob/unity-gamesmith/commit/643400df18fcb6418ed141bd58690cae5b2afb3e))
* implement create_game_ui tool (Task 27) ([3ab9e1b](https://github.com/muammar-yacoob/unity-gamesmith/commit/3ab9e1b89215fc68692f26bd4bbc110b06aee997))
* implement create_projectile_system tool (Task 24) ([6cd4e60](https://github.com/muammar-yacoob/unity-gamesmith/commit/6cd4e60696cdfedd3fda2adf56c4f5d232c85ac3))
* implement create_unity_project tool (task 22) ([535590c](https://github.com/muammar-yacoob/unity-gamesmith/commit/535590ce943696ddd437964ff0ad59f0b9010643))
* implement file-based KiCad client with real file generation ([4ecbf93](https://github.com/muammar-yacoob/unity-gamesmith/commit/4ecbf93b8ba57b0ffefca9fd134a4a2193fce730))
* implement project creation with templates and prompt parsing ([71f725d](https://github.com/muammar-yacoob/unity-gamesmith/commit/71f725d4ae2733785dd4406ee2a2feea96361c7b))
* implement setup_level_system tool (Task 26) ([8d6f7d6](https://github.com/muammar-yacoob/unity-gamesmith/commit/8d6f7d65744ae91f7658aa969f2ad504e29c1d45))
* implement setup_player tool with C# script generation (task 23) ([7539b50](https://github.com/muammar-yacoob/unity-gamesmith/commit/7539b503feee3a2700d3aa265062a1438dcce1cc))
* integrate semantic-release for automated versioning and publishing ([7c21611](https://github.com/muammar-yacoob/unity-gamesmith/commit/7c21611c42dc70bd83787ba47fce951b08ecc8f8))
* remove husky and simplify development workflow ([7e26cf8](https://github.com/muammar-yacoob/unity-gamesmith/commit/7e26cf8f375a977dd8d4f94d740e86f03ff840d2))
* upgrade to modern tooling stack with Bun + Biome ([261c350](https://github.com/muammar-yacoob/unity-gamesmith/commit/261c3508c0af77433ea097c296872aff671079fa))


### BREAKING CHANGES

* Removed git hooks and lint-staged configuration

Benefits:
- Eliminates semantic-release conflicts
- Faster commits without pre-commit checks
- Simpler CI/CD pipeline
- Less configuration complexity

Changes:
- Remove .husky directory and all git hooks
- Remove lint-staged configuration
- Remove husky and lint-staged dependencies
- Simplify package.json scripts
- Clean up workflow configuration

Code quality is now enforced via:
- GitHub Actions CI/CD
- Manual biome commands when needed
- Developer discretion
* Switched from pnpm to Bun and ESLint/Prettier to Biome

Performance improvements:
- 3-5x faster installs with Bun
- 10-100x faster linting/formatting with Biome
- Reduced bundle size and complexity

Tooling changes:
- Replace pnpm with Bun package manager
- Replace ESLint + Prettier with Biome
- Update all scripts to use Bun
- Modernize lint-staged configuration
- Remove legacy ESLint config files

Developer experience:
- Faster CI/CD builds
- Simplified configuration
- Better error messages
- Zero-config setup
* Replaces MockKiCadClient with FileKiCadClient as default

- Created Python bridge (kicad_bridge.py) using kiutils library
- Implemented FileKiCadClient that generates real .kicad_pro, .kicad_sch, .kicad_pcb files
- Added 3D model generation via KiCad CLI (STEP/VRML formats)
- Updated documentation to reflect file-based implementation
- Created SETUP.md with comprehensive installation instructions
- Added requirements.txt for Python dependencies (kiutils)

The MCP now creates actual KiCad files that can be opened and edited in KiCad software.

Components:
- Python bridge handles file generation via kiutils
- TypeScript FileKiCadClient spawns Python process for file operations
- KiCad CLI integration for 3D model export

Prerequisites:
- Python 3 with kiutils package (required)
- KiCad with CLI tools (optional, for 3D export)

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>

# [1.0.0](https://github.com/muammar-yacoob/unity-gamesmith/releases/tag/v1.0.0) (2025-10-08)

### Features

- Initial release of Unity GameSmith
- AI-powered code generation with Ollama and OpenAI support
- Enhanced window with template library browser
- 10+ production-ready Unity code templates
- Favorites system for quick template access
- Natural language command processing
- Classic window for streamlined workflow
- Automatic script generation in Assets/Scripts/
- Support for 2D shooter mechanics, player systems, enemy AI, and UI components
