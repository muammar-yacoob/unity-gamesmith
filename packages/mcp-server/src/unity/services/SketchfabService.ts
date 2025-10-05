import * as fs from 'fs-extra';
import * as path from 'path';
import https from 'https';

export interface SketchfabSearchParams {
  query: string;
  rigged?: boolean;
  animated?: boolean;
  downloadable?: boolean;
  maxResults?: number;
}

export interface SketchfabModel {
  uid: string;
  name: string;
  description: string;
  downloadUrl?: string;
  thumbnailUrl?: string;
  faceCount: number;
  vertexCount: number;
  animationCount: number;
  license: string;
  author: string;
  archives: {
    glb?: { size: number; textureCount: number };
    gltf?: { size: number; textureCount: number };
  };
}

export class SketchfabService {
  private readonly apiBase = 'https://api.sketchfab.com/v3';

  /**
   * Search for 3D character models on Sketchfab
   */
  async searchCharacters(params: SketchfabSearchParams): Promise<SketchfabModel[]> {
    const {
      query,
      rigged = true,
      animated = true,
      downloadable = true,
      maxResults = 5
    } = params;

    const searchUrl = new URL(`${this.apiBase}/search`);
    searchUrl.searchParams.append('type', 'models');
    searchUrl.searchParams.append('q', query);
    searchUrl.searchParams.append('count', maxResults.toString());

    if (rigged) searchUrl.searchParams.append('rigged', 'true');
    if (animated) searchUrl.searchParams.append('animated', 'true');
    if (downloadable) searchUrl.searchParams.append('downloadable', 'true');

    const response = await this.fetchJson(searchUrl.toString());

    return response.results.map((result: any) => ({
      uid: result.uid,
      name: result.name,
      description: result.description || '',
      thumbnailUrl: result.thumbnails?.images?.[2]?.url || '',
      faceCount: result.faceCount || 0,
      vertexCount: result.vertexCount || 0,
      animationCount: result.animationCount || 0,
      license: result.license?.label || 'Unknown',
      author: result.user?.displayName || result.user?.username || 'Unknown',
      archives: {
        glb: result.archives?.glb ? {
          size: result.archives.glb.size,
          textureCount: result.archives.glb.textureCount
        } : undefined,
        gltf: result.archives?.gltf ? {
          size: result.archives.gltf.size,
          textureCount: result.archives.gltf.textureCount
        } : undefined
      }
    }));
  }

  /**
   * Download a Sketchfab model in GLB format
   * Note: Requires Sketchfab API token for actual downloads
   */
  async downloadModel(modelUid: string, outputPath: string, apiToken?: string): Promise<string> {
    // Get download URL (requires authentication)
    if (!apiToken) {
      throw new Error('Sketchfab API token required for downloads. Get one at https://sketchfab.com/settings/password');
    }

    const downloadUrl = `${this.apiBase}/models/${modelUid}/download`;

    const response = await this.fetchJson(downloadUrl, {
      headers: {
        'Authorization': `Token ${apiToken}`
      }
    });

    // Download GLB file
    const glbUrl = response.gltf?.url || response.glb?.url;
    if (!glbUrl) {
      throw new Error('No GLB/glTF download available for this model');
    }

    await fs.ensureDir(path.dirname(outputPath));
    await this.downloadFile(glbUrl, outputPath);

    return outputPath;
  }

  private async fetchJson(url: string, options?: { headers?: Record<string, string> }): Promise<any> {
    return new Promise((resolve, reject) => {
      https.get(url, { headers: options?.headers }, (res) => {
        let data = '';
        res.on('data', (chunk) => data += chunk);
        res.on('end', () => {
          try {
            resolve(JSON.parse(data));
          } catch (error) {
            reject(new Error(`Failed to parse JSON response: ${error}`));
          }
        });
      }).on('error', reject);
    });
  }

  private async downloadFile(url: string, outputPath: string): Promise<void> {
    return new Promise((resolve, reject) => {
      const file = fs.createWriteStream(outputPath);
      https.get(url, (response) => {
        response.pipe(file);
        file.on('finish', () => {
          file.close();
          resolve();
        });
      }).on('error', (err) => {
        fs.unlink(outputPath);
        reject(err);
      });
    });
  }
}
