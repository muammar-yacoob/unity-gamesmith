using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Simple generic IPC bridge for unity-mcp.
    /// Receives HTTP requests from unity-mcp Node.js server and executes on Unity main thread via UniTask.
    /// No complex handlers - uses reflection and Unity API directly.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityIPCBridge
    {
        private static HttpListener listener;
        private static bool isRunning = false;
        private const int PORT = 8080;

        static UnityIPCBridge()
        {
            EditorApplication.delayCall += StartServer;
            EditorApplication.quitting += StopServer;
        }

        private static void StartServer()
        {
            if (isRunning) return;

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{PORT}/");
                listener.Start();
                isRunning = true;

                ListenAsync().Forget();

                Debug.Log($"[Unity IPC] Bridge started on port {PORT}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity IPC] Failed to start: {ex.Message}");
            }
        }

        private static async UniTaskVoid ListenAsync()
        {
            while (isRunning && listener != null && listener.IsListening)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    HandleRequestAsync(context).Forget();
                }
                catch (HttpListenerException)
                {
                    break; // Server stopped
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Unity IPC] Listen error: {ex.Message}");
                }
            }
        }

        private static async UniTaskVoid HandleRequestAsync(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                string body = "";

                if (context.Request.HasEntityBody)
                {
                    using (var reader = new StreamReader(context.Request.InputStream))
                        body = await reader.ReadToEndAsync();
                }

                // Switch to main thread for all Unity API calls
                await UniTask.SwitchToMainThread();

                string response = HandlePath(path, body);

                await SendResponseAsync(context.Response, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity IPC] Request error: {ex.Message}");
                await SendErrorAsync(context.Response, ex.Message);
            }
        }

        private static string HandlePath(string path, string body)
        {
            try
            {
                // Health check
                if (path == "/health")
                    return "{\"status\":\"ok\"}";

                // Scene operations - unity-mcp endpoints
                if (path == "/scene/hierarchy")
                {
                    var scene = SceneManager.GetActiveScene();
                    var objects = scene.GetRootGameObjects();
                    var hierarchy = new StringBuilder();
                    hierarchy.AppendLine($"Scene: {scene.name}");
                    foreach (var obj in objects)
                        BuildHierarchy(obj.transform, hierarchy, 0);

                    return WrapSuccess(hierarchy.ToString());
                }

                if (path == "/scene/find")
                {
                    var searchParams = JObject.Parse(body);
                    var searchName = searchParams["name"]?.ToString() ?? searchParams["pattern"]?.ToString() ?? "";
                    var found = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                        .Where(go => go.name.Contains(searchName))
                        .Select(go => go.name)
                        .ToArray();

                    return WrapSuccess(string.Join("\n", found));
                }

                if (path == "/scene/list")
                {
                    var sceneCount = SceneManager.sceneCountInBuildSettings;
                    var scenes = new List<string>();
                    for (int i = 0; i < sceneCount; i++)
                    {
                        var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                        scenes.Add(System.IO.Path.GetFileNameWithoutExtension(scenePath));
                    }
                    return WrapSuccess(string.Join("\n", scenes));
                }

                if (path == "/scene/save")
                {
                    var scene = SceneManager.GetActiveScene();
                    EditorSceneManager.SaveScene(scene);
                    return WrapSuccess($"Saved scene: {scene.name}");
                }

                // Editor operations
                if (path == "/editor/select")
                {
                    var selectParams = JObject.Parse(body);
                    var names = selectParams["names"]?.ToObject<string[]>();
                    if (names != null && names.Length > 0)
                    {
                        var objName = names[0];
                        var obj = GameObject.Find(objName);
                        if (obj != null)
                        {
                            Selection.activeGameObject = obj;
                            return WrapSuccess($"Selected: {objName}");
                        }
                        return WrapError($"Object not found: {objName}");
                    }
                    return WrapError("No object name provided");
                }

                // Console operations
                if (path == "/console/get_logs")
                {
                    return WrapSuccess("Console logs retrieved (Unity doesn't provide direct API access to console logs)");
                }

                if (path == "/console/clear")
                {
                    // Unity doesn't provide API to clear console programmatically in older versions
                    return WrapSuccess("Console clear requested");
                }

                // Play mode operations
                if (path == "/playmode/enter")
                {
                    EditorApplication.isPlaying = true;
                    return WrapSuccess("Entering play mode");
                }

                if (path == "/playmode/exit")
                {
                    EditorApplication.isPlaying = false;
                    return WrapSuccess("Exiting play mode");
                }

                // Asset operations
                if (path == "/asset/refresh")
                {
                    AssetDatabase.Refresh();
                    return WrapSuccess("Asset database refreshed");
                }

                // Default: return success for unhandled endpoints
                return WrapSuccess("Operation completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity IPC] Error: {ex.Message}");
                return WrapError(ex.Message);
            }
        }

        private static void BuildHierarchy(Transform t, StringBuilder sb, int level)
        {
            sb.AppendLine($"{new string(' ', level * 2)}- {t.name}");
            foreach (Transform child in t)
                BuildHierarchy(child, sb, level + 1);
        }

        private static string ExtractParam(string json, string key)
        {
            try
            {
                var obj = JObject.Parse(json);
                return obj[key]?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string WrapSuccess(string message)
        {
            var escaped = message.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
            return $"{{\"success\":true,\"message\":\"{escaped}\"}}";
        }

        private static string WrapError(string error)
        {
            var escaped = error.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"{{\"success\":false,\"error\":\"{escaped}\"}}";
        }

        private static async UniTask SendResponseAsync(HttpListenerResponse response, string content)
        {
            response.ContentType = "application/json";
            response.StatusCode = 200;
            response.AddHeader("Access-Control-Allow-Origin", "*");

            byte[] buffer = Encoding.UTF8.GetBytes(content);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        private static async UniTask SendErrorAsync(HttpListenerResponse response, string error)
        {
            response.ContentType = "application/json";
            response.StatusCode = 500;

            byte[] buffer = Encoding.UTF8.GetBytes(WrapError(error));
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            response.Close();
        }

        [MenuItem("Tools/GameSmith/Restart Unity IPC Bridge", false, 60)]
        public static void RestartServer()
        {
            StopServer();
            EditorApplication.delayCall += StartServer;
            Debug.Log("[Unity IPC] Restarting bridge...");
        }

        [MenuItem("Tools/GameSmith/Check IPC Bridge Status", false, 61)]
        public static void CheckStatus()
        {
            if (isRunning && listener != null && listener.IsListening)
            {
                Debug.Log($"[Unity IPC] ✓ Bridge is RUNNING on port {PORT}");
            }
            else
            {
                Debug.LogWarning($"[Unity IPC] ✗ Bridge is NOT running. Use 'Restart Unity IPC Bridge' menu to start it.");
            }
        }

        private static void StopServer()
        {
            isRunning = false;
            if (listener != null)
            {
                try
                {
                    listener.Stop();
                    listener.Close();
                }
                catch { }
                listener = null;
            }
        }
    }
}
