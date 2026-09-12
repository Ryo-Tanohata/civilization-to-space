using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CivilizationToSpace.Core.Json;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// StreamingAssets のコピーだけを読む。正本は site/data/earth-eras.json であり、
    /// Unity側から書き戻さない。外部通信を行わない。
    /// </summary>
    public static class EraCatalogLoader
    {
        /// <summary>StreamingAssets 内のファイル名。転送契約はファイル名だけを変えて再利用できる。</summary>
        public const string EraCatalogFileName = "earth-eras.json";

        /// <summary>
        /// ファイルを読み、SHA-256を測り、検証済みカタログを返す。
        /// 例外を外へ漏らさない。呼び出し側は UserMessage だけを画面へ出す。
        /// </summary>
        public static CatalogLoadResult LoadFromFile(string path)
        {
            byte[] bytes;
            try
            {
                // 交換契約はUTF-8テキストとして読むことを定めている。
                // SHA-256は読んだバイト列そのものに対して測るため、いったんバイトで受ける。
                bytes = File.ReadAllBytes(path);
            }
            catch (Exception)
            {
                return CatalogLoadResult.Failure(
                    "時代データのファイルを取得できませんでした。StreamingAssets に " +
                    EraCatalogFileName + " があるか確認してください。",
                    string.Empty);
            }

            var sha256 = ComputeSha256(bytes);

            string text;
            try
            {
                text = new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (Exception)
            {
                return CatalogLoadResult.Failure("時代データのファイル形式が正しくありません。", sha256);
            }

            JsonValue parsed;
            try
            {
                parsed = JsonParser.Parse(text);
            }
            catch (JsonParseException)
            {
                return CatalogLoadResult.Failure("時代データのファイル形式が正しくありません。", sha256);
            }

            try
            {
                return CatalogLoadResult.Success(CatalogNormalizer.Normalize(parsed), sha256);
            }
            catch (CatalogDataException error)
            {
                return CatalogLoadResult.Failure(error.UserMessage, sha256);
            }
            catch (Exception)
            {
                return CatalogLoadResult.Failure("時代データの内容を確認できませんでした。", sha256);
            }
        }

        public static string ComputeSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
