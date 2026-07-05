# Plan — 캐릭터 스프라이트 프리팹화 + 조준 방향 연동 회전

research.md에서 확정한 사실관계(씬/코드 어디에도 캐릭터 시각 오브젝트가 없다는 것, 4개 스프라이트가 임포트 기본값 상태라는 것, `BallLauncher.LaunchDirection`을 `TrajectoryPreview`처럼 폴링하는 패턴이 이미 존재한다는 것)를 바탕으로, 오케스트레이터가 사용자와 논의해 확정한 4가지 설계 방향(배치 위치, 무기 회전 피벗 해결 방식, 계산 위치, 신규 스크립트 경로)을 구체적인 프리팹 계층·스크립트 구조·에디터 자동화 절차로 구체화한 문서다. 이 문서는 계획 단계이며, TaskRules.md 규칙에 따라 사용자의 명시적 승인 전까지 실제 `.cs`/`.prefab`/`.unity` 파일 수정은 진행하지 않는다.

## 구현 목표

- `Character_Main_body/head/weapon.png` 3파츠(+참고용 합성본)를 조립해 `Character.prefab`을 신규 생성하고, `BallLauncher`의 `LaunchPoint`(WallFitter가 화면비에 따라 Y좌표를 동적 재계산하는 지점) 자식으로 배치해 별도 리프레임 코드 없이 반응형 위치를 물려받게 한다.
- 무기 스프라이트(`Character_main_weapon.png`)의 회전축을 스프라이트 임포트 설정(피벗) 변경 없이, 빈 부모 `WeaponPivot` GameObject로 손/어깨 위치에 대체 배치한다.
- 조준 방향(`BallLauncher.Instance.LaunchDirection`)에 따라 몸통/머리를 좌우 반전하고, 무기(및 보조적으로 머리 일부)를 회전시키는 뷰 전용 신규 스크립트 `CharacterAimView.cs`를 `Scripts/Character/` 폴더에 작성한다. `CharacterManager.cs`(HP/XP 로직 전용, `UIRules.md` 섹션 10)에는 이 로직을 섞지 않는다.
- 위 조립·배치를 자동화하는 신규 에디터 스크립트 `CharacterSetupEditor.cs`를 작성한다. 기존 `SceneSetupEditor.cs`/`BallSetupEditor.cs`/`MonsterSetupEditor.cs` 등은 일절 수정하지 않는다.

## 단계별 작업 계획

### 1단계 — `Character.prefab` 계층 구조 설계

```
Character (root, empty GameObject)
 ├─ CharacterAimView (컴포넌트)
 ├─ Body       (SpriteRenderer: Character_Main_body.png)
 ├─ Head       (SpriteRenderer: Character_Main_head.png)
 └─ WeaponPivot (empty, 손/어깨 위치로 오프셋)
      └─ Weapon (SpriteRenderer: Character_main_weapon.png, WeaponPivot 기준 오프셋)
```

- **회전 피벗 문제 해결**: `Character_main_weapon.png`는 `.meta`상 `alignment: 0`, `pivot: {0,0}`(=사실상 스프라이트 중앙 고정, 커스텀 없음)이라 스프라이트 자체 회전축이 손 위치가 아니다. `WeaponPivot`을 손/어깨 위치에 두고 `Weapon`은 그 자식으로 오프셋을 줘서, 회전은 항상 `WeaponPivot.localRotation`(z축)만 조작하는 방식으로 처리한다. 스프라이트 파일·`.meta`는 건드리지 않는다. 이 패턴은 `TrajectoryPreview.cs`가 원(레드닷/고리)을 자식 `LineRenderer`로 구성하는 기존 방식과 위상이 같다.
- **초기 배치 수치(제안값, 로컬 검증 필수)**: 4개 스프라이트 `.meta`를 직접 읽어 확인한 결과 `Character_Main_body/head/weapon.png`는 각각 완전히 별도의 PNG 캔버스이고 스프라이트 rect가 전부 `x=0, y=0`으로 시작한다(참고용 합성본 `Character_Main.png`만 `x=23, y=2`인 것과 달리, 파츠 3개는 서로 어떤 좌표계로 겹쳐지는지 알려주는 공유 오프셋 정보가 메타데이터에 전혀 없다). 즉 파츠 간 정확한 상대 위치는 계산으로 도출할 수 없고, 아래는 픽셀 크기(몸통 48x48, 머리 131x92, 무기 59x116, `spritePixelsToUnits: 100`)만 근거로 삼은 대략적인 시작값이다.
  - `Body`: `localPosition (0, 0, 0)`, `localScale (1,1,1)` → 캐릭터 전체의 앵커 기준점.
  - `Head`: `localPosition` 대략 `(0, 0.4, 0)` 부근(몸통 위쪽에 걸치도록) — 머리 스프라이트(1.31 x 0.92유닛)가 몸통(0.48 x 0.48유닛) 대비 매우 크므로, 합성본(`Character_Main.png`)과 육안 비교하며 위치/스케일 조정이 사실상 필수.
  - `WeaponPivot`: `localPosition` 대략 `(0.2, 0.15, 0)`(몸통 오른쪽 어깨 높이 부근으로 추정), `localRotation (0,0,0)`.
  - `Weapon`(WeaponPivot 자식): `localPosition` 대략 `(0, 0.4, 0)`(무기 손잡이가 피벗에 오도록, 무기 스프라이트 세로 길이 1.16유닛의 절반 부근 오프셋).
  - 위 수치는 전부 초기 추정치이며, "주의사항" 섹션에서 다시 강조하듯 원격 환경에는 Unity 에디터가 없어 실제 조립 결과를 육안으로 검증할 수 없다. 사용자가 로컬 Unity에서 `Character_Main.png`(합성 완성본)를 참고 삼아 미세 조정하는 과정이 필요하다.
- **레이어링(그리기 순서) 제안**: `Body.sortingOrder = 0`, `Weapon.sortingOrder = 1`, `Head.sortingOrder = 2`(무기가 몸통보다 앞, 머리가 가장 앞) — 다만 이 역시 로컬 육안 확인 후 뒤바뀔 수 있다.

### 2단계 — `CharacterAimView.cs` 신규 스크립트 작성

- 경로: `Assets/_Project/Scripts/Character/CharacterAimView.cs` (신규 폴더 `Scripts/Character/` 생성 — 기존 `Scripts/Ball/`, `Scripts/Monster/`, `Scripts/Skill/`, `Scripts/UI/`, `Scripts/Wave/`의 도메인별 폴더 관례를 따름). `CharacterManager.cs`는 `Scripts/Core/`에 그대로 두고 옮기지 않는다(관련 없는 파일 이동 금지 원칙).
- `[SerializeField]` 참조 필드: `_bodySpriteRenderer`, `_headSpriteRenderer`, `_headTransform`, `_weaponPivot`(Transform), `_headRotationRatio`(float, Inspector 조절, 기본값 예: `0.25f`).
- `Update()`에서 `TrajectoryPreview.cs`와 동일한 폴링 패턴으로 `BallLauncher.Instance.LaunchDirection`을 매 프레임 읽는다(이벤트 구독 아님 — `InputHandler.OnDrag`는 터치가 없을 때 발행되지 않아 회전이 멈추는 문제가 있기 때문).
- 계산 로직(개념적 의사코드, 최종 수식은 로컬 시각 검증 후 조정 가능):
  ```csharp
  Vector2 dir = BallLauncher.Instance.LaunchDirection;
  if (dir.sqrMagnitude < 0.0001f) return; // TrajectoryPreview와 동일한 가드

  bool facingLeft = dir.x < 0f;
  _bodySpriteRenderer.flipX = facingLeft;
  _headSpriteRenderer.flipX = facingLeft;

  // WeaponPivot 위치도 반전 방향(어깨 쪽)으로 따라가야 하므로 x부호만 뒤집는다.
  Vector3 pivotPos = _weaponPivotBaseLocalPosition; // Awake에서 캐시
  pivotPos.x = facingLeft ? -Mathf.Abs(pivotPos.x) : Mathf.Abs(pivotPos.x);
  _weaponPivot.localPosition = pivotPos;

  // 반전 시 로컬 좌표계 자체는 안 뒤집히므로(SpriteRenderer.flipX만 사용), 각도 계산 시
  // x축 기준으로 방향을 미러링해 보정한다.
  Vector2 effectiveDir = facingLeft ? new Vector2(-dir.x, dir.y) : dir;
  float weaponAngle = Mathf.Atan2(effectiveDir.y, effectiveDir.x) * Mathf.Rad2Deg - 90f; // 무기 기본 자세(수직)를 0도로 보정
  _weaponPivot.localRotation = Quaternion.Euler(0f, 0f, weaponAngle);

  // 머리는 무기 회전각의 일부 비율만 보조적으로 반영("방향을 대략 암시"하는 용도)
  _headTransform.localRotation = Quaternion.Euler(0f, 0f, weaponAngle * _headRotationRatio);
  ```
  - `SpriteRenderer.flipX`만 사용하고 `Body`/`Head`/루트의 `localScale.x` 부호는 건드리지 않는다(스케일 반전 시 자식 Transform 좌표계까지 함께 뒤집혀 `WeaponPivot` 회전 계산이 더 복잡해지므로, `flipX` + 위치/각도 수동 보정 조합이 더 단순하다).
  - `_weaponPivotBaseLocalPosition`은 `Awake()`에서 최초 1회 캐시한다.
  - "무기는 방향을 대략 암시하는 용도"라는 논의 결과에 따라 완벽한 물리적 정확성(예: 실제 손 관절 IK)은 요구하지 않는다.

### 3단계 — `CharacterSetupEditor.cs` 신규 에디터 스크립트 작성

- 경로: `Assets/_Project/Scripts/Editor/CharacterSetupEditor.cs` (신규 파일). **기존 `SceneSetupEditor.cs`, `BallSetupEditor.cs`, `MonsterSetupEditor.cs` 등은 어떤 라인도 수정하지 않는다.**
- 메뉴 아이템: `[MenuItem("PurpleCow/Setup/Character System Setup")]` — 기존 관례(`Ball System Setup`, `Monster System Setup`, `Skill System Setup`, `Scene Setup` 등)와 동일한 네이밍 패턴.
- 처리 절차(기존 `SceneSetupEditor`의 `Step1_CreateBallPrefab()`/`PlaceColliderObject()`/`Step8_ConnectBallLauncherRefs()` 패턴을 참고):
  1. `Assets/_Project/Prefabs/Character` 폴더가 없으면 생성(`AssetDatabase.IsValidFolder`/`CreateFolder`).
  2. `Character.prefab`이 이미 존재하면 로그만 남기고 스킵(멱등성 유지, 기존 `Ball.prefab`/몬스터 프리팹 생성 로직과 동일한 방식).
  3. 존재하지 않으면 1단계 계층대로 GameObject를 코드로 구성한다: `Body`/`Head`에 `SpriteRenderer` 부착 후 `Assets/_Project/Sprites/Character/Character_Main_body.png`, `Character_Main_head.png`를 `AssetDatabase.LoadAssetAtPath<Sprite>()`로 로드해 연결(스프라이트를 못 찾으면 `Debug.LogWarning`, 기존 관례와 동일). `WeaponPivot` → `Weapon` 자식 생성 후 `Character_main_weapon.png` 연결.
  4. 루트에 `CharacterAimView` 컴포넌트를 부착하고, `SerializedObject`로 `_bodySpriteRenderer`/`_headSpriteRenderer`/`_headTransform`/`_weaponPivot` 참조를 자동 연결한다(기존 `Step6_SetupWallFitter()`, `Step9_ConnectBallPrefabRefs()`와 동일한 `SerializedObject.FindProperty(...).objectReferenceValue = ...` 패턴).
  5. `PrefabUtility.SaveAsPrefabAsset()`으로 `Assets/_Project/Prefabs/Character/Character.prefab` 저장 후 씬의 임시 GameObject는 `DestroyImmediate`.
  6. 씬에 `LaunchPoint` GameObject(`GameObject.Find("LaunchPoint")`)를 찾아, 그 자식으로 `Character.prefab` 인스턴스를 배치한다(`PrefabUtility.InstantiatePrefab()` + `SetParent(launchPoint, false)` + `localPosition = Vector3.zero`). 이미 `LaunchPoint` 밑에 `Character`라는 자식이 있으면 스킵(멱등성).
  7. 마지막에 `AssetDatabase.SaveAssets()`, `AssetDatabase.Refresh()`, `EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())` 호출(`SceneSetupEditor.SetupScene()`과 동일한 마무리 패턴).
- `LaunchPoint` 자체의 역할(볼 발사/귀환 좌표, `BallLauncher._launchPoint`, `WallFitter._launchPoint` 참조)은 이 에디터 스크립트가 전혀 건드리지 않는다 — `Character`는 어디까지나 `LaunchPoint`의 자식으로 "얹히는" 것뿐이다.

## 예상 변경/생성 파일 목록

| 파일 경로 | 변경 내용 |
|---|---|
| `Assets/_Project/Scripts/Character/CharacterAimView.cs` | 신규 생성. 조준 방향(`BallLauncher.Instance.LaunchDirection`) 매 프레임 폴링 → 몸통/머리 좌우 반전(`flipX`) + `WeaponPivot` 회전 + 머리 보조 회전 |
| `Assets/_Project/Scripts/Editor/CharacterSetupEditor.cs` | 신규 생성. `Character.prefab` 조립(파츠 계층·`WeaponPivot`·`CharacterAimView` 참조 연결) + `LaunchPoint` 자식으로 씬 인스턴스화. 메뉴: `PurpleCow/Setup/Character System Setup`. 기존 에디터 스크립트는 미수정 |
| `Assets/_Project/Prefabs/Character/Character.prefab` | 신규 생성 예정 — `CharacterSetupEditor` 메뉴를 **로컬 Unity 에디터에서 실행**해야 만들어짐. 이 원격 환경에는 Unity 에디터가 없어 직접 생성 불가 |
| `Assets/Scenes/SampleScene.unity` | `LaunchPoint` 자식으로 `Character` 인스턴스 추가 — 마찬가지로 로컬에서 메뉴 실행 필요 |

## 주의사항

- **plan.md 작성만 완료된 상태**: 사용자의 명시적 승인 전까지 위 `.cs`/`.prefab`/`.unity` 실제 수정은 진행하지 않는다.
- **기존 에디터 스크립트 절대 미수정**: `SceneSetupEditor.cs`, `BallSetupEditor.cs`, `MonsterSetupEditor.cs` 등 기존 파일은 이번 작업에서 어떤 라인도 건드리지 않는다. `CharacterSetupEditor.cs`는 반드시 새 파일로 작성한다.
- **원격 환경 한계**: 이 원격 환경에는 Unity 에디터가 없어 `Character.prefab` 실제 생성 결과나 `SampleScene.unity`에 배치된 인스턴스를 직접 눈으로 검증할 수 없다. `.cs` 스크립트는 텍스트 파일이라 이 환경에서도 작성 가능하지만, 프리팹/씬에 실제 GameObject 계층을 만드는 것은 `BallSetupEditor`/`MonsterOverhaulSetupEditor`와 동일하게 "에디터 스크립트를 작성하고 사용자가 로컬 Unity에서 메뉴를 실행"하는 방식으로 처리한다.
- **초기 배치 수치는 반드시 로컬 미세 조정 필요**: 1단계에서 제안한 `WeaponPivot` 오프셋 좌표(손/어깨 위치), 머리 보조 회전 비율(`_headRotationRatio`), 파츠별 `localPosition`/`sortingOrder`는 스프라이트 픽셀 크기만 근거로 한 초기값이다. `Character_Main_body/head/weapon.png` 각각이 완전히 독립된 PNG 캔버스이고(`rect x=0, y=0`으로 서로 겹쳐지는 공유 좌표계 정보가 `.meta`에 없음), 참고용 합성본 `Character_Main.png`만 별도 오프셋(`x=23, y=2`)을 갖고 있어 파츠 간 정확한 상대 위치를 코드/계산만으로 도출할 수 없다. 사용자가 로컬 Unity에서 `Character_Main.png`를 육안 참고 삼아 조립 결과를 직접 확인하고 조정하는 과정이 사실상 필수다.
- **좌우 반전 방식 고정**: 반전은 `SpriteRenderer.flipX`로만 처리하고 `localScale.x` 부호 반전은 사용하지 않는다(자식 Transform 좌표계까지 함께 뒤집혀 `WeaponPivot` 회전/위치 보정이 더 복잡해지기 때문). 대신 `WeaponPivot`의 로컬 위치 x부호와 무기 회전각 계산에서 방향 벡터를 미러링하는 방식으로 별도 보정한다.
- **`LaunchPoint`의 기존 역할 보존**: `LaunchPoint`는 여전히 볼 발사/귀환 좌표 전용 기준점이며, `BallLauncher`/`WallFitter`의 참조나 로직은 이번 작업에서 변경하지 않는다. `Character`는 그 자식으로 추가될 뿐이다.
- **`CharacterManager.cs`와의 책임 분리 유지**: HP/XP 로직(`UIRules.md` 섹션 10)과 시각적 회전/반전 로직(`CharacterAimView.cs`)은 완전히 분리된 별도 컴포넌트로 유지하며, 서로의 파일을 수정하지 않는다.
- **이번 작업 범위 제외**: 완벽한 물리적 정확성(실제 손 관절 IK, 무기 궤적 물리 시뮬레이션 등)은 요구하지 않는다("무기는 방향을 대략 암시하는 용도"). `UIRules.md`에 캐릭터 시각 규칙 섹션을 신설할지 여부는 이번 plan.md 범위에서 결정하지 않으며, 구현 완료 후 별도로 검토한다.
