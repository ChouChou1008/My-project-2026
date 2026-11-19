using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 메뉴: Tools > Puzzle Forest > Build Scene (Map 1-5)
///
/// TestSence를 PuzzleForest로 복사한 뒤(플레이어, 카메라, UI 연결을 그대로 쓰기 위해)
/// 맵 1~5를 왼쪽에서 오른쪽으로 한 줄로 이어 붙인다.
///
///   맵 1 (기존 TestSence 맵, x 0 ~ 96)
///   | 문 | 맵 2 이끼 발판 | 문 | 맵 3 높은 뿌리 턱 | 문 | 맵 4 빛나는 버섯 | 문 | 맵 5 엘프의 숲 입구 | 마지막 문
///
/// 각 맵 끝의 문은 퍼즐을 풀기 전까지 보이지 않는 벽으로 막혀 있고, 풀면 빛나며 열린다.
/// 다시 실행하면 PuzzleForest.unity를 새로 만든다(TestSence는 건드리지 않음).
/// </summary>
public static class PuzzleForestBuilder
{
    private const string SourceScenePath = "Assets/Scenes/TestSence.unity";
    private const string TargetScenePath = "Assets/Scenes/PuzzleForest.unity";
    private const string RegionScenePath = "Assets/Imports/ForestWhispers/Scenes/";
    private const string ForestPrefabPath = "Assets/Imports/ForestWhispers/Prefabs/";
    private const string ItemPrefabPath = "Assets/Materials/Prefabs/";
    private const string MaterialFolder = "Assets/Materials/Puzzle";

    private const string BoxVisualPath = "Assets/Imports/KUBIKOS - 3D Cube Village And Farm/Prefabs/Buildings/Hey.prefab";
    private const string ExitEffectPath = "Assets/Imports/Hovl Studio/Auras pack 3/Prefabs/Forest aura.prefab";
    private const string MonsterPath = "Assets/Imports/Suriyun/Monster Pack Forest/Prefab/Flower Dryad/Flower Dryad.prefab";
    private const string RuneGate = "03_MiddleForest/Props/Rune_Gate.prefab";
    private const string ElvenGate = "06_ElvenForest/Props/Elven_Gate.prefab";

    // ── 맵 1(사용자 맵)과 이어지는 부분 ──
    // 03_MiddleForest(x 34.9)의 Ground_Floor(x 76.84) 오른쪽 끝 바닥 높이가 약 2.4라서, 그 높이로 이어 붙인다.
    // 그보다 오른쪽 지형은 2단 점프가 필요한 높이라 지금 플레이어로는 갈 수 없어서 잘라 낸다.
    private const string Map1RootName = "03_MiddleForest";
    private const float Map1WalkableFrom = 73f;     // 이 구간의 지형 충돌선을 Ground 레이어로 바꿔 걸을 수 있게
    private const float Map1TrimX = 81f;            // 이보다 오른쪽의 맵 1 지형·소품은 지움
    private const float FloorStartX = 79.8f;        // 새 바닥이 시작되는 곳 (맵 1 바닥과 같은 높이에서 이어짐)
    private const float FloorY = 2.4f;
    private const float Map1End = 96.02f;           // 03_MiddleForest 배경판 두 번째 장이 끝나는 곳 = 맵 2 시작

    // ── 퍼즐 맵 ──
    private const float MapWidth = 61.12f;          // 지역 배경판 두 장 (30.56 × 2)
    private const float RegionGroundY = 1.1f;       // 지역 배경판이 그려진 바닥 높이 → FloorY에 맞춰 배경을 올린다
    private const float LedgeHeight = 2.4f;         // 점프(약 1.8)로는 못 오르고, 박스(1) + 점프로는 오르는 높이

    private static readonly Color ExitLightColor = new Color(0.55f, 1f, 0.8f);
    private static readonly Color KeyLightColor = new Color(1f, 0.85f, 0.4f);

    private static int _groundLayer;
    private static int _boxLayer;
    private static int _playerLayer;
    private static Scene _scene;
    private static Material _plateMaterial;

    private static float MapStart(int mapNumber)
    {
        return Map1End + MapWidth * (mapNumber - 2);    // 맵 2부터 5까지
    }

    private static float WorldEnd => MapStart(6);

    [MenuItem("Tools/Puzzle Forest/Build Scene (Map 1-5)")]
    public static void Build()
    {
        if (!CheckLayers() || !CheckSource())
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (File.Exists(TargetScenePath) &&
            !EditorUtility.DisplayDialog("Puzzle Forest", TargetScenePath + " 를 새로 만들까요?\n(기존 PuzzleForest 씬은 지워집니다. TestSence는 그대로예요.)", "새로 만들기", "취소"))
        {
            return;
        }

        try
        {
            EditorUtility.DisplayProgressBar("Puzzle Forest", "TestSence 복사", 0f);
            AssetDatabase.DeleteAsset(TargetScenePath);
            AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath);
            _scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

            ConfigurePlayer();
            CreateMaterials();

            EditorUtility.DisplayProgressBar("Puzzle Forest", "맵 1 정리 + 바닥", 0.1f);
            PrepareMap1();
            ClearOldCollidersOnPath();
            Transform world = CreateWorldPath();

            EditorUtility.DisplayProgressBar("Puzzle Forest", "맵 2", 0.3f);
            BuildMap2(MapStart(2));

            EditorUtility.DisplayProgressBar("Puzzle Forest", "맵 3", 0.5f);
            BuildMap3(MapStart(3));

            EditorUtility.DisplayProgressBar("Puzzle Forest", "맵 4", 0.7f);
            BuildMap4(MapStart(4));

            EditorUtility.DisplayProgressBar("Puzzle Forest", "맵 5", 0.9f);
            BuildMap5(MapStart(5));

            // 맵 1 → 맵 2 경계의 문 (맵 1은 퍼즐이 없어서 처음부터 열림)
            CreateExit(world, Map1End, FloorY, true, RuneGate, false, false);

            AddToBuildSettings();
            EditorSceneManager.MarkSceneDirty(_scene);
            EditorSceneManager.SaveScene(_scene);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log("[PuzzleForest] " + TargetScenePath + " 를 만들었어요. 맵 2~5 시작 x: "
                  + MapStart(2) + ", " + MapStart(3) + ", " + MapStart(4) + ", " + MapStart(5) + " / 끝 x: " + WorldEnd);
        EditorUtility.DisplayDialog("Puzzle Forest", "PuzzleForest 씬을 만들었어요.\n\n맵 1 → 5가 한 줄로 이어져 있고,\n각 맵 끝의 문은 퍼즐을 풀면 빛나며 열립니다.", "확인");
    }

    // ───────────────────────── 준비 ─────────────────────────

    private static bool CheckLayers()
    {
        _groundLayer = LayerMask.NameToLayer("Ground");
        _boxLayer = LayerMask.NameToLayer("Box");
        _playerLayer = LayerMask.NameToLayer("Player");

        if (_groundLayer >= 0 && _boxLayer >= 0 && _playerLayer >= 0)
        {
            return true;
        }

        EditorUtility.DisplayDialog("Puzzle Forest", "Ground, Box, Player 레이어가 모두 있어야 해요.\n(Project Settings > Tags and Layers)", "확인");
        return false;
    }

    private static bool CheckSource()
    {
        if (File.Exists(SourceScenePath))
        {
            return true;
        }

        EditorUtility.DisplayDialog("Puzzle Forest", SourceScenePath + " 를 찾지 못했어요.", "확인");
        return false;
    }

    private static void ConfigurePlayer()
    {
        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("[PuzzleForest] TestSence에 PlayerController가 없어요.");
            return;
        }

        // 박스 위에 서도 땅으로 인식하고, 박스를 밀 수 있도록
        var serialized = new SerializedObject(player);
        serialized.FindProperty("_groundLayer").intValue = (1 << _groundLayer) | (1 << _boxLayer);
        serialized.FindProperty("_pushableLayer").intValue = 1 << _boxLayer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Puzzle");
        }

        string path = MaterialFolder + "/PressurePlate.mat";
        _plateMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (_plateMaterial == null)
        {
            _plateMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(_plateMaterial, path);
        }

        _plateMaterial.SetColor("_BaseColor", new Color(0.32f, 0.36f, 0.34f));
        _plateMaterial.EnableKeyword("_EMISSION");
        _plateMaterial.SetColor("_EmissionColor", new Color(0.25f, 0.5f, 0.45f));
        _plateMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(_plateMaterial);
    }

    private static void AddToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == TargetScenePath))
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(TargetScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ───────────────────────── 맵 1 정리 + 이어지는 바닥 ─────────────────────────

    private static void PrepareMap1()
    {
        GameObject middle = _scene.GetRootGameObjects().FirstOrDefault(g => g.name == Map1RootName);
        if (middle == null)
        {
            Debug.LogWarning("[PuzzleForest] " + Map1RootName + " 를 찾지 못해서 맵 1은 그대로 두었어요.");
            return;
        }

        // 1) 이어지는 구간(x 73 ~ 81)의 지형을 Ground 레이어로 → 플레이어가 서고 걸을 수 있게
        foreach (EdgeCollider2D edge in middle.GetComponentsInChildren<EdgeCollider2D>(true))
        {
            float centerX = edge.bounds.center.x;
            if (edge.name == "WalkableSurface" && centerX >= Map1WalkableFrom && centerX <= Map1TrimX)
            {
                edge.gameObject.layer = _groundLayer;
            }
        }

        // 2) 맵 2와 겹치는 오른쪽 부분(지형, 소품, 폭포, 배경판 세 번째 장, 출입구)을 지움
        string[] groups = { "Terrain", "Props", "Waterfalls", "Background", "Foreground" };
        var targets = new List<GameObject>();
        foreach (Transform group in middle.GetComponentsInChildren<Transform>(true).Where(t => groups.Contains(t.name)))
        {
            foreach (Transform child in group)
            {
                if (GetCenterX(child.gameObject) > Map1TrimX)
                {
                    targets.Add(child.gameObject);
                }
            }
        }

        targets.AddRange(middle.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Plate_2" || t.name == "PlateSeam_2" || t.name == "GateRight")
            .Select(t => t.gameObject));

        DestroyAll(targets);

        // 3) 맵 1 오른쪽 끝을 막던 벽이 있으면 끔
        foreach (Collider2D col in middle.GetComponentsInChildren<Collider2D>(true))
        {
            if (col.bounds.center.x > Map1TrimX)
            {
                col.enabled = false;
            }
        }
    }

    private static void ClearOldCollidersOnPath()
    {
        // 새 길(x 79.8 ~) 위에 남아 있는 예전 맵의 충돌체(벽, 지형)가 길을 막지 않도록 끔. 트리거(채집물 등)는 그대로.
        foreach (Collider2D col in Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (col.isTrigger || col.GetComponentInParent<PlayerController>() != null)
            {
                continue;
            }

            if (col.bounds.size.sqrMagnitude > 0f && col.bounds.min.x > FloorStartX - 0.1f)
            {
                col.enabled = false;
            }
        }
    }

    private static Transform CreateWorldPath()
    {
        var world = new GameObject("PuzzlePath").transform;

        // 맵 1 끝(x 79.8)부터 맵 5 끝까지 하나로 이어진 평평한 바닥 + 맨 끝 벽
        CreateSolid(world, "FloorCollider", new Rect(FloorStartX, FloorY - 3f, WorldEnd - FloorStartX + 1f, 3f));
        CreateSolid(world, "WallEnd", new Rect(WorldEnd, FloorY - 3f, 1.5f, 30f));

        // 바닥 그림: 04_DeepForest의 긴 바닥(30.56)을 번갈아 뒤집어 깐다 (충돌은 위의 평평한 바닥만 사용)
        const float pieceWidth = 30.56f;
        int index = 0;
        for (float x = FloorStartX + pieceWidth * 0.5f; x - pieceWidth * 0.5f < WorldEnd; x += pieceWidth, index++)
        {
            GameObject piece = AddForestPrefab(world, "04_DeepForest/Terrain/Ground_Floor.prefab", new Vector3(x, FloorY - 0.455f, 0f));
            DisableColliders(piece);
            if (piece != null && index % 2 == 1)
            {
                piece.transform.localScale = new Vector3(-1f, 1f, 1f);
            }
        }

        GameObject cliff = AddForestPrefab(world, "04_DeepForest/Terrain/Cliff_R.prefab", new Vector3(WorldEnd + 2.6f, FloorY + 0.4f, 0f));
        DisableColliders(cliff);
        if (cliff != null)
        {
            cliff.transform.localScale = new Vector3(1f, 1.6f, 1f);
        }

        return world;
    }

    // ───────────────────────── 맵 2: 박스 + 발판 ─────────────────────────

    private static void BuildMap2(float start)
    {
        Transform root = CreateMapRoot("Map2_MossPlate", "02_ShallowForest", start);

        AddProp(root, "02_ShallowForest/Props/Mushroom_Pair_L.prefab", start + 8f, 1.2f);
        AddProp(root, "02_ShallowForest/Props/Flowers_L.prefab", start + 22f, 1.4f);
        AddItem(root, "Flower1_4.prefab", start + 6f, false);
        AddItem(root, "Carrot.prefab", start + 12f, false);
        AddItem(root, "MushroomYellow.prefab", start + 18f, false);

        PushableBox2D box = CreateBox(root, start + 34f);
        PressurePlate plate = CreatePlate(root, start + 48f);

        ExitPortal exit = CreateExit(root, start + MapWidth, FloorY, false, RuneGate, true, false);
        exit.gameObject.AddComponent<PuzzleGate>().Setup(new PuzzleCondition[] { plate }, exit);

        AddRoom(root, "2. 숲 가장자리 · 이끼 발판", start, new[] { box });
    }

    // ───────────────────────── 맵 3: 박스 디딤대 ─────────────────────────

    private static void BuildMap3(float start)
    {
        Transform root = CreateMapRoot("Map3_RootLedge", "04_DeepForest", start);

        AddProp(root, "04_DeepForest/Props/Glow_Mushroom_Floor.prefab", start + 7f, 1.2f);
        AddProp(root, "04_DeepForest/Props/Glow_Mushrooms_R.prefab", start + 24f, 1.5f);
        AddItem(root, "MushroomYellow.prefab", start + 10f, false);
        AddItem(root, "Carrot.prefab", start + 16f, false);

        PushableBox2D box = CreateBox(root, start + 32f);

        // 맵 끝의 높은 턱: 박스를 턱 밑까지 밀고 밟아야만 올라가서 다음 맵으로 넘어갈 수 있다
        float ledgeLeft = start + 50f;
        float ledgeRight = start + MapWidth;
        float ledgeTop = FloorY + LedgeHeight;
        CreateSolid(root, "LedgeCollider", new Rect(ledgeLeft, FloorY - 3f, ledgeRight - ledgeLeft, LedgeHeight + 3f));

        GameObject ledgeVisual = AddForestPrefab(root, "04_DeepForest/Terrain/Root_Ledge.prefab",
            new Vector3((ledgeLeft + ledgeRight) * 0.5f + 0.3f, ledgeTop - 1.2f, 0f));
        if (ledgeVisual != null)
        {
            ledgeVisual.transform.localScale = new Vector3(1.38f, 1.1f, 1f);
            DisableColliders(ledgeVisual);
        }

        AddItem(root, "Flower3_2.prefab", start + 53f, true, ledgeTop);                 // 턱 위 보상
        CreateExit(root, ledgeRight - 1.5f, ledgeTop, true, RuneGate, false, false);   // 위치 자체가 퍼즐이라 처음부터 열림

        AddRoom(root, "3. 깊은 숲 · 높은 뿌리 턱", start, new[] { box });
    }

    // ───────────────────────── 맵 4: 채집 열쇠 + 몬스터 ─────────────────────────

    private static void BuildMap4(float start)
    {
        Transform root = CreateMapRoot("Map4_GlowKey", "05_CrimsonForest", start);

        AddProp(root, "05_CrimsonForest/Props/Red_Mushroom.prefab", start + 8f, 1.2f);
        AddProp(root, "05_CrimsonForest/Props/Crystal_Mid.prefab", start + 44f, 1.8f);
        AddItem(root, "Carrot.prefab", start + 6f, false);
        AddItem(root, "Flower1_4.prefab", start + 14f, false);

        CreateMonsterEvent(root, start + 26f, start + 36f);

        GameObject key = AddItem(root, "MushroomYellow.prefab", start + 50f, true);
        if (key != null)
        {
            key.name = "GlowKey_Mushroom";
        }

        ExitPortal exit = CreateExit(root, start + MapWidth, FloorY, false, RuneGate, true, false);
        var keyCondition = exit.gameObject.AddComponent<GatherKeyCondition>();
        keyCondition.Setup(key);
        exit.gameObject.AddComponent<PuzzleGate>().Setup(new PuzzleCondition[] { keyCondition }, exit);

        AddRoom(root, "4. 붉은 숲 · 빛나는 버섯", start, new PushableBox2D[0]);
    }

    private static void CreateMonsterEvent(Transform root, float triggerX, float monsterX)
    {
        var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPath);
        if (monsterPrefab == null)
        {
            Debug.LogWarning("[PuzzleForest] 몬스터 프리팹이 없어요: " + MonsterPath);
            return;
        }

        var monster = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab, _scene);
        monster.transform.SetParent(root, true);
        monster.transform.position = new Vector3(monsterX, FloorY, 2.5f);      // 길 뒤쪽(배경 쪽)에 서 있음
        monster.transform.rotation = Quaternion.Euler(0f, 180f, 0f);           // 카메라 쪽을 봄
        PlaceOnFloor(monster, FloorY);
        MonsterState state = monster.AddComponent<MonsterState>();

        var trigger = new GameObject("MonsterTrigger");
        trigger.transform.SetParent(root, false);
        trigger.transform.position = new Vector3(triggerX, FloorY + 2f, 0f);
        var box = trigger.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(1f, 4f);

        var turret = trigger.AddComponent<Turret>();
        var serialized = new SerializedObject(turret);
        serialized.FindProperty("_playerLayer").intValue = 1 << _playerLayer;
        serialized.FindProperty("_monsterState").objectReferenceValue = state;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ───────────────────────── 맵 5: 종합 (박스 2 + 발판 2) ─────────────────────────

    private static void BuildMap5(float start)
    {
        Transform root = CreateMapRoot("Map5_ElvenGate", "06_ElvenForest", start);

        AddProp(root, "04_DeepForest/Props/Glow_Mushrooms_L.prefab", start + 6f, 1.6f);
        AddProp(root, "04_DeepForest/Props/Glow_Mushroom_Floor.prefab", start + 20f, 1.2f);
        AddItem(root, "Flower3_2.prefab", start + 10f, false);

        // 두 박스가 일렬로 있어서, 앞 박스(B)를 먼저 멀리 밀어야 뒤 박스(A)를 쓸 수 있다
        PushableBox2D boxA = CreateBox(root, start + 30f);
        PushableBox2D boxB = CreateBox(root, start + 36f);
        PressurePlate plateA = CreatePlate(root, start + 45f);
        PressurePlate plateB = CreatePlate(root, start + 52f);

        ExitPortal exit = CreateExit(root, start + MapWidth - 1.5f, FloorY, false, ElvenGate, true, true);
        exit.gameObject.AddComponent<PuzzleGate>().Setup(new PuzzleCondition[] { plateA, plateB }, exit);

        AddRoom(root, "5. 엘프의 숲 입구", start, new[] { boxA, boxB });
    }

    // ───────────────────────── 맵 공통 ─────────────────────────

    private static Transform CreateMapRoot(string name, string region, float start)
    {
        ImportRegionBackground(region, start);

        var root = new GameObject(name).transform;
        root.position = new Vector3(start, 0f, 0f);
        return root;
    }

    private static void AddRoom(Transform root, string title, float start, PushableBox2D[] boxes)
    {
        var spawn = new GameObject("RespawnPoint").transform;
        spawn.SetParent(root, false);
        spawn.position = new Vector3(start + 2f, FloorY + 0.1f, 0f);

        root.gameObject.AddComponent<PuzzleRoom>().Setup(title, start, start + MapWidth, FloorY - 6f, spawn, boxes);
    }

    private static void ImportRegionBackground(string region, float start)
    {
        string path = RegionScenePath + region + ".unity";
        if (!File.Exists(path))
        {
            Debug.LogWarning("[PuzzleForest] 지역 씬이 없어요: " + path);
            return;
        }

        Scene source = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        GameObject regionRoot = source.GetRootGameObjects().FirstOrDefault(g => g.name == region);
        if (regionRoot == null)
        {
            Debug.LogWarning("[PuzzleForest] " + region + " 씬에서 " + region + " 오브젝트를 찾지 못했어요.");
            EditorSceneManager.CloseScene(source, true);
            return;
        }

        SceneManager.MoveGameObjectToScene(regionRoot, _scene);
        EditorSceneManager.CloseScene(source, true);                // 원본 지역 씬은 저장하지 않고 닫음

        StripRegion(regionRoot);
        regionRoot.name = region + "_Background";
        regionRoot.transform.position = new Vector3(start, FloorY - RegionGroundY, 0f);   // 배경 속 바닥을 새 바닥 높이에 맞춤
    }

    private static void StripRegion(GameObject regionRoot)
    {
        // 원래 지형은 높이가 제각각이라 퍼즐과 안 맞으므로 빼고, 배경판 두 장·배경 나무·앞쪽 풀만 남긴다
        string[] removeNames =
        {
            "Terrain", "Props", "Waterfalls", "Bounds", "GateLeft", "GateRight", "WallLeft", "WallRight",
            "Player", "Main Camera", "Directional Light", "Plate_2", "PlateSeam_2"
        };

        var targets = regionRoot.GetComponentsInChildren<Transform>(true)
            .Where(t => t != regionRoot.transform && removeNames.Contains(t.name))
            .Select(t => t.gameObject)
            .ToList();

        // 배경 나무·앞쪽 풀 중 맵 너비(배경판 두 장) 밖에 있는 것은 다음 맵과 겹치므로 지움
        foreach (Transform group in regionRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Background" || t.name == "Foreground"))
        {
            foreach (Transform child in group)
            {
                if (child.position.x > MapWidth + 2f)
                {
                    targets.Add(child.gameObject);
                }
            }
        }

        DestroyAll(targets);

        foreach (Camera cam in regionRoot.GetComponentsInChildren<Camera>(true))
        {
            cam.gameObject.SetActive(false);
        }

        foreach (Light light in regionRoot.GetComponentsInChildren<Light>(true).Where(l => l.type == LightType.Directional))
        {
            light.enabled = false;
        }

        DisableColliders(regionRoot);
    }

    // ───────────────────────── 퍼즐 부품 ─────────────────────────

    private static PushableBox2D CreateBox(Transform root, float x)
    {
        var go = new GameObject("PushBox") { layer = _boxLayer };
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(x, FloorY + 0.5f, 0f);

        PushableBox2D box = go.AddComponent<PushableBox2D>();              // Rigidbody2D, BoxCollider2D 자동 추가
        go.GetComponent<BoxCollider2D>().size = Vector2.one;
        var body = go.GetComponent<Rigidbody2D>();
        body.mass = 2f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        var serialized = new SerializedObject(box);
        serialized.FindProperty("_blockLayer").intValue = (1 << _groundLayer) | (1 << _boxLayer);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoxVisualPath);
        if (visualPrefab != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, _scene);
            visual.transform.SetParent(go.transform, false);
            Disable3DColliders(visual);
            FitInside(visual, go.transform.position, Vector3.one);
        }
        else
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
            cube.transform.SetParent(go.transform, false);
        }

        return box;
    }

    private static PressurePlate CreatePlate(Transform root, float x)
    {
        var go = new GameObject("PressurePlate");
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(x, FloorY, 0f);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "PlateVisual";
        Object.DestroyImmediate(visual.GetComponent<BoxCollider>());
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        visual.transform.localScale = new Vector3(1.3f, 0.12f, 1.4f);
        var renderer = visual.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _plateMaterial;

        Light light = CreatePointLight(go.transform, new Vector3(0f, 0.6f, -0.8f), ExitLightColor, 2.5f, 3.5f);
        light.enabled = false;

        PressurePlate plate = go.AddComponent<PressurePlate>();
        plate.Setup(1 << _boxLayer, renderer, light);
        return plate;
    }

    /// <param name="hasBarrier">true면 열리기 전까지 보이지 않는 벽이 길을 막는다</param>
    /// <param name="isFinal">true면 들어갔을 때 클리어 문구를 띄운다</param>
    private static ExitPortal CreateExit(Transform root, float x, float groundY, bool startOpen, string gatePrefab, bool hasBarrier, bool isFinal)
    {
        var go = new GameObject(isFinal ? "FinalGate" : "GlowGate");
        go.transform.SetParent(root, true);
        go.transform.position = new Vector3(x, groundY, 0f);

        var trigger = go.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.4f, 2.6f);
        trigger.offset = new Vector2(0f, 1.3f);

        Collider2D barrier = null;
        if (hasBarrier)
        {
            var wall = new GameObject("Barrier") { layer = _groundLayer };
            wall.transform.SetParent(go.transform, false);
            wall.transform.localPosition = new Vector3(isFinal ? 0.8f : 0f, 15f, 0f);
            var wallBox = wall.AddComponent<BoxCollider2D>();
            wallBox.size = new Vector2(0.5f, 30f);
            barrier = wallBox;
        }

        GameObject gate = AddForestPrefab(go.transform, gatePrefab, go.transform.position + new Vector3(0f, 0f, 0.8f));
        if (gate != null)
        {
            DisableColliders(gate);
            ScaleToHeight(gate, 3.6f);
            PlaceOnFloor(gate, groundY);
        }

        Light light = CreatePointLight(go.transform, new Vector3(0f, 1.6f, -1.2f), ExitLightColor, 3f, 7f);

        GameObject effect = null;
        var effectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExitEffectPath);
        if (effectPrefab != null)
        {
            effect = (GameObject)PrefabUtility.InstantiatePrefab(effectPrefab, _scene);
            effect.transform.SetParent(go.transform, false);
            effect.transform.localPosition = new Vector3(0f, 0f, 0.2f);
        }

        ExitPortal exit = go.AddComponent<ExitPortal>();
        exit.Setup(1 << _playerLayer, barrier, isFinal, light, effect, startOpen);
        return exit;
    }

    private static GameObject AddItem(Transform root, string prefabName, float x, bool glow, float groundY = FloorY)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath + prefabName);
        if (prefab == null)
        {
            Debug.LogWarning("[PuzzleForest] 채집물 프리팹이 없어요: " + prefabName);
            return null;
        }

        var item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
        item.transform.SetParent(root, true);
        item.transform.position = new Vector3(x, groundY, -0.3f);
        PlaceOnFloor(item, groundY);

        if (glow)
        {
            CreatePointLight(item.transform, new Vector3(0f, 0.6f, -0.6f), KeyLightColor, 2.5f, 3f, true);
        }

        return item;
    }

    private static void AddProp(Transform root, string prefabPath, float x, float z)
    {
        GameObject prop = AddForestPrefab(root, prefabPath, new Vector3(x, FloorY, z));
        if (prop == null)
        {
            return;
        }

        DisableColliders(prop);
        PlaceOnFloor(prop, FloorY - 0.05f);
    }

    private static void CreateSolid(Transform root, string name, Rect rect)
    {
        var go = new GameObject(name) { layer = _groundLayer };
        go.transform.SetParent(root, false);
        go.transform.position = new Vector3(rect.center.x, rect.center.y, 0f);
        var box = go.AddComponent<BoxCollider2D>();
        box.size = rect.size;
    }

    private static Light CreatePointLight(Transform parent, Vector3 localPosition, Color color, float intensity, float range, bool worldScaleFix = false)
    {
        var go = new GameObject("Glow Light");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        if (worldScaleFix)
        {
            // 작게 줄인 채집물 아래에 붙여도 원래 크기로 보이도록
            Vector3 scale = parent.lossyScale;
            go.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            go.transform.localPosition = new Vector3(localPosition.x / scale.x, localPosition.y / scale.y, localPosition.z / scale.z);
        }

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    // ───────────────────────── 도우미 ─────────────────────────

    private static GameObject AddForestPrefab(Transform parent, string relativePath, Vector3 worldPosition)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForestPrefabPath + relativePath);
        if (prefab == null)
        {
            Debug.LogWarning("[PuzzleForest] 프리팹이 없어요: " + relativePath);
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _scene);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPosition;
        return go;
    }

    private static void DestroyAll(List<GameObject> targets)
    {
        foreach (GameObject target in targets.Distinct())
        {
            if (target == null)
            {
                continue;                                             // 부모와 함께 이미 지워짐
            }

            bool canDestroy = !PrefabUtility.IsPartOfPrefabInstance(target) || PrefabUtility.IsOutermostPrefabInstanceRoot(target);
            if (canDestroy)
            {
                Object.DestroyImmediate(target);
            }
            else
            {
                target.SetActive(false);                              // 프리팹 안쪽 오브젝트는 지울 수 없어서 끔
            }
        }
    }

    private static float GetCenterX(GameObject go)
    {
        return TryGetBounds(go, out Bounds bounds) ? bounds.center.x : go.transform.position.x;
    }

    private static void DisableColliders(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        foreach (Collider2D col in go.GetComponentsInChildren<Collider2D>(true))
        {
            col.enabled = false;
        }
    }

    private static void Disable3DColliders(GameObject go)
    {
        foreach (Collider col in go.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }
    }

    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer || renderer is SpriteRenderer))
            {
                continue;
            }

            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return found;
    }

    private static void PlaceOnFloor(GameObject go, float floorY)
    {
        if (!TryGetBounds(go, out Bounds bounds))
        {
            return;
        }

        go.transform.position += new Vector3(0f, floorY - bounds.min.y, 0f);
    }

    private static void ScaleToHeight(GameObject go, float height)
    {
        if (!TryGetBounds(go, out Bounds bounds) || bounds.size.y <= 0.001f)
        {
            return;
        }

        go.transform.localScale *= height / bounds.size.y;
    }

    private static void FitInside(GameObject visual, Vector3 center, Vector3 size)
    {
        if (!TryGetBounds(visual, out Bounds bounds))
        {
            return;
        }

        float scale = Mathf.Min(size.x / bounds.size.x, size.y / bounds.size.y);
        visual.transform.localScale *= scale;

        TryGetBounds(visual, out bounds);
        visual.transform.position += center - bounds.center;
    }
}
