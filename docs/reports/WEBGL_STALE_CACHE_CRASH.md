# 公開ビルドが起動時に落ちた件（memory access out of bounds）

対象：`unity/CivilizationToSpace/Assets/Editor/WebGlBuild.cs` と `site/unity/`
Unity：6000.4.8f1 / WebGL

## 症状

公開ページをスマートフォンのブラウザで開くと、読み込みの途中で落ちる。

```
RuntimeError: memory access out of bounds
  at wasm://wasm/07407fd6:wasm-function[50626]
  ...
  at Module._main / callMain / doRun / run
```

`_main` の中なので、起動の途中である。操作する前に落ちる。

## 原因

**古い `.data` と新しい `.wasm` が組み合わさって読み込まれていた。**

ビルドの成果物は名前が固定で、公開するたびに中身だけが変わっていた。

| ファイル | 名前 | 残り方 |
| --- | --- | --- |
| `unity.data.unityweb` | 固定 | `dataCaching` により IndexedDB へ保存され、再訪時に使い回される |
| `unity.wasm.unityweb` | 固定 | ブラウザのHTTPキャッシュに残る |
| `unity.framework.js.unityweb` | 固定 | 同上 |

IndexedDB の鍵になる `productVersion` は `"1.0"` のまま変えていない。
そのため、**中身が変わっても鍵もURLも変わらない。**

短い時間に3回続けて公開したため、どちらか片方だけが古いまま残る状況が生まれた。
データとコードは対で作られるので、食い違えばコードが期待する場所に無いものを読む。
結果が `memory access out of bounds` である。

手元の Chrome（デスクトップ・モバイル表示とも）では再現しなかった。
初めて読み込む環境ではキャッシュが無く、対がそろうためである。

## 対処

`PlayerSettings.WebGL.nameFilesAsHashes = true` にした。
ビルドの成果物が内容のハッシュで名付けられる。

```
Build/271a819a0ff8d9df050b867de41242ad.wasm.unityweb
Build/b0fa3e5106a439020ace4bcb7d34138e.data.unityweb
Build/dde40f44c0c54feb312e9a7e8a26a768.framework.js.unityweb
Build/a9b9f932cf4b95d6d8b157978358c150.loader.js
```

**中身が変われば URL も変わる。** 古いものと新しいものが混ざりようがない。
キャッシュも安全に効くようになる。

古い名前のファイルは残しても参照されないため、ビルド前に `site/unity/Build` を消す。

## この件で欠けていた確認

**ビルドが通ることは確かめたが、ブラウザで動かす確認をせずに公開していた。**
`BehaviourCheck` も `CaptureTool` もエディタ上で動くため、
WebGLビルド固有の問題は一切通らない。

以後は公開前に、ローカルで配信して実際に起動するところまで見る。

```
python -m http.server 8765 --bind 127.0.0.1   # site/ で実行
# ヘッドレスChromeで http://127.0.0.1:8765/unity/ を開き、
# canvas の生成・読み込みの完了・例外の有無を見る
```

今回の版はこの手順で、読み込み完了・例外なしを確認してから公開した。

## 利用者側で必要な操作

**古い index.html がキャッシュに残っていると、消したファイルを参照して404になる。**
一度リロードすれば新しい index.html を取りに行く。
直らない場合はサイトのデータを消すか、シークレットタブで開く。
