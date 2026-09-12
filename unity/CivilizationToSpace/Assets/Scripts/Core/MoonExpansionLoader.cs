using System;
using System.Collections.Generic;
using System.IO;
using CivilizationToSpace.Core.Json;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 月への展開の読込と検証。
    ///
    /// 転送契約は時代データと同じである。正本は site/data/moon-expansion.json のみで、
    /// Unityは StreamingAssets のバイト同一コピーだけを読む。外部通信を行わない。
    /// 受理規則も <see cref="JsonRules"/> をそのまま使い、判定を実装ごとにずらさない。
    ///
    /// スキーマ系列は時代データと分ける。moon- の接頭辞は識別子であり、互換性の版ではない。
    /// </summary>
    public static class MoonExpansionLoader
    {
        public const string FileName = "moon-expansion.json";

        private static readonly string[] SupportedSchemaVersions = { "moon-1.0.0" };

        /// <summary>
        /// 段階の必須件数。地球を回る拠点の段階を先頭へ足したため、4から5になった。
        /// 件数を固定しているのは、段階の抜けを読み込み時に気づけるようにするためである。
        /// </summary>
        private const int RequiredPhaseCount = 5;

        public sealed class Result
        {
            private Result(bool ok, MoonExpansion expansion, string userMessage, string sha256)
            {
                Ok = ok;
                Expansion = expansion;
                UserMessage = userMessage;
                Sha256 = sha256;
            }

            public bool Ok { get; }
            public MoonExpansion Expansion { get; }
            public string UserMessage { get; }
            public string Sha256 { get; }

            public static Result Success(MoonExpansion expansion, string sha256)
            {
                return new Result(true, expansion, string.Empty, sha256);
            }

            public static Result Failure(string userMessage, string sha256)
            {
                return new Result(false, null, userMessage, sha256 ?? string.Empty);
            }
        }

        public static Result LoadFromFile(string path)
        {
            byte[] bytes;
            try
            {
                bytes = StreamingBytes.Read(path);
            }
            catch (Exception)
            {
                return Result.Failure(
                    "月への展開のファイルを取得できませんでした。StreamingAssets に " + FileName + " があるか確認してください。",
                    string.Empty);
            }

            var sha256 = EraCatalogLoader.ComputeSha256(bytes);

            JsonValue parsed;
            try
            {
                parsed = JsonParser.Parse(new System.Text.UTF8Encoding(false, true).GetString(bytes));
            }
            catch (Exception)
            {
                return Result.Failure("月への展開のファイル形式が正しくありません。", sha256);
            }

            try
            {
                return Result.Success(Normalize(parsed), sha256);
            }
            catch (CatalogDataException error)
            {
                return Result.Failure(error.UserMessage, sha256);
            }
            catch (Exception)
            {
                return Result.Failure("月への展開の内容を確認できませんでした。", sha256);
            }
        }

        private static MoonExpansion Normalize(JsonValue raw)
        {
            if (raw == null || !raw.IsObject)
            {
                throw new CatalogDataException("月への展開のデータ形式が正しくありません。");
            }

            var schemaVersion = raw.Member("schemaVersion");
            if (!JsonRules.IsText(schemaVersion) ||
                Array.IndexOf(SupportedSchemaVersions, JsonRules.JsTrim(schemaVersion.StringValue)) < 0)
            {
                throw new CatalogDataException("この実装が対応していないデータ形式です。");
            }

            var title = raw.Member("title");
            var disclaimer = raw.Member("disclaimer");
            if (!JsonRules.IsText(title) || !JsonRules.IsText(disclaimer))
            {
                throw new CatalogDataException("月への展開の見出しまたは注意書きがありません。");
            }

            var rawPhases = raw.Member("phases");
            if (rawPhases == null || !rawPhases.IsArray || rawPhases.Items.Count != RequiredPhaseCount)
            {
                throw new CatalogDataException(
                    "段階は" + RequiredPhaseCount + "件必要ですが、件数が一致しません。");
            }

            var phases = new List<MoonPhase>(RequiredPhaseCount);
            for (var i = 0; i < rawPhases.Items.Count; i++)
            {
                phases.Add(NormalizePhase(rawPhases.Items[i], i));
            }

            var ids = new HashSet<string>();
            foreach (var phase in phases)
            {
                if (!ids.Add(phase.Id))
                {
                    throw new CatalogDataException("段階のIDが重複しています。");
                }
            }

            phases.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            for (var i = 0; i < phases.Count; i++)
            {
                if (phases[i].SortOrder != i + 1)
                {
                    throw new CatalogDataException(
                        "段階の並び順が1から" + RequiredPhaseCount + "の連番になっていません。");
                }
            }

            // phaseOrder は必須IDの明細。存在する場合は順序まで一致することを求める。
            var rawOrder = raw.Member("phaseOrder");
            if (rawOrder != null && rawOrder.IsArray)
            {
                var expected = new List<string>();
                foreach (var item in rawOrder.Items)
                {
                    if (JsonRules.IsText(item))
                    {
                        expected.Add(JsonRules.JsTrim(item.StringValue));
                    }
                }

                if (expected.Count != phases.Count)
                {
                    throw new CatalogDataException("段階のIDまたは並び順が想定と一致しません。");
                }

                for (var i = 0; i < expected.Count; i++)
                {
                    if (expected[i] != phases[i].Id)
                    {
                        throw new CatalogDataException("段階のIDまたは並び順が想定と一致しません。");
                    }
                }
            }

            return new MoonExpansion(
                JsonRules.JsTrim(title.StringValue),
                JsonRules.JsTrim(disclaimer.StringValue),
                JsonRules.TextOrEmpty(raw.Member("parameterNote")),
                JsonRules.TextOrEmpty(raw.Member("status")),
                phases);
        }

        private static MoonPhase NormalizePhase(JsonValue raw, int position)
        {
            var where = (position + 1) + "番目の段階";
            if (raw == null || !raw.IsObject)
            {
                throw new CatalogDataException(where + "のデータ形式が正しくありません。");
            }

            foreach (var key in new[] { "id", "displayName", "summary", "status" })
            {
                if (!JsonRules.IsText(raw.Member(key)))
                {
                    throw new CatalogDataException(where + "に必須の情報がありません。");
                }
            }

            var sortOrder = raw.Member("sortOrder");
            if (!JsonRules.IsCount(sortOrder) || sortOrder.NumberValue < 1d)
            {
                throw new CatalogDataException(where + "の並び順が正しくありません。");
            }

            var visual = raw.Member("visual");
            if (visual == null || !visual.IsObject)
            {
                throw new CatalogDataException(where + "の視覚情報がありません。");
            }

            var presentation = raw.Member("presentation");
            if (presentation == null || !presentation.IsObject)
            {
                throw new CatalogDataException(where + "の表示情報がありません。");
            }

            var tags = new List<string>();
            var rawTags = presentation.Member("tags");
            if (rawTags != null && rawTags.IsArray)
            {
                foreach (var tag in rawTags.Items)
                {
                    if (JsonRules.IsText(tag))
                    {
                        tags.Add(JsonRules.JsTrim(tag.StringValue));
                    }
                }
            }

            return new MoonPhase(
                JsonRules.JsTrim(raw.Member("id").StringValue),
                JsonRules.JsTrim(raw.Member("displayName").StringValue),
                sortOrder.NumberValue,
                JsonRules.JsTrim(raw.Member("summary").StringValue),
                JsonRules.JsTrim(raw.Member("status").StringValue),
                JsonRules.TextOrEmpty(presentation.Member("caption")),
                tags,
                Ratio(visual.Member("transfer")),
                Ratio(visual.Member("facility")),
                Ratio(visual.Member("surfaceLights")),
                Ratio(visual.Member("orbitStation")));
        }

        /// <summary>比率は欠損・型不正・範囲外を0へ寄せる。時代データの縮退と同じ考え方である。</summary>
        private static double Ratio(JsonValue value)
        {
            return JsonRules.IsRatio(value) ? value.NumberValue : 0d;
        }
    }
}
