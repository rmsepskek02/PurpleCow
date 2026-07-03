using UnityEngine;

[ExecuteAlways]
public class BackgroundFitter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private float _zoomFactor = 1.3f;

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
        if (_spriteRenderer == null || _targetCamera == null) return;

        Vector2 camSize = new Vector2(
            _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
            _targetCamera.orthographicSize * 2f);
        Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;
        transform.localScale = new Vector3(
            camSize.x / spriteSize.x * _zoomFactor,
            camSize.y / spriteSize.y * _zoomFactor,
            1f);
    }
}
