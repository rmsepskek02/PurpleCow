# Research — PDF 스펙 정합

이 문서는 PDF 요구사항과 현재 구현 사이의 차이를 분석한다. 분석 대상은 BallData 기본값, 스킬 레벨 시스템, 액티브 5종 스킬 효과, 패시브 7종 → 5종 교체, 스킬 장착 제한, 스킬 선택 로직, MonsterData 이름 총 7개 항목이다.

---

## 현재 상태

### BallData.cs
- `_damage`: 기본값 10f (PDF 요구 8)
- `_criticalChance`: 기본값 0.1f (PDF 요구 0)
- `_criticalMultiplier`: 기본값 2.0f (PDF 요구 1.5)
- BallSetupEditor.cs에서 에셋 생성 시 위 값들을 하드코딩으로 세팅
- 구조 자체는 변경 불필요, 기본값만 수정 대상

### SkillData.cs
- 레벨 개념 없음. `_value1 / _value2 / _value3` 3개 float 필드만 존재
- 스킬 하나당 에셋 파일 1개 구조
- 레벨별 수치를 저장할 구조가 전혀 없음

### 액티브 스킬 5종

| 스킬 | 현재 구현 | PDF 요구 |
|------|----------|---------|
| FireBallSkill | OverlapCircle 범위 폭발 (반경 Value1, 추가DMG Value2) | DOT 중첩 (지속시간, 중첩수, 초당피해), 레벨별 볼 데미지 |
| IceBallSkill | ApplyFreeze(턴수) | 확률+냉동+이동속도감소+추가피해, 레벨별 볼 데미지 |
| LaserBallSkill | RaycastAll 직선 관통 즉시 처리 | 같은 행 전체 추가 피해 (레벨별 수치), 레벨별 볼 데미지 |
| GhostBallSkill | 관통 (SetGhostMode) | 관통 동일, 레벨별 볼 데미지만 추가 |
| ClusterBallSkill | 서브볼 개수(Value1) 발사 | 확률+서브볼 피해, 레벨별 볼 데미지 |

- FireBallSkill의 로직이 범위 폭발 → DOT 중첩으로 완전히 달라짐
- IceBallSkill의 ApplyFreeze(턴수) → 확률 판정 + 초단위 지속시간 + 이동속도 감소율 + 추가 피해율로 인터페이스 확장 필요
- LaserBallSkill은 RaycastAll(직선 전체) → 같은 행만 타격으로 범위 변경
- ClusterBallSkill은 서브볼 개수 고정 → 확률 판정 + 서브볼 피해 수치 추가

### 패시브 스킬 — 현재 7종

| 클래스 | 역할 |
|--------|------|
| DamageUpPassive | 데미지 배율 보너스 |
| CritChanceUpPassive | 치명타 확률 보너스 |
| CritDamageUpPassive | 치명타 데미지 보너스 |
| SpeedUpPassive | 볼 속도 보너스 |
| BounceUpPassive | 반사 횟수 보너스 |
| KillShotPassive | 몬스터 처치 시 서브볼 발사 |
| LastHitPassive | 볼 반납 직전 최저 HP 몬스터 추가 타격 |

- 위 7개 클래스 전부 삭제 대상
- PassiveSkillId 열거형 7개 항목 전부 제거 후 5개로 재정의

### SkillManager.cs
- `_equippedActiveSkill`: 단일 BallSkillBase만 보관 → 액티브 최대 4개 지원 필요
- `_passiveSkills`: List 보관 중이나 상한 없음 → 최대 2개 제한 필요
- Passive 보너스 필드 5개(DamageMultiplier, CritChance, CritDamage, Speed, Bounce)는 신규 패시브 설계에 따라 전면 재설계 필요

### SkillFactory.cs
- PassiveSkillId 7개 매핑 → 5개로 교체 필요
- 신규 패시브 클래스 5개와 연결 필요

### SkillSelectionPanel.cs
- `ShowRandomSkills()`: _allSkillDatas 풀에서 무작위 선택
- 스킬 레벨/장착 수 조건 없음
- 트리거 조건: OnEnable (웨이브 클리어 이벤트와 연동됨을 WaveManager에서 확인)
- 중복 카드 방지 로직 없음

### WaveManager.cs
- `CheckWaveCleared()` → `OnWaveCleared` 이벤트 발행 → UIManager가 SkillSelectionPanel 오픈
- 처치 수 기반 스킬 선택지 노출 로직 없음
- `_currentKillCount` 필드 없음

### MonsterBase.cs
- DOT 처리 메서드 없음 (`ApplyFreeze`만 존재)
- 이동속도 감소 필드 없음
- 치명타 확률 증가(1회) 적용 구조 없음

### MonsterSetupEditor.cs
- 생성 이름 배열: `{ "Fluffy", "Spike", "Blaze", "Stone" }`
- PDF 기준: Spike → Spider, Blaze → StoneBug, Stone → ForestDeer

---

## 관련 파일 및 의존성

```
SkillData.cs
 ├── BallSkillBase.cs (생성자에서 SkillData 수신)
 │    ├── FireBallSkill.cs
 │    ├── IceBallSkill.cs
 │    ├── LaserBallSkill.cs
 │    ├── GhostBallSkill.cs
 │    └── ClusterBallSkill.cs
 ├── PassiveSkillBase.cs
 │    ├── (7개 기존 패시브 — 삭제)
 │    └── (5개 신규 패시브 — 생성)
 ├── SkillFactory.cs (SkillId 기반 인스턴스 생성)
 └── SkillSetupEditor.cs (에셋 파일 생성)

SkillManager.cs
 ├── Ball.cs (ApplySkillToBall, 보너스 수치 참조)
 └── SkillSelectionPanel.cs (EquipActiveSkill, AddPassiveSkill 호출)

WaveManager.cs
 └── SkillSelectionPanel.cs (OnWaveCleared 이벤트 수신)

MonsterBase.cs
 ├── FireBallSkill.cs (신규 DOT 적용 메서드 필요)
 ├── IceBallSkill.cs (이동속도 감소 메서드 필요)
 └── AmethystDaggerPassive / EmeraldDaggerPassive (치명타 확률 1회 증가 메서드 필요)
```

---

## 문제점 / 구현 대상 파악

### 문제 1 — BallData 기본값 불일치
BallSetupEditor에서 damage=10, critChance=0.1, critMult=2.0으로 에셋을 생성한다. PDF는 8 / 0 / 1.5를 요구한다.

### 문제 2 — SkillData에 레벨 시스템 없음
현재 value1~3 3개 float만 있다. Lv.1/2/3 각각 다른 수치와 볼 데미지가 필요하므로, 레벨별 데이터를 배열 또는 중첩 구조체로 저장하는 구조 추가가 필요하다.

### 문제 3 — 액티브 스킬 로직 전면 변경
- FireBallSkill: OverlapCircle 폭발 → DOT 코루틴 중첩
- IceBallSkill: 턴수 프리즈 → 초단위 프리즈 + 이동속도 감소 + 추가 피해 + 확률 판정
- LaserBallSkill: 직선 전체 Raycast → 같은 행(row) 내 모든 적 추가 피해
- ClusterBallSkill: 서브볼 개수 고정 → 확률 판정 후 서브볼 + 서브볼 피해 수치 전달

### 문제 4 — 패시브 7종 전면 교체
기존 7개 클래스 파일을 삭제하고 5개 신규 파일을 생성해야 한다. PassiveSkillId 열거형도 재정의 필요. SkillManager의 보너스 필드들도 새 패시브 설계에 맞게 변경된다.

### 문제 5 — SkillManager 장착 한도 없음
액티브 4개, 패시브 2개 제한 로직이 없다.

### 문제 6 — 스킬 선택 로직 미구현
- 트리거: 웨이브 클리어 → 처치 수 조건 충족으로 변경
- 보유 여부/레벨에 따른 카드 풀 필터링 없음
- 중복 카드 방지 없음

### 문제 7 — MonsterData 이름 불일치
Spike/Blaze/Stone → Spider/StoneBug/ForestDeer

### 문제 8 — MonsterBase DOT/이동속도 감소 미지원
신규 스킬들이 요구하는 상태이상 메서드가 MonsterBase에 없다.

---

## 결론

총 수정 범위는 아래와 같다.

- **수정**: BallData.cs 없음(에셋 수치만), BallSetupEditor.cs, SkillData.cs, BallSkillBase.cs, PassiveSkillBase.cs, 액티브 5개, SkillManager.cs, SkillFactory.cs, SkillSelectionPanel.cs, WaveManager.cs, MonsterBase.cs, MonsterSetupEditor.cs, SkillSetupEditor.cs
- **삭제**: 기존 패시브 7개 파일
- **생성**: 신규 패시브 5개 파일

가장 파급 범위가 큰 변경은 SkillData 레벨 구조 추가(SkillFactory, SkillSetupEditor, 모든 스킬 클래스에 영향)와 패시브 전면 교체(SkillManager 보너스 필드 재설계 포함)이다.
