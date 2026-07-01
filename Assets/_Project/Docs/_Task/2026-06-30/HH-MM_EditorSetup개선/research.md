# Research — EditorSetup 개선 (SkillSetupEditor 오타 수정 + SceneSetupEditor 신규 생성)

이 문서는 두 가지 에디터 스크립트 작업의 현재 상태를 분석한다.
첫째, SkillSetupEditor.cs에 하드코딩된 아이콘 경로의 대소문자 오타를 파악한다.
둘째, 씬 초기 세팅을 자동화하는 SceneSetupEditor 신규 생성을 위해 관련 스크립트들의 컴포넌트 구조를 파악한다.

---

## 현재 상태

### 작업 1: SkillSetupEditor 경로 오타

`Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`에서 4개 Active 스킬의 아이콘 경로가 소문자 `b`로 끝나고 있다.
실제 파일명은 대문자 `B`로 끝나는 `Ball`이다.

| 라인 | 현재 (오타) | 정상 |
|------|------------|------|
| 55 | `Ball_Ice_ball.png` | `Ball_Ice_Ball.png` |
| 67 | `Ball_Ghost_ball.png` | `Ball_Ghost_Ball.png` |
| 79 | `Ball_Laser_ball.png` | `Ball_Laser_Ball.png` |
| 91 | `Ball_Cluster_ball.png` | `Ball_Cluster_Ball.png` |

Ball_Fire_ball.png(라인 43)도 동일 패턴이지만 실제 파일명이 `Ball_Fire_ball.png`인지 `Ball_Fire_Ball.png`인지 요청서에 명시되지 않아 수정 대상에서 제외한다.
(요청서에 명시된 4개 파일: Ball_Ice_Ball, Ball_Ghost_Ball, Ball_Laser_Ball, Ball_Cluster_Ball)

### 작업 2: SceneSetupEditor 신규 생성 — 스크립트 분석

#### Ball.cs
- `Ball : MonoBehaviour, IPoolable`
- SerializeField: `BallData _ballData`
- 필요 컴포넌트: `Rigidbody2D`, `Collider2D` (CircleCollider2D)
- Tag 비교: `"Monster"`, `"Wall"`, `"Ground"`
- `BallLauncher.Instance.ReturnBall(this)` 호출 → BallLauncher 싱글톤 필요

#### BallLauncher.cs
- `BallLauncher : Singleton<BallLauncher>`
- SerializeField: `Ball _ballPrefab`, `Transform _poolParent`, `int _initialPoolSize`, `Transform _launchPoint`
- `OnEnable`에서 `InputHandler.Instance`, `GameManager.Instance` 참조 → 두 싱글톤이 씬에 존재해야 함

#### MonsterBase.cs
- `MonsterBase : MonoBehaviour, IPoolable`
- SerializeField: `MonsterData _monsterData`
- 필요 컴포넌트: Rigidbody2D(IsKinematic), BoxCollider2D
- Tag 비교: `"Ball"`

#### WaveManager.cs
- `WaveManager : Singleton<WaveManager>`
- SerializeField: `WaveData[] _waveDatas`, `MonsterBase _monsterPrefab`, `Transform _poolParent`, `int _initialPoolSize`, `Transform _spawnRoot`, `float _gridCellSize`, `float _monsterMoveDistance`, `float _bottomBoundaryY`
- `OnEnable`에서 `BallLauncher.OnAllBallsReturned`, `MonsterBase.OnMonsterDied` 이벤트 구독

#### GameManager.cs
- `GameManager : Singleton<GameManager>`
- SerializeField 없음 (순수 상태 머신)
- 이벤트: `OnGameStateChanged`

#### InputHandler.cs
- `InputHandler : Singleton<InputHandler>`
- SerializeField 없음
- 마우스 입력을 드래그/릴리즈 이벤트로 변환

#### SkillManager.cs
- `SkillManager : Singleton<SkillManager>`
- SerializeField 없음
- Passive 보너스 누적 및 Active 스킬 적용 담당

---

## 관련 파일 및 의존성

```
SceneSetupEditor (신규)
├── Ball 프리팹
│   ├── Ball.cs → BallData(SO) 필요
│   ├── Rigidbody2D, CircleCollider2D
│   └── Sprite: Ball_Nomal_Ball.png
├── Monster 프리팹 (4종)
│   ├── MonsterBase.cs → MonsterData(SO) 필요
│   ├── Rigidbody2D(Kinematic), BoxCollider2D
│   └── Sprites: Fluffy / Spider / StoneBug / ForestDeer
├── Block 프리팹 (4종)
│   ├── MonsterBase.cs → MonsterData(SO) 필요
│   ├── BoxCollider2D
│   └── Sprites: Block_1x1 / 1x2 / 2x1 / 2x2
└── 씬 오브젝트
    ├── Background (SpriteRenderer)
    ├── Wall_Left / Wall_Right / Ground (BoxCollider2D)
    └── Managers: GameManager, InputHandler, BallLauncher, WaveManager, SkillManager, UIManager
```

---

## 문제점 / 구현 대상 파악

### 작업 1
- 파일시스템이 대소문자를 구분하는 Linux/Mac 환경에서 경로 오타는 런타임 로드 실패를 유발한다
- SkillSetupEditor 실행 시 4개 아이콘이 `LogWarning`만 출력하고 연결되지 않는 상태
- 수정 범위: 라인 55, 67, 79, 91의 `_ball.png` → `_Ball.png`

### 작업 2
- 현재 씬을 수동으로 구성해야 해 초기 설정 비용이 높음
- MonsterBase 스크립트가 MonsterData ScriptableObject를 SerializeField로 요구하므로, 프리팹 생성 후 별도로 MonsterData를 연결해야 한다 (SceneSetupEditor 내에서 자동 연결 불가 또는 기본 에셋 경로 참조 필요)
- BallLauncher가 `_ballPrefab`과 `_poolParent`를 Inspector에서 연결받으므로 SceneSetupEditor에서 생성 후 자동 연결 처리 필요
- UIManager 스크립트 파일은 현재 분석 대상에 포함되지 않으나, 빈 GameObject 생성 + 스크립트 부착 방식으로 처리 (Inspector 연결은 별도 수동 작업)

---

## 결론

- **작업 1**은 4개 라인의 문자열 단순 수정으로 범위가 명확하다
- **작업 2**는 에디터 스크립트 신규 생성으로, 프리팹/씬 오브젝트 생성과 Inspector 연결까지 자동화하는 범위가 크다. 단계별로 이미 존재하면 스킵하는 안전 처리를 포함한다
- MonsterData, BallData ScriptableObject 에셋은 프리팹에 자동 연결하지 않으며, 생성 후 Inspector에서 수동 연결하거나 별도 Data Setup 에디터에서 처리하는 것으로 범위를 한정한다
