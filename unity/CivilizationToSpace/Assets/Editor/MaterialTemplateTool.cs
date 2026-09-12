using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// 実行時に複製するためのStandard材質の雛形を書き出す。
    ///
    /// ビルドは、プロジェクト内の材質に現れないキーワードの組み合わせを削る。
    /// 実行時にしか組まない材質はビルド時に見えないため、必要な組み合わせが消え、
    /// WebGLで半透明が不透明に描かれていた。理由は View/StandardMaterials.cs にある。
    ///
    /// ここで書き出す6件は、実行時に材質が**行き着く**組み合わせと一対一で対応する。
    ///
    /// | 雛形 | 使う場所 | キーワード |
    /// | --- | --- | --- |
    /// | StandardOpaque | 月面 | なし |
    /// | StandardOpaqueEmissive | 岩・人工衛星・拠点・行き来 | _EMISSION |
    /// | StandardOpaqueSurface | 地球の地表 | _EMISSION _NORMALMAP |
    /// | StandardFade | 雲・大気 | _ALPHABLEND_ON |
    /// | StandardFadeEmissive | ぶつかった跡の光 | _ALPHABLEND_ON _EMISSION |
    /// | StandardFadeSurface | 地表の移り変わり | _ALPHABLEND_ON _EMISSION _NORMALMAP |
    ///
    /// 混ぜ方（加算・通常）は材質の値であってキーワードではないため、
    /// 加算で描く光も StandardFadeEmissive でまかなえる。
    /// </summary>
    public static class MaterialTemplateTool
    {
        private const string Folder = "Assets/Resources/Materials";

        [MenuItem("Civilization to Space/材質の雛形を作り直す")]
        public static void CreateOrUpdate()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[Template] Standard シェーダーが見つかりません。");
                return;
            }

            if (!Directory.Exists(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            Write(shader, "StandardOpaque", false, false, false);
            Write(shader, "StandardOpaqueEmissive", false, true, false);
            Write(shader, "StandardOpaqueSurface", false, true, true);
            Write(shader, "StandardFade", true, false, false);
            Write(shader, "StandardFadeEmissive", true, true, false);
            Write(shader, "StandardFadeSurface", true, true, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Template] 材質の雛形を6件書き出しました。 " + Folder);
        }

        /// <summary>batchmode 用。</summary>
        public static void RunFromCommandLine()
        {
            CreateOrUpdate();
        }

        private static void Write(Shader shader, string name, bool fade, bool emissive, bool normalMap)
        {
            var path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var isNew = material == null;
            if (isNew)
            {
                material = new Material(shader);
            }
            else
            {
                material.shader = shader;
            }

            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.color = Color.white;
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            if (fade)
            {
                // Standard の Fade と同じ指定。
                material.SetFloat("_Mode", 2f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_Mode", 0f);
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHABLEND_ON");
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = -1;
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.white);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            if (normalMap)
            {
                // 凹凸の絵は実行時に貼る。ここでは組み合わせを残すためにキーワードだけ立てる。
                material.EnableKeyword("_NORMALMAP");
                material.SetFloat("_BumpScale", 1f);
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            if (isNew)
            {
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            Debug.Log("[Template] " + path
                      + " fade=" + fade + " emissive=" + emissive + " normalMap=" + normalMap);
        }
    }
}
