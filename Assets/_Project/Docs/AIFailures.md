# AIFailures.md

이 문서는 AI가 작업 중 발생한 실패 사례를 기록합니다. 동일한 실수가 반복되지 않도록 원인과 재발 방지 방법을 함께 기록합니다.

---

## 실패 기록 형식

```
### [날짜] 실패 요약
- **상황**: 어떤 작업 중 발생했는지
- **실패 내용**: 무엇을 잘못했는지
- **원인**: 왜 실패했는지
- **재발 방지**: 앞으로 어떻게 해야 하는지
```

---

## 2026-06-30

### 한글 경로 리소스 복사 실패
- **상황**: 리소스 폴더를 Assets로 복사하는 작업
- **실패 내용**: PowerShell과 Bash 모두 한글 경로 인코딩 문제로 복사 실패
- **원인**: Windows 환경에서 한글 폴더명 인코딩 불일치
- **재발 방지**: 한글 폴더명 사용 금지, 영문 폴더명으로 작업

### orchestrator 에이전트 백그라운드 완료 대기 실패
- **상황**: orchestrator가 docs 에이전트를 호출한 후 완료 대기
- **실패 내용**: 백그라운드 에이전트 완료 알림을 orchestrator가 받지 못하고 종료
- **원인**: 백그라운드 완료 알림은 메인 세션(Claude)에게만 전달되는 구조
- **재발 방지**: Claude가 orchestrator 역할을 직접 수행, 에이전트는 docs/dev/qa/design 4개만 운영

### Claude가 docs 에이전트 작업을 직접 처리
- **상황**: DevRules.md 문서 업데이트 작업
- **실패 내용**: docs 에이전트에게 위임하지 않고 Claude가 직접 파일 수정
- **원인**: 에이전트 운영 구조 규칙이 미확립된 상태
- **재발 방지**: CLAUDE.md에 에이전트 운영 구조 명시, 문서 작업은 반드시 docs 에이전트에게 위임

---

## 2026-07-01

### 에디터 스크립트 씬 자동 저장 누락
- **상황**: UISetupEditor, SceneSetupEditor에서 씬 오브젝트 참조(BallLauncher._launchPoint, HUDPanel._waveText 등) 연결 후 `AssetDatabase.SaveAssets()`만 호출
- **실패 내용**: 씬 오브젝트 변경이 디스크에 저장되지 않아 재실행 시 참조가 초기화됨
- **원인**: `AssetDatabase.SaveAssets()`는 .asset, .prefab 등 에셋 파일만 저장하며 씬(.unity) 파일은 저장하지 않음
- **재발 방지**: 씬 오브젝트를 수정하는 에디터 스크립트는 반드시 마지막에 `EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene())` 호출

### 카메라 orthographic size 미설정
- **상황**: SceneSetupEditor에서 카메라 orthographic size 설정 누락
- **실패 내용**: 기본값 5로 방치 → 플레이 영역(x:±5.5, y:-10~+8)의 약 1/4만 보임, 배경만 표시되는 것처럼 보이는 문제 발생
- **원인**: 에디터 스크립트가 카메라 설정을 포함하지 않았고, 플레이 영역 좌표 검토 시 카메라 시야 범위를 함께 확인하지 않음
- **재발 방지**: SceneSetupEditor 실행 시 카메라 orthographic size를 플레이 영역에 맞게 설정 (현재 프로젝트: 1080x1920, 플레이 영역 width 11 → orthographic size = 10)
