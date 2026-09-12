using System;

namespace CivilizationToSpace.Core.Json
{
    /// <summary>
    /// JSONの構文が壊れていることを示す。
    /// 本例外の内容は診断用であり、利用者向け画面へそのまま出さない。
    /// </summary>
    public sealed class JsonParseException : Exception
    {
        public JsonParseException(string message, int index)
            : base(message)
        {
            Index = index;
        }

        /// <summary>文字単位の位置。ファイルパスは持たない。</summary>
        public int Index { get; }
    }
}
