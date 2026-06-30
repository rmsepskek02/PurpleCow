# Research — 플레이어 액티브 스킬 시스템

이 문서는 플레이어 액티브 스킬 시스템 구현에 앞서 현재 프로젝트 상태와 구현 대상을 파악한 내용입니다.
기존 로그라이크 스킬 선택 시스템(볼/패시브 스킬)과 분리된, 버튼 입력으로 발동하는 4종 고정 스킬의 요구사항과 의존성을 정리합니다.

---

## 현재 상태

- Unity 6000.3.10f1, Android 타겟
- `Assets/_Project/Scripts/Skill/` 폴더 존재, 하위에 `Active/`, `Passive/`, `Base/` 서브폴더 생성되어 있음 (스크립트 없음)
- `Assets/_Project/Scripts/Core/` 폴더 존재, 아직 구현된 스크립트 없음 (Core 시스템 plan.md 작성 완료, 미구현)
- `Assets/_Project/Scripts/UI/` 폴더 존재, 비어있음
- 프로젝트 전체 스크립트 미구현 상태
- 기술 결정 확정: ScriptableObject(read-only data), Generic Singleton, C# event, ObjectPool

## 관련 파일 및 의존성

### 의존 대상 (구현 전제 조건)

| 대상 | 이유 | 현재 상태 |
|------|------|-----------|
| `Singleton<T>` | PlayerActiveSkillController가 싱글톤으로 동작해야 하는 경우 참조 필요 | 미구현 (Core plan.md 존재) |
| `GameManager` | 게임 상태(Playing 여부) 확인, 스킬 발동 가능 여부 판단에 필요 | 미구현 |
| 볼 관련 클래스 | 버서크(볼 속도 배율), 분신(현재 활성 볼 복사) 스킬 적용 대상 | 미구현 |
| 몬스터 관련 클래스 | 마법 폭격(전체 적 고정 피해), 필드 동결(전체 적 정지) 스킬 적용 대상 | 미구현 |

### 신규 생성 대상

| 파일 | 경로 | 역할 |
|------|------|------|
| `PlayerActiveSkillSO.cs` | `Assets/_Project/Scripts/Skill/Active/` | 스킬 1종 데이터 컨테이너 ScriptableObject |
| `PlayerActiveSkillController.cs` | `Assets/_Project/Scripts/Skill/Active/` | 4개 스킬 발동 로직 및 쿨타임 상태 관리 |
| `SkillButtonUI.cs` | `Assets/_Project/Scripts/UI/` | 개별 스킬 버튼의 쿨타임 오버레이 및 텍스트 처리 |
| SO 에셋 4종 | `Assets/_Project/Data/Skills/` | 스킬별 수치 데이터 에셋 |

## 문제점 / 구현 대상 파악

### 스킬 4종 확정 사항

| 스킬명 | 쿨타임 | 핵심 동작 | 수치 |
|--------|--------|-----------|------|
| 필드 동결 | 30초 | 씬 내 모든 몬스터 이동 및 행동 정지 | 정지 지속 4초 |
| 버서크 | 30초 | 현재 활성화된 모든 볼의 속도에 배율 적용 | 속도 1.5배, 지속 6초 |
| 분신 | 30초 | 현재 활성화된 모든 볼을 복사하여 추가 발사, 복사본은 발사 횟수 제한 후 소멸 | 2회 발사 후 소멸 |
| 마법 폭격 | 30초 | 씬 내 모든 몬스터에게 즉시 고정 피해 | 30 고정 피해 |

### 스킬 시스템 동작 규칙

- 게임 시작 시 4개 모두 쿨타임 상태에서 시작 (30초 대기 필요)
- 쿨타임 시작 시점: 스킬 발동 즉시 (효과 종료 후가 아님)
- 사용 가능 타이밍: 볼 비행 중을 포함하여 언제든 발동 가능
- 분신 스킬: 현재 활성화된 볼 전체 복사. "추가 1개 고정"이 아님에 주의

### UI 확정 사항

- 위치: 하단 여백(캐릭터 HP바 아래) 가로 배치, 스킬 버튼 4개
- 쿨타임 표현: Image fillAmount Clockwise 방식 반투명 오버레이 + 중앙 남은 시간 텍스트
- 쿨타임 종료 시: 오버레이 제거, 버튼 인터랙션 활성화

### ScriptableObject 설계 (PlayerActiveSkillSO)

스킬마다 개별 에셋 1개씩 생성. 필드는 사용하지 않는 스킬 종류에서는 기본값 유지.

| 필드명 | 타입 | 용도 |
|--------|------|------|
| `_skillName` | string | 스킬 표시명 |
| `_cooldown` | float | 쿨타임 (초) |
| `_duration` | float | 지속형 스킬 지속 시간 (버서크, 필드 동결 공통) |
| `_speedMultiplier` | float | 버서크 볼 속도 배율 |
| `_cloneLaunchCount` | int | 분신 복사본 발사 횟수 제한 |
| `_damageAmount` | float | 마법 폭격 고정 피해량 |
| `_icon` | Sprite | 버튼에 표시할 아이콘 |

### 구현 범위 제한 (현재 단계)

- 시각 이펙트(필드 동결 화면 이펙트, 마법 폭격 이펙트, 버서크/분신 캐릭터 이펙트)는 구현 제외
- 볼/몬스터 클래스가 미구현 상태이므로, 인터페이스 기반으로 연결 지점만 명확히 설계

### 미구현 의존성 처리 방향

볼 및 몬스터 클래스가 아직 없으므로 `PlayerActiveSkillController`는 인터페이스를 통해 느슨하게 결합한다.

- 몬스터: `IFreezable` (정지/해제), `IDamageable` (피해 적용) 인터페이스를 통해 접근
- 볼: `ISpeedModifiable` (속도 배율 적용/해제), 복사를 위한 볼 참조 수집은 씬 내 활성 오브젝트 탐색 또는 정적 이벤트로 위임

## 결론

신규 스크립트 3개(PlayerActiveSkillSO, PlayerActiveSkillController, SkillButtonUI)와 SO 에셋 4종 생성이 필요하다.
볼/몬스터 클래스가 미구현 상태이므로 인터페이스를 먼저 정의하여 실제 연결은 해당 클래스 구현 시점에 처리하는 방식으로 진행한다.
Core 시스템(Singleton, GameManager)도 미구현이므로, PlayerActiveSkillController는 MonoBehaviour로 구현하고 추후 Singleton 상속으로 교체 가능하도록 설계한다.
