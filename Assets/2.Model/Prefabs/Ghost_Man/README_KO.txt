idle / walk / run 애니메이션 사용 안내

이전 파일에는 뼈대와 스킨 웨이트만 있었고 재생할 애니메이션 클립은 없었습니다.
새 파일 Masked_Humanoid_Idle_Walk_Run.fbx에는 실제로 모델을 움직이는 3개의 애니메이션 클립이 포함되어 있습니다.

가장 쉬운 확인 방법
1. Unity에서 Masked_Humanoid_Animations.unitypackage를 가져오고 Import를 누릅니다.
2. Assets/MaskedHumanoid/Animation_Demo 씬을 엽니다.
3. Play를 누르면 idle, walk, run 캐릭터 3개가 각 동작을 반복합니다.
   이 데모의 카메라가 넓은 화면에 맞춰져 있으므로 Game 뷰를 16:9로 설정하면 모두 보기 편합니다.

자신의 씬에서 사용
- Assets/MaskedHumanoid/MaskedHumanoid_Animated.prefab을 씬에 배치하고 Play를 누르면 idle이 재생됩니다.
- 연결된 IdleWalkRun Animator Controller의 정수 파라미터 Motion으로 동작을 바꿉니다.
  Motion = 0: idle
  Motion = 1: walk
  Motion = 2: run
- 코드에서는 Animator.SetInteger("Motion", 1)과 같은 방식으로 변경할 수 있습니다.
- 세 동작은 제자리에서 반복하는 모션입니다. 캐릭터의 실제 전진/방향 전환은 게임의 이동 코드에서 처리합니다.
- Apply Root Motion은 끈 상태로 준비되어 있습니다.

FBX만 사용하는 경우
1. Masked_Humanoid_Idle_Walk_Run.fbx를 Assets에 가져옵니다.
2. Rig > Animation Type: Humanoid, Avatar Definition: Create From This Model > Apply.
3. Animation 탭에서 Import Animation을 켜고 idle, walk, run 클립을 확인합니다.
4. 각 클립에 Loop Time을 켭니다. 미리보기 재생 버튼으로 확인할 수 있습니다.
5. 씬에서 계속 재생하려면 클립을 Animator Controller의 상태에 연결하고, 캐릭터 Animator의 Controller에 그 컨트롤러를 지정해야 합니다.
   FBX를 씬에 놓는 것만으로 애니메이션이 자동 재생되지는 않습니다.
- 제공된 .fbx.meta와 FBX를 함께 복사하면 Unity Humanoid 및 반복 설정을 그대로 가져올 수 있습니다.
- 위 설정이 모두 포함된 unitypackage 사용을 권장합니다.

동작
- idle: 3초, 호흡과 작은 상체/머리 흔들림.
- walk: 1.2초, 교대 발 디딤, 무릎 굽힘, 팔 흔들림.
- run: 0.8초, 더 큰 보폭과 무릎 굽힘, 팔 흔들림, 짧은 공중 구간.
- 30fps로 제작했고 원본의 구부정한 자세를 유지했습니다.
- 기본 전신 동작이며 손가락 개별 모션, 얼굴 표정, 캐릭터 이동 코드는 포함하지 않았습니다.

보정과 확인
- 큰 동작에서 반대쪽 다리로 늘어나는 문제를 해결하기 위해 접촉 부위 정점 18개를 분리하고 다리/신발 웨이트를 다시 정리했습니다.
- 원본 정점 위치와 폴리곤 모양, UV 및 재질 설정은 유지했습니다.
- 총 본 22개, 모든 정점 스킨 웨이트 적용, 정점당 최대 4개 본 영향.
- Blender에서 시작/끝 자세가 일치하는 루프를 확인했습니다.
- Unity 6000.2.8f1에서 Humanoid Avatar 유효, 필수 본 15/15 연결.
- 세 클립 모두 Humanoid 애니메이션 및 반복 클립으로 인식됨을 확인했습니다.
- Unity 애니메이션 재생 평가 후 SkinnedMeshRenderer의 실제 정점이 변형되는 것을 확인했습니다.

재질
- 원본 색상 JPG는 첨부되지 않아 색상 텍스처를 포함할 수 없었습니다.
- 데모/프리팹에는 형상을 확인하기 위한 중성 회색 재질을 연결했습니다.
- 원래 텍스처를 가지고 있다면 재질의 Base Map/Albedo에 연결하여 사용하십시오.

공식 설정 안내
https://docs.unity3d.com/6000.2/Documentation/Manual/ConfiguringtheAvatar.html
