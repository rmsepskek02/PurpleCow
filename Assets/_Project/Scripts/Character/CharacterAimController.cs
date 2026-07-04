using UnityEngine;

// BallLauncher.LaunchDirection(현재 조준 방향)을 읽어 Weapon Transform을 조준 방향으로
// 부드럽게 회전시킨다. Body/Head는 고정 파츠이며 이 스크립트의 회전 대상이 아니다.
public class CharacterAimController : MonoBehaviour
{
    [SerializeField] private Transform _weaponTransform;

    // 무기 스프라이트 원본 그림이 기본(0도) 상태에서 이미 대각선으로 그려져 있어 필요한 보정값.
    // 정확한 부호(+18 vs -18)는 Unity에서 실제로 회전시켜보며 시각적으로 확정한다.
    [SerializeField] private float _rotationOffset = -18f;

    // 좌우 클램프 각도. 원본 게임은 위쪽 반원(±90도)만 허용한다.
    [SerializeField] private float _clampAngle = 90f;

    // 회전 속도(도/초). Mathf.MoveTowardsAngle의 보간 속도로 사용.
    [SerializeField] private float _rotationSpeed = 360f;

    private float _currentAngle;

    private void Update()
    {
        if (_weaponTransform == null || BallLauncher.Instance == null)
            return;

        Vector2 direction = BallLauncher.Instance.LaunchDirection;

        float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        targetAngle += _rotationOffset;
        targetAngle = Mathf.Clamp(targetAngle, -_clampAngle, _clampAngle);

        _currentAngle = Mathf.MoveTowardsAngle(_currentAngle, targetAngle, _rotationSpeed * Time.deltaTime);

        _weaponTransform.localRotation = Quaternion.Euler(0f, 0f, -_currentAngle);
    }
}
