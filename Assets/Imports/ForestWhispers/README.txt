숲의 속삭임 (Forest Whispers) – 3D 메시 버전 v5 (레퍼런스 일치판)

조작은 2D(좌우·점프)이고, 지형·바위·건물·나무는 두께 있는 3D 메시로 조명과 그림자를 받습니다.

설치
  1. 프로젝트에 남아 있는 예전 ForestWhispers 관련 폴더를 모두 지웁니다.
     (ForestWhispers, ForestWhispers_Full, ForestWhispers_Map2D, ForestWhispers_Map3D, ForestWhispers_25D, ForestWhispers_Kit)
  2. 이 ForestWhispers 폴더를 Assets/Imports/ 에 넣습니다.
  3. Edit > Project Settings > Player > Active Input Handling = Both
  4. 메뉴 Forest Whispers > Build Prefabs And Maps

결과
  Prefabs/<지역>/Terrain, Props, Background, Waterfalls, Foreground
  Scenes/01_Village ~ 06_ElvenForest (Build Settings 자동 등록, 맵 끝에서 다음 맵으로 이동)

조작: A/D 이동, Shift 달리기, Space 점프(2단), E 채집

폴더
  Editor/   ForestMapBuilder(맵·Prefab 생성), GatherIKTool(채집 IK)
  Layouts/  지역별 배치·깊이·충돌선 데이터
  Meshes/   에셋별 3D 메시 데이터 (빌더가 Generated/Meshes에 Mesh 에셋으로 변환)
  Scripts/  플레이어, 카메라, 맵 이동, 채집, 이펙트
  Shaders/  WindLit(조명+바람 흔들림), Sprite25D(깊이 안개), SpriteAdditive(빛), WaterfallFlow(폭포), PlateBlend(배경판 이음)
  Sprites/  지역별 에셋 PNG, _n(노멀맵), _x(측면·윗면용 색 확장), Background_Plate.jpg(배경판)

v5 특징
  - 카메라가 각 구간 중앙에 오면 화면이 컨셉 그림과 그대로 겹치도록 3D 메시·배경판 위치를 원근 보정했습니다.
  - 게임 화면은 지역 높이(13~14.4 유닛) 전체를 보여 주고, 16:9 화면에서는 컨셉 그림의 가운데 부분이 보입니다.
  - 카메라 기울기는 2°(Main Camera > CameraFollow25D > Pitch). 0이면 그림과 가장 정확히, 키우면 지형 윗면이 더 보입니다.
  - URP는 그림자 거리(URP Asset의 Shadow Max Distance)를 50 이상으로 두세요.
