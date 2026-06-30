using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillSelectionPanel : MonoBehaviour
{
    [SerializeField] private SkillCardUI[] _skillCards;
    [SerializeField] private SkillData[]   _allSkillDatas;

    private void OnEnable()
    {
        WaveManager.OnKillCountReached += OpenPanel;
        ShowRandomSkills();
    }

    private void OnDisable()
    {
        WaveManager.OnKillCountReached -= OpenPanel;
    }

    private void OpenPanel()
    {
        gameObject.SetActive(true);
        ShowRandomSkills();
    }

    private void ShowRandomSkills()
    {
        var candidates = BuildSkillCardPool();
        // 3개 무작위 선택
        candidates = candidates.OrderBy(_ => UnityEngine.Random.value).Take(3).ToList();
        for (int i = 0; i < _skillCards.Length; i++)
        {
            if (i < candidates.Count)
            {
                _skillCards[i].gameObject.SetActive(true);
                _skillCards[i].Setup(candidates[i], OnSkillSelected);
            }
            else
            {
                _skillCards[i].gameObject.SetActive(false);
            }
        }
    }

    private List<SkillData> BuildSkillCardPool()
    {
        var pool = new List<SkillData>();
        var sm = SkillManager.Instance;

        var activeSkillIds  = sm.ActiveSkillIds;
        var passiveSkillIds = sm.PassiveSkillIds;

        foreach (var data in _allSkillDatas)
        {
            if (data.SkillType == SkillType.Active)
            {
                bool owned = activeSkillIds.Contains(data.SkillId);
                if (owned)
                { if (data.CurrentLevel < data.MaxLevel - 1) pool.Add(data); }
                else
                { if (sm.CanEquipActive) pool.Add(data); }
            }
            else
            {
                bool owned = passiveSkillIds.Contains(data.SkillId);
                if (owned)
                { if (data.CurrentLevel < data.MaxLevel - 1) pool.Add(data); }
                else
                { if (sm.CanEquipPassive) pool.Add(data); }
            }
        }
        return pool;
    }

    private void OnSkillSelected(SkillData selectedData)
    {
        ApplySkill(selectedData);
        UIManager.Instance.OnSkillSelectionComplete();
        gameObject.SetActive(false);
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
