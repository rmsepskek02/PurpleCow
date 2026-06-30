using TMPro;
using UnityEngine;

public class HUDPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text  _waveText;
    [SerializeField] private TMP_Text  _scoreText;
    [SerializeField] private GameObject _launchReadyIndicator;

    private int _totalWaves;

    private void OnEnable()
    {
        WaveManager.OnWaveStarted         += HandleWaveStarted;
        BallLauncher.OnAllBallsReturned   += HandleAllBallsReturned;
        UIManager.OnScoreChanged          += HandleScoreChanged;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveStarted         -= HandleWaveStarted;
        BallLauncher.OnAllBallsReturned   -= HandleAllBallsReturned;
        UIManager.OnScoreChanged          -= HandleScoreChanged;
    }

    private void Start()
    {
        _totalWaves = WaveManager.Instance.TotalWaves;
        UpdateScore(0);
        _launchReadyIndicator.SetActive(false);
    }

    private void HandleWaveStarted(int waveNumber)
    {
        _waveText.text = $"WAVE {waveNumber} / {_totalWaves}";
        _launchReadyIndicator.SetActive(true);
    }

    private void HandleAllBallsReturned()
    {
        _launchReadyIndicator.SetActive(false);
    }

    private void HandleScoreChanged(int score)
    {
        UpdateScore(score);
    }

    private void UpdateScore(int score)
    {
        _scoreText.text = $"처치: {score}";
    }
}
