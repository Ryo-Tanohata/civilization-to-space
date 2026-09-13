using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 実行時に複製するための材質の雛形を書き出す。
    ///
    /// **なぜ雛形が要るのか。**
    /// 実行時に <c>new Material(Shader.Find(...))</c> で組んだ材質はビルド時に存在せず、
    /// 必要なシェーダーが残らないことがある。雛形を資産として置けば確実に残る。
    ///
    /// **なぜ自作シェーダーなのか。**
    /// Standard はキーワードの有無で実体が分かれ、その分かれた先がWebGLビルドに残らない。
    /// 地色も発光も半透明も画面に出なかった。常時含めるシェーダーの登録・
    /// キーワードを揃えた雛形・バリアントコレクションの3つを試したがいずれも効かず、
    /// 一方で自作シェーダーは効いていた。そこで地表まわりを自作シェーダーへ移した。
    /// 自作側はキーワードで分岐しないので、分かれた先が消えるという問題が起きない。
    ///
    /// 雛形はシェーダーごとに用途で分けてある。名前は呼び出し側との対応を保つため、
    /// 以前のものをそのまま使っている。
    /// </summary>
    public static class MaterialTemplateTool
    {
        private const string Folder = "Assets/Resources/Materials";

        [MenuItem("Civilization to Space/材質の雛形を作り直す")]
        public static void CreateOrUpdate()
        {
            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            var opaque = Shader.Find("CivilizationToSpace/PlanetOpaque");
            var fade = Shader.Find("CivilizationToSpace/PlanetFade");
            var glow = Shader.Find("CivilizationToSpace/AdditiveGlow");

            if (opaque == null || fade == null || glow == null)
            {
                Debug.LogError("[Template] 自作シェーダーが見つかりません。"
                               + " PlanetOpaque=" + (opaque != null)
                               + " PlanetFade=" + (fade != null)
                               + " AdditiveGlow=" + (glow != null));
                return;
            }

            // 凹凸の絵を貼るのは地表だけ。ほかは平らに扱う。
            Write(opaque, "StandardOpaque", false);
            Write(opaque, "StandardOpaqueEmissive", false);
            Write(opaque, "StandardOpaqueSurface", true);
            Write(fade, "StandardFade", false);
            Write(fade, "StandardFadeEmissive", false);
            Write(fade, "StandardFadeSurface", true);
            Write(glow, "AdditiveGlow", false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Template] 材質の雛形を7件書き出しました。 " + Folder);
        }

        /// <summary>batchmode 用。</summary>
        public static void RunFromCommandLine()
        {
            CreateOrUpdate();
        }

        private static void Write(Shader shader, string name, bool normalMap)
        {
            var path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var isNew = material == null;
            material = isNew ? new Material(shader) : material;
            material.shader = shader;

            material.color = Color.white;
            material.SetColor("_EmissionColor", Color.black);

            // 加算の材質には滑らかさも金属らしさも無い。指定しても無視される。
            if (shader.name != "CivilizationToSpace/AdditiveGlow")
            {
                material.SetFloat("_Glossiness", 0f);
                material.SetFloat("_Metallic", 0f);
                material.SetFloat("_BumpScale", normalMap ? 1f : 0f);
            }

            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;

            if (isNew)
            {
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            Debug.Log("[Template] " + path + " : " + shader.name);
        }
    }
}
