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

    private Vector3 _headBasePosition;
    private Vector3 _bodyBasePosition;
    private Vector3 _weaponBasePosition;

    private void Start()
    {
        _headBasePosition   = _headRenderer.transform.localPosition;
        _bodyBasePosition   = _bodyRenderer.transform.localPosition;
        _weaponBasePosition = _weaponRenderer.transform.localPosition;
    }

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

        // flipX는 스프라이트 비트맵만 반전시킬 뿐 Transform.localPosition에는 영향을 주지 않으므로,
        // 정면 기준 위치에서 벗어난 파츠(Head/Body)는 좌우 반전 시 X 좌표 부호도 함께 뒤집어야 한다.
        float sign = _facingRight ? 1f : -1f;
        _headRenderer.transform.localPosition   = new Vector3(_headBasePosition.x * sign, _headBasePosition.y, _headBasePosition.z);
        _bodyRenderer.transform.localPosition   = new Vector3(_bodyBasePosition.x * sign, _bodyBasePosition.y, _bodyBasePosition.z);
        _weaponRenderer.transform.localPosition = new Vector3(_weaponBasePosition.x * sign, _weaponBasePosition.y, _weaponBasePosition.z);

        // Weapon: 감쇠 없이 조준 방향을 거의 그대로 따라간다.
        _weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle);

        // Head: 감쇠 계수를 적용해 약하게 갸웃하는 정도로만 회전한다.
        _headRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, aimAngle * _headDampFactor);

        // Body는 회전하지 않고 flipX만 적용한다.
    }
}
