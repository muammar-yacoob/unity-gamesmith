import * as fs from 'fs-extra';
import * as path from 'path';
import Handlebars from 'handlebars';

export interface LevelSetupParams {
  projectPath: string;
  numberOfLevels?: number;
  difficultyMultiplier?: number;
  spawnPoints?: number;
}

export class LevelService {
  private templatesDir = path.join(__dirname, '../templates');

  async setupLevelSystem(params: LevelSetupParams) {
    const {
      projectPath,
      numberOfLevels = 5,
      difficultyMultiplier = 1.5,
      spawnPoints = 4
    } = params;

    const scriptsPath = path.join(projectPath, 'Assets/Scripts/Managers');
    if (!await fs.pathExists(scriptsPath)) {
      throw new Error(`Managers scripts path not found: ${scriptsPath}`);
    }

    // Generate level management scripts
    const scripts = await this.generateLevelScripts({
      numberOfLevels,
      difficultyMultiplier
    });

    const scriptFiles = [
      { name: 'LevelManager.cs', content: scripts.levelManager },
      { name: 'WaveSpawner.cs', content: scripts.waveSpawner },
      { name: 'SpawnPoint.cs', content: scripts.spawnPoint }
    ];

    for (const file of scriptFiles) {
      const filePath = path.join(scriptsPath, file.name);
      await fs.writeFile(filePath, file.content);
      await this.createMetaFile(filePath);
    }

    // Create LevelManager prefab
    await this.createLevelManagerPrefab(projectPath);

    // Create SpawnPoint prefabs
    await this.createSpawnPointPrefabs(projectPath, spawnPoints);

    return {
      success: true,
      scriptsGenerated: scriptFiles.map(f => f.name),
      location: scriptsPath,
      spawnPointsCreated: spawnPoints,
      message: `Level system setup completed with ${numberOfLevels} levels and ${spawnPoints} spawn points`
    };
  }

  private async generateLevelScripts(params: {
    numberOfLevels: number;
    difficultyMultiplier: number;
  }) {
    const templates = {
      levelManager: await fs.readFile(path.join(this.templatesDir, 'LevelManager.cs.hbs'), 'utf-8'),
      waveSpawner: await fs.readFile(path.join(this.templatesDir, 'WaveSpawner.cs.hbs'), 'utf-8'),
      spawnPoint: await fs.readFile(path.join(this.templatesDir, 'SpawnPoint.cs.hbs'), 'utf-8')
    };

    return {
      levelManager: Handlebars.compile(templates.levelManager)(params),
      waveSpawner: Handlebars.compile(templates.waveSpawner)(params),
      spawnPoint: Handlebars.compile(templates.spawnPoint)(params)
    };
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

  private async createLevelManagerPrefab(projectPath: string) {
    const prefabPath = path.join(projectPath, 'Assets/Prefabs');
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
  - component: {fileID: 114}
  - component: {fileID: 115}
  m_Layer: 0
  m_Name: LevelManager
  m_TagString: GameController
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
  m_Children: []
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
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
--- !u!114 &115
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

    const prefabFilePath = path.join(prefabPath, 'LevelManager.prefab');
    await fs.writeFile(prefabFilePath, prefabContent);
    await this.createMetaFile(prefabFilePath);
  }

  private async createSpawnPointPrefabs(projectPath: string, count: number) {
    const prefabPath = path.join(projectPath, 'Assets/Prefabs');
    await fs.ensureDir(prefabPath);

    for (let i = 1; i <= count; i++) {
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
  - component: {fileID: 114}
  m_Layer: 0
  m_Name: SpawnPoint${i}
  m_TagString: SpawnPoint
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
  m_Children: []
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
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

      const prefabFilePath = path.join(prefabPath, `SpawnPoint${i}.prefab`);
      await fs.writeFile(prefabFilePath, prefabContent);
      await this.createMetaFile(prefabFilePath);
    }
  }
}
