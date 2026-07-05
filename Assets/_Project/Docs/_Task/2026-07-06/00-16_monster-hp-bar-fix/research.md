# Research — 몬스터 HP바 버그 수정

몬스터 HP바가 (1) 풀링 재사용 시 두 번째 스폰부터 데미지에 반응하지 않는 구독 버그와 (2) 애초에 화면에 렌더링되지 않는 그래픽 누락 버그를 함께 갖고 있어 조사했다. 여기에 `UIRules.md` 섹션 9에 신규 확정된 "피격 시에만 표출" 설계까지 함께 반영해야 한다. 아래는 관련 코드/프리팹/에디터 스크립트를 직접 읽어 재검증한 내용이다.

## 현재 상태

- `MonsterHpBar.cs`는 `Start()`에서 부모의 `MonsterBase`를 찾아 `OnHpChanged` 이벤트를 구독하고, `OnDisable()`에서 구독을 해제한다. `UpdateHp()`는 `_slider.value`만 갱신하며 표시/숨김 로직은 없다.
- `MonsterBase`는 `OnSpawn()`(풀에서 꺼낼 때)과 `ApplyData()`(데이터 최초 적용 시) 양쪽에서 `_currentHp = _monsterData.Hp`로 만피 세팅 후 `OnHpChanged?.Invoke(_currentHp, _monsterData.Hp)`를 발행한다. `TakeDamage()`에서도 데미지 적용 후 `OnHpChanged?.Invoke(Mathf.Max(_currentHp, 0f), _monsterData.Hp)`를 발행한다.
- `ObjectPool<T>.Get()`은 비활성 상태인 기존 인스턴스를 찾으면 `SetActive(true)` 후 `obj.OnSpawn()`만 호출한다. `Instantiate` 자체가 새로 일어나는 것은 풀이 부족해 `CreateNew()`가 새 인스턴스를 만들 때뿐이며, 이 경우에만 Unity가 `Start()`를 호출한다. `Return()`은 `OnDespawn()` 호출 후 `SetActive(false)`로 비활성화한다.
- `Fluffy.prefab` 구조를 직접 확인한 결과, `HpBarCanvas`(World Space Canvas, `MonsterHpBar` 부착)가 `BlockVisual`의 자식으로 이미 배치되어 있고, `HpSlider`(Slider 컴포넌트, `m_Value: 1`)가 그 자식으로 존재한다. `MonsterHpBar._slider`는 이 `HpSlider`의 `Slider` 컴포넌트를 정상적으로 참조하고 있다(`fileID: 6264743512786608632`).
- 그러나 `HpSlider`의 `RectTransform.m_Children: []` — 자식 오브젝트가 전혀 없다. `Slider` 컴포넌트의 `m_FillRect: {fileID: 0}`, `m_HandleRect: {fileID: 0}`로 둘 다 비어 있고, `m_TargetGraphic: {fileID: 0}`도 비어 있다. `m_Interactable: 1`(활성 상태)이다.
- `MonsterOverhaulSetupEditor.cs`는 이미 몬스터 오버홀 작업 전용으로 만들어진 에디터 스크립트로, `[MenuItem("PurpleCow/Setup/Monster Overhaul Setup")]` 하나에서 `SetupMonsterDataBlockSizes()` → `SetupWaveTableData()` → `SetupPrefabBlockVisuals()` 3단계를 순차 실행한다. 이 중 `SetupPrefabBlockVisuals()`(내부적으로 `SetupPrefabBlockVisual(config)`를 4개 프리팹에 반복 호출)가 `BlockVisual` 자식 생성과 `HpBarCanvas` 재배치를 담당하며, `PrefabUtility.EditPrefabContentsScope`로 각 프리팹을 열어 수정한다.
- 중요한 구조적 포인트: `SetupPrefabBlockVisual()` 내부에 `if (root.transform.Find("BlockVisual") != null) { ...; return; }`라는 조기 반환(early return)이 있다. `UIRules.md`에 "배치(블록 앞면 임베드) 자체는 이미 구현 완료 상태"라고 명시된 대로, 4개 프리팹 모두 이미 `BlockVisual`을 갖고 있을 것으로 추정되므로, 이 메서드는 현재 호출 시 스케일만 갱신하고 즉시 `return`하여 그 아래의 `HpBarCanvas` 재배치 로직(및 향후 추가될 Fill/Background 생성 로직)까지 도달하지 못한다. 즉, Fill/Background 생성 로직을 이 메서드 내부(early return 이후 위치)에 단순히 끼워 넣으면 실행되지 않는 문제가 생긴다.

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/UI/MonsterHpBar.cs` — HP바 표시 스크립트. `MonsterBase.OnHpChanged` 구독, `Slider.value` 갱신.
- `Assets/_Project/Scripts/Monster/MonsterBase.cs` — `OnHpChanged` 이벤트 발행처(`OnSpawn`/`ApplyData`/`TakeDamage`). `IPoolable` 구현(`OnSpawn`/`OnDespawn`).
- `Assets/_Project/Scripts/Core/ObjectPool.cs` — `Get()`이 `SetActive(true)` + `OnSpawn()`만 호출, `Start()` 재호출 없음. `Return()`이 `OnDespawn()` + `SetActive(false)`.
- `Assets/_Project/Prefabs/Monster/Fluffy.prefab` — `HpSlider`(Slider, 자식 없음, `m_FillRect`/`m_HandleRect` 미연결) 구조 확인. Spider/StoneBug/ForestDeer도 동일한 패턴으로 추정(`MonsterOverhaulSetupEditor.cs`가 4종 모두 동일 코드 경로로 세팅하므로).
- `Assets/_Project/Scripts/Editor/MonsterOverhaulSetupEditor.cs` — 이번 프리팹 구조 보완을 얹을 기존 에디터 스크립트. `Configs` 배열(4개 프리팹 설정), `SetupPrefabBlockVisual()`의 early return 구조.
- `Assets/_Project/Docs/UIRules.md` 섹션 9 — 배치 방식(블록 앞면 임베드, 폭 비례) 확정 설계 + 버그 1/버그 2 기록 + "피격 시에만 표출" 신규 확정 설계.

## 문제점 / 구현 대상 파악

1. **버그 1 (구독 끊김)**: `Start()`는 오브젝트 생애 1회만 호출되지만 `OnDisable()`은 풀 반납 시마다 호출되어, 최초 스폰 이후 재사용부터 `OnHpChanged` 구독이 영구히 사라진다. → `Start()`를 `OnEnable()`으로 바꿔 재활성화 시마다 재구독되도록 해야 한다.
2. **버그 2 (그래픽 없음)**: `HpSlider`에 `Background`/`Fill Area`/`Fill` 자식이 전혀 없고 `Slider.m_FillRect`가 비어 있어, `Slider.value`가 바뀌어도 그려줄 그래픽이 없다. → 4개 프리팹의 `HpSlider` 아래에 시각적 자식 구조를 생성하고 `m_FillRect`를 연결해야 한다. 프리팹 애셋 수정이라 에디터 스크립트(`MenuItem`)로 처리해야 한다.
3. **신규 설계 (피격 시에만 표출)**: 현재 `UpdateHp()`는 값 갱신만 하고 표시/숨김을 제어하지 않는다. `current < max`일 때만 `_slider.gameObject`(HpSlider, HpBarCanvas의 자식)를 활성화해야 한다. `HpBarCanvas`(스크립트 자신이 붙은 오브젝트)를 끄면 `OnDisable()`이 호출되어 구독이 끊기므로, 반드시 자식인 `_slider.gameObject`만 토글해야 한다.
4. **에디터 스크립트 확장 시 구조적 함정**: `SetupPrefabBlockVisual()`의 `BlockVisual` 존재 시 early return 때문에, Fill/Background 생성 로직을 그 메서드 안에 단순 추가하면 이미 `BlockVisual`이 존재하는 현재 4개 프리팹에서는 실행되지 않는다. 별도의 독립적인 단계(메서드)로 분리해 `SetupMonsterOverhaul()`에서 별도 호출하는 방식이 필요하다.

## 결론

버그 1은 `MonsterHpBar.cs`의 `Start()` → `OnEnable()` 이름 변경만으로 해결 가능한 순수 코드 수정이다. 신규 확정 설계("피격 시에만 표출")는 같은 파일의 `UpdateHp()`에 `_slider.gameObject.SetActive(current < max)` 한 줄만 추가하면 되고, `MonsterBase`가 스폰 시 `current == max`로 이벤트를 발행하는 기존 동작 덕분에 별도 플래그 없이 자동으로 리셋된다. 버그 2는 프리팹 애셋 구조 변경이 필요해 코드만으로는 해결 불가능하며, `MonsterOverhaulSetupEditor.cs`에 Fill/Background 생성 + `Slider.m_FillRect` 연결 로직을 새로운 독립 단계로 추가하고, 기존 `SetupPrefabBlockVisual()`의 early return 구조와 충돌하지 않도록 별도 메서드/별도 프리팹 오픈 스코프로 분리해야 한다. 구체적인 구현 순서와 파일별 변경 사항은 `plan.md`에 정리한다.
