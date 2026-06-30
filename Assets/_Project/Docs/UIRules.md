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
  ├─ TopBar   (스테이지명, 몬스터 HP바, %, 아이콘)
  ├─ TopButtons (▶ 속도, Auto, ⏸ 일시정지)
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
- HUD: `CharacterHpBar` (MonoBehaviour, Slider), `CharacterHP` 오브젝트에 부착

### 경험치 / 레벨

- `[SerializeField] private int[] _xpPerLevel` — 레벨별 필요 XP (Inspector 배열 설정)
- XP 획득 조건: 몬스터 처치 / 몬스터 통과 모두 `MonsterData.reward`만큼 획득
- XP 가득 차면 레벨업 → `OnLevelUp(int newLevel)` 이벤트 발행 → `LevelUpPanel` 오픈
- 이벤트: `public static event Action<int, int> OnXpChanged` (현재, 필요량)
- HUD: `CharacterXpBar` (MonoBehaviour, Slider + TMP_Text 레벨 표시), `CharacterXP` 오브젝트에 부착

### WaveManager 연동

- `WaveManager`에 `public static event Action<MonsterBase> OnMonsterReachedBottom` 추가 필요
- `CharacterManager`가 이 이벤트를 구독하여 HP/XP 처리
