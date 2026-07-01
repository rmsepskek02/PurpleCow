#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SkillSetupEditor
{
    [MenuItem("PurpleCow/Setup/Skill System Setup")]
    private static void SetupSkillSystem()
    {
        EnsureDataFolder();
        CreateActiveSkillDataAssets();
        CreatePassiveSkillDataAssets();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[SkillSetupEditor] Skill System Setup 완료.");
    }

    private static void EnsureDataFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Data"))
        {
            AssetDatabase.CreateFolder("Assets/_Project", "Data");
        }
    }

    // ──────────────────────────────────────────
    //  Active SkillData (5종)
    // ──────────────────────────────────────────

    private static void CreateActiveSkillDataAssets()
    {
        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Fire.asset",
            skillId:   (int)ActiveSkillId.Fire,
            skillName: "Fire Ball",
            skillType: SkillType.Active,
            desc:      "충돌한 몬스터에게 지속 화상 피해를 줍니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 21f, Value1 = 4f,   Value2 = 3f, Value3 = 8f  },
                new SkillLevelData { BallDamage = 24f, Value1 = 4.5f, Value2 = 4f, Value3 = 10f },
                new SkillLevelData { BallDamage = 27f, Value1 = 5f,   Value2 = 5f, Value3 = 12f },
            },
            iconPath: "Assets/_Project/Sprites/BallSkillIcon/Ball_Fire_ball.png"
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Ice.asset",
            skillId:   (int)ActiveSkillId.Ice,
            skillName: "Ice Ball",
            skillType: SkillType.Active,
            desc:      "확률적으로 충돌한 몬스터를 빙결/둔화시킵니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 25f, Value1 = 0.3f,  Value2 = 5f, Value3 = 0.1f  },
                new SkillLevelData { BallDamage = 37f, Value1 = 0.35f, Value2 = 6f, Value3 = 0.15f },
                new SkillLevelData { BallDamage = 50f, Value1 = 0.4f,  Value2 = 7f, Value3 = 0.2f  },
            },
            iconPath: "Assets/_Project/Sprites/BallSkillIcon/Ball_Ice_Ball.png"
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Laser.asset",
            skillId:   (int)ActiveSkillId.Laser,
            skillName: "Laser Ball",
            skillType: SkillType.Active,
            desc:      "충돌한 몬스터와 같은 행의 다른 몬스터에게 추가 피해를 줍니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 11f, Value1 = 7f,  Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 15f, Value1 = 11f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 19f, Value1 = 15f, Value2 = 0f, Value3 = 0f },
            },
            iconPath: "Assets/_Project/Sprites/BallSkillIcon/Ball_Laser_Ball.png"
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Ghost.asset",
            skillId:   (int)ActiveSkillId.Ghost,
            skillName: "Ghost Ball",
            skillType: SkillType.Active,
            desc:      "볼이 몬스터를 관통하여 계속 이동합니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 14f, Value1 = 0f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 21f, Value1 = 0f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 28f, Value1 = 0f, Value2 = 0f, Value3 = 0f },
            },
            iconPath: "Assets/_Project/Sprites/BallSkillIcon/Ball_Ghost_Ball.png"
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Cluster.asset",
            skillId:   (int)ActiveSkillId.Cluster,
            skillName: "Cluster Ball",
            skillType: SkillType.Active,
            desc:      "확률적으로 몬스터 충돌 시 서브볼을 추가 발사합니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 27f, Value1 = 0.4f, Value2 = 10f, Value3 = 0f },
                new SkillLevelData { BallDamage = 30f, Value1 = 0.5f, Value2 = 15f, Value3 = 0f },
                new SkillLevelData { BallDamage = 33f, Value1 = 0.6f, Value2 = 20f, Value3 = 0f },
            },
            iconPath: "Assets/_Project/Sprites/BallSkillIcon/Ball_Cluster_Ball.png"
        );
    }

    // ──────────────────────────────────────────
    //  Passive SkillData (5종)
    // ──────────────────────────────────────────

    private static void CreatePassiveSkillDataAssets()
    {
        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Passive_WarmTinHeart.asset",
            skillId:   (int)PassiveSkillId.WarmTinHeart,
            skillName: "Warm Tin Heart",
            skillType: SkillType.Passive,
            desc:      "볼의 기본 데미지 배율을 증가시킵니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 0f, Value1 = 0.2f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.3f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.4f, Value2 = 0f, Value3 = 0f },
            },
            iconPath: ""
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Passive_MagicMirror.asset",
            skillId:   (int)PassiveSkillId.MagicMirror,
            skillName: "Magic Mirror",
            skillType: SkillType.Passive,
            desc:      "볼이 벽에 반사될 때마다 다음 공격 데미지를 증가시킵니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 0f, Value1 = 0.2f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.4f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.6f, Value2 = 0f, Value3 = 0f },
            },
            iconPath: ""
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Passive_AmethystDagger.asset",
            skillId:   (int)PassiveSkillId.AmethystDagger,
            skillName: "Amethyst Dagger",
            skillType: SkillType.Passive,
            desc:      "몬스터 정면 타격 시 보너스 크리티컬 확률을 부여합니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 0f, Value1 = 0.1f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.2f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.3f, Value2 = 0f, Value3 = 0f },
            },
            iconPath: ""
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Passive_EmeraldDagger.asset",
            skillId:   (int)PassiveSkillId.EmeraldDagger,
            skillName: "Emerald Dagger",
            skillType: SkillType.Passive,
            desc:      "몬스터 후면 타격 시 보너스 크리티컬 확률을 부여합니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 0f, Value1 = 0.2f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.3f, Value2 = 0f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 0.4f, Value2 = 0f, Value3 = 0f },
            },
            iconPath: ""
        );

        CreateSkillData(
            path:      "Assets/_Project/Data/SkillData_Passive_LastMatch.asset",
            skillId:   (int)PassiveSkillId.LastMatch,
            skillName: "Last Match",
            skillType: SkillType.Passive,
            desc:      "몬스터 처치 시 주변 적들에게 추가 피해를 줍니다.",
            levels: new SkillLevelData[]
            {
                new SkillLevelData { BallDamage = 0f, Value1 = 10f, Value2 = 1.5f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 20f, Value2 = 1.5f, Value3 = 0f },
                new SkillLevelData { BallDamage = 0f, Value1 = 30f, Value2 = 1.5f, Value3 = 0f },
            },
            iconPath: ""
        );
    }

    // ──────────────────────────────────────────
    //  공통 생성 헬퍼
    // ──────────────────────────────────────────

    private static void CreateSkillData(
        string         path,
        int            skillId,
        string         skillName,
        SkillType      skillType,
        string         desc,
        SkillLevelData[] levels,
        string         iconPath)
    {
        if (AssetDatabase.LoadAssetAtPath<SkillData>(path) != null)
        {
            Debug.Log($"[SkillSetupEditor] 이미 존재, 스킵: {path}");
            return;
        }

        SkillData data = ScriptableObject.CreateInstance<SkillData>();

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("_skillId").intValue        = skillId;
        so.FindProperty("_skillName").stringValue   = skillName;
        so.FindProperty("_description").stringValue = desc;
        so.FindProperty("_skillType").enumValueIndex = (int)skillType;

        SerializedProperty levelsProp = so.FindProperty("_levels");
        levelsProp.arraySize = levels.Length;
        for (int i = 0; i < levels.Length; i++)
        {
            SerializedProperty elem = levelsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("BallDamage").floatValue = levels[i].BallDamage;
            elem.FindPropertyRelative("Value1").floatValue     = levels[i].Value1;
            elem.FindPropertyRelative("Value2").floatValue     = levels[i].Value2;
            elem.FindPropertyRelative("Value3").floatValue     = levels[i].Value3;
        }

        // 아이콘 자동 연결
        if (!string.IsNullOrEmpty(iconPath))
        {
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon != null)
                so.FindProperty("_icon").objectReferenceValue = icon;
            else
                Debug.LogWarning($"[SkillSetupEditor] 아이콘 없음, 스킵: {iconPath}");
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[SkillSetupEditor] SkillData 생성: {path}");
    }
}
#endif
