# Core 시스템 구현 - Plan

이 문서는 research.md를 바탕으로 Core 시스템 4개 클래스의 구체적인 구현 계획을 정리한 것입니다.
총 5개 스크립트를 새로 생성하며, 외부 의존성이 없어 독립적으로 구현 가능합니다.
**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**

---

## 구현 목표

- Core 클래스 완성으로 모든 매니저의 기반 구조 수립
- 입력 처리 이벤트 구조 완성

## 생성 파일 목록

| 파일 | 경로 |
|------|------|
| Singleton.cs | `Assets/_Project/Scripts/Core/Singleton.cs` |
| IPoolable.cs | `Assets/_Project/Scripts/Core/IPoolable.cs` |
| ObjectPool.cs | `Assets/_Project/Scripts/Core/ObjectPool.cs` |
| GameManager.cs | `Assets/_Project/Scripts/Core/GameManager.cs` |
| InputHandler.cs | `Assets/_Project/Scripts/Core/InputHandler.cs` |

## 단계별 구현 계획

### Step 1. Singleton<T>
- MonoBehaviour 상속 추상 클래스
- Instance 프로퍼티 (public static)
- Awake()에서 중복 체크: 이미 Instance 존재 시 Destroy(gameObject) 후 return
- virtual Awake로 자식 클래스 확장 가능

### Step 2. IPoolable 인터페이스
- OnSpawn(): 풀에서 꺼낼 때 호출
- OnDespawn(): 풀에 반납할 때 호출

### Step 3. ObjectPool<T>
- 제네릭 (T : MonoBehaviour, IPoolable)
- 생성자: prefab, parent, initialSize 받음
- Get(): 비활성 오브젝트 반환, 없으면 새로 생성 후 반환
- Return(T obj): 오브젝트 비활성화 후 풀에 반납

### Step 4. GameManager
- Singleton<GameManager> 상속
- GameState enum: Ready, Playing, Result
- event Action<GameState> OnGameStateChanged
- StartGame(), EndGame(bool isSuccess), RestartGame() 메서드
- 이 단계에서는 다른 매니저와 직접 연결하지 않음

### Step 5. InputHandler
- Singleton<InputHandler> 상속
- event Action<Vector2> OnDrag
- event Action OnRelease
- Update()에서 Input.GetMouseButton / Input.GetMouseButtonUp 으로 입력 감지
- 드래그 방향: 시작 위치 → 현재 위치 벡터를 정규화하여 OnDrag 발행
- 릴리즈: OnRelease 발행

## 주의사항

- DevRules.md 네이밍 규칙 준수 (private 필드: _camelCase, SerializeField)
- MonoBehaviour lifecycle 준수 (Awake: GetComponent, Start: 외부 참조)
- GameManager는 현재 단계에서 WaveManager/UIManager와 연결하지 않음
- InputHandler는 New Input System 대신 기본 Input 클래스 사용 (추후 교체 가능하도록 인터페이스 분리 고려)
