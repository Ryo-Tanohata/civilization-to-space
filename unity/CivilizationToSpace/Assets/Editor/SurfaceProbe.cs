using System.IO;
using CivilizationToSpace.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 地表から見た風景を1枚のPNGへ書き出す。
    ///
    /// 絵の言語が成立するかを、作り込む前に見て決めるための道具である。
    /// 9つの場面を作ってから「これでは読めない」と分かると、下流がすべてやり直しになる。
    /// まず1場面だけ出して判断する。
    ///
    /// Play Mode を使わない。地表の見せ物は光を必要とせず（空は自ら光り、
    /// 手前のものは黒い影である）、組んで撮るだけで足りるためである。
    /// </summary>
    public static class SurfaceProbe
    {
        private const string OutputArgument = "-outputPath";
        private const int Width = 1280;
        private const int Height = 720;

        [MenuItem("Tools/Civilization to Space/地表の見え方を書き出す", false, 303)]
        public static void Run()
        {
            var path = EditorUtility.SaveFilePanel("地表の書き出し先", string.Empty, "surface.png", "png");
            if (!string.IsNullOrEmpty(path))
            {
                Capture(path);
            }
        }

        /// <summary>batchmode 用。-outputPath &lt;file.png&gt; で書き出し先を渡す。</summary>
        public static void RunFromCommandLine()
        {
            var path = ReadArgument(OutputArgument);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Surface] " + OutputArgument + " が指定されていません。");
                EditorApplication.Exit(1);
                return;
            }

            Capture(path);
            EditorApplication.Exit(0);
        }

        private static void Capture(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 空だけが光り、手前のものは黒い影になる。回り込む光は要らない。
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var host = new GameObject("Surface");
            var view = host.AddComponent<SurfaceView>();

            // 巨大生物の時代。**特定の種を表していない。**
            // 背の高い針葉樹と、首の長い四つ足、小さな二本足を置いているだけである。
            view.Build(new SurfaceView.Landscape
            {
                SkyHigh = new Color32(0x3E, 0x6F, 0xA8, 0xFF),
                SkyLow = new Color32(0xE6, 0xC7, 0x9A, 0xFF),
                PlantHeight = 20f,
                Conifers = 18,
                Ferns = 26,
                Broadleaves = 9,
                Quadrupeds = 5,
                Bipeds = 6,
                Seed = 5,
            }, HideFlags.DontSave);

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = SurfaceView.EyePosition;

            // 少しだけ見上げる。地平線を真ん中に置くと空も地面も半分ずつになり、
            // 立っているものの背の高さが読めない。
            camera.transform.rotation = Quaternion.Euler(-6f, 0f, 0f);
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 400f;

            var texture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            texture.Create();

            var previousActive = RenderTexture.active;
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;

            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            image.Apply();

            camera.targetTexture = null;
            RenderTexture.active = previousActive;

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("[Surface] 書き出しました " + Width + "x" + Height);

            Object.DestroyImmediate(image);
            texture.Release();
            Object.DestroyImmediate(texture);
            view.Clear();
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(cameraObject);
        }

        private static string ReadArgument(string name)
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
