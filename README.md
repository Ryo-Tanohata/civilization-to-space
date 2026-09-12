# Civilization to Space Simulator

地球の形成から文明と宇宙居住までを、異なる条件で比較・考察する探索的・象徴的なシナリオシミュレーター。

## 現在の段階

フェーズ1：R1ブラウザモックのP0が動作します。`site/` をローカルHTTPサーバー経由で開くと、6時代を選択・再生できます。起動手順は[P0ブラウザモック実装報告](docs/reports/P0_BROWSER_MOCK_IMPLEMENTATION.md)にあります。

同じページに、R2「生命圏の比較」がブラウザ拡張として加わっています。画面上部の切替で、6時代の観察と、生命圏に関わる4つの比較条件のA/B比較を行き来できます。R2-P0は人によるブラウザレビューを完了しました（[R2-P0レビュー報告](docs/reports/R2_P0_HUMAN_REVIEW.md)）。

**Unityでの最小再現（R1-P1）が動作します。** `unity/CivilizationToSpace/` を Unity 6000.4.8f1 で開き、`EarthTimelineDemo` シーンを Play すると、同じ6時代データを読み込んだ象徴的な地球を、選択・自動再生・視点操作で見られます。起動手順と検証結果は[R1-P1実装報告](docs/reports/R1_P1_UNITY_IMPLEMENTATION.md)にあります。

**同じ内容をブラウザでも動かせます。** `site/unity/` にWebGLビルドを置いているため、スマートフォンからも開けます。地球の形成4段階・6時代・月への展開5段階を通して見られ、指2本で寄り引きできます。追加した操作と、公開ビルドに残っていた不具合2件の対応は[WebGL版の対応報告](docs/reports/UNITY_WEBGL_FONT_FIX.md)にあります。

R1のブラウザモックレビューは[実施済み](docs/reports/R1_P0_HUMAN_REVIEW.md)です。人によるUnityレビューは未実施で、FR-04（Futureの2案切替）はブラウザ・Unityとも未実装です。したがってR1全体のDefinition of Doneは未達です。文書の仕様・優先度はレビュー用の初案のままです。

## R1: Earth Through Time

Hadean、EarlyOcean、Snowball、GreenEarth、Information、Futureの6時代を選択・再生するブラウザモックを先に検証し、その後Unityへ同じ時代データの概念を移します。人類・文明の説明はInformationへの導入とし、7番目の時代を追加しません。

科学的未来予測や正確な地球科学モデルではありません。未来は条件付きの探索案であり、確率や実現時期を断定しません。

## R2: 生命圏の比較

海洋中心・低酸素化、酸素化の移行、寒冷・氷の制約、陸上植生の広がりという4つの比較条件を、基準Aと比較Bとして選び、6つの変数で見比べます。0〜100の値は実測値でも予測値でもなく、比較のために設計した象徴値です。高い値が望ましいという意味はありません。

4件は時代順・進歩順・難易度順ではありません。時代はR1の時間軸、比較条件はR2の固定状態セットであり、R2は7番目の時代を追加しません。

R2のブラウザ実装はR1のUnity最小再現より先に進めています。この判断と各段階の扱いは[R2実装順序](docs/planning/IMPLEMENTATION_ORDER_R2.md)に記録しています。

## 進め方

要件定義 → 計画 → 設計 → ブラウザモック → レビュー → Unity実装 → 検証 → 報告。各段階で小さく確認できる成果物を残します。GitHubのPublicリポジトリをリモートの正本とし、ローカルの未コミット変更はレビュー中の案として扱います。commit・push・PR・公開には個別の明示承認が必要です。

## 制作体制

本プロジェクトはプロジェクトオーナーとAIアシスタント（Anthropic Claude）の共同作成です。範囲の決定、要件・設計の判断、受入レビュー、commit・push・公開はオーナーが行います。AIは承認された範囲で文書作成、実装、検証を担当し、未検証の項目は未検証として報告します。

## ディレクトリ構造

```text
README.md
AGENTS.md
.github/workflows/pages.yml        site/をGitHub Pagesへ配信
docs/
  requirements/REQUIREMENTS_R1.md
  requirements/REQUIREMENTS_R2.md
  planning/ROADMAP.md
  planning/WBS_R1.md
  planning/BACKLOG.md
  planning/IMPLEMENTATION_ORDER_R2.md
  design/PRODUCT_VISION_R1.md
  design/EARTH_ERA_CATALOG.md
  design/STORYBOARD.md
  design/BROWSER_MOCK_SPEC.md
  planning/IMPLEMENTATION_ORDER_R1_P1.md
  planning/IMPLEMENTATION_ORDER_AFTER_R1_P1.md
  design/DATA_INTERCHANGE_R1.md
  reports/P0_BROWSER_MOCK_IMPLEMENTATION.md
  reports/R2_P0_HUMAN_REVIEW.md
  reports/R1_P0_HUMAN_REVIEW.md
  reports/R1_P1_UNITY_IMPLEMENTATION.md
site/
  index.html                       画面構造（R1・R2の両パネル）
  styles.css                       R1のCSSのみの象徴的地球とレイアウト
  app.js                           R1の状態管理・データ検証・再生制御
  r2.css                           R2の比較ビューのスタイル
  r2.js                            R2の状態管理・データ検証・比較表示
  data/earth-eras.json             6時代データの正本
  data/biosphere-scenarios.json    4比較条件データの正本
unity/CivilizationToSpace/
  Assets/Scripts/Core/             データ読込・検証・時代の状態・再生（描画に依存しない）
  Assets/Scripts/View/             地球の表現・画面・カメラ・日本語表示
  Assets/Scripts/AppRoot.cs        組み立て。R2-P1が参照する唯一の入口
  Assets/Editor/                   生成・同期・検証の道具
  Assets/Scenes/EarthTimelineDemo.unity
  Assets/StreamingAssets/earth-eras.json   正本の派生コピー（編集禁止）
```

`site/` は外部ライブラリ・CDN・npm・Node.js・外部APIに依存せず、ローカルHTTPサーバーとブラウザだけで動作します。

`unity/` はUnity 6000.4.8f1・Built-in Render Pipeline で動作し、追加パッケージはUnity同梱の `com.unity.ugui` のみです。日本語表示のため、SIL Open Font License 1.1 の Noto Sans JP から必要な文字だけを抜き出したフォントを `Assets/Resources/Fonts/` に同梱しています（出典と改変内容は同ディレクトリの `NOTICE.md`）。外部通信を行いません。時代データの正本は `site/data/earth-eras.json` だけであり、`StreamingAssets/` のコピーはblobハッシュの一致で同一性を示します。blender/（制作素材）は将来の承認後に作成します。

## 公開

`main` の `site/` を [Pages配信ワークフロー](.github/workflows/pages.yml) がGitHub Pagesへ公開します。`docs/` の要件・設計文書は配信対象に含みません。`site/` 内の参照はすべて相対パスのため、プロジェクトページのサブパスでもそのまま動作します。

配信を有効にするには、リポジトリのSettings → PagesでSourceを「GitHub Actions」に設定します。公開URLはワークフロー実行結果のdeployステップに表示されます。

## 文書一覧

- [エージェント規則](AGENTS.md)
- [R1要件](docs/requirements/REQUIREMENTS_R1.md)
- [ロードマップ](docs/planning/ROADMAP.md)
- [R1 WBS](docs/planning/WBS_R1.md)
- [バックログ](docs/planning/BACKLOG.md)
- [体験ビジョン](docs/design/PRODUCT_VISION_R1.md)
- [6時代カタログ](docs/design/EARTH_ERA_CATALOG.md)
- [絵コンテ](docs/design/STORYBOARD.md)
- [ブラウザモック仕様](docs/design/BROWSER_MOCK_SPEC.md)
- [P0ブラウザモック実装報告](docs/reports/P0_BROWSER_MOCK_IMPLEMENTATION.md)
- [R2要件](docs/requirements/REQUIREMENTS_R2.md)
- [R2実装順序](docs/planning/IMPLEMENTATION_ORDER_R2.md)
- [R2-P0レビュー報告](docs/reports/R2_P0_HUMAN_REVIEW.md)
- [R1-P0レビュー報告](docs/reports/R1_P0_HUMAN_REVIEW.md)
- [R1-P1実装順序](docs/planning/IMPLEMENTATION_ORDER_R1_P1.md)
- [R1-P1以降の実装順序](docs/planning/IMPLEMENTATION_ORDER_AFTER_R1_P1.md)
- [データ交換形式](docs/design/DATA_INTERCHANGE_R1.md)
- [R1-P1実装報告](docs/reports/R1_P1_UNITY_IMPLEMENTATION.md)
