# ProjectStatus.md

이 문서는 현재 프로젝트의 상태를 기록합니다. 작업 완료마다 업데이트합니다.

---

## 현재 상태 (2026-07-06 기준)

**단계**: 핵심 구현과 Android 실제 기기 20웨이브 검증 완료. 선택 Polish 1·6번(치명타 텍스트 색상, 바닥 도달 진동→돌진→소멸 연출)은 Android 실기기 검증까지 완료해 `TODO.md`에서 제거되었고, 5번(아이스볼 및 몬스터 이동 겹침 방지)도 추가로 발견된 몬스터 겹침/관통 버그(`Physics2D.autoSyncTransforms` 비활성화가 원인)를 수정하고 실기기 검증까지 완료해 `TODO.md`에서 제거되었다. 선택 Polish 7번은 구현 및 실기기 검증까지 완료. 고스트볼이 벽/바닥을 뚫고 나가던 미작동 버그(`TODO.md` 9번)도 원인 규명 및 두 차례 수정, 실기기 테스트 검증까지 완료해 `TODO.md`에서 제거되었다. 레이저볼 가로 행/지속 대미지(DoT) 대미지 텍스트 미표시 버그(`TODO.md` 8·10번)도 원인 규명 및 수정, 실기기 테스트 검증까지 완료해 `TODO.md`에서 제거되었다.

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
- [x] 볼 궤적 조준 개선 (`_Task/2026-07-03/15-41_ball-trajectory-aim-fix`): 궤적 프리뷰 상시 표시 전환, 스크린→월드 변환을 통한 조준 정확도 보정, 궤적 프리뷰 색상/크기 원본 레퍼런스 맞춤 조정, 상대 드래그 → 절대 조준 모델 전환, 터치 시작 폴링 누락 버그 수정까지 5개 이슈를 모두 구현 완료했고, 사용자가 유니티 에디터에서 직접 플레이 테스트를 진행해 조작감에 불편함이 없고 매우 좋다고 확인함
- [x] 배경 격자 정사각형 보정 (`_Task/2026-07-04/16-40_background-square-grid-fix`): 몬스터 시스템 개편(4종 랜덤 웨이브, 종류별 고정 블록 크기 기반 정사각형 그리드 점유 체크)의 선행 작업으로, 배경 텍스처(`Background_1_Stage.png`) 격자가 140×85px(비정사각형, 약 1.65:1)로 그려져 몬스터/블록 스프라이트 및 원본 게임 실제 격자(정사각형)와 어긋나던 문제를 `BackgroundFitter.cs`/`WallFitter.cs`의 스케일 계산식을 "텍스처 고유 비율 보정(`_cellAspectCorrection ≈ 1.647`) + 격자 영역 기준 균일 Cover 배율" 2단계 공식으로 교체해 해결(신규 필드 주입은 기존 `SceneSetupEditor.cs`를 건드리지 않고 별도 `BackgroundGridFitSetupEditor.cs`로 분리, 리소스 PNG는 수정하지 않음). 사용자가 로컬에서 여러 실기기(Galaxy Note 10 등)로 검증해 격자가 정사각형으로 정상 렌더링됨을 확인했고, 새 계산식이 필요로 하는 배율이 기존보다 커진 것을 반영해 `_zoomFactor` 기본값을 1.3 → 0.5(에디터 미리보기는 0.6)로 최종 조정, 커밋 완료
- [x] PDF 스펙 대비 문서 재감사: `MonsterRules.md`/`UIRules.md`를 공식 요구사항 PDF와 재대조. 첫 시도(커밋 `d5a3b06`)는 "문서에 구현 상태만 기록"하는 방향이었는데, 실제로 필요했던 건 "문서에 적힌 규칙 자체가 PDF 목표와 모순되는지" 감사였음이 드러나 사용자가 되돌림(`16ec529`). 이후 올바른 기준(규칙-목표 모순 여부만 검사)으로 재감사했으나 확실한 모순은 발견되지 않아 문서 수정 없이 결과만 보고, PR #12로 main 머지 완료
- [x] PrismPanel(융합 시스템 잔재) 제거: `UISetupEditor.cs`의 Canvas_Panel 생성 목록에서 이름만 있고 실제 로직이 전혀 없던 빈 스텁 패널 `PrismPanel`을 발견, PDF 스펙의 "구현 제외 항목"인 융합 시스템 관련 잔재로 판단해 삭제 확정. `panelNames` 배열에서 `"PrismPanel"` 제거, `SampleScene.unity`에 이미 생성돼 있던 빈 GameObject도 YAML에서 직접 제거, `UIRules.md`의 Canvas 계층도에서도 해당 줄 삭제(`LevelUpPanel`/`PausePanel`/`BallLevelUpPanel`은 그대로 유지). PR #12로 main 머지 완료
- [x] 볼 궤적 프리뷰 고리(Ring) 점선화 + 회전 효과 (`_Task/2026-07-05/11-20_trajectory-ring-dash-rotate`): 2차 충돌 지점 레드닷을 감싸는 고리(`_hitRing`)가 완전한 실선이던 것을, 원본 게임 레퍼런스처럼 끊어진 점선 + 조준 여부와 무관하게 항상 시계방향으로 회전하는 효과로 재구현. 궤적선 색상 등 Inspector 조절 가능화는 기존 코드(`_lineColor` 등 6개 `[SerializeField]` 필드)로 이미 완료돼 있음을 확인해 별도 구현은 불필요했음. 구현 과정에서 시행착오를 거쳤음 — (1) 텍스처 반복(타일링) 방식으로 10개 점선을 목표했으나 실제로는 2개로 보이는 문제(원인 미확인, 원격 환경에 Unity가 없어 검증 불가) → (2) `LineRenderer.colorGradient`(alphaKeys 8개) 방식으로 교체해 정확히 4개를 보장했으나, 사용자가 보내준 실제 레퍼런스 이미지(`targetUI/circle.jpg`) 대조 결과 경계가 과도하게 흐려지는 근본적 한계 확인 → (3) 텍스처 타일링 방식으로 재전환하되 목표를 4개로 조정하고, `loop = true` 대신 원을 명시적으로 닫는 정점(`CIRCLE_SEGMENTS + 1`개, explicit close)을 추가하는 방식으로 재구현. 회전 속도는 `[SerializeField] private float _ringRotationSpeed = 90f;`(deg/sec)로 Inspector 노출. **이 최종(3번) 버전이 실제로 정확히 4개의 호로 보이는지는 사용자가 아직 로컬 Unity에서 재확인하지 않은 상태 — "구현 완료, 최종 시각 확인은 사용자 로컬 테스트 대기 중"으로 구분**. PR #12로 main 머지 완료(구현 코드 기준, 시각 확인은 별개)
- [x] 볼-볼 물리 충돌 방지 (`_Task/2026-07-05/16-40_ball-ball-collision-fix`): `Ball`/`Wall`/`Ground`/`Monster`가 전부 Default 레이어(0)에 있고 `Physics2DSettings.asset`의 레이어 충돌 매트릭스가 Default-Default 충돌을 허용해, 여러 볼이 동시에 존재할 때 물리적으로 서로 튕겨나가던 버그를 발견(`Ball.OnCollisionEnter2D`의 태그 분기는 물리 반응 이후 호출되는 콜백이라 코드로는 막을 수 없었음). 전용 "Ball" Physics2D 레이어를 신설(`BallSetupEditor.cs`에 `AddBallLayer()`/`AssignBallPrefabLayer()` 신규 메서드 추가, `PurpleCow/Setup/Ball System Setup` 메뉴 실행 시 자동 처리)하고 `BallLauncher.Awake()`에서 `Physics2D.IgnoreLayerCollision(ballLayer, ballLayer, true)` 1회 호출로 볼-볼 충돌만 전역 비활성화, Wall/Ground/Monster는 Default 레이어 그대로 유지. **사용자가 로컬에서 `PurpleCow/Setup/Ball System Setup` 메뉴를 재실행한 뒤 실제 플레이 테스트로 "볼 발사 정상 동작"과 "볼-볼 물리 충돌 방지(서로 안 튕김)" 둘 다 검증 완료 확인함.** 다만 **아직 main에 병합되지 않고 현재 브랜치(`claude/project-review-bugs-qq65d1`)에만 커밋된 상태 — main 병합 대기 중**
- [x] 볼 조준 방향 Y좌표 하한 제한 (`_Task/2026-07-05/18-30_aim-direction-y-clamp`): 실제 플레이 테스트 중 사용자가 "볼 궤도를 설정할 때 일정 y좌표 밑으로는 설정하지 못하게 하자"고 요청. 처음엔 기준점을 "격자타일 밑변"(`WallFitter`가 기기별로 동적 재계산하는 `Ground` Transform 위치)으로 논의해 research.md까지 작성했으나, `WallFitter._ground`가 private이라 `InputHandler`에서 접근하려면 씬 참조 연결이 추가로 필요하다는 복잡성이 확인됨. 이후 사용자가 방향을 단순화 — 이미 존재하는 몬스터 바닥 도달 게임오버 판정 기준선(`WaveManager._bottomBoundaryY`)을 재사용하기로 확정, `WaveManager`가 이미 싱글톤이라 씬 참조 연결/에디터 스크립트 수정이 전혀 필요 없어짐. `WaveManager.cs`에 `public float BottomBoundaryY => _bottomBoundaryY;` 프로퍼티를 추가하고, `InputHandler.ComputeAimDirection()`에서 터치 위치를 월드 좌표로 변환한 직후 `worldPos.y = Mathf.Max(worldPos.y, WaveManager.Instance.BottomBoundaryY);`로 clamp한 뒤 발사 지점 기준 방향을 계산하도록 수정(이 clamp는 조준 가능한 목표 지점 범위만 제한하며, 발사된 볼이 물리 반사로 기준선 아래까지 내려가는 것 자체를 막는 장치는 아님). `TrajectoryPreview.cs`는 이미 clamp된 `BallLauncher.Instance.LaunchDirection`을 그대로 받아 그리므로 별도 수정 불필요. `GameplayMechanics.md` 섹션 1에도 이 규칙을 문서화. **사용자가 로컬 Unity에서 직접 플레이 테스트하여 정상 동작 확인 완료.** 다만 **아직 main에 병합되지 않고 현재 브랜치에만 커밋된 상태 — main 병합 대기 중**
- [x] 캐릭터 스프라이트 프리팹 + 조준 방향 연동 회전 (`_Task/2026-07-05/17-27_character-sprite-prefab`): plan.md의 초기 설계(`WeaponPivot` 빈 부모 오브젝트 + `flipX` 기반 좌우 반전)는 로컬 실플레이 테스트에서 여러 버그가 발견되어 최종적으로 다른 구조로 귀결되었다. 신규 `Assets/_Project/Scripts/Character/CharacterAimView.cs`, `Assets/_Project/Scripts/Editor/CharacterSetupEditor.cs`(메뉴 `PurpleCow/Setup/Character System Setup`)를 작성했고, 사용자가 로컬 Unity에서 해당 메뉴 실행 + 직접 다회 수정을 거쳐 `Assets/_Project/Prefabs/Character/Character.prefab`을 완성했다. 최종 구조는 `Character`(루트, `CharacterAimView`) → `Body`/`Head`(SpriteRenderer) + `Weapon`(SpriteRenderer, Sprite Editor에서 스프라이트 자체 피벗을 손잡이 위치로 재설정, 이에 따라 별도 회전축이던 `WeaponPivot`은 제거하고 코드 필드명(`_weaponPivot`)만 유지한 채 실제로는 `Weapon`을 연결). `Character.prefab`은 `BallLauncher`의 `LaunchPoint` 자식으로 배치해 `WallFitter`의 화면비 리프레임을 자동 상속받는다. 좌우 반전은 캐릭터 기본 아트가 왼쪽을 보는 점에 착안해 조준 방향(`BallLauncher.Instance.LaunchDirection`) x가 양수일 때만 루트 `transform.localScale.x`를 -1로 반전하는 방식으로 확정(개별 스프라이트 `flipX` 방식은 반전 조건이 반대로 되는 버그로 폐기). 무기/머리 회전은 `Mathf.Atan2` 각도 계산 방식을 여러 차례 시도했으나 Unity Z축 회전 방향(CW/CCW) 규약을 매번 잘못 추측해 반복적으로 반대 방향을 가리키는 문제가 발생, 최종적으로 `Quaternion.FromToRotation(Vector3.up, 목표방향)`으로 교체해 근본 해결(루트가 반전된 상태에서는 목표 방향 x부호를 미리 뒤집어 계산해야 반전과 상쇄되어 결과가 맞음을 확인). 머리는 무기 회전의 일부 비율(`_headRotationRatio`, 기본 0.25)만 `Quaternion.Slerp`로 보조 추종한다. 실플레이 미세조정 피드백("조준이 수평에 가까울수록 무기가 덜 눕는 것 같다")을 반영해 `_horizontalBiasDegrees`(기본 15도, `[SerializeField]`)를 각도와 무관하게 항상 고정값으로 더하는 보정을 추가했다(처음엔 각도 비례로 시도했다가 사용자 요청으로 고정값으로 변경). `_bodySpriteRenderer`/`_headSpriteRenderer` 필드는 과거 `flipX` 방식의 잔재로 코드 로직상 더 이상 쓰이지 않지만, `CharacterSetupEditor.cs`의 기존 참조 연결 코드를 건드리지 않기 위해 의도적으로 남겨두었다. `CharacterManager.cs`(HP/XP 로직, `Scripts/Core/`)는 이번 작업과 분리되어 전혀 수정하지 않았다. **사용자가 로컬 Unity 실제 플레이 테스트로 최종 확인 완료.**
- [x] 플레이어 액티브 스킬 2종 구현 및 검증 (`_Task/2026-07-05/21-30_player-active-skill-system`): 회수 볼 FIFO 순차 재발사, 스피드업(30초 쿨타임, 6초간 모든 볼 속도 1.5배), 분신(원본 로스터만 복제, 순차 발사, 두 번째 회수 시 발사 지점에서 소멸), 전용 ScriptableObject/매니저/HUD 버튼을 구현. `speedUp`/`illusion` 버튼과 EventSystem/InputSystem UI 입력을 연결하고, UI 터치는 조준 입력에서 제외. 런타임 및 에디터 C# 어셈블리 빌드 오류 0개와 사용자 Unity 플레이 테스트 정상 동작을 확인함
- [x] 고스트볼 벽/바닥 미작동 버그 수정 (`_Task/2026-07-06/19-50_ghost-ball-wall-fix`): 고스트 모드에서 볼 Collider가 트리거로 전환되며 Wall/Ground 접촉이 `OnTriggerEnter2D`로만 발생하는데 이 콜백에 Wall/Ground 처리가 없어 벽을 뚫고 나가던 버그를 발견. 1차 수정(PR #23)으로 `HandleWallHit()`/`HandleGroundHit()`를 `OnTriggerEnter2D`에도 연결했으나, 트리거 콜라이더는 물리 엔진이 속도를 자동 반사시키지 않는다는 점이 남아 있어 2차 수정(PR #24)으로 `ReflectOffTriggerWall()`을 추가해 트리거 경로에서 직접 속도를 반사시키도록 보완. 몬스터 관통 피해는 그대로 유지. 사용자가 실제 테스트로 벽 반사·바닥 귀환·몬스터 관통 피해 모두 정상 동작함을 확인함
- [x] 레이저볼/DoT 대미지 텍스트 미표시 버그 수정 (`_Task/2026-07-06/20-14_damage-text-event-fix`): 레이저볼 가로 행 부가 피해와 DoT 틱 피해가 `Ball.OnHitMonster` 이벤트를 발행하지 않아 대미지 텍스트가 뜨지 않던 문제를, `Ball.cs`에 신설한 `RaiseHitMonster()` 공개 정적 메서드로 두 호출부가 이벤트를 재발행하도록 수정(PR #25, 이벤트 캡슐화 위반 빌드 오류를 잡은 PR #26 후속 수정 포함). 사용자가 실제 테스트로 정상 동작을 확인함
- [x] 몬스터 이동 겹침/관통 버그 수정 (`_Task/2026-07-06/21-25_monster-overlap-freeze-fix`): 실기기 재검증 대기 중이던 5번 항목에서 1×1끼리·1×1과 2칸짜리 완전 겹침, 레인 중상단에서 빙결 몬스터 관통 현상을 추가로 발견. 스프라이트-Collider 크기 불일치, 빙결 중 바닥 돌진 등 1차 가설은 사용자 반박으로 기각되었고, 재조사로 `ProjectSettings/Physics2DSettings.asset`의 `m_AutoSyncTransforms: 0`과 `MonsterBase.Update()`의 직접 Transform 이동이 결합되어 `Collider2D.bounds`가 지연되는 것이 진짜 원인임을 확인. `WaveManager.Awake()`에서 `Physics2D.autoSyncTransforms = true`로 전역 설정해 해결(PR #28). 사용자가 실제 테스트로 겹침·관통 현상이 사라진 것을 확인함

**진행 중**
- [ ] 없음 (선택 Polish 1·5·6·7·8·9·10번 모두 실기기 검증 완료)

**다음 작업 순서**
1. 남은 `TODO.md` 2·3·4번 검토
2. 제출 전 최종 Android 빌드 생성

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

---

## 2026-07-06 UI 오버홀 완료

- [x] 원본 게임 캡처와 PDF 제외 범위를 반영한 HUD, 레벨업 3택지, 일시정지, 결과 팝업 구현
- [x] 전체 텍스트 `Maplestory Bold SDF.asset` 적용
- [x] 캐릭터 HP `현재/최대`, XP, 레벨, 스테이지 진행률 표시
- [x] 캐릭터 HP바 `104×24` 확대 및 진한 갈색 텍스트·크림색 외곽선 가독성 보정
- [x] Ground `-7.5`, LaunchPoint `-6.7`, Character 로컬 Y `-0.4` 최종 배치 및 공 발사·반사 실기기 검증
- [x] 액티브 공격력/스킬 레벨 배지 분리와 패시브 아이콘 비율 보존
- [x] 성공·실패 결과, 게임 정지, 다시 시작 및 `Time.timeScale` 복구 실기기 검증
- [x] 결과 팝업 검증 완료 후 제출 정리 단계에서 좌측 상단 `S`/`F` 테스트 버튼 제거

현재 UI 오버홀 task(`2026-07-06/02-05_ui-overhaul`)는 구현 및 실기기 검증 완료 상태입니다.

## 2026-07-06 삼택지 스킬 효과 및 성장 구조 구현

- [x] 캐릭터 최대 레벨을 Lv.19로 확장하고 필요 XP를 50부터 레벨마다 18씩 증가하도록 변경
- [x] `SkillData`를 읽기 전용으로 전환하고 보유 스킬의 현재 레벨을 `SkillRuntimeState`로 분리
- [x] 액티브 5종 전용 볼 Sprite와 레벨별 `BallDamage`를 실제 로스터 볼에 적용
- [x] 파이어 화상 스택, 아이스 상태이상/추가 피해, 레이저 같은 행 피해, 고스트 관통, 클러스터 서브 볼 구현 보정
- [x] 따뜻한 양철 심장의 노멀 볼 한정 배율, 마법 거울의 볼별 다음 타격 배율, 전·후면 단검의 현재 타격 치명타, 마지막 성냥 폭발 적용
- [x] 삼택지 `New!`, 다음 레벨, 다음 레벨 공격력 및 장착 슬롯 현재 레벨 표시 수정
- [x] 런타임/에디터 C# 빌드 오류 0개 확인(기존 `Rigidbody2D.isKinematic` 경고 1개 제외)
- [x] Unity 플레이 모드에서 전용 볼 외형, 스킬 효과, XP 진행 및 성장 구조 정상 동작 확인

상세 조사와 구현 계획은 `_Task/2026-07-06/10-03_skill-effects-progression/`에 기록되어 있습니다.

## 2026-07-06 제출 정리 및 README

- [x] Android 실제 기기에서 20웨이브 전체 플레이 완료
- [x] 결과 확인용 `S`/`F` 테스트 버튼을 HUD 코드, UI 생성기, 씬에서 완전히 제거
- [x] 플레이어 액티브 스킬 명칭을 코드·데이터·씬·문서 전체에서 `SpeedUp/스피드업`으로 통일
- [x] `PlayerActiveSkillData_SpeedUp.asset`으로 이름을 변경하면서 기존 GUID와 씬 참조 유지
- [x] 프로젝트 개요, 실행법, 구현 범위, 기술 설계, 제외 항목, 검증 내역을 담은 루트 `README.md` 작성
- [x] README에 Codex·Claude/Claude Code 활용 내역과 GPT 제작 버튼 에셋 `SpeedUp.png`·`Copy.png`를 명시

## 2026-07-06 선택 Polish 1·5·6번

- [x] 치명타 데미지 텍스트를 노란색에서 `#FF4B3E`로 변경
- [x] 아이스볼을 직접 맞은 몬스터에만 Freeze/Slow/추가 피해가 적용되도록 후방 전체 전파 제거
- [x] 모든 몬스터의 프레임별 이동 거리를 가장 가까운 앞 몬스터 Collider 간격까지 제한
- [x] 먼 후방 몬스터는 계속 이동하고 접촉한 몬스터만 정지·재이동하도록 열 전체 차단 제거
- [x] 가로 2칸 몬스터를 포함한 1×1·1×2·2×1 일반 이동 겹침 방지
- [x] 중심점 반경 기반 스폰 점유 검사를 실제 Collider Bounds 기반 검사로 교체해 부분 겹침 스폰 방지
- [x] 기존 겹침에서 음수 안전 이동거리가 반환되어 몬스터가 상단 벽 방향으로 밀리던 회귀 수정(접촉·겹침 시 0, 반환 범위 `0..희망 거리`)
- [x] 후보 셀 Bounds가 Collider Y 오프셋을 누락해 실기기에서 겹친 스폰을 허용하던 잔존 결함 수정(`IsCellFree()` 제거, 후보 프리팹의 실제 크기·오프셋·스케일 기반 Bounds 검사로 일반/전체 그리드 스폰 통일)
- [x] 바닥 도달 몬스터의 빠른 진동 → 0.25초 캐릭터 돌진 → 도착 순간 피해·소멸 구현
- [x] 바닥 공격 중 이동·피격·Collider 차단과 풀 재사용 상태 초기화
- [x] 진동 시간·강도·횟수·돌진 시간을 `WaveManager` Inspector에서 조정 가능하도록 노출
- [x] `WaveManager`에 실제 캐릭터 Transform 참조 연결 및 Setup Editor 갱신
- [x] 런타임/Editor C# 빌드 오류 0개 확인(기존 `Rigidbody2D.isKinematic` 경고 1개 제외)
- [x] 치명타 텍스트 색상, 바닥 도달 진동→돌진→소멸 연출 Android 실기기 검증 완료
- [ ] 아이스볼 이동 겹침 방지 Android 실기기 재검증 대기

상세 조사와 구현 계획은 `_Task/2026-07-06/14-45_critical-bottom-ice-polish/`에 기록되어 있습니다.

## 2026-07-06 선택 Polish 7번

- [x] 몬스터 피격 시 본체 스프라이트가 흰색으로 짧게 반짝이는 히트 플래시 구현
- [x] 아이스볼 냉동/슬로우 지속 중 하늘색, 파이어볼 화상 지속 중 붉은주황 틴트 구현, 얼음/화상 동시 발동 시 나중에 걸린 효과 색 우선 적용
- [x] 원래 계획한 `SpriteRenderer.color` 오버브라이트(곱하기 틴트) 방식이 실기기 테스트에서 배율을 3배→8배로 올려도 시각적 변화가 없어 폐기 — `SpriteRenderer.color`가 스프라이트 메쉬 정점 색상으로 저장되며 0~1로 클램핑돼 1을 넘는 값이 전부 잘리는 것이 원인으로 확정 진단
- [x] 신규 `Assets/_Project/Shaders/SpriteFlashOverlay.shader`(URP 호환, 텍스처 알파를 마스크로 균일한 색을 그리는 셰이더)와 `MonsterBase.cs`의 런타임 생성 자식 오버레이 `SpriteRenderer`(`_flashOverlayRenderer`)로 교체, 본체 색(상태 틴트 전담)과 플래시(오버레이 전담)를 완전히 분리
- [x] 발판(`BlockVisual`)은 원래 색상 효과 제외 대상이었으나, 오버레이 방식이 상태 틴트와 무관한 별도 레이어라 문제없다고 판단해 동일한 흰색 플래시가 뜨도록 확장 적용
- [x] 얼음/화상 지속 중 플래시를 생략하던 원래 방침을, 오버레이 분리 이후 틴트를 가리는 문제가 사라져 상태이상 지속 중에도 항상 플래시가 뜨도록 최종 변경
- [x] `_hitFlashColor`/`_hitFlashDuration`/`_freezeTintColor`/`_burnTintColor` 4개 값을 `[SerializeField]`로 노출, `_hitFlashDuration`은 테스트 중 0.4초로 상향
- [x] 실기기 빌드로 히트 플래시(몸체+발판), 얼음 틴트, 화상 틴트 모두 정상 동작 확인
- [x] 실기기 재검증 중 흰색 히트 플래시만 전혀 보이지 않는 회귀 발견 — HP바는 정상 반응해 `TakeDamage()` 로직 자체는 문제없이 실행되고 있었고, 원인은 코드가 아니라 Unity 빌드 설정으로 확정 진단: 커스텀 셰이더 `Assets/_Project/Shaders/SpriteFlashOverlay.shader`가 어떤 Material 에셋에도 정적으로 연결되지 않고 `MonsterBase.cs`에서 `Shader.Find("PurpleCow/SpriteFlashOverlay")`로만 런타임에 찾는 구조라, 실기기(APK) 빌드 시 "어디서도 정적으로 참조되지 않는 미사용 셰이더"로 간주돼 최종 빌드에서 제외됨(에디터는 프로젝트의 모든 셰이더를 항상 포함하므로 Play 모드에서는 정상 작동, 실기기에서만 재현되는 증상과 정확히 일치)
- [x] `ProjectSettings/GraphicsSettings.asset`의 Always Included Shaders 목록에 해당 셰이더(`{fileID: 4800000, guid: 07d1d10750f4bd5449e44439088e0b41, type: 3}`)를 추가해 실기기 빌드에도 항상 포함되도록 수정, 코드(.cs)는 전혀 수정하지 않음
- [x] 사용자가 실기기 재빌드로 직접 테스트해 히트 플래시가 정상적으로 다시 보임을 최종 확인

상세 조사와 구현 계획은 `_Task/2026-07-06/16-20_monster-hit-status-color-fx/`에 기록되어 있습니다.
