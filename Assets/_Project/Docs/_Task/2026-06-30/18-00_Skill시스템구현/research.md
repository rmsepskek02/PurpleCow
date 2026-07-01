# Research — Skill 시스템 구현

이 문서는 Skill 시스템 구현에 앞서 현재 프로젝트 상태를 파악하고, 기존 Ball/Monster/Core 시스템과의 의존성을 분석한 결과입니다.
Active 스킬(볼 타입별 5종)과 Passive 스킬(7종)의 구현 대상을 명확히 하고,
SkillManager 및 SkillData ScriptableObject 설계를 위한 기초 정보를 정리합니다.

---

## 현재 상태

### Scripts/Skill 폴더
- `Assets/_Project/Scripts/Skill/Base/` — 폴더만 존재, 스크립트 없음
- `Assets/_Project/Scripts/Skill/Active/` — 폴더만 존재, 스크립트 없음
- `Assets/_Project/Scripts/Skill/Passive/` — 폴더만 존재, 스크립트 없음

Skill 시스템은 아직 전혀 구현되지 않은 상태입니다.

### 구현 완료된 시스템
| 시스템 | 주요 클래스 | 상태 |
|--------|------------|------|
| Core | `Singleton<T>`, `ObjectPool<T>`, `IPoolable`, `GameManager`, `InputHandler` | 완료 |
| Ball | `BallData`, `Ball`, `BallLauncher` | 완료 |
| Monster | `MonsterData`, `MonsterBase`, `WaveData`, `WaveManager` | 완료 |

---

## 관련 파일 및 의존성

### Core 시스템 (읽기 전용 의존)
- `Singleton<T>` — SkillManager가 상속
- `ObjectPool<T>` — ClusterBall 서브볼 풀링에 활용 가능
- `GameManager` — 게임 상태(Playing/Result) 구독 필요

### Ball 시스템 (확장 대상)
- `Ball.cs`
  - `OnCollisionEnter2D` — 충돌 처리 로직이 이미 구현됨
  - `Ball.OnHitMonster` (static event) — 스킬 효과 연동 가능
  - `BallData` SO 참조 구조 — SkillData SO와 유사한 패턴 적용 예정
- `BallLauncher.cs`
  - `LaunchBall()` — Cluster 스킬의 서브볼 추가 발사와 연동 필요
  - `OnAllBallsReturned` — 스킬 쿨다운/턴 종료 감지 가능

### Monster 시스템 (연동 대상)
- `MonsterBase.cs`
  - `TakeDamage(float)` — 스킬 효과(지속 데미지, 둔화)가 호출할 메서드
  - `MoveDown(float)` — Ice 스킬의 이동 정지 효과와 연동
  - `OnMonsterDied` — Passive 스킬의 "처치 시 발동" 조건 감지

### 스프라이트 에셋 현황
**BallSkillIcon (Active 스킬 아이콘 — 5종):**
| 파일명 | 볼 타입 |
|--------|--------|
| `Ball_Nomal_Ball.png` | Normal |
| `Ball_Fire_ball.png` | Fire |
| `Ball_Ice_Ball.png` | Ice |
| `Ball_Ghost_Ball.png` | Ghost |
| `Ball_Laser_Ball.png` | Laser |
| `Ball_Cluster_Ball.png` | Cluster |

**Passive 아이콘 (7종):**
| 파일명 | ID |
|--------|-----|
| `icon_passive_3000.png` | 3000 |
| `icon_passive_3002.png` | 3002 |
| `icon_passive_3003.png` | 3003 |
| `icon_passive_3006.png` | 3006 |
| `icon_passive_3007.png` | 3007 |
| `icon_passive_3013.png` | 3013 |
| `icon_passive_3014.png` | 3014 |

---

## 문제점 / 구현 대상 파악

### Active 스킬 (볼 타입별 5종)
채용과제 PDF 및 Ball 시스템 plan.md 기준으로 확인된 볼 타입:

| 볼 타입 | 스킬 효과 | 아이콘 |
|--------|----------|--------|
| Normal | 기본 데미지, 반사 | Ball_Nomal_Ball.png |
| Fire | 충돌 시 범위 폭발 데미지 (스플래시) | Ball_Fire_ball.png |
| Ice | 충돌 몬스터 이동 정지 (1턴) | Ball_Ice_Ball.png |
| Ghost | 몬스터 관통 (충돌 후 소멸 없음) | Ball_Ghost_Ball.png |
| Laser | 직선 관통, 모든 몬스터 데미지 | Ball_Laser_Ball.png |
| Cluster | 충돌 시 서브볼 N개 추가 생성 | Ball_Cluster_Ball.png |

> Normal은 기본 볼(스킬 없음) 취급. Active 스킬 구현 대상은 Fire/Ice/Ghost/Laser/Cluster 5종.

### Passive 스킬 (7종)
스프라이트 ID 기준으로 추정되는 패시브 목록 (채용과제 PDF 내 상세 설명 참조):

| ID | 추정 효과 |
|----|----------|
| 3000 | 데미지 증가 (기본 공격력 % 상승) |
| 3002 | 크리티컬 확률 증가 |
| 3003 | 크리티컬 데미지 배율 증가 |
| 3006 | 볼 속도 증가 |
| 3007 | 볼 반사 횟수 증가 |
| 3013 | 몬스터 처치 시 추가 데미지 볼 발사 |
| 3014 | 볼이 바닥에 닿기 전 마지막 몬스터 추가 타격 |

> ID 기반 추정이므로 PDF 확인 후 조정이 필요할 수 있습니다.

### 핵심 설계 문제
1. **Active 스킬과 Ball의 관계**: Ball.cs를 직접 수정하지 않고 스킬 효과를 확장해야 함 → `BallSkillBase` 컴포넌트를 Ball에 런타임 부착 방식 사용
2. **Passive 스킬 적용 시점**: 몬스터 충돌, 턴 종료, 데미지 계산 등 다양한 시점에 개입 → Event 훅 기반 설계 필요
3. **SkillManager의 역할**: 현재 장착된 Active 1개 + Passive N개를 관리하고, 볼 발사 시 스킬 적용을 중개
4. **ScriptableObject 분리**: 스킬 수치를 하드코딩하지 않기 위해 `SkillData` SO 필수

---

## 결론

- Skill 폴더(Base/Active/Passive)는 이미 준비되어 있으나 내용이 전무한 상태
- Core/Ball/Monster 시스템이 모두 완성되어 있어 스킬 시스템이 의존할 기반은 갖춰짐
- Active 5종 + Passive 7종의 스킬이 구현 대상
- Ball.cs의 충돌/데미지 흐름을 최소한으로 수정하면서 스킬 효과를 주입할 수 있는 구조 설계가 핵심 과제
- `SkillManager (Singleton)`, `SkillData (SO)`, `BallSkillBase` (abstract), `PassiveSkillBase` (abstract), 각 구체 클래스, `SkillSetupEditor` 총 15개 내외 파일 신규 생성 예정
