import * as fs from 'fs-extra';
import * as path from 'path';
import Handlebars from 'handlebars';

export interface PlayerSetupParams {
  projectPath: string;
  movementSpeed?: number;
  shootingCooldown?: number;
  maxHealth?: number;
}

export class PlayerService {
  private templatesDir = path.join(__dirname, '../templates');

  async setupPlayer(params: PlayerSetupParams) {
    const {
      projectPath,
      movementSpeed = 5.0,
      shootingCooldown = 0.5,
      maxHealth = 100
    } = params;

    // Validate project path
    const scriptsPath = path.join(projectPath, 'Assets/Scripts/Player');
    if (!await fs.pathExists(scriptsPath)) {
      throw new Error(`Project path not found: ${projectPath}. Please create a Unity project first.`);
    }

    // Generate C# scripts
    const scripts = await this.generateScripts({
      movementSpeed,
      shootingCooldown,
      maxHealth
    });

    // Write scripts to project
    const scriptFiles = [
      { name: 'PlayerController.cs', content: scripts.controller },
      { name: 'PlayerHealth.cs', content: scripts.health },
      { name: 'PlayerShooting.cs', content: scripts.shooting }
    ];

    for (const file of scriptFiles) {
      const filePath = path.join(scriptsPath, file.name);
      await fs.writeFile(filePath, file.content);

      // Create .meta file for Unity
      await this.createMetaFile(filePath);
    }

    // Create player prefab YAML
    await this.createPlayerPrefab(projectPath);

    return {
      success: true,
      scriptsGenerated: scriptFiles.map(f => f.name),
      location: scriptsPath,
      message: 'Player setup completed successfully'
    };
  }

  private async generateScripts(params: {
    movementSpeed: number;
    shootingCooldown: number;
    maxHealth: number;
  }) {
    const templates = {
      controller: await fs.readFile(path.join(this.templatesDir, 'PlayerController.cs.hbs'), 'utf-8'),
      health: await fs.readFile(path.join(this.templatesDir, 'PlayerHealth.cs.hbs'), 'utf-8'),
      shooting: await fs.readFile(path.join(this.templatesDir, 'PlayerShooting.cs.hbs'), 'utf-8')
    };

    return {
      controller: Handlebars.compile(templates.controller)(params),
      health: Handlebars.compile(templates.health)(params),
      shooting: Handlebars.compile(templates.shooting)(params)
    };
  }

  private async createMetaFile(scriptPath: string) {
    const guid = this.generateGuid();
    const metaContent = `fileFormatVersion: 2
guid: ${guid}
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData:
  assetBundleName:
  assetBundleVariant:
`;
    await fs.writeFile(`${scriptPath}.meta`, metaContent);
  }

  private async createPlayerPrefab(projectPath: string) {
    const prefabPath = path.join(projectPath, 'Assets/Prefabs/Player');
    await fs.ensureDir(prefabPath);

    const prefabYaml = `%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 4}
  - component: {fileID: 212}
  - component: {fileID: 50}
  - component: {fileID: 58}
  - component: {fileID: 114}
  - component: {fileID: 115}
  - component: {fileID: 116}
  m_Layer: 0
  m_Name: Player
  m_TagString: Player
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &4
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children:
  - {fileID: 5}
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!212 &212
SpriteRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {fileID: 0}
  m_ProbeAnchor: {fileID: 0}
  m_LightProbeVolumeOverride: {fileID: 0}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {fileID: 0}
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_Sprite: {fileID: 0}
  m_Color: {r: 0, g: 0.5, b: 1, a: 1}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {x: 1, y: 1}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 0
  m_MaskInteraction: 0
  m_SpriteSortPoint: 0
--- !u!50 &50
Rigidbody2D:
  serializedVersion: 4
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_BodyType: 0
  m_Simulated: 1
  m_UseFullKinematicContacts: 0
  m_UseAutoMass: 0
  m_Mass: 1
  m_LinearDrag: 0
  m_AngularDrag: 0.05
  m_GravityScale: 0
  m_Material: {fileID: 0}
  m_Interpolate: 0
  m_SleepingMode: 1
  m_CollisionDetection: 0
  m_Constraints: 4
--- !u!58 &58
CircleCollider2D:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_Density: 1
  m_Material: {fileID: 0}
  m_IsTrigger: 0
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {x: 0, y: 0}
  serializedVersion: 2
  m_Radius: 0.5
--- !u!114 &114
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${this.generateGuid()}, type: 3}
  m_Name:
  m_EditorClassIdentifier:
--- !u!114 &115
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${this.generateGuid()}, type: 3}
  m_Name:
  m_EditorClassIdentifier:
--- !u!114 &116
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${this.generateGuid()}, type: 3}
  m_Name:
  m_EditorClassIdentifier:
--- !u!1 &5
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 6}
  m_Layer: 0
  m_Name: FirePoint
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &6
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 5}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0.5, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_Children: []
  m_Father: {fileID: 4}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
`;

    const prefabFile = path.join(prefabPath, 'Player.prefab');
    await fs.writeFile(prefabFile, prefabYaml);

    // Create .meta file for prefab
    await this.createMetaFile(prefabFile);
  }

  private generateGuid(): string {
    return 'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'.replace(/x/g, () => {
      return Math.floor(Math.random() * 16).toString(16);
    });
  }
}
