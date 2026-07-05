using UnityEngine;

public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rectTransform;
    private Canvas        _canvas;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas        = GetComponentInParent<Canvas>();
        Refresh();
    }

    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea ||
            _lastScreenSize.x != Screen.width ||
            _lastScreenSize.y != Screen.height)
            Refresh();
    }

    private void Refresh()
    {
        _lastSafeArea = Screen.safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        ApplySafeArea(_lastSafeArea);
    }

    private void ApplySafeArea(Rect safeArea)
    {
        if (_canvas == null) return;

        Vector2 min = safeArea.position;
        Vector2 max = safeArea.position + safeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;
        _rectTransform.anchorMin = min;
        _rectTransform.anchorMax = max;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;
    }
}
