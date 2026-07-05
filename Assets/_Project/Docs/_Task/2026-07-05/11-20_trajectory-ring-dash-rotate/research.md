# Research — 궤적 프리뷰 고리(Ring) 점선화 및 회전 효과

이 문서는 `TrajectoryPreview.cs`의 2차 충돌 지점에 표시되는 레드닷을 감싸는 원형 궤적선(고리, `_hitRing`)이 현재 하나로 이어진 실선 24각형으로 그려지고 있으며 회전 애니메이션이 전혀 없다는 점을 확인하고, 사용자가 지적한 "끊어진 선(점선) + 회전 효과"를 원본 게임 레퍼런스 이미지에서 실측 확인한 내용을 정리합니다. 아울러 사용자가 별도로 요청한 "궤적선 색상 Inspector 조절 가능화"는 이미 코드/씬 양쪽에서 구현이 끝나 있다는 사실도 함께 확인했습니다. 구체적 구현 방법은 이 문서에서 확정하지 않고 plan.md에서 다룹니다.

## 현재 상태

### 1. `_hitRing` 렌더링 방식 (`Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`)

- `Awake()`에서 `_hitRing = CreateLineRenderer("HitRing", _lineWidth, _ringColor, CreateSolidTexture())`로 생성된다(29번 줄). 텍스처가 `CreateSolidTexture()` — 2x2 전체 불투명 흰색 텍스처이므로 링 전체가 하나의 이어진 실선으로 렌더링된다.
- `_hitRing.loop = true`(31번 줄)로 설정되어 마지막 정점과 첫 정점이 자동으로 이어져 닫힌 원이 된다.
- 매 프레임 `UpdateTrajectory()`에서 2차 충돌이 있을 때만 `DrawCircle(_hitRing, hit2.point, _ringRadius)`(73번 줄)가 호출되어 정점 위치가 갱신된다.
- `DrawCircle()`(113~122번 줄)은 `CIRCLE_SEGMENTS = 24`(9번 줄)개의 정점을 `center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius`로 계산해 `lr.SetPosition(i, point)`로 **월드 좌표를 매 프레임 직접 덮어쓰는** 방식이다. 각 정점의 각도(`angle`)는 `(i / CIRCLE_SEGMENTS) * 2π`로 고정되어 있으며, 시간에 따라 변하는 요소가 전혀 없다 — 즉 회전 애니메이션이 존재하지 않는다.
- `_lineWidth`(궤적선/고리 공용 두께 필드), `_ringColor`, `_ringRadius` 모두 `[SerializeField]`로 이미 Inspector 노출되어 있다(12~17번 줄).

### 2. 대비되는 기존 패턴 — `_trajectoryLine`의 점선 구현

- `_trajectoryLine = CreateLineRenderer("TrajectoryLine", _lineWidth, _lineColor, CreateDashTexture())`(27번 줄) — 궤적선은 이미 점선 텍스처를 사용 중이다.
- `CreateDashTexture()`(161~173번 줄): 가로 4픽셀 텍스처를 만들어 앞쪽 2픽셀은 불투명 흰색, 뒤쪽 2픽셀은 알파 0(투명)으로 채운 뒤 `wrapMode = TextureWrapMode.Repeat`로 설정 — 앞쪽 절반 보임/뒤쪽 절반 안 보임이 반복되는 패턴.
- `CreateLineRenderer()`(136~158번 줄)에서 `lr.textureMode = LineTextureMode.Tile`(150번 줄)로 설정하고, `material.mainTextureScale = new Vector2(1f / DASH_WORLD_SIZE, 1f)`(154번 줄, `DASH_WORLD_SIZE = 0.15f`)로 월드 단위 길이에 비례해 텍스처가 반복되도록 스케일을 맞춘다. 즉 LineRenderer의 실제 월드 길이가 길어질수록 텍스처 반복 횟수가 늘어나 점선 간격이 일정하게 유지된다.
- 이 패턴은 `_hitRing`에도 그대로 재사용 가능한 기존 구조다. 다만 `_hitRing`은 원의 둘레 길이(`2π × _ringRadius`)가 궤적선처럼 가변적이지 않고 `_ringRadius`에 의해 고정되므로, `mainTextureScale`을 둘레 길이에 맞춰 계산하는 별도 로직이 필요하다(둘레가 짧으면 점선 개수가 너무 적거나 많아질 수 있음).
- 참고로 `_hitDot`(레드닷)도 `CreateSolidTexture()`를 사용하며, 이번 사용자 피드백은 레드닷 자체가 아니라 그 **주변 고리**에 대한 것이므로 `_hitDot`은 이번 조사 범위에서 변경 대상이 아니다.

### 3. `Awake()`/`Update()`/`UpdateTrajectory()` 흐름 및 `CreateLineRenderer()` 재사용 구조

- `Awake()`: `_trajectoryLine`/`_hitDot`/`_hitRing` 3개의 `LineRenderer`를 자식 GameObject로 생성(`CreateLineRenderer()` 공용 헬퍼 사용) 후 `SetVisible(true)`로 초기 표시.
- `Update()`: 터치 여부와 무관하게 매 프레임 `BallLauncher.Instance.LaunchDirection`을 읽어 `UpdateTrajectory()` 호출.
- `UpdateTrajectory()`: 1차 레이캐스트 → 반사 방향 계산 → 2차 레이캐스트 → 2차 충돌이 있으면 `_hitDot`/`_hitRing`을 `DrawCircle()`로 갱신하고 `SetHitMarkersVisible(true)`, 없으면 `SetHitMarkersVisible(false)`.
- `CreateLineRenderer()`는 두께/색상/텍스처만 다르게 받아 3개 LineRenderer 생성에 공용으로 쓰이는 헬퍼이며, `useWorldSpace = true`, `textureMode = Tile`, `sortingOrder = 100` 등 공통 설정을 담당한다. 회전 효과를 이 헬퍼 레벨에서 처리할 수는 없고(정적 설정만 담당), 실제 회전은 `DrawCircle()` 호출 시점(매 프레임)에서 처리되어야 한다.

### 4. Inspector 노출 항목 재확인 — 사용자가 요청한 "색상 조절 가능화"는 이미 완료된 상태

사용자가 "궤적선 색상을 Inspector에서 조절 가능하게 해달라"고 요청했으나, 실제 확인 결과:

- 코드(`TrajectoryPreview.cs` 12~17번 줄): `_lineWidth`, `_lineColor`, `_hitColor`, `_ringColor`, `_dotRadius`, `_ringRadius` 전부 `[SerializeField]`로 이미 노출되어 있다.
- `UIRules.md` 섹션 11 "Inspector 조절 값" 표(207~217번 줄)에도 이 6개 필드가 이미 문서화되어 있다.
- 씬 파일(`Assets/Scenes/SampleScene.unity`, `TrajectoryPreview` 컴포넌트, fileID 71915423) 직렬화 오버라이드 값을 직접 읽어 코드 기본값과 대조한 결과, 완전히 일치했다:

| 필드 | 코드 기본값 | 씬 오버라이드 값 | 일치 여부 |
|------|------------|------------------|-----------|
| `_lineWidth` | 0.05 | 0.05 | 일치 |
| `_lineColor` | (225,225,220,255) → (0.882353, 0.882353, 0.862745, 1) | (0.88235295, 0.88235295, 0.8627451, 1) | 일치 |
| `_hitColor` | (206,90,82,255) → (0.807843, 0.352941, 0.321569, 1) | (0.80784315, 0.3529412, 0.32156864, 1) | 일치 |
| `_ringColor` | (225,225,220,255) | (0.88235295, 0.88235295, 0.8627451, 1) | 일치 |
| `_dotRadius` | 0.05 | 0.05 | 일치 |
| `_ringRadius` | 0.3 | 0.3 | 일치 |

과거(2026-07-03) task에서 씬의 직렬화 오버라이드 값이 코드 기본값 변경을 가려버려 실제 플레이 화면에는 옛 값이 반영되던 전례가 있었기 때문에 이번에도 동일 문제가 있는지 별도로 확인했으나, 이번 건은 문제가 없다. **즉 "색상 Inspector 조절 가능화"는 추가 구현이 전혀 필요 없으며, 사용자는 Unity Editor의 `TrajectoryPreview` 컴포넌트 Inspector에서 이미 각 색상/두께/반경 값을 직접 바꿀 수 있는 상태다.**

### 5. 관련 문서 서술 현황

- `GameplayMechanics.md` 섹션 1, 18번 줄: "두 번째 충돌 지점에는 **빨간 점(레드닷)**과 그 점을 감싸는 **둥근 원형 궤적선(고리 형태)**이 표시된다." — 점선/회전에 대한 언급이 전혀 없다.
- `UIRules.md` 섹션 11, 198번 줄: "2차 충돌 지점에는 빨간 점(레드닷)과 그 점을 감싸는 원형 궤적선(고리)이 표시된다." 및 205번 줄 "레드닷/원형 궤적선도 `LineRenderer`로 그린 원형 점열(정다각형 근사)로 구현한다." — 마찬가지로 점선/회전에 대한 서술이 없다. "원형 점열"이라는 표현은 다각형 근사(24각형)를 의미할 뿐 점선을 뜻하는 것은 아니다(현재 구현이 실선인 것과 일치).
- 두 문서 모두 이번 사용자 피드백(점선 + 회전)을 반영해 갱신이 필요한 상태이며, 갱신 여부/시점은 plan.md 승인 이후 진행한다.

### 6. 원본 게임 레퍼런스 이미지 실측 확인 (`Assets/_Project/Docs/targetUI/`)

아래 3개 이미지에서 고리형 마커를 직접 확인했다.

- **`KakaoTalk_20260701_190324151.jpg`**: 화면 상단 "9 9" 스켈레톤 몬스터 오른쪽(대략 이미지 좌표 600,435 부근, 표시 크기 923x2000 기준)에 작은 원형 마커가 보인다. 또한 화면 하단 발사 지점 좌측(85,1205 부근)과 우측 몬스터 옆(785,1230 부근)에도 십자선이 겹쳐진 원형(⊙ 형태) 마커가 각각 보인다. 이들은 완전히 매끈한 실선 원이 아니라, 얇은 링 안에 방사형 틱(tick)/게이지 다이얼 같은 질감이 있어 하나의 이어진 실선처럼 보이지 않는다.
- **`KakaoTalk_20260701_190324151_01.jpg`**: 캐릭터에서 뻗어나가는 점선 궤적이 몬스터(지렁이형 몹) 몸통까지 이어지고, 그 충돌 지점 근처(대략 335,1250 부근)에 흰 점과 함께 옅은 링 형태가 겹쳐 보인다. 화면 우측 몬스터 그룹 옆(790,1195 부근)에도 레드닷 유사 마커를 감싸는 작은 원형 십자선(⊙) 표시가 있으며, 완전한 단일 실선 원으로 보이지 않고 끊긴 구간이 있는 것으로 보인다.
- **`KakaoTalk_20260701_190324151_02.jpg`**: 가장 뚜렷한 사례로, 상단 몬스터(HP바 대략 30% 남은 개체, 이미지 좌표 497,623 부근)의 HP바 우측 끝에서 점선 궤적이 끝나는 지점에 원형 마커가 있는데, 이 마커는 완전히 닫힌 원이 아니라 **일부 구간이 끊긴 호(arc) 형태**로 보인다. 정지 이미지 특성상 회전 여부(애니메이션)를 직접 확인할 수는 없지만, 끊어진 호 형태 자체는 명확히 관찰되며, 이런 형태의 마커는 모바일 게임에서 통상 회전 애니메이션과 함께 쓰이는 "타겟팅 링" UI 패턴과 일치한다.
- 종합하면, 사용자가 지적한 "레드닷을 감싸는 고리가 하나의 이어진 실선이 아니라 끊어진 선(점선/파선) 형태"라는 점은 이미지 상에서 시각적으로 뒷받침된다. 다만 회전 자체는 정지 이미지로는 직접 검증이 불가능하며, 사용자의 실제 플레이 경험 진술(회전한다)에 근거해 구현 대상으로 판단해야 한다.

## 관련 파일 및 의존성

| 파일 | 역할 |
|------|------|
| `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` | 이번 수정 대상. `_hitRing` 생성/갱신 로직(`Awake`, `DrawCircle`, `CreateSolidTexture`)이 위치 |
| `Assets/_Project/Docs/GameplayMechanics.md` | 섹션 1에 궤적 프리뷰 스펙 서술 — 점선/회전 미반영 상태, plan.md 승인 후 갱신 필요 |
| `Assets/_Project/Docs/UIRules.md` | 섹션 11에 궤적 프리뷰 시각 규칙 및 Inspector 조절 값 표 — 마찬가지로 갱신 필요 |
| `Assets/Scenes/SampleScene.unity` | `TrajectoryPreview` 컴포넌트 직렬화 오버라이드 보유 — 코드 기본값과 이미 일치 확인 완료(추가 조치 불필요) |
| `Assets/_Project/Docs/targetUI/KakaoTalk_20260701_190324151.jpg`, `_01.jpg`, `_02.jpg` | 레드닷+고리 마커 형태 실측 확인용 레퍼런스 이미지 |
| `Assets/_Project/Docs/_Task/2026-07-03/15-41_ball-trajectory-aim-fix/` | 직전 궤적 프리뷰 수정 task(레드닷/링/점선 색상·크기 실측 조정 이력) — 참고용 |

의존성 관점에서는 `TrajectoryPreview.cs` 단독 수정으로 해결 가능한 범위이며, `BallLauncher`/`Ball` 등 다른 시스템과의 연동은 없다.

## 문제점 / 구현 대상 파악

1. **점선화**: `_hitRing`이 `CreateSolidTexture()`로 생성되어 있어 완전한 실선 원으로 렌더링된다. 원본 레퍼런스는 끊어진(점선/파선) 형태다. 궤적선(`_trajectoryLine`)에 이미 적용된 `CreateDashTexture()` + `LineRenderer.textureMode = Tile` 조합을 `_hitRing`에도 적용하는 것이 기존 코드 패턴과 일관된 방향으로 보인다. 다만 원의 둘레 길이(`2π × _ringRadius`)가 궤적선처럼 가변 길이가 아니라 `_ringRadius`에 의해 고정되므로, 궤적선처럼 `DASH_WORLD_SIZE` 상수 하나로 나누는 방식이 아니라 **둘레 길이에 맞춘 별도의 `mainTextureScale` 계산**(또는 별도 상수)이 필요할 가능성이 있다. 점선 개수/간격을 몇 개로 할지는 `_ringRadius`(현재 0.3) 기준으로 시각적으로 조정해야 하는 부분이라 plan.md에서 구체적 수치를 확정해야 한다.
2. **회전 효과**: 현재 `DrawCircle()`은 각 정점의 각도를 `(i / CIRCLE_SEGMENTS) * 2π`로 고정 계산해 `center + offset`을 **월드 좌표로 직접** `SetPosition`한다. 이 구조상 `_hitRing.transform.Rotate()`를 매 프레임 호출해도 실제로는 아무 효과가 없을 가능성이 높다 — `DrawCircle()`이 매 프레임 정점의 월드 좌표를 처음부터 다시 계산해 덮어쓰기 때문에, `transform`에 준 회전이 다음 프레임의 `SetPosition` 호출로 그대로 무시(또는 이중 적용되어 예상과 다른 결과)될 수 있다. 따라서 회전을 구현하려면 `DrawCircle()`이 사용하는 `angle` 계산 자체에 **시간 기반 회전 오프셋**(예: `Time.time * 회전속도`)을 더하는 방식이 구조적으로 더 적합해 보인다. 다만 `DrawCircle()`은 현재 `_hitDot`(레드닷)에도 공용으로 쓰이는 static 메서드이므로, 회전 오프셋을 `_hitRing`에만 적용하려면 메서드 시그니처 확장(선택적 회전 각도 파라미터 추가) 또는 `_hitRing` 전용 별도 메서드 분리가 필요하다 — 이 구체적인 방식은 plan.md에서 확정한다.
3. **회전 속도 노출 여부**: 회전 애니메이션을 추가할 경우 회전 속도(각속도)를 하드코딩할지, 기존 다른 시각 파라미터들처럼 `[SerializeField]`로 Inspector에 노출할지는 아직 정해지지 않았다. `UIRules.md` 섹션 5의 "모든 수치는 `[SerializeField]`로 Inspector에서 조절" 공통 규칙과의 일관성을 고려하면 노출하는 쪽이 기존 프로젝트 컨벤션에 부합하지만, 이는 사용자 확인이 필요한 사항으로 판단해 결론에 열린 질문으로 남긴다.
4. **문서 갱신 필요**: `GameplayMechanics.md` 섹션 1과 `UIRules.md` 섹션 11 모두 현재 "원형 궤적선(고리)"라고만 서술되어 있어, 점선/회전 사양이 확정되면 두 문서 모두 해당 서술을 갱신해야 한다(plan.md 이후 별도 처리).
5. **색상 Inspector 조절**: 이미 완료된 상태이므로 이번 task의 구현 대상이 아니다(위 "현재 상태" 4번 항목 참고). 사용자가 혼동하고 있었을 가능성이 있어 이 사실을 명확히 알리는 것이 이번 research.md의 목적 중 하나다.

## 결론

- `_hitRing`을 실선(`CreateSolidTexture`)에서 점선(`CreateDashTexture` 유사 방식)으로 바꾸는 작업과, `DrawCircle()`의 각도 계산에 시간 기반 오프셋을 추가해 회전 효과를 주는 작업 두 가지가 이번 task의 핵심 구현 대상이다. 두 작업 모두 `TrajectoryPreview.cs` 내부 수정만으로 해결 가능하며 외부 시스템 의존성은 없다.
- "궤적선 색상을 Inspector에서 조절 가능하게 해달라"는 요청은 이미 코드(`[SerializeField]` 6개 필드)와 씬(직렬화 오버라이드 값이 코드 기본값과 완전히 일치) 양쪽에서 충족되어 있어 **추가 구현이 필요 없다**. Unity Editor에서 `TrajectoryPreview` 컴포넌트를 선택하면 `_lineColor`/`_hitColor`/`_ringColor`/`_lineWidth`/`_dotRadius`/`_ringRadius`를 즉시 조절할 수 있다.
- 원본 게임 레퍼런스 이미지(`KakaoTalk_20260701_190324151.jpg`, `_01.jpg`, `_02.jpg`) 실측 결과, 레드닷을 감싸는 고리가 완전히 이어진 실선이 아니라 끊어진 호/틱 형태로 보인다는 사용자의 지적은 시각적으로 뒷받침된다. 회전 여부는 정지 이미지로 직접 검증할 수 없으나 사용자의 실제 플레이 경험을 근거로 구현 대상에 포함한다.

**열린 질문 (plan.md 진행 전 사용자 확인 필요)**

1. 점선 개수/간격(원 둘레에 몇 개의 점선 세그먼트를 넣을지)을 수치로 확정할 방법 — 레퍼런스 이미지 실측 기반 근사치로 정할지, 시각적으로 보면서 임의값을 정할지.
2. 회전 속도를 하드코딩할지, `[SerializeField]` 필드로 Inspector에 노출할지.
3. 회전 방향(시계/반시계)에 대한 원본 레퍼런스상 특별한 지침이 없는데, 임의로 정해도 무방한지.
