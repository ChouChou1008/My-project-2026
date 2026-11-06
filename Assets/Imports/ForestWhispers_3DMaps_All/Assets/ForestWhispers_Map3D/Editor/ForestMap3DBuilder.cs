using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForestWhispers.EditorTools
{
    /// Menu: Forest Whispers > Build 6 3D Maps. Needs ForestWhispers_Full imported first.
    /// Makes materials for you, builds terrain, surrounding backdrop and grass, places every model, saves six scenes.
    public static class ForestMap3DBuilder
    {
        const string Root = "Assets/ForestWhispers_Map3D/";
        const string Fbx = "Assets/ForestWhispers_Full/Models_3D/FBX/";
        const string Gen = Root + "Generated/";

        [Serializable] class Item { public string fbx; public float x, y, z, r, s; public bool solid; }
        [Serializable] class MapData { public string name; public Item[] items; public float x0, x1, z0, z1; public int nx, nz; public float[] gh, grass; public string fog; public bool water; public int[] top; }

        static bool Urp => GraphicsSettings.currentRenderPipeline != null;

        [MenuItem("Forest Whispers/Build 6 3D Maps")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(Fbx.TrimEnd('/')))
            {
                EditorUtility.DisplayDialog("Forest Whispers", "ForestWhispers_Full 폴더를 먼저 Assets에 넣어 주세요.", "확인");
                return;
            }
            Directory.CreateDirectory(Gen); Directory.CreateDirectory(Root + "Scenes");
            AssetDatabase.Refresh();
            var files = Directory.GetFiles(Root + "Maps", "*.json").OrderBy(f => f).ToArray();
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < files.Length; i++)
            {
                MapData d = JsonUtility.FromJson<MapData>(File.ReadAllText(files[i]));
                EditorUtility.DisplayProgressBar("Forest Whispers", d.name, (float)i / files.Length);
                try
                {
                    string path = Build(d);
                    if (!scenes.Any(s => s.path == path)) scenes.Add(new EditorBuildSettingsScene(path, true));
                }
                finally { EditorUtility.ClearProgressBar(); }
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(scenes.First(s => s.path.Contains("01_Village")).path);
            Debug.Log("Forest Whispers: 3D maps saved in " + Root + "Scenes");
        }

        static string Build(MapData d)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string dir = Gen + d.name + "/"; Directory.CreateDirectory(dir); AssetDatabase.Refresh();
            var root = new GameObject(d.name).transform;

            ColorUtility.TryParseHtmlString(d.fog, out Color fog);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog; RenderSettings.fogStartDistance = 22f; RenderSettings.fogEndDistance = 120f;
            Color sky = new Color(d.top[0] / 255f, d.top[1] / 255f, d.top[2] / 255f);

            Mesh g = GroundMesh(d); Save(g, dir + "ground.asset");
            var gm = Part(root, "Ground", g, Mat(dir + "ground.mat", Tex(d.name + "_ground.jpg", false), false));
            gm.AddComponent<MeshCollider>().sharedMesh = g;

            Mesh w = WallMesh(); Save(w, dir + "backdrop.asset");
            Part(root, "Backdrop", w, Mat(dir + "backdrop.mat", Tex(d.name + "_wall.jpg", true), false));

            Mesh gr = GrassMesh(d); Save(gr, dir + "grass.asset");
            Part(root, "Grass", gr, Mat(dir + "grass.mat", Tex(d.name + "_grass.png", false), true));

            if (d.water)
            {
                Mesh wt = WaterMesh(); Save(wt, dir + "water.asset");
                var wo = Part(root, "Water", wt, Mat(dir + "water.mat", Tex("lake.jpg", true), false));
                wo.transform.position = new Vector3(-3f, -.25f, -7f);
            }

            var props = new GameObject("Models").transform; props.SetParent(root, false);
            foreach (Item e in d.items)
            {
                string fbx = Fbx + e.fbx + ".fbx";
                Prepare(fbx);
                var pre = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (pre == null) { Debug.LogWarning("Missing model: " + fbx); continue; }
                var o = (GameObject)PrefabUtility.InstantiatePrefab(pre, props);
                o.transform.position = new Vector3(-e.x, e.y, e.z);
                o.transform.rotation = Quaternion.Euler(0f, -e.r * Mathf.Rad2Deg, 0f) * pre.transform.rotation;
                o.transform.localScale = Vector3.Scale(pre.transform.localScale, Vector3.one * e.s);
                if (e.solid) foreach (var f in o.GetComponentsInChildren<MeshFilter>()) f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh;
            }

            var player = new GameObject("Player");
            player.transform.position = new Vector3(30f, 3f, 1.2f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var cc = player.AddComponent<CharacterController>(); cc.height = 1.8f; cc.radius = .35f; cc.center = new Vector3(0f, .9f, 0f);
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(player.transform, false); camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 62f; cam.farClipPlane = 420f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = sky;
            camGo.AddComponent<AudioListener>();
            player.AddComponent<WalkPlayer>().head = camGo.transform;

            string path = Root + "Scenes/" + d.name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static GameObject Part(Transform parent, string name, Mesh m, Material mat)
        {
            var o = new GameObject(name); o.transform.SetParent(parent, false);
            o.AddComponent<MeshFilter>().sharedMesh = m;
            o.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return o;
        }

        static void Save(Mesh m, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
        }

        static Texture2D Tex(string file, bool repeat)
        {
            string p = Root + "Textures/" + file;
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            if (ti != null)
            {
                ti.maxTextureSize = 4096; ti.textureCompression = TextureImporterCompression.CompressedHQ;
                ti.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                if (file.EndsWith(".png")) { ti.alphaIsTransparency = true; }
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }

        /// One unlit material that works in both Built-in and URP projects.
        static Material NewMat(Texture tex, bool cutout)
        {
            Shader s = Urp ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find(cutout ? "Unlit/Transparent Cutout" : "Unlit/Texture");
            var m = new Material(s);
            if (Urp)
            {
                m.SetTexture("_BaseMap", tex);
                if (cutout)
                {
                    m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", .5f); m.SetFloat("_Cull", 0f);
                    m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450;
                }
            }
            else
            {
                m.SetTexture("_MainTex", tex);
                if (cutout) m.SetFloat("_Cutoff", .5f);
            }
            return m;
        }

        static Material Mat(string path, Texture tex, bool cutout)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) AssetDatabase.DeleteAsset(path);
            var m = NewMat(tex, cutout); AssetDatabase.CreateAsset(m, path); return m;
        }

        static readonly HashSet<string> done = new HashSet<string>();

        /// Replaces the read-only material inside an FBX with an editable unlit one, so nobody has to do it by hand.
        static void Prepare(string fbx)
        {
            if (!done.Add(fbx)) return;
            var imp = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (imp == null) return;
            string name = fbx.Substring(Fbx.Length).Replace('/', '_').Replace(".fbx", "");
            string dir = Gen + "Models/" + name + "/";
            Directory.CreateDirectory(dir); AssetDatabase.Refresh();
            var embedded = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Material>().ToArray();
            int i = 0; bool changed = false;
            foreach (Material src in embedded)
            {
                string p = dir + "mat" + (i++) + ".mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(p) != null) continue;
                Texture t = src.mainTexture;
                if (t == null)
                {
                    imp.ExtractTextures(dir); AssetDatabase.Refresh();
                    string g = AssetDatabase.FindAssets("t:Texture2D", new[] { dir.TrimEnd('/') }).FirstOrDefault();
                    if (g != null) t = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g));
                }
                var m = NewMat(t, false); AssetDatabase.CreateAsset(m, p);
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), m);
                changed = true;
            }
            if (changed) imp.SaveAndReimport();
        }

        static Mesh GroundMesh(MapData d)
        {
            int nx = d.nx, nz = d.nz; var v = new Vector3[nx * nz]; var uv = new Vector2[nx * nz]; var t = new List<int>();
            for (int j = 0; j < nz; j++)
                for (int i = 0; i < nx; i++)
                {
                    float x = Mathf.Lerp(d.x0, d.x1, i / (nx - 1f)), z = Mathf.Lerp(d.z0, d.z1, j / (nz - 1f));
                    v[j * nx + i] = new Vector3(-x, d.gh[j * nx + i], z);
                    uv[j * nx + i] = new Vector2(i / (nx - 1f), 1f - j / (nz - 1f));
                }
            for (int j = 0; j < nz - 1; j++)
                for (int i = 0; i < nx - 1; i++)
                {
                    int a = j * nx + i, b = a + 1, c = a + nx, e = c + 1;
                    t.AddRange(new[] { a, b, c, b, e, c });
                }
            return Finish(v, uv, t);
        }

        static Mesh WallMesh()
        {
            const int n = 240; const float a = 74f, b = 58f, cz = -8f, h = 38f;
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>(); float s = 0f; Vector3 last = Vector3.zero;
            var len = new float[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float q = i / (float)n * Mathf.PI * 2f, c = Mathf.Cos(q), e = Mathf.Sin(q);
                var p = new Vector3(-a * Mathf.Sign(c) * Mathf.Sqrt(Mathf.Abs(c)), 0f, cz + b * Mathf.Sign(e) * Mathf.Sqrt(Mathf.Abs(e)));
                if (i > 0) s += Vector3.Distance(p, last);
                last = p; len[i] = s;
                v.Add(p + Vector3.down * 3f); v.Add(p + Vector3.up * h);
            }
            for (int i = 0; i <= n; i++) { uv.Add(new Vector2(len[i] / s, 0f)); uv.Add(new Vector2(len[i] / s, 1f)); }
            for (int i = 0; i < n; i++) { int k = i * 2; t.AddRange(new[] { k, k + 1, k + 2, k + 2, k + 1, k + 3, k, k + 2, k + 1, k + 2, k + 3, k + 1 }); }
            return Finish(v.ToArray(), uv.ToArray(), t);
        }

        static Mesh GrassMesh(MapData d)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i + 5 < d.grass.Length; i += 6)
            {
                float x = -d.grass[i], y = d.grass[i + 1], z = d.grass[i + 2], s = d.grass[i + 3], r = d.grass[i + 4]; int c = (int)d.grass[i + 5];
                for (int q = 0; q < 2; q++)
                {
                    float a = r + q * Mathf.PI / 2f, dx = Mathf.Cos(a) * s * .68f, dz = Mathf.Sin(a) * s * .68f, h = s * .95f, u0 = c / 4f, u1 = (c + 1) / 4f; int k = v.Count;
                    v.Add(new Vector3(x - dx, y, z - dz)); v.Add(new Vector3(x + dx, y, z + dz)); v.Add(new Vector3(x - dx, y + h, z - dz)); v.Add(new Vector3(x + dx, y + h, z + dz));
                    uv.Add(new Vector2(u0, 0f)); uv.Add(new Vector2(u1, 0f)); uv.Add(new Vector2(u0, 1f)); uv.Add(new Vector2(u1, 1f));
                    t.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3, k, k + 1, k + 2, k + 1, k + 3, k + 2 });
                }
            }
            return Finish(v.ToArray(), uv.ToArray(), t);
        }

        static Mesh WaterMesh()
        {
            const float w = 55f, h = 35f;
            var v = new[] { new Vector3(-w, 0f, -h), new Vector3(-w, 0f, h), new Vector3(w, 0f, -h), new Vector3(w, 0f, h) };
            var uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 4f), new Vector2(4f, 0f), new Vector2(4f, 4f) };
            return Finish(v, uv, new List<int> { 0, 1, 2, 2, 1, 3 });
        }

        static Mesh Finish(Vector3[] v, Vector2[] uv, List<int> t)
        {
            var m = new Mesh { indexFormat = IndexFormat.UInt32, vertices = v, uv = uv };
            m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}
