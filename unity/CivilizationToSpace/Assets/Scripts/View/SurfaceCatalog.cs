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
            land.Ground = Hex(0x1E1512);
            land.GroundGlow = Hex(0x8A2A08);
            land.Rock = Hex(0x16100E);
            land.Rocks = 70;

            // 巨大衝突。大きく、ゆっくり落ちる。実際の大きさも速さも表していない。
            land.ImpactorSize = 46f;
            land.Impactor = Hex(0xFF9A5A);
            land.ImpactSeconds = 26f;
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
            land.Ground = Hex(0x5E5434);
            land.Foliage = Hex(0x3E6E34);
            land.PlantHeight = 30f;
            land.Conifers = 40;
            land.Ferns = 44;
            land.Broadleaves = 10;
            land.Quadrupeds = 0;
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
            land.PlantHeight = 26f;
            land.Conifers = 34;
            land.Ferns = 46;
            land.Broadleaves = 22;
            land.Quadrupeds = 3;
            land.Bipeds = 4;
            land.DeadTrunks = 0;
            land.Buildings = 0;
            return land;
        }

        /// <summary>
        /// 衝突と暗い空。塵で日光がさえぎられ、昼でも暗い。
        /// 葉を持つ木を出さず、枯れた幹だけを残す。
        /// </summary>
        private static SurfaceView.Landscape Impact()
        {
            var land = Near(6);
            land.SkyHigh = Hex(0x231C18);
            land.SkyLow = Hex(0x54453A);
            land.Ground = Hex(0x39322C);
            land.Trunk = Hex(0x2E261F);
            land.PlantHeight = 22f;

            // 白亜紀の終わりの衝突。小さく速い。実際の大きさも速さも表していない。
            land.ImpactorSize = 7f;
            land.Impactor = Hex(0xFFD08A);
            land.ImpactSeconds = 14f;
            land.Conifers = 0;
            land.Ferns = 0;
            land.Broadleaves = 0;
            land.Quadrupeds = 0;
            land.Bipeds = 0;
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
            land.Ground = Hex(0x8A8468);
            land.Foliage = Hex(0x6E7748);
            land.Creature = Hex(0x6B5540);
            land.Rock = Hex(0x7E7868);
            land.Rocks = 26;
            land.PlantHeight = 9f;
            land.Conifers = 16;
            land.Ferns = 150;
            land.Broadleaves = 0;
            land.Quadrupeds = 3;
            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 0;
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
            land.Conifers = 18;
            land.Ferns = 26;
            land.Broadleaves = 20;
            land.Quadrupeds = 0;
            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 46;
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
            land.RocketSeconds = 12f;
            land.Conifers = 16;
            land.Ferns = 0;
            land.Broadleaves = 26;
            land.Quadrupeds = 0;
            land.Bipeds = 0;
            land.DeadTrunks = 0;
            land.Buildings = 150;
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
            land.RocketSeconds = 9f;
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
                position = new Vector3(0f, 5.5f, -30f);
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
