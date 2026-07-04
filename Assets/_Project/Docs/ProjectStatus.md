# ProjectStatus.md

이 문서는 현재 프로젝트의 상태를 기록합니다. 작업 완료마다 업데이트합니다.

---

## 현재 상태 (2026-07-04 기준)

**단계**: LaunchPoint 궤도화 재설계 완료 — 로컬 씬 반영 및 실제 플레이 테스트 대기 중

**완료된 작업**
- [x] 프로젝트 생성 (Unity 6000.3.10f1, Universal 2D URP, Android)
- [x] 폴더 구조 생성 (Scripts/Core, Ball, Monster, Skill, UI, Data, Util)
- [x] 에이전트 시스템 구축 (dev / qa / design / docs)
- [x] 문서 시스템 구축 (CLAUDE.md, AGENTS.md, DevRules.md, TaskRules.md 등)
- [x] 아키텍처 설계 확정
- [x] Core 시스템 task 문서 작성 (research.md + plan.md)
- [x] Inspector 연결 에디터 스크립트 자동화 완성 (LaunchPoint, SkillCard, HUD/Result/SkillSelection 패널, WaveData 스폰 데이터)
- [x] 런타임 버그 수정: InputHandler (New Input System), GameManager 자동 시작, 카메라 orthographic size 조정
- [x] 볼 발사 메커닉 재설계 (`2026-07-01/21-15_ball-launch-mechanics`): 터치 즉시 조준(`InputHandler.OnAimBegin`), 2단계 궤적 프리뷰(`TrajectoryPreview.cs`), 화면 하단 귀환 후 자동 재발사 사이클, 노말볼 5개+특수볼 최대 4종 로스터 모델(`BallLauncher`/`Ball`) 도입, 몬스터 하강을 볼 사이클에서 분리해 시간 연속 하강으로 재설계(`WaveManager`/`MonsterBase`), 냉동/슬로우 초 단위 전환. QA 검토로 발견된 Critical 2건(벽 반사 소진 시 로스터 볼 영구 이탈, `BallData.asset._maxBounces` 데이터 오류) + Major 1건(게임 종료 후 재발사 지속) 수정 완료, 이후 "로스터 볼은 벽 반사 횟수 무관하게 항상 순수 반사만 하고 Ground 충돌에서만 귀환"으로 최종 정정. PR #6으로 main 머지 완료
- [x] `UISetupEditor` 버그 수정: `CharacterHpBar`/`CharacterXpBar`의 `_slider`/`_levelText` 참조 연결 누락으로 인한 몬스터 처치 시 `NullReferenceException` 수정, PR #7로 main 머지 완료
- [x] WaveData → WaveTableData 리팩토링: `Assets/_Project/Data/`에 asset이 과다하다는 지적에 따라 `WaveData.cs` + `WaveData_Wave1~20.asset`(20개 개별 asset) 구조를 `WaveTableData.cs`(`WaveEntry` 리스트를 담는 단일 ScriptableObject) 구조로 통합, `WaveManager`/`MonsterSetupEditor`/`SceneSetupEditor` 함께 수정. task 문서(research.md/plan.md) 없이 예외적으로 바로 구현 진행, main에 직접 커밋(`9c188a8`). 이후 사용자가 로컬 Unity에서 `PurpleCow/Setup/Monster System Setup` → `PurpleCow/Setup/Scene Setup`을 실행해 `WaveTableData.asset` 생성 및 `SampleScene.unity`의 `WaveManager._waveTable` 참조 재연결 완료, 커밋(`ceeb9e2`)/푸시 완료. 오케스트레이터가 직접 검증: `WaveTableData.asset`의 웨이브 1~20 스폰 데이터가 의도한 진행과 정확히 일치, 씬의 구 `_waveDatas` 필드 완전 제거 및 `_waveTable` 단일 참조로 정상 교체, `Assets/_Project/Data/` asset 개수 35개 → 16개로 감소 확인
- [x] 볼 천장 이탈 버그 수정 (`2026-07-03/12-48_ball-ceiling-wall-fix`): 실 플레이 테스트 중 볼이 맵 외곽에서 튕기지 않고 맵 밖(천장)으로 나가버리는 버그 발견, research.md로 원인(`SceneSetupEditor.Step5_PlaceWallsAndGround()`에 상단 벽 콜라이더 생성 코드 자체가 없어 위쪽만 완전히 뚫려 있음)을 특정하고 plan.md 사용자 승인 후 dev 에이전트가 `Wall_Top` 콜라이더 생성 1줄 추가(커밋 `345ae29`). 사용자가 로컬 Unity에서 `PurpleCow/Setup/Scene Setup` 메뉴를 재실행해 `SampleScene.unity`에 `Wall_Top` 반영 완료, 실제 플레이 테스트로 천장 반사 정상 동작 검증 완료
- [x] 배경/해상도 대응 (`_Task/2026-07-03/12-30_background-resolution-fix`): 다양한 Android 기기 종횡비 대응을 위해 배경 스케일 방식이 Cover → Contain → Stretch 순으로 시행착오를 거쳐 Stretch로 최종 확정됐고, 카메라 시야를 기기별로 동적 확장하는 `CameraFitter`는 도입 후 "Wall이 화면에서 차지하는 비율이 orthographic size와 무관하게 항상 일정하다"는 사실이 수학적으로 밝혀지며 불필요해져 삭제됐으며, 대신 `WallFitter`를 도입해 벽/Ground/LaunchPoint를 배경 격자 그림에 비례해 연동시키고 `_zoomFactor` 공통 확대 배율과 Inspector 실시간 반영(`[ExecuteAlways]`/`OnValidate`) 기능을 추가한 뒤 여러 차례 실기기 테스트로 벽 기준값을 최종 확정 완료
- [x] 캐릭터 비주얼 구현 (`2026-07-04/01-38_character-visual-implementation`): 사용자 요청("캐릭터 에셋을 사용해서 캐릭터를 구현할 것")에 따라 그동안 씬에 전혀 존재하지 않던 캐릭터 시각 표현(Body/Head/Weapon 스프라이트)을 신규 구현, research.md/plan.md 작성 후 사용자 승인을 거쳐 진행. design 에이전트가 `Character_main_weapon.png.meta`(무기가 `spriteMode: 2`(Multiple)라 최상위 `spritePivot`이 아닌 `spriteSheet.sprites[0]`의 `alignment`/`pivot`이 실제 적용됨을 확인)의 Pivot을 중앙(`alignment: 0`, Center)에서 그립(손잡이) 위치(`alignment: 9` Custom, 0.39, 0.43)로 재설정. dev 에이전트가 신규 `CharacterAimController.cs`(`Assets/_Project/Scripts/Character/`)를 작성해 `BallLauncher.LaunchDirection`을 매 프레임 읽어 Weapon은 조준 방향을 거의 그대로 따라가는 회전, Head는 감쇠된 약한 회전, Body는 회전 없이 flipX만 적용하도록 구현(좌우 반전은 `localScale` 반전 대신 `SpriteRenderer.flipX`만 사용해 반전-회전 부호 충돌을 원천 차단), `SceneSetupEditor.cs`에 `Step11_SetupCharacterVisual()` 신규 추가해 `Character` 오브젝트를 `LaunchPoint`와 동일 위치(BallLauncher의 형제 오브젝트)에 생성하고 Body/Head/Weapon 자식 3개를 자동 배치. qa 에이전트 검토에서 Major 2건(Body/Head/Weapon이 모두 Character 원점(0,0,0)에 겹쳐 배치되어 캐릭터 형태로 보이지 않는 문제, design 에이전트가 작성한 agent-memory 파일에 툴 호출 잔재 텍스트 혼입) 발견 → 오케스트레이터가 원본 합성 이미지(`Character_Main.png`)를 픽셀 템플릿 매칭으로 직접 분석해 Head/Body 상대 위치(Head: 0.51,-0.23 / Body: 0.42,-0.75, Weapon 그립 기준)를 역산해 수정 지시, `SpriteRenderer.flipX`가 Transform 위치에는 영향을 주지 않아 좌우 반전 시 Head/Body 위치가 미러링되지 않던 버그도 함께 발견해 수정 완료(커밋 `794713d`). `CharacterManager.cs`(HP/XP 로직), `BallLauncher.cs`/`Ball.cs`(볼 발사/귀환 로직)는 이번 작업에서 전혀 수정하지 않음 — 순수 시각 레이어 추가. 미완료: 원격 환경에 Unity 에디터가 없어 사용자가 로컬에서 `PurpleCow/Setup/Scene Setup` 메뉴를 재실행해 씬에 `Character` 오브젝트를 반영하고, 실제 플레이 테스트로 조준 반응(회전/반전)이 자연스러운지 확인 필요. **참고**: main에 병합된 배경/해상도 대응 작업이 `WallFitter`로 `LaunchPoint`를 화면 비율에 맞춰 런타임에 동적으로 재배치하는데, 캐릭터 비주얼 구현 당시에는 이 사실을 모른 채 `Character`를 `LaunchPoint`와 동일한 위치를 한 번만 복사하는 형제 오브젝트로 만들어서, 최신 main과 병합한 후 `WallFitter`가 `LaunchPoint`를 움직이면 `Character`가 따라가지 못하고 위치가 어긋나는 문제가 있음이 드러남 — 이 문제는 이후 "LaunchPoint 궤도화 재설계" task로 해결됨(아래 항목 참고)
- [x] LaunchPoint 궤도화 재설계 (`2026-07-04/09-41_launchpoint-character-orbit`): 캐릭터 비주얼 구현 직후 main 병합 과정에서 드러난 "WallFitter가 런타임에 LaunchPoint를 화면비에 맞춰 재배치하지만 Character는 씬 설정 시점에 위치를 한 번만 복사해 따라가지 못하는" 문제를 계기로, 발사/귀환 지점의 역할 구조 자체를 재설계함. research.md로 `LaunchPoint`(`BallLauncher._launchPoint`)가 겸하던 4가지 역할(발사 스폰 위치/귀환 목적지/궤적 프리뷰 원점/WallFitter 재배치 대상)을 확인하고, plan.md 단계에서 사용자와 논의를 거쳐 `Character`를 `WallFitter`가 화면비에 맞춰 한 번 재배치하는 고정 기준점으로 삼고 발사 시작점(무기 끝)은 `Character 위치 + LaunchDirection × 무기길이(0.6612)`로 매 프레임 계산되는 값, 귀환 목적지는 `Character`의 `Body` 위치로 분리하기로 확정(Transform 오브젝트를 매 프레임 갱신하는 방식이 아닌 계산 프로퍼티 방식 채택 — 이번 세션에서 이미 두 차례 겪은 "복사된 값이 원본과 어긋나는" 버그 유형을 재도입하지 않기 위함). dev 에이전트가 `CharacterAimController`를 `Singleton<CharacterAimController>`로 전환하고 `_weaponLength`/`BodyPosition`/`WeaponLength` 추가, `BallLauncher`의 고정 `_launchPoint`(Transform)를 삭제하고 `LaunchOrigin`/`ReturnPoint` 계산 프로퍼티로 대체, `Ball.cs`/`TrajectoryPreview.cs`의 참조를 각각 `ReturnPoint`/`LaunchOrigin`으로 변경, `WallFitter`의 재배치 대상을 `LaunchPoint`에서 `Character`로 변경(`_launchPoint`→`_character`, `_nativeLaunchPointY`→`_nativeCharacterY` 리네이밍), `SceneSetupEditor.cs`에서 무의미해진 `LaunchPoint` 생성/연결 코드만 삭제(그 외 로직은 유지), 신규 `CharacterLaunchOrbitSetupEditor.cs`(`PurpleCow/Setup/Character LaunchPoint Orbit Setup` 메뉴)로 `Character` 초기 위치 지정과 `WallFitter`↔`Character` 배선을 분리 담당하도록 구현. 오케스트레이터가 역할이 바뀐 뒤 남아있던 옛 이름(변수 `toLaunchPoint`→`toReturnPoint`, 메서드 `ReturnToLaunchPoint()`→`ReturnToCharacter()`, 관련 주석 등)을 로직 변경 없이 정리. qa 에이전트 검토에서 `LaunchOrigin` 계산식과 `CharacterAimController`의 무기 회전 공식이 회전행렬로 봤을 때 수학적으로 정확히 일치함을 검증하고, `Singleton<T>` 전환도 기존 `Awake()` 부재로 충돌 없음을 확인했으며, Major 1건(`CharacterLaunchOrbitSetupEditor`가 `Character`를 못 찾은 경우에도 `WallFitter._character`를 null로 덮어쓸 수 있던 문제) 발견 → 즉시 수정 완료. `CharacterManager.cs`(HP/XP 로직), 볼의 물리/충돌/데미지 판정 로직은 전혀 수정하지 않음. 미완료: 원격 환경에 Unity 에디터가 없어 사용자가 로컬에서 (1) `PurpleCow/Setup/Scene Setup` 재실행, (2) 씬에 남아있는 구 `LaunchPoint` GameObject 수동 정리(필요 시), (3) 신규 `PurpleCow/Setup/Character LaunchPoint Orbit Setup` 메뉴 실행이 필요하며, `WallFitter` 필드 리네이밍으로 로컬에서 튜닝했던 값이 초기화될 수 있어 재설정이 필요할 수 있음. 실제 조준 시 무기 끝에서 볼이 발사되는 것처럼 보이는지, 귀환 시 캐릭터 몸통으로 들어오는지는 사용자의 로컬 실제 플레이 테스트로 검증 필요
- [x] 볼 궤적 조준 개선 (`_Task/2026-07-03/15-41_ball-trajectory-aim-fix`): 궤적 프리뷰 상시 표시 전환, 스크린→월드 변환을 통한 조준 정확도 보정, 궤적 프리뷰 색상/크기 원본 레퍼런스 맞춤 조정, 상대 드래그 → 절대 조준 모델 전환, 터치 시작 폴링 누락 버그 수정까지 5개 이슈를 모두 구현 완료했고, 사용자가 유니티 에디터에서 직접 플레이 테스트를 진행해 조작감에 불편함이 없고 매우 좋다고 확인함

**진행 중**
없음

**다음 작업 순서**
1. 사용자가 로컬 Unity에서 (1) `PurpleCow/Setup/Scene Setup` 재실행, (2) 씬에 남아있는 구 `LaunchPoint` GameObject 수동 정리(필요 시), (3) 신규 `PurpleCow/Setup/Character LaunchPoint Orbit Setup` 메뉴 실행하여 LaunchPoint 궤도화 재설계를 씬에 반영
2. 위 반영 후 실제 플레이 테스트로 무기 끝에서의 발사, Body로의 귀환, 화면비 대응(WallFitter의 Character 재배치)이 모두 자연스러운지 확인
3. 실제 플레이 테스트를 진행하면서 발견되는 문제를 하나씩 수정해나가는 단계 (별도 큰 항목 사전 나열보다 테스트 중 발견되는 버그/밸런스/UI 이슈를 그때그때 task로 정리해 처리)

## 주요 기술 결정

| 항목 | 결정 | 이유 |
|------|------|------|
| 이벤트 시스템 | C# event | 단순성, 프로젝트 규모에 적합 |
| 매니저 패턴 | Generic Singleton | 단일 씬, DontDestroyOnLoad 불필요 |
| 데이터 | ScriptableObject (read-only) | Unity 표준, 인스펙터 편집 용이 |
| 오브젝트 풀 | Generic ObjectPool<T> | Ball/Monster/데미지텍스트 재사용 |
| 입력 | InputHandler (C# event 발행) | BallLauncher가 구독 |

## 리소스 현황

- `Assets/_Project/Resource/` 폴더에 과제 제공 리소스 배치 완료
- 실제 사용 시 임포트 설정 필요
