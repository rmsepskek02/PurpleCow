# Research — Character Visual Implementation

캐릭터 에셋(머리/몸통/무기 파츠)을 사용해 실제 화면에 보이는 캐릭터 오브젝트를 구현하기 전, 현재 프로젝트에 캐릭터 관련 코드/에셋이 어떤 상태인지 조사한 문서입니다. `CharacterManager.cs`가 HP/XP 로직만 담당하고 시각 표현은 전혀 없다는 점, `LaunchPoint`가 스프라이트 없는 빈 오브젝트라는 점, 무기 스프라이트의 그립 위치가 기본 Pivot과 어긋나 있다는 점을 확인했습니다. 이후 사용자와 조준 회전 방식(Body/Head/Weapon 각각의 반전·회전 강도)을 논의해 방향성을 확정했으며, 구현 방법(plan.md)은 포함하지 않습니다.

---

## 현재 상태

### 캐릭터 스프라이트 에셋 (`Assets/_Project/Sprites/Character/`)

4개 파일이 존재하며, 실제 이미지를 확인한 결과는 다음과 같다.

- `Character_Main.png` (178x218) — 후드를 쓴 캐릭터가 지팡이형 무기를 든 모습의 파츠 합성 미리보기(레퍼런스용 완성 이미지로 추정, 파츠 분리본이 아님)
- `Character_Main_head.png` (132x92) — 빨간 후드와 해골 얼굴이 결합된 머리 파츠
- `Character_Main_body.png` (48x48) — 후드 아래로 보이는 옷자락(몸통 하단) 파츠
- `Character_main_weapon.png` (파일 자체는 64x116, `.meta`의 `spriteSheet.rect`는 `width: 59, height: 116` — 알파 여백을 제외한 실제 스프라이트 사각형이 좁게 잡혀 있음) — 갈고리형 머리, 가로 줄무늬 그립, 뾰족한 스파이크 끝으로 구성된 지팡이형 무기

`Character_main_weapon.png.meta`를 직접 확인한 결과 `spriteMode: 2`(Multiple)이고 `pivot: {x: 0, y: 0}`(스프라이트 시트 슬라이스 자체의 pivot, 좌하단), 하지만 `TextureImporter` 최상단의 `spritePivot: {x: 0.5, y: 0.5}`도 함께 존재해 실제 Import 시 적용되는 Pivot이 정중앙임을 확인했다. 즉 현재 무기 스프라이트는 **중앙 Pivot**으로 임포트되어 있다.

무기 스프라이트를 확대하여 육안으로 확인한 결과, 파츠 구성은 다음과 같다.
- 위쪽: 넓게 갈라진 갈고리(hook) 모양 머리 부분
- 중간(원본 픽셀 기준 대략 x≈23, y≈66 지점, 위쪽 기준): 가로 띠 무늬가 감긴 그립(손잡이) 부분 — 캐릭터가 실제로 쥐는 지점으로 추정
- 아래쪽: 가늘고 뾰족한 스파이크 끝
- Unity Sprite Pivot(좌하단 원점, 0~1 정규화 좌표) 기준으로 환산하면 그립 위치는 대략 **(0.36, 0.43)**로, 현재 설정된 중앙 Pivot(0.5, 0.5)과는 차이가 있다.

원본 게임 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/KakaoTalk_20260701_190324151.jpg`)을 확인한 결과, 캐릭터는 화면 하단 중앙 부근에 고정 배치되어 있고, 손에 쥔 무기(지팡이)가 조준 방향(화면에 점선으로 표시되는 궤적 프리뷰 라인)을 향해 회전한 형태로 그려져 있다. 몸통 자체는 캐릭터가 이동하지 않고 한 자리에 고정된 것으로 보인다.

### CharacterManager.cs (`Assets/_Project/Scripts/Core/CharacterManager.cs`)

- `Singleton<CharacterManager>`로 구현되어 있으며, 담당 로직은 HP(`_currentHp`/`_maxHp`), XP(`_currentXp`/`_xpPerLevel`), 레벨(`_currentLevel`)뿐이다.
- `WaveManager.OnMonsterReachedBottom`, `MonsterBase.OnMonsterDied` 이벤트를 구독해 데미지/경험치 처리를 하고, `OnHpChanged`/`OnXpChanged`/`OnLevelUp` 이벤트를 발행한다.
- `SpriteRenderer`, `Transform` 참조, 애니메이션, 회전 등 시각적 요소를 다루는 필드/메서드가 전혀 없다. 즉 이 클래스는 순수하게 스탯 관리 전용이며, 캐릭터의 "모습"과는 무관하다.

### 씬 배치 — SceneSetupEditor.cs (`Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`)

- `Step6_PlaceManagers()`(383~409행)가 `PlaceManager<CharacterManager>("CharacterManager")`(408행)를 호출해 `CharacterManager` 매니저 오브젝트를 씬에 배치하지만, 이는 `BoxCollider2D`만 붙는 매니저 전용 배치 헬퍼(`PlaceManager<T>`, 412행~)로 스프라이트나 자식 파츠 구조와는 무관하다.
- `Step7_ConnectBallLauncherRefs(Ball ballPrefab)`(436행~) 안에서 `BallLauncher`의 자식으로 `LaunchPoint`(473~482행)를 생성한다. `localPosition = new Vector3(0f, -8f, 0f)`로 고정 좌표에 배치되며, `SpriteRenderer`나 다른 시각 컴포넌트는 전혀 추가하지 않는 **빈 GameObject**다. 생성 직후 `BallLauncher._launchPoint`(`SerializedProperty`)에 바로 연결된다.
- 즉 현재 씬 어디에도 "화면에 보이는 캐릭터"에 대응하는 GameObject/스프라이트가 존재하지 않는다. `LaunchPoint`는 볼 발사/귀환 기준점 역할만 하는 순수 좌표값이다.

### BallLauncher.cs (`Assets/_Project/Scripts/Ball/BallLauncher.cs`) — 무기 회전에 필요한 조준 데이터

- `Singleton<BallLauncher>`. `_launchDirection`(16행, 기본값 `Vector2.up`)을 `HandleDrag(Vector2 direction)`(58~61행)에서 매 프레임 갱신한다. `HandleDrag`는 `OnEnable`(46~50행)에서 `InputHandler.OnDrag`에 구독되어 있다.
- `public Vector2 LaunchDirection => _launchDirection;`(25행)로 이미 외부에 읽기 전용으로 노출되어 있어, 무기 회전을 담당할 신규 스크립트가 별도의 이벤트 구독 없이도 `BallLauncher.Instance.LaunchDirection`을 폴링하거나, `InputHandler.OnDrag`를 직접 구독해 동일한 방향값을 받아볼 수 있다.
- `LaunchPoint` 프로퍼티(24행)로 `_launchPoint` Transform도 노출되어 있어, 캐릭터 오브젝트를 이 위치에 배치하는 데 활용 가능하다.
- 참고로 이번 조사 중 `BallLauncher.cs`가 최근 볼 로스터(`BallRosterEntry`, `_roster`) 구조로 이미 재설계되어 있음을 확인했다(2026-07-01 `ball-launch-mechanics` task의 결과로 추정). 이는 이번 캐릭터 시각 구현 task와 직접 관련은 없으나, `LaunchDirection`/`LaunchPoint` 두 프로퍼티의 존재와 동작 방식은 변함없이 그대로 사용 가능함을 재확인했다.

### PDF 요구사항 스펙 (`PurpleCow_클라이언트_채용과제.pdf`, 레포 루트)

- 핵심 필수 구현 항목은 핀볼 로직/웨이브 진행/3택지 스킬 선택/결과 팝업 등이며, 캐릭터의 시각적 표현(스프라이트 파츠 구성, 무기 회전 연출 등)에 대한 명시적 요구사항은 없다.
- 다만 이 프로젝트가 원본 게임(통통 디펜스: 핀볼 마스터) 1스테이지를 카피하는 과제이므로, 시각적 완성도 차원에서 캐릭터 표현이 필요하다는 것이 이번 작업의 동기다.

---

## 관련 파일 및 의존성

| 파일 | 역할 | 비고 |
|---|---|---|
| `Assets/_Project/Sprites/Character/Character_Main.png` | 파츠 합성 완성 미리보기 (178x218) | 실제 게임 오브젝트 구성에 직접 쓰이는 파츠가 아닌 것으로 추정 |
| `Assets/_Project/Sprites/Character/Character_Main_head.png` | 머리(후드+얼굴) 파츠 (132x92) | 좌우 반전 + 약한 감쇠 회전 적용 대상 |
| `Assets/_Project/Sprites/Character/Character_Main_body.png` | 몸통(옷자락) 파츠 (48x48) | 회전 없음, 좌우 반전만 적용 |
| `Assets/_Project/Sprites/Character/Character_main_weapon.png` | 무기(지팡이) 파츠 (파일 64x116 / 스프라이트 사각형 59x116) | 좌우 반전 + 조준 방향을 거의 그대로 따르는 강한 회전 적용 대상. 현재 Pivot이 중앙(0.5, 0.5)으로 설정되어 있어 그립 위치(대략 0.36, 0.43)로 재설정 필요 |
| `Assets/_Project/Scripts/Core/CharacterManager.cs` | HP/XP/레벨 관리 전용 Singleton | 시각 요소 없음. 이번 작업에서 건드리지 않고 그대로 유지 |
| `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | 씬 자동 배치 에디터 스크립트. `Step6_PlaceManagers()`(CharacterManager 배치), `Step7_ConnectBallLauncherRefs()`(LaunchPoint 생성 및 BallLauncher 연결) | 캐릭터 시각 오브젝트 배치용 Step이 아직 없음. 기존 Step 추가 패턴을 재사용할 여지가 있음 |
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | 조준 방향(`LaunchDirection`, 25행)과 발사 기준점(`LaunchPoint`, 24행)을 외부에 노출 | 무기 회전 스크립트가 참조할 데이터 소스. `_launchDirection`은 `InputHandler.OnDrag` 구독으로 매 프레임 갱신됨 |
| `Assets/_Project/Scripts/Core/InputHandler.cs` | 터치/마우스 입력을 `OnDrag`/`OnRelease` 이벤트로 변환 | `BallLauncher`가 이미 구독 중인 원본 이벤트 소스. 캐릭터 회전 스크립트가 직접 구독할 수도 있음(설계 시 선택지) |
| `Assets/_Project/Docs/targetUI/KakaoTalk_20260701_190324151.jpg` | 원본 게임 실제 플레이 캡처 | 캐릭터가 화면 하단 중앙에 고정 배치되고, 무기가 조준 방향을 향해 회전하는 형태를 시각적으로 확인 |
| `PurpleCow_클라이언트_채용과제.pdf` (레포 루트) | 공식 요구사항 스펙 | 캐릭터 시각 표현에 대한 명시적 요구사항 없음(참고용 배경 정보) |

---

## 문제점 / 구현 대상 파악

- **시각 캐릭터 오브젝트 자체가 부재**: 씬에 캐릭터의 모습을 렌더링하는 GameObject/SpriteRenderer가 전혀 없다. `LaunchPoint`는 좌표 기준점일 뿐 스프라이트가 붙어 있지 않으며, `CharacterManager`도 스탯 로직 전용이라 이 둘 중 어느 쪽도 시각 표현을 대신하지 않는다. 머리/몸통/무기 3개 파츠를 각각 렌더링할 오브젝트 구조를 새로 만들어야 한다.
- **무기 스프라이트 Pivot 불일치**: `Character_main_weapon.png`는 현재 중앙 Pivot(0.5, 0.5)으로 임포트되어 있으나, 실제 그립(손잡이) 위치는 대략 (0.36, 0.43)이다. 기본 Pivot 그대로 회전시키면 회전축이 그립이 아닌 몸통 중앙 부근에 잡혀, 캐릭터가 무기를 쥔 손 위치를 중심으로 회전하는 것이 아니라 무기 전체가 부자연스럽게 허공에서 도는 것처럼 보일 수 있다. Import Settings에서 Pivot을 Custom으로 재설정하는 작업이 필요하다.
- **파츠별 회전/반전 강도 차등 적용 로직 부재**: 사용자와 확정한 방향에 따르면 Body는 회전 없이 좌우 반전만, Head는 좌우 반전 + 약하게 감쇠된 회전, Weapon은 좌우 반전 + 조준 방향을 거의 그대로 따르는 강한 회전이 필요하다. 그러나 현재 이 3단계 차등 회전을 계산하는 스크립트가 전혀 없으며, `BallLauncher.LaunchDirection`을 구독/폴링해 각도를 계산하고 파츠별 감쇠 계수를 적용하는 로직을 새로 작성해야 한다.
- **[열린 이슈] 좌우 반전과 자식 회전 각도의 부호 충돌 문제**: 캐릭터 전체(또는 파츠별)를 `localScale.x = -1` 방식으로 반전할 경우, 그 자식으로 붙은 Head/Weapon의 로컬 회전 각도 부호도 함께 뒤집혀 반전 상태에 따라 같은 조준 방향이라도 다르게 회전해 보이는 문제가 발생할 수 있다. 이를 해결하는 구체적인 방식(예: 반전 여부에 따라 회전 각도 계산식을 다시 매핑하는 방법, 또는 `localScale` 반전 대신 `SpriteRenderer.flipX`만 사용하고 회전은 항상 월드 좌표 기준으로 별도 계산하는 방법 등)은 아직 결정되지 않았다. 이는 plan.md 단계에서 dev 에이전트와 함께 구체화가 필요한 미결정 사항이다.
- **캐릭터 오브젝트 배치 위치 확정 필요**: `LaunchPoint`(`BallLauncher`의 자식, `localPosition = (0, -8, 0)`)가 곧 캐릭터가 서 있는 위치로 추정되나, 캐릭터 시각 오브젝트를 `LaunchPoint`와 동일한 위치에 별도로 둘지, 아니면 `LaunchPoint` 자체를 캐릭터 오브젝트의 자식/자기 자신으로 통합할지는 아직 결정되지 않았다. 무기 스프라이트가 이 위치를 중심으로 회전해야 하므로 위치/계층 구조 확정이 필요하다.
- **씬 자동화 스크립트에 캐릭터 배치 Step 부재**: `SceneSetupEditor.cs`는 매니저 오브젝트(`Step6_PlaceManagers`)와 `LaunchPoint`(`Step7_ConnectBallLauncherRefs`)를 자동 생성하는 기존 패턴을 갖고 있지만, 캐릭터 파츠(Body/Head/Weapon) SpriteRenderer 배치를 자동화하는 Step은 아직 없다.

---

## 결론

- 현재 프로젝트에는 캐릭터의 "모습"을 화면에 그리는 코드/오브젝트가 전혀 없으며, `CharacterManager`(스탯 전용)와 `LaunchPoint`(좌표 기준점 전용)는 각자의 역할에 충실할 뿐 시각 표현과는 무관하다는 점이 이번 조사로 명확해졌다.
- 사용자와 확정한 구현 방향(범위: 무기 조준 회전 포함 / Body는 반전만 / Head는 반전 + 약한 감쇠 회전 / Weapon은 반전 + 강한 회전)은 그대로 plan.md의 목표로 이어갈 수 있다.
- 다만 좌우 반전과 자식 회전 각도 부호 충돌 문제는 아직 해결 방식이 정해지지 않은 열린 이슈이며, plan.md 단계에서 dev 에이전트와 함께 구체적인 기술 접근(예: `flipX` 기반 처리 vs `localScale` 반전 후 각도 재매핑 등)을 확정해야 한다.
- 구조적으로는 `LaunchPoint` 위치에 `Character` GameObject를 두고 그 자식으로 Body/Head/Weapon SpriteRenderer 3개를 배치한 뒤, `BallLauncher.LaunchDirection`을 구독하는 신규 스크립트(가칭 `CharacterAimController.cs` 또는 `WeaponAimController.cs`)가 회전/반전을 계산하는 방안이 제안되었으나, 이는 확정된 설계가 아니라 plan.md 단계에서 구체화할 방향성 수준이다. `CharacterManager.cs`(HP/XP 로직)는 이번 작업과 무관하므로 그대로 유지하고, `SceneSetupEditor.cs`에 캐릭터 배치용 Step을 신규로 추가하는 기존 자동화 패턴 재사용 여부도 plan.md에서 함께 결정한다.
- 무기 스프라이트의 Pivot을 그립 위치(대략 0.36, 0.43)로 재설정하는 작업은 이번 구현의 전제 조건으로 필요할 가능성이 높으며, 정확한 픽셀 좌표 확정과 실제 적용 방법은 plan.md 단계에서 다룬다.
