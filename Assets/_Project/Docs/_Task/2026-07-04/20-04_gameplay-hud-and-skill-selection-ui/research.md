# Research — Gameplay HUD 진행도 개편 및 삼택지 스킬 선택 UI 개편

이 문서는 게임플레이 중 표시되는 HUD(스테이지/웨이브 진행도)와 3택지(3-choice) 스킬 선택 카드 UI, 두 가지를 함께 개편하기 위한 조사 문서입니다. 현재 코드 구현 상태, PDF 공식 스펙, 원본 게임 레퍼런스 이미지, UIRules.md 설계 문서를 대조하여 갭을 파악하고, 사용자와 논의를 거쳐 확정된 결정사항을 정리합니다. 실제 구현 단계는 이후 plan.md에서 다룹니다.

---

## 현재 상태

### 1. HUD (`HUDPanel.cs`)

`Assets/_Project/Scripts/UI/HUDPanel.cs`는 다음 필드만 가지고 있습니다.

- `_waveText` (TMP_Text): `WaveManager.OnWaveStarted` 이벤트 수신 시 `"WAVE {waveNumber} / {_totalWaves}"` 문자열로 갱신 (17번째 줄, `HandleWaveStarted`)
- `_progressText` (TMP_Text): `WaveManager.OnMonsterCountChanged(remaining, total)` 이벤트 수신 시 `"{percent}%"` 문자열로 갱신. percent는 `(total - remaining) / total * 100`으로 계산됨
- `_scoreText` (TMP_Text): `UIManager.OnScoreChanged` 이벤트 수신 시 `"처치: {score}"` 문자열로 갱신
- `_launchReadyIndicator` / `_launchReadyCanvasGroup`: `GameManager.OnGameStateChanged`가 `Playing` 상태일 때 alpha 1, 그 외 0으로 표시/숨김 (`SetLaunchIndicatorVisible`)
- `_canvasGroup` 기반 `Show()`/`Hide()`: DOTween Sequence로 슬라이드+페이드 애니메이션 처리 (UIRules.md 5번 규칙과 일치)

**시각적 진행바(Image의 fillAmount, Slider 등)는 전혀 존재하지 않으며, 진행도는 오직 텍스트로만 표현됩니다.** 웨이브 번호를 나타내는 배지, 진행바를 감싸는 프레임 등 시각 요소도 없습니다.

`WaveManager.cs`(`Assets/_Project/Scripts/Wave/WaveManager.cs`)를 확인한 결과, `OnMonsterCountChanged`가 전달하는 `(remaining, total)`은 **현재 웨이브 한 번(SpawnWave 호출 시점)의 몬스터 수 기준**입니다(`_currentWaveTotalCount`는 `SpawnWave()`에서 웨이브가 시작될 때마다 새로 계산되어 초기화됨, 61~82번째 줄). 즉 `_progressText`가 표시하는 %는 "스테이지 전체 진행률"이 아니라 "현재 웨이브 내 처치율"에 해당하는 값입니다. 반면 레퍼런스 이미지(`KakaoTalk_20260703_115951058_01/02/03.jpg`)에서는 `"1. 깊은 숲"` 스테이지명 옆에 빨간 %바가 있고, 그 아래에 별도의 금색 웨이브 진행바 + 웨이브 번호 배지가 있어 두 개의 서로 다른 바가 공존합니다. 현재 코드에는 이 두 바를 구분할 수 있는 시각 요소 자체가 없고, 계산 로직도 하나(`_progressText`)뿐입니다.

또한 `WaveTableData.cs`(`Assets/_Project/Scripts/Data/WaveTableData.cs`)와 `WaveEntry` 구조체를 확인한 결과, 스테이지/웨이브 데이터에는 `WaveNumber`와 `SpawnEntries`만 있고 **"깊은 숲" 같은 스테이지명을 담는 필드가 전혀 존재하지 않습니다.**

### 2. 삼택지 스킬 선택 (`SkillSelectionPanel.cs`, `SkillCardUI.cs`)

`Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`

- `WaveManager.OnKillCountReached` 이벤트(일정 처치 수마다 발행, `WaveManager._killCountForSkill` 단위)로 `OpenPanel()` 호출 → PDF의 "처치 수 조건 충족 시 노출" 스펙과 일치
- `BuildSkillCardPool()`: 액티브/패시브 각각에 대해 보유 중(`activeSkillIds.Contains`)이면 `CurrentLevel < MaxLevel - 1`일 때만 후보에 추가(레벨업 카드), 미보유 상태면 `SkillManager.CanEquipActive`/`CanEquipPassive`(액티브 4개, 패시브 2개 미만)일 때만 후보에 추가(신규 카드) — PDF 스펙(보유하지 않은 스킬은 레벨1만, 최대보유 시 업그레이드만, 동일 카드 중복 없이 레벨업만 가능)과 일치하는 것으로 확인됨
- 후보 풀에서 `OrderBy(Random.value).Take(3)`으로 3장 무작위 선택 — PDF의 "전체 풀에서 랜덤 추출" 스펙과 일치
- `_activeSlotGroup`/`_passiveSlotGroup`(`SkillSlotGroup`)으로 액티브/패시브 슬롯 요약을 함께 갱신

`Assets/_Project/Scripts/UI/SkillCardUI.cs`

- `_iconImage`, `_nameText`, `_descriptionText`, `_typeText`(액티브/패시브 구분 텍스트), `_damageText`(액티브 스킬만 표시), `_selectButton`만 존재
- **"New!" 라벨, 희귀도 표시 관련 필드가 전혀 없음**
- `SkillData.cs`(`Assets/_Project/Scripts/Data/SkillData.cs`)를 확인한 결과 `_skillId`, `_skillName`, `_icon`, `_description`, `_skillType`, `_levels`(SkillLevelData 3단계), `_currentLevel` 필드만 존재하며, **희귀도(rarity)를 나타내는 필드가 전혀 없음**을 확인

`Assets/_Project/Scripts/UI/SkillSlotGroup.cs` / `SkillSlotIcon.cs`

- 액티브 4슬롯/패시브 2슬롯 각각에 대해 장착된 스킬이 있으면 아이콘 + `"x{level}"` 텍스트(`SetFilled`), 없으면 빈 슬롯(`SetEmpty`) 표시 로직이 이미 구현되어 있음. 이 부분은 이번 개편 대상이 아니며 그대로 재사용 가능함.

`Assets/_Project/Scripts/Skill/SkillManager.cs`

- `EquipActiveSkill`: 기존 스킬이면 `LevelUp()`만 하고 `false` 반환(신규 장착 아님), 신규 스킬이고 4개 미만이면 추가 후 `true` 반환 — "신규 획득 여부"를 이 반환값 또는 `ActiveSkillIds`/`PassiveSkillIds` 포함 여부로 판별 가능함을 확인(단, 실제 판별 로직 설계는 plan.md에서 다룰 예정이며 이 문서에서는 가능성만 확인)

### 3. UIRules.md에 정의된 구조 대비 실제 구현

`Assets/_Project/Docs/UIRules.md` 1번 섹션(Canvas 구조)에는 다음과 같이 정의되어 있습니다.

```
Canvas_HUD (Sort Order: 10)
  ├─ TopBar   (스테이지명, %, 아이콘*)
  ├─ TopButtons (▶ 재생, ⏸ 일시정지)
  ├─ WaveBar  (진행바 + 웨이브 번호 배지)
  ├─ CharacterHP
  └─ CharacterXP
```

- `TopButtons`의 배속/Auto 버튼은 문서에 이미 "구현하지 않음"으로 명시되어 있음(35번째 줄)
- `TopBar`의 "아이콘"(보스 등장 아이콘)은 "장식용으로만 유지하거나 생략 가능"으로 이미 명시되어 있음(37번째 줄)
- `WaveBar`(진행바 + 웨이브 번호 배지)는 문서상 정의만 있고, 위 1번 항목에서 확인했듯 **실제 코드에는 대응하는 시각 컴포넌트가 전혀 없음** — 문서와 실구현 사이의 갭
- `Canvas_Panel` 목록에 `LevelUpPanel`/`BallLevelUpPanel`이 있으나, 실제로는 별도 스크립트 없이 `SkillSelectionPanel` 하나로 스킬 선택 UI가 구현되어 있음(문서-실구현 불일치, 이번 문서에서는 사실로만 기록하고 판단은 유보)
- 12번 섹션에서 "Best!" 추천 아이콘은 리소스가 없어 "완전히 제외한다"고 이미 명시되어 있음

### 4. 레퍼런스 이미지 관찰 (`Assets/_Project/Docs/targetUI/`, 총 10장)

- `KakaoTalk_20260703_115951058_01.jpg`, `_02.jpg`, `_03.jpg`: 실제 스테이지 1("1. 깊은 숲") 인게임 화면. 좌상단 재생/Auto 버튼, 상단에 `"1. 깊은 숲"` 텍스트 + 빨간 진행바(%) + 우측 끝 보스 아이콘, 그 아래 금색 반짝이는 바 + 상자 모양 배지(웨이브 번호), 우상단 일시정지 버튼, 하단 캐릭터 + 초록 HP바 구조
- 삼택지 카드 화면(나머지 이미지 중 포함): `New!` 라벨(신규 스킬 카드에만 표시), `Best!` 추천 아이콘(제외 확정), 아이콘/이름/데미지(별 배지)/설명, 하단 희귀도 다이아몬드 3개, `다시 뽑기` 버튼+광고 횟수(제외 확정), `융합` 표시(제외 확정)
- 액티브 4슬롯/패시브 2슬롯 요약 행이 삼택지 화면과 일시정지 화면 모두에 공통으로 나타나며, 슬롯에 `x1`/`x2` 또는 `Max` 표시가 있음(이 부분은 `SkillSlotGroup`/`SkillSlotIcon`으로 이미 구현되어 있어 개편 대상 아님)

### 5. 에셋 현황

`Assets/_Project/Sprites/` 하위에는 BallSkillIcon, Passive 아이콘, Character/Monster/Ball/Background 스프라이트만 존재합니다. **진행바, 웨이브 번호 배지, 카드 프레임, 희귀도 다이아몬드 등에 쓸 수 있는 전용 이미지 에셋은 전혀 없습니다.**

---

## 관련 파일 및 의존성

| 파일 | 역할 |
|------|------|
| `Assets/_Project/Scripts/UI/HUDPanel.cs` | HUD 텍스트(웨이브/진행률/스코어) 및 발사 준비 인디케이터 제어. 시각적 진행바 없음 |
| `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | 3택지 카드 오픈/랜덤 추출/스킬 적용 로직, 슬롯 그룹 갱신 |
| `Assets/_Project/Scripts/UI/SkillCardUI.cs` | 개별 카드 UI(아이콘/이름/설명/타입/데미지/선택 버튼) |
| `Assets/_Project/Scripts/UI/SkillSlotGroup.cs` | 액티브/패시브 슬롯 요약 그룹 갱신(개편 대상 아님, 재사용) |
| `Assets/_Project/Scripts/UI/SkillSlotIcon.cs` | 개별 슬롯 아이콘/레벨(`x{level}`)/빈 슬롯 표시(개편 대상 아님, 재사용) |
| `Assets/_Project/Scripts/Data/SkillData.cs` | 스킬 데이터(아이콘/이름/설명/타입/레벨). 희귀도 필드 없음 |
| `Assets/_Project/Scripts/Skill/SkillManager.cs` | 장착 스킬 목록/장착 가능 여부(`CanEquipActive`/`CanEquipPassive`) 관리, 신규 장착 여부 판별 가능성 확인 |
| `Assets/_Project/Scripts/Wave/WaveManager.cs` | 웨이브 시작/처치 수 변경/처치 조건 도달 이벤트 발행. 웨이브별 몬스터 수만 계산(스테이지 전체 진행률 아님) |
| `Assets/_Project/Scripts/Data/WaveTableData.cs` | 웨이브 테이블 데이터. 스테이지명 필드 없음 |
| `Assets/_Project/Docs/UIRules.md` | Canvas 구조(TopBar/TopButtons/WaveBar 등), 패널 표시/애니메이션/버튼 피드백 규칙 |
| `PurpleCow_클라이언트_채용과제.pdf` (레포 루트) | 3택지 스펙(처치 수 조건, 보유/미보유, 최대보유 시 업그레이드만, 랜덤 추출, 중복 없음), 배속/자동조준/보스/다시뽑기/융합 구현 제외 항목 |
| `Assets/_Project/Docs/targetUI/` (10장) | 원본 게임 실제 캡처. 특히 `KakaoTalk_20260703_115951058_01/02/03.jpg`가 스테이지 1 HUD 직접 참고 대상 |

---

## 문제점 / 구현 대상 파악

### A. HUD 진행도 파트

1. **시각적 진행바 부재**: `_waveText`/`_progressText`는 텍스트로만 존재하며, 원본 게임처럼 눈에 보이는 바(Image fillAmount 또는 Slider) 형태의 진행바가 전혀 없다. 레퍼런스는 상단에 빨간 %바, 그 아래 금색 웨이브 진행바 총 2개의 바를 사용하지만 현재 코드에는 대응 요소가 하나도 없다.
2. **"웨이브 N/전체" 텍스트 vs 스테이지명 표기 방식 불일치**: 현재 `_waveText`는 `"WAVE {n} / {total}"` 형식의 기능적 텍스트이지만, 레퍼런스는 `"1. 깊은 숲"`처럼 스테이지 번호+이름을 표기하는 방식을 쓴다. 이 스테이지명 데이터는 `WaveTableData`/`WaveEntry`에 필드 자체가 존재하지 않아 신규로 추가되어야 하는 데이터 갭이다.
3. **% 진행률의 의미 중복/불명확**: 현재 `_progressText`가 계산하는 %는 `WaveManager.OnMonsterCountChanged`가 전달하는 "현재 웨이브 내" remaining/total 기준이다. 그런데 레퍼런스에는 상단 %바(스테이지 전체 진행 뉘앙스)와 하단 웨이브 진행바(현재 웨이브 진행 뉘앙스)가 별도로 존재한다. 즉 지금의 % 계산 로직 하나만으로는 두 개의 서로 다른 바(TopBar % vs WaveBar)의 의미를 모두 충족시킬 수 없고, 각 바가 정확히 무엇을 표시할지 재정의가 필요하다.
4. **WaveBar 자체가 미구현**: UIRules.md에는 `WaveBar`(진행바 + 웨이브 번호 배지)가 이미 문서화되어 있으나 실제 코드/씬 오브젝트로 구현된 적이 없다. TopBar와 별개로 신규 구현이 필요하다.
5. **자체 추가 요소 존재**: `_scoreText`("처치: N")와 `_launchReadyIndicator`(발사 준비 인디케이터)는 원본 레퍼런스 어디에도 대응하는 요소가 없는, 이 프로젝트에서 자체적으로 추가했던 HUD 요소로 확인된다.
6. **에셋 부재**: 진행바/배지에 쓸 전용 스프라이트가 프로젝트에 전혀 없다(Unity 기본 Image/Slider로 대체할 필요가 있는 영역).

### B. 삼택지 카드 파트

1. **"New!" 라벨 필드 부재**: `SkillCardUI`에는 신규 스킬 여부를 표시할 텍스트/아이콘 필드가 없다. `BuildSkillCardPool()`의 보유(owned) 판별 로직은 이미 존재하므로, 이 정보를 카드 UI에 전달할 경로만 추가되면 되는 상태다(단, 필드 추가/연동 방식 자체는 plan.md에서 다룸).
2. **희귀도 다이아몬드 데이터 부재**: 레퍼런스 카드 하단에는 희귀도를 나타내는 다이아몬드 3개가 있으나, `SkillData`에는 관련 데이터 필드가 전혀 없다. PDF 스펙에도 희귀도 관련 언급이 없다.
3. **"Best!" 추천 아이콘**: UIRules.md 12번 섹션에서 이미 "리소스 없음, 완전히 제외"로 확정되어 있어 이번 조사에서도 별도 갭으로 취급하지 않는다.
4. **다시 뽑기 버튼/광고 횟수, 융합 표시**: PDF 공식 스펙에서 "선택지 다시뽑기"와 "융합 시스템"이 구현 제외 항목으로 명시되어 있어, 레퍼런스에 존재하더라도 이번 갭 분석 대상에서 제외한다.
5. **슬롯 요약 UI(SkillSlotGroup/SkillSlotIcon)**: 이미 구현되어 있고 레퍼런스와 기능적으로 일치하므로 이번 개편 대상이 아니다(그대로 유지).

---

## 결론

사용자와의 논의를 거쳐 아래 6가지 사항이 이번 task(HUD 진행도 개편 + 삼택지 카드 개편)의 구현 대상 범위로 확정되었습니다. 실제 구현 방법/단계는 이후 plan.md에서 다룹니다.

1. **에셋 전략 — 기능 먼저**: 진행바, 카드 프레임 등은 Unity 기본 Image/Slider로 색상/도형 수준으로 우선 구현한다. 스타일 에셋 제작(전용 스프라이트 등)은 이번 범위에 포함하지 않고 이후 별도 task로 design 에이전트에게 위임한다.
2. **스테이지 타이틀 방식 변경**: 레퍼런스 1스테이지("1. 깊은 숲") 스타일을 그대로 따른다. 기존 `"WAVE N/20"` 텍스트 표기 대신, 스테이지명 텍스트 + 그 아래 웨이브 % 진행바 + 우측 끝 장식용 보스 아이콘(장식 유지, PDF상 보스 미구현이므로 기능 없음) 구조를 채택한다.
3. **HUD 자체 추가 요소 제거**: `_scoreText`("처치: N")와 `_launchReadyIndicator`(발사 준비 인디케이터) 둘 다 레퍼런스에 없는 이 프로젝트 자체 추가 요소이므로 제거 대상으로 확정한다.
4. **삼택지 카드 장식 — New! 포함, 희귀도 제외**: `New!` 라벨은 신규(미보유) 스킬 카드에만 표시하고 업그레이드(보유) 카드에는 표시하지 않는 방식으로 구현 대상에 포함한다. 하단 희귀도 다이아몬드는 PDF 스펙에 없고 `SkillData`에 관련 데이터 필드도 없으므로 이번 범위에서 제외한다.
5. **WaveBar 신규 구현**: UIRules.md에 정의만 되어 있고 실제 구현이 없던 `WaveBar`(웨이브 진행 상황을 나타내는 두 번째 바 + 웨이브 번호 배지)를 TopBar(스테이지명 + % 진행바)와 별도로 이번 작업의 구현 대상에 포함한다.
6. **문서 통합 범위**: 이번 task는 HUD 진행도 개편과 삼택지 카드 개편(New! 라벨 추가 포함) 두 가지를 하나의 research.md/plan.md로 통합하여 다룬다.
