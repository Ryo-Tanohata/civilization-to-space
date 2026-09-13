using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 操作ボタンの絵。再生・速度・動き・視点・説明の5つ。
    ///
    /// 狭い画面では文字のボタンが入りきらず、「動きを減らす：オフ」のような長い名前が
    /// 2行に折り返されて帯を高くしていた。帯が高いぶん地球の見える範囲が狭くなる。
    /// 絵にすると同じ幅へ5つ収まり、名前は長押し（またはマウスを乗せる）で出せる。
    ///
    /// 段階の絵（<see cref="StageIcons"/>）と違い、こちらはデータを持たない。
    /// 操作の意味は固定なので、形も固定でよい。
    /// </summary>
    public static class ControlIcons
    {
        private const int Size = 64;
        private const float Middle = (Size - 1) * 0.5f;

        private static readonly Color Line = new Color(0.90f, 0.94f, 0.98f, 1f);
        private static readonly Color Muted = new Color(0.58f, 0.65f, 0.74f, 1f);

        /// <summary>再生の三角。</summary>
        public static Sprite Play()
        {
            var c = new Raster();
            c.Triangle(24f, 16f, 24f, 48f, 48f, 32f, Line, 1f);
            return c.ToSprite();
        }

        /// <summary>停止の二本線。</summary>
        public static Sprite Pause()
        {
            var c = new Raster();
            c.Rect(22f, 16f, 29f, 48f, Line, 1f);
            c.Rect(35f, 16f, 42f, 48f, Line, 1f);
            return c.ToSprite();
        }

        /// <summary>最初から再生。三角の手前に、戻る先を示す線を置く。</summary>
        public static Sprite Replay()
        {
            var c = new Raster();
            c.Rect(17f, 16f, 22f, 48f, Line, 1f);
            c.Triangle(27f, 16f, 27f, 48f, 49f, 32f, Line, 1f);
            return c.ToSprite();
        }

        /// <summary>
        /// 速度。右向きの三角を2つ重ねた早送りの形にする。
        /// 何倍かは絵では表せないので、倍率の数字はボタンの文字で別に出す。
        /// </summary>
        public static Sprite Speed()
        {
            var c = new Raster();
            c.Triangle(16f, 18f, 16f, 46f, 33f, 32f, Line, 1f);
            c.Triangle(32f, 18f, 32f, 46f, 49f, 32f, Line, 1f);
            return c.ToSprite();
        }

        /// <summary>
        /// 動きの設定。流れを表す3本の線。
        /// 減らしているときは斜線を重ねて「切ってある」ことを示す。
        /// </summary>
        public static Sprite Motion(bool reduced)
        {
            var c = new Raster();
            var color = reduced ? Muted : Line;
            c.Rect(14f, 20f, 46f, 24f, color, 1f);
            c.Rect(14f, 30f, 40f, 34f, color, 1f);
            c.Rect(14f, 40f, 34f, 44f, color, 1f);

            if (reduced)
            {
                c.Line(14f, 46f, 48f, 16f, 3.2f, Line, 1f);
            }

            return c.ToSprite();
        }

        /// <summary>視点をもどす。輪の中心に点を置き、真ん中へ戻すことを示す。</summary>
        public static Sprite ResetView()
        {
            var c = new Raster();
            c.Ring(Middle, Middle, 19f, 15f, Line, 1f);
            c.Disc(Middle, Middle, 6f, Line, 1f);
            return c.ToSprite();
        }

        /// <summary>説明の出し入れ。よくある「i」の形。</summary>
        public static Sprite Description(bool visible)
        {
            var c = new Raster();
            var color = visible ? Line : Muted;
            c.Ring(Middle, Middle, 22f, 18f, color, 1f);
            c.Disc(Middle, 20f, 3.4f, color, 1f);
            c.Rect(Middle - 3f, 28f, Middle + 3f, 45f, color, 1f);
            return c.ToSprite();
        }

        /// <summary>
        /// コマ送り。三角の先に止め板を置いた、少しだけ進めることを表す形。
        /// 再生の三角と間違えないよう、板の有無で見分けられるようにしてある。
        /// </summary>
        public static Sprite StepFrame()
        {
            var c = new Raster();
            c.Triangle(18f, 17f, 18f, 47f, 39f, 32f, Line, 1f);
            c.Rect(43f, 17f, 48f, 47f, Line, 1f);
            return c.ToSprite();
        }

        /// <summary>前へ・次へ。向きだけを変えた三角。</summary>
        public static Sprite Step(bool forward)
        {
            var c = new Raster();
            if (forward)
            {
                c.Triangle(24f, 14f, 24f, 50f, 46f, 32f, Line, 1f);
            }
            else
            {
                c.Triangle(40f, 14f, 40f, 50f, 18f, 32f, Line, 1f);
            }

            return c.ToSprite();
        }

        /// <summary>
        /// 小さな絵を描くための入れ物。
        /// y は下向きに数える。上下は見た目のとおりに書けたほうが読みやすいので、
        /// 最後に <see cref="ToSprite"/> で上下を入れ替える。
        /// </summary>
        private sealed class Raster
        {
            private readonly Color[] pixels = new Color[Size * Size];

            public Raster()
            {
                var clear = new Color(0f, 0f, 0f, 0f);
                for (var i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = clear;
                }
            }

            public void Rect(float x0, float y0, float x1, float y1, Color color, float alpha)
            {
                var ix0 = Mathf.Max(0, Mathf.FloorToInt(x0));
                var ix1 = Mathf.Min(Size - 1, Mathf.CeilToInt(x1));
                var iy0 = Mathf.Max(0, Mathf.FloorToInt(y0));
                var iy1 = Mathf.Min(Size - 1, Mathf.CeilToInt(y1));

                for (var y = iy0; y <= iy1; y++)
                {
                    for (var x = ix0; x <= ix1; x++)
                    {
                        var cover = Mathf.Clamp01(Mathf.Min(x + 1f, x1) - Mathf.Max(x + 0f, x0))
                                    * Mathf.Clamp01(Mathf.Min(y + 1f, y1) - Mathf.Max(y + 0f, y0));
                        Blend(x, y, color, alpha * cover);
                    }
                }
            }

            public void Disc(float cx, float cy, float r, Color color, float alpha)
            {
                var x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1f));
                var x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + r + 1f));
                var y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1f));
                var y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + r + 1f));

                for (var y = y0; y <= y1; y++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        Blend(x, y, color, alpha * Mathf.Clamp01(r + 0.5f - d));
                    }
                }
            }

            /// <summary>輪。外側の半径と内側の半径で挟んだ帯を塗る。</summary>
            public void Ring(float cx, float cy, float outer, float inner, Color color, float alpha)
            {
                var x0 = Mathf.Max(0, Mathf.FloorToInt(cx - outer - 1f));
                var x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cx + outer + 1f));
                var y0 = Mathf.Max(0, Mathf.FloorToInt(cy - outer - 1f));
                var y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(cy + outer + 1f));

                for (var y = y0; y <= y1; y++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                        var cover = Mathf.Min(
                            Mathf.Clamp01(outer + 0.5f - d),
                            Mathf.Clamp01(d - inner + 0.5f));
                        Blend(x, y, color, alpha * cover);
                    }
                }
            }

            public void Triangle(
                float ax, float ay, float bx, float by, float cx, float cy, Color color, float alpha)
            {
                var x0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ax, Mathf.Min(bx, cx)) - 1f));
                var x1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(ax, Mathf.Max(bx, cx)) + 1f));
                var y0 = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(ay, Mathf.Min(by, cy)) - 1f));
                var y1 = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(ay, Mathf.Max(by, cy)) + 1f));

                // 角を滑らかにするため、1画素を2×2で見て内側の割合を数える。
                for (var y = y0; y <= y1; y++)
                {
                    for (var x = x0; x <= x1; x++)
                    {
                        var hits = 0;
                        for (var sy = 0; sy < 2; sy++)
                        {
                            for (var sx = 0; sx < 2; sx++)
                            {
                                var px = x + 0.25f + sx * 0.5f;
                                var py = y + 0.25f + sy * 0.5f;
                                if (Inside(px, py, ax, ay, bx, by, cx, cy))
                                {
                                    hits++;
                                }
                            }
                        }

                        if (hits > 0)
                        {
                            Blend(x, y, color, alpha * (hits / 4f));
                        }
                    }
                }
            }

            /// <summary>太さのある線分。斜線に使う。</summary>
            public void Line(float x0, float y0, float x1, float y1, float width, Color color, float alpha)
            {
                var dx = x1 - x0;
                var dy = y1 - y0;
                var length = Mathf.Sqrt(dx * dx + dy * dy);
                if (length < 0.0001f)
                {
                    return;
                }

                var nx = -dy / length * (width * 0.5f);
                var ny = dx / length * (width * 0.5f);

                Triangle(x0 + nx, y0 + ny, x0 - nx, y0 - ny, x1 + nx, y1 + ny, color, alpha);
                Triangle(x1 + nx, y1 + ny, x1 - nx, y1 - ny, x0 - nx, y0 - ny, color, alpha);
            }

            private static bool Inside(
                float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
            {
                var d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
                var d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
                var d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
                var negative = d1 < 0f || d2 < 0f || d3 < 0f;
                var positive = d1 > 0f || d2 > 0f || d3 > 0f;
                return !(negative && positive);
            }

            private void Blend(int x, int y, Color color, float alpha)
            {
                if (alpha <= 0f || x < 0 || y < 0 || x >= Size || y >= Size)
                {
                    return;
                }

                var i = y * Size + x;
                var under = pixels[i];
                var a = Mathf.Clamp01(alpha);
                var outA = a + under.a * (1f - a);
                if (outA <= 0f)
                {
                    pixels[i] = new Color(0f, 0f, 0f, 0f);
                    return;
                }

                var r = (color.r * a + under.r * under.a * (1f - a)) / outA;
                var g = (color.g * a + under.g * under.a * (1f - a)) / outA;
                var b = (color.b * a + under.b * under.a * (1f - a)) / outA;
                pixels[i] = new Color(r, g, b, outA);
            }

            public Sprite ToSprite()
            {
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.DontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };

                // 描くときは上から下へ数えた。テクスチャは下から上なので入れ替える。
                var flipped = new Color[pixels.Length];
                for (var y = 0; y < Size; y++)
                {
                    for (var x = 0; x < Size; x++)
                    {
                        flipped[(Size - 1 - y) * Size + x] = pixels[y * Size + x];
                    }
                }

                texture.SetPixels(flipped);
                texture.Apply(false, false);

                var sprite = Sprite.Create(
                    texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
                sprite.hideFlags = HideFlags.DontSave;
                return sprite;
            }
        }
    }
}
