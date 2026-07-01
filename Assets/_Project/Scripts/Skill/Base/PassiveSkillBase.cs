public abstract class PassiveSkillBase
{
    protected SkillData _skillData;

    protected PassiveSkillBase(SkillData skillData)
    {
        _skillData = skillData;
    }

    // SkillManager가 장착 시 호출 → 이벤트 구독
    public abstract void Apply();

    // SkillManager가 해제 시 호출 → 이벤트 해제
    public abstract void Remove();

    public SkillData SkillData => _skillData;

    protected SkillLevelData LevelData => _skillData.CurrentLevelData;
}
