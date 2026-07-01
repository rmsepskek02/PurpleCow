using System;
using UnityEngine;

public class UIManager : Singleton<UIManager>
{
    [SerializeField] private HUDPanel            _hudPanel;
    [SerializeField] private ResultPanel         _resultPanel;
    [SerializeField] private SkillSelectionPanel _skillSelectionPanel;

    private int _score;
    public int Score => _score;

    public static event Action<int> OnScoreChanged;

    protected override void Awake()
    {
        base.Awake();
        _hudPanel.Hide();
        _resultPanel.Hide();
        _skillSelectionPanel.Hide();
    }

    private void OnEnable()
    {
        GameManager.OnGameStateChanged  += HandleGameStateChanged;
        WaveManager.OnAllWavesCleared   += HandleAllWavesCleared;
        MonsterBase.OnMonsterDied       += HandleMonsterDied;
    }

    private void OnDisable()
    {
        GameManager.OnGameStateChanged  -= HandleGameStateChanged;
        WaveManager.OnAllWavesCleared   -= HandleAllWavesCleared;
        MonsterBase.OnMonsterDied       -= HandleMonsterDied;
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Ready:
                ShowHUD(false);
                ShowResult(false);
                ShowSkillSelection(false);
                _score = 0;
                break;
            case GameManager.GameState.Playing:
                ShowHUD(true);
                ShowResult(false);
                ShowSkillSelection(false);
                break;
            case GameManager.GameState.Result:
                ShowHUD(false);
                ShowResult(true);
                ShowSkillSelection(false);
                break;
        }
    }

    private void HandleAllWavesCleared()
    {
        GameManager.Instance.EndGame(true);
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        _score++;
        OnScoreChanged?.Invoke(_score);
    }

    public void OnSkillSelectionComplete()
    {
        Time.timeScale = 1f;
        ShowSkillSelection(false);
        WaveManager.Instance.AdvanceToNextWave();
    }

    private void ShowHUD(bool show)            { if (show) _hudPanel.Show(); else _hudPanel.Hide(); }
    private void ShowResult(bool show)         { if (show) _resultPanel.Show(); else _resultPanel.Hide(); }
    private void ShowSkillSelection(bool show) { if (show) _skillSelectionPanel.Show(); else _skillSelectionPanel.Hide(); }
}
