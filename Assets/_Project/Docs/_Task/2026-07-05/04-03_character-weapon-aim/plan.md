# Plan — 캐릭터 프리팹 구성 + 무기 조준 회전 애니메이션

이 문서는 research.md에서 확인한 현재 상태와, 오케스트레이터가 Python(PIL/numpy)으로 실제 스프라이트 픽셀을 분석해 확정한 수치(pivot, 파츠 배치 좌표)를 바탕으로 Character 프리팹 구성과 무기 조준 회전 기능의 구체적인 구현 절차를 정리한다. 스프라이트 meta 불일치 수정 → pivot/배치 재조정 → 프리팹 생성 → 회전 스크립트 작성 → 씬 배치 순으로 진행한다. 아래 수치는 모두 이미지 분석 기반 추정값이며, Unity 에디터에서 실제로 배치·회전해보며 시각적으로 검증/미세조정하는 절차를 포함한다.

**개정 이력**: 1차 구현(weapon meta rect 수정, `CharacterAimController.cs`, `CharacterSetupEditor.cs`, `SceneSetupEditor.cs` Step11)이 이미 완료·커밋된 이후, 오케스트레이터가 원본 게임 레퍼런스 스크린샷을 다시 크롭·확대 분석하고 사용자와 추가로 논의한 결과를 반영해 무기 회전 관련 설계를 전면 수정했다(research.md의 "추가 조사" 섹션 참고). 1단계(meta rect 수정)와 6단계(씬 배치)는 이번 개정과 무관하게 그대로 유효하며, 2~5단계의 무기 회전 설계가 이번에 바뀌었다.

## 구현 목표

- `Character_main_weapon.png`의 meta rect 불일치(59×116 vs 실제 64×116)를 먼저 바로잡는다.
- Body(머리+몸통 중 고정 부분), Head(조준 각도에 따라 0~10도만 소폭 회전), WeaponPivot+Weapon(조준 각도를 그대로 따라가는 회전 축 구조)로 구성된 Character 프리팹을 생성한다.
- `BallLauncher.Instance.LaunchDirection`을 읽어 `WeaponPivot`을 조준 방향으로 부드럽게 회전시키고, 조준 각도의 부호에 따라 캐릭터 전체를 좌우 반전시키는 `CharacterAimController.cs`를 작성한다.
- Character 프리팹을 씬에 배치하고 `LaunchPoint`와 위치를 정합시킨다.
- 이번 범위는 여기까지이며, 액티브 스킬 이펙트(필드 동결/버서크/분신/마법 폭격)나 `CharacterManager`의 HP/XP 로직 변경은 포함하지 않는다.

## 단계별 작업 계획

### 1단계. 무기 스프라이트 meta rect 불일치 확인 및 수정 (최우선, 변경 없음)

- 현재 `Character_main_weapon.png.meta`의 `spriteSheet.sprites[0].rect`가 `width: 59, height: 116`으로 기록되어 있으나, 실제 PNG 픽셀 분석 결과 64×116이다. PNG가 meta 생성 이후 교체되고 meta가 갱신되지 않은 상태로 보인다.
- Unity 에디터에서 `Character_main_weapon.png`를 우클릭 → Reimport 를 먼저 시도해 rect가 64×116으로 자동 갱신되는지 확인한다.
- 자동 갱신되지 않으면 Sprite Editor를 열어 rect를 이미지 전체(0,0,64,116)로 수동 재설정한다.
- 이 단계가 끝나기 전에는 아래 pivot/배치 수치가 의미를 갖지 않으므로 반드시 가장 먼저 처리한다.
- 참고로 `Character_Main_head.png`(rect 131×92, 실측 132×92로 거의 일치)와 `Character_Main_body.png`(rect 48×48, 실측 48×48 정확히 일치)는 문제가 없어 별도 조치가 필요 없다.
- 이 단계는 이번 설계 개정과 무관하게 여전히 유효하다.

### 2단계. 무기 스프라이트 pivot — 회전축을 무기 자신에서 `WeaponPivot`으로 이전

- **설계 변경**: 기존에는 `Character_main_weapon.png` 자체의 커스텀 pivot(손잡이 지점)을 회전축으로 삼아 Weapon 오브젝트 자신의 `localRotation`을 돌리는 방식이었다. 그런데 원본 게임 레퍼런스를 재분석한 결과, 무기의 손잡이 쪽은 각도가 바뀌어도 캐릭터 어깨 근처의 비슷한 자리에 거의 고정되어 있고 갈고리 끝(먼 쪽)만 크게 호를 그리며 움직인다. 즉 회전축이 무기 스프라이트 내부의 한 지점이 아니라 캐릭터 몸 쪽에 가까운 별도의 고정점이라는 것이 확인되었다.
- 이를 반영해 `WeaponPivot`이라는 빈 부모 오브젝트를 캐릭터 쪽 고정 위치(어깨 근처, 기존에 추정한 Weapon 배치 좌표와 비슷한 자리)에 두고, `Weapon`(SpriteRenderer)은 이 `WeaponPivot`의 자식으로 고정된 로컬 오프셋만큼 떨어진 위치에 둔다(무기가 축에서 바깥으로 뻗어나가는 형태). 회전은 `Weapon` 자신이 아니라 `WeaponPivot`에 적용한다.
- 이 변경에 따라 `Character_main_weapon.png`의 커스텀 스프라이트 pivot(`0.18, 0.29`, 손잡이 끝에서 약 25% 지점)은 더 이상 회전축으로서의 의미가 없어졌다. 다만 그대로 유지해도 무방하고(Weapon이 WeaponPivot 자식으로서 갖는 로컬 오프셋 계산에 참고 지점으로 쓸 수 있음), 기본값(`0.5, 0.5`)으로 되돌려도 무방하다. 어느 쪽을 택하든 최종 결과는 Unity Scene 뷰에서 실제로 회전시켜보며 갈고리 끝이 자연스러운 호를 그리는지 시각 검증으로 확정한다.
- `WeaponPivot`의 정확한 위치와 `Weapon`의 오프셋 거리는 이미지 분석만으로 확정하기 어려우므로, Scene 뷰에서 `WeaponPivot`을 회전시켜보며 시각적으로 자연스러운 위치가 나올 때까지 미세조정하는 절차를 반드시 거친다. 과신하지 않고 반드시 시각 검증을 거친다.

### 3단계. 무기 회전 각도 오프셋 — 18도 보정 삭제

- 1차 구현에서는 무기 스프라이트 내용물의 손잡이 끝→갈고리 끝 축이 기본 상태(0도)에서 이미 수직으로부터 약 18도 기울어져 그려져 있다고 보고, 조준 각도에 이 오프셋을 보정해서 더하거나 빼는 설계였다.
- 이번에 삭제한다. 무기는 정밀한 방향 지시자가 아니라 대략적인 방향을 보여주는 연출 요소이므로, 스프라이트 자체의 그려진 기울기를 상쇄하는 정밀 보정(18도)은 필요 없다고 확정했다. 조준 각도를 그대로 사용한다.
- 즉 `CharacterAimController.cs`에서 목표 각도 계산 후 오프셋을 더하거나 빼는 로직 자체를 제거한다(자세한 것은 5단계 참고).

### 4단계. Character 프리팹 계층 구조 생성

- 계층 구조:
  - `Character` (루트, 빈 GameObject) — 조준 각도의 부호에 따라 `localScale.x`를 `+1`/`-1`로 전환해 좌우 반전을 담당(3단계 참고는 삭제되었으니 아래 좌우 반전 설계 참고)
    - `Body` (SpriteRenderer, `Character_Main_body.png`, sortingOrder 낮게, 고정, 회전 없음)
    - `Head` (SpriteRenderer, `Character_Main_head.png`, Character의 자식, sortingOrder 중간, 조준 각도에 따라 자기 자신의 `localRotation`을 0~10도 범위로만 tilt)
    - `WeaponPivot` (빈 GameObject, Character의 자식, 어깨 근처 고정 위치, 조준 각도에 따라 회전)
      - `Weapon` (SpriteRenderer, `Character_main_weapon.png`, `WeaponPivot`의 자식, 고정 로컬 오프셋, sortingOrder 가장 높게 — 레퍼런스 스크린샷에서 무기가 몸통보다 앞에 그려짐)
- 파츠 배치 좌표(Body 중심 기준 로컬 좌표, Pixels Per Unit 100 기준, 템플릿 매칭 역산 추천값 — "오른쪽 조준 기본 자세" 배치 기준):
  - Head: 약 `(-0.34, +0.58)`
  - `WeaponPivot`: 약 `(+0.29, +0.65)` 부근에서 시작해 Scene 뷰 시각 검증으로 최종 확정(2단계 참고)
- 이 좌표들은 `Character_Main.png`(합본, 141×178 rect) 안에서 각 파츠가 차지하는 위치를 이미지 템플릿 매칭으로 역산한 추정치다. 다만 최초 산출 시 합본 이미지의 자세를 오른쪽 조준 기본 자세(방울 왼쪽·무기 오른쪽)로 잘못 판단해 X좌표 부호가 반대로 기록되었던 적이 있다. 이후 왼쪽 조준 레퍼런스 스크린샷과 다시 비교한 결과 합본은 실제로 방울 오른쪽·무기 왼쪽인 "왼쪽 조준" 자세였음이 확인되어, 위 좌표는 그 재확인을 반영해 부호를 정정한 값이다(자세한 경위는 research.md의 "왼쪽 조준 레퍼런스 추가 확인 및 좌표 부호 오류 정정" 섹션 참고). 특히 Weapon/WeaponPivot 쪽은 다른 파츠보다 매칭 신뢰도가 낮았다. Unity 씬에 `Character_Main.png`를 반투명 참고 오버레이(예: 임시 SpriteRenderer, alpha 0.3~0.5)로 띄워놓고, Head/Body/WeaponPivot을 눈으로 겹쳐보며 최종 위치를 미세조정한다. 오버레이 오브젝트는 검증 후 삭제하거나 비활성화한다.
- 프리팹 저장 위치: `Assets/_Project/Prefabs/Character/Character.prefab` (신규 `Character` 서브폴더 생성).

### 5단계. `CharacterAimController.cs` 작성 (전면 재작성 대상)

- 배치: `Assets/_Project/Scripts/Character/CharacterAimController.cs` (신규 `Character` 폴더 생성, 기존 `Ball`/`Monster`/`UI`/`Skill` 폴더 구조와 동일한 방식).
- 역할: `Update()`에서 `BallLauncher.Instance.LaunchDirection`을 읽어 다음을 수행한다.
  1. `Mathf.Atan2(direction.x, direction.y)`로 목표 각도(위쪽 기준) 계산. **18도 오프셋 보정 로직은 넣지 않는다(3단계에서 삭제 확정).**
  2. 좌우 ±90도로 `Mathf.Clamp`(기존 클램프 로직은 유지).
  3. **좌우 반전**: 목표 각도의 부호(양수=오른쪽 조준, 음수=왼쪽 조준)에 따라 `Character` 루트의 `localScale.x`를 `+1`(양수 각도) 또는 `-1`(음수 각도)로 전환한다. 부모의 `localScale.x`가 `-1`로 뒤집히면 그 자식(`Head`, `WeaponPivot`)의 회전 방향도 시각적으로 반전되므로(같은 회전값이라도 좌우가 뒤집힌 부모 아래에서는 반대 방향으로 보임), `WeaponPivot`/`Head`에 적용하는 회전값의 부호를 이 반전 상태에 맞게 보정해야 한다. 정확한 부호 처리는 이미지로 검증되지 않은 부분이라(아래 주의사항 참고) 구현 후 Unity에서 실제로 좌우로 조준해보며 육안 검증이 반드시 필요하다.
  4. `Mathf.LerpAngle` 또는 `Quaternion.RotateTowards`로 부드럽게 보간(회전 속도는 `[SerializeField] private float _rotationSpeed`로 Inspector 노출)하여 `WeaponPivot` Transform의 Z 회전에 적용(기존에는 `Weapon` 자신을 회전시켰으나, 이번 개정으로 `WeaponPivot`을 회전시키는 것으로 변경).
  5. `Head`는 같은 목표 각도를 `Mathf.Clamp(-10f, 10f)`로 다시 한 번 좁게 클램프한 값을 `Head` 자신의 `localRotation`(제자리 tilt, 별도 pivot 오브젝트 불필요)에 적용한다. 즉 조준 각도가 10도를 넘어가도 머리는 10도에서 멈추고, 무기(`WeaponPivot`)만 계속 최대 90도까지 회전을 이어간다.
- DevRules.md 네이밍 컨벤션 준수: private 변수 `_camelCase`, `[SerializeField]` 사용, 컴포넌트 참조(`WeaponPivot`/`Head` Transform 등)는 `[SerializeField] private` 필드로 Inspector에서 연결.
- 회전 속도, 클램프 각도(무기 ±90도, 머리 ±10도)는 밸런스 수치라기보다 이번 프로토타입 규모의 연출 상수에 가까워 별도 ScriptableObject로 분리하지 않고 `[SerializeField] private`로 노출하는 선에서 충분하다고 판단한다. DevRules.md의 "3줄로 해결되는 것을 클래스로 만들지 않는다"는 단순함 원칙과 일치한다.
- `TrajectoryPreview.cs`는 `LaunchDirection`을 매 프레임 즉시 반영하는 반면 Weapon은 보간을 거치므로, 드래그 중 순간적으로 점선 궤적과 무기 방향이 어긋날 수 있다는 트레이드오프가 있음을 인지한다(research.md에서 이미 확인된 사항, 이번 범위에서 별도 조치는 하지 않는다).
- **이번 개정으로 1차 구현의 `CharacterAimController.cs`(18도 오프셋, `Weapon` 자신을 회전시키는 클램프 로직)와 `CharacterSetupEditor.cs`(Weapon을 Body의 바로 아래 자식으로 배치하는 계층 구성)는 다음 단계에서 dev 에이전트가 재작성해야 한다. 이 plan.md 수정 자체에서는 코드를 고치지 않는다.**

### 6단계. 씬 배치 및 `LaunchPoint` 정합 (변경 없음)

- `SceneSetupEditor.cs`에 새 Step(예: `Step11_PlaceCharacter()`)을 추가해 Character 프리팹 인스턴스를 씬에 배치하고, 위치를 `LaunchPoint`의 월드 좌표와 일치시킨다.
- Character를 `LaunchPoint`의 자식으로 완전히 재구성하는 대신, "같은 월드 좌표에 배치"하는 방식을 채택한다. 이유: `LaunchPoint`는 이미 `BallLauncher` 계층 구조(`Step8_ConnectBallLauncherRefs`)에 속해 있고 `WallFitter`(Step6) 등 다른 로직이 이 계층을 참조하므로, Character를 그 아래로 끼워 넣으면 기존 구조를 건드리게 되어 부작용 위험이 커진다. 좌표만 맞추는 방식은 기존 `BallLauncher`/`LaunchPoint` 계층을 그대로 유지하면서 시각적 정합만 달성할 수 있어 더 안전하다.
- 대안으로 `LaunchPoint`를 Character의 자식으로 재구성하는 방법도 있으나, 이번 작업에서는 채택하지 않는다.
- 새 Step은 `Step8_ConnectBallLauncherRefs` 이후, `LaunchPoint`의 월드 좌표가 확정된 시점에 실행되도록 `RunFullSetup()`(또는 해당 진입 메서드) 호출 순서에 추가한다.
- **추가 확정 사항**: 사용자가 실제 안드로이드 기기 2대에서 테스트한 결과 현재 배경/벽 위치 설계(`WallFitter`)를 그대로 채택하기로 확정했다. 따라서 Character 위치가 런타임에 `LaunchPoint`를 동적으로 계속 따라가도록 별도 스크립트를 추가할 필요는 없다. 기존에 "실기기 테스트 후 재검토" 대상이었던 이슈는 이번에 종결되었다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Sprites/Character/Character_main_weapon.png.meta` — rect 64×116으로 수정. `spritePivot`은 기존 `{x: 0.18, y: 0.29}`을 유지하거나 기본값 `{x: 0.5, y: 0.5}`로 되돌리는 것 모두 가능하며, Scene 뷰 시각 검증으로 최종 확정.
- `Assets/_Project/Prefabs/Character/Character.prefab` — 신규 생성 (`Character` 루트 아래 Body/Head/WeaponPivot+Weapon 계층, 루트에서 좌우 반전 처리)
- `Assets/_Project/Scripts/Character/CharacterAimController.cs` — 신규 생성 (1차 구현분이 있다면 재작성)
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — Character 프리팹 씬 배치 Step 추가

## 주의사항

- 위 실측 수치(pivot `(0.18, 0.29)` 또는 기본값, Head/WeaponPivot 로컬 좌표 `(-0.34, +0.58)`/`(+0.29, +0.65)` 부근)는 모두 Python 이미지 분석 기반 추정값이다. Unity 에디터에서 실제로 배치·회전시켜보면서 시각적으로 검증하고 필요하면 수치를 조정해야 하며, 이 값들을 "정답"으로 과신하지 않는다.
- `Character_main_weapon.png`의 meta rect 불일치(59×116 vs 실제 64×116) 수정은 반드시 1단계로 가장 먼저 처리한다. 이 수정이 끝나기 전에는 pivot·배치 좌표 수치가 의미를 갖지 않는다.
- 이번 설계 개정으로 확정된 4가지 사항 — (1) 18도 각도 오프셋 삭제, (2) 회전축을 무기 자신의 pivot에서 `WeaponPivot`으로 이전, (3) 좌우 반전 추가, (4) 머리 회전 추가(0~10도 클램프) — 은 모두 사용자가 명시적으로 확정한 내용이다. 다만 정확한 수치(`WeaponPivot` 위치, `Weapon`의 오프셋 거리, 좌우 반전 시 회전 부호 보정)만 Unity 시각 검증이 필요한 추정 대상으로 남아 있다는 점을 구분해서 다뤄야 한다.
- 좌우 반전 자세(방울 오른쪽·무기 왼쪽, 조준 각도 음수 구간)는 왼쪽 조준 중인 원본 게임 레퍼런스 스크린샷 3장으로 직접 검증되었다(research.md의 "왼쪽 조준 레퍼런스 추가 확인 및 좌표 부호 오류 정정" 섹션 참고). 다만 이 검증 과정에서 `Character_Main.png` 합본 자체가 오른쪽 조준이 아니라 왼쪽 조준 자세였다는 것이 함께 확인되어 Head/WeaponPivot 배치 좌표의 부호를 정정했으므로(4단계 참고), 구현 후에도 Unity에서 왼쪽·오른쪽 조준을 모두 시도해보며 방울/무기 위치와 회전 방향이 자연스러운지 육안 검증이 필요하다.
- 이번 설계 변경으로 기존 구현(1차 dev 작업분)의 `CharacterAimController.cs`(18도 오프셋, 클램프 로직)와 `CharacterSetupEditor.cs`(Weapon을 Body의 바로 아래 자식으로 배치)는 다음 단계에서 dev 에이전트가 재작성해야 한다. 이번 문서 수정 자체에서는 코드를 고치지 않는다.
- 이번 작업 범위는 "캐릭터 프리팹 구성 + 무기 조준 회전"까지다. `PlayerActiveSkillDesign.md`에 언급된 액티브 스킬 이펙트(필드 동결/버서크/분신/마법 폭격)나 `CharacterManager`의 HP/XP 로직 변경은 이번 범위에 포함되지 않는다.
