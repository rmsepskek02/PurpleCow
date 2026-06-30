using UnityEngine;

public enum SkillType
{
    Active,
    Passive
}

public enum ActiveSkillId
{
    Fire    = 1001,
    Ice     = 1002,
    Ghost   = 1003,
    Laser   = 1004,
    Cluster = 1005
}

public enum PassiveSkillId
{
    DamageUp     = 3000,
    CritChanceUp = 3002,
    CritDamageUp = 3003,
    SpeedUp      = 3006,
    BounceUp     = 3007,
    KillShot     = 3013,
    LastHit      = 3014
}

[CreateAssetMenu(fileName = "SkillData", menuName = "PurpleCow/SkillData")]
public class SkillData : ScriptableObject
{
    [SerializeField] private int       _skillId;
    [SerializeField] private string    _skillName;
    [SerializeField] private Sprite    _icon;
    [SerializeField] private string    _description;
    [SerializeField] private SkillType _skillType;

    // 수치 — 패시브/액티브 공통. 필요한 항목만 설정, 나머지 0으로 유지
    [SerializeField] private float _value1;   // 예: 데미지 증가량, 폭발 반경, 서브볼 개수
    [SerializeField] private float _value2;   // 예: 크리티컬 배율, 둔화 지속 시간
    [SerializeField] private float _value3;   // 예: 추가 데미지 배율

    public int       SkillId     => _skillId;
    public string    SkillName   => _skillName;
    public Sprite    Icon        => _icon;
    public string    Description => _description;
    public SkillType SkillType   => _skillType;
    public float     Value1      => _value1;
    public float     Value2      => _value2;
    public float     Value3      => _value3;
}
