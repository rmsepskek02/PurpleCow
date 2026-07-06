# TODO.md

이 문서는 사용자와 오케스트레이터가 대화로 논의를 마쳤지만 아직 구현하지 않은 "게임 다듬기(Polish)" 작업 백로그를 기록합니다.
각 항목은 방향(무엇을 왜 어떻게 하기로 했는지)에 대한 합의만 담고 있으며, 코드 구현은 포함하지 않습니다.
개별 항목 구현에 착수할 때는 [TaskRules.md](TaskRules.md) 규칙에 따라 별도의 `research.md`/`plan.md`를 작성한 뒤 사용자 승인을 받아 진행합니다.

---

## 1. 치명타 데미지 텍스트 색상 변경

- **현재 상태**: `Assets/_Project/Scripts/UI/DamageTextFx.cs`의 `_criticalColor` 필드가 `Color.yellow`(노란색)로 설정되어 있음. 치명타 시 이 색상으로 데미지 텍스트가 표시됨(`Play()`에서 `isCritical`이면 `_criticalColor` 적용).
- **확정된 목표**: 치명타 데미지 텍스트 색상을 노란색에서 붉은 계열로 변경한다.
- **비고**: 정확한 색상값(RGB)은 구현 착수 시 결정한다.

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

## 5. 아이스볼 - 같은 열 후방 몬스터 동시 정지

- **현재 상태**: `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`의 `OnBallHit()`은 맞은 몬스터 1마리에만 `MonsterBase.ApplyFreeze()`/`ApplySlow()`를 적용함. `Assets/_Project/Scripts/Wave/WaveManager.cs`에는 같은 행(row, y좌표 유사)을 조회하는 `GetMonstersInRow()`는 있으나, 같은 열(column, x좌표 유사)을 조회하는 기능은 없음. 몬스터는 `MonsterBase.Update()`에서 매 프레임 `Vector3.down * speed * deltaTime`으로 계속 하강하며 고정된 그리드 칸에 종속되지 않는다(그리드 점유 체크는 `WaveManager`의 스폰 시점에만 이루어짐).
- **확정된 목표**: 사용자가 목적을 명확히 함 — 밸런스 목적이 아니라 "몬스터를 멈춰세울 때 뒤에서 하강하는 몬스터가 겹치는 것을 방지"하기 위한 것. 얼어붙어 제자리에 멈춘 몬스터와 같은 열(x좌표 유사)에서 아직 하강 중이며 그 몬스터보다 뒤쪽(y좌표가 더 큰, 즉 위쪽)에서 내려오는 몬스터들에도 동일한 Freeze/Slow를 전파해 겹쳐 보이지 않도록 한다.
- **비고**: `GetMonstersInRow()`와 유사한 형태의 열 조회 로직(`GetMonstersInColumn()` 등) 신설이 필요할 것으로 보이나, 정확한 판정 기준(x좌표 허용 오차, y 비교 방향)은 구현 착수 시점에 확정한다.

---

## 6. 몬스터 바닥 도달 시 진동 → 박치기 돌진 → 소멸 연출 (신규 추가 항목)

- **현재 상태**: `Assets/_Project/Scripts/Wave/WaveManager.cs`의 `CheckGameOver()`가 몬스터의 y좌표가 `_bottomBoundaryY` 이하로 내려가면 즉시 `_activeMonsters`에서 제거 후 풀 반환하고 `OnMonsterReachedBottom` 이벤트를 발행함. `Assets/_Project/Scripts/Core/CharacterManager.cs`의 `HandleMonsterReachedBottom()`이 이 이벤트를 받아 그 즉시 `TakeDamage(monster.Data.Damage)`로 플레이어 체력을 깎음. 현재는 바닥 도달과 동시에 순식간에 사라지고 데미지가 들어가며 별도 연출은 없음.
- **확정된 목표**: 바닥 도달 시 즉시 사라지는 대신 다음 순서로 연출을 변경한다.
  1. 제자리에서 부들부들 진동하는 연출
  2. 캐릭터 쪽으로 돌진(박치기)하는 연출
  3. 캐릭터와 충돌하는 순간 소멸 + 그 순간에 플레이어 체력 감소
- **비고**: 세부 값(진동 지속시간, 돌진 속도, 돌진 목표 지점이 정확히 캐릭터 오브젝트 위치인지 등)은 아직 확정되지 않았으며, 구현 착수 시점에 추가 논의/결정이 필요하다.

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

위 7개 항목은 아직 어느 것도 구현되지 않은 상태입니다. 각 항목을 실제로 구현하기 전에는 [TaskRules.md](TaskRules.md)의 규칙에 따라 `Assets/_Project/Docs/_Task/YYYY-MM-DD/HH-MM_작업요약/` 경로에 `research.md`와 `plan.md`를 작성하고, 사용자의 명시적인 승인을 받은 뒤에 구현을 시작합니다.
