# ブラウザとUnityのデータ交換形式

状態：R1-P1の着手にあたり、未決だった共通データ交換形式を確定した文書。[BROWSER_MOCK_SPEC.md](BROWSER_MOCK_SPEC.md) は編集しないため、項目案との食い違いをここに残す。本書はUnityプロジェクト・C#ファイルを生成する許可ではない。

## 背景

[R2-P0レビュー報告](../reports/R2_P0_HUMAN_REVIEW.md) は、R1-P1へ進む前に必要なものとして「共通データ交換形式の方針決定」を挙げている。[P0ブラウザモック実装報告](../reports/P0_BROWSER_MOCK_IMPLEMENTATION.md) も「Unity移行データ形式の確定」をW6の前提としている。本書はこれに応えるものである。

## 交換契約を2層に分ける

交換形式という語を、次の2層に分けて定義する。

| 層 | 内容 | R1とR2で共通か |
|---|---|---|
| 第1層 転送契約 | 正本の所在、Unity側の読込経路、コピーの位置づけ、同一性の証明方法 | 共通とする。R2-P1も同じ経路に載せる |
| 第2層 スキーマ契約 | キー名、型、値域、検証規則 | 共通にしない。R2は `r2-` 系列のまま分離する |

`site/data/biosphere-scenarios.json` の `interchangeNote` は「このファイルはブラウザR2専用です。Unityとの共通交換形式の候補ではなく、R1の site/data/earth-eras.json とはスキーマ系列を分けています。」と述べている。これが否定しているのは第2層のスキーマ共通化であり、第1層の配置・同期・検証の共通化までは否定していない。本書はこの解釈を採る。

## 第1層 転送契約

| 規則 | 内容 |
|---|---|
| 正本 | `site/data/*.json` のみ。正本は1つであり、複製しても増えない |
| Unity側の読込 | `unity/CivilizationToSpace/Assets/StreamingAssets/` 配下のバイト同一コピーだけを読む |
| コピーの位置づけ | 派生物である。編集を禁止する。隣に `README-DO-NOT-EDIT.md` を置いて宣言する |
| 同一性の証明 | gitのblobハッシュ一致をもって証明する |
| 更新の向き | `site/data/` から `StreamingAssets/` への一方向のみ。逆向きの反映を行わない |
| 読込方式 | `File.ReadAllText` でUTF-8として読む。外部通信を行わない |

Gitはcontent-addressedであり、内容が同一なら2つのパスは同一blobを共有する。したがって次の2値が一致すれば、バイト同一であることと、コピーによる容量増が実質ゼロであることを同時に示せる。

```text
git rev-parse HEAD:site/data/earth-eras.json
git rev-parse HEAD:unity/CivilizationToSpace/Assets/StreamingAssets/earth-eras.json
```

照合はディスク上のファイルハッシュではなくblobハッシュを正とする。作業ツリーの改行コードは `core.autocrlf` の影響を受けうるが、blobは正規化後の内容であり環境差に左右されないためである。この前提を守るため、`unity/.gitattributes` では `*.json` を `text` として扱い、`-text`（バイナリ扱い）にしない。

### 同期ズレの検出

| タイミング | 実行者 | 手段 |
|---|---|---|
| コピー作成直後 | AI | `git diff --no-index` で差分なしを確認 |
| ステージング前 | AI | `git status --short` で意図外の差分がないことを確認 |
| コミット後 | AI | 上記2つの `git rev-parse` が同一SHAであることを確認 |
| Unity起動時 | Unity | 読み込んだJSONのSHA-256をコンソールへ出力する |
| 任意 | オーナー | Editorメニューから照合を実行する |

### 採用しなかった案

| 案 | 不採用の理由 |
|---|---|
| Unityから `site/data/` を相対パスで直読み | ビルドに含まれない。Editorでしか動かず、プロジェクト移動でパスが壊れる。R1全体DoDが求める起動手順が環境依存になる |
| リポジトリ直下に共通 `data/` を新設して正本を移す | `site/app.js` の読込先変更が必要であり `site/` の改変になる。さらにGitHub Pagesは `site` ディレクトリのみを配信するため、公開サイトが動かなくなる |
| シンボリックリンク・ジャンクション | 本リポジトリの `core.symlinks` は `false` である。ジャンクションはgitが実体として辿るため結局二重にコミットされる |
| 公開URLからHTTPで取得 | 外部通信が必要になり、オフラインで動かない。[R1要件](../requirements/REQUIREMENTS_R1.md) のNFR-01の趣旨に反する |
| `Assets/Resources/` へ置く | 常にビルドへ含まれ剥がせない。異常系検証でロード元を差し替えにくい |

## 第2層 スキーマ契約

### キー名は実装側を正とする

[BROWSER_MOCK_SPEC.md](BROWSER_MOCK_SPEC.md) の項目案と、`site/data/earth-eras.json` の実キーが食い違っている。**実装キー名を交換形式の正とし、仕様書の項目案は不採用とする。**

| 仕様書の項目案 | 採用するキー |
|---|---|
| eraId | `id` |
| order | `sortOrder` |
| ageLabel | `rangeLabel` |
| qualityStatus | `status` |
| scenarioOptions | `presentation.scenarioCandidates` |
| 対応なし | `presentation.tags` / `presentation.caption` / `presentation.futureNote` / `eraOrder` / `catalogTitle` / `disclaimer` / `parameterNote` を正式な構成要素とする |
| そのまま一致 | `schemaVersion` / `displayName` / `summary` / `visual` / `events` |

理由は次のとおり。

- 仕様書側へ寄せると `site/data/earth-eras.json` と `site/app.js` の両方を書き換えることになる。R2-P0は「R1はハッシュ同一で無改変」を証明して受け入れられており、これを壊す
- 既存の検証実績（実ブラウザ108件、状態機械117件）はすべて現在のキー名に対して取得されている。改名は全件の再取得を要求する
- 「6時代データの唯一の正本」という原則に照らせば、動いている実データが正本であり、実装前の項目案は正本ではない
- C#側のフィールド名をJSONキーへ合わせるだけで済み、追加コストがない

[BROWSER_MOCK_SPEC.md](BROWSER_MOCK_SPEC.md) は編集しない。[IMPLEMENTATION_ORDER_R2.md](../planning/IMPLEMENTATION_ORDER_R2.md) と同じく、食い違いは本書に残す。

### schemaVersion の運用方針

| 規則 | 内容 |
|---|---|
| 形式 | `[<データセット接頭辞>-]<major>.<minor>.<patch>` |
| 既存2件 | `"1.0.0"`（earth-eras）と `"r2-1.0.0"`（biosphere-scenarios）を凍結する。`site/` を変更しないため改名しない |
| 歴史的例外 | `"1.0.0"` は接頭辞を持たないが earth-eras 系列を指す。今後、接頭辞のない版を新規に作らない |
| 新規データセット | 必ず接頭辞を付ける |
| 受理規則 | 各実装は完全一致文字列のみを受理する。majorが同じなら受理するといった緩い規則を採らない |
| 版の上げ方 | キーの削除・改名・型変更はmajor、キーの追加はminor、値のみの変更はpatch |
| 同時更新規則 | majorを上げる変更は、ブラウザとUnityの両方を更新できるまでコミットしない |
| 未知フィールド | 無視する。未知の必須IDと未知のschemaVersionは受け入れない |
| 系列間の関係 | 接頭辞はデータセットの識別子であり、互換性の版ではない。`r2-1.0.0` は `1.0.0` の後継ではない |

### 検証規則はブラウザ実装と1対1にする

Unity側の検証は `site/app.js` と同じ判定を行う。値の意味づけを実装ごとにずらさないためである。

| 判定 | 内容 |
|---|---|
| 文字列 | 空文字と空白のみを不可とする |
| 比率 | 有限の数値で 0 以上 1 以下 |
| 色 | `#` に続く16進3桁・4桁・6桁・8桁のみ。5桁・7桁は不可 |
| 個数 | 0 以上の整数 |

全体エラーと縮退の切り分けもブラウザに合わせる。カタログ構造・必須文字列・件数・ID重複・並び順・`eraOrder` の不一致は全体エラーとして操作を無効化する。`visual` の個別キーの欠損・型不正・範囲外は、その時代だけ中立値へ置換して簡略表示の注記を出し、読み込みは継続する。

利用者へ内部パス・例外全文・スタックトレースを表示しない。本リポジトリはPublicであり、ローカル絶対パスにユーザー名が含まれるため、この規則はUnity側でも維持する。

## 見た目の一致は求めない

[BROWSER_MOCK_SPEC.md](BROWSER_MOCK_SPEC.md) は「ID・順序・テキスト・品質状態を共通化し、両実装の見た目の完全一致は求めない」と定めている。本書もこれを踏襲する。

`visual` の 0〜1 の値は無次元の演出強度であり、レンダリング側がCSS表現とUnity表現へそれぞれ変換する。共通データへCSSのクラス名やUnityのマテリアル名・シェーダプロパティ名・アセット参照を混ぜない。

## R2-P1への引き継ぎ

R2-P1でUnityへ `biosphere-scenarios.json` を移す際は、第1層の転送契約をそのまま再利用し、ファイル名だけを変える。第2層は `r2-` 系列のまま分離を維持する。その時点でスキーマ系列を統合するかどうかは、別途判断する。

関連：[ブラウザモック仕様](BROWSER_MOCK_SPEC.md)／[R1要件](../requirements/REQUIREMENTS_R1.md)／[R1-P1実装順序](../planning/IMPLEMENTATION_ORDER_R1_P1.md)／[P0実装報告](../reports/P0_BROWSER_MOCK_IMPLEMENTATION.md)
