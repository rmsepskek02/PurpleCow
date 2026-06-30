# Plan — 플레이어 액티브 스킬 시스템

이 문서는 research.md를 바탕으로 플레이어 액티브 스킬 시스템의 구체적인 구현 계획을 정리한 것입니다.
스크립트 3개 신규 생성, SO 에셋 4종 생성을 수행하며, 볼/몬스터 미구현에 대비한 인터페이스 설계를 포함합니다.
**이 문서에 대한 사용자의 명시적인 승인 후에만 구현을 시작합니다.**

---

## 구현 목표

- 버튼 입력으로 발동하는 4종 액티브 스킬(필드 동결, 버서크, 분신, 마법 폭격) 로직 구현
- ScriptableObject 기반 수치 관리로 Inspector에서 밸런스 조정 가능
- 쿨타임 오버레이(fillAmount Clockwise) + 남은 시간 텍스트 UI 구현
- 볼/몬스터 클래스 미구현 상황을 고려한 인터페이스 기반 연결 지점 확보

## 단계별 작업 계획

### Step 1. 인터페이스 정의

파일: `Assets/_Project/Scripts/Skill/Base/IFreezable.cs`, `ISpeedModifiable.cs`

스킬 효과 적용 대상의 인터페이스를 먼저 정의하여 PlayerActiveSkillController가 볼/몬스터 구현 없이 컴파일 가능하도록 한다.

- `IFreezable`
  - `void Freeze(float duration)` — 지정 시간 동안 이동/행동 정지
  - `void Unfreeze()` — 강제 해제 (필요 시)
- `IDamageable` (이미 DevRules.md에서 언급, 재확인 후 없으면 신규 생성)
  - `void TakeDamage(float amount)`
- `ISpeedModifiable`
  - `void ApplySpeedMultiplier(float multiplier)`
  - `void ResetSpeedMultiplier()`

> 볼/몬스터 클래스는 추후 해당 인터페이스를 implements하여 자동 연결됨.

---

### Step 2. PlayerActiveSkillSO (ScriptableObject)

파일: `Assets/_Project/Scripts/Skill/Active/PlayerActiveSkillSO.cs`

```csharp
[CreateAssetMenu(menuName = "PurpleCow/Skill/PlayerActiveSkill")]
public class PlayerActiveSkillSO : ScriptableObject
{
    [SerializeField] private string _skillName;
    [SerializeField] private float _cooldown;
    [SerializeField] private float _duration;          // 버서크, 필드 동결
    [SerializeField] private float _speedMultiplier;   // 버서크
    [SerializeField] private int _cloneLaunchCount;    // 분신
    [SerializeField] private float _damageAmount;      // 마법 폭격
    [SerializeField] private Sprite _icon;

    public string SkillName => _skillName;
    public float Cooldown => _cooldown;
    public float Duration => _duration;
    public float SpeedMultiplier => _speedMultiplier;
    public int CloneLaunchCount => _cloneLaunchCount;
    public float DamageAmount => _damageAmount;
    public Sprite Icon => _icon;
}
```

---

### Step 3. PlayerActiveSkillController

파일: `Assets/_Project/Scripts/Skill/Active/PlayerActiveSkillController.cs`

#### 역할
- 4개 스킬의 쿨타임 상태(남은 시간) 관리
- 스킬 발동 메서드 제공 (SkillButtonUI에서 호출)
- 각 스킬 효과 실행 (코루틴 기반 지속 효과)
- 쿨타임 변경 시 이벤트 발행 → SkillButtonUI에서 구독

#### 주요 구조

```
enum SkillType { FieldFreeze, Berserk, Clone, MagicBombard }
```

```
[SerializeField] private PlayerActiveSkillSO[] _skillDataArray;  // 인스펙터에서 4개 슬롯 연결
private float[] _remainingCooldowns;                             // 런타임 쿨타임 추적

public static event Action<SkillType, float> OnCooldownChanged; // (스킬 타입, 남은 시간)
```

#### 메서드

| 메서드 | 설명 |
|--------|------|
| `TryActivateSkill(SkillType type)` | 쿨타임 체크 후 스킬 발동, 즉시 쿨타임 시작 |
| `ExecuteFieldFreeze(PlayerActiveSkillSO data)` | 씬 내 IFreezable 전체 수집 후 Freeze() 호출, 코루틴으로 duration 후 Unfreeze() |
| `ExecuteBerserk(PlayerActiveSkillSO data)` | 씬 내 ISpeedModifiable 전체 수집 후 ApplySpeedMultiplier() 호출, 코루틴으로 duration 후 ResetSpeedMultiplier() |
| `ExecuteClone(PlayerActiveSkillSO data)` | 씬 내 활성 볼 목록 수집 후 각 볼 위치/방향으로 복사본 생성, cloneLaunchCount 소진 시 소멸 처리 |
| `ExecuteMagicBombard(PlayerActiveSkillSO data)` | 씬 내 IDamageable 전체 수집 후 TakeDamage() 호출 |
| `CoCooldown(int index)` | 코루틴: 쿨타임 감소, 매 프레임 OnCooldownChanged 발행 |

#### 씬 내 오브젝트 수집 방법

볼/몬스터 미구현 단계에서는 `FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)`으로 수집 후 인터페이스 캐스팅. 볼/몬스터 구현 완료 시 별도 정적 리스트로 교체 가능하도록 수집 로직을 별도 private 메서드로 분리.

---

### Step 4. SkillButtonUI

파일: `Assets/_Project/Scripts/UI/SkillButtonUI.cs`

#### 역할
- 스킬 버튼 1개 단위 컴포넌트 (프리팹에 부착)
- 쿨타임 오버레이(Image fillAmount Clockwise) 및 남은 시간 텍스트 관리
- 버튼 클릭 시 PlayerActiveSkillController.TryActivateSkill() 호출

#### 주요 구조

```
[SerializeField] private PlayerActiveSkillSO _skillData;  // SO 연결
[SerializeField] private SkillType _skillType;            // 이 버튼이 담당하는 스킬
[SerializeField] private Image _icon;
[SerializeField] private Image _cooldownOverlay;          // fillAmount 방식 오버레이
[SerializeField] private TextMeshProUGUI _cooldownText;   // 남은 시간 표시
[SerializeField] private Button _button;
```

#### 동작

- `Awake()`: 컴포넌트 캐싱, 아이콘 이미지 설정
- `OnEnable()`: `PlayerActiveSkillController.OnCooldownChanged` 구독
- `OnDisable()`: 구독 해제
- `OnCooldownChanged` 핸들러:
  - 자신의 SkillType에 해당하는 이벤트만 처리
  - 남은 시간 > 0: fillAmount = remainingCooldown / totalCooldown, 텍스트 표시, 버튼 비활성화
  - 남은 시간 == 0: fillAmount = 0, 텍스트 숨김, 버튼 활성화
- 버튼 onClick: `PlayerActiveSkillController.Instance.TryActivateSkill(_skillType)` 호출

---

### Step 5. SO 에셋 4종 생성

경로: `Assets/_Project/Data/Skills/`

| 에셋명 | skillName | cooldown | duration | speedMultiplier | cloneLaunchCount | damageAmount |
|--------|-----------|----------|----------|-----------------|------------------|--------------|
| `SK_FieldFreeze.asset` | 필드 동결 | 30 | 4 | 1 | 0 | 0 |
| `SK_Berserk.asset` | 버서크 | 30 | 6 | 1.5 | 0 | 0 |
| `SK_Clone.asset` | 분신 | 30 | 0 | 1 | 2 | 0 |
| `SK_MagicBombard.asset` | 마법 폭격 | 30 | 0 | 1 | 0 | 30 |

> SO 에셋은 Unity Editor에서 CreateAssetMenu를 통해 직접 생성하거나 스크립트로 생성.

---

## 예상 변경/생성 파일 목록

| 구분 | 파일 | 경로 |
|------|------|------|
| 신규 | `IFreezable.cs` | `Assets/_Project/Scripts/Skill/Base/` |
| 신규 | `IDamageable.cs` | `Assets/_Project/Scripts/Skill/Base/` (미존재 시) |
| 신규 | `ISpeedModifiable.cs` | `Assets/_Project/Scripts/Skill/Base/` |
| 신규 | `PlayerActiveSkillSO.cs` | `Assets/_Project/Scripts/Skill/Active/` |
| 신규 | `PlayerActiveSkillController.cs` | `Assets/_Project/Scripts/Skill/Active/` |
| 신규 | `SkillButtonUI.cs` | `Assets/_Project/Scripts/UI/` |
| 신규 | `SK_FieldFreeze.asset` | `Assets/_Project/Data/Skills/` |
| 신규 | `SK_Berserk.asset` | `Assets/_Project/Data/Skills/` |
| 신규 | `SK_Clone.asset` | `Assets/_Project/Data/Skills/` |
| 신규 | `SK_MagicBombard.asset` | `Assets/_Project/Data/Skills/` |

변경 파일 없음. 전부 신규 생성.

---

## 주의사항

- **시각 이펙트 제외**: 필드 동결, 마법 폭격, 버서크/분신 캐릭터 이펙트는 이번 구현 대상이 아님. 이펙트 호출 훅(빈 메서드 또는 주석)만 남겨두기
- **분신 스킬 복사 범위**: "현재 모든 활성 볼" 전체 복사. 추가 1개 고정 아님
- **쿨타임 시작 시점**: 스킬 발동 즉시 시작. 효과 종료 후 시작 아님
- **게임 시작 시 쿨타임**: PlayerActiveSkillController.Start()에서 모든 스킬 쿨타임을 _cooldown 값으로 초기화
- **Singleton 미구현 대응**: PlayerActiveSkillController는 현재 일반 MonoBehaviour로 구현. Core 시스템 구현 완료 후 Singleton<PlayerActiveSkillController> 상속으로 교체
- **DevRules.md 네이밍 규칙 준수**: private 필드 _camelCase, SerializeField 적용
- **SO는 읽기 전용**: 런타임 쿨타임 추적은 PlayerActiveSkillController 내 float[] 배열로 별도 관리
