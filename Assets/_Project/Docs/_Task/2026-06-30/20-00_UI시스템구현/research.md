# Research — UI 시스템 구현

이 문서는 PurpleCow 프로젝트의 UI 시스템 구현을 위한 현재 상태를 분석합니다.
현재 프로젝트에는 UI 관련 스크립트가 전혀 존재하지 않으며, 게임 상태 전환·웨이브·스킬 관련 이벤트가 이미 각 시스템에 정의되어 있어 UI가 이를 구독하는 방식으로 설계하면 됩니다.
PDF 채용과제 문서에서 요구하는 HUD(현재 웨이브, 점수), Result 화면(성공/실패, 최종 점수, 재시작), 스킬 선택 UI(웨이브 클리어 후 카드 3장 선택) 구성을 목표로 합니다.

---

## 현재 상태

### UI 폴더 현황
- `Assets/_Project/Scripts/UI/` 폴더 자체가 존재하지 않음
- UI 관련 스크립트 없음
- UIManager 없음

### 기존 시스템에서 UI가 구독할 수 있는 이벤트 목록

| 이벤트 | 발행 클래스 | 시그니처 | 의미 |
|--------|------------|---------|------|
| `OnGameStateChanged` | `GameManager` | `Action<GameManager.GameState>` | Ready / Playing / Result 전환 |
| `OnWaveStarted` | `WaveManager` | `static Action<int>` | 웨이브 번호 전달 |
| `OnAllWavesCleared` | `WaveManager` | `static Action` | 모든 웨이브 클리어 |
| `OnAllBallsReturned` | `BallLauncher` | `static Action` | 발사한 볼이 모두 회수됨 |
| `OnMonsterDied` | `MonsterBase` | `static Action<MonsterBase>` | 몬스터 사망 (점수 계산용) |
| `OnHitMonster` | `Ball` | `static Action<float, bool>` | 데미지·크리티컬 여부 (데미지 팝업용) |
| `OnActiveSkillChanged` | `SkillManager` | `static Action<BallSkillBase>` | 액티브 스킬 장착 변경 |
| `OnPassiveSkillsChanged` | `SkillManager` | `static Action<List<PassiveSkillBase>>` | 패시브 스킬 목록 변경 |

### GameManager 상태 머신

```
Ready → Playing → Result
  ↑                  |
  └── RestartGame() ←┘
```

- `GameState.Ready` : 게임 시작 전 대기
- `GameState.Playing` : 플레이 중 (HUD 표시)
- `GameState.Result` : 게임 종료 (ResultPanel 표시)

### WaveManager 흐름
- `Start()` 시 첫 웨이브 스폰
- 볼이 모두 회수(`OnAllBallsReturned`) → 몬스터 이동 → 게임오버 체크
- 모든 몬스터 처치 → `AdvanceToNextWave()` → 다음 웨이브 스폰 또는 `OnAllWavesCleared`
- `_waveDatas.Length` 로 총 웨이브 수 파악 가능

### SkillManager
- 액티브 스킬 1개 + 패시브 스킬 다수 보유
- `SkillData` : `SkillId`, `SkillName`, `Icon(Sprite)`, `Description`, `SkillType`
- 스킬 선택 UI에서 `EquipActiveSkill()` / `AddPassiveSkill()` 호출 필요

### BallSkillBase / PassiveSkillBase
- `BallSkillBase` : MonoBehaviour, `SkillData` 프로퍼티 보유
- `PassiveSkillBase` : 순수 C# 클래스, `SkillData` 프로퍼티 보유

---

## 관련 파일 및 의존성

```
GameManager.cs         ← UIManager가 OnGameStateChanged 구독
WaveManager.cs         ← HUDPanel이 OnWaveStarted 구독, UIManager가 OnAllWavesCleared 구독
BallLauncher.cs        ← HUDPanel이 OnAllBallsReturned 구독 (발사 가능 여부 표시)
MonsterBase.cs         ← HUDPanel이 OnMonsterDied 구독 (점수 계산)
Ball.cs                ← OnHitMonster 구독 (데미지 팝업 — 선택 구현)
SkillManager.cs        ← SkillSelectionPanel이 EquipActiveSkill/AddPassiveSkill 호출
SkillData.cs           ← SkillSelectionPanel의 카드 표시 데이터 소스
```

---

## 문제점 / 구현 대상 파악

### 1. UIManager 부재
- 패널 간 전환 로직이 없음
- `GameManager.OnGameStateChanged` 이벤트를 수신하여 HUD ↔ Result 패널을 전환할 중앙 관리자 필요

### 2. HUD 없음
- 현재 웨이브 번호 표시 없음
- 점수(처치한 몬스터 수 기반) 표시 없음
- 발사 가능 여부 피드백 없음

### 3. ResultPanel 없음
- 게임 종료 시 성공/실패 메시지 없음
- 최종 점수 표시 없음
- 재시작 버튼(`GameManager.RestartGame()`) 없음

### 4. SkillSelectionPanel 없음
- 웨이브 클리어 후 스킬 선택 흐름 없음
- 선택 중 게임 일시정지(볼 발사 불가) 처리 없음
- `SkillData` 3장을 무작위로 제시하는 로직 없음

### 5. 점수 시스템 없음
- 처치한 몬스터 수를 누적하는 런타임 카운터가 없음
- UIManager 또는 별도 ScoreManager에서 `MonsterBase.OnMonsterDied` 구독으로 처리 가능
- 단순성 원칙에 따라 UIManager 내부에서 관리

---

## 결론

- UI 관련 스크립트 디렉터리(`Assets/_Project/Scripts/UI/`)를 신규 생성해야 함
- 구현 대상: `UIManager`, `HUDPanel`, `ResultPanel`, `SkillSelectionPanel`, `SkillCardUI`(카드 1장 단위 컴포넌트)
- 모든 UI 클래스는 기존 시스템 이벤트를 구독하는 방식으로 구현 — 기존 코드 수정 최소화
- `WaveManager`에 `OnWaveCleared` 이벤트 추가 또는 `OnAllWavesCleared` 이전 단계에서 스킬 선택 패널을 호출하는 연결 지점 필요
- 점수는 `MonsterBase.OnMonsterDied` 카운트로 단순 계산
