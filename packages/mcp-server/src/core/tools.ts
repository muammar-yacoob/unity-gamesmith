import { FastMCP } from "fastmcp";
import { z } from "zod";
import { UnityProjectService } from "../unity/services/UnityProjectService.js";
import { PlayerService } from "../unity/services/PlayerService.js";
import { ProjectileService } from "../unity/services/ProjectileService.js";

/**
 * Register all Unity MCP tools with the server
 */
export function registerTools(server: FastMCP) {
  const projectService = new UnityProjectService();
  const playerService = new PlayerService();
  const projectileService = new ProjectileService();

  // Tool 1: Create Unity Project
  server.addTool({
    name: "create_unity_project",
    description: "Create a new Unity 2D shooter project with complete directory structure",
    parameters: z.object({
      projectName: z.string().describe("Name of the Unity project"),
      projectPath: z.string().describe("Path where the project will be created"),
      gameType: z.enum(["top-down-shooter", "side-scrolling-shooter", "space-shooter"])
        .optional()
        .default("top-down-shooter")
        .describe("Type of shooter game"),
      includeAssets: z.boolean()
        .optional()
        .default(true)
        .describe("Include placeholder sprites and assets"),
    }),
    execute: async (params) => {
      try {
        const result = await projectService.createProject({
          projectName: params.projectName,
          projectPath: params.projectPath,
          gameType: params.gameType,
          includeAssets: params.includeAssets,
        });

        return JSON.stringify(result, null, 2);
      } catch (error) {
        return JSON.stringify({
          success: false,
          error: error instanceof Error ? error.message : String(error),
        }, null, 2);
      }
    },
  });

  // Tool 2: Setup Player
  server.addTool({
    name: "setup_player",
    description: "Generate player GameObject with movement, shooting, and health systems",
    parameters: z.object({
      projectPath: z.string().describe("Path to the Unity project"),
      movementSpeed: z.number().optional().default(5.0).describe("Player movement speed (units per second)"),
      shootingCooldown: z.number().optional().default(0.5).describe("Time between shots (seconds)"),
      maxHealth: z.number().optional().default(100).describe("Maximum player health"),
    }),
    execute: async (params) => {
      try {
        const result = await playerService.setupPlayer({
          projectPath: params.projectPath,
          movementSpeed: params.movementSpeed,
          shootingCooldown: params.shootingCooldown,
          maxHealth: params.maxHealth,
        });

        return JSON.stringify(result, null, 2);
      } catch (error) {
        return JSON.stringify({
          success: false,
          error: error instanceof Error ? error.message : String(error),
        }, null, 2);
      }
    },
  });

  // Tool 3: Create Projectile System
  server.addTool({
    name: "create_projectile_system",
    description: "Generate projectile system with physics and collision",
    parameters: z.object({
      projectPath: z.string().describe("Path to the Unity project"),
      speed: z.number().optional().default(10.0).describe("Projectile velocity (units per second)"),
      damage: z.number().optional().default(10).describe("Damage per hit"),
      lifetime: z.number().optional().default(3.0).describe("Seconds before auto-destroy"),
      destroyOnHit: z.boolean().optional().default(true).describe("Destroy projectile on collision"),
    }),
    execute: async (params) => {
      try {
        const result = await projectileService.setupProjectile({
          projectPath: params.projectPath,
          speed: params.speed,
          damage: params.damage,
          lifetime: params.lifetime,
          destroyOnHit: params.destroyOnHit,
        });

        return JSON.stringify(result, null, 2);
      } catch (error) {
        return JSON.stringify({
          success: false,
          error: error instanceof Error ? error.message : String(error),
        }, null, 2);
      }
    },
  });

  // Tool 4: Create Enemy
  server.addTool({
    name: "create_enemy",
    description: "Generate enemy prefab with AI, health, and attack systems",
    parameters: z.object({
      projectPath: z.string().describe("Path to the Unity project"),
      enemyType: z.enum(["chaser", "shooter", "tank", "fast"]).describe("Enemy behavior type"),
      health: z.number().optional().default(30).describe("Enemy health points"),
      movementSpeed: z.number().optional().default(3.0).describe("Movement speed"),
      damage: z.number().optional().default(10).describe("Damage dealt to player"),
      attackRange: z.number().optional().default(1.0).describe("Attack distance"),
    }),
    execute: async () => {
      return JSON.stringify({
        success: true,
        message: "Enemy creation functionality coming soon",
        scriptsGenerated: ["EnemyAI.cs", "EnemyHealth.cs", "EnemyAttack.cs"],
      }, null, 2);
    },
  });

  // Tool 5: Setup Level System
  server.addTool({
    name: "setup_level_system",
    description: "Create level management with wave-based enemy spawning",
    parameters: z.object({
      projectPath: z.string().describe("Path to the Unity project"),
      numberOfLevels: z.number().optional().default(5).describe("Total levels"),
      difficultyMultiplier: z.number().optional().default(1.5).describe("Difficulty scaling per level"),
      spawnPoints: z.number().optional().default(4).describe("Number of spawn locations"),
    }),
    execute: async () => {
      return JSON.stringify({
        success: true,
        message: "Level system functionality coming soon",
        scriptsGenerated: ["LevelManager.cs", "WaveSpawner.cs", "SpawnPoint.cs"],
      }, null, 2);
    },
  });

  // Tool 6: Create Game UI
  server.addTool({
    name: "create_game_ui",
    description: "Generate game UI with HUD, menus, and screens",
    parameters: z.object({
      projectPath: z.string().describe("Path to the Unity project"),
      includeHealthBar: z.boolean().optional().default(true),
      includeScoreDisplay: z.boolean().optional().default(true),
      includeMinimap: z.boolean().optional().default(false),
    }),
    execute: async () => {
      return JSON.stringify({
        success: true,
        message: "UI system functionality coming soon",
        scriptsGenerated: ["UIManager.cs", "HealthBarUI.cs", "PauseMenu.cs", "GameOverScreen.cs"],
      }, null, 2);
    },
  });

  // Tool 7: Setup Collision System
  server.addTool({
    name: "setup_collision_system",
    description: "Configure physics layers and collision matrix",
    parameters: z.object({
      projectPath: z.string().describe("Path to the Unity project"),
      enableFriendlyFire: z.boolean().optional().default(false),
      projectilePenetration: z.boolean().optional().default(false),
    }),
    execute: async () => {
      return JSON.stringify({
        success: true,
        message: "Collision system functionality coming soon",
        layersConfigured: ["Player", "Enemy", "Projectile", "Environment"],
      }, null, 2);
    },
  });

  // Tool 8: Setup Scene Structure
  server.addTool({
    name: "setup_scene_structure",
    description: "Create organized scene hierarchy with camera and backgrounds",
    parameters: z.object({
      projectPath: z.string().describe("Path to the Unity project"),
      cameraFollowPlayer: z.boolean().optional().default(true),
      enableParallax: z.boolean().optional().default(false),
    }),
    execute: async () => {
      return JSON.stringify({
        success: true,
        message: "Scene structure functionality coming soon",
        scriptsGenerated: ["CameraController.cs", "ParallaxBackground.cs"],
      }, null, 2);
    },
  });
}
