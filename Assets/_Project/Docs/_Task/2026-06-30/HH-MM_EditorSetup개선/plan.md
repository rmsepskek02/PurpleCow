# Plan — EditorSetup 개선 (SkillSetupEditor 오타 수정 + SceneSetupEditor 신규 생성)

이 문서는 두 가지 에디터 스크립트 작업의 구현 계획을 정의한다.
작업 1은 SkillSetupEditor.cs의 아이콘 경로 오타 4곳을 수정한다.
작업 2는 씬 초기 세팅을 자동화하는 SceneSetupEditor.cs를 신규 생성하며, Ball/Monster/Block 프리팹 생성과 씬 오브젝트 배치를 포함한다.

---

## 구현 목표

### 작업 1
- `SkillSetupEditor.cs`의 아이콘 경로에서 소문자 `ball`로 끝나는 4개 경로를 대문자 `Ball`로 수정한다
- 수정 후 `PurpleCow/Setup/Skill System Setup` 메뉴 실행 시 4개 아이콘이 정상 연결된다

### 작업 2
- `PurpleCow/Setup/Scene Setup` 메뉴 항목으로 실행되는 `SceneSetupEditor.cs`를 신규 생성한다
- 실행 시 Ball 프리팹 1종, Monster 프리팹 4종, Block 프리팹 4종을 생성하고 씬에 필요한 오브젝트들을 배치한다
- 각 단계는 이미 존재하면 스킵하여 중복 실행에도 안전하다

---

## 단계별 작업 계획

### [작업 1] SkillSetupEditor.cs 오타 수정

수정 파일: `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`

| 라인 | 수정 전 | 수정 후 |
|------|---------|---------|
| 55 | `Ball_Ice_ball.png` | `Ball_Ice_Ball.png` |
| 67 | `Ball_Ghost_ball.png` | `Ball_Ghost_Ball.png` |
| 79 | `Ball_Laser_ball.png` | `Ball_Laser_Ball.png` |
| 91 | `Ball_Cluster_ball.png` | `Ball_Cluster_Ball.png` |

총 4개 문자열만 변경하며, 나머지 코드는 일절 수정하지 않는다.

---

### [작업 2] SceneSetupEditor.cs 신규 생성

생성 파일: `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`

#### Step 1. Ball 프리팹 생성

- 경로: `Assets/_Project/Prefabs/Ball/Ball.prefab`
- 이미 존재하면 스킵
- 구성:
  - `SpriteRenderer`: `Assets/_Project/Sprites/Ball/Ball_Nomal_Ball.png` 연결
  - `Rigidbody2D`: GravityScale = 0, CollisionDetectionMode = Continuous
  - `CircleCollider2D`: 기본값 (PhysicsMaterial2D는 에셋 경로가 미정이므로 수동 연결 안내 로그 출력)
  - `Ball` 스크립트 부착
  - Tag = `"Ball"` (Tag가 프로젝트에 존재할 경우 설정, 없으면 경고 로그)

#### Step 2. Monster 프리팹 4종 생성

- 경로: `Assets/_Project/Prefabs/Monster/{이름}.prefab`
- 이미 존재하면 스킵

| 프리팹 이름 | 스프라이트 경로 |
|------------|----------------|
| `Fluffy.prefab` | `Assets/_Project/Sprites/Monster/Fluffy.png` |
| `Spider.prefab` | `Assets/_Project/Sprites/Monster/Spider.png` |
| `StoneBug.prefab` | `Assets/_Project/Sprites/Monster/StoneBug.png` |
| `ForestDeer.prefab` | `Assets/_Project/Sprites/Monster/ForestDeer.png` |

- 공통 구성:
  - `SpriteRenderer`: 각 스프라이트 연결
  - `Rigidbody2D`: GravityScale = 0, IsKinematic = true
  - `BoxCollider2D`: 기본값
  - `MonsterBase` 스크립트 부착
  - Tag = `"Monster"`
  - `MonsterData` ScriptableObject는 자동 연결하지 않음 (생성 후 Inspector에서 수동 연결)

#### Step 3. Block 프리팹 4종 생성

- 경로: `Assets/_Project/Prefabs/Monster/{이름}.prefab`
- 이미 존재하면 스킵

| 프리팹 이름 | 스프라이트 경로 |
|------------|----------------|
| `Block_1x1.prefab` | `Assets/_Project/Sprites/Monster/Block_1x1.png` |
| `Block_1x2.prefab` | `Assets/_Project/Sprites/Monster/Block_1x2.png` |
| `Block_2x1.prefab` | `Assets/_Project/Sprites/Monster/Block_2x1.png` |
| `Block_2x2.prefab` | `Assets/_Project/Sprites/Monster/Block_2x2.png` |

- 공통 구성:
  - `SpriteRenderer`: 각 스프라이트 연결
  - `BoxCollider2D`: 기본값
  - `MonsterBase` 스크립트 부착
  - Tag = `"Monster"`
  - `MonsterData` ScriptableObject는 자동 연결하지 않음

#### Step 4. 씬 오브젝트 배치 — Background

- 이름: `Background`
- 이미 씬에 존재하면 스킵
- 구성:
  - `SpriteRenderer`: `Assets/_Project/Sprites/Background/Background_1_Stage.png` 연결
  - Position: (0, 0, 0) 중앙 배치

#### Step 5. 씬 오브젝트 배치 — 벽/바닥

- 이미 씬에 존재하면 스킵

| 오브젝트 이름 | Tag | Position | BoxCollider2D Size |
|--------------|-----|----------|--------------------|
| `Wall_Left` | `"Wall"` | (-5.5f, 0, 0) | (0.2f, 20f) |
| `Wall_Right` | `"Wall"` | (5.5f, 0, 0) | (0.2f, 20f) |
| `Ground` | `"Ground"` | (0, -10f, 0) | (12f, 0.2f) |

- Tag가 프로젝트에 없으면 경고 로그만 출력하고 진행

#### Step 6. 씬 오브젝트 배치 — Manager 오브젝트들

- 이미 씬에 존재하면 스킵 (오브젝트 이름으로 중복 확인)

| 오브젝트 이름 | 부착 스크립트 |
|--------------|--------------|
| `GameManager` | `GameManager` |
| `InputHandler` | `InputHandler` |
| `BallLauncher` | `BallLauncher` |
| `WaveManager` | `WaveManager` |
| `SkillManager` | `SkillManager` |
| `UIManager` | `UIManager` |

- 각 오브젝트는 빈 GameObject로 생성 후 스크립트 부착
- UIManager 스크립트가 없으면 `AddComponent` 시 오류 발생 가능 → 예외 처리 후 경고 로그 출력

#### Step 7. BallLauncher Inspector 연결

- `BallLauncher` 오브젝트의 `SerializedObject`를 통해 연결:
  - `_ballPrefab`: Step 1에서 생성한 `Ball.prefab`
  - `_poolParent`: 빈 GameObject `PoolRoot`를 생성하고 연결
- `_launchPoint`는 자동 연결하지 않음 (씬 내 위치가 게임별로 다름, 수동 연결 안내 로그 출력)

---

## 예상 변경/생성 파일 목록

### 수정
- `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs` — 라인 55, 67, 79, 91 경로 오타 수정

### 신규 생성
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
- `Assets/_Project/Prefabs/Ball/Ball.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/Fluffy.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/Spider.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/StoneBug.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/ForestDeer.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/Block_1x1.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/Block_1x2.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/Block_2x1.prefab` (메뉴 실행 시 생성)
- `Assets/_Project/Prefabs/Monster/Block_2x2.prefab` (메뉴 실행 시 생성)

---

## 주의사항

1. **PhysicsMaterial2D 미연결**: Ball의 CircleCollider2D에 BallBounce 물리 머티리얼 경로가 확정되지 않아 자동 연결을 보류한다. 스크립트 내 주석과 Debug.LogWarning으로 수동 연결을 안내한다.

2. **MonsterData / BallData ScriptableObject 미연결**: Monster/Ball 프리팹에 Data 에셋을 자동 연결하지 않는다. ScriptableObject 에셋 생성은 별도 DataSetupEditor 또는 수동으로 처리한다.

3. **Tag 사전 등록 필요**: "Ball", "Monster", "Wall", "Ground" Tag가 Project Settings > Tags & Layers에 미리 등록되어 있어야 자동 설정이 적용된다. 미등록 시 경고 로그만 출력하고 계속 진행한다.

4. **UIManager 스크립트 미존재 가능성**: UIManager.cs가 없는 경우 AddComponent 호출이 실패한다. try-catch로 예외를 잡고 로그를 출력한 뒤 나머지 단계를 계속 진행한다.

5. **WaveManager 참조 미연결**: WaveManager의 `_waveDatas`, `_monsterPrefab`, `_spawnRoot` 등은 자동 연결하지 않는다. 복잡한 게임 데이터이므로 수동 연결로 남긴다.

6. **BallLauncher `_launchPoint` 미연결**: 발사 위치는 게임 씬 레이아웃에 따라 다르므로 수동 연결로 남긴다.

---

**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**
