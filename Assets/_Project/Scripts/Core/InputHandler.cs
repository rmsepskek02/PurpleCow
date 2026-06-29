using System;
using UnityEngine;

public class InputHandler : Singleton<InputHandler>
{
    public event Action<Vector2> OnDrag;
    public event Action OnRelease;

    private Vector2 _dragStartPosition;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragStartPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(0))
        {
            Vector2 currentPosition = Input.mousePosition;
            Vector2 direction = (currentPosition - _dragStartPosition).normalized;
            OnDrag?.Invoke(direction);
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnRelease?.Invoke();
        }
    }
}
