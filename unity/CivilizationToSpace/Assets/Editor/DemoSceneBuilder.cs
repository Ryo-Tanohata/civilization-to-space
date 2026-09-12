using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// EarthTimelineDemo シーンを生成し直す。
    ///
    /// .unity をテキストとして直接編集しない。壊れたときはこのメニューから作り直す。
    /// S2の段階では、カメラと AppRoot だけを置く。地球と操作UIはS3以降で足す。
    /// </summary>
    public static class DemoSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/EarthTimelineDemo.unity";
        private const string SceneFolder = "Assets/Scenes";
        private const string AppRootName = "AppRoot";

        [MenuItem("Tools/Civilization to Space/EarthTimelineDemo シーンを生成し直す", false, 1)]
        public static void RebuildScene()
        {
            Build();
            Debug.Log("[Scene] " + ScenePath + " を生成しました。");
        }

        /// <summary>batchmode 用。</summary>
        public static void BuildFromCommandLine()
        {
            Build();
            Debug.Log("[Scene] " + ScenePath + " を生成しました。");
        }

        /// <summary>
        /// Playしていない状態のシーンの中身を並べる。
        /// 編集中プレビューが出ているか、保存対象に混ざっていないかを確かめるために使う。
        /// </summary>
        [MenuItem("Tools/Civilization to Space/シーンの中身を点検", false, 2)]
        public static void ReportSceneContents()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var builder = new System.Text.StringBuilder();
            builder.Append("[Scene] ").Append(ScenePath).Append(" の中身\n");

            foreach (var root in scene.GetRootGameObjects())
            {
                Describe(builder, root.transform, 0);
            }

            builder.Append("保存対象（DontSave が付いていないもの）だけが .unity に残る。");
            Debug.Log(builder.ToString());
        }

        public static void ReportSceneContentsFromCommandLine()
        {
            ReportSceneContents();
        }

        private static void Describe(System.Text.StringBuilder builder, Transform target, int depth)
        {
            builder.Append(new string(' ', depth * 2)).Append("- ").Append(target.name);

            var flags = target.gameObject.hideFlags;
            builder.Append(flags == HideFlags.None ? "  [保存対象]" : "  [" + flags + "]");

            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                builder.Append("  色 ").Append(ColorUtility.ToHtmlStringRGBA(renderer.sharedMaterial.color));
                builder.Append(target.gameObject.activeSelf ? "  表示" : "  非表示");
            }

            builder.Append('\n');

            for (var i = 0; i < target.childCount; i++)
            {
                Describe(builder, target.GetChild(i), depth + 1);
            }
        }

        private static void Build()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            SetUpCamera();
            SetUpLight();

            var appRoot = new GameObject(AppRootName);
            appRoot.AddComponent<AppRoot>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 地球は原点に置き、カメラを右へずらす。こうすると地球が画面の左寄りに写り、
        /// 右側を説明の領域として空けられる。カメラは回さない。視点操作はS4で足す。
        /// </summary>
        private static void SetUpCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.transform.position = new Vector3(2.9f, 0f, -9f);
            camera.transform.rotation = Quaternion.identity;
            camera.fieldOfView = 60f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.043f, 0.063f, 0.106f, 1f);
        }

        private static void SetUpLight()
        {
            var light = Object.FindObjectOfType<Light>();
            if (light == null || light.type != LightType.Directional)
            {
                return;
            }

            light.transform.rotation = Quaternion.Euler(28f, -36f, 0f);
            light.intensity = 1.15f;
        }

        /// <summary>ビルド設定の先頭に置く。Play Mode の検証では使わないが、設定の欠落を残さない。</summary>
        private static void RegisterInBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(entry => entry.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
