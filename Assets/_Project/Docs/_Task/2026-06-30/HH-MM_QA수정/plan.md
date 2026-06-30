# Plan — QA 수정

이 문서는 QA 검토 결과 중 논의가 완료된 항목에 대한 구현 계획을 기술합니다.
현재는 CRITICAL 2(WaveData MonsterData 미반영), CRITICAL 3(스킬 선택 중 게임 일시정지 처리) 두 건의 수정 계획이 확정되었으며, 나머지 항목은 논의 완료 후 순차적으로 STEP으로 추가될 예정입니다.

---

## 구현 목표

WaveManager.SpawnWave()에서 몬스터 스폰 직후 WaveData에 설정된 MonsterData를 실제로 주입하여, 20웨이브에 걸쳐 다양한 몬스터 스탯과 보상이 정상 반영되도록 한다.

---

## 단계별 작업 계획

### STEP 1 — CRITICAL 2: WaveData MonsterData 미반영 수정

**배경**

ObjectPool.Get()은 내부적으로 OnSpawn()을 호출하여 프리팹에 기본 설정된 MonsterData로 몬스터를 초기화한다. 이후 별도로 올바른 MonsterData를 덮어쓰지 않으면 WaveData에 지정된 스탯/보상/종류가 무시된다.

**수정 파일 1: `Assets/_Project/Scripts/Monster/MonsterBase.cs`**

- `public void ApplyData(MonsterData data)` 메서드를 추가한다.
- 메서드 내부 처리 순서:
  1. `_monsterData = data` — 내부 데이터 교체
  2. `_currentHp = _monsterData.Hp` — HP 재초기화
  3. `OnHpChanged?.Invoke(_currentHp, _monsterData.Hp)` — HP 변경 이벤트 발행

**수정 파일 2: `Assets/_Project/Scripts/Wave/WaveManager.cs`**

- `SpawnWave()` 메서드에서 `_monsterPool.Get()` 직후에 아래 조건부 호출을 추가한다.

```
if (entry.Data != null)
    monster.ApplyData(entry.Data);
```

- 기존 Get() 이후 코드 흐름은 변경하지 않는다.

---

### STEP 2 — CRITICAL 3: 스킬 선택 중 게임 일시정지 처리

**배경**

SkillSelectionPanel은 인게임 도중 열리기 때문에 패널이 열려 있는 동안 게임 로직(몬스터 이동, 공격 등)이 계속 진행되는 문제가 있다. ResultPanel과 HUDPanel은 게임이 이미 멈춘 상태(Result/Ready)에서 열리므로 해당 처리가 불필요하지만, SkillSelectionPanel만 이 처리가 필요하다.

**수정 파일 1: `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`**

- `OpenPanel()` 메서드에 `Time.timeScale = 0f` 를 추가하여 패널 열림과 동시에 게임을 일시정지한다.
- `Show()` 및 `Hide()` 메서드의 DOTween Sequence에 `.SetUpdate(true)` 를 추가한다.
  - `Time.timeScale = 0`일 때 DOTween 기본 설정은 scaled time을 따르므로 애니메이션이 멈춘다. `.SetUpdate(true)`를 적용하면 unscaled time 기준으로 재생되어 timeScale에 무관하게 UI 애니메이션이 정상 동작한다.

**수정 파일 2: `Assets/_Project/Scripts/UI/UIManager.cs`**

- `OnSkillSelectionComplete()` 메서드에 `Time.timeScale = 1f` 를 추가하여 스킬 선택 완료 시 게임을 재개한다.

---

## 예상 변경/생성 파일 목록

| 구분 | 파일 | 변경 내용 |
|------|------|-----------|
| 수정 | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | `ApplyData(MonsterData data)` 메서드 추가 |
| 수정 | `Assets/_Project/Scripts/Wave/WaveManager.cs` | `SpawnWave()` 내 `ApplyData()` 호출 추가 |
| 수정 | `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs` | `OpenPanel()`에 `Time.timeScale = 0f` 추가, `Show()`/`Hide()` Sequence에 `.SetUpdate(true)` 추가 |
| 수정 | `Assets/_Project/Scripts/UI/UIManager.cs` | `OnSkillSelectionComplete()`에 `Time.timeScale = 1f` 추가 |

---

## 주의사항

- `ApplyData()`는 `OnSpawn()` 이후에 호출되므로, `OnSpawn()` 내부의 초기화 로직과 충돌하지 않는지 확인한다.
- `entry.Data != null` 조건을 반드시 포함하여 MonsterData가 지정되지 않은 항목은 프리팹 기본값을 유지하도록 한다.
- HP 재초기화 후 `OnHpChanged` 이벤트를 발행해야 HP UI(체력바 등)가 즉시 갱신된다.

---

## 논의 예정 항목

아래 항목들은 수정 방향이 아직 확정되지 않았습니다. 논의 완료 후 각 항목이 STEP으로 이 plan.md에 추가될 예정입니다.

- **CRITICAL 4:** 재시작 초기화 미구현 (RestartGame)
- **WARNING 2:** 패시브 스킬 레벨업 시 이벤트 이중 구독
- **WARNING 3:** MonsterBase 빈 이벤트 핸들러 불필요 구독
- **WARNING 4:** SkillSelectionPanel Hide() 이중 호출
- **WARNING 6:** OnEnable에서 Singleton Instance 직접 접근
- **INFO 2:** WaveData 20개 미생성
- **INFO 3:** DamageTextManager Ball.OnHitMonster 미연결 (시그니처 변경 필요)
