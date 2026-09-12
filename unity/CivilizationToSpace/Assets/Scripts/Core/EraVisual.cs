namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 時代ごとの演出強度。0〜1 は無次元であり、描画側がUnity表現へ変換する。
    /// マテリアル名・シェーダプロパティ名・アセット参照をここへ持ち込まない。
    /// </summary>
    public sealed class EraVisual
    {
        public string EarthColor { get; internal set; }
        public string EmissionColor { get; internal set; }
        public double OceanLevel { get; internal set; }
        public double CloudDensity { get; internal set; }
        public double IceCoverage { get; internal set; }
        public double Vegetation { get; internal set; }
        public double VolcanicActivity { get; internal set; }
        public double CityLights { get; internal set; }

        /// <summary>
        /// 個数。int ではなく double で持つ。
        /// 受理規則が桁数に上限を設けないため、int では壊れたデータで溢れる。
        /// 実際に何基描くかは描画側が持ち数で抑える。
        /// </summary>
        public double SatelliteCount { get; internal set; }

        /// <summary>視覚パラメータが欠損したときに使う中立の地球。いずれの時代も表さない。</summary>
        public static EraVisual CreateNeutral()
        {
            return new EraVisual
            {
                EarthColor = "#697887",
                EmissionColor = "#384b60",
                OceanLevel = 0.45d,
                CloudDensity = 0.2d,
                IceCoverage = 0.1d,
                Vegetation = 0.2d,
                VolcanicActivity = 0d,
                CityLights = 0d,
                SatelliteCount = 0d
            };
        }
    }
}
