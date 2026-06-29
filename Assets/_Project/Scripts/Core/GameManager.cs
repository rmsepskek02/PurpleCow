using System;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public enum GameState { Ready, Playing, Result }

    public event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void EndGame(bool isSuccess)
    {
        CurrentState = GameState.Result;
        OnGameStateChanged?.Invoke(CurrentState);
    }

    public void RestartGame()
    {
        CurrentState = GameState.Ready;
        OnGameStateChanged?.Invoke(CurrentState);
    }
}
