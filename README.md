# REC

Unity 6 기반의 3D 공포 게임 프로젝트입니다. 플레이어는 CCTV로 이상현상을 확인하고 현장을 순찰하며, 제한 시간 안에 해당 회차의 미션을 해결합니다. 사물·이상현상·미션·일정·회차 정의를 CSV로 관리하고, 실행 중 상태와 Unity 표현을 분리하는 구조로 개발합니다.

현재는 **TestScene에서 코어 흐름을 검증하는 개발 단계**입니다. 실제 InGame 맵의 사물 연결, 최종 UI와 연출은 별도 통합 작업이 필요합니다.

## 1. 개발 환경

| 항목 | 기준 |
|---|---|
| Unity | 6000.2.8f1 |
| C# | 9.0 범위의 Unity 지원 문법 |
| 렌더링 | Universal Render Pipeline 17.2.0 |
| 카메라 | Cinemachine 3.1.7 |
| 입력 | Input System 1.14.2 |
| UI | uGUI, TextMesh Pro / 코어 테스트 화면은 IMGUI |
| 데이터 | CSV → 형식 변환 → 참조 검증 → 타입별 저장소 |
| 별도 코어 테스트 | .NET 10 SDK, PowerShell |

Unity와 패키지 버전의 기준 파일은 `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`, `Packages/packages-lock.json`입니다. 별도 테스트 도구의 .NET 10과 Unity 프로젝트의 런타임 지원 범위는 구분합니다.

## 2. 빠른 실행

1. Unity Hub에서 이 저장소를 **6000.2.8f1**로 열고 패키지 복원과 컴파일을 완료합니다.
2. `Assets/1.Scene/TestScene.unity`를 엽니다.
3. Hierarchy의 `CoreLoopTest`가 활성 상태인지 확인합니다. 데이터와 하이라이트 Material은 씬에 연결되어 있습니다.
4. Play를 실행합니다. 테스트 방과 CSV 대상에 대응하는 임시 사물이 생성됩니다.

`CoreLoopTest` Inspector에서 시작 회차, 난수 시드, 하이라이트 농도·굵기를 조정할 수 있습니다. 기존 테스트 오브젝트는 삭제되지 않고 재생 중에만 비활성화됩니다. 기존 개별 시험을 사용하려면 재생을 멈추고 `CoreLoopTest` 게임 오브젝트를 비활성화합니다.

| 입력 | 테스트씬 동작 |
|---|---|
| WASD / 마우스 이동 | 이동 / 시점 회전 |
| 마우스 왼쪽 버튼 1초 유지 | 조준한 활성 사건 해결 |
| Tab | CCTV실 문 근처에서 복귀 요청 |
| N | 허용된 상태에서 다음 미션 오픈 시각까지 건너뛰기 |
| Esc | 테스트 메뉴와 마우스 커서 전환 |

CCTV실로 중간 복귀한 뒤 화면의 시간 건너뛰기 → **CCTV 확인** → **현장 순찰 시작** 순서로 진행합니다. 테스트 CCTV는 동일한 맵을 보여주는 단일 고정 시점입니다. 실제 다중 CCTV 화면의 최종 구성을 의미하지 않습니다.

세부 실행 절차와 Inspector 연결은 [코어 루프 작업 안내](Docs/CoreLoop.md)를 참고하세요.

## 3. 게임 규칙

### 회차 진행

```text
22:00 초기 순찰
  → CCTV실 중간 복귀 (23:00까지 복귀 안내)
  → 23:30부터 CCTV 확인 가능 / 첫 사건 오픈
  → 현장 순찰 및 이상현상 해결
  → 모든 배정 미션 해결
  → CCTV실 최종 복귀
  → 다음 회차 또는 최종 클리어
```

- 미션은 **회차 시작에 미리 배정**하며, 각 미션의 `RealTime`에 잠금이 해제됩니다.
- 이후 일정이 열려도 이전 미해결 사건을 유지합니다. 서로 다른 일정의 추첨은 독립적입니다.
- CCTV 확인 후 현장 순찰을 시작하면 모든 배정 미션을 해결할 때까지 CCTV실 복귀를 막습니다.
- 아직 열리지 않은 미션도 완료 조건에 포함합니다. 현재 보이는 사건만 해결했다고 회차가 끝나지 않습니다.
- 최종 복귀 후에는 해당 회차의 추가 순찰을 허용하지 않습니다.
- 현재 게임오버 조건은 **06:00에 미완료 미션이 남아 있는 경우**입니다. 23시 중간 복귀 지연은 안내만 표시합니다.
- 마감 전에 미션을 모두 해결한 경우, 아직 최종 복귀 중이라는 이유로 추가 게임오버를 만들지 않습니다.
- 다음 회차는 `Round.NextRoundID`로 연결하며, `0`이면 마지막 회차입니다. 현재 CSV는 4회차이고 코드에 회차 수를 고정하지 않습니다.

### 시간 단위

`RealTime`은 회차 시작부터 경과한 실제 시간의 **밀리초(ms)**입니다. 기본 설정에서 실제 2.5초가 게임 시간 1분이며, UI는 22:00부터 분 단위로 표시합니다.

| 경과 시간 | 게임 시각 |
|---:|---|
| 0ms | 22:00 |
| 150,000ms | 23:00 |
| 225,000ms | 23:30 |
| 525,000ms | 01:30 |
| 825,000ms | 03:30 |
| 1,200,000ms | 06:00 |

`GameSessionHost`는 경과 시간을 누적하여 `GameSession.Tick`에 전달합니다. 시간 건너뛰기도 같은 경로를 사용합니다. 일시정지(`Time.timeScale <= 0`)와 애플리케이션 포커스 해제 중에는 Host의 시간이 진행되지 않습니다. 그 외에는 `unscaledDeltaTime`을 사용합니다.

## 4. 현재 구현 범위

| 기능 | 현재 상태 |
|---|---|
| CSV 파싱·행 변환·ID 조회·참조 검증 | 구현 |
| 고정 배정·가중치 비복원 추첨 | 구현 |
| 예약 오픈·미해결 유지·중복 해결 차단 | 구현 |
| 사물 추가·소실·이동·회전·복구 | 구현 |
| 그룹 이동 | 실행 구조 구현, 개별 이동 위치는 Inspector 설정 |
| 그림자 추가·제거 | 표시 상태 관리 구현, 테스트용 모양 사용 |
| 사물 교체 | 전략과 어댑터 확장 지점 구현, 현재 CSV 콘텐츠 없음 |
| 조준·유지 입력·흰색 하이라이트 | 기존 컴포넌트 및 테스트씬 입력/표현 구현 |
| 시간·복귀 제한·회차 초기화·게임 종료 | TestScene 연결 구현 |
| 연출 | 완료 이벤트와 Inspector 연결 지점 제공, 실제 연출 미확정 |
| InGame 통합 | 실제 사물·문·CCTV·UI 연결 필요 |

코드 구현, 자동 검증 통과, Unity 플레이 모드 검증은 서로 구분합니다. 검증 상태는 아래 항목에 기록합니다.

## 5. 프로젝트 구조

주요 자체 코드와 데이터의 위치입니다.

```text
Assets/
├─ 1.Scene/                 # Start, InGame, TestScene, GameOver, GameClear
├─ 3.Script/
│  ├─ Data/
│  │  ├─ Definitions/       # CSV 행 정의
│  │  ├─ Loading/           # 파서, 매퍼, 저장소, 데이터 Bootstrapper
│  │  ├─ Validation/        # 데이터 간 참조·규칙 검증
│  │  └─ GameDataCatalog.cs # TextAsset 목록
│  ├─ InGame/
│  │  ├─ Core/              # Unity에 직접 의존하지 않는 회차·미션 규칙
│  │  ├─ Session/           # Unity 어댑터, 의존성 조립, 맵 초기화
│  │  ├─ Camera/            # 기존 CameraManager, CCTVTerminal
│  │  ├─ Interaction/       # 상호작용 입력, 안내, 하이라이트
│  │  ├─ Map/               # 사물 ID와 맵 사물 연결
│  │  ├─ Phenomenon/        # 행동 레지스트리 및 기존 현상 코드
│  │  ├─ Player/            # 기존 플레이어 상태·이동
│  │  └─ Testing/           # 개별 시험과 코어 테스트씬 구성
│  ├─ OutGame/
│  │  ├─ StartMenu/         # 시작 메뉴 MVC
│  │  └─ Result/            # 결과 화면 MVC
│  └─ Manager/              # 기존 관리 코드
├─ GameData/
│  ├─ CSV/                  # 게임 정의 테이블
│  └─ Catalog/              # 데이터 카탈로그 에셋
├─ Shader/                  # 자체 하이라이트 셰이더
└─ 6.Materials/             # 자체 재질
Docs/CoreLoop.md            # 코어 실행·연결 상세 안내
Tools/CoreChecks/           # 독립 자동 검증 도구
```

## 6. 설계 원칙과 MVC 적용

객체지향, 데이터 기반 설정, 이벤트 기반 통신을 함께 사용합니다. 데이터 정의, 변경 가능한 실행 상태, 규칙, 화면 표현, Unity 연결 책임을 분리합니다.

프로젝트 전체를 `Model/View/Controller` 폴더 세 개로 나누지는 않습니다. **기능별 폴더 안에서 책임을 나누는 구조**입니다. 시작 메뉴·결과 화면은 Model/Controller/View와 Navigation 인터페이스를 사용합니다. 인게임 코어는 같은 책임 분리를 도메인 서비스와 Unity 어댑터로 확장합니다.

| 책임 | 대표 구현 |
|---|---|
| 데이터 정의 | `AnomalyData`, `QuestData`, `ScheduleData`, `RoundData` |
| 실행 상태 | `MissionRuntime`, `GameSession`의 회차·시간·단계 |
| 규칙 및 명령 처리 | `MissionPlanner`, `GameSession`, `AnomalyRuntime` |
| Unity 연결 | `AnomalyTargetAdapter`, `GameSessionHost`, `RoundResetScope` |
| 표현 | `InteractionPromptView`, `InteractionHighlightView`, 테스트 HUD |
| 입력 | `PlayerInteractor`, `PlayerController`, 테스트 플레이어 |

### 패턴을 사용하는 이유

- **Strategy(전략):** `AnomalyAction`을 상속한 행동별 구현을 등록하여 발생 동작을 확장합니다.
- **Observer(관찰자):** 해결·회차 전환 완료 이벤트를 UI와 연출에 전달합니다. 요청과 완료 이벤트를 구분합니다.
- **Adapter(어댑터):** `IAnomalyBody`의 계약을 Transform, 활성 상태, Collider에 연결합니다.
- **구성 루트와 의존성 주입:** Host에서 저장소·난수·규칙·실행기를 조립합니다.
- **명시적인 상태 전환:** 회차 단계는 열거형과 전환 조건으로 관리합니다. 현재 구현은 단계마다 별도 State 클래스를 만드는 구조가 아닙니다.

상속은 행동처럼 실제 종류 관계가 성립할 때 사용하고, 사물별 기능은 컴포넌트 조합으로 확장합니다. 모든 데이터를 상속하거나 모든 호출을 전역 이벤트 버스로 우회하지 않습니다. `MonoBehaviour`는 생명주기와 엔진 연결을 담당하고, 핵심 흐름은 명시적인 `Tick`과 명령으로 진행합니다.

## 7. CSV 데이터 흐름

```text
GameDataCatalog의 TextAsset 참조
  → CSVParser: 헤더와 셀 해석
  → CsvMapper: C# 행 형식으로 변환
  → 임시 DataManager 저장소에 등록
  → GameDataValidator: ID·참조·일정 규칙 검증
  → 검증 성공 시 저장소 공개
  → MissionPlanner / GameSession 사용
```

현재 필수 로드 테이블은 다음 7개입니다.

| 테이블 | 역할 |
|---|---|
| Anomaly | 이상현상 정의와 발생·해결 행동 |
| Detail | 이상현상과 대상·이동 설정 연결 |
| MovementParameter | 이동 종류, 축, 값, 단위 |
| TargetCodes | 사물의 안정적인 ID와 식별 정보 |
| Quest | 회차·일정별 미션 후보, 오픈 시각, 가중치 |
| Schedule | 추첨 수, 후보 수, 일정 순서 |
| Round | 회차 구성과 다음 회차 연결 |

`CoreLoop`, `Parameters`, `Direction`, `FieldGuide`, `DirectionParameter`는 현재 필수 로더에 등록되어 있지 않습니다. 미확정 연출 테이블을 임의로 실행하지 않습니다.

### 데이터 작성 규칙

- ID는 양수이며 같은 테이블 안에서 중복할 수 없습니다. 다른 테이블을 가리키는 ID는 존재해야 합니다.
- `MainDetailIDs`는 이름과 달리 **단일 int**입니다.
- `Detail.TargetID`는 이상현상이 적용되는 사물 ID입니다. 이동 설정이 있으면 `MovementParameter.TargetID`와 일치해야 합니다.
- 발생·해결 문자열은 등록된 행동 쌍과 정확히 일치해야 합니다. 문자열로 임의 메서드를 실행하지 않습니다.
- Offset의 `Centimeter` 값 50은 Unity 좌표 0.5m입니다. 회전 단위는 `Degree`입니다.
- `SpawnType=0`은 고정 배정, `1`은 가중치 추첨입니다. 같은 일정 안에서는 중복 추첨하지 않습니다.
- `Round`에 들어 있는 날짜 문자열을 현재 게임 시각으로 사용하지 않습니다. Host의 시간 설정과 Quest의 `RealTime`을 사용합니다.
- 검증 실패 시 저장소를 공개하지 않습니다. 실행에 필요한 사물 연결 실패는 게임오버가 아닌 설정 오류로 표시합니다.

새 테이블은 행 정의, Bootstrapper의 형식 등록, 필요한 참조 검증, 카탈로그 참조를 추가합니다. 기본 셀 파싱과 공통 타입 변환을 시트마다 다시 작성하지 않습니다. `DataManager.Seal()`은 테이블 추가를 막는 기능이며, 행 객체를 깊은 불변 객체로 변환하는 기능은 아닙니다.

## 8. 이상현상과 맵 연결

`MapTarget`은 CSV의 TargetID와 실제 Visual을 연결합니다. `AnomalyTargetAdapter`가 발생 동작을 적용하고, `Interact.HoldCompleted`를 해결 명령으로 전달합니다.

소실형은 **Visual만 숨기고 대상 루트와 감지 영역을 유지**합니다. 추가형은 정상 상태의 Visual을 꺼 둡니다. 이동·회전은 시작 시 저장한 기준 상태를 바탕으로 처리합니다.

같은 사물에 여러 사건이 겹치면 기준 상태 위에 활성 사건을 발생 순서대로 적용합니다. 하나를 해결할 때 나머지 사건을 다시 적용하므로, 이전 미해결 현상을 함께 없애지 않습니다. 콘텐츠 ID와 실행 건을 구분하는 `OccurrenceId`를 사용하여 중복 해결을 막습니다.

새 행동은 전략 등록과 어댑터 지원을 추가하고, 데이터에서 해당 행동 이름을 사용합니다. 그룹 이동의 개별 위치와 실제 그림자 모델은 미완성 콘텐츠이므로 Inspector/프리팹 연결을 확정해야 합니다.

## 9. 초기화와 연출

데이터 로드가 끝난 뒤 Host가 맵 연결을 검증하고 기준 상태를 캡처한 후 회차를 시작합니다. 실제 맵에서는 데이터 초기화 완료 후 Host를 초기화하도록 실행 순서를 연결합니다.

`RoundResetScope`는 지정한 범위의 Transform, 활성 상태와 Rigidbody 속도를 초기화합니다. 문 잠금·컨테이너 등 별도의 내부 상태는 `IRoundResetParticipant`로 캡처·복구합니다. 초기 맵 사물을 Destroy한 경우 자동 재생성하지 않습니다. 동적으로 추가된 사물은 초기화 참여자에서 반환·정리해야 합니다.

`GameSessionHost.Changed`는 완료된 상태 변경을 전달합니다. 사건 종류, 회차/미션/대상/발생 건 ID, DirectionGroupID, 경과 ms를 포함합니다. Inspector의 `On Presentation Cue`로 연출 어댑터를 연결할 수 있습니다. 구독자는 수명 종료 시 구독을 해제하며, 연출이 게임 규칙을 변경해야 할 때에는 명령을 전달합니다.

## 10. 성능과 확장 범위

- CSV는 초기화 때 로드하고 타입별 ID 저장소로 조회합니다.
- 미션 추첨·사물 구성은 회차 시작이나 사건 전환 때 처리합니다.
- 테스트씬의 사물과 하이라이트를 재사용하며, 조준에 고정 크기 `RaycastNonAlloc` 버퍼를 사용합니다.
- 테스트 플레이어의 하이라이트는 한 쌍의 표시 객체/재질을 공유합니다. 기존 사물별 하이라이트 컴포넌트는 별도 구현입니다.
- 테스트 HUD의 IMGUI 문자열 갱신과 실제 맵 렌더링 비용은 별도 프로파일링 대상입니다. 프로젝트 전체의 성능 개선을 측정 완료한 상태는 아닙니다.

시간과 난수는 외부에서 주입하고 명령 경계에서 ID를 사용합니다. 향후 호스트/서버가 추첨과 해결 권한을 소유하는 구조로 확장할 수 있습니다. 현재 네트워크 전송, P2P 권한 처리, 전용 서버, 저장·불러오기, 리플레이는 구현하지 않았습니다. 정수 ms와 시드만으로 플랫폼 간 결정론을 보장하지 않으며 Unity 물리와 명령 순서의 동기화가 별도로 필요합니다.

## 11. 테스트

프로젝트 루트에서 실행합니다.

```powershell
dotnet run --project Tools/CoreChecks/CoreChecks.csproj
& Tools/CoreChecks/CompileUnity.ps1
```

| 검증 | 범위 및 기록 |
|---|---|
| CoreChecks | 실제 CSV와 Core 소스, 100개 시드 × 4회차, 10,118개 assertion 통과 |
| Unity 참조 컴파일 | 자체 `Assets/3.Script` 소스, C# 9 컴파일 오류 없음 |
| 씬 정적 점검 | 새 씬 ID 중복과 스크립트 meta 누락 없음 |
| 별도 엔진 검증 | 실행 미완료 |
| 새 코어 테스트씬 플레이·렌더링 | 직접 검증 필요 |

위 기록은 **2026-09-18 코어 구현 시점** 기준이며, 이후 수정 시 다시 실행해야 합니다. 문서 작성만으로 검증 기록을 갱신하지 않습니다.

`CompileUnity.ps1`은 로컬 Unity 설치 경로와 생성된 `Assembly-CSharp.csproj`, `Library/ScriptAssemblies`를 사용합니다. 다른 PC에서는 스크립트의 Unity 경로를 맞추고 Unity 프로젝트 파일을 먼저 생성해야 합니다. Inspector에 연결되는 필드의 CS0649 경고는 Unity 외부 컴파일에서 나타날 수 있습니다.

`RunAdapterSmoke.ps1`은 별도 임시 Unity 프로젝트에서 Transform·활성 상태 등을 확인하는 보조 도구입니다. 실행에는 Unity 에디터와 라이선스 환경이 필요합니다. .NET 코어 테스트 통과는 Unity 화면·물리·입력 검증을 대신하지 않습니다.

## 12. 협업 규칙과 남은 작업

- 다른 작업자가 수정 중인 `InGame` 씬 작업은 조율하고, 코어 검증은 `TestScene`에서 진행합니다.
- `.meta` 파일을 에셋과 함께 관리합니다. Play 중 Inspector 수정은 종료 후 유지되지 않을 수 있으므로 저장할 설정은 편집 모드에서 변경합니다.
- `Assets/0.Dark - Complete Horror UI` 폴더의 에셋과 코드는 현재 작업에 사용하거나 참고하지 않습니다.
- 검증 대상과 관련 없는 에셋·패키지 삭제, 전체 구조 변경은 별도 작업으로 다룹니다.
- 기능 정의 → 책임 분리 → 데이터 → 상태/명령 → 완료 이벤트 → Unity 연결 → 성능·네트워크 영향 → 검증 순서로 확장합니다.

다음 통합 작업이 남아 있습니다.

1. TestScene 실제 플레이·화면 확인과 입력/하이라이트 검증.
2. 확정된 맵 사물을 CSV TargetID에 연결하고 그룹 위치·그림자·교체 콘텐츠 구성.
3. InGame의 문·Tab·CCTV 전환을 회차 명령과 복귀 제한에 연결.
4. 실제 건물의 복잡한 공간, 계단, CCTV별 카메라 경계 검증.
5. 테스트 HUD를 최종 UI로 연결하고 결과 씬 전환 통합.
6. 확정된 연출 테이블을 해석하는 어댑터와 자원 수명 관리 구현.
7. 실제 맵에서 Profiler로 CPU·GPU·메모리를 측정한 뒤 병목 최적화.

현재 `CameraManager`가 새 회차 규칙에 자동 연결된 상태는 아닙니다. TestScene의 복귀 제한 구현과 InGame 통합 완료를 혼동하지 않습니다.
