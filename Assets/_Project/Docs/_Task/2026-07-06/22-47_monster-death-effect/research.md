# Research — 몬스터 사망 연출 추가

TODO.md 2번 항목("몬스터 사망 연출 추가")의 구현 착수를 위해, 현재 `MonsterBase.Die()`의 즉시 풀 반환 흐름과 몬스터의 스프라이트 구성을 확인하고, 스케일 축소+페이드아웃 연출을 재생하는 동안 풀 반환을 지연시킬 방법을 조사한다.

## 현재 상태

- `MonsterBase.Die()`(`Assets/_Project/Scripts/Monster/MonsterBase.cs`)는 `_isDead = true`로 설정한 직후 곧바로 `OnMonsterDied?.Invoke(this);`를 발행한다. 그 사이 지연이나 연출은 전혀 없다.
- `WaveManager.HandleMonsterDied(MonsterBase monster)`(`Assets/_Project/Scripts/Wave/WaveManager.cs`)가 이 이벤트를 구독해서, 이벤트를 받는 즉시 `_activeMonsters.Remove(monster)` → `_poolByData[monster.Data].Return(monster)` → `_totalKillCount++` → `CheckSkillUnlock()`을 순서대로 실행한다. 즉 몬스터가 죽는 순간과 화면에서 사라지는 순간(풀 반환) 사이에 시간차가 전혀 없다.
- `MonsterBase`는 `SpriteRenderer`를 4개 가지고 있다: 본체 `_spriteRenderer`, 발판(`BlockVisual`) `_blockSpriteRenderer`(1×1 몬스터는 없을 수 있음, null 체크 존재), 그리고 7번 항목(피격 색상 효과) 작업으로 추가된 히트 플래시 오버레이 `_flashOverlayRenderer`/`_blockFlashOverlayRenderer` 2개.
- `GetSafeDownwardDistance()`(`WaveManager.cs`)는 후보 몬스터를 `!monster.IsAlive`(=`_isDead`)이면 곧바로 건너뛴다. 즉 `Die()`에서 `_isDead = true`를 설정한 시점부터는, 이 몬스터가 여전히 `_activeMonsters`에 남아 있고 화면에 계속 보이더라도 다른 몬스터의 이동 정지 판정에서 장애물로 취급되지 않는다.
- 기존에 "연출 재생 중 즉시 처리를 지연시키는" 유사 패턴이 이미 프로젝트에 있다: `MonsterBase.BeginBottomAttack()`은 `_isBottomAttacking = true`로 즉시 상태를 바꾸고(이후 `Update()`/`TakeDamage()`/상태이상 적용 등 대부분의 로직이 이 플래그를 확인해 조기 종료), DOTween 시퀀스(진동→돌진)를 재생한 뒤 `OnComplete` 콜백에서야 `OnMonsterReachedBottom` 이벤트를 발행해 실제 피해 적용과 풀 반환을 트리거한다.
- DOTween은 이미 프로젝트에 도입되어 있고(`DamageTextFx.cs`, `MonsterBase.cs`의 바닥 도달 연출 등) 새 패키지 도입은 필요 없다.

## 관련 파일 및 의존성

| 파일 | 역할 |
|---|---|
| `Assets/_Project/Scripts/Monster/MonsterBase.cs` | `Die()`(수정 대상), `BeginBottomAttack()`(참고할 기존 패턴), 4개 `SpriteRenderer` 필드 |
| `Assets/_Project/Scripts/Wave/WaveManager.cs` | `HandleMonsterDied()`(즉시 풀 반환 지점), `GetSafeDownwardDistance()`(`IsAlive` 필터로 이미 안전함을 확인) |
| DOTween(`DG.Tweening`) | 스케일 축소 + 페이드아웃 연출 구현에 재사용 |

## 문제점 / 구현 대상 파악

1. **풀 반환을 지연시키는 방식 선택**: `BeginBottomAttack()`과 동일한 패턴을 재사용하는 것이 가장 자연스럽다 — `Die()` 내부에서 즉시 이벤트를 발행하는 대신, DOTween 시퀀스(스케일 축소 + 페이드아웃)를 재생하고 `OnComplete`에서야 `OnMonsterDied`를 발행한다. `_isDead = true`는 지금처럼 `Die()` 시작 시점에 즉시 설정해, 연출 중에는 추가 피격/상태이상/이동이 전혀 적용되지 않도록 한다(기존에도 `TakeDamage()` 등이 `_isDead` 확인 후 조기 종료하는 구조이므로 자연히 안전하다).
2. **연출 대상 스프라이트 범위**: 본체(`_spriteRenderer`)뿐 아니라 발판(`_blockSpriteRenderer`, 존재하는 경우)까지 함께 축소·페이드해야 몬스터 전체가 자연스럽게 사라지는 것처럼 보인다. 히트 플래시 오버레이(`_flashOverlayRenderer`/`_blockFlashOverlayRenderer`)는 평소 알파 0으로 유지되는 보조 레이어라 굳이 애니메이션 대상에 포함할 필요는 없지만, 스케일은 본체와 같은 Transform 하위에 있다면 자동으로 함께 축소된다(오버레이가 본체의 자식 오브젝트인지 확인 필요 — `CreateOverlayFor(_spriteRenderer, transform)` 호출로 보아 본체 Transform의 자식으로 생성되는 것으로 보이며, 이 경우 부모(본체) `transform.localScale`을 줄이면 자식 오버레이도 함께 축소되어 별도 처리가 필요 없다).
3. **처치 카운트/스킬 해금 판정 시점**: `_totalKillCount++`와 `CheckSkillUnlock()`이 `HandleMonsterDied()`에서 `OnMonsterDied` 이벤트와 함께 처리되므로, 이벤트 발행을 연출 종료 시점으로 미루면 카운트 반영도 그만큼(연출 길이만큼, 예상 0.3~0.5초) 늦어진다. 이는 `BeginBottomAttack()`이 이미 채택한 것과 동일한 트레이드오프이며, 몬스터 사망 이펙트가 짧다면 사용자 체감상 문제가 되지 않을 가능성이 높다. 다만 정확히 이 지연이 허용되는지는 확인이 필요하다.
4. **웨이브 클리어 조건과의 상호작용**: `WaveManager`가 "이번 웨이브 스폰 완료 + 활성 몬스터 0마리"를 웨이브 클리어 조건으로 쓰고 있다면(정확한 조건은 `WaveManager.cs`의 웨이브 진행 로직에서 재확인 필요), 연출 중인 몬스터를 `_activeMonsters`에 남겨두는 것이 "마지막 몬스터가 죽었는데 연출 때문에 웨이브가 아주 잠깐 안 끝난 것처럼 보이는" 정도의 사소한 지연만 유발할 뿐, `BeginBottomAttack()` 사례에서 이미 검증된 것과 동일한 안전한 패턴이다.

## 결론

- `BeginBottomAttack()`과 동일한 "상태 플래그 즉시 설정 → DOTween 연출 재생 → `OnComplete`에서 이벤트 발행" 패턴을 그대로 재사용하면, 웨이브 진행 로직이나 이동 정지 판정에 부작용 없이 사망 연출을 추가할 수 있다.
- 연출 대상은 본체(`_spriteRenderer`)와 발판(`_blockSpriteRenderer`, 존재 시)이며, 히트 플래시 오버레이는 본체의 자식이라면 스케일 축소가 자동으로 전파되어 별도 처리가 불필요할 가능성이 높다(구현 착수 시 프리팹 계층 구조로 최종 확인 필요).
- 확정이 필요한 모호한 지점: (1) 연출 지속시간(구체적 초 단위), (2) 처치 카운트/스킬 해금 판정을 연출 종료 후로 미루는 것이 허용되는지, (3) `_blockSpriteRenderer`가 없는 1×1 몬스터와 있는 몬스터 모두에서 동일한 코드 경로로 처리 가능한지(이미 null 체크 패턴이 있어 문제없을 것으로 보임).
