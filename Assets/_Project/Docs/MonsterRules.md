# MonsterRules.md

이 문서는 몬스터 시스템(스폰, 전진, HP, 상태이상, 웨이브 진행)에 관한 규칙을 한곳에 모은 통합 문서입니다.
기존에 `GameplayMechanics.md`(스폰/전진 메커닉)와 `UIRules.md`(HP바, 캐릭터 HP/XP)에 흩어져 있던 몬스터 관련 서술과,
현재 코드와 어긋난 낡은 초기 설계 task 문서(`_Task/2026-06-30/14-00_Monster시스템구현/`)를 정리하기 위해 신규 작성되었습니다.
`MonsterBase.cs`/`WaveManager.cs` 실제 코드를 기준으로 작성되었으며, 이후 몬스터 관련 규칙이 추가/변경되면 이 문서를 기준으로 갱신합니다.
이번 갱신에서는 웨이브 구성(전종류 랜덤 등장), 스폰 위치 결정 방식(런타임 랜덤 배치 + 점유 체크), 몬스터별 고정 블록 크기, HP바 표시 방식이 새로 확정되어 반영되었습니다.
확정된 설계이지만 아직 코드에는 반영되지 않은 부분은 각 섹션에 "(구현 예정 — 아직 코드 미반영)" 또는 "(현재 구현 — 아직 새 규칙 미반영)"으로 표시했습니다.

---

## 1. 개요

- 이 문서는 **몬스터 전용 규칙의 단일 기준(source of truth)**입니다. 몬스터와 관련된 내용을 새로 정의하거나 수정할 때는 이 문서를 갱신합니다.
- `GameplayMechanics.md` 섹션 2(몬스터 스폰/전진 시스템)에 있던 본문은 이 문서로 이관되었습니다. 해당 문서에는 안내 문구와 링크만 남아 있습니다.
- 몬스터 HP바(블록 앞면 임베드 방식, 7장 참고) UI, 캐릭터 HP/XP/레벨 처리는 `UIRules.md` 섹션 9, 10에 이미 정의되어 있으므로 이 문서에서 중복 서술하지 않고 7장에서 링크만 겁니다.
- 초기 구현 단계의 task 문서(`_Task/2026-06-30/14-00_Monster시스템구현/`)는 static event 기반 `OnHitMonster`/`LastDamage` 충돌 감지라는 낡은 설계를 담고 있어 현재 코드(Unity 물리 충돌 콜백 + `MonsterBase.TakeDamage` 직접 호출 방식)와 다릅니다. 이 문서는 그 낡은 설계를 참고하지 않고 현재 코드를 기준으로 작성되었습니다.

---

## 2. 몬스터 스폰 및 전진 메커닉

원본 게임 실제 플레이어(사용자)가 확인해준 몬스터 스폰/전진 메커닉이며, 이후 논의를 거쳐 스폰 규칙 일부가 아래와 같이 새로 확정되었습니다.

- 한 웨이브가 시작되면 해당 웨이브에 소속된 몬스터가 **한 번에 전부** 스폰된다. 시간차를 두고 순차적으로 스폰되지 않는다.
- **(확정)** 웨이브 1부터 20까지 전 구간에서 Fluffy/Spider/StoneBug/ForestDeer **4종 전부가 랜덤하게 섞여서 등장**한다. 특정 웨이브 구간에서만 종류를 점진적으로 늘리던 기존 방식은 폐지되었다(3장 참고). 웨이브 개수는 기존과 동일하게 20웨이브를 유지한다.
- **(확정)** 스폰 위치는 필드 상단 그리드 영역 내에서 **웨이브가 시작될 때마다 매번 새로 런타임에 랜덤 계산**된다. 이전 웨이브에서 어떤 위치가 점유되어 있었는지와 무관하게 매 웨이브 새로 랜덤 결정되며, 2칸을 차지하는 몬스터(StoneBug/ForestDeer)가 다른 몬스터와 겹치지 않도록 **점유 체크(occupancy check)** 로직을 거쳐 좌표가 결정된다(자세한 내용은 6장 참고).
- 몬스터는 스폰 직후부터 **시간 경과에 따라 연속적으로(부드럽게)** 화면 하단을 향해 전진한다. 그리드 한 칸씩 딱딱 끊어지는 스텝 이동이 아니다.
- 전진은 볼 발사/귀환 사이클과 **무관한 독립적인 시간 흐름**에 따라 진행된다. 볼이 몇 개 남아있든, 귀환했든과 상관없이 몬스터는 계속 전진한다.
- 몬스터가 화면 하단(캐릭터 주변 경계선)에 도달하면 캐릭터 HP를 깎고 소멸한다. 캐릭터 HP 차감 및 경험치(XP) 처리는 `UIRules.md` 섹션 10 "캐릭터 HP / 경험치 / 레벨 시스템"에 이미 정리되어 있으므로 이 문서에서는 중복 서술하지 않는다.
- 웨이브 클리어 조건은 해당 웨이브에 스폰된 몬스터를 **전멸**시키는 것이다. 전멸하면 다음 웨이브가 스폰된다.
- 게임의 목표는 몬스터 처치를 통한 생존이다.
- 그리드는 **정사각형 셀을 전제**로 한다. 배경 이미지 비율 보정(`BackgroundFitter`/`WallFitter`)은 볼 충돌벽/캐릭터 위치 등 여러 시스템에 영향을 주는 위험도 높은 작업이라 별도의 선행 task로 진행할 예정이며, 이번 문서 갱신 범위에는 포함하지 않는다.

### 구현 현황

관련 코드: `WaveManager.cs`, `MonsterBase.cs`, `MonsterSetupEditor.cs`

- **(구현 완료)** `WaveManager`는 `SpawnWave()`에서 웨이브에 속한 `MonsterSpawnEntry` 전체를 한 프레임에 일괄 스폰한다.
- **(구현 예정 — 아직 코드 미반영)** 현재 코드는 스폰 위치를 `MonsterSpawnEntry.GridPosition`(그리드 좌표) × `_gridCellSize`를 `_spawnRoot.position`에 더해 계산하는데, 이 좌표는 런타임 랜덤이 아니라 `MonsterSetupEditor.SetupWaveSpawnEntries()`가 **에디터에서 한 번 실행 시점에 고정값으로 계산해 `WaveTableData.asset`에 미리 구워넣은 값**이다. 즉 현재는 언제 플레이해도 같은 웨이브는 항상 같은 위치에 몬스터가 뜨며, 위에서 확정한 "매 웨이브 런타임 랜덤 배치 + 점유 체크" 규칙과 어긋난다. `WaveManager`가 점유 체크 기반으로 좌표를 직접 계산하도록 교체가 필요하다(6장 참고).
- **(구현 완료)** `MonsterBase.Update()`가 매 프레임 `MonsterData.MoveSpeed` 기반으로 `Vector3.down` 방향 이동을 수행한다(시간 연속 하강). 볼 발사/귀환 사이클(`BallLauncher`/`Ball`)과는 완전히 독립적으로 동작한다.
- **(구현 완료)** `WaveManager.CheckGameOver()`가 `Update()`에서 매 프레임 활성 몬스터의 `transform.position.y`가 `_bottomBoundaryY` 이하인지 체크해 하단 도달을 판정한다. 도달 시 풀 반납 + `OnMonsterReachedBottom` 이벤트 발행.
- 냉동(`ApplyFreeze`)/슬로우(`ApplySlow`)도 턴 기반이 아니라 초 단위 타이머로 동작한다(4장/5장 참고).

---

## 3. 몬스터 종류 및 스탯

### 종류 및 고정 블록(베이스) 크기 — 신규 확정

몬스터 프리팹은 **블록(베이스, 발판) + 캐릭터 스프라이트가 합쳐진 하나의 프리팹**이다. 원본 게임 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/`)에서도 몬스터가 블록 발판 위에 서 있는 형태로 확인된다. 4종 모두 스프라이트 픽셀 실측을 기준으로 아래와 같이 종류별 고정 블록 크기가 매칭된다.

| 몬스터 | 블록 크기 | 비고 |
|---|---|---|
| Fluffy | `Block_1x1` | 정사각 1칸 |
| Spider | `Block_1x1` | 정사각 1칸 |
| StoneBug | `Block_2x1` | 가로로 2칸 |
| ForestDeer | `Block_1x2` | 세로로 2칸 |

- `Block_2x2`(정사각 2칸)는 이번 4종 매칭에는 사용하지 않는다.
- 콜라이더는 몬스터가 차지하는 블록 전체 크기를 커버해야 한다(2칸짜리 몬스터는 콜라이더도 2칸만큼 커버).
- 2칸을 차지하는 몬스터(StoneBug/ForestDeer)가 스폰될 때 인접한 다른 몬스터와 겹치지 않도록 **점유 체크(occupancy check)** 로직이 반드시 필요하다(겹침 절대 불가, 랜덤 스폰 — 6장 참고).
- **(구현 예정 — 아직 코드 미반영)** 위 블록 크기 매핑과 점유 체크 로직은 현재 코드에 반영되어 있지 않다.

### 웨이브별 등장 구성 — 전종류 랜덤 (신규 확정)

- **(확정)** 웨이브 1~20 전 구간에서 4종(Fluffy/Spider/StoneBug/ForestDeer) 전부가 랜덤하게 섞여서 등장한다. 특정 웨이브 구간에서만 종류가 점진적으로 늘어나던 기존 방식은 폐지되었다(2장 참고).
- 난이도 스케일링 방향성: 웨이브가 진행될수록 (a) 웨이브당 스폰 수가 증가하고, (b) 2칸 몬스터(StoneBug/ForestDeer)의 등장 가중치가 증가한다. **정확한 수치 공식(스폰 수 증가 폭, 가중치 곡선 등)은 아직 미정이며 추후 별도 plan.md에서 결정한다.**

> **(현재 구현 — 아직 새 규칙 미반영)** 현재 코드(`MonsterSetupEditor.SetupWaveSpawnEntries()`)는 웨이브 1~5는 Fluffy만, 6~10은 +Spider, 11~15는 +StoneBug, 16~20은 +ForestDeer가 추가되는 방식으로 웨이브별 등장 종류를 점진적으로 늘리며, 좌표까지 고정값으로 계산해 `WaveTableData.asset`에 구워넣는다. 각 웨이브의 스폰 수도 그룹(5웨이브 단위)과 그룹 내 위치에 따라 점진적으로 증가하도록 자동 계산된다(`spawnCount = 3 + posInGroup + groupIdx * 2`). 이 방식은 위 확정 규칙에 따라 폐기될 예정이다.

### `MonsterData` (ScriptableObject) 필드

`Assets/_Project/Scripts/Data/MonsterData.cs`

| 필드 | 타입 | 설명 |
|---|---|---|
| `Hp` | float | 최대 체력 |
| `MoveSpeed` | float | 초당 하강 속도 |
| `Damage` | int | 하단 도달 시 캐릭터에게 주는 피해량 |
| `Reward` | int | 처치/통과 시 획득 XP (`UIRules.md` 섹션 10 참고) |

- **(확정)** 2칸 몬스터(StoneBug/ForestDeer)는 1칸 몬스터(Fluffy/Spider)보다 `Hp`/`Reward`가 더 크게 차등을 둔다. 정확한 수치는 아직 미정이며 "더 크다"는 방향성만 규칙으로 확정되었다.
- **(현재 구현 — 아직 새 규칙 미반영)** `MonsterSetupEditor.CreateMonsterDataAssets()`가 생성하는 기본값은 4종 모두 동일(Hp 30 / MoveSpeed 1 / Damage 1 / Reward 10)이다. 몬스터별 실제 밸런스 차이(2칸 몬스터 상향 포함)는 아직 반영되지 않았으며, 이후 각 `MonsterData` 에셋을 개별 조정하는 방식으로 반영될 예정이다.

---

## 4. HP 관리 및 사망 처리

관련 코드: `MonsterBase.cs`, `Ball.cs`

### 풀링 초기화

- `OnSpawn()`: 풀에서 꺼내질 때 호출. `_currentHp`를 `MonsterData.Hp`로 리셋하고 냉동/슬로우/보너스 크리티컬 상태를 모두 초기화한 뒤 `OnHpChanged` 이벤트를 발행한다.
- `ApplyData(MonsterData data)`: `WaveManager.SpawnWave()`가 스폰 직후 호출해 이번에 사용할 `MonsterData`를 주입하고 `_currentHp`를 갱신한다.
- `OnDespawn()`: 풀 반납 시 `_isDead = true`로 설정.

### 데미지 처리 및 사망

- `TakeDamage(float damage)`: `_isDead`면 무시. HP를 차감하고 `OnHpChanged(currentHp, maxHp)` 이벤트를 발행. HP가 0 이하가 되면 `Die()` 호출.
- `Die()`: `_isDead = true` 설정 후 static event `OnMonsterDied` 발행. `WaveManager`가 이를 구독해 풀 반납/킬카운트 증가/웨이브 클리어 판정을 처리한다(6장 참고).
- `public event Action<float, float> OnHpChanged`: 몬스터별 HP바(`MonsterHpBar`, `UIRules.md` 섹션 9)가 구독하는 이벤트.
- `public static event Action<MonsterBase> OnMonsterDied`: `WaveManager`가 구독.

### Ball → Monster 데미지 전달 흐름 (실제 구현 기준)

몬스터에 대한 충돌 감지는 별도의 수동 레이캐스트가 아니라 **Unity 물리 콜백**을 통해 이루어진다.

1. `Ball.OnCollisionEnter2D`에서 상대 오브젝트 태그가 `"Monster"`면 `MonsterBase` 컴포넌트를 가져와 `CalculateDamage(monster)`를 호출한다.
2. `CalculateDamage()`는 `BallData.Damage`(또는 서브볼 데미지 오버라이드)와 `BallData.CriticalChance` + `monster.ConsumeBonusCritChance()`(상태이상으로 누적된 보너스 크리티컬 확률, 5장 참고)를 합산해 크리티컬 여부/최종 데미지를 계산하고, `SkillManager`의 데미지 배율/다음 발사 보너스 데미지를 추가로 적용한다.
3. 계산된 데미지로 `target.TakeDamage(damage)`를 호출해 실제 HP를 차감하고, `LastDamage`(볼의 마지막 적용 데미지, 일부 액티브 스킬이 참조)를 갱신한다.
4. 데미지 적용 후 static event `Ball.OnHitMonster(target, damage, isCritical)`를 발행한다(데미지 텍스트 등 다른 시스템이 소비).
5. 볼의 이동 방향(`velocity.y`)이 아래쪽이면 `OnHitMonsterFront`, 위쪽(귀환 중 튕겨나가는 방향)이면 `OnHitMonsterBack`을 발행한다. 이 두 이벤트는 전면/후면 히트 조건부 패시브 스킬(`AmethystDaggerPassive`/`EmeraldDaggerPassive` 등)이 구독한다.
6. Ball에 장착된 `BallSkillBase` 스킬들의 `OnBallHit(monster)`도 순서대로 호출되어 각 스킬 고유 효과(냉동/슬로우/도트 등, 5장 참고)를 적용한다.
7. Ghost 스킬(`GhostBallSkill`)이 장착되어 콜라이더가 트리거로 전환된 경우에는 `OnCollisionEnter2D` 대신 `OnTriggerEnter2D`에서 동일하게 `CalculateDamage()` + `OnBallHit()`가 호출된다.

> 참고: 초기 task 문서(`_Task/2026-06-30/14-00_Monster시스템구현/`)에 기술된 static event 기반 충돌 감지 설계는 현재 코드와 다르므로 참고하지 않는다. 실제로는 Unity `OnCollisionEnter2D`/`OnTriggerEnter2D` 콜백에서 직접 `TakeDamage()`를 호출하는 방식이며, `OnHitMonster` 등의 static event는 데미지 적용 "이후" 다른 시스템에 알리는 용도로만 쓰인다.

---

## 5. 상태이상 처리

관련 코드: `MonsterBase.cs`

| API | 파라미터 | 동작 |
|---|---|---|
| `ApplyFreeze(float seconds)` | 지속시간(초) | `_frozenSecondsRemaining`을 기존 값과 새 값 중 큰 값으로 갱신(중첩 시 갱신, 값이 줄어들지 않음). `Update()`에서 남은 시간이 0보다 크면 이동을 완전히 멈추고(조기 `return`) 슬로우 타이머도 함께 멈춘다. |
| `ApplySlow(float seconds, float percent)` | 지속시간(초), 감속 비율(0~1) | `_slowSecondsRemaining`/`_slowPercent`를 새 값으로 덮어쓴다(중첩 누적 아님, 마지막 호출 값으로 대체). `Update()`에서 냉동 상태가 아닐 때 `speed *= (1 - percent)`로 이동속도를 감소시키며 타이머를 감소시킨다. |
| `ApplyBonusCritChance(float bonus)` | 크리티컬 확률 보너스 | `_bonusCritChance`에 누적(`+=`) 저장. `Ball.CalculateDamage()`가 다음 피격 시 `ConsumeBonusCritChance()`로 값을 읽고 0으로 리셋한다(1회성 소비). |
| `ApplyDot(float damagePerSec, float duration, int maxStacks)` | 초당 피해, 지속시간(초), 최대 중첩 | 코루틴(`CoDotTick`)을 새로 시작해 1초 간격으로 `damagePerSec * maxStacks`만큼 `TakeDamage()`를 호출한다(`duration`초 동안 반복). |

### 스킬 시스템과의 연동

이 상태이상 API들은 `BallSkillBase`를 상속하는 볼 스킬 클래스들이 `OnBallHit(MonsterBase target)` 또는 전면/후면 히트 이벤트 콜백에서 호출한다.

- `IceBallSkill`(Active): 일정 확률로 `ApplyFreeze` + `ApplySlow`를 동시에 적용하고 추가 데미지도 `TakeDamage`로 직접 부여.
- `FireBallSkill`(Active): 피격 대상에 `ApplyDot`(지속시간/최대중첩/초당피해)을 적용.
- `AmethystDaggerPassive`(Passive, `Ball.OnHitMonsterFront` 구독): 전면 피격 시 `ApplyBonusCritChance` 적용.
- `EmeraldDaggerPassive`(Passive, `Ball.OnHitMonsterBack` 구독): 후면 피격 시 `ApplyBonusCritChance` 적용.

각 스킬의 수치는 `SkillData`/`SkillLevelData`(레벨별 `Value1`/`Value2`/`Value3`)에서 읽어온다.

---

## 6. 웨이브 시스템

관련 코드: `WaveTableData.cs`, `WaveManager.cs`

### 데이터 구조 — 신규 확정 구조

**(확정)** `WaveTableData`는 더 이상 몬스터의 정확한 스폰 좌표를 저장하지 않는다. 웨이브당 "스폰 수"와 "몬스터 종류별 등장 가중치" 같은 **구성 파라미터만** 가지며, 실제 좌표는 웨이브가 시작될 때마다 `WaveManager`가 점유 체크 로직을 이용해 매번 새로 랜덤 계산한다(2장/3장 참고).

- `WaveTableData`(ScriptableObject): 20개 웨이브 전체를 테이블 형태로 관리하는 큰 구조는 유지한다.
- `WaveEntry`: 웨이브 번호 + 이 웨이브의 스폰 수, 몬스터 종류별 등장 가중치 등 구성 파라미터를 갖는다. 정확한 필드 설계(가중치 표현 방식 등)는 아직 미정이며 추후 plan.md에서 확정한다.
- 좌표(`GridPosition`)는 더 이상 `WaveEntry`/`MonsterSpawnEntry`에 저장되지 않는다.

> **(현재 구현 — 아직 새 규칙 미반영)** 현재 코드는 `WaveEntry`가 `List<MonsterSpawnEntry> SpawnEntries`를 가지며, `MonsterSpawnEntry`는 `MonsterData Data`(몬스터 종류) + `Vector2Int GridPosition`(정확한 스폰 좌표)을 그대로 갖고 있다. 이 좌표는 `MonsterSetupEditor.SetupWaveSpawnEntries()`가 에디터에서 한 번 계산해 에셋에 구워넣은 고정값이며, 위 확정 구조(구성 파라미터만 저장)로 교체가 필요하다.

### `WaveManager` 흐름 — 신규 확정 흐름

**(확정)** `WaveManager.SpawnWave()`는 `WaveTableData`에서 이번 웨이브의 스폰 수/가중치 구성만 읽어오고, 웨이브가 시작되는 시점에 매번 다음을 새로 계산해야 한다.

1. 구성된 스폰 수만큼, 몬스터 종류를 가중치에 따라 랜덤 결정한다.
2. 필드 상단 그리드 영역 내에서 각 몬스터의 스폰 위치를 랜덤으로 결정하되, 점유 체크 로직으로 이미 배치가 확정된 다른 몬스터(특히 2칸짜리)와 겹치지 않는 좌표만 후보로 사용한다(겹침 절대 불가).
3. 결정된 좌표에 몬스터를 배치한다.

이전 웨이브의 점유 상태와는 무관하게 매 웨이브 새로 랜덤 결정된다(2장 참고). **(구현 예정 — 아직 코드 미반영)**

> **(현재 구현 — 아직 새 규칙 미반영)** 현재 `WaveManager`는 아래처럼 동작하며, 위 확정 흐름으로 교체가 필요하다.
>
> 1. `Awake()`: `MonsterBase` 프리팹으로 `ObjectPool<MonsterBase>` 생성.
> 2. `Start()`: `SpawnWave(0)` 호출로 첫 웨이브 스폰.
> 3. `SpawnWave(index)`: 해당 웨이브의 `SpawnEntries`를 순회하며 풀에서 몬스터를 꺼내(`ApplyData` + `GridPosition` 기반 **고정** 위치 설정) `_activeMonsters`에 추가. `OnWaveStarted(waveNumber)`, `OnMonsterCountChanged(현재, 전체)` 발행.
> 4. `Update() → CheckGameOver()`: 매 프레임 활성 몬스터의 `position.y`가 `_bottomBoundaryY` 이하인지 체크(전진은 `MonsterBase.Update()`가 자체적으로 수행). 도달 시 리스트에서 제거 + 풀 반납 + `OnMonsterReachedBottom` 발행.
> 5. `MonsterBase.OnMonsterDied` 구독(`OnEnable`/`OnDisable`) → `HandleMonsterDied()`: 리스트 제거, 풀 반납, `_totalKillCount` 증가 → `CheckSkillUnlock()`(`_killCountForSkill`마다 `OnKillCountReached` 발행) → `OnMonsterCountChanged` 발행 → `CheckWaveCleared()`.
> 6. `CheckWaveCleared()`: 활성 몬스터 수가 0이면 마지막 웨이브인 경우 `OnAllWavesCleared`를 즉시 발행하고, 아니면 `OnWaveCleared`를 발행한다(UIManager가 이를 받아 SkillSelectionPanel을 연다).
> 7. `AdvanceToNextWave()`: SkillSelectionPanel에서 스킬 선택 완료 콜백 이후 UIManager가 호출. 웨이브 인덱스를 증가시키고 다음 웨이브를 스폰하거나(모두 소진 시) `OnAllWavesCleared`를 발행.

위 3~7번 흐름 중 스킬/킬카운트/웨이브 클리어 판정 로직(5~7번)은 좌표 계산 방식이 바뀌어도 그대로 유지될 예정이다. 변경이 필요한 부분은 3번(`SpawnWave`)의 위치 계산 로직뿐이다.

### 스킬 시스템이 참조하는 헬퍼

- `GetWeakestMonster()`: 활성 몬스터 중 `CurrentHp`가 가장 낮은 대상을 반환.
- `GetMonstersInRow(MonsterBase reference)`: 기준 몬스터와 같은 y좌표(±0.1 오차)에 있는 몬스터들을 반환.

이벤트 목록: `OnWaveStarted(int)`, `OnWaveCleared`, `OnAllWavesCleared`, `OnKillCountReached`, `OnMonsterReachedBottom(MonsterBase)`, `OnMonsterCountChanged(int, int)` (모두 static event).

---

## 7. UI 연동 참조

- **(확정)** 몬스터 HP바는 몬스터 머리 위가 아니라 **블록(베이스)의 앞면(정면 하단)에 임베드된 형태**로 표시된다. 폭은 블록의 가로 길이에 비례한다(2칸 블록은 HP바도 그만큼 넓다). 기존 "머리 위 월드 스페이스 캔버스 + 슬라이더" 방식은 폐기되었다. 상세 배치/비율/구현 방식은 `UIRules.md` 섹션 9를 참고한다.
- 캐릭터 HP / 경험치 / 레벨 시스템(하단 도달 시 HP 차감, XP 획득): `UIRules.md` 섹션 10 참고.

이 문서에서는 위 내용을 중복 서술하지 않습니다.

---

## 8. 관련 파일 목록

| 파일 | 경로 | 설명 |
|---|---|---|
| MonsterData.cs | `Assets/_Project/Scripts/Data/MonsterData.cs` | 몬스터 스탯 ScriptableObject (Hp/MoveSpeed/Damage/Reward) |
| MonsterBase.cs | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | 몬스터 HP/이동/상태이상/사망 처리, 풀링(IPoolable) |
| WaveTableData.cs | `Assets/_Project/Scripts/Data/WaveTableData.cs` | 웨이브 20개 구성 테이블 ScriptableObject (WaveEntry/MonsterSpawnEntry) |
| WaveManager.cs | `Assets/_Project/Scripts/Wave/WaveManager.cs` | 웨이브 스폰/진행/클리어 판정, 킬카운트 기반 스킬 선택 트리거 |
| Ball.cs | `Assets/_Project/Scripts/Ball/Ball.cs` | 몬스터와의 충돌 감지 및 데미지 계산/적용 |
| BallSkillBase.cs | `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs` | 볼 스킬 공통 베이스, `OnBallHit(MonsterBase)`에서 몬스터 상태이상 API 호출 |
| MonsterSetupEditor.cs | `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` | 몬스터/웨이브 SO 에셋 자동 생성 및 웨이브별 스폰 데이터 자동 설정 에디터 스크립트 |
