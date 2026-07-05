using UnityEngine;

// BallLauncher.LaunchDirection(현재 조준 방향)을 읽어 WeaponPivot과 Head를 조준 방향으로
// 즉시 회전시키고, 조준 각도의 부호에 따라 캐릭터 전체를 좌우 반전시킨다.
// 이 스크립트는 Character 프리팹의 루트 오브젝트에 부착된다(좌우 반전은 자기 자신의 localScale.x를 사용).
public class CharacterAimController : MonoBehaviour
{
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private Transform _head;

    // 무기 회전 클램프. 원본 게임은 위쪽 반원(±90도)만 허용한다.
    [SerializeField] private float _weaponClampAngle = 90f;

    // 머리 회전 클램프. 무기보다 훨씬 좁게(±10도) 제자리 tilt만 한다.
    [SerializeField] private float _headClampAngle = 10f;

    // 좌우 반전(localScale.x < 0) 상태일 때 WeaponPivot/Head에 적용하는 회전 부호 보정 계수.
    // 부모(Character 루트)가 좌우 반전되면 자식의 로컬 회전이 시각적으로 반대 방향으로 보이므로
    // 보정이 필요하다. 기본값(-1)은 수학적으로 유도한 값이나 Unity 시각 검증 전에는 100% 확신할 수
    // 없으므로, 실제로 좌우 조준 시 무기 방향이 어색하면 Inspector에서 이 값을 +1로 뒤집어 확인한다.
    [SerializeField] private float _mirroredRotationSign = -1f;

    private float _currentWeaponAngle;
    private float _currentHeadAngle;

    private void Update()
    {
        if (BallLauncher.Instance == null)
            return;

        Vector2 direction = BallLauncher.Instance.LaunchDirection;

        // Vector2.up 기준 각도. 오른쪽(+x)으로 갈수록 양수, 왼쪽(-x)으로 갈수록 음수.
        // 18도 오프셋 보정은 삭제되었다(원본 게임 무기 연출은 정밀한 방향 지시자가 아니므로 불필요).
        float targetAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        // 좌우 반전: 목표 각도가 양수면 오른쪽 조준 자세(scale.x=+1), 음수면 왼쪽 조준 자세(scale.x=-1).
        Vector3 scale = transform.localScale;
        scale.x = targetAngle >= 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;

        bool isMirrored = transform.localScale.x < 0f;
        float mirrorSign = isMirrored ? _mirroredRotationSign : 1f;

        // 무기: ±90도 클램프, WeaponPivot에 적용.
        float weaponTarget = Mathf.Clamp(targetAngle, -_weaponClampAngle, _weaponClampAngle);
        _currentWeaponAngle = weaponTarget;
        if (_weaponPivot != null)
            _weaponPivot.localRotation = Quaternion.Euler(0f, 0f, -_currentWeaponAngle * mirrorSign);

        // 머리: 같은 목표 각도를 ±10도로 다시 좁게 클램프해 제자리 tilt만 적용.
        float headTarget = Mathf.Clamp(targetAngle, -_headClampAngle, _headClampAngle);
        _currentHeadAngle = headTarget;
        if (_head != null)
            _head.localRotation = Quaternion.Euler(0f, 0f, -_currentHeadAngle * mirrorSign);
    }
}
