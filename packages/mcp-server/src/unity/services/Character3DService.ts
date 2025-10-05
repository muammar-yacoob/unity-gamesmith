import * as fs from 'fs-extra';
import * as path from 'path';
import Handlebars from 'handlebars';
import { SketchfabService } from './SketchfabService.js';

export interface Character3DSetupParams {
  projectPath: string;
  searchQuery?: string;
  sketchfabApiToken?: string;
  moveSpeed?: number;
  rotationSpeed?: number;
  jumpHeight?: number;
  cameraDistance?: number;
  cameraHeight?: number;
  followSpeed?: number;
}

export class Character3DService {
  private templatesDir = path.join(__dirname, '../templates');
  private sketchfabService = new SketchfabService();

  async setup3DCharacter(params: Character3DSetupParams) {
    const {
      projectPath,
      searchQuery = 'character walk',
      sketchfabApiToken,
      moveSpeed = 5.0,
      rotationSpeed = 10.0,
      jumpHeight = 1.5,
      cameraDistance = 8.0,
      cameraHeight = 5.0,
      followSpeed = 1.0
    } = params;

    const scriptsPath = path.join(projectPath, 'Assets/Scripts/Character3D');
    await fs.ensureDir(scriptsPath);

    const modelsPath = path.join(projectPath, 'Assets/Models/Characters');
    await fs.ensureDir(modelsPath);

    // Search for character model on Sketchfab
    let characterModel = null;
    try {
      const models = await this.sketchfabService.searchCharacters({
        query: searchQuery,
        rigged: true,
        animated: true,
        downloadable: true,
        maxResults: 1
      });

      if (models.length > 0) {
        characterModel = models[0];
      }
    } catch (error) {
      console.warn('Failed to search Sketchfab:', error);
    }

    // Generate controller scripts
    const scripts = await this.generateScripts({
      moveSpeed: moveSpeed.toString(),
      rotationSpeed: rotationSpeed.toString(),
      jumpHeight: jumpHeight.toString(),
      cameraDistance: cameraDistance.toString(),
      cameraHeight: cameraHeight.toString(),
      followSpeed: followSpeed.toString()
    });

    const scriptFiles = [
      { name: 'CharacterController3D.cs', content: scripts.controller },
      { name: 'CinemachineSetup.cs', content: scripts.cinemachine }
    ];

    for (const file of scriptFiles) {
      const filePath = path.join(scriptsPath, file.name);
      await fs.writeFile(filePath, file.content);
      await this.createMetaFile(filePath);
    }

    // Download model if API token provided
    let modelDownloaded = false;
    let modelPath = '';
    if (characterModel && sketchfabApiToken) {
      try {
        modelPath = path.join(modelsPath, `${characterModel.uid}.glb`);
        await this.sketchfabService.downloadModel(
          characterModel.uid,
          modelPath,
          sketchfabApiToken
        );
        await this.createMetaFile(modelPath);
        modelDownloaded = true;
      } catch (error) {
        console.warn('Failed to download model:', error);
      }
    }

    // Create README with setup instructions
    await this.createSetupInstructions(projectPath, characterModel, modelDownloaded);

    return {
      success: true,
      scriptsGenerated: scriptFiles.map(f => f.name),
      scriptsLocation: scriptsPath,
      characterModel: characterModel ? {
        name: characterModel.name,
        author: characterModel.author,
        license: characterModel.license,
        animationCount: characterModel.animationCount,
        downloaded: modelDownloaded,
        modelPath: modelDownloaded ? modelPath : null
      } : null,
      message: modelDownloaded
        ? '3D character system setup with downloaded model'
        : '3D character system setup (model download requires Sketchfab API token)',
      nextSteps: [
        'Install Cinemachine via Package Manager (Window > Package Manager > Unity Registry > Cinemachine)',
        characterModel ? `Import ${characterModel.name} model into Unity` : 'Import your 3D character model',
        'Create CharacterController3D GameObject and attach CharacterController3D.cs script',
        'Create Virtual Camera GameObject and attach CinemachineSetup.cs script',
        'Configure Animator with walk animation clip',
        'Set up Input Manager for WASD and Jump keys'
      ]
    };
  }

  private async generateScripts(params: {
    moveSpeed: string;
    rotationSpeed: string;
    jumpHeight: string;
    cameraDistance: string;
    cameraHeight: string;
    followSpeed: string;
  }) {
    const templates = {
      controller: await fs.readFile(
        path.join(this.templatesDir, 'CharacterController3D.cs.hbs'),
        'utf-8'
      ),
      cinemachine: await fs.readFile(
        path.join(this.templatesDir, 'CinemachineSetup.cs.hbs'),
        'utf-8'
      )
    };

    return {
      controller: Handlebars.compile(templates.controller)(params),
      cinemachine: Handlebars.compile(templates.cinemachine)(params)
    };
  }

  private async createSetupInstructions(
    projectPath: string,
    characterModel: any,
    modelDownloaded: boolean
  ) {
    const readme = `# 3D Character Setup Instructions

## Recommended Character Model

${characterModel ? `
**${characterModel.name}** by ${characterModel.author}
- License: ${characterModel.license}
- Animations: ${characterModel.animationCount}
- Status: ${modelDownloaded ? '✅ Downloaded to Assets/Models/Characters' : '⚠️ Manual download required'}
${!modelDownloaded ? `
### Download Instructions
1. Get a free Sketchfab API token from https://sketchfab.com/settings/password
2. Re-run the import_3d_character tool with the sketchfabApiToken parameter
` : ''}
` : `
⚠️ No model auto-selected. Search Sketchfab at https://sketchfab.com for a rigged, animated character.
`}

## Unity Setup Steps

### 1. Install Cinemachine
1. Open Unity Package Manager (Window > Package Manager)
2. Select "Unity Registry" from dropdown
3. Find and install "Cinemachine"

### 2. Setup Character GameObject
1. Import your 3D character model (if not auto-downloaded)
2. Drag model into scene hierarchy
3. Add CharacterController component (Component > Physics > Character Controller)
4. Add Animator component (Component > Miscellaneous > Animator)
5. Attach CharacterController3D.cs script
6. Tag GameObject as "Player"

### 3. Setup Animator
1. Create Animator Controller (Create > Animator Controller)
2. Add walk animation clip to controller
3. Create "Speed" float parameter
4. Create "IsGrounded" bool parameter
5. Create "Jump" trigger parameter
6. Assign Animator Controller to character's Animator component

### 4. Setup Cinemachine Camera
1. Create empty GameObject named "Virtual Camera"
2. Attach CinemachineSetup.cs script
3. Assign player character to Follow Target field (or use Player tag)
4. The script will auto-configure camera following

### 5. Input Configuration
Ensure Input Manager has these axes configured (should be default):
- Horizontal (A/D or Left/Right arrows)
- Vertical (W/S or Up/Down arrows)
- Jump (Space)

## Testing
1. Enter Play mode
2. Use WASD to move character
3. Press Space to jump
4. Camera should smoothly follow the character

## Troubleshooting
- **Character not moving:** Check CharacterController is attached and enabled
- **Camera not following:** Ensure player is tagged as "Player" or Follow Target is assigned
- **No animation:** Configure Animator Controller with Speed parameter
- **Cinemachine errors:** Install Cinemachine package via Package Manager
`;

    const readmePath = path.join(projectPath, 'Assets/Scripts/Character3D/README.md');
    await fs.writeFile(readmePath, readme);
    await this.createMetaFile(readmePath);
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
}
