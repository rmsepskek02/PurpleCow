public class LastHitPassive : PassiveSkillBase
{
    public LastHitPassive(SkillData skillData) : base(skillData) { }

    public override void Apply()
    {
        Ball.OnBeforeReturn += HandleBeforeReturn;
    }

    public override void Remove()
    {
        Ball.OnBeforeReturn -= HandleBeforeReturn;
    }

    private void HandleBeforeReturn(Ball ball)
    {
        // WaveManager에서 현재 살아있는 몬스터 목록을 받아
        // HP가 가장 낮은 몬스터를 찾아 추가 데미지 적용
        MonsterBase weakest = WaveManager.Instance.GetWeakestMonster();
        if (weakest != null)
        {
            weakest.TakeDamage(_skillData.Value1);
        }
    }
}
