import * as fs from 'fs-extra';
import * as path from 'path';
import Handlebars from 'handlebars';

export interface ProjectileSetupParams {
  projectPath: string;
  speed?: number;
  lifetime?: number;
  damage?: number;
  destroyOnHit?: boolean;
}

export class ProjectileService {
  private templatesDir = path.join(__dirname, '../templates');

  async setupProjectile(params: ProjectileSetupParams) {
    const {
      projectPath,
      speed = 10.0,
      lifetime = 3.0,
      damage = 10,
      destroyOnHit = true
    } = params;

    const scriptsPath = path.join(projectPath, 'Assets/Scripts/Weapons');
    if (!await fs.pathExists(scriptsPath)) {
      throw new Error(`Weapons scripts path not found: ${scriptsPath}`);
    }

    // Generate Projectile script
    const script = await this.generateProjectileScript({
      speed,
      lifetime,
      damage,
      destroyOnHit: destroyOnHit ? 'true' : 'false'
    });

    const scriptPath = path.join(scriptsPath, 'Projectile.cs');
    await fs.writeFile(scriptPath, script);
    await this.createMetaFile(scriptPath);

    // Create projectile prefab
    await this.createProjectilePrefab(projectPath);

    return {
      success: true,
      scriptsGenerated: ['Projectile.cs'],
      location: scriptsPath,
      message: 'Projectile system setup completed successfully'
    };
  }

  private async generateProjectileScript(params: {
    speed: number;
    lifetime: number;
    damage: number;
    destroyOnHit: string;
  }) {
    const templatePath = path.join(this.templatesDir, 'Projectile.cs.hbs');
    const templateContent = await fs.readFile(templatePath, 'utf-8');
    const template = Handlebars.compile(templateContent);
    return template(params);
  }

  private async createMetaFile(filePath: string) {
    const guid = this.generateGUID();
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
    await fs.writeFile(`${filePath}.meta`, metaContent);
  }

  private generateGUID(): string {
    return 'xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx'.replace(/x/g, () => {
      return Math.floor(Math.random() * 16).toString(16);
    });
  }

  private async createProjectilePrefab(projectPath: string) {
    const prefabPath = path.join(projectPath, 'Assets/Prefabs/Weapons');
    await fs.ensureDir(prefabPath);

    const prefabContent = `%YAML 1.1
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
  m_Layer: 0
  m_Name: Projectile
  m_TagString: Projectile
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
  m_LocalScale: {x: 0.2, y: 0.5, z: 1}
  m_Children: []
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
  m_Color: {r: 1, g: 1, b: 0, a: 1}
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
  m_CollisionDetection: 1
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
  m_IsTrigger: 1
  m_UsedByEffector: 0
  m_UsedByComposite: 0
  m_Offset: {x: 0, y: 0}
  serializedVersion: 2
  m_Radius: 0.15
--- !u!114 &114
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: ${this.generateGUID()}, type: 3}
  m_Name:
  m_EditorClassIdentifier:
`;

    const prefabFilePath = path.join(prefabPath, 'Projectile.prefab');
    await fs.writeFile(prefabFilePath, prefabContent);
    await this.createMetaFile(prefabFilePath);
  }
}
