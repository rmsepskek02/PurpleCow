using UnityEngine;

// BallLauncher.LaunchDirection(조준 방향)을 매 프레임 읽어 캐릭터 파츠(Body/Head/Weapon)의
// 좌우 반전과 회전을 갱신한다. 반전은 SpriteRenderer.flipX만 사용하며 회전 각도 계산에는
// 관여하지 않는다(localScale 반전 금지 — 반전/회전 부호 충돌 방지를 위한 확정된 설계).
public class CharacterAimController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _bodyRenderer;
    [SerializeField] private SpriteRenderer _headRenderer;
    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private float _headDampFactor = 0.25f;
    [SerializeField] private float _flipDeadzone = 0.05f;

    private bool _facingRight = true;

    private void Update()
    {
        Vector2 direction = BallLauncher.Instance.LaunchDirection;

        // 스프라이트 기본 정면이 위쪽(Vector2.up)을 향하도록 그려졌다고 가정한 오프셋(-90f).
        // 실제 스프라이트 기본 방향이 다르면 이 값은 추후 실제 플레이 확인 후 조정 가능하다.
        float aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // 좌우 반전 판정: 데드존 구간에서는 이전 상태를 유지해 떨림을 방지한다.
        if (direction.x > _flipDeadzone)
            _facingRight = true;
        else if (direction.x < -_flipDeadzone)
            _facingRight = false;

        _bodyRenderer.flipX = _headRenderer.flipX = _weaponRenderer.flipX = !_facingRight;

        // Weapon: 감쇠 없이 조준 방향을 거의 그대로 따라간다.
        _weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle);

        // Head: 감쇠 계수를 적용해 약하게 갸웃하는 정도로만 회전한다.
        _headRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle * _headDampFactor);

        // Body는 회전하지 않고 flipX만 적용한다.
    }
}
