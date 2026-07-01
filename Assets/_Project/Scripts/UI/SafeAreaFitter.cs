using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Canvas        _canvas;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas        = GetComponentInParent<Canvas>();
        ApplySafeArea(Screen.safeArea);
    }

    private void ApplySafeArea(Rect safeArea)
    {
        if (_canvas == null) return;

        Vector2 canvasSize = _canvas.pixelRect.size;
        Vector2 offsetMin  = safeArea.position;
        Vector2 offsetMax  = safeArea.position + safeArea.size - canvasSize;

        _rectTransform.offsetMin = offsetMin;
        _rectTransform.offsetMax = offsetMax;
    }
}
