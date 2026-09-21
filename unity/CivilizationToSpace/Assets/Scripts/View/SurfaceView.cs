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

            /// <summary>
            /// 下草の数。**手前をうめるための小さな草。**
            ///
            /// これまで画面の下半分は、何も置かれていない地面だった。
            /// 遠くに木が並ぶだけでは野の中にいる感じにならず、
            /// **影も落ちる先が無かった。** 手前に何も無い地面では、
            /// 遠くの木が落とす影は地平線ぞいの細い線に潰れてしまう。
            ///
            /// 1本1本は目立たなくてよい。集まったときに、
            /// 地面の起伏と光の向きが読めるようになる。
            /// </summary>
            public int GroundCover;

            /// <summary>
            /// 手前に転がす小石の数。草の生えていない時代に使う。
            /// 冥王代・最初の海・全球凍結には草が無いので、
            /// 手前をうめるものが石しかない。
            /// </summary>
            public int GroundRubble;

            /// <summary>
            /// 遠景に貼る写真の名前（Resources/Sky/ の中）。空なら描いた空を使う。
            ///
            /// **作った空には限界がある。** 色の帯と、手で作った雲と、
            /// 雑音で作った稜線を重ねても、「それらしい遠く」にはなっても
            /// 「そこにある遠く」にはならない。写真を1枚貼るほうが早い。
            ///
            /// 写真を使うときは、雲・太陽・稜線を作らない。
            /// 写真がすでにその3つを持っているので、重ねると二重になる。
            /// </summary>
            public string Backdrop;
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
            /// 四つ足の生きものの背の高さ（メートル相当）。
            ///
            /// **時代ごとに形の比を変える。同じ姿を使い回さない。**
            /// 首の長い大きな四つ足は白亜紀の終わりに絶滅しており、
            /// そのあとの時代に立っていてはいけない。
            /// </summary>
            public float CreatureHeight;

            /// <summary>胴の長さ（背の高さに対する比）。</summary>
            public float CreatureBody;

            /// <summary>脚の高さ（背の高さに対する比）。</summary>
            public float CreatureLegs;

            /// <summary>首の長さ。1で大きく伸び、0に近いほど頭が胴へ寄る。</summary>
            public float CreatureNeck;

            /// <summary>尾の長さ。1で長く伸び、0に近いほど短い。</summary>
            public float CreatureTail;

            /// <summary>牙を付けるか。</summary>
            public bool CreatureTusks;

            /// <summary>
            /// 落ちてくるものの見かけの大きさ（メートル相当）。0なら出さない。
            ///
            /// 巨大衝突では火星ほどの天体、白亜紀の終わりでは直径10〜14kmの小天体
            /// （NASA JPL「A 'Smoking Gun' for Dinosaur Extinction」）。
            /// **どちらも実際の大きさも速さも表していない。** 空を横切り、
            /// 地平線の向こうで光る、という出来事の順序だけを見せる。
            /// </summary>
            public float ImpactorSize;

            /// <summary>落ちてくるものの色。</summary>
            public Color Impactor;

            /// <summary>
            /// 落ちた瞬間に世界が枯れるか。
            ///
            /// **結果を先に描かない。** はじめ、衝突の場面は最初から枯れた幹と
            /// 暗い空で作っていた。隕石がまだ空にあるのに地上はすでに死んでおり、
            /// 恐竜→隕石→氷期という順につながって見えなかった。
            /// 真にすると、落ちるまでは生きている世界を出し、
            /// 光った瞬間に枯れた幹と暗い空へ入れ替える。
            /// </summary>
            public bool DiesOnImpact;

            /// <summary>落ちた直後の空と地面の色。舞い上がった塵で赤黒い。</summary>
            public Color AfterSkyHigh;
            public Color AfterSkyLow;
            public Color AfterGround;

            /// <summary>
            /// 塵が空をおおったあとの、冷えた空と地面の色。
            ///
            /// 「衝突の冬」と呼ばれる寒冷化があったことは共通しているが、
            /// **何が主に冷やしたのかは文献で分かれている。**
            ///
            /// - Brugger ほか (2017, Geophysical Research Letters)：硫酸エアロゾルが主因。
            ///   世界の年平均気温が**少なくとも26℃**下がり、年平均が氷点下の年が
            ///   **3年ほど**（エアロゾルの滞留時間しだいで3〜16年）続き、
            ///   気候がもどるのに**30年**あまりかかった。**氷冠は広がった。**
            ///   塵は比較的すぐ落ちるため、長い寒冷化への寄与は小さいとする。
            /// - Senel ほか (2023, Nature Geoscience)：細かいケイ酸塩の塵が
            ///   大気中に**15年**とどまり、世界の平均気温を**最大15℃**下げた。
            ///   光合成は衝突後**2年ちかく**止まった。塵の寄与はこれまで
            ///   見積もられてきたより大きいとする。
            ///
            /// **地面を白くしているのは「氷冠が広がった」「年平均が氷点下」に
            /// ならったものである。** 画面に出す降るものは、舞い上がったものが
            /// 降ってくることを読ませるための代表であって、
            /// **どのエアロゾルが冷やしたかを主張するものではない。**
            ///
            /// **これは数年から数十年の出来事であり、
            /// 約259万年前から続く第四紀の氷期とは別の出来事である。**
            ///
            /// **年数も気温も画面では表していない。** 暗く冷たくなった、
            /// という移り変わりだけを見せる。
            /// </summary>
            public Color WinterSkyHigh;
            public Color WinterSkyLow;
            public Color WinterGround;

            /// <summary>
            /// 地面の起伏の高さ（奥行きに対する比）。0で平ら。
            ///
            /// **平らな板だと、地面が床にしか見えない。**
            /// 参考に挙がった風景では、起伏が手前から奥へ続いていて、
            /// それが奥行きと質感の大きな部分を占めていた。
            ///
            /// 置いたものは <see cref="GroundHeight"/> で地面に沿わせる。
            /// 高さを引かずに置くと、丘の斜面で木が宙に浮く。
            /// **実在の地形ではない。**
            /// </summary>
            public float Relief;

            /// <summary>起伏の細かさ。大きいほど細かく波打つ。</summary>
            public float ReliefDetail;

            /// <summary>
            /// 雲の濃さ。0で雲を出さない。
            ///
            /// **空がひと色の帯だけだと、奥行きも時刻も伝わらない。**
            /// 上下のぼかしだけでは、どこまでが空でどこからが遠景か分からず、
            /// 板を1枚立てたように見える。雲の層を重ねると、空に厚みが出る。
            /// **実際の雲の量も種類も表していない。**
            /// </summary>
            public float CloudCover;

            /// <summary>
            /// 太陽を空に出すか。
            ///
            /// 光は当たっているのに光源そのものが画面に無いと、
            /// どちらが明るいのかが読めない。丸と暈を出す。
            /// **実際の視直径ではない。** 見て分かる大きさに広げている。
            /// </summary>
            public bool ShowsSun;

            /// <summary>
            /// 遠くの山なみの数。0なら出さない。
            ///
            /// **平らな地平線だけでは遠さが出ない。** 重なる稜線があると、
            /// どこまでも続いているように見える。奥ほど空の色へ溶かす。
            /// **実在の地形ではない。**
            /// </summary>
            public int Ridges;

            /// <summary>いちばん手前の山なみの高さ（奥行きに対する比）。</summary>
            public float RidgeHeight;

            /// <summary>降ってくるものの数。0なら出さない。</summary>
            public int AshFlakes;

            /// <summary>
            /// 降ってくるものの色。焼けた直後は灰、冷えるにつれて白へ移す。
            /// 灰が降る空から、凍る空への移り変わりを、色だけで見せる。
            /// </summary>
            public Color AshEarly;
            public Color AshLate;

            /// <summary>枯れた幹の色。生きている木の幹とは別に持つ。</summary>
            public Color DeadTrunk;

            /// <summary>
            /// 落ちてくるものを出すか。
            ///
            /// **周期は持たない。1段階を見ているあいだにちょうど一巡させる。**
            /// はじめ14秒や26秒の周期にしていたが、1段階は1倍速で4秒しかない。
            /// 再生していると、落ちきる前に次の時代へ移ってしまい、
            /// **一度も見られなかった。** 周期は段階の長さに合わせる。
            /// </summary>
            public bool ShowsImpact;

            /// <summary>
            /// 打ち上げを出すか。
            ///
            /// **地表からも打ち上げが見えるようにする。** 宇宙の側では衛星も拠点も
            /// 地表から上がる様子を出しているのに、地表へ降りると何も上がらないのでは、
            /// 同じ出来事を見ている感じにならない。
            ///
            /// 落ちてくるものと同じく、周期は段階の長さに合わせる。
            /// **実際の高度も速度も打ち上げにかかる時間も表していない。**
            /// </summary>
            public bool ShowsRocket;

            /// <summary>機体の色と、噴射の色。</summary>
            public Color Rocket;
            public Color Flame;

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

        /// <summary>光っていないときの星の明るさ。</summary>
        private const float TwinkleBase = 0.5f;

        /// <summary>
        /// 光ったときに足す明るさ。**1を超えてよい。**
        /// 加算で描いているので、超えたぶんは白く飛んで強い光になる。
        /// これが無いと、明るくなったり暗くなったりするだけで
        /// 「ピカピカ」には見えない。
        /// </summary>
        private const float TwinkleDepth = 2.0f;

        /// <summary>
        /// 光のとがり。大きいほど、ふだんは控えめでときどき短く強く光る。
        /// 小さいとゆっくり息をしているように見える。
        /// </summary>
        private const float TwinkleSharp = 3.4f;

        /// <summary>
        /// 衝突の場面の進み具合を、どこで区切るか。0で場面の始め、1で終わり。
        ///
        /// **落ちたあとに半分以上を使う。** 落ちてくるところは一瞬でよく、
        /// 塵が広がって暗くなり、冷えて凍るまでを見せるほうが時間を要る。
        /// はじめは落下に0.72を使っていたため、塵が舞うところが一瞬で終わっていた。
        /// </summary>
        private const float Struck = 0.42f;

        /// <summary>閃光の終わり。短く強く光らせる。</summary>
        private const float FlashTo = 0.48f;

        /// <summary>舞い上がった粉塵の柱が立ちのぼる範囲。</summary>
        private const float ColumnFrom = 0.43f;
        private const float ColumnTo = 0.60f;

        /// <summary>塵が空をおおっていく範囲。</summary>
        private const float DustFrom = 0.44f;
        private const float DustTo = 0.64f;

        /// <summary>冷えていく範囲。ここを過ぎると凍ったまま保つ。</summary>
        private const float ChillFrom = 0.62f;
        private const float ChillTo = 0.88f;

        /// <summary>
        /// 動きを減らす設定。真のとき星を揺らさない。
        /// **瞬きは動きである。** 減らすと決めたなら止める。
        /// </summary>
        public bool ReducedMotion { get; set; }

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
        private Material cloudMaterial;
        private Material sunMaterial;
        private Texture2D cloudTexture;
        private Texture2D sunTexture;
        private Transform sunQuad;
        private float skyDistance;
        private Terrain terrain;
        private TerrainData terrainData;
        private Texture2D skyTexture;
        private Material starMaterial;
        private Texture2D starTexture;
        private Transform rocket;
        private Transform exhaust;

        /// <summary>
        /// 足もとに広がる噴煙。
        ///
        /// **離陸そのものを見せるための煙である。** 機体だけだと、
        /// 立っているのか上がっているのかが分かりにくい。
        /// 地面から噴煙が出ていれば、いま地面を離れたことが読める。
        /// </summary>
        private Transform padSmoke;
        private Material rocketMaterial;
        private Material flameMaterial;
        private Material bandMaterial;
        private Material smokeMaterial;

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

        /// <summary>生きているものと枯れたもの。落ちた瞬間に入れ替える。</summary>
        private Transform livingRoot;
        private Transform deadRoot;

        /// <summary>撒くものの置き先。null なら自分の直下。</summary>
        private Transform scatterParent;

        private bool worldIsDead;

        /// <summary>いま世界が枯れているか。点検ツールが読む。</summary>
        public bool WorldIsDead
        {
            get { return worldIsDead; }
        }

        /// <summary>いまの塵の濃さ。点検ツールが読む。</summary>
        public float DustCover
        {
            get { return dustCover; }
        }

        /// <summary>
        /// 空をおおう塵の濃さ。0で澄んでいる、1でふさがっている。
        ///
        /// **塵が地球をおおって日光をさえぎった、という一つの見方に沿っている。**
        /// 細かいケイ酸塩の塵が大気中に15年とどまり、世界の平均気温を最大15℃下げ、
        /// 光合成を2年ちかく止めたとする（Senel ほか 2023, Nature Geoscience）。
        /// 硫酸エアロゾルを主因とする見方（Brugger ほか 2017）もあり、**定説は一つではない。**
        /// ここでは塵でおおわれる見方を採っている。
        /// **濃さも年数も実際の値を表していない。**
        /// </summary>
        private float dustCover;

        /// <summary>降ってくる塵。落ちたあとだけ出す。</summary>
        private Transform ashRoot;
        private Transform[] ash;
        private Vector3[] ashSeat;
        private float[] ashSpeed;
        private Material ashMaterial;
        private float ashTop;

        /// <summary>直前に塗ったときの昼の度合いと朝夕の赤み。塗り直しに使う。</summary>
        private float lastDaylight = 1f;
        private float lastDusk;
        private Color livingSkyHigh;
        private Color livingSkyLow;
        private Color livingGround;

        private static Mesh coneMesh;
        private static Mesh upQuadMesh;
        private Texture2D grainTexture;
        private Texture2D grainNormalTexture;
        private Texture2D backdrop;

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
            BuildRocket(land);

            // 落ちた瞬間に枯れる時代は、生きているものと枯れたものを
            // 別の入れ物へ入れ、片方だけを出す。
            livingSkyHigh = land.SkyHigh;
            livingSkyLow = land.SkyLow;
            livingGround = land.Ground;
            worldIsDead = false;
            if (land.DiesOnImpact)
            {
                livingRoot = Group("Living");
                deadRoot = Group("Dead");
            }

            scatterParent = livingRoot;
            Scatter(land.Conifers, BuildConifer, land.PlantHeight, false);
            Scatter(land.Ferns, BuildFern, land.PlantHeight * 0.30f, false);
            Scatter(land.Broadleaves, BuildBroadleaf, land.PlantHeight * 0.75f, false);
            ScatterCover(land.GroundCover, BuildFern, land.PlantHeight * 0.11f);

            // 岩は落ちても残る。どちらの入れ物にも入れない。
            scatterParent = null;
            Scatter(land.Rocks, BuildRock, Mathf.Max(1.2f, land.PlantHeight * 0.22f), false);
            ScatterCover(land.GroundRubble, BuildRock, Mathf.Max(0.5f, land.PlantHeight * 0.075f));

            scatterParent = deadRoot;
            Scatter(land.DeadTrunks, BuildDeadTrunk, land.PlantHeight * 0.8f, false);

            scatterParent = null;
            PlaceBuildings(land);

            // 生きものは手前寄りに置く。奥へ撒くと小さすぎて形が読めない。
            scatterParent = livingRoot;
            ScatterNear(land.Quadrupeds, BuildQuadruped,
                land.CreatureHeight > 0f ? land.CreatureHeight : land.PlantHeight * 0.85f, 0.34f);
            ScatterNear(land.Bipeds, BuildBiped, land.PlantHeight * 0.40f, 0.26f);
            scatterParent = null;

            BuildAsh(land);

            if (deadRoot != null)
            {
                deadRoot.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 降ってくる塵。
        ///
        /// **空が暗くなるだけでは、何におおわれたのかが分からない。**
        /// 舞い上がったものが降ってくるところを出して、
        /// 暗いのは塵のせいだと読めるようにする。
        /// **量も大きさも降る速さも、実際のものを表していない。**
        /// </summary>
        private void BuildAsh(Landscape land)
        {
            if (land.AshFlakes <= 0 || !land.DiesOnImpact)
            {
                return;
            }

            ashMaterial = StandardMaterials.CreateGlow();
            ashMaterial.hideFlags = createdFlags;
            ashMaterial.color = Color.white;
            ashMaterial.SetColor("_EmissionColor", Color.black);

            ashRoot = Group("Ash");
            ashTop = land.FarZ * 0.30f;

            ash = new Transform[land.AshFlakes];
            ashSeat = new Vector3[land.AshFlakes];
            ashSpeed = new float[land.AshFlakes];

            // **細かくする。** 大きくすると降ってくる玉にしか見えない。
            var size = Mathf.Max(0.12f, land.PlantHeight * 0.016f);
            for (var i = 0; i < ash.Length; i++)
            {
                var flake = PrimitiveMeshes.Create(PrimitiveType.Sphere, "Ash", createdFlags);
                flake.transform.SetParent(ashRoot, false);
                flake.transform.localScale = Vector3.one * (size * (0.6f + (float)random.NextDouble()));
                flake.GetComponent<Renderer>().sharedMaterial = ashMaterial;

                ash[i] = flake.transform;
                // カメラのすぐ前には置かない。近いものだけが大きく目立ってしまう。
                ashSeat[i] = new Vector3(
                    (float)(random.NextDouble() * 2.0 - 1.0) * land.HalfWidth * 0.55f,
                    (float)random.NextDouble(),
                    Mathf.Lerp(land.FarZ * 0.06f, land.FarZ * 0.55f, (float)random.NextDouble()));
                ashSpeed[i] = 0.5f + (float)random.NextDouble() * 0.9f;
            }

            ashRoot.gameObject.SetActive(false);
        }

        /// <summary>塵を降らせる。落ちたあとだけ動かす。</summary>
        private void MoveAsh(float phase, float chill)
        {
            if (ash == null || ashRoot == null)
            {
                return;
            }

            ashRoot.gameObject.SetActive(worldIsDead);
            if (!worldIsDead)
            {
                return;
            }

            // 塵は一度に満ちない。濃くなるにつれて数を増やす。
            var showing = Mathf.RoundToInt(ash.Length * (0.18f + 0.82f * dustCover));
            for (var i = 0; i < ash.Length; i++)
            {
                if (i >= showing)
                {
                    ash[i].gameObject.SetActive(false);
                    continue;
                }

                ash[i].gameObject.SetActive(true);

                var fall = phase * 3.2f * ashSpeed[i] + ashSeat[i].y;
                fall -= Mathf.Floor(fall);
                ash[i].localPosition = new Vector3(
                    ashSeat[i].x + Mathf.Sin((fall + ashSeat[i].y) * 6.283f) * ashTop * 0.03f,
                    ashTop * (1f - fall),
                    ashSeat[i].z);
            }

            // 灰が降る空から、凍る空へ。色だけで移り変わりを見せる。
            var early = current.AshEarly.maxColorComponent > 0.001f
                ? current.AshEarly
                : Color.white;
            var late = current.AshLate.maxColorComponent > 0.001f
                ? current.AshLate
                : Color.white;
            ashMaterial.SetColor("_EmissionColor",
                Color.Lerp(early, late, chill) * (0.4f + chill * 0.6f));
        }

        /// <summary>入れ物をひとつ作る。</summary>
        private Transform Group(string name)
        {
            var group = new GameObject(name);
            group.hideFlags = createdFlags;
            group.transform.SetParent(transform, false);
            return group.transform;
        }

        /// <summary>撒いたものの置き先。</summary>
        private Transform ScatterRoot
        {
            get { return scatterParent != null ? scatterParent : transform; }
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

            // 写真は Resources から借りているだけなので壊さない。
            // 壊すと、次に読み込んだときに真っ白になる。
            backdrop = null;

            DestroyImmediate(cloudMaterial);
            DestroyImmediate(sunMaterial);
            DestroyImmediate(cloudTexture);
            DestroyImmediate(sunTexture);
            cloudMaterial = null;
            sunMaterial = null;
            cloudTexture = null;
            sunTexture = null;
            sunQuad = null;

            DestroyImmediate(rocketMaterial);
            DestroyImmediate(flameMaterial);
            DestroyImmediate(bandMaterial);
            DestroyImmediate(smokeMaterial);
            bandMaterial = null;
            smokeMaterial = null;
            rocketMaterial = null;
            flameMaterial = null;
            rocket = null;
            exhaust = null;
            padSmoke = null;

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

            livingRoot = null;
            deadRoot = null;
            scatterParent = null;
            worldIsDead = false;

            DestroyImmediate(terrainData);
            DestroyImmediate(grainTexture);
            DestroyImmediate(grainNormalTexture);
            terrainData = null;
            terrain = null;
            grainTexture = null;
            grainNormalTexture = null;

            DestroyImmediate(ashMaterial);
            ashMaterial = null;
            ashRoot = null;
            ash = null;
            ashSeat = null;
            ashSpeed = null;
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

            // **写真があればそれを貼る。** 縦の色の帯は作らない。
            backdrop = string.IsNullOrEmpty(land.Backdrop)
                ? null
                : Resources.Load<Texture2D>("Sky/" + land.Backdrop);

            skyMaterial = StandardMaterials.CreateOpaque(true);
            skyMaterial.hideFlags = createdFlags;
            skyMaterial.color = Color.black;
            skyMaterial.SetTexture("_EmissionMap", backdrop != null ? (Texture)backdrop : skyTexture);
            skyMaterial.SetColor("_EmissionColor", Color.white);

            // **塵でふさぐときの行き先を、次の絵として持たせておく。**
            // 写真に色を掛けるだけでは、青空の青が残ってしまう。
            // 塵が日を遮った空は色を失い、一枚のふたになる。
            // 描いた空（縦の帯）を次の絵に置き、塵の濃さで混ぜれば、
            // 遠くの丘もろとも murk に沈む。これが見通しの利かなさになる。
            if (backdrop != null)
            {
                skyMaterial.SetTexture("_EmissionMapNext", skyTexture);
            }

            skyMaterial.SetFloat("_Blend", 0f);

            // **空を遠くへ置く。** 山なみを空の手前へ並べる余地がいる。
            // カメラの遠い側の切り取り面は FarZ の3倍まで広げてある。
            var distance = land.FarZ * 2.2f;
            skyDistance = distance;

            var sky = PrimitiveMeshes.Create(PrimitiveType.Quad, "Sky", createdFlags);
            sky.transform.SetParent(transform, false);
            sky.transform.localPosition = new Vector3(0f, distance * 0.36f, distance);
            sky.transform.localScale = new Vector3(distance * 4f, distance * 1.9f, 1f);
            sky.GetComponent<Renderer>().sharedMaterial = skyMaterial;

            // **影の届く距離を、見えている野の広さに合わせる。**
            // 既定は数十メートルしかなく、手前の数本にしか影が出ない。
            // 遠くまで伸ばすと粗くなるので、野の奥行きの半分までにする。
            // **品質設定で影そのものが切られていることがある。**
            // 光の側で影を有効にしても、ここが切れていると何も出ない。
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowProjection = ShadowProjection.StableFit;
            QualitySettings.shadowDistance = Mathf.Max(60f, land.FarZ * 0.55f);
            QualitySettings.shadowCascades = 2;

            // **写真には雲も太陽も稜線も写っている。** 重ねない。
            // 重ねると、雲が二層になり、太陽が2つ出て、稜線が写真の
            // 地平線を横切る。どれも「貼り物」であることを見せてしまう。
            if (backdrop == null)
            {
                BuildClouds(land, distance * 0.955f);
                BuildSun(land, distance * 0.975f);
            }

            BuildStars(distance * 0.99f);

            if (backdrop == null)
            {
                BuildRidges(land);
            }
        }

        /// <summary>
        /// 雲。
        ///
        /// **絵は1枚作って使い回し、時刻では色だけを変える。**
        /// 毎こま描き直すと、512x256の点を打ち直すことになり重い。
        /// 形は変わらなくてよい。変わるのは明るさと色だからである。
        ///
        /// 加算で重ねる。日の当たった雲は空より明るいので、足すだけで形が出る。
        /// 暗い雲は出せないが、そのぶん夜に光ってしまうことも無い。
        /// **実際の雲の量も種類も動きも表していない。**
        /// </summary>
        private void BuildClouds(Landscape land, float distance)
        {
            if (land.CloudCover <= 0.001f)
            {
                return;
            }

            const int width = 512;
            const int height = 256;
            cloudTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cloudTexture.hideFlags = createdFlags;
            cloudTexture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[width * height];
            var seed = new System.Random(land.Seed * 31 + 7);

            // 粗さの違う波を重ねる。1つだけだと縞にしか見えない。
            // 横に伸ばす。雲は縦より横に広い。
            const int layers = 5;
            var offsets = new float[layers * 2];
            for (var i = 0; i < offsets.Length; i++)
            {
                offsets[i] = (float)seed.NextDouble() * 100f;
            }

            for (var y = 0; y < height; y++)
            {
                var v = y / (float)(height - 1);

                // 地平線の際と天頂は薄く。まんなかの帯をいちばん濃くする。
                var band = Mathf.Sin(Mathf.Clamp01((v - 0.12f) / 0.88f) * Mathf.PI);
                band = Mathf.Pow(Mathf.Max(0f, band), 0.7f);

                for (var x = 0; x < width; x++)
                {
                    var u = x / (float)(width - 1);
                    var sum = 0f;
                    var amplitude = 1f;
                    var scale = 3f;
                    for (var i = 0; i < layers; i++)
                    {
                        sum += amplitude * Mathf.PerlinNoise(
                            u * scale * 2.6f + offsets[i * 2],
                            v * scale * 7f + offsets[i * 2 + 1]);
                        amplitude *= 0.52f;
                        scale *= 2.1f;
                    }

                    sum /= 1.95f;

                    // 下を切り上げてすき間を作る。全面にかけると靄になる。
                    var cloud = Mathf.Clamp01((sum - 0.46f) / 0.34f) * band;
                    cloud = cloud * cloud * (3f - 2f * cloud);

                    var level = (byte)Mathf.Clamp(cloud * 255f, 0f, 255f);
                    pixels[y * width + x] = new Color32(level, level, level, 255);
                }
            }

            cloudTexture.SetPixels32(pixels);
            cloudTexture.Apply();

            cloudMaterial = StandardMaterials.CreateGlow();
            cloudMaterial.hideFlags = createdFlags;
            cloudMaterial.color = Color.white;
            cloudMaterial.SetTexture("_EmissionMap", cloudTexture);
            cloudMaterial.SetColor("_EmissionColor", Color.black);

            var quad = PrimitiveMeshes.Create(PrimitiveType.Quad, "Clouds", createdFlags);
            quad.transform.SetParent(transform, false);
            quad.transform.localPosition = new Vector3(0f, distance * 0.36f, distance);
            quad.transform.localScale = new Vector3(distance * 4f, distance * 1.9f, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
        }

        /// <summary>
        /// 太陽。丸と、そのまわりの暈。
        ///
        /// 光は当たっているのに光源そのものが画面に無いと、
        /// どちらが明るいのかが読めない。
        /// **実際の視直径ではない。** 見て分かる大きさに広げている。
        /// 空のどこに出るかも、光の向きをそのまま写したものではない。
        /// </summary>
        private void BuildSun(Landscape land, float distance)
        {
            if (!land.ShowsSun)
            {
                return;
            }

            const int size = 128;
            sunTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            sunTexture.hideFlags = createdFlags;
            sunTexture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);

                    // 丸そのものは小さく、暈は広く薄く。
                    // 暈が無いと貼り付けた丸に見える。
                    var disc = Mathf.Clamp01((0.14f - d) / 0.05f);
                    var halo = Mathf.Pow(Mathf.Clamp01(1f - d), 3.2f) * 0.55f;
                    var level = Mathf.Clamp01(disc + halo);

                    var b = (byte)Mathf.Clamp(level * 255f, 0f, 255f);
                    pixels[y * size + x] = new Color32(b, b, b, 255);
                }
            }

            sunTexture.SetPixels32(pixels);
            sunTexture.Apply();

            sunMaterial = StandardMaterials.CreateGlow();
            sunMaterial.hideFlags = createdFlags;
            sunMaterial.color = Color.white;
            sunMaterial.SetTexture("_EmissionMap", sunTexture);
            sunMaterial.SetColor("_EmissionColor", Color.black);

            var quad = PrimitiveMeshes.Create(PrimitiveType.Quad, "Sun", createdFlags);
            quad.transform.SetParent(transform, false);
            quad.transform.localScale = Vector3.one * (distance * 1.05f);
            quad.GetComponent<Renderer>().sharedMaterial = sunMaterial;
            sunQuad = quad.transform;
            sunQuad.localPosition = new Vector3(-distance * 0.26f, distance * 0.25f, distance);
        }

        /// <summary>
        /// 遠くの山なみ。
        ///
        /// **平らな地平線だけでは遠さが出ない。** 重なる稜線があると、
        /// どこまでも続いているように見える。奥ほど空の色へ溶かす。
        /// これは大気で遠くのものが淡くなることにならった置き換えである。
        /// **実在の地形ではない。**
        /// </summary>
        private void BuildRidges(Landscape land)
        {
            if (land.Ridges <= 0)
            {
                return;
            }

            var seed = new System.Random(land.Seed * 17 + 3);
            for (var layer = 0; layer < land.Ridges; layer++)
            {
                var span = land.Ridges > 1 ? layer / (float)(land.Ridges - 1) : 0f;
                var depth = land.FarZ * Mathf.Lerp(1.35f, 1.95f, span);
                var height = depth * land.RidgeHeight * Mathf.Lerp(1f, 0.62f, span);

                // 奥ほど空の色へ寄せる。手前の稜線ほど濃く残る。
                // **寄せすぎない。** 空と同じ色まで薄めると、山ではなく
                // ただの霞に見えて、稜線の形が読めなくなる。
                var tint = Color.Lerp(land.Rock, land.SkyLow, 0.34f + 0.44f * span);

                var material = StandardMaterials.CreateOpaque(true);
                material.hideFlags = createdFlags;
                material.color = Color.black;
                material.SetColor("_EmissionColor", tint);
                material.SetFloat("_Blend", 0f);
                materials["Ridge/" + layer] = material;

                var mesh = BuildRidgeMesh(depth, height, seed);
                var host = new GameObject("Ridge" + layer, typeof(MeshFilter), typeof(MeshRenderer));
                host.hideFlags = createdFlags;
                host.transform.SetParent(transform, false);
                host.GetComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = host.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        /// <summary>稜線をひと続きの帯にする。下は地平線より下まで伸ばす。</summary>
        private Mesh BuildRidgeMesh(float depth, float height, System.Random seed)
        {
            const int steps = 96;
            var half = depth * 2.2f;
            var baseY = -depth * 0.2f;

            var vertices = new Vector3[(steps + 1) * 2];
            var triangles = new int[steps * 6];

            var phase = new float[4];
            for (var i = 0; i < phase.Length; i++)
            {
                phase[i] = (float)seed.NextDouble() * 100f;
            }

            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var x = Mathf.Lerp(-half, half, t);

                // 粗い波に細かい波を足す。1つだけだと規則正しい山になる。
                var h = Mathf.PerlinNoise(t * 2.6f + phase[0], phase[1]) * 0.7f
                        + Mathf.PerlinNoise(t * 7.4f + phase[2], phase[3]) * 0.3f;
                h = Mathf.Pow(Mathf.Clamp01(h), 1.6f);

                vertices[i * 2] = new Vector3(x, baseY, depth);
                vertices[i * 2 + 1] = new Vector3(x, height * h, depth);
            }

            for (var i = 0; i < steps; i++)
            {
                var v = i * 2;
                var tri = i * 6;
                triangles[tri + 0] = v;
                triangles[tri + 1] = v + 1;
                triangles[tri + 2] = v + 3;
                triangles[tri + 3] = v;
                triangles[tri + 4] = v + 3;
                triangles[tri + 5] = v + 2;
            }

            var mesh = new Mesh();
            mesh.hideFlags = createdFlags;
            mesh.name = "Ridge";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
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
            if (land.ImpactorSize <= 0f || !land.ShowsImpact)
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

        /// <summary>
        /// 打ち上げる場所。**街より手前に置く。**
        /// 街の中に混ぜると、建物に隠れて上がる様子が見えない。
        /// </summary>
        private Vector3 LaunchPad(Landscape land)
        {
            // **街の中へ置かない。画面の真ん中に置く。**
            // 街並みにまぎれていたとき、機体は建物と同じ白さで同じ高さに立ち、
            // **地面から離れるところが見えなかった。**
            // 建物がまだ始まらない、いちばん手前の草地へ置く。
            // そこなら足もとが空いていて、離陸そのものが見える。
            // 横は0にする。カメラは x=0 にあるので、画面の真ん中に立つ。
            return new Vector3(0f, GroundHeight(0f, land.NearZ), land.NearZ);
        }

        /// <summary>機体の背の高さ。近くに置くので、建物より大きく取る。</summary>
        private static float RocketScale(Landscape land)
        {
            // 手前へ出したぶん、建物より高く取ってよい。
            // 遠くに置いて小さくすると、街並みの一部にしか見えない。
            return Mathf.Max(14f, land.BuildingHeight * 1.35f);
        }

        /// <summary>
        /// 上がっていく機体と、その噴射。
        ///
        /// 形は胴と先端と炎だけで、段や翼を作らない。
        /// **特定のロケットを表していない。** 上がっていく、ということだけを見せる。
        /// </summary>
        private void BuildRocket(Landscape land)
        {
            if (!land.ShowsRocket)
            {
                return;
            }

            rocketMaterial = StandardMaterials.CreateOpaque(false);
            rocketMaterial.hideFlags = createdFlags;
            rocketMaterial.color = land.Rocket;
            rocketMaterial.SetFloat("_Glossiness", 0f);
            rocketMaterial.SetFloat("_Metallic", 0f);

            flameMaterial = StandardMaterials.CreateGlow();
            flameMaterial.hideFlags = createdFlags;
            flameMaterial.color = Color.white;
            flameMaterial.SetColor("_EmissionColor", Color.black);

            bandMaterial = StandardMaterials.CreateOpaque(false);
            bandMaterial.hideFlags = createdFlags;
            bandMaterial.color = land.Rocket * 0.28f;
            bandMaterial.SetFloat("_Glossiness", 0f);
            bandMaterial.SetFloat("_Metallic", 0f);

            smokeMaterial = StandardMaterials.CreateGlow();
            smokeMaterial.hideFlags = createdFlags;
            smokeMaterial.color = Color.white;
            smokeMaterial.SetColor("_EmissionColor", Color.black);

            var scale = RocketScale(land);

            var root = new GameObject("Rocket");
            root.hideFlags = createdFlags;
            root.transform.SetParent(transform, false);
            rocket = root.transform;

            var body = PrimitiveMeshes.Create(PrimitiveType.Cylinder, "Body", createdFlags);
            body.transform.SetParent(rocket, false);
            body.transform.localPosition = new Vector3(0f, scale * 0.5f, 0f);
            body.transform.localScale = new Vector3(scale * 0.17f, scale * 0.5f, scale * 0.17f);
            body.GetComponent<Renderer>().sharedMaterial = rocketMaterial;

            var nose = new GameObject("Nose", typeof(MeshFilter), typeof(MeshRenderer));
            nose.hideFlags = createdFlags;
            nose.transform.SetParent(rocket, false);
            nose.transform.localPosition = new Vector3(0f, scale, 0f);
            // 先端は胴と同じ太さにする。太いとキノコに見える。
            nose.transform.localScale = new Vector3(scale * 0.17f, scale * 0.26f, scale * 0.17f);
            nose.GetComponent<MeshFilter>().sharedMesh = Cone();
            nose.GetComponent<MeshRenderer>().sharedMaterial = rocketMaterial;

            // 濃い帯を1本入れる。まっ白のままだと、白い建物の並びに溶けて消える。
            var band = PrimitiveMeshes.Create(PrimitiveType.Cylinder, "Band", createdFlags);
            band.transform.SetParent(rocket, false);
            band.transform.localPosition = new Vector3(0f, scale * 0.22f, 0f);
            band.transform.localScale = new Vector3(scale * 0.178f, scale * 0.055f, scale * 0.178f);
            band.GetComponent<Renderer>().sharedMaterial = bandMaterial;

            var fire = PrimitiveMeshes.Create(PrimitiveType.Sphere, "Flame", createdFlags);
            fire.transform.SetParent(transform, false);
            fire.GetComponent<Renderer>().sharedMaterial = flameMaterial;
            exhaust = fire.transform;

            // 足もとの噴煙。地面に沿って広がるので平たくする。
            var smoke = PrimitiveMeshes.Create(PrimitiveType.Sphere, "PadSmoke", createdFlags);
            smoke.transform.SetParent(transform, false);
            smoke.GetComponent<Renderer>().sharedMaterial = smokeMaterial;
            padSmoke = smoke.transform;
        }

        /// <summary>
        /// 打ち上げを進める。0で発射の直前、1で次の打ち上げの始め。
        ///
        /// 0.00〜0.10 は台の上で待ち、0.10〜0.80 で加速しながら上がり、そこから消える。
        /// **高度も速度も、実際のものを表していない。**
        /// </summary>
        public void SetRocket(float phase)
        {
            if (rocket == null || exhaust == null)
            {
                return;
            }

            phase -= Mathf.Floor(phase);

            var land = current;
            var pad = LaunchPad(land);
            var flying = phase < 0.80f;

            rocket.gameObject.SetActive(flying);
            exhaust.gameObject.SetActive(flying);
            if (!flying)
            {
                if (padSmoke != null)
                {
                    padSmoke.gameObject.SetActive(false);
                }

                return;
            }

            var scale = RocketScale(land);

            // 待っているあいだは台の上。上がり始めてからは、加速して高くなる。
            //
            // **上がる高さは機体の丈で決める。** 野の奥行きで決めていたときは、
            // 奥行き900mの街で1000m以上まで一気に飛び、ひとこまで消えていた。
            var climb = Mathf.Max(0f, (phase - 0.18f) / 0.62f);
            var height = scale * 5f * climb * climb;

            rocket.localPosition = pad + new Vector3(0f, height, 0f);

            // 噴射は上がり始めてから出す。待っているあいだは出さない。
            var burning = phase >= 0.18f;
            // 噴射は機体より細く短く。大きくすると白い楕円にしか見えない。
            var flame = burning ? scale * (0.22f + Mathf.Sin(phase * 120f) * 0.04f) : 0f;
            exhaust.localScale = new Vector3(flame * 0.55f, flame, flame * 0.55f);
            exhaust.localPosition = rocket.localPosition + new Vector3(0f, -flame * 0.75f, 0f);
            flameMaterial.SetColor("_EmissionColor",
                burning ? land.Flame * (1.35f - climb * 0.7f) : Color.black);

            if (padSmoke == null)
            {
                return;
            }

            // 足もとの噴煙。上がり始めに地面へ広がり、離れるにつれて薄れる。
            // これが無いと、地面から離れた瞬間が読めない。
            var puff = Mathf.Clamp01((phase - 0.18f) / 0.34f);
            padSmoke.gameObject.SetActive(burning && puff < 1f);
            if (burning && puff < 1f)
            {
                var spread = scale * Mathf.Lerp(0.30f, 1.5f, puff);
                padSmoke.localPosition = pad + new Vector3(0f, spread * 0.16f, 0f);
                padSmoke.localScale = new Vector3(spread, spread * 0.42f, spread);
                smokeMaterial.SetColor("_EmissionColor", Color.white * (1f - puff) * 0.8f);
            }
        }

        /// <summary>
        /// 生きている世界と枯れた世界を入れ替える。
        ///
        /// 木と生きものを出し入れし、空と地面の色を差し替える。
        /// 岩は残す。落ちても岩は無くならない。
        /// </summary>
        private void SetWorldDead(bool dead)
        {
            if (!current.DiesOnImpact || dead == worldIsDead)
            {
                return;
            }

            worldIsDead = dead;

            if (livingRoot != null)
            {
                livingRoot.gameObject.SetActive(!dead);
            }

            if (deadRoot != null)
            {
                deadRoot.gameObject.SetActive(dead);
            }

            current.SkyHigh = dead ? current.AfterSkyHigh : livingSkyHigh;
            current.SkyLow = dead ? current.AfterSkyLow : livingSkyLow;
            current.Ground = dead ? current.AfterGround : livingGround;

            RecolorGround();
        }

        /// <summary>
        /// 星の光り方を材質へ渡す。
        /// 動きを減らす設定のときは光らせず、そのぶん地の明るさを上げて
        /// 星が暗くならないようにする。
        /// </summary>
        private void ApplyTwinkle()
        {
            if (starMaterial == null)
            {
                return;
            }

            starMaterial.SetFloat("_TwinkleBase", ReducedMotion ? 1f : TwinkleBase);
            starMaterial.SetFloat("_TwinkleDepth", ReducedMotion ? 0f : TwinkleDepth);
        }

        /// <summary>
        /// 太陽を空へ置く。
        ///
        /// **光の向きをそのまま写してはいない。** 写すと、真昼には画面の外の
        /// 真上へ行ってしまい、いちばん明るい時刻にいちばん見えなくなる。
        /// 高さを縮めて、昇って沈むことだけが読めるようにしている。
        /// 地平線より下へ行ったら引っ込める。
        /// </summary>
        /// <summary>
        /// 空のどこに太陽の丸を出すか。奥の面までの割合で返す。
        /// 画面の横の広がりは片側で40度ほどなので、その内側に収める。
        /// 丸が画面から出ると、光がどこから来ているのかが読めなくなる。
        /// </summary>
        private static Vector3 SunSpot(float elevation)
        {
            var lift = Mathf.Clamp01(elevation);
            return new Vector3(-0.30f, Mathf.Lerp(-0.02f, 0.72f, lift), 1f);
        }

        /// <summary>
        /// 照らす向き（太陽へ向かう向き）。
        ///
        /// **空に描く丸とは、わざと合わせていない。**
        /// 丸は画面のなかに見えていないと、光の源が読めない。だから正面寄りに置く。
        /// ところが正面から当てると、影は物の真後ろへ落ちて物自身に隠れる。
        /// 実際これまで影は1つも見えていなかった。合わせたうえで横へ振ると、
        /// 今度は逆光になって、生きものも木もすべて黒い影絵になった。
        ///
        /// そこで光だけを大きく振る。**左から当たっている**という読みは
        /// 丸と揃うので、食い違いとしては現れない。舞台で、見えている灯りとは
        /// 別に横から当てるのと同じ考えである。
        ///
        /// **カメラの後ろ寄り、かつ大きく横へ。**
        /// 正面から当てると影は手前へ伸びてよく見えるが、こちらを向いた面が
        /// すべて陰になり、生きものも木も黒い影絵になる。実際そうなった。
        /// 後ろから当てれば面は明るいままで、横へ大きく振ってあるぶん、
        /// 影は物の真後ろではなく真横へずれて出る。物に隠れない。
        ///
        /// **低くしすぎない。** 影の長さは高さの 1/tan 倍なので、
        /// 地平線まで下ろすと影が影の届く距離を越え、そこでぶつりと切れる。
        /// 切れた縁は地面に引いた線に見え、影よりも目立ってしまう。
        /// 15度を下限にすると、影は物の高さの3.7倍でとまる。
        /// </summary>
        private static Vector3 SunLight(float elevation)
        {
            // 画面の奥を0度、真横を90度として、左へどれだけ振るか。
            // 90度を越えているぶんがカメラの後ろ側にあたる。
            const float Azimuth = -118f;

            var lift = Mathf.Clamp01(elevation);
            var high = Mathf.Lerp(15f, 30f, lift) * Mathf.Deg2Rad;
            var yaw = Azimuth * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Sin(yaw) * Mathf.Cos(high),
                Mathf.Sin(high),
                Mathf.Cos(yaw) * Mathf.Cos(high));
        }

        private void PlaceSun(float elevation, float daylight, float dusk)
        {
            if (sunQuad == null || sunMaterial == null)
            {
                return;
            }

            var up = elevation > -0.06f;
            sunQuad.gameObject.SetActive(up);
            if (!up)
            {
                return;
            }

            var distance = skyDistance * 0.975f;
            sunQuad.localPosition = SunSpot(elevation) * distance;

            // 地平線の近くは赤く、高いところは白い。
            // 塵がかかっていれば、どちらでも弱める。
            var warm = Color.Lerp(new Color(1f, 0.52f, 0.26f), Color.white,
                Mathf.Clamp01(elevation * 2.2f));
            var strength = Mathf.Lerp(0.35f, 1.5f, daylight) * (1f - dustCover * 0.75f);
            sunMaterial.SetColor("_EmissionColor", warm * strength);
        }

        /// <summary>
        /// 雲の色を時刻で決める。形は作ったときのまま。
        /// 朝夕は赤く、昼は白く、夜はほとんど出さない。
        /// </summary>
        private void TintClouds(float daylight, float dusk)
        {
            if (cloudMaterial == null)
            {
                return;
            }

            var lit = Color.Lerp(new Color(0.20f, 0.24f, 0.33f), Color.white, daylight);
            lit = Color.Lerp(lit, new Color(1f, 0.64f, 0.40f), dusk * 0.7f);

            // 塵がおおっているあいだは、雲の形も沈める。
            var strength = current.CloudCover * Mathf.Lerp(0.22f, 1f, daylight)
                           * (1f - dustCover * 0.8f);
            cloudMaterial.SetColor("_EmissionColor", lit * strength);
        }

        /// <summary>
        /// その場所の地面の高さ。
        ///
        /// **置くものはすべてここを通す。** 地面を波打たせたのに置き場所を
        /// 0のままにすると、丘の斜面で木も生きものも宙に浮く。
        /// 地面そのものもこの値で作るので、形は必ず一致する。
        ///
        /// 粗い波に細かい波を足している。1つだけだと規則正しいうねりになる。
        /// **実在の地形ではない。**
        /// </summary>
        public float GroundHeight(float x, float z)
        {
            if (current.Relief <= 0.0001f)
            {
                return 0f;
            }

            var scale = Mathf.Max(1f, current.FarZ);
            var detail = current.ReliefDetail > 0f ? current.ReliefDetail : 1f;

            var u = x / scale;
            var w = z / scale;

            // 種で位置をずらす。時代ごとに違う地形にする。
            var shift = (current.Seed % 97) * 0.37f;

            // **格子を曲げてから引く。**
            // そのまま引くと、Perlin の格子が縦横に並んでいるのが見えて、
            // 規則正しい碁盤の目のうねりになる。引く場所そのものを
            // 別の波でずらすと、稜線がうねって自然な曲がりになる。
            var warpU = Mathf.PerlinNoise(u * 1.1f + shift * 5f, w * 1.1f + shift * 5f) - 0.5f;
            var warpW = Mathf.PerlinNoise(u * 1.1f + shift * 7f, w * 1.1f + shift * 7f) - 0.5f;
            u += warpU * 0.45f;
            w += warpW * 0.45f;

            // **尾根を立てる。** ただ足し合わせると丸い瘤の連なりにしかならない。
            // 0.5からの隔たりを取って裏返すと、峰が細く谷が広い形になり、
            // 山なみらしい稜線が出る。
            var h = 0f;
            var amplitude = 1f;
            var total = 0f;
            var frequency = 1.6f * detail;
            for (var octave = 0; octave < 4; octave++)
            {
                var n = Mathf.PerlinNoise(
                    u * frequency + shift * (octave + 1f),
                    w * frequency + shift * (octave + 1f));

                var ridged = 1f - Mathf.Abs(n * 2f - 1f);
                ridged *= ridged;

                // 粗い層は尾根を強く、細かい層はそのまま足して肌を作る。
                h += amplitude * Mathf.Lerp(n, ridged, octave < 2 ? 0.7f : 0.25f);
                total += amplitude;
                amplitude *= 0.48f;
                frequency *= 2.3f;
            }

            h /= Mathf.Max(0.0001f, total);

            // **手前は平らに寄せる。** 目のすぐ前が盛り上がっていると、
            // 立っている場所が分からず、画面の下半分が土手でふさがる。
            var near = Mathf.Clamp01(z / (current.FarZ * 0.35f));
            h = Mathf.Lerp(0.5f, h, near * near);

            // 真ん中あたりを基準にして、上下へ振らせる。
            return (h - 0.5f) * current.Relief * scale;
        }

        /// <summary>今の空の色と昼の度合いで、空の帯を塗る。</summary>
        private void PaintSky()
        {
            // **写真は塗り替えない。掛ける。**
            // 写真は1つの時刻で撮られている。夜にするには、
            // 絵の上から暗く青い色を掛けるほかない。
            // 朝夕は暖かい色を掛ける。形は変わらないので、
            // 「同じ景色の時刻が移った」ようには見えるが、
            // **影の向きは動かない。** そこは表せていない。
            if (backdrop != null)
            {
                TintBackdrop();
            }

            if (skyTexture == null)
            {
                return;
            }

            var high = Color.Lerp(NightHigh, current.SkyHigh, lastDaylight);
            var low = Color.Lerp(NightLow, current.SkyLow, lastDaylight);
            low = Color.Lerp(low, DuskLow, lastDusk * 0.85f);

            // **塵が濃いほど、空の上下の差を消す。**
            // 晴れた空は上が濃く下が淡い。おおわれた空はその差が無くなり、
            // 一枚のふたのように見える。これで「ふさがれた」ことを伝える。
            if (dustCover > 0f)
            {
                // 濃い側（上）へ寄せてから平らにする。真ん中で平らにすると、
                // 明るい下の色に引かれて、ふさがっているのに明るい空になった。
                var flat = Color.Lerp(high, low, 0.3f) * (1f - dustCover * 0.28f);
                high = Color.Lerp(high, flat, dustCover * 0.95f);
                low = Color.Lerp(low, flat, dustCover * 0.95f);
            }

            var height = skyTexture.height;
            for (var y = 0; y < height; y++)
            {
                var t = y / (float)(height - 1);
                skyTexture.SetPixel(0, y, Color.Lerp(low, high, Mathf.Pow(t, 0.6f)));
            }

            skyTexture.Apply();
        }

        /// <summary>遠景の写真に、時刻の色を掛ける。</summary>
        private void TintBackdrop()
        {
            if (skyMaterial == null)
            {
                return;
            }

            // 夜は暗く青く。昼はそのまま。
            var tint = Color.Lerp(new Color(0.10f, 0.13f, 0.24f), Color.white, lastDaylight);

            // 朝夕は暖かく。掛け算なので、青を落として赤を残す形になる。
            tint = Color.Lerp(tint, new Color(1f, 0.66f, 0.42f), lastDusk * 0.55f);

            skyMaterial.SetColor("_EmissionColor", tint);

            // **塵がおおえば、写真ごと描いた空へ移す。**
            // 色を掛けるだけでは青空の青が残る。塵が日を遮った空は
            // 色を失うので、写真から縦の帯（塵で平らに塗ってある）へ
            // 混ぜていく。遠くの丘も一緒に沈み、見通しが利かなくなる。
            skyMaterial.SetFloat("_Blend", dustCover);
        }

        /// <summary>地面の帯を今の色で塗り直す。遠いぶんは空の色へ寄せる。</summary>
        private void RecolorGround()
        {
            for (var i = 0; i < DepthSteps; i++)
            {
                Material material;
                if (!materials.TryGetValue("Ground/" + i, out material) || material == null)
                {
                    continue;
                }

                var t = DepthSteps > 1 ? i / (float)(DepthSteps - 1) : 0f;
                material.color = Color.Lerp(current.Ground, current.SkyLow, t * HazeStrength);


            }
        }

        /// <summary>ぶつかる場所。地平線の少し手前へ置く。</summary>
        private Vector3 ImpactPoint(Landscape land)
        {
            var x = land.HalfWidth * 0.5f;
            var z = land.FarZ * 0.9f;
            return new Vector3(x, GroundHeight(x, z), z);
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

            // **1段階につき1回だけ落ちる。くり返さない。**
            // 周期でぐるぐる回していたころは、枯れた世界がまた生き返って
            // もう一度落ちてきた。起きたことが取り消されたように見える。
            // 呼ぶ側が0から1へ一度だけ進めるので、ここでは端で止めるだけにする。
            phase = Mathf.Clamp01(phase);

            // **光った瞬間に世界を入れ替える。**
            // 落ちる前は生きている世界、落ちたあとは枯れた世界。
            // 順番を逆にすると、原因より先に結果が画面に出てしまう。
            SetWorldDead(phase >= Struck);

            // **落ちたあとは5つの場面をこの順でたどる。**
            // 1 生きた森 → 2 落ちてくる → 3 閃光 → 4 塵が空をおおって暗くなる → 5 凍る
            // 実際の年数も温度も濃さも表していない。順序だけを見せる。
            //
            // **落ちたあとに半分以上を使う。** 落ちてくるところは一瞬でよく、
            // 塵が広がって暗くなり、冷えて凍るまでを見せるほうが時間を要る。
            dustCover = worldIsDead ? Mathf.Clamp01((phase - DustFrom) / (DustTo - DustFrom)) : 0f;
            var chill = worldIsDead ? Mathf.Clamp01((phase - ChillFrom) / (ChillTo - ChillFrom)) : 0f;

            if (worldIsDead && current.WinterSkyLow.maxColorComponent > 0.001f)
            {
                current.SkyHigh = Color.Lerp(current.AfterSkyHigh, current.WinterSkyHigh, chill);
                current.SkyLow = Color.Lerp(current.AfterSkyLow, current.WinterSkyLow, chill);
                current.Ground = Color.Lerp(current.AfterGround, current.WinterGround, chill);
                RecolorGround();
            }

            MoveAsh(phase, chill);

            var land = current;
            var hit = ImpactPoint(land);
            var falling = phase < Struck;

            impactor.gameObject.SetActive(falling);
            trail.gameObject.SetActive(falling);
            if (falling)
            {
                var t = phase / Struck;

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
            var burning = phase >= Struck && phase < FlashTo;
            flash.gameObject.SetActive(burning);
            if (burning)
            {
                var t = (phase - Struck) / (FlashTo - Struck);

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
            // 冷えきるまでに消す。残っていると、冷えた野に柱が立って見える。
            var rising = phase >= ColumnFrom && phase < ColumnTo;
            column.gameObject.SetActive(rising);
            if (rising)
            {
                var t = (phase - ColumnFrom) / (ColumnTo - ColumnFrom);
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
            Shake = phase >= Struck && phase < Struck + 0.08f
                ? (1f - (phase - Struck) / 0.08f)
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
                pixels[i] = new Color32(0, 0, 0, 0);
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
                Put(pixels, size, (int)(u * size), (int)(vBand * size),
                    level, level, (byte)(level + 8), (byte)starRandom.Next(256));
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

                // 揺れの位相。**1粒のあいだは同じ値にする。**
                var phase = (byte)starRandom.Next(256);
                Put(pixels, size, x, y,
                    warm ? level : (byte)(level * 0.86f),
                    (byte)(level * 0.92f),
                    warm ? (byte)(level * 0.78f) : level,
                    phase);

                if (level > 210)
                {
                    // 明るい星だけ十字ににじませる。粒だけだと点にしか見えない。
                    var dim = (byte)(level / 3);
                    Put(pixels, size, x + 1, y, dim, dim, dim, phase);
                    Put(pixels, size, x - 1, y, dim, dim, dim, phase);
                    Put(pixels, size, x, y + 1, dim, dim, dim, phase);
                    Put(pixels, size, x, y - 1, dim, dim, dim, phase);
                }
            }

            starTexture.SetPixels32(pixels);
            starTexture.Apply();

            // **星は加算の層ではなく専用の材質で描く。**
            // 加算の層は材質ごとに1つの明るさしか持てないので、
            // それで描くと空ぜんたいが同じ明るさで点滅し、瞬いて見えない。
            starMaterial = StandardMaterials.CreateStarField();
            starMaterial.hideFlags = createdFlags;
            starMaterial.color = Color.white;
            starMaterial.SetTexture("_EmissionMap", starTexture);
            starMaterial.SetColor("_EmissionColor", Color.black);
            starMaterial.SetFloat("_TwinkleSpeed", 2.2f);
            starMaterial.SetFloat("_TwinkleSharp", TwinkleSharp);
            ApplyTwinkle();

            var stars = PrimitiveMeshes.Create(PrimitiveType.Quad, "Stars", createdFlags);
            stars.transform.SetParent(transform, false);
            stars.transform.localPosition = new Vector3(0f, distance * 0.36f, distance * 0.985f);
            stars.transform.localScale = new Vector3(distance * 4f, distance * 1.9f, 1f);
            stars.GetComponent<Renderer>().sharedMaterial = starMaterial;
        }

        /// <summary>
        /// 星を1点置く。<paramref name="phase"/> は揺れの位相で、絵のアルファに焼く。
        ///
        /// **同じ星の中心と十字のにじみには、同じ位相を渡す。**
        /// 別々にすると1粒が分解して揺れ、粒に見えなくなる。
        /// 色は重ねて足すが、位相はあとから置いたもので上書きする。
        /// </summary>
        private static void Put(Color32[] pixels, int size, int x, int y,
            byte r, byte g, byte b, byte phase)
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
                phase);
        }

        /// <summary>
        /// 時刻を入れる。0で真夜中、0.25で日の出、0.5で正午、0.75で日の入り。
        ///
        /// 空の色・星の明るさ・光の向きと強さ・回り込む明るさを、まとめて決める。
        /// 別々に持つと、夜なのに地面だけ明るいといった食い違いが起きる。
        /// </summary>
        public void SetTimeOfDay(float phase, Light sun)
        {
            if (skyTexture == null && backdrop == null)
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

            lastDaylight = daylight;
            lastDusk = dusk;
            PaintSky();
            PlaceSun(elevation, daylight, dusk);
            TintClouds(daylight, dusk);

            if (starMaterial != null)
            {
                ApplyTwinkle();

                // 星は夜の深さでそのまま明るくする。薄明のあいだは弱い。
                // 星は薄明のあいだに急に消える。少しでも空が明るいと見えない。
                var night = Mathf.Clamp01(1f - daylight * 2.6f);
                night *= night;

                // **塵におおわれた空には星が出ない。**
                // 日光をさえぎるほどの塵なら、星明かりも通さない。
                if (worldIsDead)
                {
                    night *= 0.12f;
                }

                starMaterial.SetColor("_EmissionColor", Color.white * night);
            }

            if (sun != null)
            {
                var day = elevation > 0f;

                // **影を落とす光にする。** 立体感はほとんど影から来る。
                // 夜は月あかりなので、影を弱めて輪郭だけ残す。
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = day ? 0.80f : 0.32f;
                sun.shadowBias = 0.02f;
                sun.shadowNormalBias = 0.6f;

                // **塵が濃くなるにつれて、日光が弱まっていく。**
                // 一段で暗くすると、暗幕を下ろしたように見えて理由が読めない。
                // 塵が増えるのに合わせて落としていくと、
                // **塵が日光をさえぎったのだ**と分かる。
                // 何年つづいたかは表していない。
                var dust = Mathf.Lerp(1f, 0.22f, dustCover);

                // **横から当てる。** 影はここからしか生まれず、
                // 立体に見えるかどうかも影で決まる。詳しくは SunLight を参照。
                //
                // 夜は月あかりに置き換える。真っ暗にすると地形も輪郭も読めない。
                // 月は太陽と左右を入れ替えた側に置く。
                var toward = SunLight(elevation);
                if (!day)
                {
                    toward.x = -toward.x;
                }

                var toSun = transform.TransformDirection(toward.normalized);
                sun.transform.rotation = Quaternion.LookRotation(-toSun, Vector3.up);
                sun.color = day
                    ? Color.Lerp(new Color(1f, 0.74f, 0.52f), Color.white, Mathf.Clamp01(elevation * 2.4f))
                    : MoonLight;
                sun.intensity = dust * (day
                    ? Mathf.Lerp(0.35f, 1.25f, Mathf.Clamp01(elevation * 1.6f))
                    : 0.16f);
            }

            // **影の側を黒く沈ませない。** 横から当てると、カメラを向いた面は
            // 直接の光を受けなくなる。回り込みが一色だけだと、生きものも木も
            // 黒い影絵になり、時代ごとの色が消える。実際そうなった。
            //
            // 上からは空の色、下からは地面の色を入れる。
            // **日なたの地面は明るく、まわりへ色を跳ね返している。**
            // 砂なら暖かく、氷なら白く、跳ね返った色が物の下側に回る。
            // 一色で足すより、これのほうが場所の色が出る。
            // **上からの回り込みは弱めに。** ここを強くすると、影に落ちた地面と
            // 日の当たる地面の差が詰まって、全体が白っぽく平らになる。
            // 下からの照り返しは残す。そちらは陰の側だけを起こすので、
            // 影を薄めずに黒つぶれだけを防げる。
            var fromSky = Color.Lerp(NightLow * 0.7f, current.SkyLow * 0.40f, daylight);
            var fromGround = Color.Lerp(NightLow * 0.4f, current.Ground * 0.45f, daylight);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = fromSky;
            RenderSettings.ambientEquatorColor = Color.Lerp(fromGround, fromSky, 0.5f);
            RenderSettings.ambientGroundColor = fromGround;
        }

        /// <summary>
        /// 地面。帯に分けて、遠い帯ほど空の色へ寄せる。
        /// 1枚の板だと手前も地平線も同じ色になり、奥行きが消えるためである。
        /// </summary>
        private void BuildGround(Landscape land)
        {
            if (land.Relief > 0.0001f)
            {
                BuildTerrain(land);
                return;
            }

            BuildFlatGround(land);
        }

        /// <summary>
        /// 起伏のある地面。Unity の Terrain を**コードから組み立てる**。
        ///
        /// **手で塗らない。** 時代ごとに地面を手で描くと、時代を足すたびに
        /// 手作業が増え、データから組み立てるという作りが崩れる。
        /// 高さは <see cref="GroundHeight"/> から引く。置くものと同じ式なので、
        /// 木も生きものも必ず地面の上に立つ。
        ///
        /// **材質は Terrain の既定を使わない。** 既定の地形用シェーダーは
        /// 塗り分けの絵を要求し、WebGLビルドに残るかも確かめにくい。
        /// この作品で実績のある自前のシェーダーを割り当て、色1つで塗る。
        /// 遠さによる霞は、地面ぜんたいでは掛けずに空の色へ寄せた1色で近似する。
        ///
        /// **実在の地形ではない。**
        /// </summary>
        private void BuildTerrain(Landscape land)
        {
            // Terrain の高さの升目は 2のべき乗+1 でなければならない。
            const int resolution = 129;

            var width = land.HalfWidth * 6f;
            var length = land.FarZ * 1.6f;
            var originZ = -land.FarZ * 0.25f;

            // 起伏の振れ幅を測ってから、その幅に合わせて升目を正規化する。
            var lowest = float.MaxValue;
            var highest = float.MinValue;
            var raw = new float[resolution, resolution];
            for (var zi = 0; zi < resolution; zi++)
            {
                var z = originZ + length * zi / (float)(resolution - 1);
                for (var xi = 0; xi < resolution; xi++)
                {
                    var x = -width * 0.5f + width * xi / (float)(resolution - 1);

                    // Terrain の高さは [zi, xi] の順に入れる。逆にすると地形が転置する。
                    var h = GroundHeight(x, z);
                    raw[zi, xi] = h;
                    lowest = Mathf.Min(lowest, h);
                    highest = Mathf.Max(highest, h);
                }
            }

            var span = Mathf.Max(0.01f, highest - lowest);
            for (var zi = 0; zi < resolution; zi++)
            {
                for (var xi = 0; xi < resolution; xi++)
                {
                    raw[zi, xi] = (raw[zi, xi] - lowest) / span;
                }
            }

            terrainData = new TerrainData();
            terrainData.hideFlags = createdFlags;
            terrainData.heightmapResolution = resolution;
            terrainData.size = new Vector3(width, span, length);
            terrainData.SetHeights(0, 0, raw);

            var host = Terrain.CreateTerrainGameObject(terrainData);
            host.name = "Ground";
            host.hideFlags = createdFlags;
            host.transform.SetParent(transform, false);
            host.transform.localPosition = new Vector3(-width * 0.5f, lowest, originZ);

            // 当たり判定は要らない。誰も歩かないし、当たりを取る相手もいない。
            var collider = host.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            terrain = host.GetComponent<Terrain>();
            terrain.materialTemplate = GroundMaterial(land, width, length);
            terrain.drawTreesAndFoliage = false;
            terrain.heightmapPixelError = 8f;
            terrain.basemapDistance = land.FarZ * 2f;
            // **地面は影を受ける。** 受けないと、木を照らしても足もとに何も出ず、
            // 置いたものが地面から浮いて見える。落とす側にはしなくてよい。
            terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// 地面の材質。**粒と凹凸を入れる。**
        ///
        /// これまで地面は1色で塗っていた。起伏を付けても、色が一様だと
        /// 大きな床にしか見えない。手前に寄るほど、何も無いことがはっきりする。
        ///
        /// 細かい粒があると、目はそれを土や砂として読む。粒の凹凸を法線の絵で
        /// 与えると、**光の向きに応じて明暗が出る。** 横から当てている場面では
        /// これが効く。落ちた影も、平らな面より地面らしく見える。
        ///
        /// 絵は1枚だけ作って敷き詰める。継ぎ目が出ないよう、
        /// 端がつながる作り方（TileableNoise）で作る。
        /// 時代ごとの色は _Color で掛けるので、絵は時代によらず1枚でよい。
        /// </summary>
        private Material GroundMaterial(Landscape land, float width, float length)
        {
            const int size = 256;
            if (grainTexture == null)
            {
                var field = TileableNoise(size, 91);
                grainTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
                grainTexture.hideFlags = createdFlags;
                grainTexture.wrapMode = TextureWrapMode.Repeat;

                var grain = new Color32[size * size];
                for (var i = 0; i < field.Length; i++)
                {
                    // 0.70〜1.0 に収める。1を越える値は絵に入らないので、
                    // 明るい側を1に置き、そのぶん色のほうを持ち上げる。
                    var v = (byte)Mathf.Clamp(Mathf.Lerp(0.70f, 1f, field[i]) * 255f, 0f, 255f);
                    grain[i] = new Color32(v, v, v, 255);
                }

                grainTexture.SetPixels32(grain);
                grainTexture.Apply();
            }

            if (grainNormalTexture == null)
            {
                var field = TileableNoise(size, 91);
                grainNormalTexture = new Texture2D(size, size, TextureFormat.RGBA32, true);
                grainNormalTexture.hideFlags = createdFlags;
                grainNormalTexture.wrapMode = TextureWrapMode.Repeat;

                var packed = new Color32[size * size];
                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var left = field[y * size + (x + size - 1) % size];
                        var right = field[y * size + (x + 1) % size];
                        var down = field[((y + size - 1) % size) * size + x];
                        var up = field[((y + 1) % size) * size + x];

                        var nx = Mathf.Clamp((left - right) * 2.4f, -1f, 1f);
                        var ny = Mathf.Clamp((down - up) * 2.4f, -1f, 1f);

                        // **2通りの詰め方の両方に合うように入れる。**
                        // Unity の UnpackNormal は環境によって (a, g) を読む形と
                        // (r, g, b) を読む形に分かれる。x を r と a の両方に入れ、
                        // z にあたる b を1に置けば、どちらで読まれても成り立つ。
                        var r = (byte)Mathf.Clamp((nx * 0.5f + 0.5f) * 255f, 0f, 255f);
                        var g = (byte)Mathf.Clamp((ny * 0.5f + 0.5f) * 255f, 0f, 255f);
                        packed[y * size + x] = new Color32(r, g, 255, r);
                    }
                }

                grainNormalTexture.SetPixels32(packed);
                grainNormalTexture.Apply();
            }

            var material = StandardMaterials.CreateOpaque(true);
            material.hideFlags = createdFlags;

            // **色は持ち上げない。** 粒の絵は1を越えられないので、
            // 明るさを取り戻そうと色を上げると、白に近い地面（全球凍結の氷）が
            // 振り切れて真っ白になり、粒も起伏も消える。実際そうなった。
            // 地面は少し暗くなるが、そのほうが影も出る。
            material.color = land.Ground;
            material.SetTexture("_MainTex", grainTexture);
            material.SetTexture("_BumpMap", grainNormalTexture);
            material.SetFloat("_BumpScale", 0.85f);
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);

            // 1枚を何メートルぶんに敷くか。
            // **敷く枚数に上限を置く。** 街の時代は野が2km近くあり、
            // 20mごとに敷くと100枚並んで、同じ模様の繰り返しが縞に見えた。
            var tiles = Mathf.Clamp(width / 24f, 4f, 40f);
            var scale = new Vector2(tiles, tiles * length / width);
            material.SetTextureScale("_MainTex", scale);
            material.SetTextureScale("_BumpMap", scale);

            materials["Terrain"] = material;
            return material;
        }

        /// <summary>
        /// 端どうしがつながる濃淡を作る。敷き詰めても継ぎ目が出ない。
        ///
        /// Mathf.PerlinNoise は端がつながらないため、敷き詰めると
        /// 升目の線がそのまま見える。ここでは周期のある格子の上で
        /// 値を補間し、格子の端を反対側へ回している。
        /// </summary>
        private static float[] TileableNoise(int size, int seed)
        {
            var field = new float[size * size];
            var random = new System.Random(seed);
            var amplitude = 1f;
            var total = 0f;

            // **粗い波を入れない。** 粗い波は敷き詰めたときに
            // まだら模様として繰り返しが見えてしまう。大きな起伏は
            // 地形そのものが持っているので、ここは細かい粒だけでよい。
            for (var octave = 0; octave < 4; octave++)
            {
                var period = 16 << octave;
                if (period > size)
                {
                    break;
                }

                var lattice = new float[period * period];
                for (var i = 0; i < lattice.Length; i++)
                {
                    lattice[i] = (float)random.NextDouble();
                }

                for (var y = 0; y < size; y++)
                {
                    var fy = y * period / (float)size;
                    var y0 = (int)fy;
                    var ty = fy - y0;
                    var y1 = (y0 + 1) % period;
                    var sy = ty * ty * (3f - 2f * ty);

                    for (var x = 0; x < size; x++)
                    {
                        var fx = x * period / (float)size;
                        var x0 = (int)fx;
                        var tx = fx - x0;
                        var x1 = (x0 + 1) % period;
                        var sx = tx * tx * (3f - 2f * tx);

                        var a = Mathf.Lerp(lattice[y0 * period + x0], lattice[y0 * period + x1], sx);
                        var b = Mathf.Lerp(lattice[y1 * period + x0], lattice[y1 * period + x1], sx);
                        field[y * size + x] += Mathf.Lerp(a, b, sy) * amplitude;
                    }
                }

                total += amplitude;
                amplitude *= 0.55f;
            }

            for (var i = 0; i < field.Length; i++)
            {
                field[i] /= total;
            }

            return field;
        }

        /// <summary>
        /// 平らな地面。奥行きで色を分けた帯を並べる。
        /// 起伏を持たない時代（水面や溶けた地面）で使う。
        /// </summary>
        private void BuildFlatGround(Landscape land)
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
                // **面が複数あるものは、面ごとに塗る。**
                // 1つ目だけ差し替えると、2つ目の面に素材側の色が残る。
                var marker = renderer.GetComponent<ModelRoles>();
                if (marker != null && marker.KeepOwnMaterials)
                {
                    // 自前の絵を持っている。塗り直さない。
                    continue;
                }

                if (marker != null && marker.Roles != null && marker.Roles.Length > 1)
                {
                    var slots = new Material[marker.Roles.Length];
                    for (var i = 0; i < slots.Length; i++)
                    {
                        var role = marker.Roles[i];
                        slots[i] = Tinted(role, ColorFor(role), step);
                    }

                    renderer.sharedMaterials = slots;
                    continue;
                }

                var key = renderer.gameObject.name;
                renderer.sharedMaterial = Tinted(key, ColorFor(key), step);
            }
        }

        private Color ColorFor(string key)
        {
            switch (key)
            {
                case "Trunk": return current.Trunk;
                case "DeadTrunk":
                    return current.DeadTrunk.maxColorComponent > 0.001f
                        ? current.DeadTrunk
                        : current.Trunk;
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
                item.transform.SetParent(ScatterRoot, false);
                item.transform.localPosition = new Vector3(x, GroundHeight(x, z), z);

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
                    item.transform.localPosition = new Vector3(x, GroundHeight(x, z), z);
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
                item.transform.SetParent(ScatterRoot, false);
                item.transform.localPosition = new Vector3(x, GroundHeight(x, z), z);
                item.transform.localRotation = Quaternion.Euler(0f,
                    (random.Next(2) == 0 ? 90f : 270f) + (float)(random.NextDouble() * 2.0 - 1.0) * 24f, 0f);
                ApplyDepth(item, z);
            }
        }

        /// <summary>
        /// 下草を撒く。**カメラのすぐ手前から撒く。**
        ///
        /// ほかの撒き方は置き場所の手前の端（NearZ）より先にしか置かない。
        /// カメラはそこからさらに手前にあるので、画面の下半分には何も無く、
        /// 地面の帯だけが広がっていた。ここでは端より手前まで撒く。
        ///
        /// **手前を濃くする。** 木とは逆である。木は近いと画面を覆ってしまうが、
        /// 下草は膝の高さしかないので、近いほど地面の起伏が読める。
        /// </summary>
        private void ScatterCover(int count, Func<float, GameObject> build, float height)
        {
            if (count <= 0)
            {
                return;
            }

            var from = -current.NearZ * 2.4f;
            var to = current.FarZ * 0.42f;

            // **かたまりで生やす。** 一様に撒くと、間が等しく空いて
            // 人が並べたように見える。草は群れて生え、あいだに地面が出る。
            // その粗密こそが、地面が平らでないことを見せる。
            var clumpCount = Mathf.Max(1, count / 5);
            var clumpX = new float[clumpCount];
            var clumpZ = new float[clumpCount];
            for (var i = 0; i < clumpCount; i++)
            {
                clumpZ[i] = Mathf.Lerp(from, to, Mathf.Pow((float)random.NextDouble(), 1.45f));
                clumpX[i] = (float)(random.NextDouble() * 2.0 - 1.0) * current.HalfWidth * 0.72f;
            }

            for (var i = 0; i < count; i++)
            {
                var clump = random.Next(clumpCount);

                // かたまりの広がりは、奥行きに合わせて広げる。
                // 手前で広げすぎると、群れではなく散らばりに見える。
                var spread = Mathf.Lerp(3f, 16f,
                    Mathf.Clamp01(Mathf.InverseLerp(from, to, clumpZ[clump])));
                var z = clumpZ[clump] + (float)(random.NextDouble() * 2.0 - 1.0) * spread;
                var x = clumpX[clump] + (float)(random.NextDouble() * 2.0 - 1.0) * spread;
                z = Mathf.Clamp(z, from, to);
                x = Mathf.Clamp(x, -current.HalfWidth * 0.8f, current.HalfWidth * 0.8f);

                // 手前のものを少し大きくする。奥は小さく、粒として効く。
                var near = 1f - Mathf.Clamp01(Mathf.InverseLerp(from, to, z));

                var scale = height * Mathf.Lerp(0.55f, 1.25f, near)
                            * (0.7f + (float)random.NextDouble() * 0.7f);

                var item = build(scale);
                item.transform.SetParent(ScatterRoot, false);
                item.transform.localPosition = new Vector3(x, GroundHeight(x, z), z);
                item.transform.localRotation = Quaternion.Euler(
                    0f, (float)random.NextDouble() * 360f, 0f);
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
            var model = TryModel("ph_rock", height);
            if (model != null)
            {
                return model;
            }

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
            var model = TryModel("imp_dead_trunk", height);
            if (model != null)
            {
                return model;
            }

            var root = Root("DeadTrunk");
            var lean = (float)(random.NextDouble() * 2.0 - 1.0) * 14f;

            var trunk = Piece(root, PrimitiveType.Cylinder, "DeadTrunk",
                new Vector3(0f, height * 0.5f, 0f),
                new Vector3(height * 0.045f, height * 0.5f, height * 0.045f));
            trunk.transform.localRotation = Quaternion.Euler(lean, 0f, lean * 0.6f);

            // 折れた枝を2本だけ。多いと生きている木に見えてしまう。
            for (var i = 0; i < 2; i++)
            {
                var branch = Piece(root, PrimitiveType.Cylinder, "DeadTrunk",
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
        /// <summary>
        /// **試し用。** 外から持ってきた形があれば、それを使う。
        /// 無ければこれまでどおり球と円錐で組み立てる。
        /// </summary>
        private GameObject TryModel(string name, float height)
        {
            // **丸ごと1枚の絵に焼いたものは、板に貼る。**
            // 名前が imp_ で始まるものがこれにあたる。形を持たず絵だけなので、
            // 形の読み込みより先に分ける。あとに置くと、形が無いところで
            // 帰ってしまい、絵にたどり着かない。
            if (name.StartsWith("imp_"))
            {
                return BuildImpostor(name, height);
            }

            var prefab = Resources.Load<GameObject>("Nature/" + name);
            if (prefab == null)
            {
                return null;
            }

            // **写真から起こした形は、絵をそのまま貼る。**
            // 名前が ph_ で始まるものは Poly Haven の素材で、
            // 幹にも葉にも写真の絵が付いている。時代の色で塗りつぶすと、
            // 写実であることの意味が無くなる。
            if (name.StartsWith("ph_"))
            {
                return TexturedModel(prefab, name, height);
            }

            var item = Instantiate(prefab);
            item.name = name;
            item.hideFlags = createdFlags;

            // 素材ごとの大きさはまちまちなので、高さをそろえる。
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var first = true;
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                // **素材側の色を連れてこさせない。**
                // そのまま置くと水色の木や桃色の幹が並び、
                // 時代ごとに決めた色調から外れる。
                // 面ごとの役を材質名から読み取って覚えておき、
                // 置いたあとで時代の色へ塗り直す。
                var sources = renderer.sharedMaterials;
                var roles = new string[sources.Length];
                for (var i = 0; i < sources.Length; i++)
                {
                    roles[i] = RoleFromMaterial(sources[i] != null ? sources[i].name : string.Empty);
                }

                var marker = renderer.gameObject.AddComponent<ModelRoles>();
                marker.Roles = roles;
                renderer.gameObject.name = roles.Length > 0 ? roles[0] : "Foliage";
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            if (!first && bounds.size.y > 0.0001f)
            {
                // **拡大率を上書きしない。掛ける。**
                // Blender から書き出した形は、根に100倍の拡大率を持って入る。
                // 上書きすると、その100倍が消えて100分の1の大きさになる。
                item.transform.localScale *= height / Reference(bounds.size);
            }

            return item;
        }

        /// <summary>
        /// 丸ごと1枚の絵に焼いた木を、板に貼って立てる。
        ///
        /// **面は2つしかない。** 葉の茂りは絵の中にあるので、
        /// 枚数を減らして枝だけになる、ということが起きない。
        /// 容量も絵の分（数百KB）だけで済む。
        ///
        /// 板は根元を地面に合わせる。板そのものは中心が原点なので、
        /// 入れ物を作って高さの半分だけ持ち上げている。
        /// </summary>
        /// <summary>
        /// 焼いた木の絵に掛ける明るさ。1だと場面の照明と二重になって白へ飛ぶ。
        /// 値は見え方で決めた。奥の遠景の写真と手前の木の明るさが逆転しない範囲である。
        /// </summary>
        private const float ImpostorAlbedo = 0.60f;

        /// <summary>
        /// 立っているものの絵を、地面からどれだけ持ち上げるか（背丈に対する比）。
        /// 焼いた絵の中で根元が下端より少し上にあるので、そのぶん下げてある。
        /// </summary>
        private const float StandingRise = 0.46f;

        /// <summary>
        /// 横たわっているものの絵を、地面からどれだけ持ち上げるか。
        ///
        /// **倒木は立たない。** `imp_dead_trunk` は横たわった幹を焼いた絵であり、
        /// 立ち枯れた木ではない。立木と同じだけ持ち上げると、板が宙に浮いて
        /// 横たわり、枯れ木にも倒木にも見えない。衝突の時代では、浮いた幹が
        /// 地平線の上に帯をつくっていた。
        ///
        /// 絵の中で幹は上下の中ほどにあるので、ここをほぼ0にすると地面に寝る。
        /// 板の下半分は透けているだけなので、地面へ埋まっても何も出ない。
        /// </summary>
        private const float LyingRise = 0.03f;

        private GameObject BuildImpostor(string name, float height)
        {
            // 横たわった幹だけは、立木と置き方を変える。
            return BuildImpostor(name, height, name == "imp_dead_trunk" ? LyingRise : StandingRise);
        }

        private GameObject BuildImpostor(string name, float height, float rise)
        {
            var texture = Resources.Load<Texture2D>("Nature/" + name);
            if (texture == null)
            {
                return null;
            }

            var root = Root(name);
            root.AddComponent<Billboard>();

            var quad = new GameObject("Impostor", typeof(MeshFilter), typeof(MeshRenderer));
            quad.hideFlags = createdFlags;
            quad.GetComponent<MeshFilter>().sharedMesh = UpFacingQuad();
            quad.transform.SetParent(root.transform, false);

            // 焼いた絵は正方形で、木は中央に収まっている。
            // どれだけ持ち上げるかは、立っているか横たわっているかで変わる。
            quad.transform.localPosition = new Vector3(0f, height * rise, 0f);
            quad.transform.localScale = new Vector3(height, height, 1f);

            var id = "Impostor/" + name;
            Material material;
            if (!materials.TryGetValue(id, out material) || material == null)
            {
                material = StandardMaterials.CreateFoliageCutout();
                material.hideFlags = createdFlags;

                // **焼いた絵には、もう照明が入っている。**
                // 絵は Blender で正射影・カメラ側から弱く照明して焼いたものなので、
                // 明暗はすでに絵の中にある。そこへ場面の日光と回り込みをそのまま
                // 掛けると二重になり、明るいところが1を越えて白へ飽和する。
                // 色の三つの成分がそろって頭打ちになるため、**色みまで抜ける。**
                // 実際、森の時代の木が幹まで白い幽霊のようになり、
                // 奥の遠景の写真より手前の木のほうが明るい、という逆転が出ていた。
                //
                // **板の法線は真上に向けてある**（UpFacingQuad）。
                // 葉が上から光を受ける見え方にするための決めだが、そのぶん
                // 日が高いときは常に最大の光量を受ける。二重掛けがそのまま出る。
                //
                // ここで絵の明るさを落としておく。場面の照明は掛かったままなので、
                // 朝夕の赤みも夜の暗さも、これまでどおり効く。
                material.color = Color.white * ImpostorAlbedo;
                material.SetTexture("_MainTex", texture);
                material.SetFloat("_Cutoff", 0.35f);
                material.SetFloat("_Glossiness", 0f);
                material.SetFloat("_Metallic", 0f);
                materials[id] = material;
            }

            var renderer = quad.GetComponent<Renderer>();
            renderer.sharedMaterial = material;

            // **板でも影を落とす。** 切り抜いた形のまま地面へ落ちるので、
            // 板だと気づかれにくくなり、地面との結びつきも強まる。
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = false;

            // 置いたあとの塗り直しから守る。焼いた絵を時代の色で塗りつぶさない。
            var keep = quad.AddComponent<ModelRoles>();
            keep.KeepOwnMaterials = true;

            return root;
        }

        /// <summary>
        /// 写真から起こした形。絵をそのまま貼る。
        ///
        /// 葉は板に絵を貼って抜き色で切り抜く作りなので、切り抜くシェーダーを使う。
        /// 幹は普通の不透明でよい。どちらかは材質名から決める。
        ///
        /// **遠さで色を薄める仕組みは通さない。** 写真の絵を空の色へ寄せると
        /// 濁って見える。そのぶん、遠くのものは山なみと雲に任せる。
        /// </summary>
        private GameObject TexturedModel(GameObject prefab, string name, float height)
        {
            var item = Instantiate(prefab);
            item.name = name;
            item.hideFlags = createdFlags;

            KeepOneLod(item);

            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            var first = true;
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                if (first)
                {
                    bounds = renderer.bounds;
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                var sources = renderer.sharedMaterials;
                var slots = new Material[sources.Length];
                for (var i = 0; i < slots.Length; i++)
                {
                    var part = sources[i] != null ? sources[i].name.ToLowerInvariant() : string.Empty;
                    slots[i] = TexturedMaterial(name, part);
                }

                renderer.sharedMaterials = slots;
                // **影を落とさせる。** 立体感はほとんど影から来る。
                // 影が無いと、どれも同じ平面に貼った絵のように見える。
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;

                // 置いたあとの塗り直しから守る。写真の絵を時代の色で
                // 塗りつぶすと、写実であることの意味が無くなる。
                var keep = renderer.gameObject.AddComponent<ModelRoles>();
                keep.KeepOwnMaterials = true;
            }

            if (!first && bounds.size.y > 0.0001f)
            {
                // **拡大率を上書きしない。掛ける。**
                // Blender から書き出した形は、根に100倍の拡大率を持って入る。
                // 上書きすると、その100倍が消えて100分の1の大きさになる。
                item.transform.localScale *= height / Reference(bounds.size);
            }

            return item;
        }

        /// <summary>
        /// **段階のある形は1つだけ残す。**
        /// 写真計測の素材は、遠さで差し替えるための粗さ違い（LOD0〜LOD3）を
        /// まとめて持っている。Unity は差し替えの仕掛けを知らないので、
        /// そのまま置くと4つ全部が同じ場所に重なって描かれる。
        /// いちばん粗いものだけを残す。遠くに小さく映るので、細かさは要らない。
        /// </summary>
        private static void KeepOneLod(GameObject item)
        {
            var best = -1;
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                var level = LodLevel(renderer.gameObject.name);
                if (level > best)
                {
                    best = level;
                }
            }

            if (best < 0)
            {
                return;
            }

            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                var level = LodLevel(renderer.gameObject.name);
                if (level >= 0 && level != best)
                {
                    renderer.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 高さをそろえるときの基準の長さ。
        ///
        /// **縦の長さだけを見てはいけない。** 平たい岩は縦が極端に短いので、
        /// 縦を置きたい高さに合わせると、横がその何十倍にも伸びる。
        /// 実際、岩が地面に横たわる巨大な板になって出ていた。
        /// 縦と、いちばん長い辺の6割の、大きいほうを基準にする。
        /// 立っているものは縦が、寝ているものは長辺が効く。
        /// </summary>
        private static float Reference(Vector3 size)
        {
            var longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            return Mathf.Max(0.0001f, Mathf.Max(size.y, longest * 0.6f));
        }

        /// <summary>名前の末尾の LOD の段。持たないものは -1。</summary>
        private static int LodLevel(string name)
        {
            var at = name.LastIndexOf("LOD", System.StringComparison.OrdinalIgnoreCase);
            if (at < 0 || at + 3 >= name.Length)
            {
                return -1;
            }

            int level;
            return int.TryParse(name.Substring(at + 3), out level) ? level : -1;
        }

        /// <summary>
        /// 写真の絵を貼った材質。名前ごとに1つだけ作って使い回す。
        /// 置くたびに作ると、木の数だけ材質が増えて描き直しが重くなる。
        /// </summary>
        private Material TexturedMaterial(string modelName, string part)
        {
            // **素材ごとに絵の呼び名が違う。** 取りうる名前を順に試す。
            // leaves と leaf、trunk と main が素材によって入れ替わる。
            // 決め打ちにすると、名前の合わない素材が白いまま置かれる。
            string[] candidates;
            var leafy = part.Contains("leaf") || part.Contains("leaves");
            if (leafy)
            {
                candidates = new[] { "leaves", "leaf", "main" };
            }
            else if (part.Contains("branch"))
            {
                candidates = new[] { "branches", "branch", "trunk", "main" };
            }
            else
            {
                candidates = new[] { "trunk", "bark", "main" };
            }

            var stem = candidates[candidates.Length - 1];
            Texture2D cutout = null;
            foreach (var candidate in candidates)
            {
                var texture = Resources.Load<Texture2D>("Nature/" + modelName + "_" + candidate);
                if (texture == null)
                {
                    continue;
                }

                cutout = texture;
                stem = candidate;
                break;
            }

            var id = modelName + "/" + stem;
            Material found;
            if (materials.TryGetValue(id, out found) && found != null)
            {
                return found;
            }

            // 葉だけ切り抜く。幹や岩を切り抜くと、影の縁がぎざぎざになる。
            leafy = leafy || modelName.Contains("fern");
            var material = leafy
                ? StandardMaterials.CreateFoliageCutout()
                : StandardMaterials.CreateOpaque(false);

            material.hideFlags = createdFlags;
            material.color = Color.white;
            if (cutout != null)
            {
                material.SetTexture("_MainTex", cutout);
            }

            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);

            if (leafy)
            {
                // **切り抜く境目を低めに取る。**
                // 葉の絵は縁がぼけており、高い境目にすると輪郭から削られていく。
                // 板そのものが消えて、枝だけの木になっていた。
                material.SetFloat("_Cutoff", 0.15f);
            }

            materials[id] = material;
            return material;
        }

        /// <summary>
        /// 材質名から役を決める。
        /// Kenney の素材は leafsGreen / woodBark のように名前が中身を表している。
        /// 分からないものは岩あつかいにする。地面の色に近く、浮いて見えない。
        /// </summary>
        private static string RoleFromMaterial(string materialName)
        {
            var lower = materialName.ToLowerInvariant();

            if (lower.Contains("leaf") || lower.Contains("green") || lower.Contains("foliage")
                || lower.Contains("grass"))
            {
                return "Foliage";
            }

            if (lower.Contains("wood") || lower.Contains("bark") || lower.Contains("trunk")
                || lower.Contains("brown"))
            {
                return "Trunk";
            }

            // 岩と土。地面の色に近い。
            return "Rock";
        }

        private GameObject BuildConifer(float height)
        {
            var model = TryModel("imp_tree_quiver", height);
            if (model != null)
            {
                return model;
            }

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
            var model = TryModel("ph_fern", height);
            if (model != null)
            {
                return model;
            }

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
            var model = TryModel("imp_tree_broad", height);
            if (model != null)
            {
                return model;
            }

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
        /// 四つ足の生きもの。首と尾の長さ、脚の高さ、牙の有無は時代の表から取る。
        ///
        /// **特定の種ではない。** 形の傾向だけを置いている。
        /// ただし**傾向は時代ごとに変える。** 首と尾の長い大きな四つ足は
        /// 白亜紀の終わり（約6600万年前）に絶滅しており、そのあとの時代に
        /// 立っていてはいけない。氷期はその6400万年あとである。
        /// </summary>
        private GameObject BuildQuadruped(float height)
        {
            // **牙のある獣は、ふつうの比では読み取れない。**
            // 胴に脚を付けて牙を足すだけでは、丸い塊に棒が刺さった姿になる。
            // 別の組み立てへ回す。
            if (current.CreatureTusks)
            {
                return BuildTusker(height);
            }

            var root = Root("Quadruped");

            // **比は時代ごとに違う。同じ姿を使い回さない。**
            // 首と尾の長い大きな四つ足は白亜紀の終わりに絶滅しており、
            // そのあとの時代に立っていてはいけない。
            // 脚を短く、胴を長く取る。脚が長いと竹馬に乗ったように浮いて見える。
            var bodyRatio = current.CreatureBody > 0f ? current.CreatureBody : 0.72f;
            var legRatio = current.CreatureLegs > 0f ? current.CreatureLegs : 0.30f;
            var neck = current.CreatureNeck > 0f ? current.CreatureNeck : 1f;
            var tail = current.CreatureTail > 0f ? current.CreatureTail : 1f;

            var body = height * bodyRatio;
            var legs = height * legRatio;

            Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, legs + body * 0.28f, 0f),
                new Vector3(body * 0.42f, body * 0.46f, body));

            // 首は球を隙間なく重ねて弧にする。離すと数珠に見える。
            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                // 短くすると頭が胴のすぐ前へ来る。長くすると高く前へ伸びる。
                var y = legs + body * (0.36f + t * 0.62f * neck);
                var z = body * (0.42f + (t * 0.78f + Mathf.Sin(t * 3.1f) * 0.1f) * neck);
                var r = Mathf.Lerp(body * 0.21f, Mathf.Lerp(body * 0.18f, body * 0.09f, neck), t);
                Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, y, z), new Vector3(r, r, r));
            }

            var headY = legs + body * (0.36f + 0.63f * neck);
            var headZ = body * (0.42f + 0.86f * neck);
            Piece(root, PrimitiveType.Sphere, "Creature",
                new Vector3(0f, headY, headZ),
                new Vector3(body * 0.12f, body * 0.11f, body * 0.22f));

            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                var r = Mathf.Lerp(body * 0.17f, body * 0.03f, t);
                Piece(root, PrimitiveType.Sphere, "Creature",
                    new Vector3(0f, legs + body * (0.3f - t * 0.16f),
                        -body * (0.45f + t * 0.72f * tail)),
                    new Vector3(r, r, r));
            }

            // 脚は地面から胴の中ほどまで通す。
            // 胴の下で止めると、隙間ができて胴が宙に浮いて見える。
            var hip = legs + body * 0.28f;
            for (var i = 0; i < 4; i++)
            {
                var front = i < 2;
                var side = (i % 2 == 0) ? 1f : -1f;
                Piece(root, PrimitiveType.Cylinder, "Creature",
                    new Vector3(side * body * 0.20f, hip * 0.5f, front ? body * 0.3f : -body * 0.3f),
                    new Vector3(body * 0.17f, hip * 0.5f, body * 0.17f));
            }

            return root;
        }

        /// <summary>
        /// 牙のある四つ足（マンモスにならう）。
        ///
        /// **形は試作から移している。** `docs/prototypes/mammoth.html` は
        /// 距離関数を光線でたどって描いており、ここは球の組み立てなので、
        /// 描き方はまったく違う。**移しているのは各部の位置と太さの比だけである。**
        /// 試作の座標は「前・上・横」で、頭の上までがおよそ3.52ある。
        /// その高さで割って、与えられた背丈へ合わせる。
        ///
        /// **マンモスらしさは4つから来る。** 高い肩から後ろへ下がる背、
        /// 丸く盛り上がった頭頂、垂れる鼻、外へ張り出してから前で持ち上がり
        /// 内へ巻く牙である。耳は小さい。寒いところの獣は耳が小さく、
        /// そこが象との違いになる。
        ///
        /// **特定の種を写したものではない。** 毛の色も長さも分かっていない。
        /// </summary>
        private GameObject BuildTusker(float height)
        {
            var root = Root("Quadruped");
            var s = height / 3.52f;

            // 胴。前が高く、後ろへ下がる。
            TuskerBall(root, s, new Vector3(-0.20f, 1.95f, 0f), new Vector3(1.55f, 0.95f, 0.95f));
            TuskerBall(root, s, new Vector3(0.75f, 2.45f, 0f), new Vector3(0.85f, 0.70f, 0.80f));
            TuskerBall(root, s, new Vector3(-1.25f, 1.85f, 0f), new Vector3(0.75f, 0.75f, 0.82f));

            // 頭。丸い頭頂と、小さな耳。
            TuskerBall(root, s, new Vector3(1.75f, 2.55f, 0f), new Vector3(0.62f, 0.72f, 0.60f));
            TuskerBall(root, s, new Vector3(1.62f, 3.12f, 0f), new Vector3(0.42f, 0.40f, 0.36f));
            TuskerBall(root, s, new Vector3(1.45f, 2.62f, 0.56f), new Vector3(0.20f, 0.26f, 0.07f));
            TuskerBall(root, s, new Vector3(1.45f, 2.62f, -0.56f), new Vector3(0.20f, 0.26f, 0.07f));

            // 鼻。太いところから細いところへ垂らす。
            TuskerChain(root, s,
                new[]
                {
                    new Vector3(2.22f, 2.45f, 0f), new Vector3(2.52f, 1.85f, 0f),
                    new Vector3(2.62f, 1.20f, 0f), new Vector3(2.50f, 0.62f, 0f),
                    new Vector3(2.68f, 0.36f, 0f),
                },
                new[] { 0.28f, 0.21f, 0.16f, 0.12f, 0.10f }, 1f);

            // 尾。短い。長いと別の獣に見える。
            TuskerChain(root, s,
                new[] { new Vector3(-1.95f, 2.15f, 0f), new Vector3(-2.20f, 1.55f, 0f) },
                new[] { 0.09f, 0.05f }, 1f);

            // 牙。下へ出て外へ張り出し、前で持ち上がって内へ巻く。
            var tusk = new[]
            {
                new Vector3(2.05f, 2.20f, 0.28f), new Vector3(2.30f, 1.72f, 0.40f),
                new Vector3(2.65f, 1.38f, 0.56f), new Vector3(3.05f, 1.30f, 0.70f),
                new Vector3(3.42f, 1.48f, 0.68f), new Vector3(3.62f, 1.85f, 0.52f),
                new Vector3(3.58f, 2.22f, 0.28f),
            };
            var tuskRadii = new[] { 0.13f, 0.12f, 0.11f, 0.095f, 0.08f, 0.06f, 0.03f };
            TuskerChain(root, s, tusk, tuskRadii, 1f);
            TuskerChain(root, s, tusk, tuskRadii, -1f);

            // 脚。腰は胴の中まで入れ、足先は地面に付ける。
            TuskerLeg(root, s, new Vector3(0.95f, 1.85f, 0.50f), new Vector3(0.95f, 0.30f, 0.52f));
            TuskerLeg(root, s, new Vector3(0.95f, 1.85f, -0.50f), new Vector3(0.95f, 0.30f, -0.52f));
            TuskerLeg(root, s, new Vector3(-1.25f, 1.75f, 0.50f), new Vector3(-1.30f, 0.30f, 0.52f));
            TuskerLeg(root, s, new Vector3(-1.25f, 1.75f, -0.50f), new Vector3(-1.30f, 0.30f, -0.52f));

            return root;
        }

        /// <summary>
        /// 試作の「前・上・横」の座標へ球を1つ置く。
        /// Unity では前がZ、横がXなので、入れ替えて渡す。
        /// 半径で受け取り、置くときに直径へ直す。
        /// </summary>
        private void TuskerBall(GameObject root, float s, Vector3 at, Vector3 radii)
        {
            Piece(root, PrimitiveType.Sphere, "Creature",
                new Vector3(at.z * s, at.y * s, at.x * s),
                new Vector3(radii.z * 2f * s, radii.y * 2f * s, radii.x * 2f * s));
        }

        /// <summary>
        /// 折れ線に沿って球を並べる。離すと数珠に見えるので区間ごとに刻む。
        /// side に -1 を渡すと左右を返す。牙のもう一方に使う。
        /// </summary>
        private void TuskerChain(GameObject root, float s, Vector3[] path, float[] radii, float side)
        {
            for (var i = 0; i < path.Length - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                TuskerBone(root, s,
                    new Vector3(a.x, a.y, a.z * side),
                    new Vector3(b.x, b.y, b.z * side),
                    radii[i], radii[i + 1]);
            }
        }

        /// <summary>
        /// 試作の座標で、2点のあいだに1本の骨（カプセル）を渡す。
        ///
        /// **球を並べると数珠に見える。** 試作は距離関数どうしを滑らかにつないで
        /// いるので、脚も鼻も牙も一続きに見える。球の列で真似ると、粒の輪郭が
        /// そのまま出て、つながって見えない。実際そうなった。
        /// カプセルなら両端が丸いので、節どうしの継ぎ目も埋まる。
        /// </summary>
        private void TuskerBone(GameObject root, float s, Vector3 a, Vector3 b, float ra, float rb)
        {
            var p0 = new Vector3(a.z * s, a.y * s, a.x * s);
            var p1 = new Vector3(b.z * s, b.y * s, b.x * s);
            var dir = p1 - p0;
            var length = dir.magnitude;
            if (length < 0.0001f)
            {
                return;
            }

            // 太さは両端の平均で取る。半径2つぶんが直径にあたる。
            var thickness = (ra + rb) * s;

            // カプセルは丸い端を含めて高さ2なので、縦の拡大率は長さの半分。
            // 太いものが短いと端の丸みで潰れるので、下限を太さに合わせる。
            var piece = Piece(root, PrimitiveType.Capsule, "Creature", (p0 + p1) * 0.5f,
                new Vector3(thickness, Mathf.Max(length * 0.5f, thickness * 0.5f), thickness));
            piece.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);
        }

        /// <summary>腰から足先まで、太さの変わる脚を1本通す。</summary>
        private void TuskerLeg(GameObject root, float s, Vector3 hip, Vector3 foot)
        {
            // 膝で一度折る。まっすぐ通すと柱にしか見えない。
            var knee = Vector3.Lerp(hip, foot, 0.5f) + new Vector3(0.06f, 0f, 0f);
            TuskerBone(root, s, hip, knee, 0.42f, 0.34f);
            TuskerBone(root, s, knee, foot, 0.34f, 0.30f);
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
        /// <summary>
        /// インポスター用の板。**法線を真上へ向けてある。**
        ///
        /// 板はいつもカメラを向くので、普通の板だと法線もいつもカメラを向く。
        /// すると明るさは光の前後の成分だけで決まり、横から当てたとたんに
        /// どの木も真っ黒になる。実際そうなった。
        ///
        /// 焼いた絵にはすでに陰影が入っているので、ここでは向きによらず
        /// 平らに照らすのが正しい。法線を上へ向けると、明るさは太陽の高さだけで
        /// 決まる。朝夕は暗く、昼は明るく、横を向いても変わらない。
        /// </summary>
        private static Mesh UpFacingQuad()
        {
            if (upQuadMesh != null)
            {
                return upQuadMesh;
            }

            upQuadMesh = new Mesh();
            upQuadMesh.name = "SurfaceImpostorQuad";
            upQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
            };
            upQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
            };
            upQuadMesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            upQuadMesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            upQuadMesh.RecalculateBounds();
            upQuadMesh.hideFlags = HideFlags.DontSave;
            return upQuadMesh;
        }

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
