using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 実行時に作る材質の素になるもの。Standardの雛形を資産として置き、それを複製して返す。
    ///
    /// **なぜ雛形から複製するのか。**
    /// 以前は各所で <c>new Material(Shader.Find("Standard"))</c> として材質を組んでいた。
    /// Standardはキーワードの組み合わせごとに別々の実体へ分かれ、ビルドは
    /// プロジェクト内の材質に現れない組み合わせを削り落とす。
    /// 実行時にしか組まない材質はビルド時には存在しないため、
    /// 必要な組み合わせがひとつも残らなかった。
    ///
    /// 半透明の組み合わせが失われると、Standardは不透明の枝で描かれる。
    /// 不透明の枝は不透明度を1として出力するため、混ぜ方をSrcAlphaにしていても
    /// 結果は完全な不透明になる。そのためWebGLでは、地球を包む雲の殻と大気の殻が
    /// 不透明な球として地表を覆い隠し、地球が単色の球に見えていた。
    /// Editorでは全組み合わせが揃っているので再現しない。
    ///
    /// 雛形は、実行時に**行き着く**組み合わせと一致させる必要がある。
    /// 地表の材質は生成後に <c>AssignSurface</c> が <c>_NORMALMAP</c> を立てるので、
    /// 法線の有無まで含めて雛形を分けている。ここを取り違えると、
    /// 雛形を置いても対応する組み合わせが残らない。
    ///
    /// 雛形自体は書き換えない。複製してから色や滑らかさを指定すること。
    /// 雛形は Editor の MaterialTemplateTool が書き出す。
    /// </summary>
    public static class StandardMaterials
    {
        private const string Folder = "Materials/";

        /// <summary>不透明。月面・岩・人工衛星・拠点に使う。</summary>
        public static Material CreateOpaque(bool emissive)
        {
            return Copy(emissive ? "StandardOpaqueEmissive" : "StandardOpaque");
        }

        /// <summary>不透明で、発光と凹凸の絵を貼るもの。地球の地表に使う。</summary>
        public static Material CreateOpaqueSurface()
        {
            return Copy("StandardOpaqueSurface");
        }

        /// <summary>重ねて透かすもの。雲と大気に使う。</summary>
        public static Material CreateFade()
        {
            return Copy("StandardFade");
        }

        /// <summary>重ねて透かし、発光もするもの。ぶつかった跡の光に使う。</summary>
        public static Material CreateFadeEmissive()
        {
            return Copy("StandardFadeEmissive");
        }

        /// <summary>
        /// 重ねて透かし、発光と凹凸の絵も貼るもの。
        /// 地表が次の時代へ移り変わる途中の層に使う。
        /// </summary>
        public static Material CreateFadeSurface()
        {
            return Copy("StandardFadeSurface");
        }

        private static Material Copy(string name)
        {
            var template = Resources.Load<Material>(Folder + name);
            if (template != null)
            {
                return new Material(template);
            }

            // 雛形が見つからないのは、Resources から外れたときだけである。
            // Editorでは全組み合わせが揃っているので絵は出るが、
            // WebGLでは半透明が不透明に描かれる。気づけるように警告を出す。
            Debug.LogWarning("[Material] 雛形 \"" + name + "\" が見つかりません。"
                             + "実行時生成へ落ちるため、WebGLでは半透明が正しく描かれません。");

            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[Material] Standard シェーダーが見つかりません。");
                return null;
            }

            return new Material(shader);
        }
    }
}
