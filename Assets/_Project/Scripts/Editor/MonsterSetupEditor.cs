#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class MonsterSetupEditor
{
    [MenuItem("PurpleCow/Setup/Monster System Setup")]
    private static void SetupMonsterSystem()
    {
        EnsureMonsterTag();
        CreateMonsterDataAssets();
        CreateWaveDataAssets();
        ConnectMonsterDataToPrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MonsterSetupEditor] Monster System Setup 완료.");
    }

    private static void EnsureMonsterTag()
    {
        const string tag = "Monster";
        string[] existingTags = UnityEditorInternal.InternalEditorUtility.tags;

        foreach (string existing in existingTags)
        {
            if (existing == tag)
            {
                Debug.Log("[MonsterSetupEditor] 태그 'Monster' 이미 존재, 스킵.");
                return;
            }
        }

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset")
        );
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        int index = tagsProp.arraySize;
        tagsProp.InsertArrayElementAtIndex(index);
        tagsProp.GetArrayElementAtIndex(index).stringValue = tag;
        tagManager.ApplyModifiedProperties();

        Debug.Log("[MonsterSetupEditor] 태그 추가: Monster");
    }

    private static void CreateMonsterDataAssets()
    {
        EnsureDataFolder();

        string[] names = { "Fluffy", "Spider", "StoneBug", "ForestDeer" };

        foreach (string name in names)
        {
            string path = $"Assets/_Project/Data/MonsterData_{name}.asset";

            if (AssetDatabase.LoadAssetAtPath<MonsterData>(path) != null)
            {
                Debug.Log($"[MonsterSetupEditor] MonsterData_{name} 이미 존재, 스킵.");
                continue;
            }

            MonsterData data = ScriptableObject.CreateInstance<MonsterData>();

            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_hp").floatValue = 30f;
            so.FindProperty("_moveSpeed").floatValue = 1f;
            so.FindProperty("_damage").intValue = 1;
            so.FindProperty("_reward").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(data, path);
            Debug.Log($"[MonsterSetupEditor] MonsterData 생성: {path}");
        }
    }

    private static void CreateWaveDataAssets()
    {
        EnsureDataFolder();

        for (int n = 1; n <= 20; n++)
        {
            string path = $"Assets/_Project/Data/WaveData_Wave{n}.asset";

            if (AssetDatabase.LoadAssetAtPath<WaveData>(path) != null)
            {
                Debug.Log($"[MonsterSetupEditor] WaveData_Wave{n} 이미 존재, 스킵.");
                continue;
            }

            WaveData waveData = ScriptableObject.CreateInstance<WaveData>();

            SerializedObject so = new SerializedObject(waveData);
            so.FindProperty("_waveNumber").intValue = n;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(waveData, path);
            Debug.Log($"[MonsterSetupEditor] WaveData 생성: {path}");
        }
    }

    private static void EnsureDataFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        }
    }

    private static void ConnectMonsterDataToPrefabs()
    {
        string[] names = { "Fluffy", "Spider", "StoneBug", "ForestDeer" };

        foreach (string name in names)
        {
            string prefabPath = $"Assets/_Project/Prefabs/Monster/{name}.prefab";
            string dataPath   = $"Assets/_Project/Data/MonsterData_{name}.asset";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogWarning($"[MonsterSetupEditor] {name}.prefab 없음, 스킵. Scene Setup을 먼저 실행하세요.");
                continue;
            }

            MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(dataPath);
            if (data == null)
            {
                Debug.LogWarning($"[MonsterSetupEditor] MonsterData_{name}.asset 없음, 스킵.");
                continue;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                MonsterBase monster = scope.prefabContentsRoot.GetComponent<MonsterBase>();
                if (monster == null)
                {
                    Debug.LogWarning($"[MonsterSetupEditor] {name}.prefab MonsterBase 컴포넌트 없음.");
                    continue;
                }

                SerializedObject so = new SerializedObject(monster);
                so.FindProperty("_monsterData").objectReferenceValue = data;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Debug.Log($"[MonsterSetupEditor] {name}.prefab MonsterData 연결 완료.");
        }
    }
}
#endif
