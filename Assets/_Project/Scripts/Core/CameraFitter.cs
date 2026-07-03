using UnityEngine;

public class CameraFitter : MonoBehaviour
{
    [SerializeField] private Camera _targetCamera;
    [SerializeField] private float _baseOrthographicSize = 10f;
    [SerializeField] private float _requiredHalfWidth = 5.6f;

    private void Awake()
    {
        if (_targetCamera == null) return;

        float requiredSize = _requiredHalfWidth / _targetCamera.aspect;
        _targetCamera.orthographicSize = Mathf.Max(_baseOrthographicSize, requiredSize);
    }
}
