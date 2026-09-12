using System;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 時代データが受理規則を満たさないことを示す全体エラー。
    /// UserMessage だけが利用者へ見せてよい文言である。内部パス・例外全文・スタックトレースを含めない。
    /// </summary>
    public sealed class CatalogDataException : Exception
    {
        public CatalogDataException(string userMessage)
            : base("era catalog validation failed")
        {
            UserMessage = userMessage;
        }

        public string UserMessage { get; }
    }
}
