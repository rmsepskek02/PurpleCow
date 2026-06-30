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
            path:       "Assets/_Project/Data/SkillData_Fire.asset",
            skillId:    (int)ActiveSkillId.Fire,
            skillName:  "Fire Ball",
            skillType:  SkillType.Active,
            desc:       "충돌 지점 주변 범위 내 모든 몬스터에게 폭발 데미지를 줍니다.",
            value1:     1.5f,   // 폭발 반경
            value2:     5f,     // 폭발 추가 데미지
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/BallSkillIcon/Ball_Fire_ball.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Ice.asset",
            skillId:    (int)ActiveSkillId.Ice,
            skillName:  "Ice Ball",
            skillType:  SkillType.Active,
            desc:       "충돌한 몬스터를 일정 턴 동안 이동 정지시킵니다.",
            value1:     1f,     // 이동 정지 턴 수
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/BallSkillIcon/Ball_Ice_Ball.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Ghost.asset",
            skillId:    (int)ActiveSkillId.Ghost,
            skillName:  "Ghost Ball",
            skillType:  SkillType.Active,
            desc:       "볼이 몬스터를 관통하여 계속 이동합니다.",
            value1:     0f,
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/BallSkillIcon/Ball_Ghost_Ball.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Laser.asset",
            skillId:    (int)ActiveSkillId.Laser,
            skillName:  "Laser Ball",
            skillType:  SkillType.Active,
            desc:       "발사 즉시 직선상의 모든 몬스터에게 데미지를 줍니다.",
            value1:     20f,    // 레이저 데미지
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/BallSkillIcon/Ball_Laser_Ball.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Cluster.asset",
            skillId:    (int)ActiveSkillId.Cluster,
            skillName:  "Cluster Ball",
            skillType:  SkillType.Active,
            desc:       "몬스터 충돌 시 서브볼을 추가 발사합니다.",
            value1:     3f,     // 서브볼 개수
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/BallSkillIcon/Ball_Cluster_Ball.png"
        );
    }

    // ──────────────────────────────────────────
    //  Passive SkillData (7종)
    // ──────────────────────────────────────────

    private static void CreatePassiveSkillDataAssets()
    {
        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Passive_3000.asset",
            skillId:    (int)PassiveSkillId.DamageUp,
            skillName:  "Damage Up",
            skillType:  SkillType.Passive,
            desc:       "볼의 기본 데미지를 증가시킵니다.",
            value1:     0.1f,   // 데미지 증가 배율 (10%)
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/Passive/icon_passive_3000.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Passive_3002.asset",
            skillId:    (int)PassiveSkillId.CritChanceUp,
            skillName:  "Crit Chance Up",
            skillType:  SkillType.Passive,
            desc:       "크리티컬 발생 확률을 증가시킵니다.",
            value1:     0.05f,  // 크리티컬 확률 증가 (+5%)
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/Passive/icon_passive_3002.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Passive_3003.asset",
            skillId:    (int)PassiveSkillId.CritDamageUp,
            skillName:  "Crit Damage Up",
            skillType:  SkillType.Passive,
            desc:       "크리티컬 데미지 배율을 증가시킵니다.",
            value1:     0.5f,   // 크리티컬 배율 증가 (+0.5)
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/Passive/icon_passive_3003.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Passive_3006.asset",
            skillId:    (int)PassiveSkillId.SpeedUp,
            skillName:  "Speed Up",
            skillType:  SkillType.Passive,
            desc:       "볼 이동 속도를 증가시킵니다.",
            value1:     2f,     // 속도 증가량
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/Passive/icon_passive_3006.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Passive_3007.asset",
            skillId:    (int)PassiveSkillId.BounceUp,
            skillName:  "Bounce Up",
            skillType:  SkillType.Passive,
            desc:       "볼의 최대 반사 횟수를 증가시킵니다.",
            value1:     2f,     // 추가 반사 횟수
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/Passive/icon_passive_3007.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Passive_3013.asset",
            skillId:    (int)PassiveSkillId.KillShot,
            skillName:  "Kill Shot",
            skillType:  SkillType.Passive,
            desc:       "몬스터 처치 시 처치 위치에서 추가 볼 1개를 발사합니다.",
            value1:     0f,
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/Passive/icon_passive_3013.png"
        );

        CreateSkillData(
            path:       "Assets/_Project/Data/SkillData_Passive_3014.asset",
            skillId:    (int)PassiveSkillId.LastHit,
            skillName:  "Last Hit",
            skillType:  SkillType.Passive,
            desc:       "볼이 반납되기 직전 HP가 가장 낮은 몬스터에게 추가 타격을 가합니다.",
            value1:     5f,     // 추가 데미지
            value2:     0f,
            value3:     0f,
            iconPath:   "Assets/_Project/Sprites/Passive/icon_passive_3014.png"
        );
    }

    // ──────────────────────────────────────────
    //  공통 생성 헬퍼
    // ──────────────────────────────────────────

    private static void CreateSkillData(
        string    path,
        int       skillId,
        string    skillName,
        SkillType skillType,
        string    desc,
        float     value1,
        float     value2,
        float     value3,
        string    iconPath)
    {
        if (AssetDatabase.LoadAssetAtPath<SkillData>(path) != null)
        {
            Debug.Log($"[SkillSetupEditor] 이미 존재, 스킵: {path}");
            return;
        }

        SkillData data = ScriptableObject.CreateInstance<SkillData>();

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("_skillId").intValue       = skillId;
        so.FindProperty("_skillName").stringValue  = skillName;
        so.FindProperty("_description").stringValue = desc;
        so.FindProperty("_skillType").enumValueIndex = (int)skillType;
        so.FindProperty("_value1").floatValue      = value1;
        so.FindProperty("_value2").floatValue      = value2;
        so.FindProperty("_value3").floatValue      = value3;

        // 아이콘 자동 연결
        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (icon != null)
        {
            so.FindProperty("_icon").objectReferenceValue = icon;
        }
        else
        {
            Debug.LogWarning($"[SkillSetupEditor] 아이콘 없음, 스킵: {iconPath}");
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(data, path);
        Debug.Log($"[SkillSetupEditor] SkillData 생성: {path}");
    }
}
#endif
