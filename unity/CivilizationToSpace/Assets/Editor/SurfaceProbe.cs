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

        private static void CaptureAll(string directory)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1日のうち4つの時刻で撮る。昼夜が巡ることを1枚ずつで確かめる。
            var creatureEye = new Vector3(0f, 5.5f, -30f);
            Capture(directory, "1-noon", Creatures(), creatureEye, -2.5f, 0.50f);
            Capture(directory, "2-sunset", Creatures(), creatureEye, -2.5f, 0.75f);
            Capture(directory, "3-night", Creatures(), creatureEye, -2.5f, 0.00f);
            Capture(directory, "4-dawn", Creatures(), creatureEye, -2.5f, 0.28f);

            Capture(directory, "5-hamlet-night", Hamlet(), new Vector3(0f, 8f, -20f), 2f, 0.02f);
            Capture(directory, "6-city-night", City(), new Vector3(0f, 58f, -200f), 4f, 0.04f);
        }

        /// <summary>
        /// 巨大生物の時代。**特定の種を表していない。**
        /// 首の長い四つ足と小さな二本足を、背の高い針葉樹の中に置いているだけである。
        /// 色は読みやすさのための決めで、復元色ではない。
        /// </summary>
        private static SurfaceView.Landscape Creatures()
        {
            return new SurfaceView.Landscape
            {
                SkyHigh = Hex(0x5C8FC8),
                SkyLow = Hex(0xCBDCE6),
                Ground = Hex(0x6E5C3E),
                Trunk = Hex(0x5B4632),
                Foliage = Hex(0x4C7A3A),
                Creature = Hex(0x8A7A55),
                Building = Hex(0xB9BCC0),
                Window = Hex(0x6E8FA8),
                NearZ = 7f,
                FarZ = 260f,
                HalfWidth = 95f,
                PlantHeight = 26f,
                BuildingHeight = 0f,
                BuildingSpacing = 1.05f,
                Windows = false,
                Conifers = 34,
                Ferns = 46,
                Broadleaves = 22,
                Quadrupeds = 3,
                Bipeds = 4,
                Buildings = 0,
                Seed = 5,
            };
        }

        /// <summary>人類の広がり。低い建物が少しだけ集まっている段階。</summary>
        private static SurfaceView.Landscape Hamlet()
        {
            return new SurfaceView.Landscape
            {
                SkyHigh = Hex(0x6796C6),
                SkyLow = Hex(0xD8E4EA),
                Ground = Hex(0x6A6B3C),
                Trunk = Hex(0x54432F),
                Foliage = Hex(0x4E7A3E),
                Creature = Hex(0x7A6A55),
                Building = Hex(0xC2A882),
                Window = Hex(0x6E8FA8),
                NearZ = 12f,
                FarZ = 110f,
                HalfWidth = 46f,
                PlantHeight = 9f,
                BuildingHeight = 5f,
                BuildingSpacing = 1.05f,
                Windows = false,
                Conifers = 18,
                Ferns = 26,
                Broadleaves = 20,
                Quadrupeds = 0,
                Bipeds = 0,
                Buildings = 46,
                Seed = 8,
            };
        }

        /// <summary>情報・地球規模接続。建物が高くなり、数が増えた段階。</summary>
        private static SurfaceView.Landscape City()
        {
            return new SurfaceView.Landscape
            {
                SkyHigh = Hex(0x6F9AC8),
                SkyLow = Hex(0xDDE7EC),
                Ground = Hex(0x515C46),
                Trunk = Hex(0x4E4034),
                Foliage = Hex(0x46683C),
                Creature = Hex(0x7A6A55),
                Building = Hex(0xB6BABF),
                Window = Hex(0x5F87A6),
                NearZ = 110f,
                FarZ = 900f,
                HalfWidth = 330f,
                PlantHeight = 16f,
                BuildingHeight = 52f,
                BuildingSpacing = 2.1f,
                Windows = true,
                Conifers = 16,
                Ferns = 0,
                Broadleaves = 26,
                Quadrupeds = 0,
                Bipeds = 0,
                Buildings = 150,
                Seed = 9,
            };
        }

        private static Color Hex(uint value)
        {
            return new Color32(
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF),
                0xFF);
        }

        private static void Capture(string directory, string name, SurfaceView.Landscape land,
            Vector3 eye, float pitch, float timeOfDay)
        {
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;

            var host = new GameObject("Surface");
            var view = host.AddComponent<SurfaceView>();
            view.Build(land, HideFlags.DontSave);

            // 空の色・星の明るさ・光の向きと強さは、時刻からまとめて決まる。
            view.SetTimeOfDay(timeOfDay, light);

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
