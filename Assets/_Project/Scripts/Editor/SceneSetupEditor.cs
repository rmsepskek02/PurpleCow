#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

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
        Debug.LogWarning("[SceneSetupEditor] Ball.prefab: CircleCollider2D의 PhysicsMaterial2D는 수동으로 연결하세요.");

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

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[SceneSetupEditor] {name}.prefab 생성 완료.");
        }
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

        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.LogWarning("[SceneSetupEditor] BallLauncher._launchPoint는 수동으로 연결하세요.");
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
