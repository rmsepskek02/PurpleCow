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
