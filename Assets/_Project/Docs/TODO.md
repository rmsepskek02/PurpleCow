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

## 다음 단계

1·5·6번은 구현과 C# 빌드 검증을 완료했으며 Unity 플레이 검증을 기다리고 있습니다. 남은 미구현 항목은 2·3·4·7번입니다. 각 항목을 실제로 구현하기 전에는 [TaskRules.md](TaskRules.md)의 규칙에 따라 `Assets/_Project/Docs/_Task/YYYY-MM-DD/HH-MM_작업요약/` 경로에 `research.md`와 `plan.md`를 작성하고, 사용자의 명시적인 승인을 받은 뒤에 구현을 시작합니다.
