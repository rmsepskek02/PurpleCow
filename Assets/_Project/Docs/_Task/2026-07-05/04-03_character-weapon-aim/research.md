# Research — 캐릭터 프리팹 구성 + 무기 조준 회전 애니메이션

원본 게임(통통 디펜스: 핀볼 마스터) 레퍼런스를 보면 화면 하단 중앙의 플레이어 캐릭터가 오른손에 갈고리 모양 지팡이(무기)를 들고, 그 무기가 조준 방향을 따라 회전한다. 이 문서는 이를 재현하기 위해 필요한 캐릭터 프리팹 구성과 무기 회전 애니메이션을 구현하기 전, 현재 프로젝트에 존재하는 스프라이트 리소스·코드 상태를 파악하고 구현 시 고려해야 할 지점을 정리하는 데 목적이 있다. 구체적인 스크립트 설계나 수치(pivot 좌표, 각도 오프셋 등)의 확정은 이 문서가 아니라 plan.md에서 다룬다.

## 현재 상태

### 스프라이트 리소스

`Assets/_Project/Sprites/Character/`에는 다음 4개 PNG가 이미 존재한다.

| 파일 | 픽셀 크기 | 용도(추정) |
|---|---|---|
| `Character_Main.png` | 141×178 (스프라이트 rect 기준, 캔버스 내 오프셋 23,2) | 머리+몸통+무기가 전부 합쳐진 전체 합본 이미지. 참고/미리보기용으로 추정되며 런타임에서 그대로 쓰기는 어려움 |
| `Character_Main_head.png` | 131×92 | 머리(해골 얼굴) 단독 |
| `Character_Main_body.png` | 48×48 | 몸통/후드 단독 |
| `Character_main_weapon.png` | 59×116 | 갈고리 지팡이(무기) 단독 |

실제로 이미지를 열어 확인한 결과는 다음과 같다.

- `Character_Main.png`(합본)에서 캐릭터는 빨간 후드를 쓴 해골 얼굴이며, 무기는 몸 왼쪽(뷰어 기준)에서 대각선으로 위쪽을 향해 뻗어 있다. 손잡이 쪽(뾰족한 끝)이 몸 쪽 아래에, 갈고리 모양 끝(3갈래로 갈라진 부분)이 위쪽에 위치한다.
- `Character_main_weapon.png` 단독 이미지도 59×116px 세로로 긴 캔버스 안에, 내용물 자체가 좌하단(뾰족한 끝)에서 우상단(갈고리 끝)으로 대각선으로 그려져 있다. 즉 이 스프라이트는 "캔버스 기준 정면(0도)"에서 이미 수직이 아니라 대각선 방향을 향하고 있는 것으로 보인다. 이는 나중에 무기를 조준 방향에 맞춰 회전시킬 때, 단순히 `Vector2.up` 기준 각도를 그대로 적용할 수 없고 별도의 각도 오프셋 보정이 필요하다는 것을 의미한다. 다만 정확한 오프셋 각도는 육안 확인만으로는 정밀하게 특정하기 어려워, **plan.md 단계에서 픽셀 좌표 실측을 통해 확정이 필요**하다.
- `Character_Main_body.png`, `Character_Main_head.png`는 각각 몸통/머리만 잘라낸 형태로, 조준 회전과는 무관하게 고정 파츠로 쓰일 수 있는 상태다.

4개 스프라이트 `.meta` 파일을 모두 확인한 결과, `spritePivot`이 전부 기본값 `{x: 0.5, y: 0.5}`(중앙, `alignment: 0`)로 되어 있다. 무기를 손 위치를 축으로 자연스럽게 회전시키려면 `Character_main_weapon.png`의 pivot을 회전축(캐릭터가 무기를 쥔 손 부근)으로 재조정해야 한다.

### 프리팹/코드 상태

- `Assets/_Project/Prefabs/`에는 `Ball`, `Monster`, `UI` 서브폴더만 존재하고, Character 프리팹은 아직 없다.
- `Assets/_Project/Scripts/Core/CharacterManager.cs`는 HP/XP/레벨 로직(`OnHpChanged`/`OnXpChanged`/`OnLevelUp` 이벤트, `WaveManager.OnMonsterReachedBottom`/`MonsterBase.OnMonsterDied` 구독)만 가지고 있고, 스프라이트나 씬 상의 캐릭터 비주얼과는 전혀 연결되어 있지 않다.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `Step7_PlaceManagers()`에서 `PlaceManager<CharacterManager>("CharacterManager")`로 씬에 빈 매니저 오브젝트만 배치한다. 캐릭터 비주얼(스프라이트/애니메이션)을 생성하는 로직은 없다.

### 조준 방향 데이터 소스 (기존 코드 확인)

무기 회전에 재사용할 "현재 조준 방향" 데이터는 이미 다음 경로로 흐르고 있다.

1. `Assets/_Project/Scripts/Core/InputHandler.cs`: 터치/마우스 입력을 받아 `ComputeAimDirection()`에서 `BallLauncher.Instance.LaunchPoint.position` → 터치 월드 좌표를 향하는 정규화된 방향 벡터를 계산하고, `OnDrag`(`Action<Vector2>`) 이벤트로 매 프레임 발행한다. 터치 시작 시 `OnAimBegin`, 해제 시 `OnRelease` 이벤트도 있다.
2. `Assets/_Project/Scripts/Ball/BallLauncher.cs`: `OnEnable()`에서 `InputHandler.OnDrag`를 구독(`HandleDrag`)해 `_launchDirection` 필드를 갱신하고, `public Vector2 LaunchDirection => _launchDirection;` 프로퍼티로 외부에 노출한다. 기본값은 `Vector2.up`이며, 터치하지 않을 때도 마지막 조준 방향을 그대로 유지한다. `LaunchPoint`는 `BallLauncher`의 자식 Transform으로, `SceneSetupEditor.cs`(`Step8_ConnectBallLauncherRefs`)에서 로컬 좌표 `(0, -8, 0)`에 빈 GameObject로 생성되며 현재 비주얼은 없다.
3. `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`: `Update()`에서 매 프레임 `BallLauncher.Instance.LaunchDirection`을 읽어 궤적 프리뷰(점선 2단계 + 2차 충돌 지점 레드닷/링)를 그린다. 터치 여부와 무관하게 항상 최신 방향을 즉시(보간 없이) 반영한다.

즉 무기 회전에 필요한 "현재 조준 방향" 값은 이미 `BallLauncher.Instance.LaunchDirection`으로 존재하며, 별도의 신규 데이터 소스를 만들 필요는 없어 보인다. 다만 `TrajectoryPreview`는 이 값을 프레임마다 즉시 반영하는 반면, 무기는 부드러운 보간으로 뒤따라가야 하므로 두 비주얼이 같은 값을 다른 속도로 추적하게 된다(아래 문제점 참고).

### 원본 게임 레퍼런스 재확인

`Assets/_Project/Docs/targetUI/KakaoTalk_20260703_115951058_01.jpg` 등 스테이지 1 실제 플레이 캡처를 다시 확인한 결과, 캐릭터는 화면 하단 중앙에 고정 배치되어 있고 오른손(뷰어 기준 우측)에 무기를 쥔 채 발사 궤적(점선)의 시작 방향과 무기가 같은 방향을 향하는 것으로 보인다. 다만 캡처가 정적 스크린샷이라 회전의 보간 속도, 클램프 각도의 정확한 한계까지는 이미지만으로 확인할 수 없다.

## 관련 파일 및 의존성

| 파일 | 역할 | 이번 작업과의 관계 |
|---|---|---|
| `Assets/_Project/Sprites/Character/Character_Main.png` | 머리+몸통+무기 합본 스프라이트 | 참고용, 파츠 분리 스프라이트를 대신 사용할 예정이라 직접 사용 여부는 plan.md에서 결정 |
| `Assets/_Project/Sprites/Character/Character_Main_head.png` | 머리 단독 스프라이트 | Body 파츠에 합쳐 고정 파츠로 사용(별도 분리 안 함) |
| `Assets/_Project/Sprites/Character/Character_Main_body.png` | 몸통/후드 단독 스프라이트 | Body 파츠(고정, 회전 없음) |
| `Assets/_Project/Sprites/Character/Character_main_weapon.png` | 무기 단독 스프라이트 | Weapon 파츠(회전 대상), pivot 재조정 필요 |
| `Assets/_Project/Scripts/Core/CharacterManager.cs` | HP/XP/레벨 로직 전담 싱글톤 | 스프라이트/비주얼과 무관, 이번 작업 대상 아님(그대로 유지) |
| `Assets/_Project/Scripts/Core/InputHandler.cs` | 터치 입력을 조준 방향 벡터로 변환, `OnDrag` 이벤트 발행 | 무기 회전이 참조할 조준 방향의 원천 데이터 |
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | `OnDrag` 구독, `LaunchDirection` 프로퍼티 노출, `LaunchPoint` 보유 | 신규 무기 회전 스크립트가 `LaunchDirection`을 읽어올 대상. `LaunchPoint`와 Character 배치 위치 정합 문제 관련 |
| `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` | `LaunchDirection`을 매 프레임 즉시 반영해 궤적 프리뷰 표시 | 즉시 반영 vs 무기의 부드러운 보간 간 시각적 어긋남 발생 가능 |
| `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | 씬에 매니저/프리팹 자동 배치 | Character 프리팹 생성 및 씬 배치 로직이 아직 없음. `LaunchPoint` 생성 로직(Step8) 존재 |
| `Assets/_Project/Prefabs/` (Ball, Monster, UI 서브폴더) | 기존 프리팹 저장 위치 | Character 서브폴더 신규 생성 필요 |

## 문제점 / 구현 대상 파악

### 1. 무기 스프라이트 pivot 재조정

4개 스프라이트 모두 pivot이 `{0.5, 0.5}`(중앙)로 되어 있다. 특히 회전 대상인 `Character_main_weapon.png`는 캐릭터가 손으로 쥔 지점(이미지 하단 뾰족한 끝 부근)을 pivot으로 잡아야 회전축이 자연스러워진다. 정확한 pivot 좌표값은 이 문서에서 확정하지 않고 plan.md에서 실측/결정한다.

### 2. 무기 원본 이미지의 "0도 기준 방향" 파악

`Character_main_weapon.png`는 캔버스(59×116) 안에서 내용물이 좌하단→우상단 대각선으로 그려져 있어, 회전 스크립트에서 "조준 방향 각도"를 그대로 `transform.rotation`에 대입하면 실제 갈고리 끝이 조준 방향과 다른 곳을 향할 가능성이 높다. 즉 스프라이트 자체의 기본 방향과 조준 각도 사이에 상수 오프셋을 더하거나 빼야 한다. 육안 확인상 갈고리 끝이 이미지 내에서 위쪽보다는 오른쪽으로 상당히 기울어진 대각선 방향을 향하고 있는 것으로 보이나, 정확한 오프셋 각도(도 단위)는 이미지 픽셀 좌표를 다시 실측해 plan.md 단계에서 확정이 필요하다.

### 3. 캐릭터 배치 위치와 `LaunchPoint`의 정합

현재 `LaunchPoint`는 `BallLauncher`의 자식 Transform으로 로컬 좌표 `(0, -8, 0)`에 위치하며 비주얼이 없는 빈 오브젝트다. 화면상 캐릭터가 서 있는 위치와 볼이 실제로 발사되는 지점(`LaunchPoint`)이 시각적으로 일치해야 하므로, 다음과 같은 방안이 후보로 있을 수 있다.

- Character 프리팹을 씬에 별도 배치하고 좌표를 `LaunchPoint`의 월드 좌표와 수동으로 맞춘다.
- `LaunchPoint`를 Character 프리팹의 자식으로 재구성해 항상 캐릭터를 따라가도록 한다.

어느 방식을 택할지, `SceneSetupEditor.cs`의 어느 Step에 캐릭터 배치 로직을 추가할지는 plan.md에서 결정한다.

### 4. 무기 회전 범위 및 보간 로직

사용자 확정 사항에 따라 다음 두 가지 설계가 이미 정해져 있으나, 실제 계산 방식은 plan.md에서 확정한다.

- **회전 범위**: 위쪽 반원만 허용(좌우 ±90도로 클램프). 볼이 항상 위쪽으로 발사/재발사되는 구조와 맞추기 위함. 계산 방식은 `Mathf.Atan2(direction.x, direction.y)` 등으로 각도를 구한 뒤 `Mathf.Clamp`로 제한하는 방식이 후보로 있으나 확정은 아니다.
- **보간**: 즉시 스냅이 아닌 부드러운 보간 적용. `Mathf.LerpAngle`, `Quaternion.RotateTowards` 등이 후보로 있으나 확정은 plan.md 몫이다.

이 보간 방식 선택으로 인해, `TrajectoryPreview`(매 프레임 즉시 반영)와 무기가 가리키는 방향이 드래그 중 순간적으로 어긋날 수 있다는 트레이드오프가 존재한다. 즉 손가락을 빠르게 움직이면 점선 궤적은 즉시 새 방향을 가리키지만 무기는 잠시 뒤처져 따라가는 시각적 차이가 발생할 수 있다. 이는 사용자가 이미 인지하고 감수하기로 한 트레이드오프이며, 이 문서에서는 문제로만 명시해둔다.

### 5. 신규 스크립트 필요

현재 조준 방향에 따라 무기 Transform을 회전시키는 로직을 담당하는 스크립트가 존재하지 않는다. 신규 스크립트가 필요하다는 사실만 이 문서에서 확인하며, 정확한 이름(가칭 `CharacterAimController.cs` 등)과 부착 위치(Character 프리팹 루트 또는 Weapon 자식 오브젝트)는 plan.md에서 확정한다.

### 6. 프리팹 파츠 구성

사용자 확정 사항: Body(head+body 합본, 고정) + Weapon(별도 자식 Transform, 회전 대상)의 2파츠 구조로 만든다. Head는 별도로 분리하지 않는다(현재 스펙에 머리 각도 연출 요구가 없어 단순함 우선 원칙에 따름). 다만 `Character_Main_head.png`와 `Character_Main_body.png`가 별도 파일로 분리되어 있는 만큼, 이 둘을 하나의 Body 오브젝트 아래 자식 스프라이트 2장으로 둘지, 아니면 사전에 하나로 합성해서 쓸지는 plan.md에서 결정이 필요하다.

## 결론

Character 프리팹을 구성하고 무기가 조준 방향을 따라 회전하도록 만드는 작업은, 리소스 관점에서는 이미 존재하는 4개 스프라이트(`Character_Main`/`Character_Main_head`/`Character_Main_body`/`Character_main_weapon`) 중 Body(head+body 고정)와 Weapon(회전 대상) 2파츠를 조합하는 구조로 진행하면 되고, 데이터 관점에서는 신규 데이터 소스를 만들 필요 없이 이미 `BallLauncher.LaunchDirection`으로 조준 방향이 흐르고 있어 이를 그대로 재사용하면 된다.

다만 실제 구현에 앞서 확정이 필요한 지점이 세 가지 있다. 첫째, `Character_main_weapon.png`는 4개 스프라이트 모두 pivot이 중앙(`0.5, 0.5`)으로 되어 있어 손 위치 기준으로 재조정이 필요하고, 이미지 자체가 대각선으로 그려져 있어 조준 각도와 스프라이트 기본 방향 사이의 오프셋 계산도 필요하다(정확한 수치는 plan.md에서 실측 확정). 둘째, 캐릭터를 씬의 어디에 배치하고 `LaunchPoint`와 어떻게 정합시킬지 결정해야 한다. 셋째, 좌우 ±90도 클램프와 부드러운 보간을 구체적으로 어떤 API 조합으로 구현할지 확정해야 하며, 이 보간으로 인해 즉시 반영되는 `TrajectoryPreview`와 무기 사이에 순간적인 방향 불일치가 생길 수 있다는 점은 이미 알려진 트레이드오프로 남겨둔다. 이 모든 확정 사항은 다음 plan.md에서 다룬다.
