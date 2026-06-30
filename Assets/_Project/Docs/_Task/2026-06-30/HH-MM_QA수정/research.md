# Research — QA 수정

이 문서는 QA 에이전트가 수행한 코드 검토 결과를 구조화한 분석 문서입니다.
CRITICAL 5건, WARNING 6건, INFO 3건 총 14개 항목을 대상으로 각 문제의 원인, 영향 범위, 관련 파일을 정리합니다.
이 내용을 바탕으로 plan.md에서 수정 우선순위와 구체적인 구현 계획을 수립합니다.

---

## 현재 상태

QA 검토 결과 총 14개 항목이 식별되었습니다.

- CRITICAL 5건: 즉시 수정 필요. 스펙 불충족 또는 게임플레이 핵심 기능 오작동
- WARNING 6건: 잠재적 버그 또는 불필요한 코드로 인한 부작용 위험
- INFO 3건: 누락된 데이터 초기화 및 미연결 기능

---

## 관련 파일 및 의존성

| 파일 | 연관 항목 |
|------|-----------|
| `Assets/_Project/Scripts/Data/SkillData.cs` | CRITICAL 1 |
| `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | CRITICAL 1, CRITICAL 3, WARNING 4 |
| `Assets/_Project/Scripts/Wave/WaveManager.cs` | CRITICAL 2 |
| `Assets/_Project/Scripts/Core/GameManager.cs` | CRITICAL 4 |
| `Assets/_Project/Scripts/Skill/SkillManager.cs` | CRITICAL 5, WARNING 2 |
| `Assets/_Project/Scripts/Core/CharacterManager.cs` | WARNING 1 |
| `Assets/_Project/Scripts/Monster/MonsterBase.cs` | WARNING 3 |
| `Assets/_Project/Scripts/Ball/BallLauncher.cs` | WARNING 5, WARNING 6 |
| `Assets/_Project/Scripts/UI/ResultPanel.cs` | WARNING 6 |
| `Assets/_Project/Scripts/UI/DamageTextManager.cs` | INFO 3 |
| `Assets/_Project/Scripts/Data/BallData.cs` | INFO 1 |
| `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` | INFO 1 |
| `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` | INFO 2 |
| 패시브 스킬 5종 (`MagicMirrorPassive.cs`, `AmethystDaggerPassive.cs`, `EmeraldDaggerPassive.cs`, `LastMatchPassive.cs`, `WarmTinHeartPassive.cs`) | WARNING 2 |

---

## 문제점 / 구현 대상 파악

### CRITICAL 1 — 스킬 Lv3 도달 불가

- **파일:** `SkillData.cs` (LevelUp 메서드), `SkillSelectionPanel.cs` (업그레이드 조건)
- **원인:** `LevelUp()` 조건이 `_currentLevel < MaxLevel - 1`로 되어 있어 index 기준 최대값이 1(Lv2)에 멈춤. MaxLevel이 3일 때 index 2(Lv3)에 진입 불가
- **PDF 스펙:** 스킬 Lv1/Lv2/Lv3 3단계 명시
- **영향:** 스킬이 최고 레벨에 도달하지 못해 스펙 미충족
- **수정 방향:** `_currentLevel < MaxLevel - 1` → `_currentLevel < MaxLevel`로 변경. `SkillSelectionPanel.cs`의 동일 조건도 함께 수정. off-by-one 경계(index vs count) 재확인 필요

---

### CRITICAL 2 — WaveData MonsterData 미반영

- **파일:** `WaveManager.cs` (SpawnWave 메서드, L62 근방)
- **원인:** `WaveData.MonsterSpawnEntry`에 `Data` 필드(MonsterData)가 정의되어 있으나 `SpawnWave()`에서 `entry.Data`를 MonsterBase에 주입하는 코드가 없음. 결과적으로 프리팹에 기본 설정된 MonsterData만 사용됨
- **영향:** 20웨이브에 걸친 몬스터 스탯/보상/종류 다양화 로직이 완전히 무효화됨
- **수정 방향:** SpawnWave() 내 스폰 직후 `monster.Initialize(entry.Data)` 또는 `monster.Data = entry.Data` 형태로 MonsterData를 주입하는 코드 추가

---

### CRITICAL 3 — 스킬 선택 중 게임 진행 (Time.timeScale 미처리)

- **파일:** `SkillSelectionPanel.cs` (OpenPanel, UIManager.OnSkillSelectionComplete)
- **원인:** `OpenPanel()` 호출 시 `Time.timeScale = 0f` 설정 없음. 선택 완료 시 `Time.timeScale = 1f` 복원도 없음
- **영향:** 스킬 선택 UI가 열려 있는 동안 볼과 몬스터가 계속 이동하여 게임플레이가 중단 없이 진행됨
- **수정 방향:** `OpenPanel()`에 `Time.timeScale = 0f` 추가, `UIManager.OnSkillSelectionComplete()`에 `Time.timeScale = 1f` 추가

---

### CRITICAL 4 — 재시작 초기화 미구현

- **파일:** `GameManager.cs` (RestartGame, L27~31)
- **원인:** `RestartGame()`이 `GameState = Ready` 변경과 이벤트 발행만 수행. WaveManager 웨이브 인덱스, CharacterManager HP/XP, SkillManager 스킬 목록, 스폰된 몬스터 풀 등 각 시스템의 상태 초기화가 없음
- **PDF 스펙:** "결과 팝업에서 1스테이지 재시작 가능" 필수 항목
- **영향:** 재시작 시 이전 웨이브/스탯/스킬 상태가 그대로 남아 1스테이지부터 정상 재시작 불가
- **수정 방향:** 씬 재로드(`SceneManager.LoadScene`) 방식 또는 각 시스템에 `Reset()`/`Initialize()` 메서드를 추가하고 RestartGame에서 순서대로 호출하는 방식 중 선택 필요

---

### CRITICAL 5 — 스킬 레벨업 시 Ball 내 인스턴스 공유 문제

- **파일:** `SkillManager.cs` (EquipActiveSkill, L27~28)
- **원인:** 레벨업 시 `existing.SkillData.LevelUp()` 호출 후 기존 BallSkillBase 인스턴스를 교체하지 않음. `ApplySkillToBall()`에서 동일 인스턴스를 여러 Ball이 참조
- **영향:** GhostBallSkill 등 상태(OnActivate/OnDeactivate)가 있는 스킬의 경우 한 Ball 조작이 다른 Ball 상태에 영향을 미침
- **수정 방향:** 레벨업 후 각 Ball에 대해 기존 스킬 인스턴스를 제거하고 새 인스턴스를 생성하여 재적용

---

### WARNING 1 — XP 중복 지급 위험

- **파일:** `CharacterManager.cs` (L37~45)
- **원인:** `HandleMonsterReachedBottom()`과 `HandleMonsterDied()` 모두 `AddXp(monster.Data.Reward)` 호출. `_isDead` 가드가 있으나 DoT 코루틴 타이밍에 따라 `OnDespawn()` 이전에 `Die()`가 트리거되면 이중 지급 가능
- **현재 상태:** 일반 흐름에서는 안전. DoT 동시 작동 시 잠재적 위험
- **수정 방향:** XP 지급 처리를 단일 경로로 통합하거나, 지급 여부를 별도 플래그로 관리

---

### WARNING 2 — 패시브 스킬 레벨업 시 이벤트 이중 구독

- **파일:** 패시브 5종, `SkillManager.cs` (AddPassiveSkill, L39)
- **원인:** 레벨업 시 `existing.Apply()` 재호출 전 `existing.Remove()`를 호출하지 않아 동일 핸들러가 이벤트에 2회 이상 등록됨
- **영향:** 마법 거울 등 이벤트 기반 패시브 효과가 레벨업할수록 배수로 중복 적용됨
- **수정 방향:** `AddPassiveSkill()`에서 레벨업 분기 시 `existing.Remove()` → `LevelUp()` → `existing.Apply()` 순서로 처리

---

### WARNING 3 — MonsterBase 빈 이벤트 핸들러 불필요 구독

- **파일:** `MonsterBase.cs` (OnEnable, L24~35)
- **원인:** `Ball.OnHitMonster`를 구독하는 `HandleHitMonster` 핸들러 내부가 비어 있음. 실제 데미지 처리는 `Ball.CalculateDamage()`에서 직접 `target.TakeDamage()` 호출
- **영향:** 활성 몬스터 수만큼 빈 델리게이트 호출이 매 타격마다 발생 (성능 낭비)
- **수정 방향:** `OnEnable`/`OnDisable`에서 해당 구독/해제 코드와 빈 핸들러 메서드 제거

---

### WARNING 4 — SkillSelectionPanel Hide() 이중 호출

- **파일:** `SkillSelectionPanel.cs` (OnSkillSelected, L87~92)
- **원인:** `OnSkillSelected()`에서 `UIManager.Instance.OnSkillSelectionComplete()` + `Hide()` 모두 호출. `OnSkillSelectionComplete()` 내부에서도 `ShowSkillSelection(false)` → `Hide()` 재호출
- **영향:** DOTween Sequence 두 개가 동시 실행되어 `gameObject.SetActive(false)` 경합 발생 가능
- **수정 방향:** `OnSkillSelected()`에서 직접 `Hide()` 호출 제거. `UIManager.OnSkillSelectionComplete()` 하나의 경로로 통일

---

### WARNING 5 — ClusterBall 서브볼 발사 방향 불확실

- **파일:** `BallLauncher.cs` (LaunchSubBalls, L68)
- **원인:** `if (randomDir.y < 0) randomDir.y = -randomDir.y`로 y를 항상 양수 강제. 씬 레이아웃에서 몬스터가 위쪽에 있다면 의도적이나, 반대 구성이라면 서브볼이 항상 몬스터 반대 방향으로 발사됨
- **영향:** ClusterBall 스킬 효과가 의도한 방향으로 동작하지 않을 수 있음
- **수정 방향:** 실제 씬 레이아웃(몬스터 위치 기준 방향) 확인 후 부호 결정. 필요 시 `randomDir.y = Mathf.Abs(randomDir.y)` 또는 `-Mathf.Abs(randomDir.y)`로 수정

---

### WARNING 6 — OnEnable에서 Singleton Instance 직접 접근

- **파일:** `BallLauncher.cs` (OnEnable, L24~35), `ResultPanel.cs` (OnEnable, L25~27)
- **원인:** `OnEnable()`에서 `InputHandler.Instance`, `GameManager.Instance` 등 싱글톤에 직접 접근. 씬 초기화 순서에 따라 해당 싱글톤이 아직 생성되지 않았을 수 있음
- **영향:** NullReferenceException 발생 가능
- **수정 방향:** null 체크 추가 또는 static event 방식으로 전환하여 싱글톤 의존 제거

---

### INFO 1 — BallData 기본값 코드 보장 없음

- **파일:** `BallData.cs`, `BallSetupEditor.cs`
- **내용:** PDF 기본값(damage=8, critChance=0, critMultiplier=1.5)이 `BallSetupEditor`에만 존재. `BallData.cs` 필드에 기본값 초기화 코드 없음
- **영향:** Setup 에디터 메뉴를 실행하지 않으면 모든 필드가 0으로 초기화됨
- **수정 방향:** `BallData.cs` 필드 선언부에 기본값 직접 지정 또는 `Reset()` 메서드 추가

---

### INFO 2 — WaveData 1개만 생성 (20웨이브 미달)

- **파일:** `MonsterSetupEditor.cs` (L76~91)
- **내용:** 에디터 스크립트에서 `WaveData_Wave1.asset` 1개만 생성. PDF 스펙은 총 20웨이브 요구
- **영향:** 현재 실행 시 1웨이브 후 즉시 클리어 처리됨
- **수정 방향:** Wave2~Wave20 에셋 수동 생성 또는 에디터 스크립트에 반복 생성 로직 추가

---

### INFO 3 — DamageTextManager Ball.OnHitMonster 미연결

- **파일:** `DamageTextManager.cs`
- **내용:** `Ball.OnHitMonster(MonsterBase, float, bool)` 이벤트를 구독하여 `ShowDamage()`를 호출하는 코드가 어디에도 없음
- **영향:** 게임 중 데미지 숫자 텍스트가 전혀 표시되지 않음
- **수정 방향:** `DamageTextManager`의 `OnEnable()` 또는 초기화 메서드에서 `Ball.OnHitMonster += HandleHitMonster` 구독 추가

---

## 결론

CRITICAL 5건은 PDF 스펙 미충족 및 핵심 게임플레이 오작동에 해당하므로 최우선 수정이 필요합니다.

- **CRITICAL 1, 3**은 수정 범위가 좁고 명확하여 즉시 처리 가능합니다.
- **CRITICAL 2, 4, 5**는 설계 결정(씬 재로드 vs 개별 Reset, 인스턴스 공유 해소 방식)이 필요하므로 plan.md에서 구체적인 접근법을 확정해야 합니다.

WARNING 중 **WARNING 2(패시브 이중 구독)**는 스킬 레벨업 시 즉각적인 효과 이상을 유발하므로 CRITICAL에 준하여 처리를 권장합니다.

INFO 항목은 기능 완성도 측면의 항목으로, CRITICAL·WARNING 수정 이후 처리합니다.
