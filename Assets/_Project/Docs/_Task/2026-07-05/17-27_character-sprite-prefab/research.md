# Research — 캐릭터 스프라이트 프리팹화 + 조준 방향 연동 회전

캐릭터 스프라이트(몸통/머리/무기 분리본)를 프리팹으로 만들어 씬에 배치하고, 조준 방향에 따라 무기(및 보조적으로 머리)가 회전하도록 구현하기 위한 현재 상태 조사 문서다. 아직 어떤 프리팹도 만들어진 적이 없고 캐릭터를 표현하는 시각적 GameObject 자체가 씬에 존재하지 않는다는 것, 그리고 조준 방향 데이터가 이미 `BallLauncher`/`InputHandler`/`TrajectoryPreview` 사이에서 어떻게 흐르고 있는지를 코드·씬 파일·스프라이트 임포트 설정을 직접 열어 확인했다. 구체적 구현 방법(스크립트 구조, 프리팹 계층)은 다루지 않으며, plan.md 작성에 필요한 사실관계만 정리한다.

## 현재 상태

- `Assets/_Project/Sprites/Character/` 폴더에는 스프라이트 4장이 있으며, 각 `.meta` 파일을 직접 읽어 실제 픽셀 크기를 확인했다.
  - `Character_Main.png`(몸통+머리+무기 합성 완성본): sprite rect `x=23, y=2, width=141, height=178`(라인 117-122).
  - `Character_Main_body.png`(몸통만): rect `width=48, height=48`(라인 117-122) — 다른 파츠 대비 매우 작아, 실제 몸통 그림이 캔버스 여백 없이 정사각형으로 꽉 차게 크롭되어 있을 가능성이 있다. 프리팹 조립 시 파츠 간 상대 크기(스케일)를 그대로 픽셀 비율로 맞추면 안 되고 조립 후 육안 확인이 필요하다.
  - `Character_Main_head.png`(머리만): rect `width=131, height=92`.
  - `Character_main_weapon.png`(무기만): rect `width=59, height=116`, 세로로 긴 형태(갈고리/지팡이 형상과 일치).
  - 4개 `.meta` 전부 `spriteMode: 2`(Multiple), 전역 `spritePivot: {x: 0.5, y: 0.5}`(Center), 개별 스프라이트 엔트리의 `alignment: 0`, `pivot: {x: 0, y: 0}`, `spritePixelsToUnits: 100`, `spriteBorder`는 전부 0으로 동일하다. 즉 4장 모두 커스텀 피벗/보더 조정이 전혀 없는 임포트 기본값 상태이며, 특히 무기(`Character_main_weapon.png`)는 회전축이 캐릭터의 손/어깨 위치가 아니라 스프라이트 중앙(0.5, 0.5)으로 잡혀 있어, 이대로 `transform.rotation`만 돌리면 무기가 손이 아니라 무기 그림 한가운데를 축으로 빙글 도는 부자연스러운 결과가 나온다.
- `Assets/_Project/Prefabs/` 하위에는 `Ball/`, `Monster/`, `UI/` 폴더만 존재하고(`Glob` 결과 확인) `Character/` 폴더 자체가 없다. 4개 캐릭터 스프라이트는 어떤 프리팹으로도 조립된 적이 없다.
- `Assets/Scenes/SampleScene.unity`를 직접 검색한 결과, `Character`라는 이름의 시각적 GameObject(SpriteRenderer 포함)는 존재하지 않는다. `Character` 문자열이 들어간 오브젝트는 `CharacterHP`(라인 514, `CharacterHpBar` 부착 — HUD용), `CharacterXP`(라인 3532, `CharacterXpBar` 부착 — HUD용), `CharacterManager`(라인 3144, 순수 로직) 3개뿐이며 전부 UI/로직용이지 캐릭터를 화면에 그리는 오브젝트가 아니다.
- 같은 씬 파일에서 `LaunchPoint` GameObject(라인 2510-2540)를 확인했다. `m_Component`가 `Transform` 하나뿐이고(`SpriteRenderer` 없음), `m_Children: []`(자식 없음), `m_Father: {fileID: 19688685}`로 `BallLauncher`의 자식임을 확인했다. 현재 로컬 포지션은 `(0, -5.610236, 0)`(라인 2535)으로, `SceneSetupEditor.cs`가 최초 생성 시 부여하는 `(0, -8, 0)`(아래 항목 참고)과 다른데, 이는 `WallFitter.Apply()`가 `OnValidate()`/`Start()` 시점에 화면비에 맞춰 Y좌표를 동적으로 재계산해 덮어쓰기 때문으로 보인다(런타임/에디터 양쪽에서 위치가 바뀔 수 있는 지점이라는 뜻).

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/Core/CharacterManager.cs` — `Singleton<CharacterManager>`(라인 4)를 상속하는 순수 로직 클래스. `_maxHp`, `_xpPerLevel`, `_currentHp/_currentXp/_currentLevel` 필드(라인 6-11)와 `TakeDamage()`(라인 48-53), `AddXp()`(라인 55-67)만 있고, `Transform`/`SpriteRenderer` 등 시각 표현과 관련된 필드나 참조가 전혀 없다. `UIRules.md` 섹션 10에서도 이 클래스의 책임 범위를 "HP/XP/레벨 시스템"으로만 규정하고 있어(라인 163-187), 캐릭터의 시각적 회전/스프라이트는 이 클래스의 책임 범위 밖이라는 것이 문서상으로도 확인된다.
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
  - `Step7_PlaceManagers()`(라인 479-489)가 `PlaceManager<CharacterManager>("CharacterManager")`(라인 487)를 호출해 빈 GameObject로 배치한다. `PlaceManager<T>()`(라인 491-509)는 `AddComponent<T>()`만 호출할 뿐 `SpriteRenderer`를 추가하는 코드가 없다.
  - `Step8_ConnectBallLauncherRefs()`(라인 515-567)에서 `BallLauncher`의 자식으로 `LaunchPoint`를 로컬 좌표 `(0, -8, 0)`에 생성하고(라인 556-561) `BallLauncher._launchPoint`에 연결한다(라인 562). 이 시점에는 별도 시각 오브젝트를 만들지 않는다.
  - `Step6_SetupWallFitter()`(라인 419-462)에서 `WallFitter._launchPoint`에 방금 만든 `LaunchPoint` Transform을 연결하고(라인 443, 452) `_nativeLaunchPointY = -6.0f`(라인 457) 등 native 좌표값을 넣어준다.
- `Assets/_Project/Scripts/Core/WallFitter.cs` — `[ExecuteAlways]`(라인 3)로 에디터에서도 동작. `Apply()`(라인 33-54)가 카메라 화면비 기반으로 `uniformScale`을 계산해(라인 37-47) `_wallLeft/_wallRight/_wallTop/_ground`와 함께 `_launchPoint`의 Y좌표도 `SetY(_launchPoint, _nativeLaunchPointY * scaleY)`(라인 53)로 매번 재조정한다. 즉 `LaunchPoint`는 화면비에 따라 위치가 계속 바뀌는 동적 기준점이며, 만약 캐릭터를 `LaunchPoint`의 자식으로 붙인다면 별도 좌표 계산 없이 캐릭터도 같은 리프레임 로직의 혜택을 받지만, 반대로 `LaunchPoint` 자체의 역할("볼 발사/귀환 좌표")과 "캐릭터가 서 있는 시각적 위치"라는 두 책임이 한 Transform에 뒤섞이게 된다.
- `Assets/_Project/Scripts/Ball/BallLauncher.cs`
  - `LaunchPoint`(라인 24, `Transform` 프로퍼티)와 `LaunchDirection`(라인 25, `Vector2` 프로퍼티, 내부 필드 `_launchDirection`은 라인 16에서 기본값 `Vector2.up`)가 외부에 공개된 실시간 조준 방향 데이터다.
  - `HandleDrag(Vector2 direction)`(라인 63-66)이 `InputHandler.OnDrag` 이벤트를 구독해 `_launchDirection`을 매 프레임 갱신한다. 터치를 떼도 `_launchDirection`은 마지막 값을 그대로 유지한다(별도로 리셋하는 코드 없음).
- `Assets/_Project/Scripts/Core/InputHandler.cs` — `ComputeAimDirection(Vector2 screenPos)`(라인 22-28)가 `BallLauncher.Instance.LaunchPoint.position`을 기준점으로 삼아 터치 스크린 좌표까지의 정규화된 방향 벡터를 계산하고, `Update()`(라인 30-71)에서 터치/마우스 입력이 있을 때만 `OnDrag`를 발행한다(라인 63). 터치가 없을 때는 이벤트 자체가 발행되지 않으므로, 무기 회전 로직이 `OnDrag` 이벤트만 구독하면 터치가 끝난 뒤에는 갱신이 멈춘다는 점에 유의해야 한다(반면 `BallLauncher.LaunchDirection` 프로퍼티는 마지막 값을 계속 들고 있으므로 폴링 방식이면 문제없다).
- `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs` — `Update()`(라인 51-54)에서 매 프레임 `BallLauncher.Instance.LaunchDirection`을 직접 읽어(이벤트 구독이 아니라 폴링) `UpdateTrajectory()`를 호출한다(라인 53). 주석(라인 48-50)에 "터치 여부와 무관하게 매 프레임 궤적을 재계산한다... 별도 상태 분기가 필요 없다"고 명시되어 있어, 무기 회전도 동일하게 `BallLauncher.Instance.LaunchDirection`을 매 프레임 폴링하는 방식이 `OnDrag` 이벤트 구독보다 기존 코드 패턴과 일관되고 안전하다.
- `Assets/_Project/Docs/UIRules.md` 섹션 9(라인 137-160)·10(라인 163-187)·11(라인 190-219) — 몬스터 HP바, 캐릭터 HP/XP, 궤적 프리뷰 규칙만 있고 캐릭터 본체의 스프라이트/회전에 대한 규칙은 아직 없다. 섹션 12(리소스 참고 사항, 라인 223-227)에도 캐릭터 스프라이트 관련 언급이 없다. 즉 이번 작업과 문서상 충돌하는 기존 규칙은 없으며, plan.md 단계에서 이 문서에 캐릭터 시각 규칙 섹션을 새로 추가하는 것을 고려할 수 있다(다만 이는 plan.md에서 다룰 사항).

## 문제점 / 구현 대상 파악

- **씬에 캐릭터 시각 오브젝트가 아예 없다는 사실 확정**: `SampleScene.unity`와 `SceneSetupEditor.cs` 양쪽을 직접 확인해, 캐릭터를 그리는 `SpriteRenderer`가 붙은 GameObject가 현재 프로젝트 어디에도 존재하지 않는다는 것을 재차 확정했다. `CharacterManager`가 배치되어 있지만 로직 전용이라 이번 작업과 직접 연결되지 않는다. 새 프리팹 3파츠(body/head/weapon) 조립 + 씬 배치가 이번 작업의 핵심이며, 완전히 새로 만드는 것이지 기존 오브젝트를 수정하는 것이 아니다.
- **배치 위치 후보 두 가지와 트레이드오프**: (a) 캐릭터를 `LaunchPoint`의 자식으로 붙이면 `WallFitter.Apply()`가 이미 처리하는 화면비 대응 로직을 그대로 물려받아 별도 좌표 계산이 필요 없지만, `LaunchPoint`가 "볼 발사/귀환 기준점"과 "캐릭터가 서 있는 시각적 지점"이라는 두 역할을 겸하게 되어 책임이 섞인다. (b) 캐릭터를 별도 Transform으로 씬에 두고 `LaunchPoint`는 계속 볼 발사 좌표 전용으로 유지하면 역할은 분리되지만, `WallFitter`가 캐릭터 위치는 자동으로 리프레임해주지 않으므로 캐릭터 전용 리프레임 로직(또는 `WallFitter`에 캐릭터 참조 추가)이 별도로 필요해진다. 레퍼런스 이미지 분석상 캐릭터 위치가 `LaunchPoint`(원본 게임에서 볼이 발사되는 지점)와 거의 일치하는 것으로 보이므로, 어느 쪽을 택하든 두 지점의 좌표값 자체는 사실상 같아야 한다 — plan.md에서 결정할 사항이다.
- **무기 회전 피벗 문제**: `Character_main_weapon.png`의 `.meta`가 `alignment: 0`, `pivot: {0, 0}`(스프라이트 엔트리 기준, 사실상 중앙 기준 기본값)로 커스텀되지 않은 상태이므로, 스프라이트 자체의 피벗을 손/어깨 위치로 옮기거나(임포트 설정 변경), 무기를 감싸는 빈 부모 GameObject를 만들어 그 부모의 로컬 원점을 손 위치에 맞추고 무기 스프라이트는 그 자식으로 오프셋을 주는 방식(회전은 부모만 담당) 중 하나가 필요하다. 후자가 스프라이트 파일 자체를 건드리지 않아도 되므로 기존 프로젝트의 "임포트 설정 대신 계층 구조로 해결" 접근에 더 가깝다(예: `TrajectoryPreview.cs`가 원 오브젝트를 자식 `LineRenderer`들로 구성하는 것과 유사한 패턴).
- **좌우 반전(flip) 시 회전각 보정**: 몸통/머리를 `SpriteRenderer.flipX`로 좌우 반전할 경우, 같은 조준 방향이라도 반전 여부에 따라 무기 회전각의 부호(또는 로컬 좌표계 자체)가 달라지므로, 무기 각도 계산 시 `_launchDirection`의 x 부호에 따른 반전 로직과 반드시 함께 다뤄져야 한다. 현재 코드베이스에는 flip과 회전을 동시에 다루는 기존 사례가 없어(몬스터/볼 모두 좌우 반전 로직 없음) 참고할 기존 패턴이 없다는 점도 확인했다.
- **머리의 보조 회전(무기 각도의 일부만 반영)**: 이런 "부모 각도의 일부 비율만 자식에 반영" 패턴도 현재 코드베이스에 선례가 없다. `DrawCircle()`의 `rotationOffsetDeg` 파라미터(`TrajectoryPreview.cs` 라인 137-153)처럼 각도를 계산해 넘기는 유틸리티 함수 패턴은 있지만 이는 정점 좌표 계산용이라 그대로 재사용할 수는 없고, 새 계산 로직이 필요하다.
- **어느 클래스에 붙일지**: 기존 아키텍처는 `Ball`/`BallLauncher`처럼 발사 로직(모델)과 `TrajectoryPreview`처럼 순수 시각 표현(뷰)을 별도 클래스로 분리하는 패턴을 일관되게 쓰고 있다(`TrajectoryPreview`가 `BallLauncher.LaunchDirection`을 매 프레임 읽기만 하고 쓰지 않는 것도 이 패턴의 예). `CharacterManager`는 HP/XP 로직 전용으로 이미 역할이 명확히 정의되어 있으므로(`UIRules.md` 섹션 10), 캐릭터의 시각적 회전/반전 로직은 `CharacterManager`에 섞지 않고 `TrajectoryPreview`와 유사한 위상의 별도 뷰 전용 컴포넌트로 분리하는 것이 기존 패턴과 일관될 것으로 판단된다(구체적 클래스 설계는 plan.md에서 다룸).
- **레퍼런스 이미지 재확인**: 사용자와의 논의에서 언급된 두 스크린샷(`KakaoTalk_20260701_190324151.jpg`, `_01.jpg`)은 이번 문서 작성 과정에서 직접 열람하지 않았다(오케스트레이터가 사전 논의 단계에서 이미 분석 완료한 내용을 그대로 인용). 필요 시 plan.md 단계에서 재확인 가능하다.

## 결론

캐릭터를 표현하는 시각적 GameObject는 씬/코드 어디에도 아직 존재하지 않으며(`CharacterManager`는 순수 로직, `CharacterHP`/`CharacterXP`는 HUD), 4개 스프라이트(`Character_Main_body/head/weapon.png` + 참고용 합성본 `Character_Main.png`)도 어떤 프리팹으로도 조립된 적이 없는 임포트 기본값 상태임을 확인했다. 조준 방향 데이터(`BallLauncher.LaunchDirection`)는 이미 `TrajectoryPreview`가 매 프레임 폴링하는 방식으로 안정적으로 소비되고 있어, 무기 회전도 동일한 폴링 패턴을 따르는 것이 기존 아키텍처와 일관된다. plan.md에서 확정해야 할 주요 결정 사항은 (1) 캐릭터를 `LaunchPoint`의 자식으로 둘지 별도 Transform으로 둘지, (2) 무기 회전 피벗을 스프라이트 임포트 설정 수정으로 옮길지 빈 부모 GameObject 계층으로 해결할지, (3) 좌우 반전과 회전각 보정을 어느 컴포넌트에서 계산할지, (4) 새 뷰 전용 스크립트를 어디에(어떤 이름으로) 추가할지 네 가지이며, 이 문서는 그 판단에 필요한 사실관계(파일 크기, 기존 코드 구조, 문서 규칙 유무)만 정리했다.
