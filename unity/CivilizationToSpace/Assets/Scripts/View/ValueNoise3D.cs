using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 3次元の値ノイズ。外部ライブラリを増やさずに大陸の形を作るために置く。
    ///
    /// 球面の方向ベクトルをそのまま入力にできるため、経度0度の位置に継ぎ目が出ない。
    /// Mathf.PerlinNoise は2次元しかなく、平面の座標を球へ貼ると継ぎ目と極の潰れが出る。
    ///
    /// 同じ入力からは常に同じ値が出る。乱数の種を持たず、再現できる。
    /// </summary>
    public static class ValueNoise3D
    {
        /// <summary>格子点の値。整数座標から決まる。</summary>
        private static float Hash(int x, int y, int z, int seed)
        {
            var n = x * 374761393 + y * 668265263 + z * 1274126177 + seed * 144665537;
            n = (n ^ (n >> 13)) * 1274126177;
            n = n ^ (n >> 16);
            return (n & 0x7FFFFFFF) / (float)0x7FFFFFFF;
        }

        /// <summary>なめらかに補間するための重み。端で傾きが0になる。</summary>
        private static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }

        public static float Sample(Vector3 point, int seed)
        {
            var xi = Mathf.FloorToInt(point.x);
            var yi = Mathf.FloorToInt(point.y);
            var zi = Mathf.FloorToInt(point.z);

            var xf = Smooth(point.x - xi);
            var yf = Smooth(point.y - yi);
            var zf = Smooth(point.z - zi);

            var c000 = Hash(xi, yi, zi, seed);
            var c100 = Hash(xi + 1, yi, zi, seed);
            var c010 = Hash(xi, yi + 1, zi, seed);
            var c110 = Hash(xi + 1, yi + 1, zi, seed);
            var c001 = Hash(xi, yi, zi + 1, seed);
            var c101 = Hash(xi + 1, yi, zi + 1, seed);
            var c011 = Hash(xi, yi + 1, zi + 1, seed);
            var c111 = Hash(xi + 1, yi + 1, zi + 1, seed);

            var x00 = Mathf.Lerp(c000, c100, xf);
            var x10 = Mathf.Lerp(c010, c110, xf);
            var x01 = Mathf.Lerp(c001, c101, xf);
            var x11 = Mathf.Lerp(c011, c111, xf);

            var y0 = Mathf.Lerp(x00, x10, yf);
            var y1 = Mathf.Lerp(x01, x11, yf);

            return Mathf.Lerp(y0, y1, zf);
        }

        /// <summary>細かさの違う層を重ねる。大陸の輪郭に凹凸を与える。</summary>
        public static float Fractal(Vector3 point, int seed, int octaves, float gain)
        {
            var sum = 0f;
            var amplitude = 1f;
            var total = 0f;

            for (var i = 0; i < octaves; i++)
            {
                sum += Sample(point, seed + i * 101) * amplitude;
                total += amplitude;
                amplitude *= gain;
                point *= 2f;
            }

            return total > 0f ? sum / total : 0f;
        }
    }
}
