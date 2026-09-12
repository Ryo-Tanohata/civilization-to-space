using System;
using System.IO;
using System.Text;
using CivilizationToSpace.Core;
using UnityEditor;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 任意のパスにある時代データを読ませ、受理・全体エラー・縮退のどれになるかを報告する。
    ///
    /// 異常系の検証は、リポジトリ内のJSONを書き換えず、一時フォルダへ置いた複製を読ませる。
    /// そのための唯一の入口をここに置く。検証を使い捨てスクリプトに頼らず、再実行できる形で残す。
    /// </summary>
    public static class EraCatalogProbe
    {
        private const string PathArgument = "-catalogPath";
        private const string DirectoryArgument = "-catalogDir";

        [MenuItem("Tools/Civilization to Space/時代データを指定して読み込み検証", false, 200)]
        public static void ProbeWithDialog()
        {
            var path = EditorUtility.OpenFilePanel("検証する時代データを選ぶ", string.Empty, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Debug.Log(Describe(path));
        }

        /// <summary>
        /// batchmode 用。-catalogPath &lt;path&gt; で1件、-catalogDir &lt;folder&gt; でフォルダ内の *.json を名前順に検証する。
        /// 読めない・弾かれるのは検証の想定結果なので、プロセスは 0 で終える。
        /// </summary>
        public static void ProbeFromCommandLine()
        {
            var directory = ReadArgument(DirectoryArgument);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    Debug.LogError("[Probe] 指定されたフォルダがありません。");
                    EditorApplication.Exit(1);
                    return;
                }

                var files = Directory.GetFiles(directory, "*.json");
                Array.Sort(files, StringComparer.Ordinal);
                foreach (var file in files)
                {
                    Debug.Log("[Probe] 対象 " + Path.GetFileName(file) + "\n" + Describe(file));
                }

                Debug.Log("[Probe] 検証した件数 " + files.Length);
                return;
            }

            var path = ReadArgument(PathArgument);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[Probe] " + PathArgument + " または " + DirectoryArgument + " が指定されていません。");
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log("[Probe] 対象 " + Path.GetFileName(path) + "\n" + Describe(path));
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

        private static string Describe(string path)
        {
            var result = EraCatalogLoader.LoadFromFile(path);
            var builder = new StringBuilder();
            builder.Append("[Probe] 結果 ").Append(result.Ok ? "受理" : "全体エラー").Append('\n');
            builder.Append("SHA-256 ").Append(result.Sha256.Length > 0 ? result.Sha256 : "(読めなかった)").Append('\n');

            if (!result.Ok)
            {
                builder.Append("文言 ").Append(result.UserMessage);
                return builder.ToString();
            }

            var catalog = result.Catalog;
            builder.Append("件数 ").Append(catalog.Eras.Count).Append('\n');

            var degraded = 0;
            for (var i = 0; i < catalog.Eras.Count; i++)
            {
                var era = catalog.Eras[i];
                if (era.VisualDegraded)
                {
                    degraded++;
                }

                builder.Append(i + 1).Append(' ').Append(era.Id)
                    .Append(" | sortOrder ").Append(era.SortOrder)
                    .Append(" | 縮退 ").Append(era.VisualDegraded ? "あり" : "なし")
                    .Append(" | 海 ").Append(era.Visual.OceanLevel)
                    .Append(" | 地表色 ").Append(era.Visual.EarthColor)
                    .Append(" | 衛星 ").Append(era.Visual.SatelliteCount)
                    .Append('\n');
            }

            builder.Append("縮退した時代 ").Append(degraded).Append("件");
            return builder.ToString();
        }
    }
}
