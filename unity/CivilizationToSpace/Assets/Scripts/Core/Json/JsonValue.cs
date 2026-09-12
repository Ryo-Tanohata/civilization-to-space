using System;
using System.Collections.Generic;

namespace CivilizationToSpace.Core.Json
{
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object
    }

    /// <summary>
    /// JSONの1値。キーの「欠損」と「値がnull」を区別できることが本クラスの存在理由である。
    /// UnityのJsonUtilityは両者を区別できず、視覚値の欠損と 0 を同一に扱ってしまうため採用しない。
    /// </summary>
    public sealed class JsonValue
    {
        private static readonly JsonValue[] NoItems = new JsonValue[0];

        private readonly bool boolValue;
        private readonly double numberValue;
        private readonly string stringValue;
        private readonly List<JsonValue> items;
        private readonly Dictionary<string, JsonValue> members;

        private JsonValue(JsonKind kind, bool b, double n, string s, List<JsonValue> a, Dictionary<string, JsonValue> o)
        {
            Kind = kind;
            boolValue = b;
            numberValue = n;
            stringValue = s;
            items = a;
            members = o;
        }

        public JsonKind Kind { get; }

        public static JsonValue Null { get; } = new JsonValue(JsonKind.Null, false, 0d, null, null, null);
        public static JsonValue True { get; } = new JsonValue(JsonKind.Bool, true, 0d, null, null, null);
        public static JsonValue False { get; } = new JsonValue(JsonKind.Bool, false, 0d, null, null, null);

        public static JsonValue FromNumber(double value)
        {
            return new JsonValue(JsonKind.Number, false, value, null, null, null);
        }

        public static JsonValue FromString(string value)
        {
            return new JsonValue(JsonKind.String, false, 0d, value ?? string.Empty, null, null);
        }

        public static JsonValue FromArray(List<JsonValue> value)
        {
            return new JsonValue(JsonKind.Array, false, 0d, null, value ?? new List<JsonValue>(), null);
        }

        public static JsonValue FromObject(Dictionary<string, JsonValue> value)
        {
            return new JsonValue(JsonKind.Object, false, 0d, null, null, value ?? new Dictionary<string, JsonValue>());
        }

        public bool IsObject => Kind == JsonKind.Object;
        public bool IsArray => Kind == JsonKind.Array;
        public bool IsString => Kind == JsonKind.String;
        public bool IsNumber => Kind == JsonKind.Number;

        public bool BoolValue => Kind == JsonKind.Bool ? boolValue : throw new InvalidOperationException("not a bool");
        public double NumberValue => Kind == JsonKind.Number ? numberValue : throw new InvalidOperationException("not a number");
        public string StringValue => Kind == JsonKind.String ? stringValue : throw new InvalidOperationException("not a string");

        /// <summary>配列の要素。配列でなければ空を返す。</summary>
        public IReadOnlyList<JsonValue> Items => Kind == JsonKind.Array ? (IReadOnlyList<JsonValue>)items : NoItems;

        /// <summary>オブジェクトのメンバ。オブジェクトでない場合とキーが無い場合はいずれも null を返す。</summary>
        public JsonValue Member(string name)
        {
            if (Kind != JsonKind.Object || name == null)
            {
                return null;
            }

            JsonValue found;
            return members.TryGetValue(name, out found) ? found : null;
        }

        public bool HasMember(string name)
        {
            return Kind == JsonKind.Object && name != null && members.ContainsKey(name);
        }
    }
}
