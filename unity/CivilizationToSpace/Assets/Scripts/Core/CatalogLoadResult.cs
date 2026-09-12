namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 読込の結果。失敗しても例外を投げ返さず、利用者へ見せてよい文言だけを持って返す。
    /// </summary>
    public sealed class CatalogLoadResult
    {
        private CatalogLoadResult(bool ok, EraCatalog catalog, string userMessage, string sha256)
        {
            Ok = ok;
            Catalog = catalog;
            UserMessage = userMessage;
            Sha256 = sha256;
        }

        public bool Ok { get; }

        /// <summary>成功時のみ非 null。</summary>
        public EraCatalog Catalog { get; }

        /// <summary>失敗時に画面へ出してよい文言。内部パスも例外全文も含まない。</summary>
        public string UserMessage { get; }

        /// <summary>読み込んだバイト列のSHA-256。読めなかった場合は空文字。</summary>
        public string Sha256 { get; }

        public static CatalogLoadResult Success(EraCatalog catalog, string sha256)
        {
            return new CatalogLoadResult(true, catalog, string.Empty, sha256);
        }

        public static CatalogLoadResult Failure(string userMessage, string sha256)
        {
            return new CatalogLoadResult(false, null, userMessage, sha256 ?? string.Empty);
        }
    }
}
