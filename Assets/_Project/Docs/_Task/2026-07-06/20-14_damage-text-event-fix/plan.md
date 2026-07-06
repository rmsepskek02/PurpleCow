# Plan — 레이저볼/지속대미지(DoT) 대미지 텍스트 미표시 통합 수정

research.md에서 확인한 대로, 레이저볼 가로 행 부가 피해(`LaserBallSkill.OnBallHit()`)와 DoT 틱 피해(`MonsterBase.UpdateDot()`)는 둘 다 `Ball.CalculateDamage()`를 거치지 않고 `MonsterBase.TakeDamage()`를 직접 호출해 `Ball.OnHitMonster` 이벤트가 발행되지 않는다. 이 문서는 두 호출부에서 각각 `Ball.OnHitMonster`를 직접 발행하도록(research.md의 방식 A) 최소 범위로 수정하는 계획을 다룬다.

## 구현 목표

- 레이저볼이 가로 행 전체에 부가 피해를 입힐 때, 직접 피격 대상뿐 아니라 행의 나머지 몬스터에게도 대미지 텍스트가 표시되도록 한다.
- DoT(지속 대미지) 틱마다 몬스터에게 대미지 텍스트가 표시되도록 한다.
- 기존 볼 충돌 경로(`Ball.CalculateDamage()`)의 대미지 텍스트 동작·치명타 표시는 전혀 변경하지 않는다(회귀 없음).

## 단계별 작업 계획

1. **`LaserBallSkill.OnBallHit()` 수정**
   - `monster.TakeDamage(LevelData.Value1)` 호출 직후, `Ball.OnHitMonster?.Invoke(monster, LevelData.Value1, false)`를 추가로 호출한다.
   - `isCritical`은 항상 `false`로 고정한다(레이저볼 부가 피해에는 치명타 개념이 없음).

2. **`MonsterBase.UpdateDot()` 수정**
   - `TakeDamage(tickDamage)` 호출 직후, `Ball.OnHitMonster?.Invoke(this, tickDamage, false)`를 추가로 호출한다.
   - `MonsterBase.cs` 상단에 `Ball` 네임스페이스 참조가 필요하면 추가한다(프로젝트에 별도 네임스페이스 구분이 없다면 추가 `using` 없이 바로 참조 가능한지 먼저 확인하고, 필요한 경우에만 추가한다).
   - `isCritical`은 여기서도 항상 `false`로 고정한다(DoT 틱에는 치명타 개념이 없음).

3. **회귀 확인(코드 리뷰 수준)**
   - `Ball.CalculateDamage()`의 `OnHitMonster?.Invoke(...)` 호출부는 수정하지 않으므로, 볼 직접 충돌 시 이벤트가 중복 발행되지 않는지 코드상으로 재확인한다.
   - `DamageTextManager.HandleHitMonster()`가 `MonsterBase, float, bool` 시그니처를 그대로 받는지 확인해 별도 수정이 필요 없음을 확인한다.

## 예상 변경/생성 파일 목록

| 파일 | 변경 내용 |
|---|---|
| `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs` | `OnBallHit()`에서 부가 피해 적용 직후 `Ball.OnHitMonster` 발행 추가 |
| `Assets/_Project/Scripts/Monster/MonsterBase.cs` | `UpdateDot()`에서 틱 피해 적용 직후 `Ball.OnHitMonster` 발행 추가 |

신규 생성 파일은 없다.

## 주의사항

- DevRules.md "단순함 우선" 원칙에 따라 `MonsterBase.TakeDamage()` 자체를 수정하는 방식(research.md 방식 B)은 채택하지 않는다. `TakeDamage()`를 공통 발행 지점으로 바꾸면 `Ball.CalculateDamage()`가 이미 발행하는 이벤트와 중복 발행될 위험이 있고, 이를 피하려면 `TakeDamage()` 시그니처 변경과 `CalculateDamage()` 쪽 로직 이전이 함께 필요해 변경 범위가 커지기 때문이다.
- 레이저볼 부가 피해와 DoT 틱 피해 모두 `isCritical`은 항상 `false`로 고정한다. 두 경로 모두 원본 스펙/기존 코드 어디에도 치명타 판정 로직이 없으므로 새로 치명타 확률을 계산해 부여하지 않는다.
- 두 신규 이벤트 발행 지점은 요청받은 파일(`LaserBallSkill.cs`, `MonsterBase.cs`)의 해당 메서드만 수정하며, `Ball.cs`, `DamageTextManager.cs`, `DamageTextFx.cs` 등 다른 파일은 수정하지 않는다(외과적 변경).
- **구현 착수 시 재확인이 필요한 모호한 부분**:
  - DoT 피해 텍스트를 일반 피해(볼 직접 충돌)와 시각적으로 구분할지 여부(예: 화상 전용 색상 등). `DamageTextFx.Play()`는 현재 `isCritical` 하나로만 색상/크기를 분기하며 DoT 전용 스타일 파라미터가 없다. 본 plan은 별도 시각 구분 없이 기존 "일반 피해" 스타일(`_normalColor`, 기본 크기)로 표시하는 것을 기본값으로 가정한다 — 구현 착수 전 사용자에게 이 가정이 맞는지 재확인이 필요하다.
  - 레이저볼 부가 피해 텍스트도 마찬가지로 일반 피해와 동일한 스타일로 표시하는 것을 기본값으로 가정한다.
  - 위 두 가정과 다른 표시 방식을 원할 경우, `DamageTextFx`/`DamageTextManager`에 대한 추가 변경이 필요하므로 이 plan의 범위를 벗어난다.
