using System;
using System.Collections.Generic;
using System.IO;
using CivilizationToSpace.Core.Json;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 地球ができるまでの読込と検証。
    ///
    /// 転送契約は時代データと同じである。正本は site/data/earth-formation.json のみで、
    /// Unityは StreamingAssets のバイト同一コピーだけを読む。外部通信を行わない。
    /// 受理規則も <see cref="JsonRules"/> をそのまま使い、判定を実装ごとにずらさない。
    /// </summary>
    public static class EarthFormationLoader
    {
        public const string FileName = "earth-formation.json";

        private static readonly string[] SupportedSchemaVersions = { "formation-1.0.0" };

        private const int RequiredStageCount = 4;

        public sealed class Result
        {
            private Result(bool ok, EarthFormation formation, string userMessage, string sha256)
            {
                Ok = ok;
                Formation = formation;
                UserMessage = userMessage;
                Sha256 = sha256;
            }

            public bool Ok { get; }
            public EarthFormation Formation { get; }
            public string UserMessage { get; }
            public string Sha256 { get; }

            public static Result Success(EarthFormation formation, string sha256)
            {
                return new Result(true, formation, string.Empty, sha256);
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
                    "形成過程のファイルを取得できませんでした。StreamingAssets に " + FileName + " があるか確認してください。",
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
                return Result.Failure("形成過程のファイル形式が正しくありません。", sha256);
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
                return Result.Failure("形成過程の内容を確認できませんでした。", sha256);
            }
        }

        private static EarthFormation Normalize(JsonValue raw)
        {
            if (raw == null || !raw.IsObject)
            {
                throw new CatalogDataException("形成過程のデータ形式が正しくありません。");
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
                throw new CatalogDataException("形成過程の見出しまたは注意書きがありません。");
            }

            var rawStages = raw.Member("stages");
            if (rawStages == null || !rawStages.IsArray || rawStages.Items.Count != RequiredStageCount)
            {
                throw new CatalogDataException(
                    "段階は" + RequiredStageCount + "件必要ですが、件数が一致しません。");
            }

            var stages = new List<FormationStage>(RequiredStageCount);
            for (var i = 0; i < rawStages.Items.Count; i++)
            {
                stages.Add(NormalizeStage(rawStages.Items[i], i));
            }

            var ids = new HashSet<string>();
            foreach (var stage in stages)
            {
                if (!ids.Add(stage.Id))
                {
                    throw new CatalogDataException("段階のIDが重複しています。");
                }
            }

            stages.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            for (var i = 0; i < stages.Count; i++)
            {
                if (stages[i].SortOrder != i + 1)
                {
                    throw new CatalogDataException(
                        "段階の並び順が1から" + RequiredStageCount + "の連番になっていません。");
                }
            }

            var rawOrder = raw.Member("stageOrder");
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

                if (expected.Count != stages.Count)
                {
                    throw new CatalogDataException("段階のIDまたは並び順が想定と一致しません。");
                }

                for (var i = 0; i < expected.Count; i++)
                {
                    if (expected[i] != stages[i].Id)
                    {
                        throw new CatalogDataException("段階のIDまたは並び順が想定と一致しません。");
                    }
                }
            }

            return new EarthFormation(
                JsonRules.JsTrim(title.StringValue),
                JsonRules.JsTrim(disclaimer.StringValue),
                JsonRules.TextOrEmpty(raw.Member("parameterNote")),
                JsonRules.TextOrEmpty(raw.Member("status")),
                stages);
        }

        private static FormationStage NormalizeStage(JsonValue raw, int position)
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

            return new FormationStage(
                JsonRules.JsTrim(raw.Member("id").StringValue),
                JsonRules.JsTrim(raw.Member("displayName").StringValue),
                sortOrder.NumberValue,
                JsonRules.JsTrim(raw.Member("summary").StringValue),
                JsonRules.JsTrim(raw.Member("status").StringValue),
                JsonRules.TextOrEmpty(presentation.Member("caption")),
                tags,
                Ratio(visual.Member("bodyScale")),
                Ratio(visual.Member("swarm")),
                Ratio(visual.Member("impactor")),
                Ratio(visual.Member("debris")),
                Ratio(visual.Member("moon")));
        }

        /// <summary>比率は欠損・型不正・範囲外を0へ寄せる。時代データの縮退と同じ考え方である。</summary>
        private static double Ratio(JsonValue value)
        {
            return JsonRules.IsRatio(value) ? value.NumberValue : 0d;
        }
    }
}
