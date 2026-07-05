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

        // Character(루트) — 조준 각도 부호에 따라 좌우 반전(localScale.x)을 담당.
        GameObject root = new GameObject("Character");

        // Body: 고정 파츠(회전 없음). Head/WeaponPivot 좌표는 Character 루트(=Body 위치) 기준이다.
        CreateSpritePart("Body", root.transform, BodySpritePath, Vector2.zero, 0);

        // Head: Character의 직접 자식(Body 자식 아님). 조준 각도를 ±10도로 좁게 클램프해 제자리 tilt만 한다.
        GameObject head = CreateSpritePart("Head", root.transform, HeadSpritePath, new Vector2(0.34f, 0.58f), 1);

        // WeaponPivot: 회전축 역할을 하는 빈 오브젝트. 원본 게임 레퍼런스를 재분석한 결과
        // 무기의 손잡이 쪽은 캐릭터 어깨 근처에 거의 고정되고 갈고리 끝만 호를 그리므로,
        // 회전축을 무기 스프라이트 자신이 아니라 이 빈 오브젝트로 분리했다.
        GameObject weaponPivot = new GameObject("WeaponPivot");
        weaponPivot.transform.SetParent(root.transform, false);
        weaponPivot.transform.localPosition = new Vector3(-0.29f, 0.65f, 0f);

        // Weapon: WeaponPivot의 자식. Character_main_weapon.png의 커스텀 spritePivot(0.18, 0.29,
        // 손잡이 부근)이 이미 "축 → 콘텐츠" 오프셋을 담당하므로 Weapon 자신의 로컬 오프셋은 0으로 둔다.
        // Scene 뷰에서 회전시켜보며 갈고리 끝이 자연스러운 호를 그리는지 시각 검증이 필요하다.
        GameObject weapon = CreateSpritePart("Weapon", weaponPivot.transform, WeaponSpritePath, Vector2.zero, 2);

        CharacterAimController aimController = root.AddComponent<CharacterAimController>();
        SerializedObject so = new SerializedObject(aimController);
        so.FindProperty("_weaponPivot").objectReferenceValue = weaponPivot.transform;
        so.FindProperty("_head").objectReferenceValue = head.transform;
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
