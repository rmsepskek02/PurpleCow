using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text _resultTitleText;
    [SerializeField] private TMP_Text _finalScoreText;
    [SerializeField] private Button   _restartButton;

    private void Awake()
    {
        _restartButton.onClick.AddListener(HandleRestartClicked);
    }

    private void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        _restartButton.onClick.RemoveListener(HandleRestartClicked);
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state != GameManager.GameState.Result)
            return;

        bool isSuccess = GameManager.Instance.IsLastGameSuccess;
        _resultTitleText.text = isSuccess ? "SUCCESS" : "GAME OVER";
        _finalScoreText.text  = $"최종 점수: {UIManager.Instance.Score}";
    }

    private void HandleRestartClicked()
    {
        GameManager.Instance.RestartGame();
    }
}
