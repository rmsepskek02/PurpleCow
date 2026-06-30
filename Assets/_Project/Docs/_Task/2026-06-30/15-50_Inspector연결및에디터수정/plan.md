# Plan — Inspector 연결 및 에디터 수정

이 문서는 모든 task의 코드 구현이 완료된 상태에서 남아 있는 후처리 작업 계획을 기술합니다.
에디터 스크립트 코드 수정 2건(STEP 1~2)과 Inspector 참조 연결 자동화 4건(STEP 3~6)으로 구성됩니다.

---

## 구현 목표

에디터 스크립트의 버그 2건을 수정하고(STEP 1~2, 완료), 기존 에디터 스크립트에 Inspector 참조 자동 연결 로직을 추가한다(STEP 3~6).

- **STEP 1**: `BallSetupEditor.cs`의 `_maxBounces` 누락 수정 (완료)
- **STEP 2**: `SceneSetupEditor.cs`의 DamageTextManager 중복 생성 제거 (완료)
- **STEP 3**: `SceneSetupEditor.cs` — Ball.prefab 참조 자동 연결 추가
- **STEP 4**: `MonsterSetupEditor.cs` — Monster 프리팹 MonsterData 자동 연결 추가
- **STEP 5**: `SceneSetupEditor.cs` — 씬 WaveManager 참조 자동 연결 추가
- **STEP 6**: `UISetupEditor.cs` — DamageTextFx 프리팹 생성 + DamageTextManager 참조 자동 연결 추가

---

## 단계별 작업 계획

### STEP 1 — BallSetupEditor.cs `_maxBounces` 누락 수정

**배경**

`CreateBallDataAsset()` 내부에서 `_maxBounces` 필드를 설정하는 코드가 없어 BallData.asset의 `_maxBounces`가 0으로 생성된다. `Ball.cs`에서 `_remainingBounces = _ballData.MaxBounces`로 초기화하므로, 볼이 첫 충돌 즉시 소멸하는 문제가 발생한다.

**수정 파일: `Assets/_Project/Scripts/Editor/BallSetupEditor.cs`**

- `so.FindProperty("_criticalMultiplier").floatValue = 1.5f;` 바로 아래에 다음 라인을 추가한다.

```
so.FindProperty("_maxBounces").intValue = 10;
```

**주의**

이미 생성된 `BallData.asset`에는 코드 수정이 반영되지 않는다. 코드 수정 후 기존 에셋을 삭제하고 Ball System Setup을 재실행하거나, Inspector에서 `_maxBounces` 값을 직접 10으로 수정해야 한다.

---

### STEP 2 — SceneSetupEditor.cs DamageTextManager 중복 제거

**배경**

`Step6_PlaceManagers()`에서 `PlaceManager<DamageTextManager>("DamageTextManager")`를 호출해 DamageTextManager GameObject만 생성한다. `UISetupEditor`의 `Step4_SetupManagers()`도 DamageTextManager를 생성하며 자식에 `DamageTextPool`을 추가한다. SceneSetupEditor가 먼저 실행되면 UISetupEditor Step4가 스킵되어 `DamageTextPool`이 생성되지 않고, `DamageTextManager._poolParent`가 null이 되어 런타임 NullReferenceException이 발생한다.

**수정 파일: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`**

- `Step6_PlaceManagers()` 내부에서 아래 라인을 제거한다.

```
PlaceManager<DamageTextManager>("DamageTextManager");
```

DamageTextManager 생성은 UISetupEditor가 전담한다.

---

### STEP 3 — SceneSetupEditor.cs Ball.prefab 참조 자동 연결

**배경**

Ball.prefab의 `_ballData`(BallData.asset)와 CircleCollider2D의 PhysicsMaterial2D가 연결되지 않아 볼이 데이터를 참조하지 못하고 물리 반사가 동작하지 않는다.

**수정 파일: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`**

`SetupScene()` 호출 흐름에 `Step8_ConnectBallPrefabRefs()` 추가:
- `PrefabUtility.EditPrefabContentsScope`로 Ball.prefab 열기
- Ball 컴포넌트의 `_ballData` → `Assets/_Project/Data/BallData.asset` 연결
- CircleCollider2D의 `m_Material` 프로퍼티 → `Assets/_Project/Physics/BallBounce.physicsMaterial2D` 연결
- `[MenuItem("PurpleCow/Setup/Connect Ball Prefab Refs")]` 단독 실행 메뉴도 추가

**실행 순서**: BallSetupEditor 실행(BallData.asset + BallBounce.physicsMaterial2D 생성) → SceneSetupEditor 실행(Ball.prefab 생성 + 참조 연결)

---

### STEP 4 — MonsterSetupEditor.cs Monster 프리팹 MonsterData 자동 연결

**배경**

Monster 프리팹 4종의 `_monsterData`가 null이라 SpawnWave()에서 ApplyData() 호출 시 데이터가 없어 몬스터가 기본값으로 동작한다.

**수정 파일: `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`**

`SetupMonsterSystem()` 호출 흐름에 `ConnectMonsterDataToPrefabs()` 추가:
- `PrefabUtility.EditPrefabContentsScope`로 각 프리팹 열기
- MonsterBase 컴포넌트의 `_monsterData` → `Assets/_Project/Data/MonsterData_{이름}.asset` 연결
- 대상: Fluffy, Spider, StoneBug, ForestDeer 4종

**실행 순서**: MonsterSetupEditor 실행(MonsterData 에셋 생성 + 프리팹 연결)

---

### STEP 5 — SceneSetupEditor.cs 씬 WaveManager 참조 자동 연결

**배경**

씬 WaveManager의 `_waveDatas`, `_monsterPrefab`, `_poolParent`, `_spawnRoot`가 모두 null이라 게임 시작 시 NullReferenceException이 발생한다.

**수정 파일: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`**

`SetupScene()` 호출 흐름에 `Step9_ConnectWaveManagerRefs()` 추가:
- `_waveDatas` 배열 크기 20으로 설정 → WaveData_Wave1 ~ WaveData_Wave20 순서대로 연결
- `_monsterPrefab` → `Assets/_Project/Prefabs/Monster/Fluffy.prefab`
- `_poolParent` → 씬의 PoolRoot Transform
- `_spawnRoot` → 씬의 PoolRoot Transform (같은 오브젝트 사용)

**실행 순서**: MonsterSetupEditor 실행(WaveData 에셋 생성) → SceneSetupEditor 실행(WaveManager 참조 연결)

---

### STEP 6 — 씬 DamageTextManager Inspector 연결

**대상: Hierarchy의 `DamageTextManager` 오브젝트**

- `_prefab` → `DamageTextFx` 프리팹 연결
  - 프리팹이 없을 경우 `DamageTextFx.cs` 기반으로 프리팹을 수동 생성 필요
- `_poolParent` → `DamageTextManager` 하위의 `DamageTextPool` 오브젝트 연결
- 씬 저장: Ctrl+S

---

## 예상 변경/생성 파일 목록

| 구분 | 파일 | 변경 내용 |
|------|------|-----------|
| 수정 | `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` | `CreateBallDataAsset()`에 `_maxBounces` 초기값 10 설정 코드 추가 |
| 수정 | `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | `Step6_PlaceManagers()`에서 `PlaceManager<DamageTextManager>()` 호출 제거 |
| 수정 | `Assets/_Project/Prefabs/Ball/Ball.prefab` | `_ballData`, `_maxBounces`, `Circle Collider 2D Material` 연결 |
| 수정 | `Assets/_Project/Prefabs/Monster/Fluffy.prefab` | `_monsterData` → `MonsterData_Fluffy.asset` 연결 |
| 수정 | `Assets/_Project/Prefabs/Monster/Spider.prefab` | `_monsterData` → `MonsterData_Spider.asset` 연결 |
| 수정 | `Assets/_Project/Prefabs/Monster/StoneBug.prefab` | `_monsterData` → `MonsterData_StoneBug.asset` 연결 |
| 수정 | `Assets/_Project/Prefabs/Monster/ForestDeer.prefab` | `_monsterData` → `MonsterData_ForestDeer.asset` 연결 |
| 수정 | 씬 내 `WaveManager` | `_waveDatas`, `_monsterPrefab`, `_poolParent`, `_spawnRoot` 연결 |
| 수정 | 씬 내 `DamageTextManager` | `_prefab`, `_poolParent` 연결 |

---

## 주의사항

- STEP 1 코드 수정 후 BallData.asset을 재생성하면 STEP 3의 `_maxBounces` 직접 수정이 불필요하다. 반대로 재생성하지 않는다면 Inspector에서 직접 10을 입력해야 한다.
- STEP 2 수정 후 씬에 이미 SceneSetupEditor로 생성된 DamageTextManager가 존재한다면, UISetupEditor Step4를 다시 실행하거나 `DamageTextPool` 오브젝트를 수동으로 DamageTextManager 하위에 생성해야 한다.
- STEP 5에서 `_waveDatas` 배열에 WaveData 에셋이 하나라도 누락되면 해당 웨이브 진입 시 NullReferenceException이 발생한다. 0~19 전 인덱스를 반드시 채운다.
- Inspector 연결 작업(STEP 3~6)은 코드 수정(STEP 1~2) 이후에 진행하는 것을 권장한다.
