# REC

Unity 6 기반 3D 공포 게임입니다. 플레이어는 CCTV로 이상현상을 확인하고 현장을 순찰하며 사물을 복구합니다. 사물·이상현상·미션·일정·회차는 CSV로 정의하고, 실행 상태와 Unity 표현을 분리합니다.

현재 **InGame 씬에 코어 기능과 1~4회차 연출을 연결하여 플레이 확인 중**입니다. TestScene은 독립 코어 검증용으로 유지합니다. 이 문서는 2026-09-23 현재 코드와 사용자 확인 내용을 기준으로 하며, 구현·플레이 확인·예정 작업을 구분합니다.

## 1. 현재 상태와 먼저 확인할 사항

| 항목 | 현재 상태 |
|---|---|
| Main Menu → 카드 선택 → InGame | 사용자 연결 확인 |
| 데이터 기반 이상현상·사물 복구·문 상호작용 | InGame 연결 구현 |
| 복구 홀드 사운드 / 23:30 화면 노이즈 | 사용자 작동 확인 |
| 1회차 냉장고 문 연출 | 사용자 작동 확인 |
| 2회차 관 복구·뒤쪽 귀신·인지 시 조작 제한 | 구현 및 설정 가이드 제공 |
| 3회차 스탠딩 귀신·피 글씨·점멸·돌진·셰이크 | 구현, 사용자 배치 및 플레이 확인 진행 |
| 3회차 귀신 소멸 변경 | 해결 즉시 스탠딩 숨김, 돌진 종료 즉시 돌진 귀신 숨김으로 수정 |
| 4회차 03:30 귀신·접근 사운드·퇴장 | 미션과 독립하도록 수정, 사용자 작동 확인 |
| 임시 엔딩 / 영상 교체 슬롯 | 구현, 미리보기 가능 |
| 발소리 | 실제 이동 판정 및 전용 재생 코드 반영, 청취 확인 필요 |
| 환경음 | 플레이어 시점 재생 / CCTV 확인 중 일시정지 코드 반영 |
| 일반 문 열림·닫힘 소리 | 토글 상호작용·동작 이벤트·3D 사운드 연결 반영 |
| 모든 미션 해결 → 영상 → Main Menu | **전환 작업 진행 중, 아직 완성 아님** |

### 현재 코드의 중요 주의사항

문서 작성 시 확인한 실제 상태입니다. 이번 문서 작업에서는 코드를 수정하지 않았습니다.

1. `GameSession.RequestResolve`는 마지막 회차의 모든 배정 미션 해결 시 `Ending`으로 전환하도록 바뀌었습니다.
2. `Publish("MissionResolved", mission)` 발행은 현재 코드에 복원되어 있습니다. 연출이 실행되지 않으면 해결 로그의 Round/Quest/Target ID와 Inspector 필터를 비교하세요.
3. `RoundFourPresentation.Handle`에는 아직 **Quest 28 필터**가 있습니다. 다른 미션이 마지막으로 해결되면 엔딩 상태에는 진입하지만 영상이 시작되지 않을 수 있습니다. `EndingStarted` 수신을 특정 미션 ID와 분리해야 합니다.
4. `InGameSessionController`의 완료 목적지는 아직 **GameClear**입니다. 최종 목표인 **Main Menu (Desktop)**으로의 변경은 남아 있습니다.
5. 최신 CSV에서 영정사진 Quest 28은 활성화되어 있으며 Target 4가 I_14에 연결되어 있습니다. 4회차 귀신 접근 연출은 이 미션과 관계없이 03:30에 활성화됩니다.

최종 목표는 **마지막 회차의 모든 배정 미션 해결 → 엔딩 영상(없으면 임시 화면) → Main Menu (Desktop)**입니다. GameClear 씬을 최종 동선에 사용하지 않을 계획이며, 내부 완료 상태·이벤트 이름은 씬 이름과 별개로 유지할 수 있습니다.

## 2. 개발 환경

| 항목 | 버전 / 기준 |
|---|---|
| Unity | 6000.2.8f1 |
| C# | 별도 컴파일 검사 기준 9.0 |
| URP | 17.2.0 |
| Cinemachine | 3.1.7 |
| Input System | 1.14.2 |
| UI | uGUI / 프로젝트 UI, 테스트 도구 별도 |
| 독립 규칙 검사 | .NET 10 SDK, PowerShell |

버전 기준은 `ProjectSettings/ProjectVersion.txt`와 `Packages/manifest.json`입니다. 외부 .NET 검사 환경과 Unity 런타임은 구분합니다.

## 3. 실행과 조작

### InGame에서 확인

1. Unity에서 `Assets/1.Scene/InGame.unity`를 엽니다.
2. InGameCore의 Bootstrapper 및 회차별 Presentation 컴포넌트, GameDataRoot, Player, Main Camera, CameraManager 연결을 확인합니다.
3. 편집 모드에서 앵커·음원·참조를 저장한 뒤 Play합니다.
4. CCTV 확인 → 현장 순찰을 진행하고, 데이터에 배정된 이상현상을 해결합니다.

메뉴부터 확인할 때는 `Assets/1.Scene/Main Menu (Desktop).unity`를 사용합니다. TestScene의 자동 테스트 방 구성은 실제 InGame 맵과 별개입니다.

| 입력 | 동작 |
|---|---|
| WASD / 마우스 | 이동 / 시점 회전 |
| 마우스 왼쪽 버튼 홀드 | 활성 이상현상 복구, 기본 2초 |
| 마우스 왼쪽 버튼 단발 | 데이터에 등록된 일반 문 상호작용 |
| Tab | 허용된 조건에서 CCTV실 복귀 요청 |
| N / 다음 일정으로 | 허용된 상태에서 다음 일정 시각으로 진행 |
| Esc | 일시정지 메뉴 |

복구 홀드 시간은 설정값이며 향후 변경할 수 있습니다. 홀드를 중단하면 진행률을 0으로 초기화하고 복구 소리를 즉시 정지합니다. 화면의 버튼과 실제 입력 허용 여부는 세션 단계 및 연출 잠금에 따릅니다.

### 특정 회차 테스트

Play 중 InGameCore의 GameSessionHost에서 Test Round Id를 지정한 뒤 컴포넌트 메뉴의 **테스트 → 지정 회차 시작**을 실행합니다. 회차는 22:00부터 다시 시작합니다. 이후 CCTV 확인과 현장 순찰을 진행합니다.

`다음 일정으로`는 임의의 모든 연출 시각으로 이동하는 버튼이 아닙니다. 다음 미션 일정 등 현재 세션이 허용하는 목적지로 이동합니다.

## 4. 시간·미션·회차 규칙

- 미션은 회차 시작 시 고정 배정 및 가중치 비복원 추첨으로 결정됩니다.
- 각 미션의 `RealTime`에 사건을 활성화합니다. 이전 미해결 사건은 유지합니다.
- 아직 열리지 않은 배정 사건도 전체 완료 조건에 포함됩니다.
- 23:00: 초기 순찰 상태라면 강제 복귀 이벤트로 CCTV실 단계에 진입합니다.
- 23:30: 관측 가능 시각 및 공통 화면 노이즈 이벤트가 연결되어 있습니다. 노이즈는 화면 전체를 덮되 조작 UI를 가리지 않도록 별도 레이어로 표시합니다.
- 06:00: 미완료 미션이 남으면 게임오버입니다.
- 일반 회차는 전체 해결 후 최종 복귀를 거쳐 다음 회차로 진행합니다.
- 마지막 회차는 전체 해결 후 엔딩으로 전환하는 작업 중입니다. 현재 주의사항은 1절을 참고하세요.
- 다음 회차는 `Round.NextRoundID`로 결정합니다. `0`은 마지막 회차입니다.

기본 시작 시각은 22:00, 실제 2.5초가 게임 1분입니다. `RealTime`은 회차 시작 후 경과 밀리초입니다.

| 경과 ms | 게임 시각 |
|---:|---|
| 0 | 22:00 |
| 150000 | 23:00 |
| 225000 | 23:30 |
| 525000 | 01:30 |
| 825000 | 03:30 |
| 1200000 | 06:00 |

`GameSessionHost`가 경과 시간을 누적해 `GameSession.Tick`으로 전달합니다. 일시정지·포커스 해제·엔딩 잠금 시에는 시간이 멈춥니다. 연출 문서의 예정 시각을 맞추기 위해 CSV 시간을 임의 변경하지 않습니다.

## 5. 데이터와 맵 연결

### 로딩 흐름

```text
GameDataCatalog → CSVParser → CsvMapper → 임시 DataManager
  → ID·형식·범위·참조 검증 → Seal → 세션 초기화
```

현재 로더의 필수 테이블은 8개입니다.

| 테이블 | 책임 |
|---|---|
| Anomaly | 이상현상과 발생·해결 행동 |
| Detail | 대상과 세부 설정 연결 |
| MovementParameter | 이동·회전 종류, 축, 값, 단위 |
| TargetCodes | 대상 ID 및 씬 사물 연결 |
| Quest | 미션 후보, 시각, 가중치, 활성 상태 |
| Schedule | 일정과 배정 수 |
| Round | 회차 구성과 다음 회차 |
| Door | 일반 문 연결과 동작 설정 |

Direction 계열 등 폴더에 존재하는 모든 CSV를 자동으로 실행하는 구조는 아닙니다. 실제 로더 등록과 카탈로그 연결을 확인해야 합니다.

### 작성 및 연결 원칙

- 안정적인 양수 ID, 중복 ID, 필수 값, 타입·범위 및 교차 참조를 검증합니다.
- 등록되지 않은 사물에 상호작용이나 미션을 임의로 붙이지 않습니다.
- 기본 상호작용 대상은 Interaction 하위 I_1~I_13 계열이며, I_7은 I_7_2만 허용합니다. 사물의 하위 메시마다 개별 상호작용을 붙이는 방식이 아닙니다.
- Bootstrapper의 Additional Target Bindings는 추가 경로 허용 목록입니다. 경로만 추가했다고 미션이 생성되는 것은 아니며, 활성 데이터 행과 일치해야 합니다.
- 일반 문은 Door 테이블로 연결합니다. 연출용 냉장고 문은 별도 Presentation으로 제어합니다.
- Offset의 기획 단위는 cm입니다. 예: 50cm는 Unity 0.5m입니다. 회전은 도 단위이며, 입력 데이터의 단위 표현과 검증 규칙도 함께 확인해야 합니다.
- 미완성 미션 활성화 시 Quest뿐 아니라 Schedule·Round의 배정 수와 Target 연결도 함께 검토합니다.

최신 저장된 CSV와 InGame 씬의 고정 사건 연결은 다음과 같습니다. ID는 영구 고정 규칙이 아니라 현재 콘텐츠 값입니다.

| 회차 | Quest ID | Target ID | 시각 / RealTime | 대상 |
|---|---:|---:|---|---|
| 1 | 4 | 1 | 01:30 / 525000 | I_1 꽃병 |
| 2 | 11 | 2 | 01:30 / 525000 | I_2/I_2_2 관 뚜껑 |
| 3 | 18 | 3 | 01:30 / 525000 | I_3 베개 |
| 4 | 28 | 4 | 03:30 / 825000 | I_14 영정사진 |

네 고정 사건 모두 현재 CSV에서 Enabled=1입니다. 3회차 이불 `SYNC_04__UNI_02_FoldedBlanket`은 귀신 배치 기준이며, 실제 복구 대상은 현재 I_3 베개입니다. 기존 상세 가이드에 적힌 Quest 1/8/15 및 비활성 영정사진 설명은 이전 데이터 기준입니다.

### ID·시각 변경 시 확인

1. Quest의 `RealTime`은 발생 시각이고, `ID`는 연출 매칭에 쓰는 식별자입니다. 시간을 바꾼다고 ID를 반드시 바꿀 필요는 없습니다.
2. Quest ID나 Target ID를 변경하면 참조 테이블과 Inspector의 해당 필터도 함께 맞춥니다. 코드의 필드 기본값보다 저장된 씬의 직렬화 값이 우선합니다.
3. 1회차는 `DoorOpenPresentation`이 부모 `PresentationEffect.Matches`의 Event/Round/Quest/Target 필터를 사용합니다. 고정 미션(SpawnType=0)을 자동 식별하는 방식은 아직 아닙니다.
4. `InGameCore → Presentation Director → Effects`에서 문 연출 참조를 찾아 대상 컴포넌트로 이동할 수 있습니다. 1회차 현재 값은 MissionResolved / Round 1 / Quest 4 / Target 1입니다.
5. ID 필터를 모두 0으로 풀면 랜덤 사건 해결에도 발동할 수 있으므로 고정 사건 연결을 대신하는 방법으로 사용하지 않습니다.
6. CSV 저장 및 Unity 임포트 후 Play를 새로 시작합니다. 실행 중 기존 DataManager가 자동으로 CSV를 재로드하지 않습니다.

## 6. 구조와 책임

```text
Assets/
├─ 1.Scene/                 # Main Menu (Desktop), InGame, TestScene, 기존 결과 씬
├─ 2.Model/Prefabs/Obj/     # 맵 사물과 문 프리팹
├─ 3.Script/
│  ├─ Data/                 # 행 정의, 파싱·매핑, 저장소, 검증
│  ├─ Audio/                # 복구·발소리·환경음·문 소리
│  ├─ InGame/
│  │  ├─ Core/              # 세션·미션·추첨·이상현상 규칙
│  │  ├─ Session/           # Bootstrapper, Host, Controller, HUD, 맵 초기화
│  │  ├─ Interaction/       # 조준, 홀드, 프롬프트, 하이라이트
│  │  ├─ Presentation/      # 회차 연출, 귀신, 트리거, 화면 효과, 엔딩
│  │  ├─ Camera/            # CCTV / 플레이어 카메라
│  │  ├─ Player/            # 이동·시점·조작 제한
│  │  ├─ Map/               # 대상 ID와 실제 사물
│  │  ├─ Phenomenon/        # 행동 등록 및 현상 구현
│  │  └─ Testing/           # 테스트씬·개별 시험
│  └─ OutGame/              # 메뉴 / 기존 결과 화면
├─ 8.Audio/                 # 임시 음원과 설정 에셋
└─ GameData/                # CSV 및 Catalog
Docs/                       # 세부 연결·검증 가이드
Tools/                      # 독립 규칙·참조 컴파일 검사
```

| 책임 | 주요 코드 |
|---|---|
| 정의 데이터 | QuestData, TargetCodeData 등 CSV 행 |
| 실행 상태·규칙 | MissionRuntime, GameSession, MissionPlanner |
| 이상현상 실행 | AnomalyRuntime, 행동 전략 레지스트리 |
| 엔진 어댑터 | IAnomalyBody / AnomalyTargetAdapter |
| 의존성 조립 | InGameSceneBootstrapper, GameSessionHost |
| 입력·화면 | PlayerInteractor, PlayerController, InGameSessionController, InGameHud |
| 연출 | PresentationDirector, 회차별 Presentation, PresentationActor |

상속은 이상현상 행동과 공통 연출처럼 실제 종류 관계에 사용합니다. 사물별 기능은 컴포넌트 조합으로 확장합니다. Strategy는 행동 확장, Observer는 완료 이벤트 소비, Adapter는 도메인과 Unity 연결, 명시적인 상태 전환은 회차·연출 진행에 사용합니다. 필요 없이 패턴을 추가하거나 모든 호출을 전역 이벤트 버스로 우회하지 않습니다.

명령은 상태 변경을 요청하고, 이벤트는 완료된 결과를 전달합니다. 연출은 `GameSessionHost.Changed`를 구독하고 수명 종료 시 해제합니다. 회차별 연출은 Update 기반 진행을 사용하며, 게임 규칙은 도메인 Tick이 담당합니다.

## 7. 상호작용·사운드·초기화

### 복구와 하이라이트

소실형은 Visual을 숨기되 대상 루트와 감지 영역을 유지합니다. 이동·회전은 저장한 기준 상태로 복구합니다. 복수 사건이 같은 사물에 겹치면 기준 상태에 남은 활성 사건을 다시 적용합니다. 실행 건은 `OccurrenceId`로 구분합니다.

흰색 하이라이트의 농도·굵기는 Inspector에서 설정합니다. 실제 사물 메시의 피벗과 표시 중심은 다를 수 있으므로 연출 위치는 전용 앵커로 지정합니다.

### 상호작용 거리

`Player → Player Interactor → Interaction Distance`로 조준 가능한 최대 거리를 조정합니다. 현재 저장된 InGame 씬 값은 **1.8m**, 스크립트 기본값은 3m입니다. 코드 기본값 변경만으로 기존 씬 설정이 덮어써지지는 않습니다.

거리 기준은 카메라에서 감지 Collider 표면까지입니다. 특정 사물만 멀리서 잡히면 InteractionArea의 크기도 확인하세요. 회차 연출의 Corridor/Approach/Close 트리거 크기는 별도 설정이며 이 값을 줄여도 달라지지 않습니다.

### 오디오

`RecoveryAudioBinder`가 플레이어 상호작용 이벤트를 `RecoveryAudioPlayer`에 연결하고, 설정 에셋에서 복구 종류별 음원을 선택합니다. 홀드 시작부터 완료·취소까지 재생하며 모든 전환은 Cut입니다. `dummy.wav` 및 현재 테스트 음원은 임시 자원입니다.

4회차 Ambience Sources에는 씬의 AudioSource를 연결합니다. 비어 있으면 Temporary Ambience를 사용합니다. 환경음은 접근 시 음소거하고 퇴장 완료 후 원래 음소거 상태로 복구합니다.

#### 발소리

- `PlayerController`가 CharacterController.Move 전후 수평 변위를 비교합니다. 입력이 있어도 벽에 막혀 실제로 움직이지 않으면 재생하지 않습니다.
- `movementSampleFrame = Time.frameCount` 기록과 `IsMoving` 속성으로 현재 프레임의 이동 결과를 제공합니다. Teleport에서는 판정을 초기화합니다.
- `PlayerFootstepAudio`가 LateUpdate에서 결과를 읽고 2D 반복 음원을 재생합니다. 이동 종료·조작 제한·일시정지·포커스 해제 시 즉시 정지합니다.
- Player 및 Footstep Loop 슬롯을 연결합니다. 발소리 AudioSource는 복구 소리와 공유하지 않습니다. 발소리의 보행 간격은 현재 반복 클립 자체에 따릅니다.

#### 환경음

- `GameAmbienceAudio`가 CameraManager.ModeChanged를 구독하고 플레이어 시점에서 재생합니다. CCTV실 위치만으로 차단하지 않고 **CCTV 화면 확인 중** Pause합니다.
- 플레이어 시점 복귀 시 UnPause하여 멈춘 위치부터 이어집니다. 일시정지·포커스 해제도 처리합니다. 페이드는 없습니다.
- Camera Manager와 Ambience Loop를 연결합니다. 전용 AudioSource는 2D 반복 재생입니다.
- 같은 AudioSource를 `Round Four Presentation → Ambience Sources`에 연결해야 심장박동 중 환경음이 함께 들리지 않습니다. 이 배열에는 음원 파일이 아닌 씬의 AudioSource가 들어갑니다.
- CCTV 제어는 Pause/UnPause, 4회차 연출은 mute/원래 mute 복구를 사용합니다. 한쪽이 끝났다고 다른 쪽의 차단을 해제하지 않도록 책임을 구분합니다.

#### 일반 문 소리

- `DoorInteraction`은 클릭으로 열기/닫기를 번갈아 수행합니다. 회전 중에는 추가 상호작용을 막고 완료 후 다시 허용합니다.
- 실제 동작 시작 시 `MotionStarted(bool opening)`, 초기화·취소 시 `MotionCancelled`를 전달합니다.
- `DoorAudioPlayer`는 이벤트를 받아 열림/닫힘 클립을 3D로 한 번 재생합니다. 클립은 끝까지 재생하되 다음 동작·초기화·비활성화 시 즉시 끊습니다. 회차 초기화로 문이 닫힐 때는 닫힘 소리를 내지 않습니다.
- Bootstrapper의 BuildDoor에서 DoorInteraction, DoorAudioPlayer, AudioSource를 연결합니다. 프리팹마다 직접 컴포넌트를 붙일 필요는 없습니다.
- `InGameCore → In Game Scene Bootstrapper`의 Door Open Clip, Door Close Clip, Door Volume, Door Min/Max Distance를 설정합니다. 테이블에 등록된 일반 문이 공통 설정을 사용합니다.
- 1회차 냉장고 문은 일반 문 사운드와 별개이며 DoorOpenPresentation의 Open Clip을 사용합니다.

세 신규 오디오 기능은 코드 반영을 확인했습니다. 실제 청취 성공, 음원 품질, 볼륨 밸런스는 별도 플레이 확인이 필요합니다.

### 회차 초기화와 입력 잠금

`RoundResetScope`는 캡처한 맵 상태를 복구하고, 별도 내부 상태는 초기화 계약으로 관리합니다. 연출은 자체 귀신·조명·화면·소리·트리거를 초기화합니다. 런타임 모델은 생성 후 재사용하며 임의 Destroy로 맵 복구를 대신하지 않습니다.

연출 입력 잠금은 소유자별로 관리합니다. 한 연출이 끝났다는 이유로 다른 연출이나 세션의 잠금을 풀지 않습니다.

## 8. 회차별 연출 설정

아래는 구현된 연출의 의도와 연결입니다. 데이터 ID 변경 시 5절의 현재 연결표와 Inspector 조건을 함께 확인하세요.

### 1회차: 냉장고 문

꽃병 사건 해결 후 0.5초 지연 → 지정 냉장고 문 회전 및 소리입니다. 연출 문은 플레이어 상호작용·밀림 처리를 요구하지 않습니다. Hinge는 문 경첩 위치에 둔 회전 기준 Transform이며, 문판을 자식으로 둡니다. Open Angle과 Duration은 Inspector에서 조절합니다.

### 2회차: 관과 뒤쪽 귀신

- 기존 복구는 관 뚜껑을 즉시 원위치로 돌립니다.
- 사건 발생 시 관 안 임시 안광을 표시합니다.
- 해결 시 플레이어 카메라 수평 뒤쪽에 CreepyGirl을 배치합니다.
- 화면 중앙 범위·거리·벽 가림 검사를 모두 통과하면 1초 조작 제한.
- 조작 제한 해제 후 추가 2초가 지나면 귀신을 숨깁니다.
- 연출 고유 사운드는 없으며 공통 복구 사운드는 유지합니다.

상세: [2회차 관 연출](Docs/RoundTwoPresentation.md)

### 3회차: 스탠딩 귀신·피 글씨·복도 돌진

사건 발생 → 스탠딩 여자 귀신 → 해결 시 스탠딩 숨김 및 I_15 피 글씨 활성화 → 복도 진입 → 3회 점멸·암전 → 복구와 동시에 기어다니는 귀신 돌진 → 돌진 중 화면 점멸·셰이크 → 이동 종료 시 귀신 숨김입니다. 피 글씨는 다음 회차 초기화까지 유지합니다.

| 슬롯 | 기준 |
|---|---|
| Standing Anchor | 침대 옆 귀신의 발 위치 |
| Corridor Anchor | 복도 감지 박스 중심 |
| Rush Anchor | 돌진 귀신의 발 위치 |
| Trigger Size | 생성되는 감지 박스 크기 |

앵커는 빈 오브젝트로 만들고 별도 Collider를 붙이지 않습니다. 감지 박스는 실행 중 `Round3_CorridorTrigger`로 생성됩니다. 발 앵커는 바닥, 감지 앵커는 플레이어 몸통이 겹칠 높이에 둡니다. Rush Anchor가 없으면 돌진 시작 시 플레이어 전방에서 생성하므로 벽 안에 나오지 않는지 확인해야 합니다.

### 4회차: 03:30 접근 연출

**미션 발생·해결과 무관하게 4회차 게임 시각 03:30부터 활성화**됩니다. 시간 건너뛰기도 처리합니다. 먼 영역 진입 → 환경음 OFF·심장박동 ON → 가까운 영역 진입 → 남자 귀신이 빈소 안으로 이동 후 소멸 → 환경음 복구입니다. 정상 진행에서 회차당 한 번 실행합니다.

| 슬롯 | 기준 |
|---|---|
| Peek Anchor | 화환 뒤 남자 귀신의 발 위치, forward가 바라보는 방향 |
| Retreat Anchor | 빈소 안 퇴장 목적지의 발 위치 |
| Approach Anchor / Size | 심장박동 시작 영역 중심과 크기 |
| Close Anchor / Size | 퇴장 시작 영역 중심과 크기 |
| Retreat Seconds | 귀신이 퇴장하는 시간 |
| Activation Hour / Minute | 기본 3 / 30 |

앵커는 초기화 때 읽으므로 편집 모드에서 배치하고 다시 Play합니다. 생성되는 감지 영역은 `Round4_ApproachTrigger`, `Round4_CloseTrigger`입니다. 가까운 영역 안에서 활성화되면 곧바로 퇴장할 수 있습니다. 최소 노출 시간은 따로 구현되어 있지 않습니다. 충분히 보이게 하려면 두 영역을 구분하고 Retreat Seconds를 조정하세요.

### 엔딩 미리보기

Play 중 4회차에서 InGameCore의 Round Four Presentation 제목을 우클릭하고 **테스트 → 엔딩 미리보기 (클리어 전환 없음)**를 실행합니다.

- Ending Clip 없음: Temporary Ending Seconds 동안 임시 화면 표시(기본 2초).
- 영상 지정: 영상 재생 후 종료. 준비 실패·시간 초과 시 임시 화면으로 대체.
- 미리보기 동안 조작과 게임 시간을 제한하고 종료 후 해제.
- 미리보기는 미션 완료나 씬 이동을 실행하지 않음.
- 실제 전체 해결 후 메뉴 복귀 경로는 아직 연결 수정이 필요함.

상세 배치 안내: [3·4회차 연출 가이드](Docs/RoundThreeFourPresentation.md). 이전 가이드의 엔딩 설명보다 이 문서의 현재 상태와 최신 합의가 우선합니다.

## 9. 성능·네트워크 확장 범위

CSV 로드, 맵 탐색, 연출 모델 및 트리거 생성은 초기화 단계에 집중합니다. 모델을 회차마다 재사용하고, 조준은 고정 버퍼 기반 조회를 사용합니다. 실제 맵의 조명·스킨드 메시·렌더텍스처·셰이크 비용은 Profiler로 별도 측정해야 하며, 프로젝트 전체 최적화를 완료했다고 보지 않습니다.

명령 경계에는 안정적인 ID와 실행 건 ID를 사용합니다. 향후 호스트나 서버가 배정·시간·해결 권한을 소유하고 클라이언트가 결과를 표현하도록 확장할 수 있습니다. 현재 네트워크 전송·P2P 권한·전용 서버·리플레이는 구현하지 않았습니다. 정수 시간과 시드만으로 Unity 물리·연출의 플랫폼 간 결정론이 보장되지는 않습니다. 명령 순서, 동기화, 연출용 시간·난수 분리는 별도 과제입니다.

## 10. 검증 방법과 기록

프로젝트 루트에서 실행합니다.

```powershell
# 자체 스크립트를 로컬 Unity 참조로 컴파일
& Tools/CoreChecks/CompileUnity.ps1

# NuGet 복원 없이 .NET 10으로 엔딩 도메인 검사
& Tools/PresentationChecks/Run.ps1

# 기존 코어 전체 검사 (환경에 따라 NuGet 복원 필요)
dotnet run --project Tools/CoreChecks/CoreChecks.csproj
```

컴파일 도구는 설치된 Unity 경로, 생성된 csproj 및 Library 참조를 사용합니다. 다른 PC에서는 경로와 프로젝트 파일 생성 상태를 확인하세요. 별도 컴파일의 Inspector 필드 CS0649 경고와 실제 컴파일 오류는 구분합니다.

| 기록 | 적용 범위 |
|---|---|
| 기존 CoreChecks 10,118 assertions | 2026-09-18 당시 기록, 현재 재검증 결과 아님 |
| PresentationChecks 351 assertions | 엔딩 조건 변경 이전 통과 기록 |
| 3회차 소멸 / 4회차 시각 활성화 수정 | 당시 참조 컴파일 통과 |
| 사용자 플레이 확인 | 복구 소리, 노이즈, 1회차 문, 4회차 등장·접근·퇴장 등 대화에서 확인된 범위 |
| 최신 엔딩 전환 | Quest 필터·메뉴 목적지 수정 및 검증 필요 |

현재 PresentationChecks에는 이전의 영정사진 전용 엔딩·최종 복귀 기대값이 남아 있습니다. 최신 전체 해결 엔딩 규칙에 맞춰 검사를 갱신해야 합니다. 문서 작성만으로 검사를 재실행하거나 성공 기록을 갱신하지 않습니다. 자동 도메인 검사는 Unity 배치·물리·청취·영상·씬 전환 확인을 대신하지 않습니다.

## 11. 남은 작업과 협업 규칙

1. 변경된 Quest/Target ID와 1~3회차 연출 필터를 맞추고 회귀 확인. 해결 이벤트 발행은 복원된 상태.
2. EndingStarted 수신을 미션 ID 필터와 분리하고 완료 목적지를 Main Menu (Desktop)으로 변경.
3. 엔딩 규칙 검사 갱신 및 전체 해결 → 임시 엔딩 → 메뉴 복귀 플레이 확인.
4. 최신 테이블 전체 참조·배정 수·씬 연결 검증. 이불 기획과 현재 베개 대상 차이 정리. 사건 시간은 사용자 결정 없이 변경하지 않음.
5. 최종 엔딩 영상·사운드·안광 등 임시 자원 교체.
6. 발소리·CCTV 환경음·문 사운드 청취 확인 및 환경음/심장박동 동시 차단 검증.
7. 회차 초기화, 일시정지, 벽 가림, 트리거 중복 진입 및 실제 맵 성능 측정.

- 가이드나 검토만 요청받은 경우 코드·씬을 수정하지 않습니다.
- 사용자가 편집한 씬·프리팹·테이블을 임의로 되돌리지 않습니다.
- `.meta`를 에셋과 함께 관리하고 배치 변경은 편집 모드에서 저장합니다.
- 현재 검증 중심은 InGame입니다. TestScene 전용 안내와 혼동하지 않습니다.
- `Assets/0.Dark - Complete Horror UI`는 현재 작업에 사용하거나 참고하지 않습니다.
- 기능 정의 → 책임 → 데이터 → 상태/명령 → 결과 이벤트 → Unity 연결 → 성능·네트워크 영향 → 집중 검증 순서로 확장합니다.
