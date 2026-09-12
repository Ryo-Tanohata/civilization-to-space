using System.IO;
using UnityEngine;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// StreamingAssets に置いた交換データをバイト列で読む。
    ///
    /// WebGL では StreamingAssets がURLになり <see cref="File"/> で読めない。
    /// そのためWebGLだけは、ビルド時に Resources へ複製した同じバイト列を読む。
    /// 複製はビルドの前後にだけ存在し、リポジトリには残さない
    /// （<c>WebGlBuild</c> が作成と削除の両方を行う）。
    ///
    /// どの経路でも返すのは同じバイト列なので、呼び出し側が測るSHA-256は変わらない。
    /// 正本は site/data/*.json のままで、複製を増やしたことにはならない。
    /// </summary>
    public static class StreamingBytes
    {
        /// <summary>WebGL用の複製を置く Resources 配下のフォルダ名。</summary>
        public const string ResourceFolder = "StreamingCopy";

        /// <summary>
        /// 読めなかった場合は例外を投げる。呼び出し側は既存のとおり捕まえて
        /// 利用者向けの文言に変換する。
        /// </summary>
        public static byte[] Read(string path)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var name = Path.GetFileNameWithoutExtension(path);
            var asset = Resources.Load<TextAsset>(ResourceFolder + "/" + name);
            if (asset == null)
            {
                throw new FileNotFoundException("WebGL用の複製が見つかりません: " + name, path);
            }

            return asset.bytes;
#else
            return File.ReadAllBytes(path);
#endif
        }
    }
}
