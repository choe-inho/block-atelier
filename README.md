# 블록 아틀리에 (Unity)

블록을 놓아 줄을 지우면, 지운 색이 픽셀 그림을 칠하는 하이브리드 캐주얼 퍼즐.

## 지금 들어 있는 것

| 경로 | 내용 |
| --- | --- |
| `Assets/_Project/Game` | 화면, 조작, 이펙트, 효과음 (코드로 조립, 에셋 없이 동작) |
| `Assets/_Project/Editor/Bridge` | Claude 자동화 다리: 컴파일·콘솔 로그, 명령 파일, 테스트 실행 |
| `Assets/_Project/Core` | 게임 규칙 전체 (UnityEngine 의존 없음). `GameSession`이 한 판을 담당 |
| `Assets/_Project/Core/Sim/AutoSolver.cs` | 자동 풀이 봇. 난이도 측정용, 나중에 힌트 기능에도 사용 |
| `Assets/_Project/Tests` | 규칙 테스트 34개 (Unity Test Runner, EditMode) |
| `Assets/_Project/Resources/Levels` | 앨범 10개 × 10 = 레벨 100개 JSON |
| `Assets/_Project/Art/Previews` | 앨범별 그림 미리보기 (칠한 모습 / 칠하기 전) |
| `Tools/LevelTool` | 레벨 생성·밸런싱 도구 (봇으로 클리어율을 재서 블록 순서를 고름) |
| `Tools/art` | 24x24 픽셀 그림 원본(벡터 도형 → 픽셀, 자동 명암·윤곽선)과 미리보기 렌더러 (Python) |
| `Tools/WebPrototype` | 폰에서 해보는 웹 프로토타입 (같은 규칙의 JS 버전) |

광고, 결제, 분석 SDK는 아직 없다. 코어와 화면 위에 인터페이스로 붙일 예정.

## 플레이

`Assets/Scenes/SampleScene`을 열고 Play. 씬에 아무것도 없어도 `GameRoot`가 자동으로 생긴다.
Game 뷰 해상도는 세로 폰 비율(예: 1080x2340)로 두면 실제 화면과 같다.

- 블록을 끌어 보드에 놓는다. 보관함에 끌어 넣으면 보관.
- 줄을 지우면 페인트가 그림으로 날아간다. 같은 색 한 줄은 2배(붓질), 가로·세로 동시 제거는 십자 폭발.
- 앱을 켜면 홈: 이어서 그릴 그림(그리던 판이 있으면 그대로 이어짐), 앨범 책장(잠금·진행·별), 마이페이지(닉네임, 대표 그림, 기록), 설정(소리, 효과 줄이기, 튜토리얼 다시 보기, 진행 초기화).
- 게임 화면의 '홈' 버튼으로 돌아간다. 앨범을 누르면 그 앨범의 레벨 목록. 앞 레벨을 깨야 다음 레벨이 열린다.
- 닉네임은 2~10자 한글·영문·숫자(Core/Profile.cs). 폰에서는 시스템 키보드, 에디터에서는 개발 명령 `nick 이름`.
- 별: 레벨의 `stars.three` / `stars.two` 수 이하로 완성하면 별 3개 / 2개, 그 밖이나 이어하기를 쓰면 1개. 화면 오른쪽 위 별 게이지가 지금 수로 받을 별을 보여 준다.
- 시간: 첫 블록을 놓은 순간부터, 조작할 수 있던 시간만 잰다 (연출·창·앱 전환 중엔 멈춤). 이어하기를 쓴 판은 시간 기록 제외.
- 기록은 레벨마다 최고 별과 최단 시간만 `PlayerPrefs`의 `ba_progress`에 저장한다 (`Core/Progress.cs`, 서버 동기화 때 그대로 병합).

## 이펙트 조절

`Game/Fx.cs` 위쪽의 `DropTime`, `DropStagger`, `ShakePerLine`, `ShakeExplode`와
`GameRoot.PlayMove`의 대기 시간으로 손맛을 조절한다. 블룸은 `GameRoot.SetupPost`.

## 에디터 자동화 (개발용)

`Logs/claude_cmd.txt`에 한 줄씩 쓰면 에디터가 실행한다: `refresh`, `play`, `stop`, `tests`, `projectsetup`, `ping`.
플레이 중에는 `Logs/claude_game.txt`로 게임 명령을 보낸다:
`level N`, `home`, `mypage`, `settings`, `album N`, `press 버튼글자`, `nick 이름`, `demo`, `demo off`, `speed X`, `wait 초`, `shot 이름`, `shots 이름 개수 간격`,
`place 슬롯 x y`, `fillrow y 색 빈칸`, `fillcol x 색 빈칸`, `give 모양 색 ...`, `until Won 초`, `state`, `reset`.
캡처는 `Logs/shots/`에 1080x2340으로 저장된다.

## Unity에서 여는 법

1. Unity Hub에서 2D 템플릿으로 새 프로젝트를 만든다 (Unity 6 LTS 권장).
2. 이 폴더의 `Assets/_Project`를 새 프로젝트의 `Assets` 아래에 복사한다.
3. `Window > General > Test Runner > EditMode > Run All`. 34개가 모두 통과해야 한다.

레벨 불러오기 예시:

```csharp
var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Levels/level_001.json"));
var game = new GameSession(LevelData.FromJson(json));
var result = game.TryPlace(BlockSource.Conveyor(0), 2, 3);   // null이면 놓을 수 없는 자리
```

출시 빌드에서는 `Data/Levels`를 `StreamingAssets/Levels`나 Addressables로 옮긴다.

## 규칙 요약 (기획서 3장과 같음)

- 8x8 보드, 컨베이어 5칸 중 앞 3칸 선택, 보관함 1칸. 블록을 놓을 때만 컨베이어가 전진한다.
- 줄이 차면 지워지고, 지운 칸 하나가 페인트 한 방울. 한 방울이 레벨의 `brush` 수만큼 픽셀을 칠한다. 그림에 필요한 색만 칠해진다.
- 그림의 `L` 픽셀은 처음부터 그려진 윤곽선, `shades`는 픽셀별 명암(0 밝음, 1 기본, 2 어두움).
- 같은 색 한 줄은 2배(붓질), 가로와 세로를 함께 지우면 교차점 3x3 추가 제거(십자 폭발).
- 한 색을 다 칠하면 컨베이어의 그 색 블록은 아직 필요한 색으로 바뀐다. 21레벨부터(`grayOnComplete`)는 대신 회색이 되어 방해물이 된다.
- 선택 가능한 블록과 보관함 블록 모두 놓을 곳이 없고 보관으로 새 블록을 볼 수도 없으면 실패. 이어하기는 회색 3칸 제거 + 작은 블록 3개.

## 레벨 다시 만들기 (Unity 없이)

Mono가 있으면 터미널에서:

```bash
mcs -langversion:7.2 -optimize+ -out:LevelTool.exe $(find Assets/_Project/Core -name '*.cs') Tools/LevelTool/Program.cs
python3 Tools/art/export.py Tools/LevelTool/pictures.json          # 그림 100장 → JSON
mono LevelTool.exe Tools/LevelTool/pictures.json Assets/_Project/Resources/Levels 30 120 1 50 &
mono LevelTool.exe Tools/LevelTool/pictures.json Assets/_Project/Resources/Levels 30 120 51 100
```

인자: 그림 JSON, 출력 폴더, 탐색 횟수, 확인 횟수, 시작 레벨, 끝 레벨 (범위를 나눠 병렬 실행).
그림 미리보기: `cd Tools/art && python3 sheet.py a01_animals a02_sea out.png`

별 기준 보정 (레벨을 다시 만든 뒤 반드시 실행):

```bash
mcs -langversion:7.2 -optimize+ -out:StarTool.exe $(find Assets/_Project/Core -name '*.cs') Tools/LevelTool/StarTool.cs
mono StarTool.exe Assets/_Project/Resources/Levels 200 1 50 &
mono StarTool.exe Assets/_Project/Resources/Levels 200 51 100
```

사람 흉내 봇으로 깬 판의 수 분포를 재서, 별 3개 25% / 2개 이상 65% (1~10레벨은 45% / 85%)가 되도록 맞춘다.
새 표본으로 검증해서 벗어나면 표본을 합쳐 다시 맞추기를 반복하고, 결과는 `Tools/LevelTool/star_report.tsv`.

난이도 목표는 `Tools/LevelTool/Program.cs`의 `Plans` 표에서 바꾼다. 봇의 사람 흉내 정도(`HumanNoise`)는 실제 플레이 테스트 결과로 보정해야 한다.

테스트도 Unity 없이 돌릴 수 있다:

```bash
mcs -langversion:7.2 -out:tests.exe $(find Assets/_Project/Core -name '*.cs') Assets/_Project/Tests/*.cs Tools/TestShim/NUnitShim.cs
mono tests.exe
```
