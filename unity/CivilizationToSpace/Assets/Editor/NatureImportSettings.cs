using UnityEditor;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 外から持ってきた絵の取り込み方をそろえる。
    ///
    /// **抜き色を既定任せにしない。** 葉の板は絵のアルファで切り抜く。
    /// 取り込みの既定は圧縮を掛けるため、アルファの縁が崩れて
    /// 切り抜きの結果が変わる。実際、葉が1枚も出ない状態になった。
    /// 何が入っているかを確かめられるよう、圧縮せずそのまま持たせる。
    ///
    /// 形のほうは、法線を面ごとに持たせる。滑らかにすると、
    /// 葉の板が互いに影響し合って、平らな板が歪んで見える。
    /// </summary>
    public sealed class NatureImportSettings : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/Nature/";

        private bool InFolder
        {
            get { return assetPath.StartsWith(Folder); }
        }

        private void OnPreprocessTexture()
        {
            if (!InFolder)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
        }

        private void OnPreprocessModel()
        {
            if (!InFolder)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            importer.importNormals = ModelImporterNormals.Calculate;
            importer.normalCalculationMode = ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;

            // 当たり判定は要らない。誰も歩かない。
            importer.addCollider = false;

            // 材質は作らせる。名前から役を引くのに使う。
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }
    }
}
