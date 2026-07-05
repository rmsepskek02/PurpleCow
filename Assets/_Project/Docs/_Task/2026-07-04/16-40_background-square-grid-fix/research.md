# Research — 배경 격자 정사각형 보정

몬스터 시스템 개편(4종 랜덤 웨이브, 종류별 고정 블록 크기, 정사각형 그리드 기반 점유 체크)의 선행 작업으로, 현재 배경 이미지(`Background_1_Stage.png`) 안에 그려진 장식용 격자선이 정사각형이 아니라 가로로 길쭉한 직사각형으로 되어 있는 문제를 다룬다. 이 문서는 현재 코드/에셋 상태를 파악하고 원인을 분석하는 데 목적이 있으며, 해결 방안은 아직 확정하지 않는다. 특히 이번 작업이 `BackgroundFitter`/`WallFitter`가 담당하는 "기기별 여백 없는 화면 채우기(Stretch)"와 근본적으로 충돌할 수 있는 지점이 있어, 이를 명확히 짚는 것이 이 문서의 핵심 목적이다.

## 현재 상태

### 배경 텍스처 실측 결과

`Background_1_Stage.png`를 픽셀 단위로 직접 분석한 결과는 다음과 같다.

- 이미지 크기 2048×2048px, Sprite Pixels Per Unit(PPU) = 100
- 이미지 안에 그려진 장식용 격자선 간격: 세로선 약 140px, 가로선 약 85px → 9열 × 12행, 셀 크기 1.40 × 0.85 월드유닛
- 즉 **가로 셀이 세로 셀보다 약 1.65배 길다** — 정사각형이 아니다.
- 반면 몬스터/블록 스프라이트(`Fluffy`/`Spider`/`Block_1x1` 등)는 PPU 100 기준으로 약 0.96~0.99 월드유닛(1×1)의 정사각형 1유닛 체계로 제작되어 있다. `StoneBug`(1.84×1.10)는 `Block_2x1`(1.92×0.96)과, `ForestDeer`(0.96×1.98)는 `Block_1x2`(0.96×1.92)와 각각 유사한 비율이다.
- 원본 게임 실제 플레이 레퍼런스 스크린샷(`Assets/_Project/Docs/targetUI/KakaoTalk_20260703_115951058_01.jpg` 등, Stage 1 "깊은 숲")을 동일한 방식으로 실측하면 격자선 간격이 가로/세로 모두 약 97px로 **정사각형**임을 확인했다. 즉 원본 게임의 그리드는 정사각형이 맞고, 우리 프로젝트의 배경 에셋만 비정사각형 상태다.

### `BackgroundFitter.cs` — 배경을 화면에 맞추는 현재 로직

`Assets/_Project/Scripts/Core/BackgroundFitter.cs`는 카메라 뷰포트 크기(`orthographicSize * 2 * aspect`, `orthographicSize * 2`)를 배경 스프라이트 바운드 크기로 나눠 `scaleX`/`scaleY`를 **각각 독립적으로** 계산한 뒤 `transform.localScale`에 적용한다.

```csharp
transform.localScale = new Vector3(
    camSize.x / spriteSize.x * _zoomFactor,
    camSize.y / spriteSize.y * _zoomFactor,
    1f);
```

이 방식은 `ProjectHistory.md`(2026-07-03 "배경/해상도 대응")에 기록된 대로, Cover/Contain 방식을 실기기에서 시행착오 끝에 폐기하고 채택된 **Stretch 방식**이다. 다양한 Android 기기 종횡비에서 배경이 화면을 여백 없이 꽉 채우도록 하는 것이 목적이며, 그 대가로 가로/세로 배율이 기기마다 서로 다르게(독립적으로) 늘어나는 것을 전제로 한다.

### `WallFitter.cs` — 벽/발사 위치를 배경과 동일 배율로 연동하는 현재 로직

`Assets/_Project/Scripts/Core/WallFitter.cs`는 `Wall_Left`/`Wall_Right`/`Wall_Top`/`Ground`/`LaunchPoint`의 좌표를 "실측 기준 좌표(`_nativeLeftX = -6.5`, `_nativeRightX = 6.3`, `_nativeTopY = 6.0`, `_nativeBottomY = -6.5`, `_nativeLaunchPointY = -6.0`)" × "그 순간 배경과 동일한 scaleX/scaleY"로 계산해 배치한다. `scaleX`/`scaleY` 계산식은 `BackgroundFitter`와 완전히 동일하며(카메라 크기 / 스프라이트 크기 × `_zoomFactor`), `WallFitter` 안에서 자체적으로 다시 계산한다(두 스크립트가 배경 스프라이트 참조를 공유하지만 계산 로직 자체는 중복 구현되어 있음).

`_zoomFactor`(현재 두 스크립트 모두 1.3)는 반드시 같은 값을 써야 벽-배경 정렬이 유지된다고 이미 `ProjectHistory.md`에 문서화되어 있다.

### `SceneSetupEditor.cs`의 연결 부분

- `Step4_PlaceBackground()`(약 345~369행): `Background` 오브젝트 생성/스프라이트 로드 후 `ConnectBackgroundFitterRefs()`에서 `BackgroundFitter._spriteRenderer`/`_targetCamera`/`_zoomFactor(1.3)`를 연결한다.
- `Step5_PlaceWallsAndGround()`(약 388~394행): `Wall_Left`(-5.5, 0 / size 0.2×20), `Wall_Right`(5.5, 0 / size 0.2×20), `Ground`(0, -10 / size 12×0.2), `Wall_Top`(0, 8 / size 12×0.2) 4개 콜라이더 오브젝트를 생성한다. 이 좌표/크기는 `WallFitter`가 이후 실측 기준값으로 재배치하기 전의 초기 배치값이다.
- `Step6_SetupWallFitter()`(약 419~462행): `Main Camera`에 `WallFitter`를 붙이고 `_backgroundSpriteRenderer`, `_wallLeft`/`_wallRight`/`_wallTop`/`_ground`/`_launchPoint` 참조와 `_nativeLeftX(-6.5)`/`_nativeRightX(6.3)`/`_nativeTopY(6.0)`/`_nativeBottomY(-6.5)`/`_nativeLaunchPointY(-6.0)`/`_zoomFactor(1.3)` 값을 `SerializedObject`로 주입한다. 주석에 "WallFitter는 Step8에서 생성되는 LaunchPoint를 참조해야 하므로 Step8 이후에 실행한다"고 명시되어 있어, `Step6` 실제 실행 순서는 `Step8`(BallLauncher 연동, LaunchPoint 생성) 이후로 재배치되어 있다.

### `MonsterRules.md`의 선행 작업 언급

- 2장: "그리드는 **정사각형 셀을 전제**로 한다. 배경 이미지 비율 보정(`BackgroundFitter`/`WallFitter`)은 볼 충돌벽/캐릭터 위치 등 여러 시스템에 영향을 주는 위험도 높은 작업이라 별도의 선행 task로 진행할 예정이며, 이번 문서 갱신 범위에는 포함하지 않는다."
- 3장: 몬스터별 고정 블록 크기(`Fluffy`/`Spider` → `Block_1x1`, `StoneBug` → `Block_2x1`, `ForestDeer` → `Block_1x2`)와 점유 체크 로직이 "정사각형 1유닛" 체계를 전제로 설계되어 있다.
- 즉 이번 배경 격자 보정 작업은 이후 몬스터 스폰 그리드/점유 체크 구현이 참조할 "1유닛 = 정사각형" 전제를 배경 쪽에서도 성립시키기 위한 선행 작업으로 자리매김되어 있다.

## 관련 파일 및 의존성

| 파일 | 역할 | 이번 작업과의 관계 |
|---|---|---|
| `Assets/_Project/Scripts/Core/BackgroundFitter.cs` | 배경 스프라이트를 카메라 뷰포트에 맞춰 가로/세로 독립 Stretch | 격자 비율 보정 시 계산식 수정 후보 |
| `Assets/_Project/Scripts/Core/WallFitter.cs` | 벽/Ground/LaunchPoint를 배경과 동일 배율로 배치 | 배경 배율 계산식이 바뀌면 반드시 동일하게 반영해야 벽-격자 정렬 유지 |
| `Assets/_Project/Scripts/Editor/SceneSetupEditor.cs` | 씬에 Background/Wall/WallFitter 자동 배치 및 참조 연결 | `_nativeLeftX` 등 실측 기준 좌표값, `_zoomFactor` 값이 하드코딩되어 있어 배경 보정 시 함께 검토 필요 |
| `Assets/_Project/Sprites/Background/Background_1_Stage.png` | 배경 텍스처(정사각형이 아닌 격자 포함) | 보정 대상 원본 |
| `Assets/_Project/Docs/ProjectHistory.md` (2026-07-03 섹션) | Stretch 방식 채택 배경, `WallFitter` 실측 기준값 확정 과정 기록 | 이번 트레이드오프 판단의 전제 근거 |
| `Assets/_Project/Docs/MonsterRules.md` (2장, 3장) | 정사각형 그리드 전제, 선행 작업 언급 | 이번 작업의 목적/후속 작업(몬스터 그리드) 연결점 |

## 문제점 / 구현 대상 파악

### 핵심 쟁점 — "격자를 정사각형으로 보이게" vs "기기별 Stretch로 여백 없이 꽉 채우기"는 서로 다른 축의 요구사항

`BackgroundFitter`가 기기 종횡비에 따라 매번 `scaleX`/`scaleY`를 독립적으로 다르게 계산하는 한, Stretch 방식 자체가 "기기마다 가로/세로 배율이 달라도 된다(오히려 달라야 여백 없이 꽉 찬다)"는 것을 전제로 하고 있다. 따라서 배경 텍스처 안의 특정 사각형 영역(격자 셀)이 **모든 기기에서 항상 정사각형으로 보인다**는 것은 수학적으로 보장되기 어렵다. 특정 기준 종횡비 1개에서만 셀이 정사각형이 되고, 그 기준에서 벗어난 기기에서는 다시 비정사각형으로 보이게 된다.

이 프로젝트가 실제로 원하는 것이 다음 중 어느 쪽인지 구분이 필요하다.

1. **텍스처 자체가 갖고 있는 고유한 비정사각형 셀(140×85)을 정사각형 비율로 보정**하는 것 — 이는 기기와 무관하게 텍스처 자체의 문제이므로, 코드에서 `BackgroundFitter`/`WallFitter`의 계산식에 "텍스처 자체의 셀 종횡비 보정 계수"(예: 가로 방향에 85/140 ≈ 0.607을 추가로 곱하는 방식)를 넣어 해결 가능하다. 이 경우 기기별 Stretch 전제(여백 없이 화면 채우기)는 그대로 유지된다.
2. **모든 기기에서 화면에 실제로 보이는 최종 렌더링 결과가 정사각형**임을 보장하는 것 — 이는 Stretch 방식의 "기기별 독립 배율" 전제와 근본적으로 충돌한다. `scaleX`와 `scaleY`가 기기 종횡비에 따라 서로 다른 비율로 늘어나는 한, 텍스처 안의 셀 비율을 아무리 보정해도 특정 기준 기기를 벗어나면 다시 셀이 비정사각형으로 보일 수 있다.

1번은 텍스처 자체의 문제를 코드로 보정하는 것이라 기존 Stretch 정책과 공존 가능하지만, 2번을 완전히 만족하려면 Stretch 방식 자체를 재검토(예: 특정 기준 종횡비를 기준으로 Cover/Contain을 적용하고 그 외 기기는 약간의 여백/잘림을 허용하는 방식 등)해야 할 가능성이 있다. 이는 `ProjectHistory.md`에 기록된 대로 이미 Cover/Contain을 실기기 테스트 끝에 폐기하고 Stretch로 확정한 과거 결정과 다시 맞닿는 지점이라, 신중한 판단이 필요하다.

### 파생 쟁점 — "완벽한 픽셀 정합"이 필수인지, "육안으로 자연스러운 정도"면 충분한지

`ProjectHistory.md`(2026-07-03 "배경/해상도 대응")에는 이미 "Wall 좌표가 배경 이미지 속 격자 그림 경계와 애초에 일치하지 않는다"는 사실이 실측으로 드러났고, 이를 완전히 일치시키기보다 `WallFitter`의 `_nativeLeftX`/`_nativeRightX`/`_nativeTopY`/`_nativeBottomY`/`_nativeLaunchPointY` 값을 실기기 테스트를 반복하며 육안으로 자연스러운 수준까지만 조정해 확정한 선례가 있다(좌우 비대칭 약 2.5%도 실측 기반으로 그대로 유지하기로 한 결정 포함).

몬스터 스폰에 쓰이는 "논리적 그리드"(`WaveManager`가 스폰 위치 계산에 사용할 그리드, `MonsterRules.md` 6장)는 배경 텍스처의 시각적 격자와 픽셀 단위로 완전히 일치할 필요는 없다는 것이 기존 선례다. 즉 이번 작업의 목표 수준을 다음 중 어느 쪽으로 잡을지도 함께 판단이 필요하다.

- (A) 배경 텍스처의 격자선이 실제로 정사각형 비율(140×140 또는 실측 97px 비율에 맞춘 값)로 렌더링되도록 픽셀 단위로 정밀 보정
- (B) 육안으로 자연스럽게 정사각형처럼 보이는 수준으로만 근사 보정하고, 몬스터 스폰 그리드는 그와 별개로 "1유닛 정사각형"이라는 논리적 전제만 지키면 충분

### 영향 범위

- `BackgroundFitter`/`WallFitter` 계산식을 수정하면 배경 스케일뿐 아니라 벽(`Wall_Left`/`Wall_Right`/`Wall_Top`/`Ground`)과 볼 발사 지점(`LaunchPoint`)의 실제 월드 좌표가 함께 바뀐다. 두 스크립트가 같은 `_zoomFactor`/스케일 계산식을 공유해야 정렬이 유지된다는 기존 제약이 이번 수정에도 그대로 적용된다.
- `SceneSetupEditor.cs`에 하드코딩된 `_nativeLeftX` 등 실측 기준값과 `Wall_Left`/`Wall_Right`/`Wall_Top`/`Ground`의 초기 배치 좌표(Step5)도 계산식 변경 방향에 따라 재검토가 필요할 수 있다.
- 실제 반영을 위해서는 `ProjectHistory.md`에 기록된 전례와 마찬가지로, 로컬 Unity 에디터에서 `PurpleCow/Setup/Scene Setup` 재실행 및 실기기 테스트를 통한 반복 조정이 필요할 가능성이 높다.

## 결론

배경 텍스처(`Background_1_Stage.png`)의 장식용 격자는 세로 140px × 가로 85px(비정사각형, 약 1.65:1)로 그려져 있는 반면, 몬스터/블록 스프라이트는 정사각형 1유닛 체계로 제작되어 있고 원본 게임의 실제 격자도 정사각형(약 97×97px)임을 실측으로 확인했다. 이 불일치를 보정하는 것이 이번 작업의 출발점이다.

다만 현재 `BackgroundFitter`가 채택한 Stretch 방식(기기 종횡비별 가로/세로 독립 배율)은 "텍스처 자체의 셀 종횡비를 정사각형으로 보정"하는 것과는 공존 가능하지만, "모든 기기에서 최종 렌더링 결과가 항상 정사각형"임을 보장하는 것과는 근본적으로 충돌할 수 있다. 또한 몬스터 스폰용 논리적 그리드가 배경 텍스처와 픽셀 단위로 완전히 일치할 필요는 없다는 기존 선례도 있어, 이번 보정의 목표 수준(정밀 보정 vs 육안 근사)에 따라 구현 난이도와 리스크가 달라진다.

이 두 가지 트레이드오프(텍스처 보정 vs 최종 렌더링 정사각형 보장, 정밀 보정 vs 육안 근사)에 대한 사용자의 선택에 따라 plan.md의 구현 방향이 갈라질 것으로 판단되며, 이번 research.md에서는 결론을 확정하지 않고 위 쟁점을 공유하는 데 그친다.
