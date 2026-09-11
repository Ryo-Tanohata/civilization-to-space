# Civilization to Space Simulator

地球の形成から文明と宇宙居住までを、異なる条件で比較・考察する探索的・象徴的なシナリオシミュレーター。

## 現在の段階

フェーズ1：R1ブラウザモックのP0が動作します。`site/` をローカルHTTPサーバー経由で開くと、6時代を選択・再生できます。起動手順は[P0ブラウザモック実装報告](docs/reports/P0_BROWSER_MOCK_IMPLEMENTATION.md)にあります。

同じページに、R2「生命圏の比較」がブラウザ拡張として加わっています。画面上部の切替で、6時代の観察と、生命圏に関わる4つの比較条件のA/B比較を行き来できます。R2-P0は人によるブラウザレビューを完了しました（[R2-P0レビュー報告](docs/reports/R2_P0_HUMAN_REVIEW.md)）。

人によるモックレビューは未実施で、Unityプロジェクトは未着手です。文書の仕様・優先度はレビュー用の初案のままです。

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
  reports/P0_BROWSER_MOCK_IMPLEMENTATION.md
  reports/R2_P0_HUMAN_REVIEW.md
site/
  index.html                       画面構造（R1・R2の両パネル）
  styles.css                       R1のCSSのみの象徴的地球とレイアウト
  app.js                           R1の状態管理・データ検証・再生制御
  r2.css                           R2の比較ビューのスタイル
  r2.js                            R2の状態管理・データ検証・比較表示
  data/earth-eras.json             6時代データの正本
  data/biosphere-scenarios.json    4比較条件データの正本
```

`site/` は外部ライブラリ・CDN・npm・Node.js・外部APIに依存せず、ローカルHTTPサーバーとブラウザだけで動作します。unity/（Unity）、blender/（制作素材）は将来の承認後に作成します。

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
