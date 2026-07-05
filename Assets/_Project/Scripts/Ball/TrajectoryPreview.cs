using UnityEngine;

// 터치 여부와 무관하게 매 프레임 1차/2차 충돌 지점을 계산해
// 점선 궤적과 2차 충돌 지점의 레드닷/원형 궤적선을 표시하는 궤적 프리뷰.
// GameplayMechanics.md 섹션 1 스펙을 그대로 따른다(2단계 점선 + 2차 지점 레드닷/링, 3차 충돌 이후 미표시).
public class TrajectoryPreview : MonoBehaviour
{
    private const float MAX_RAY_DISTANCE   = 50f;
    private const int   CIRCLE_SEGMENTS    = 24;
    private const int   RING_ARC_COUNT     = 4;
    private const int   RING_ARC_SEGMENTS  = 6;

    [SerializeField] private float _lineWidth  = 0.05f;
    [SerializeField, Min(0.001f)] private float _dashLength      = 0.12f;
    [SerializeField, Min(0.001f)] private float _dashGap         = 0.04f;
    [SerializeField, Min(0f)]     private float _dashScrollSpeed = 0.5f;
    [SerializeField] private Color _lineColor  = new Color32(225, 225, 220, 255);
    [SerializeField] private Color _hitColor   = new Color32(206, 90, 82, 255);
    [SerializeField] private Color _ringColor  = new Color32(225, 225, 220, 255);
    [SerializeField] private float _dotRadius  = 0.05f;
    [SerializeField] private float _ringRadius = 0.3f;
    [SerializeField] private float _ringRotationSpeed = 90f; // 고리 시계방향 회전 속도(deg/sec)

    private static readonly string[] _blockingTags = { "Wall", "Ground", "Monster" };

    private LineRenderer _trajectoryLine;
    private LineRenderer _hitDot;
    private LineRenderer[] _hitArcs;
    private Material _trajectoryMaterial;

    private void Awake()
    {
        float dashPeriod = _dashLength + _dashGap;
        _trajectoryLine = CreateLineRenderer(
            "TrajectoryLine",
            _lineWidth,
            _lineColor,
            CreateDashTexture(_dashLength / dashPeriod),
            1f / dashPeriod);
        _trajectoryMaterial = _trajectoryLine.sharedMaterial;

        Texture2D solidTexture = CreateSolidTexture();
        _hitDot = CreateLineRenderer("HitDot", _dotRadius * 1.6f, _hitColor, solidTexture, 1f);

        _hitArcs = new LineRenderer[RING_ARC_COUNT];
        for (int i = 0; i < _hitArcs.Length; i++)
        {
            _hitArcs[i] = CreateLineRenderer(
                $"HitArc_{i + 1}",
                _lineWidth,
                _ringColor,
                solidTexture,
                1f);
        }

        _hitDot.loop  = true;

        SetVisible(true);
    }

    // 터치 여부와 무관하게 매 프레임 궤적을 재계산한다.
    // BallLauncher.LaunchDirection은 터치 중에는 드래그 방향, 터치하지 않을 때는
    // 마지막 조준 방향(기본값 Vector2.up)을 담고 있어 별도 상태 분기가 필요 없다.
    private void Update()
    {
        UpdateDashOffset();
        UpdateTrajectory(BallLauncher.Instance.LaunchDirection);
    }

    private void UpdateDashOffset()
    {
        float dashPeriod = _dashLength + _dashGap;
        float offset = -Time.time * _dashScrollSpeed / dashPeriod;
        _trajectoryMaterial.mainTextureOffset = new Vector2(offset, 0f);
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
            float ringRotationOffsetDeg = Time.time * _ringRotationSpeed;
            DrawRingArcs(hit2.point, _ringRadius, ringRotationOffsetDeg);
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

    private static void DrawCircle(LineRenderer lr, Vector2 center, float radius)
    {
        lr.positionCount = CIRCLE_SEGMENTS;
        for (int i = 0; i < CIRCLE_SEGMENTS; i++)
        {
            float angle = (i / (float)CIRCLE_SEGMENTS) * Mathf.PI * 2f;
            Vector3 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            lr.SetPosition(i, point);
        }
    }

    // 4개의 호를 각각 별도 LineRenderer로 그려 텍스처 타일링 결과와 무관하게 개수를 보장한다.
    // 각 호는 45도, 호 사이의 빈 구간도 45도로 구성되어 1:1 비율을 유지한다.
    private void DrawRingArcs(Vector2 center, float radius, float rotationOffsetDeg)
    {
        float sectionAngle = 360f / RING_ARC_COUNT;
        float arcAngle = sectionAngle * 0.5f;

        for (int arcIndex = 0; arcIndex < _hitArcs.Length; arcIndex++)
        {
            LineRenderer arc = _hitArcs[arcIndex];
            arc.positionCount = RING_ARC_SEGMENTS + 1;
            float startAngle = arcIndex * sectionAngle - rotationOffsetDeg;

            for (int pointIndex = 0; pointIndex <= RING_ARC_SEGMENTS; pointIndex++)
            {
                float t = pointIndex / (float)RING_ARC_SEGMENTS;
                float angle = (startAngle + arcAngle * t) * Mathf.Deg2Rad;
                Vector3 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                arc.SetPosition(pointIndex, point);
            }
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
        foreach (LineRenderer arc in _hitArcs)
            arc.enabled = visible;
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

    // 점선 표현용 텍스처. 실선과 빈 공간의 비율은 Inspector의 길이/간격 값으로 결정한다.
    private static Texture2D CreateDashTexture(float solidRatio)
    {
        const int textureWidth = 64;
        int solidPixelCount = Mathf.Clamp(Mathf.RoundToInt(textureWidth * solidRatio), 1, textureWidth - 1);

        var tex = new Texture2D(textureWidth, 1, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode   = TextureWrapMode.Repeat;
        var pixels = new Color[textureWidth];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = i < solidPixelCount
                ? Color.white
                : new Color(1f, 1f, 1f, 0f);
        }

        tex.SetPixels(pixels);
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
