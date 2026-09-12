using System;
using System.Text.RegularExpressions;
using CivilizationToSpace.Core.Json;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 値の受理規則。site/app.js の isText / isRatio / isColor / isCount と1対1に対応させる。
    /// 判定を実装ごとにずらさないため、規則はこのクラスにだけ置く。
    /// </summary>
    public static class JsonRules
    {
        /// <summary>
        /// CSSとして有効な16進色の桁数だけを受け付ける。5桁・7桁は無効な色なので弾く。
        /// 終端は \z を使う。.NET の $ は末尾の改行の前にも一致してしまい、JavaScript の $ と挙動が違う。
        /// </summary>
        private static readonly Regex ColorPattern =
            new Regex(@"^#(?:[0-9a-fA-F]{3,4}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})\z", RegexOptions.CultureInvariant);

        /// <summary>
        /// JavaScript の String.prototype.trim と同じ範囲を落とす。
        /// .NET の Trim は U+FEFF を空白と見なさないため、明示的に加える。
        /// </summary>
        public static string JsTrim(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            var start = 0;
            var end = value.Length;
            while (start < end && IsJsWhitespace(value[start]))
            {
                start++;
            }

            while (end > start && IsJsWhitespace(value[end - 1]))
            {
                end--;
            }

            return value.Substring(start, end - start);
        }

        private static bool IsJsWhitespace(char c)
        {
            return char.IsWhiteSpace(c) || c == '\uFEFF';
        }

        /// <summary>文字列であり、前後の空白を落として空にならない。</summary>
        public static bool IsText(JsonValue value)
        {
            return value != null && value.IsString && JsTrim(value.StringValue).Length > 0;
        }

        /// <summary>有限の数値で 0 以上 1 以下。</summary>
        public static bool IsRatio(JsonValue value)
        {
            if (value == null || !value.IsNumber)
            {
                return false;
            }

            var number = value.NumberValue;
            return !double.IsNaN(number) && !double.IsInfinity(number) && number >= 0d && number <= 1d;
        }

        /// <summary>文字列であり、16進色として有効。</summary>
        public static bool IsColor(JsonValue value)
        {
            return IsText(value) && ColorPattern.IsMatch(JsTrim(value.StringValue));
        }

        /// <summary>0 以上の整数。JavaScript の Number.isInteger に合わせ、桁数の上限は設けない。</summary>
        public static bool IsCount(JsonValue value)
        {
            if (value == null || !value.IsNumber)
            {
                return false;
            }

            var number = value.NumberValue;
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                return false;
            }

            return Math.Floor(number) == number && number >= 0d;
        }

        /// <summary>前後の空白を落とした文字列。文字列でなければ空文字を返す。</summary>
        public static string TextOrEmpty(JsonValue value)
        {
            return IsText(value) ? JsTrim(value.StringValue) : string.Empty;
        }
    }
}
