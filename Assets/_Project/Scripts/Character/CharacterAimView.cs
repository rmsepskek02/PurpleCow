using UnityEngine;

// 조준 방향(BallLauncher.Instance.LaunchDirection)에 맞춰 캐릭터 몸통/머리를 좌우 반전하고
// 무기(WeaponPivot)를 회전시키는 뷰 전용 컴포넌트. TrajectoryPreview.cs와 동일하게
// 이벤트 구독이 아니라 매 프레임 폴링 방식을 사용한다(InputHandler.OnDrag는 터치가 없을 때
// 발행되지 않아 회전이 멈추는 문제가 있기 때문).
public class CharacterAimView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _bodySpriteRenderer;
    [SerializeField] private SpriteRenderer _headSpriteRenderer;
    [SerializeField] private Transform _headTransform;
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private float _headRotationRatio = 0.25f;

    private Vector3 _weaponPivotBaseLocalPosition;

    private void Awake()
    {
        _weaponPivotBaseLocalPosition = _weaponPivot.localPosition;
    }

    private void Update()
    {
        UpdateAim(BallLauncher.Instance.LaunchDirection);
    }

    private void UpdateAim(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        bool facingLeft = direction.x < 0f;
        _bodySpriteRenderer.flipX = facingLeft;
        _headSpriteRenderer.flipX = facingLeft;

        // WeaponPivot 위치도 반전 방향(어깨 쪽)으로 따라가야 하므로 x부호만 뒤집는다.
        Vector3 pivotPos = _weaponPivotBaseLocalPosition;
        pivotPos.x = facingLeft ? -Mathf.Abs(pivotPos.x) : Mathf.Abs(pivotPos.x);
        _weaponPivot.localPosition = pivotPos;

        // WeaponPivot의 로컬 좌표계는 SpriteRenderer.flipX와 무관하게 facingLeft 여부와 상관없이
        // 항상 월드 좌표계와 동일하므로, 방향 미러링 없이 원본 direction으로 그대로 각도를 계산한다.
        float weaponAngle = 90f - Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; // Unity Z축 회전은 CW이므로 부호 반전(90 - atan2)
        _weaponPivot.localRotation = Quaternion.Euler(0f, 0f, weaponAngle);

        // 머리는 무기 회전각의 일부 비율만 보조적으로 반영("방향을 대략 암시"하는 용도)
        _headTransform.localRotation = Quaternion.Euler(0f, 0f, weaponAngle * _headRotationRatio);
    }
}
