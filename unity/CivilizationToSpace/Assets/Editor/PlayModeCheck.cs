using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// EarthTimelineDemo を Play Mode で起動し、AppRoot が読み込みに成功したかを報告する。
    ///
    /// 起動確認を口頭の手順ではなく再実行できる検証として残すためのものである。
    /// Play Mode へ入るとドメインリロードで静的な状態が消えるため、
    /// 継続の意思は SessionState に預ける。SessionState はUnityを終了するまで残る。
    /// </summary>
    public static class PlayModeCheck
    {
        private const string PendingKey = "CivilizationToSpace.PlayModeCheck.Pending";
        private const string ExitKey = "CivilizationToSpace.PlayModeCheck.Exit";

        /// <summary>Awake が済んでいることを確かめるために少しだけ回す。</summary>
        private const int FramesToObserve = 5;

        private static int observedFrames;

        [MenuItem("Tools/Civilization to Space/Play Mode で起動を確認", false, 300)]
        public static void Run()
        {
            Start(false);
        }

        /// <summary>batchmode 用。読込に失敗したらプロセスを 1 で終える。</summary>
        public static void RunFromCommandLine()
        {
            Start(true);
        }

        private static void Start(bool exitWhenDone)
        {
            EditorSceneManager.OpenScene(DemoSceneBuilder.ScenePath);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(ExitKey, exitWhenDone);
            observedFrames = 0;
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void HookAfterDomainReload()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                return;
            }

            observedFrames = 0;
            EditorApplication.update -= Observe;
            EditorApplication.update += Observe;
        }

        private static void Observe()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            observedFrames++;
            if (observedFrames < FramesToObserve)
            {
                return;
            }

            EditorApplication.update -= Observe;
            SessionState.SetBool(PendingKey, false);

            var appRoot = Object.FindAnyObjectByType<AppRoot>();
            var ok = Report(appRoot);

            if (SessionState.GetBool(ExitKey, false))
            {
                EditorApplication.Exit(ok ? 0 : 1);
                return;
            }

            EditorApplication.isPlaying = false;
        }

        private static bool Report(AppRoot appRoot)
        {
            if (appRoot == null)
            {
                Debug.LogError("[PlayMode] シーンに AppRoot がありません。");
                return false;
            }

            if (appRoot.LoadFailed)
            {
                Debug.LogError("[PlayMode] AppRoot は起動しましたが読込に失敗しました: " + appRoot.FailureMessage);
                return false;
            }

            var catalog = appRoot.Catalog;
            var builder = new System.Text.StringBuilder();
            builder.Append("[PlayMode] AppRoot は Play Mode で読込に成功しました。")
                .Append(" 時代 ").Append(catalog.Eras.Count).Append("件");

            for (var i = 0; i < catalog.Eras.Count; i++)
            {
                builder.Append('\n').Append(i + 1).Append(' ').Append(catalog.Eras[i].Id)
                    .Append(" | ").Append(catalog.Eras[i].DisplayName);
            }

            Debug.Log(builder.ToString());
            return true;
        }
    }
}
