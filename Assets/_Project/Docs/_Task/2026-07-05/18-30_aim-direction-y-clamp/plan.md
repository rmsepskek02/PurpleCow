# Plan — 조준 방향 Y좌표 하한 제한 (WaveManager 기준선 재사용)

research.md에서는 격자 밑변 기준점으로 `WallFitter.Ground`를 참조하는 방식(A/B/C 후보)을 검토했으나, 사용자가 방향을 단순화했다 — 몬스터 게임오버 판정에 이미 쓰이는 `WaveManager._bottomBoundaryY`를 그대로 재사용하기로 확정했다. `WaveManager`는 이미 `Singleton<WaveManager>`이므로 `WaveManager.Instance`로 어디서든 접근 가능해, `Ground`/`WallFitter` 참조 연결이나 씬 수정이 전혀 필요 없다. 이 문서는 대안 비교 없이 확정된 구현 내용만 서술한다.

## 구현 목표

- 조준(터치 드래그) 목표 지점의 월드 Y좌표가 `WaveManager._bottomBoundaryY`(게임오버 판정 기준선)보다 아래로 내려가지 못하도록 제한한다.
- 씬 참조 연결, 에디터 스크립트 수정 없이 코드 두 곳만 수정해 최소 변경으로 해결한다.

## 단계별 작업 계획

1. **`WaveManager.cs`에 프로퍼티 추가**
   - 기존 `public int TotalWaves => _waveTable.TotalWaves;`와 동일한 위치/스타일로 다음 프로퍼티를 추가한다.
     ```csharp
     public float BottomBoundaryY => _bottomBoundaryY;
     ```
   - `_bottomBoundaryY` 필드 자체는 그대로 두고 읽기 전용 접근만 새로 연다.

2. **`InputHandler.ComputeAimDirection` 수정**
   - `worldPos`를 계산한 직후, `LaunchPoint` 기준 방향을 구하기 전에 Y좌표를 clamp한다.
     ```csharp
     private Vector2 ComputeAimDirection(Vector2 screenPos)
     {
         Vector2 worldPos = _mainCamera.ScreenToWorldPoint(screenPos);
         worldPos.y = Mathf.Max(worldPos.y, WaveManager.Instance.BottomBoundaryY);
         Vector2 launchPointPos = BallLauncher.Instance.LaunchPoint.position;
         return (worldPos - launchPointPos).normalized;
     }
     ```
   - 터치 위치를 월드 좌표로 변환한 뒤, 그 Y좌표가 게임오버 기준선보다 낮으면 기준선 높이로 끌어올리고(clamp), 보정된 좌표를 기준으로 발사 지점 대비 방향을 계산한다. 이렇게 하면 조준 목표 지점이 기준선보다 아래로는 절대 내려가지 않아, 볼 궤도(조준 방향)가 항상 그 기준선 이상을 향하게 된다.

3. **씬/에디터 스크립트 변경 없음**
   - `WaveManager`가 이미 싱글톤이므로 `Ground`/`WallFitter` 참조 연결, `SceneSetupEditor.cs` 수정이 모두 불필요하다.

4. **문서화**
   - `Assets/_Project/Docs/GameplayMechanics.md` 섹션 1(볼 발사 및 궤도 시스템)에 "조준 목표 지점의 Y좌표는 게임오버 판정 기준선(`WaveManager._bottomBoundaryY`)보다 아래로 내려갈 수 없다"는 취지의 규칙 한 줄을 기존 문서 톤에 맞게 추가한다.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Wave/WaveManager.cs` — `BottomBoundaryY` 프로퍼티 추가
- `Assets/_Project/Scripts/Core/InputHandler.cs` — `ComputeAimDirection`에 Y좌표 clamp 로직 추가
- `Assets/_Project/Docs/GameplayMechanics.md` — 섹션 1에 새 규칙 문서화

## 주의사항

- `WaveManager.Instance`가 `null`일 가능성은 없다 — 씬에 항상 존재하는 싱글톤이므로 별도 null 체크가 불필요하며, 기존 코드 관례상 `BallLauncher.Instance`도 이미 null 체크 없이 바로 사용하고 있어 일관된 스타일이다.
- 이 clamp는 어디까지나 "조준 목표 지점"의 Y좌표만 보정하는 것이다. 발사된 볼이 실제로 날아가다가 벽/몬스터에 반사되어 기준선보다 아래로 내려가는 것 자체를 막는 장치가 아니며, 그것은 물리 반사의 자연스러운 결과로 이번 작업 범위 밖이다. 오직 "플레이어가 조준할 수 있는 방향의 범위"만 제한하는 것임을 명확히 한다.
- `TrajectoryPreview.cs`는 `BallLauncher.Instance.LaunchDirection`(이미 clamp된 방향 벡터)을 그대로 받아 궤적을 그리므로 별도 수정이 필요 없다. 전파 경로는 `InputHandler.ComputeAimDirection` → `OnDrag` 이벤트 → `BallLauncher._launchDirection` → `TrajectoryPreview.Update()`가 매 프레임 참조하는 순서다.
- 원격 환경엔 Unity가 없어 실제 clamp 동작을 시각적으로 검증할 수 없다. 최종 확인은 사용자 로컬 테스트로 진행한다.
