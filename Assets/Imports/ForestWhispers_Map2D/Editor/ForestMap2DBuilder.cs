using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForestWhispers.Map2D.EditorTools
{
    /// <summary>
    /// 메뉴: Forest Whispers > Build 6 2.5D Maps.
    /// 3D 맵의 배치를 옮긴 JSON을 읽어 2D 플레이 공간과 깊이 배경, 이펙트가 있는 씬 6개를 만든다.
    /// </summary>
    public static class ForestMap2DBuilder
    {
        [Serializable] private class TileData { public string tex; public float x, y, z, w, h, fog; public int order; }
        [Serializable] private class PropData
        {
            public string path, color, frames, tag;
            public float x, y, z, w, h, fog, sway, alpha, pulse, period, rot, drift, fps, bob, fly, spin;
            public int order;
            public bool flip, add;
        }
        [Serializable] private class ParticleData { public string preset, tex, color; public float x, y, z, w, h, rate, size, life, grav, speed; public bool add; }
        [Serializable] private class PolylineData { public float[] pts; }
        [Serializable] private class WaterData { public float x0, x1, y, bottom; }
        [Serializable] private class MapData
        {
            public string name, haze;
            public int index;
            public float width, camDist, viewH, killY;
            public float[] camY, spawn, spawnEnd;
            public int[] sky;
            public TileData[] tiles;
            public PropData[] props;
            public ParticleData[] particles;
            public PolylineData[] ground, platforms;
            public WaterData water;
            public bool hasPrev, hasNext;
        }

        private const string TexPrefix = "TEX:";
        private const int PlayerOrder = 10;

        private static string _root;
        private static string _fullRoot;
        private static float _currentWidth;
        private static readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();

        [MenuItem("Forest Whispers/Build 6 2.5D Maps")]
        public static void BuildAll()
        {
            if (!FindRoots())
            {
                EditorUtility.DisplayDialog("Forest Whispers", GetMissingMessage(), "확인");
                return;
            }

            Directory.CreateDirectory(_root + "Scenes");
            Directory.CreateDirectory(_root + "Materials");
            AssetDatabase.Refresh();
            _materials.Clear();

            string[] files = Directory.GetFiles(_root + "Maps", "*.json").OrderBy(f => f).ToArray();
            MapData[] maps = files.Select(f => JsonUtility.FromJson<MapData>(File.ReadAllText(f))).ToArray();
            if (!CheckTextures(maps))
            {
                return;
            }


            try
            {
                EditorUtility.DisplayProgressBar("Forest Whispers", "텍스처 설정", 0f);
                PrepareTextures(maps);
                List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
                for (int i = 0; i < maps.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Forest Whispers", maps[i].name, (i + 1f) / (maps.Length + 1f));
                    string path = BuildScene(maps[i], maps);
                    if (!scenes.Any(s => s.path == path))
                    {
                        scenes.Add(new EditorBuildSettingsScene(path, true));
                    }
                }

                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene(_root + "Scenes/" + maps[0].name + ".unity");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log("Forest Whispers: 2.5D 맵을 " + _root + "Scenes 에 저장했습니다.");
        }

        private static bool FindRoots()
        {
            _root = FindFolder("ForestWhispers_Map2D", "Maps");
            _fullRoot = FindFolder("ForestWhispers_Full", "Sprites_Drawn");
            return _root != null && _fullRoot != null;
        }

        private static string FindFolder(string name, string requiredChild)
        {
            string[] folders = Directory.GetDirectories("Assets", name, SearchOption.AllDirectories);
            foreach (string folder in folders)
            {
                if (!Directory.Exists(Path.Combine(folder, requiredChild)))
                {
                    continue;
                }

                return folder.Replace('\\', '/') + "/";
            }

            return null;
        }

        private static bool CheckTextures(MapData[] maps)
        {
            var missing = new List<string>();
            foreach (MapData map in maps)
            {
                missing.AddRange(map.tiles.Select(t => _root + "Textures/" + t.tex).Where(p => !File.Exists(p)));
                missing.AddRange(map.props.Where(p => !File.Exists(Resolve(p.path))).Select(p => Resolve(p.path)));
                missing.AddRange(map.props.Where(p => !string.IsNullOrEmpty(p.frames) && !Directory.Exists(GetFrameFolder(p.frames))).Select(p => GetFrameFolder(p.frames)));
            }

            if (missing.Count == 0)
            {
                return true;
            }

            string[] unique = missing.Distinct().ToArray();
            Debug.LogError("Forest Whispers 누락 파일 " + unique.Length + "개:\n" + string.Join("\n", unique.Take(30)));
            EditorUtility.DisplayDialog("Forest Whispers", "맵 데이터와 텍스처 버전이 맞지 않습니다. 파일 " + unique.Length + "개가 없습니다.\n\nForestWhispers_Map2D 폴더를 지우고 최신 ForestWhispers_Map2D.zip을 다시 넣어 주세요.", "확인");
            return false;
        }

        private static string GetFrameFolder(string folder)
        {
            return folder.StartsWith(TexPrefix) ? _root + "Textures/" + folder.Substring(TexPrefix.Length) : _fullRoot + folder;
        }

        private static string GetMissingMessage()
        {
            string message = "프로젝트의 Assets 안에서 다음 폴더를 찾지 못했습니다.\n";
            if (_fullRoot == null)
            {
                message += "\n- ForestWhispers_Full (안에 Sprites_Drawn 폴더가 있어야 함)";
            }

            if (_root == null)
            {
                message += "\n- ForestWhispers_Map2D (안에 Maps 폴더가 있어야 함)";
            }

            return message;
        }

        private static string Resolve(string path)
        {
            if (path.StartsWith(TexPrefix))
            {
                return _root + "Textures/" + path.Substring(TexPrefix.Length);
            }

            return _fullRoot + path;
        }

        private static string ResolveParticleTexture(string tex)
        {
            if (tex.StartsWith("fx_"))
            {
                return _fullRoot + "Sprites_Drawn/07_Effects/" + tex + ".png";
            }

            return _fullRoot + tex + ".png";
        }

        private static string[] FramePaths(string folder)
        {
            return Directory.GetFiles(GetFrameFolder(folder), "*.png")
                .Select(f => f.Replace('\\', '/'))
                .OrderBy(f => f, StringComparer.Ordinal)
                .ToArray();
        }

        private static void PrepareTextures(MapData[] maps)
        {
            var sprites = new HashSet<string>();
            foreach (MapData map in maps)
            {
                foreach (TileData tile in map.tiles)
                {
                    sprites.Add(_root + "Textures/" + tile.tex);
                }

                foreach (PropData prop in map.props)
                {
                    sprites.Add(Resolve(prop.path));
                    if (!string.IsNullOrEmpty(prop.frames))
                    {
                        sprites.UnionWith(FramePaths(prop.frames));
                    }
                }

                if (map.water.x1 > 0f)
                {
                    sprites.Add(_root + "Textures/" + map.name + "/water.jpg");
                }
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in sprites)
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
                Debug.LogWarning("텍스처를 찾을 수 없습니다: " + path);
                return;
            }

            bool isSingleSprite = importer.textureType == TextureImporterType.Sprite &&
                importer.spriteImportMode == SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool isFullRect = settings.spriteMeshType == SpriteMeshType.FullRect;
            if (isSingleSprite && isFullRect && importer.mipmapEnabled)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = true;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static string BuildScene(MapData map, MapData[] maps)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(map.name).transform;
            _currentWidth = map.width;
            Color fog = ParseColor(map.haze);

            CreateTiles(map, root, fog);
            CreateProps(map, root, fog);
            CreateWater(map, root);
            CreateParticles(map, root);
            CreateCollision(map, root);
            GameObject player = CreatePlayer(map);
            CreateCamera(map, player.transform);
            CreateGates(map, maps, root);

            string path = _root + "Scenes/" + map.name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static Color ParseColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static void CreateTiles(MapData map, Transform root, Color fog)
        {
            Transform group = Group(root, "Layers");
            foreach (TileData tile in map.tiles)
            {
                string path = _root + "Textures/" + tile.tex;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning("스프라이트가 없습니다: " + path);
                    continue;
                }

                var renderer = CreateSprite(Path.GetFileNameWithoutExtension(tile.tex), group, sprite,
                    new Vector3(tile.x, tile.y, tile.z), tile.w, tile.h, tile.order);
                renderer.sharedMaterial = GetDepthMaterial(map, fog, tile.fog, 0f);
            }
        }

        private static void CreateProps(MapData map, Transform root, Color fog)
        {
            Transform group = Group(root, "Props");
            foreach (PropData prop in map.props)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Resolve(prop.path));
                if (sprite == null)
                {
                    Debug.LogWarning("스프라이트가 없습니다: " + prop.path);
                    continue;
                }

                var renderer = CreateSprite(sprite.name, group, sprite, new Vector3(prop.x, prop.y, prop.z), prop.w, prop.h, prop.order);
                renderer.flipX = prop.flip;
                Color color = ParseColor(prop.color);
                color.a = prop.alpha;
                renderer.color = color;
                renderer.transform.rotation = Quaternion.Euler(0f, 0f, prop.rot);
                renderer.sharedMaterial = prop.add ? GetAdditiveMaterial() : GetDepthMaterial(map, fog, prop.fog, prop.sway);
                AddMotion(renderer.gameObject, prop);
                AddGatherItem(renderer, prop);
            }
        }

        private static void AddGatherItem(SpriteRenderer renderer, PropData prop)
        {
            if (prop.tag != "collect")
            {
                return;
            }

            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(_fullRoot + "Sprites_Drawn/07_Effects/fx_glow_soft.png");
            var glow = new GameObject("Highlight");
            glow.transform.SetParent(renderer.transform, false);
            var glowRenderer = glow.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = glowSprite;
            glowRenderer.sharedMaterial = GetAdditiveMaterial();
            glowRenderer.sortingOrder = renderer.sortingOrder - 1;
            glowRenderer.enabled = false;
            Vector3 parentScale = renderer.transform.localScale;
            float size = 1.8f / glowSprite.bounds.size.x;
            glow.transform.localScale = new Vector3(size / parentScale.x, size / parentScale.y, 1f);

            string itemId = Path.GetFileNameWithoutExtension(prop.path).Replace("collect_", "");
            renderer.gameObject.AddComponent<GatherItem>().Setup(itemId, 1, glowRenderer);
        }

        private static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Vector3 position, float width, float height, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            Vector3 size = sprite.bounds.size;
            go.transform.localScale = new Vector3(width / size.x, height / size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void AddMotion(GameObject go, PropData prop)
        {
            if (prop.pulse > 0f)
            {
                go.AddComponent<GlowPulse>().Setup(prop.pulse, prop.period);
            }

            if (prop.drift > 0f || prop.bob > 0f)
            {
                go.AddComponent<DriftMotion>().Setup(prop.drift, prop.bob * 0.35f);
            }

            if (Mathf.Abs(prop.fly) > 0f)
            {
                go.AddComponent<FlyAcross>().Setup(prop.fly, _currentWidth);
            }

            if (Mathf.Abs(prop.spin) > 0f)
            {
                go.AddComponent<SpinMotion>().Setup(prop.spin);
            }

            if (string.IsNullOrEmpty(prop.frames))
            {
                return;
            }

            Sprite[] frames = FramePaths(prop.frames).Select(AssetDatabase.LoadAssetAtPath<Sprite>).Where(s => s != null).ToArray();
            go.AddComponent<SpriteFlipbook>().Setup(frames, prop.fps);
        }

        private static Material GetDepthMaterial(MapData map, Color fogColor, float fog, float sway)
        {
            string key = map.name + "_fog" + Mathf.RoundToInt(fog * 100f) + "_sway" + Mathf.RoundToInt(sway * 100f);
            return GetMaterial(key, "ForestWhispers/Sprite25D", material =>
            {
                material.SetColor("_FogColor", fogColor);
                material.SetFloat("_Fog", fog);
                material.SetFloat("_Sway", sway);
            });
        }

        private static Material GetAdditiveMaterial()
        {
            return GetMaterial("additive", "ForestWhispers/SpriteAdditive", material => { });
        }

        private static Material GetMaterial(string key, string shaderName, Action<Material> configure)
        {
            if (_materials.TryGetValue(key, out Material cached))
            {
                return cached;
            }

            string path = _root + "Materials/" + key + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            var material = new Material(Shader.Find(shaderName));
            configure(material);
            AssetDatabase.CreateAsset(material, path);
            _materials[key] = material;
            return material;
        }

        private static void CreateWater(MapData map, Transform root)
        {
            if (map.water.x1 <= 0f)
            {
                return;
            }

            string path = _root + "Textures/" + map.name + "/water.jpg";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("수면 텍스처가 없습니다: " + path);
                return;
            }

            WaterData water = map.water;
            var position = new Vector3((water.x0 + water.x1) * 0.5f, (water.y + water.bottom) * 0.5f, 0.3f);
            var renderer = CreateSprite("Water", root, sprite, position, water.x1 - water.x0, water.y - water.bottom, -7);
            renderer.sharedMaterial = GetMaterial(map.name + "_water", "ForestWhispers/Water", material => { });
        }

        private static void CreateParticles(MapData map, Transform root)
        {
            Transform group = Group(root, "Particles");
            foreach (ParticleData data in map.particles)
            {
                var go = new GameObject(data.preset);
                go.transform.SetParent(group, false);
                go.transform.position = new Vector3(data.x, data.y, data.z);
                ConfigureParticles(go.AddComponent<ParticleSystem>(), map, data);
            }
        }

        private static void ConfigureParticles(ParticleSystem system, MapData map, ParticleData data)
        {
            Color color = ParseColor(data.color);
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(data.life * 0.6f, data.life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, data.speed);
            main.startSize = new ParticleSystem.MinMaxCurve(data.size * 0.5f, data.size);
            main.startColor = color;
            main.gravityModifier = data.grav;
            main.maxParticles = Mathf.CeilToInt(data.rate * data.life * 1.3f) + 8;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = data.rate;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(data.w, data.h, 1f);

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = data.speed;
            noise.frequency = 0.25f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial(map, data);
            renderer.sortingOrder = data.z < 0f ? 62 : (data.z <= 2f ? PlayerOrder + 1 : -30);
        }

        private static Material GetParticleMaterial(MapData map, ParticleData data)
        {
            string key = "particle_" + map.name + "_" + data.preset;
            string shader = data.add ? "ForestWhispers/SpriteAdditive" : "ForestWhispers/Sprite25D";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ResolveParticleTexture(data.tex));
            return GetMaterial(key, shader, material => material.mainTexture = texture);
        }

        private static void CreateCollision(MapData map, Transform root)
        {
            Transform group = Group(root, "Collision");
            foreach (PolylineData line in map.ground)
            {
                CreateEdge("Ground", group, line);
            }

            foreach (PolylineData line in map.platforms)
            {
                EdgeCollider2D edge = CreateEdge("Platform", group, line);
                edge.usedByEffector = true;
                var effector = edge.gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.surfaceArc = 150f;
            }

            if (!map.hasPrev)
            {
                CreateWall("WallLeft", group, 1f);
            }

            if (!map.hasNext)
            {
                CreateWall("WallRight", group, map.width - 1f);
            }
        }

        private static EdgeCollider2D CreateEdge(string name, Transform parent, PolylineData line)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var edge = go.AddComponent<EdgeCollider2D>();
            var points = new Vector2[line.pts.Length / 2];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(line.pts[i * 2], line.pts[i * 2 + 1]);
            }

            edge.points = points;
            return edge;
        }

        private static void CreateWall(string name, Transform parent, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var edge = go.AddComponent<EdgeCollider2D>();
            edge.points = new[] { new Vector2(x, -6f), new Vector2(x, 30f) };
        }

        private static GameObject CreatePlayer(MapData map)
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

            var physics = new PhysicsMaterial2D("Player") { friction = 0f, bounciness = 0f };
            capsule.sharedMaterial = physics;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            var controller = player.AddComponent<PlayerController2D>();
            controller.Setup(map.killY, new Vector2(map.spawn[0], map.spawn[1]), new Vector2(map.spawnEnd[0], map.spawnEnd[1]), visual.transform);
            //player.AddComponent<PlayerGather>().Setup(controller);
            player.transform.position = new Vector3(map.spawn[0], map.spawn[1], 0f);
            return player;
        }

        private static void CreateCamera(MapData map, Transform target)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = false;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 400f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(map.sky[0] / 255f, map.sky[1] / 255f, map.sky[2] / 255f);
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(map.spawn[0], map.camY[0], -map.camDist);
            go.AddComponent<CameraFollow25D>().Setup(target, map.camDist, map.viewH, map.width, map.camY[0], map.camY[1]);
        }

        private static void CreateGates(MapData map, MapData[] maps, Transform root)
        {
            if (map.hasPrev)
            {
                CreateGate("GateLeft", root, 0.2f, maps[map.index - 1].name, true);
            }

            if (map.hasNext)
            {
                CreateGate("GateRight", root, map.width - 0.2f, maps[map.index + 1].name, false);
            }
        }

        private static void CreateGate(string name, Transform parent, float x, string nextScene, bool arriveAtRightEnd)
        {
            var go = new GameObject(name) { layer = 2 };
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, 5f, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.8f, 40f);
            go.AddComponent<MapGate>().Setup(nextScene, arriveAtRightEnd);
        }
    }
}
