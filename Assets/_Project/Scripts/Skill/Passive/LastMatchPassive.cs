using UnityEngine;

public class LastMatchPassive : PassiveSkillBase
{
    public LastMatchPassive(SkillRuntimeState state) : base(state) { }

    public override void Apply()
    {
        MonsterBase.OnMonsterDied += HandleMonsterDied;
    }

    public override void Remove()
    {
        MonsterBase.OnMonsterDied -= HandleMonsterDied;
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        float radius = LevelData.Value2 > 0f ? LevelData.Value2 : 1.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(monster.transform.position, radius);
        foreach (var hit in hits)
        {
            MonsterBase m = hit.GetComponent<MonsterBase>();
            if (m != null && m != monster && m.IsAlive)
                m.TakeDamage(LevelData.Value1);
        }
    }
}
