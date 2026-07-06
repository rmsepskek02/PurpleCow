# Plan - Skill Effects and Character Progression

확정된 기준에 따라 캐릭터 최대 레벨을 19로 확장하고, 필요 경험치를 50부터 레벨마다 18씩 증가시킨다. 동시에 `SkillData`의 런타임 변경을 제거하고, 삼택지에서 획득한 스킬 레벨·특수 볼 외형·기본 데미지·고유 효과가 실제 전투에 일관되게 적용되도록 스킬 런타임 구조를 정리한다.

## 구현 목표

- 캐릭터 레벨 범위를 Lv.1~Lv.19로 확장한다.
- 18번의 레벨업을 통해 액티브 4개 + 패시브 2개를 모두 Lv.3까지 성장시킬 수 있게 한다.
- Lv.2 필요 XP는 50, 이후 레벨마다 18씩 증가시킨다.
- 신규 액티브 스킬 획득 시 해당 전용 Sprite를 사용하는 특수 볼 한 개를 로스터에 추가한다.
- 액티브 볼은 현재 스킬 레벨의 `BallDamage`를 실제 기본 공격력으로 사용한다.
- 공식 PDF에 정의된 액티브/패시브 10종의 수치 효과를 실제 전투 계산에 적용한다.
- `SkillData`는 읽기 전용 정의로 유지하고 보유 여부와 현재 레벨은 런타임 상태로 분리한다.
- 삼택지 카드의 `New!`, 다음 레벨, 다음 레벨 공격력 표시를 실제 선택 결과와 일치시킨다.

## 단계별 작업 계획

### 1. 캐릭터 경험치 곡선과 최대 레벨 확장

`CharacterManager`의 3칸 배열 방식을 다음 진행 규칙으로 교체한다.

- 시작 레벨: Lv.1
- 최대 레벨: Lv.19
- Lv.2 필요 XP: 50
- 다음 레벨 필요 XP 증가량: 18
- 계산식: `50 + (현재 레벨 - 1) × 18`
- Lv.19에서는 `RequiredXp == 0`으로 처리하고 추가 경험치는 받지 않는다.
- 한 번에 많은 XP를 받았을 때 기존처럼 여러 번 레벨업할 수 있도록 반복 처리는 유지한다.
- 각 레벨 상승마다 `OnLevelUp`을 한 번씩 발행해 삼택지를 총 18번 연다.

예상 필요 경험치는 `50, 68, 86, ... , 338, 356`이며 누적 3,654 XP다.

### 2. 스킬 런타임 상태 분리

`SkillData.cs`에 별도 런타임 클래스 `SkillRuntimeState`를 추가한다.

- 읽기 전용 `SkillData` 참조
- 0부터 시작하는 현재 레벨 인덱스
- 표시용 현재 레벨(1~3)
- 현재 레벨 수치
- 최대 레벨 여부
- 안전한 1단계 레벨업 메서드

`SkillData`에서는 `_currentLevel`, `LevelUp()`, `ResetLevel()`을 제거한다. 스킬명, 설명, 아이콘, 전용 볼 Sprite, 레벨별 수치만 보관한다.

`BallSkillBase`와 `PassiveSkillBase`는 `SkillData` 대신 같은 `SkillRuntimeState`를 공유한다. 이 구조로 다음을 보장한다.

- 이미 비행 중인 특수 볼도 스킬 업그레이드 직후 최신 레벨 수치를 참조한다.
- ScriptableObject 에셋을 런타임에 변경하지 않는다.
- 씬 재시작 시 새 `SkillManager`와 함께 상태가 자연스럽게 초기화된다.

### 3. SkillManager 획득·업그레이드 흐름 정리

`SkillManager`가 신규 획득과 레벨업을 직접 관리하도록 API를 변경한다.

- 액티브 신규 획득: `SkillRuntimeState` 생성, 액티브 목록에 추가, 신규 여부 반환
- 액티브 재선택: 기존 상태를 Lv.3 한도 내에서 1단계 상승
- 패시브 신규 획득: 상태와 효과 인스턴스 생성 후 적용
- 패시브 재선택: 기존 효과 제거 → 상태 레벨업 → 최신 수치로 재적용
- 신규 획득과 레벨업 양쪽 모두 변경 이벤트 발행
- 액티브 4개, 패시브 2개 제한 유지
- 보유 상태 조회와 현재 레벨 조회 API 제공

기존 전역 `_nextShotDamageBonus`는 제거한다. 마법 거울 보너스는 각 `Ball` 인스턴스가 보관한다.

패시브 계산용 런타임 값은 의미에 맞게 분리한다.

- 노멀 볼 추가 피해 배율
- 전면 타격 추가 치명타 확률
- 후면 타격 추가 치명타 확률

### 4. 특수 볼 Sprite와 기본 데미지 연결

`SkillData`에 전용 볼 Sprite 필드를 추가하고 액티브 5종 에셋에 다음 리소스를 연결한다.

- Fire → `Ball_Fire_ball.png`
- Ice → `Ball_Ice_Ball.png`
- Ghost → `Ball_Ghost_Ball.png`
- Laser → `Ball_Laser_Ball.png`
- Cluster → `Ball_Cluster_Ball.png`

`Ball`은 기본 노멀 Sprite를 캐시하고 스폰 시 모든 임시 상태를 초기화한다.

- 노멀 로스터 볼: `BallData.Damage` 8, 노멀 Sprite
- 특수 로스터 볼: 공유 `SkillRuntimeState.CurrentLevelData.BallDamage`, 전용 Sprite
- 분신 볼: 원본 로스터 볼의 상태와 Sprite 유지
- 클러스터 서브 볼: 클러스터 Sprite와 10/15/20 고정 피해 사용
- 풀 반환 후 재사용 시 이전 특수 Sprite, 고정 피해, 마법 거울 보너스가 남지 않도록 초기화

`BallLauncher`의 로스터 항목은 단순 `SkillData` 대신 해당 액티브의 `SkillRuntimeState`를 참조하도록 변경한다. 기존 규칙대로 신규 액티브 획득 때만 볼 수가 한 개 증가하고, 재선택은 같은 볼의 레벨만 올린다.

### 5. 실제 데미지·치명타 계산 순서 수정

`Ball.CalculateDamage()`를 다음 순서로 정리한다.

1. 노멀/특수/서브 볼에 맞는 기본 피해 결정
2. 충돌 진행 방향으로 전면/후면 판정
3. 기본 치명타 확률에 해당 방향 단검 보너스 추가
4. 치명타 판정과 치명타 배율 적용
5. 노멀 볼인 경우에만 따뜻한 양철 심장 배율 적용
6. 해당 볼에 쌓인 마법 거울 다음 타격 배율 적용 후 소비
7. 최종 피해 적용 및 이벤트 발행

이 순서로 자수정/에메랄드 단검이 “다음 타격”이 아니라 현재 전면/후면 타격의 치명타 확률을 올리도록 수정한다. 고스트 볼의 트리거 충돌도 같은 판정 경로를 사용한다.

### 6. 액티브 스킬 5종 효과 보정

#### 파이어 볼

- 타격마다 화상 스택을 정확히 1개 추가한다.
- 각 스택은 획득 당시 레벨의 DPS와 지속시간을 가진다.
- 최대 스택은 현재 레벨 기준 3/4/5개다.
- 최대 상태에서 다시 타격하면 가장 오래된 스택을 새 스택으로 갱신한다.
- 1초마다 살아 있는 스택의 DPS 합계를 피해로 적용한다.
- 몬스터 스폰/디스폰 시 화상 스택과 타이머를 모두 제거해 풀 재사용 오염을 막는다.

#### 아이스 볼

- 30/35/40% 확률 판정 유지
- 냉동 5/6/7초 적용
- 이동속도 10/15/20% 감소 적용
- 실제 특수 볼 피해의 10/15/20%를 추가 피해로 적용

냉동/감속 타이머는 같은 프레임 시간으로 함께 감소시켜 냉동 종료 후 감속 시간이 다시 처음부터 남는 현상을 방지한다.

#### 레이저 볼

- 충돌 대상과 같은 행의 다른 모든 살아 있는 몬스터에게 7/11/15 추가 피해 적용
- 충돌 대상은 특수 볼 기본 피해만 받고 같은 행 추가 피해가 중복되지 않도록 유지

#### 고스트 볼

- 몬스터 충돌 시 관통 유지
- 트리거 충돌에서도 기본 피해, 단검 치명타, 마법 거울, 고유 효과가 일반 볼과 동일한 순서로 처리되도록 통합

#### 클러스터 볼

- 적 타격 시 40/50/60% 확률로 서브 볼 한 개 생성
- 서브 볼 피해는 10/15/20
- 서브 볼은 클러스터 Sprite를 사용
- 서브 볼에는 클러스터 스킬을 다시 부여하지 않아 연쇄 생성을 막음

### 7. 패시브 스킬 5종 효과 보정

#### 따뜻한 양철 심장

- 노멀 로스터 볼과 노멀 분신 볼에만 20/30/40% 추가 피해 적용
- Fire/Ice/Ghost/Laser/Cluster 및 클러스터 서브 볼에는 적용하지 않음

#### 마법 거울

- 벽에 충돌한 해당 볼에만 다음 타격 보너스를 20/40/60% 누적
- 해당 볼이 다음에 몬스터를 타격할 때 한 번에 소비
- 다른 볼이 보너스를 가져갈 수 없게 함
- 풀 반환 또는 새 스폰 시 남은 보너스 초기화

#### 자수정 단검 / 에메랄드 단검

- 자수정: 현재 전면 타격의 치명타 확률 +10/20/30%
- 에메랄드: 현재 후면 타격의 치명타 확률 +20/30/40%
- 몬스터에 다음 타격용 치명타 상태를 저장하던 기존 방식 제거

#### 마지막 성냥

- 몬스터 사망 시 반경 내 다른 살아 있는 몬스터에게 10/20/30 피해
- 기존 폭발 연쇄 처리는 유지
- 자기 자신과 이미 죽은 몬스터는 제외

### 8. 삼택지와 장착 슬롯 표시 수정

후보 생성 시 `SkillRuntimeState` 보유 여부를 기준으로 판정한다.

- 미보유 스킬: `New!`, `Lv.1`, Lv.1 수치 표시
- 보유 Lv.1 스킬: `Lv.2`, Lv.2 수치 표시
- 보유 Lv.2 스킬: `Lv.3`, Lv.3 수치 표시
- Lv.3 스킬: 후보에서 제외
- 슬롯 표시: 현재 실제 레벨 표시
- 신규/업그레이드 모두 슬롯 UI 즉시 갱신

총 18번째 선택 시 남은 후보가 3개보다 적을 수 있으므로 기존처럼 가능한 카드만 노출한다. 마지막 선택 후 모든 장착 스킬이 Lv.3이면 XP가 최대 레벨에서 정지한다.

### 9. 에디터 데이터 갱신

`SkillSetupEditor`가 기존 에셋도 안전하게 갱신하도록 보완한다.

- 공식 PDF의 기존 수치 유지
- 액티브 5종 전용 볼 Sprite 연결
- `SkillData`의 제거된 런타임 레벨 필드에 의존하지 않도록 수정

실제 `SkillData_*.asset`에도 전용 볼 Sprite 참조를 반영해 별도 메뉴 실행 전에도 프로젝트 데이터가 완성된 상태가 되게 한다.

## 검증 계획

### 정적 검증

- `Assembly-CSharp.csproj` 빌드
- `Assembly-CSharp-Editor.csproj` 빌드
- `git diff --check`
- `SkillData`에 런타임 변경 필드/메서드가 남지 않았는지 검색
- 액티브 5종 에셋의 전용 볼 Sprite 참조 확인

### Unity 플레이 검증

1. 게임 시작 직후 삼택지가 열리지 않는지 확인
2. 기본 몬스터 약 5마리 분량 XP 후 첫 레벨업이 발생하는지 확인
3. 신규 액티브 선택 시 전용 외형의 볼 한 개가 추가되는지 확인
4. 같은 액티브 재선택 시 볼 개수는 유지되고 Lv.2/Lv.3 피해만 상승하는지 확인
5. 각 액티브 5종의 발동 확률·피해·관통·같은 행·화상 동작 확인
6. 각 패시브 5종이 PDF 설명과 같은 대상/시점/배율로 적용되는지 확인
7. 삼택지 카드와 상단 슬롯의 New/레벨/공격력 표시 확인
8. 최대 Lv.19 이후 XP가 더 이상 증가하지 않고 삼택지가 열리지 않는지 확인
9. 재시작 후 캐릭터 레벨, 보유 스킬, 특수 볼 로스터가 모두 초기화되는지 확인

확률 효과와 Lv.19 도달은 일반 플레이만으로 검증 시간이 길 수 있다. 필요하면 기존 S/F 버튼과 같은 방식의 에디터/Inspector 테스트 경로를 사용하되, 제출 빌드에 새로운 디버그 UI는 추가하지 않는다.

## 예상 변경·생성 파일 목록

### 수정 - 진행/데이터

- `Assets/_Project/Scripts/Core/CharacterManager.cs`
- `Assets/_Project/Scripts/Data/SkillData.cs` (`SkillRuntimeState` 포함)
- `Assets/_Project/Scripts/Editor/SkillSetupEditor.cs`
- `Assets/_Project/Data/SkillData_Fire.asset`
- `Assets/_Project/Data/SkillData_Ice.asset`
- `Assets/_Project/Data/SkillData_Ghost.asset`
- `Assets/_Project/Data/SkillData_Laser.asset`
- `Assets/_Project/Data/SkillData_Cluster.asset`

### 수정 - 스킬/볼/몬스터

- `Assets/_Project/Scripts/Skill/SkillManager.cs`
- `Assets/_Project/Scripts/Skill/SkillFactory.cs`
- `Assets/_Project/Scripts/Skill/Base/BallSkillBase.cs`
- `Assets/_Project/Scripts/Skill/Base/PassiveSkillBase.cs`
- `Assets/_Project/Scripts/Skill/Active/FireBallSkill.cs`
- `Assets/_Project/Scripts/Skill/Active/IceBallSkill.cs`
- `Assets/_Project/Scripts/Skill/Active/GhostBallSkill.cs`
- `Assets/_Project/Scripts/Skill/Active/LaserBallSkill.cs`
- `Assets/_Project/Scripts/Skill/Active/ClusterBallSkill.cs`
- `Assets/_Project/Scripts/Skill/Passive/WarmTinHeartPassive.cs`
- `Assets/_Project/Scripts/Skill/Passive/MagicMirrorPassive.cs`
- `Assets/_Project/Scripts/Skill/Passive/AmethystDaggerPassive.cs`
- `Assets/_Project/Scripts/Skill/Passive/EmeraldDaggerPassive.cs`
- `Assets/_Project/Scripts/Skill/Passive/LastMatchPassive.cs`
- `Assets/_Project/Scripts/Ball/Ball.cs`
- `Assets/_Project/Scripts/Ball/BallLauncher.cs`
- `Assets/_Project/Scripts/Monster/MonsterBase.cs`

### 수정 - UI

- `Assets/_Project/Scripts/UI/SkillSelectionPanel.cs`
- `Assets/_Project/Scripts/UI/SkillCardUI.cs`
- `Assets/_Project/Scripts/UI/SkillSlotGroup.cs`

### 구현 완료 후 문서 갱신

- `Assets/_Project/Docs/ProjectStatus.md`
- `Assets/_Project/Docs/ProjectHistory.md`

## 주의사항

- 현재 작업 트리의 UI, 씬, 폰트, 문서 변경은 사용자 작업이므로 덮어쓰거나 되돌리지 않는다.
- 기존 UI 레이아웃과 모든 텍스트 폰트 설정은 이번 범위에서 변경하지 않는다.
- 보스, 선택지 다시 뽑기, 융합 시스템은 공식 PDF 제외 항목이므로 추가하지 않는다.
- 플레이어 직접 발동 스킬인 버서크/분신 시스템은 로그라이크 액티브 볼 스킬과 별개이므로 수정하지 않는다.
- 공식 PDF 수치 자체는 변경하지 않고, 현재 연결이 빠졌거나 계산 방식이 잘못된 부분만 바로잡는다.
- 확률은 0~1, 퍼센트 추가 피해는 곱셈 배율, 고정 추가 피해는 절대값으로 구분한다.
- 물리 충돌의 전면/후면 기준은 현재 프로젝트에서 사용 중인 볼 이동 방향 기준을 유지하되, 치명타 판정 전에 계산한다.
