using UnityEngine;

public class WallFitter : MonoBehaviour
{
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private SpriteRenderer _backgroundSpriteRenderer;
    [SerializeField] private Transform _wallLeft;
    [SerializeField] private Transform _wallRight;
    [SerializeField] private Transform _wallTop;
    [SerializeField] private Transform _ground;
    [SerializeField] private float _nativeLeftX = -6.04f;
    [SerializeField] private float _nativeRightX = 5.89f;
    [SerializeField] private float _nativeTopY = 5.55f;
    [SerializeField] private float _nativeBottomY = -5.33f;

    private void Start()
    {
        if (_targetCamera == null || _backgroundSpriteRenderer == null) return;

        Vector2 camSize = new Vector2(
            _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
            _targetCamera.orthographicSize * 2f);
        Vector2 spriteSize = _backgroundSpriteRenderer.sprite.bounds.size;
        float scaleX = camSize.x / spriteSize.x;
        float scaleY = camSize.y / spriteSize.y;

        SetX(_wallLeft, _nativeLeftX * scaleX);
        SetX(_wallRight, _nativeRightX * scaleX);
        SetY(_wallTop, _nativeTopY * scaleY);
        SetY(_ground, _nativeBottomY * scaleY);
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
