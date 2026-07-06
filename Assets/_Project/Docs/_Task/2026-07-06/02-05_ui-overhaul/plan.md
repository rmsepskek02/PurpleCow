# Plan — Original Game UI Overhaul

이 계획은 공식 PDF 범위를 지키면서 실제 게임 캡처 13장의 인게임 HUD, 레벨업 3택지, 일시정지 화면을 재현하고 결과 팝업을 동일한 시각 언어로 제작하는 작업을 다룹니다.
홈, 설정, `Best!`, Auto, 배속, 보스, 다시 뽑기, 융합은 구현하지 않으며 기존 스피드업·분신 기능은 보존합니다.

## 구현 목표

- 1080 x 2340 기준의 세로형 모바일 UI를 구성합니다.
- 모든 텍스트에 `Maplestory Bold SDF.asset`을 적용합니다.
- 상단에 스테이지명, 전체 진행률, 캐릭터 XP, 캐릭터 레벨, 일시정지 버튼을 배치합니다.
- 캐릭터 HP를 캐릭터 바로 아래의 World Space UI로 표시합니다.
- 레벨업 시 원본형 3택지 패널을 표시합니다.
- 홈·설정 없이 이어하기만 제공하는 일시정지 패널을 구현합니다.
- 성공·실패 결과와 재시작 기능을 갖는 결과 팝업을 구현합니다.
- 스피드업·분신 버튼을 우측 하단 Safe Area 안에 통합합니다.
- 원본 전용 UI 리소스가 없는 부분은 Unity 기본 Image, Slider, TMP, 중첩 프레임으로 제작합니다.
- 현재 사용자가 조정한 TrajectoryPreview 씬 수치를 보존합니다.

## 단계별 작업 계획

### 1. UI 상태 데이터와 이벤트 정리

- `CharacterManager`에 현재 HP, 최대 HP, 현재 XP, 필요 XP, 현재 레벨을 읽을 수 있는 프로퍼티를 추가합니다.
- XP 획득 후 레벨업과 잔여 XP가 UI에 올바르게 반영되도록 이벤트 발행 순서를 정리합니다.
- `WaveManager`에 현재 웨이브 번호와 전체 진행률 계산에 필요한 읽기 전용 상태를 제공합니다.
- UI에서 초기 표시 시 이벤트를 기다리지 않고 현재 값을 읽어 즉시 렌더링할 수 있게 합니다.
- 기존 전투·웨이브 밸런스 수치는 변경하지 않습니다.

### 2. 공통 UI 동작 보완

- `SafeAreaFitter`가 Safe Area 또는 화면 크기 변경 시 다시 적용되게 수정합니다.
- `UIButton`에 PointerExit/취소 복구와 DOTween 중복 종료를 추가합니다.
- HUD, 레벨업, 일시정지, 결과 패널을 CanvasGroup 중심으로 표시·숨김 처리합니다.
- TimeScale 0 상태의 패널 애니메이션에는 Unscaled Update를 사용합니다.

### 3. 인게임 HUD 로직 재구성

- `HUDPanel`을 스테이지 제목, 전체 진행률, XP, 레벨, 일시정지 버튼 중심으로 변경합니다.
- 기존 일반 HUD의 처치 점수 표시는 제거하고 결과 팝업 데이터로만 유지합니다.
- CharacterXP UI는 상단 노란 게이지와 우측 레벨 배지로 갱신합니다.
- Auto, 배속, 보스 아이콘은 생성하지 않습니다.
- 스피드업·분신 버튼의 기능 연결과 쿨다운 표시는 그대로 유지합니다.

### 4. 캐릭터 HP World Space UI

- `Character.prefab` 아래에 World Space Canvas와 HP Slider를 구성합니다.
- 캐릭터 바로 아래에 초록 Fill, 어두운 배경, `현재 HP/최대 HP` 숫자를 표시합니다.
- `CharacterHpBar`가 활성화될 때 현재 HP를 즉시 반영하게 수정합니다.
- 기존 화면 하단의 CharacterHP 오브젝트는 중복되지 않게 제거합니다.

### 5. 레벨업 3택지 로직과 화면

- `SkillSelectionPanel`의 표시 트리거를 누적 킬 카운트가 아니라 `CharacterManager.OnLevelUp`으로 변경합니다.
- 레벨업 시 TimeScale을 정지하고 선택 완료 후 복구합니다.
- 새 레벨 배지와 가득 찬 XP바를 패널 상단에 표시합니다.
- 액티브 4칸과 패시브 2칸을 원본 비율로 배치합니다.
- SkillCard 프리팹을 세로형 카드로 재구성합니다.
- 액티브 카드는 적갈색, 패시브 카드는 청록색으로 표시합니다.
- `New!`, 타입, 이름, 아이콘, 데미지, 설명, 레벨 단계를 표시합니다.
- 다시 뽑기, 융합, `Best!`는 생성하지 않습니다.
- PDF의 보유 수 및 중복 카드 제한 로직은 유지합니다.

### 6. 일시정지 패널

- 새 `PausePanel` 스크립트를 작성합니다.
- 우측 상단 일시정지 버튼으로 열고 이어하기 버튼으로 닫습니다.
- 패널이 열리면 TimeScale을 0으로, 닫히면 1로 복구합니다.
- 제목, 스테이지 정보, 보유 스킬, 스테이지 드롭 영역을 원본 스타일로 배치합니다.
- 하단에는 중앙의 이어하기 버튼만 배치합니다.
- 홈과 설정 버튼 및 관련 기능은 만들지 않습니다.

### 7. 성공·실패 결과 팝업

- `ResultPanel`을 `Canvas_Popup` 아래의 전체 화면 팝업으로 재구성합니다.
- 성공은 금색, 실패는 적갈색 제목을 사용합니다.
- 스테이지명, 도달 웨이브, 처치 수를 표시합니다.
- 큰 주황색 다시 시작 버튼을 배치하고 기존 재시작 기능에 연결합니다.
- 추가 보상, 광고, 다음 스테이지 버튼은 만들지 않습니다.

### 8. UISetupEditor 재작성

- 기존 `UISetupEditor`를 목표 구조를 반복 실행해도 중복되지 않는 방식으로 정리합니다.
- `Canvas_HUD/SafeAreaPanel`, `Canvas_Panel`, `Canvas_Popup` 계층을 생성합니다.
- 기존 UI 스캐폴딩 중 목표 구조와 충돌하는 오브젝트만 정리합니다.
- 원본 전용 Sprite가 없는 프레임은 Unity 내장 UI Sprite와 중첩 Image로 제작합니다.
- 모든 TMP 컴포넌트에 Maplestory Bold Font Asset을 연결합니다.
- SkillCard, SkillSlot, DamageText 프리팹의 폰트와 레이아웃을 갱신합니다.
- 스피드업·분신 버튼을 `Canvas_HUD/SafeAreaPanel` 아래로 옮기고 우측 하단 세로 그룹으로 배치합니다.
- 실행 가능한 메뉴 `PurpleCow/Setup/UI Setup`을 유지합니다.

### 9. 씬과 프리팹 적용

- 열린 Unity Editor에서 `PurpleCow/Setup/UI Setup`을 실행합니다.
- 생성된 씬 계층과 직렬화 참조를 저장합니다.
- `Character.prefab`, `SkillCard.prefab`, `SkillSlot.prefab`, `DamageTextFx.prefab` 변경을 저장합니다.
- TrajectoryPreview의 `_dashLength`, `_dashGap`, `_dashScrollSpeed` 값이 보존됐는지 확인합니다.

### 10. 검증

- C# 컴파일 오류가 없는지 확인합니다.
- 1080 x 2340 기준 Game View에서 HUD 요소가 캡처의 상대 위치와 일치하는지 확인합니다.
- 648 x 1368 비율에서 Safe Area와 카드 레이아웃이 잘리지 않는지 확인합니다.
- XP 증가, 레벨 배지 상승, 3택지 표시, 스킬 선택 후 재개를 확인합니다.
- 일시정지/이어하기와 TimeScale 복구를 확인합니다.
- 성공/실패 팝업과 재시작을 확인합니다.
- 스피드업·분신 버튼의 기존 기능과 쿨다운 표시를 확인합니다.
- 캐릭터·몬스터 HP바와 조준 입력이 UI 변경 후에도 정상인지 확인합니다.
- 최종적으로 `git diff --check`를 실행합니다.

## 예상 변경/생성 파일 목록

### 문서

- `Assets/_Project/Docs/UIRules.md`
- `Assets/_Project/Docs/_Task/2026-07-06/02-05_ui-overhaul/research.md`
- `Assets/_Project/Docs/_Task/2026-07-06/02-05_ui-overhaul/plan.md`
- 구현 완료 후 `Assets/_Project/Docs/ProjectStatus.md`
- 구현 완료 후 `Assets/_Project/Docs/ProjectHistory.md`

### 기존 스크립트

- `Assets/_Project/Scripts/Core/CharacterManager.cs`
- `Assets/_Project/Scripts/Wave/WaveManager.cs`
- `Assets/_Project/Scripts/UI/UIManager.cs`
- `Assets/_Project/Scripts/UI/HUDPanel.cs`
- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`
- `Assets/_Project/Scripts/UI/SkillCardUI.cs`
- `Assets/_Project/Scripts/UI/SkillSlotIcon.cs`
- `Assets/_Project/Scripts/UI/ResultPanel.cs`
- `Assets/_Project/Scripts/UI/CharacterHpBar.cs`
- `Assets/_Project/Scripts/UI/CharacterXpBar.cs`
- `Assets/_Project/Scripts/UI/PlayerActiveSkillButton.cs`
- `Assets/_Project/Scripts/UI/SafeAreaFitter.cs`
- `Assets/_Project/Scripts/UI/UIButton.cs`
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs`

### 신규 스크립트

- `Assets/_Project/Scripts/UI/PausePanel.cs`

### 씬과 프리팹

- `Assets/Scenes/SampleScene.unity`
- `Assets/_Project/Prefabs/Character/Character.prefab`
- `Assets/_Project/Prefabs/UI/SkillCard.prefab`
- `Assets/_Project/Prefabs/UI/SkillSlot.prefab`
- `Assets/_Project/Prefabs/UI/DamageTextFx.prefab`

실제 구현 중 불필요한 파일 변경이 확인되면 해당 파일은 수정 대상에서 제외합니다.
새 UI 전용 이미지 파일은 만들지 않고 Unity 기본 UI를 사용합니다.

## 주의사항

- `SampleScene.unity`의 기존 TrajectoryPreview 점선 설정은 사용자 변경이므로 덮어쓰지 않습니다.
- `.claude/settings.local.json`은 수정하지 않습니다.
- 몬스터 HP바의 현재 정상 구현을 되돌리지 않습니다.
- 스피드업·분신의 게임플레이 로직과 데이터는 수정하지 않습니다.
- 홈, 설정, `Best!`, Auto, 배속, 보스, 다시 뽑기, 융합을 추가하지 않습니다.
- 스킬 밸런스, 웨이브 밸런스, 볼 물리는 UI 작업 범위 밖입니다.
- Unity Editor가 열린 상태이므로 별도 BatchMode 실행 대신 현재 Editor의 컴파일과 Setup 메뉴를 사용합니다.
- 실제 캡처에서 확인할 수 없는 애니메이션 수치는 Inspector 조절값으로 두고 과도한 연출을 추가하지 않습니다.
- plan 승인 전에는 코드, 씬, 프리팹을 수정하지 않습니다.

## 2026-07-06 실기기 검증 결과

- 일반 플레이에서 공 발사, 하단 벽 반사, 궤적 시작점이 정상 동작함을 확인했습니다.
- 최종 배치값은 Ground `-7.5`, LaunchPoint `-6.7`, Character 로컬 Y `-0.4`입니다.
- 캐릭터 HP `현재/최대` 표시와 좌우 반전 시 정방향 유지가 확인됐습니다.
- 우측 하단 액티브 버튼의 프레임 밖 배경이 제거됐습니다.
- 레벨업 카드의 공격력과 상단 슬롯의 스킬 레벨이 아이콘과 분리되어 표시됩니다.
- 패시브 검 아이콘의 원본 가로세로 비율이 보존됩니다.
- 레벨업 및 일시정지 패널의 스킬 슬롯, 카드, 이어하기 동작이 정상 표시됩니다.
- 액티브 볼 아이콘은 원본 유효 영역이 약 39~50px여서 해상도 한계가 남지만, 배경판과 비율 보존으로 식별성이 개선됐습니다.
- 성공/실패 결과 팝업은 이번 캡처 묶음에 포함되지 않아 별도 확인이 남아 있습니다.
- 결과 팝업 테스트를 위해 HUD 좌측 상단에 일시정지 버튼과 같은 크기의 `S`/`F` 버튼을 추가합니다. `S`는 성공, `F`는 실패 결과 흐름을 호출합니다.
- 첫 `S`/`F` 테스트에서 결과 분기는 동작했으나 제목과 스테이지명이 Top Anchor 위로 빠지고 패널이 화면 상단에 붙는 문제가 확인됐습니다. 결과 내용을 중앙 패널의 자식으로 재배치하고, 결과 진입 시 `Time.timeScale=0` 및 Result Tween의 Unscaled Update를 적용했습니다. 수정본 실기기 확인이 남아 있습니다.
- 수정 후 실기기 캡처에서 성공/실패 제목, 스테이지명, 도달 웨이브, 최종 점수, 다시 시작 버튼이 중앙 패널 안에 정상 표시되고 성공 금색/실패 적갈색 구분이 적용된 것을 확인했습니다. `다시 시작` 버튼을 눌렀을 때 게임과 `Time.timeScale`이 정상 복구되는지만 마지막 확인 항목으로 남아 있습니다.
- 사용자가 `다시 시작` 버튼까지 직접 테스트해 게임과 `Time.timeScale`이 정상 복구되는 것을 확인했습니다. 이 task의 HUD, 레벨업, 일시정지, 성공/실패 결과 및 재시작 실기기 검증을 완료했습니다.
- 후속 가독성 보정으로 캐릭터 HP바를 `86×16`에서 `104×24`로 확대하고, `현재/최대` 텍스트를 14pt 진한 갈색 + 크림색 외곽선으로 변경했습니다. 사용자가 실기기 캡처로 텍스트가 바 안에 들어오고 대비가 개선된 것을 확인했습니다.
