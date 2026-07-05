using UnityEngine;

// BallLauncher.LaunchDirection(현재 조준 방향)을 읽어 WeaponPivot과 Head를 조준 방향으로
// 즉시 회전시키고, 조준 방향의 좌우 부호에 따라 캐릭터 전체를 좌우 반전시킨다.
// 방향(좌우 어디를 바라볼지)은 direction.x의 부호로만 결정하고, 회전량(얼마나 돌지)은
// direction.x의 절댓값과 direction.y로 계산한 "항상 0 이상인 각도"만 사용한다.
// 이렇게 하면 좌우 반전 상태와 무관하게 항상 같은 회전 공식을 적용할 수 있어
// 반전 보정 계수가 필요 없다.
// 이 스크립트는 Character 프리팹의 루트 오브젝트에 부착된다(좌우 반전은 자기 자신의 localScale.x를 사용).
public class CharacterAimController : MonoBehaviour
{
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private Transform _head;

    // 무기 회전 클램프. 원본 게임은 위쪽 반원(0~90도)만 허용한다.
    [SerializeField] private float _weaponClampAngle = 90f;

    // 머리 회전 클램프. 무기보다 훨씬 좁게(0~10도) 제자리 tilt만 한다.
    [SerializeField] private float _headClampAngle = 10f;

    private void Update()
    {
        if (BallLauncher.Instance == null)
            return;

        Vector2 direction = BallLauncher.Instance.LaunchDirection;

        // 좌우 반전: 방향의 좌우는 direction.x 부호로만 결정한다(오른쪽=scale.x>0, 왼쪽=scale.x<0).
        Vector3 scale = transform.localScale;
        scale.x = direction.x >= 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        // 회전량은 x의 절댓값과 y로 계산한 항상 0 이상인 각도만 사용한다.
        // 좌우 어느 쪽을 바라보든 같은 공식이 적용되므로 반전 보정이 필요 없다.
        float angle = Mathf.Atan2(Mathf.Abs(direction.x), direction.y) * Mathf.Rad2Deg;

        // 무기: 0~90도 클램프, WeaponPivot에 적용.
        float weaponAngle = Mathf.Clamp(angle, 0f, _weaponClampAngle);
        if (_weaponPivot != null)
            _weaponPivot.localRotation = Quaternion.Euler(0f, 0f, -weaponAngle);

        // 머리: 같은 각도를 0~10도로 다시 좁게 클램프해 제자리 tilt만 적용.
        float headAngle = Mathf.Clamp(angle, 0f, _headClampAngle);
        if (_head != null)
            _head.localRotation = Quaternion.Euler(0f, 0f, -headAngle);
    }
}
