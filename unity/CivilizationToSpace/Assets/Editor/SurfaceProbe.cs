using System.IO;
using CivilizationToSpace.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 地表から見た風景をPNGへ書き出す。
    ///
    /// 絵の言語が成立するかを、作り込む前に見て決めるための道具である。
    /// 場面を作り込んでから「これでは読めない」と分かると、下流がすべてやり直しになる。
    ///
    /// **寄り方は2つある。** 生きものが画面に収まる寄りと、街が画面に収まる寄りである。
    /// 同じ作りのまま、置くものの大きさと数と、カメラの位置だけを変えている。
    /// </summary>
    public static class SurfaceProbe
    {
        private const string OutputArgument = "-outputDir";
        private const int Width = 1280;
        private const int Height = 720;

        [MenuItem("Tools/Civilization to Space/地表の見え方を書き出す", false, 303)]
        public static void Run()
        {
            var directory = EditorUtility.SaveFolderPanel("地表の書き出し先", string.Empty, string.Empty);
            if (!string.IsNullOrEmpty(directory))
            {
                CaptureAll(directory);
            }
        }

        /// <summary>batchmode 用。-outputDir &lt;folder&gt; で書き出し先を渡す。</summary>
        public static void RunFromCommandLine()
        {
            var directory = ReadArgument(OutputArgument);
            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogError("[Surface] " + OutputArgument + " が指定されていません。");
                EditorApplication.Exit(1);
                return;
            }

            CaptureAll(directory);
            EditorApplication.Exit(0);
        }

        private static readonly string[] EraNames =
        {
            "01-Hadean", "02-EarlyOcean", "03-Snowball", "04-GreenEarth", "05-Dinosaurs",
            "06-Impact", "07-IceAge", "08-Humans", "09-Information", "10-Future",
        };

        private static void CaptureAll(string directory)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            for (var era = 0; era < EraNames.Length; era++)
            {
                Vector3 eye;
                float pitch;
                SurfaceCatalog.EyeForEra(era, out eye, out pitch);

                Capture(directory, EraNames[era] + "-day", SurfaceCatalog.ForEra(era), eye, pitch, 0.5f);
            }

            // 夜も何枚か撮る。星と灯りが出ることを確かめる。
            foreach (var era in new[] { 4, 7, 8 })
            {
                Vector3 eye;
                float pitch;
                SurfaceCatalog.EyeForEra(era, out eye, out pitch);
                Capture(directory, EraNames[era] + "-night", SurfaceCatalog.ForEra(era), eye, pitch, 0.02f);
            }

            // 落ちてくるものを、順を追って撮る。
            foreach (var era in new[] { 0, 5 })
            {
                Vector3 eye;
                float pitch;
                SurfaceCatalog.EyeForEra(era, out eye, out pitch);
                var phases = new[] { 0.10f, 0.45f, 0.68f, 0.76f, 0.86f };
                for (var i = 0; i < phases.Length; i++)
                {
                    Capture(directory, EraNames[era] + "-impact" + (i + 1),
                        SurfaceCatalog.ForEra(era), eye, pitch, 0.5f, phases[i]);
                }
            }
        }

        private static void Capture(string directory, string name, SurfaceView.Landscape land,
            Vector3 eye, float pitch, float timeOfDay)
        {
            Capture(directory, name, land, eye, pitch, timeOfDay, -1f);
        }

        private static void Capture(string directory, string name, SurfaceView.Landscape land,
            Vector3 eye, float pitch, float timeOfDay, float impactPhase)
        {
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;

            var host = new GameObject("Surface");
            var view = host.AddComponent<SurfaceView>();
            view.Build(land, HideFlags.DontSave);

            // 空の色・星の明るさ・光の向きと強さは、時刻からまとめて決まる。
            view.SetTimeOfDay(timeOfDay, light);
            view.SetImpact(impactPhase < 0f ? 0.99f : impactPhase);

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = eye;
            camera.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            camera.fieldOfView = 52f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = land.FarZ * 3f;

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

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            Debug.Log("[Surface] 書き出しました " + name);

            Object.DestroyImmediate(image);
            texture.Release();
            Object.DestroyImmediate(texture);
            view.Clear();
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(lightObject);
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
