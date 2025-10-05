import { FastMCP } from "fastmcp";

/**
 * Register all prompts with the MCP server
 *
 * @param server The FastMCP server instance
 */
export function registerPrompts(server: FastMCP) {
  // Unity game design assistance prompt
  server.addPrompt({
    name: "game_design_help",
    description: "Get help with Unity game design tasks",
    arguments: [
      {
        name: "task",
        description: "The game design task you need help with",
        required: true
      }
    ],
    load: async (args: Record<string, unknown>) => {
      const task = args.task as string;
      return `I'll help you with your Unity game design task: ${task}

I have access to the following Unity automation tools:
- Project initialization and management
- Player setup (movement, shooting, health)
- Enemy creation (AI, health, attacks)
- Level system (wave-based progression, difficulty)
- UI generation (HUD, menus, screens)
- Collision system configuration
- 3D character import from Sketchfab
- Scene structure setup (camera, lighting, GameObjects)

What specific assistance do you need with this task?`;
    }
  });

  // Game rule analysis prompt
  server.addPrompt({
    name: "analyze_game_rules",
    description: "Analyze and fix game rule violations",
    arguments: [
      {
        name: "projectPath",
        description: "Path to the Unity project",
        required: false
      }
    ],
    load: async (args: Record<string, unknown>) => {
      const projectPath = args.projectPath as string | undefined;
      const projectInfo = projectPath ? `for project at ${projectPath}` : "for the current project";
      return `I'll analyze the game rules ${projectInfo} and help fix any violations.

I will:
1. Run design rule checks to identify gameplay issues (e.g., unbalanced levels, unfair mechanics)
2. Run technical checks to identify script issues (e.g., performance bottlenecks, logic errors)
3. Provide detailed information about any errors or warnings
4. Suggest fixes for common issues

Would you like me to proceed with the analysis?`;
    }
  });

  // GameObject placement prompt
  server.addPrompt({
    name: "place_game_objects",
    description: "Guide for placing GameObjects in a Unity scene",
    arguments: [
      {
        name: "objectType",
        description: "Type of GameObjects to place (e.g., enemies, power-ups, obstacles)",
        required: false
      }
    ],
    load: async (args: Record<string, unknown>) => {
      const objectType = args.objectType as string | undefined;
      const typeInfo = objectType ? ` for ${objectType}` : "";
      return `I'll help you place GameObjects${typeInfo} in your Unity scene.

I can assist with:
- Adding GameObjects with specific prefabs and components
- Positioning GameObjects at specific coordinates
- Setting GameObject rotation and scale
- Getting a list of all current GameObjects
- Organizing GameObjects for optimal scene layout

What GameObjects would you like to place, and where?`;
    }
  });

  // Export and build prompt
  server.addPrompt({
    name: "prepare_build",
    description: "Prepare Unity project for building",
    arguments: [
      {
        name: "platform",
        description: "Target platform (e.g., Windows, macOS, WebGL)",
        required: false
      }
    ],
    load: async (args: Record<string, unknown>) => {
      const platform = args.platform as string | undefined;
      const platformInfo = platform ? ` for ${platform}` : "";
      return `I'll help you prepare your Unity project for building${platformInfo}.

I can configure:
- Build settings (platform, architecture, quality)
- Player settings (resolution, icons, splash screen)
- Asset bundling and optimization
- Game executable generation
- Installer creation (future feature)

Which build files do you need, and for what platform?`;
    }
  });

  // Scene routing assistance prompt
  server.addPrompt({
    name: "auto_route_scenes",
    description: "Help with automatic scene transitions and flow",
    arguments: [],
    load: async () => {
      return `I'll help you set up automatic scene transitions and game flow.

Auto-routing can:
- Automatically connect scenes based on game progression
- Route player between levels and menus
- Optimize scene loading for smooth transitions
- Respect game logic and design rules

Important considerations:
- Manual review is recommended after auto-routing
- Critical transitions may need manual configuration
- Game logic compliance should be verified with testing

Would you like me to proceed with auto-routing scenes?`;
    }
  });

  // Project creation prompt
  server.addPrompt({
    name: "create_unity_project",
    description: "Guide for creating a new Unity project",
    arguments: [
      {
        name: "projectType",
        description: "Type of project (e.g., 2D shooter, 3D platformer, VR experience)",
        required: false
      }
    ],
    load: async (args: Record<string, unknown>) => {
      const projectType = args.projectType as string | undefined;
      const typeInfo = projectType ? ` for a ${projectType}` : "";
      return `I'll help you create a new Unity project${typeInfo}.

To get started, I need:
- Project name
- Project location (optional, defaults to current directory)
- Any specific requirements or templates

What would you like to name your project?`;
    }
  });
}
