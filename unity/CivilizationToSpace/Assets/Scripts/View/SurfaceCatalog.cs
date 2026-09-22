using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 時代ごとの、地表から見た風景の値。
    ///
    /// 決めごとの根拠は docs/design/SURFACE_SCENE_SPEC.md にある。
    /// 調べた出典もそこに書いてある。ここは表であって、判断は書かない。
    ///
    /// **時代ごとの専用コードを書かないための表である。**
    /// 新しい時代を足すときは、この表へ1行足すだけで済むようにする。
    /// 形の語彙（針葉樹・シダ・広葉樹・枯れた幹・四つ足・二足・建物）は増やさない。
    ///
    /// いずれは時代データ（earth-eras.json）へ移したい。いまはまだ、
    /// 交換データの形式と2つの読込を変える段階にないので、ここに置いてある。
    /// </summary>
    public static class SurfaceCatalog
    {
        /// <summary>寄り方。どちらで見るかは時代ごとに決まる。</summary>
        public enum Framing
        {
            /// <summary>生きものが画面に収まる寄り。</summary>
            Creatures,

            /// <summary>街が画面に収まる寄り。</summary>
            Settlement,
        }

        /// <summary>時代の並び順（0が最初）から風景を返す。範囲外は端で止める。</summary>
        public static SurfaceView.Landscape ForEra(int eraIndex)
        {
            switch (Mathf.Clamp(eraIndex, 0, 10))
            {
                case 0: return Hadean();
                case 1: return EarlyOcean();
                case 2: return Snowball();
                case 3: return GreenEarth();
                case 4: return Dinosaurs();
                case 5: return Impact();
                case 6: return IceAge();
                case 7: return Humans();
                case 8: return Industrial();
                case 9: return Information();
                default: return Future();
            }
        }

        /// <summary>
        /// その時代を、はじめから地表で見せるか。
        ///
        /// **時代によって、見るべき場所が違う。**
        /// 森ができたこと、巨大な生きものがいたこと、空が塵で暗くなったこと、
        /// 大きな木が無い氷期の原、家が寄り集まった集落。これらは地表に降りないと
        /// 何も分からない。宇宙から見ても緑や白の色が変わるだけである。
        ///
        /// 逆に、地球ができること、海が地球を覆うこと、全球が凍ること、
        /// 衛星が上がって地球規模につながること、月へ出ていくことは、
        /// 地球全体が見えていないと分からない。
        ///
        /// どちらの時代でも、ボタンでもう一方へ移れる。ここで決めるのは最初の一枚である。
        /// </summary>
        public static bool DefaultsToSurface(int eraIndex)
        {
            switch (Mathf.Clamp(eraIndex, 0, 10))
            {
                case 3:  // 森林と陸上生態系
                case 4:  // 巨大生物の時代
                case 5:  // 衝突と暗い空
                case 6:  // 氷期のくり返し
                case 7:  // 人類の広がり
                case 8:  // 産業と機械
                case 9:  // 情報と接続
                    // **打ち上げは地上の出来事である。**
                    // 宇宙を既定にしていたとき、ロケットは地表にしか出ないため
                    // 一度も見られなかった。街から機体が上がるところを既定にし、
                    // 地球ぜんたいを見たいときは切り替えて見てもらう。
                    // 地球を離れたあと（未来・月・ラグランジュ）は宇宙のままにする。
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>その時代をどちらの寄りで見るか。</summary>
        public static Framing FramingForEra(int eraIndex)
        {
            return Mathf.Clamp(eraIndex, 0, 10) >= 7 ? Framing.Settlement : Framing.Creatures;
        }

        /// <summary>生きもの寄りの共通の置き場所。</summary>
        private static SurfaceView.Landscape Near(int seed)
        {
            return new SurfaceView.Landscape
            {
                NearZ = 7f,
                FarZ = 260f,
                HalfWidth = 95f,
                PlantHeight = 24f,
                BuildingSpacing = 1.05f,
                GroundGlow = Color.black,
                Trunk = Hex(0x5B4632),
                Foliage = Hex(0x4C7A3A),
                Creature = Hex(0x8A7A55),
                Building = Hex(0xB9BCC0),
                Window = Hex(0x6E8FA8),
                Rock = Hex(0x6A625A),
                Impactor = Hex(0xFFB070),
                Rocket = Hex(0xE8ECF0),
                Flame = Hex(0xFFB24A),

                // **空を1色の帯で終わらせない。**
                // 上下のぼかしだけでは、どこまでが空でどこからが遠景か分からず、
                // 板を1枚立てたように見えていた。
                // 雲の層・太陽・重なる稜線の3つで奥行きを出す。
                // 起伏。平らな板だと地面が床にしか見えない。
                // 置くものは同じ高さの式を通すので、斜面でも浮かない。
                Relief = 0.085f,
                ReliefDetail = 1f,
                CloudCover = 0.75f,
                ShowsSun = true,
                Ridges = 3,
                RidgeHeight = 0.30f,
                Seed = seed,
            };
        }

        /// <summary>街寄りの共通の置き場所。</summary>
        private static SurfaceView.Landscape Town(int seed)
        {
            var land = Near(seed);
            land.NearZ = 12f;
            land.FarZ = 110f;
            land.HalfWidth = 46f;
            land.PlantHeight = 9f;
            return land;
        }

        /// <summary>マグマの地球。空の色は決めであって、分かっている値ではない。</summary>
        private static SurfaceView.Landscape Hadean()
        {
            var land = Near(1);
            land.SkyHigh = Hex(0x2A1008);
            land.SkyLow = Hex(0x7A2A10);
            // **地面は暗いままにし、光るほうで熱を出す。**
            // 明るい色にすると、粒の絵と起伏の影が消えて平らな板に見える。
            // 暗い岩に、割れ目から漏れる熱の色を重ねるほうが溶けて見える。
            land.Ground = Hex(0x241611);
            land.GroundGlow = Hex(0xA33A10);
            land.Rock = Hex(0x16100E);
            land.Rocks = 70;
            land.GroundRubble = 90;

            // 巨大衝突。大きく、ゆっくり落ちる。実際の大きさも速さも表していない。
            land.ImpactorSize = 46f;
            land.Impactor = Hex(0xFF9A5A);
            land.ShowsImpact = true;
            land.PlantHeight = 16f;
            land.Conifers = 0;
            land.Ferns = 0;
            land.Broadleaves = 0;
            land.Quadrupeds = 0;
            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 0;
            return land;
        }

        /// <summary>
        /// 海の誕生。**空を青くしない。**
        /// 大酸化事変より前の大気には遊離した酸素がほぼ無かった。
        /// </summary>
        private static SurfaceView.Landscape EarlyOcean()
        {
            var land = Near(2);
            land.SkyHigh = Hex(0x6B5A4A);
            land.SkyLow = Hex(0xB8A188);
            land.Ground = Hex(0x4A443E);
            land.Rock = Hex(0x5E564E);

            // **水を張る。** この時代に水が無く、地面と空の色だけで
            // マグマの時代と区別していた。時代の名が示すものが画面に無かった。
            //
            // 水位を与えると、尾根が陸として残り谷が沈む。海岸線は勝手にできる。
            // 1.2mにすると、目の前は浅い水で、遠くの尾根が島として残る。
            // 濁った緑がかった色にする。初期の海は澄んでいなかったはずだが、
            // 濁りは色で表し、透明度では表さない。
            land.WaterLevel = 1.2f;
            land.Water = Hex(0x33474A);

            // 岩は減らす。多くは水に沈んで見えないので、置くだけ無駄になる。
            land.Rocks = 34;
            land.GroundRubble = 20;
            land.PlantHeight = 14f;
            land.Conifers = 0;
            land.Ferns = 0;
            land.Broadleaves = 0;
            land.Quadrupeds = 0;
            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 0;
            return land;
        }

        /// <summary>全球凍結。一面の氷。</summary>
        private static SurfaceView.Landscape Snowball()
        {
            var land = Near(3);
            land.SkyHigh = Hex(0x7FA6C8);
            land.SkyLow = Hex(0xDCE8F0);
            land.Ground = Hex(0xD6E2EA);
            land.Rock = Hex(0xAFC4D2);
            land.Rocks = 70;
            land.GroundRubble = 80;
            land.PlantHeight = 15f;
            land.Conifers = 0;
            land.Ferns = 0;
            land.Broadleaves = 0;
            land.Quadrupeds = 0;
            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 0;
            return land;
        }

        /// <summary>
        /// 森林と陸上生態系。高い木を中心に置く。**草原にしない。**
        /// 陸上の生きものは出さない。
        /// </summary>
        private static SurfaceView.Landscape GreenEarth()
        {
            var land = Near(4);
            land.SkyHigh = Hex(0x5C8FC8);
            land.SkyLow = Hex(0xCBDCE6);
            // 遠景の林床は下草の緑なので、手前もそちらへ寄せる。
            land.Ground = Hex(0x55552F);
            land.Foliage = Hex(0x3E6E34);
            land.PlantHeight = 30f;
            land.Conifers = 48;
            land.Ferns = 56;
            land.Broadleaves = 14;
            land.GroundCover = 120;

            // 遠景は写真。**これは現在の針葉樹林である。** 石炭紀の森でも
            // 白亜紀の森でもない。表しているのは「木が密に立ち並ぶ奥行き」だけ。
            land.Backdrop = "backdrop_forest";

            // **陸上生態系の時代に、生きものが1匹もいなかった。**
            // 木と下生えだけの森で、時代の名が示すものが画面に無かった。
            // 低く這う四肢動物を木の下へ撒く。脚を体の横へ張り出した姿なので、
            // あとの時代の獣とは見間違えない。
            //
            // **数を多くする。** 5匹では、奥行き260m・幅190mの野に散ってしまい、
            // 画面に1匹も入らないことが実際に起きた。撒く範囲は変えず数で埋める。
            // 背丈は場面から出さず、人と同じく固定する（理由は SurfaceView 側）。
            land.Quadrupeds = 16;
            land.EarlyTetrapods = true;

            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 0;
            return land;
        }

        /// <summary>
        /// 巨大生物の時代。**地面を草原にしない。**
        /// 草が広い面積を覆うのは中新世以降である。
        /// </summary>
        private static SurfaceView.Landscape Dinosaurs()
        {
            var land = Near(5);
            land.SkyHigh = Hex(0x5C8FC8);
            land.SkyLow = Hex(0xCBDCE6);
            land.Ground = Hex(0x6E5C3E);

            // **獣の色を地面から離す。** 既定の砂色のままだと、乾いた野に
            // 同じ色の獣が立ち、輪郭が地面に溶けて数も形も読めなかった。
            // 緑がかった暗い色にすると、砂の上でも遠景の丘の上でも影絵になる。
            // 復元色ではない。読みやすさのための決めである。
            land.Creature = Hex(0x5C6146);
            land.PlantHeight = 26f;
            land.Conifers = 42;
            land.Ferns = 58;
            land.Broadleaves = 28;
            land.GroundCover = 110;

            // **遠景は写真。** 乾いた低木地と、その向こうの丘。
            // 白亜紀の写真は存在しない。**これは現在の南アフリカである。**
            // 表しているのは「乾いた広い野と、遠くの丘」という形だけで、
            // 白亜紀の植生でも地形でもない。
            land.Backdrop = "backdrop_dry_scrub";
            land.Quadrupeds = 3;
            land.Bipeds = 4;

            // 生きものは素材から採る。ここだけで立てる旗である。
            land.HornedBeasts = true;
            land.DeadTrunks = 0;
            land.Buildings = 0;

            // 首と尾の長い大きな四つ足。全長25mほど、肩の高さ4.5mほど。
            // **素材に合わせて下げた。** 15は、首の長い獣を手組みしていたころの
            // 値である。素材の角のあるものと背板のあるものは背が低く長いので、
            // 同じ値を求めると全長30メートルを超えた。
            land.CreatureHeight = 6.5f;
            land.CreatureBody = 0.72f;
            land.CreatureLegs = 0.30f;
            land.CreatureNeck = 1f;
            land.CreatureTail = 1f;
            land.CreatureTusks = false;
            return land;
        }

        /// <summary>
        /// 衝突。**恐竜のいる世界から始め、落ちた瞬間に枯れて暗くなる。**
        ///
        /// はじめ、この場面は最初から枯れた幹と暗い空で作っていた。
        /// 隕石がまだ空にあるのに地上はすでに死んでおり、
        /// **原因より先に結果が出ていた。** 恐竜→隕石→氷期という順に
        /// つながって見えず、見ている側には順番が入れ替わって映る。
        ///
        /// そこで、落ちる前は前の時代と同じ姿にしている。
        /// 同じ森、同じ生きもの、同じ寄り方である。変わるのは落ちたあとだけ。
        /// </summary>
        private static SurfaceView.Landscape Impact()
        {
            // 落ちる前は恐竜の時代そのもの。種だけ変えて並びをずらす。
            var land = Dinosaurs();
            land.Seed = 6;

            // 草は枯れて消えるので、枯れたあとの手前に残るものを足しておく。
            // 何も無い地面に枯れ幹だけが並ぶと、遠くの帯にしか見えない。
            land.GroundRubble = 60;

            // 白亜紀の終わりの衝突。小さく速い。実際の大きさも速さも表していない。
            land.ImpactorSize = 7f;
            land.Impactor = Hex(0xFFD08A);
            land.ShowsImpact = true;

            // 落ちたあと。塵で日光がさえぎられ、昼でも暗い。
            // 葉を持つ木は消え、枯れた幹だけが残る。
            land.DiesOnImpact = true;
            land.AfterSkyHigh = Hex(0x231C18);
            land.AfterSkyLow = Hex(0x54453A);
            land.AfterGround = Hex(0x39322C);

            // そのあと「衝突の冬」へ移る。
            //
            // 地面を白くしているのは Brugger ほか (2017, Geophysical Research
            // Letters) による。世界の年平均気温が少なくとも26℃下がり、
            // 年平均が氷点下の年が3年ほど（3〜16年）続き、**氷冠が広がった。**
            // 気候がもどるのに30年あまりかかった。
            // 降る塵は Senel ほか (2023, Nature Geoscience) による。細かい
            // ケイ酸塩の塵が大気中に15年とどまり、光合成は2年ちかく止まった。
            //
            // **何が主に冷やしたのかは文献で分かれている。** 前者は硫酸エアロゾル、
            // 後者は細かい塵を重く見る。画面の降るものはどちらかを主張していない。
            //
            // **これは数年から数十年の出来事であり、約259万年前から続く
            // 第四紀の氷期（次の時代）とは別のものである。**
            // 年数も気温も画面では表していない。
            land.WinterSkyHigh = Hex(0x2E3740);
            land.WinterSkyLow = Hex(0x818C96);
            land.WinterGround = Hex(0x9AA0A4);

            // 降ってくるもの。舞い上がった塵から、凍る白へ移す。
            land.AshFlakes = 420;
            land.AshEarly = Hex(0x9E8F82);
            land.AshLate = Hex(0xFFFFFF);
            land.DeadTrunk = Hex(0x2E261F);
            land.DeadTrunks = 46;
            land.Buildings = 0;
            return land;
        }

        /// <summary>
        /// 氷期のくり返し。**大きな木を出さない。**
        /// マンモスステップには大きな木がほとんど無く、苔も乏しかった。
        /// 草本と低木、まばらな矮性の木だけである。
        /// </summary>
        private static SurfaceView.Landscape IceAge()
        {
            var land = Near(7);
            land.SkyHigh = Hex(0x6E93B6);
            land.SkyLow = Hex(0xD4DFE6);
            // **遠景の雪と手前の地面を近づける。**
            // 遠くが雪の野で手前が乾いた土のままだと、境目で色が切れて
            // 貼り物であることが見えてしまう。
            //
            // **写真の雪の色を、そのまま反射率に使う。** 以前は雪をかぶった
            // 枯れ草の色（#B2AE99）を置いていたが、届いていなかった。測ると、
            // 地平線をまたぐ明度の段差が103あり（写真側114／3D側217）、
            // 画面の6.8%が純白へ飛んでいた。原因は2つで、どちらも反射率にある。
            //
            // 1. **明るすぎた。** #B2AE99 は明度173。真昼の白い太陽（強度1.25）と
            //    空からの環境光を掛けると 1.6 倍ほどになり、天井を超える。
            // 2. **色が逆だった。** #B2AE99 は青が赤より25低い暖色。写真の雪原は
            //    青が赤より21高い寒色である。雪の陰影は空の青でできているので、
            //    暖色の反射率では、隣に貼った写真と色温度が合わない。
            //
            // 写真（backdrop_snow_steppe）の雪原を測ると RGB(152,157,173)、明度157。
            // その色相のまま、照明の倍率で割った明度（157 / 1.6 ≒ 114）へ落とす。
            // 照り返し（Ground × 0.45）も寒色になるので、獣の陰の側にも空の色が回る。
            land.Ground = Hex(0x6E727D);
            land.Foliage = Hex(0x6E7748);
            land.Creature = Hex(0x6B5540);
            land.Rock = Hex(0x7E7868);

            // **手前をうめるのは石である。** 下の草木をすべて0にしたので、
            // 石を増やさないと手前が雪の帯だけになり、起伏も光の向きも読めない。
            // マグマ・最初の海・全球凍結と同じ理由で、同じくらいの数を置く。
            land.Rocks = 70;

            // **野の広さは草木の背丈に合わせる。**
            // 260m先まで並べていたときは、9mの草木がすべて地平線上の粒になり、
            // 画面が空と地面だけになった。恐竜の時代の比（奥行きは背丈の10倍ほど）に
            // そろえて、奥行きと幅を詰める。
            land.NearZ = 5f;
            land.FarZ = 130f;
            land.HalfWidth = 52f;
            land.PlantHeight = 9f;

            // **雪の野に緑を置かない。**
            // 針葉樹・シダ・下生えは、どれも写真計測の素材をそのまま貼るため、
            // 時代ごとの色で塗り直せない（自前の絵を持っているので守られる）。
            // 結果、雪の上に緑の葉が散り、針葉樹の役には乾燥地のアロエの木
            // （quiver tree）が立っていた。どちらも氷期の野には合わない。
            //
            // **置き換える立木が無い。** 遠景の写真は「雪におおわれた平らな野と、
            // まばらな裸の木」だが、手前に立てられる葉の無い木の素材が無い。
            // 枯れた幹で代えようとしたが、あれは倒木の絵で立たない（下記）。
            // いまは草木を置かず、石と獣だけにする。
            land.Conifers = 0;
            land.Ferns = 0;
            land.Broadleaves = 0;
            land.GroundCover = 0;
            land.Quadrupeds = 5;
            land.Bipeds = 0;

            // **首の長い四つ足を立たせてはいけない。**
            // それらは白亜紀の終わり（約6600万年前）に絶滅しており、
            // 氷期はその6400万年あとである。
            // ここでは毛のある大型の草食獣にならい、首と尾を短く、
            // 胴を太く、肩を高く取る。肩の高さ3.2mほど。
            land.CreatureHeight = 3.6f;
            land.CreatureBody = 0.95f;
            land.CreatureLegs = 0.45f;
            land.CreatureNeck = 0.14f;
            land.CreatureTail = 0.16f;
            land.CreatureTusks = true;

            // **枯れた幹は使えない。** 素材の `imp_dead_trunk` は
            // 横たわった倒木を焼いた絵であり、立ち枯れた木ではない。
            // 立てると板が宙に浮いて横たわり、枯れ木には見えなかった。
            // 葉の落ちた立木の素材が要る。いまは置かない。
            land.DeadTrunks = 0;
            land.Buildings = 0;

            // 遠景は写真。**これは現在の雪の野である。** 氷期の景観ではない。
            // 表しているのは「雪におおわれた平らな野と、まばらな裸の木」だけ。
            // **木の無い雪原へ替える。** これまでの snowy_field は地平線が
            // 裸の木で埋まっており、3D側の草木をすべて0にして「氷期の原に
            // 大きな木が無い」と表しているそばから、背景がそれを否定していた。
            // Poly Haven の snow_field_2（CC0）は開けた雪原で、木は地平線の
            // 細い帯に下がる。電線の写る snow_field は避けた。
            land.Backdrop = "backdrop_snow_plain";
            return land;
        }

        /// <summary>
        /// 人類の広がり。**平らな屋根、隙間なく背中合わせ、道なし、窓なし。**
        /// チャタルホユックの姿にならっている。
        /// </summary>
        private static SurfaceView.Landscape Humans()
        {
            var land = Town(8);
            land.SkyHigh = Hex(0x6796C6);
            land.SkyLow = Hex(0xD8E4EA);
            land.Ground = Hex(0x6A6B3C);
            land.Building = Hex(0xC2A882);
            land.BuildingHeight = 5f;
            land.BuildingSpacing = 1.05f;
            land.Windows = false;

            // Society 2.0（農耕）の初期。屋根から梯子で出入りし、家のあいだに
            // 道が無い。壁に戸口は開かず、屋根に穴を開ける。
            land.RoofOpening = true;

            // 踏み固めた土。石畳ではない。農耕の集落なので舗装はまだ無い。
            land.Pavement = Hex(0x8A7A5C);

            // **蓄えと囲い。** 農耕を狩猟から分けるのはこの2つである。
            // 高床倉庫は集落の縁に置く。中に混ぜると家に紛れる。
            land.Granaries = 6;
            land.Palisade = true;

            land.Conifers = 24;
            land.Ferns = 34;
            land.Broadleaves = 26;
            land.GroundCover = 84;
            land.Quadrupeds = 0;

            // **人類の広がりに人がいなかった。** 家だけが並ぶ野になっていた。
            // 集落のまわりに立たせる。顔も服も作らない（仕様の決めごと9）。
            land.Bipeds = 7;
            land.People = true;

            // **肌の色は地面の色から作る。** 色を選ぶこと自体が、どの集団かの
            // 表明になってしまう。地面（0x6A6B3C）を白へ35%寄せた値にしてある。
            // 既定の暗い色のままでは、草地の上で黒い影にしか見えなかった。
            land.Creature = Hex(0x9E9F80);

            land.DeadTrunks = 0;
            land.Buildings = 46;

            // 遠景は写真。**これは現在の草地である。** アナトリアでもない。
            // 表しているのは「草地と、その向こうの林」だけ。
            land.Backdrop = "backdrop_meadow";
            return land;
        }

        /// <summary>
        /// 産業と機械（Society 3.0）。
        ///
        /// **煙突と、同じ形の反復。** この段階を他から分けるのはこの2つである。
        /// 動力が人と家畜から機械へ移ったことは煙突で、働く人が都市へ集まった
        /// ことは同じ形の住まいが並ぶ姿で表す。
        ///
        /// 集落（2.0）より大きく、情報の都市（4.0）より低い。窓は開けるが、
        /// ガラスの格子ではないので帯は細く少なくする。
        ///
        /// **年代・地域・産業の種類は表していない。**
        /// </summary>
        private static SurfaceView.Landscape Industrial()
        {
            var land = Town(9);

            // **空から青を抜く。** 以前は #74808C を置いていたが、これは青が赤より
            // 24高い「青みを帯びた灰」である。遠景写真の青空（青が赤より63高い）へ
            // 煙霧で寄せても、青と青みがかった灰を混ぜることになり、測ると+49で
            // 青が残った。煙で濁った空は色味を失うので、色相を抜いた灰にする。
            // 明度は125のまま変えない。暗くするのではなく、色を抜くのが煙である。
            land.SkyHigh = Hex(0x7C7C78);
            land.SkyLow = Hex(0xC8C2B4);

            // **空を濁らせる。** 煙が出る時代なので、青くは晴れない。
            land.Ground = Hex(0x5E5A42);
            land.Building = Hex(0x8E6F58);
            land.Window = Hex(0x3E3A34);

            land.NearZ = 40f;
            land.FarZ = 340f;
            land.HalfWidth = 150f;
            land.PlantHeight = 12f;

            land.BuildingHeight = 14f;
            land.BuildingSpacing = 1.35f;
            land.Windows = true;
            land.Buildings = 96;

            // 地面は均す。街は均した土地に建つ。
            land.Relief = 0f;

            // 煉瓦と石炭の煤。灰色の舗装にはまだならない。
            land.Pavement = Hex(0x6E6459);
            land.Plinth = true;
            land.RoofCap = false;

            // **煙突を立てる。** この時代の印になる。
            land.Chimneys = 14;

            // **煙霧を掛ける。** 上で灰色の空（#74808C→#C8C2B4）を指定しているが、
            // 遠景に写真を敷くとその色は使われず、画面には写真の青空が出ていた。
            // 測ると空の明度は223で、他の時代と変わらない。指定した灰色が
            // 一度も見えていなかったことになる。
            //
            // 煙突を立てても青空のままでは、動力が機械へ移ったことが
            // 煙突の形だけに頼ることになる。空を寄せれば、形と色の両方で読める。
            //
            // 0.55は、遠くの丘が霞んで残る濃さである。1にすると写真が消えて
            // 丘も失われ、煙ではなく単色の壁になる。
            land.Smog = 0.55f;

            land.Conifers = 10;
            land.Ferns = 0;
            land.Broadleaves = 14;
            land.GroundCover = 260;
            land.Quadrupeds = 0;

            // 働く人。集落より多い。
            land.Bipeds = 12;
            land.People = true;
            land.Creature = Hex(0x8E8874);

            land.DeadTrunks = 0;
            land.Rocks = 0;
            land.GroundRubble = 40;
            land.ShowsRocket = false;
            land.Backdrop = "backdrop_wide_plain";
            return land;
        }

        /// <summary>情報・地球規模接続。高い建物が道を挟んで並ぶ。</summary>
        private static SurfaceView.Landscape Information()
        {
            var land = Town(9);
            land.SkyHigh = Hex(0x6F9AC8);
            land.SkyLow = Hex(0xDDE7EC);
            land.Ground = Hex(0x515C46);
            land.Building = Hex(0xB6BABF);
            land.Window = Hex(0x5F87A6);
            land.NearZ = 110f;
            land.FarZ = 900f;
            land.HalfWidth = 330f;

            // **都市の地面は平らにする。**
            // 起伏のある地面に舗装を敷くと、1枚109mの板が地形をまたいで
            // 段差を作った。街は均した土地に建つものなので、起伏を外す。
            // 集落（升5m）は起伏のままでよい。板が小さいので段が出ない。
            land.Relief = 0f;
            land.PlantHeight = 16f;
            land.BuildingHeight = 52f;
            land.BuildingSpacing = 2.1f;
            land.Windows = true;

            // Society 4.0（情報）。窓の格子と、屋上の設備。足もとに一段置く。
            land.Plinth = true;
            land.RoofCap = true;

            // 灰色の舗装。道と広場がここで出る。
            land.Pavement = Hex(0x8E9094);

            // 衛星が上がり始めた時代。地表からも打ち上げが見える。
            land.ShowsRocket = true;
            land.Conifers = 20;
            land.Ferns = 0;
            land.Broadleaves = 32;
            // 目の高さから見るので、手前の草を増やす。少ないと地面が
            // 一様な面に見える。
            land.GroundCover = 520;
            land.Quadrupeds = 0;
            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 150;

            // 遠景は写真。**これは現在の南アフリカの高地である。**
            // 高いところから見下ろす広い野。街はその中に置かれる。
            // 実在の都市でも、実在の地形でもない。
            // Future() はこれを受け継ぐので、未来の街も同じ野に立つ。
            land.Backdrop = "backdrop_wide_plain";
            return land;
        }

        /// <summary>未来の分岐。**仮想シナリオ案である。** 高さと数だけを上げる。</summary>
        private static SurfaceView.Landscape Future()
        {
            var land = Information();
            land.Seed = 10;
            land.Building = Hex(0xC4CBD2);
            land.Window = Hex(0x74C8B4);

            // **Society 5.0 は、4.0 を大きくした姿ではない。**
            // これまでは情報の都市を高く（52m→78m）、多く（150→210棟）して
            // いただけで、画面では「同じ街の背が伸びた」以上のことが起きて
            // いなかった。絵コンテでも9と10が見分けにくいと記録していた。
            //
            // 内閣府の定義は「サイバー空間とフィジカル空間を高度に融合させた
            // システムにより、経済発展と社会的課題の解決を両立する人間中心の
            // 社会」であり、建物の形式で定義された段階ではない。
            // 地表に出せる違いは、建物そのものより**配置**にある。
            //
            // | 要点 | 画面での表し方 |
            // | --- | --- |
            // | 大きな中心が消える | 背を情報より低くする（78m→30m） |
            // | 緑と建物が混ざる | 棟を減らし（210→110）、木を増やす（40→150） |
            // | 足もとが空く | 間隔を広げる（2.0→3.1） |
            // | 供給が分散する | 屋根いちめんを発電面の色でおおう |
            land.BuildingHeight = 30f;
            land.BuildingSpacing = 3.1f;
            land.Buildings = 110;
            land.Broadleaves = 150;
            land.Conifers = 70;
            land.GroundCover = 140;
            land.RoofPanel = Hex(0x2E4A5E);

            // **舗装を明るく、緑を残す。** 5.0 は道が細く足もとが空く。
            // 一面を灰色にすると4.0と同じ街になるので、明るい色にして
            // 草地との差を小さくする。
            land.Pavement = Hex(0xA8A9A0);
            land.RoofCap = false;

            land.ShowsRocket = true;
            return land;
        }

        /// <summary>その時代を見るカメラの置き場所と、見下ろす角度。</summary>
        public static void EyeForEra(int eraIndex, out Vector3 position, out float pitch)
        {
            var land = ForEra(eraIndex);

            if (FramingForEra(eraIndex) == Framing.Creatures)
            {
                // **主役が草木よりずっと低い時代は、主役に合わせて近づく。**
                // 氷期の草木は9m、獣は肩の高さ3mほどで、草木に合わせて置くと
                // 獣が地平線上の粒になる。恐竜（獣15m・草木26m）はここに入らない。
                if (land.CreatureHeight > 0f && land.CreatureHeight * 2f < land.PlantHeight)
                {
                    position = new Vector3(0f, land.CreatureHeight * 0.5f, -land.CreatureHeight * 1.2f);
                    pitch = -1f;
                    return;
                }

                // **寄り方は時代の背丈に合わせる。**
                // 固定の距離にしていたときは、背の低い時代（氷期の草木は9m）で
                // すべてが地平線の上の小さな粒になり、画面が空と地面だけになった。
                // 恐竜の時代（26m）でちょうど良かった比をそのまま使う。
                var tallest = Mathf.Max(8f, Mathf.Max(land.PlantHeight, land.CreatureHeight * 1.8f));
                position = new Vector3(0f, tallest * 0.212f, -tallest * 1.154f);
                pitch = -2.5f;
                return;
            }

            // **目の高さは人の高さにする。**
            //
            // 集落を目の高さ8m・20m手前から2度見下ろし、都市を58m・200m手前から
            // 4度見下ろしていた。どちらも建物より高いところから見下ろす構図で、
            // **机の上の模型を眺めている絵**になっていた。街を見ているのに
            // 現実味が出ないのは、置いてあるものではなく視点のせいだった。
            //
            // 人の目の高さから見ると、5mの家は見上げる高さになり、52mの塔は
            // 遠くにそびえる。同じものを置いていても、そこに立っている絵になる。
            //
            // **並びを画面に収めるのは距離で行う。** 高さで稼ぐと見下ろしになる。
            //
            // **建物の位置から逆算する。** 固定の座標に置いていたとき、集落は
            // 75m手前から眺めることになり、画面の6割が空の草地になった。
            // 建物は NearZ から3割4分のところに並ぶので、そこを基準に寄る。
            var frontZ = Mathf.Lerp(land.NearZ, land.FarZ, 0.34f);

            // 並びの幅。画角のおよそ半分（40度）に収まる距離を求める。
            var columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(land.Buildings * 1.6f)));
            var step = Mathf.Max(1f, land.BuildingHeight * Mathf.Max(0.4f, land.BuildingSpacing));
            var spread = (columns - 1) * step * 0.5f;

            // **近すぎても遠すぎてもいけない。**
            // 近いと手前の1棟しか見えず、遠いと地平線の粒になる。
            // 都市は並びが1km を越えるので、全部を入れようとせず途中で止める。
            var approach = Mathf.Clamp(spread * 1.19f, 26f, 260f);

            if (land.BuildingHeight < 20f)
            {
                position = new Vector3(0f, 1.8f, frontZ - approach);
                pitch = 0f;
                return;
            }

            // 都市は遠い。高さは変えず、距離で並びを入れる。
            // わずかに見上げることで、塔の高さが出る。
            position = new Vector3(0f, 2.0f, frontZ - approach);
            pitch = -1f;
        }

        private static Color Hex(uint value)
        {
            return new Color32(
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)(value & 0xFF),
                0xFF);
        }
    }
}
