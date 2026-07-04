#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Character 시각 오브젝트 생성, 초기 위치 설정, WallFitter 연동을 한 번에 처리하는 에디터 스크립트.
// SceneSetupEditor.cs는 이미 안정화된 공용 자동화 스크립트이므로 이번 재설계로 새로 필요해진
// Character/WallFitter 관련 배선은 이 파일에서 별도로 처리한다.
public static class CharacterLaunchOrbitSetupEditor
{
    [MenuItem("PurpleCow/Setup/Character LaunchPoint Orbit Setup")]
    private static void SetupCharacterLaunchOrbit()
    {
        Transform character = SetupCharacterVisual();
        if (character == null)
            return;

        character.localPosition = new Vector3(0f, -8f, 0f);
        Debug.Log("[CharacterLaunchOrbitSetupEditor] Character 초기 위치 설정 완료.");

        SetupWallFitter(character);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    // ──────────────────────────────────────────
    //  Character 시각 오브젝트 생성
    // ──────────────────────────────────────────

    private static Transform SetupCharacterVisual()
    {
        GameObject launcherObj = GameObject.Find("BallLauncher");
        if (launcherObj == null)
        {
            Debug.LogWarning("[CharacterLaunchOrbitSetupEditor] BallLauncher 오브젝트를 찾을 수 없어 Character 배치를 건너뜁니다.");
            return null;
        }

        Transform characterTransform = launcherObj.transform.Find("Character");
        GameObject characterObj;
        if (characterTransform == null)
        {
            characterObj = new GameObject("Character");
            characterObj.transform.SetParent(launcherObj.transform);
            Debug.Log("[CharacterLaunchOrbitSetupEditor] Character 생성 완료.");
        }
        else
        {
            characterObj = characterTransform.gameObject;
        }

        SpriteRenderer bodyRenderer   = CreateCharacterPart(characterObj.transform, "Body",   "Assets/_Project/Sprites/Character/Character_Main_body.png",  0, new Vector3(0.42f, -0.75f, 0f));
        SpriteRenderer headRenderer   = CreateCharacterPart(characterObj.transform, "Head",   "Assets/_Project/Sprites/Character/Character_Main_head.png",  1, new Vector3(0.51f, -0.23f, 0f));
        SpriteRenderer weaponRenderer = CreateCharacterPart(characterObj.transform, "Weapon", "Assets/_Project/Sprites/Character/Character_main_weapon.png", 2, new Vector3(-0.177f, -0.36f, 0f));

        CharacterAimController aimController = characterObj.GetComponent<CharacterAimController>();
        if (aimController == null)
            aimController = characterObj.AddComponent<CharacterAimController>();

        SerializedObject so = new SerializedObject(aimController);
        so.FindProperty("_bodyRenderer").objectReferenceValue   = bodyRenderer;
        so.FindProperty("_headRenderer").objectReferenceValue   = headRenderer;
        so.FindProperty("_weaponRenderer").objectReferenceValue = weaponRenderer;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[CharacterLaunchOrbitSetupEditor] Character 시각 오브젝트 배치 완료.");

        return characterObj.transform;
    }

    private static SpriteRenderer CreateCharacterPart(Transform parent, string partName, string spritePath, int sortingOrder, Vector3 localPosition)
    {
        Transform existing = parent.Find(partName);
        GameObject partObj = existing != null ? existing.gameObject : new GameObject(partName);
        if (existing == null)
            partObj.transform.SetParent(parent, false);

        partObj.transform.localPosition = localPosition;

        SpriteRenderer sr = partObj.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = partObj.AddComponent<SpriteRenderer>();

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
            sr.sprite = sprite;
        else
            Debug.LogWarning($"[CharacterLaunchOrbitSetupEditor] {partName} 스프라이트를 찾을 수 없음: {spritePath}");

        sr.sortingOrder = sortingOrder;

        return sr;
    }

    // ──────────────────────────────────────────
    //  Main Camera WallFitter 연동
    // ──────────────────────────────────────────

    private static void SetupWallFitter(Transform character)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[CharacterLaunchOrbitSetupEditor] Main Camera를 찾을 수 없어 WallFitter 연동을 건너뜁니다.");
            return;
        }

        WallFitter fitter = mainCamera.GetComponent<WallFitter>();
        if (fitter == null)
            fitter = mainCamera.gameObject.AddComponent<WallFitter>();

        GameObject background = GameObject.Find("Background");
        SpriteRenderer backgroundSr = null;
        if (background != null)
            backgroundSr = background.GetComponent<SpriteRenderer>();
        else
            Debug.LogWarning("[CharacterLaunchOrbitSetupEditor] Background 오브젝트를 찾을 수 없어 WallFitter._backgroundSpriteRenderer 연결을 건너뜁니다.");

        Transform wallLeft = FindTransformOrWarn("Wall_Left");
        Transform wallRight = FindTransformOrWarn("Wall_Right");
        Transform wallTop = FindTransformOrWarn("Wall_Top");
        Transform ground = FindTransformOrWarn("Ground");

        SerializedObject so = new SerializedObject(fitter);
        so.FindProperty("_targetCamera").objectReferenceValue = mainCamera;
        so.FindProperty("_backgroundSpriteRenderer").objectReferenceValue = backgroundSr;
        so.FindProperty("_wallLeft").objectReferenceValue = wallLeft;
        so.FindProperty("_wallRight").objectReferenceValue = wallRight;
        so.FindProperty("_wallTop").objectReferenceValue = wallTop;
        so.FindProperty("_ground").objectReferenceValue = ground;
        so.FindProperty("_nativeLeftX").floatValue = -6.5f;
        so.FindProperty("_nativeRightX").floatValue = 6.3f;
        so.FindProperty("_nativeTopY").floatValue = 6.0f;
        so.FindProperty("_nativeBottomY").floatValue = -6.5f;
        so.FindProperty("_zoomFactor").floatValue = 1.3f;
        so.FindProperty("_character").objectReferenceValue = character;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[CharacterLaunchOrbitSetupEditor] WallFitter 연동 완료.");
    }

    private static Transform FindTransformOrWarn(string objName)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null)
        {
            Debug.LogWarning($"[CharacterLaunchOrbitSetupEditor] {objName} 오브젝트를 찾을 수 없어 WallFitter 참조 연결을 건너뜁니다.");
            return null;
        }
        return go.transform;
    }
}
#endif
