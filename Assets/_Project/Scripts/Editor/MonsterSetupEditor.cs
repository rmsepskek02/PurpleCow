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
        SetupWaveSpawnEntries();
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

        const string path = "Assets/_Project/Data/WaveTableData.asset";

        if (AssetDatabase.LoadAssetAtPath<WaveTableData>(path) != null)
        {
            Debug.Log("[MonsterSetupEditor] WaveTableData 이미 존재, 스킵.");
            return;
        }

        WaveTableData waveTable = ScriptableObject.CreateInstance<WaveTableData>();

        SerializedObject so = new SerializedObject(waveTable);
        SerializedProperty wavesProp = so.FindProperty("_waves");
        wavesProp.arraySize = 20;
        for (int n = 1; n <= 20; n++)
        {
            SerializedProperty waveEntry = wavesProp.GetArrayElementAtIndex(n - 1);
            waveEntry.FindPropertyRelative("WaveNumber").intValue = n;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(waveTable, path);
        Debug.Log($"[MonsterSetupEditor] WaveTableData 생성: {path}");
    }

    [MenuItem("PurpleCow/Setup/Setup Wave Spawn Entries")]
    private static void SetupWaveSpawnEntries()
    {
        string[] monsterNames = { "Fluffy", "Spider", "StoneBug", "ForestDeer" };
        MonsterData[] monsterDatas = new MonsterData[4];
        for (int i = 0; i < 4; i++)
        {
            monsterDatas[i] = AssetDatabase.LoadAssetAtPath<MonsterData>(
                $"Assets/_Project/Data/MonsterData_{monsterNames[i]}.asset");
            if (monsterDatas[i] == null)
                Debug.LogWarning($"[MonsterSetupEditor] MonsterData_{monsterNames[i]}.asset 없음.");
        }

        const string tablePath = "Assets/_Project/Data/WaveTableData.asset";
        WaveTableData waveTable = AssetDatabase.LoadAssetAtPath<WaveTableData>(tablePath);
        if (waveTable == null)
        {
            Debug.LogWarning($"[MonsterSetupEditor] {tablePath} 없음. Monster System Setup을 먼저 실행하세요.");
            return;
        }

        SerializedObject tableSo = new SerializedObject(waveTable);
        SerializedProperty wavesProp = tableSo.FindProperty("_waves");

        for (int waveIdx = 0; waveIdx < 20; waveIdx++)
        {
            int waveNumber = waveIdx + 1;
            if (waveIdx >= wavesProp.arraySize) continue;

            SerializedProperty waveEntry = wavesProp.GetArrayElementAtIndex(waveIdx);
            SerializedProperty entriesProp = waveEntry.FindPropertyRelative("SpawnEntries");

            // 이미 스폰 데이터가 있으면 스킵
            if (entriesProp.arraySize > 0)
            {
                Debug.Log($"[MonsterSetupEditor] Wave{waveNumber} 이미 스폰 데이터 있음, 스킵.");
                continue;
            }

            // 이 웨이브에서 사용할 몬스터 종류 결정
            // Wave 1-5: Fluffy only, Wave 6-10: Fluffy+Spider, Wave 11-15: +StoneBug, Wave 16-20: +ForestDeer
            int monsterTypeCount = waveIdx < 5 ? 1 : waveIdx < 10 ? 2 : waveIdx < 15 ? 3 : 4;

            // 스폰 수: 그룹(0~3)과 그룹 내 위치(0~4)에 따라 증가
            int groupIdx = waveIdx / 5;
            int posInGroup = waveIdx % 5;
            int spawnCount = 3 + posInGroup + groupIdx * 2;

            // GridPosition은 _spawnRoot 기준 상대 격자 인덱스
            // x: -2 ~ +2 (5열), y: 그룹마다 다른 시작 행(8, 6, 4, 2)에서 행 증가마다 -1
            int startY = 8 - groupIdx * 2;

            entriesProp.arraySize = spawnCount;
            for (int s = 0; s < spawnCount; s++)
            {
                SerializedProperty entry = entriesProp.GetArrayElementAtIndex(s);
                int typeIdx = s % monsterTypeCount;
                entry.FindPropertyRelative("Data").objectReferenceValue = monsterDatas[typeIdx];

                SerializedProperty gridPos = entry.FindPropertyRelative("GridPosition");
                gridPos.FindPropertyRelative("x").intValue = (s % 5) - 2;   // -2 to +2
                gridPos.FindPropertyRelative("y").intValue = startY - (s / 5); // startY, startY-1, ...
            }

            Debug.Log($"[MonsterSetupEditor] Wave{waveNumber} 스폰 데이터 {spawnCount}개 설정 완료.");
        }

        tableSo.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log("[MonsterSetupEditor] Wave Spawn Entries 설정 완료.");
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
