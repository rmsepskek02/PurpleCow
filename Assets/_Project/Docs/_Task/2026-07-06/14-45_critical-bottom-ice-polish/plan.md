# Plan — Critical Text, Bottom Attack, and Ice Column Polish

이 계획은 `TODO.md`의 1번, 6번, 5번을 순서대로 구현하고 검증하기 위한 작업 범위를 정의한다. 치명타 색상 변경은 직렬화된 프리팹까지 반영하며, 바닥 공격 연출은 몬스터 풀링과 피해 이벤트 순서를 안전하게 재구성하고, 아이스볼은 실제 Collider 점유 폭을 기준으로 후방 정지 효과를 전파한다.

## 구현 목표

1. 치명타 데미지 텍스트를 `#FF4B3E` 색상으로 표시한다.
2. 바닥에 도달한 몬스터가 0.35초 진동한 뒤 0.25초 동안 캐릭터 중심으로 돌진하고, 도착 순간 피해를 준 뒤 사라지게 한다.
3. 바닥 공격 중인 몬스터는 이동과 피격을 중단하고 중복 연출이나 풀 반환 충돌이 발생하지 않게 한다.
4. 아이스볼이 적중하면 같은 수평 점유 범위에서 뒤따라 내려오는 몬스터에도 Freeze와 Slow를 적용한다.
5. 후방 몬스터에는 추가 피해를 전파하지 않는다.

## 단계별 작업 계획

### 1단계 — 치명타 텍스트 색상 변경

- `DamageTextFx.cs`의 `_criticalColor` 기본값을 `#FF4B3E`에 해당하는 Unity Color 값으로 변경한다.
- 기존 `DamageTextFx.prefab`의 직렬화된 `_criticalColor`도 같은 값으로 변경한다.
- 일반 데미지의 흰색, 치명타 굵기와 크기, 이동·페이드 연출은 변경하지 않는다.

### 2단계 — 몬스터 바닥 공격 상태 추가

- `MonsterBase`에 바닥 공격 진행 상태를 추가한다.
- 바닥 공격 시작 시 일반 이동과 `TakeDamage()` 처리를 중단한다.
- 본체 Collider를 비활성화해 볼과의 추가 충돌을 막는다.
- DOTween Sequence로 다음 연출을 실행한다.
  1. 현재 위치에서 0.35초 진동
  2. 캐릭터 Transform 중심까지 0.25초 돌진
- 풀 반환 시 진행 중 Tween을 종료하고 Transform 및 Collider 상태를 복구한다.
- 풀에서 다시 꺼낼 때 바닥 공격 상태가 남지 않도록 초기화한다.

### 3단계 — 바닥 도달 처리 순서 재구성

- `WaveManager`가 화면 비율에 따라 이동하는 실제 캐릭터 Transform을 참조하도록 연결한다.
- `CheckGameOver()`에서 바닥 공격 중이 아닌 몬스터만 연출을 시작한다.
- 연출이 끝날 때까지 몬스터를 활성 목록에 유지해 조기 웨이브 클리어를 방지한다.
- 캐릭터 도착 콜백에서 아래 순서를 지킨다.
  1. `OnMonsterReachedBottom` 이벤트 발행
  2. 활성 목록 제거
  3. 풀 반환
  4. 몬스터 수 이벤트 갱신
  5. 웨이브 클리어 검사
- `CharacterManager`의 기존 이벤트 구독을 유지해 충돌 순간 HP 감소와 기존 XP 처리가 실행되도록 한다.
- 이벤트 발행 시점에는 몬스터가 아직 활성 상태이므로 `MonsterData`를 안전하게 읽을 수 있게 한다.

### 4단계 — 캐릭터 Transform 참조 연결

- 현재 씬의 `LaunchPoint/Character` Transform을 `WaveManager`의 직렬화 필드에 연결한다.
- 프로젝트 Setup Editor가 씬을 다시 구성해도 동일 참조를 연결하도록 관련 Editor 스크립트를 함께 갱신한다.
- 고정 월드 좌표는 사용하지 않아 `WallFitter`의 화면 비율 보정을 그대로 따른다.

### 5단계 — 아이스볼 후방 전파

- `WaveManager`에 피격 몬스터와 수평 Collider 범위가 겹치면서 y좌표가 더 큰 활성 몬스터를 조회하는 메서드를 추가한다.
- 자기 자신, 비활성 몬스터, 사망 또는 바닥 공격 중인 몬스터는 제외한다.
- Collider 범위가 경계에서 단순 접촉하는 경우를 같은 열로 과대 판정하지 않도록 실제 양수 폭의 교집합이 있는 경우만 포함한다.
- `IceBallSkill.OnBallHit()`의 확률 판정이 성공하면:
  - 직접 피격 대상에는 기존 Freeze, Slow, 추가 피해를 적용한다.
  - 조회된 후방 몬스터에는 동일 시간과 비율의 Freeze, Slow만 적용한다.

### 6단계 — 문서와 상태 갱신

- 완료된 TODO 1·5·6 항목을 구현 완료 상태로 정리한다.
- `ProjectStatus.md`에 신규 Polish 기능과 검증 결과를 반영한다.
- `ProjectHistory.md`에 작업 내역을 추가한다.
- 구현 과정에서 새로운 실패 사례가 확인될 경우에만 `AIFailures.md`를 갱신한다.

### 7단계 — 검증

- 런타임 및 Editor C# 프로젝트를 빌드해 컴파일 오류를 확인한다.
- 잔여 직렬화 참조와 씬 연결을 검색으로 점검한다.
- Unity 플레이 모드에서 다음 항목을 확인한다.
  1. 일반 데미지는 흰색, 치명타는 `#FF4B3E`
  2. 바닥 도달 시 즉시 피해가 들어가지 않음
  3. 진동과 돌진 후 도착 순간에만 HP 감소
  4. 연출 중 볼 피격과 중복 돌진이 발생하지 않음
  5. 공격 완료 후 몬스터 수와 웨이브 진행이 정상
  6. 1×1·1×2·2×1 몬스터 조합에서 같은 열 후방만 정지
  7. 후방 몬스터에는 아이스볼 추가 피해가 들어가지 않음

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/UI/DamageTextFx.cs`
- `Assets/_Project/Prefabs/UI/DamageTextFx.prefab`
- `Assets/_Project/Scripts/Monster/MonsterBase.cs`
- `Assets/_Project/Scripts/Wave/WaveManager.cs`
- `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/_Project/Docs/TODO.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`

구현 과정에서 실제 참조 구조를 확인한 결과에 따라 관련 Setup Editor 한 파일이 추가로 변경될 수 있으나, 신규 런타임 시스템이나 아트 리소스는 생성하지 않는다.

## 주의사항

- 몬스터를 풀에 반환하기 전에 피해 이벤트를 동기적으로 처리한다.
- 바닥 공격 연출 중 게임이 종료되거나 오브젝트가 비활성화될 때 Tween 콜백이 뒤늦게 실행되지 않도록 한다.
- 바닥 공격 진입 몬스터는 처치로 계산하지 않으며 킬 카운트를 증가시키지 않는다.
- 기존 동작과 동일하게 바닥 도달 보상 XP는 유지한다.
- 아이스볼의 직접 대상 추가 피해와 후방 상태이상 전파를 분리한다.
- 사용자가 조정한 하단 벽, LaunchPoint, Character 위치값은 변경하지 않는다.
- 요청 범위 밖인 TODO 2·3·4·7은 수정하지 않는다.

---

## 후속 수정 계획 — 아이스볼 겹침 방지 및 Inspector 연출 조정

최초 구현 후 사용자 플레이 테스트에서 아이스볼 빙결 중 같은 열에 새로 배치된 몬스터가 정지 대상에 포함되지 않아 앞 몬스터와 겹치는 현상이 재현되었다. 또한 사용자가 바닥 공격 연출 수치를 Inspector에서 직접 조정할 수 있도록 요청했다.

### 구현 목표

1. 아이스볼 적중 이후 새로 배치되는 후방 몬스터도 아래쪽 빙결 몬스터가 존재하는 동안 이동하지 않게 한다.
2. 빙결이 끝나거나 빙결 몬스터가 처치되면 별도 상태 정리 없이 자동으로 이동을 재개한다.
3. 바닥 공격의 진동 시간·진동 강도·돌진 시간을 `WaveManager` Inspector에서 조정 가능하게 한다.
4. 현재 확정값 `0.35 / 0.12 / 0.25`와 기존 플레이 결과를 유지한다.

### 1단계 — 동적 빙결 열 차단 조회

- `WaveManager`에 특정 몬스터보다 아래쪽에 빙결 중인 몬스터가 있는지 조회하는 메서드를 추가한다.
- 같은 열 판정은 기존과 동일하게 실제 Collider 가로 범위의 양수 폭 교집합을 사용한다.
- 자기 자신, 사망·비활성·바닥 공격 중인 몬스터는 제외한다.
- 후보 빙결 몬스터의 y좌표가 이동하려는 몬스터보다 아래쪽일 때만 차단한다.

### 2단계 — 몬스터 이동 직전 차단

- `MonsterBase.Update()`에서 자신의 Freeze 처리 후 일반 이동을 적용하기 직전에 동적 조회를 호출한다.
- 아래쪽 같은 열에 빙결 몬스터가 있으면 해당 프레임의 하강을 생략한다.
- 빙결 상태 자체를 후방 몬스터에 새로 누적하지 않으므로 지속시간이 늘어나거나 상태 UI가 왜곡되지 않게 한다.
- 기존 `IceBallSkill`의 적중 순간 Freeze/Slow 전파는 유지한다. 기존 몬스터는 실제 상태이상을 공유하고, 이후 합류한 몬스터는 겹침 방지를 위한 이동만 차단한다.

### 3단계 — 바닥 공격 수치 Inspector 노출

- `WaveManager`의 다음 상수를 `[SerializeField] private float` 필드로 전환한다.
  - 진동 시간: `_bottomAttackShakeDuration = 0.35f`
  - 진동 강도: `_bottomAttackShakeStrength = 0.12f`
  - 돌진 시간: `_bottomAttackDashDuration = 0.25f`
- 음수 입력을 막기 위해 `[Min(0f)]`를 적용한다.
- `CheckGameOver()`의 바닥 공격 호출이 새 직렬화 필드를 사용하도록 변경한다.
- `SampleScene.unity`에 동일한 현재 값을 직렬화한다.
- `SceneSetupEditor`가 재실행될 때도 세 기본값을 연결하도록 갱신한다.

### 4단계 — 회귀 검증

- 런타임 및 Editor C# 프로젝트 빌드를 실행한다.
- 씬에 세 연출 값이 직렬화되고 캐릭터 참조가 유지되는지 확인한다.
- Unity 플레이 모드에서 다음을 확인한다.
  1. 빙결 전에 존재하던 후방 몬스터가 함께 정지
  2. 빙결 후 같은 열에 새로 배치된 몬스터도 앞 몬스터와 겹치기 전에 정지
  3. 인접 열 몬스터는 정상 이동
  4. 빙결 종료 또는 대상 처치 후 후방 몬스터 이동 재개
  5. Inspector에서 진동 시간·강도·돌진 시간을 변경하면 다음 바닥 공격부터 반영
  6. 기본값에서 기존 진동·돌진·피해 시점 유지

### 4단계 이전 추가 작업 — Collider Bounds 기반 스폰 점유 검사

- `MonsterBase`에 활성화된 본체 `BoxCollider2D`의 전체 월드 Bounds를 안전하게 반환하는 메서드를 추가한다.
- 기존 수평 범위 조회도 동일 Bounds 정보를 사용해 중복 기준을 줄인다.
- `WaveManager.IsCellFree()`의 중심점 거리·고정 반경 검사를 제거한다.
- 검사할 그리드 셀 중심에 `_gridCellSize × _gridCellSize` 크기의 월드 Bounds를 구성한다.
- 각 활성 몬스터 Collider Bounds와 다음 기준으로 비교한다.
  1. x축 교집합 폭이 epsilon보다 큼
  2. y축 교집합 폭이 epsilon보다 큼
  3. 두 조건을 모두 만족할 때만 점유 중으로 판정
- Collider 경계가 정확히 맞닿는 경우는 정상적인 인접 배치이므로 빈 공간으로 허용한다.
- 기존 BlockSize별 셀 검사 구조는 유지한다.
  - 1×1: top 셀
  - 2×1: top 셀과 오른쪽 top 셀
  - 1×2: top 셀과 바로 아래 셀
- 같은 스폰 틱에서 사용하는 `topRowFree` 갱신 방식과 전체 그리드 최초 배치용 `free[,]`는 원인이 아니므로 변경하지 않는다.

### 예상 추가 변경 파일

- `Assets/_Project/Scripts/Wave/WaveManager.cs`
- `Assets/_Project/Scripts/Monster/MonsterBase.cs`
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/_Project/Docs/TODO.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`
- `Assets/_Project/Docs/AIFailures.md`

### 후속 주의사항

- 일반 이동 전체를 물리 충돌 방식으로 바꾸지 않는다.
- 빙결 몬스터가 없는 평상시 열 이동과 스폰 간격은 변경하지 않는다.
- 스폰 검사에서는 Transform 중심점 근사값을 다시 사용하지 않고 실제 Collider Bounds를 단일 기준으로 삼는다.
- Collider가 비활성화된 바닥 공격 몬스터는 스폰 영역과 멀리 떨어진 상태이며 점유 검사 대상에서 제외한다.
- 매 프레임 활성 몬스터 목록을 조회하지만 현재 최대 규모가 작으므로 별도 캐시나 공간 분할을 추가하지 않는다.
- 이번 실패 원인인 “시간 지속 효과를 적중 순간 목록으로만 처리한 설계”와 “Collider 점유를 작은 중심점 반경으로 근사한 설계”를 `AIFailures.md`에 기록한다.

---

## 최종 교정 계획 — 일반 이동 간격 제한과 아이스볼 직접 대상화

사용자 재검증 결과, 앞선 “아래쪽에 빙결 몬스터가 있으면 같은 열의 이동을 모두 차단”하는 후속 계획은 요구 동작과 다르므로 폐기한다. 이 절의 계획이 앞선 아이스볼 후방 전파·열 차단 계획보다 우선하며 최종 구현 기준이다.

### 최종 구현 목표

1. 아이스볼은 직접 맞은 몬스터만 빙결·감속하고 추가 피해를 준다.
2. 후방 몬스터는 멀리 떨어져 있으면 계속 이동하고, 바로 앞 몬스터의 Collider 경계에 도달했을 때만 정지한다.
3. 모든 몬스터 이동에 같은 간격 제한을 적용해 아이스볼과 무관한 1×1·1×2·2×1 조합의 겹침도 방지한다.
4. Unity 물리 충돌은 새로 도입하지 않고 실제 Collider Bounds를 사용해 프레임별 Transform 이동량을 제한한다.
5. 별도의 부들거림 연출은 추가하지 않는다.
6. 돌진 전 진동을 더 빠르고 강하게 조정하고 진동 횟수를 Inspector에 노출한다.

### 1단계 — 기존 열 전체 정지 로직 제거

- `IceBallSkill.OnBallHit()`에서 `GetMonstersBehindInColumn()` 조회와 후방 전체 Freeze/Slow 적용을 제거한다.
- 직접 피격 대상의 확률 판정, Freeze, Slow, 추가 피해는 유지한다.
- `MonsterBase.Update()`의 `HasFrozenMonsterAhead()` 기반 전체 프레임 중단을 제거한다.
- 더 이상 사용하지 않는 `WaveManager.GetMonstersBehindInColumn()`과 `HasFrozenMonsterAhead()`를 삭제한다.

### 2단계 — 가장 가까운 앞 몬스터까지 이동량 제한

- `MonsterBase.Update()`에서 Freeze와 Slow를 반영한 이번 프레임의 희망 하강 거리 `speed × deltaTime`을 계산한다.
- `WaveManager`에 현재 몬스터가 안전하게 이동할 수 있는 실제 하강 거리를 반환하는 메서드를 추가한다.
- 활성 몬스터 중 다음 조건을 만족하는 대상을 앞쪽 장애물 후보로 사용한다.
  - 자기 자신이 아님
  - 생존 상태
  - 바닥 공격 중이 아니며 Collider Bounds 조회 가능
  - 현재 몬스터보다 아래쪽
  - 두 Collider의 x축 범위가 양수 폭으로 겹침
- 현재 몬스터 Collider 아래 경계와 후보 Collider 위 경계 사이의 가장 작은 간격을 구한다.
- 희망 이동 거리를 가장 작은 간격 이하로 제한한다.
- 간격이 0이면 이동하지 않는다.
- 이미 소량 겹친 상태라면 해당 프레임의 이동 거리를 0으로 제한하며 위쪽으로 보정하지 않는다.
- 앞에 몬스터가 없으면 기존 속도로 그대로 하강한다.

### 3단계 — 스폰 점유 검사 유지

- 이미 구현된 실제 Collider Bounds 기반 `IsCellFree()`는 유지한다.
- 스폰 셀과 활성 몬스터 Collider가 x·y 양쪽에서 실제로 겹칠 때 배치를 막는다.
- 정확히 경계만 닿는 정상 인접 배치는 허용한다.
- 이동 간격 제한과 스폰 점유 검사가 동일한 `TryGetColliderBounds()`를 사용하도록 유지한다.

### 4단계 — 돌진 전 진동 강화와 Inspector 노출

- 사용자 승인값을 새 기본값으로 사용한다.
  - 진동 시간: `0.25초`
  - 진동 강도: `0.18`
  - 진동 횟수: `20`
  - 돌진 시간: `0.25초`
- `WaveManager`에 `[SerializeField, Min(1)] private int _bottomAttackShakeVibrato`를 추가한다.
- `MonsterBase.BeginBottomAttack()`이 고정값 `12` 대신 전달받은 진동 횟수를 사용하도록 변경한다.
- `SampleScene.unity`와 `SceneSetupEditor` 기본값도 동일하게 갱신한다.
- 기존 Inspector 필드인 진동 시간·강도·돌진 시간은 유지한다.

### 5단계 — 결정적 계산 검증

Unity 물리 재현 테스트 기반이 별도로 없으므로 Bounds 수치 계산을 정적 검증 루프로 사용한다.

- 앞 몬스터와 간격이 희망 이동 거리보다 크면 기존 거리만큼 이동
- 간격이 희망 이동 거리보다 작으면 경계까지만 이동
- 경계가 닿아 있으면 이동 거리 0
- 이미 겹쳤으면 이동 거리 0 반환
- 가로 범위가 겹치지 않는 인접 열 몬스터는 이동에 영향 없음
- 가로 2칸 몬스터는 실제 두 칸 폭 모두에서 앞 몬스터를 감지

### 6단계 — 빌드·플레이 검증

- 런타임 및 Editor C# 프로젝트 빌드 오류 0개를 확인한다.
- 씬의 진동 네 필드와 캐릭터 참조를 검증한다.
- Unity 플레이 모드에서 다음을 확인한다.
  1. 아이스볼 직접 대상만 빙결·감속
  2. 멀리 떨어진 같은 열 몬스터는 계속 전진
  3. 앞 몬스터와 맞닿을 때만 정지
  4. 빙결 종료 후 앞 몬스터가 움직이면 뒤 몬스터도 재이동
  5. 아이스볼 없이도 가로 2칸 몬스터와 다른 몬스터가 겹치지 않음
  6. 인접 열 이동은 서로 방해하지 않음
  7. 돌진 전 진동이 기존보다 빠르고 강하게 표시됨
  8. Inspector 네 값을 변경하면 다음 연출부터 반영

### 최종 변경 파일

- `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`
- `Assets/_Project/Scripts/Monster/MonsterBase.cs`
- `Assets/_Project/Scripts/Wave/WaveManager.cs`
- `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/_Project/Docs/TODO.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`
- `Assets/_Project/Docs/AIFailures.md`

### Git 기준

- 구현 착수 전 `git fetch origin main`으로 원격 상태를 확인했다.
- 현재 브랜치는 `main`이며 `HEAD`와 `origin/main`은 동일하다.
- 작업 완료 시에도 원격 `main`을 다시 fetch해 최신 여부를 확인한다.
- 사용자가 명시적으로 요청하지 않은 커밋·push는 수행하지 않는다.
- 다른 작업의 미추적 `.meta` 및 Shader 파일은 수정하거나 삭제하지 않는다.

---

## 상단 벽 이탈 회귀 수정 계획

Collider 간격 기반 이동 제한 적용 후 일부 몬스터가 상단 벽까지 올라가는 회귀가 사용자 플레이 이미지에서 확인되었다. 정상 최고 스폰 Collider 경계는 상단 벽 안쪽보다 약 `0.98` 낮으므로 스폰 좌표 문제가 아니며, 이미 겹친 간격을 음수 이동 거리로 반환한 것이 원인이다.

### 구현 목표

1. 일반 몬스터 이동이 어떤 경우에도 y축 위쪽으로 실행되지 않게 한다.
2. 앞 몬스터와 접촉하거나 이미 겹친 경우 해당 프레임의 하강 거리를 `0`으로 제한한다.
3. 기존 스폰 Bounds 검사와 겹치기 전 이동 제한은 유지한다.
4. 상단 벽 이탈을 막으면서 아이스볼 직접 대상 빙결과 근접 대기열 동작은 유지한다.

### 단계별 수정

1. `WaveManager.GetSafeDownwardDistance()`가 후보별 `verticalGap`을 반영할 때 음수 값을 `0`으로 제한한다.
2. 메서드 최종 반환값도 `Mathf.Clamp(safeDistance, 0f, desiredDistance)`로 방어한다.
3. `MonsterBase.Update()`는 계속 `Vector3.down * safeDistance`만 적용하므로 반환값이 0 이상이면 위쪽 이동이 불가능하다.
4. 음수 간격을 위쪽 위치 보정으로 사용한다는 이전 계획과 문서 설명을 폐기한다.

### 결정적 회귀 검증

- 간격이 희망 이동 거리보다 크면 희망 거리 반환
- 간격이 작으면 간격까지만 반환
- 경계가 접촉하면 `0`
- 간격이 음수여도 `0`
- 인접 열은 희망 거리 유지
- 가로 2칸의 실제 x범위가 겹치면 동일 제한 적용
- 반환값이 항상 `0 ≤ safeDistance ≤ desiredDistance`인지 확인

### 빌드·플레이 검증

- 런타임 및 Editor C# 프로젝트 빌드
- 이전 열 전체 Freeze 코드가 없는지 정적 검색
- Unity 플레이를 새로 시작해 최고 스폰 행보다 위로 이동하는 몬스터가 없는지 확인
- 빙결 몬스터 뒤에서 먼 몬스터는 이동하고, 접촉한 몬스터만 정지하는지 재확인
- 가로 2칸 몬스터 조합이 겹치거나 위로 밀리지 않는지 확인

### 변경 파일

- `Assets/_Project/Scripts/Wave/WaveManager.cs`
- `Assets/_Project/Docs/TODO.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`
- `Assets/_Project/Docs/AIFailures.md`

---

## 실기기 스폰 겹침 회귀 수정 계획

상단 방향 밀림을 제거한 뒤 실기기에서 몬스터가 겹친 채 유지되는 현상이 확인되었다. `research.md`의 결정적 수치 재현 결과, 현재 `IsCellFree()`가 후보 몬스터의 실제 Collider 오프셋을 반영하지 않는 것이 직접 원인이다. 이번 수정은 셀 Bounds 근사를 제거하고 생성될 몬스터의 실제 Collider 구성을 그대로 투영한 후보 Bounds를 모든 스폰 경로의 단일 점유 기준으로 사용한다.

### 구현 목표

1. 스폰 가능 여부를 셀 중심이나 반경 근사값이 아니라 후보 몬스터 전체 Collider Bounds로 판정한다.
2. 1×1·2×1·1×2 몬스터의 크기, Collider 오프셋, 프리팹 루트 스케일을 모두 반영한다.
3. 일반 상단 스폰과 1·11웨이브 전체 그리드 배치가 같은 점유 검사 함수를 사용한다.
4. 같은 스폰 틱에서 먼저 배치된 몬스터도 다음 후보의 충돌 검사 대상에 포함한다.
5. 경계만 접촉하는 정상 배치는 허용하고 x·y 양쪽에 양수 교집합이 있는 배치만 거부한다.
6. 기존 아이스볼 직접 대상 Freeze/Slow, 일반 이동 간격 제한, 상단 방향 이동 차단은 그대로 유지한다.
7. Collider 오프셋과 프리팹 시각 배치는 변경하지 않는다.

### 1단계 — 후보 Collider Bounds 투영 API 추가

`MonsterBase`에 프리팹 설정을 기준으로 특정 위치에 생성될 Collider Bounds를 계산하는 메서드를 추가한다.

- 입력:
  - 적용할 `MonsterData`
  - 최종 루트 월드 위치
  - 실제 인스턴스가 배치될 부모 Transform의 월드 스케일
- 사용 정보:
  - `ColliderSizeMap`의 BlockSize별 로컬 Collider 크기
  - 프리팹 `BoxCollider2D.offset`
  - 프리팹 루트 `transform.localScale`
  - 풀 부모의 `lossyScale`
- 계산:
  - Collider 중심은 최종 루트 위치에 스케일이 반영된 로컬 오프셋을 더한다.
  - Collider 크기는 BlockSize별 로컬 크기에 실제 월드 스케일의 절댓값을 곱한다.
  - 현재 몬스터 프리팹은 회전 없는 축 정렬 BoxCollider2D이므로 회전 Bounds 확장은 추가하지 않는다.
- Collider 또는 데이터가 없으면 계산 실패를 반환해 배치를 허용하지 않는다.

런타임 활성 몬스터의 `TryGetColliderBounds()`는 실제 `BoxCollider2D.bounds`를 계속 사용한다. 새 API는 아직 생성되지 않은 후보 전용이다.

### 2단계 — MonsterData와 프리팹 매핑 보존

`WaveManager.Awake()`에서 기존 `_poolByData`와 같은 네 쌍으로 `_prefabByData`를 구성한다.

- FluffyData → `_fluffyPrefab`
- SpiderData → `_spiderPrefab`
- StoneBugData → `_stoneBugPrefab`
- ForestDeerData → `_forestDeerPrefab`

후보 Bounds 계산은 이 매핑의 프리팹 설정을 사용한다. `ObjectPool`의 공개 API는 변경하지 않는다.

### 3단계 — 단일 후보 배치 검사 구현

`WaveManager`에 다음 책임을 가진 후보 단위 검사 함수를 추가한다.

1. `_prefabByData`에서 후보 프리팹 조회
2. 후보의 예상 Collider Bounds 계산
3. `_activeMonsters`의 생존 중이며 Collider 조회 가능한 모든 몬스터 순회
4. 기존 `HasPositiveBoundsOverlap()`으로 후보와 실제 Bounds 비교
5. 하나라도 x·y 양쪽 양수 교집합이 있으면 배치 불가
6. 모든 후보와 경계 접촉 또는 분리 상태라면 배치 가능

계산 실패 시 안전하게 배치 불가를 반환한다. 셀 Bounds 기반 `IsCellFree(col, row)`는 최종 점유 판단에서 제거한다.

### 4단계 — 일반 상단 스폰 교체

`TryDispenseRoster()`를 데이터 후보별 검사 방식으로 변경한다.

- 기존 열 셔플과 틱당 3~7마리 제한은 유지한다.
- 각 열과 로스터 후보에 대해 BlockSize가 그리드 경계를 넘지 않는지 먼저 확인한다.
- 기존과 같은 방식으로 BlockSize별 최종 월드 위치를 계산한다.
- 해당 `MonsterData + worldPosition`의 전체 후보 Bounds가 배치 가능한 경우에만 fitting 후보에 넣는다.
- 후보 선택 직후 같은 위치로 `PlaceMonster()`를 호출한다.
- 배치된 몬스터는 즉시 `_activeMonsters`에 들어가므로 같은 틱의 다음 후보 검사에 자동 포함된다.
- `topRowFree`와 `IsCellFree()`를 조합한 셀별 사전 판정은 제거한다.

### 5단계 — 전체 그리드 스폰 교체

`SpawnRosterAcrossFullGrid()`에도 같은 후보 Bounds 검사를 적용한다.

- 기존 `free[,]`는 그리드 경계와 논리적 셀 점유를 빠르게 거르는 용도로 유지한다.
- BlockSize별 셀 점유 조건을 통과한 후보라도 최종 월드 위치의 전체 Collider Bounds가 배치 가능한 경우에만 candidates 목록에 넣는다.
- 1웨이브와 11웨이브 모두 같은 경로를 사용한다.
- 한 후보가 배치되면 기존처럼 `free[,]`를 갱신하고, 실제 인스턴스도 `_activeMonsters`에 추가되어 다음 후보의 실제 Bounds 검사에 포함된다.

### 6단계 — 기존 이동 로직 비변경 확인

다음 코드는 수정하지 않는다.

- `IceBallSkill.OnBallHit()`의 직접 대상 Freeze/Slow/추가 피해
- `MonsterBase.Update()`의 희망 하강 거리 계산
- `WaveManager.GetSafeDownwardDistance()`의 실제 활성 Collider 간격 제한
- 접촉·기존 겹침 시 하강 거리 0
- 반환값 `0..desiredDistance` 제한

스폰 단계에서 신규 겹침을 차단하므로 기존 겹침을 위쪽으로 분리하는 보정은 다시 도입하지 않는다.

### 7단계 — 결정적 회귀 검증

현재 코드에서 실패하는 `0.75`칸 하강 사례를 최소 재현으로 유지한다.

1. 기존 1×1 몬스터가 최고 스폰 행에서 `0.75` 내려간 상태
   - 기존 셀 Bounds 방식: 잘못된 배치 허용
   - 새 후보 Collider 방식: `0.1` 겹침을 검출해 배치 거부
2. 기존 몬스터가 정확히 `0.85` 내려간 상태
   - Collider 경계만 접촉하므로 배치 허용
3. Freeze/Slow로 하강량이 `0.646~0.849` 범위에 머무는 상태
   - 모든 양수 교집합 배치 거부
4. 좌우 인접 열
   - x축 경계만 접촉하면 서로 방해하지 않음
5. 2×1 후보와 1×1·1×2 활성 몬스터 조합
   - 두 열 전체 폭에서 겹침 검사
6. 1×2 후보와 1×1·2×1 활성 몬스터 조합
   - 두 행 전체 높이와 Collider Y 오프셋 반영
7. 후보 계산 결과와 실제 배치 후 `TryGetColliderBounds()` 결과
   - 중심·크기가 허용 오차 내에서 동일

프로젝트에 해당 런타임 흐름을 직접 실행하는 자동화된 Unity 테스트 기반이 없으므로, Bounds 계산의 결정적 수치 하네스와 코드 정적 검증을 사용한다. 최종 동작 판정은 Unity 플레이 및 실기기 테스트로 수행한다.

### 8단계 — 빌드 및 플레이 검증

- 런타임 `Assembly-CSharp.csproj` 빌드
- Editor `Assembly-CSharp-Editor.csproj` 빌드
- 제거 대상인 셀 Bounds 기반 `IsCellFree()` 호출이 남지 않았는지 검색
- Unity 새 플레이 세션에서 다음 확인:
  1. 아이스볼 없이 일반 상단 스폰이 겹치지 않음
  2. Freeze 상태의 몬스터 위에 새 몬스터가 겹쳐 생성되지 않음
  3. Slow 상태에서도 동일
  4. 1×1·2×1·1×2 모든 조합이 겹치지 않음
  5. 멀리 떨어진 후방 몬스터는 계속 이동
  6. 접촉한 후방 몬스터만 정지
  7. 상단 벽 방향으로 밀리는 몬스터가 없음
  8. 1웨이브와 11웨이브 전체 그리드 배치 정상
- Unity 확인 후 Android 실기기에서 반복 검증

### 문서 갱신

구현과 정적 검증 후 다음 문서에 “실기기 재검증 대기” 상태를 반영한다.

- `Assets/_Project/Docs/TODO.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`
- `Assets/_Project/Docs/AIFailures.md`
- `Assets/_Project/Docs/MonsterRules.md`

사용자가 실기기 검증 완료를 확인하기 전에는 TODO 1·5·6번을 삭제하지 않는다.

### 예상 변경 파일

- `Assets/_Project/Scripts/Monster/MonsterBase.cs`
- `Assets/_Project/Scripts/Wave/WaveManager.cs`
- `Assets/_Project/Docs/TODO.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`
- `Assets/_Project/Docs/AIFailures.md`
- `Assets/_Project/Docs/MonsterRules.md`
- `Assets/_Project/Docs/_Task/2026-07-06/14-45_critical-bottom-ice-polish/plan.md`

### 제외 범위

- 몬스터 프리팹 Collider 오프셋·크기 수정
- Rigidbody2D 또는 Unity 물리 충돌 기반 몬스터 밀어내기
- 이미 겹친 몬스터를 위쪽으로 강제 분리
- 부들거림 연출 추가
- 아이스볼 수치·확률·지속시간 변경
- 몬스터 이동 속도 및 스폰 주기 변경
- 사용자 요청 없는 커밋·push

### 구현 및 정적 검증 결과

- `MonsterBase.TryGetProjectedColliderBounds()`가 후보 데이터, 최종 위치, 프리팹 Collider 오프셋, 프리팹 루트 및 풀 부모 스케일로 예상 Bounds를 계산하도록 구현하였다.
- `WaveManager`에 `MonsterData→Prefab` 매핑, `GetPlacementWorldPosition()`, `CanPlaceMonster()`를 추가하였다.
- 일반 상단 스폰은 `topRowFree`와 셀별 `IsCellFree()` 사전 캐시를 제거하고 데이터 후보별 전체 Bounds 검사로 교체하였다.
- 1·11웨이브 전체 그리드 배치는 기존 논리적 `free[,]` 필터 이후 동일한 `CanPlaceMonster()` 검사를 추가하였다.
- 셀 Bounds 기반 `IsCellFree()`는 코드에서 완전히 제거하였다.
- 기존 오판 204개 구간을 재현한 뒤 새 후보 Bounds가 모든 양수 겹침 표본을 거부하는 것을 확인하였다.
- `0.75`칸 하강 겹침 거부, `0.85`칸 경계 접촉 허용, 인접 열, 2×1 전체 폭, 1×2 전체 높이 검증을 통과하였다.
- 런타임 빌드 경고·오류 0개, Editor 빌드 오류 0개를 확인하였다. 기존 `SceneSetupEditor.cs`의 `Rigidbody2D.isKinematic` 폐기 예정 경고 1개는 이번 범위 밖이다.
- Android 실기기 재검증 전이므로 TODO 1·5·6번은 유지한다.
