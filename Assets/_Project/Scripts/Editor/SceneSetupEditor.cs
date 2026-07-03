#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SceneSetupEditor
{
    [MenuItem("PurpleCow/Setup/Scene Setup")]
    private static void SetupScene()
    {
        EnsurePrefabFolders();

        Ball ballPrefab = Step1_CreateBallPrefab();
        Step2_CreateMonsterPrefabs();
        Step3_CreateBlockPrefabs();
        Step4_PlaceBackground();
        Step5_PlaceWallsAndGround();
        Step6_PlaceManagers();
        Step7_ConnectBallLauncherRefs(ballPrefab);
        Step8_ConnectBallPrefabRefs();
        Step9_ConnectWaveManagerRefs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[SceneSetupEditor] Scene Setup 완료.");
    }

    // ──────────────────────────────────────────
    //  폴더 생성
    // ──────────────────────────────────────────

    private static void EnsurePrefabFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Ball"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Ball");

        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Monster"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Monster");
    }

    // ──────────────────────────────────────────
    //  Step 1. Ball 프리팹 생성
    // ──────────────────────────────────────────

    private static Ball Step1_CreateBallPrefab()
    {
        const string prefabPath = "Assets/_Project/Prefabs/Ball/Ball.prefab";

        Ball existing = AssetDatabase.LoadAssetAtPath<Ball>(prefabPath);
        if (existing != null)
        {
            Debug.Log("[SceneSetupEditor] Ball.prefab 이미 존재, 스킵.");
            return existing;
        }

        GameObject go = new GameObject("Ball");

        // SpriteRenderer
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite ballSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Sprites/Ball/Ball_Nomal_Ball.png");
        if (ballSprite != null)
            sr.sprite = ballSprite;
        else
            Debug.LogWarning("[SceneSetupEditor] Ball 스프라이트를 찾을 수 없음: Ball_Nomal_Ball.png");

        // Rigidbody2D
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // CircleCollider2D
        go.AddComponent<CircleCollider2D>();

        // Ball 스크립트
        go.AddComponent<Ball>();

        // Tag
        TrySetTag(go, "Ball");

        Ball ballComponent = PrefabUtility.SaveAsPrefabAsset(go, prefabPath).GetComponent<Ball>();
        Object.DestroyImmediate(go);

        Debug.Log("[SceneSetupEditor] Ball.prefab 생성 완료.");
        return ballComponent;
    }

    // ──────────────────────────────────────────
    //  Step 2. Monster 프리팹 4종 생성
    // ──────────────────────────────────────────

    private static void Step2_CreateMonsterPrefabs()
    {
        string[] names = { "Fluffy", "Spider", "StoneBug", "ForestDeer" };

        foreach (string name in names)
        {
            string prefabPath  = $"Assets/_Project/Prefabs/Monster/{name}.prefab";
            string spritePath  = $"Assets/_Project/Sprites/Monster/{name}.png";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[SceneSetupEditor] {name}.prefab 이미 존재, 스킵.");
                continue;
            }

            GameObject go = new GameObject(name);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
                sr.sprite = sprite;
            else
                Debug.LogWarning($"[SceneSetupEditor] {name} 스프라이트를 찾을 수 없음: {spritePath}");

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.isKinematic  = true;

            go.AddComponent<BoxCollider2D>();
            go.AddComponent<MonsterBase>();

            TrySetTag(go, "Monster");

            AddMonsterHpBar(go);

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[SceneSetupEditor] {name}.prefab 생성 완료.");
        }
    }

    private static void AddMonsterHpBar(GameObject go)
    {
        // 이미 HpBar 자식이 있으면 스킵
        if (go.transform.Find("HpBarCanvas") != null) return;

        // World Space Canvas
        GameObject canvasObj = new GameObject("HpBarCanvas");
        canvasObj.transform.SetParent(go.transform, false);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasObj.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta        = new Vector2(1f, 0.15f);
        canvasRect.localPosition    = new Vector3(0f, 0.6f, 0f);
        canvasRect.localScale       = new Vector3(0.01f, 0.01f, 0.01f);

        // Slider (HP바)
        GameObject sliderObj = new GameObject("HpSlider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 1f;
        slider.wholeNumbers = false;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // MonsterHpBar 컴포넌트
        MonsterHpBar hpBar = canvasObj.AddComponent<MonsterHpBar>();
        SerializedObject so = new SerializedObject(hpBar);
        so.FindProperty("_slider").objectReferenceValue = slider;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("PurpleCow/Setup/Update Monster HpBar")]
    private static void UpdateMonsterHpBars()
    {
        string[] names = { "Fluffy", "Spider", "StoneBug", "ForestDeer" };
        foreach (string name in names)
        {
            string prefabPath = $"Assets/_Project/Prefabs/Monster/{name}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) { Debug.LogWarning($"[SceneSetupEditor] {name}.prefab 없음, 스킵."); continue; }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                AddMonsterHpBar(scope.prefabContentsRoot);
            }
            Debug.Log($"[SceneSetupEditor] {name}.prefab MonsterHpBar 추가 완료.");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ──────────────────────────────────────────
    //  Step 8. Ball.prefab 참조 연결
    // ──────────────────────────────────────────

    [MenuItem("PurpleCow/Setup/Connect Ball Prefab Refs")]
    private static void Step8_ConnectBallPrefabRefs()
    {
        const string prefabPath  = "Assets/_Project/Prefabs/Ball/Ball.prefab";
        const string ballDataPath = "Assets/_Project/Data/BallData.asset";
        const string matPath      = "Assets/_Project/Physics/BallBounce.physicsMaterial2D";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            Debug.LogWarning("[SceneSetupEditor] Ball.prefab 없음. Scene Setup을 먼저 실행하세요.");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            Ball ball = root.GetComponent<Ball>();
            if (ball == null) { Debug.LogWarning("[SceneSetupEditor] Ball 컴포넌트 없음."); return; }

            BallData ballData = AssetDatabase.LoadAssetAtPath<BallData>(ballDataPath);
            SerializedObject ballSo = new SerializedObject(ball);
            if (ballData != null)
                ballSo.FindProperty("_ballData").objectReferenceValue = ballData;
            else
                Debug.LogWarning("[SceneSetupEditor] BallData.asset 없음. Ball System Setup을 먼저 실행하세요.");
            ballSo.ApplyModifiedPropertiesWithoutUndo();

            PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(matPath);
            CircleCollider2D col = root.GetComponent<CircleCollider2D>();
            if (mat != null && col != null)
            {
                SerializedObject colSo = new SerializedObject(col);
                colSo.FindProperty("m_Material").objectReferenceValue = mat;
                colSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (mat == null)
                Debug.LogWarning("[SceneSetupEditor] BallBounce.physicsMaterial2D 없음. Ball System Setup을 먼저 실행하세요.");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[SceneSetupEditor] Ball.prefab 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    //  Step 9. WaveManager 참조 연결
    // ──────────────────────────────────────────

    private static void Step9_ConnectWaveManagerRefs()
    {
        GameObject waveManagerObj = GameObject.Find("WaveManager");
        if (waveManagerObj == null)
        {
            Debug.LogWarning("[SceneSetupEditor] WaveManager 씬 오브젝트 없음.");
            return;
        }

        WaveManager waveManager = waveManagerObj.GetComponent<WaveManager>();
        if (waveManager == null)
        {
            Debug.LogWarning("[SceneSetupEditor] WaveManager 컴포넌트 없음.");
            return;
        }

        SerializedObject so = new SerializedObject(waveManager);

        // _waveTable 연결
        const string waveTablePath = "Assets/_Project/Data/WaveTableData.asset";
        WaveTableData waveTable = AssetDatabase.LoadAssetAtPath<WaveTableData>(waveTablePath);
        if (waveTable != null)
            so.FindProperty("_waveTable").objectReferenceValue = waveTable;
        else
            Debug.LogWarning($"[SceneSetupEditor] {waveTablePath} 없음. Monster System Setup을 먼저 실행하세요.");

        // _monsterPrefab → Fluffy
        MonsterBase fluffyPrefab = AssetDatabase.LoadAssetAtPath<MonsterBase>(
            "Assets/_Project/Prefabs/Monster/Fluffy.prefab");
        if (fluffyPrefab != null)
            so.FindProperty("_monsterPrefab").objectReferenceValue = fluffyPrefab;
        else
            Debug.LogWarning("[SceneSetupEditor] Fluffy.prefab 없음. Scene Setup을 먼저 실행하세요.");

        // _poolParent, _spawnRoot → PoolRoot
        GameObject poolRoot = GameObject.Find("PoolRoot");
        if (poolRoot != null)
        {
            so.FindProperty("_poolParent").objectReferenceValue = poolRoot.transform;
            so.FindProperty("_spawnRoot").objectReferenceValue  = poolRoot.transform;
        }
        else
            Debug.LogWarning("[SceneSetupEditor] PoolRoot 없음.");

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[SceneSetupEditor] WaveManager 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    //  Step 3. Block 프리팹 4종 생성
    // ──────────────────────────────────────────

    private static void Step3_CreateBlockPrefabs()
    {
        string[] names = { "Block_1x1", "Block_1x2", "Block_2x1", "Block_2x2" };

        foreach (string name in names)
        {
            string prefabPath = $"Assets/_Project/Prefabs/Monster/{name}.prefab";
            string spritePath = $"Assets/_Project/Sprites/Monster/{name}.png";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                Debug.Log($"[SceneSetupEditor] {name}.prefab 이미 존재, 스킵.");
                continue;
            }

            GameObject go = new GameObject(name);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
                sr.sprite = sprite;
            else
                Debug.LogWarning($"[SceneSetupEditor] {name} 스프라이트를 찾을 수 없음: {spritePath}");

            go.AddComponent<BoxCollider2D>();
            go.AddComponent<MonsterBase>();

            TrySetTag(go, "Monster");

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[SceneSetupEditor] {name}.prefab 생성 완료.");
        }
    }

    // ──────────────────────────────────────────
    //  Step 4. Background 씬 배치
    // ──────────────────────────────────────────

    private static void Step4_PlaceBackground()
    {
        if (GameObject.Find("Background") != null)
        {
            Debug.Log("[SceneSetupEditor] Background 이미 존재, 스킵.");
            return;
        }

        GameObject go = new GameObject("Background");
        go.transform.position = new Vector3(0f, 0f, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/_Project/Sprites/Background/Background_1_Stage.png");
        if (sprite != null)
            sr.sprite = sprite;
        else
            Debug.LogWarning("[SceneSetupEditor] Background 스프라이트를 찾을 수 없음: Background_1_Stage.png");

        Debug.Log("[SceneSetupEditor] Background 배치 완료.");
    }

    // ──────────────────────────────────────────
    //  Step 5. Wall / Ground 씬 배치
    // ──────────────────────────────────────────

    private static void Step5_PlaceWallsAndGround()
    {
        PlaceColliderObject("Wall_Left",  "Wall",   new Vector3(-5.5f, 0f, 0f),  new Vector2(0.2f, 20f));
        PlaceColliderObject("Wall_Right", "Wall",   new Vector3(5.5f,  0f, 0f),  new Vector2(0.2f, 20f));
        PlaceColliderObject("Ground",     "Ground", new Vector3(0f, -10f, 0f),   new Vector2(12f,  0.2f));
    }

    private static void PlaceColliderObject(string objName, string tag, Vector3 position, Vector2 colliderSize)
    {
        if (GameObject.Find(objName) != null)
        {
            Debug.Log($"[SceneSetupEditor] {objName} 이미 존재, 스킵.");
            return;
        }

        GameObject go = new GameObject(objName);
        go.transform.position = position;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = colliderSize;

        TrySetTag(go, tag);

        Debug.Log($"[SceneSetupEditor] {objName} 배치 완료.");
    }

    // ──────────────────────────────────────────
    //  Step 6. Manager 오브젝트 씬 배치
    // ──────────────────────────────────────────

    private static void Step6_PlaceManagers()
    {
        PlaceManager<GameManager>("GameManager");
        PlaceManager<InputHandler>("InputHandler");
        PlaceManager<BallLauncher>("BallLauncher");
        PlaceManager<WaveManager>("WaveManager");
        PlaceManager<SkillManager>("SkillManager");
        PlaceManager<UIManager>("UIManager");
        PlaceManager<CharacterManager>("CharacterManager");
        PlaceManager<TrajectoryPreview>("TrajectoryPreview");
    }

    private static void PlaceManager<T>(string objName) where T : Component
    {
        if (GameObject.Find(objName) != null)
        {
            Debug.Log($"[SceneSetupEditor] {objName} 이미 존재, 스킵.");
            return;
        }

        GameObject go = new GameObject(objName);
        try
        {
            go.AddComponent<T>();
            Debug.Log($"[SceneSetupEditor] {objName} 배치 완료.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SceneSetupEditor] {objName} AddComponent 실패: {e.Message}");
        }
    }

    // ──────────────────────────────────────────
    //  Step 7. BallLauncher 참조 연결
    // ──────────────────────────────────────────

    private static void Step7_ConnectBallLauncherRefs(Ball ballPrefab)
    {
        GameObject launcherObj = GameObject.Find("BallLauncher");
        if (launcherObj == null)
        {
            Debug.LogWarning("[SceneSetupEditor] BallLauncher 오브젝트를 찾을 수 없어 참조 연결을 건너뜁니다.");
            return;
        }

        BallLauncher launcher = launcherObj.GetComponent<BallLauncher>();
        if (launcher == null)
        {
            Debug.LogWarning("[SceneSetupEditor] BallLauncher 컴포넌트를 찾을 수 없어 참조 연결을 건너뜁니다.");
            return;
        }

        // PoolRoot 빈 GameObject 생성
        GameObject poolRoot = GameObject.Find("PoolRoot");
        if (poolRoot == null)
        {
            poolRoot = new GameObject("PoolRoot");
            Debug.Log("[SceneSetupEditor] PoolRoot 생성 완료.");
        }

        SerializedObject so = new SerializedObject(launcher);

        if (ballPrefab != null)
        {
            so.FindProperty("_ballPrefab").objectReferenceValue = ballPrefab;
        }
        else
        {
            Debug.LogWarning("[SceneSetupEditor] BallLauncher._ballPrefab: Ball 프리팹이 없어 연결을 건너뜁니다.");
        }

        so.FindProperty("_poolParent").objectReferenceValue = poolRoot.transform;

        // Create or find LaunchPoint as child of BallLauncher
        Transform launchPoint = launcherObj.transform.Find("LaunchPoint");
        if (launchPoint == null)
        {
            GameObject lpObj = new GameObject("LaunchPoint");
            lpObj.transform.SetParent(launcherObj.transform);
            lpObj.transform.localPosition = new Vector3(0f, -8f, 0f);
            launchPoint = lpObj.transform;
            Debug.Log("[SceneSetupEditor] LaunchPoint 생성 완료.");
        }
        so.FindProperty("_launchPoint").objectReferenceValue = launchPoint;

        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[SceneSetupEditor] BallLauncher 참조 연결 완료.");
    }

    // ──────────────────────────────────────────
    //  유틸리티
    // ──────────────────────────────────────────

    private static void TrySetTag(GameObject go, string tag)
    {
        try
        {
            go.tag = tag;
        }
        catch
        {
            Debug.LogWarning($"[SceneSetupEditor] Tag '{tag}'가 등록되지 않아 설정을 건너뜁니다. Project Settings > Tags & Layers에서 먼저 등록하세요.");
        }
    }
}
#endif
