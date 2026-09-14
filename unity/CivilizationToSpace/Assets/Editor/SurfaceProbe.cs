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

            Capture(directory, "1-creatures", Creatures(), new Vector3(0f, 5.5f, -34f), -2.5f);
            Capture(directory, "2-hamlet", Hamlet(), new Vector3(0f, 20f, -90f), 3f);
            Capture(directory, "3-city", City(), new Vector3(0f, 58f, -200f), 4f);
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
                Ground = Hex(0x4C6030),
                Trunk = Hex(0x5B4632),
                Foliage = Hex(0x4C7A3A),
                Creature = Hex(0x8A7A55),
                Building = Hex(0xB9BCC0),
                Window = Hex(0x6E8FA8),
                NearZ = 13f,
                FarZ = 260f,
                HalfWidth = 95f,
                PlantHeight = 24f,
                BuildingHeight = 0f,
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
                Ground = Hex(0x5C6A36),
                Trunk = Hex(0x54432F),
                Foliage = Hex(0x4E7A3E),
                Creature = Hex(0x7A6A55),
                Building = Hex(0xC2A882),
                Window = Hex(0x6E8FA8),
                NearZ = 55f,
                FarZ = 420f,
                HalfWidth = 150f,
                PlantHeight = 16f,
                BuildingHeight = 7f,
                Conifers = 18,
                Ferns = 26,
                Broadleaves = 20,
                Quadrupeds = 0,
                Bipeds = 0,
                Buildings = 30,
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
            Vector3 eye, float pitch)
        {
            // 太陽は斜め後ろから当てる。真正面からだと影が出ず、形が平らに見える。
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = Color.white;
            light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);

            // 空からの回り込みぶん。真っ黒にすると日陰が潰れる。
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = land.SkyLow * 0.5f;

            var host = new GameObject("Surface");
            var view = host.AddComponent<SurfaceView>();
            view.Build(land, HideFlags.DontSave);

            var cameraObject = new GameObject("Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = eye;
            camera.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);
            camera.fieldOfView = 52f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = land.SkyHigh;
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
