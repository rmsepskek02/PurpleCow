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
    [SerializeField] private float _horizontalBiasDegrees = 15f;

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

        // 루트가 반전되면 자식 회전도 화면에 같이 미러링되어 보이므로, 반전 상태일 때는
        // 목표 방향의 x부호를 미리 뒤집어서 로컬 회전을 계산해야 최종 결과가 실제 조준 방향과 일치한다.
        Vector2 localTargetDir = mirrored ? new Vector2(-direction.x, direction.y) : direction;
        Quaternion weaponRotation = Quaternion.FromToRotation(Vector3.up, localTargetDir);

        // 조준 방향이 수평에 가까울수록(Vector3.up에서 많이 벗어날수록) 지팡이가 조금 더 눕도록 추가 보정한다.
        weaponRotation.ToAngleAxis(out float angle, out Vector3 axis);
        float horizontalness = Mathf.Clamp01(angle / 90f);
        weaponRotation = Quaternion.AngleAxis(angle + _horizontalBiasDegrees * horizontalness, axis);

        _weaponPivot.localRotation = weaponRotation;

        // 머리는 무기 회전의 일부 비율만 보조적으로 따라가도록 보간한다.
        _headTransform.localRotation = Quaternion.Slerp(Quaternion.identity, weaponRotation, _headRotationRatio);
    }
}
