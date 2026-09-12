using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 時代ごとの地表を手続き的に描き出す。海と陸、起伏、植生、氷、火山、都市光、雲を作る。
    ///
    /// 画像素材を持ち込まない。外部の地図データも使わない。すべて visual の値と
    /// 大陸の配置（<see cref="PlanetGeography"/>）と3次元ノイズから作る。
    /// したがってライセンスの問題も、容量の増加も生じない。
    ///
    /// 大陸の配置は古地理の再現ではなく、デフォルメした象徴表現である。
    /// 色の対応づけもすべて描画側の都合であり、共通データではない。
    /// </summary>
    public static class PlanetSurfaceBaker
    {
        /// <summary>横は経度360度、縦は緯度180度に対応する。</summary>
        public const int Width = 512;
        public const int Height = 256;

        /// <summary>海岸線を崩すノイズの細かさ。</summary>
        private const float CoastFrequency = 3.4f;

        /// <summary>山や谷を作る細かいノイズ。</summary>
        private const float ReliefFrequency = 9f;

        private static readonly Color32 OceanDeep = new Color32(0x0C, 0x2C, 0x52, 0xFF);
        private static readonly Color32 OceanShallow = new Color32(0x2E, 0x7A, 0xB4, 0xFF);
        private static readonly Color32 Vegetation = new Color32(0x3C, 0x78, 0x30, 0xFF);
        private static readonly Color32 DryLand = new Color32(0x96, 0x7E, 0x5C, 0xFF);
        private static readonly Color32 HighLand = new Color32(0xB4, 0xA8, 0x96, 0xFF);
        private static readonly Color32 Ice = new Color32(0xE8, 0xF2, 0xF8, 0xFF);
        private static readonly Color32 Magma = new Color32(0xFF, 0x6A, 0x22, 0xFF);
        private static readonly Color32 CityGlow = new Color32(0xFF, 0xD8, 0x9E, 0xFF);

        public struct Surface
        {
            /// <summary>地表の色。</summary>
            public Texture2D Albedo;

            /// <summary>自ら光る量。火山と都市光をここへ入れる。</summary>
            public Texture2D Emission;

            /// <summary>凹凸の向き。陰影に起伏を出す。</summary>
            public Texture2D Normal;

            /// <summary>雲。透明度で濃さを表す。</summary>
            public Texture2D Clouds;

            /// <summary>球を膨らませる量。0が海面、大きいほど高い陸。</summary>
            public float[] Elevation;
        }

        public static Surface Bake(EraVisual visual, int eraIndex)
        {
            var ocean = Mathf.Clamp01((float)visual.OceanLevel);
            var vegetationAmount = Mathf.Clamp01((float)visual.Vegetation);
            var iceAmount = Mathf.Clamp01((float)visual.IceCoverage);
            var volcanic = Mathf.Clamp01((float)visual.VolcanicActivity);
            var city = Mathf.Clamp01((float)visual.CityLights);
            var cloud = Mathf.Clamp01((float)visual.CloudDensity);

            Color earthColor;
            if (!ColorUtility.TryParseHtmlString(visual.EarthColor, out earthColor))
            {
                earthColor = new Color(0.41f, 0.47f, 0.53f);
            }

            var count = Width * Height;
            var field = new float[count];
            var detail = new float[count];
            var directions = new Vector3[count];

            // 第1段：陸の強さを求める。
            for (var y = 0; y < Height; y++)
            {
                var latitude = ((y + 0.5f) / Height - 0.5f) * Mathf.PI;
                var cosLatitude = Mathf.Cos(latitude);
                var sinLatitude = Mathf.Sin(latitude);

                for (var x = 0; x < Width; x++)
                {
                    var longitude = (x + 0.5f) / Width * Mathf.PI * 2f;
                    var direction = new Vector3(
                        cosLatitude * Mathf.Cos(longitude), sinLatitude, cosLatitude * Mathf.Sin(longitude));

                    var index = y * Width + x;
                    directions[index] = direction;

                    var coast = ValueNoise3D.Fractal(direction * CoastFrequency, 17, 4, 0.5f);
                    detail[index] = ValueNoise3D.Fractal(direction * ReliefFrequency, 91, 4, 0.5f);

                    // 大陸の輪郭をノイズで崩す。円のまま残すと人工物に見える。
                    field[index] = PlanetGeography.Sample(direction, eraIndex) + (coast - 0.5f) * 0.70f;
                }
            }

            // 海の広さは、しきい値ではなく面積の割合で決める。
            // ノイズの値の分布は一様ではないため、しきい値を直に決めると海の広さが大きくずれる。
            var oceanFraction = Mathf.Lerp(0f, 0.78f, ocean);
            var seaLevel = FindThreshold(field, oceanFraction);

            var albedo = new Color32[count];
            var emission = new Color32[count];
            var clouds = new Color32[count];

            // 球を膨らませる量と法線はこれを見る。海は0、つまり平らにする。
            // 見えているのは水面であって海底ではない。海底を凹凸にすると水面が波打ち、
            // 輪郭も陰影も硬くなる。
            var elevation = new float[count];

            // 色を塗るための深さ。海の色だけに使う。
            var depth = new float[count];

            // 第2段：高さを決める。
            var span = 0.0001f;
            for (var i = 0; i < count; i++)
            {
                var above = field[i] - seaLevel;
                if (above > span)
                {
                    span = above;
                }
            }

            for (var i = 0; i < count; i++)
            {
                var above = (field[i] - seaLevel) / span;
                if (above <= 0f)
                {
                    // 水面は平ら。深さは色にだけ使う。
                    elevation[i] = 0f;
                    depth[i] = Mathf.Clamp01(-above * 2.2f);
                }
                else
                {
                    // 海岸はなだらかに立ち上げる。急に持ち上げると、
                    // 切り立った台地のようになって地形に見えない。
                    var ramp = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(above / 0.18f));
                    var shape = Mathf.Pow(above, 0.8f) * (0.55f + detail[i] * 0.62f);
                    elevation[i] = Mathf.Clamp01(shape) * ramp;
                }
            }

            // 第3段：色を塗る。
            for (var y = 0; y < Height; y++)
            {
                var latitude = ((y + 0.5f) / Height - 0.5f) * Mathf.PI;
                var sinLatitude = Mathf.Sin(latitude);

                for (var x = 0; x < Width; x++)
                {
                    var index = y * Width + x;
                    var height01 = Mathf.Clamp01(elevation[index]);
                    var isLand = elevation[index] > 0f;
                    var relief = detail[index];

                    Color surface;
                    if (isLand)
                    {
                        var ground = Color.Lerp((Color)DryLand, earthColor, 0.45f);

                        // 高い山だけ岩肌の色にする。低地まで灰色にすると緑が消える。
                        surface = Color.Lerp(ground, (Color)HighLand, Mathf.InverseLerp(0.58f, 1f, height01));

                        var greenness = vegetationAmount
                                        * Mathf.Clamp01(1.45f - Mathf.Abs(sinLatitude) * 1.5f)
                                        * Mathf.Clamp01(1.35f - height01 * 0.9f)
                                        * Mathf.Clamp01(0.55f + relief * 0.8f);
                        surface = Color.Lerp(surface, (Color)Vegetation, Mathf.Clamp01(greenness));
                    }
                    else
                    {
                        surface = Color.Lerp((Color)OceanShallow, (Color)OceanDeep, depth[index]);
                        surface = Color.Lerp(surface, earthColor, 0.18f);
                    }

                    // 極から氷で覆う。境目はノイズでぼかす。
                    var iceLatitude = Mathf.Lerp(1.2f, 0.02f, iceAmount);
                    var iceEdge = Mathf.Abs(sinLatitude) - iceLatitude + (relief - 0.5f) * 0.18f;
                    if (iceEdge > 0f)
                    {
                        surface = Color.Lerp(surface, (Color)Ice, Mathf.Clamp01(iceEdge * 6f));
                    }

                    albedo[index] = surface;

                    // 火山は割れ目として光らせる。面全体を一様に光らせると発光が飽和して地形が消える。
                    var glow = Color.black;
                    if (volcanic > 0f)
                    {
                        var crack = Mathf.InverseLerp(0.54f, 0.80f, relief);
                        glow += (Color)Magma * Mathf.Clamp01(volcanic * (0.08f + crack * crack * 1.05f));
                    }

                    // 都市光は陸の低地に点として散らす。
                    if (city > 0f && isLand)
                    {
                        var speck = ValueNoise3D.Sample(directions[index] * 60f, 233);
                        var density = Mathf.InverseLerp(0.93f - city * 0.16f, 1f, speck);
                        glow += (Color)CityGlow * (density * city * 1.2f);
                    }

                    emission[index] = new Color(
                        Mathf.Clamp01(glow.r), Mathf.Clamp01(glow.g), Mathf.Clamp01(glow.b), 1f);

                    var cloudNoise = ValueNoise3D.Fractal(directions[index] * 2.3f + Vector3.one * 11.3f, 57, 4, 0.55f);
                    var band = 0.55f + 0.45f * Mathf.Cos(latitude * 5.5f);
                    var cloudAlpha = Mathf.Clamp01((cloudNoise - 0.54f) * 3.2f) * cloud * band * 0.75f;
                    clouds[index] = new Color(1f, 1f, 1f, Mathf.Clamp01(cloudAlpha));
                }
            }

            return new Surface
            {
                Albedo = CreateTexture(albedo, "EraAlbedo" + eraIndex, false),
                Emission = CreateTexture(emission, "EraEmission" + eraIndex, false),
                Normal = CreateNormal(elevation, "EraNormal" + eraIndex),
                Clouds = CreateTexture(clouds, "EraClouds" + eraIndex, false),
                Elevation = elevation
            };
        }

        /// <summary>
        /// 高さの傾きから法線を作る。これが無いと、色は変わっても面が平らに見え、
        /// 山も谷も海岸も読み取れない。
        /// </summary>
        private static Texture2D CreateNormal(float[] elevation, string name)
        {
            // 起伏の強さ。大きいほど陰影がはっきりする。デフォルメとして強めに取る。
            const float Strength = 14f;

            var pixels = new Color32[Width * Height];
            for (var y = 0; y < Height; y++)
            {
                var up = Mathf.Min(Height - 1, y + 1);
                var down = Mathf.Max(0, y - 1);

                for (var x = 0; x < Width; x++)
                {
                    // 経度方向は一周して繋がる。
                    var right = (x + 1) % Width;
                    var left = (x + Width - 1) % Width;

                    var dx = (elevation[y * Width + right] - elevation[y * Width + left]) * Strength;
                    var dy = (elevation[up * Width + x] - elevation[down * Width + x]) * Strength;

                    var normal = new Vector3(-dx, -dy, 1f).normalized;

                    // 法線は -1〜1 を 0〜1 へ写して格納する。
                    pixels[y * Width + x] = new Color(
                        normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1f);
                }
            }

            return CreateTexture(pixels, name, true);
        }

        /// <summary>
        /// 望む割合ぶんが下に来る位置を返す。全画素を並べ替えると重いため、
        /// 間引いた標本から求める。海陸の境を1画素単位で厳密に合わせる必要はない。
        /// </summary>
        private static float FindThreshold(float[] values, float fraction)
        {
            if (fraction <= 0f)
            {
                return float.MinValue;
            }

            if (fraction >= 1f)
            {
                return float.MaxValue;
            }

            const int Stride = 7;
            var sample = new float[values.Length / Stride];
            for (var i = 0; i < sample.Length; i++)
            {
                sample[i] = values[i * Stride];
            }

            System.Array.Sort(sample);
            var position = Mathf.Clamp(Mathf.RoundToInt(sample.Length * fraction), 0, sample.Length - 1);
            return sample[position];
        }

        private static Texture2D CreateTexture(Color32[] pixels, string name, bool linear)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true, linear)
            {
                name = name,

                // 経度方向は繋がっているので繰り返す。緯度方向は端で止める。
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }
    }
}
