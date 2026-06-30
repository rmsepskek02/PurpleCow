## 2026-06-30

### 작업 내용
- TaskRules.md 폴더명 포맷 HH:MM → HH-MM 으로 수정 (Windows 콜론 제약)
- Core 시스템 task 문서 생성: research.md, plan.md

### 결과
- Assets/_Project/Docs/_Task/2026-06-30/02-30_Core시스템구현/ 폴더 및 문서 생성 완료

### 주요 결정사항
- Windows에서 폴더명 콜론 사용 불가 → HH-MM 형식 채택

---

### 작업 내용 (추가)
- ProjectHistory.md에 2026-06-30 상세 섹션 추가 (에이전트 시스템, 문서 시스템, 아키텍처 결정, Core task 문서 현황)
- ProjectStatus.md 전면 갱신 (현재 상태, 완료 체크리스트, 기술 결정 표, 리소스 현황)

### 결과
- 두 문서 모두 업데이트 완료

### 주요 결정사항
- ProjectHistory.md는 기존 내용 유지 + 신규 섹션 추가 방식으로 편집

---

### 작업 내용 (추가)
- UIRules.md 신규 생성 (Assets/_Project/Docs/UIRules.md)
- AGENTS.md Docs 문서 인덱스에 UIRules.md 등록

### 결과
- UIRules.md 생성 완료: Canvas 구조/레이어, 해상도 대응, Safe Area, 패널 표시/숨김, UI 애니메이션, 버튼 피드백, 성능 최적화 7개 섹션 포함
- AGENTS.md 인덱스 업데이트 완료

### 주요 결정사항
- DevRules.md 스타일(마크다운 테이블, 코드블록, 한국어 설명) 일관성 유지
- 사용자 확정 내용 그대로 반영, 임의 추가 없음

---

### 작업 내용 (추가)
- 플레이어 액티브 스킬 시스템 task 문서 생성: research.md, plan.md
- 경로: Assets/_Project/Docs/_Task/2026-06-30/09-12_플레이어액티브스킬시스템/

### 결과
- research.md: 현재 상태(Scripts/Skill/Active|Passive|Base 폴더 존재, 스크립트 없음), 의존성 분석, 4종 스킬 요구사항, SO 설계, 구현 범위 제한 정리
- plan.md: Step1 인터페이스(IFreezable, ISpeedModifiable, IDamageable) → Step2 PlayerActiveSkillSO → Step3 PlayerActiveSkillController → Step4 SkillButtonUI → Step5 SO 에셋 4종 순서로 단계별 계획 수립

### 주요 결정사항
- 볼/몬스터 미구현 상태이므로 인터페이스 기반으로 연결 지점만 확보, 실제 연결은 해당 클래스 구현 시점에 처리
- PlayerActiveSkillController는 MonoBehaviour로 구현 후 Core 완료 시 Singleton 상속으로 교체 예정
- 씬 내 오브젝트 수집 로직을 별도 private 메서드로 분리하여 추후 정적 리스트 방식으로 교체 용이하게 설계
- 시각 이펙트는 이번 구현 제외, 이펙트 훅만 남기는 것으로 확정
