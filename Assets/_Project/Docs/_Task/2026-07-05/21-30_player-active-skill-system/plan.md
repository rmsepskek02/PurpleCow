# Plan - Player Active Skill System

이번 작업은 사용자와 확정한 스피드업/분신 2종 플레이어 액티브 스킬과, 그 선행 조건인 회수 볼 FIFO 재발사 구조를 구현한다. 기존 3택지 로그라이크 스킬 시스템과는 섞지 않고 별도 데이터/매니저/UI로 구성한다.

## 구현 목표

- 회수된 볼을 FIFO 큐로 재발사한다.
- 큐 재발사 간격은 `_rosterLaunchInterval`과 동일하게 사용한다.
- 스피드업: 30초 쿨타임, 지속시간 동안 모든 볼 속도 1.5배.
- 분신: 30초 쿨타임, 현재 사용 중인 원본 로스터 볼만 복사해 순차 발사.
- 분신 복사볼은 2회 회수되면 소멸하고, 복사 대상/로스터에는 포함하지 않는다.
- HUD 하단 버튼 2개와 쿨타임 표시를 추가한다.

## 단계별 작업 계획

1. `BallLauncher` 재발사 흐름 변경
   - 회수 대기 큐 추가.
   - `LaunchPoint` 도착 볼을 큐에 넣고, 코루틴으로 `_rosterLaunchInterval` 간격 재발사.
   - 실제 발사 순간의 `LaunchDirection` 사용.
2. `Ball` 생명주기 보강
   - 속도 배율 필드 추가.
   - 분신 복사볼 여부와 남은 회수 횟수 관리.
   - 복사볼이 2회 회수되면 풀로 반환.
3. `PlayerActiveSkillData` 추가
   - `SpeedUp`, `Clone` 타입과 쿨타임/지속시간/배율/회수 횟수 수치 저장.
4. `PlayerActiveSkillManager` 추가
   - 2개 스킬 쿨타임 상태 관리.
   - 스피드업 발동 시 `BallLauncher` 전역 속도 배율 적용.
   - 분신 발동 시 원본 로스터 볼 스냅샷을 복사 발사.
5. `PlayerActiveSkillButton` 추가
   - 버튼 클릭, 쿨타임 overlay fill, 남은 초 텍스트 표시.
6. 에디터 자동 세팅 확장
   - `SkillSetupEditor`가 `PlayerActiveSkillData_SpeedUp.asset`, `PlayerActiveSkillData_Clone.asset` 생성.
   - `UISetupEditor`가 하단 버튼 2개를 만들고 `PlayerActiveSkillManager` 참조를 연결.

## 예상 변경/생성 파일 목록

- `Assets/_Project/Scripts/Data/PlayerActiveSkillData.cs`
- `Assets/_Project/Scripts/Skill/PlayerActiveSkillManager.cs`
- `Assets/_Project/Scripts/UI/PlayerActiveSkillButton.cs`
- `Assets/_Project/Scripts/Ball/BallLauncher.cs`
- `Assets/_Project/Scripts/Ball/Ball.cs`
- `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs`
- `Assets/_Project/Docs/PlayerActiveSkillDesign.md`

## 주의사항

- 분신 복사볼은 원본 로스터 볼만 복사하며, 복사볼은 다시 복사하지 않는다.
- 분신 복사볼은 클러스터 서브볼처럼 로스터 밖의 임시 볼로 취급하되, Ground 회수 후 최대 1회 더 재발사할 수 있다.
- Unity 에디터 Setup과 씬 참조 연결을 완료했다.

## 구현 결과

- 계획한 런타임 코드, 데이터 구조, HUD 버튼, Setup Editor 확장을 구현했다.
- 테스트용 `speedUp`/`illusion` 버튼을 재사용하며, 게임 시작 시 두 스킬을 즉시 사용할 수 있도록 설정했다.
- `Assembly-CSharp.csproj`와 `Assembly-CSharp-Editor.csproj` 빌드 결과 신규 오류 0개를 확인했다.
- 사용자가 Unity 플레이 테스트를 완료해 FIFO 재발사, 스피드업, 분신, 쿨타임 UI와 버튼 터치 차단이 정상 동작함을 확인했다.
