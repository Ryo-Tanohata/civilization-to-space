using UnityEditor;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 遠景の写真の取り込み方をそろえる。
    ///
    /// **縮小の絵（ミップマップ）を作らない。** この絵は空の板に
    /// ほぼ画素と画素が1対1で貼られる。縮小の絵を持たせると、
    /// 距離で切り替わったときに遠景だけがぼやける。
    ///
    /// **端は伸ばして止める。** 繰り返すと、板の端で地平線が
    /// 二重に出てしまう。
    /// </summary>
    public sealed class SkyImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/Sky/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;

            // 圧縮する。遠景は画面の広い面積を占めるので、
            // 非圧縮のままだと1枚で数MBになる。
            importer.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
