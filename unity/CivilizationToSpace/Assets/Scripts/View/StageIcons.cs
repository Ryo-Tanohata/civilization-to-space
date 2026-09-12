using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 段階ボタンの絵を、その段階のデータそのものから描く。
    ///
    /// 絵を別に用意しない。用意すると、データを変えたときに絵だけ古くなり、
    /// ボタンと3D表示が食い違う。氷が増えれば絵の氷冠も増える、という関係を保つ。
    /// したがってここに現れる数値は「見せ方」であって、新しい意味を足していない。
    ///
    /// 文字を出さないのは、段階が15個あり、狭い画面では1文字ずつ縦に折り返されて
    /// 読めなくなるためである。名前と説明は長押し（またはマウスを乗せる）で出す。
    /// </summary>
    public static class StageIcons
    {
        private const int Size = 64;
        private const float Middle = (Size - 1) * 0.5f;

        /// <summary>黄金角。点を偏りなく散らすために使う。</summary>
        private const float GoldenAngle = 2.399963f;

        private static readonly Color Rock = new Color(0.62f, 0.58f, 0.54f, 1f);
        private static readonly Color HotBody = new Color(0.86f, 0.42f, 0.20f, 1f);
        private static readonly Color Impactor = new Color(0.78f, 0.36f, 0.46f, 1f);
        private static readonly Color Debris = new Color(1f, 0.68f, 0.34f, 1f);
        private static readonly Color MoonGrey = new Color(0.76f, 0.77f, 0.80f, 1f);
        private static readonly Color Ocean = new Color(0.12f, 0.40f, 0.68f, 1f);
        private static readonly Color Ice = new Color(0.93f, 0.98f, 1f, 1f);
        private static readonly Color Leaf = new Color(0.33f, 0.68f, 0.31f, 1f);
        private static readonly Color Lava = new Color(1f, 0.58f, 0.16f, 1f);
        private static readonly Color Cloud = Color.white;
        private static readonly Color CityLight = new Color(1f, 0.93f, 0.60f, 1f);
        private static readonly Color Satellite = new Color(0.88f, 0.95f, 1f, 1f);
        private static readonly Color EarthBlue = new Color(0.13f, 0.34f, 0.55f, 1f);
        private static readonly Color Route = new Color(0.70f, 0.90f, 1f, 1f);
        private static readonly Color Facility = new Color(0.45f, 0.78f, 0.98f, 1f);
        private static readonly Color Colony = new Color(0.82f, 0.93f, 1f, 1f);

        // ------------------------------------------------------------------
        // 形成過程
        // ------------------------------------------------------------------

        public static Sprite Formation(FormationStage stage)
        {
            var canvas = new Canvas();
            if (stage == null)
            {
                return canvas.ToSprite();
            }

            var bodyScale = (float)stage.BodyScale;
            var swarm = (float)stage.Swarm;
            var impactor = (float)stage.Impactor;
            var debris = (float)stage.Debris;
            var moon = (float)stage.Moon;

            var body = 6f + 13f * bodyScale;

            // 破片の輪。円盤に見せるため縦をつぶす。
            canvas.RingDots(Mathf.RoundToInt(debris * 16f), 25f, 1.5f, Debris, 0.95f, 0.42f, 0.4f, 0.25f);

            // 集まってくる微惑星
            canvas.RingDots(Mathf.RoundToInt(swarm * 11f), 26f, 2f, Rock, 1f, 1f, 0f, 0.22f);

            canvas.Disc(Middle, Middle, body, HotBody, 1f, true);

            if (impactor > 0.05f)
            {
                canvas.Disc(Middle + 19f, Middle - 15f, 4f + 4f * impactor, Impactor, 1f, true);

                // 当たる向きの筋
                for (var k = 0; k < 5; k++)
                {
                    var t = k / 4f;
                    canvas.Disc(Middle + 19f - 10f * t, Middle - 15f + 8f * t, 1.1f,
                        new Color(1f, 0.85f, 0.6f, 1f), 0.7f - 0.5f * t, false);
                }
            }

            if (moon > 0.05f)
            {
                canvas.Disc(Middle + 22f, Middle + 14f, 3f + 3f * moon, MoonGrey, 1f, true);
            }

            return canvas.ToSprite();
        }

        // ------------------------------------------------------------------
        // 時代
        // ------------------------------------------------------------------

        public static Sprite Era(EraVisual visual)
        {
            var canvas = new Canvas();
            if (visual == null)
            {
                return canvas.ToSprite();
            }

            const float R = 21f;

            var baseColor = ParseColor(visual.EarthColor, new Color(0.41f, 0.47f, 0.53f, 1f));
            var emission = ParseColor(visual.EmissionColor, new Color(0.22f, 0.29f, 0.38f, 1f));

            var ocean = (float)visual.OceanLevel;
            var cloud = (float)visual.CloudDensity;
            var ice = (float)visual.IceCoverage;
            var vegetation = (float)visual.Vegetation;
            var volcano = (float)visual.VolcanicActivity;
            var lights = (float)visual.CityLights;

            // ふちの光。細くする。太いと球がぼやけて、どの時代か読めなくなる。
            canvas.Disc(Middle, Middle, R + 2.2f, emission, 0.30f, false);
            canvas.Disc(Middle, Middle, R, baseColor, 1f, true);

            if (ocean > 0.02f)
            {
                canvas.Disc(Middle, Middle, R, Ocean, ocean * 0.75f, true, R);
            }

            if (vegetation > 0.02f)
            {
                canvas.Disc(Middle - 8f, Middle - 5f, 7f, Leaf, vegetation, false, R);
                canvas.Disc(Middle + 6f, Middle + 2f, 6f, Leaf, vegetation, false, R);
                canvas.Disc(Middle - 3f, Middle + 10f, 5f, Leaf, vegetation, false, R);
                canvas.Disc(Middle + 9f, Middle + 11f, 4f, Leaf, vegetation, false, R);
            }

            if (ice > 0.02f)
            {
                // 極冠。球の上端・下端から「深さ」ぶんだけ覆う。
                // 大きな円をずらして重ねるので、深さを直に決めないと、
                // 氷が少なくても半球が埋まってしまう。
                var depth = R * (0.10f + 0.42f * ice);
                var cap = R * 0.9f;
                var offset = R + cap - depth;
                canvas.Disc(Middle, Middle - offset, cap, Ice, 0.97f, false, R);
                canvas.Disc(Middle, Middle + offset, cap, Ice, 0.97f, false, R);
            }

            if (volcano > 0.02f)
            {
                for (var k = 0; k < 6; k++)
                {
                    var a = 0.6f + k * GoldenAngle;
                    canvas.Disc(Middle + Mathf.Cos(a) * R * 0.55f, Middle + Mathf.Sin(a) * R * 0.55f,
                        2.6f, Lava, volcano, false, R);
                }
            }

            if (cloud > 0.02f)
            {
                // 2本の帯にする。全面に散らすとただの霞になり、地球の色が読めない。
                for (var k = 0; k < 2; k++)
                {
                    var y = Middle - 6f + k * 11f;
                    for (var x = -3; x <= 3; x++)
                    {
                        canvas.Disc(Middle + x * 5f + (k % 2) * 2.5f, y, 3f, Cloud, cloud * 0.42f, false, R);
                    }
                }
            }

            if (lights > 0.02f)
            {
                for (var k = 0; k < 8; k++)
                {
                    var a = k * GoldenAngle;
                    canvas.Disc(Middle + Mathf.Cos(a) * R * 0.62f, Middle + Mathf.Sin(a) * R * 0.62f,
                        1.5f, CityLight, lights, false, R);
                }
            }

            canvas.RingDots(Mathf.RoundToInt((float)visual.SatelliteCount), 27f, 2f, Satellite, 1f, 0.42f, 0.9f, 0f);
            return canvas.ToSprite();
        }

        // ------------------------------------------------------------------
        // 月への展開
        // ------------------------------------------------------------------

        public static Sprite Moon(MoonPhase phase)
        {
            var canvas = new Canvas();
            if (phase == null)
            {
                return canvas.ToSprite();
            }

            var transfer = (float)phase.Transfer;
            var facility = (float)phase.Facility;
            var surface = (float)phase.SurfaceLights;
            var station = (float)phase.OrbitStation;
            var colony = (float)phase.LagrangeColony;

            var ex = Middle - 13f;
            var ey = Middle + 5f;
            const float Er = 12f;
            var mx = Middle + 16f;
            var my = Middle - 9f;
            const float Mr = 8f;

            // 往復の経路
            var steps = 3 + Mathf.RoundToInt(transfer * 6f);
            for (var k = 0; k < steps; k++)
            {
                var t = (k + 0.5f) / steps;
                canvas.Disc(ex + (mx - ex) * t, ey + (my - ey) * t, 1.2f,
                    Route, 0.25f + 0.75f * transfer, false);
            }

            canvas.Disc(ex, ey, Er, EarthBlue, 1f, true);
            canvas.Disc(ex - 3f, ey - 2f, 4.5f, new Color(0.30f, 0.55f, 0.35f, 1f), 0.8f, false, Er, ex, ey);
            canvas.Disc(mx, my, Mr, MoonGrey, 1f, true);

            if (station > 0.02f)
            {
                var n = 1 + Mathf.RoundToInt(station * 2f);
                for (var k = 0; k < n; k++)
                {
                    var a = 0.8f + k * 2.2f;
                    canvas.Disc(ex + Mathf.Cos(a) * (Er + 5f), ey + Mathf.Sin(a) * (Er + 5f) * 0.5f,
                        1.4f + 1.2f * station, Satellite, 1f, false);
                }
            }

            if (facility > 0.02f)
            {
                var n = 1 + Mathf.RoundToInt(facility * 4f);
                for (var k = 0; k < n; k++)
                {
                    var a = 2.3f + k * 0.7f;
                    canvas.Disc(mx + Mathf.Cos(a) * Mr * 0.6f, my + Mathf.Sin(a) * Mr * 0.6f,
                        1.3f + 1.1f * facility, Facility, 1f, false, Mr, mx, my);
                }
            }

            if (surface > 0.02f)
            {
                var n = 2 + Mathf.RoundToInt(surface * 6f);
                for (var k = 0; k < n; k++)
                {
                    var a = k * GoldenAngle;
                    canvas.Disc(mx + Mathf.Cos(a) * Mr * 0.62f, my + Mathf.Sin(a) * Mr * 0.62f,
                        1.2f, CityLight, surface, false, Mr, mx, my);
                }
            }

            if (colony > 0.02f)
            {
                DrawColony(canvas, ex, ey, mx, my, colony);
            }

            return canvas.ToSprite();
        }

        /// <summary>
        /// L4（地球・月と正三角形をつくる位置）に、回転する円筒形の居住地を描く。
        /// 地球から月への向きを60度まわした先が、その位置になる。
        /// **大きさは実物の比ではない。** 見えるまで拡げている。
        /// </summary>
        private static void DrawColony(Canvas canvas, float ex, float ey, float mx, float my, float amount)
        {
            var dx = mx - ex;
            var dy = my - ey;

            // 画面のyは下向きなので、-60度のほうが空いている側へ来る。
            const float Cos = 0.5f;
            const float Sin = -0.8660254f;

            var lx = ex + dx * Cos - dy * Sin;
            var ly = ey + dx * Sin + dy * Cos;

            // 絵の端で切れないよう、少し内側へ寄せる。正三角形の関係は崩れるが、
            // この大きさでは位置関係が読めれば足りる。
            lx = Middle + (lx - Middle) * 0.82f;
            ly = Middle + (ly - Middle) * 0.82f;

            // 円筒の軸。月への向きに直交させる。
            var length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length < 1e-3f)
            {
                return;
            }

            var ax = -dy / length;
            var ay = dx / length;

            // 点を並べて短い棒にする。この大きさでは、これ以上の形は潰れて読めない。
            for (var k = -2; k <= 2; k++)
            {
                canvas.Disc(lx + ax * k * 1.6f, ly + ay * k * 1.6f, 2.0f, Colony, amount, false);
            }

            // 両端を明るくして、輪のある円筒に見せる。
            canvas.Disc(lx + ax * 3.2f, ly + ay * 3.2f, 1.5f, Color.white, amount, false);
            canvas.Disc(lx - ax * 3.2f, ly - ay * 3.2f, 1.5f, Color.white, amount, false);
        }

        private static Color ParseColor(string text, Color fallback)
        {
            Color parsed;
            return !string.IsNullOrEmpty(text) && ColorUtility.TryParseHtmlString(text, out parsed)
                ? parsed
                : fallback;
        }

        // ------------------------------------------------------------------
        // 画素を置く場所
        // ------------------------------------------------------------------

        /// <summary>
        /// 小さな絵を1枚ぶん置く入れ物。ふちは1画素ぶんでなめらかにする。
        /// 図形が単純なので、はみ出しを数えるだけで足りる（拡大して描いてから縮めない）。
        /// </summary>
        private sealed class Canvas
        {
            private readonly Color[] pixels = new Color[Size * Size];

            public Canvas()
            {
                var clear = new Color(0f, 0f, 0f, 0f);
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = clear;
                }
            }

            /// <summary>
            /// 円をひとつ置く。
            /// shade を立てると左上から当たった光をつけ、球らしく見せる。
            /// clipRadius を渡すと、その円の内側にだけ描く（氷や雲が地球からはみ出さないように）。
            /// </summary>
            public void Disc(
                float cx, float cy, float r, Color color, float alpha, bool shade,
                float clipRadius = -1f, float clipX = Middle, float clipY = Middle)
            {
                if (r <= 0f || alpha <= 0f)
                {
                    return;
                }

                var x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1f));
                var x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + r + 1f));
                var y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1f));
                var y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + r + 1f));

                for (var y = y0; y <= y1; y++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var dx = x - cx;
                        var dy = y - cy;
                        var cover = r + 0.5f - Mathf.Sqrt(dx * dx + dy * dy);
                        if (cover <= 0f)
                        {
                            continue;
                        }

                        if (cover > 1f)
                        {
                            cover = 1f;
                        }

                        if (clipRadius > 0f)
                        {
                            var cdx = x - clipX;
                            var cdy = y - clipY;
                            var inside = clipRadius + 0.5f - Mathf.Sqrt(cdx * cdx + cdy * cdy);
                            if (inside <= 0f)
                            {
                                continue;
                            }

                            if (inside < 1f)
                            {
                                cover *= inside;
                            }
                        }

                        var shown = color;
                        if (shade)
                        {
                            var lit = Mathf.Clamp01((-dx / r * 0.6f - dy / r * 0.6f) * 0.5f + 0.5f);
                            var k = 0.55f + 0.85f * lit;
                            shown = new Color(
                                Mathf.Min(1f, color.r * k),
                                Mathf.Min(1f, color.g * k),
                                Mathf.Min(1f, color.b * k),
                                1f);
                        }

                        Blend(x, y, shown, cover * alpha);
                    }
                }
            }

            /// <summary>輪の上に点を散らす。squash を1未満にすると、輪が円盤に見える。</summary>
            public void RingDots(
                int count, float radius, float dot, Color color, float alpha,
                float squash, float phase, float jitter)
            {
                for (var i = 0; i < count; i++)
                {
                    var a = phase + i * GoldenAngle;
                    var rr = radius * (1f + ((i * 7 % 5) / 5f - 0.4f) * jitter);
                    Disc(Middle + Mathf.Cos(a) * rr, Middle + Mathf.Sin(a) * rr * squash, dot, color, alpha, false);
                }
            }

            private void Blend(int x, int y, Color color, float a)
            {
                if (a <= 0f)
                {
                    return;
                }

                if (a > 1f)
                {
                    a = 1f;
                }

                var index = y * Size + x;
                var under = pixels[index];
                var outA = a + under.a * (1f - a);
                if (outA <= 0f)
                {
                    return;
                }

                pixels[index] = new Color(
                    (color.r * a + under.r * under.a * (1f - a)) / outA,
                    (color.g * a + under.g * under.a * (1f - a)) / outA,
                    (color.b * a + under.b * under.a * (1f - a)) / outA,
                    outA);
            }

            public Sprite ToSprite()
            {
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave
                };

                // ここでは上を0行目として描いてきた。テクスチャは下が0行目なので上下を入れ替える。
                var flipped = new Color[pixels.Length];
                for (var y = 0; y < Size; y++)
                {
                    var source = y * Size;
                    var destination = (Size - 1 - y) * Size;
                    for (var x = 0; x < Size; x++)
                    {
                        flipped[destination + x] = pixels[source + x];
                    }
                }

                texture.SetPixels(flipped);
                texture.Apply();

                var sprite = Sprite.Create(
                    texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }
        }
    }
}
