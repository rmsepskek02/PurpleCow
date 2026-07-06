# Plan - Submission Cleanup and README

제출 전 정리 작업으로 S/F 테스트 버튼을 완전히 제거하고, 플레이어 액티브 스킬 명칭을 코드·데이터·씬·문서 전체에서 `SpeedUp/스피드업`으로 통일한다. 이후 실제 구현 내용, AI 활용 범위, Android 실기기 20웨이브 검증을 포함한 루트 `README.md`를 작성한다.

## 구현 목표

- 제출 씬과 런타임 코드에서 성공/실패 강제 테스트 버튼 흔적을 제거한다.
- 영문 타입은 `SpeedUp`, camelCase 식별자는 `speedUp`, 한국어 표시명은 `스피드업`으로 통일한다.
- Unity 에셋 GUID와 직렬화 참조를 유지한다.
- 프로젝트를 처음 보는 평가자가 실행법, 조작법, 구현 범위, 기술 구조, AI 활용 내역을 README 하나로 파악할 수 있게 한다.
- Android 실제 기기 20웨이브 테스트 완료 사실을 상태 문서와 README에 기록한다.

## 단계별 작업 계획

### 1. S/F 테스트 버튼 코드 제거

`HUDPanel.cs`에서 다음을 제거한다.

- `_successTestButton`, `_failureTestButton`
- 두 버튼의 클릭 리스너 등록/해제
- `HandleSuccessTestClicked()`, `HandleFailureTestClicked()`

일반 성공 경로인 20웨이브 클리어와 실패 경로인 캐릭터 HP 0은 수정하지 않는다.

### 2. S/F 테스트 버튼 생성 코드 제거

`UIOverhaulSetupEditor.cs`에서 다음을 제거한다.

- `SuccessTestButton`, `FailureTestButton` 생성
- `S`, `F` 텍스트와 좌측 상단 배치
- `HUDPanel`의 제거된 직렬화 필드 연결

Setup 메뉴를 다시 실행해도 테스트 버튼이 재생성되지 않게 한다.

### 3. SampleScene에서 S/F 오브젝트 제거

`SampleScene.unity`에서 S/F 관련 YAML 블록만 제한적으로 제거한다.

- `SuccessTestButton` GameObject와 RectTransform/Image/Button/Text/CanvasRenderer 등 연결 컴포넌트
- `FailureTestButton` GameObject와 연결 컴포넌트
- 부모 RectTransform의 자식 참조
- `HUDPanel`의 `_successTestButton`, `_failureTestButton` 직렬화 참조

다른 HUD 오브젝트의 fileID와 계층은 변경하지 않는다.

### 4. SpeedUp 내부 명칭 전체 변경

다음 기준으로 변경한다.

- enum: `PlayerActiveSkillType.SpeedUp`
- 코루틴 필드: `_speedUpCoroutine`
- 코루틴 메서드: `CoSpeedUp()`
- 데이터 파일: `PlayerActiveSkillData_SpeedUp.asset`
- 데이터 표시명: `Speed Up`
- 생성기 버튼명: `SpeedUpButton`
- 씬 오브젝트명: `speedUp`
- 문서 표시명: `스피드업`

에셋 파일은 `.asset`과 `.meta`를 함께 이동해 기존 GUID를 보존한다. 코드 enum은 순서를 유지해 기존 직렬화된 enum 값이 같은 기능을 가리키게 한다.

변경 후 저장소 전체 검색으로 변경 전 명칭이 남지 않았는지 확인한다.

### 5. README 작성

루트에 한국어 `README.md`를 새로 작성한다.

#### 프로젝트 개요

- CookApps 클라이언트 채용 과제
- 통통 디펜스: 핀볼 마스터 1스테이지 카피
- Unity 6000.3.10f1 / Android / Universal 2D URP

#### 실행 및 조작

- `Assets/Scenes/SampleScene.unity`
- 터치 드래그 조준
- 볼 자동 귀환/FIFO 재발사
- 일시정지
- 스피드업/분신 플레이어 액티브 스킬

#### 구현 범위

- 핀볼 충돌·반사·귀환
- 20웨이브, 4종 몬스터, 블록 크기와 스폰 로직
- XP와 Lv.19 성장, 3택지
- 액티브 볼 5종, 패시브 5종
- 결과 팝업과 재시작
- 해상도/Safe Area/UI 대응

#### 주요 기술 설계

- 볼 로스터와 오브젝트 풀
- ScriptableObject 읽기 전용 데이터와 런타임 상태 분리
- 컨베이어식 웨이브 스폰
- 조준 입력과 UI 터치 분리
- Setup Editor 자동 연결

#### 가산점 구현

- 스피드업: 6초간 모든 볼 속도 1.5배, 쿨타임 30초
- 분신: 현재 원본 로스터 복제, 두 번째 회수 시 제거, 쿨타임 30초
- 원본 캡처 기반 UI 오버홀
- 캐릭터 조준 방향 연동과 궤적 프리뷰 강화

#### 구현 제외 항목

- 튜토리얼
- 배속
- 1스테이지 보스
- 자동 조준
- 선택지 다시 뽑기
- 융합 시스템

#### AI 활용

- Codex: 코드베이스 조사, 구현, 디버깅, 문서화, 빌드 검증 지원
- Claude/Claude Code: 초기 구조 설계, 코드·문서 작업, QA 지원
- GPT: `SpeedUp.png`, `Copy.png` 플레이어 액티브 스킬 버튼 이미지 제작
- 사용자가 설계 의사결정, Unity 적용, 실제 기기 테스트와 최종 검증 수행

#### 검증

- Android 실제 기기에서 20웨이브 전체 플레이 완료
- 성공/실패/재시작, 스킬, UI 동작 확인

#### 문서 링크

- `AGENTS.md`
- `Assets/_Project/Docs/GameplayMechanics.md`
- `Assets/_Project/Docs/MonsterRules.md`
- `Assets/_Project/Docs/UIRules.md`
- `Assets/_Project/Docs/PlayerActiveSkillDesign.md`

### 6. 현재 문서 갱신

- `PlayerActiveSkillDesign.md`: 스피드업 명칭
- `UIRules.md`: 스피드업 명칭과 테스트 버튼 제거 상태
- `ProjectStatus.md`: S/F 제거, README 작성, 20웨이브 실기기 검증
- `ProjectHistory.md`: 제출 정리 작업 기록
- `AGENTS.md`: 문서 설명에서 스피드업 명칭 반영
- 과거 task 문서의 명칭도 현재 기준인 `SpeedUp/스피드업`으로 통일

### 7. 검증

1. `rg`로 변경 전 스킬 명칭과 `SuccessTestButton`, `FailureTestButton`, `_successTestButton`, `_failureTestButton` 잔여 검색
2. `SampleScene.unity`의 S/F GameObject 및 HUD 참조 제거 확인
3. SpeedUp 데이터 에셋 GUID와 모든 참조 경로 확인
4. `Assembly-CSharp.csproj` 빌드
5. `Assembly-CSharp-Editor.csproj` 빌드
6. `git diff --check`
7. README 내부 링크와 사실 관계 확인

## 예상 변경·생성 파일

### 신규

- `README.md`

### 이름 변경

- `Assets/_Project/Data/PlayerActiveSkillData_SpeedUp.asset`
  → `Assets/_Project/Data/PlayerActiveSkillData_SpeedUp.asset`
- 대응 `.meta` 파일도 함께 이동

### 코드/씬

- `Assets/_Project/Scripts/UI/HUDPanel.cs`
- `Assets/_Project/Scripts/Editor/UIOverhaulSetupEditor.cs`
- `Assets/_Project/Scripts/Editor/UISetupEditor.cs`
- `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`
- `Assets/_Project/Scripts/Data/PlayerActiveSkillData.cs`
- `Assets/_Project/Scripts/Skill/PlayerActiveSkillManager.cs`
- `Assets/Scenes/SampleScene.unity`

### 문서

- `AGENTS.md`
- `Assets/_Project/Docs/PlayerActiveSkillDesign.md`
- `Assets/_Project/Docs/UIRules.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`
- `Assets/_Project/Docs/_Task/` 아래 기존 문서 중 명칭 사용 파일

## 주의사항

- 씬 전체 재생성은 하지 않고 S/F와 SpeedUp 명칭 관련 항목만 수정한다.
- 데이터 에셋 GUID를 바꾸지 않는다.
- enum 순서를 바꾸지 않아 기존 직렬화 값과 호환되게 한다.
- `SpeedUp.png`, `Copy.png` 외의 리소스를 GPT 제작물로 표기하지 않는다.
- README에 미구현 TODO Polish 항목을 완료 기능처럼 기록하지 않는다.
- 기존 `apk.apk`, `.claude/settings.local.json`은 건드리지 않는다.
