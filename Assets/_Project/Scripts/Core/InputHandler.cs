using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public static event Action OnAimBegin;
    public static event Action<Vector2> OnDrag;
    public static event Action OnRelease;

    private Camera _mainCamera;

    // 스크린(픽셀) 좌표가 아닌 월드 좌표를 저장한다(스크린→월드 변환 후 값).
    private Vector2 _dragStartPosition;
    private bool _isDragging;

    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
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
            _dragStartPosition = _mainCamera.ScreenToWorldPoint(pressedPos.Value);
            _isDragging = true;
            OnAimBegin?.Invoke();
        }

        if (currentPos.HasValue && _isDragging)
        {
            Vector2 currentWorldPos = _mainCamera.ScreenToWorldPoint(currentPos.Value);
            Vector2 direction = (currentWorldPos - _dragStartPosition).normalized;
            OnDrag?.Invoke(direction);
        }

        if (released && _isDragging)
        {
            _isDragging = false;
            OnRelease?.Invoke();
        }
    }
}
