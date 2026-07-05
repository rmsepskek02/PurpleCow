#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BallSetupEditor
{
    [MenuItem("PurpleCow/Setup/Ball System Setup")]
    private static void SetupBallSystem()
    {
        AddRequiredTags();
        AddBallLayer();

        // 레이어 등록을 먼저 디스크에 반영해야 LayerMask.NameToLayer("Ball")이
        // 정상적인 값을 반환한다(AssignBallPrefabLayer에서 조회).
        AssetDatabase.SaveAssets();

        AssignBallPrefabLayer();
        CreatePhysicsMaterial();
        CreateBallDataAsset();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BallSetupEditor] Ball System Setup 완료.");
    }

    private static void AddRequiredTags()
    {
        string[] requiredTags = { "Monster", "Wall", "Ground" };
        string[] existingTags = UnityEditorInternal.InternalEditorUtility.tags;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset")
        );
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        foreach (string tag in requiredTags)
        {
            bool found = false;
            foreach (string existing in existingTags)
            {
                if (existing == tag)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                int index = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(index);
                tagsProp.GetArrayElementAtIndex(index).stringValue = tag;
                Debug.Log($"[BallSetupEditor] 태그 추가: {tag}");
            }
        }

        tagManager.ApplyModifiedProperties();
    }

    // 볼끼리 물리적으로 충돌(튕겨나감)하는 것을 막기 위한 전용 Physics2D 레이어를 등록한다.
    // 커스텀 레이어 슬롯(인덱스 8~31) 중 비어있는 첫 슬롯에 "Ball"을 채운다.
    // 이미 등록되어 있으면 스킵한다(멱등성 보장).
    private static void AddBallLayer()
    {
        const string layerName = "Ball";

        if (LayerMask.NameToLayer(layerName) != -1)
        {
            Debug.Log("[BallSetupEditor] Ball 레이어 이미 존재, 스킵.");
            return;
        }

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset")
        );
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        bool assigned = false;
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty layerSlot = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(layerSlot.stringValue))
            {
                layerSlot.stringValue = layerName;
                assigned = true;
                Debug.Log($"[BallSetupEditor] 레이어 추가: {layerName} (slot {i})");
                break;
            }
        }

        if (!assigned)
        {
            Debug.LogError("[BallSetupEditor] 커스텀 레이어 슬롯이 모두 사용 중이어서 Ball 레이어를 추가하지 못했습니다.");
        }

        tagManager.ApplyModifiedProperties();
    }

    // Ball.prefab의 GameObject 레이어를 위에서 등록한 "Ball" 레이어로 변경한다.
    // 반드시 AddBallLayer()가 먼저 실행되고 그 결과가 저장된 이후에 호출되어야 한다.
    private static void AssignBallPrefabLayer()
    {
        const string prefabPath = "Assets/_Project/Prefabs/Ball/Ball.prefab";

        int ballLayer = LayerMask.NameToLayer("Ball");
        if (ballLayer == -1)
        {
            Debug.LogError("[BallSetupEditor] \"Ball\" 레이어를 찾을 수 없어 Ball.prefab 레이어 할당을 스킵합니다.");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            Debug.LogError($"[BallSetupEditor] 프리팹을 찾을 수 없습니다: {prefabPath}");
            return;
        }

        if (prefabRoot.layer != ballLayer)
        {
            prefabRoot.layer = ballLayer;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            Debug.Log($"[BallSetupEditor] Ball.prefab 레이어를 \"Ball\"(index {ballLayer})로 변경.");
        }
        else
        {
            Debug.Log("[BallSetupEditor] Ball.prefab 레이어가 이미 \"Ball\", 스킵.");
        }

        PrefabUtility.UnloadPrefabContents(prefabRoot);
    }

    private static void CreatePhysicsMaterial()
    {
        const string path = "Assets/_Project/Physics/BallBounce.physicsMaterial2D";

        if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path) != null)
        {
            Debug.Log("[BallSetupEditor] PhysicsMaterial2D 이미 존재, 스킵.");
            return;
        }

        // 폴더 생성
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Physics"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Physics");
        }

        PhysicsMaterial2D mat = new PhysicsMaterial2D
        {
            bounciness = 1f,
            friction = 0f
        };

        AssetDatabase.CreateAsset(mat, path);
        Debug.Log($"[BallSetupEditor] PhysicsMaterial2D 생성: {path}");
    }

    private static void CreateBallDataAsset()
    {
        const string path = "Assets/_Project/Data/BallData.asset";

        if (AssetDatabase.LoadAssetAtPath<BallData>(path) != null)
        {
            Debug.Log("[BallSetupEditor] BallData 에셋 이미 존재, 스킵.");
            return;
        }

        // 폴더 생성
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        }

        BallData ballData = ScriptableObject.CreateInstance<BallData>();

        // SerializedObject를 통해 기본값 설정
        SerializedObject so = new SerializedObject(ballData);
        so.FindProperty("_damage").floatValue = 8f;
        so.FindProperty("_speed").floatValue = 10f;
        so.FindProperty("_criticalChance").floatValue = 0f;
        so.FindProperty("_criticalMultiplier").floatValue = 1.5f;
        so.FindProperty("_maxBounces").intValue = 10;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(ballData, path);
        Debug.Log($"[BallSetupEditor] BallData 에셋 생성: {path}");
    }
}
#endif
