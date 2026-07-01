#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BallSetupEditor
{
    [MenuItem("PurpleCow/Setup/Ball System Setup")]
    private static void SetupBallSystem()
    {
        AddRequiredTags();
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
