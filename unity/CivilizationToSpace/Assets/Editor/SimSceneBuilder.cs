using System.Collections.Generic;
using CivilizationToSpace;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// FormationSim シーンを生成し直す。
    ///
    /// .unity をテキストとして直接編集しない。壊れたときはこのメニューから作り直す。
    /// 置くのはカメラ・光・<see cref="SimRoot"/> だけである。天体は実行時に計算で生まれる。
    /// </summary>
    public static class SimSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/FormationSim.unity";
        private const string SceneFolder = "Assets/Scenes";
        private const string RootName = "SimRoot";

        [MenuItem("Tools/Civilization to Space/FormationSim シーンを生成し直す", false, 3)]
        public static void RebuildScene()
        {
            Build();
            Debug.Log("[Scene] " + ScenePath + " を生成しました。");
        }

        /// <summary>batchmode 用。</summary>
        public static void BuildFromCommandLine()
        {
            RebuildScene();
        }

        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetUpCamera();
            SetUpLight();

            var root = new GameObject(RootName);
            root.AddComponent<SimRoot>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// カメラは <see cref="CivilizationToSpace.View.SimCameraControl"/> が毎フレーム置き直す。
        /// ここでは背景と画角だけを決める。位置はどこでもよい。
        /// </summary>
        private static void SetUpCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(0f, 12f, -42f);
            camera.transform.rotation = Quaternion.Euler(16f, 0f, 0f);
            camera.fieldOfView = 55f;
            camera.clearFlags = CameraClearFlags.SolidColor;

            // 宇宙なので、ほとんど黒にする。真っ黒にしないのは、
            // 画面が消えているのか動いているのか分からなくなるためである。
            camera.backgroundColor = new Color(0.016f, 0.020f, 0.035f, 1f);
        }

        private static void SetUpLight()
        {
            var light = Object.FindObjectOfType<Light>();
            if (light == null || light.type != LightType.Directional)
            {
                return;
            }

            light.transform.rotation = Quaternion.Euler(24f, -40f, 0f);
            light.intensity = 1.5f;
            light.color = new Color(1f, 0.97f, 0.92f, 1f);

            // 環境光を落として、球の陰影を出す。宇宙に一様な明かりは無い。
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.09f, 0.10f, 0.14f, 1f);
        }

        /// <summary>
        /// ビルド設定の**末尾**に足す。先頭へ入れない。
        ///
        /// 先頭のシーンが起動時に開く。WebGLの公開ビルドは EarthTimelineDemo から
        /// 始まっており、ここを先頭にすると公開中の画面が入れ替わってしまう。
        /// </summary>
        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(entry => entry.path == ScenePath);
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
