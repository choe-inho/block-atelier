// Claude가 Unity 에디터를 원격으로 다루기 위한 작은 다리.
// - 컴파일 결과를 Logs/claude_compile.txt에 남긴다.
// - 콘솔의 경고와 에러를 Logs/claude_console.txt에 남긴다.
// - Logs/claude_cmd.txt에 적힌 명령(refresh, play, stop, tests, menu, open)을 실행한다.
// 게임 코드와 독립된 어셈블리라서 게임 코드가 컴파일에 실패해도 계속 동작한다.
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace BlockAtelier.Bridge
{
    [InitializeOnLoad]
    public static class ClaudeBridge
    {
        static readonly string LogDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
        static string P(string name) { return Path.Combine(LogDir, name); }
        static double nextPoll;
        static readonly object fileLock = new object();

        static ClaudeBridge()
        {
            Directory.CreateDirectory(LogDir);
            PlayerSettings.runInBackground = true;
            CompilationPipeline.compilationStarted += _ => Write("claude_compile.txt", "컴파일 시작 " + Now() + "\n", false);
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
            Application.logMessageReceivedThreaded += OnLog;
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += s => Write("claude_ack.txt", Now() + " 상태 " + s + "\n", true);
            Write("claude_status.txt", "브리지 로드 " + Now() + " Unity " + Application.unityVersion + "\n", false);
        }

        static string Now() { return DateTime.Now.ToString("HH:mm:ss"); }

        static void Write(string file, string text, bool append)
        {
            lock (fileLock)
            {
                try
                {
                    if (append) File.AppendAllText(P(file), text, Encoding.UTF8);
                    else File.WriteAllText(P(file), text, Encoding.UTF8);
                }
                catch (Exception) { }
            }
        }

        static void OnAssemblyCompiled(string asm, CompilerMessage[] messages)
        {
            var sb = new StringBuilder();
            int errors = 0;
            foreach (var m in messages)
            {
                if (m.type == CompilerMessageType.Error) errors++;
                sb.Append(m.type == CompilerMessageType.Error ? "ERROR " : "WARN  ").Append(m.message).Append('\n');
            }
            Write("claude_compile.txt", "[" + Path.GetFileName(asm) + "] 에러 " + errors + "\n" + sb, true);
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            var line = Now() + " " + type + " " + msg + "\n";
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                line += stack + "\n";
            try
            {
                var fi = new FileInfo(P("claude_console.txt"));
                if (fi.Exists && fi.Length > 400000) Write("claude_console.txt", "", false);
            }
            catch (Exception) { }
            Write("claude_console.txt", line, true);
        }

        static void Poll()
        {
            // 에디터가 뒤에 있어도 플레이 모드가 계속 돌도록
            if (EditorApplication.isPlaying && !EditorApplication.isPaused) EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.4;
            var path = P("claude_cmd.txt");
            if (!File.Exists(path)) return;
            string[] lines;
            try { lines = File.ReadAllLines(path); File.Delete(path); }
            catch (Exception) { return; }

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                string result;
                try { result = Run(line); }
                catch (Exception e) { result = "실패: " + e.Message; }
                Write("claude_ack.txt", Now() + " " + line + " → " + result + "\n", true);
            }
        }

        static string Run(string line)
        {
            int sp = line.IndexOf(' ');
            string cmd = sp < 0 ? line : line.Substring(0, sp);
            string arg = sp < 0 ? "" : line.Substring(sp + 1).Trim();
            switch (cmd)
            {
                case "refresh":
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    return "ok";
                case "recompile":
                    CompilationPipeline.RequestScriptCompilation();
                    return "ok";
                case "play":
                    EditorApplication.isPlaying = true;
                    return "ok";
                case "stop":
                    EditorApplication.isPlaying = false;
                    return "ok";
                case "menu":
                    return EditorApplication.ExecuteMenuItem(arg) ? "ok" : "메뉴 없음";
                case "open":
                    EditorSceneManager.OpenScene(arg);
                    return "ok";
                case "save":
                    EditorSceneManager.SaveOpenScenes();
                    return "ok";
                case "tests":
                    RunTests();
                    return "시작";
                case "ping":
                    return "pong " + (EditorApplication.isCompiling ? "컴파일 중" : "대기") + (EditorApplication.isPlaying ? " 플레이 중" : "");
                default:
                    return "알 수 없는 명령";
            }
        }

        static void RunTests()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new TestCallbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        sealed class TestCallbacks : ICallbacks
        {
            readonly StringBuilder sb = new StringBuilder();
            public void RunStarted(ITestAdaptor testsToRun) { sb.Length = 0; sb.Append("테스트 시작 " + Now() + "\n"); }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
                sb.Append(result.TestStatus == TestStatus.Passed ? "통과  " : "실패  ").Append(result.Name);
                if (result.TestStatus != TestStatus.Passed) sb.Append(" : ").Append(result.Message);
                sb.Append('\n');
            }
            public void RunFinished(ITestResultAdaptor result)
            {
                sb.Append("결과: 통과 " + result.PassCount + ", 실패 " + result.FailCount + "\n");
                Write("claude_tests.txt", sb.ToString(), false);
            }
        }
    }
}
