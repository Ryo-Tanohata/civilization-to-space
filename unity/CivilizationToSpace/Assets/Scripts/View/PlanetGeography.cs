using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 時代ごとの大陸の配置。
    ///
    /// **これは古地理の再現ではない。** 時代データの品質状態はすべて「象徴表現案・出典未検証」であり、
    /// 注意書きも「年代・分布・順序の厳密さを保証しません」と明記している。
    /// ここで置いているのは、地球の長い変化を読み取りやすくするためにデフォルメした象徴的な配置であり、
    /// 特定の年代の大陸の位置・形・面積を主張するものではない。
    ///
    /// 並びの意図は次のとおり。小さな陸塊がいくつか → ひとつの大きな陸 → 分かれた複数の大陸。
    /// 「陸が集まり、また分かれた」という変化が絵として読めることだけを狙っている。
    ///
    /// 塊は緯度・経度・半径（度）・高さで表す。重ね合わせて輪郭を作り、
    /// ノイズで海岸線を崩す。円のまま残すと人工物に見えるためである。
    /// </summary>
    public static class PlanetGeography
    {
        /// <summary>塊の大きさの倍率。全体の陸の量を一括で調整する。</summary>
        private const float Scale = 1.18f;

        /// <summary>ひとつの陸塊。緯度、経度、半径（度）、高さ。</summary>
        private struct Landmass
        {
            public readonly float Latitude;
            public readonly float Longitude;
            public readonly float Radius;
            public readonly float Height;

            public Landmass(float latitude, float longitude, float radius, float height)
            {
                Latitude = latitude;
                Longitude = longitude;
                Radius = radius;
                Height = height;
            }
        }

        /// <summary>小さな陸塊がいくつか散っている段階。</summary>
        private static readonly Landmass[] Cratons =
        {
            new Landmass(12f, -20f, 17f, 0.90f),
            new Landmass(-5f, 60f, 15f, 0.85f),
            new Landmass(36f, 120f, 14f, 0.85f),
            new Landmass(-28f, -150f, 12f, 0.80f)
        };

        /// <summary>陸がひとつに集まった段階。</summary>
        private static readonly Landmass[] Supercontinent =
        {
            new Landmass(0f, 10f, 42f, 1.00f),
            new Landmass(28f, 22f, 30f, 0.95f),
            new Landmass(-30f, 26f, 30f, 0.95f),
            new Landmass(12f, 52f, 24f, 0.90f),
            new Landmass(-8f, -18f, 22f, 0.85f)
        };

        /// <summary>陸が分かれた段階。今の地球に近い並びへデフォルメしている。</summary>
        private static readonly Landmass[] Separated =
        {
            new Landmass(50f, -100f, 23f, 1.00f),
            new Landmass(30f, -95f, 13f, 0.90f),
            new Landmass(70f, -42f, 9f, 0.80f),
            new Landmass(-8f, -60f, 19f, 0.95f),
            new Landmass(-30f, -62f, 12f, 0.85f),
            new Landmass(8f, 18f, 21f, 1.00f),
            new Landmass(-24f, 26f, 15f, 0.90f),
            new Landmass(56f, 70f, 36f, 1.00f),
            new Landmass(46f, 14f, 17f, 0.90f),
            new Landmass(30f, 105f, 21f, 0.95f),
            new Landmass(-25f, 134f, 13f, 0.85f),
            new Landmass(-82f, 0f, 20f, 0.85f)
        };

        /// <summary>陸がまったく無い段階。溶岩の時代に使う。</summary>
        private static readonly Landmass[] None = new Landmass[0];

        private static Landmass[] ForEra(int eraIndex)
        {
            switch (eraIndex)
            {
                case 0:
                    return None;
                case 1:
                case 2:
                    return Cratons;
                case 3:
                    return Supercontinent;
                default:
                    return Separated;
            }
        }

        /// <summary>
        /// 方向ベクトルに対する陸の強さ。0で完全な海、大きいほど内陸で高い。
        /// 緯度・経度ではなく方向ベクトルを受け取るのは、経度0度に継ぎ目を作らないためである。
        /// </summary>
        public static float Sample(Vector3 direction, int eraIndex)
        {
            var masses = ForEra(eraIndex);
            var strongest = 0f;

            for (var i = 0; i < masses.Length; i++)
            {
                var mass = masses[i];
                var center = FromLatitudeLongitude(mass.Latitude, mass.Longitude);

                // 方向どうしの内積から中心角を出す。球面上の距離である。
                var angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction, center), -1f, 1f)) * Mathf.Rad2Deg;
                var radius = mass.Radius * Scale;
                if (angle >= radius)
                {
                    continue;
                }

                var falloff = 1f - angle / radius * (angle / radius);
                var value = mass.Height * falloff;
                if (value > strongest)
                {
                    strongest = value;
                }
            }

            return strongest;
        }

        private static Vector3 FromLatitudeLongitude(float latitude, float longitude)
        {
            var a = latitude * Mathf.Deg2Rad;
            var b = longitude * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * Mathf.Cos(b), Mathf.Sin(a), Mathf.Cos(a) * Mathf.Sin(b));
        }
    }
}
