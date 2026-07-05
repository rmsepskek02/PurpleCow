# Research — 몬스터 시스템 개편

이 문서는 `MonsterRules.md`에 이미 확정된 새 규칙(전종류 랜덤 웨이브 구성, 런타임 랜덤 스폰 + 점유 체크, 몬스터별 고정 블록 크기, 블록 앞면 임베드 HP바)을 기준으로, 현재 코드(`MonsterData`/`MonsterBase`/`WaveTableData`/`WaveManager`/`MonsterSetupEditor`)와 프리팹 구조가 각각 어떤 지점에서 얼마나 벗어나 있는지 파일 단위로 상세 분석한다. 배경 격자 정사각형 보정(`BackgroundFitter`/`WallFitter`)은 선행 작업으로 이미 완료되어 이번 그리드 배치 로직이 참조할 수 있는 상태다. 해결책/구현 방법은 확정하지 않으며, 설계 선택지 나열까지만 다루고 결정은 이후 plan.md에서 진행한다.

## 현재 상태

### 1. 프리팹 구조 — 블록+캐릭터 합성 프리팹이 존재하지 않음

`Assets/_Project/Prefabs/Monster/` 폴더를 직접 열어 확인한 결과, 현재 두 계열의 프리팹이 완전히 분리되어 있다.

- **캐릭터 프리팹 4종** (`Fluffy.prefab`/`Spider.prefab`/`StoneBug.prefab`/`ForestDeer.prefab`): 루트에 캐릭터 스프라이트 `SpriteRenderer` + `Rigidbody2D` + `BoxCollider2D` + `MonsterBase`(`_monsterData` 연결됨) + tag `"Monster"`. 자식으로 `HpBarCanvas`(World Space Canvas + `UnityEngine.UI.Slider` + `MonsterHpBar`, 몬스터 머리 위 오프셋)가 붙어 있다. 블록 베이스는 전혀 없다.
- **블록 스텁 프리팹 4종** (`Block_1x1.prefab`/`Block_1x2.prefab`/`Block_2x1.prefab`/`Block_2x2.prefab`): `SpriteRenderer`(블록 스프라이트) + `BoxCollider2D` + `MonsterBase`(`_monsterData` **미연결**, `Rigidbody2D` 없음) + tag `"Monster"`만 있는 미완성 스텁. `MonsterSetupEditor.cs` 어디에서도 참조되지 않는 죽은 에셋이다.

즉 "블록 위에 캐릭터가 서 있는" 하나의 프리팹은 지금 존재하지 않으며, `MonsterRules.md` 3장이 요구하는 "블록(베이스)+캐릭터 스프라이트가 합쳐진 하나의 프리팹" 구조를 이번 작업에서 새로 합성해야 한다.

### 2. 스프라이트 픽셀 실측 (PPU 100 기준, Pillow로 실측 완료)

| 스프라이트 | 픽셀 크기 | 월드유닛 | 비고 |
|---|---|---|---|
| `Fluffy.png` | 95×99 | ~0.95×0.99 | 정사각 1칸 |
| `Spider.png` | 94×92 | ~0.94×0.92 | 정사각 1칸 |
| `StoneBug.png` | 184×110 | ~1.84×1.10 | 가로로 김 |
| `ForestDeer.png` | 96×198 | ~0.96×1.98 | 세로로 김 |
| `Block_1x1.png` | 96×96 | 0.96×0.96 | |
| `Block_1x2.png` | 96×192 | 0.96×1.92 | |
| `Block_2x1.png` | 192×96 | 1.92×0.96 | |
| `Block_2x2.png` | 192×187 | 1.92×1.87 | 이번 4종 매칭엔 미사용 |

`MonsterRules.md` 3장이 확정한 매핑: Fluffy→`Block_1x1`, Spider→`Block_1x1`, StoneBug→`Block_2x1`, ForestDeer→`Block_1x2`. `Block_2x2`는 사용하지 않는다.

### 3. `MonsterData.cs` — 현재 필드

```
_hp (float), _moveSpeed (float), _damage (int), _reward (int)
```

블록 크기(footprint)를 나타내는 필드가 전혀 없다. `MonsterSetupEditor.CreateMonsterDataAssets()`가 4종 모두 동일한 기본값(Hp 30 / MoveSpeed 1 / Damage 1 / Reward 10)으로 에셋(`MonsterData_Fluffy/Spider/StoneBug/ForestDeer.asset`, 이미 생성되어 `Assets/_Project/Data/`에 존재 확인됨)을 생성하며, 2칸 몬스터(StoneBug/ForestDeer) 상향 차등은 반영되어 있지 않다.

### 4. `MonsterBase.cs` — 현재 동작

- `OnSpawn()`/`ApplyData()`/`TakeDamage()`/`Die()`가 HP 초기화·차감·사망 처리를 담당하고, `OnHpChanged(float current, float max)` 이벤트를 발행한다. 이 이벤트 발행 시점과 시그니처는 `UIRules.md` 9장이 "재사용 가능"으로 명시한 부분과 일치하며 이번 개편에서 변경이 필요 없어 보인다.
- `Update()`가 매 프레임 `MonsterData.MoveSpeed` 기반 `Vector3.down` 이동, 냉동/슬로우 처리를 수행한다. 이 로직도 전진 방식 자체는 `MonsterRules.md` 2장(연속 하강)과 일치하여 이번 개편 범위 밖으로 보인다.
- 블록 크기(footprint)나 콜라이더 크기 관련 로직은 `MonsterBase`에 전혀 없다 — 콜라이더는 프리팹의 `BoxCollider2D` 컴포넌트 값(Inspector 고정값)으로만 결정되는 구조다.

### 5. 콜라이더 — 현재 캐릭터 스프라이트 크기 기준

캐릭터 프리팹의 `BoxCollider2D`는 캐릭터 스프라이트 크기에 맞춰져 있다(예: `Fluffy.prefab`은 약 0.95×0.99). 2칸 몬스터의 경우 블록 전체 크기(예: StoneBug는 1.92×0.96, ForestDeer는 0.96×1.92)로 콜라이더를 확장해야 한다는 것이 `MonsterRules.md` 3장의 확정 규칙이지만, 현재 캐릭터 프리팹의 콜라이더는 그보다 작은 캐릭터 스프라이트 크기에 머물러 있다.

### 6. HP바 — 현재 머리 위 고정 배치

`HpBarCanvas`(World Space Canvas + `Slider`)가 캐릭터 루트의 자식으로, 로컬 오프셋 `y=0.6`(머리 위)에 고정 배치되어 있고 `RectTransform.sizeDelta`도 `{1, 0.15}`로 4종 모두 동일한 고정값이다. `MonsterRules.md` 7장 / `UIRules.md` 9장이 확정한 "블록 앞면(정면 하단) 임베드 + 블록 가로폭 비례 길이" 방식과는 부착 위치(부모: 캐릭터 루트 → 블록으로 변경 필요)와 크기(고정값 → 블록별 가변값)가 모두 어긋난다. 다만 `MonsterHpBar` 스크립트 자체와 `OnHpChanged` 구독 구조는 `UIRules.md` 9장이 "재사용 가능"으로 명시했으므로 이번 개편에서 변경 대상이 아니다.

### 7. `WaveTableData.cs` — 현재 데이터 구조

```csharp
public class WaveEntry {
    public int WaveNumber;
    public List<MonsterSpawnEntry> SpawnEntries;
}
public class MonsterSpawnEntry {
    public MonsterData Data;
    public Vector2Int GridPosition;
}
```

`MonsterSpawnEntry`가 몬스터 종류(`Data`)뿐 아니라 정확한 스폰 좌표(`GridPosition`)까지 데이터로 직접 들고 있다. `MonsterRules.md` 6장이 확정한 새 구조("웨이브당 스폰 수 + 몬스터 종류별 등장 가중치 같은 구성 파라미터만 저장, 좌표는 런타임에 매번 새로 계산")와 근본적으로 다르다.

`Assets/_Project/Data/WaveTableData.asset`이 이미 존재하며, 20웨이브 분량 `SpawnEntries`(좌표 포함)가 이미 구워져 있는 상태로 추정된다(`MonsterSetupEditor.SetupWaveSpawnEntries()`가 생성한 값). 이 asset은 Unity 에디터 GUI로만 내용 확인/수정이 가능하며, 이번 원격 환경(Read/Grep 기반 텍스트 분석)에서는 `.asset` YAML을 직접 열람하면 대략적인 값 확인은 가능하지만 구조 변경 후 재직렬화·재생성은 에디터 실행이 필요하다.

### 8. `WaveManager.cs` — 현재 스폰 흐름

`SpawnWave(int index)`가 하는 일:

1. `waveEntry.SpawnEntries`를 순회하며 풀에서 `MonsterBase`를 꺼내 `entry.Data`로 `ApplyData()` 호출.
2. `worldPosition = _spawnRoot.position + new Vector3(entry.GridPosition.x * _gridCellSize, entry.GridPosition.y * _gridCellSize, 0)`로 위치를 계산해 그대로 대입.

즉 현재는 완전히 결정론적(같은 웨이브 = 항상 같은 좌표) 배치이며, `MonsterRules.md` 2장/6장이 확정한 "웨이브 시작마다 런타임 랜덤 계산 + 점유 체크"와 배치되는 구조다. 점유 체크(occupancy check)에 필요한 자료구조(어떤 셀이 이미 점유되어 있는지 추적하는 그리드 맵 등)도 현재 코드에 전혀 없다.

`_gridCellSize`는 `[SerializeField] float = 1.0f` 단일 필드로, 그리드 열/행 개수나 영역 경계에 대한 정보는 `WaveManager`에 없다. 반면 `BackgroundFitter.cs`/`WallFitter.cs`는 이미 `_gridAreaWidth = 14.53f`, `_gridAreaHeight = 10.16f`, `_cellAspectCorrection = 1.647f` 필드를 갖고 있어, 배경/벽 스케일링에 쓰인 "격자 영역" 크기 정보가 이미 존재한다. 다만 이 필드들은 `BackgroundFitter`/`WallFitter` 각각에 중복 선언된 `[SerializeField]` 값이며, `WaveManager`가 이 값을 직접 참조하는 연결 고리는 현재 없다.

### 9. `MonsterSetupEditor.SetupWaveSpawnEntries()` — 현재 자동화 로직

- 몬스터 종류 점진적 해금: `waveIdx < 5 ? 1 : waveIdx < 10 ? 2 : waveIdx < 15 ? 3 : 4` (Wave 1~5는 Fluffy만, 6~10은 +Spider, 11~15는 +StoneBug, 16~20은 +ForestDeer) — `MonsterRules.md` 2장/3장이 이미 폐지를 확정한 방식.
- 스폰 수 계산: `groupIdx = waveIdx / 5`, `posInGroup = waveIdx % 5`, `spawnCount = 3 + posInGroup + groupIdx * 2` — 5웨이브 단위 그룹 내에서 점진적으로 증가.
- 좌표 계산: `gridPos.x = (s % 5) - 2` (5열, -2~+2), `gridPos.y = startY - (s / 5)` (그룹마다 시작 행이 8/6/4/2로 다름) — 완전히 고정된 그리드 좌표 공식.
- 이 메서드가 실제로 대체하는 두 가지 역할(웨이브당 스폰 수 자동 계산 로직 / 좌표 배치 공식)을 새 구조에서 무엇이 대신할지가 열려 있다. 스폰 수 계산 자체(난이도 스케일링 방향성)는 `MonsterRules.md` 3장에서 "웨이브 진행에 따라 스폰 수 증가"로 방향성만 확정되어 있고 정확한 공식은 미정이다.

### 10. 격자 영역 정보 — `BackgroundFitter`/`WallFitter`

- `BackgroundFitter._gridAreaWidth = 14.53f`, `_gridAreaHeight = 10.16f`, `_cellAspectCorrection = 1.647f` — 배경 스프라이트를 카메라 뷰포트에 맞춰 스케일링할 때 셀 종횡비를 보정하는 데 쓰인다.
- `WallFitter`도 동일한 3개 필드(`_gridAreaWidth`/`_gridAreaHeight`/`_cellAspectCorrection`)를 독립적으로 갖고 있으며, 벽/바닥/발사 지점의 좌표를 `_nativeXXX * scaleX/Y` 형태로 재배치하는 데 사용한다.
- 두 스크립트 모두 이 값들로 "정사각형 셀이 되도록" 스케일을 계산하지만, 몇 개의 열/행으로 그리드가 나뉘는지(셀 개수)에 대한 정보는 어느 스크립트에도 없다. 즉 "영역 크기"는 알 수 있으나 "셀 크기/개수"는 별도로 정의해야 한다.

## 관련 파일 및 의존성

| 파일 | 경로 | 현재 역할 | 이번 개편 관련도 |
|---|---|---|---|
| MonsterData.cs | `Assets/_Project/Scripts/Data/MonsterData.cs` | 몬스터 스탯 SO (Hp/MoveSpeed/Damage/Reward) | 블록 크기 필드 추가 여부 검토 대상 |
| MonsterBase.cs | `Assets/_Project/Scripts/Monster/MonsterBase.cs` | HP/이동/상태이상/사망, 풀링 | 콜라이더·HP바 관련 로직은 없음(변경 범위는 프리팹/자식 오브젝트 쪽) |
| WaveTableData.cs | `Assets/_Project/Scripts/Data/WaveTableData.cs` | 20웨이브 테이블 SO | 구조 자체 변경 필요(좌표 제거, 구성 파라미터화) |
| WaveManager.cs | `Assets/_Project/Scripts/Wave/WaveManager.cs` | 웨이브 스폰/진행/클리어 판정 | `SpawnWave()` 위치 계산 로직 교체 필요, 점유 체크 신규 추가 필요 |
| MonsterSetupEditor.cs | `Assets/_Project/Scripts/Editor/MonsterSetupEditor.cs` | SO 에셋 자동 생성, 웨이브 스폰 데이터 자동 설정 | `SetupWaveSpawnEntries()` 전체 폐기/재작성 대상 |
| BackgroundFitter.cs | `Assets/_Project/Scripts/Core/BackgroundFitter.cs` | 배경 스프라이트 정사각형 셀 보정 | 격자 영역 크기 정보 제공(참조용, 선행 작업 완료) |
| WallFitter.cs | `Assets/_Project/Scripts/Core/WallFitter.cs` | 벽/바닥/발사점 좌표 보정 | 격자 영역 크기 정보 제공(참조용, 선행 작업 완료) |
| MonsterHpBar.cs | `Assets/_Project/Scripts/UI/MonsterHpBar.cs` | HP바 슬라이더 갱신 | 스크립트 자체는 재사용, 부착 위치/크기 설정만 변경 |
| 프리팹 4종 (캐릭터) | `Assets/_Project/Prefabs/Monster/Fluffy·Spider·StoneBug·ForestDeer.prefab` | 캐릭터+콜라이더+HpBarCanvas | 블록 자식 합성, 콜라이더 확장, HP바 재배치 대상 |
| 프리팹 4종 (블록 스텁) | `Assets/_Project/Prefabs/Monster/Block_1x1·1x2·2x1·2x2.prefab` | 미사용 스텁 | 그대로 재사용 불가(MonsterBase 중복 문제), 참고용 스프라이트 소스로만 활용 가능 |
| WaveTableData.asset | `Assets/_Project/Data/WaveTableData.asset` | 20웨이브 데이터 (좌표 포함 기존 구조로 이미 구워짐) | 구조 변경 시 재생성/재설정 필요, 에디터 실행 필요 |
| MonsterData_*.asset 4종 | `Assets/_Project/Data/MonsterData_Fluffy·Spider·StoneBug·ForestDeer.asset` | 몬스터별 스탯 값 (현재 4종 동일) | 2칸 몬스터 상향 차등 반영 필요, 블록 크기 필드 추가 시 재설정 필요 |
| UIRules.md 9장 | `Assets/_Project/Docs/UIRules.md` | HP바 블록 앞면 임베드 방식 확정 문서 | 이미 문서화됨, 코드 미반영 |
| MonsterRules.md | `Assets/_Project/Docs/MonsterRules.md` | 몬스터 시스템 단일 기준 문서 | 이번 research의 규칙 출처 |

## 문제점 / 구현 대상 파악

### 1. `MonsterData.cs` — 블록 크기(footprint) 표현 방식 미정

블록 크기 정보를 어디에 둘지 두 갈래 선택지가 있다.

- **선택지 A**: `MonsterData`에 `enum BlockSize { OneByOne, TwoByOne, OneByTwo }` 같은 필드를 추가해 데이터 쪽에서 크기를 표현. 장점은 점유 체크 로직(`WaveManager`)이 `MonsterData`만 보고도 몇 칸을 차지하는지 알 수 있어 좌표 계산이 데이터 드리븐이 됨. 단점은 프리팹의 실제 블록 자식 크기와 별개로 데이터가 하나 더 늘어나 두 값(데이터의 BlockSize vs 프리팹의 실제 자식 오브젝트 크기)이 어긋날 여지가 생김.
- **선택지 B**: `MonsterData`는 현재처럼 스탯만 유지하고, 블록 크기는 프리팹 쪽(블록 자식 오브젝트의 실제 `Transform`/`Collider` 크기, 혹은 `MonsterBase`가 런타임에 자식 콜라이더에서 읽어오는 방식)에서만 결정. 장점은 데이터 SO가 순수 스탯만 담아 단순함을 유지. 단점은 점유 체크(웨이브 시작 시 좌표 계산)를 하려면 스폰 전에 프리팹을 인스턴스화하거나 별도 매핑 테이블(몬스터 종류 → 블록 크기)이 코드 어딘가에 필요.
- 두 선택지 모두 "몬스터 종류 → 블록 크기"라는 고정 매핑 자체는 `MonsterRules.md` 3장에 이미 확정되어 있으므로, 이 매핑을 어느 계층(SO 데이터 / 프리팹 구조 / 코드 상수 테이블)에 둘지의 문제로 좁혀진다.

### 2. 프리팹 4종 재구성 — 블록+캐릭터 합성 구조

- 기존 `Block_*.prefab` 4개는 `MonsterBase`가 이미 붙어 있는 미완성 스텁이라 그대로 자식으로 끌어와 붙이면 부모(캐릭터 루트)와 자식(블록) 양쪽에 `MonsterBase`가 중복되는 문제가 생긴다. `MonsterBase`는 몬스터 전체의 HP/이동/사망을 관리하는 컴포넌트이므로 블록 쪽에는 필요 없고, 블록은 순수 시각(SpriteRenderer)+콜라이더 역할만 하는 자식으로 재구성해야 한다는 것이 문제 제기다.
- 구체적으로 정리가 필요한 지점: (a) 블록 스프라이트를 캐릭터 루트의 새 자식 오브젝트로 추가하고 `Block_*.prefab`의 `MonsterBase`/기존 컴포넌트는 제거, (b) 콜라이더를 캐릭터 루트에 유지할지 블록 자식으로 옮길지, (c) 렌더 순서(블록이 캐릭터 스프라이트보다 뒤에 그려져야 함 — `SpriteRenderer.sortingOrder` 조정 필요 여부).
- 기존 스텁 4개(`Block_1x1/1x2/2x1/2x2.prefab`)는 "그대로 재사용 불가"하지만 블록 스프라이트 에셋 자체(`Block_*.png`)는 재사용 가능하다.

### 3. 콜라이더 — 캐릭터 크기 → 블록 전체 크기로 확장

- 현재: 캐릭터 프리팹의 `BoxCollider2D`가 캐릭터 스프라이트 크기 기준(`Fluffy.prefab`은 0.95×0.99).
- 필요: 2칸 몬스터(StoneBug/ForestDeer)는 블록 전체 크기(StoneBug 1.92×0.96, ForestDeer 0.96×1.92)로 콜라이더 확장.
- 열린 질문: 콜라이더를 몬스터 루트에 유지하고 크기/오프셋만 블록 크기에 맞게 수정할지, 아니면 블록 자식 오브젝트에 콜라이더를 옮기고 캐릭터 스프라이트 쪽 콜라이더는 제거할지. 후자를 선택하면 Ball 충돌 감지(`Ball.OnCollisionEnter2D` → 태그 `"Monster"` 검사 → `GetComponent<MonsterBase>()`)가 자식 콜라이더에서 부모의 `MonsterBase`를 어떻게 찾을지(`GetComponentInParent` 필요 여부)도 함께 검토해야 한다.

### 4. HP바 — 부착 위치 및 크기 로직 변경

- 현재: `HpBarCanvas`가 캐릭터 루트의 자식, 로컬 오프셋 `y=0.6`(머리 위), `sizeDelta = {1, 0.15}` 고정.
- 필요: 블록 앞면(정면 하단)에 임베드, 폭은 블록 가로 길이에 비례(1칸 블록·세로 2칸 블록은 좁게, 가로 2칸 블록은 2배 폭).
- `MonsterHpBar.cs`/`MonsterBase.OnHpChanged` 이벤트 발행 로직 자체는 `UIRules.md` 9장이 "재사용 가능"으로 명시했으므로 변경 대상이 아니며, 변경 대상은 `HpBarCanvas`의 부모(어느 오브젝트의 자식이 되는지)와 `RectTransform`의 앵커/크기 설정뿐이다.
- 열린 질문: 4종 프리팹마다 HP바 폭 값을 개별로 수동 설정할지, 아니면 블록 크기(footprint)에서 자동 계산하는 로직을 추가할지 — 이는 위 1번 질문(블록 크기를 어디서 읽어올지)과 연결된다.

### 5. `WaveTableData.cs`/`MonsterSpawnEntry` — 좌표 제거, 구성 파라미터화

- 현재 `Data`(MonsterData) + `GridPosition`(Vector2Int) 구조에서 `GridPosition`을 제거해야 한다.
- `MonsterRules.md` 6장이 요구하는 "웨이브당 스폰 수 + 몬스터 종류별 등장 가중치"를 표현할 필드 설계는 여러 선택지가 있다: (a) `int SpawnCount` + `float[] TypeWeights`(4종 고정 배열), (b) `int SpawnCount` + `List<MonsterTypeWeight>`(몬스터 종류-가중치 쌍의 리스트, 확장에 유연), (c) 가중치를 `WaveEntry`가 아니라 전역 설정(웨이브 진행도에 따른 공식)으로 빼고 `WaveEntry`는 `SpawnCount`만 가짐. 정확한 필드 설계는 plan.md에서 결정.
- `MonsterData` 참조 자체(4종 에셋)는 그대로 유지되며, 각 웨이브가 "어떤 `MonsterData` 에셋을 얼마의 가중치로 뽑을지"만 구성 파라미터로 남는다.

### 6. `WaveManager.SpawnWave()` — 고정 좌표 → 런타임 랜덤 배치 + 점유 체크

- 현재 로직(`entry.GridPosition * _gridCellSize`)을 몬스터 종류 가중치 랜덤 선택 + 좌표 랜덤 계산 + 점유 체크로 교체해야 한다.
- 그리드 열/행 개수를 어떻게 정의할지가 핵심 설계 질문이다. `BackgroundFitter`/`WallFitter`의 `_gridAreaWidth`(14.53)/`_gridAreaHeight`(10.16)는 "월드 유닛 기준 전체 영역 크기"이며, 셀 크기(`_gridCellSize = 1.0f`, `WaveManager`에 이미 존재)로 나누면 대략적인 열/행 개수를 얻을 수 있으나(`14.53 / 1.0 ≈ 14열`), 이 값이 실제로 스폰에 사용 가능한 "필드 상단 그리드 영역"과 일치하는지, 아니면 스폰 전용 영역이 별도로 더 좁게 설정되어야 하는지는 검토가 필요하다. 또한 `_gridAreaWidth`/`_gridAreaHeight`가 `BackgroundFitter`/`WallFitter` 두 스크립트에 각각 중복 선언되어 있어, `WaveManager`가 이 값을 어떤 경로로 참조할지(제3의 공용 설정 SO/상수 클래스로 뽑아낼지, 또는 필드를 다시 한 번 `WaveManager`에도 직접 선언할지)도 선택지로 남는다.
- 점유 체크 자료구조: 2차원 bool 배열, `HashSet<Vector2Int>`, 또는 다른 방식 중 선택 필요. 2칸 몬스터가 차지하는 여러 셀을 한 번에 점유 표시/해제하는 로직 설계도 필요.
- 열린 질문: 점유 체크가 "같은 웨이브 내에서 이미 배치된 몬스터끼리만" 겹치지 않으면 되는지(매 웨이브 새로 초기화), 아니면 이전 웨이브에서 아직 하단에 도달하지 않고 필드에 남아있는 몬스터도 고려해야 하는지. `MonsterRules.md` 2장/6장은 "이전 웨이브의 점유 상태와는 무관하게 매 웨이브 새로 랜덤 결정"이라 명시하지만, 한 웨이브가 끝나야(전멸) 다음 웨이브가 스폰되는 현재 클리어 조건(`CheckWaveCleared` — 활성 몬스터 0일 때만 다음 웨이브)과 결합해서 보면 스폰 시점에는 이전 웨이브 몬스터가 이미 전부 사라진 상태이므로 실질적으로는 문제가 되지 않을 가능성이 높다 — 이 부분은 plan.md에서 재확인 필요.

### 7. `MonsterSetupEditor.SetupWaveSpawnEntries()` — 폐기 대상 로직의 대체

- 폐기되는 것: 종류 점진적 해금 계산(`monsterTypeCount`), 좌표 공식(`gridPos.x/y`).
- 대체가 필요한 것: 웨이브당 스폰 수 자동 계산(`spawnCount = 3 + posInGroup + groupIdx * 2`)이 하던 역할 — 이 자동 생성 로직 자체를 유지하되 좌표 없이 "스폰 수"만 `WaveEntry`에 채우는 방향으로 남길지, 혹은 스폰 수 자체도 런타임에 웨이브 번호 기반 공식으로 계산해 에디터 사전 굽기를 아예 없앨지(즉 `WaveTableData.asset`을 더 단순화하거나 완전히 없애고 `WaveManager`가 웨이브 번호로부터 직접 계산)가 선택지로 열려 있다. 후자를 선택하면 `MonsterSetupEditor.SetupWaveSpawnEntries()` 메서드 자체가 필요 없어질 수도 있다.

### 8. 기존 `WaveTableData.asset` — 재생성/재설정 필요

- `Assets/_Project/Data/WaveTableData.asset`이 이미 존재하며 기존 구조(좌표 포함) 데이터가 구워져 있는 것으로 추정된다.
- `WaveEntry`/`MonsterSpawnEntry`의 필드 구조 자체가 바뀌면 이 asset의 직렬화 스키마가 바뀌므로, Unity가 기존 필드를 못 찾아 데이터 유실(빈 값)이 발생하거나 재직렬화가 필요하다.
- 이 작업은 Unity 에디터(Inspector 또는 `MonsterSetupEditor`류 자동화 스크립트)의 실행이 필요하며, 현재 원격/텍스트 기반 환경에서는 `.asset` YAML을 직접 읽고 쓰는 것이 기술적으로는 가능하지만 위험도가 높고(스키마 불일치 시 Unity가 에셋을 깨진 것으로 인식할 수 있음) 검증이 어렵다는 한계가 있다. plan.md에서 이 재생성 작업을 에디터 자동화 스크립트(`MonsterSetupEditor` 확장)로 처리할지, 사용자가 직접 Unity에서 실행해야 하는 수동 단계로 남길지 결정이 필요하다.

## 결론

`MonsterRules.md`에 확정된 새 규칙과 현재 코드/프리팹 사이의 간극은 크게 네 갈래로 나뉜다.

1. **데이터 설계**: `MonsterData`에 블록 크기 필드를 추가할지 여부(선택지 A/B), `MonsterSpawnEntry`의 좌표 제거 후 가중치 표현 방식(선택지 a/b/c) — 둘 다 여러 대안이 가능하며 plan.md에서 결정 필요.
2. **프리팹 합성**: 캐릭터 4종 프리팹에 블록 자식을 추가하고 기존 블록 스텁(`Block_*.prefab`)의 `MonsterBase`/컴포넌트를 제거해 순수 시각+콜라이더 역할로 재구성. 콜라이더를 부모/자식 중 어디에 둘지, Ball 충돌 감지 코드(`GetComponent` vs `GetComponentInParent`)에 영향이 있는지 확인 필요.
3. **HP바 재배치**: `MonsterHpBar`/`OnHpChanged`는 그대로 재사용하되, `HpBarCanvas`의 부모와 `RectTransform` 앵커/크기만 블록 앞면 하단 임베드 방식으로 변경. 폭 값을 수동 설정할지 블록 크기에서 자동 계산할지는 1번 질문과 연결.
4. **웨이브/스폰 로직 재작성**: `WaveManager.SpawnWave()`에 종류 가중치 랜덤 선택 + 좌표 랜덤 계산 + 점유 체크를 새로 구현해야 하며, 그리드 열/행 개수를 `BackgroundFitter`/`WallFitter`의 격자 영역 크기와 어떻게 연결할지가 핵심 설계 질문으로 남아 있다. `MonsterSetupEditor.SetupWaveSpawnEntries()`가 폐기되면서 웨이브당 스폰 수 자동 계산 로직을 에디터 사전 굽기로 유지할지, 런타임 공식으로 대체할지도 결정이 필요하다. 기존 `WaveTableData.asset`은 구조 변경 시 재생성이 필요하며 에디터 실행이 요구되는 작업이라는 점을 별도로 인지해야 한다.

모든 선택지는 나열만 하였으며, 실제 결정(필드 설계, 프리팹 구조, 그리드 계산 공식 등)은 사용자와 논의 후 plan.md에서 확정한다.
