using System;
using System.Collections.Generic;
using System.IO;
using CivilizationToSpace.Core;
using UnityEditor;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 正本 site/data/earth-eras.json から StreamingAssets へのコピーと、その照合。
    ///
    /// 更新の向きは site/data/ から StreamingAssets/ への一方向のみである。
    /// このクラスは site/ へ一切書き込まない。
    ///
    /// 改行コードについて。core.autocrlf が有効な環境では、正本は作業ツリー上でCRLFになる一方、
    /// コピー側は unity/.gitattributes の eol=lf によりLFで取り出される。
    /// したがって生バイトの比較は環境によって食い違う。
    /// データ交換形式が「照合はディスク上のファイルハッシュではなくblobハッシュを正とする」と
    /// 定めているのに合わせ、本ツールはLFへ正規化した内容どうしを比べる。
    /// 書き込みもLFで行い、作業ツリーとblobを一致させる。
    /// </summary>
    public static class EraCatalogSyncTool
    {
        private const string MenuRoot = "Tools/Civilization to Space/";
        private const string SourceRelativePath = "site/data/" + EraCatalogLoader.EraCatalogFileName;

        [MenuItem(MenuRoot + "正本から交換データを同期", false, 100)]
        public static void SyncFromSite()
        {
            string message;
            if (Sync(out message))
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        [MenuItem(MenuRoot + "交換データのコピーを照合", false, 101)]
        public static void VerifyCopy()
        {
            string message;
            if (Verify(out message))
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        /// <summary>batchmode 用。失敗したらプロセスを 1 で終える。</summary>
        public static void SyncFromCommandLine()
        {
            RunForCommandLine(Sync);
        }

        /// <summary>batchmode 用。失敗したらプロセスを 1 で終える。</summary>
        public static void VerifyFromCommandLine()
        {
            RunForCommandLine(Verify);
        }

        private delegate bool Operation(out string message);

        private static void RunForCommandLine(Operation operation)
        {
            string message;
            var ok = operation(out message);
            if (ok)
            {
                Debug.Log(message);
                return;
            }

            Debug.LogError(message);
            EditorApplication.Exit(1);
        }

        private static bool Sync(out string message)
        {
            string source;
            string destination;
            if (!ResolvePaths(out source, out destination, out message))
            {
                return false;
            }

            try
            {
                var sourceBytes = NormalizeToLf(File.ReadAllBytes(source));
                var sourceHash = EraCatalogLoader.ComputeSha256(sourceBytes);

                if (File.Exists(destination) && ByteEquals(sourceBytes, File.ReadAllBytes(destination)))
                {
                    message = "[EraCatalog] コピーは正本と同一です。書き込みは不要でした。SHA-256 " + sourceHash;
                    return true;
                }

                var directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(destination, sourceBytes);
                AssetDatabase.Refresh();
                message = "[EraCatalog] 正本を StreamingAssets へコピーしました。SHA-256 " + sourceHash;
                return true;
            }
            catch (Exception error)
            {
                message = "[EraCatalog] コピーに失敗しました: " + error.Message;
                return false;
            }
        }

        private static bool Verify(out string message)
        {
            string source;
            string destination;
            if (!ResolvePaths(out source, out destination, out message))
            {
                return false;
            }

            if (!File.Exists(destination))
            {
                message = "[EraCatalog] StreamingAssets にコピーがありません。同期を先に実行してください。";
                return false;
            }

            try
            {
                var sourceBytes = NormalizeToLf(File.ReadAllBytes(source));
                var destinationBytes = NormalizeToLf(File.ReadAllBytes(destination));
                var sourceHash = EraCatalogLoader.ComputeSha256(sourceBytes);
                var destinationHash = EraCatalogLoader.ComputeSha256(destinationBytes);

                if (!ByteEquals(sourceBytes, destinationBytes))
                {
                    message = "[EraCatalog] 正本とコピーが一致しません。正本 " + sourceHash +
                              " / コピー " + destinationHash;
                    return false;
                }

                var loaded = EraCatalogLoader.LoadFromFile(destination);
                if (!loaded.Ok)
                {
                    message = "[EraCatalog] コピーは正本と同一ですが、検証に失敗しました: " + loaded.UserMessage;
                    return false;
                }

                message = "[EraCatalog] 正本とコピーはLF正規化後の内容が同一で、検証も通りました。SHA-256 " +
                          sourceHash + " / 時代 " + loaded.Catalog.Eras.Count + "件";
                return true;
            }
            catch (Exception error)
            {
                message = "[EraCatalog] 照合に失敗しました: " + error.Message;
                return false;
            }
        }

        private static bool ResolvePaths(out string source, out string destination, out string message)
        {
            source = null;
            destination = null;

            // Application.dataPath は <リポジトリ>/unity/CivilizationToSpace/Assets を指す。
            var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
            source = Path.GetFullPath(Path.Combine(repositoryRoot, SourceRelativePath));
            destination = Path.GetFullPath(Path.Combine(
                Application.dataPath, "StreamingAssets", EraCatalogLoader.EraCatalogFileName));

            if (!File.Exists(source))
            {
                // 相対位置だけを示す。絶対パスはユーザー名を含むため出さない。
                message = "[EraCatalog] 正本 " + SourceRelativePath + " が見つかりません。";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// CRLF と単独のCR をLFへ寄せる。gitがblobへ格納する内容と同じ形にするためである。
        /// JSONの文字列内部に生の改行は現れない（JsonParser が制御文字を弾く）ので、値を壊さない。
        /// </summary>
        private static byte[] NormalizeToLf(byte[] bytes)
        {
            var output = new List<byte>(bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0x0D)
                {
                    if (i + 1 < bytes.Length && bytes[i + 1] == 0x0A)
                    {
                        i++;
                    }

                    output.Add(0x0A);
                    continue;
                }

                output.Add(bytes[i]);
            }

            return output.ToArray();
        }

        private static bool ByteEquals(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
