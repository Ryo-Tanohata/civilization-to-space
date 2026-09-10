# Civilization to Space Simulator

地球の形成から文明と宇宙居住までを、異なる条件で比較・考察する探索的・象徴的なシナリオシミュレーター。

## 現在の段階

フェーズ0-B：初期ドキュメント基盤。アプリは実装前であり、起動できるモックやUnityプロジェクトはまだありません。文書の仕様・優先度はレビュー用の初案です。

## R1: Earth Through Time

Hadean、EarlyOcean、Snowball、GreenEarth、Information、Futureの6時代を選択・再生するブラウザモックを先に検証し、その後Unityへ同じ時代データの概念を移します。人類・文明の説明はInformationへの導入とし、7番目の時代を追加しません。

科学的未来予測や正確な地球科学モデルではありません。未来は条件付きの探索案であり、確率や実現時期を断定しません。

## 進め方

要件定義 → 計画 → 設計 → ブラウザモック → レビュー → Unity実装 → 検証 → 報告。各段階で小さく確認できる成果物を残します。GitHubのPrivateリポジトリをリモートの正本とし、ローカルの未コミット変更はレビュー中の案として扱います。commit・push・PR・公開には個別の明示承認が必要です。

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
  reports/                         検証・レビュー報告用（現在は空）
```

site/（ブラウザ）、unity/（Unity）、blender/（制作素材）は将来の承認後に作成します。空のreportsディレクトリはGitでは追跡されません。

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
