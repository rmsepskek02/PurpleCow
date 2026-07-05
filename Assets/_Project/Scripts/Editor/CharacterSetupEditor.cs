#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CharacterSetupEditor
{
    private const string PREFAB_FOLDER = "Assets/_Project/Prefabs/Character";
    private const string PREFAB_PATH   = PREFAB_FOLDER + "/Character.prefab";
    private const string SPRITE_FOLDER = "Assets/_Project/Sprites/Character";

    [MenuItem("PurpleCow/Setup/Character System Setup")]
    private static void SetupCharacterSystem()
    {
        EnsurePrefabFolder();

        GameObject prefab = CreateCharacterPrefab();
        PlaceCharacterInScene(prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("[CharacterSetupEditor] Character System Setup 완료.");
    }

    // ──────────────────────────────────────────
    //  폴더 생성
    // ──────────────────────────────────────────

    private static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

        if (!AssetDatabase.IsValidFolder(PREFAB_FOLDER))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Character");
    }

    // ──────────────────────────────────────────
    //  Character.prefab 생성
    // ──────────────────────────────────────────

    private static GameObject CreateCharacterPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (existing != null)
        {
            Debug.Log("[CharacterSetupEditor] Character.prefab 이미 존재, 스킵.");
            return existing;
        }

        GameObject root = new GameObject("Character");

        // Body
        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0f, 0f);
        SpriteRenderer bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sprite = LoadSpriteOrWarn("Character_Main_body.png");
        bodySr.sortingOrder = 0;

        // Head
        GameObject head = new GameObject("Head");
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.4f, 0f); // 초기 추정값, 로컬 육안 조정 필요
        SpriteRenderer headSr = head.AddComponent<SpriteRenderer>();
        headSr.sprite = LoadSpriteOrWarn("Character_Main_head.png");
        headSr.sortingOrder = 2;

        // WeaponPivot -> Weapon
        GameObject weaponPivot = new GameObject("WeaponPivot");
        weaponPivot.transform.SetParent(root.transform, false);
        weaponPivot.transform.localPosition = new Vector3(0.2f, 0.15f, 0f); // 초기 추정값, 로컬 육안 조정 필요

        GameObject weapon = new GameObject("Weapon");
        weapon.transform.SetParent(weaponPivot.transform, false);
        weapon.transform.localPosition = new Vector3(0f, 0.4f, 0f); // 초기 추정값, 로컬 육안 조정 필요
        SpriteRenderer weaponSr = weapon.AddComponent<SpriteRenderer>();
        weaponSr.sprite = LoadSpriteOrWarn("Character_main_weapon.png");
        weaponSr.sortingOrder = 1;

        // CharacterAimView 부착 및 참조 연결
        CharacterAimView aimView = root.AddComponent<CharacterAimView>();
        SerializedObject so = new SerializedObject(aimView);
        so.FindProperty("_bodySpriteRenderer").objectReferenceValue = bodySr;
        so.FindProperty("_headSpriteRenderer").objectReferenceValue = headSr;
        so.FindProperty("_headTransform").objectReferenceValue      = head.transform;
        so.FindProperty("_weaponPivot").objectReferenceValue        = weaponPivot.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        Object.DestroyImmediate(root);

        Debug.Log("[CharacterSetupEditor] Character.prefab 생성 완료.");
        return savedPrefab;
    }

    private static Sprite LoadSpriteOrWarn(string fileName)
    {
        string path = $"{SPRITE_FOLDER}/{fileName}";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[CharacterSetupEditor] 스프라이트를 찾을 수 없음: {path}");
        return sprite;
    }

    // ──────────────────────────────────────────
    //  씬 배치 (LaunchPoint 자식)
    // ──────────────────────────────────────────

    private static void PlaceCharacterInScene(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[CharacterSetupEditor] Character.prefab이 없어 씬 배치를 건너뜁니다.");
            return;
        }

        GameObject launchPointObj = GameObject.Find("LaunchPoint");
        if (launchPointObj == null)
        {
            Debug.LogWarning("[CharacterSetupEditor] LaunchPoint 오브젝트를 찾을 수 없어 씬 배치를 건너뜁니다.");
            return;
        }

        if (launchPointObj.transform.Find("Character") != null)
        {
            Debug.Log("[CharacterSetupEditor] LaunchPoint 자식에 Character가 이미 존재, 스킵.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(launchPointObj.transform, false);
        instance.transform.localPosition = Vector3.zero;

        Debug.Log("[CharacterSetupEditor] Character를 LaunchPoint 자식으로 배치 완료.");
    }
}
#endif
