# Plan — 몬스터 시스템 개편

이 문서는 `research.md`에서 나열된 열린 설계 질문들을 오케스트레이터와 사용자가 논의를 거쳐 전부 확정한 결과를 바탕으로, 실제 구현 순서와 변경 범위를 정리한다. 핵심은 `MonsterData`에 블록 크기(`BlockSize`) 필드를 추가해 콜라이더/HP바 크기를 런타임에 데이터 기준으로 자동 적용하는 것, 캐릭터 프리팹에 순수 시각용 블록 자식(`BlockVisual`)을 합성하는 것, `WaveTableData`/`WaveManager`를 좌표 사전 굽기 방식에서 파라미터 공식 기반 런타임 랜덤 배치(9×5 그리드 + 점유 체크) 방식으로 전면 재구성하는 것 세 갈래다. `Ball.cs`와 `MonsterHpBar.cs`는 이번 작업 범위에서 전혀 수정하지 않는다.

## 구현 목표

- `MonsterData`에 `BlockSize` 필드(`OneByOne`/`TwoByOne`/`OneByTwo`)를 추가하고, `MonsterBase`가 스폰 시 이 값을 기준으로 `BoxCollider2D` 크기와 HP바 `RectTransform` 폭을 런타임에 자동으로 맞춘다 — 프리팹 Inspector 값과 무관하게 `MonsterData`가 항상 단일 기준(source of truth)이 되도록 한다.
- 캐릭터 프리팹 4종(`Fluffy`/`Spider`/`StoneBug`/`ForestDeer`)에 순수 시각 전용 블록 자식(`BlockVisual`)을 추가해 "블록 위에 캐릭터가 서 있는" 합성 구조를 완성한다. 콜라이더/`MonsterBase`/tag는 루트에 그대로 유지해 `Ball.cs`의 충돌 감지 코드를 전혀 건드리지 않는다.
- `HpBarCanvas`를 `BlockVisual` 자식으로 재배치하고, 블록 앞면(정면 하단)에 블록 폭 비례 크기로 임베드한다. `MonsterHpBar.cs`/`OnHpChanged` 로직은 그대로 재사용한다.
- `WaveTableData`를 좌표 포함 20웨이브 사전 데이터 구조에서 공식 파라미터(기본 스폰 수, 웨이브당 증가량, 2칸 몬스터 가중치, 총 웨이브 수) + 몬스터 종류 4종 참조 구조로 재구성한다.
- `WaveManager.SpawnWave()`를 9열×5행 그리드 기반 런타임 랜덤 배치 + 점유 체크 로직으로 전면 재작성한다.
- `MonsterSetupEditor.cs`를 새 데이터 구조에 맞게 정리한다 (`SetupWaveSpawnEntries()` 폐기, `CreateMonsterDataAssets()`/`CreateWaveDataAssets()` 재작성).

## 단계별 작업 계획

### STEP 1. `MonsterData.cs` — BlockSize 필드 추가

- 파일: `Assets/_Project/Scripts/Data/MonsterData.cs`
- 신규 `public enum BlockSize { OneByOne, TwoByOne, OneByTwo }`을 파일 상단(클래스 밖 또는 별도 파일)에 정의한다.
- `[SerializeField] private BlockSize _blockSize;` 필드와 `public BlockSize BlockSize => _blockSize;` 프로퍼티를 기존 `_hp`/`_moveSpeed`/`_damage`/`_reward` 옆에 추가한다.
- 기존 4개 필드/프로퍼티는 그대로 유지한다(외과적 추가만).

### STEP 2. `MonsterBase.cs` — BlockSize 기준 콜라이더/HP바 자동 적용

- 파일: `Assets/_Project/Scripts/Monster/MonsterBase.cs`
- `BlockSize → Vector2`(콜라이더 실측 크기) 매핑 테이블을 `private static readonly Dictionary<BlockSize, Vector2>` 형태로 클래스 내부에 추가한다.
  ```csharp
  private static readonly Dictionary<BlockSize, Vector2> ColliderSizeMap = new() {
      { BlockSize.OneByOne, new Vector2(0.96f, 0.96f) },
      { BlockSize.TwoByOne, new Vector2(1.92f, 0.96f) },
      { BlockSize.OneByTwo, new Vector2(0.96f, 1.92f) },
  };
  ```
- `BlockSize → HP바 폭`(RectTransform.sizeDelta.x) 매핑도 함께 둔다(1칸 폭 기준값 × 가로 2칸이면 2배). 정확한 기준 폭 값은 기존 `sizeDelta = {1, 0.15}`를 참고해 dev 에이전트가 합리적으로 결정한다.
- `OnSpawn()`과 `ApplyData(MonsterData data)` 양쪽에서 공통 처리 메서드(예: `private void ApplyBlockSize()`)를 호출해:
  1. `GetComponent<BoxCollider2D>().size`를 `ColliderSizeMap[_monsterData.BlockSize]`로 설정.
  2. 자식(`BlockVisual/HpBarCanvas`)의 `RectTransform`을 찾아(`GetComponentInChildren<RectTransform>()` 또는 자식 이름으로 탐색) `sizeDelta.x`를 블록 폭 기준으로 설정.
- `_currentHp`/`OnHpChanged` 관련 기존 로직은 순서만 유지하며 변경하지 않는다.
- `Update()`의 이동/냉동/슬로우 로직은 이번 작업 범위 밖이므로 수정하지 않는다.

### STEP 3. 프리팹 4종 — BlockVisual 자식 합성 + 콜라이더 확장 + HP바 재배치

- 대상: `Assets/_Project/Prefabs/Monster/Fluffy.prefab`, `Spider.prefab`, `StoneBug.prefab`, `ForestDeer.prefab`
- 각 프리팹에 신규 자식 오브젝트 `BlockVisual`을 추가한다.
  - `SpriteRenderer` 컴포넌트만 부착(콜라이더 없음).
  - 스프라이트는 몬스터별 고정 매핑에 따라 기존 `Block_1x1.png`/`Block_2x1.png`/`Block_1x2.png`(스텁 프리팹이 아닌 스프라이트 에셋 자체)를 재사용: Fluffy/Spider → `Block_1x1.png`, StoneBug → `Block_2x1.png`, ForestDeer → `Block_1x2.png`.
  - `sortingOrder`를 캐릭터 스프라이트보다 낮은 값으로 설정(예: 블록 0, 캐릭터 1)해 캐릭터 뒤에 그려지도록 한다.
- 루트의 `BoxCollider2D` 크기는 STEP 2에서 런타임에 자동 재설정되므로 프리팹 Inspector상의 초기값은 크게 중요하지 않으나, 혼동 방지를 위해 블록 크기에 맞춰 미리 갱신해 둔다.
- `HpBarCanvas`(기존 캐릭터 루트 자식)를 `BlockVisual`의 자식으로 옮기고, 블록 앞면(정면 하단) 위치로 로컬 좌표를 조정한다. `RectTransform.sizeDelta.x`는 STEP 2의 자동 적용 로직이 덮어쓰므로 프리팹상 초기값은 근사치로만 둔다.
- 참고 이미지(`Assets/_Project/Docs/targetUI/`)를 기준으로 배치하되, 정밀한 미세 조정은 사용자의 Unity 에디터 후속 검증 몫으로 남긴다.
- `MonsterHpBar.cs`는 `GetComponentInParent<MonsterBase>()`로 조상 전체를 탐색하므로 `HpBarCanvas`의 부모가 `BlockVisual`로 바뀌어도 `MonsterBase`(루트)를 정상적으로 찾는다 — 스크립트 수정 불필요.
- **`Ball.cs`는 수정하지 않는다.** `Ball.OnCollisionEnter2D`/`OnTriggerEnter2D`가 `collision.gameObject`/`other`에서 직접 `MonsterBase`를 찾는 방식이며, 콜라이더와 `MonsterBase`가 계속 같은 루트 오브젝트에 있으므로 영향이 없다.
- 기존 블록 스텁 4종(`Block_1x1.prefab`/`Block_1x2.prefab`/`Block_2x1.prefab`/`Block_2x2.prefab`)은 그 안의 `MonsterBase`/`BoxCollider2D` 컴포넌트가 미완성 중복 구조이므로 더 이상 사용하지 않는다. 스프라이트 에셋만 재사용하고 프리팹 파일 자체는 미사용 정리 대상으로 남긴다(실제 삭제 여부는 dev 에이전트 판단).

### STEP 4. `WaveTableData.cs` — 좌표 제거, 파라미터 공식 구조로 재작성

- 파일: `Assets/_Project/Scripts/Data/WaveTableData.cs`
- 기존 `WaveEntry`/`MonsterSpawnEntry`(좌표 포함) 구조를 완전히 제거한다.
- 신규 구조:
  ```csharp
  public class WaveTableData : ScriptableObject
  {
      [SerializeField] private int   _baseSpawnCount = 3;
      [SerializeField] private float _spawnCountPerWave = 0.5f;
      [SerializeField] private float _baseTwoCellWeight = 0.1f;
      [SerializeField] private float _twoCellWeightPerWave = 0.03f;
      [SerializeField] private int   _totalWaves = 20;
      [SerializeField] private MonsterData _fluffyData;
      [SerializeField] private MonsterData _spiderData;
      [SerializeField] private MonsterData _stoneBugData;
      [SerializeField] private MonsterData _forestDeerData;

      public int   BaseSpawnCount      => _baseSpawnCount;
      public float SpawnCountPerWave   => _spawnCountPerWave;
      public float BaseTwoCellWeight   => _baseTwoCellWeight;
      public float TwoCellWeightPerWave => _twoCellWeightPerWave;
      public int   TotalWaves          => _totalWaves;
      public MonsterData FluffyData    => _fluffyData;
      public MonsterData SpiderData    => _spiderData;
      public MonsterData StoneBugData  => _stoneBugData;
      public MonsterData ForestDeerData => _forestDeerData;
  }
  ```
  (정확한 필드명/프로퍼티 네이밍은 dev 에이전트가 기존 코드 스타일에 맞춰 조정 가능하나, 핵심은 "20개 웨이브 개별 데이터가 아니라 공식 파라미터 몇 개 + 몬스터 종류 4개 참조" 구조를 유지하는 것이다.)
- 기존 `WaveManager.TotalWaves`가 참조하던 `WaveTableData.WaveCount`는 삭제되고, `WaveManager`는 `_waveTable.TotalWaves`(신규 `_totalWaves` 필드)를 참조하도록 STEP 5에서 함께 수정한다.

### STEP 5. `WaveManager.cs` — 그리드 랜덤 배치 + 점유 체크로 전면 재작성

- 파일: `Assets/_Project/Scripts/Wave/WaveManager.cs`
- 신규 `[SerializeField] private int _gridColumns = 9;`, `[SerializeField] private int _gridRows = 5;` 필드 추가. 기존 `_gridCellSize`(1.0f)는 그대로 유지.
- `TotalWaves` 프로퍼티를 `_waveTable.WaveCount` → `_waveTable.TotalWaves`로 변경.
- `SpawnWave(int index)` 전면 재작성:
  1. `spawnCount = Mathf.RoundToInt(_waveTable.BaseSpawnCount + _waveTable.SpawnCountPerWave * index)` 계산.
  2. `twoCellWeight = Mathf.Clamp01(_waveTable.BaseTwoCellWeight + _waveTable.TwoCellWeightPerWave * index)` 계산.
  3. 그리드 총 용량(`_gridColumns * _gridRows`, 9×5=45) 기준 안전 상한을 두어 `spawnCount = Mathf.Min(spawnCount, capacityLimit)`로 제한한다(2칸 몬스터가 최악의 경우 전부 나와도 45칸을 넘지 않도록, 예: `capacityLimit = (_gridColumns * _gridRows) / 2` ≈ 22). 듬성듬성 배치는 허용되며 그리드가 가득 차 배치 불가능한 상황 자체를 사전에 막는 것이 목적이다.
  4. `bool[,] occupied = new bool[_gridColumns, _gridRows]`를 웨이브 시작마다 새로 생성(매 웨이브 초기화, 이전 웨이브 점유 상태는 고려하지 않음 — 클리어 조건상 스폰 시점에는 이전 웨이브 몬스터가 이미 전부 사라진 상태이므로 문제 없음).
  5. `twoCellWeight`에 따라 이번 웨이브에서 몇 마리를 2칸 몬스터로 할지 결정 후, StoneBug(`TwoByOne`, 가로 2칸)/ForestDeer(`OneByTwo`, 세로 2칸) 중 랜덤 선택 → 각 크기가 들어갈 수 있는 빈 칸 후보를 모아 랜덤으로 하나 선택해 배치 → `occupied` 갱신. 이를 먼저 수행한다.
  6. 나머지 스폰 수만큼 Fluffy/Spider(`OneByOne`) 중 랜덤 선택 → 남은 빈 칸 후보 중 랜덤 선택해 배치.
  7. 그리드 좌표 → 월드 좌표 변환은 `_spawnRoot.position + new Vector3(col * _gridCellSize, row * _gridCellSize, 0)` 형태를 유지한다. 상단 스폰 영역 배치는 씬에서 `_spawnRoot` 위치 자체를 사용자가 조정하는 것으로 처리하며, 코드 로직 변경은 불필요하다.
  8. 스폰마다 `_monsterPool.Get()` → 선택된 `MonsterData`로 `ApplyData()` → 계산된 월드 좌표 대입 → `_activeMonsters.Add()`(기존 흐름 유지).
  9. `_currentWaveTotalCount`/`OnWaveStarted`/`OnMonsterCountChanged` 발행 흐름은 기존과 동일하게 유지한다.
- `CheckGameOver()`/`HandleMonsterDied()`/`CheckWaveCleared()`/`AdvanceToNextWave()`/`GetWeakestMonster()`/`GetMonstersInRow()`는 이번 좌표/데이터 구조 변경과 무관하므로 수정하지 않는다.

### STEP 6. 신규 `MonsterOverhaulSetupEditor.cs` 작성 (새 데이터 구조 세팅 전용)

- 대상 파일: `Assets/_Project/Scripts/Editor/MonsterOverhaulSetupEditor.cs` (신규 생성)
- **`Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`는 이번 작업에서 전혀 수정하지 않는다.** 그 안의 기존 메서드(`SetupWaveSpawnEntries()`, 기존 `CreateWaveDataAssets()`/`CreateMonsterDataAssets()`)는 새 데이터 구조와 맞지 않게 되어 더 이상 유효하지 않지만, 문자열 기반 리플렉션 API(`SerializedObject`/`FindProperty`)를 쓰므로 컴파일에는 영향이 없다. 해당 기존 메뉴 항목들은 이제 쓸모없어지지만 그대로 방치한다(제거하지 않음) — 이유는 파일 수정 자체를 피하기 위함이다.
- 신규 스크립트가 담당할 역할(별도 `MenuItem`, 예: `PurpleCow/Setup/Monster Overhaul Setup`):
  1. `MonsterData_Fluffy/Spider/StoneBug/ForestDeer.asset` 4종에 `_blockSize` 필드를 종류별로 채운다(Fluffy/Spider → `OneByOne`, StoneBug → `TwoByOne`, ForestDeer → `OneByTwo`). 2칸 몬스터(StoneBug/ForestDeer)는 `_hp`/`_reward`를 1칸 몬스터보다 상향 조정한다 — **정확한 수치는 dev 에이전트가 임의로 합리적인 값을 정하고, 구현 완료 후 오케스트레이터에게 어떤 값으로 정했는지 보고한다** (예: 1칸 기본값 Hp30/Reward10 유지, 2칸은 Hp45~60/Reward15~20 선에서 dev 에이전트 재량으로 확정).
  2. `Assets/_Project/Data/WaveTableData.asset`을 새 파라미터 구조(`_baseSpawnCount`/`_spawnCountPerWave`/`_baseTwoCellWeight`/`_twoCellWeightPerWave`/`_totalWaves`/몬스터 4종 참조)에 맞게 생성 또는 갱신한다. asset이 이미 존재하면(기존 좌표 포함 구조로 구워진 상태) 새 필드 구조로 안전하게 재설정한다.
  3. 프리팹 4종(`Fluffy`/`Spider`/`StoneBug`/`ForestDeer.prefab`)에 `BlockVisual` 자식 오브젝트를 추가하고(스프라이트/`sortingOrder` 설정), `HpBarCanvas`를 `BlockVisual`의 자식으로 재배치하는 작업도 이 신규 스크립트가 `PrefabUtility.EditPrefabContentsScope`로 처리한다(기존 `MonsterSetupEditor.ConnectMonsterDataToPrefabs()`와 유사한 패턴이되 완전히 독립된 코드).
- 이 신규 스크립트는 기존 `MonsterSetupEditor.cs`/`BackgroundGridFitSetupEditor.cs`와 같은 관행(`GameObject.Find`/`AssetDatabase.LoadAssetAtPath` 등으로 대상 탐색 → `SerializedObject`/`FindProperty` 패턴으로 값 주입)을 따르되 완전히 독립적으로 작성한다.

## 예상 변경/생성 파일 목록

| 파일 | 경로 | 변경 유형 |
|---|---|---|
| MonsterData.cs | `Assets/_Project/Scripts/Data/MonsterData.cs` | 수정 (BlockSize enum/필드 추가) |
| MonsterBase.cs | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | 수정 (콜라이더/HP바 자동 적용 로직 추가) |
| WaveTableData.cs | `Assets/_Project/Scripts/Data/WaveTableData.cs` | 수정 (구조 전면 재작성) |
| WaveManager.cs | `Assets/_Project/Scripts/Wave/WaveManager.cs` | 수정 (SpawnWave 전면 재작성, 그리드 필드 추가) |
| MonsterOverhaulSetupEditor.cs | `Assets/_Project/Scripts/Editor/MonsterOverhaulSetupEditor.cs` | 신규 생성 (새 데이터 구조 세팅 전용 에디터 스크립트) |
| Fluffy.prefab | `Assets/_Project/Prefabs/Monster/Fluffy.prefab` | 수정 (BlockVisual 자식 추가, HpBarCanvas 재배치) |
| Spider.prefab | `Assets/_Project/Prefabs/Monster/Spider.prefab` | 수정 (동일) |
| StoneBug.prefab | `Assets/_Project/Prefabs/Monster/StoneBug.prefab` | 수정 (동일) |
| ForestDeer.prefab | `Assets/_Project/Prefabs/Monster/ForestDeer.prefab` | 수정 (동일) |
| Block_1x1/1x2/2x1/2x2.prefab | `Assets/_Project/Prefabs/Monster/Block_*.prefab` | 미사용 정리 대상 (스프라이트만 재사용, 프리팹 자체 폐기 검토) |
| MonsterData_*.asset 4종 | `Assets/_Project/Data/MonsterData_Fluffy·Spider·StoneBug·ForestDeer.asset` | 재생성/값 갱신 (BlockSize, 2칸 Hp/Reward 상향) — 에디터 실행 필요 |
| WaveTableData.asset | `Assets/_Project/Data/WaveTableData.asset` | 구조 변경에 따른 재생성/마이그레이션 — 에디터 실행 필요 |

`Ball.cs`, `MonsterHpBar.cs`는 이번 작업 범위에서 수정하지 않는다.

## 주의사항

1. `WaveTableData.asset`/`MonsterData_*.asset`의 실제 재생성·검증은 Unity 로컬 에디터 실행이 필요하며, 원격/텍스트 기반 환경에서는 코드(SO 클래스, `MonsterOverhaulSetupEditor`)까지만 완결되고 실제 asset 재직렬화는 사용자가 로컬에서 `PurpleCow/Setup/Monster Overhaul Setup` 메뉴를 실행해야 완료된다.
2. 프리팹 4종(`Fluffy`/`Spider`/`StoneBug`/`ForestDeer`)에 `BlockVisual` 자식을 추가하고 `HpBarCanvas`를 재배치하는 작업 역시 Unity 에디터에서 프리팹을 열어 저장해야 하는 작업이라, 코드/스크립트만으로는 완결되지 않을 수 있다. 가능한 범위는 `PrefabUtility.EditPrefabContentsScope` 기반 에디터 자동화 스크립트로 처리하되, 세밀한 위치/크기 조정은 사용자의 후속 Unity 에디터 검증이 필요하다.
3. HP바/블록 배치의 정확한 좌표·두께·색상은 참고 이미지(`Assets/_Project/Docs/targetUI/`)를 기준으로 근사치로 구현하며, 최종 미세 조정은 사용자 몫으로 남긴다.
4. `Ball.cs`는 콜라이더가 몬스터 루트에 그대로 유지되므로 이번 작업 범위에서 전혀 수정하지 않는다. 향후 콜라이더 위치를 자식으로 옮기는 등의 추가 변경이 생기면 `Ball.OnCollisionEnter2D`/`OnTriggerEnter2D`의 `TryGetComponent<MonsterBase>()` 호출을 `GetComponentInParent`로 바꿔야 할 수 있다는 점을 참고용으로 남긴다.
5. 그리드 스폰 수 상한(예: 22)과 2칸 몬스터 Hp/Reward 상향 폭 등 정확한 수치는 dev 에이전트가 본 문서에 명시된 방향성(그리드 용량 이내로 제한, 2칸 몬스터가 명확히 더 강함) 안에서 합리적으로 결정한다. 결정한 수치는 구현 완료 보고 시 함께 명시한다.
6. 기존 `MonsterSetupEditor.cs`는 이번 작업에서 수정하지 않으며, 새 데이터 구조 세팅은 별도의 신규 에디터 스크립트(`MonsterOverhaulSetupEditor.cs`)로 분리한다.

---

**본 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작한다.**
