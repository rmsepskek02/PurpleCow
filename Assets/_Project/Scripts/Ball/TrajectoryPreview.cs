using UnityEngine;

// 터치 여부와 무관하게 매 프레임 1차/2차 충돌 지점을 계산해
// 점선 궤적과 2차 충돌 지점의 레드닷/원형 궤적선을 표시하는 궤적 프리뷰.
// GameplayMechanics.md 섹션 1 스펙을 그대로 따른다(2단계 점선 + 2차 지점 레드닷/링, 3차 충돌 이후 미표시).
public class TrajectoryPreview : MonoBehaviour
{
    private const float MAX_RAY_DISTANCE   = 50f;
    private const int   CIRCLE_SEGMENTS    = 24;
    private const float DASH_WORLD_SIZE    = 0.15f;
    private const int   RING_DASH_COUNT    = 4; // 고리(_hitRing)에 보여야 하는 호(dash) 개수

    [SerializeField] private float _lineWidth  = 0.05f;
    [SerializeField] private Color _lineColor  = new Color32(225, 225, 220, 255);
    [SerializeField] private Color _hitColor   = new Color32(206, 90, 82, 255);
    [SerializeField] private Color _ringColor  = new Color32(225, 225, 220, 255);
    [SerializeField] private float _dotRadius  = 0.05f;
    [SerializeField] private float _ringRadius = 0.3f;
    [SerializeField] private float _ringRotationSpeed = 90f; // 고리 시계방향 회전 속도(deg/sec)

    private static readonly string[] _blockingTags = { "Wall", "Ground", "Monster" };

    private LineRenderer _trajectoryLine;
    private LineRenderer _hitDot;
    private LineRenderer _hitRing;

    private void Awake()
    {
        _trajectoryLine = CreateLineRenderer("TrajectoryLine", _lineWidth, _lineColor, CreateDashTexture(), 1f / DASH_WORLD_SIZE);
        _hitDot         = CreateLineRenderer("HitDot",  _dotRadius * 1.6f, _hitColor,  CreateSolidTexture(), 1f);

        // 고리 둘레(2πr) 기준으로 텍스처가 정확히 RING_DASH_COUNT번 반복되도록 스케일을 계산한다.
        // CreateRingDashTexture()가 50:50(불투명:투명) 텍스처이므로 이 반복 횟수가 곧 보이는 호 개수가 된다.
        float ringCircumference = 2f * Mathf.PI * _ringRadius;
        float ringTextureScaleX = RING_DASH_COUNT / ringCircumference;
        _hitRing = CreateLineRenderer("HitRing", _lineWidth, _ringColor, CreateRingDashTexture(), ringTextureScaleX);

        _hitDot.loop  = true;
        // _hitRing은 loop을 쓰지 않는다. Unity LineRenderer가 loop = true + textureMode = Tile을
        // 함께 쓸 때, 자동으로 닫히는 마지막 구간(마지막 정점 -> 첫 정점)의 길이가 텍스처 타일링
        // 누적 길이 계산에 반영되지 않는 경우가 있어(과거 시도에서 목표 10개가 실제 2개로 보였던
        // 원인으로 추정) DrawCircle()에서 명시적으로 닫는 정점을 추가하는 방식(explicitClose)으로 대체한다.
        _hitRing.loop = false;

        SetVisible(true);
    }

    // 터치 여부와 무관하게 매 프레임 궤적을 재계산한다.
    // BallLauncher.LaunchDirection은 터치 중에는 드래그 방향, 터치하지 않을 때는
    // 마지막 조준 방향(기본값 Vector2.up)을 담고 있어 별도 상태 분기가 필요 없다.
    private void Update()
    {
        UpdateTrajectory(BallLauncher.Instance.LaunchDirection);
    }

    private void UpdateTrajectory(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Vector2 origin = BallLauncher.Instance.LaunchPoint.position;

        if (!TryBlockingRaycast(origin, direction, out RaycastHit2D hit1))
        {
            _trajectoryLine.positionCount = 2;
            _trajectoryLine.SetPosition(0, origin);
            _trajectoryLine.SetPosition(1, origin + direction * MAX_RAY_DISTANCE);
            SetHitMarkersVisible(false);
            return;
        }

        Vector2 reflectDir = Vector2.Reflect(direction, hit1.normal).normalized;
        Vector2 secondRayOrigin = hit1.point + reflectDir * 0.01f;

        bool hasSecondHit = TryBlockingRaycast(secondRayOrigin, reflectDir, out RaycastHit2D hit2);

        _trajectoryLine.positionCount = hasSecondHit ? 3 : 2;
        _trajectoryLine.SetPosition(0, origin);
        _trajectoryLine.SetPosition(1, hit1.point);

        if (hasSecondHit)
        {
            _trajectoryLine.SetPosition(2, hit2.point);
            DrawCircle(_hitDot,  hit2.point, _dotRadius);
            // 터치 여부와 무관하게 항상 진행되는 Update()/UpdateTrajectory() 흐름을 그대로 타므로
            // 조준 중이 아닐 때도 고리는 계속 회전한다. Time.time 기반으로 각도 오프셋을 누적한다.
            float ringRotationOffsetDeg = Time.time * _ringRotationSpeed;
            // explicitClose: true로 마지막 정점을 0번 정점(회전 오프셋 반영된 위치)과 동일하게
            // 명시적으로 채워 원을 닫는다. loop에 의존하지 않으므로 폴리라인 총 길이가 정확히
            // 둘레 길이와 일치해 텍스처 타일링 계산(Awake의 ringTextureScaleX)이 어긋나지 않는다.
            DrawCircle(_hitRing, hit2.point, _ringRadius, ringRotationOffsetDeg, explicitClose: true);
            SetHitMarkersVisible(true);
        }
        else
        {
            _trajectoryLine.SetPosition(2, hit1.point + reflectDir * MAX_RAY_DISTANCE);
            SetHitMarkersVisible(false);
        }
    }

    // Wall/Ground/Monster 태그를 가진 콜라이더만 유효한 충돌로 취급한다.
    // 로스터 사이클로 상시 비행 중인 다른 볼의 콜라이더(태그 없음)는 자연히 무시된다.
    private static bool TryBlockingRaycast(Vector2 origin, Vector2 direction, out RaycastHit2D result)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, MAX_RAY_DISTANCE);
        System.Array.Sort(hits, (a, b) => a.fraction.CompareTo(b.fraction));

        foreach (RaycastHit2D hit in hits)
        {
            if (IsBlockingTag(hit.collider.tag))
            {
                result = hit;
                return true;
            }
        }

        result = default;
        return false;
    }

    private static bool IsBlockingTag(string tag)
    {
        for (int i = 0; i < _blockingTags.Length; i++)
        {
            if (_blockingTags[i] == tag)
                return true;
        }
        return false;
    }

    // rotationOffsetDeg가 0이면 기존과 동일하게 동작한다(_hitDot은 항상 기본값 0으로 호출).
    // Unity 2D 좌표계(Y+ 위쪽)에서 cos/sin 기준 angle이 증가할수록 반시계 방향으로 움직이므로,
    // 시계 방향으로 보이려면 시간에 따라 커지는 오프셋을 angle에서 "빼야" 한다.
    // explicitClose가 true이면(_hitRing 전용) 정점을 CIRCLE_SEGMENTS + 1개로 만들어 마지막 정점을
    // 0번 정점과 동일한 위치로 채워 loop 없이도 원을 명시적으로 닫는다(_hitDot은 기존처럼
    // CIRCLE_SEGMENTS개 정점 + loop = true를 그대로 사용하므로 explicitClose를 쓰지 않는다).
    private static void DrawCircle(LineRenderer lr, Vector2 center, float radius, float rotationOffsetDeg = 0f, bool explicitClose = false)
    {
        lr.positionCount = explicitClose ? CIRCLE_SEGMENTS + 1 : CIRCLE_SEGMENTS;
        float offsetRad = rotationOffsetDeg * Mathf.Deg2Rad;
        Vector3 firstPoint = Vector3.zero;
        for (int i = 0; i < CIRCLE_SEGMENTS; i++)
        {
            float angle = (i / (float)CIRCLE_SEGMENTS) * Mathf.PI * 2f - offsetRad;
            Vector3 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            lr.SetPosition(i, point);
            if (i == 0)
                firstPoint = point;
        }

        if (explicitClose)
            lr.SetPosition(CIRCLE_SEGMENTS, firstPoint);
    }

    private void SetVisible(bool visible)
    {
        _trajectoryLine.enabled = visible;
        SetHitMarkersVisible(visible);
    }

    private void SetHitMarkersVisible(bool visible)
    {
        _hitDot.enabled  = visible;
        _hitRing.enabled = visible;
    }

    private LineRenderer CreateLineRenderer(string childName, float width, Color color, Texture2D texture, float textureScaleX)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.startWidth     = width;
        lr.endWidth       = width;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 4;
        lr.startColor     = color;
        lr.endColor       = color;
        lr.sortingOrder   = 100;
        lr.textureMode    = LineTextureMode.Tile;

        var material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = texture;
        material.mainTextureScale = new Vector2(textureScaleX, 1f);
        lr.material = material;

        return lr;
    }

    // 점선 표현용 텍스처: 앞쪽 절반 불투명, 뒤쪽 절반 투명.
    private static Texture2D CreateDashTexture()
    {
        var tex = new Texture2D(4, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode   = TextureWrapMode.Repeat;
        tex.SetPixels(new[]
        {
            Color.white, Color.white,
            new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f)
        });
        tex.Apply();
        return tex;
    }

    // 고리(_hitRing) 전용 점선 텍스처: CreateDashTexture()와 동일하게 앞쪽 절반 불투명/뒤쪽
    // 절반 투명(50:50)으로 구성한다. 레퍼런스(targetUI/circle.jpg)의 호가 간격보다 두꺼워
    // 보이는 것과 시각적으로 맞아떨어지는 비율이며, 궤적선과 별도 메서드로 분리해 두어
    // 향후 고리 쪽 비율만 독립적으로 조정할 수 있게 한다.
    private static Texture2D CreateRingDashTexture()
    {
        var tex = new Texture2D(4, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode   = TextureWrapMode.Repeat;
        tex.SetPixels(new[]
        {
            Color.white, Color.white,
            new Color(1f, 1f, 1f, 0f), new Color(1f, 1f, 1f, 0f)
        });
        tex.Apply();
        return tex;
    }

    // 레드닷(_hitDot) 전용 단색 텍스처.
    private static Texture2D CreateSolidTexture()
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return tex;
    }
}
