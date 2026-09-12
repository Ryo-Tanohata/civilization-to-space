# 同梱フォントの出典と改変内容

## なぜ同梱するのか

WebGLビルドにはOSのフォントが存在しない。`JapaneseFont` はOSの日本語フォントを
動的に割り当てる方式のため、WebGLでは内蔵フォント（LegacyRuntime）へ落ち、
日本語が1文字も表示されなかった。ボタン15個がすべて空の四角になり、操作できない状態だった。

そのため、表示に必要な文字だけを抜き出した日本語フォントを同梱する。

## 置き場所

コードから `Resources.Load<Font>("Fonts/NotoSansJP-Subset")` で読むため、
`Assets/Resources/Fonts/` に置いている。ライセンス全文も同じ場所に置いてあり、
ビルド成果物の中へ一緒に入る。

## ファイル

| ファイル | 内容 |
| --- | --- |
| `Resources/Fonts/NotoSansJP-Subset.ttf` | Noto Sans JP から必要な文字だけを抜き出したもの（461KB） |
| `Resources/Fonts/OFL.txt` | SIL Open Font License 1.1 の全文と著作権表示 |

## 出典

- 元フォント：Noto Sans JP（可変フォント `NotoSansJP-VF.ttf`、バージョン 2.004）
- 著作権表示：`© 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source'.`
- ライセンス：SIL Open Font License, Version 1.1（`OFL.txt` に全文を同梱）
- ライセンス本文の取得元：<https://openfontlicense.org/documents/OFL.txt>

## 改変内容

1. 可変軸 `wght` を **400（Regular）** で固定した。このフォントの既定値は
   100（Thin）であり、そのまま固定すると画面の文字が細くて読めないためである。
2. 画面に出る文字だけを残した（1,064文字。うち漢字663文字）。
   元の 9,590,844 バイトから 461,112 バイト（4.8%）になった。
3. フォント名を `Noto Sans JP Subset` / `NotoSansJP-Subset` へ変更した。

OFL 1.1 の条項3が定める Reserved Font Name は `Source` であり、
改変後の名前はこれを含まない。条項2に従い、著作権表示とライセンス全文を同梱している。

## 残す文字の決め方

画面に出る文字の出どころは2つしかない。

1. `Assets/StreamingAssets/*.json`（時代・形成過程・月への展開のデータ）と
   正本である `site/data/*.json`
2. `Assets/Scripts/` と `Assets/Editor/` の C# ソース中の非ASCII文字

この2つを全走査したうえで、取りこぼし対策として
印字可能なASCII・ひらがな全域・カタカナ全域・全角英数記号・よく使う記号を足している。
**データをかな書きで書き換えた場合は必ず表示できる。**
新しい漢字を追加した場合は、フォントを作り直す必要がある。

## 作り直し方

抜き出しは `fontTools` で行っている。リポジトリにはツールも元フォントも含めていない。
作り直しの手順と文字の集め方は
`docs/reports/UNITY_WEBGL_FONT_FIX.md` に記録している。
