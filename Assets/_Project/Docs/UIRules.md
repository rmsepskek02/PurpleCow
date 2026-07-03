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
  ├─ BallLevelUpPanel
  └─ PrismPanel

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

**방식**: 각 몬스터 프리팹에 World Space Canvas + Slider 자식으로 부착

- `MonsterHpBar` (MonoBehaviour): 자신의 부모 `MonsterBase`의 `OnHpChanged(float current, float max)` 이벤트 구독
- `MonsterBase`에 `public event Action<float, float> OnHpChanged` 추가 필요 — TakeDamage/Die 시 발행
- HP 0이 되면 HP바 오브젝트는 풀 반납 시 자동 비활성화
- Canvas 설정: Render Mode = World Space, Sorting Layer = UI

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

- 조준 중(터치 시작~릴리즈)에만 표시되고, 조준하지 않을 때는 숨겨진다.
- 궤적선은 점선(dashed line) 형태이며, 1차 충돌 지점까지 + 반사 후 2차 충돌 지점까지 총 2단계 선분으로 그려진다. 3차 충돌 이후는 표시하지 않는다.
- 2차 충돌 지점에는 빨간 점(레드닷)과 그 점을 감싸는 원형 궤적선(고리)이 표시된다.
- 충돌 판정은 `Physics2D.RaycastAll` + `Wall`/`Ground`/`Monster` 태그 화이트리스트 필터링으로 처리한다. 태그가 없는 다른 볼 오브젝트는 자동으로 제외된다.

### 구현 방식

- `LineRenderer` 기반으로 그린다. 별도 스프라이트 에셋은 사용하지 않는다.
- 점선은 런타임 생성한 텍스처(4x1)와 `LineRenderer.textureMode = Tile` 조합으로 구현한다.
- 레드닷/원형 궤적선도 `LineRenderer`로 그린 원형 점열(정다각형 근사)로 구현한다.

### Inspector 조절 값

| 필드 | 설명 |
|------|------|
| `_lineWidth` | 궤적선 두께 |
| `_lineColor` | 궤적선 색상 |
| `_hitColor` | 레드닷/원형 궤적선(히트 마커) 색상 |
| `_dotRadius` | 레드닷 반경 |
| `_ringRadius` | 원형 궤적선(고리) 반경 |

---

## 12. 리소스 참고 사항

- 필드에 떠 있는 아이템(다이아몬드 젬처럼 보이는 것)은 별도 전용 에셋이 아니라 볼 스프라이트 재사용으로 추정된다 (`Assets/_Project/Sprites/Ball/`에 6종 이미 존재).
- 스킬 카드의 "Best!" 추천 아이콘은 프로젝트에 관련 리소스가 없으며, 추가할 계획도 없으므로 이번 구현 범위에서 완전히 제외한다.
