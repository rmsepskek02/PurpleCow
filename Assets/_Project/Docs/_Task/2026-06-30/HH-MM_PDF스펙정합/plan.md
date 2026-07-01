# Plan — PDF 스펙 정합

이 문서는 PDF 요구사항에 맞게 기존 구현을 수정하는 전체 계획을 기술한다. BallData 기본값 교정, SkillData 레벨 시스템 도입, 액티브 스킬 5종 로직 변경, 패시브 7종 → 5종 전면 교체, 스킬 장착 제한, 스킬 선택 로직 개선, MonsterData 이름 정합의 7개 항목을 순서대로 구현한다.

---

## 구현 목표

1. BallData 기본값을 damage 8 / criticalChance 0 / criticalMultiplier 1.5로 수정
2. SkillData에 레벨(Lv.1/2/3) 구조 추가 — 레벨별 수치와 볼 데미지 보관
3. 액티브 스킬 5종 로직을 PDF 효과 기준으로 수정
4. 패시브 기존 7종 삭제, 신규 5종 구현
5. SkillManager에 액티브 최대 4개 / 패시브 최대 2개 장착 제한 추가
6. 스킬 선택 로직: 처치 수 조건 + 보유 여부/레벨 기반 카드 필터링
7. MonsterSetupEditor 이름 배열 수정 (Spike → Spider, Blaze → StoneBug, Stone → ForestDeer)

---

## 단계별 작업 계획

---

### STEP 1 — SkillData 레벨 구조 설계

**설계 방침**

레벨별 수치를 직렬화 가능한 중첩 구조체 `SkillLevelData`로 묶어 `SkillData` 안에 길이 3의 배열로 보관한다. 기존 `_value1 / _value2 / _value3`는 제거하고 `_levels` 배열로 대체한다.

**수정 파일: `Assets/_Project/Scripts/Data/SkillData.cs`**

변경 내용:
- `[System.Serializable] public struct SkillLevelData` 추가
  - `float BallDamage` — 레벨별 볼 데미지
  - `float Value1 / Value2 / Value3` — 레벨별 스킬 수치
- `SkillData`에 `[SerializeField] private SkillLevelData[] _levels` 추가 (크기 3 — [0]=Lv1, [1]=Lv2, [2]=Lv3)
- `SkillData`에 `[SerializeField] private int _currentLevel` 추가 (0-based: 0=Lv1)
- 기존 `_value1 / _value2 / _value3` 필드 제거
- 프로퍼티 추가:
  - `public int CurrentLevel => _currentLevel;`
  - `public int MaxLevel => _levels.Length;`
  - `public SkillLevelData GetLevelData(int level)` — 범위 클램핑 포함
  - `public SkillLevelData CurrentLevelData => GetLevelData(_currentLevel);`
  - `public void LevelUp()` — `_currentLevel` 증가 (MaxLevel-1 초과 불가)

**PassiveSkillId 열거형 재정의 (같은 파일 내)**

기존:
```
DamageUp, CritChanceUp, CritDamageUp, SpeedUp, BounceUp, KillShot, LastHit
```

변경 후:
```
WarmTinHeart   = 3001,
MagicMirror    = 3002,
AmethystDagger = 3003,
EmeraldDagger  = 3004,
LastMatch      = 3005
```

---

### STEP 2 — BallData 기본값 수정

**수정 파일: `Assets/_Project/Scripts/Editor/BallSetupEditor.cs`**

`CreateBallDataAsset()` 내 SerializedObject 세팅 값 변경:
- `_damage`: 10f → **8f**
- `_criticalChance`: 0.1f → **0f**
- `_criticalMultiplier`: 2f → **1.5f**

> 이미 생성된 에셋(`Assets/_Project/Data/BallData.asset`)이 존재하면 에디터 메뉴로 재생성할 수 없으므로, Inspector에서 직접 수정하거나 에셋을 삭제 후 재실행하는 방법을 주석으로 안내한다.

---

### STEP 3 — BallSkillBase / PassiveSkillBase 베이스 변경

**수정 파일: `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs`**

- 기존 `SkillData _skillData` 외에 현재 레벨을 가져오는 편의 프로퍼티 추가:
  - `protected SkillLevelData LevelData => _skillData.CurrentLevelData;`

**수정 파일: `Assets/_Project/Scripts/Skill/Base/PassiveSkillBase.cs`**

- 동일하게 `protected SkillLevelData LevelData => _skillData.CurrentLevelData;` 추가

---

### STEP 4 — MonsterBase 상태이상 확장

**수정 파일: `Assets/_Project/Scripts/Monster/MonsterBase.cs`**

신규 필드:
- `private float _slowDuration` — 이동속도 감소 남은 시간(초)
- `private float _slowPercent` — 이동속도 감소율 (0~1)
- `private float _bonusCritChance` — 다음 1회 치명타 확률 보너스
- `private bool _hasBonusCrit` — 보너스 치명타 적용 여부 플래그

신규 메서드:
- `public void ApplySlow(float duration, float percent)` — 슬로우 적용
- `public void ApplyBonusCritChance(float bonus)` — 치명타 확률 1회 부여
- `public float ConsumeBonusCritChance()` — 보너스 치명타 소비 후 반환 (없으면 0)
- `public void ApplyDot(float damagePerSec, float duration, int maxStacks)` — DOT 적용 (코루틴)
- `private IEnumerator CoDotTick(float dps, float duration, int stacks)` — DOT 틱 처리

기존 `ApplyFreeze(int turns)` 유지, DOT/Slow는 초 단위 시간 기반으로 추가.

`MoveDown(float distance)` 수정:
- Frozen 확인 후 SlowPercent 반영: `distance *= (1f - _slowPercent)` 후 이동
- 슬로우 시간 감소: `_slowDuration -= Time.deltaTime` (또는 턴 기반으로 설계한다면 턴마다 차감)

> 슬로우는 턴 기반 게임 흐름에 맞게 `MoveDown` 호출 시 지속시간을 1턴씩 차감하는 방식을 채택한다.

---

### STEP 5 — 액티브 스킬 5종 수정

#### 5-1. `FireBallSkill.cs` — 전면 수정

PDF 효과: 타격 시 DOT(화상) 중첩.
- Lv1: 지속 4초, 최대 중첩 3, 초당 8 피해
- Lv2: 지속 4.5초, 최대 중첩 4, 초당 10 피해
- Lv3: 지속 5초, 최대 중첩 5, 초당 12 피해
- 볼 데미지: 21 / 24 / 27

변경 내용:
- 기존 `OverlapCircle` 폭발 로직 제거
- `OnBallHit`에서 `LevelData`를 참조하여 `target.ApplyDot(dps, duration, maxStacks)` 호출
  - `Value1` = 지속시간, `Value2` = 최대 중첩, `Value3` = 초당 피해

#### 5-2. `IceBallSkill.cs` — 전면 수정

PDF 효과: 확률로 냉동 + 이동속도 감소 + 추가 피해.
- Lv1: 30%, 5초, -10%, +10%
- Lv2: 35%, 6초, -15%, +15%
- Lv3: 40%, 7초, -20%, +20%
- 볼 데미지: 25 / 37 / 50

변경 내용:
- 기존 `ApplyFreeze(턴수)` → 확률 판정 + 초 단위 프리즈 + 슬로우 + 추가 피해
- `OnBallHit`에서:
  - `Random.value < LevelData.Value1` (확률) 판정
  - 성공 시 `target.ApplyFreeze(duration초)` + `target.ApplySlow(duration, slowPercent)` + `target.TakeDamage(baseDamage * bonusRate)`
  - `Value1` = 확률(0.3~0.4), `Value2` = 지속시간, `Value3` = 슬로우율 (0.1~0.2)
  - 추가 피해율은 슬로우율과 동일 수치이므로 별도 value 없이 Value3 재사용

> ApplyFreeze는 기존 턴 기반이므로 초 기반으로 오버로드를 추가한다.

#### 5-3. `LaserBallSkill.cs` — 로직 변경

PDF 효과: 같은 행의 모든 적에게 추가 피해.
- 추가 피해: 7 / 11 / 15
- 볼 데미지: 11 / 15 / 19

변경 내용:
- 기존 `OnActivate`에서 RaycastAll 즉시 처리 + ForceReturn 제거
- `OnBallHit`에서 처리:
  - 타격한 몬스터의 Y좌표와 동일한 행에 있는 모든 활성 몬스터를 `WaveManager.Instance.GetMonstersInRow(target)` 로 획득
  - 획득한 몬스터들에게 `LevelData.Value1` 추가 피해
- `WaveManager`에 `GetMonstersInRow(MonsterBase reference)` 메서드 추가 필요

#### 5-4. `GhostBallSkill.cs` — 유지, 볼 데미지 수치만 레벨별 적용

PDF 효과: 관통 (효과 동일, 볼 데미지만 레벨별 다름).
- 볼 데미지: 14 / 21 / 28

변경 내용:
- 로직 자체 변경 없음
- SkillData 에셋의 레벨별 BallDamage 수치를 설정하는 것으로 처리
- 볼 데미지를 SkillData에서 읽어 Ball의 데미지를 오버라이드하는 구조가 필요 (STEP 8에서 Ball.cs 수정과 함께 처리)

#### 5-5. `ClusterBallSkill.cs` — 수정

PDF 효과: 확률로 서브볼 생성 + 서브볼 피해.
- Lv1: 40%, 서브볼 피해 10
- Lv2: 50%, 서브볼 피해 15
- Lv3: 60%, 서브볼 피해 20
- 볼 데미지: 27 / 30 / 33

변경 내용:
- `OnBallHit`에서:
  - `Random.value < LevelData.Value1` (확률) 판정
  - 성공 시 `BallLauncher.Instance.LaunchSubBalls(_ball.transform.position, 1, LevelData.Value2)` 호출
  - `Value1` = 확률(0.4~0.6), `Value2` = 서브볼 피해
- `BallLauncher.LaunchSubBalls`에 피해 파라미터 추가 필요 (시그니처 변경)
- `_hasExploded` 플래그 제거 — 확률 기반이므로 매 타격마다 판정

---

### STEP 6 — 패시브 기존 7종 삭제 / 신규 5종 생성

#### 삭제 파일 목록
- `DamageUpPassive.cs`
- `CritChanceUpPassive.cs`
- `CritDamageUpPassive.cs`
- `SpeedUpPassive.cs`
- `BounceUpPassive.cs`
- `KillShotPassive.cs`
- `LastHitPassive.cs`

#### 신규 생성 파일 5종

**6-1. `WarmTinHeartPassive.cs`** — 노멀 볼 추가 피해 20/30/40%

- `Apply()`: `SkillManager.Instance.AddDamageMultiplier(LevelData.Value1)`
- `Remove()`: `SkillManager.Instance.RemoveDamageMultiplier(LevelData.Value1)`
- Value1 = 0.2 / 0.3 / 0.4

**6-2. `MagicMirrorPassive.cs`** — 벽 타격마다 다음 공격 피해 20/40/60% 증가

- `Apply()`: `Ball.OnWallHit += HandleWallHit`
- `Remove()`: `Ball.OnWallHit -= HandleWallHit`
- `HandleWallHit()`: `SkillManager.Instance.AddNextShotDamageBonus(LevelData.Value1)` 후 즉시 소비 설계
- SkillManager에 `_nextShotDamageBonus` 필드 + Add/Consume 메서드 추가
- Ball.cs에 `public static event Action OnWallHit` 추가 (벽 충돌 시 발행)
- Value1 = 0.2 / 0.4 / 0.6

**6-3. `AmethystDaggerPassive.cs`** — 적 전면 타격 시 치명타 확률 +10/20/30% (1회)

- `Apply()`: `Ball.OnHitMonsterFront += HandleFrontHit`
- `Remove()`: `Ball.OnHitMonsterFront -= HandleFrontHit`
- `HandleFrontHit(MonsterBase target)`: `target.ApplyBonusCritChance(LevelData.Value1)`
- Ball.cs에 `public static event Action<MonsterBase> OnHitMonsterFront` 추가
  - 전면 판정: 볼 이동 방향 vs 몬스터 정면 방향 비교 (볼이 몬스터 앞에서 오면 전면)
- Value1 = 0.1 / 0.2 / 0.3

**6-4. `EmeraldDaggerPassive.cs`** — 적 후면 타격 시 치명타 확률 +20/30/40% (1회)

- `Apply()`: `Ball.OnHitMonsterBack += HandleBackHit`
- `Remove()`: `Ball.OnHitMonsterBack -= HandleBackHit`
- `HandleBackHit(MonsterBase target)`: `target.ApplyBonusCritChance(LevelData.Value1)`
- Ball.cs에 `public static event Action<MonsterBase> OnHitMonsterBack` 추가
- Value1 = 0.2 / 0.3 / 0.4

**6-5. `LastMatchPassive.cs`** — 적 사망 시 폭발, 근처 적에게 10/20/30 피해

- `Apply()`: `MonsterBase.OnMonsterDied += HandleMonsterDied`
- `Remove()`: `MonsterBase.OnMonsterDied -= HandleMonsterDied`
- `HandleMonsterDied(MonsterBase monster)`: `Physics2D.OverlapCircleAll` 후 근처 적에게 `LevelData.Value1` 피해
- Value1 = 10 / 20 / 30
- 폭발 반경은 `Value2`로 Inspector 설정 (별도 설계 필요 없으므로 SkillData 에셋에서 관리)

---

### STEP 7 — SkillManager 재설계

**수정 파일: `Assets/_Project/Scripts/Skill/SkillManager.cs`**

#### 7-1. 액티브 슬롯 단일 → 리스트 (최대 4개)

- `_equippedActiveSkill` → `private List<BallSkillBase> _activeSkills`
- `EquipActiveSkill(BallSkillBase skill)`:
  - 동일 SkillId 보유 여부 확인 → 있으면 `skill.SkillData.LevelUp()` 후 종료
  - 없으면 리스트에 추가 (최대 4개 초과 시 무시 또는 예외)
- `CanEquipActive` 프로퍼티: `_activeSkills.Count < 4`
- `ApplySkillToBall(Ball ball)`: 리스트 내 모든 스킬을 볼에 적용

#### 7-2. 패시브 최대 2개 제한

- `AddPassiveSkill(PassiveSkillBase skill)`:
  - 동일 SkillId 보유 여부 확인 → 있으면 `skill.SkillData.LevelUp()` 후 `Apply()` 재호출
  - 없으면 리스트 추가 (최대 2개 초과 시 무시)
- `CanEquipPassive` 프로퍼티: `_passiveSkills.Count < 2`

#### 7-3. 보너스 필드 재설계

제거:
- `_damageMultiplierBonus`, `_critChanceBonus`, `_critDamageBonus`, `_speedBonus`, `_bounceBonus`
- 관련 Add/Remove 메서드 (WarmTinHeart만 DamageMultiplier 재사용)

추가:
- `_damageMultiplierBonus` — WarmTinHeartPassive 전용 (재사용)
- `_nextShotDamageBonus` — MagicMirrorPassive 전용
- `public float ConsumeNextShotDamageBonus()` — 소비 후 0으로 초기화

제거된 필드에 의존하는 `Ball.cs`의 참조도 함께 정리 (SpeedBonus, BounceBonus, CritChanceBonus, CritDamageBonus는 신규 패시브에서 미사용).

---

### STEP 8 — Ball.cs 수정

**수정 파일: `Assets/_Project/Scripts/Ball/Ball.cs`**

변경 내용:
- `OnBeforeReturn` 이벤트 제거 (LastHitPassive 삭제됨)
- 신규 이벤트 추가:
  - `public static event Action OnWallHit` — MagicMirrorPassive 연동
  - `public static event Action<MonsterBase> OnHitMonsterFront` — AmethystDaggerPassive 연동
  - `public static event Action<MonsterBase> OnHitMonsterBack` — EmeraldDaggerPassive 연동
- `OnCollisionEnter2D`에서 "Wall" 충돌 시 `OnWallHit?.Invoke()` 추가
- `OnCollisionEnter2D`에서 몬스터 충돌 시 전면/후면 판정 후 이벤트 발행
  - 판정 방법: 볼 이동 방향 `_rigidbody.linearVelocity.normalized`와 몬스터 → 볼 방향 내적. 양수면 후면, 음수면 전면 (몬스터는 항상 아래를 향한다고 가정)
- `CalculateDamage()`에서 `target.ConsumeBonusCritChance()`를 치명타 확률에 더함
- `CalculateDamage()`에서 `SkillManager.Instance.ConsumeNextShotDamageBonus()`를 데미지 배율에 더함
- `SkillManager.Instance.SpeedBonus` / `BounceBonus` / `CritChanceBonus` / `CritDamageBonus` 참조 제거 (해당 패시브 삭제됨)

> `CalculateDamage`가 현재 MonsterBase를 인자로 받지 않으므로, 전면/후면 이벤트 발행과 ConsumeBonusCritChance를 위해 `CalculateDamage(MonsterBase target)` 시그니처로 변경한다.

---

### STEP 9 — SkillSelectionPanel + WaveManager 스킬 선택 로직 변경

#### 9-1. WaveManager.cs 수정

변경 내용:
- `[SerializeField] private int _killCountForSkill` 추가 (처치 수 조건)
- `_totalKillCount` 카운터 추가
- `HandleMonsterDied`에서 `_totalKillCount++` 후 `CheckSkillUnlock()` 호출
- `CheckSkillUnlock()`: `_totalKillCount % _killCountForSkill == 0` 이면 `OnKillCountReached` 이벤트 발행
- `public static event Action OnKillCountReached` 추가
- 기존 `CheckWaveCleared()` 내 `OnWaveCleared` 발행은 유지 (다음 웨이브 전환용으로만 사용)
- `GetMonstersInRow(MonsterBase reference)` 추가: `_activeMonsters`에서 Y좌표가 동일한 몬스터 목록 반환

#### 9-2. SkillSelectionPanel.cs 수정

변경 내용:
- `OnEnable`에서 이벤트 구독:
  - `WaveManager.OnKillCountReached += OpenPanel` (추가)
- `OnDisable`에서 구독 해제
- `ShowRandomSkills()` → `BuildSkillCardPool()` 로직 개선:

  ```
  1. 현재 보유 액티브 수 == 4 이면 액티브 카드 후보 = 보유 액티브 스킬의 업그레이드 선택지만
  2. 그렇지 않으면 액티브 후보 = 미보유 Lv.1 스킬
  3. 현재 보유 패시브 수 == 2 이면 패시브 카드 후보 = 보유 패시브 스킬의 업그레이드 선택지만
  4. 그렇지 않으면 패시브 후보 = 미보유 Lv.1 스킬
  5. 통합 후보 풀에서 3개 무작위 선택 (중복 없음)
  ```

- `_allSkillDatas` 배열을 액티브/패시브로 분리하여 Inspector에 노출하거나, SkillType으로 런타임 분류
- `SkillCardUI.Setup`에 레벨 정보 표시용 데이터 전달 (레벨 UI 표시는 별도 UI 작업 범위)

---

### STEP 10 — SkillFactory.cs 수정

**수정 파일: `Assets/_Project/Scripts/Skill/SkillFactory.cs`**

변경 내용:
- `CreatePassiveSkill` switch에서 기존 7개 항목 제거, 신규 5개 항목 추가:
  ```
  PassiveSkillId.WarmTinHeart   => new WarmTinHeartPassive(data),
  PassiveSkillId.MagicMirror    => new MagicMirrorPassive(data),
  PassiveSkillId.AmethystDagger => new AmethystDaggerPassive(data),
  PassiveSkillId.EmeraldDagger  => new EmeraldDaggerPassive(data),
  PassiveSkillId.LastMatch      => new LastMatchPassive(data),
  ```

---

### STEP 11 — SkillSetupEditor.cs 수정

**수정 파일: `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`**

변경 내용:
- `CreatePassiveSkillDataAssets()` 내 기존 7종 에셋 생성 코드 제거, 신규 5종으로 교체
- 각 에셋 파일명: `SkillData_Passive_WarmTinHeart.asset` 등 클래스명 기반으로 통일
- `CreateActiveSkillDataAssets()` 내 각 스킬의 레벨별 수치를 `_levels` 배열로 설정하도록 수정
  - `so.FindProperty("_levels").arraySize = 3` 후 각 인덱스별 BallDamage / Value1~3 설정
- `CreateSkillData` 헬퍼 메서드 시그니처를 레벨 배열을 받도록 변경

액티브 스킬 레벨별 수치 설정:

| 스킬 | Lv | BallDmg | Value1 | Value2 | Value3 |
|------|----|---------|--------|--------|--------|
| Fire | 1 | 21 | 4f (지속시간) | 3f (중첩수) | 8f (초당피해) |
| Fire | 2 | 24 | 4.5f | 4f | 10f |
| Fire | 3 | 27 | 5f | 5f | 12f |
| Ice  | 1 | 25 | 0.3f (확률) | 5f (지속) | 0.1f (슬로우율) |
| Ice  | 2 | 37 | 0.35f | 6f | 0.15f |
| Ice  | 3 | 50 | 0.4f | 7f | 0.2f |
| Laser| 1 | 11 | 7f (추가피해) | 0f | 0f |
| Laser| 2 | 15 | 11f | 0f | 0f |
| Laser| 3 | 19 | 15f | 0f | 0f |
| Ghost| 1 | 14 | 0f | 0f | 0f |
| Ghost| 2 | 21 | 0f | 0f | 0f |
| Ghost| 3 | 28 | 0f | 0f | 0f |
| Cluster| 1 | 27 | 0.4f (확률) | 10f (피해) | 0f |
| Cluster| 2 | 30 | 0.5f | 15f | 0f |
| Cluster| 3 | 33 | 0.6f | 20f | 0f |

패시브 스킬 레벨별 수치:

| 스킬 | Lv | Value1 | Value2 |
|------|----|--------|--------|
| WarmTinHeart | 1 | 0.2f | 0f |
| WarmTinHeart | 2 | 0.3f | 0f |
| WarmTinHeart | 3 | 0.4f | 0f |
| MagicMirror | 1 | 0.2f | 0f |
| MagicMirror | 2 | 0.4f | 0f |
| MagicMirror | 3 | 0.6f | 0f |
| AmethystDagger | 1 | 0.1f | 0f |
| AmethystDagger | 2 | 0.2f | 0f |
| AmethystDagger | 3 | 0.3f | 0f |
| EmeraldDagger | 1 | 0.2f | 0f |
| EmeraldDagger | 2 | 0.3f | 0f |
| EmeraldDagger | 3 | 0.4f | 0f |
| LastMatch | 1 | 10f | 폭발반경 | 
| LastMatch | 2 | 20f | 폭발반경 |
| LastMatch | 3 | 30f | 폭발반경 |

> LastMatch의 폭발 반경은 Value2에 저장하며 기본값 1.5f를 사용한다.

---

### STEP 12 — MonsterSetupEditor.cs 수정

**수정 파일: `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`**

`CreateMonsterDataAssets()` 내 이름 배열 변경:

```csharp
// 변경 전
string[] names = { "Fluffy", "Spike", "Blaze", "Stone" };

// 변경 후
string[] names = { "Fluffy", "Spider", "StoneBug", "ForestDeer" };
```

---

## 예상 변경/생성 파일 목록

### 수정 파일 (13개)

| 파일 | 변경 내용 요약 |
|------|--------------|
| `Data/SkillData.cs` | SkillLevelData 구조체, _levels 배열, PassiveSkillId 재정의 |
| `Editor/BallSetupEditor.cs` | BallData 기본값 수정 (damage/critChance/critMult) |
| `Skill/Base/BallSkillBase.cs` | LevelData 편의 프로퍼티 추가 |
| `Skill/Base/PassiveSkillBase.cs` | LevelData 편의 프로퍼티 추가 |
| `Skill/Active/FireBallSkill.cs` | DOT 중첩 로직으로 전면 교체 |
| `Skill/Active/IceBallSkill.cs` | 확률+슬로우+추가피해 로직으로 전면 교체 |
| `Skill/Active/LaserBallSkill.cs` | 같은 행 추가 피해 로직으로 변경 |
| `Skill/Active/ClusterBallSkill.cs` | 확률 판정 + 서브볼 피해 파라미터 추가 |
| `Skill/SkillManager.cs` | 액티브 리스트화, 장착 제한, 보너스 필드 재설계 |
| `Skill/SkillFactory.cs` | 패시브 매핑 7→5종 교체 |
| `UI/SkillSelectionPanel.cs` | 처치 수 트리거, 보유 여부/레벨 기반 카드 필터링 |
| `Wave/WaveManager.cs` | 처치 수 카운터, OnKillCountReached 이벤트, GetMonstersInRow |
| `Monster/MonsterBase.cs` | DOT/Slow/BonusCritChance 메서드 추가 |
| `Ball/Ball.cs` | OnWallHit/OnHitMonsterFront/OnHitMonsterBack 이벤트, CalculateDamage 시그니처 변경 |
| `Editor/SkillSetupEditor.cs` | 레벨별 수치 설정, 패시브 7→5종 교체 |
| `Editor/MonsterSetupEditor.cs` | 이름 배열 수정 |

### 삭제 파일 (7개)

| 파일 |
|------|
| `Skill/Passive/DamageUpPassive.cs` |
| `Skill/Passive/CritChanceUpPassive.cs` |
| `Skill/Passive/CritDamageUpPassive.cs` |
| `Skill/Passive/SpeedUpPassive.cs` |
| `Skill/Passive/BounceUpPassive.cs` |
| `Skill/Passive/KillShotPassive.cs` |
| `Skill/Passive/LastHitPassive.cs` |

### 생성 파일 (5개)

| 파일 | 클래스 |
|------|--------|
| `Skill/Passive/WarmTinHeartPassive.cs` | WarmTinHeartPassive |
| `Skill/Passive/MagicMirrorPassive.cs` | MagicMirrorPassive |
| `Skill/Passive/AmethystDaggerPassive.cs` | AmethystDaggerPassive |
| `Skill/Passive/EmeraldDaggerPassive.cs` | EmeraldDaggerPassive |
| `Skill/Passive/LastMatchPassive.cs` | LastMatchPassive |

---

## 주의사항

1. **SkillData 구조 변경의 파급**: `_value1~3` 제거 후 `_levels` 배열로 대체되므로 기존에 생성된 `SkillData_*.asset` 파일 전부가 무효화된다. SkillSetupEditor를 통해 에셋을 재생성해야 한다.

2. **BallData 에셋 재생성 필요**: `BallSetupEditor`의 기본값 수정은 코드만 바꾸며, 이미 생성된 `Assets/_Project/Data/BallData.asset`은 자동 갱신되지 않는다. 에셋을 삭제 후 메뉴를 재실행하거나 Inspector에서 직접 수정해야 한다.

3. **MagicMirrorPassive의 "다음 공격" 보너스 소비 타이밍**: Ball 단위가 아닌 발사 단위로 소비할지 명확히 결정해야 한다. 현재 설계는 CalculateDamage 호출 시 즉시 소비하는 방식으로, 한 번 소비되면 다음 벽 타격까지 보너스 없음.

4. **전면/후면 판정 기준**: 몬스터가 항상 아래를 향한다는 가정 하에, 볼이 위에서 내려오면 전면, 아래에서 올라오면 후면으로 판정한다. 이 판정은 게임 규칙 확인이 필요하다.

5. **SlowDuration 차감 방식**: 턴 기반이므로 `MoveDown` 호출 시 매 턴 1씩 차감하는 방식을 채택한다. `Time.deltaTime` 기반이 아님에 주의.

6. **액티브 스킬 다중 장착과 볼 스킬 적용 방식**: `ApplySkillToBall`이 여러 스킬을 볼 하나에 적용하려면, Ball이 단일 `_skill` 필드 대신 `List<BallSkillBase>`를 보유해야 한다. Ball.cs의 `SetSkill` 및 `OnCollisionEnter2D`도 함께 변경이 필요하다.

7. **GhostBallSkill 데미지 오버라이드**: 볼의 기본 데미지(`BallData.Damage`)를 스킬의 `BallDamage`로 교체하려면 `Ball.cs`에서 스킬 활성화 시 데미지 오버라이드 필드를 도입해야 한다. 이는 모든 액티브 스킬에 공통으로 필요하다.

---

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**
