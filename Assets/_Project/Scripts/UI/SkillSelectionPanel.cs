using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class SkillSelectionPanel : MonoBehaviour
{
    [SerializeField] private SkillCardUI[] _skillCards;
    [SerializeField] private SkillData[]   _allSkillDatas;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _slideDist    = 50f;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private Ease  _ease         = Ease.OutCubic;
    private Vector3 _originalPos;

    private void Awake()
    {
        _originalPos = transform.localPosition;
    }

    private void OnEnable()
    {
        WaveManager.OnKillCountReached += OpenPanel;
    }

    private void OnDisable()
    {
        WaveManager.OnKillCountReached -= OpenPanel;
    }

    private void OpenPanel()
    {
        ShowRandomSkills();
        Show();
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
                _skillCards[i].SetVisible(true);
                _skillCards[i].Setup(candidates[i], OnSkillSelected);
            }
            else
            {
                _skillCards[i].SetVisible(false);
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
        Hide();
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

    public void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;
        transform.localPosition     = _originalPos + Vector3.down * _slideDist;
        _canvasGroup.alpha          = 0f;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(_originalPos.y, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(1f, _animDuration));
        seq.OnComplete(() => { _canvasGroup.blocksRaycasts = true; _canvasGroup.interactable = true; });
    }

    public void Hide()
    {
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOLocalMoveY(_originalPos.y - _slideDist, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(0f, _animDuration));
        seq.OnComplete(() => { transform.localPosition = _originalPos; gameObject.SetActive(false); });
    }
}
