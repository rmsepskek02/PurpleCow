using System.Collections.Generic;
using UnityEngine;

public class SkillSlotGroup : MonoBehaviour
{
    [SerializeField] private SkillSlotIcon[] _slots;

    public void UpdateActiveSlots(List<BallSkillBase> skills)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < skills.Count)
                _slots[i].SetFilled(skills[i].SkillData.Icon, skills[i].SkillData.CurrentLevel + 1);
            else
                _slots[i].SetEmpty();
        }
    }

    public void UpdatePassiveSlots(List<PassiveSkillBase> skills)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (i < skills.Count)
                _slots[i].SetFilled(skills[i].SkillData.Icon, skills[i].SkillData.CurrentLevel + 1);
            else
                _slots[i].SetEmpty();
        }
    }
}
