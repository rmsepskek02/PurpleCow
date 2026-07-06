# TODO.md

이 문서는 사용자와 오케스트레이터가 합의한 "게임 다듬기(Polish)" 작업의 구현 상태를 기록합니다.
미구현 항목은 방향(무엇을 왜 어떻게 하기로 했는지)을 담고, 구현된 항목은 실제 적용 내용과 검증 상태를 함께 기록합니다.
개별 항목 구현에 착수할 때는 [TaskRules.md](TaskRules.md) 규칙에 따라 별도의 `research.md`/`plan.md`를 작성한 뒤 사용자 승인을 받아 진행합니다.

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

## 5. 아이스볼 및 몬스터 이동 겹침 방지 — 구현 완료 / 실기기 재검증 대기

- **아이스볼 효과**: 확률 판정 성공 시 직접 맞은 몬스터에만 Freeze/Slow/추가 피해를 적용한다. 후방 전체 상태이상 전파는 제거했다.
- **일반 이동 간격**: 모든 몬스터가 이동하기 전에 x축 Collider 범위가 겹치는 가장 가까운 앞 몬스터까지의 세로 간격을 계산한다. 희망 이동 거리를 해당 간격 이하로 제한해 1×1·1×2·2×1 조합이 서로 겹치지 않게 한다.
- **근접 정지**: 멀리 떨어진 몬스터는 계속 이동하고 앞 몬스터의 Collider 경계에 도달한 몬스터만 정지한다. 앞 몬스터가 다시 움직이면 생긴 공간만큼 자동으로 이동을 재개한다.
- **기존 겹침 처리**: 이미 소량 겹쳤거나 경계가 접촉한 경우 해당 프레임의 하강 거리를 0으로 제한한다. 음수 이동 거리로 위쪽 보정하지 않는다.
- **스폰 겹침**: `MonsterData`별 프리팹 Collider 크기·오프셋·루트 스케일과 풀 부모 스케일로 생성 후 후보 Bounds를 미리 계산하고, 활성 몬스터의 실제 Collider Bounds와 비교한다. 일반 상단 스폰과 1·11웨이브 전체 그리드 배치가 같은 검사를 사용하며, x·y 양쪽에 양수 교집합이 있는 후보만 거부한다.
- **구현 방식**: Collider의 실제 크기 정보만 사용하고 Unity 물리 충돌은 새로 도입하지 않는다. 별도의 부들거림 연출도 추가하지 않는다.
- **추가 회귀 수정**: 실기기에서 확인된 스폰 직후 겹침은 후보 셀 Bounds가 실제 Collider Y 오프셋 `-0.23`을 반영하지 않아 발생했다. 셀 단위 `IsCellFree()`를 제거하고 후보 전체 Collider Bounds 검사로 교체했다.
- **정적 검증**: 기존 오판 204개 구간 재현, `0.75`칸 하강 시 배치 거부, `0.85`칸 경계 접촉 허용, 인접 열, 2×1 전체 폭, 1×2 전체 높이 검사를 통과했다. 런타임/Editor C# 빌드 오류 0개.
- **남은 검증**: Android 실기기에서 일반·Freeze·Slow 상태의 후속 스폰, 1×1·2×1·1×2 모든 조합, 상단 방향 밀림 재발 여부를 확인한다.

---

## 8. 레이저볼 가로 행 대미지 텍스트 미표시

- **현재 상태**: `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs`의 `OnBallHit(MonsterBase target)`은 `WaveManager.Instance.GetMonstersInRow(target)`으로 같은 가로 행의 몬스터들을 모두 가져온 뒤, 직접 피격한 `target`을 제외한 나머지 몬스터에게 `monster.TakeDamage(LevelData.Value1)`을 직접 호출한다. 반면 대미지 텍스트는 `Assets/_Project/Scripts/UI/DamageTextManager.cs`가 `Ball.OnHitMonster` 정적 이벤트를 구독해서 `HandleHitMonster()` → `ShowDamage()`로 스폰하는 구조인데, 이 이벤트는 `Assets/_Project/Scripts/Ball/Ball.cs`의 `CalculateDamage()` 안에서 `target.TakeDamage(damage)` 직후 `OnHitMonster?.Invoke(target, damage, isCritical)`로 딱 한 번만 발행된다(`OnCollisionEnter2D`/`OnTriggerEnter2D`에서 직접 피격 대상에 대해서만 `CalculateDamage()`가 호출되고, 그 다음에 `foreach (var skill in _skills) skill.OnBallHit(monster)`가 실행됨). 즉 레이저볼의 `OnBallHit()`이 행의 나머지 몬스터에게 가하는 추가 피해는 `MonsterBase.TakeDamage()`만 호출할 뿐 `Ball.OnHitMonster` 이벤트를 전혀 발행하지 않으므로, `DamageTextManager`가 이를 감지하지 못해 직접 피격한 몬스터에게만 대미지 텍스트가 뜨고 같은 행의 나머지 몬스터에게는 텍스트가 뜨지 않는다. 코드로 원인이 명확히 특정됨.
- **확정된 목표**: 레이저볼이 가로 행 전체에 피해를 입힐 때, 직접 피격 대상뿐 아니라 행의 나머지 몬스터에게도 대미지 텍스트가 표시되도록 수정한다.
- **비고**: 구현 착수 시 `LaserBallSkill.OnBallHit()`에서 나머지 몬스터에게 피해를 적용할 때 `DamageTextManager.Instance.ShowDamage(...)`를 직접 호출할지, 아니면 `Ball.OnHitMonster` 이벤트를 재사용/재발행할지 방식을 결정해야 한다. 또한 이 추가 피해에도 치명타(`isCritical`) 개념을 적용할지, 아니면 항상 일반 피해로 표시할지도 결정이 필요하다.

---

## 10. 지속 대미지(DoT) 발생 시 대미지 텍스트 미표시

- **현재 상태**: 파이어볼의 화상 효과는 `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs`의 `OnBallHit(MonsterBase target)`에서 `target.ApplyDot(LevelData.Value3, LevelData.Value1, (int)LevelData.Value2)`를 호출해 `Assets/_Project/Scripts/Monster/MonsterBase.cs`의 `ApplyDot()`에 DoT 스택(`DotStack{ DamagePerSecond, RemainingSeconds }`)을 등록하는 것으로 끝나며, 이 시점에는 실제 피해가 전혀 적용되지 않는다. 실제 틱 피해는 `MonsterBase.Update()`에서 매 프레임 호출되는 `UpdateDot(float deltaTime)`이 담당하는데, `_dotTickTimer`가 1초 이상 누적될 때마다 `while (_dotTickTimer >= 1f && !_isDead)` 루프 안에서 살아있는 스택들의 `DamagePerSecond` 합을 구해 `TakeDamage(tickDamage)`를 몬스터 자신에게 직접 호출한다. 이 경로는 `Assets/_Project/Scripts/Ball/Ball.cs`의 `CalculateDamage()`를 전혀 거치지 않으며, `OnHitMonster?.Invoke(target, damage, isCritical)` 이벤트도 발행하지 않는다. `Assets/_Project/Scripts/UI/DamageTextManager.cs`는 `OnEnable()`에서 `Ball.OnHitMonster += HandleHitMonster`로 이 이벤트만 구독해 `ShowDamage()`로 대미지 텍스트를 스폰하므로, DoT 틱 피해는 이벤트가 아예 발행되지 않아 대미지 텍스트로 이어질 방법이 코드상 없다. (참고: 기존 7번 항목 조사 당시 언급되었던 코루틴 기반 `CoDotTick()`은 현재 코드에는 존재하지 않으며, 이후 7번 항목(몬스터 피격/상태이상 스프라이트 색상 효과) 구현 과정에서 `Update()` 기반의 `UpdateDot()`으로 대체된 것으로 보인다.) 원인이 코드로 명확히 특정됨.
- **확정된 목표**: 지속 대미지(DoT) 틱마다 대미지 텍스트가 표시되도록 수정한다.
- **비고**: 구현 착수 시 `MonsterBase.UpdateDot()`의 틱 피해 적용 지점에서 `Ball.OnHitMonster` 이벤트를 재사용/재발행할지, 아니면 `DamageTextManager.Instance.ShowDamage(...)`를 직접 호출할지 방식을 결정해야 한다(8번 레이저볼 항목과 유사한 종류의 결정 필요). DoT 틱은 특정 `Ball` 인스턴스와 무관하게 몬스터 쪽에서 발생하므로, `Ball.OnHitMonster`의 시그니처(`Action<MonsterBase, float, bool>`)를 그대로 재사용할 경우 `isCritical` 값을 항상 `false`로 둘지도 함께 정해야 한다. 또한 DoT 피해 텍스트를 일반 피해와 시각적으로 구분할지(예: 색상/크기 차별화) 여부도 구현 착수 시 결정이 필요하다.

---

## 다음 단계

1·6·9번은 구현과 C# 빌드 검증, 실기기 검증까지 모두 완료되어 이 문서에서 제거되었으며 `ProjectHistory.md`에 이관되었습니다. 5번은 구현과 C# 빌드 검증을 완료했으며 실기기 재검증을 기다리고 있습니다. 남은 미구현 항목은 2·3·4·8·10번입니다. 각 항목을 실제로 구현하기 전에는 [TaskRules.md](TaskRules.md)의 규칙에 따라 `Assets/_Project/Docs/_Task/YYYY-MM-DD/HH-MM_작업요약/` 경로에 `research.md`와 `plan.md`를 작성하고, 사용자의 명시적인 승인을 받은 뒤에 구현을 시작합니다.
