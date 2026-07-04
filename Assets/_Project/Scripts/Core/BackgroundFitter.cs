using UnityEngine;

[ExecuteAlways]
public class BackgroundFitter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Camera _targetCamera;
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
        if (_spriteRenderer == null || _targetCamera == null) return;

        Vector2 camSize = new Vector2(
            _targetCamera.orthographicSize * 2f * _targetCamera.aspect,
            _targetCamera.orthographicSize * 2f);

        float scaleXNeeded = camSize.x / _gridAreaWidth;
        float scaleYNeeded = camSize.y / (_gridAreaHeight * _cellAspectCorrection);

        float uniformScale = Mathf.Max(scaleXNeeded, scaleYNeeded) * _zoomFactor;

        float scaleX = uniformScale;
        float scaleY = uniformScale * _cellAspectCorrection;

        transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }
}
