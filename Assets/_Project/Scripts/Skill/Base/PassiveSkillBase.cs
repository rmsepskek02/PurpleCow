public abstract class PassiveSkillBase
{
    protected SkillRuntimeState _state;

    protected PassiveSkillBase(SkillRuntimeState state)
    {
        _state = state;
    }

    // SkillManager가 장착 시 호출 → 이벤트 구독
    public abstract void Apply();

    // SkillManager가 해제 시 호출 → 이벤트 해제
    public abstract void Remove();

    public SkillData SkillData => _state.Data;
    public SkillRuntimeState State => _state;

    protected SkillLevelData LevelData => _state.CurrentLevelData;
}
