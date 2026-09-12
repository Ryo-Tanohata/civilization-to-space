using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 象徴的な地球。visual の 0〜1 をUnityの見た目へ変換する。
    ///
    /// 海・植生・氷は本体の色へ混ぜ込む。均一な半透明の殻として重ねると、
    /// 外側の層が内側を覆い隠し、値の違いが読めなくなるためである。
    /// 火山と都市光は加算合成の殻にする。隠すのではなく光を足す量として扱う。
    /// 大気だけは通常の半透明にする。球の外側にはみ出した部分が輪郭の光として見えるためである。
    ///
    /// ブラウザとの見た目の一致は求めない。層の色・半径・合成はすべて描画側の都合であり、
    /// 共通データではない。いずれの色も特定の時代を表さない。
    /// </summary>
    public sealed class EarthView : MonoBehaviour
    {
        private const float BaseRadius = 2.2f;

        /// <summary>地球の半径。カメラのフレーミングが同じ値を見る。</summary>
        public static float Radius
        {
            get { return BaseRadius; }
        }

        // 混色と発光に使う色。描画側の定数であり、時代データではない。
        private static readonly Color OceanColor = new Color32(0x23, 0x6C, 0xA8, 0xFF);
        private static readonly Color VegetationColor = new Color32(0x4F, 0x93, 0x3C, 0xFF);
        private static readonly Color IceColor = new Color32(0xE6, 0xF1, 0xF8, 0xFF);
        private static readonly Color VolcanoColor = new Color32(0xFF, 0x6A, 0x2A, 0xFF);
        private static readonly Color CityColor = new Color32(0xFF, 0xD2, 0x93, 0xFF);
        private static readonly Color CloudColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        private static readonly Color SatelliteColor = new Color32(0xC8, 0xD4, 0xE0, 0xFF);
        private static readonly Color FallbackColor = new Color32(0x69, 0x78, 0x87, 0xFF);

        // 混色の効き。1.0にすると本体色が完全に置き換わるため、手前で止める。
        private const float OceanMix = 0.60f;
        private const float VegetationMix = 0.62f;
        private const float IceMix = 0.88f;

        /// <summary>衛星の表示上限。データ側の値に上限は無いため、描画側で持ち数を決める。</summary>
        private const int SatellitePoolSize = 8;

        private Material baseMaterial;
        private Material cloudMaterial;
        private Material volcanoMaterial;
        private Material cityMaterial;
        private Material atmosphereMaterial;

        private GameObject[] satellites;

        /// <summary>
        /// 生成物に付ける印。編集中のプレビューでは DontSave を渡し、
        /// シーンへ保存されないようにする。
        /// </summary>
        private HideFlags createdFlags = HideFlags.None;

        public void Build()
        {
            Build(HideFlags.None);
        }

        public void Build(HideFlags flags)
        {
            createdFlags = flags;
            baseMaterial = CreateOpaque(FallbackColor);
            CreateSphere("Base", 1.000f, baseMaterial);

            volcanoMaterial = CreateAdditive(VolcanoColor, 0);
            CreateSphere("Volcano", 1.014f, volcanoMaterial);

            cityMaterial = CreateAdditive(CityColor, 1);
            CreateSphere("CityLights", 1.020f, cityMaterial);

            cloudMaterial = CreateTransparent(CloudColor, 2);
            CreateSphere("Clouds", 1.032f, cloudMaterial);

            atmosphereMaterial = CreateTransparent(FallbackColor, 3);
            CreateSphere("Atmosphere", 1.085f, atmosphereMaterial);

            BuildSatellites();
        }

        /// <summary>時代の視覚値を写す。呼ばれるたびに全体を上書きする。</summary>
        public void Apply(EraVisual visual)
        {
            var earthColor = ParseColor(visual.EarthColor);
            var emissionColor = ParseColor(visual.EmissionColor);

            var ocean = (float)visual.OceanLevel;
            var vegetation = (float)visual.Vegetation;
            var ice = (float)visual.IceCoverage;
            var cloud = (float)visual.CloudDensity;
            var volcano = (float)visual.VolcanicActivity;
            var city = (float)visual.CityLights;

            // 海→植生→氷の順に混ぜる。氷を最後にすると、凍結の時代が白く読める。
            var surface = earthColor;
            surface = Color.Lerp(surface, OceanColor, ocean * OceanMix);
            surface = Color.Lerp(surface, VegetationColor, vegetation * VegetationMix);
            surface = Color.Lerp(surface, IceColor, ice * IceMix);
            surface.a = 1f;

            baseMaterial.color = surface;
            SetEmission(baseMaterial, earthColor * 0.10f + VolcanoColor * (volcano * 0.75f));

            SetAlpha(volcanoMaterial, VolcanoColor, volcano * 0.55f);
            SetAlpha(cityMaterial, CityColor, city * 0.40f);
            SetAlpha(cloudMaterial, CloudColor, cloud * 0.32f);

            SetAlpha(atmosphereMaterial, emissionColor, 0.32f);
            SetEmission(atmosphereMaterial, emissionColor * 0.75f);

            ApplySatellites(visual.SatelliteCount);
        }

        private void BuildSatellites()
        {
            satellites = new GameObject[SatellitePoolSize];
            var material = CreateOpaque(SatelliteColor);
            SetEmission(material, SatelliteColor * 0.45f);

            var ring = new GameObject("Satellites");
            ring.hideFlags = createdFlags;
            ring.transform.SetParent(transform, false);
            ring.transform.localRotation = Quaternion.Euler(24f, 0f, 12f);

            for (var i = 0; i < SatellitePoolSize; i++)
            {
                var angle = 360f / SatellitePoolSize * i;
                var satellite = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                satellite.name = "Satellite" + (i + 1);
                satellite.hideFlags = createdFlags;
                SafeDestroy(satellite.GetComponent<Collider>());
                satellite.transform.SetParent(ring.transform, false);
                satellite.transform.localPosition =
                    Quaternion.Euler(0f, angle, 0f) * new Vector3(BaseRadius * 1.45f, 0f, 0f);
                satellite.transform.localScale = Vector3.one * (BaseRadius * 0.055f);
                satellite.GetComponent<Renderer>().sharedMaterial = material;
                satellite.SetActive(false);
                satellites[i] = satellite;
            }
        }

        private void ApplySatellites(double count)
        {
            // データ側の個数に上限は無い。持ち数を超える分は描かない。
            var visible = count > SatellitePoolSize ? SatellitePoolSize : (int)count;
            for (var i = 0; i < satellites.Length; i++)
            {
                satellites[i].SetActive(i < visible);
            }
        }

        private void CreateSphere(string name, float radiusScale, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.hideFlags = createdFlags;
            SafeDestroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * (BaseRadius * 2f * radiusScale);
            sphere.GetComponent<Renderer>().sharedMaterial = material;
        }

        private Material CreateOpaque(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.hideFlags = createdFlags;
            material.color = color;
            material.SetFloat("_Glossiness", 0.12f);
            material.SetFloat("_Metallic", 0f);
            return material;
        }

        /// <summary>通常の半透明（Fade）。内側の層より後に描くようキューをずらす。</summary>
        private Material CreateTransparent(Color color, int queueOffset)
        {
            var material = CreateBlended(
                color,
                queueOffset,
                UnityEngine.Rendering.BlendMode.SrcAlpha,
                UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_Mode", 2f);
            return material;
        }

        /// <summary>加算合成。下の層を隠さず、光だけを足す。</summary>
        private Material CreateAdditive(Color color, int queueOffset)
        {
            var material = CreateBlended(
                color,
                queueOffset,
                UnityEngine.Rendering.BlendMode.SrcAlpha,
                UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_Mode", 3f);
            return material;
        }

        private Material CreateBlended(
            Color color,
            int queueOffset,
            UnityEngine.Rendering.BlendMode source,
            UnityEngine.Rendering.BlendMode destination)
        {
            var material = new Material(Shader.Find("Standard"));
            material.hideFlags = createdFlags;
            material.SetInt("_SrcBlend", (int)source);
            material.SetInt("_DstBlend", (int)destination);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + queueOffset;
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.color = color;
            return material;
        }

        /// <summary>
        /// 編集中は Destroy が次のフレームまで効かない。プレビューを組み直せるよう経路を分ける。
        /// </summary>
        private static void SafeDestroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        /// <summary>生成した子と材質を片付ける。プレビューを作り直すときに呼ぶ。</summary>
        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(transform.GetChild(i).gameObject);
            }

            SafeDestroy(baseMaterial);
            SafeDestroy(cloudMaterial);
            SafeDestroy(volcanoMaterial);
            SafeDestroy(cityMaterial);
            SafeDestroy(atmosphereMaterial);
            satellites = null;
        }

        private static void SetAlpha(Material material, Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            material.color = color;
        }

        private static void SetEmission(Material material, Color color)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color);
        }

        /// <summary>
        /// 16進色を解釈する。検証を通ったデータは必ず解釈できるが、
        /// 解釈できない場合も落とさず中立の色へ寄せる。
        /// </summary>
        private static Color ParseColor(string value)
        {
            Color parsed;
            return ColorUtility.TryParseHtmlString(value, out parsed) ? parsed : (Color)FallbackColor;
        }
    }
}
