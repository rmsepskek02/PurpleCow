using UnityEngine;

public class BackgroundFitter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Camera _targetCamera;

    private void Start()
    {
        if (_spriteRenderer == null || _targetCamera == null) return;

        Vector2 camSize = new Vector2(
            _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
            _targetCamera.orthographicSize * 2f);
        Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;
        transform.localScale = new Vector3(camSize.x / spriteSize.x, camSize.y / spriteSize.y, 1f);
    }
}
