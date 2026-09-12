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

        private static void Build()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var appRoot = new GameObject(AppRootName);
            appRoot.AddComponent<AppRoot>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();
            AssetDatabase.SaveAssets();
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
