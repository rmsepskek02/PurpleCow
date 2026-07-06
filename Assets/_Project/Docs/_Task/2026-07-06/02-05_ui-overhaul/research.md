# Research — Original Game UI Overhaul

이 문서는 실제 게임 캡처 13장과 공식 채용 과제 PDF를 기준으로 PurpleCow의 UI를 원본에 가깝게 재구현하기 위한 현재 상태 조사입니다.
`targetUI/TestResult/`는 비교 대상에서 제외하며, PDF가 구현 제외로 지정한 기능은 원본 화면에 있더라도 만들지 않습니다.

## 현재 상태

### 확정된 UI 기준

- 공식 PDF가 기능 범위의 최우선 기준입니다.
- `Assets/_Project/Docs/targetUI/` 루트의 실제 게임 캡처 13장이 시각적 기준입니다.
- PDF 제외 항목인 튜토리얼, 배속, 보스, 자동 조준, 다시 뽑기, 융합은 UI에서도 제거합니다.
- 보스 제외에 따라 스테이지 진행바 끝의 보스 얼굴 아이콘도 제거합니다.
- 전용 리소스가 없는 프레임, 게이지, 버튼은 Unity 기본 UI로 유사하게 제작합니다.
- 프로젝트 내 모든 텍스트는 `Assets/_Project/Fonts/Maplestory Bold SDF.asset`을 사용합니다.
- 스피드업과 분신은 가산점 추가 콘텐츠이므로 기존 기능을 보존하고 최종 HUD에 통합합니다.

### 원본 화면에서 확인한 정보 구조

- 상단 중앙에 `스테이지 번호. 스테이지명`이 표시됩니다.
- 스테이지명 아래의 가느다란 적색 게이지는 전체 스테이지 진행률을 표시합니다.
- 적색 게이지 아래의 긴 노란 게이지는 캐릭터 경험치를 표시합니다.
- 노란 경험치바 오른쪽 숫자 배지는 경험치에 따라 상승하는 캐릭터 레벨입니다.
- 캐릭터 HP는 화면 전체 폭의 하단 HUD가 아니라 캐릭터 바로 아래에 표시됩니다.
- 레벨업 화면은 딤 배경, 제목, 경험치바, 액티브 4칸, 패시브 2칸, 카드 3장으로 구성됩니다.
- 일시정지 화면은 스테이지 정보, 보유 스킬, 드롭 정보, 이어하기 버튼으로 구성합니다.
- 원본의 홈 버튼과 설정 버튼은 사용자 결정에 따라 제외합니다.
- 원본의 `Best!` 추천 표시는 추천 규칙을 새로 만들지 않고 제외합니다.
- 성공·실패 화면의 직접적인 캡처는 없으며 PDF는 결과 팝업과 1스테이지 재시작을 필수로 요구합니다.

### 현재 씬 구조

현재 `Assets/Scenes/SampleScene.unity`에는 다음 UI가 존재합니다.

- `Canvas_HUD`, `Canvas_Panel`, `Canvas_Popup`
- `HUDPanel`, `ResultPanel`, `SkillSelectionPanel`
- `CharacterHP`, `CharacterXP`
- 빈 `LevelUpPanel`, `PausePanel`, `BallLevelUpPanel`
- 스피드업/분신 버튼

하지만 현재 구조는 기능 연결용 초기 스캐폴딩에 가깝고 원본 UI 레이아웃은 구현되지 않았습니다.

- `SafeAreaPanel`이 비어 있어 HUD가 Safe Area의 적용을 받지 않습니다.
- `ResultPanel`과 `SkillSelectionPanel`이 `Canvas_HUD` 아래에 있습니다.
- `Canvas_Popup`은 비어 있습니다.
- 스피드업/분신 버튼은 `Canvas_Panel`에 직접 배치되어 있습니다.
- CharacterHP/XP Slider에는 시각적인 Fill 그래픽이 없습니다.
- CharacterHP는 화면 하단 전체 폭, CharacterXP는 화면 하단에 배치되어 원본과 다릅니다.
- SkillCard 프리팹과 인스턴스는 기본 100 x 100 크기이며 세 카드가 중앙에 겹칩니다.
- SkillSlot 프리팹도 기본적인 검정 사각형과 텍스트만 존재합니다.
- PausePanel은 빈 오브젝트입니다.
- ResultPanel은 텍스트와 그래픽이 없는 재시작 Button만 존재합니다.

### 현재 UI 로직

- `HUDPanel`은 현재 `WAVE n / total`, 처치 점수, 현재 웨이브 몬스터 비율을 표시합니다.
- `CharacterManager`는 HP, XP, 레벨 이벤트를 이미 제공합니다.
- `SkillSelectionPanel`은 `CharacterManager.OnLevelUp`이 아니라 `WaveManager.OnKillCountReached`를 구독합니다.
- `UIManager`, `HUDPanel`, `SkillSelectionPanel`, `ResultPanel`은 `SetActive`와 CanvasGroup을 혼용합니다.
- `UIButton`은 PointerDown/PointerUp만 처리해 PointerExit 또는 입력 취소 시 축척이 복구되지 않을 수 있습니다.
- `SafeAreaFitter`는 Awake에서 한 번만 적용하며 화면 영역 변경을 다시 감지하지 않습니다.
- `PlayerActiveSkillButton`은 기존 쿨다운 오버레이와 남은 시간 표시 기능을 갖고 있습니다.
- 몬스터 HP바는 피격 후 표시, 풀링 재사용 구독, Background/Fill 그래픽이 현재 구현되어 있습니다.

## 관련 파일 및 의존성

### 기준 문서와 자료

| 파일 | 역할 |
|------|------|
| `PurpleCow_클라이언트_채용과제.pdf` | 필수·제외 기능 기준 |
| `Assets/_Project/Docs/targetUI/*.jpg` | 원본 UI 시각 기준 |
| `Assets/_Project/Docs/UIRules.md` | 확정된 UI 구현 규칙 |
| `Assets/_Project/Docs/PlayerActiveSkillDesign.md` | 스피드업·분신 기능 규칙 |

### UI 스크립트

| 파일 | 현재 역할 | 주요 변경 필요성 |
|------|-----------|------------------|
| `Scripts/UI/UIManager.cs` | HUD, 결과, 스킬 패널 제어 | 새 Canvas 계층과 패널 상태 연결 |
| `Scripts/UI/HUDPanel.cs` | 웨이브·점수·진행 텍스트 | 스테이지 진행률, XP, 레벨, 일시정지 HUD로 재구성 |
| `Scripts/UI/SkillSelectionPanel.cs` | 3택지 생성과 선택 | 레벨업 이벤트 연결 및 새 레이아웃 대응 |
| `Scripts/UI/SkillCardUI.cs` | 카드 데이터 표시 | 신규/레벨/타입/Best 표시 확장 |
| `Scripts/UI/SkillSlotGroup.cs` | 보유 슬롯 갱신 | 레이아웃은 프리팹/Setup에서 변경 |
| `Scripts/UI/SkillSlotIcon.cs` | 슬롯 아이콘·레벨 표시 | `xN`, `Max`, 폰트와 색상 반영 |
| `Scripts/UI/ResultPanel.cs` | 결과 제목·점수·재시작 | 성공/실패 스타일과 결과 정보 확장 |
| `Scripts/UI/CharacterHpBar.cs` | 캐릭터 HP Slider | World Space 캐릭터 하단 표시 |
| `Scripts/UI/CharacterXpBar.cs` | XP Slider·레벨 | 상단 HUD 게이지와 레벨 배지 표시 |
| `Scripts/UI/PlayerActiveSkillButton.cs` | 스피드업·분신 버튼 | 기능 보존, HUD 위치·폰트·외형 변경 |
| `Scripts/UI/SafeAreaFitter.cs` | Safe Area 적용 | 화면 영역 변경 재적용 |
| `Scripts/UI/UIButton.cs` | 버튼 축척 피드백 | PointerExit/취소 및 Tween 중복 방지 |

### 게임 상태 및 데이터

| 파일 | 연동 내용 |
|------|-----------|
| `Scripts/Core/CharacterManager.cs` | HP, XP, 레벨 이벤트와 초기값 |
| `Scripts/Wave/WaveManager.cs` | 20웨이브 진행 상태와 기존 킬 카운트 트리거 |
| `Scripts/Core/GameManager.cs` | 게임 상태, 일시정지, 성공·실패, 재시작 |
| `Scripts/Skill/SkillManager.cs` | 액티브·패시브 보유 상태 |
| `Scripts/Data/SkillData.cs` | 카드 이름, 설명, 아이콘, 레벨 데이터 |

### 에디터·씬·프리팹

| 파일 | 변경 목적 |
|------|-----------|
| `Scripts/Editor/UISetupEditor.cs` | 목표 UI 계층과 스타일을 반복 생성 가능한 방식으로 구성 |
| `Assets/Scenes/SampleScene.unity` | 생성된 UI 계층과 참조 저장 |
| `Prefabs/UI/SkillCard.prefab` | 원본형 세로 카드 |
| `Prefabs/UI/SkillSlot.prefab` | 액티브/패시브 보유 슬롯 |
| `Prefabs/UI/DamageTextFx.prefab` | 전역 폰트 적용 |
| `Prefabs/Character/Character.prefab` | 캐릭터 하단 World Space HP바 |

### 외부 의존성

- Unity 6000.3.10f1
- UGUI
- TextMeshPro
- DOTween
- Input System

현재 Unity Editor가 프로젝트를 열고 있으므로 구현 중 스크립트 재컴파일은 가능하지만, 같은 프로젝트를 별도 BatchMode 인스턴스로 동시에 열 수는 없습니다.
씬 생성 메뉴 실행과 실제 Game View 시각 검증은 현재 열린 Editor에서 수행해야 합니다.

## 문제점 / 구현 대상 파악

### 1. Canvas 계층 전면 재구성

- 상시 HUD를 `Canvas_HUD/SafeAreaPanel` 아래로 이동합니다.
- 레벨업과 일시정지는 `Canvas_Panel`에 배치합니다.
- 결과 팝업은 `Canvas_Popup`에 배치합니다.
- 캐릭터 HP와 몬스터 HP는 각 월드 프리팹에서 관리합니다.
- 기존의 빈 `LevelUpPanel`, `BallLevelUpPanel`과 중복 구조를 정리해야 합니다.

### 2. 원본형 인게임 HUD

- 스테이지명과 전체 진행률을 상단 중앙에 배치합니다.
- 노란 XP바와 캐릭터 레벨 배지를 그 아래에 배치합니다.
- Auto, 배속, 보스 아이콘은 제외합니다.
- 일시정지 버튼만 우측 상단에 둡니다.
- 스피드업/분신은 우측 하단 Safe Area 안에 세로로 배치하는 안을 적용합니다.

### 3. 캐릭터 상태 UI

- CharacterXP는 하단에서 상단 HUD로 이동합니다.
- CharacterHP는 캐릭터 프리팹의 World Space Canvas로 이동합니다.
- HP 텍스트는 원본처럼 현재 HP 숫자만 표시합니다.
- UI 활성화 시 현재 값이 즉시 표시되도록 CharacterManager의 현재 상태 접근 또는 초기 이벤트 발행이 필요합니다.

### 4. 3택지 패널

- 레벨업 이벤트를 표시 트리거로 사용합니다.
- 딤, `레벨 업` 제목, XP/레벨, 보유 슬롯, 카드 3장을 원본 비율로 배치합니다.
- 액티브는 적갈색, 패시브는 청록색 계열로 구분합니다.
- 다시 뽑기와 융합은 제외합니다.
- 모든 카드 텍스트와 아이콘이 카드 내부에 안정적으로 배치되어야 합니다.

### 5. 일시정지 패널

- 캡처의 스테이지 정보, 스킬 슬롯, 드롭 영역, 하단 버튼 구성을 재현합니다.
- 이어하기는 TimeScale 복구와 패널 닫기를 수행합니다.
- 프로젝트에 대응 시스템이 없는 홈과 설정 버튼은 만들지 않습니다.

### 6. 결과 팝업

- 기존 UI의 어두운 딤, 금색/적갈색 제목, 주황색 버튼 스타일을 응용합니다.
- 성공/실패, 스테이지명, 도달 웨이브, 처치 수를 표시합니다.
- 재시작 버튼은 기존 `GameManager.RestartGame()`을 사용합니다.

### 7. 공통 스타일과 폰트

- 모든 TMP 컴포넌트에 `Maplestory Bold SDF.asset`을 연결합니다.
- 전용 UI Sprite가 없는 프레임과 버튼은 중첩 Image와 9-slice로 제작합니다.
- 장식 Graphic의 Raycast Target을 비활성화합니다.
- 버튼만 Raycast를 받도록 정리합니다.

### 8. 패널 상태와 입력

- 패널 제어를 CanvasGroup 중심으로 통일합니다.
- TimeScale 0에서도 패널 애니메이션이 동작하도록 Unscaled Update를 사용합니다.
- 버튼 위 터치가 볼 조준으로 전달되지 않게 유지합니다.
- UIButton의 축척 복구와 Tween 중복을 보완합니다.

### 9. 기존 사용자 변경 보호

현재 `SampleScene.unity`에는 사용자가 조절한 TrajectoryPreview 점선 수치 변경이 있습니다.

- `_dashLength: 0.04`
- `_dashGap: 0.02`
- `_dashScrollSpeed: 2`

UI 구현 시 이 변경을 보존하고 관련 없는 게임플레이 값은 수정하지 않습니다.
`.claude/settings.local.json`도 사용자 파일로 간주해 수정하지 않습니다.

### 10. 사용자 확정 사항

- 일시정지 화면의 홈 버튼은 제외합니다.
- 일시정지 화면의 설정 버튼은 제외합니다.
- 스킬 카드의 `Best!` 추천 표시는 제외합니다.
- 위 항목을 위한 별도 기능이나 빈 버튼도 만들지 않습니다.

## 결론

현재 프로젝트에는 HP·XP·레벨·스킬 선택·결과·가산점 액티브 스킬의 기본 로직과 참조 구조가 존재하지만, 시각 UI는 대부분 기본 오브젝트 상태입니다.
원본 복제 목표를 달성하려면 `UISetupEditor`를 중심으로 Canvas 계층, 프리팹, 공통 스타일을 재구성하고 UI 스크립트의 이벤트·상태 연결을 함께 수정해야 합니다.

사용자 확정 사항을 반영해 홈, 설정, `Best!`를 제외한 범위로 `plan.md`를 작성합니다.
