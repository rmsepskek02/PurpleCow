# Plan — 몬스터 HP바 버그 수정

`research.md`에서 확인한 버그 1(구독 끊김), 버그 2(그래픽 미렌더링), 그리고 신규 확정 설계("피격 시에만 표출")를 함께 구현하는 계획이다. 코드 수정은 `MonsterHpBar.cs` 한 파일, 프리팹 구조 변경은 새로 작성하는 전용 에디터 스크립트 `MonsterHpBarSetupEditor.cs`의 메뉴 실행으로 처리한다(기존 `MonsterOverhaulSetupEditor.cs`는 건드리지 않는다).

## 구현 목표

1. 몬스터가 오브젝트 풀에서 재사용되어도 HP바가 매 스폰마다 정상적으로 데미지 이벤트를 구독하도록 수정한다.
2. 몬스터 HP바를 스폰 직후(만피)에는 `CanvasGroup.alpha = 0`으로 숨기고, 데미지를 받아 `current < max`가 되는 순간 `alpha = 1`로 표출하며, 재스폰 시 다시 숨김 상태(`alpha = 0`)로 리셋되도록 만든다. `GameObject.SetActive`는 표시/숨김 목적으로는 사용하지 않는다.
3. 4개 몬스터 프리팹(Fluffy/Spider/StoneBug/ForestDeer)의 `HpSlider`에 실제로 채워지는 그래픽(Border/Background/Fill Area/Fill)을 추가하고 `Slider.m_FillRect`를 연결해, HP바가 화면에 실제로 렌더링되도록 한다. 색상은 Border/Fill `#5A100F`, Background `#2C2C2C`로 확정한다.

## 단계별 작업 계획

### 1. `MonsterHpBar.cs` — 구독 시점 수정 (버그 1)

`Start()`를 `OnEnable()`으로 이름만 변경한다. 구독 로직 내용은 동일하게 유지한다.

```csharp
private void OnEnable()
{
    _monster = GetComponentInParent<MonsterBase>();
    if (_monster != null)
        _monster.OnHpChanged += UpdateHp;
}
```

`OnEnable()`은 오브젝트가 재활성화될 때마다(풀에서 `SetActive(true)`로 다시 꺼내질 때마다) 매번 호출되므로, `OnDisable()`(풀 반납 시 매번 호출, 구독 해제)과 정확히 짝을 이루게 되어 재사용 시에도 매번 재구독된다.

### 2. `MonsterHpBar.cs` — 피격 시에만 표출, `CanvasGroup.alpha`로 토글 (신규 확정 설계)

사용자 피드백("체력바도 canvas로 구성된 거 아닌가? 활성화 비활성화는 이런 문제가 자주 발생하니까 canvas alpha 값으로 조정하자")에 따라, `GameObject.SetActive` 대신 `CanvasGroup.alpha`로 표시/숨김을 토글한다. `CanvasGroup`은 `HpBarCanvas`(`Canvas` 컴포넌트와 `MonsterHpBar` 스크립트가 함께 붙어있는 바로 그 오브젝트)에 추가한다.

`_canvasGroup` 필드는 에디터에서 수동으로 연결하지 않고, `OnEnable()`에서 `GetComponent<CanvasGroup>()`으로 런타임에 자동 참조한다. `MonsterHpBar` 스크립트 자신이 `HpBarCanvas`에 붙어있으므로 `GetComponent`만으로 항상 같은 오브젝트의 `CanvasGroup`을 정확히 가져올 수 있고, 새 에디터 스크립트가 프리팹 저장 시 `MonsterHpBar` 컴포넌트의 필드를 별도로 연결해줄 필요가 없어 더 간단하다(에디터 스크립트는 `CanvasGroup` 컴포넌트 추가만 담당).

```csharp
[SerializeField] private Slider _slider;

private MonsterBase _monster;
private CanvasGroup _canvasGroup;

private void OnEnable()
{
    _monster = GetComponentInParent<MonsterBase>();
    if (_monster != null)
        _monster.OnHpChanged += UpdateHp;

    if (_canvasGroup == null)
        _canvasGroup = GetComponent<CanvasGroup>();
}

private void OnDisable()
{
    if (_monster != null)
        _monster.OnHpChanged -= UpdateHp;
}

private void UpdateHp(float current, float max)
{
    _slider.value = max > 0f ? current / max : 0f;
    _canvasGroup.alpha = current < max ? 1f : 0f;
}
```

이 방식의 핵심 장점은 버그 1(구독 끊김)과 완전히 독립적으로 동작한다는 것이다. `CanvasGroup.alpha` 조정은 GameObject를 비활성화하지 않으므로 `OnEnable`/`OnDisable`이 전혀 발동하지 않는다. 따라서 기존 계획에서 있었던 "스크립트가 붙은 오브젝트(`HpBarCanvas`)를 직접 꺼서는 안 되고 자식(`_slider.gameObject`)만 꺼야 한다"는 제약 자체가 사라진다 — 애초에 어떤 `SetActive`도 표시/숨김 용도로 쓰지 않기 때문이다.

`MonsterBase.OnSpawn()`/`ApplyData()`가 스폰 시점에 `current == max`로 `OnHpChanged`를 발행하므로, 스폰될 때마다 자동으로 `alpha = 0`(숨김) 상태로 리셋되고, `TakeDamage()`로 `current < max`가 되는 순간 자동으로 `alpha = 1`(표출)로 전환된다. 별도의 "이미 맞았는지" 플래그는 필요 없다 — HP 값 자체로 표시 여부를 판단한다.

추가로, HP바는 클릭/드래그 대상이 아니므로 `CanvasGroup.blocksRaycasts = false`, `CanvasGroup.interactable = false`로 설정해 `alpha = 0`이어도 레이캐스트를 막지 않도록 한다. 이 두 값은 코드가 아니라 에디터 스크립트가 `CanvasGroup` 컴포넌트를 추가할 때 기본값으로 설정한다(런타임에 매번 설정할 필요 없음).

### 3. `MonsterHpBarSetupEditor.cs` (신규) — Border/Background/Fill 생성 + `m_FillRect` 연결 + `CanvasGroup` 추가 (버그 2)

사용자 피드백("에디터작업은 새로운 에디터스크립트를 작성해서 진행하자")에 따라, 기존 `MonsterOverhaulSetupEditor.cs`는 전혀 수정하지 않는다. 대신 완전히 새로운 파일 `Assets/_Project/Scripts/Editor/MonsterHpBarSetupEditor.cs`를 만들어 독립된 `[MenuItem("PurpleCow/Setup/Monster HP Bar Setup")]`으로 실행한다. 이는 프로젝트 컨벤션(`BackgroundGridFitSetupEditor.cs`, `MonsterOverhaulSetupEditor.cs`가 이미 각자 목적별 전용 에디터 스크립트로 분리되어 있음)을 따르는 방식이다.

- 대상 프리팹 4개(Fluffy/Spider/StoneBug/ForestDeer)는 `MonsterOverhaulSetupEditor.cs`의 `Configs` 배열과 동일한 경로(`Assets/_Project/Prefabs/Monster/{Name}.prefab`)를 이 새 스크립트 안에 별도로(중복) 나열한다 — 기존 파일의 `Configs`를 참조/공유하지 않고 새 스크립트 내부에 자체 목록을 둔다(기존 파일을 건드리지 않기 위함).
- `[MenuItem("PurpleCow/Setup/Monster HP Bar Setup")]`이 붙은 진입 메서드가 4개 프리팹을 순회하며, 프리팹별로 `PrefabUtility.EditPrefabContentsScope`를 열어 다음을 수행한다:
  1. `root.GetComponentInChildren<Slider>(true)`로 `HpSlider`(`Slider` 컴포넌트)를 찾는다(못 찾으면 경고 로그 후 스킵).
  2. `slider.transform`에서 조상 방향으로 올라가 `Canvas` 컴포넌트를 가진 오브젝트(`HpBarCanvas`)를 찾는다(`GetComponentInParent<Canvas>()`). 여기에 `CanvasGroup` 컴포넌트가 없으면 `AddComponent<CanvasGroup>()`으로 추가하고, `blocksRaycasts = false`, `interactable = false`를 기본값으로 설정한다(이미 있으면 스킵, 값은 그대로 둠).
  3. `MonsterHpBar.cs`가 `OnEnable()`에서 `GetComponent<CanvasGroup>()`으로 런타임에 자동 참조하는 방식을 채택했으므로, `MonsterHpBar` 컴포넌트의 필드를 에디터에서 별도로 연결하는 단계는 필요 없다.
  4. **멱등성 체크**: `slider.fillRect != null`이면 이미 세팅된 것으로 간주하고 로그만 남기고 스킵(중복 생성 방지).
  5. 아직 세팅되지 않았다면 `HpSlider` 아래에 다음 계층 구조를 생성한다:
     - `Border` (Image, `HpSlider`를 anchorMin (0,0)/anchorMax (1,1)/sizeDelta (0,0)로 채움, 색상 `#5A100F` 불투명 — RGBA ≈ (0.353, 0.063, 0.059, 1))
       - `Background` (Image, `Border`보다 약간 안쪽으로 들어간(inset) 영역 — 예: anchorMin (0.06, 0.15)/anchorMax (0.94, 0.85) 또는 이에 준하는 값으로 좁혀 `Border`가 테두리처럼 보이도록 함, 색상 `#2C2C2C` 불투명 — RGBA ≈ (0.173, 0.173, 0.173, 1))
       - `Fill Area` (RectTransform, `Background`와 동일한 inset 영역)
         - `Fill` (Image, `Fill Area`를 anchorMin (0,0)/anchorMax (1,1)/sizeDelta (0,0)로 채움, 색상 `#5A100F` 불투명 — `Border`와 동일 색상)
  6. `slider.fillRect`를 새로 만든 `Fill`의 `RectTransform`으로 연결한다. `slider.handleRect`는 연결하지 않는다(핸들 없는 순수 표시용 바).
  7. `slider.interactable = false`로 설정한다(플레이어가 직접 조작하는 UI가 아니므로).
  8. 완료 로그를 남기고, 전체 프리팹 처리 후 `AssetDatabase.SaveAssets()`/`AssetDatabase.Refresh()`를 호출한다.

`Border`와 `Fill`이 같은 색상(`#5A100F`)이라 시각적으로는 `Background`(`#2C2C2C`)를 감싸는 프레임 역할만 하며, 실제 체력 표시는 `Fill`의 `fillAmount`(`Slider.value` 연동)로 표현된다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/UI/MonsterHpBar.cs` (수정) — `Start()` → `OnEnable()`, `_canvasGroup` 필드(런타임에 `GetComponent<CanvasGroup>()`으로 자동 참조) 추가, `UpdateHp()`에 `_canvasGroup.alpha = current < max ? 1f : 0f` 표시/숨김 토글 추가(`SetActive`는 사용하지 않음).
- `Assets/_Project/Scripts/Editor/MonsterHpBarSetupEditor.cs` (신규 생성) — `[MenuItem("PurpleCow/Setup/Monster HP Bar Setup")]`으로 4개 프리팹을 순회하며 `HpBarCanvas`에 `CanvasGroup` 추가(`blocksRaycasts = false`, `interactable = false`), `HpSlider` 아래 `Border`(`#5A100F`)/`Background`(`#2C2C2C`)/`Fill Area`/`Fill`(`#5A100F`) 자식 생성, `Slider.fillRect` 연결, `interactable = false` 처리. 기존 `MonsterOverhaulSetupEditor.cs`는 수정하지 않는다.
- `Assets/_Project/Prefabs/Monster/Fluffy.prefab`, `Spider.prefab`, `StoneBug.prefab`, `ForestDeer.prefab` (신규 에디터 스크립트 실행 결과로 변경됨) — `HpBarCanvas`에 `CanvasGroup` 컴포넌트 추가, `HpSlider` 아래 `Border`/`Background`/`Fill Area`/`Fill` 자식 추가, `Slider.m_FillRect` 연결, `m_Interactable: 0` 설정.

## 주의사항

- 이 작업은 Unity 에디터 실행이 필요한 프리팹 구조 변경을 포함한다. 코드 수정(`MonsterHpBar.cs`, `MonsterHpBarSetupEditor.cs` 신규 생성) 후 사용자가 로컬 Unity에서 `PurpleCow/Setup/Monster HP Bar Setup` 메뉴를 실행해야 실제 프리팹에 `CanvasGroup`/`Border`/`Background`/`Fill`이 생성된다. 이 원격 환경에는 Unity 에디터가 없어 프리팹 결과를 직접 검증할 수 없다.
- 색상은 사용자가 확정했다: Border/Fill `#5A100F`, Background `#2C2C2C`. 각 Image 자체의 알파는 불투명(1)로 두며, 실제 표시 여부는 `CanvasGroup.alpha`가 전담한다(Image 알파로 숨김을 처리하지 않는다).
- `CanvasGroup.alpha = 0`이어도 기본값 `blocksRaycasts = true`, `interactable = true`이면 레이캐스트를 막을 수 있으므로, 에디터 스크립트가 `CanvasGroup`을 추가할 때 반드시 `blocksRaycasts = false`, `interactable = false`를 기본값으로 설정해야 한다.
- 에디터 스크립트를 반복 실행해도 중복 생성되지 않도록(멱등성) `slider.fillRect != null` 여부로 스킵 체크가 필요하다. `CanvasGroup` 추가 역시 이미 존재하면 재추가하지 않는다.
- HP바가 표시된 이후에도 `Slider.value`는 계속 갱신되어야 하며, 표시/숨김 토글과 값 갱신 로직이 같은 `UpdateHp()` 메서드 안에서 함께 처리되어 순서 문제가 없어야 한다.
- `MonsterHpBarSetupEditor.cs`는 완전히 새로운 파일이며, 기존 `MonsterOverhaulSetupEditor.cs`의 `Configs` 배열이나 메서드를 공유/참조하지 않고 대상 프리팹 경로를 자체적으로 나열한다 — 기존 파일은 이번 작업에서 전혀 수정하지 않는다.
- `.cs`/`.prefab` 파일은 이번 task 문서 수정 단계에서는 읽기만 했으며 수정하지 않았다. 실제 구현은 사용자의 plan.md 승인 후 진행한다.
