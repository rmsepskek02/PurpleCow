# Research - Submission Cleanup and README

이번 작업은 실기기 20웨이브 테스트가 완료된 현재 프로젝트를 제출 가능한 상태로 정리하기 위한 조사이다. 테스트 전용 성공/실패 버튼을 코드·Setup Editor·씬에서 완전히 제거하고, 공식 과제 요구사항과 실제 구현 내역을 설명하는 루트 `README.md`를 새로 작성하는 것이 범위다.

## 현재 상태

### 20웨이브 검증

- 사용자가 Android 실제 기기에서 20웨이브 테스트를 완료했다.
- 기존 `ProjectStatus.md`에는 개별 UI/스킬 테스트 완료 기록은 있으나, 전체 20웨이브 실기기 완주 검증이 명시적으로 정리되어 있지 않다.
- README의 검증 항목과 프로젝트 상태 문서에 해당 사실을 기록할 필요가 있다.

### S/F 테스트 버튼

성공/실패 결과 팝업을 강제로 확인하기 위해 추가한 테스트 버튼이 세 위치에 남아 있다.

1. `HUDPanel.cs`
   - `_successTestButton`, `_failureTestButton` 직렬화 필드
   - `Start()`의 클릭 리스너 등록
   - `OnDestroy()`의 클릭 리스너 해제
   - `HandleSuccessTestClicked()`, `HandleFailureTestClicked()`

2. `UIOverhaulSetupEditor.cs`
   - `SuccessTestButton`, `FailureTestButton` 생성
   - `S`, `F` 텍스트와 좌측 상단 배치
   - `HUDPanel` 테스트 버튼 필드 연결

3. `SampleScene.unity`
   - `SuccessTestButton`, `FailureTestButton` GameObject와 모든 UI 컴포넌트
   - `HUDPanel`의 두 직렬화 참조

현재 씬에서 두 버튼 GameObject는 비활성 상태지만 제출물에는 테스트 전용 오브젝트와 코드가 남지 않도록 완전히 제거하는 편이 적절하다. 실제 성공/실패 흐름은 20웨이브 클리어와 캐릭터 HP 0 경로로 유지된다.

### README

- 레포 루트에 `README.md`가 없다.
- 공식 과제 PDF는 구현한 내용을 README에 상세히 기록하는 것을 가산점 항목으로 안내한다.
- 현재 구현 정보는 `ProjectStatus.md`, `ProjectHistory.md`, `UIRules.md`, `GameplayMechanics.md`, `MonsterRules.md`, `PlayerActiveSkillDesign.md`에 분산되어 있다.
- 최종 제출자가 프로젝트를 처음 열어도 실행 방법, 조작법, 주요 구현, 설계 의도, 제외 범위, 검증 상태를 한 문서에서 파악할 수 있도록 요약 문서가 필요하다.

## README에 포함할 내용

README는 프로젝트 문서 언어와 동일하게 한국어를 기본으로 작성하고, 클래스명·메뉴명·기술 용어는 원문 영문 표기를 유지하는 안이 적절하다.

1. 프로젝트 개요
   - 통통 디펜스: 핀볼 마스터 1스테이지 카피 과제
   - Unity/Android 대상

2. 개발 환경
   - Unity `6000.3.10f1`
   - Universal 2D URP
   - New Input System
   - TextMeshPro / DOTween

3. 실행 및 빌드 방법
   - `Assets/Scenes/SampleScene.unity` 실행
   - Android 빌드 대상
   - 필수 Setup 메뉴는 이미 적용된 상태임을 명시

4. 조작법
   - 터치/드래그 조준
   - 자동 순환 발사
   - 일시정지
   - 스피드업/분신 액티브 버튼

5. 필수 구현
   - 핀볼 충돌·반사·귀환·FIFO 재발사
   - 20웨이브와 4종 몬스터
   - XP/캐릭터 Lv.19/삼택지
   - 액티브 볼 5종과 패시브 5종
   - 성공/실패 결과와 재시작

6. 주요 기술 구현
   - 타입별 볼 로스터와 오브젝트 풀
   - 읽기 전용 ScriptableObject + 런타임 스킬 상태 분리
   - 해상도/Safe Area/배경 격자 대응
   - 몬스터 컨베이어식 웨이브 스폰
   - 입력과 UI 터치 분리

7. 가산점 구현
   - 스피드업/분신 플레이어 액티브 스킬
   - 원본 캡처 기반 UI 오버홀
   - 캐릭터 조준 방향 연동
   - 향상된 궤적 프리뷰

8. 구현 제외 항목
   - 튜토리얼, 배속, 보스, 자동 조준, 선택지 다시 뽑기, 융합

9. 검증
   - Android 실제 기기 20웨이브 테스트 완료
   - 성공/실패/재시작 및 스킬·UI 동작 검증

10. 문서 안내
   - `AGENTS.md`를 프로젝트 문서 인덱스로 연결
   - 주요 설계 문서 링크 제공

11. AI 활용 내역
   - Codex: 코드 조사, 구현, 디버깅, 문서화, 빌드 검증 지원
   - Claude/Claude Code: 초기 설계, 에이전트 운영, 코드·문서 작업 지원
   - GPT: 게임 에셋 제작 지원
   - AI 결과물을 그대로 사용한 것으로 과장하지 않고, 사용자가 실제 Unity 플레이 테스트와 의사결정을 수행했다는 점을 함께 기록

## 스피드업 명칭 변경 조사

변경 전 명칭은 다음 범위에 사용되고 있었다.

- 사용자 데이터 표시명: `PlayerActiveSkillData_SpeedUp.asset._displayName = Speed Up`
- 데이터 생성기 표시명: `SkillSetupEditor.cs`의 `"SpeedUp"`
- 내부 식별자: `PlayerActiveSkillType.SpeedUp`
- 내부 코루틴/필드: `CoSpeedUp`, `_speedUpCoroutine`
- 파일명: `PlayerActiveSkillData_SpeedUp.asset`
- Setup Editor 내부 오브젝트명: `SpeedUpButton`
- 설계/상태/히스토리/UI 문서의 `스피드업`

기능은 “6초 동안 모든 볼 속도 1.5배”이므로 `스피드업`이 실제 효과를 더 정확히 설명한다. 사용자는 표시명뿐 아니라 코드, 데이터 파일, Setup Editor, 씬 오브젝트, 문서의 명칭을 모두 변경하기로 확정했다.

- 한국어 표시명은 `스피드업`으로 통일
- 영문 식별자는 `SpeedUp`으로 통일
- camelCase 식별자는 `speedUp`으로 통일
- 데이터 파일은 `PlayerActiveSkillData_SpeedUp.asset`으로 통일
- enum/메서드/필드/버튼 오브젝트명도 동일 기준으로 변경

Unity 에셋 파일은 `.asset`과 `.meta`를 함께 이동해 GUID를 유지하고, 기존 씬과 프리팹의 참조가 끊기지 않도록 해야 한다.

## AI 활용 내역 확정

- Codex와 Claude/Claude Code를 코드 조사, 설계, 구현, 디버깅, 문서화 지원에 사용했다.
- GPT로 제작한 에셋은 플레이어 액티브 스킬 버튼 이미지 2개뿐이다.
  - `Assets/_Project/Sprites/SpeedUp.png`
  - `Assets/_Project/Sprites/Copy.png`
- 나머지 게임 리소스를 GPT 제작물로 잘못 표기하지 않는다.
- 최종 의사결정, Unity 적용, Android 실제 기기 20웨이브 플레이 검증은 사용자가 수행했다.

## 관련 파일

- `Assets/_Project/Scripts/UI/HUDPanel.cs`
- `Assets/_Project/Scripts/Editor/UIOverhaulSetupEditor.cs`
- `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`
- `Assets/_Project/Data/PlayerActiveSkillData_SpeedUp.asset`
- `Assets/Scenes/SampleScene.unity`
- `README.md` (신규)
- `Assets/_Project/Docs/PlayerActiveSkillDesign.md`
- `Assets/_Project/Docs/UIRules.md`
- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`

## 주의사항

- `SampleScene.unity`에는 사용자가 완료한 실제 UI/게임 설정이 있으므로 S/F 관련 GameObject와 참조만 제한적으로 제거한다.
- S/F 제거 후 성공 조건인 20웨이브 클리어와 실패 조건인 HP 0 코드는 변경하지 않는다.
- README에는 실제 구현·검증된 내용만 기록하고 예정된 TODO Polish 항목을 완료 기능처럼 작성하지 않는다.
- 루트의 기존 `apk.apk`는 이번 작업에서 삭제하거나 교체하지 않는다.
- `.claude/settings.local.json`은 사용자 로컬 설정이므로 건드리지 않는다.

## 결론

제출 전 필수 정리 대상은 S/F 테스트 버튼의 코드·생성기·씬 잔재 제거, `SpeedUp/스피드업` 명칭의 코드·데이터·씬·문서 전체 변경, README 신규 작성이다. README는 한국어 중심으로 작성하고 공식 요구사항, 실제 구현 범위, 기술적 특징, AI 활용 내역, Android 실기기 20웨이브 검증을 한눈에 확인할 수 있도록 구성한다.
