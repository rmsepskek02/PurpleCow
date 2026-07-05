using System;
using System.Collections;
using UnityEngine;

public class PlayerActiveSkillManager : Singleton<PlayerActiveSkillManager>
{
    [SerializeField] private PlayerActiveSkillData[] _skills = new PlayerActiveSkillData[2];

    private float[] _remainingCooldowns;
    private Coroutine _berserkCoroutine;

    public event Action<int, float, float> OnCooldownChanged;

    protected override void Awake()
    {
        base.Awake();
        _remainingCooldowns = new float[_skills.Length];

        for (int i = 0; i < _skills.Length; i++)
        {
            if (_skills[i] != null)
                _remainingCooldowns[i] = _skills[i].Cooldown;
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        for (int i = 0; i < _remainingCooldowns.Length; i++)
        {
            if (_remainingCooldowns[i] <= 0f || _skills[i] == null)
                continue;

            _remainingCooldowns[i] = Mathf.Max(0f, _remainingCooldowns[i] - Time.deltaTime);
            OnCooldownChanged?.Invoke(i, _remainingCooldowns[i], _skills[i].Cooldown);
        }
    }

    public PlayerActiveSkillData GetSkill(int index)
    {
        if (index < 0 || index >= _skills.Length)
            return null;

        return _skills[index];
    }

    public float GetRemainingCooldown(int index)
    {
        if (index < 0 || index >= _remainingCooldowns.Length)
            return 0f;

        return _remainingCooldowns[index];
    }

    public bool TryUseSkill(int index)
    {
        if (GameManager.Instance == null ||
            GameManager.Instance.CurrentState != GameManager.GameState.Playing ||
            index < 0 ||
            index >= _skills.Length ||
            _skills[index] == null ||
            _remainingCooldowns[index] > 0f)
            return false;

        PlayerActiveSkillData skill = _skills[index];
        Activate(skill);
        _remainingCooldowns[index] = skill.Cooldown;
        OnCooldownChanged?.Invoke(index, skill.Cooldown, skill.Cooldown);
        return true;
    }

    private void Activate(PlayerActiveSkillData skill)
    {
        switch (skill.SkillType)
        {
            case PlayerActiveSkillType.Berserk:
                if (_berserkCoroutine != null)
                    StopCoroutine(_berserkCoroutine);
                _berserkCoroutine = StartCoroutine(CoBerserk(skill));
                break;

            case PlayerActiveSkillType.Clone:
                BallLauncher.Instance.LaunchRosterClones(skill.CloneReturnCount);
                break;
        }
    }

    private IEnumerator CoBerserk(PlayerActiveSkillData skill)
    {
        BallLauncher.Instance.SetSpeedMultiplier(skill.SpeedMultiplier);
        yield return new WaitForSeconds(skill.Duration);
        BallLauncher.Instance.SetSpeedMultiplier(1f);
        _berserkCoroutine = null;
    }
}
