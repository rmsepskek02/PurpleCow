using UnityEngine;
using DG.Tweening;

// 조준 방향(BallLauncher.Instance.LaunchDirection)에 맞춰 캐릭터 루트를 좌우 반전하고
// 무기를 회전시키는 뷰 전용 컴포넌트. TrajectoryPreview.cs와 동일하게
// 이벤트 구독이 아니라 매 프레임 폴링 방식을 사용한다(InputHandler.OnDrag는 터치가 없을 때
// 발행되지 않아 회전이 멈추는 문제가 있기 때문).
// 발사 순간의 반동 연출만은 예외적으로 BallLauncher.OnBallLaunched 이벤트를 구독해 1회성으로 재생한다.
public class CharacterAimView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _bodySpriteRenderer;
    [SerializeField] private SpriteRenderer _headSpriteRenderer;
    [SerializeField] private Transform _headTransform;
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private float _headRotationRatio = 0.25f;
    [SerializeField] private float _horizontalBiasDegrees = 15f;
    [SerializeField] private float _recoilPunchStrength = 0.15f;
    [SerializeField] private float _recoilDuration = 0.2f;

    private void OnEnable()
    {
        BallLauncher.OnBallLaunched += HandleBallLaunched;
    }

    private void OnDisable()
    {
        BallLauncher.OnBallLaunched -= HandleBallLaunched;
    }

    private void Update()
    {
        UpdateAim(BallLauncher.Instance.LaunchDirection);
    }

    // 발사 순간(총기 반동처럼) 캐릭터 루트를 발사 방향의 반대쪽으로 짧게 밀었다가 복귀시킨다.
    // localPosition은 부모(LaunchPoint)의 좌표계 기준이므로, 부모의 회전을 반영해 월드 반동 방향을
    // 부모 로컬 방향으로 변환한다(자기 자신의 좌우 반전 scale은 자신의 localPosition 해석에
    // 영향을 주지 않으므로 mirrored 보정은 필요 없다).
    private void HandleBallLaunched()
    {
        Vector3 worldRecoilDir = -(Vector3)BallLauncher.Instance.LaunchDirection;
        Vector3 localRecoilDir = transform.parent != null
            ? transform.parent.InverseTransformDirection(worldRecoilDir)
            : worldRecoilDir;

        DOTween.Kill(transform);
        transform.DOPunchPosition(localRecoilDir.normalized * _recoilPunchStrength, _recoilDuration);
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

        // 조준 각도와 무관하게 항상 고정된 보정치만큼 추가 회전시킨다.
        weaponRotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (axis.sqrMagnitude < 0.0001f || angle < 0.0001f) axis = Vector3.forward; // 조준이 정확히 위쪽이면(angle=0) 회전축이 정의되지 않으므로, 2D 회전 평면인 Z축으로 대체
        weaponRotation = Quaternion.AngleAxis(angle + _horizontalBiasDegrees, axis);

        _weaponPivot.localRotation = weaponRotation;

        // 머리는 무기 회전의 일부 비율만 보조적으로 따라가도록 보간한다.
        _headTransform.localRotation = Quaternion.Slerp(Quaternion.identity, weaponRotation, _headRotationRatio);
    }
}
