using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SkillSelectionPanel : MonoBehaviour
{
    [SerializeField] private SkillCardUI[] _skillCards;
    [SerializeField] private SkillData[]   _allSkillDatas;

    [SerializeField] private SkillSlotGroup _activeSlotGroup;
    [SerializeField] private SkillSlotGroup _passiveSlotGroup;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private HUDPanel _hudPanel;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _slideDist    = 50f;
    [SerializeField] private float _animDuration = 0.3f;
    [SerializeField] private Ease  _ease         = Ease.OutCubic;
    private Vector3 _originalPos;

    private void Awake()
    {
        _originalPos = transform.localPosition;
        if (_hudPanel == null)
            _hudPanel = FindFirstObjectByType<HUDPanel>();
    }

    private void OnEnable()
    {
        CharacterManager.OnLevelUp += OpenPanel;
        SkillManager.OnActiveSkillsChanged  += HandleActiveSkillsChanged;
        SkillManager.OnPassiveSkillsChanged += HandlePassiveSkillsChanged;
    }

    private void OnDisable()
    {
        CharacterManager.OnLevelUp -= OpenPanel;
        SkillManager.OnActiveSkillsChanged  -= HandleActiveSkillsChanged;
        SkillManager.OnPassiveSkillsChanged -= HandlePassiveSkillsChanged;
    }

    private void HandleActiveSkillsChanged(List<BallSkillBase> skills)
        => _activeSlotGroup.UpdateActiveSlots(skills);

    private void HandlePassiveSkillsChanged(List<PassiveSkillBase> skills)
        => _passiveSlotGroup.UpdatePassiveSlots(skills);

    private void OpenPanel(int newLevel)
    {
        Time.timeScale = 0f;
        _hudPanel?.SetCharacterProgressVisible(false);
        if (_levelText != null) _levelText.text = newLevel.ToString();
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
                SkillData data = candidates[i];
                SkillRuntimeState state = data.SkillType == SkillType.Active
                    ? SkillManager.Instance.GetActiveSkillState(data.SkillId)
                    : SkillManager.Instance.GetPassiveSkillState(data.SkillId);
                _skillCards[i].Setup(data, state, OnSkillSelected);
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

        foreach (var data in _allSkillDatas)
        {
            if (data.SkillType == SkillType.Active)
            {
                SkillRuntimeState state = sm.GetActiveSkillState(data.SkillId);
                bool owned = state != null;
                if (owned)
                { if (!state.IsMaxLevel) pool.Add(data); }
                else
                { if (sm.CanEquipActive) pool.Add(data); }
            }
            else
            {
                SkillRuntimeState state = sm.GetPassiveSkillState(data.SkillId);
                bool owned = state != null;
                if (owned)
                { if (!state.IsMaxLevel) pool.Add(data); }
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
            bool isNewSkill = SkillManager.Instance.EquipActiveSkill(data, out SkillRuntimeState state);
            // 신규 타입 선택 시에만 로스터에 볼이 1개 추가된다.
            // 재선택은 로스터가 공유하는 SkillRuntimeState만 레벨업하므로 볼 개수는 늘지 않는다.
            if (isNewSkill)
                BallLauncher.Instance.AddBallToRoster(state);
        }
        else
        {
            SkillManager.Instance.AddPassiveSkill(data);
        }
    }

    public void Show()
    {
        transform.DOKill();
        _canvasGroup.DOKill();
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
        transform.DOKill();
        _canvasGroup.DOKill();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(transform.DOLocalMoveY(_originalPos.y - _slideDist, _animDuration).SetEase(_ease));
        seq.Join(_canvasGroup.DOFade(0f, _animDuration));
        seq.OnComplete(() =>
        {
            transform.localPosition = _originalPos;
            _hudPanel?.SetCharacterProgressVisible(true);
        });
    }
}
