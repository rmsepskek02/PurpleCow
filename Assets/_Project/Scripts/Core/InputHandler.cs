using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public static event Action<Vector2> OnDrag;
    public static event Action OnRelease;

    private Vector2 _dragStartPosition;
    private bool _isDragging;

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
            _dragStartPosition = pressedPos.Value;
            _isDragging = true;
        }

        if (currentPos.HasValue && _isDragging)
        {
            Vector2 direction = (currentPos.Value - _dragStartPosition).normalized;
            OnDrag?.Invoke(direction);
        }

        if (released && _isDragging)
        {
            _isDragging = false;
            OnRelease?.Invoke();
        }
    }
}
