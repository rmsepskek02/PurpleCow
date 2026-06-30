using System.Collections.Generic;
using UnityEngine;

public class SkillSelectionPanel : MonoBehaviour
{
    [SerializeField] private SkillCardUI[] _cards;
    [SerializeField] private SkillData[]   _allSkillDatas;

    private void OnEnable()
    {
        ShowRandomSkills();
    }

    private void ShowRandomSkills()
    {
        List<SkillData> pool = new List<SkillData>(_allSkillDatas);
        for (int i = 0; i < _cards.Length; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            _cards[i].Setup(pool[randomIndex], OnCardSelected);
            pool.RemoveAt(randomIndex);
        }
    }

    private void OnCardSelected(SkillData selectedData)
    {
        ApplySkill(selectedData);
        UIManager.Instance.OnSkillSelectionComplete();
    }

    private void ApplySkill(SkillData data)
    {
        if (data.SkillType == SkillType.Active)
        {
            BallSkillBase skill = SkillFactory.CreateActiveSkill(data);
            SkillManager.Instance.EquipActiveSkill(skill);
        }
        else
        {
            PassiveSkillBase skill = SkillFactory.CreatePassiveSkill(data);
            SkillManager.Instance.AddPassiveSkill(skill);
        }
    }
}
