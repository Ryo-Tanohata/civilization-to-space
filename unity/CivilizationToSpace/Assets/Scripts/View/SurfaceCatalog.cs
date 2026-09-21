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
            switch (Mathf.Clamp(eraIndex, 0, 9))
            {
                case 0: return Hadean();
                case 1: return EarlyOcean();
                case 2: return Snowball();
                case 3: return GreenEarth();
                case 4: return Dinosaurs();
                case 5: return Impact();
                case 6: return IceAge();
                case 7: return Humans();
                case 8: return Information();
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
            switch (Mathf.Clamp(eraIndex, 0, 9))
            {
                case 3:  // 森林と陸上生態系
                case 4:  // 巨大生物の時代
                case 5:  // 衝突と暗い空
                case 6:  // 氷期のくり返し
                case 7:  // 人類の広がり
                case 8:  // 情報と接続
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
            return Mathf.Clamp(eraIndex, 0, 9) >= 7 ? Framing.Settlement : Framing.Creatures;
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
            land.Rocks = 80;
            land.GroundRubble = 90;
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
            // 貼り物であることが見えてしまう。雪をかぶった枯れ草の色にする。
            land.Ground = Hex(0xB2AE99);
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
            land.Backdrop = "backdrop_snow_steppe";
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
            land.PlantHeight = 16f;
            land.BuildingHeight = 52f;
            land.BuildingSpacing = 2.1f;
            land.Windows = true;

            // 衛星が上がり始めた時代。地表からも打ち上げが見える。
            land.ShowsRocket = true;
            land.Conifers = 20;
            land.Ferns = 0;
            land.Broadleaves = 32;
            land.GroundCover = 60;
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
            land.BuildingHeight = 78f;
            land.BuildingSpacing = 2.0f;
            land.ShowsRocket = true;
            land.Buildings = 210;
            land.Broadleaves = 40;
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

            // 建物が高いほど、離れて高いところから見ないと並びが入らない。
            if (land.BuildingHeight < 20f)
            {
                position = new Vector3(0f, 8f, -20f);
                pitch = 2f;
                return;
            }

            position = new Vector3(0f, 58f, -200f);
            pitch = 4f;
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
