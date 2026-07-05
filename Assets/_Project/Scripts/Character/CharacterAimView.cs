using UnityEngine;

// 조준 방향(BallLauncher.Instance.LaunchDirection)에 맞춰 캐릭터 루트를 좌우 반전하고
// 무기를 회전시키는 뷰 전용 컴포넌트. TrajectoryPreview.cs와 동일하게
// 이벤트 구독이 아니라 매 프레임 폴링 방식을 사용한다(InputHandler.OnDrag는 터치가 없을 때
// 발행되지 않아 회전이 멈추는 문제가 있기 때문).
public class CharacterAimView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _bodySpriteRenderer;
    [SerializeField] private SpriteRenderer _headSpriteRenderer;
    [SerializeField] private Transform _headTransform;
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private float _headRotationRatio = 0.25f;

    private void Update()
    {
        UpdateAim(BallLauncher.Instance.LaunchDirection);
    }

    private void UpdateAim(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        bool mirrored = direction.x > 0f; // 캐릭터 기본 아트가 왼쪽 기준이므로, 오른쪽 조준일 때만 반전
        transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);

        float weaponAngle = 90f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (mirrored) weaponAngle = -weaponAngle; // 루트 반전 시 회전 방향도 같이 뒤집히므로 보정
        _weaponPivot.localRotation = Quaternion.Euler(0f, 0f, weaponAngle);

        _headTransform.localRotation = Quaternion.Euler(0f, 0f, weaponAngle * _headRotationRatio);
    }
}
