using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public static event Action OnAimBegin;
    public static event Action<Vector2> OnDrag;
    public static event Action OnRelease;

    private Camera _mainCamera;

    private bool _isDragging;

    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
    }

    // 발사 지점(BallLauncher.Instance.LaunchPoint)에서 screenPos(스크린 좌표)를 향하는 절대 조준 방향을 계산한다.
    private Vector2 ComputeAimDirection(Vector2 screenPos)
    {
        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
        Vector2 launchPointPos = BallLauncher.Instance.LaunchPoint.position;
        return (worldPos - launchPointPos).normalized;
    }

    private void Update()
    {
        Vector2? pressedPos  = null;
        Vector2? currentPos  = null;
        bool     released    = false;

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            var phase = touch.phase.ReadValue();

            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                pressedPos = touch.position.ReadValue();
            else if (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                     phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                currentPos = touch.position.ReadValue();
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                released = true;
        }
        else if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                pressedPos = Mouse.current.position.ReadValue();
            if (Mouse.current.leftButton.isPressed)
                currentPos = Mouse.current.position.ReadValue();
            if (Mouse.current.leftButton.wasReleasedThisFrame)
                released = true;
        }

        if (pressedPos.HasValue)
        {
            _isDragging = true;
            OnAimBegin?.Invoke();
            OnDrag?.Invoke(ComputeAimDirection(pressedPos.Value));
        }

        if (currentPos.HasValue && _isDragging)
        {
            OnDrag?.Invoke(ComputeAimDirection(currentPos.Value));
        }

        if (released && _isDragging)
        {
            _isDragging = false;
            OnRelease?.Invoke();
        }
    }
}
