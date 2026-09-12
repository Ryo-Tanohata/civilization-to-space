using System.Collections.Generic;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 月への展開のひとつの段階。
    ///
    /// これは予測でも計画でもなく、仮想シナリオである。実現の可否・時期・確率を持たない。
    /// 値はすべて0〜1に正規化した演出強度であり、輸送回数・施設数・費用・人数ではない。
    /// </summary>
    public sealed class MoonPhase
    {
        public MoonPhase(
            string id,
            string displayName,
            double sortOrder,
            string summary,
            string status,
            string caption,
            IReadOnlyList<string> tags,
            double transfer,
            double facility,
            double surfaceLights,
            double orbitStation,
            double lagrangeColony)
        {
            Id = id;
            DisplayName = displayName;
            SortOrder = sortOrder;
            Summary = summary;
            Status = status;
            Caption = caption;
            Tags = tags;
            Transfer = transfer;
            Facility = facility;
            SurfaceLights = surfaceLights;
            OrbitStation = orbitStation;
            LagrangeColony = lagrangeColony;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public double SortOrder { get; }
        public string Summary { get; }
        public string Status { get; }
        public string Caption { get; }
        public IReadOnlyList<string> Tags { get; }

        /// <summary>地球と月のあいだの行き来の多さ。</summary>
        public double Transfer { get; }

        /// <summary>月面の拠点の広がり。</summary>
        public double Facility { get; }

        /// <summary>月面の明かりの多さ。</summary>
        public double SurfaceLights { get; }

        /// <summary>
        /// 地球を回る拠点の育ち具合。0で無く、1で本来の大きさ。
        /// 月面の施設とは別のもので、月へ向かう前から置かれる。
        /// </summary>
        public double OrbitStation { get; }

        /// <summary>
        /// ラグランジュ点（L4・L5）に置くコロニーの見せ方の強さ。
        /// 寸法・人数・建設量を表さない。欠けている段階では0になる。
        /// </summary>
        public double LagrangeColony { get; }
    }

    /// <summary>検証済みの月への展開。段階は sortOrder の昇順で並ぶ。</summary>
    public sealed class MoonExpansion
    {
        public MoonExpansion(
            string title,
            string disclaimer,
            string parameterNote,
            string status,
            IReadOnlyList<MoonPhase> phases)
        {
            Title = title;
            Disclaimer = disclaimer;
            ParameterNote = parameterNote;
            Status = status;
            Phases = phases;
        }

        public string Title { get; }
        public string Disclaimer { get; }
        public string ParameterNote { get; }
        public string Status { get; }
        public IReadOnlyList<MoonPhase> Phases { get; }
    }
}
