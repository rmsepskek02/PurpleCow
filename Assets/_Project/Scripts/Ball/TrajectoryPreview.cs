using UnityEngine;

// 조준 중(OnAimBegin~OnRelease) 1차/2차 충돌 지점을 계산해
// 점선 궤적과 2차 충돌 지점의 레드닷/원형 궤적선을 표시하는 궤적 프리뷰.
// GameplayMechanics.md 섹션 1 스펙을 그대로 따른다(2단계 점선 + 2차 지점 레드닷/링, 3차 충돌 이후 미표시).
public class TrajectoryPreview : MonoBehaviour
{
    private const float MAX_RAY_DISTANCE = 50f;
    private const int   CIRCLE_SEGMENTS  = 24;
    private const float DASH_WORLD_SIZE  = 0.3f;

    [SerializeField] private float _lineWidth  = 0.05f;
    [SerializeField] private Color _lineColor  = Color.white;
    [SerializeField] private Color _hitColor   = Color.red;
    [SerializeField] private float _dotRadius  = 0.08f;
    [SerializeField] private float _ringRadius = 0.3f;

    private static readonly string[] _blockingTags = { "Wall", "Ground", "Monster" };

    private LineRenderer _trajectoryLine;
    private LineRenderer _hitDot;
    private LineRenderer _hitRing;

    private void Awake()
    {
        _trajectoryLine = CreateLineRenderer("TrajectoryLine", _lineWidth, _lineColor, CreateDashTexture());
        _hitDot         = CreateLineRenderer("HitDot",  _dotRadius * 1.6f, _hitColor, CreateSolidTexture());
        _hitRing        = CreateLineRenderer("HitRing", _lineWidth,        _hitColor, CreateSolidTexture());
        _hitDot.loop  = true;
        _hitRing.loop = true;

        SetVisible(false);
    }

    private void OnEnable()
    {
        InputHandler.OnAimBegin += HandleAimBegin;
        InputHandler.OnDrag     += HandleDrag;
        InputHandler.OnRelease  += HandleRelease;
    }

    private void OnDisable()
    {
        InputHandler.OnAimBegin -= HandleAimBegin;
        InputHandler.OnDrag     -= HandleDrag;
        InputHandler.OnRelease  -= HandleRelease;
    }

    private void HandleAimBegin()
    {
        SetVisible(true);
        UpdateTrajectory(BallLauncher.Instance.LaunchDirection);
    }

    private void HandleDrag(Vector2 direction)
    {
        UpdateTrajectory(direction);
    }

    private void HandleRelease()
    {
        SetVisible(false);
    }

    private void UpdateTrajectory(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Vector2 origin = BallLauncher.Instance.LaunchOrigin;

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
            DrawCircle(_hitRing, hit2.point, _ringRadius);
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

    // 레드닷/원형 궤적선용 단색 텍스처.
    private static Texture2D CreateSolidTexture()
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        return tex;
    }
}
