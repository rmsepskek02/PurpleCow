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
        _hudPanel.gameObject.SetActive(false);
        _resultPanel.gameObject.SetActive(false);
        _skillSelectionPanel.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        WaveManager.OnWaveCleared               += HandleWaveCleared;
        WaveManager.OnAllWavesCleared           += HandleAllWavesCleared;
        MonsterBase.OnMonsterDied               += HandleMonsterDied;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        WaveManager.OnWaveCleared               -= HandleWaveCleared;
        WaveManager.OnAllWavesCleared           -= HandleAllWavesCleared;
        MonsterBase.OnMonsterDied               -= HandleMonsterDied;
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

    private void HandleWaveCleared()
    {
        ShowSkillSelection(true);
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
        ShowSkillSelection(false);
        WaveManager.Instance.AdvanceToNextWave();
    }

    private void ShowHUD(bool show)            => _hudPanel.gameObject.SetActive(show);
    private void ShowResult(bool show)         => _resultPanel.gameObject.SetActive(show);
    private void ShowSkillSelection(bool show) => _skillSelectionPanel.gameObject.SetActive(show);
}
