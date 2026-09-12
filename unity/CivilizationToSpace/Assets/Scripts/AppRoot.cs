using System.IO;
using System.Text;
using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace
{
    /// <summary>
    /// R1-P1 S2の起点。StreamingAssets のコピーを読み、検証結果をコンソールへ出す。
    ///
    /// S2では描画と時代切替を行わない。画面表示はS3で日本語表示方式（UG-07）を決めてから足す。
    /// static も Singleton も作らない。R2-P1はこのコンポーネントの参照だけに依存する。
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        private CatalogLoadResult result;

        /// <summary>検証済みカタログ。読込に失敗した場合は null。</summary>
        public EraCatalog Catalog
        {
            get { return result != null ? result.Catalog : null; }
        }

        /// <summary>読込に失敗したかどうか。真のとき操作を無効化する。</summary>
        public bool LoadFailed
        {
            get { return result == null || !result.Ok; }
        }

        /// <summary>画面へ出してよい失敗の文言。成功時は空文字。</summary>
        public string FailureMessage
        {
            get { return result != null ? result.UserMessage : string.Empty; }
        }

        private void Awake()
        {
            Load();
        }

        private void Load()
        {
            // 絶対パスにはユーザー名が含まれる。本リポジトリはPublicであるため、パスを出力しない。
            var path = Path.Combine(Application.streamingAssetsPath, EraCatalogLoader.EraCatalogFileName);
            result = EraCatalogLoader.LoadFromFile(path);

            if (LoadFailed)
            {
                Debug.LogError("[EraCatalog] 読込に失敗しました: " + FailureMessage);
                return;
            }

            Debug.Log(BuildSummary(result));
        }

        private static string BuildSummary(CatalogLoadResult loaded)
        {
            var catalog = loaded.Catalog;
            var builder = new StringBuilder();
            builder.Append("[EraCatalog] SHA-256 ").Append(loaded.Sha256).Append('\n');
            builder.Append("件数 ").Append(catalog.Eras.Count).Append('\n');
            builder.Append("見出し ").Append(catalog.Title).Append('\n');

            for (var i = 0; i < catalog.Eras.Count; i++)
            {
                var era = catalog.Eras[i];
                builder.Append(i + 1).Append('/').Append(catalog.Eras.Count).Append(' ')
                    .Append(era.Id).Append(" | ").Append(era.DisplayName)
                    .Append(" | ").Append(era.RangeLabel)
                    .Append(" | sortOrder ").Append(era.SortOrder)
                    .Append(" | ").Append(era.Status)
                    .Append(" | タグ ").Append(era.Tags.Count)
                    .Append(" | 代表イベント ").Append(era.Events.Count)
                    .Append(" | 将来シナリオ候補 ").Append(era.Scenarios.Count)
                    .Append(era.VisualDegraded ? " | 視覚値を簡略表示へ縮退" : string.Empty)
                    .Append('\n');
            }

            return builder.ToString().TrimEnd('\n');
        }
    }
}
