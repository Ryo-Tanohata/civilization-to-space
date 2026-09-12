using System.Collections.Generic;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 日本語表示方式（UG-07）の実体。
    ///
    /// TextMeshProは導入しない。uGUIの Text へ日本語フォントを割り当てる。
    ///
    /// 探す順序は3段。
    ///   1. 同梱フォント（Resources/Fonts/NotoSansJP-Subset）
    ///   2. OSの日本語フォント
    ///   3. 内蔵フォント（日本語は出ない）
    ///
    /// 同梱を1番目に置いているのは、WebGLにOSのフォントが存在しないためである。
    /// 以前は2番から始めていたため、WebGLビルドでは必ず3番へ落ち、
    /// 日本語が1文字も出ずボタンがすべて空の四角になっていた。
    ///
    /// 同梱フォントは表示に必要な文字だけを抜き出した約460KBのもので、
    /// 出典・ライセンス・改変内容・作り直し方は Assets/Resources/Fonts/NOTICE.md にある。
    /// </summary>
    public static class JapaneseFont
    {
        /// <summary>同梱フォントの置き場所。Resources から見た相対パスで拡張子は付けない。</summary>
        private const string BundledPath = "Fonts/NotoSansJP-Subset";

        /// <summary>OSのフォントを使うときに上から順に試す。Windowsの標準的な日本語フォント。</summary>
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

        /// <summary>内蔵フォントへ落ちたかどうか。真のとき日本語が出ない。</summary>
        public static bool FellBackToBuiltin { get; private set; }

        /// <summary>同梱フォントを使っているかどうか。WebGLではこれが真になる。</summary>
        public static bool UsingBundled { get; private set; }

        public static Font Get()
        {
            if (cached != null)
            {
                return cached;
            }

            var bundled = Resources.Load<Font>(BundledPath);
            if (bundled != null)
            {
                cached = bundled;
                resolvedName = bundled.name + "（同梱）";
                FellBackToBuiltin = false;
                UsingBundled = true;
                return cached;
            }

            // 同梱が見つからないのは、Resources から外れたときだけである。
            // その場合でもEditorとWindowsでは動くように、OSのフォントを試す。
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
                resolvedName = name + "（OS）";
                FellBackToBuiltin = false;
                UsingBundled = false;
                return cached;
            }

            cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            resolvedName = cached != null ? cached.name : "(なし)";
            FellBackToBuiltin = true;
            UsingBundled = false;
            return cached;
        }
    }
}
