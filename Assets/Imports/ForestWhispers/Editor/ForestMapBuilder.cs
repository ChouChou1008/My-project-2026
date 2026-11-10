using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ForestWhispers.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForestWhispers.EditorTools
{
    /// <summary>
    /// 메뉴: Forest Whispers > Build Prefabs And Maps.
    /// Layouts/*.json을 읽어 나무, 건물, 바위, 지형, 폭포를 개별 Prefab으로 만들고,
    /// 지형에는 걸을 수 있는 충돌선(EdgeCollider2D)을 넣은 뒤 맵 씬 6개를 조립한다.
    /// 맵은 원본 구간 | 좌우 반전 구간 | 원본 구간으로 이어져 원래보다 3배 길다.
    /// 지형, 바위, 건물, 나무는 두께 있는 3D 메시(앞면 그림 + 측면/윗면)로 만들고 조명과 그림자를 받는다.
    /// </summary>
    public static class ForestMapBuilder
    {
        [Serializable] private class ItemData
        {
            public string name, kind, sprite, glow, mesh;
            public float x, y, z, w, h, sway, spin, bob, front, back;
            public bool wind, tiles;
            public int order;
            public bool oneway;
            public float[] top;
        }

        [Serializable] private class MeshData
        {
            public float[] v, uv, n, c;
            public int[] t0, t1, t2;
        }

        [Serializable] private class PlaceData
        {
            public string name;
            public float x, y;
            public bool flip;
            public int seg;
        }

        [Serializable] private class PlateData
        {
            public string sprite;
            public float x;
            public bool flip;
        }

        [Serializable] private class MapData
        {
            public string name, haze;
            public int index;
            public float width, height, segment, camDist, viewH, plateZ, killY;
            public bool hasPrev, hasNext;
            public ItemData[] items;
            public PlaceData[] places;
            public PlateData[] plates;
        }

        private const float PixelsPerUnit = 100f;
        private const int PlayerOrder = 10;
        private const string ScenePrefix = "";

        private static string _root;
        private static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private static readonly Color[] LightColors =
        {
            new Color(1f, 0.93f, 0.78f), new Color(0.95f, 1f, 0.82f), new Color(0.75f, 0.95f, 0.9f),
            new Color(0.62f, 0.62f, 1f), new Color(1f, 0.62f, 0.6f), new Color(0.8f, 0.95f, 1f)
        };
        private const float LightIntensity = 0.43f;

        [MenuItem("Forest Whispers/Build Prefabs And Maps")]
        public static void BuildAll()
        {
            _root = FindRoot();
            if (_root == null)
            {
                EditorUtility.DisplayDialog("Forest Whispers", "ForestWhispers 폴더(안에 Layouts 폴더)를 찾지 못했습니다.", "확인");
                return;
            }

            if (Shader.Find("ForestWhispers/Sprite25D") == null)
            {
                EditorUtility.DisplayDialog("Forest Whispers", "ForestWhispers/Shaders 폴더의 셰이더를 찾지 못했습니다. 임포트가 끝난 뒤 다시 실행해 주세요.", "확인");
                return;
            }

            _materials.Clear();
            MapData[] maps = Directory.GetFiles(_root + "Layouts", "*.json")
                .OrderBy(f => f)
                .Select(f => JsonUtility.FromJson<MapData>(File.ReadAllText(f)))
                .ToArray();

            try
            {
                Directory.CreateDirectory(_root + "Prefabs");
                Directory.CreateDirectory(_root + "Materials");
                Directory.CreateDirectory(_root + "Scenes");
                Directory.CreateDirectory(_root + "Generated/Meshes");
                EditorUtility.DisplayProgressBar("Forest Whispers", "스프라이트 설정", 0f);
                ConfigureSprites();

                List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
                for (int i = 0; i < maps.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Forest Whispers", maps[i].name, (i + 1f) / (maps.Length + 1f));
                    Dictionary<string, GameObject> prefabs = BuildPrefabs(maps[i]);
                    string path = BuildScene(maps[i], maps, prefabs);
                    if (!scenes.Any(s => s.path == path))
                    {
                        scenes.Add(new EditorBuildSettingsScene(path, true));
                    }
                }

                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(_root + "Scenes/" + ScenePrefix + maps[0].name + ".unity");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("Forest Whispers: Prefab과 씬을 " + _root + " 에 만들었습니다.");
        }

        private static string FindRoot()
        {
            foreach (string folder in Directory.GetDirectories("Assets", "ForestWhispers", SearchOption.AllDirectories))
            {
                if (!Directory.Exists(Path.Combine(folder, "Layouts")))
                {
                    continue;
                }

                return folder.Replace('\\', '/') + "/";
            }

            return null;
        }

        private static void ConfigureSprites()
        {
            string[] files = Directory.GetFiles(_root + "Sprites", "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg"))
                .Select(f => f.Replace('\\', '/'))
                .ToArray();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in files)
                {
                    ConfigureSprite(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void ConfigureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (path.EndsWith("_n.png") || path.EndsWith("_x.png") || path.EndsWith("_top.jpg") || path.EndsWith("_side.jpg"))
            {
                ConfigureSurfaceTexture(importer, path.EndsWith("_n.png"), path.EndsWith(".jpg"));
                return;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool isReady = importer.textureType == TextureImporterType.Sprite &&
                Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit) &&
                settings.spriteMeshType == SpriteMeshType.FullRect &&
                importer.anisoLevel == 8;
            if (isReady)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = true;
            importer.mipMapBias = -0.4f;
            importer.anisoLevel = 8;
            importer.filterMode = FilterMode.Trilinear;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void ConfigureSurfaceTexture(TextureImporter importer, bool isNormal, bool isTile)
        {
            TextureImporterType type = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            TextureWrapMode wrap = isTile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (importer.textureType == type && importer.maxTextureSize == 4096 && importer.wrapMode == wrap)
            {
                return;
            }

            importer.textureType = type;
            importer.mipmapEnabled = true;
            importer.wrapMode = wrap;
            importer.anisoLevel = 8;
            importer.mipMapBias = -0.4f;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string relativePath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(_root + "Sprites/" + relativePath);
        }

        private static Dictionary<string, GameObject> BuildPrefabs(MapData map)
        {
            var result = new Dictionary<string, GameObject>();
            Color fog = ParseColor(map.haze);
            foreach (ItemData item in map.items)
            {
                Sprite sprite = LoadSprite(item.sprite);
                if (sprite == null)
                {
                    Debug.LogWarning("스프라이트가 없습니다: " + item.sprite);
                    continue;
                }

                string folder = _root + "Prefabs/" + map.name + "/" + GetCategory(item.kind) + "/";
                Directory.CreateDirectory(folder);
                GameObject go = CreateItemObject(item, sprite, fog, map.name);
                result[item.name] = PrefabUtility.SaveAsPrefabAsset(go, folder + item.name + ".prefab");
                UnityEngine.Object.DestroyImmediate(go);
            }

            return result;
        }

        private static string GetCategory(string kind)
        {
            switch (kind)
            {
                case "terrain": return "Terrain";
                case "fall": return "Waterfalls";
                case "back": return "Background";
                case "fg": return "Foreground";
                default: return "Props";
            }
        }

        private static GameObject CreateItemObject(ItemData item, Sprite sprite, Color fogColor, string mapName)
        {
            if (!string.IsNullOrEmpty(item.mesh))
            {
                GameObject meshObject = CreateMeshObject(item, sprite, mapName);
                if (meshObject != null)
                {
                    AddCollider(meshObject, item);
                    AddGlow(meshObject, item);
                    AddMotion(meshObject, item);
                    return meshObject;
                }
            }

            var go = new GameObject(item.name);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = item.order;
            renderer.sharedMaterial = item.kind == "fall" ? GetFallMaterial(fogColor, GetFog(item.z)) : GetSpriteMaterial(fogColor, GetFog(item.z), item.sway);

            AddCollider(go, item);
            AddGlow(go, item);
            AddMotion(go, item);
            return go;
        }

        private static GameObject CreateMeshObject(ItemData item, Sprite sprite, string mapName)
        {
            string jsonPath = _root + "Meshes/" + item.mesh;
            if (!File.Exists(jsonPath))
            {
                return null;
            }

            Mesh mesh = SaveMesh(JsonUtility.FromJson<MeshData>(File.ReadAllText(jsonPath)), mapName, item.name);
            string spritePath = _root + "Sprites/" + item.sprite;
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            var extended = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath.Replace(".png", "_x.png"));
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath.Replace(".png", "_n.png"));

            Texture2D side = extended != null ? extended : albedo;
            Texture2D top = side;
            string key = mapName + "/" + item.name;

            var go = new GameObject(item.name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            Material front = item.wind ? GetWindMaterial(key + "_Front", albedo, normal, item) : GetLitMaterial(key + "_Front", albedo, normal, true, Color.white);
            Material sides = item.wind ? GetWindMaterial(key + "_Side", side, null, item) : GetLitMaterial(key + "_Side", side, null, false, new Color(0.8f, 0.8f, 0.8f));
            Material tops = item.wind ? sides : GetLitMaterial(key + "_Top", top, null, false, new Color(0.95f, 0.95f, 0.95f));
            renderer.sharedMaterials = new[] { front, sides, tops };
            renderer.shadowCastingMode = item.kind == "fg" ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = true;

            return go;
        }

        private static Mesh SaveMesh(MeshData data, string mapName, string name)
        {
            string folder = _root + "Generated/Meshes/" + mapName + "/";
            Directory.CreateDirectory(folder);
            string path = folder + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew)
            {
                mesh = new Mesh();
            }

            mesh.Clear();
            int count = data.v.Length / 3;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(data.v[i * 3], data.v[i * 3 + 1], data.v[i * 3 + 2]);
                normals[i] = new Vector3(data.n[i * 3], data.n[i * 3 + 1], data.n[i * 3 + 2]);
                uvs[i] = new Vector2(data.uv[i * 2], data.uv[i * 2 + 1]);
            }

            mesh.indexFormat = count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            if (data.c != null && data.c.Length == count * 4)
            {
                var colors = new Color[count];
                for (int i = 0; i < count; i++)
                {
                    colors[i] = new Color(data.c[i * 4], data.c[i * 4 + 1], data.c[i * 4 + 2], data.c[i * 4 + 3]);
                }

                mesh.colors = colors;
            }

            mesh.subMeshCount = 3;
            mesh.SetTriangles(data.t0, 0);
            mesh.SetTriangles(data.t1 ?? new int[0], 1);
            mesh.SetTriangles(data.t2 ?? new int[0], 2);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (isNew)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }

            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static bool IsUrp()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            return pipeline != null && pipeline.GetType().Name.Contains("Universal");
        }

        /// <summary>Built-in은 Standard, URP는 URP/Lit. 앞면은 알파 컷아웃으로 그림 윤곽을 살린다.</summary>
        private static Material GetLitMaterial(string key, Texture2D albedo, Texture2D normal, bool isCutout, Color tint)
        {
            bool isUrp = IsUrp();
            string shaderName = isUrp ? "Universal Render Pipeline/Lit" : "Standard";
            return GetMaterial("Lit/" + key, shaderName, m =>
            {
                m.SetTexture(isUrp ? "_BaseMap" : "_MainTex", albedo);
                m.SetColor(isUrp ? "_BaseColor" : "_Color", tint);
                m.SetFloat("_Smoothness", 0.12f);
                m.SetFloat("_Glossiness", 0.12f);
                m.SetFloat("_Metallic", 0f);
                if (normal != null)
                {
                    m.SetTexture("_BumpMap", normal);
                    m.SetFloat("_BumpScale", 0.5f);
                    m.EnableKeyword("_NORMALMAP");
                }

                if (!isCutout)
                {
                    return;
                }

                m.SetFloat("_Cutoff", 0.35f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.AlphaTest;
                if (isUrp)
                {
                    m.SetFloat("_AlphaClip", 1f);
                    return;
                }

                m.SetFloat("_Mode", 1f);
                m.SetOverrideTag("RenderType", "TransparentCutout");
            });
        }

        /// <summary>
        /// 바람 셰이더. 나무와 고사리는 크게, 지형의 늘어진 덩굴과 일반 소품은 잎만 살짝 흔들린다.
        /// 셰이더가 없으면 일반 조명 머티리얼로 대신한다.
        /// </summary>
        private static Material GetWindMaterial(string key, Texture2D albedo, Texture2D normal, ItemData item)
        {
            if (Shader.Find("ForestWhispers/WindLit") == null)
            {
                return GetLitMaterial(key, albedo, normal, normal != null, Color.white);
            }

            float strength = item.kind == "terrain" ? 0.04f : (item.kind == "back" ? 0.18f : 0.12f);
            float flutter = item.kind == "terrain" ? 0.02f : 0.035f;
            return GetMaterial("Wind/" + key, "ForestWhispers/WindLit", m =>
            {
                m.SetTexture("_MainTex", albedo);
                if (normal != null)
                {
                    m.SetTexture("_BumpMap", normal);
                }

                m.SetFloat("_Cutoff", normal != null ? 0.35f : 0.02f);
                m.SetFloat("_WindStrength", strength);
                m.SetFloat("_LeafFlutter", flutter);
            });
        }

        private static float GetFog(float z)
        {
            // 그림 자체에 공기 원근이 이미 칠해져 있으므로 배경판보다 먼 것만 살짝 흐리게
            return Mathf.Clamp((z - 6f) / 30f, 0f, 0.35f);
        }

        private static void AddCollider(GameObject go, ItemData item)
        {
            if (item.kind != "terrain" || item.top == null || item.top.Length < 4)
            {
                return;
            }

            var holder = new GameObject("WalkableSurface");
            holder.transform.SetParent(go.transform, false);
            var edge = holder.AddComponent<EdgeCollider2D>();
            var points = new Vector2[item.top.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(item.top[i * 2], item.top[i * 2 + 1]);
            }

            edge.points = points;
            edge.edgeRadius = 0.04f;
            if (!item.oneway)
            {
                return;
            }

            edge.usedByEffector = true;
            var effector = holder.AddComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.surfaceArc = 160f;
        }

        private static void AddGlow(GameObject go, ItemData item)
        {
            if (string.IsNullOrEmpty(item.glow))
            {
                return;
            }

            Sprite glowSprite = LoadSprite("Common/Glow.png");
            if (glowSprite == null)
            {
                return;
            }

            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            glow.transform.localPosition = new Vector3(0f, 0f, -item.front - 0.05f);
            var renderer = glow.AddComponent<SpriteRenderer>();
            renderer.sprite = glowSprite;
            var parentSprite = go.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = parentSprite != null ? parentSprite.sortingOrder + 1 : 30;
            renderer.sharedMaterial = GetAdditiveMaterial();
            Color color = ParseColor(item.glow);
            color.a = 0.25f;
            renderer.color = color;
            float size = Mathf.Max(item.w, item.h) * 1.6f / glowSprite.bounds.size.x;
            glow.transform.localScale = new Vector3(size, size, 1f);
            glow.AddComponent<GlowPulse>().Setup(0.35f, 3.5f);
        }

        private static void AddMotion(GameObject go, ItemData item)
        {
            if (Mathf.Abs(item.spin) > 0f)
            {
                go.AddComponent<SpinMotion>().Setup(item.spin);
            }

            if (item.bob > 0f)
            {
                go.AddComponent<DriftMotion>().Setup(0f, item.bob * 0.25f);
            }
        }

        private static Material GetSpriteMaterial(Color fogColor, float fog, float sway)
        {
            string key = "Sprite_fog" + Mathf.RoundToInt(fog * 100f) + "_sway" + Mathf.RoundToInt(sway * 100f) + "_" + ColorUtility.ToHtmlStringRGB(fogColor);
            return GetMaterial(key, "ForestWhispers/Sprite25D", m =>
            {
                m.SetColor("_FogColor", fogColor);
                m.SetFloat("_Fog", fog);
                m.SetFloat("_Sway", sway);
            });
        }

        private static Material GetFallMaterial(Color fogColor, float fog)
        {
            string key = "Waterfall_fog" + Mathf.RoundToInt(fog * 100f) + "_" + ColorUtility.ToHtmlStringRGB(fogColor);
            return GetMaterial(key, "ForestWhispers/WaterfallFlow", m =>
            {
                m.SetColor("_FogColor", fogColor);
                m.SetFloat("_Fog", fog);
            });
        }

        private static Material GetAdditiveMaterial()
        {
            return GetMaterial("Additive", "ForestWhispers/SpriteAdditive", m => { });
        }

        private static Material GetMaterial(string key, string shaderName, Action<Material> configure)
        {
            if (_materials.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            string path = _root + "Materials/" + key + ".mat";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shaderName));
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = Shader.Find(shaderName);
            configure(material);
            EditorUtility.SetDirty(material);
            _materials[key] = material;
            return material;
        }

        private static string BuildScene(MapData map, MapData[] maps, Dictionary<string, GameObject> prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(map.name).transform;
            Dictionary<string, ItemData> items = map.items.ToDictionary(i => i.name);

            CreatePlates(map, root);
            Transform groupRoot = new GameObject("Assets").transform;
            groupRoot.SetParent(root, false);
            var groups = new Dictionary<string, Transform>();
            foreach (PlaceData place in map.places)
            {
                if (!prefabs.TryGetValue(place.name, out GameObject prefab) ||
                    !items.TryGetValue(place.name, out ItemData item))
                {
                    continue;
                }

                string category = GetCategory(item.kind);
                if (!groups.TryGetValue(category, out Transform group))
                {
                    group = new GameObject(category).transform;
                    group.SetParent(groupRoot, false);
                    groups[category] = group;
                }

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, group);
                PlaceAtDepth(instance.transform, place, GetPlacedDepth(item, map), map);
                if (item.kind == "fall" && item.z >= 1.5f)
                {
                    instance.AddComponent<SegmentFade>().Setup((place.seg + 0.5f) * map.segment, map.segment);
                }
            }

            CreateBounds(map, root);
            CreateLighting(map, root);
            GameObject player = CreatePlayer(map, items);
            CreateCamera(map, player.transform);
            CreateGates(map, maps, root);

            string path = _root + "Scenes/" + ScenePrefix + map.name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        /// <summary>
        /// 앞쪽 폭포는 지형 앞면보다 앞에 둬야 절벽 앞으로 떨어지는 것처럼 보인다.
        /// 먼 폭포는 배경판 바로 앞에 붙여 배경 그림과 겹쳐 보이게 한다.
        /// </summary>
        private static float GetPlacedDepth(ItemData item, MapData map)
        {
            if (item.kind != "fall")
            {
                return item.z;
            }

            return item.z < 1.5f ? -0.8f : map.plateZ - 0.1f;
        }

        /// <summary>
        /// 깊이 z에 놓인 물체가 카메라가 바로 앞에 왔을 때 원래 그림 위치와 크기로 보이도록 배치한다.
        /// 좌우 반전 구간은 음수 스케일로 뒤집으므로 충돌선도 함께 뒤집힌다.
        /// </summary>
        /// <remarks>
        /// 카메라가 구간 중앙(segCenter)에 있을 때를 기준으로, 깊이에 따른 원근 축소를 되돌려
        /// 앞면이 레퍼런스 그림의 픽셀 위치와 정확히 겹치게 한다. 메시 꼭짓점도 같은 방식으로 미리 보정되어 있다.
        /// </remarks>
        private static void PlaceAtDepth(Transform target, PlaceData place, float z, MapData map)
        {
            float scale = 1f + z / map.camDist;
            float centerX = (place.seg + 0.5f) * map.segment;
            float centerY = map.height * 0.5f;
            float x = centerX + (place.x - centerX) * scale;
            float y = centerY + (place.y - centerY) * scale;
            target.position = new Vector3(x, y, z);
            target.localScale = new Vector3(place.flip ? -scale : scale, scale, scale);
        }

        /// <summary>
        /// 배경판은 구간마다 한 장씩 깊이 plateZ에 둔다. 카메라가 구간 중앙에 있을 때 화면을 꽉 채우는 크기(scale)라
        /// 이웃 배경판과 구간 경계 양옆으로 (scale-1)×segment 만큼 겹친다.
        /// 겹치는 띠는 오른쪽 배경판을 PlateBlend 셰이더로 한 번 더 그려, 카메라 위치에 따라 PlateBlender가 섞는다.
        /// </summary>
        private static void CreatePlates(MapData map, Transform root)
        {
            var group = new GameObject("Background_Plates").transform;
            group.SetParent(root, false);
            float scale = 1f + map.plateZ / map.camDist;
            float halfW = map.segment * scale * 0.5f;
            float halfH = map.height * scale * 0.5f;
            float overlap = map.segment * (scale - 1f);
            float y0 = map.height * 0.5f - halfH;
            float y1 = map.height * 0.5f + halfH;
            var strips = new List<Renderer>();
            var seams = new List<float>();
            for (int i = 0; i < map.plates.Length; i++)
            {
                PlateData plate = map.plates[i];
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(_root + "Sprites/" + plate.sprite);
                if (texture == null)
                {
                    continue;
                }

                float centerX = (i + 0.5f) * map.segment;
                float left = centerX - halfW;
                float split = i == 0 ? left : i * map.segment + overlap * 0.5f;
                Material plateMaterial = GetMaterial("Plate/" + map.name, "ForestWhispers/Sprite25D", m =>
                {
                    m.SetTexture("_MainTex", texture);
                    m.SetColor("_FogColor", ParseColor(map.haze));
                    m.SetFloat("_Fog", 0f);
                    m.SetFloat("_Sway", 0f);
                    m.renderQueue = (int)RenderQueue.Transparent - 100;
                });
                CreatePlatePart(group, "Plate_" + i, map, i, "", split, centerX + halfW, y0, y1, centerX, halfW, plate.flip, plateMaterial);
                if (i == 0)
                {
                    continue;
                }

                Material blendMaterial = GetMaterial("PlateBlend/" + map.name, "ForestWhispers/PlateBlend", m =>
                {
                    m.SetTexture("_MainTex", texture);
                    m.SetFloat("_Blend", 0f);
                });
                strips.Add(CreatePlatePart(group, "PlateSeam_" + i, map, i, "_Seam", left, split, y0, y1, centerX, halfW, plate.flip, blendMaterial));
                seams.Add(i * map.segment);
            }

            group.gameObject.AddComponent<PlateBlender>().Setup(strips.ToArray(), seams.ToArray(), map.segment);
        }

        private static Renderer CreatePlatePart(Transform parent, string name, MapData map, int index, string suffix,
            float x0, float x1, float y0, float y1, float centerX, float halfW, bool flip, Material material)
        {
            float u0 = (x0 - centerX) / (2f * halfW) + 0.5f;
            float u1 = (x1 - centerX) / (2f * halfW) + 0.5f;
            if (flip)
            {
                u0 = 1f - u0;
                u1 = 1f - u1;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 0f, map.plateZ);
            go.AddComponent<MeshFilter>().sharedMesh = SavePlateMesh(map.name, "Plate_" + index + suffix, x0, x1, y0, y1, u0, u1);
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Mesh SavePlateMesh(string mapName, string name, float x0, float x1, float y0, float y1, float u0, float u1)
        {
            string folder = _root + "Generated/Meshes/" + mapName + "/";
            Directory.CreateDirectory(folder);
            string path = folder + name + ".asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool isNew = mesh == null;
            if (isNew)
            {
                mesh = new Mesh();
            }

            mesh.Clear();
            mesh.vertices = new[] { new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f), new Vector3(x1, y1, 0f), new Vector3(x0, y1, 0f) };
            mesh.uv = new[] { new Vector2(u0, 0f), new Vector2(u1, 0f), new Vector2(u1, 1f), new Vector2(u0, 1f) };
            mesh.uv2 = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (isNew)
            {
                AssetDatabase.CreateAsset(mesh, path);
            }

            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static void CreateLighting(MapData map, Transform root)
        {
            Color haze = ParseColor(map.haze);
            var sun = new GameObject("Directional Light");
            sun.transform.SetParent(root, false);
            sun.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            // 앞면(카메라를 향한 면)이 그림 색 그대로(주변광 0.72 + 직사광 0.43×0.66 ≈ 1.0) 나오도록 맞춘다.
            // 빛 색은 맵 분위기를 아주 약하게만 섞어 그림 색이 변하지 않게 한다.
            light.color = Color.Lerp(LightColors[Mathf.Clamp(map.index, 0, LightColors.Length - 1)], Color.white, 0.75f);
            light.intensity = LightIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(haze, Color.white, 0.92f) * 0.72f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = haze;
            RenderSettings.fogStartDistance = map.camDist + 12f;
            RenderSettings.fogEndDistance = map.camDist + 60f;
        }

        private static void CreateBounds(MapData map, Transform root)
        {
            var group = new GameObject("Bounds").transform;
            group.SetParent(root, false);
            if (!map.hasPrev)
            {
                CreateWall(group, "WallLeft", 0.05f, map.height);
            }

            if (!map.hasNext)
            {
                CreateWall(group, "WallRight", map.width - 0.05f, map.height);
            }
        }

        private static void CreateWall(Transform parent, string name, float x, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var edge = go.AddComponent<EdgeCollider2D>();
            edge.points = new[] { new Vector2(x, -5f), new Vector2(x, height + 10f) };
        }

        private static Vector2 GetSpawn(MapData map, Dictionary<string, ItemData> items, bool fromRight)
        {
            float bestX = fromRight ? float.MinValue : float.MaxValue;
            Vector2 best = new Vector2(1f, map.height * 0.5f);
            foreach (PlaceData place in map.places)
            {
                if (!items.TryGetValue(place.name, out ItemData item) ||
                    item.kind != "terrain" ||
                    item.top == null ||
                    item.top.Length < 4)
                {
                    continue;
                }

                for (int i = 0; i < item.top.Length; i += 2)
                {
                    float x = place.x + (place.flip ? -item.top[i] : item.top[i]);
                    bool isBetter = fromRight ? x > bestX : x < bestX;
                    if (!isBetter)
                    {
                        continue;
                    }

                    bestX = x;
                    best = new Vector2(x, place.y + item.top[i + 1]);
                }
            }

            float inset = fromRight ? -1.2f : 1.2f;
            return new Vector2(best.x + inset, best.y + 0.6f);
        }

        private static GameObject CreatePlayer(MapData map, Dictionary<string, ItemData> items)
        {
            var player = new GameObject("Player") { layer = 2 };
            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.7f, 1.7f);
            capsule.offset = new Vector2(0f, 0.85f);
            capsule.sharedMaterial = new PhysicsMaterial2D("Player") { friction = 0f, bounciness = 0f };

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            var controller = player.AddComponent<PlayerController2D>();
            controller.Setup(map.killY, GetSpawn(map, items, false), GetSpawn(map, items, true), visual.transform);
            player.AddComponent<PlayerGather>().Setup(controller);
            Vector2 spawn = GetSpawn(map, items, false);
            player.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            return player;
        }

        private static void CreateCamera(MapData map, Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = false;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ParseColor(map.haze);
            go.AddComponent<AudioListener>();
            float half = map.viewH * 0.5f;
            float minY = half;
            float maxY = Mathf.Max(half, map.height - half);
            go.transform.position = new Vector3(target.position.x, minY, -map.camDist);
            go.AddComponent<CameraFollow25D>().Setup(target, map.camDist, map.viewH, map.width, minY, maxY);
        }

        private static void CreateGates(MapData map, MapData[] maps, Transform root)
        {
            if (map.hasPrev)
            {
                CreateGate(root, "GateLeft", 0.15f, map.height, ScenePrefix + maps[map.index - 1].name, true);
            }

            if (map.hasNext)
            {
                CreateGate(root, "GateRight", map.width - 0.15f, map.height, ScenePrefix + maps[map.index + 1].name, false);
            }
        }

        private static void CreateGate(Transform parent, string name, float x, float height, string nextScene, bool arriveAtRightEnd)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, height * 0.5f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.6f, height + 10f);
            go.AddComponent<MapGate>().Setup(nextScene, arriveAtRightEnd);
        }

        private static Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
