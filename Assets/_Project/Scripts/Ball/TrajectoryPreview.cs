using UnityEngine;

// 터치 여부와 무관하게 매 프레임 1차/2차 충돌 지점을 계산해
// 점선 궤적과 2차 충돌 지점의 레드닷/원형 궤적선을 표시하는 궤적 프리뷰.
// GameplayMechanics.md 섹션 1 스펙을 그대로 따른다(2단계 점선 + 2차 지점 레드닷/링, 3차 충돌 이후 미표시).
public class TrajectoryPreview : MonoBehaviour
{
    private const float MAX_RAY_DISTANCE   = 50f;
    private const int   CIRCLE_SEGMENTS    = 24;
    private const float DASH_WORLD_SIZE    = 0.15f;

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
        _trajectoryLine = CreateLineRenderer("TrajectoryLine", _lineWidth, _lineColor, CreateDashTexture());
        _hitDot         = CreateLineRenderer("HitDot",  _dotRadius * 1.6f, _hitColor,  CreateSolidTexture());
        _hitRing        = CreateLineRenderer("HitRing", _lineWidth, _ringColor, CreateSolidTexture());

        _hitDot.loop  = true;
        _hitRing.loop = true;

        // 텍스처 타일링 계산은 LineRenderer의 실제 렌더링 결과와 어긋날 수 있어(원격 환경
        // 미검증 이슈 발생) colorGradient 기반으로 교체한다. 원 둘레를 4등분해 각 등분의
        // 중앙을 알파 1(피크), 등분 경계를 알파 0(골)로 삼으면 alphaKeys 8개(Gradient 최대치)로
        // 정확히 4개의 밝은 호가 보장된다. _ringColor는 Inspector에서 런타임에 바뀔 수 있지만
        // 기존 startColor/endColor 방식과 동일하게 Awake 시점에 한 번만 고정한다.
        _hitRing.colorGradient = BuildRingDashGradient(_ringColor);

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
            DrawCircle(_hitRing, hit2.point, _ringRadius, ringRotationOffsetDeg);
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
    private static void DrawCircle(LineRenderer lr, Vector2 center, float radius, float rotationOffsetDeg = 0f)
    {
        lr.positionCount = CIRCLE_SEGMENTS;
        float offsetRad = rotationOffsetDeg * Mathf.Deg2Rad;
        for (int i = 0; i < CIRCLE_SEGMENTS; i++)
        {
            float angle = (i / (float)CIRCLE_SEGMENTS) * Mathf.PI * 2f - offsetRad;
            Vector3 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            lr.SetPosition(i, point);
        }
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

    private LineRenderer CreateLineRenderer(string childName, float width, Color color, Texture2D texture)
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
        material.mainTextureScale = new Vector2(1f / DASH_WORLD_SIZE, 1f);
        lr.material = material;

        return lr;
    }

    // 고리(_hitRing)에 정확히 4개의 밝은 호가 보이도록 하는 Gradient를 생성한다.
    // 원 둘레를 4등분해 각 등분의 정중앙을 피크(alpha 1), 등분 경계를 골(alpha 0)로 삼으면
    // Gradient가 허용하는 alphaKeys 최대 개수(8개)로 정확히 맞아떨어져 텍스처 타일링
    // 계산 없이도 항상 정확한 개수를 보장한다. colorKeys는 RGB만 의미가 있으므로
    // 시작/끝 2개를 모두 ringColor로 고정한다.
    private static Gradient BuildRingDashGradient(Color ringColor)
    {
        var gradient = new Gradient();
        var colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(ringColor, 0f),
            new GradientColorKey(ringColor, 1f),
        };
        var alphaKeys = new GradientAlphaKey[8];
        for (int i = 0; i < 4; i++)
        {
            float peakT   = i * 0.25f;
            float valleyT = peakT + 0.125f;
            alphaKeys[i * 2]     = new GradientAlphaKey(1f, peakT);
            alphaKeys[i * 2 + 1] = new GradientAlphaKey(0f, valleyT);
        }
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
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

    // 레드닷(_hitDot)/고리(_hitRing) 공용 단색 텍스처. 고리의 점선 형태(4개 호)는
    // 텍스처가 아니라 colorGradient(BuildRingDashGradient)로 표현한다.
    private static Texture2D CreateSolidTexture()
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return tex;
    }
}
