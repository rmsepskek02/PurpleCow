using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class SkillSelectionPanel : MonoBehaviour
{
    [SerializeField] private SkillCardUI[] _skillCards;
    [SerializeField] private SkillData[]   _allSkillDatas;

    [SerializeField] private SkillSlotGroup _activeSlotGroup;
    [SerializeField] private SkillSlotGroup _passiveSlotGroup;

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
        WaveManager.OnKillCountReached  += OpenPanel;
        GameManager.OnGameStateChanged  += HandleGameStateChanged;
        SkillManager.OnActiveSkillsChanged  += HandleActiveSkillsChanged;
        SkillManager.OnPassiveSkillsChanged += HandlePassiveSkillsChanged;
    }

    private void OnDisable()
    {
        WaveManager.OnKillCountReached  -= OpenPanel;
        GameManager.OnGameStateChanged  -= HandleGameStateChanged;
        SkillManager.OnActiveSkillsChanged  -= HandleActiveSkillsChanged;
        SkillManager.OnPassiveSkillsChanged -= HandlePassiveSkillsChanged;
    }

    private void HandleActiveSkillsChanged(List<BallSkillBase> skills)
        => _activeSlotGroup.UpdateActiveSlots(skills);

    private void HandlePassiveSkillsChanged(List<PassiveSkillBase> skills)
        => _passiveSlotGroup.UpdatePassiveSlots(skills);

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Ready)
            return;

        foreach (var data in _allSkillDatas)
            data.ResetLevel();
    }

    private void OpenPanel()
    {
        Time.timeScale = 0f;
        ShowRandomSkills();
        RefreshSlotGroups();
        Show();
    }

    private void RefreshSlotGroups()
    {
        var sm = SkillManager.Instance;
        _activeSlotGroup.UpdateActiveSlots(new List<BallSkillBase>(sm.EquippedActiveSkills));
        _passiveSlotGroup.UpdatePassiveSlots(new List<PassiveSkillBase>(sm.EquippedPassiveSkills));
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
    }

    private void ApplySkill(SkillData data)
    {
        if (data.SkillType == SkillType.Active)
        {
            BallSkillBase skill = SkillFactory.CreateActiveSkill(data);
            bool isNewSkill = SkillManager.Instance.EquipActiveSkill(skill);
            // 신규 타입 선택 시에만 로스터에 볼이 1개 추가된다. 기존 타입 재선택은 레벨업만 반영되며
            // (SkillData.CurrentLevel이 이미 갱신되어 로스터가 재발사 시 자동으로 최신 레벨을 참조),
            // 볼 개수는 늘지 않는다.
            if (isNewSkill)
                BallLauncher.Instance.AddBallToRoster(data);
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

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(transform.DOLocalMoveY(_originalPos.y, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(1f, _animDuration));
        seq.OnComplete(() => { _canvasGroup.blocksRaycasts = true; _canvasGroup.interactable = true; });
    }

    public void Hide()
    {
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(transform.DOLocalMoveY(_originalPos.y - _slideDist, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(0f, _animDuration));
        seq.OnComplete(() => { transform.localPosition = _originalPos; gameObject.SetActive(false); });
    }
}
