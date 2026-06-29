# Core 시스템 구현 - Research

이 문서는 Core 시스템 구현을 위해 현재 프로젝트 상태를 파악한 내용입니다.
현재 Scripts/Core 폴더는 비어있으며, 구현에 필요한 4개 클래스(Singleton, ObjectPool, GameManager, InputHandler)의 역할과 의존성을 정리했습니다.

---

## 현재 상태

- Unity 6000.3.10f1 + Universal 2D URP, Android 타겟
- `Assets/_Project/Scripts/Core/` 폴더 존재, 비어있음
- `Assets/InputSystem_Actions.inputactions` 파일 존재 (New Input System 패키지 포함)
- Architecture: Manager Pattern + Interfaces + C# event + ScriptableObject + Object Pooling

## 구현 대상 클래스

### Singleton<T>
- 모든 매니저의 베이스 클래스
- Awake에서 중복 인스턴스 방지
- DontDestroyOnLoad 미사용 (단일 씬)

### ObjectPool<T>
- 범용 제네릭 풀 (T : MonoBehaviour)
- 풀링 대상: Ball, Monster, 데미지 텍스트
- Get(), Return() 메서드, 부족 시 자동 생성

### GameManager
- Singleton<GameManager> 상속
- 게임 전체 상태 관리: Ready → Playing → Result
- C# event: OnGameStateChanged
- 성공/실패 판정, 재시작 처리

### InputHandler
- Singleton<InputHandler> 상속
- 터치/마우스 입력 감지 (New Input System 기반)
- C# event: OnDrag(Vector2), OnRelease()
- BallLauncher에서 구독 예정

## 의존성

- Singleton<T>: 외부 의존성 없음 (가장 먼저 구현)
- ObjectPool<T>: IPoolable 인터페이스 필요 (함께 생성)
- GameManager: Singleton<T> 필요
- InputHandler: Singleton<T> 필요

## 결론

4개 클래스 모두 신규 생성 필요. 외부 시스템 의존성 없이 독립적으로 구현 가능.
구현 순서: Singleton → ObjectPool(+IPoolable) → GameManager → InputHandler
