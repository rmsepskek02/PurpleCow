# Research — Ball 시스템 구현

이 문서는 Ball 시스템 구현을 위해 현재 프로젝트 상태를 파악한 내용입니다.
Core 시스템(Singleton, ObjectPool, IPoolable, GameManager, InputHandler)이 이미 구현된 상태이며,
Ball 폴더는 비어 있고 스프라이트 에셋 6종이 준비되어 있습니다.
Ball이 가져야 할 동작, 의존하는 Core 클래스, 구현 대상 클래스 목록을 정리합니다.

---

## 현재 상태

### Ball 관련 파일 현황

- `Assets/_Project/Scripts/Ball/` 폴더: 존재하나 .cs 파일 없음 (.meta만 존재)
- `Assets/_Project/Sprites/Ball/` 폴더: 스프라이트 6종 준비 완료

| 스프라이트 파일 | 설명 |
|----------------|------|
| `Ball_Nomal_Ball.png` | 기본 볼 |
| `Ball_Fire_ball.png` | 파이어 볼 |
| `Ball_Ice_Ball.png` | 아이스 볼 |
| `Ball_Ghost_Ball.png` | 고스트 볼 |
| `Ball_Laser_Ball.png` | 레이저 볼 |
| `Ball_Cluster_Ball.png` | 클러스터 볼 |

### 기타 폴더 현황 (구현 예정 폴더)

- `Assets/_Project/Scripts/Skill/` — 스킬 폴더 존재, 파일 없음
- `Assets/_Project/Scripts/Monster/` — 몬스터 폴더 존재, 파일 없음
- `Assets/_Project/Scripts/Data/` — 데이터 폴더 존재, 파일 없음
- `Assets/_Project/Scripts/Wave/` — 웨이브 폴더 존재, 파일 없음
- `Assets/_Project/Scripts/UI/` — UI 폴더 존재, 파일 없음

---

## 의존하는 Core 클래스들

### Singleton<T> (`Core/Singleton.cs`)
- 모든 매니저 베이스 클래스
- BallLauncher가 Singleton으로 구현됨

### IPoolable (`Core/IPoolable.cs`)
- `OnSpawn()` / `OnDespawn()` 인터페이스
- Ball 클래스가 반드시 구현해야 함 (ObjectPool에 의해 관리됨)

### ObjectPool<T> (`Core/ObjectPool.cs`)
- `T : MonoBehaviour, IPoolable` 제약
- `Get()` / `Return(T obj)` 메서드
- BallLauncher가 `ObjectPool<Ball>` 인스턴스를 소유하고 관리

### GameManager (`Core/GameManager.cs`)
- `GameState enum`: Ready / Playing / Result
- `event Action<GameState> OnGameStateChanged`
- BallLauncher가 GameState를 구독하여 Playing 상태일 때만 발사 허용

### InputHandler (`Core/InputHandler.cs`)
- `event Action<Vector2> OnDrag` — 드래그 방향 벡터 (정규화)
- `event Action OnRelease` — 터치/마우스 릴리즈
- BallLauncher가 구독하여 발사 방향 계산 및 발사 트리거로 사용

---

## 구현해야 할 클래스 목록 및 역할

### 1. BallData (ScriptableObject)
- 경로: `Assets/_Project/Scripts/Data/BallData.cs`
- SO 원본: `Assets/_Project/ScriptableObjects/BallData.asset` (구현 시 생성)
- 역할: Ball의 기본 수치 데이터 보관 (읽기 전용)
- 필드:
  - `_damage` (float): 기본 데미지
  - `_speed` (float): 이동 속도
  - `_criticalChance` (float): 치명타 확률 (0~1)
  - `_criticalMultiplier` (float): 치명타 데미지 배율

### 2. Ball (MonoBehaviour + IPoolable)
- 경로: `Assets/_Project/Scripts/Ball/Ball.cs`
- 역할: 볼 1개의 이동·충돌·피해 처리 담당
- IPoolable 구현: OnSpawn / OnDespawn으로 상태 초기화 및 정리
- 의존: BallData (수치), ObjectPool (자기 반납)
- 주요 동작:
  - FixedUpdate에서 방향 * 속도로 이동 (Rigidbody2D velocity)
  - 몬스터 충돌 시 데미지 계산 (치명타 여부 포함) 후 데미지 이벤트 발행
  - 벽(화면 경계) 충돌 시 반사 이동
  - 바닥 충돌 시 풀에 반납

### 3. BallLauncher (MonoBehaviour, Singleton)
- 경로: `Assets/_Project/Scripts/Ball/BallLauncher.cs`
- 역할: 입력을 받아 Ball을 발사하고 풀을 관리
- 의존: InputHandler (이벤트), GameManager (상태 확인), ObjectPool<Ball>
- 주요 동작:
  - InputHandler.OnDrag 구독 → 발사 방향 미리보기(가이드라인) 갱신
  - InputHandler.OnRelease 구독 → 풀에서 Ball 꺼내어 방향 설정 후 발사
  - GameState가 Playing일 때만 발사 허용
  - 발사 후 일정 딜레이 또는 모든 Ball이 반납된 후 다음 턴 이벤트 발행

---

## 게임 요구사항에서 Ball이 해야 할 동작

PDF를 직접 파싱할 수 없어 스프라이트 에셋과 Core 시스템 설계로부터 추론한 내용입니다.

### 발사 방식
- 드래그 방향으로 Ball을 발사 (Angry Birds / Bricks Breaker 스타일 추정)
- InputHandler.OnDrag → 조준 방향 표시
- InputHandler.OnRelease → 실제 발사

### 이동 및 충돌
- Ball은 직선 이동 후 벽에서 반사
- 몬스터(Enemy)에 충돌 시 데미지 적용
- 화면 하단(바닥)에 닿으면 풀에 반납

### 볼 종류 (스프라이트 기준 6종)
스프라이트 6종으로 미루어 볼 때 스킬 시스템과 연동되는 특수 볼 타입이 존재합니다.
현재 Ball 시스템에서는 기본 볼(Normal) 동작만 구현하고, 특수 볼 동작은 스킬 시스템 구현 시 확장합니다.

| 볼 종류 | 특이사항 |
|---------|---------|
| Normal | 기본 이동 + 반사 + 데미지 |
| Fire | (스킬 시스템에서 구현) |
| Ice | (스킬 시스템에서 구현) |
| Ghost | (스킬 시스템에서 구현) |
| Laser | (스킬 시스템에서 구현) |
| Cluster | (스킬 시스템에서 구현) |

### 데미지 계산
- 기본 데미지 × 치명타 배율 (치명타 확률로 확률적 적용)
- BallData ScriptableObject에 수치 보관

---

## 결론

- Ball 관련 .cs 파일 없음 → Ball.cs, BallLauncher.cs, BallData.cs 신규 생성 필요
- Core 시스템(5종)은 모두 구현 완료 → Ball 시스템이 즉시 의존 가능
- BallData는 Data 폴더에 ScriptableObject로 분리 (DevRules.md 준수)
- 이번 task 범위: Normal 볼의 이동·충돌·데미지·풀링 + BallLauncher 발사 로직
- 특수 볼 타입(Fire, Ice 등)은 스킬 시스템 task에서 별도 구현
