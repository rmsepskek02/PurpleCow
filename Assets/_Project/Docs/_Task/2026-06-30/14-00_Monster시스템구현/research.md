# Research — Monster 시스템 구현

이 문서는 Monster 시스템 구현을 위해 현재 프로젝트 상태를 파악한 내용입니다.
Ball 시스템(Ball.cs, BallLauncher.cs, BallData.cs)이 이미 구현된 상태이며,
Monster 폴더에는 .cs 파일이 없고 스프라이트 에셋 6종(캐릭터 4종 + 블록 2종)이 준비되어 있습니다.
Monster가 의존해야 할 Core 클래스, Ball.OnHitMonster 이벤트 시그니처, WaveManager 연동 구조를 정리합니다.

---

## 현재 상태

### Monster 관련 파일 현황

- `Assets/_Project/Scripts/Monster/` 폴더: 존재하나 .cs 파일 없음 (폴더만 존재)
- `Assets/_Project/Scripts/Wave/` 폴더: 존재하나 .cs 파일 없음 (폴더만 존재)
- `Assets/_Project/Sprites/Monster/` 폴더: 스프라이트 6종 준비 완료

| 스프라이트 파일 | 유형 |
|----------------|------|
| `Fluffy.png` | 캐릭터형 몬스터 |
| `Spider.png` | 캐릭터형 몬스터 |
| `StoneBug.png` | 캐릭터형 몬스터 |
| `ForestDeer.png` | 캐릭터형 몬스터 |
| `Block_1x1.png` | 블록형 오브젝트 |
| `Block_1x2.png` | 블록형 오브젝트 |
| `Block_2x1.png` | 블록형 오브젝트 |
| `Block_2x2.png` | 블록형 오브젝트 |

---

## 의존하는 Core 클래스 및 기존 시스템

### Singleton<T> (`Core/Singleton.cs`)
- `abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour`
- `public static T Instance { get; private set; }`
- WaveManager가 Singleton으로 구현됨

### IPoolable (`Core/IPoolable.cs`)
- `void OnSpawn()` / `void OnDespawn()` 인터페이스
- MonsterBase가 반드시 구현해야 함 (ObjectPool에 의해 관리됨)

### ObjectPool<T> (`Core/ObjectPool.cs`)
- `T : MonoBehaviour, IPoolable` 제약
- `Get()` / `Return(T obj)` 메서드
- WaveManager가 `ObjectPool<MonsterBase>` 인스턴스를 소유하고 관리

### GameManager (`Core/GameManager.cs`)
- `GameState enum`: Ready / Playing / Result
- `event Action<GameState> OnGameStateChanged`
- WaveManager가 구독하여 게임 상태에 따라 웨이브 흐름 제어

### BallLauncher (`Ball/BallLauncher.cs`)
- `public static event Action OnAllBallsReturned`
- 모든 볼이 바닥에 닿아 풀에 반납된 후 발행됨
- WaveManager가 구독하여 볼 턴 종료 시점을 알고 다음 턴(몬스터 전진)을 진행

---

## Ball.OnHitMonster 이벤트 시그니처

```csharp
// Ball.cs (구현 완료)
public static event Action<float, bool> OnHitMonster;
// 파라미터: (float damage, bool isCritical)
// 발행 시점: Ball이 "Monster" 태그 오브젝트와 충돌하는 순간
// 문제: static event이므로 어느 MonsterBase가 맞았는지 특정 불가
```

### 충돌 식별 방식 결정

`Ball.OnHitMonster`는 static event이기 때문에 "어느 몬스터가 맞았는가"를 구별할 수 없습니다.
MonsterBase는 자신의 Collider2D를 이용하여 **OnCollisionEnter2D**로 Ball 충돌을 직접 감지하고,
Ball.OnHitMonster 이벤트 대신 물리 충돌 콜백에서 TakeDamage를 처리합니다.

단, Ball의 데미지 계산(치명타 등)은 Ball 측에서만 계산되고 static event로 외부에 공개됩니다.
따라서 MonsterBase의 OnCollisionEnter2D에서 Ball을 감지하면,
Ball 컴포넌트의 public 메서드나 static event 결과값을 활용해야 합니다.

**결론:** Ball.cs 충돌 처리 흐름을 분석하면,
Ball이 Monster 태그 충돌 시 `OnHitMonster(damage, isCritical)` 발행 → 반사 지속.
Monster 측 OnCollisionEnter2D에서 충돌 감지 시, Ball에서 계산된 데미지를 직접 받아야 합니다.

가장 단순한 방식: **MonsterBase.OnCollisionEnter2D**에서 Ball 컴포넌트를 가져와
`Ball.LastCalculatedDamage`(추가 예정) 또는 파라미터로 받는 방법을 검토.

현재 Ball.cs 분석 결과:
- `CalculateDamage()`는 private — 외부 접근 불가
- `OnHitMonster` 발행 직후 반사 지속 (데미지는 static event로만 전달됨)
- MonsterBase가 static event를 구독하고, Ball의 물리 충돌을 감지하여 두 정보를 함께 처리

**최종 결정:** MonsterBase의 `OnCollisionEnter2D`에서 Ball 태그를 감지하고,
`Ball.OnHitMonster` static event 구독을 통해 마지막 발행된 데미지를 TakeDamage에 적용.
(동일 프레임 내 Ball 충돌 → OnHitMonster 발행 → MonsterBase 처리 순서가 보장됨)

---

## 게임 구조 분석 (채용과제 PDF + 에셋 기반 추론)

### 몬스터 동작 (Breakout/Brick-Breaker 스타일 추정)
- 몬스터는 그리드 위에 배치되어 있음
- 볼이 충돌하면 HP가 감소하고 0이 되면 사망 (풀에 반납)
- 매 턴마다 몬스터가 한 칸씩 아래로 전진
- 몬스터가 바닥 경계에 도달하면 게임 오버

### Wave 구조 추정
- WaveData: 웨이브마다 어떤 몬스터를 몇 마리, 어떤 그리드 위치에 배치할지 정의
- WaveManager: BallLauncher.OnAllBallsReturned 구독 → 볼 턴 종료 감지 → 몬스터 전진 → 사망 확인 → 다음 웨이브 스폰

### 점수/보상
- 몬스터 처치 시 reward(점수) 획득
- MonsterData에 reward 필드 보관 → 처치 시 GameManager에 전달 예정

---

## Wave 폴더 현황

- `Assets/_Project/Scripts/Wave/` 폴더 존재, 파일 없음
- WaveData.cs, WaveManager.cs 신규 생성 대상

---

## 구현해야 할 클래스 목록

### 1. MonsterData (ScriptableObject)
- 경로: `Assets/_Project/Scripts/Data/MonsterData.cs`
- 역할: 몬스터 수치 데이터 읽기 전용 보관
- 필드: `_hp`, `_moveSpeed`, `_damage`, `_reward`

### 2. MonsterBase (MonoBehaviour + IPoolable)
- 경로: `Assets/_Project/Scripts/Monster/MonsterBase.cs`
- 역할: 개별 몬스터 HP 관리, 충돌 감지, 사망 처리
- 의존: MonsterData, ObjectPool, Ball.OnHitMonster(static event)

### 3. WaveData (ScriptableObject)
- 경로: `Assets/_Project/Scripts/Data/WaveData.cs`
- 역할: 웨이브별 몬스터 구성 및 그리드 배치 정의
- 필드: 웨이브 번호, 몬스터 종류·수량·위치 배열

### 4. WaveManager (Singleton)
- 경로: `Assets/_Project/Scripts/Wave/WaveManager.cs`
- 역할: WaveData 기반 몬스터 스폰, 턴 진행, 게임 종료 조건 판단
- 의존: BallLauncher.OnAllBallsReturned, GameManager, ObjectPool<MonsterBase>

### 5. MonsterSetupEditor (Editor 스크립트)
- 경로: `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs`
- 역할: Monster 태그("Monster") 확인 및 MonsterData/WaveData 에셋 자동 생성
- 패턴: BallSetupEditor.cs와 동일한 구조

---

## 결론

- Monster / Wave .cs 파일 없음 → MonsterData, MonsterBase, WaveData, WaveManager, MonsterSetupEditor 신규 생성 필요
- Core 시스템(5종) 및 Ball 시스템(3종) 모두 구현 완료 → Monster 시스템이 즉시 의존 가능
- Ball.OnHitMonster의 static event 특성상 MonsterBase는 물리 충돌(OnCollisionEnter2D)과 static event를 병행하여 데미지 처리
- MonsterData, WaveData는 ScriptableObject로 분리 (DevRules.md 준수)
- 이번 task 범위: MonsterBase의 HP 관리 + WaveManager의 웨이브 스폰 + 턴 진행 로직
