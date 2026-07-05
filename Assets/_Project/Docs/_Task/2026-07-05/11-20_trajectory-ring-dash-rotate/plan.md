# Plan — 궤적 프리뷰 고리(Ring) 점선화 및 회전 효과

이 문서는 research.md에서 확인한 `TrajectoryPreview.cs`의 `_hitRing`(2차 충돌 지점 레드닷을 감싸는 고리)을 현재의 실선 원에서 점선(끊어진 호) 형태로 바꾸고, 시계방향으로 회전하는 애니메이션을 추가하기 위한 구현 계획을 다룹니다. 회전 속도는 `[SerializeField]`로 Inspector에 노출하며, 점선 개수는 정확한 수치를 못박지 않고 dev 에이전트가 레퍼런스 이미지에 가깝게 시각적으로 조정하는 방향으로 진행합니다. 관련 문서(`GameplayMechanics.md`, `UIRules.md`)도 함께 갱신합니다. "궤적선 색상 Inspector 조절"은 이미 완료된 상태이므로 이번 계획 범위에 포함하지 않습니다.

## 구현 목표

- `_hitRing`을 실선(`CreateSolidTexture()`)에서 점선(끊어진 호) 텍스처로 변경한다.
- `_hitRing`이 매 프레임 시계방향으로 회전하는 효과를 추가한다.
- 회전 속도를 `[SerializeField]` 필드(예: `_ringRotationSpeed`, 단위 deg/sec)로 Inspector에 노출한다.
- `GameplayMechanics.md` 섹션 1과 `UIRules.md` 섹션 11의 관련 서술을 점선/회전 사양에 맞게 갱신한다.
- `_hitDot`(레드닷)과 궤적선 색상 관련 기존 구현은 변경하지 않는다.

## 단계별 작업 계획

1. **`TrajectoryPreview.cs` — `_hitRing` 텍스처를 점선으로 교체**
   - `Awake()`에서 `_hitRing = CreateLineRenderer("HitRing", _lineWidth, _ringColor, CreateSolidTexture())` 호출부의 텍스처 인자를 점선용으로 교체한다.
   - 기존 `CreateDashTexture()`(4x1, 앞쪽 절반 불투명/뒤쪽 절반 투명)를 그대로 재사용할지, 고리 전용 별도 메서드(예: `CreateRingDashTexture()`)를 새로 만들지는 dev 에이전트 재량으로 정한다. 궤적선과 고리의 점선 비율(보임:안보임)이 달라야 시각적으로 더 자연스럽다면 별도 메서드를 권장한다.
   - 궤적선은 `material.mainTextureScale = new Vector2(1f / DASH_WORLD_SIZE, 1f)`로 월드 길이 기준 고정 스케일을 쓰지만, `_hitRing`은 둘레 길이(`2π × _ringRadius`)가 `_ringRadius`에 의해 고정되어 있으므로, 원 둘레를 목표 세그먼트 개수로 나눈 값 기준으로 `mainTextureScale`(또는 `_hitRing.material.mainTextureScale`)을 별도 계산하는 로직이 필요하다. `CreateLineRenderer()` 공용 헬퍼가 `material.mainTextureScale`을 고정값으로 설정하므로, `_hitRing` 전용으로 이 값을 재설정하는 코드를 `Awake()`에 추가하거나, `CreateLineRenderer()`에 텍스처 스케일을 선택적으로 받는 파라미터를 추가하는 방식 중 dev 에이전트가 더 간결한 쪽을 선택한다.
   - 목표 시각 결과: 레퍼런스 이미지(`Assets/_Project/Docs/targetUI/KakaoTalk_20260701_190324151_02.jpg` 등)처럼 원 둘레에 8~12개 정도의 짧은 호가 고르게 배치되어 끊어진 고리로 보이는 것. 정확한 개수/간격 수치는 이 문서에서 확정하지 않으며, dev 에이전트가 구현 후 시각적으로 자연스러운 값을 임의로 정한다.

2. **`TrajectoryPreview.cs` — 회전 효과 추가**
   - 신규 필드 `[SerializeField] private float _ringRotationSpeed = <적절한 기본값>;`을 추가한다(단위: 도/초, deg/sec). 다른 `[SerializeField]` 필드들(12~17번 줄)과 같은 위치에 선언한다. 기본값은 dev 에이전트가 시각적으로 자연스럽다고 판단하는 값으로 임의 결정한다(예: 60f 전후를 참고 수준으로 고려 가능하나 강제하지 않음).
   - 현재 `DrawCircle(LineRenderer lr, Vector2 center, float radius)`는 `_hitDot`과 `_hitRing`에 공용으로 쓰이는 static 메서드이며, 각 정점의 `angle`을 `(i / CIRCLE_SEGMENTS) * 2π`로 고정 계산해 월드 좌표를 매 프레임 직접 `SetPosition`한다. 이 구조상 `_hitRing.transform.Rotate()`는 효과가 없으므로(다음 프레임에 `DrawCircle()`이 각도를 처음부터 다시 계산해 덮어씀), 회전은 `angle` 계산식 자체에 시간 기반 오프셋을 더하는 방식으로 구현해야 한다.
   - 구현 방식은 다음 중 dev 에이전트가 더 간결한 쪽을 선택한다.
     - (a) `DrawCircle()` 시그니처를 `DrawCircle(LineRenderer lr, Vector2 center, float radius, float rotationOffsetDeg = 0f)`처럼 확장하고, `_hitRing` 호출부(`DrawCircle(_hitRing, hit2.point, _ringRadius, rotationOffsetDeg)`)에서만 0이 아닌 오프셋을 넘긴다.
     - (b) `_hitRing` 전용 별도 메서드(예: `DrawRotatingCircle()`)를 분리해 `DrawCircle()`은 기존 그대로 `_hitDot`에만 쓰이게 유지한다.
   - 회전 각도 누적은 `Time.time * _ringRotationSpeed`(도 단위)로 계산해 `angle`에 더하거나 뺀다. Unity 2D 좌표계에서는 `Mathf.Cos(angle)`, `Mathf.Sin(angle)` 조합 기준 각도가 증가할수록 반시계 방향으로 회전하는 것이 일반적이므로, **시계방향**으로 보이려면 각도가 시간에 따라 감소하는 방향(예: `angle - Mathf.Deg2Rad * Time.time * _ringRotationSpeed`)으로 오프셋을 적용해야 할 가능성이 높다. 정확한 부호는 dev 에이전트가 실제 좌표계 기준으로 확인 후 반영한다(이 원격 환경에서는 Unity 에디터로 시각 확인이 불가능하므로 로직상 올바른 방향으로 작성하되 최종 검증은 사용자 로컬 테스트에서 진행).
   - `_hitDot`(레드닷)에는 회전 오프셋을 적용하지 않는다(파라미터 기본값 0 유지, 또는 메서드 분리 시 `_hitDot`은 기존 `DrawCircle()` 그대로 사용).

3. **`GameplayMechanics.md` 섹션 1 갱신**
   - 18번 줄 "두 번째 충돌 지점에는 **빨간 점(레드닷)**과 그 점을 감싸는 **둥근 원형 궤적선(고리 형태)**이 표시된다." 서술을, 고리가 점선(끊어진 호) 형태이며 시계방향으로 계속 회전한다는 내용을 포함하도록 정정한다.

4. **`UIRules.md` 섹션 11 갱신**
   - 198번 줄 "2차 충돌 지점에는 빨간 점(레드닷)과 그 점을 감싸는 원형 궤적선(고리)이 표시된다." 서술에 점선/회전 특징을 추가한다.
   - 205번 줄 "레드닷/원형 궤적선도 `LineRenderer`로 그린 원형 점열(정다각형 근사)로 구현한다." 서술을, 고리는 점선 텍스처(궤적선과 유사하되 둘레 길이 기준 별도 스케일 계산)로 구현되며, 회전은 `transform.Rotate()`가 아니라 `DrawCircle()`의 각도 계산에 시간 기반 오프셋을 더하는 방식으로 구현한다는 점을 명시하도록 갱신한다.
   - 207~217번 줄 "Inspector 조절 값" 표에 신규 필드 `_ringRotationSpeed` 행을 추가한다(설명: 원형 궤적선 회전 속도, 단위 deg/sec).

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` (수정) — `_hitRing` 텍스처 교체, 회전용 `[SerializeField]` 필드 추가, `DrawCircle()` 확장 또는 분리
- `Assets/_Project/Docs/GameplayMechanics.md` (수정) — 섹션 1 서술 갱신
- `Assets/_Project/Docs/UIRules.md` (수정) — 섹션 11 서술 및 Inspector 조절 값 표 갱신
- `Assets/Scenes/SampleScene.unity`는 이번 계획에서 직접 수정 대상이 아니다. 신규 필드 `_ringRotationSpeed`는 코드에 기본값과 함께 선언되므로, 사용자가 로컬에서 Unity Editor로 씬을 열면 자동으로 기본값이 직렬화되어 채워진다. 이 원격 환경에는 Unity가 없어 씬 파일에 이 필드가 지금 당장 나타나지는 않는다.

## 주의사항

- `_hitDot`(레드닷)은 이번 변경 대상이 아니다 — 계속 `CreateSolidTexture()`(단색 채워진 점)를 유지하고, 회전 오프셋도 적용하지 않는다.
- `DrawCircle()`을 확장하거나 분리할 때 `_hitDot` 호출부(`DrawCircle(_hitDot, hit2.point, _dotRadius)`)의 동작이 기존과 달라지지 않도록 주의한다.
- 이 원격 환경에는 Unity 에디터가 없어 실제 점선 간격/회전 애니메이션을 눈으로 직접 검증할 수 없다. 문법/로직 검토까지만 가능하며, 최종 시각적 확인(점선 개수가 자연스러운지, 회전 방향이 실제로 시계방향으로 보이는지, 회전 속도가 적절한지)은 사용자의 로컬 플레이 테스트에서 진행한다.
- "궤적선 색상을 Inspector에서 조절 가능하게 해달라"는 기존 요청은 이미 코드(`[SerializeField]` 6개 필드)와 씬(직렬화 오버라이드 값 일치) 양쪽에서 완료된 상태이므로, 이번 plan.md 범위에 포함하지 않는다.
- 점선 개수/간격의 정확한 수치와 회전 속도 기본값은 이 문서에서 확정하지 않는다. dev 에이전트가 구현 시 레퍼런스 이미지에 가까운 시각적 결과가 나오도록 임의로 정하되, 이후 사용자가 Inspector에서 직접 조절 가능한 범위(회전 속도)와 코드 상수(점선 개수, 필요 시)로 남겨둔다.
