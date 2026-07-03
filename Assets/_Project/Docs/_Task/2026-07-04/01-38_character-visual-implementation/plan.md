# Plan — Character Visual Implementation

research.md에서 확인한 대로 씬에 존재하지 않는 캐릭터 시각 표현(Body/Head/Weapon)을 실제로 렌더링하고, 조준 방향(`BallLauncher.LaunchDirection`)에 따라 반응하도록 구현하는 계획을 다룹니다. research.md에서 열린 이슈로 남겨두었던 좌우 반전-회전 부호 충돌 문제는 오케스트레이터가 사용자와 논의를 거쳐 `SpriteRenderer.flipX` 기반 처리 방식으로 확정했으며, 이 문서에서는 확정된 사항으로 반영합니다. 무기 스프라이트 Pivot 재설정, 신규 회전 제어 스크립트 작성, 씬 자동화 Step 추가, 코드 리뷰까지의 단계별 작업 계획을 담습니다.

---

## 구현 목표

- research.md에서 확인된 대로, 씬에 존재하지 않는 캐릭터 시각 표현(Body/Head/Weapon)을 실제로 렌더링하고, 조준 방향(`BallLauncher.LaunchDirection`)에 따라 반응하도록 만든다.
- 범위 확정 사항:
  - Body는 좌우 반전만 적용하며 회전은 하지 않는다.
  - Head는 좌우 반전 + 약하게 감쇠된 회전(살짝 갸웃하는 정도)을 적용한다.
  - Weapon은 좌우 반전 + 조준 방향을 거의 그대로 따라가는 강한 회전을 적용한다.
- **좌우 반전-회전 부호 충돌 문제 (확정 사항)**: `localScale.x = -1` 방식의 반전은 사용하지 않는다. 대신 각 파츠(Body/Head/Weapon)의 `SpriteRenderer.flipX`만으로 좌우 반전을 처리하고, 회전은 항상 `BallLauncher.LaunchDirection`(월드 좌표계 벡터)을 `Mathf.Atan2`로 변환한 각도를 그대로 사용한다. `flipX`는 렌더링 단계에서만 좌우를 뒤집을 뿐 Transform의 회전 계산에는 관여하지 않으므로, 반전 여부와 무관하게 항상 동일한 각도 공식이 성립해 별도의 부호 재매핑이 필요 없다.
- `CharacterManager.cs`(HP/XP 로직)와 볼 발사/귀환 로직(`BallLauncher`/`Ball`)은 이번 작업 범위에서 제외하고 건드리지 않는다. 이번 작업은 순수 시각 레이어 추가에 한정한다.

---

## 단계별 작업 계획

### 1단계 — 무기 스프라이트 Pivot 재설정 [design 에이전트]

- `Character_main_weapon.png`의 Import Settings Pivot을 현재 중앙(0.5, 0.5)에서 그립 위치(대략 0.36, 0.43 — research.md에서 픽셀 분석한 값)로 Custom 재설정한다.

### 2단계 — CharacterAimController.cs 작성 [dev 에이전트]

- 신규 폴더 `Assets/_Project/Scripts/Character/`에 `CharacterAimController.cs`를 작성한다.
- `_bodyRenderer`, `_headRenderer`, `_weaponRenderer`(SpriteRenderer), `_headDampFactor`(float, 인스펙터 노출 기본값 제안 0.25) 등을 SerializeField로 노출한다.
- 매 프레임(`Update()`) `BallLauncher.Instance.LaunchDirection`을 읽어 `aimAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f`로 각도를 계산한다. (기본 정면을 위쪽으로 가정한 오프셋이며, 실제 스프라이트 기본 방향에 따라 dev 구현 중 미세조정 가능)
- `facingRight` 판정: `direction.x`가 임계값(deadzone, 예: 0.05) 이상/이하일 때만 갱신하고, 그 사이 구간에서는 이전 상태를 유지해 좌우 반전이 떨리지 않도록 한다.
- Body/Head/Weapon 각 SpriteRenderer의 `flipX = !facingRight`로 반전 처리한다. (localScale 반전 금지 — 위 확정 사항)
- Weapon: `transform.localRotation = Quaternion.Euler(0, 0, aimAngle)` (감쇠 없이 거의 그대로 따라감)
- Head: `transform.localRotation = Quaternion.Euler(0, 0, aimAngle * _headDampFactor)` (감쇠된 회전)
- Body: 회전 없음, flipX만 적용

### 3단계 — SceneSetupEditor.cs에 캐릭터 배치 Step 추가 [dev 에이전트]

- 기존 Step6/Step7 자동화 패턴을 재사용해 신규 Step을 추가한다.
- `Character` GameObject를 `LaunchPoint`와 동일 위치(또는 자식)에 생성한다.
- 그 자식으로 Body/Head/Weapon 3개의 SpriteRenderer 오브젝트를 만들어 각각 대응하는 스프라이트 에셋을 연결한다.
- `CharacterAimController` 컴포넌트를 추가하고 SerializeField 참조를 연결한다.
- Sorting Order(그리기 순서)는 이번 단계에서 확정하지 않고, 실제 배치 후 겹침 형태를 보고 조정할 대상으로 남긴다.

### 4단계 — 코드 리뷰 및 로직 검증 [qa 에이전트]

- 작성된 `CharacterAimController.cs`와 `SceneSetupEditor.cs` 변경 사항에 대한 코드 리뷰 및 로직 검증을 진행한다.
- 실제 플레이 테스트는 사용자가 로컬 Unity 환경에서 진행한다.

---

## 예상 변경/생성 파일 목록

- 신규: `Assets/_Project/Scripts/Character/CharacterAimController.cs`
- 수정: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` (캐릭터 배치 Step 추가)
- 수정: `Assets/_Project/Sprites/Character/Character_main_weapon.png.meta` (spritePivot 값 변경)
- (사용자가 로컬에서 처리) `SampleScene.unity`는 코드 수정만으로 자동 반영되지 않으므로, 사용자가 로컬 Unity에서 `PurpleCow/Setup/Scene Setup` 메뉴를 재실행해야 씬에 `Character` 오브젝트가 실제로 생성된다. 이는 이 프로젝트의 기존 반복 패턴(WaveTableData, Wall_Top 사례)과 동일하다.

---

## 주의사항

- 이 작업이 진행되는 원격 환경에는 Unity 에디터가 없어, `SceneSetupEditor.cs` 코드 수정만으로는 이미 커밋된 `SampleScene.unity`에 즉시 반영되지 않는다. 사용자가 로컬 Unity에서 `PurpleCow/Setup/Scene Setup` 메뉴를 재실행해야 완전히 동작한다. (기존 `ProjectHistory.md`에 기록된 WaveTableData/Wall_Top 사례와 동일한 제약)
- `CharacterManager.cs`, `BallLauncher.cs`, `Ball.cs` 등 기존 게임플레이 로직은 이번 작업에서 수정하지 않는다.
- Weapon 회전 각도의 정확한 오프셋(-90도 등)은 실제 스프라이트가 기본적으로 어느 방향을 향하도록 그려졌는지에 따라 dev 구현 중 미세조정이 필요할 수 있다.
- Sorting Order(그리기 순서)는 미리 확정하지 않고 실제 배치 후 조정한다.
