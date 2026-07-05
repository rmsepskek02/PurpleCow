using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public static event Action OnAimBegin;
    public static event Action<Vector2> OnDrag;
    public static event Action OnRelease;

    private Camera _mainCamera;

    private bool _isDragging;
    private bool _isPointerBlockedByUI;

    protected override void Awake()
    {
        base.Awake();
        _mainCamera = Camera.main;
    }

    // 발사 지점(BallLauncher.Instance.LaunchPoint)에서 screenPos(스크린 좌표)를 향하는 절대 조준 방향을 계산한다.
    private Vector2 ComputeAimDirection(Vector2 screenPos)
    {
        Vector2 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.y = Mathf.Max(worldPos.y, WaveManager.Instance.BottomBoundaryY);
        Vector2 launchPointPos = BallLauncher.Instance.LaunchPoint.position;
        return (worldPos - launchPointPos).normalized;
    }

    private void Update()
    {
        Vector2? touchPos = null;
        bool     released = false;

        if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {
            var touch = Touchscreen.current.touches[0];
            var phase = touch.phase.ReadValue();
            Vector2 position = touch.position.ReadValue();

            bool isActiveTouch =
                phase == UnityEngine.InputSystem.TouchPhase.Began ||
                phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                phase == UnityEngine.InputSystem.TouchPhase.Stationary;

            if (isActiveTouch && !_isDragging && !_isPointerBlockedByUI)
                _isPointerBlockedByUI = IsPointerOverUI(position);

            if (_isPointerBlockedByUI)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                    phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    _isPointerBlockedByUI = false;

                return;
            }

            if (isActiveTouch)
                touchPos = position;
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                     phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                released = true;
        }
        else if (Mouse.current != null)
        {
            Vector2 position = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame)
                _isPointerBlockedByUI = IsPointerOverUI(position);

            if (Mouse.current.leftButton.isPressed && !_isPointerBlockedByUI)
                touchPos = position;

            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                released = !_isPointerBlockedByUI;
                _isPointerBlockedByUI = false;
            }
        }

        if (touchPos.HasValue)
        {
            if (!_isDragging)
            {
                _isDragging = true;
                OnAimBegin?.Invoke();
            }
            OnDrag?.Invoke(ComputeAimDirection(touchPos.Value));
        }

        if (released && _isDragging)
        {
            _isDragging = false;
            OnRelease?.Invoke();
        }
    }

    private static bool IsPointerOverUI(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(result.gameObject) != null)
                return true;
        }

        return false;
    }
}
