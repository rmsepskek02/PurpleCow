using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { Ready, Playing, Result }

    public static event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; }
    public bool IsLastGameSuccess { get; private set; }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void EndGame(bool isSuccess)
    {
        IsLastGameSuccess = isSuccess;
        CurrentState = GameState.Result;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void RestartGame()
    {
        CurrentState = GameState.Ready;
        OnGameStateChanged?.Invoke(CurrentState);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
