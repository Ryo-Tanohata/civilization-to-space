using System.Collections.Generic;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 地球ができるまでのひとつの段階。
    ///
    /// これは仮説に基づく象徴表現である。月の成り立ちはジャイアントインパクト説を採っているが、
    /// 決着した事実ではない。値はすべて0〜1に正規化した演出強度であり、
    /// 質量・個数・速度・衝突エネルギーではない。
    /// </summary>
    public sealed class FormationStage
    {
        public FormationStage(
            string id,
            string displayName,
            double sortOrder,
            string summary,
            string status,
            string caption,
            IReadOnlyList<string> tags,
            double bodyScale,
            double swarm,
            double impactor,
            double debris,
            double moon)
        {
            Id = id;
            DisplayName = displayName;
            SortOrder = sortOrder;
            Summary = summary;
            Status = status;
            Caption = caption;
            Tags = tags;
            BodyScale = bodyScale;
            Swarm = swarm;
            Impactor = impactor;
            Debris = debris;
            Moon = moon;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public double SortOrder { get; }
        public string Summary { get; }
        public string Status { get; }
        public string Caption { get; }
        public IReadOnlyList<string> Tags { get; }

        /// <summary>育ちかけの塊の大きさ。1で完成した地球と同じ。</summary>
        public double BodyScale { get; }

        /// <summary>ぶつかってくる小さな天体の多さ。</summary>
        public double Swarm { get; }

        /// <summary>大きな天体が近づいているか。</summary>
        public double Impactor { get; }

        /// <summary>散らばった破片の多さ。</summary>
        public double Debris { get; }

        /// <summary>月が現れているか。</summary>
        public double Moon { get; }
    }

    /// <summary>検証済みの形成過程。段階は sortOrder の昇順で並ぶ。</summary>
    public sealed class EarthFormation
    {
        public EarthFormation(
            string title,
            string disclaimer,
            string parameterNote,
            string status,
            IReadOnlyList<FormationStage> stages)
        {
            Title = title;
            Disclaimer = disclaimer;
            ParameterNote = parameterNote;
            Status = status;
            Stages = stages;
        }

        public string Title { get; }
        public string Disclaimer { get; }
        public string ParameterNote { get; }
        public string Status { get; }
        public IReadOnlyList<FormationStage> Stages { get; }
    }
}
