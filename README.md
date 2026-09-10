# Civilization to Space Simulator

地球の形成から文明と宇宙居住までを、異なる条件で比較・考察する探索的・象徴的なシナリオシミュレーター。

## 現在の段階

フェーズ1：R1ブラウザモックのP0が動作します。`site/` をローカルHTTPサーバー経由で開くと、6時代を選択・再生できます。起動手順は[P0ブラウザモック実装報告](docs/reports/P0_BROWSER_MOCK_IMPLEMENTATION.md)にあります。

実機ブラウザでのモックレビューは未実施で、Unityプロジェクトは未着手です。文書の仕様・優先度はレビュー用の初案のままです。

## R1: Earth Through Time

Hadean、EarlyOcean、Snowball、GreenEarth、Information、Futureの6時代を選択・再生するブラウザモックを先に検証し、その後Unityへ同じ時代データの概念を移します。人類・文明の説明はInformationへの導入とし、7番目の時代を追加しません。

科学的未来予測や正確な地球科学モデルではありません。未来は条件付きの探索案であり、確率や実現時期を断定しません。

## 進め方

要件定義 → 計画 → 設計 → ブラウザモック → レビュー → Unity実装 → 検証 → 報告。各段階で小さく確認できる成果物を残します。GitHubのPrivateリポジトリをリモートの正本とし、ローカルの未コミット変更はレビュー中の案として扱います。commit・push・PR・公開には個別の明示承認が必要です。

## 制作体制

本プロジェクトはプロジェクトオーナーとAIアシスタント（Anthropic Claude）の共同作成です。範囲の決定、要件・設計の判断、受入レビュー、commit・push・公開はオーナーが行います。AIは承認された範囲で文書作成、実装、検証を担当し、未検証の項目は未検証として報告します。

## ディレクトリ構造

```text
README.md
AGENTS.md
docs/
  requirements/REQUIREMENTS_R1.md
  planning/ROADMAP.md
  planning/WBS_R1.md
  planning/BACKLOG.md
  design/PRODUCT_VISION_R1.md
  design/EARTH_ERA_CATALOG.md
  design/STORYBOARD.md
  design/BROWSER_MOCK_SPEC.md
  reports/P0_BROWSER_MOCK_IMPLEMENTATION.md
site/
  index.html                       画面構造
  styles.css                       CSSのみの象徴的地球とレイアウト
  app.js                           状態管理・データ検証・再生制御
  data/earth-eras.json             6時代データの正本
```

`site/` は外部ライブラリ・CDN・npm・Node.js・外部APIに依存せず、ローカルHTTPサーバーとブラウザだけで動作します。unity/（Unity）、blender/（制作素材）は将来の承認後に作成します。

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
