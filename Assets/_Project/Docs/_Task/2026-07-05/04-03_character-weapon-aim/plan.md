# Plan — 캐릭터 프리팹 구성 + 무기 조준 회전 애니메이션

이 문서는 research.md에서 확인한 현재 상태와, 오케스트레이터가 Python(PIL/numpy)으로 실제 스프라이트 픽셀을 분석해 확정한 수치(각도 오프셋, pivot, 파츠 배치 좌표)를 바탕으로 Character 프리팹 구성과 무기 조준 회전 기능의 구체적인 구현 절차를 정리한다. 스프라이트 meta 불일치 수정 → pivot/배치 재조정 → 프리팹 생성 → 회전 스크립트 작성 → 씬 배치 순으로 진행한다. 아래 수치는 모두 이미지 분석 기반 추정값이며, Unity 에디터에서 실제로 배치·회전해보며 시각적으로 검증/미세조정하는 절차를 포함한다.

## 구현 목표

- `Character_main_weapon.png`의 meta rect 불일치(59×116 vs 실제 64×116)를 먼저 바로잡는다.
- Body(머리+몸통 고정)와 Weapon(회전 대상) 2파츠로 구성된 Character 프리팹을 생성한다.
- `BallLauncher.Instance.LaunchDirection`을 읽어 Weapon Transform을 조준 방향으로 부드럽게 회전시키는 `CharacterAimController.cs`를 작성한다.
- Character 프리팹을 씬에 배치하고 `LaunchPoint`와 위치를 정합시킨다.
- 이번 범위는 여기까지이며, 액티브 스킬 이펙트(필드 동결/버서크/분신/마법 폭격)나 `CharacterManager`의 HP/XP 로직 변경은 포함하지 않는다.

## 단계별 작업 계획

### 1단계. 무기 스프라이트 meta rect 불일치 확인 및 수정 (최우선)

- 현재 `Character_main_weapon.png.meta`의 `spriteSheet.sprites[0].rect`가 `width: 59, height: 116`으로 기록되어 있으나, 실제 PNG 픽셀 분석 결과 64×116이다. PNG가 meta 생성 이후 교체되고 meta가 갱신되지 않은 상태로 보인다.
- Unity 에디터에서 `Character_main_weapon.png`를 우클릭 → Reimport 를 먼저 시도해 rect가 64×116으로 자동 갱신되는지 확인한다.
- 자동 갱신되지 않으면 Sprite Editor를 열어 rect를 이미지 전체(0,0,64,116)로 수동 재설정한다.
- 이 단계가 끝나기 전에는 아래 pivot/배치 수치가 의미를 갖지 않으므로 반드시 가장 먼저 처리한다.
- 참고로 `Character_Main_head.png`(rect 131×92, 실측 132×92로 거의 일치)와 `Character_Main_body.png`(rect 48×48, 실측 48×48 정확히 일치)는 문제가 없어 별도 조치가 필요 없다.

### 2단계. 무기 스프라이트 pivot 재조정

- 64×116 캔버스 기준으로, 손잡이 끝(하단)에서 갈고리 끝(상단)으로 향하는 축 위에서 손잡이 끝으로부터 약 25% 지점을 pivot으로 잡는다.
- 산출된 추천값: `spritePivot` ≈ `(0.18, 0.29)` (Unity 좌표계, (0,0)=좌하단, (1,1)=우상단 기준).
- `Character_main_weapon.png.meta`에서 `alignment: 0` → `alignment: 9`(Custom)로 변경하고 `spritePivot`을 위 값으로 설정한다.
- 이 값은 이미지 픽셀 분석으로 산출한 추정치이므로, Unity Scene 뷰에서 Weapon 오브젝트를 실제로 회전시켜보면서 회전축이 자연스러운지(갈고리 끝이 손 위치를 축으로 원을 그리며 도는지) 최종 미세조정한다. 과신하지 않고 반드시 시각 검증을 거친다.

### 3단계. 무기 회전 각도 오프셋 확정

- 무기 스프라이트(64×116) 내용물의 손잡이 끝→갈고리 끝 축이, 스프라이트가 회전 없이(0도) 배치된 기본 상태에서 이미 수직(`Vector2.up`)으로부터 약 18도만큼 오른쪽(+X 방향)으로 기울어져 그려져 있다.
- 따라서 조준 각도를 그대로 `transform.eulerAngles.z`에 대입하면 안 되고, 약 18도의 보정값을 빼거나 더해야 한다.
- 정확한 부호(+18 vs -18)는 Unity의 Z축 회전 방향(반시계 방향이 양수)과 실측한 "오른쪽으로 기운" 방향을 대조해서 결정해야 하는데, 이는 실제로 4단계에서 스크립트를 작성하고 Play 모드 또는 Scene 뷰에서 무기를 회전시켜보며 육안으로 확정한다. 이 문서 시점에서는 추정치(±18도 중 하나)로만 남겨두고, 구현 단계에서 시각적으로 검증한다.

### 4단계. Character 프리팹 계층 구조 생성

- Body를 프리팹의 기준(부모) 오브젝트로 삼는다.
- 계층 구조:
  - `Character` (루트, 빈 GameObject)
    - `Body` (SpriteRenderer, `Character_Main_body.png`, sortingOrder 낮게)
    - `Head` (SpriteRenderer, `Character_Main_head.png`, Body 또는 Character의 자식, sortingOrder 중간)
    - `Weapon` (SpriteRenderer, `Character_main_weapon.png`, Character 또는 Body의 자식, sortingOrder 가장 높게 — 레퍼런스 스크린샷에서 무기가 몸통보다 앞에 그려짐)
- 파츠 배치 좌표(Body 중심 기준 로컬 좌표, Pixels Per Unit 100 기준, 템플릿 매칭 역산 추천값):
  - Head: 약 `(+0.34, +0.58)`
  - Weapon: 약 `(-0.29, +0.65)` (2단계에서 pivot을 (0.18, 0.29)로 재조정했다는 전제 하의 값)
- 이 좌표들은 `Character_Main.png`(합본, 141×178 rect) 안에서 각 파츠가 차지하는 위치를 이미지 템플릿 매칭으로 역산한 추정치이며, 특히 Weapon은 다른 파츠보다 매칭 신뢰도가 낮았다. Unity 씬에 `Character_Main.png`를 반투명 참고 오버레이(예: 임시 SpriteRenderer, alpha 0.3~0.5)로 띄워놓고, Head/Body/Weapon을 눈으로 겹쳐보며 최종 위치를 미세조정한다. 오버레이 오브젝트는 검증 후 삭제하거나 비활성화한다.
- 프리팹 저장 위치: `Assets/_Project/Prefabs/Character/Character.prefab` (신규 `Character` 서브폴더 생성).

### 5단계. `CharacterAimController.cs` 작성

- 배치: `Assets/_Project/Scripts/Character/CharacterAimController.cs` (신규 `Character` 폴더 생성, 기존 `Ball`/`Monster`/`UI`/`Skill` 폴더 구조와 동일한 방식).
- 역할: `Update()`에서 `BallLauncher.Instance.LaunchDirection`을 읽어
  1. `Mathf.Atan2(direction.x, direction.y)`로 목표 각도(위쪽 기준) 계산.
  2. 3단계에서 확정한 약 18도 오프셋을 보정.
  3. 좌우 ±90도로 `Mathf.Clamp`.
  4. `Mathf.LerpAngle` 또는 `Quaternion.RotateTowards`로 부드럽게 보간(회전 속도는 `[SerializeField] private float _rotationSpeed`로 Inspector 노출)하여 Weapon Transform의 Z 회전에 적용.
- DevRules.md 네이밍 컨벤션 준수: private 변수 `_camelCase`, `[SerializeField]` 사용, 컴포넌트 참조(Weapon Transform 등)는 `[SerializeField] private` 필드로 Inspector에서 연결.
- 회전 속도, 클램프 각도(±90도), 오프셋 각도(18도)는 밸런스 수치라기보다 이번 프로토타입 규모의 연출 상수에 가까워 별도 ScriptableObject로 분리하지 않고 `[SerializeField] private`로 노출하는 선에서 충분하다고 판단한다. DevRules.md의 "3줄로 해결되는 것을 클래스로 만들지 않는다"는 단순함 원칙과 일치한다.
- `TrajectoryPreview.cs`는 `LaunchDirection`을 매 프레임 즉시 반영하는 반면 Weapon은 보간을 거치므로, 드래그 중 순간적으로 점선 궤적과 무기 방향이 어긋날 수 있다는 트레이드오프가 있음을 인지한다(research.md에서 이미 확인된 사항, 이번 범위에서 별도 조치는 하지 않는다).

### 6단계. 씬 배치 및 `LaunchPoint` 정합

- `SceneSetupEditor.cs`에 새 Step(예: `Step11_PlaceCharacter()`)을 추가해 Character 프리팹 인스턴스를 씬에 배치하고, 위치를 `LaunchPoint`의 월드 좌표와 일치시킨다.
- Character를 `LaunchPoint`의 자식으로 완전히 재구성하는 대신, "같은 월드 좌표에 배치"하는 방식을 채택한다. 이유: `LaunchPoint`는 이미 `BallLauncher` 계층 구조(`Step8_ConnectBallLauncherRefs`)에 속해 있고 `WallFitter`(Step6) 등 다른 로직이 이 계층을 참조하므로, Character를 그 아래로 끼워 넣으면 기존 구조를 건드리게 되어 부작용 위험이 커진다. 좌표만 맞추는 방식은 기존 `BallLauncher`/`LaunchPoint` 계층을 그대로 유지하면서 시각적 정합만 달성할 수 있어 더 안전하다.
- 대안으로 `LaunchPoint`를 Character의 자식으로 재구성하는 방법도 있으나, 이번 작업에서는 채택하지 않는다.
- 새 Step은 `Step8_ConnectBallLauncherRefs` 이후, `LaunchPoint`의 월드 좌표가 확정된 시점에 실행되도록 `RunFullSetup()`(또는 해당 진입 메서드) 호출 순서에 추가한다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Sprites/Character/Character_main_weapon.png.meta` — rect 64×116으로 수정, `alignment: 9`, `spritePivot: {x: 0.18, y: 0.29}`로 변경
- `Assets/_Project/Prefabs/Character/Character.prefab` — 신규 생성 (Body/Head/Weapon 계층)
- `Assets/_Project/Scripts/Character/CharacterAimController.cs` — 신규 생성
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` — Character 프리팹 씬 배치 Step 추가

## 주의사항

- 위 실측 수치(각도 오프셋 약 18도, pivot (0.18, 0.29), Head/Weapon 로컬 좌표 (+0.34, +0.58)/(-0.29, +0.65))는 모두 Python 이미지 분석 기반 추정값이다. Unity 에디터에서 실제로 배치·회전시켜보면서 시각적으로 검증하고 필요하면 수치를 조정해야 하며, 이 값들을 "정답"으로 과신하지 않는다.
- `Character_main_weapon.png`의 meta rect 불일치(59×116 vs 실제 64×116) 수정은 반드시 1단계로 가장 먼저 처리한다. 이 수정이 끝나기 전에는 pivot·배치 좌표 수치가 의미를 갖지 않는다.
- 이번 작업 범위는 "캐릭터 프리팹 구성 + 무기 조준 회전"까지다. `PlayerActiveSkillDesign.md`에 언급된 액티브 스킬 이펙트(필드 동결/버서크/분신/마법 폭격)나 `CharacterManager`의 HP/XP 로직 변경은 이번 범위에 포함되지 않는다.
