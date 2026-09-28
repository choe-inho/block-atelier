# 블록 아틀리에 (Unity)

블록을 놓아 줄을 지우면, 지운 색이 픽셀 그림을 칠하는 하이브리드 캐주얼 퍼즐.

## 지금 들어 있는 것

| 경로 | 내용 |
| --- | --- |
| `Assets/_Project/Core` | 게임 규칙 전체 (UnityEngine 의존 없음). `GameSession`이 한 판을 담당 |
| `Assets/_Project/Core/Sim/AutoSolver.cs` | 자동 풀이 봇. 난이도 측정용, 나중에 힌트 기능에도 사용 |
| `Assets/_Project/Tests` | 규칙 테스트 16개 (Unity Test Runner, EditMode) |
| `Assets/_Project/Data/Levels` | 동물 앨범 레벨 10개 JSON + `balance_report.tsv` |
| `Assets/_Project/Art/Previews` | 그림 10장 미리보기 |
| `Tools/LevelTool` | 레벨 생성·밸런싱 도구 (봇으로 클리어율을 재서 블록 순서를 고름) |
| `Tools/art` | 픽셀 그림 원본과 미리보기 렌더러 (Python) |
| `Tools/WebPrototype` | 폰에서 해보는 웹 프로토타입 (같은 규칙의 JS 버전) |

뷰(MonoBehaviour), 광고, 결제는 아직 없다. 다음 단계에서 코어 위에 붙인다.

## Unity에서 여는 법

1. Unity Hub에서 2D 템플릿으로 새 프로젝트를 만든다 (Unity 6 LTS 권장).
2. 이 폴더의 `Assets/_Project`를 새 프로젝트의 `Assets` 아래에 복사한다.
3. `Window > General > Test Runner > EditMode > Run All`. 16개가 모두 통과해야 한다.

레벨 불러오기 예시:

```csharp
var json = File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "Levels/level_001.json"));
var game = new GameSession(LevelData.FromJson(json));
var result = game.TryPlace(BlockSource.Conveyor(0), 2, 3);   // null이면 놓을 수 없는 자리
```

출시 빌드에서는 `Data/Levels`를 `StreamingAssets/Levels`나 Addressables로 옮긴다.

## 규칙 요약 (기획서 3장과 같음)

- 8x8 보드, 컨베이어 5칸 중 앞 3칸 선택, 보관함 1칸. 블록을 놓을 때만 컨베이어가 전진한다.
- 줄이 차면 지워지고, 지운 칸 하나가 페인트 한 방울. 그림에 필요한 색만 칠해진다.
- 같은 색 한 줄은 2배(붓질), 가로와 세로를 함께 지우면 교차점 3x3 추가 제거(십자 폭발).
- 한 색을 다 칠하면 그 색 블록은 회색이 된다. 레벨의 `grayOnComplete`가 false면 색이 남는다 (1~20레벨).
- 선택 가능한 블록과 보관함 블록 모두 놓을 곳이 없고 보관으로 새 블록을 볼 수도 없으면 실패. 이어하기는 회색 3칸 제거 + 작은 블록 3개.

## 레벨 다시 만들기 (Unity 없이)

Mono가 있으면 터미널에서:

```bash
mcs -langversion:7.2 -optimize+ -out:LevelTool.exe $(find Assets/_Project/Core -name '*.cs') Tools/LevelTool/Program.cs
mono LevelTool.exe Tools/LevelTool/pictures.json Assets/_Project/Data/Levels 40 200
```

난이도 목표는 `Tools/LevelTool/Program.cs`의 `Plans` 표에서 바꾼다. 봇의 사람 흉내 정도(`HumanNoise`)는 실제 플레이 테스트 결과로 보정해야 한다.

테스트도 Unity 없이 돌릴 수 있다:

```bash
mcs -langversion:7.2 -out:tests.exe $(find Assets/_Project/Core -name '*.cs') Assets/_Project/Tests/*.cs Tools/TestShim/NUnitShim.cs
mono tests.exe
```
