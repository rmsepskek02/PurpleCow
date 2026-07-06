using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { Ready, Playing, Result }

    public static event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; }
    public bool IsLastGameSuccess { get; private set; }

    private void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void EndGame(bool isSuccess)
    {
        Time.timeScale = 0f;
        IsLastGameSuccess = isSuccess;
        CurrentState = GameState.Result;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.Ready;
        OnGameStateChanged?.Invoke(CurrentState);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
