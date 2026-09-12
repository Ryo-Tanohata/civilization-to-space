using System.Collections.Generic;

namespace CivilizationToSpace.Core
{
    /// <summary>Futureの将来シナリオ候補。R1-P1では読み取り専用の表示に留める。</summary>
    public sealed class EraScenario
    {
        public EraScenario(string name, string assumption)
        {
            Name = name;
            Assumption = assumption;
        }

        public string Name { get; }
        public string Assumption { get; }
    }

    /// <summary>
    /// 1時代分の検証済みデータ。
    /// 値の正本は site/data/earth-eras.json のみであり、ここには既定値も代替文も持たない。
    /// 絵コンテのカット番号（presentation.storyboardCut）は読み込まない。画面へ露出させないためである。
    /// </summary>
    public sealed class EraData
    {
        public EraData(
            string id,
            string displayName,
            string rangeLabel,
            double sortOrder,
            string summary,
            string status,
            EraVisual visual,
            bool visualDegraded,
            string caption,
            IReadOnlyList<string> tags,
            string futureNote,
            IReadOnlyList<EraScenario> scenarios,
            IReadOnlyList<string> events)
        {
            Id = id;
            DisplayName = displayName;
            RangeLabel = rangeLabel;
            SortOrder = sortOrder;
            Summary = summary;
            Status = status;
            Visual = visual;
            VisualDegraded = visualDegraded;
            Caption = caption;
            Tags = tags;
            FutureNote = futureNote;
            Scenarios = scenarios;
            Events = events;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string RangeLabel { get; }

        /// <summary>並び順。受理規則が桁数に上限を設けないため double で持つ。</summary>
        public double SortOrder { get; }

        public string Summary { get; }
        public string Status { get; }
        public EraVisual Visual { get; }

        /// <summary>視覚パラメータの一部を中立値へ置き換えたかどうか。真のとき簡略表示の注記を出す。</summary>
        public bool VisualDegraded { get; }

        public string Caption { get; }
        public IReadOnlyList<string> Tags { get; }
        public string FutureNote { get; }
        public IReadOnlyList<EraScenario> Scenarios { get; }
        public IReadOnlyList<string> Events { get; }
    }
}
