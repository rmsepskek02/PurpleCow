# UIRules.md

이 문서는 UI 시스템 구현 시 모든 에이전트가 따라야 할 규칙입니다.
개발 공통 규칙은 [DevRules.md](DevRules.md)를 함께 참고하세요.

---

## 1. Canvas 구조 및 레이어

Canvas는 3개로 분리하며 Sort Order로 레이어를 관리합니다.

```
Canvas_HUD    (Sort Order: 10)  ← 항상 표시
  ├─ SafeAreaPanel              ← SafeAreaFitter 컴포넌트 부착
  │    ├─ HUD_Static Canvas     ← 변하지 않는 배경/테두리/아이콘
  │    └─ HUD_Dynamic Canvas    ← HP바, 웨이브 진행바, 텍스트
  ├─ TopBar   (스테이지명, %, 아이콘*)
  ├─ TopButtons (▶ 재생, ⏸ 일시정지)
  ├─ WaveBar  (진행바 + 웨이브 번호 배지)
  ├─ CharacterHP (캐릭터 HP바 - 하단)
  └─ CharacterXP (캐릭터 경험치바 + 레벨 텍스트 - 하단)

Canvas_Panel  (Sort Order: 20)  ← 패널 오픈 시 표시
  ├─ LevelUpPanel
  ├─ PausePanel
  └─ BallLevelUpPanel

Canvas_Popup  (Sort Order: 30)  ← 최상위
  └─ BallAcquirePopup
```

모든 Canvas는 `Screen Space - Overlay` 방식을 사용합니다.

> `TopButtons`는 PDF(`PurpleCow_클라이언트_채용과제.pdf`)에서 "배속 기능"과 "자동 조준 기능"을 구현 제외 항목으로 명시하고 있어, 배속 버튼과 Auto 버튼은 이번 프로토타입에서 만들지 않는다. 재생/일시정지 버튼만 구현 대상이다.
>
> \* `TopBar`의 "아이콘"은 원본 게임에서 보스 등장 아이콘 역할이었으나, 이번 프로토타입은 보스를 구현하지 않는다(PDF 제외 항목). 따라서 이 아이콘은 장식용으로만 유지하거나 생략 가능하며, 진행률 바(%) 자체는 정상 구현 대상이다.

---

## 2. 해상도 대응 (Canvas Scaler)

세로(Portrait) 모바일 게임 기준입니다.

| 항목 | 설정값 |
|------|--------|
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1080 x 1920 |
| Screen Match Mode | Match Width Or Height |
| Match | 0 (Width 기준) |

가로폭을 기준으로 스케일하며, 세로가 긴 기기는 상하 여백이 늘어납니다.
따라서 상단 요소는 `Anchor Top`, 하단 요소는 `Anchor Bottom`, 중앙 패널은 `Anchor Center`로 설정해야 합니다.

---

## 3. Safe Area 처리

노치, 펀치홀, 홈 인디케이터 등 기기 침범 영역을 대응합니다.

- `SafeAreaFitter` 컴포넌트를 `Canvas_HUD` 내 `SafeAreaPanel`에 부착
- `Screen.safeArea`를 읽어 RectTransform의 offsetMin/offsetMax를 자동 조정
- `Canvas_Panel` / `Canvas_Popup`은 Safe Area 미적용 (전체 화면 덮는 용도)

---

## 4. 패널 표시/숨김 방식

`SetActive` 대신 `CanvasGroup` 컴포넌트를 사용합니다.
`SetActive(false)` 시 `Awake()`/`Start()`가 호출되지 않는 초기화 문제를 방지하기 위함입니다.

| 상태 | alpha | interactable | blocksRaycasts |
|------|-------|-------------|----------------|
| 숨김 | 0 | false | false |
| 표시 | 1 | true | true |

모든 패널/팝업은 씬 시작부터 활성화 상태(`SetActive(true)`)를 유지하며 CanvasGroup으로만 제어합니다.

---

## 5. UI 애니메이션

**라이브러리**: DOTween 사용

**패널 전환 기본 패턴** (모든 패널/팝업 동일하게 적용, 추후 개별 보완):
- 진입: 아래에서 위로 슬라이드 + FadeIn
- 종료: 위에서 아래로 슬라이드 + FadeOut
- 배경 딤: 별도 FadeIn/Out

**공통 규칙**:
- 모든 수치(시간, 이동 거리, ease 타입 등)는 `[SerializeField]`로 Inspector에서 조절
- 애니메이션은 DOTween `Sequence`로 묶어 순서 보장
- 애니메이션 재생 중 `CanvasGroup.interactable = false`로 입력 차단
- 애니메이션 완료 후 입력 다시 허용

---

## 6. 버튼 피드백

모든 버튼에 `UIButton` 컴포넌트를 부착합니다.
Unity 기본 `Button`의 Transition은 **None**으로 설정하고, `UIButton`이 전담합니다.

- `OnPointerDown`: Scale 1.0 → 0.9 (DOTween)
- `OnPointerUp`: Scale 0.9 → 1.0 (DOTween)
- 수치는 `[SerializeField]`로 Inspector 조절

**비활성 버튼 처리**:
- `CanvasGroup.alpha` 낮춤 (시각적 표현)
- `CanvasGroup.interactable = false` (입력 차단)

**사운드**: 추후 별도 처리 예정

---

## 7. 성능 최적화 규칙

| 규칙 | 내용 |
|------|------|
| HUD Canvas 분리 | HUD_Static(정적 요소)과 HUD_Dynamic(동적 요소)을 별도 Canvas로 분리 |
| Raycast Target | Image/Text 컴포넌트 기본값 OFF, 버튼만 ON |
| GraphicRaycaster | Canvas당 하나씩만 유지 |
| HP바/진행바 갱신 | 매 프레임 갱신 금지, 값 변경 시 이벤트로만 갱신 |
| 비활성 패널 | SetActive 사용 금지, CanvasGroup으로만 처리 |

---

## 8. 데미지 텍스트

**방식**: World Space TMP 직접 배치 — Canvas 없이 월드 좌표에 배치

- `DamageTextFx` (MonoBehaviour + IPoolable): TMP_Text 컴포넌트, 위로 떠오르며 FadeOut (DOTween)
- `DamageTextManager` (Singleton): `ObjectPool<DamageTextFx>` 보유, `ShowDamage(Vector3 worldPos, float damage, bool isCritical)` 제공
- 크리티컬 여부에 따라 텍스트 색상/크기 차이 적용
- 수치(이동 거리, 지속 시간, 폰트 크기 등)는 `[SerializeField]`로 Inspector 조절

---

## 9. 몬스터 HP바

이 섹션은 원본 게임 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/`)을 실측 확인한 결과에 따라 전면 재작성되었습니다. 기존에 서술되어 있던 "몬스터 머리 위에 뜨는 월드 스페이스 캔버스 + 슬라이더" 방식은 폐기되었습니다.

### 확정된 방식 — 블록(베이스) 앞면 임베드

- 몬스터 프리팹은 **블록(베이스, 발판) + 캐릭터 스프라이트**가 합쳐진 하나의 프리팹이다(`MonsterRules.md` 3장 참고). HP바는 몬스터 머리 위가 아니라 **이 블록의 앞면(정면 하단)에 임베드된 형태**로 표시된다.
- HP바의 폭은 **블록의 가로 길이에 비례**한다. 1칸 블록(`Block_1x1`, Fluffy/Spider)과 세로 2칸 블록(`Block_1x2`, ForestDeer)은 좁은 폭으로, 가로 2칸 블록(`Block_2x1`, StoneBug)은 그 2배 폭으로 표시된다.
- 정확한 배치 좌표/두께/색상 등 세부 비율은 이 문서에서 수치로 못박지 않으며, `Assets/_Project/Docs/targetUI/` 레퍼런스 이미지를 그대로 재현하는 것을 기준으로 삼는다.
- **(신규 확정 — 아직 코드 미반영)** 몬스터 HP바는 스폰 직후(만피 상태)에는 숨겨져 있다가, 몬스터가 데미지를 받아 현재 HP가 최대 HP보다 작아지는 순간부터 표출된다. 즉 "피격된 대상만 HP바가 보인다"가 확정된 설계다. 한 번 표출되면 해당 몬스터가 죽거나(풀 반납) 필드에서 사라질 때까지 계속 표출 상태를 유지하며, 몬스터가 풀에서 재사용되어 다시 스폰될 때는 HP가 만피로 리셋되므로 HP바도 다시 숨김 상태로 리셋되어야 한다.
- 배치(블록 앞면 임베드) 자체는 이미 구현 완료 상태다. `Fluffy.prefab`/`StoneBug.prefab`을 직접 확인한 결과 `HpBarCanvas`(World Space Canvas)가 `BlockVisual`의 자식으로 이미 옮겨져 있고, 앞면 하단 오프셋(`m_AnchoredPosition`)으로 앵커링되어 있다. 아래 "배치 방식" 소섹션은 과거에 미구현이었던 시점의 기록이며, 현재는 완료된 상태로 정정한다.

### 배치 방식 (앵커/부모 구조 변경) — 구현 완료

- 기존(폐기됨): 몬스터 프리팹 최상위에 `World Space Canvas`를 자식으로 두고, 몬스터 머리 위쪽으로 오프셋된 위치에 배치.
- 현재(구현 완료): HP바(Slider)는 **블록 오브젝트(`BlockVisual`)의 자식**으로 옮겨 붙여져 있고, 블록 앞면 하단에 앵커링되어 있다. 블록 크기가 종류별로 다르므로(3장 참고) HP바의 `RectTransform` 가로 크기도 블록 크기에 맞춰 종류별로 다르게 설정된다(2칸 블록은 1칸 블록의 2배 폭). 이 폭 조정은 `MonsterBase.ApplyBlockSize()`에서 `HpBarWidthMap`을 참조해 런타임에 적용된다.
- Canvas 설정은 기존과 동일하게 유지한다: Render Mode = World Space, Sorting Layer = UI.

### 발견된 버그 (아직 코드 미반영)

코드/프리팹을 직접 읽어 확인한 결과, 아래 2가지 문제가 발견되었다. 수정은 이 문서 갱신 범위가 아니며 별도 task 문서(`research.md`/`plan.md`)에서 다룬다.

- **버그 1 (오브젝트 풀링과 구독 로직 충돌, 아직 코드 미반영)**: `MonsterHpBar.cs`의 `Start()`가 `_monster.OnHpChanged`를 구독하는데, Unity의 `Start()`는 오브젝트 생애 동안 딱 한 번만 호출된다. 몬스터는 풀링되어 재사용되므로(`ObjectPool.Get()`이 `SetActive(true)`만 호출하고 `Start()`는 다시 호출하지 않음), 최초 스폰 때만 구독이 성립하고 이후 `OnDisable()`(풀 반납 시 매번 호출)에서 구독 해제만 계속 쌓여, 두 번째 재사용부터는 HP바가 데미지에 전혀 반응하지 않는다.
- **버그 2 (HP바가 시각적으로 아예 렌더링되지 않음, 아직 코드 미반영)**: `Fluffy.prefab`/`StoneBug.prefab`의 `HpSlider` 오브젝트를 확인한 결과, `Slider` 컴포넌트의 `m_FillRect`와 `m_HandleRect`가 모두 비어있고(`{fileID: 0}`), Background/Fill Area/Fill/Handle 같은 시각적 자식 오브젝트가 아예 없다. `Slider.value`가 바뀌어도 실제로 그려줄 그래픽이 없어 HP바가 항상 화면에 보이지 않는 상태다.

### 재사용 가능한 부분

- `MonsterHpBar` (MonoBehaviour)와 `MonsterBase.OnHpChanged(float current, float max)` 이벤트 구독 구조는 배치 방식만 바뀔 뿐 **그대로 재사용 가능**하다.
- `MonsterBase`의 `public event Action<float, float> OnHpChanged` 발행 시점(TakeDamage/Die/OnSpawn/ApplyData)도 변경 없이 그대로 사용한다.
- HP 0이 되면 HP바 오브젝트는 풀 반납 시 자동 비활성화되는 동작도 유지한다.
- 변경되는 것은 HP바의 **부모 오브젝트(월드 캔버스의 부착 위치)와 앵커/크기 설정**뿐이다.

---

## 10. 캐릭터 HP / 경험치 / 레벨 시스템

**담당 클래스**: `CharacterManager` (Singleton)

### HP

- `[SerializeField] private int _maxHp` — Inspector 설정
- 몬스터가 맨 아래줄을 통과하면 `MonsterData._damage`만큼 HP 차감
- HP 0 → `GameManager.EndGame(false)`
- 이벤트: `public static event Action<int, int> OnHpChanged` (현재, 최대)
- HUD: `CharacterHpBar` (MonoBehaviour, Slider + TMP_Text 현재/최대 HP 숫자 표시), `CharacterHP` 오브젝트에 부착

### 경험치 / 레벨

- `[SerializeField] private int[] _xpPerLevel` — 레벨별 필요 XP (Inspector 배열 설정)
- XP 획득 조건: 몬스터 처치 / 몬스터 통과 모두 `MonsterData.reward`만큼 획득 (원본 게임 실제 플레이 경험으로 검증 완료된 사항, 규칙 변경 없음)
- XP 가득 차면 레벨업 → `OnLevelUp(int newLevel)` 이벤트 발행 → `LevelUpPanel` 오픈
- 이벤트: `public static event Action<int, int> OnXpChanged` (현재, 필요량)
- HUD: `CharacterXpBar` (MonoBehaviour, Slider + TMP_Text 레벨 표시), `CharacterXP` 오브젝트에 부착

### WaveManager 연동

- `WaveManager`에 `public static event Action<MonsterBase> OnMonsterReachedBottom` 추가 필요
- `CharacterManager`가 이 이벤트를 구독하여 HP/XP 처리

---

## 11. 궤적 프리뷰 시각 규칙

**담당 클래스**: `TrajectoryPreview` (`Assets/_Project/Scripts/Ball/TrajectoryPreview.cs`)

원본 스펙은 [GameplayMechanics.md](GameplayMechanics.md) 섹션 1을 참고합니다.

- 터치 여부와 무관하게 항상 표시되며, 매 프레임 실시간으로 갱신된다(터치 중에는 드래그 방향을, 터치하지 않을 때는 마지막 조준 방향을 기준으로 갱신).
- 궤적선은 점선(dashed line) 형태이며, 1차 충돌 지점까지 + 반사 후 2차 충돌 지점까지 총 2단계 선분으로 그려진다. 3차 충돌 이후는 표시하지 않는다.
- 2차 충돌 지점에는 빨간 점(레드닷)과 그 점을 감싸는 원형 궤적선(고리)이 표시된다. 고리는 실선이 아니라 끊어진 점선 형태이며, 조준(터치) 여부와 무관하게 항상 시계방향으로 계속 회전한다.
- 충돌 판정은 `Physics2D.RaycastAll` + `Wall`/`Ground`/`Monster` 태그 화이트리스트 필터링으로 처리한다. 태그가 없는 다른 볼 오브젝트는 자동으로 제외된다.

### 구현 방식

- `LineRenderer` 기반으로 그린다. 별도 스프라이트 에셋은 사용하지 않는다.
- 궤적선의 점선은 런타임 생성한 텍스처(4x1)와 `LineRenderer.textureMode = Tile` 조합으로 구현한다.
- 레드닷은 `CreateSolidTexture()`로 만든 단색 텍스처를 사용하는 원형 점열(정다각형 근사, `LineRenderer`)로 구현하며, 회전하지 않는다.
- 원형 궤적선(고리)은 텍스처 자체는 레드닷과 동일하게 `CreateSolidTexture()`로 만든 단색 텍스처를 사용하며, 점선처럼 보이는 효과는 텍스처 타일링이 아니라 `_hitRing.colorGradient`(`BuildRingDashGradient()`)로 만든다. 원 둘레를 4등분해 각 등분의 정중앙(t = 0, 0.25, 0.5, 0.75)은 알파 1(가장 밝음), 등분 경계(t = 0.125, 0.375, 0.625, 0.875)는 알파 0(안 보임)이 되도록 8개의 alphaKeys를 배치하고 그 사이를 선형 보간함으로써, 정확히 4개의 밝은 호가 고리 둘레에 고르게 나타난다(`colorKeys`는 시작/끝 2개만 두고 둘 다 `_ringColor`로 고정해 색상 자체는 균일하게 유지한다). 참고로 이전에는 `CreateRingDashTexture()`로 전용 점선 텍스처를 만들고 `RING_DASH_COUNT`(10개) 기준으로 `mainTextureScale`을 계산해 타일링하는 방식을 시도했으나, 실제 렌더링 결과가 의도한 개수(10개)와 다르게 나타나(2개로 보임) 이 방식은 폐기되었다.
- 고리의 회전은 `transform.Rotate()`가 아니라, 정점 좌표를 계산하는 `DrawCircle()` 메서드에 `rotationOffsetDeg` 선택적 파라미터를 추가해 `angle = 기존각도 - offsetRad`로 정점 각도 자체를 시간에 따라 감소시키는 방식으로 구현한다. `_hitRing`을 그릴 때만 `Time.time * _ringRotationSpeed`(deg/sec)로 계산한 오프셋을 넘겨 매 프레임 시계방향으로 회전시키며, 이 회전은 `Update()`가 터치(조준) 여부와 무관하게 항상 실행되는 기존 구조를 그대로 따르므로 조준 중이 아닐 때도(2차 충돌 지점이 존재해 고리가 보이는 동안은) 고리는 계속 회전한다. `colorGradient`는 정점의 월드 좌표가 아니라 정점 순서(인덱스)를 기준으로 알파값을 매기므로, 정점 각도가 매 프레임 회전해도 4개의 밝은 호는 형태를 유지한 채 고리 전체와 함께 계속 회전하는 것처럼 보인다.

### Inspector 조절 값

| 필드 | 설명 |
|------|------|
| `_lineWidth` | 궤적선 두께 |
| `_lineColor` | 궤적선 색상 |
| `_hitColor` | 레드닷 색상 |
| `_ringColor` | 원형 궤적선(고리) 색상 |
| `_dotRadius` | 레드닷 반경 |
| `_ringRadius` | 원형 궤적선(고리) 반경 |
| `_ringRotationSpeed` | 원형 궤적선(고리) 회전 속도 (deg/sec, 기본값 90) |

---

## 12. 리소스 참고 사항

- 필드에 떠 있는 아이템(다이아몬드 젬처럼 보이는 것)은 별도 전용 에셋이 아니라 볼 스프라이트 재사용으로 추정된다 (`Assets/_Project/Sprites/Ball/`에 6종 이미 존재).
- 스킬 카드의 "Best!" 추천 아이콘은 프로젝트에 관련 리소스가 없으며, 추가할 계획도 없으므로 이번 구현 범위에서 완전히 제외한다.
