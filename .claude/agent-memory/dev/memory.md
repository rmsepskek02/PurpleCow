# Dev Agent Memory

## 2026-06-29

### 작업: Core 시스템 구현 및 더미 파일 삭제

**작업 내용:**
- `Assets/_Project/Scripts/NewMonoBehaviourScript.cs` 및 `.meta` 파일 삭제
- `Assets/_Project/Scripts/Core/` 디렉토리 생성 후 5개 스크립트 생성

**생성 파일:**
- `Singleton.cs` - MonoBehaviour 상속 추상 제네릭 싱글톤 베이스 클래스
- `IPoolable.cs` - OnSpawn/OnDespawn 인터페이스
- `ObjectPool.cs` - 제네릭 오브젝트 풀 (T : MonoBehaviour, IPoolable)
- `GameManager.cs` - Singleton<GameManager> 상속, GameState enum, 이벤트 기반 상태 관리
- `InputHandler.cs` - Singleton<InputHandler> 상속, 마우스 드래그/릴리즈 이벤트

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: implement Core system (Singleton, IPoolable, ObjectPool, GameManager, InputHandler) and remove dummy script`
- push 완료 (new branch)

**주요 결정사항:**
- DontDestroyOnLoad 미사용 (단일 씬 구조, DevRules.md 기준)
- InputHandler는 기본 Input 클래스 사용 (New Input System 미사용)
- GameManager는 WaveManager/UIManager와 직접 연결 없음 (이후 단계에서 연결)
- ObjectPool은 static이 아닌 인스턴스 기반으로 구현 (사용처에서 풀 인스턴스를 직접 관리)
- namespace 없이 작성 (DevRules.md에 명시 없으므로 생략)

---

## 2026-06-29

### 작업: Ball 시스템 구현 (BallData, Ball, BallLauncher, BallSetupEditor)

**작업 내용:**
- plan.md 경로: `Assets/_Project/Docs/_Task/2026-06-30/10-00_Ball시스템구현/plan.md`
- 4개 파일 신규 생성 (Data, Ball, Editor 폴더 신규 생성 포함)

**생성 파일:**
- `Assets/_Project/Scripts/Data/BallData.cs` — ScriptableObject, damage/speed/criticalChance/criticalMultiplier 프로퍼티
- `Assets/_Project/Scripts/Ball/Ball.cs` — IPoolable 구현, OnHitMonster static event, Tag 분기 충돌 처리, CalculateDamage 치명타 계산
- `Assets/_Project/Scripts/Ball/BallLauncher.cs` — Singleton<BallLauncher> 상속, ObjectPool<Ball> 소유, InputHandler/GameManager 이벤트 구독, OnAllBallsReturned static event
- `Assets/_Project/Scripts/Editor/BallSetupEditor.cs` — #if UNITY_EDITOR 가드, MenuItem("PurpleCow/Setup/Ball System Setup"), 태그 등록/PhysicsMaterial2D 생성/BallData 에셋 생성 자동화

**Git:**
- 브랜치: `claude/recent-plan-review-xq2hsm`
- 커밋: `feat: implement Ball system (BallData, Ball, BallLauncher, BallSetupEditor)`
- push 완료 (기존 브랜치에 추가)

**주요 결정사항:**
- InputHandler.OnDrag/OnRelease, GameManager.OnGameStateChanged는 instance event이므로 OnEnable/OnDisable에서 Instance 통해 구독 (plan.md 표기는 축약이었음)
- FixedUpdate에서 velocity.normalized * speed로 속력 유지 (bounciness=1이어도 프레임 간 미세 감쇠 방지)
- BallSetupEditor는 #if UNITY_EDITOR 가드 사용 (Editor 폴더에 위치하므로 이중 보호)
- BallData 기본값은 SerializedObject를 통해 설정 (직접 필드 접근 불가 - private 필드이므로)
- OnEnable에서 Instance 접근 — Singleton Awake보다 OnEnable이 나중이므로 안전
- _activeBallCount 언더플로 방지는 미구현 (plan.md 범위 외)
