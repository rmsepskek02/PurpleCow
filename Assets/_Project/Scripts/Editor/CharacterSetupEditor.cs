#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CharacterSetupEditor
{
    private const string PrefabFolder = "Assets/_Project/Prefabs/Character";
    private const string PrefabPath   = PrefabFolder + "/Character.prefab";

    private const string BodySpritePath   = "Assets/_Project/Sprites/Character/Character_Main_body.png";
    private const string HeadSpritePath   = "Assets/_Project/Sprites/Character/Character_Main_head.png";
    private const string WeaponSpritePath = "Assets/_Project/Sprites/Character/Character_main_weapon.png";

    [MenuItem("PurpleCow/Setup/Character Setup")]
    private static void SetupCharacter()
    {
        EnsurePrefabFolder();
        CreateCharacterPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CharacterSetupEditor] Character Setup 완료.");
    }

    private static void EnsurePrefabFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Character");
    }

    private static void CreateCharacterPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            Debug.Log("[CharacterSetupEditor] Character.prefab 이미 존재, 스킵.");
            return;
        }

        GameObject root = new GameObject("Character");

        // Body: 머리+몸통 고정 파츠의 부모. Head/Weapon 좌표는 Body 로컬 기준이다.
        GameObject body = CreateSpritePart("Body", root.transform, BodySpritePath, Vector2.zero, 0);
        CreateSpritePart("Head", body.transform, HeadSpritePath, new Vector2(0.34f, 0.58f), 1);
        GameObject weapon = CreateSpritePart("Weapon", body.transform, WeaponSpritePath, new Vector2(-0.29f, 0.65f), 2);

        CharacterAimController aimController = root.AddComponent<CharacterAimController>();
        SerializedObject so = new SerializedObject(aimController);
        so.FindProperty("_weaponTransform").objectReferenceValue = weapon.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("[CharacterSetupEditor] Character.prefab 생성 완료.");
    }

    private static GameObject CreateSpritePart(string name, Transform parent, string spritePath, Vector2 localPosition, int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite != null)
            sr.sprite = sprite;
        else
            Debug.LogWarning($"[CharacterSetupEditor] 스프라이트를 찾을 수 없음: {spritePath}");
        sr.sortingOrder = sortingOrder;

        return go;
    }
}
#endif
