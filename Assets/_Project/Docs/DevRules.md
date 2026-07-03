# DevRules.md

이 문서는 개발 에이전트 전용 규칙입니다.
Unity C# 코드 작성 원칙, git 규칙, 구현 방식을 정의합니다.
공통 규칙은 [CLAUDE.md](../../../../CLAUDE.md)를 함께 참고하세요.

---

## 1. 코딩 전 먼저 생각하기 (Thinking Before Coding)

- 구현에 앞서 접근 방식을 명시적으로 설명한다
- 가정이 필요한 경우 가정임을 명시하고 진행한다
- 모호한 요구사항은 구현 전에 질문한다
- 복잡한 작업은 단계별 계획을 먼저 제시하고 동의를 구한다

## 2. 단순함 우선 (Simplicity First)

- 불필요한 추상화, 과도한 설계 패턴을 피한다
- 3줄로 해결되는 것을 클래스로 만들지 않는다
- 기존 코드 스타일과 네이밍 컨벤션을 유지한다

## 3. 목표 중심 실행 (Goal-Driven Execution)

- plan.md 기반으로만 구현을 진행한다
- 완료 후 무엇이 변경되었는지 간결하게 보고한다
- 빌드 오류가 예상되는 변경은 사전에 경고한다

## 4. 네이밍 컨벤션

### C# 코드

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase | `BallController`, `WaveManager` |
| 인터페이스 | I + PascalCase | `IDamageable`, `ISkill` |
| 메서드 | PascalCase | `FireBall()`, `TakeDamage()` |
| 프로퍼티 | PascalCase | `CurrentHp`, `IsAlive` |
| public 변수 | PascalCase | `MaxHp` |
| private 변수 | _camelCase | `_currentSpeed`, `_isDead` |
| SerializeField | _camelCase | `[SerializeField] float _speed` |
| 상수 | UPPER_SNAKE_CASE | `MAX_WAVE_COUNT` |
| 열거형 | PascalCase | `SkillType`, `BallType` |
| 열거형 값 | PascalCase | `SkillType.FireBall` |
| 코루틴 | Co + PascalCase | `CoSpawnMonster()` |

### 파일 / 에셋

| 대상 | 규칙 | 예시 |
|------|------|------|
| 스크립트 | PascalCase | `BallController.cs` |
| 프리팹 | PascalCase | `Ball_Fire.prefab` |
| 씬 | PascalCase | `GameScene.unity` |

## 5. Unity 규칙

### SerializeField 기준
- `public` 변수 직접 노출 금지
- Inspector 조정이 필요한 값 → `[SerializeField] private`
- 외부 접근이 필요한 값 → `private` 백킹 필드 + `public` 프로퍼티

```csharp
// ❌ 금지
public float speed;

// ✅ Inspector 노출만 필요
[SerializeField] private float _speed;

// ✅ 외부 접근도 필요
[SerializeField] private float _speed;
public float Speed => _speed;
```

| Inspector 노출 대상 | 여부 |
|--------------------|------|
| 컴포넌트 참조 (프리팹 연결) | ✅ |
| 밸런스 수치 (데미지, 속도 등) | ❌ ScriptableObject로 분리 |
| 씬 내 오브젝트 참조 | ✅ |
| 디버그용 임시 값 | ❌ |

---

### 싱글톤 구현 방식
- 제네릭 싱글톤 베이스 클래스 사용
- DontDestroyOnLoad 미사용 (단일 씬)
- 싱글톤 대상: `GameManager`, `WaveManager`, `SkillManager`, `ObjectPool`, `UIManager`

```csharp
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    protected virtual void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this as T;
    }
}
```

---

### 이벤트 규칙
- 모든 시스템 간 통신은 **C# event** 사용
- 같은 GameObject 내부: C# event
- 다른 GameObject/시스템 간: C# event (static event 활용)
- 구독/해제는 반드시 `OnEnable/OnDisable` 쌍으로 관리

---

### ScriptableObject 사용 범위
- SO 원본 데이터는 **읽기 전용**으로만 사용
- 런타임 수치 변경은 별도 런타임 클래스에서 관리
- 하드코딩 금지 항목: 스킬 수치, 웨이브 구성, 몬스터 스탯, 볼 기본 스탯

| SO 파일 | 담는 데이터 |
|---------|------------|
| `SkillData` | 스킬명, 레벨별 수치, 볼 데미지, 아이콘 |
| `WaveTableData` | 웨이브 20개의 몬스터 구성/처치 조건을 한 asset에 테이블로 관리 |
| `MonsterData` | 몬스터 HP, 이동속도, 크기 |
| `BallData` | 기본 데미지, 치명타 확률/데미지율 |

---

### 오브젝트 풀링
- 범용 제네릭 풀 사용
- 풀링 대상: 볼, 몬스터, 데미지 텍스트
- 부족 시 자동 추가 생성, 최대 사이즈 제한 없음

```csharp
ObjectPool<Ball>.Get();
ObjectPool<Ball>.Return(ball);
```

---

### MonoBehaviour 생명주기 규칙

| 메서드 | 역할 |
|--------|------|
| `Awake()` | 컴포넌트 캐싱, 자기 자신 초기화 |
| `Start()` | 다른 오브젝트 참조, 초기 상태 설정 |
| `OnEnable()` | 이벤트 구독 |
| `OnDisable()` | 이벤트 해제 |
| `Update()` | 입력 처리, 상태 체크 |
| `FixedUpdate()` | 물리 처리 (볼 이동, 충돌) |

- `Awake()`에서 `GetComponent` 캐싱, `Start()`에서 타 오브젝트 참조
- `Update()`에서 `GetComponent` 호출 금지
- 물리 관련 처리는 반드시 `FixedUpdate()` 사용

## 6. Git 규칙

- `git push`, `git restore`, `git reset`, `git revert`, `git checkout` 등 프로젝트 상태를 변경하는 명령어는 사용자가 명시적으로 요청한 경우에만 실행한다
- `git status`, `git log`, `git diff`, `git show` 등 읽기 전용 명령어는 자유롭게 사용한다
- 커밋은 사용자가 명시적으로 요청한 경우에만 생성한다
- 커밋 메시지 형식: `[타입] 설명` (예: `[feat] 볼 발사 로직 구현`, `[fix] 충돌 데미지 계산 오류 수정`)
- 타입 종류: `feat`, `fix`, `refactor`, `docs`, `chore`
- 머지된 브랜치를 재구성(`git checkout -B <branch> origin/<default>`)하기 전에는 반드시 해당 브랜치 자체를 `git fetch origin <branch>`로 먼저 최신화하고, `git log <branch>..origin/<branch>`로 로컬이 모르는 원격 커밋이 있는지 확인한 뒤 진행한다. `--force-with-lease`는 로컬이 마지막으로 fetch한 시점의 원격 상태를 기준으로 안전장치가 작동하므로, fetch 없이 오래된 로컬 정보로 force push하면 그 사이 원격에 추가된 커밋(예: 사용자가 직접 푸시한 커밋)을 덮어쓸 위험이 있다
