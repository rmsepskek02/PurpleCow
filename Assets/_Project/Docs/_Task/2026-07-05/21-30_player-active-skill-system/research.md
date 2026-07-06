# Research - Player Active Skill System

`PlayerActiveSkillDesign.md`는 기존 로그라이크 볼/패시브 스킬과 별개로, 플레이어가 인게임 버튼을 눌러 직접 발동하는 스킬 시스템을 정의한다. 이번 작업 범위는 사용자와 논의가 끝난 **스피드업**와 **분신** 2종이다. 두 스킬 모두 쿨타임은 30초이며, 구현 전에 볼 회수 후 재발사 흐름을 FIFO 대기열 방식으로 바꾸는 선행 작업이 필요하다.

## 현재 상태

- `BallLauncher`는 게임 시작 시 `_rosterLaunchInterval` 간격으로 노멀볼 로스터를 순차 발사한다.
- 로스터 볼이 Ground 충돌 후 `LaunchPoint`로 돌아오면 `Ball.FixedUpdate()`에서 즉시 `BallLauncher.RelaunchBall(this)`를 호출한다.
- 이 구조에서는 여러 볼이 거의 동시에 회수될 때 재발사도 동시에 몰릴 수 있어, 분신 복사볼까지 추가되면 발사 흐름이 불안정해질 수 있다.
- 기존 `SkillManager`/`SkillData`는 3택지 로그라이크 볼 스킬/패시브용이며, 플레이어 직접 발동 스킬과는 획득 방식과 UI가 다르다.

## 확정된 규칙

- 회수된 볼은 즉시 재발사하지 않고 FIFO 큐에 들어간다.
- 큐의 볼은 `_rosterLaunchInterval`과 같은 간격으로 하나씩 재발사된다.
- 원본 볼/분신 복사볼의 모든 발사는 실제 발사 순간의 현재 궤도 방향(`BallLauncher.LaunchDirection`)을 사용한다.
- 스피드업은 30초 쿨타임, 일정 시간 동안 모든 볼 속도 1.5배이다.
- 스피드업 지속 중 새로 발사/재발사되는 볼도 1.5배 속도를 사용한다.
- 분신은 30초 쿨타임, 현재 사용 중인 **원본 로스터 볼만** 복사한다.
- 분신 복사볼은 다시 복사 대상이 아니다.
- 분신 복사볼은 로스터에 등록하지 않고, 순차 발사된다.
- 분신 복사볼은 2회 회수되면 재발사하지 않고 그 자리에서 소멸한다.
- 분신은 별도 시간 제한을 두지 않고, 2회 회수 제한만 사용한다.

## 관련 파일 및 의존성

- `Assets/_Project/Scripts/Ball/BallLauncher.cs`: 로스터, 발사/재발사, 볼 풀 관리
- `Assets/_Project/Scripts/Ball/Ball.cs`: Ground 충돌 후 귀환, LaunchPoint 도착 판정, 속도 유지
- `Assets/_Project/Scripts/UI/UIManager.cs`, `UISetupEditor.cs`: HUD 하단 버튼 생성/연결
- `Assets/_Project/Scripts/Data/SkillData.cs`, `SkillSetupEditor.cs`: 기존 스킬 데이터와 분리된 신규 데이터 구조 참고

## 결론

선행으로 `BallLauncher`에 회수 대기 큐를 추가하고 `Ball`의 도착 콜백을 즉시 재발사가 아니라 큐 등록 방식으로 바꾼다. 그 위에 `PlayerActiveSkillData`, `PlayerActiveSkillManager`, `PlayerActiveSkillButton`을 추가해 스피드업/분신만 구현한다.
