using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SparkGames.UnityGameSmith.Editor
{
    /// <summary>
    /// Minimal HTTP bridge exposing Unity Editor APIs to unity-mcp server.
    /// NO orchestration, NO tool logic - only raw Unity API wrappers.
    /// unity-mcp server handles ALL tool orchestration by calling these primitives.
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

                Debug.Log($"[Unity IPC] Bridge started on port {PORT} - Exposing Unity API primitives");
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
                    break;
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

                // Switch to main thread for Unity operations
                await UniTask.SwitchToMainThread();

                // Route to appropriate Unity API wrapper
                string response = await ExecuteUnityAPI(path, body);

                await SendResponseAsync(context.Response, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Unity IPC] Request error: {ex.Message}");
                await SendErrorAsync(context.Response, ex.Message);
            }
        }

        private static async UniTask<string> ExecuteUnityAPI(string path, string body)
        {
            try
            {
                JObject data = string.IsNullOrEmpty(body) ? new JObject() : JObject.Parse(body);

                // Route to minimal Unity API wrappers
                // Supports both /unity/* and legacy unity-mcp endpoints
                switch (path)
                {
                    // Hierarchy endpoints
                    case "/unity/hierarchy":
                    case "/scene/hierarchy":
                        return GetHierarchy();

                    // Scene find endpoint
                    case "/scene/find":
                        return FindAllGameObjects(data);

                    // Selection endpoints
                    case "/unity/selection/get":
                        return GetSelection();

                    case "/unity/selection/set":
                    case "/editor/select":
                        return SetSelection(data);

                    // GameObject endpoints
                    case "/unity/gameobject/find":
                        return FindGameObject(data);

                    case "/unity/gameobject/find-all":
                        return FindAllGameObjects(data);

                    case "/unity/gameobject/create-primitive":
                    case "/gameobject/create":
                        return CreatePrimitive(data);

                    case "/unity/gameobject/destroy":
                    case "/editor/delete":
                        return DestroySelectedObjects();

                    // Transform endpoints
                    case "/unity/transform/get":
                    case "/editor/transform":
                        return GetTransform(data);

                    case "/unity/transform/set-position":
                    case "/editor/move":
                        return SetPosition(data);

                    case "/unity/transform/set-rotation":
                    case "/editor/rotate":
                        return SetRotation(data);

                    case "/unity/transform/set-scale":
                    case "/editor/scale":
                        return SetScale(data);

                    // Scene endpoints
                    case "/unity/scene/save":
                    case "/scene/save":
                        return SaveScene();

                    case "/unity/scene/mark-dirty":
                    case "/scene/dirty":
                        return MarkSceneDirty();

                    // Menu execution
                    case "/unity/menu/execute":
                    case "/editor/menu":
                    case "/advanced/execute_menu":
                        return ExecuteMenuItem(data);

                    // Material/Renderer operations
                    case "/unity/material/set-color":
                    case "/renderer/color":
                        return SetMaterialColor(data);

                    // Advanced/unsupported operations - return helpful errors
                    case "/advanced/create_script":
                        return Error("Script creation not supported. Use direct Unity APIs instead. For colors: use unity_set_material_color");

                    case "/project/get_assets":
                        return Error("Asset listing not implemented. Use Unity Project window to manage assets");

                    default:
                        return Error($"Unknown endpoint: {path}");
                }
            }
            catch (Exception ex)
            {
                return Error($"API execution failed: {ex.Message}");
            }
        }

        // === MINIMAL UNITY API WRAPPERS ===

        private static string GetHierarchy()
        {
            var scene = SceneManager.GetActiveScene();
            var rootObjects = scene.GetRootGameObjects();

            var hierarchy = new JArray();
            foreach (var root in rootObjects)
            {
                hierarchy.Add(BuildHierarchyNode(root.transform));
            }

            return Success(new JObject { ["hierarchy"] = hierarchy });
        }

        private static JObject BuildHierarchyNode(Transform t)
        {
            var node = new JObject
            {
                ["name"] = t.name,
                ["instanceID"] = t.gameObject.GetInstanceID(),
                ["children"] = new JArray()
            };

            foreach (Transform child in t)
            {
                ((JArray)node["children"]).Add(BuildHierarchyNode(child));
            }

            return node;
        }

        private static string GetSelection()
        {
            var selected = Selection.gameObjects;
            var names = new JArray(selected.Select(go => go.name));
            return Success(new JObject { ["selection"] = names });
        }

        private static string SetSelection(JObject data)
        {
            var objects = new List<GameObject>();

            // Support both "names" array and "pattern" string parameters
            var names = data["names"]?.ToObject<string[]>();
            var pattern = data["pattern"]?.ToString();

            if (names != null && names.Length > 0)
            {
                // Select by specific names
                foreach (var name in names)
                {
                    var obj = GameObject.Find(name);
                    if (obj != null) objects.Add(obj);
                }
            }
            else if (!string.IsNullOrEmpty(pattern))
            {
                // Select by pattern matching
                var allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        objects.Add(obj);
                    }
                }
            }

            Selection.objects = objects.ToArray();
            return Success(new JObject { ["count"] = objects.Count, ["selected"] = new JArray(objects.Select(o => o.name)) });
        }

        private static string FindGameObject(JObject data)
        {
            var name = data["name"]?.ToString();
            if (string.IsNullOrEmpty(name))
                return Error("Name required");

            var obj = GameObject.Find(name);
            if (obj == null)
                return Error($"GameObject '{name}' not found");

            return Success(new JObject
            {
                ["name"] = obj.name,
                ["instanceID"] = obj.GetInstanceID()
            });
        }

        private static string FindAllGameObjects(JObject data)
        {
            var tag = data["tag"]?.ToString();
            GameObject[] objects;

            if (!string.IsNullOrEmpty(tag))
            {
                objects = GameObject.FindGameObjectsWithTag(tag);
            }
            else
            {
                objects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            }

            var results = new JArray();
            foreach (var obj in objects)
            {
                results.Add(new JObject
                {
                    ["name"] = obj.name,
                    ["instanceID"] = obj.GetInstanceID()
                });
            }

            return Success(new JObject { ["objects"] = results });
        }

        private static string DestroyGameObject(JObject data)
        {
            var name = data["name"]?.ToString();
            if (string.IsNullOrEmpty(name))
                return Error("Name required");

            var obj = GameObject.Find(name);
            if (obj == null)
                return Error($"GameObject '{name}' not found");

            GameObject.DestroyImmediate(obj);
            return Success(new JObject { ["destroyed"] = name });
        }

        private static string DestroySelectedObjects()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
                return Error("No objects selected");

            var destroyed = new JArray();
            foreach (var obj in selected)
            {
                destroyed.Add(obj.name);
                GameObject.DestroyImmediate(obj);
            }

            return Success(new JObject { ["destroyed"] = destroyed, ["count"] = destroyed.Count });
        }

        private static string CreatePrimitive(JObject data)
        {
            // Get primitive type (default to Cube)
            var typeStr = data["type"]?.ToString() ?? data["primitiveType"]?.ToString() ?? "Cube";
            var name = data["name"]?.ToString();
            var posArray = data["position"]?.ToObject<float[]>();

            // Parse primitive type
            PrimitiveType primitiveType;
            if (!System.Enum.TryParse(typeStr, true, out primitiveType))
            {
                return Error($"Invalid primitive type: {typeStr}. Valid types: Cube, Sphere, Cylinder, Capsule, Plane, Quad");
            }

            // Create primitive
            var obj = GameObject.CreatePrimitive(primitiveType);

            // Set name if provided
            if (!string.IsNullOrEmpty(name))
            {
                obj.name = name;
            }

            // Set position if provided
            if (posArray != null && posArray.Length == 3)
            {
                obj.transform.position = new Vector3(posArray[0], posArray[1], posArray[2]);
            }

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            return Success(new JObject
            {
                ["name"] = obj.name,
                ["instanceID"] = obj.GetInstanceID(),
                ["type"] = primitiveType.ToString(),
                ["position"] = new JArray(obj.transform.position.x, obj.transform.position.y, obj.transform.position.z)
            });
        }

        private static string GetTransform(JObject data)
        {
            var name = data["name"]?.ToString();
            if (string.IsNullOrEmpty(name))
                return Error("Name required");

            var obj = GameObject.Find(name);
            if (obj == null)
                return Error($"GameObject '{name}' not found");

            var t = obj.transform;
            return Success(new JObject
            {
                ["position"] = new JArray(t.position.x, t.position.y, t.position.z),
                ["rotation"] = new JArray(t.eulerAngles.x, t.eulerAngles.y, t.eulerAngles.z),
                ["scale"] = new JArray(t.localScale.x, t.localScale.y, t.localScale.z)
            });
        }

        private static string SetPosition(JObject data)
        {
            var pos = data["position"]?.ToObject<float[]>();
            if (pos == null || pos.Length != 3)
                return Error("position[x,y,z] required");

            var name = data["name"]?.ToString();
            GameObject[] targets;

            if (!string.IsNullOrEmpty(name))
            {
                // Move specific object by name
                var obj = GameObject.Find(name);
                if (obj == null)
                    return Error($"GameObject '{name}' not found");
                targets = new[] { obj };
            }
            else
            {
                // Move selected objects
                targets = Selection.gameObjects;
                if (targets.Length == 0)
                    return Error("No objects selected");
            }

            var updated = new JArray();
            foreach (var obj in targets)
            {
                obj.transform.position = new Vector3(pos[0], pos[1], pos[2]);
                updated.Add(obj.name);
            }

            return Success(new JObject { ["updated"] = updated, ["count"] = updated.Count });
        }

        private static string SetRotation(JObject data)
        {
            var rot = data["rotation"]?.ToObject<float[]>();
            if (rot == null || rot.Length != 3)
                return Error("rotation[x,y,z] required");

            var name = data["name"]?.ToString();
            GameObject[] targets;

            if (!string.IsNullOrEmpty(name))
            {
                // Rotate specific object by name
                var obj = GameObject.Find(name);
                if (obj == null)
                    return Error($"GameObject '{name}' not found");
                targets = new[] { obj };
            }
            else
            {
                // Rotate selected objects
                targets = Selection.gameObjects;
                if (targets.Length == 0)
                    return Error("No objects selected");
            }

            var updated = new JArray();
            foreach (var obj in targets)
            {
                obj.transform.eulerAngles = new Vector3(rot[0], rot[1], rot[2]);
                updated.Add(obj.name);
            }

            return Success(new JObject { ["updated"] = updated, ["count"] = updated.Count });
        }

        private static string SetScale(JObject data)
        {
            var scale = data["scale"]?.ToObject<float[]>();
            if (scale == null || scale.Length != 3)
                return Error("scale[x,y,z] required");

            var name = data["name"]?.ToString();
            GameObject[] targets;

            if (!string.IsNullOrEmpty(name))
            {
                // Scale specific object by name
                var obj = GameObject.Find(name);
                if (obj == null)
                    return Error($"GameObject '{name}' not found");
                targets = new[] { obj };
            }
            else
            {
                // Scale selected objects
                targets = Selection.gameObjects;
                if (targets.Length == 0)
                    return Error("No objects selected");
            }

            var updated = new JArray();
            foreach (var obj in targets)
            {
                obj.transform.localScale = new Vector3(scale[0], scale[1], scale[2]);
                updated.Add(obj.name);
            }

            return Success(new JObject { ["updated"] = updated, ["count"] = updated.Count });
        }

        private static string SaveScene()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            return Success(new JObject { ["saved"] = scene.name });
        }

        private static string MarkSceneDirty()
        {
            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            return Success(new JObject { ["marked"] = scene.name });
        }

        private static string ExecuteMenuItem(JObject data)
        {
            var menuPath = data["menuPath"]?.ToString() ?? data["path"]?.ToString() ?? data["menuItem"]?.ToString();
            if (string.IsNullOrEmpty(menuPath))
                return Error("menuPath or menuItem required");

            // Execute the menu item
            bool success = EditorApplication.ExecuteMenuItem(menuPath);

            if (!success)
                return Error($"Failed to execute menu item: {menuPath}. Menu item may not exist.");

            // Mark scene dirty since menu items often modify the scene
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            return Success(new JObject { ["executed"] = menuPath });
        }

        private static string SetMaterialColor(JObject data)
        {
            var name = data["name"]?.ToString();
            GameObject[] targets;

            if (!string.IsNullOrEmpty(name))
            {
                var obj = GameObject.Find(name);
                if (obj == null)
                    return Error($"GameObject '{name}' not found");
                targets = new[] { obj };
            }
            else
            {
                targets = Selection.gameObjects;
                if (targets.Length == 0)
                    return Error("No objects selected");
            }

            // Parse color - support both color name and RGB array
            Color color;
            var colorName = data["color"]?.ToString();
            var colorArray = data["color"]?.ToObject<float[]>();

            if (!string.IsNullOrEmpty(colorName))
            {
                // Parse color by name
                switch (colorName.ToLower())
                {
                    case "red": color = Color.red; break;
                    case "green": color = Color.green; break;
                    case "blue": color = Color.blue; break;
                    case "yellow": color = Color.yellow; break;
                    case "cyan": color = Color.cyan; break;
                    case "magenta": color = Color.magenta; break;
                    case "white": color = Color.white; break;
                    case "black": color = Color.black; break;
                    case "gray": case "grey": color = Color.gray; break;
                    default:
                        return Error($"Unknown color: {colorName}. Use: red, green, blue, yellow, cyan, magenta, white, black, gray, or RGB array [r,g,b]");
                }
            }
            else if (colorArray != null && colorArray.Length >= 3)
            {
                color = new Color(colorArray[0], colorArray[1], colorArray[2], colorArray.Length > 3 ? colorArray[3] : 1f);
            }
            else
            {
                return Error("color required (name or RGB array [r,g,b,a])");
            }

            var updated = new JArray();
            foreach (var obj in targets)
            {
                var renderer = obj.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue; // Skip objects without renderer
                }

                // Create new material instance if using shared material
                if (renderer.sharedMaterial != null)
                {
                    renderer.material = new Material(renderer.sharedMaterial);
                }
                else
                {
                    // Create basic material
                    renderer.material = new Material(Shader.Find("Standard"));
                }

                renderer.material.color = color;
                updated.Add(obj.name);
            }

            if (updated.Count == 0)
                return Error("No objects with Renderer component found");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            return Success(new JObject
            {
                ["updated"] = updated,
                ["count"] = updated.Count,
                ["color"] = new JArray(color.r, color.g, color.b, color.a)
            });
        }

        // === HELPERS ===

        private static string Success(JObject data)
        {
            data["success"] = true;
            return data.ToString();
        }

        private static string Error(string error)
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

            byte[] buffer = Encoding.UTF8.GetBytes(Error(error));
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
