using System.Collections.Generic;

namespace CivilizationToSpace.Core
{
    /// <summary>検証済みの6時代カタログ。時代は sortOrder の昇順で並ぶ。</summary>
    public sealed class EraCatalog
    {
        public EraCatalog(string title, string disclaimer, string parameterNote, IReadOnlyList<EraData> eras)
        {
            Title = title;
            Disclaimer = disclaimer;
            ParameterNote = parameterNote;
            Eras = eras;
        }

        public string Title { get; }
        public string Disclaimer { get; }
        public string ParameterNote { get; }
        public IReadOnlyList<EraData> Eras { get; }
    }
}
