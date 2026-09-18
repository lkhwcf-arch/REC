# 6~10단계 코어 루프

## 실행

`Assets/1.Scene/TestScene.unity`를 열고 Play 합니다. `CoreLoopTest`에 데이터와 하이라이트 Material을 연결해 두었습니다. 기존 테스트 오브젝트는 삭제하지 않으며, 플레이 중에만 비활성화합니다. 기존 개별 시험으로 돌아가려면 재생을 멈추고 `CoreLoopTest` 게임 오브젝트를 비활성화하세요.

실행 시 CSV에 등록된 TargetID마다 테스트 사물을 한 번 생성합니다. 추가형은 처음에 숨겨지고, 나머지는 표시됩니다. 생성되는 방/사물/화면은 테스트용이며 InGame 씬과 외부 UI 패키지를 사용하지 않습니다.

재생 전에 `CoreLoopTest` Inspector에서 시작 회차, 난수 시드, 흰색 외곽선 농도·상대 굵기, 소실 사물의 반투명 농도를 바꿀 수 있습니다. 같은 시드는 같은 추첨 결과를 만듭니다. 굵기는 픽셀 단위가 아닌 메쉬 확대 비율입니다.

1. 22:00에 시작합니다. WASD로 이동하고 마우스로 시점을 조작합니다.
2. 뒤쪽 CCTV실 문 가까이에서 Tab을 눌러 중간 복귀합니다. 23시 미복귀는 안내만 표시하며 별도 패배 조건을 만들지 않았습니다.
3. 화면의 시간 건너뛰기로 23:30으로 이동합니다. `CCTV 확인` 후 `현장 순찰 시작`을 누릅니다. 테스트 CCTV는 같은 맵을 보여주는 단일 고정 시점입니다.
4. 활성 이상현상 사물을 3m 안에서 조준하고 왼쪽 버튼을 1초 누르면 해결됩니다. 소실형은 원래 위치를 조준합니다. 흰색 외곽선과 소실 사물의 반투명 표현이 표시됩니다.
5. N으로 다음 미션 오픈 시각까지 건너뜁니다. 이전 미해결 사건은 그대로 남습니다. 추가/소실/이동/그림자/그룹은 추첨 결과에 따라 달라집니다.
6. 잠겨 있는 미래 미션까지 전부 해결하면 최종 복귀가 허용됩니다. 문 근처에서 Tab으로 복귀하고 다음 회차 버튼을 누릅니다. 마지막 회차는 클리어 화면을 표시합니다.
7. Esc로 마우스를 풀어 테스트 메뉴를 엽니다. `시험: 06:00으로 이동`은 미완료 시 게임오버를 확인하기 위한 명시적인 시험 명령입니다.

## 책임과 패턴

| 위치 | 책임 |
|---|---|
| Data/Definitions | CSV 정의. 게임 중 미션 상태를 저장하지 않음 |
| InGame/Core/MissionPlanner | 일정별 고정/가중치 비복원 추첨, 실행 데이터 변환 |
| InGame/Core/MissionRuntime | 사건 ID, 미션 ID, 오픈 시간, 잠금/활성/해결 상태 |
| InGame/Core/AnomalyStrategies | `AnomalyAction` 추상 클래스와 실행 전략, 등록 팩토리, 사건별 재적용 |
| InGame/Core/GameSession | 명령, 시간 Tick, 회차 상태 전환, 완료 이벤트 |
| InGame/Session/AnomalyTargetAdapter | 도메인 `IAnomalyBody`를 Unity 사물·Transform·상호작용에 연결 |
| InGame/Session/GameSessionHost | 명시적 의존성 조립, Unity 시간 입력, 생명주기, 연출 이벤트 |
| InGame/Session/RoundResetScope | 맵 기준 상태와 사용자 정의 초기화 참여자 |
| InGame/Testing | 검증 공간, 테스트 입력·화면. 실제 게임 UI와 분리 |

Strategy(행동별 다형성), Observer(완료 이벤트), Adapter(Unity 경계), 구성 루트/명령 파사드를 사용합니다. 상태 수가 작고 전환 규칙이 한곳에 모이므로 상태마다 클래스를 만드는 대신 명시적인 상태 열거형을 사용합니다. 사물 데이터 클래스를 플레이어가 상속하지 않습니다. 행동은 실제 is-a 관계이므로 추상 클래스 상속을 사용합니다.

## 데이터와 실행 규칙

- `MainDetailIDs`는 기존 이름을 유지한 단일 int입니다.
- 발생/해결 이름은 등록된 행동 쌍만 허용합니다. 문자열을 Reflection으로 임의 메서드 호출에 사용하지 않습니다.
- `ObjectAddition/ObjectDeletion`, `ObjectDeletion/ObjectAddition`, `ObjectMovement/RestoreObjectState`, `ObjectGroupMovement/RestoreObjectGroupState`, `ShadowAddition/ShadowRemoval`을 구현했습니다.
- 교체 확장 지점 `ObjectReplacement/RestoreObjectState`와 교체 Visual 연결을 제공합니다. 현재 CSV에 교체 행이 없으므로 새 콘텐츠나 매핑을 임의로 추가하지 않았습니다.
- 사용자가 확정한 이동 단위에 따라 빈 Offset Unit은 `Centimeter`로 수정했습니다. 50은 0.5m, -50은 -0.5m입니다. 회전은 Degree입니다. 이동은 Visual의 부모 기준 로컬 좌표이며 회전은 로컬 축입니다.
- GroupPosition CSV에 개별 사물 위치가 없으므로 `GroupPose[]` Inspector 연결로 확장합니다. 테스트씬의 위치는 테스트 예시입니다. 실제 배치를 확정한 데이터/프리팹 연결이 필요합니다.
- 그림자 추가는 지정한 `shadowVisual`의 표시 상태를 관리합니다. 실제 연출용 그림자 모델/재질은 확정 시 연결합니다. 테스트에서는 검은 단순 모델로 구분합니다.
- 회차 시작에 전체 미션을 배정한 후 RealTime(ms)에 오픈합니다. 2,500ms는 게임 1분이며 22:00부터 시작합니다. 225,000ms=23:30, 525,000ms=01:30, 825,000ms=03:30, 1,200,000ms=06:00입니다.
- Round의 날짜 문자열과 CoreLoop.csv의 설명 행을 게임 시각으로 파싱하지 않습니다. Host의 검증되는 설정을 사용합니다. 타임스케일을 바꾸지 않으며, 일시정지/포커스 해제 중에는 테스트 시간이 멈춥니다.
- 다음 일정 건너뛰기는 도메인 Tick을 통과합니다. 이미 연 사건을 두 번 열지 않습니다.
- 최종 복귀 전 모든 배정 미션(미오픈 포함)이 해결되어야 합니다. 유일한 패배는 06:00 미완료입니다. 마감 전에 해결을 모두 끝내고 아직 복귀 중인 경우 추가 패배를 만들지 않습니다.
- 같은 사물의 활성 사건들을 기준 상태 위에 발생 순서로 적용합니다. 한 사건을 해결하면 해당 효과만 제외하고 남은 사건들을 재적용합니다. 같은 물체의 해결 명령은 가장 오래된 미해결 사건을 대상으로 합니다.
- 미연결 사물을 추첨에서 몰래 빼거나 자동 성공 처리하지 않습니다. 시작 전 모든 미션 후보의 연결을 검증하고 설정 오류를 표시합니다. 아직 CSV에 없는 맵 사물은 등록할 필요가 없습니다.

## 실제 맵으로 옮길 때

현재 작업은 TestScene 전용입니다. InGame 씬은 수정하지 않았습니다.

1. 관리 루트에 `GameSessionHost`, 맵 루트에 `RoundResetScope`를 연결합니다. 초기화 범위 안에 관리 서비스를 두지 않습니다.
2. CSV에 등장하는 사물에 `MapTarget`, `Interact`, `AnomalyTargetAdapter`를 연결하고 Host의 Targets에 등록합니다. TargetID는 중복할 수 없습니다.
3. Visual은 자식, 감지 영역은 Visual과 독립된 Trigger 자식으로 만듭니다. 소실 시 루트를 끄지 않습니다. 추가형 Visual은 최초에 꺼 두어야 합니다.
4. 그룹/그림자/교체 데이터가 필요한 사물만 해당 필드를 연결합니다. 이미 구성된 PlayerInteractor와 UI는 그대로 재사용할 수 있습니다. Interact의 HoldCompleted는 어댑터가 구독하므로 이전 시험 Restore 이벤트를 함께 연결하지 않습니다.
5. 실제 CCTV 입장·퇴장, Tab, 문 통과는 `GameSession` 요청과 `CanEnterRoom/CanPatrol`을 확인하도록 연결해야 합니다. TestScene에서는 입력 경로와 실제 문 Collider 모두 제한합니다. 기존 InGame CameraManager는 이번에 변경하지 않았으므로 자동으로 복귀 제한이 적용되는 것은 아닙니다.
6. 문 잠금/컨테이너 상태 등 사용자 정의 컴포넌트는 `IRoundResetParticipant`를 구현합니다. 초기 맵 사물은 Destroy하지 않습니다. 스폰된 동적 사물은 참여자에서 풀 반환합니다. Transform/활성 상태/Rigidbody 속도는 Scope가 복구합니다.
7. 실제 복잡한 건물의 카메라 Bounds, CCTV 배치와 최종 UI는 맵 확정 후 연결합니다. 테스트 방은 벽/천장 Collider, 플레이어 위치 제한, 작은 Near Clip으로 내부를 유지합니다.

## 연출 연결

`GameSessionHost.Changed`는 사물 변경과 상태 기록이 완료된 뒤 발행됩니다. 이벤트에는 사건 종류, RoundID, QuestID, TargetID, OccurrenceID, DirectionGroupID, 실제 경과 ms가 있습니다. `On Presentation Cue`는 Inspector에서 사건 종류/DirectionGroupID/TargetID를 받아 Timeline, 소리, 연출 어댑터에 연결할 수 있습니다.

Direction/FieldGuide/DirectionParameter 테이블은 아직 의미가 확정되지 않았으므로 실행하지 않습니다. 연출 실패가 이미 완료된 미션 상태를 되돌리지 않습니다. 연출에 의해 실제 게임 규칙을 바꿔야 할 경우 별도 명령을 사용합니다.

## 성능·네트워크 범위

미션과 맵 조회는 초기화/사건 전환 때 처리합니다. 매 프레임 CSV 파싱, 전체 씬 검색, Instantiate/Destroy를 하지 않습니다. 하이라이트 표시 객체/재질은 테스트 플레이어당 한 쌍을 공유합니다. 조준은 고정 RaycastNonAlloc 버퍼를 사용하며 버퍼가 차면 관통 선택을 하지 않습니다. 실제 UI 팩이나 텍스처는 불러오지 않습니다. 테스트용 IMGUI의 문자열 할당은 실제 HUD 제작 시 이벤트 기반 갱신으로 대체할 수 있습니다.

도메인의 시간과 난수는 주입되며, 명령은 직렬화 가능한 사건 ID를 사용합니다. 네트워크 전송/호스트 권한 판정/상태 동기화/저장은 구현하지 않았습니다. System.Random 및 Unity Transform/Physics를 포함한 플랫폼 간 결정론을 보장하지 않습니다. 서버가 미션 배정과 명령을 소유하고 결과를 동기화하는 확장 지점을 제공합니다.

## 검증

프로젝트 루트에서 다음을 실행합니다.

```powershell
dotnet run --project Tools/CoreChecks/CoreChecks.csproj
& Tools/CoreChecks/CompileUnity.ps1
```

첫 번째는 실제 CSV와 실제 Core 소스를 사용하는 자동 테스트입니다. 두 번째는 설치된 Unity 6000.2.8f1의 C# 9 컴파일러와 참조 DLL로 프로젝트 자체 스크립트만 컴파일합니다. UI 패키지 소스는 포함하지 않습니다. Inspector 전용 필드의 CS0649 경고는 Unity 밖에서 컴파일할 때 표시될 수 있습니다.

`RunAdapterSmoke.ps1`은 별도 임시 Unity 프로젝트에서 엔진 Transform/활성 상태 검증을 실행하는 보조 도구입니다. 현재 환경에서 실행 승인이 거절되어 엔진 검증 결과는 확보하지 못했습니다. TestScene 실제 화면, 손 조작, 렌더링은 Unity에서 추가 확인해야 합니다. 자동 테스트 통과를 플레이 모드 확인으로 간주하지 않습니다.
