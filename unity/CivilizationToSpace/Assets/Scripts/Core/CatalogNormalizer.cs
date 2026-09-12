using System.Collections.Generic;
using CivilizationToSpace.Core.Json;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 時代データの検証と正規化。site/app.js の normalizeVisual / normalizeEra / normalizeCatalog と
    /// 判定を1対1に対応させる。値の意味づけを実装ごとにずらさないためである。
    ///
    /// このクラスが持つ定数は、対応schemaVersion、必須件数、JSONのキー名だけである。
    /// 時代のID・名称・年代ラベル・解説・視覚値を書き写さない。
    /// </summary>
    public static class CatalogNormalizer
    {
        private static readonly string[] SupportedSchemaVersions = { "1.0.0" };
        private const int RequiredEraCount = 6;

        private static readonly string[] RatioKeys =
        {
            "oceanLevel", "cloudDensity", "iceCoverage", "vegetation", "volcanicActivity", "cityLights"
        };

        private static readonly string[] ColorKeys = { "earthColor", "emissionColor" };

        private static readonly string[] RequiredEraTextKeys =
        {
            "id", "displayName", "rangeLabel", "summary", "status"
        };

        public static EraCatalog Normalize(JsonValue raw)
        {
            if (raw == null || !raw.IsObject)
            {
                throw new CatalogDataException("時代データの形式が正しくありません。");
            }

            var schemaVersion = raw.Member("schemaVersion");
            if (!JsonRules.IsText(schemaVersion) ||
                !IsSupportedSchemaVersion(JsonRules.JsTrim(schemaVersion.StringValue)))
            {
                // ブラウザは「このモック」と表示する。Unity側は実装名だけを言い換え、判定条件は同じである。
                throw new CatalogDataException("この実装が対応していないデータ形式です。");
            }

            var catalogTitle = raw.Member("catalogTitle");
            var disclaimer = raw.Member("disclaimer");
            if (!JsonRules.IsText(catalogTitle) || !JsonRules.IsText(disclaimer))
            {
                throw new CatalogDataException("カタログの見出しまたは注意書きがありません。");
            }

            var rawEras = raw.Member("eras");
            if (rawEras == null || !rawEras.IsArray || rawEras.Items.Count != RequiredEraCount)
            {
                throw new CatalogDataException(
                    "時代は" + RequiredEraCount + "件必要ですが、件数が一致しません。");
            }

            var eras = new List<EraData>(RequiredEraCount);
            for (var i = 0; i < rawEras.Items.Count; i++)
            {
                eras.Add(NormalizeEra(rawEras.Items[i], i));
            }

            var ids = new HashSet<string>();
            foreach (var era in eras)
            {
                if (!ids.Add(era.Id))
                {
                    throw new CatalogDataException("時代のIDが重複しています。");
                }
            }

            var orders = new HashSet<double>();
            foreach (var era in eras)
            {
                if (!orders.Add(era.SortOrder))
                {
                    throw new CatalogDataException("時代の並び順が重複しています。");
                }
            }

            eras.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            for (var i = 0; i < eras.Count; i++)
            {
                if (eras[i].SortOrder != i + 1)
                {
                    throw new CatalogDataException(
                        "時代の並び順が1から" + RequiredEraCount + "の連番になっていません。");
                }
            }

            // eraOrderは必須6 IDの明細。存在する場合は順序まで一致することを求める。
            var rawOrder = raw.Member("eraOrder");
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

                if (expected.Count != eras.Count)
                {
                    throw new CatalogDataException("時代のIDまたは並び順が想定と一致しません。");
                }

                for (var i = 0; i < expected.Count; i++)
                {
                    if (expected[i] != eras[i].Id)
                    {
                        throw new CatalogDataException("時代のIDまたは並び順が想定と一致しません。");
                    }
                }
            }

            return new EraCatalog(
                JsonRules.JsTrim(catalogTitle.StringValue),
                JsonRules.JsTrim(disclaimer.StringValue),
                JsonRules.TextOrEmpty(raw.Member("parameterNote")),
                eras);
        }

        private static bool IsSupportedSchemaVersion(string value)
        {
            // 完全一致だけを受理する。majorが同じなら受理するといった緩い規則を採らない。
            foreach (var supported in SupportedSchemaVersions)
            {
                if (supported == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static EraData NormalizeEra(JsonValue raw, int position)
        {
            var where = (position + 1) + "番目の時代";
            if (raw == null || !raw.IsObject)
            {
                throw new CatalogDataException(where + "のデータ形式が正しくありません。");
            }

            foreach (var key in RequiredEraTextKeys)
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

            var rawVisual = raw.Member("visual");
            if (rawVisual == null || !rawVisual.IsObject)
            {
                throw new CatalogDataException(where + "の視覚情報がありません。");
            }

            var presentation = raw.Member("presentation");
            if (presentation == null || !presentation.IsObject)
            {
                throw new CatalogDataException(where + "の表示情報がありません。");
            }

            var rawEvents = raw.Member("events");
            if (rawEvents == null || !rawEvents.IsArray)
            {
                throw new CatalogDataException(where + "のイベント情報の形式が正しくありません。");
            }

            bool degraded;
            var visual = NormalizeVisual(rawVisual, out degraded);

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

            // R1-P1では読み取り専用の候補表示のみ。選択・分岐ロジックは実装しない。
            var scenarios = new List<EraScenario>();
            var candidates = presentation.Member("scenarioCandidates");
            if (candidates != null && candidates.IsArray)
            {
                foreach (var item in candidates.Items)
                {
                    if (item == null || !item.IsObject)
                    {
                        continue;
                    }

                    var name = item.Member("name");
                    var assumption = item.Member("assumption");
                    if (JsonRules.IsText(name) && JsonRules.IsText(assumption))
                    {
                        scenarios.Add(new EraScenario(
                            JsonRules.JsTrim(name.StringValue),
                            JsonRules.JsTrim(assumption.StringValue)));
                    }
                }
            }

            var events = new List<string>();
            foreach (var item in rawEvents.Items)
            {
                if (JsonRules.IsText(item))
                {
                    events.Add(JsonRules.JsTrim(item.StringValue));
                }
            }

            return new EraData(
                JsonRules.JsTrim(raw.Member("id").StringValue),
                JsonRules.JsTrim(raw.Member("displayName").StringValue),
                JsonRules.JsTrim(raw.Member("rangeLabel").StringValue),
                sortOrder.NumberValue,
                JsonRules.JsTrim(raw.Member("summary").StringValue),
                JsonRules.JsTrim(raw.Member("status").StringValue),
                visual,
                degraded,
                JsonRules.TextOrEmpty(presentation.Member("caption")),
                tags,
                JsonRules.TextOrEmpty(presentation.Member("futureNote")),
                scenarios,
                events);
        }

        /// <summary>
        /// 視覚パラメータの欠損・型不正・範囲外は、その時代だけ中立値へ置換して読み込みを継続する。
        /// 全体エラーにしない。
        /// </summary>
        private static EraVisual NormalizeVisual(JsonValue source, out bool degraded)
        {
            var neutral = EraVisual.CreateNeutral();
            var visual = EraVisual.CreateNeutral();
            degraded = false;

            foreach (var key in ColorKeys)
            {
                var value = source.Member(key);
                if (JsonRules.IsColor(value))
                {
                    SetColor(visual, key, JsonRules.JsTrim(value.StringValue));
                }
                else
                {
                    SetColor(visual, key, GetColor(neutral, key));
                    degraded = true;
                }
            }

            foreach (var key in RatioKeys)
            {
                var value = source.Member(key);
                if (JsonRules.IsRatio(value))
                {
                    SetRatio(visual, key, value.NumberValue);
                }
                else
                {
                    SetRatio(visual, key, GetRatio(neutral, key));
                    degraded = true;
                }
            }

            var satelliteCount = source.Member("satelliteCount");
            if (JsonRules.IsCount(satelliteCount))
            {
                visual.SatelliteCount = satelliteCount.NumberValue;
            }
            else
            {
                visual.SatelliteCount = neutral.SatelliteCount;
                degraded = true;
            }

            return visual;
        }

        private static void SetColor(EraVisual visual, string key, string value)
        {
            switch (key)
            {
                case "earthColor":
                    visual.EarthColor = value;
                    return;
                case "emissionColor":
                    visual.EmissionColor = value;
                    return;
            }
        }

        private static string GetColor(EraVisual visual, string key)
        {
            switch (key)
            {
                case "earthColor":
                    return visual.EarthColor;
                case "emissionColor":
                    return visual.EmissionColor;
                default:
                    return null;
            }
        }

        private static void SetRatio(EraVisual visual, string key, double value)
        {
            switch (key)
            {
                case "oceanLevel":
                    visual.OceanLevel = value;
                    return;
                case "cloudDensity":
                    visual.CloudDensity = value;
                    return;
                case "iceCoverage":
                    visual.IceCoverage = value;
                    return;
                case "vegetation":
                    visual.Vegetation = value;
                    return;
                case "volcanicActivity":
                    visual.VolcanicActivity = value;
                    return;
                case "cityLights":
                    visual.CityLights = value;
                    return;
            }
        }

        private static double GetRatio(EraVisual visual, string key)
        {
            switch (key)
            {
                case "oceanLevel":
                    return visual.OceanLevel;
                case "cloudDensity":
                    return visual.CloudDensity;
                case "iceCoverage":
                    return visual.IceCoverage;
                case "vegetation":
                    return visual.Vegetation;
                case "volcanicActivity":
                    return visual.VolcanicActivity;
                case "cityLights":
                    return visual.CityLights;
                default:
                    return 0d;
            }
        }
    }
}
