using System;
using System.IO;
using CivilizationToSpace.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// WebGLビルドを実行する。
    ///
    /// WebGL では StreamingAssets をファイルとして読めないため、ビルドの直前に
    /// StreamingAssets の交換データを Resources へバイト単位で複製し、
    /// ビルドが終わったら必ず消す。複製はリポジトリに残らない。
    ///
    /// 圧縮は無効にする。GitHub Pages は Content-Encoding を指定できないため、
    /// Unityの .br / .gz をそのまま置くと読み込みに失敗する。
    /// </summary>
    public static class WebGlBuild
    {
        private const string OutputArgument = "-outputPath";
        private const string DefaultOutput = "Build/WebGL";
        private const string ResourcesRoot = "Assets/Resources";

        private static string CopyFolder => ResourcesRoot + "/" + StreamingBytes.ResourceFolder;

        [MenuItem("Tools/Civilization to Space/WebGLビルドを実行", false, 400)]
        public static void Run()
        {
            Build(DefaultOutput);
        }

        /// <summary>batchmode 用。失敗したらプロセスを 1 で終える。</summary>
        public static void BuildFromCommandLine()
        {
            var output = ReadArgument(OutputArgument);
            if (string.IsNullOrEmpty(output))
            {
                output = DefaultOutput;
            }

            var ok = Build(output);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        private static bool Build(string outputPath)
        {
            var copied = 0;
            try
            {
                copied = CopyStreamingAssetsToResources();
                Debug.Log("[WebGL] Resources へ複製した交換データ " + copied + "件");

                EnsureRuntimeShadersIncluded();

                // GitHub Pages は Content-Encoding を指定できないため、
                // 圧縮しつつ展開をJS側で行うフォールバックを使う。
                // これなら静的ホストでもそのまま動き、転送量は数分の一になる。
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.SplashScreen.show = false;

                var options = new BuildPlayerOptions
                {
                    scenes = EnabledScenes(),
                    locationPathName = outputPath,
                    target = BuildTarget.WebGL,
                    targetGroup = BuildTargetGroup.WebGL,
                    options = BuildOptions.None
                };

                if (options.scenes.Length == 0)
                {
                    Debug.LogError("[WebGL] ビルド対象のシーンが1つもありません。");
                    return false;
                }

                Debug.Log("[WebGL] ビルド開始 出力=" + outputPath + " シーン=" + string.Join(", ", options.scenes));
                var report = BuildPipeline.BuildPlayer(options);
                var summary = report.summary;

                Debug.Log("[WebGL] 結果 " + summary.result
                    + " 所要 " + summary.totalTime
                    + " 出力サイズ " + summary.totalSize + " バイト"
                    + " エラー " + summary.totalErrors
                    + " 警告 " + summary.totalWarnings);

                if (summary.result != BuildResult.Succeeded)
                {
                    foreach (var step in report.steps)
                    {
                        foreach (var message in step.messages)
                        {
                            if (message.type == LogType.Error || message.type == LogType.Exception)
                            {
                                Debug.LogError("[WebGL] " + step.name + " : " + message.content);
                            }
                        }
                    }

                    return false;
                }

                return true;
            }
            catch (Exception error)
            {
                Debug.LogError("[WebGL] ビルド中に例外が発生しました: " + error.Message);
                return false;
            }
            finally
            {
                RemoveCopies();
            }
        }

        /// <summary>
        /// 実行時に <c>Shader.Find</c> で探すシェーダーを Always Included Shaders へ登録する。
        ///
        /// エディタでは見つかるが、ビルドではどのマテリアルからも参照されないシェーダーは
        /// 除去され、<c>Shader.Find</c> が null を返す。その null で Material を作ると
        /// ArgumentNullException になり、何も描画されないまま起動が止まる。
        /// </summary>
        private static void EnsureRuntimeShadersIncluded()
        {
            // Assets/Scripts/View が Shader.Find で参照している名前
            var required = new[] { "Standard" };

            var graphics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphics == null || graphics.Length == 0)
            {
                Debug.LogWarning("[WebGL] GraphicsSettings.asset を読めませんでした。シェーダー登録を省きます。");
                return;
            }

            var settings = new SerializedObject(graphics[0]);
            var list = settings.FindProperty("m_AlwaysIncludedShaders");
            if (list == null)
            {
                Debug.LogWarning("[WebGL] m_AlwaysIncludedShaders が見つかりませんでした。");
                return;
            }

            var added = 0;
            foreach (var name in required)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogError("[WebGL] シェーダー \"" + name + "\" がエディタでも見つかりません。");
                    continue;
                }

                var already = false;
                for (var i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        already = true;
                        break;
                    }
                }

                if (already)
                {
                    continue;
                }

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                added++;
            }

            if (added > 0)
            {
                settings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[WebGL] 常時含めるシェーダー 追加" + added + "件 / 合計" + list.arraySize + "件");
        }

        /// <summary>StreamingAssets の *.json を Resources へ .txt として複製する。</summary>
        private static int CopyStreamingAssetsToResources()
        {
            var source = Application.streamingAssetsPath;
            if (!Directory.Exists(source))
            {
                return 0;
            }

            Directory.CreateDirectory(CopyFolder);

            var count = 0;
            foreach (var file in Directory.GetFiles(source, "*.json"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                // .txt にしないと TextAsset として取り込まれない
                File.WriteAllBytes(Path.Combine(CopyFolder, name + ".txt"), File.ReadAllBytes(file));
                count++;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return count;
        }

        /// <summary>複製をフォルダごと消す。Resources が空になるなら Resources も消す。</summary>
        private static void RemoveCopies()
        {
            if (AssetDatabase.DeleteAsset(CopyFolder))
            {
                Debug.Log("[WebGL] Resources の複製を削除しました。");
            }

            var root = ResourcesRoot;
            if (Directory.Exists(root)
                && Directory.GetFiles(root).Length == 0
                && Directory.GetDirectories(root).Length == 0)
            {
                AssetDatabase.DeleteAsset(root);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string[] EnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<string>();
            foreach (var scene in scenes)
            {
                if (scene.enabled)
                {
                    list.Add(scene.path);
                }
            }

            return list.ToArray();
        }

        private static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
