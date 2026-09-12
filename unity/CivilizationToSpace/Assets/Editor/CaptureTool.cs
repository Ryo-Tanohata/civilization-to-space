using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// Play Mode で6時代を順に選び、それぞれの画面をPNGへ書き出す。
    ///
    /// 見え方を口頭で述べるのではなく、画像で確かめられるようにするためのもの。
    /// 出力先はリポジトリ外の一時フォルダを指定する。リポジトリへ画像を追加しない。
    /// 描画を伴うため、batchmodeで走らせる場合は -nographics を付けない。
    ///
    /// 時代を選んだ直後に撮ると、ボタンの状態がまだ切り替わっていないことがある。
    /// 選択と撮影のあいだにフレームを挟む状態機械にしてある。
    /// </summary>
    public static class CaptureTool
    {
        private const string PendingKey = "CivilizationToSpace.CaptureTool.Pending";
        private const string OutputKey = "CivilizationToSpace.CaptureTool.Output";
        private const string ExitKey = "CivilizationToSpace.CaptureTool.Exit";
        private const string DirectoryArgument = "-outputDir";
        private const string SizeArgument = "-captureSize";
        private const string CatalogArgument = "-catalogPath";

        private const int DefaultWidth = 1280;
        private const int DefaultHeight = 720;

        private const string WidthKey = "CivilizationToSpace.CaptureTool.Width";
        private const string HeightKey = "CivilizationToSpace.CaptureTool.Height";

        /// <summary>Awake と最初の描画が済むまで回すフレーム数。</summary>
        private const int WarmUpFrames = 8;

        /// <summary>
        /// 時代を選んでから撮るまでに待つ秒数。
        /// 時代の移り変わりにかかる時間より長くする。短いと移り変わりの途中を撮り、
        /// 前後の時代が混ざった絵になる。
        /// </summary>
        private const string SettleArgument = "-settleSeconds";

        private static float SettleSeconds
        {
            get { return SessionState.GetFloat("CivilizationToSpace.CaptureTool.Settle", 1.8f); }
        }

        private static int warmedFrames;
        private static float selectedAt;
        private static int nextIndex;
        private static bool selected;
        private static bool failed;

        [MenuItem("Tools/Civilization to Space/6時代のキャプチャを書き出す", false, 301)]
        public static void CaptureWithDialog()
        {
            var directory = EditorUtility.SaveFolderPanel("キャプチャの出力先", string.Empty, string.Empty);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            Start(directory, false);
        }

        /// <summary>batchmode 用。-outputDir &lt;folder&gt; で出力先を渡す。</summary>
        public static void CaptureFromCommandLine()
        {
            var directory = ReadArgument(DirectoryArgument);
            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogError("[Capture] " + DirectoryArgument + " が指定されていません。");
                EditorApplication.Exit(1);
                return;
            }

            ReadSize();
            ReadCatalogOverride();

            var settle = ReadArgument(SettleArgument);
            float parsedSettle;
            SessionState.SetFloat(
                "CivilizationToSpace.CaptureTool.Settle",
                !string.IsNullOrEmpty(settle) && float.TryParse(settle, out parsedSettle) ? parsedSettle : 1.8f);
            Start(directory, true);
        }

        /// <summary>
        /// 縦横比を変えて確かめられるようにする。
        /// カメラのフレーミングとUIの版面は画面の大きさから決まるため、
        /// 大きさを変えて撮らないと、はみ出しの有無を確かめられない。
        /// </summary>
        private static void ReadSize()
        {
            var raw = ReadArgument(SizeArgument);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var parts = raw.Split('x');
            int parsedWidth;
            int parsedHeight;
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out parsedWidth) ||
                !int.TryParse(parts[1], out parsedHeight) ||
                parsedWidth < 64 || parsedHeight < 64)
            {
                Debug.LogWarning("[Capture] " + SizeArgument + " を解釈できませんでした。既定の大きさで撮ります。");
                return;
            }

            // Play Mode へ入るとドメインリロードで静的な値が消える。SessionState へ預ける。
            SessionState.SetInt(WidthKey, parsedWidth);
            SessionState.SetInt(HeightKey, parsedHeight);
        }

        private static int Width
        {
            get { return SessionState.GetInt(WidthKey, DefaultWidth); }
        }

        private static int Height
        {
            get { return SessionState.GetInt(HeightKey, DefaultHeight); }
        }

        /// <summary>
        /// 異常系の確認用に、読込先を一時フォルダの複製へ差し替える。
        /// リポジトリ内のJSONは書き換えない。
        /// </summary>
        private static void ReadCatalogOverride()
        {
            var path = ReadArgument(CatalogArgument);
            SessionState.SetString(AppRoot.CatalogPathOverrideKey, path ?? string.Empty);
        }

        private static void Start(string directory, bool exitWhenDone)
        {
            Directory.CreateDirectory(directory);
            SessionState.SetInt(WidthKey, SessionState.GetInt(WidthKey, DefaultWidth));
            SessionState.SetInt(HeightKey, SessionState.GetInt(HeightKey, DefaultHeight));
            EditorSceneManager.OpenScene(DemoSceneBuilder.ScenePath);

            SessionState.SetString(OutputKey, directory);
            SessionState.SetBool(ExitKey, exitWhenDone);
            SessionState.SetBool(PendingKey, true);

            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void HookAfterDomainReload()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                return;
            }

            warmedFrames = 0;
            selectedAt = 0f;
            nextIndex = 0;
            selected = false;
            failed = false;

            EditorApplication.update -= Observe;
            EditorApplication.update += Observe;
        }

        private static void Observe()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (warmedFrames < WarmUpFrames)
            {
                warmedFrames++;
                return;
            }

            var appRoot = UnityEngine.Object.FindObjectOfType<AppRoot>();
            var camera = Camera.main;
            if (appRoot == null || camera == null)
            {
                Debug.LogError("[Capture] シーンに AppRoot または Main Camera がありません。");
                Finish(false);
                return;
            }

            var directory = SessionState.GetString(OutputKey, string.Empty);

            if (nextIndex == 0 && !selected)
            {
                Debug.Log("[Capture] 実際の画面の大きさ " + Screen.width + "x" + Screen.height +
                          " / 書き出し " + Width + "x" + Height);

                if (appRoot.LoadFailed)
                {
                    Save(camera, Path.Combine(directory, "00-error.png"));
                    Debug.Log("[Capture] 読込に失敗した画面を1枚書き出しました。");
                    Finish(false);
                    return;
                }
            }

            var timeline = appRoot.Timeline;

            if (nextIndex >= timeline.Count)
            {
                Debug.Log("[Capture] " + timeline.Count + "枚を書き出しました。");
                Finish(!failed);
                return;
            }

            if (!selected)
            {
                timeline.Select(nextIndex);
                Canvas.ForceUpdateCanvases();
                selected = true;
                selectedAt = Time.realtimeSinceStartup;
                return;
            }

            if (Time.realtimeSinceStartup - selectedAt < SettleSeconds)
            {
                return;
            }

            var era = timeline.Current;
            var label = timeline.HeadIndex >= 0
                ? "Form" + (timeline.HeadIndex + 1)
                : timeline.TailIndex >= 0 ? "Moon" + (timeline.TailIndex + 1) : era.Id;
            if (timeline.Index != nextIndex)
            {
                Debug.LogError("[Capture] 選択が反映されていません。要求 " + nextIndex + " / 実際 " + timeline.Index);
                failed = true;
            }

            Save(camera, Path.Combine(directory, Pad2(nextIndex + 1) + "-" + label + ".png"));
            nextIndex++;
            selected = false;
        }

        private static void Finish(bool ok)
        {
            EditorApplication.update -= Observe;
            SessionState.SetBool(PendingKey, false);

            if (SessionState.GetBool(ExitKey, false))
            {
                EditorApplication.Exit(ok ? 0 : 1);
                return;
            }

            EditorApplication.isPlaying = false;
        }

        private static void Save(Camera camera, string path)
        {
            var texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;

            camera.targetTexture = texture;

            // カメラの置き直しも描画先の大きさから決まる。差し替えた直後にやり直す。
            var framing = camera.GetComponent<CivilizationToSpace.View.EarthFraming>();
            if (framing != null)
            {
                framing.Apply();
            }

            // ScreenSpaceCameraのCanvasは、カメラの描画先の大きさに合わせて版面を組み直す。
            // 描画先を差し替えた直後に組み直しておかないと、Game Viewの大きさのまま写ってしまう。
            Canvas.ForceUpdateCanvases();

            camera.Render();

            RenderTexture.active = texture;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            image.Apply();

            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            File.WriteAllBytes(path, image.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(image);
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static string Pad2(int value)
        {
            return value < 10 ? "0" + value : value.ToString();
        }
    }
}
