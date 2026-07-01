# Research — Inspector 연결 및 에디터 수정

이 문서는 Inspector 미연결 항목 4건과 에디터 스크립트 코드 문제 2건을 파악하고 원인을 분석한다.
코드 문제는 에디터 스크립트 재실행 전 반드시 선행 수정이 필요하며, Inspector 연결 항목은 씬/프리팹 저장 후 Push로 완료된다.
각 항목의 원인과 영향 범위를 명확히 파악해 plan.md 작성의 기반으로 삼는다.

---

## 현재 상태

### 코드 문제 2건

**1. BallSetupEditor.cs — `_maxBounces` 프로퍼티 미설정**

- 파일: `Assets/_Project/Scripts/Editor/BallSetupEditor.cs`
- `CreateBallDataAsset()` 내부에서 `so.FindProperty("_maxBounces")` 설정 코드가 없음
- 결과: 생성된 `BallData.asset`의 `_maxBounces` 값이 0으로 저장됨
- `Ball.cs`에서 `_remainingBounces = _ballData.MaxBounces`로 초기화하므로, MaxBounces가 0이면 첫 충돌 즉시 볼이 소멸됨

**2. SceneSetupEditor.cs — DamageTextManager 중복 생성 충돌**

- 파일: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
- `Step6_PlaceManagers()`에 `PlaceManager<DamageTextManager>("DamageTextManager")` 코드가 포함되어 있음
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs`의 `Step4_SetupManagers()`도 DamageTextManager를 생성하고 자식으로 DamageTextPool을 추가함
- SceneSetupEditor → UISetupEditor 순서로 실행할 경우, UISetupEditor의 Step4가 "이미 존재함"으로 스킵되어 DamageTextPool이 생성되지 않음
- 결과: `DamageTextManager._poolParent`가 null인 상태가 되어 런타임 NullReferenceException 발생

---

### Inspector 미연결 4건

**3. Ball.prefab — BallData 및 PhysicsMaterial 미연결**

- 파일: `Assets/_Project/Prefabs/Ball/Ball.prefab`
- `_ballData: {fileID: 0}` — BallData.asset 미연결
- `_maxBounces: 0` — 기본값 0 상태 (볼 즉시 소멸)
- CircleCollider2D `Material: {fileID: 0}` — BallBounce.physicsMaterial2D 미연결

**4. Monster 프리팹 4종 — MonsterData 에셋 미연결**

- 파일:
  - `Assets/_Project/Prefabs/Monster/Fluffy.prefab`
  - `Assets/_Project/Prefabs/Monster/Spider.prefab`
  - `Assets/_Project/Prefabs/Monster/StoneBug.prefab`
  - `Assets/_Project/Prefabs/Monster/ForestDeer.prefab`
- 각 프리팹의 `_monsterData: {fileID: 0}` — 대응하는 MonsterData 에셋이 미연결된 상태

**5. 씬 WaveManager 오브젝트 — 다수 필드 미연결**

- 파일: `Assets/Scenes/SampleScene.unity`
- `_waveDatas: []` — WaveData_Wave1 ~ Wave20 미연결 (20종)
- `_monsterPrefab: {fileID: 0}` — 몬스터 프리팹 미연결
- `_poolParent: {fileID: 0}` — PoolRoot 미연결
- `_spawnRoot: {fileID: 0}` — 스폰 루트 미연결

**6. 씬 DamageTextManager 오브젝트 — 프리팹 및 풀 미연결**

- 파일: `Assets/Scenes/SampleScene.unity`
- `_prefab: {fileID: 0}` — DamageTextFx 프리팹 미연결
- `_poolParent: {fileID: 0}` — DamageTextPool 미연결

---

## 관련 파일 및 의존성

| 파일 | 의존 대상 |
|------|-----------|
| `BallSetupEditor.cs` | `BallData.asset` 생성 |
| `SceneSetupEditor.cs` + `UISetupEditor.cs` | DamageTextManager / DamageTextPool 생성 (충돌) |
| `Ball.prefab` | `BallData.asset`, `BallBounce.physicsMaterial2D` |
| `Fluffy.prefab` | `MonsterData_Fluffy.asset` |
| `Spider.prefab` | `MonsterData_Spider.asset` |
| `StoneBug.prefab` | `MonsterData_StoneBug.asset` |
| `ForestDeer.prefab` | `MonsterData_ForestDeer.asset` |
| `SampleScene.unity / WaveManager` | `WaveData_Wave1~20.asset` 20종, Monster 프리팹, PoolRoot, 스폰 루트 |
| `SampleScene.unity / DamageTextManager` | DamageTextFx 프리팹, DamageTextPool |

---

## 문제점 / 구현 대상 파악

**코드 수정 대상 2건**

- `BallSetupEditor.cs`: `CreateBallDataAsset()` 내 `_maxBounces` 값을 SerializedProperty로 설정하는 코드 추가 필요
- `SceneSetupEditor.cs`: `Step6_PlaceManagers()`에서 DamageTextManager 생성 라인 제거 필요 (UISetupEditor가 단독으로 담당하도록 역할 분리)

**Inspector 연결 대상 4건**

- Ball.prefab에 BallData.asset 및 BallBounce.physicsMaterial2D 연결
- Monster 프리팹 4종에 대응 MonsterData 에셋 연결
- SampleScene WaveManager에 WaveData 20종, Monster 프리팹, PoolRoot, 스폰 루트 연결
- SampleScene DamageTextManager에 DamageTextFx 프리팹, DamageTextPool 연결

---

## 결론

코드 수정 2건(BallSetupEditor `_maxBounces` 누락, SceneSetupEditor DamageTextManager 중복 생성)은 에디터 스크립트 재실행 전에 반드시 선행되어야 한다. 이를 수정하지 않으면 에디터 툴 재실행 시 동일한 문제가 재발한다.

Inspector 연결 4건은 Unity 에디터에서 씬/프리팹을 직접 수정한 뒤 저장하고 Push하면 완료된다. 코드 수정과 Inspector 연결은 순서상 코드 수정을 먼저 완료한 후 Inspector 연결을 진행하는 것이 안전하다.
