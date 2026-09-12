using System.Collections.Generic;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 日本語表示方式（UG-07）の実体。
    ///
    /// TextMeshProを導入せず、uGUIの Text へOSの日本語フォントを動的に割り当てる。
    /// フォントアトラスをリポジトリへ置かないため、容量も再配布ライセンスの問題も生じない。
    ///
    /// 既知の制限：OSに日本語フォントが無い環境では内蔵フォントへ落ち、日本語が出ない可能性がある。
    /// R1-P1の検証はWindowsのEditor Play Modeを正としており、その範囲では上の一覧が存在する。
    /// </summary>
    public static class JapaneseFont
    {
        /// <summary>上から順に試す。Windowsの標準的な日本語フォントを並べている。</summary>
        private static readonly string[] Preferred =
        {
            "Yu Gothic UI",
            "Yu Gothic",
            "Meiryo",
            "BIZ UDGothic",
            "Noto Sans JP",
            "MS Gothic"
        };

        private static Font cached;
        private static string resolvedName = "(未解決)";

        /// <summary>実際に採用したフォント名。実装報告へ記録するために公開する。</summary>
        public static string ResolvedName
        {
            get { return resolvedName; }
        }

        /// <summary>内蔵フォントへ落ちたかどうか。真のとき日本語が出ない可能性がある。</summary>
        public static bool FellBackToBuiltin { get; private set; }

        public static Font Get()
        {
            if (cached != null)
            {
                return cached;
            }

            var installed = new HashSet<string>(Font.GetOSInstalledFontNames());
            foreach (var name in Preferred)
            {
                if (!installed.Contains(name))
                {
                    continue;
                }

                var font = Font.CreateDynamicFontFromOSFont(name, 24);
                if (font == null)
                {
                    continue;
                }

                cached = font;
                resolvedName = name;
                FellBackToBuiltin = false;
                return cached;
            }

            cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resolvedName = cached != null ? cached.name : "(なし)";
            FellBackToBuiltin = true;
            return cached;
        }
    }
}
