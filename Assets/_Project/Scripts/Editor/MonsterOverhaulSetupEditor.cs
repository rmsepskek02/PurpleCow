#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// 몬스터 시스템 개편(블록 크기 데이터화 + 웨이브 파라미터화 + 블록 비주얼 합성) 전용 세팅 에디터.
// 기존 MonsterSetupEditor.cs는 이번 개편에서 전혀 수정하지 않으며, 새 데이터 구조 세팅은 이 스크립트가 전담한다.
public static class MonsterOverhaulSetupEditor
{
    private const string DataFolder = "Assets/_Project/Data";
    private const string PrefabFolder = "Assets/_Project/Prefabs/Monster";
    private const string SpriteFolder = "Assets/_Project/Sprites/Monster";

    // 2칸 몬스터(StoneBug/ForestDeer) 상향 스탯. 1칸 몬스터(Fluffy/Spider) 기본값(Hp30/Reward10)보다 명확히 강하게.
    private const float TwoCellHp = 50f;
    private const int   TwoCellReward = 18;

    private struct PrefabBlockConfig
    {
        public string Name;
        public string SpritePath;
        public Vector2 ColliderSize;
        public float HpBarYOffset;

        public PrefabBlockConfig(string name, string spritePath, Vector2 colliderSize, float hpBarYOffset)
        {
            Name = name;
            SpritePath = spritePath;
            ColliderSize = colliderSize;
            HpBarYOffset = hpBarYOffset;
        }
    }

    // HpBarYOffset: 블록 높이 절반만큼 아래로 내리되(정면 하단), 하단에 완전히 붙지 않도록 약간의 여유를 둔 근사값.
    // 세로 2칸(ForestDeer, 높이 1.92)은 절반(0.96)만큼 더 아래로 내려간다.
    private static readonly PrefabBlockConfig[] Configs =
    {
        new PrefabBlockConfig("Fluffy",     $"{SpriteFolder}/Block_1x1.png", new Vector2(0.96f, 0.96f), -0.33f),
        new PrefabBlockConfig("Spider",     $"{SpriteFolder}/Block_1x1.png", new Vector2(0.96f, 0.96f), -0.33f),
        new PrefabBlockConfig("StoneBug",   $"{SpriteFolder}/Block_2x1.png", new Vector2(1.92f, 0.96f), -0.33f),
        new PrefabBlockConfig("ForestDeer", $"{SpriteFolder}/Block_1x2.png", new Vector2(0.96f, 1.92f), -0.81f),
    };

    [MenuItem("PurpleCow/Setup/Monster Overhaul Setup")]
    private static void SetupMonsterOverhaul()
    {
        SetupMonsterDataBlockSizes();
        SetupWaveTableData();
        SetupPrefabBlockVisuals();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MonsterOverhaulSetupEditor] Monster Overhaul Setup 완료.");
    }

    // 1. MonsterData_* 4종에 BlockSize 채우기 + 2칸 몬스터 Hp/Reward 상향
    private static void SetupMonsterDataBlockSizes()
    {
        SetMonsterBlockSize("Fluffy", BlockSize.OneByOne, upgradeStats: false);
        SetMonsterBlockSize("Spider", BlockSize.OneByOne, upgradeStats: false);
        SetMonsterBlockSize("StoneBug", BlockSize.TwoByOne, upgradeStats: true);
        SetMonsterBlockSize("ForestDeer", BlockSize.OneByTwo, upgradeStats: true);
    }

    private static void SetMonsterBlockSize(string name, BlockSize blockSize, bool upgradeStats)
    {
        string path = $"{DataFolder}/MonsterData_{name}.asset";
        MonsterData data = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
        if (data == null)
        {
            Debug.LogWarning($"[MonsterOverhaulSetupEditor] {path} 없음. Monster System Setup을 먼저 실행하세요.");
            return;
        }

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("_blockSize").enumValueIndex = (int)blockSize;

        if (upgradeStats)
        {
            so.FindProperty("_hp").floatValue = TwoCellHp;
            so.FindProperty("_reward").intValue = TwoCellReward;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);

        string statLog = upgradeStats ? $" (Hp={TwoCellHp}, Reward={TwoCellReward} 상향)" : string.Empty;
        Debug.Log($"[MonsterOverhaulSetupEditor] MonsterData_{name} BlockSize={blockSize} 설정 완료{statLog}.");
    }

    // 2. WaveTableData.asset을 새 파라미터 구조로 생성/갱신
    private static void SetupWaveTableData()
    {
        EnsureDataFolder();

        string path = $"{DataFolder}/WaveTableData.asset";
        WaveTableData waveTable = AssetDatabase.LoadAssetAtPath<WaveTableData>(path);

        if (waveTable == null)
        {
            waveTable = ScriptableObject.CreateInstance<WaveTableData>();
            AssetDatabase.CreateAsset(waveTable, path);
            Debug.Log($"[MonsterOverhaulSetupEditor] WaveTableData 신규 생성: {path}");
        }

        MonsterData fluffy     = AssetDatabase.LoadAssetAtPath<MonsterData>($"{DataFolder}/MonsterData_Fluffy.asset");
        MonsterData spider     = AssetDatabase.LoadAssetAtPath<MonsterData>($"{DataFolder}/MonsterData_Spider.asset");
        MonsterData stoneBug   = AssetDatabase.LoadAssetAtPath<MonsterData>($"{DataFolder}/MonsterData_StoneBug.asset");
        MonsterData forestDeer = AssetDatabase.LoadAssetAtPath<MonsterData>($"{DataFolder}/MonsterData_ForestDeer.asset");

        SerializedObject so = new SerializedObject(waveTable);
        so.FindProperty("_baseSpawnCount").intValue = 3;
        so.FindProperty("_spawnCountPerWave").floatValue = 0.5f;
        so.FindProperty("_baseTwoCellWeight").floatValue = 0.1f;
        so.FindProperty("_twoCellWeightPerWave").floatValue = 0.03f;
        so.FindProperty("_totalWaves").intValue = 20;
        so.FindProperty("_fluffyData").objectReferenceValue = fluffy;
        so.FindProperty("_spiderData").objectReferenceValue = spider;
        so.FindProperty("_stoneBugData").objectReferenceValue = stoneBug;
        so.FindProperty("_forestDeerData").objectReferenceValue = forestDeer;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(waveTable);

        Debug.Log("[MonsterOverhaulSetupEditor] WaveTableData 새 파라미터 구조 설정 완료.");
    }

    // 3. 프리팹 4종에 BlockVisual 자식 추가 + HpBarCanvas 재배치
    private static void SetupPrefabBlockVisuals()
    {
        foreach (PrefabBlockConfig config in Configs)
        {
            SetupPrefabBlockVisual(config);
        }
    }

    private static void SetupPrefabBlockVisual(PrefabBlockConfig config)
    {
        string prefabPath = $"{PrefabFolder}/{config.Name}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            Debug.LogWarning($"[MonsterOverhaulSetupEditor] {prefabPath} 없음, 스킵.");
            return;
        }

        Sprite blockSprite = AssetDatabase.LoadAllAssetsAtPath(config.SpritePath).OfType<Sprite>().FirstOrDefault();
        if (blockSprite == null)
        {
            Debug.LogWarning($"[MonsterOverhaulSetupEditor] {config.SpritePath}에서 Sprite를 찾을 수 없음, 스킵.");
            return;
        }

        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            if (root.transform.Find("BlockVisual") != null)
            {
                Debug.Log($"[MonsterOverhaulSetupEditor] {config.Name}.prefab BlockVisual 이미 존재, 스킵.");
                return;
            }

            Transform hpBarCanvas = root.transform.Find("HpBarCanvas");
            if (hpBarCanvas == null)
            {
                Debug.LogWarning($"[MonsterOverhaulSetupEditor] {config.Name}.prefab HpBarCanvas 없음, 스킵.");
                return;
            }

            // BlockVisual: 순수 시각용 자식 (SpriteRenderer만, 콜라이더 없음)
            GameObject blockVisual = new GameObject("BlockVisual");
            blockVisual.transform.SetParent(root.transform, false);
            blockVisual.transform.localPosition = Vector3.zero;
            blockVisual.transform.localRotation = Quaternion.identity;
            blockVisual.transform.localScale = Vector3.one;

            SpriteRenderer blockRenderer = blockVisual.AddComponent<SpriteRenderer>();
            blockRenderer.sprite = blockSprite;
            blockRenderer.sortingOrder = 0;

            // 캐릭터 스프라이트가 블록보다 앞에 그려지도록 sortingOrder 조정
            SpriteRenderer characterRenderer = root.GetComponent<SpriteRenderer>();
            if (characterRenderer != null)
                characterRenderer.sortingOrder = 1;

            // 콜라이더는 STEP 2(MonsterBase.ApplyBlockSize)가 런타임에 재설정하지만,
            // 혼동 방지를 위해 프리팹 초기값도 블록 크기에 맞춰 갱신
            BoxCollider2D collider = root.GetComponent<BoxCollider2D>();
            if (collider != null)
                collider.size = config.ColliderSize;

            // HpBarCanvas를 BlockVisual의 자식으로 재배치, 블록 정면 하단 근사 위치로 이동
            hpBarCanvas.SetParent(blockVisual.transform, false);
            Vector3 hpBarLocalPos = hpBarCanvas.localPosition;
            hpBarLocalPos.y = config.HpBarYOffset;
            hpBarCanvas.localPosition = hpBarLocalPos;

            Debug.Log($"[MonsterOverhaulSetupEditor] {config.Name}.prefab BlockVisual 추가 + HpBarCanvas 재배치 완료.");
        }
    }

    private static void EnsureDataFolder()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        }
    }
}
#endif
