# TODO.md

이 문서는 사용자와 오케스트레이터가 합의한 "게임 다듬기(Polish)" 작업의 구현 상태를 기록합니다.
미구현 항목은 방향(무엇을 왜 어떻게 하기로 했는지)을 담고, 구현된 항목은 실제 적용 내용과 검증 상태를 함께 기록합니다.
개별 항목 구현에 착수할 때는 [TaskRules.md](TaskRules.md) 규칙에 따라 별도의 `research.md`/`plan.md`를 작성한 뒤 사용자 승인을 받아 진행합니다.

---

## 1. 치명타 데미지 텍스트 색상 변경 — 구현 완료 / 플레이 검증 대기

- **구현 내용**: `DamageTextFx.cs` 기본값과 `DamageTextFx.prefab` 직렬화 값을 모두 `#FF4B3E`로 변경했다.
- **정적 검증**: 런타임/Editor C# 빌드 오류 0개.
- **남은 검증**: Unity 플레이 모드에서 일반 데미지는 흰색, 치명타는 붉은색으로 구분되는지 확인한다.

---

## 2. 몬스터 사망 연출 추가

- **현재 상태**: `Assets/_Project/Scripts/Monster/MonsterBase.cs`의 `Die()`는 `_isDead = true` 처리 후 `OnMonsterDied` 이벤트만 발행하며, 별도 시각 효과 없이 곧바로 풀에 반환됨(`WaveManager.HandleMonsterDied()`가 이벤트를 받아 `_poolByData[monster.Data].Return(monster)` 호출).
- **확정된 목표**: 스케일 축소 + 페이드아웃 정도의 간단한 연출을 추가한다. 사용자가 "적당히 스케일축소 페이드아웃정도면돼"라고 명확히 확인함. 신규 아트 리소스는 필요 없으며, DOTween 기반 코드 구현으로 충분하다.
- **비고**: 없음.

---

## 3. 볼 궤적 프리뷰 조정 (점선 길이 / 스크롤 속도 튜닝)

- **현재 상태**: `Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`에 이미 점선 길이/간격(`_dashLength`, `_dashGap`)과 텍스처 스크롤 속도(`_dashScrollSpeed`) 필드가 `[SerializeField]`로 존재하고, `UpdateDashOffset()`에서 매 프레임 `_trajectoryMaterial.mainTextureOffset`을 이동시켜 점선이 흐르는 효과를 이미 내고 있음.
- **확정된 목표**: 완전 신규 기능이 아니라 기존 필드 값 튜닝 작업. 사용자가 "점선의 길이를 조절하고 서서히 움직이는 효과를 줄 것"이라고 확인함 — 점선을 더 길게, 스크롤을 더 서서히/뚜렷하게 보이도록 값을 조정한다.
- **비고**: 없음.

---

## 4. 캐릭터 볼 발사 반동 추가

- **현재 상태**: `Assets/_Project/Scripts/Character/CharacterAimView.cs`는 `BallLauncher.Instance.LaunchDirection`을 매 프레임 폴링해 캐릭터 루트 좌우 반전과 무기/머리 회전만 처리하며, 발사 시점에 반응하는 반동 로직은 없음. `Assets/_Project/Scripts/Ball/BallLauncher.cs`를 확인한 결과 `LaunchRosterEntry()`/`RelaunchQueuedBall()`에서 `ball.Launch(direction)`을 호출하지만, 발사 시점을 외부에 알리는 공개 이벤트(예: `OnBallLaunched`)는 현재 존재하지 않음.
- **확정된 목표**: 사용자가 "캐릭터 전체가 반동이 약간 생길 것"이라고 확인함 — 무기만이 아니라 `CharacterAimView`가 붙은 캐릭터 루트 오브젝트 전체가 발사 순간 살짝 밀렸다가 복귀하는 펀치성 반동을 추가한다.
- **비고**: `BallLauncher`에 현재 발사 시점을 알리는 이벤트가 없으므로, 신규 이벤트 추가 여부/방식은 구현 착수 시점에 추가 확인이 필요하다.

---

## 5. 아이스볼 - 같은 열 후방 몬스터 동시 정지 — 구현 완료 / 플레이 검증 대기

- **구현 내용**: `WaveManager.GetMonstersBehindInColumn()`을 추가했다. 직접 피격 대상보다 위쪽에 있고 두 Collider의 실제 가로 범위가 양수 폭으로 겹치는 활성 몬스터를 후방 같은 열로 판정한다.
- **효과 범위**: 직접 대상에는 기존 Freeze/Slow/추가 피해를 적용하고, 후방 대상에는 동일한 Freeze/Slow만 전파한다.
- **예외 처리**: 자기 자신, 사망·비활성·바닥 공격 중인 몬스터는 제외한다. 1×1·1×2·2×1의 실제 점유 폭을 반영한다.
- **후속 보완**: 최초 구현은 적중 순간 존재한 후방 몬스터만 처리해 이후 스폰된 몬스터가 빙결 대상을 따라잡는 문제가 있었다. 모든 몬스터가 이동 직전에 아래쪽 같은 열의 빙결 몬스터를 동적으로 조회하고, 존재하면 해당 프레임의 하강을 중단하도록 보완했다.
- **스폰 겹침 보완**: 기존 중심점 반경 방식은 Collider가 일부 겹친 상태도 빈 셀로 오판했다. 스폰 셀과 활성 몬스터의 실제 Collider Bounds가 x·y 양쪽에서 양수 폭으로 겹치는지 검사하도록 교체했다.
- **정적 검증**: 런타임/Editor C# 빌드 오류 0개.
- **남은 검증**: Unity 플레이 모드에서 다양한 몬스터 크기 조합의 같은 열/인접 열 판정, 빙결 이후 신규 스폰의 정지·재개, 스폰 순간 Collider 비겹침과 후방 추가 피해 미적용을 확인한다.

---

## 6. 몬스터 바닥 도달 시 진동 → 박치기 돌진 → 소멸 연출 — 구현 완료 / 플레이 검증 대기

- **구현 내용**: 바닥 도달 시 0.35초 동안 강도 0.12로 진동한 뒤 0.25초 동안 실제 캐릭터 Transform 중심으로 돌진한다.
- **피해 시점**: 돌진 도착 시 `OnMonsterReachedBottom` 이벤트를 먼저 발행해 HP를 감소시키고, 그 후 활성 목록 제거와 풀 반환을 수행한다.
- **상태 안전성**: 연출 중 일반 이동·피격·Collider 상호작용을 중단하며, 중복 연출을 막는다. 풀 반환 시 Tween과 상태를 초기화한다.
- **웨이브 안전성**: 공격 완료 전까지 활성 목록에 유지해 조기 웨이브 클리어를 방지한다.
- **씬 연결**: `WaveManager._characterTarget`을 `LaunchPoint/Character`에 연결했으며 Setup Editor도 동일 참조를 연결한다.
- **Inspector 조정**: `WaveManager`에서 진동 시간, 진동 강도, 돌진 시간을 직접 조정할 수 있다. 기본값은 각각 `0.35 / 0.12 / 0.25`다.
- **정적 검증**: 런타임/Editor C# 빌드 오류 0개.
- **남은 검증**: Unity 플레이 모드에서 진동·돌진·충돌 피해 시점, 연출 중 피격 차단, 풀 재사용 및 웨이브 진행을 확인한다.

---

## 7. 몬스터 피격/상태이상 스프라이트 색상 효과

- **현재 상태**: `Assets/_Project/Scripts/Monster/MonsterBase.cs`에는 `SpriteRenderer` 참조도, 피격 시 색상 변화 로직도 전혀 없음. 얼음 효과는 `_frozenSecondsRemaining`/`_slowSecondsRemaining` 필드로 지속시간을 추적하고 있지만(`ApplyFreeze()`/`ApplySlow()`), 화상 효과(`ApplyDot()`)는 `CoDotTick()` 코루틴 내부의 지역 변수로만 지속시간을 다루고 있어 "지금 화상 중인지"를 몬스터 상태로 조회할 방법이 현재 없음(얼음과 달리 지속시간을 필드로 추적하지 않음). 몬스터 프리팹(`Assets/_Project/Prefabs/Monster/Fluffy.prefab` 확인) 구조상 몬스터 본체 스프라이트(루트 오브젝트, 예: "Fluffy")와 몬스터가 밟고 있는 발판/돌판 스프라이트(자식 오브젝트 "BlockVisual")가 별도의 `SpriteRenderer`로 이미 분리되어 있음. `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs`(화상)와 `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`(냉동/슬로우)가 각각 `MonsterBase.ApplyDot()`/`ApplyFreeze()`+`ApplySlow()`를 호출하는 구조.
- **확정된 목표**:
  1. 몬스터가 피격당할 때마다 본체 스프라이트가 흰색으로 아주 짧게 반짝인다(hit flash).
  2. 아이스볼 효과(냉동/슬로우)가 지속되는 동안에는 본체 스프라이트가 하늘색 계열로 물든다.
  3. 파이어볼 효과(화상 DOT)가 지속되는 동안에는 본체 스프라이트가 붉은주황색 계열로 물든다.
  4. 얼음과 화상이 같은 몬스터에 동시에 걸리는 경우, 나중에 걸린 효과의 색을 적용한다(먼저 걸려 있던 효과의 색은 덮어쓴다).
  5. 지속효과가 모두 끝나면 원래 색(기본 스프라이트 색)으로 돌아간다.
  6. 몬스터가 밟고 있는 발판(돌판, `BlockVisual` 스프라이트)에는 이 색상 효과가 적용되지 않는다 — 몬스터 본체 스프라이트에만 적용한다.
- **비고**: 화상 상태를 얼음처럼 지속시간 기준으로 조회 가능한 필드(예: `_burnSecondsRemaining`)로 새로 추적해야 할 것으로 보인다(현재는 코루틴 로컬 변수뿐이라 외부에서 "화상 중인지" 알 수 없음). 정확한 색상 값(하늘색/붉은주황 RGB)과 흰색 피격 플래시 지속시간(예: 0.1초 등)은 구현 착수 시점에 결정한다.

---

## 8. 레이저볼 가로 행 대미지 텍스트 미표시

- **현재 상태**: `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs`의 `OnBallHit(MonsterBase target)`은 `WaveManager.Instance.GetMonstersInRow(target)`으로 같은 가로 행의 몬스터들을 모두 가져온 뒤, 직접 피격한 `target`을 제외한 나머지 몬스터에게 `monster.TakeDamage(LevelData.Value1)`을 직접 호출한다. 반면 대미지 텍스트는 `Assets/_Project/Scripts/UI/DamageTextManager.cs`가 `Ball.OnHitMonster` 정적 이벤트를 구독해서 `HandleHitMonster()` → `ShowDamage()`로 스폰하는 구조인데, 이 이벤트는 `Assets/_Project/Scripts/Ball/Ball.cs`의 `CalculateDamage()` 안에서 `target.TakeDamage(damage)` 직후 `OnHitMonster?.Invoke(target, damage, isCritical)`로 딱 한 번만 발행된다(`OnCollisionEnter2D`/`OnTriggerEnter2D`에서 직접 피격 대상에 대해서만 `CalculateDamage()`가 호출되고, 그 다음에 `foreach (var skill in _skills) skill.OnBallHit(monster)`가 실행됨). 즉 레이저볼의 `OnBallHit()`이 행의 나머지 몬스터에게 가하는 추가 피해는 `MonsterBase.TakeDamage()`만 호출할 뿐 `Ball.OnHitMonster` 이벤트를 전혀 발행하지 않으므로, `DamageTextManager`가 이를 감지하지 못해 직접 피격한 몬스터에게만 대미지 텍스트가 뜨고 같은 행의 나머지 몬스터에게는 텍스트가 뜨지 않는다. 코드로 원인이 명확히 특정됨.
- **확정된 목표**: 레이저볼이 가로 행 전체에 피해를 입힐 때, 직접 피격 대상뿐 아니라 행의 나머지 몬스터에게도 대미지 텍스트가 표시되도록 수정한다.
- **비고**: 구현 착수 시 `LaserBallSkill.OnBallHit()`에서 나머지 몬스터에게 피해를 적용할 때 `DamageTextManager.Instance.ShowDamage(...)`를 직접 호출할지, 아니면 `Ball.OnHitMonster` 이벤트를 재사용/재발행할지 방식을 결정해야 한다. 또한 이 추가 피해에도 치명타(`isCritical`) 개념을 적용할지, 아니면 항상 일반 피해로 표시할지도 결정이 필요하다.

---

## 9. 고스트볼 미작동 버그

- **현재 상태**: `Assets/_Project/Scripts/Skill/Active/GhostBallSkill.cs`는 `OnActivate()`/`OnDeactivate()`에서 `_ball.SetGhostMode(true/false)`를 호출하고, `Assets/_Project/Scripts/Ball/Ball.cs`의 `SetGhostMode(bool isGhost)`는 `_collider.isTrigger = isGhost`로 볼 자신의 Collider2D를 트리거로 전환한다. `Ball.cs`의 `OnTriggerEnter2D(Collider2D other)`는 `_skills`에 `GhostBallSkill`이 있고 `other.CompareTag("Monster")`인 경우에만 `CalculateDamage()` + 각 스킬의 `OnBallHit()`을 호출하며, "Wall"/"Ground" 태그에 대한 처리는 전혀 없다. 반면 일반(고스트 아님) 상태에서 벽 반사·바닥 귀환 로직은 전부 `OnCollisionEnter2D(Collision2D collision)`에 있다(`collision.gameObject.CompareTag("Wall")` → `OnWallHit` 발행 및 `_remainingBounces` 차감, `CompareTag("Ground")` → `ReturnToLaunchPoint()`/`ReturnToPool()`). `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`의 `PlaceColliderObject()`로 생성되는 Wall/Ground 오브젝트는 기본 `BoxCollider2D`(`isTrigger` 미설정, 기본값 `false`)이므로 평상시에는 볼과 물리적 충돌(`OnCollisionEnter2D`)로 처리된다. 그런데 Unity 2D 물리 규칙상 두 Collider 중 하나라도 `isTrigger = true`이면 그 쌍의 상호작용은 무조건 트리거 이벤트(`OnTriggerEnter2D`)로만 처리되고 충돌 이벤트(`OnCollisionEnter2D`)는 발생하지 않는다. 따라서 고스트볼이 활성화되어 볼의 Collider가 트리거로 전환되면, 몬스터뿐 아니라 Wall/Ground와의 상호작용도 전부 트리거 이벤트로 바뀌는데 `Ball.cs`의 `OnTriggerEnter2D`는 Monster 태그만 처리하므로 Wall/Ground 접촉 시 아무 로직도 실행되지 않는다 — 벽에서 반사되지도, 바닥에 닿아 `ReturnToLaunchPoint()`/`ReturnToPool()`이 호출되지도 않는다. 즉 고스트볼은 몬스터를 관통하며 피해를 주는 부분(피어싱)은 코드상 정상 동작하는 것으로 보이나, 벽/바닥과 부딪혀도 반응이 없어 그대로 화면 밖으로 날아가 다시 발사 지점으로 돌아오지 않을 가능성이 높다. 이는 사용자가 보고한 "볼이 적용되지 않는 것으로 보임" 증상과 부합할 수 있는 유력한 원인 후보다. 다만 실제 플레이 모드에서 재현하여 런타임으로 확인하지는 않았다.
- **확정된 목표**: 버그 원인 파악 필요(위 유력 후보를 실제 플레이로 재현·검증한 뒤 목표를 확정한다). 유력 후보가 맞다면 목표는 "고스트볼이 몬스터는 관통하되 벽/바닥에서는 일반 볼과 동일하게 반사·귀환하도록 수정" 정도가 될 것으로 예상된다.
- **비고**: `Assets/_Project/Scripts/Skill/SkillFactory.cs`의 `CreateActiveSkill()` switch에는 `ActiveSkillId.Ghost => new GhostBallSkill(state)` 케이스가 정상적으로 존재하며 누락되지 않았고, `BallLauncher.cs`에서도 다른 볼 타입과 동일한 경로(`ConfigureSkillBall()` + `AddSkill(SkillFactory.CreateActiveSkill(...))`)로 연결되므로 스킬 팩토리/연결 누락 쪽은 원인이 아닌 것으로 보인다(이 부분은 코드 확인만 했고 씬의 실제 Wall/Ground GameObject의 Collider 직렬화 값(씬 파일 자체)까지는 확인하지 못했다 — `SceneSetupEditor.cs`의 생성 스크립트 기준으로만 확인함). 구현 착수 시 `OnTriggerEnter2D`에 Wall/Ground 처리를 추가할지, 혹은 고스트 모드에서 몬스터 Collider만 트리거로 인식하도록(예: Layer 기반 물리 충돌 매트릭스 조정 등) 다른 접근으로 바꿀지 결정이 필요하다.

---

## 다음 단계

1·5·6번은 구현과 C# 빌드 검증을 완료했으며 Unity 플레이 검증을 기다리고 있습니다. 남은 미구현 항목은 2·3·4·7·8·9번입니다. 각 항목을 실제로 구현하기 전에는 [TaskRules.md](TaskRules.md)의 규칙에 따라 `Assets/_Project/Docs/_Task/YYYY-MM-DD/HH-MM_작업요약/` 경로에 `research.md`와 `plan.md`를 작성하고, 사용자의 명시적인 승인을 받은 뒤에 구현을 시작합니다.
