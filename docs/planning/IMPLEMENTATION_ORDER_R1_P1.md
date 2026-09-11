# R1-P1の実装順序と、ブラウザ仕様からの逸脱の記録

状態：オーナー判断を記録した文書。[BROWSER_MOCK_SPEC.md](../design/BROWSER_MOCK_SPEC.md) と [BACKLOG.md](BACKLOG.md) は編集しないため、食い違いをここに残す。本書はUnityプロジェクト・C#ファイル・`unity/CivilizationToSpace/` を生成する許可ではない。

## 対象

R1-P1「R1のUnity最小再現」。[R1要件](../requirements/REQUIREMENTS_R1.md) のFR-06とAC-09を満たし、R1全体のDefinition of Doneが求める起動手順・データ対応表・既知の制限・レビュー承認のうち、実装側の3点をそろえることを目的とする。

## 確定した環境

| 項目 | 決定 |
|---|---|
| Unityバージョン | 2022.3.11f1 |
| エディション | Unity Personal |
| テンプレート | 3D Core（Built-in Render Pipeline） |
| プロジェクト位置 | `unity/CivilizationToSpace/` |
| Scene名 | `EarthTimelineDemo` |
| 検証の正 | Editor Play Mode。スタンドアロンビルドは対象外とする |

[R1要件](../requirements/REQUIREMENTS_R1.md) は「Unityバージョンは後続レビューで確定する」としていた。本書で確定する。2022.3.11f1と2021.3.45f1がインストール済みであり、前者を採る。新規ダウンロードを伴わないため環境導入の負担がない。2022.3の3D Coreテンプレートは既定でBuilt-in Render Pipelineであり、URP・HDRPを使わないという制約をテンプレート選択のみで満たせる。

## 逸脱の記録

### 逸脱1 B06未実施のままR1-P1のS1へ着手する

[BACKLOG.md](BACKLOG.md) はB07「Unity基盤と地球表示」の依存にB06「モックレビュー」を挙げている。B06は未実施である。

判断は次のとおり。

- S1（Git基盤とデータ交換契約の確定）はB06に依存しないため、先に実施する
- S2（Unityプロジェクト生成）の着手前にB06を実施する。これを必須の前提とする
- B06をスキップしない。依存が実際に効くのは、ブラウザ側のレビュー指摘が手戻りになりうるS2以降であるため、そこで守る

S1が作るのはGit設定とデータ交換契約であり、いずれもブラウザの体験レビューの結果に左右されない。一方、Unityの表現・テキスト・時代の見分けやすさはB06の指摘対象と重なるため、S2以降を先行させない。

### 逸脱2 地球をゆっくり自転させる

[BROWSER_MOCK_SPEC.md](../design/BROWSER_MOCK_SPEC.md) は「点滅や常時回転は初期対象外」と定めている。R1-P1ではこれを外れ、地球をゆっくり自転させる。

理由は、3D空間で球体が完全に静止していると、平面の円と区別がつかず球体である必然性が失われるためである。速度は低く保ち、既定で毎秒3度程度とする。自転はカメラではなく地球の親オブジェクトを回す。カメラを回すと後述の視点操作と競合するためである。

### 逸脱3 マウスでカメラを回転・ズームできるようにする

同仕様は「カメラは固定」と定めている。R1-P1ではこれを外れ、ドラッグによる視点回転とホイールによるズームを提供する。

理由は、3D表現の理解にはある程度の視点移動が要るためである。ただし同仕様の「時代ごとにレイアウトが跳ねないようにする」という趣旨は守り、時代を切り替えてもカメラを動かさない。仰角と距離はクランプし、視点のリセット手段を常設する。視点操作は再生を停止しない。再生を停止するのは時代の移動だけという規則を維持する。

3D操作は発見可能性が低いため、操作方法を画面に常設表示する。注記がないと、[R1要件](../requirements/REQUIREMENTS_R1.md) のAC-06が求める「操作名が認識できる」を満たせない。

### 逸脱2・3に伴う動き軽減の代替手段

[R1要件](../requirements/REQUIREMENTS_R1.md) のAC-08は「動き軽減が有効なとき、装飾運動を抑え、自動開始せず、手動移動が可能」であることを求める。ブラウザはCSSの `prefers-reduced-motion` でこれを実現しているが、Unityには相当する標準の仕組みがない。

代替として、画面内に「動きを減らす」トグルを常設する。オンにすると自転を停止し、時代遷移の補間を省いて瞬時に切り替え、衛星の周回を止める。[R2要件](../requirements/REQUIREMENTS_R2.md) のR2-NFR-05が永続保存を禁じている趣旨に合わせ、`PlayerPrefs` を使わずセッション内のメモリにのみ保持する。

## 段階の呼称

[IMPLEMENTATION_ORDER_R2.md](IMPLEMENTATION_ORDER_R2.md) の呼称を引き継ぐ。「P0／P1」はリリース内の段階であり、[BACKLOG.md](BACKLOG.md) の優先度P0／P1／P2／P3とは別物である。

## 未了のまま残る事項

R1-P1は次を完了させない。いずれも取り消されたのではなく、保持されている。

| 項目 | 出典 | 状態 |
|---|---|---|
| FR-04／AC-05 Futureの2案切替 | [R1要件](../requirements/REQUIREMENTS_R1.md) | R1-P1の対象外。`scenarioCandidates` は読み取り専用表示に留める |
| B06／W5 人によるモックレビュー | [BACKLOG.md](BACKLOG.md)、[WBS_R1.md](WBS_R1.md) | S2着手前に実施する |
| InformationとFutureの見分けにくさ | [P0実装報告](../reports/P0_BROWSER_MOCK_IMPLEMENTATION.md) | 未決。Unityではライティングで差が変わるため再判断する |
| スクリーンリーダー対応 | [ブラウザモック仕様](../design/BROWSER_MOCK_SPEC.md) | UnityのuGUIは支援技術に対応しない。実現不能として制限に記す |
| R2-P1 UnityへのR2移植 | [IMPLEMENTATION_ORDER_R2.md](IMPLEMENTATION_ORDER_R2.md) | 未着手 |

R1-P1が完了しても、R1のDefinition of Doneは未達である。FR-04／AC-05が未実装であるためブラウザ段階のDoDが満たされていない。報告書にこれを明記し、R1完了と誤認させない。

## FR-04を対象外とする理由

ブラウザ側でもFR-04は意図的に未実装である。Unityで先に実装すると、同じデータを使う2つの実装のあいだで機能が非対称になり、AC-09「ID・順序・説明がブラウザと一致する」の検証が複雑になる。R1-P1は最小再現であり、両実装の対称性を保つほうを採る。

## スプリントと既存タスクの対応

| 本書 | 内容 | 既存WBS | 既存BACKLOG | 相対単位 |
|---|---|---|---|---|
| S1 | 方針確定とGit基盤 | W6の前段 | B07の前提 | 1s |
| S2 | Unity基盤とデータ経路 | W6 Unity基盤 | B07 | 3s |
| S3 | 時代切替と情報表示 | W7 Unity時代表示 | B08 | 2s |
| S4 | 時系列再生・カメラ・異常系 | W7 Unity時代表示 | B09 | 2s |
| S5 | レビューと報告 | W8 R1レビュー | B10 | 2s |

相対単位は [WBS_R1.md](WBS_R1.md) の流儀に従う。1相対作業単位を表す便宜的な表記であり、秒数でも実作業日数の見積もりでもなく、納期を意味しない。

## 新規ID体系

[R1要件](../requirements/REQUIREMENTS_R1.md) は「状態：レビュー用初案。実装未着手。」のまま更新されていない。既存のFR・NFR・AC系列を増やさず、別系列を新設する。

| 系列 | 用途 |
|---|---|
| `UAC-nn` | R1-P1の受入基準 |
| `U-nn` | Unityの手動テストケース |
| `UG-nn` | 承認ゲート |
| `UR-nn` | R1-P1固有のリスク |
| `S1`〜`S5` | スプリント |

`US-nn` はR1のユーザーストーリーで使用済みのため使わない。

既存のAC-09は書き換えない。本書では次の合成として扱う。

```text
AC-09 = UAC-04 データ同一性
      + UAC-05 IDと順序の一致
      + UAC-06 説明テキストの一致
      + UAC-08 切替の同期
      + UAC-12 再生の成立
```

## 実装順

依存順に実施する。各段階の検証が通ってから次へ進む。

1. Git設定とデータ交換契約の確定（S1）
2. B06の実施（S2の前提）
3. Unityプロジェクト生成、データ経路、読込と検証（S2）
4. 時代切替、情報表示、日本語表示方式の確定（S3）
5. 時系列再生、自転、視点操作、異常系（S4）
6. 実装報告と人によるレビュー（S5）

## R1保護の規律

R1-P1のコードと作業は次を守る。

- `site/` の7ファイルを変更しない。`site/data/earth-eras.json` は正本であり、Unity側から書き換えない
- 時代のID・名称・年代ラベル・解説・視覚値をC#へ書き写さない。コードが持つ定数は対応schemaVersion、必須件数6、基準ステップ4000ms、許容速度、中立値、JSONのキー名のみとする
- `eraOrder` はブラウザ実装と同じく正本内の自己申告として扱う
- 共通データにUnityのマテリアル名・シェーダプロパティ名・アセット参照を混ぜない
- 異常系の検証では、リポジトリ内のJSONを書き換えず、一時フォルダへ置いた複製を読ませる
- 利用者向け画面に内部パス・例外全文・スタックトレースを出さない
- 数値年代を追加しない。第7の時代を追加しない

## R2-P1への配慮

ブラウザのR2はR1の内部へ触れられないため、再生ボタンの `aria-pressed` を読んで合成クリックする形で迂回している。Unityでは最初から正しい境界を用意する。R1側は「再生中かどうかを読む」「停止を要求する」の2つだけを持つ最小のインターフェースを公開し、R2-P1はそれ以外に依存しない。staticやSingletonを作らない。

語彙も分離する。[R2要件](../requirements/REQUIREMENTS_R2.md) は「シナリオ」をR2で使わず「比較条件」と呼ぶと定めている。Unity側でも `Scenario` はFutureの2案にのみ使い、`Condition` をR2-P1のために空けておく。

## テスト順

自動テスト基盤は存在しない。既存文書にある検証件数は使い捨てスクリプトの結果であり再実行できない。R1-P1では再現不可能な数値を新たに作らず、`U-nn` 表を唯一の検証台帳とし、各行に実行者と実行手段を記す。

1. Git設定の無影響確認（既存ファイルの差分なし、無視パターンの範囲）
2. 正本とコピーのblobハッシュ照合
3. データ読込と6 ID・順序・テキストの一致
4. 日本語表示の可読性
5. 時代切替と表示の同期
6. 再生・速度・端・手動介入
7. 視点操作と動き軽減
8. データ異常時の全体エラーと縮退
9. ブラウザとの突き合わせ
10. 公開・容量・情報漏れの確認

## commit・push順

実装と文書を独立して差し戻せるよう、コミットを分ける。英語のConventional Commitsで1行とする。

| # | メッセージ | 対象 |
|---|---|---|
| 1 | `chore: add Unity gitignore and gitattributes` | `.gitattributes`、`unity/.gitattributes`、`unity/.gitignore` |
| 2 | `docs: define the shared data interchange format for Unity` | [データ交換形式](../design/DATA_INTERCHANGE_R1.md)、本書 |
| 3 | `feat: add the Unity project skeleton for Earth Through Time` | ProjectSettings、Packages、Scene、meta |
| 4 | `feat: load the shared era catalog in Unity` | StreamingAssets、Core、AppRoot、Editor拡張 |
| 5 | `feat: add era selection and display to the Unity demo` | View、UI、SceneBuilder |
| 6 | `feat: add timeline playback and camera control to the Unity demo` | Timeline、Camera、ErrorPanel |
| 7 | `docs: record the R1 P1 Unity implementation` | 報告書、README |

push前に origin/main との差分、ステージング対象、`git diff --cached --check` を確認する。push後に `git status --short --branch`、`git log --oneline`、`git rev-list --left-right --count HEAD...origin/main` が 0 / 0 であることを確認する。

## ロールバック方針

R1-P1は `unity/` と `docs/` への新規追加が中心であり、`site/` を変更しない。`README.md` への追記のみが既存ファイルへの変更である。GitHub Pagesのワークフローは `site/**` の変更でのみ発火するため、差し戻しても公開サイトへ影響しない。

- 文書だけ戻す：該当コミットを `git revert`
- 実装だけ戻す：Unity側のコミットを新しい順に `git revert`
- Git設定だけ戻す：コミット1を `git revert`
- 全部戻す：すべてを新しい順に `git revert`

`--force`、`reset --hard`、`clean`、rebase、amend は使わない。`git add --renormalize` も使わない。既存22ファイルがすべてindex側でLF正規化済みであるため不要であり、実行すると全ファイルがステージへ載って差分レビューができなくなる。

Sceneが壊れた場合、`.unity` をテキストとして直接編集しない。生成用のEditor拡張を再実行して作り直す。

## 承認ゲート

| ID | 承認事項 | 段階 |
|---|---|---|
| UG-01 | B06の扱い（S1先行、S2前に実施） | S1前 |
| UG-02 | `unity/` の作成、Unityプロジェクト生成、ProjectSettingsとPackagesの初期スナップショットのコミット、SampleSceneのリネーム | S1・S2 |
| UG-03 | `.gitignore` と `.gitattributes` の新設と配置 | S1 |
| UG-04 | データ交換契約とキー名の決定 | S1 |
| UG-05 | 逸脱2・3（自転、視点操作）と、StreamingAssetsへの派生コピー作成 | S1・S2 |
| UG-06 | Editor拡張の追加とメニュー実行 | S2〜S4 |
| UG-07 | 日本語表示方式 | S3 |
| UG-08 | 各コミットとpush | 全段階 |
| UG-09 | 報告書2件の新規作成 | S5 |
| UG-10 | batchmodeでのUnity起動 | S2 |
| UG-11 | スタンドアロンビルドの実施（本書では行わない） | 任意 |

## 後続の文書をいつ作るか

| 文書 | 作る条件 |
|---|---|
| `docs/reports/R1_P1_UNITY_IMPLEMENTATION.md` | S5で実装記録を残すとき |
| `docs/reports/R1_P1_HUMAN_REVIEW.md` | 人によるUnityレビューを実施したとき |
| `docs/planning/WBS_R2.md` / `BACKLOG_R2.md` | R2-P1に着手するとき |

[AGENTS.md](../../AGENTS.md) の「新規実装はアジャイルな縦切りで、起動・操作・レビュー可能な最小成果物にする」に沿い、文書を先にそろえるより動く最小成果物とその記録を先に置く。

関連：[R1要件](../requirements/REQUIREMENTS_R1.md)／[データ交換形式](../design/DATA_INTERCHANGE_R1.md)／[R2実装順序](IMPLEMENTATION_ORDER_R2.md)／[R1 WBS](WBS_R1.md)／[バックログ](BACKLOG.md)
