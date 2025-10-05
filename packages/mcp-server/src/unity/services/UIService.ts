import * as fs from 'fs-extra';
import * as path from 'path';
import Handlebars from 'handlebars';

export interface UISetupParams {
  projectPath: string;
  includeHealthBar?: boolean;
  includeScoreDisplay?: boolean;
  includeMinimap?: boolean;
}

export class UIService {
  private templatesDir = path.join(__dirname, '../templates');

  async setupUI(params: UISetupParams) {
    const {
      projectPath,
      includeHealthBar = true,
      includeScoreDisplay = true,
      includeMinimap = false
    } = params;

    const scriptsPath = path.join(projectPath, 'Assets/Scripts/UI');
    if (!await fs.pathExists(scriptsPath)) {
      throw new Error(`UI scripts path not found: ${scriptsPath}`);
    }

    // Generate UI scripts
    const scripts = await this.generateUIScripts({
      includeHealthBar: includeHealthBar ? 'true' : 'false',
      includeScoreDisplay: includeScoreDisplay ? 'true' : 'false'
    });

    const scriptFiles = [
      { name: 'UIManager.cs', content: scripts.uiManager },
      { name: 'HealthBarUI.cs', content: scripts.healthBar },
      { name: 'PauseMenu.cs', content: scripts.pauseMenu },
      { name: 'GameOverScreen.cs', content: scripts.gameOverScreen }
    ];

    for (const file of scriptFiles) {
      const filePath = path.join(scriptsPath, file.name);
      await fs.writeFile(filePath, file.content);
      await this.createMetaFile(filePath);
    }

    // Create UI prefabs
    await this.createUICanvasPrefab(projectPath);

    return {
      success: true,
      scriptsGenerated: scriptFiles.map(f => f.name),
      location: scriptsPath,
      features: {
        healthBar: includeHealthBar,
        scoreDisplay: includeScoreDisplay,
        minimap: includeMinimap
      },
      message: 'UI system setup completed successfully'
    };
  }

  private async generateUIScripts(params: {
    includeHealthBar: string;
    includeScoreDisplay: string;
  }) {
    const templates = {
      uiManager: await fs.readFile(path.join(this.templatesDir, 'UIManager.cs.hbs'), 'utf-8'),
      healthBar: await fs.readFile(path.join(this.templatesDir, 'HealthBarUI.cs.hbs'), 'utf-8'),
      pauseMenu: await fs.readFile(path.join(this.templatesDir, 'PauseMenu.cs.hbs'), 'utf-8'),
      gameOverScreen: await fs.readFile(path.join(this.templatesDir, 'GameOverScreen.cs.hbs'), 'utf-8')
    };

    return {
      uiManager: Handlebars.compile(templates.uiManager)(params),
      healthBar: Handlebars.compile(templates.healthBar)(params),
      pauseMenu: Handlebars.compile(templates.pauseMenu)(params),
      gameOverScreen: Handlebars.compile(templates.gameOverScreen)(params)
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

  private async createUICanvasPrefab(projectPath: string) {
    const prefabPath = path.join(projectPath, 'Assets/Prefabs/UI');
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
  - component: {fileID: 224}
  - component: {fileID: 223}
  - component: {fileID: 114}
  - component: {fileID: 225}
  m_Layer: 5
  m_Name: UICanvas
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &224
RectTransform:
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
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 0, y: 0}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0, y: 0}
--- !u!223 &223
Canvas:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  serializedVersion: 3
  m_RenderMode: 0
  m_Camera: {fileID: 0}
  m_PlaneDistance: 100
  m_PixelPerfect: 0
  m_ReceivesEvents: 1
  m_OverrideSorting: 0
  m_OverridePixelPerfect: 0
  m_SortingBucketNormalizedSize: 0
  m_VertexColorAlwaysGammaSpace: 0
  m_AdditionalShaderChannelsFlag: 0
  m_UpdateRectTransformForStandalone: 0
  m_SortingLayerID: 0
  m_SortingOrder: 0
  m_TargetDisplay: 0
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
--- !u!225 &225
CanvasScaler:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1}
  m_Enabled: 1
  m_UiScaleMode: 1
  m_ReferencePixelsPerUnit: 100
  m_ScaleFactor: 1
  m_ReferenceResolution: {x: 1920, y: 1080}
  m_ScreenMatchMode: 0
  m_MatchWidthOrHeight: 0
  m_PhysicalUnit: 3
  m_FallbackScreenDPI: 96
  m_DefaultSpriteDPI: 96
  m_DynamicPixelsPerUnit: 1
  m_PresetInfoIsWorld: 0
`;

    const prefabFilePath = path.join(prefabPath, 'UICanvas.prefab');
    await fs.writeFile(prefabFilePath, prefabContent);
    await this.createMetaFile(prefabFilePath);
  }
}
