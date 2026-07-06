using UnityEngine;

[ExecuteAlways]
public class WallFitter : MonoBehaviour
{
    private const float MinimumLaunchClearance = 0.35f;

    [SerializeField] private Camera _targetCamera;
    [SerializeField] private SpriteRenderer _backgroundSpriteRenderer;
    [SerializeField] private Transform _wallLeft;
    [SerializeField] private Transform _wallRight;
    [SerializeField] private Transform _wallTop;
    [SerializeField] private Transform _ground;
    [SerializeField] private Transform _launchPoint;
    [SerializeField] private float _nativeLeftX = -6.5f;
    [SerializeField] private float _nativeRightX = 6.3f;
    [SerializeField] private float _nativeTopY = 6.0f;
    [SerializeField] private float _nativeBottomY = -7.5f;
    [SerializeField] private float _nativeLaunchPointY = -6.7f;
    [SerializeField] private float _zoomFactor = 0.5f;
    [SerializeField] private float _cellAspectCorrection = 1.647f;
    [SerializeField] private float _gridAreaWidth = 14.53f;
    [SerializeField] private float _gridAreaHeight = 10.16f;

    private void Start()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void Apply()
    {
        if (_targetCamera == null || _backgroundSpriteRenderer == null) return;

        Vector2 camSize = new Vector2(
            _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
            _targetCamera.orthographicSize * 2f);

        float scaleXNeeded = camSize.x / _gridAreaWidth;
        float scaleYNeeded = camSize.y / (_gridAreaHeight * _cellAspectCorrection);

        float uniformScale = Mathf.Max(scaleXNeeded, scaleYNeeded) * _zoomFactor;

        float scaleX = uniformScale;
        float scaleY = uniformScale * _cellAspectCorrection;

        SetX(_wallLeft, _nativeLeftX * scaleX);
        SetX(_wallRight, _nativeRightX * scaleX);
        SetY(_wallTop, _nativeTopY * scaleY);
        float groundY = _nativeBottomY * scaleY;
        float launchPointY = _nativeLaunchPointY * scaleY;
        launchPointY = Mathf.Max(launchPointY, groundY + MinimumLaunchClearance);

        SetY(_ground, groundY);
        SetY(_launchPoint, launchPointY);
    }

    private static void SetX(Transform t, float x)
    {
        if (t == null) return;
        Vector3 p = t.position;
        p.x = x;
        t.position = p;
    }

    private static void SetY(Transform t, float y)
    {
        if (t == null) return;
        Vector3 p = t.position;
        p.y = y;
        t.position = p;
    }
}
