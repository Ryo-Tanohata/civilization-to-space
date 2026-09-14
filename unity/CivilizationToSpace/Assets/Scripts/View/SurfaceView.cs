using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 地表から見た風景。空・地面・立っているものを、色と陰影のある形で置く。
    ///
    /// **デフォルメした実物として描く。影（シルエット）では描かない。**
    /// 形は単純な塊の組み合わせで、細部は作り込まない。
    /// 化石から分かるのは骨格までで、皮膚の色も質感も分かっていない。
    /// ここで置く色は読みやすさのための決めであり、**復元色ではない。**
    /// 特定の種も表していない。首の長さ・胴の長さ・脚の高さの比を変えているだけである。
    ///
    /// **遠いものほど空の色へ溶かす。** 同じ色で塗ると、重なったところが
    /// 一つの塊にしか見えない。遠さで色を薄めると前後の層が分かれる。
    ///
    /// 置くものの種類と数と色は外から受け取る。場面ごとの専用コードを書かない。
    /// 時代が増えるたびに書き足すと、必ず破綻するためである。
    /// </summary>
    public sealed class SurfaceView : MonoBehaviour
    {
        /// <summary>地表の風景を決める値。時代データから作る。</summary>
        public struct Landscape
        {
            /// <summary>空の上側と地平線側の色。</summary>
            public Color SkyHigh;
            public Color SkyLow;

            /// <summary>地面の色。</summary>
            public Color Ground;

            /// <summary>幹・葉・生きもの・建物・窓の色。</summary>
            public Color Trunk;
            public Color Foliage;
            public Color Creature;
            public Color Building;
            public Color Window;

            /// <summary>ものを置く範囲。寄り方に合わせて変える。</summary>
            public float NearZ;
            public float FarZ;
            public float HalfWidth;

            /// <summary>植物の背の高さ（地面からの目安、メートル相当）。</summary>
            public float PlantHeight;

            /// <summary>建物の高さの目安。</summary>
            public float BuildingHeight;

            /// <summary>
            /// 建物どうしの間隔。建物の幅に対する倍率。
            ///
            /// 1.05なら背中合わせに詰まり、2.5なら道を挟んで並ぶ。
            /// 新石器時代の集落（チャタルホユック）は道が無く、家が隙間なく
            /// 詰まっていたことが分かっている。散らして置くと別のものになる。
            /// </summary>
            public float BuildingSpacing;

            /// <summary>
            /// 建物に窓の帯を入れるか。
            ///
            /// **新石器時代の集落には入れない。** チャタルホユックの家は
            /// 屋根から梯子で出入りしており、壁に並ぶ窓という作りではない。
            /// 高さだけで決めると、少し高い家に窓の帯が出てしまう。
            /// </summary>
            public bool Windows;

            public int Conifers;
            public int Ferns;
            public int Broadleaves;
            public int Quadrupeds;
            public int Bipeds;
            public int Buildings;

            /// <summary>
            /// 枯れた幹の数。衝突のあとの時代で使う。
            ///
            /// 形の語彙を増やすのは避けたいが、葉のある木の数を0にしても
            /// 「葉が落ちた幹だけが立っている」姿にはならない。ここは足すほかない。
            /// </summary>
            public int DeadTrunks;

            /// <summary>
            /// 岩の数。
            ///
            /// **形の語彙を1つ増やすだけの価値がある。**
            /// 溶けた地球・最初の海・全球凍結には植物も生きものも置かない。
            /// 岩が無いと、空と地面の色の帯だけになって何も読み取れない。
            /// 氷期でもまばらに置く。ひとつの形が4つの場面を助ける。
            /// </summary>
            public int Rocks;

            /// <summary>岩の色。</summary>
            public Color Rock;

            /// <summary>
            /// 落ちてくるものの見かけの大きさ（メートル相当）。0なら出さない。
            ///
            /// 巨大衝突では火星ほどの天体、白亜紀の終わりでは直径10〜15kmの小天体。
            /// **どちらも実際の大きさも速さも表していない。** 空を横切り、
            /// 地平線の向こうで光る、という出来事の順序だけを見せる。
            /// </summary>
            public float ImpactorSize;

            /// <summary>落ちてくるものの色。</summary>
            public Color Impactor;

            /// <summary>落ちてから次に落ちるまでの秒数。0なら出さない。</summary>
            public float ImpactSeconds;

            /// <summary>
            /// 地面が自ら放つ色。黒なら光らない。マグマの時代で使う。
            /// 溶けた地面は光を受ける面ではなく、光そのものだからである。
            /// </summary>
            public Color GroundGlow;

            /// <summary>置き方を決める種。同じ種なら同じ並びになる。</summary>
            public int Seed;
        }

        /// <summary>遠さで色を変える段の数。</summary>
        private const int DepthSteps = 8;

        /// <summary>遠くのものを空の色へどれだけ寄せるか。1で完全に溶ける。</summary>
        private const float HazeStrength = 0.34f;

        /// <summary>
        /// 1日の長さ（秒）。宇宙から見た地球の自転（1周10秒）と同じにしてある。
        ///
        /// 同じ地球を、離れて見るか地面から見るかの違いでしかない。
        /// 別々の長さにすると、画面を切り替えたときに時間の進み方が食い違う。
        /// **実際の1日を表す秒数ではない。**
        /// </summary>
        public const float DaySeconds = 10f;

        /// <summary>夜の空の色。上と地平線側。</summary>
        private static readonly Color NightHigh = new Color(0.018f, 0.030f, 0.070f, 1f);
        private static readonly Color NightLow = new Color(0.055f, 0.085f, 0.150f, 1f);

        /// <summary>朝夕の地平線の色。太陽が低いときだけ混ぜる。</summary>
        private static readonly Color DuskLow = new Color(0.93f, 0.55f, 0.28f, 1f);

        /// <summary>月あかりの色。夜に地面と木を照らす。</summary>
        private static readonly Color MoonLight = new Color(0.62f, 0.72f, 1f, 1f);

        private HideFlags createdFlags;
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private Material skyMaterial;
        private Texture2D skyTexture;
        private Material starMaterial;
        private Texture2D starTexture;
        private Transform impactor;
        private Transform trail;
        private Transform flash;
        private Transform column;
        private Material impactorMaterial;
        private Material trailMaterial;
        private Material flashMaterial;
        private Material columnMaterial;

        /// <summary>
        /// ぶつかった瞬間の揺れ。0で揺れない。
        /// カメラを持っている側（<see cref="AppRoot"/>）が受け取って動かす。
        /// 音も振動も出せないので、**揺れだけが「ぶつかった」ことを伝える手段**である。
        /// </summary>
        public float Shake { get; private set; }
        private Landscape current;
        private System.Random random;

        private static Mesh coneMesh;

        /// <summary>生きものが画面に収まる寄り方。背の高さを見るための位置。</summary>
        public static void AimAtCreatures(Camera camera)
        {
            camera.transform.position = new Vector3(0f, 8f, -38f);
            camera.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
            camera.fieldOfView = 52f;
        }

        /// <summary>街が画面に収まる寄り方。建物の並びを見るための位置。</summary>
        public static void AimAtSettlement(Camera camera)
        {
            camera.transform.position = new Vector3(0f, 95f, -300f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            camera.fieldOfView = 52f;
        }

        public void Build(Landscape land, HideFlags flags)
        {
            Clear();

            createdFlags = flags;
            current = land;
            random = new System.Random(land.Seed);

            BuildSky(land);
            BuildGround(land);
            BuildImpactor(land);

            Scatter(land.Conifers, BuildConifer, land.PlantHeight, false);
            Scatter(land.Ferns, BuildFern, land.PlantHeight * 0.30f, false);
            Scatter(land.Broadleaves, BuildBroadleaf, land.PlantHeight * 0.75f, false);
            Scatter(land.Rocks, BuildRock, Mathf.Max(1.2f, land.PlantHeight * 0.22f), false);
            Scatter(land.DeadTrunks, BuildDeadTrunk, land.PlantHeight * 0.8f, false);
            PlaceBuildings(land);
            // 生きものは手前寄りに置く。奥へ撒くと小さすぎて形が読めない。
            ScatterNear(land.Quadrupeds, BuildQuadruped, land.PlantHeight * 0.85f, 0.34f);
            ScatterNear(land.Bipeds, BuildBiped, land.PlantHeight * 0.40f, 0.26f);
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            foreach (var material in materials.Values)
            {
                DestroyImmediate(material);
            }

            materials.Clear();
            DestroyImmediate(skyMaterial);
            DestroyImmediate(skyTexture);
            DestroyImmediate(starMaterial);
            DestroyImmediate(starTexture);
            skyMaterial = null;
            skyTexture = null;
            starMaterial = null;
            starTexture = null;

            DestroyImmediate(impactorMaterial);
            DestroyImmediate(trailMaterial);
            DestroyImmediate(flashMaterial);
            DestroyImmediate(columnMaterial);
            impactorMaterial = null;
            trailMaterial = null;
            flashMaterial = null;
            columnMaterial = null;
            impactor = null;
            trail = null;
            flash = null;
            column = null;
            Shake = 0f;
        }

        /// <summary>
        /// 空。縦の色の変化だけを焼いた絵を、遠くの板に貼る。
        /// 光の当たり方に左右されないよう、地色を黒にして発光だけで出す。
        /// 空は光を受ける面ではなく、光そのものだからである。
        /// </summary>
        private void BuildSky(Landscape land)
        {
            const int height = 128;
            skyTexture = new Texture2D(1, height, TextureFormat.RGBA32, false);
            skyTexture.hideFlags = createdFlags;
            skyTexture.wrapMode = TextureWrapMode.Clamp;

            for (var y = 0; y < height; y++)
            {
                var t = y / (float)(height - 1);
                skyTexture.SetPixel(0, y, Color.Lerp(land.SkyLow, land.SkyHigh, Mathf.Pow(t, 0.6f)));
            }

            skyTexture.Apply();

            skyMaterial = StandardMaterials.CreateOpaque(true);
            skyMaterial.hideFlags = createdFlags;
            skyMaterial.color = Color.black;
            skyMaterial.SetTexture("_EmissionMap", skyTexture);
            skyMaterial.SetColor("_EmissionColor", Color.white);
            skyMaterial.SetFloat("_Blend", 0f);

            var distance = land.FarZ * 1.35f;
            var sky = PrimitiveMeshes.Create(PrimitiveType.Quad, "Sky", createdFlags);
            sky.transform.SetParent(transform, false);
            sky.transform.localPosition = new Vector3(0f, distance * 0.36f, distance);
            sky.transform.localScale = new Vector3(distance * 4f, distance * 1.9f, 1f);
            sky.GetComponent<Renderer>().sharedMaterial = skyMaterial;

            BuildStars(distance);
        }

        /// <summary>
        /// 落ちてくるものと、地平線の向こうの光。
        ///
        /// **地表からも衝突が見えるようにする。** 宇宙から見ているときだけ
        /// ぶつかる様子が出て、地表へ降りると結果しか無いのでは、
        /// 同じ出来事を見ている感じにならない。
        ///
        /// 加算で光らせる。昼でも夜でも、足した明るさとして出る。
        /// </summary>
        private void BuildImpactor(Landscape land)
        {
            if (land.ImpactorSize <= 0f || land.ImpactSeconds <= 0f)
            {
                return;
            }

            impactorMaterial = StandardMaterials.CreateGlow();
            impactorMaterial.hideFlags = createdFlags;
            impactorMaterial.color = Color.white;
            impactorMaterial.SetColor("_EmissionColor", land.Impactor);

            var body = PrimitiveMeshes.Create(PrimitiveType.Sphere, "Impactor", createdFlags);
            body.transform.SetParent(transform, false);
            body.transform.localScale = Vector3.one * land.ImpactorSize;
            body.GetComponent<Renderer>().sharedMaterial = impactorMaterial;
            impactor = body.transform;

            // 尾。落ちてくるものの後ろへ伸ばす。点だけだと星と見分けが付かない。
            trailMaterial = StandardMaterials.CreateGlow();
            trailMaterial.hideFlags = createdFlags;
            trailMaterial.color = Color.white;
            trailMaterial.SetColor("_EmissionColor", Color.black);

            var tail = PrimitiveMeshes.Create(PrimitiveType.Cylinder, "Trail", createdFlags);
            tail.transform.SetParent(transform, false);
            tail.GetComponent<Renderer>().sharedMaterial = trailMaterial;
            trail = tail.transform;

            flashMaterial = StandardMaterials.CreateGlow();
            flashMaterial.hideFlags = createdFlags;
            flashMaterial.color = Color.white;
            flashMaterial.SetColor("_EmissionColor", Color.black);

            var burst = PrimitiveMeshes.Create(PrimitiveType.Sphere, "Flash", createdFlags);
            burst.transform.SetParent(transform, false);
            burst.transform.localPosition = ImpactPoint(land);
            burst.GetComponent<Renderer>().sharedMaterial = flashMaterial;
            flash = burst.transform;

            // 立ちのぼる粉塵。光ったあとに残る。光だけだと日の出に見える。
            columnMaterial = StandardMaterials.CreateGlow();
            columnMaterial.hideFlags = createdFlags;
            columnMaterial.color = Color.white;
            columnMaterial.SetColor("_EmissionColor", Color.black);

            var dust = PrimitiveMeshes.Create(PrimitiveType.Cylinder, "Column", createdFlags);
            dust.transform.SetParent(transform, false);
            dust.GetComponent<Renderer>().sharedMaterial = columnMaterial;
            column = dust.transform;
        }

        /// <summary>ぶつかる場所。地平線の少し手前へ置く。</summary>
        private static Vector3 ImpactPoint(Landscape land)
        {
            return new Vector3(land.HalfWidth * 0.5f, 0f, land.FarZ * 0.9f);
        }

        /// <summary>
        /// 落ちてくるものを進める。0で空の高いところ、1で次の周回の始め。
        ///
        /// 0.0〜0.72 で空を横切って地平線へ落ち、0.72〜0.88 で光り、そこから消える。
        /// **落ちる速さも間隔も、実際の出来事を表していない。**
        /// </summary>
        public void SetImpact(float phase)
        {
            if (impactor == null || flash == null)
            {
                return;
            }

            phase -= Mathf.Floor(phase);

            var land = current;
            var hit = ImpactPoint(land);
            var falling = phase < 0.72f;

            impactor.gameObject.SetActive(falling);
            trail.gameObject.SetActive(falling);
            if (falling)
            {
                var t = phase / 0.72f;

                // 遠くの高いところから、落ちる場所へ向かわせる。
                var from = new Vector3(-land.HalfWidth * 1.7f, land.FarZ * 0.75f, land.FarZ * 0.9f);
                var place = Vector3.Lerp(from, hit, t * t);
                impactor.localPosition = place;

                // 近づくほど大きく明るくする。遠いうちは点にしか見えない。
                var grow = 1f + t * t * 2.4f;
                impactor.localScale = Vector3.one * (land.ImpactorSize * grow);
                impactorMaterial.SetColor("_EmissionColor", land.Impactor * (0.8f + t * 2.2f));

                // **尾を引かせる。** 点が動くだけでは星と見分けが付かない。
                // 通ってきた向きへ伸ばし、近づくほど長くする。
                var back = (from - hit).normalized;
                var length = land.ImpactorSize * (3f + t * 9f);
                trail.localPosition = place + back * length;
                trail.localRotation = Quaternion.FromToRotation(Vector3.up, back);
                trail.localScale = new Vector3(
                    land.ImpactorSize * 0.5f * grow, length, land.ImpactorSize * 0.5f * grow);
                trailMaterial.SetColor("_EmissionColor", land.Impactor * (0.25f + t * 0.8f));
            }

            // 光は短く強く。長く光らせると日の出に見える。
            var burning = phase >= 0.72f && phase < 0.80f;
            flash.gameObject.SetActive(burning);
            if (burning)
            {
                var t = (phase - 0.72f) / 0.08f;

                // **広がる大きさに上限を置く。**
                // 大きいものほど大きく光らせると、光の球がカメラを包んでしまう。
                // 内側から見ると面が裏を向くので、何も映らなくなる。
                var widest = Mathf.Min(land.ImpactorSize * 22f, land.FarZ * 0.45f);
                var size = Mathf.Lerp(land.ImpactorSize * 2f, widest, Mathf.Sqrt(t));
                flash.localPosition = hit;
                flash.localScale = Vector3.one * size;
                flashMaterial.SetColor("_EmissionColor", Color.white * (1f - t) * 3.4f);
            }

            // 光ったあと、粉塵が立ちのぼって薄れる。
            var rising = phase >= 0.74f && phase < 1f;
            column.gameObject.SetActive(rising);
            if (rising)
            {
                var t = (phase - 0.74f) / 0.26f;
                // 太さと高さに上限を置く。大きいものほど太くすると板に見える。
                var height = Mathf.Min(land.FarZ * 0.55f, land.ImpactorSize * 14f)
                             * Mathf.Lerp(0.12f, 1f, Mathf.Sqrt(t));
                var width = Mathf.Min(land.FarZ * 0.075f, land.ImpactorSize * 9f)
                            * Mathf.Lerp(0.5f, 1f, t);

                column.localPosition = hit + new Vector3(0f, height * 0.5f, 0f);
                column.localScale = new Vector3(width, height * 0.5f, width);
                columnMaterial.SetColor("_EmissionColor",
                    Color.Lerp(land.Impactor, current.SkyLow, 0.5f) * (1f - t) * 0.8f);
            }

            // 揺れ。ぶつかった直後だけ強く、すぐ収まる。
            Shake = phase >= 0.72f && phase < 0.86f
                ? (1f - (phase - 0.72f) / 0.14f)
                : 0f;
        }

        /// <summary>
        /// 星。空の板のすぐ手前へ、加算で重ねる。
        ///
        /// **加算にするのは、昼に自然と消えるためである。**
        /// 加算は足し算なので、明るさを0にすれば何も足されない。
        /// 昼夜で星を出し入れする処理が要らず、夜の深さをそのまま明るさにできる。
        ///
        /// 位置も明るさも計算で散らしている。実際の星座を表していない。
        /// 帯を1本入れてあるのは天の川に当たるが、形も向きも実際のものではない。
        /// </summary>
        private void BuildStars(float distance)
        {
            const int size = 512;
            starTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            starTexture.hideFlags = createdFlags;
            starTexture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(0, 0, 0, 255);
            }

            var starRandom = new System.Random(20260915);

            // 天の川にあたる帯。細かい星を濃く撒く。
            for (var i = 0; i < 4200; i++)
            {
                var u = (float)starRandom.NextDouble();
                var spread = (float)(starRandom.NextDouble() + starRandom.NextDouble() - 1.0) * 0.055f;
                var vBand = 0.62f + Mathf.Sin(u * 3.1f) * 0.10f + spread;
                if (vBand < 0f || vBand >= 1f)
                {
                    continue;
                }

                var level = (byte)(28 + starRandom.Next(46));
                Put(pixels, size, (int)(u * size), (int)(vBand * size), level, level, (byte)(level + 8));
            }

            // 全天に散る星。少数だけ大きく明るくする。
            for (var i = 0; i < 1500; i++)
            {
                var x = starRandom.Next(size);
                var y = starRandom.Next(size);
                var bright = (float)starRandom.NextDouble();
                var level = (byte)Mathf.Clamp(40f + bright * bright * bright * 215f, 0f, 255f);

                // 色を少しだけ振る。青白い星と橙の星が混ざると空が単調でなくなる。
                var warm = starRandom.Next(4) == 0;
                Put(pixels, size, x, y,
                    warm ? level : (byte)(level * 0.86f),
                    (byte)(level * 0.92f),
                    warm ? (byte)(level * 0.78f) : level);

                if (level > 210)
                {
                    // 明るい星だけ十字ににじませる。粒だけだと点にしか見えない。
                    Put(pixels, size, x + 1, y, (byte)(level / 3), (byte)(level / 3), (byte)(level / 3));
                    Put(pixels, size, x - 1, y, (byte)(level / 3), (byte)(level / 3), (byte)(level / 3));
                    Put(pixels, size, x, y + 1, (byte)(level / 3), (byte)(level / 3), (byte)(level / 3));
                    Put(pixels, size, x, y - 1, (byte)(level / 3), (byte)(level / 3), (byte)(level / 3));
                }
            }

            starTexture.SetPixels32(pixels);
            starTexture.Apply();

            starMaterial = StandardMaterials.CreateGlow();
            starMaterial.hideFlags = createdFlags;
            starMaterial.color = Color.white;
            starMaterial.SetTexture("_EmissionMap", starTexture);
            starMaterial.SetColor("_EmissionColor", Color.black);

            var stars = PrimitiveMeshes.Create(PrimitiveType.Quad, "Stars", createdFlags);
            stars.transform.SetParent(transform, false);
            stars.transform.localPosition = new Vector3(0f, distance * 0.36f, distance * 0.985f);
            stars.transform.localScale = new Vector3(distance * 4f, distance * 1.9f, 1f);
            stars.GetComponent<Renderer>().sharedMaterial = starMaterial;
        }

        private static void Put(Color32[] pixels, int size, int x, int y, byte r, byte g, byte b)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
            {
                return;
            }

            var index = y * size + x;
            var old = pixels[index];
            pixels[index] = new Color32(
                (byte)Mathf.Min(255, old.r + r),
                (byte)Mathf.Min(255, old.g + g),
                (byte)Mathf.Min(255, old.b + b),
                255);
        }

        /// <summary>
        /// 時刻を入れる。0で真夜中、0.25で日の出、0.5で正午、0.75で日の入り。
        ///
        /// 空の色・星の明るさ・光の向きと強さ・回り込む明るさを、まとめて決める。
        /// 別々に持つと、夜なのに地面だけ明るいといった食い違いが起きる。
        /// </summary>
        public void SetTimeOfDay(float phase, Light sun)
        {
            if (skyTexture == null)
            {
                return;
            }

            phase -= Mathf.Floor(phase);

            // 太陽の高さ。-1で真夜中、+1で正午。
            var elevation = Mathf.Sin((phase - 0.25f) * Mathf.PI * 2f);

            // 昼の度合い。
            //
            // **太陽が地平線にある瞬間は、まだ空が明るい。**
            // 日が沈んだ直後も薄明が残り、暗くなるのはもう少し経ってからである。
            // 地平線（elevation=0）で0.5ほどになるようにし、
            // そこから下がるにつれてゆっくり夜へ向かわせる。
            var daylight = Mathf.Clamp01(elevation * 1.5f + 0.50f);

            // 朝夕の赤み。太陽が地平線の近くにあるときだけ強い。
            var dusk = Mathf.Clamp01(1f - Mathf.Abs(elevation) * 3.4f);

            var high = Color.Lerp(NightHigh, current.SkyHigh, daylight);
            var low = Color.Lerp(NightLow, current.SkyLow, daylight);
            low = Color.Lerp(low, DuskLow, dusk * 0.85f);

            var height = skyTexture.height;
            for (var y = 0; y < height; y++)
            {
                var t = y / (float)(height - 1);
                skyTexture.SetPixel(0, y, Color.Lerp(low, high, Mathf.Pow(t, 0.6f)));
            }

            skyTexture.Apply();

            if (starMaterial != null)
            {
                // 星は夜の深さでそのまま明るくする。薄明のあいだは弱い。
                // 星は薄明のあいだに急に消える。少しでも空が明るいと見えない。
                var night = Mathf.Clamp01(1f - daylight * 2.6f);
                night *= night;
                starMaterial.SetColor("_EmissionColor", Color.white * night);
            }

            if (sun != null)
            {
                var day = elevation > 0f;

                // 夜は月あかりに置き換える。真っ暗にすると地形も輪郭も読めない。
                var pitch = Mathf.Lerp(4f, 62f, Mathf.Abs(elevation));
                sun.transform.rotation = Quaternion.Euler(pitch, day ? -35f : 145f, 0f);
                sun.color = day
                    ? Color.Lerp(new Color(1f, 0.74f, 0.52f), Color.white, Mathf.Clamp01(elevation * 2.4f))
                    : MoonLight;
                sun.intensity = day
                    ? Mathf.Lerp(0.35f, 1.25f, Mathf.Clamp01(elevation * 1.6f))
                    : 0.16f;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(NightLow * 0.7f, current.SkyLow * 0.5f, daylight);
        }

        /// <summary>
        /// 地面。帯に分けて、遠い帯ほど空の色へ寄せる。
        /// 1枚の板だと手前も地平線も同じ色になり、奥行きが消えるためである。
        /// </summary>
        private void BuildGround(Landscape land)
        {
            const int bands = DepthSteps;
            var end = land.FarZ * 1.3f;

            for (var i = 0; i < bands; i++)
            {
                var from = Mathf.Lerp(-land.FarZ * 0.2f, end, i / (float)bands);
                var to = Mathf.Lerp(-land.FarZ * 0.2f, end, (i + 1) / (float)bands);

                var band = PrimitiveMeshes.Create(PrimitiveType.Cube, "GroundBand", createdFlags);
                band.transform.SetParent(transform, false);
                band.transform.localPosition = new Vector3(0f, -1f, (from + to) * 0.5f);
                band.transform.localScale = new Vector3(land.HalfWidth * 9f, 2f, to - from);
                var material = Tinted("Ground", land.Ground, i);
                if (land.GroundGlow.maxColorComponent > 0.001f)
                {
                    // 溶けた地面は自ら光る。遠いぶんは弱める。
                    var fade = 1f - i / (float)(bands - 1) * 0.6f;
                    material.SetColor("_EmissionColor", land.GroundGlow * fade);
                }

                band.GetComponent<Renderer>().sharedMaterial = material;
            }
        }

        /// <summary>
        /// 色と遠さから材質を作る。作った材質は使い回す。
        /// 遠いものほど空の色へ寄せることで、前後の層が分かれて見える。
        /// </summary>
        private Material Tinted(string key, Color baseColor, int step)
        {
            var index = Mathf.Clamp(step, 0, DepthSteps - 1);
            var id = key + "/" + index;

            Material material;
            if (materials.TryGetValue(id, out material) && material != null)
            {
                return material;
            }

            var t = DepthSteps > 1 ? index / (float)(DepthSteps - 1) : 0f;
            material = StandardMaterials.CreateOpaque(true);
            material.hideFlags = createdFlags;
            material.color = Color.Lerp(baseColor, current.SkyLow, t * HazeStrength);
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            materials[id] = material;
            return material;
        }

        private int StepFor(float z)
        {
            var t = Mathf.InverseLerp(current.NearZ, current.FarZ, z);
            return Mathf.RoundToInt(t * (DepthSteps - 1));
        }

        /// <summary>置いたあとに、遠さに合わせて色を貼り直す。</summary>
        private void ApplyDepth(GameObject item, float z)
        {
            var step = StepFor(z);
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                var key = renderer.gameObject.name;
                renderer.sharedMaterial = Tinted(key, ColorFor(key), step);
            }
        }

        private Color ColorFor(string key)
        {
            switch (key)
            {
                case "Trunk": return current.Trunk;
                case "Foliage": return current.Foliage;
                case "Creature": return current.Creature;
                case "Rock": return current.Rock;
                case "Building": return current.Building;
                case "Window": return current.Window;
                default: return current.Ground;
            }
        }

        /// <summary>
        /// 置く。<paramref name="profile"/> が真なら横向きに寄せる。
        /// 生きものと建物は、正面を向くと形が重なって読めなくなる。
        /// </summary>
        private void Scatter(int count, Func<float, GameObject> build, float height, bool profile)
        {
            for (var i = 0; i < count; i++)
            {
                // 手前を薄く、奥を濃くする。同じ密度で撒くと手前が詰まりすぎる。
                var depth = Mathf.Pow((float)random.NextDouble(), 0.62f);
                var z = Mathf.Lerp(current.NearZ, current.FarZ, depth);
                var x = (float)(random.NextDouble() * 2.0 - 1.0) * current.HalfWidth;

                var scale = height * (0.62f + (float)random.NextDouble() * 0.95f);

                var item = build(scale);
                item.transform.SetParent(transform, false);
                item.transform.localPosition = new Vector3(x, 0f, z);

                var turn = profile
                    ? (random.Next(2) == 0 ? 90f : 270f) + (float)(random.NextDouble() * 2.0 - 1.0) * 26f
                    : (float)random.NextDouble() * 360f;
                item.transform.localRotation = Quaternion.Euler(0f, turn, 0f);

                ApplyDepth(item, z);
            }
        }

        /// <summary>
        /// 建物を並べる。散らさず、升目の上へ置いてから少しずらす。
        ///
        /// **人の建てたものは散らばらない。** 寄り集まって建つ。
        /// 新石器時代のチャタルホユックは家が背中合わせに詰まり、道が無かった
        /// （出典は docs/reports にある地表の報告）。ばらばらに撒くと、
        /// 集落にも都市にも見えない。
        /// </summary>
        private void PlaceBuildings(Landscape land)
        {
            if (land.Buildings <= 0)
            {
                return;
            }

            var pitch = Mathf.Max(1f, land.BuildingHeight * Mathf.Max(0.4f, land.BuildingSpacing));
            var columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(land.Buildings * 1.6f)));
            var rows = Mathf.Max(1, Mathf.CeilToInt(land.Buildings / (float)columns));
            var originX = -(columns - 1) * pitch * 0.5f;
            var originZ = Mathf.Lerp(land.NearZ, land.FarZ, 0.34f);

            var placed = 0;
            for (var row = 0; row < rows && placed < land.Buildings; row++)
            {
                for (var column = 0; column < columns && placed < land.Buildings; column++)
                {
                    placed++;

                    var jitter = pitch * 0.16f;
                    var x = originX + column * pitch
                        + (float)(random.NextDouble() * 2.0 - 1.0) * jitter;
                    var z = originZ + row * pitch
                        + (float)(random.NextDouble() * 2.0 - 1.0) * jitter;

                    var height = land.BuildingHeight * (0.7f + (float)random.NextDouble() * 0.8f);
                    var item = BuildStructure(height);
                    item.transform.SetParent(transform, false);
                    item.transform.localPosition = new Vector3(x, 0f, z);
                    item.transform.localRotation = Quaternion.Euler(0f,
                        (float)(random.NextDouble() * 2.0 - 1.0) * 8f, 0f);
                    ApplyDepth(item, z);
                }
            }
        }

        /// <summary>手前寄りに置く。<paramref name="reach"/> は置く範囲の割合。</summary>
        private void ScatterNear(int count, Func<float, GameObject> build, float height, float reach)
        {
            var far = Mathf.Lerp(current.NearZ, current.FarZ, reach);
            for (var i = 0; i < count; i++)
            {
                var z = Mathf.Lerp(current.NearZ, far, (float)random.NextDouble());
                var x = (float)(random.NextDouble() * 2.0 - 1.0) * current.HalfWidth * 0.55f;
                var scale = height * (0.7f + (float)random.NextDouble() * 0.6f);

                var item = build(scale);
                item.transform.SetParent(transform, false);
                item.transform.localPosition = new Vector3(x, 0f, z);
                item.transform.localRotation = Quaternion.Euler(0f,
                    (random.Next(2) == 0 ? 90f : 270f) + (float)(random.NextDouble() * 2.0 - 1.0) * 24f, 0f);
                ApplyDepth(item, z);
            }
        }

        private GameObject Piece(GameObject parent, PrimitiveType type, string role, Vector3 position, Vector3 scale)
        {
            var piece = PrimitiveMeshes.Create(type, role, createdFlags);
            piece.transform.SetParent(parent.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            return piece;
        }

        private GameObject ConePiece(GameObject parent, string role, Vector3 position, Vector3 scale)
        {
            var piece = new GameObject(role, typeof(MeshFilter), typeof(MeshRenderer));
            piece.hideFlags = createdFlags;
            piece.transform.SetParent(parent.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<MeshFilter>().sharedMesh = Cone();
            return piece;
        }

        private GameObject Root(string name)
        {
            var root = new GameObject(name);
            root.hideFlags = createdFlags;
            return root;
        }

        /// <summary>
        /// 岩。大きさの違う塊を寄せて、角のある形にする。
        /// 球ひとつだと石ころにしか見えない。
        /// </summary>
        private GameObject BuildRock(float height)
        {
            var root = Root("Rock");
            var lumps = 3 + random.Next(3);

            for (var i = 0; i < lumps; i++)
                {
                var r = height * (0.45f + (float)random.NextDouble() * 0.7f);
                var lump = Piece(root, PrimitiveType.Cube, "Rock", new Vector3(
                        (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.5f,
                        height * (0.18f + (float)random.NextDouble() * 0.5f),
                        (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.5f),
                    new Vector3(r, r * (0.5f + (float)random.NextDouble() * 0.6f), r * 0.9f));
                lump.transform.localRotation = Quaternion.Euler(
                    (float)random.NextDouble() * 40f - 20f,
                    (float)random.NextDouble() * 360f,
                    (float)random.NextDouble() * 40f - 20f);
            }

            return root;
        }

        /// <summary>
        /// 枯れた幹。葉を持たず、少し傾いて立つ。
        /// まっすぐ立てると柱にしか見えない。傾きと太さの違いで枯れ木に見せる。
        /// </summary>
        private GameObject BuildDeadTrunk(float height)
        {
            var root = Root("DeadTrunk");
            var lean = (float)(random.NextDouble() * 2.0 - 1.0) * 14f;

            var trunk = Piece(root, PrimitiveType.Cylinder, "Trunk",
                new Vector3(0f, height * 0.5f, 0f),
                new Vector3(height * 0.045f, height * 0.5f, height * 0.045f));
            trunk.transform.localRotation = Quaternion.Euler(lean, 0f, lean * 0.6f);

            // 折れた枝を2本だけ。多いと生きている木に見えてしまう。
            for (var i = 0; i < 2; i++)
            {
                var branch = Piece(root, PrimitiveType.Cylinder, "Trunk",
                    new Vector3(0f, height * (0.6f + i * 0.18f), 0f),
                    new Vector3(height * 0.022f, height * 0.16f, height * 0.022f));
                branch.transform.localRotation = Quaternion.Euler(
                    46f, (float)random.NextDouble() * 360f, 0f);
                branch.transform.localPosition += branch.transform.localRotation
                    * new Vector3(0f, height * 0.15f, 0f);
            }

            return root;
        }

        /// <summary>針葉樹。細い幹と、上へ細くなる円錐を重ねる。</summary>
        private GameObject BuildConifer(float height)
        {
            var root = Root("Conifer");
            Piece(root, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, height * 0.16f, 0f),
                new Vector3(height * 0.05f, height * 0.16f, height * 0.05f));
            ConePiece(root, "Foliage", new Vector3(0f, height * 0.20f, 0f),
                new Vector3(height * 0.52f, height * 0.50f, height * 0.52f));
            ConePiece(root, "Foliage", new Vector3(0f, height * 0.46f, 0f),
                new Vector3(height * 0.38f, height * 0.56f, height * 0.38f));
            return root;
        }

        /// <summary>シダ。短い幹と、上へ跳ねる葉の束。低いところを埋める。</summary>
        private GameObject BuildFern(float height)
        {
            var root = Root("Fern");
            Piece(root, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, height * 0.22f, 0f),
                new Vector3(height * 0.06f, height * 0.22f, height * 0.06f));

            const int fronds = 6;
            for (var i = 0; i < fronds; i++)
            {
                var angle = i / (float)fronds * 360f + (float)random.NextDouble() * 24f;
                var lean = 34f + (float)random.NextDouble() * 22f;
                var frond = Piece(root, PrimitiveType.Sphere, "Foliage", Vector3.zero,
                    new Vector3(height * 0.13f, height * 0.62f, height * 0.13f));
                frond.transform.localRotation = Quaternion.Euler(lean, angle, 0f);
                frond.transform.localPosition =
                    frond.transform.localRotation * new Vector3(0f, height * 0.62f, 0f)
                    + new Vector3(0f, height * 0.4f, 0f);
            }

            return root;
        }

        /// <summary>広葉樹。幹と、いびつな冠。球ひとつにすると円盤に見える。</summary>
        private GameObject BuildBroadleaf(float height)
        {
            var root = Root("Broadleaf");
            Piece(root, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, height * 0.32f, 0f),
                new Vector3(height * 0.06f, height * 0.32f, height * 0.06f));

            for (var i = 0; i < 5; i++)
            {
                var r = height * (0.16f + (float)random.NextDouble() * 0.17f);
                Piece(root, PrimitiveType.Sphere, "Foliage", new Vector3(
                        (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.26f,
                        height * (0.58f + (float)random.NextDouble() * 0.44f),
                        (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.26f),
                    new Vector3(r, r * 1.05f, r));
            }

            return root;
        }

        /// <summary>
        /// 四つ足の生きもの。首と尾を長く取る。
        /// **特定の種ではない。** 長い首と長い尾という形の傾向だけを置いている。
        /// </summary>
        private GameObject BuildQuadruped(float height)
        {
            var root = Root("Quadruped");
            // 脚を短く、胴を長く取る。脚が長いと竹馬に乗ったように浮いて見える。
            var body = height * 0.72f;
            var legs = height * 0.30f;

            Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, legs + body * 0.28f, 0f),
                new Vector3(body * 0.42f, body * 0.46f, body));

            // 首は球を隙間なく重ねて弧にする。離すと数珠に見える。
            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                var y = legs + body * (0.36f + t * 0.62f);
                var z = body * (0.42f + t * 0.78f + Mathf.Sin(t * 3.1f) * 0.1f);
                var r = Mathf.Lerp(body * 0.21f, body * 0.09f, t);
                Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, y, z), new Vector3(r, r, r));
            }

            Piece(root, PrimitiveType.Sphere, "Creature",
                new Vector3(0f, legs + body * 0.99f, body * 1.28f),
                new Vector3(body * 0.12f, body * 0.11f, body * 0.22f));

            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                var r = Mathf.Lerp(body * 0.17f, body * 0.03f, t);
                Piece(root, PrimitiveType.Sphere, "Creature",
                    new Vector3(0f, legs + body * (0.3f - t * 0.16f), -body * (0.45f + t * 0.72f)),
                    new Vector3(r, r, r));
            }

            for (var i = 0; i < 4; i++)
            {
                var front = i < 2;
                var side = (i % 2 == 0) ? 1f : -1f;
                Piece(root, PrimitiveType.Cylinder, "Creature",
                    new Vector3(side * body * 0.22f, legs * 0.5f, front ? body * 0.3f : -body * 0.3f),
                    new Vector3(body * 0.15f, legs * 0.5f, body * 0.15f));
            }

            return root;
        }

        /// <summary>二本足の生きもの。胴を立て、尾で釣り合わせる。</summary>
        private GameObject BuildBiped(float height)
        {
            var root = Root("Biped");
            var legs = height * 0.45f;
            var body = height * 0.5f;

            Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, legs + body * 0.4f, 0f),
                new Vector3(body * 0.5f, body * 0.6f, body * 0.85f));
            Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, legs + body * 0.9f, body * 0.4f),
                new Vector3(body * 0.26f, body * 0.24f, body * 0.34f));

            for (var i = 0; i < 8; i++)
            {
                var t = i / 7f;
                var r = Mathf.Lerp(body * 0.2f, body * 0.04f, t);
                Piece(root, PrimitiveType.Sphere, "Creature",
                    new Vector3(0f, legs + body * (0.36f - t * 0.1f), -body * (0.4f + t * 0.5f)),
                    new Vector3(r, r, r));
            }

            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? 1f : -1f;
                Piece(root, PrimitiveType.Cylinder, "Creature",
                    new Vector3(side * body * 0.18f, legs * 0.5f, 0f),
                    new Vector3(body * 0.13f, legs * 0.5f, body * 0.13f));
            }

            return root;
        }

        /// <summary>
        /// 人の建てたもの。高さと数だけで、集落から都市までを1つの作りで表す。
        ///
        /// 段階ごとに別の形を作らない。低くて小さいものを散らせば集落に、
        /// 高さを上げて数を増やせば都市になる。作り分けると、段階が増えるたびに
        /// 書き足すことになり、必ず破綻する。
        /// </summary>
        private GameObject BuildStructure(float height)
        {
            var root = Root("Structure");
            var width = height * (0.45f + (float)random.NextDouble() * 0.5f);
            var depth = width * (0.7f + (float)random.NextDouble() * 0.7f);

            Piece(root, PrimitiveType.Cube, "Building", new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height, depth));

            // **屋根は平らにする。三角屋根にしない。**
            // 新石器時代のチャタルホユックは平らな屋根で、屋根から梯子で出入りし、
            // 家のあいだに道が無かった。三角屋根を載せると別の時代の姿になる。
            if (!current.Windows || height < current.BuildingHeight * 0.85f)
            {
                Piece(root, PrimitiveType.Cube, "Building",
                    new Vector3(0f, height * 1.02f, 0f),
                    new Vector3(width * 1.06f, height * 0.05f, depth * 1.06f));
                return root;
            }

            // 高いものには窓の帯を入れる。のっぺりした箱に高さを感じさせるため。
            var floors = Mathf.Clamp(Mathf.RoundToInt(height / (current.BuildingHeight * 0.16f)), 2, 9);
            for (var i = 0; i < floors; i++)
            {
                var y = height * (0.16f + 0.78f * i / Mathf.Max(1, floors - 1));
                Piece(root, PrimitiveType.Cube, "Window", new Vector3(0f, y, 0f),
                    new Vector3(width * 1.02f, height * 0.035f, depth * 1.02f));
            }

            return root;
        }

        /// <summary>
        /// 円錐の網。針葉樹と屋根に使う。
        /// Unityの基本形には円錐が無いので自分で作る。球を積むと雪だるまになる。
        /// </summary>
        private static Mesh Cone()
        {
            if (coneMesh != null)
            {
                return coneMesh;
            }

            const int sides = 14;
            var vertices = new Vector3[sides + 2];
            var triangles = new int[sides * 6];

            vertices[0] = new Vector3(0f, 1f, 0f);
            vertices[sides + 1] = Vector3.zero;

            for (var i = 0; i < sides; i++)
            {
                var angle = i / (float)sides * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
            }

            for (var i = 0; i < sides; i++)
            {
                var a = i + 1;
                var b = (i + 1) % sides + 1;

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = b;
                triangles[i * 3 + 2] = a;

                triangles[sides * 3 + i * 3] = sides + 1;
                triangles[sides * 3 + i * 3 + 1] = a;
                triangles[sides * 3 + i * 3 + 2] = b;
            }

            coneMesh = new Mesh();
            coneMesh.name = "SurfaceCone";
            coneMesh.vertices = vertices;
            coneMesh.triangles = triangles;
            coneMesh.RecalculateNormals();
            coneMesh.hideFlags = HideFlags.DontSave;
            return coneMesh;
        }
    }
}
