# R2の実装順序と、ロードマップからの逸脱の記録

状態：オーナー判断を記録した文書。[ROADMAP.md](ROADMAP.md) は編集しないため、食い違いをここに残す。

## 逸脱の内容

[ROADMAP.md](ROADMAP.md) の依存グラフは `R1ブラウザモック → モックレビュー → R1 Unity最小再現 → R2生命圏` であり、R2はUnityの後段に置かれている。R2行の技術依存も「R1のデータ拡張・説明の検証」である。

今回はこの順序から外れ、**R1-P0のブラウザモックを基盤として、R2-P0のブラウザ比較機能をUnity実装より先に進めた。**

## 判断

オーナーの判断は次のとおり。

- R1-P0（ブラウザモック）を基盤としてR2-P0（ブラウザ比較）を先に進める
- この判断はR1のUnity版を不要にするものではない
- R1-P1としてUnity最小再現を後続タスクとして保持する
- R2にも将来R2-P1としてUnity移植の段階を設ける

したがってROADMAPのR1→R2という大きな流れは維持され、各リリースがブラウザ段階とUnity段階の2つを持つ構造になる。ROADMAPはこの2段構成を記述していないため、実態は本書を正とする。

## 段階の呼称

| 段階 | 内容 | 状態 |
|---|---|---|
| R1-P0 | Earth Through Time ブラウザモック | 実装済み。実ブラウザ自動検証まで完了 |
| R1-P1 | R1のUnity最小再現 | 未着手。保持する |
| R2-P0 | 生命圏の比較条件A/B比較ビュー（ブラウザ） | 本タスクで実装 |
| R2-P1 | R2のUnity移植 | 未着手 |

「P0／P1」はリリース内の段階を指す呼称であり、[BACKLOG.md](BACKLOG.md) の全プロジェクト共通の優先度 P0／P1／P2／P3 とは別物である。BACKLOGのP1は「モック承認後のR1 Unity最小再現」、P2は「R1完了後に価値を検証する拡張」を指す。混同しないこと。

## 未了のまま残る事項

R2-P0の実装は、次の項目を完了させていない。いずれも取り消されたのではなく、保持されている。

| 項目 | 出典 | 状態 |
|---|---|---|
| B06／W5 人によるモックレビュー | [BACKLOG.md](BACKLOG.md)、[WBS_R1.md](WBS_R1.md) | 未実施。R2-P0はこれを代替しない |
| FR-04／AC-05 Futureの2案切替 | [REQUIREMENTS_R1.md](../requirements/REQUIREMENTS_R1.md) | 意図的に未実装。R1のブラウザ段階DoDは未達 |
| FR-06／AC-09 Unity最小再現 | 同上 | 未着手（R1-P1） |
| Unityとの共通データ交換形式 | [BROWSER_MOCK_SPEC.md](../design/BROWSER_MOCK_SPEC.md) | 未決。R1とR2でスキーマ系列を分けたまま |
| InformationとFutureの見分けにくさ | [P0実装報告](../reports/P0_BROWSER_MOCK_IMPLEMENTATION.md) | 表現上の判断として未決 |

同一ページ上で、R1のFutureの2案が読み取り専用のまま、R2の4条件はA・Bとして選べるという非対称が生じている。これは意図的な繰り延べの結果であり、FR-04を恒久的に取り下げたものではない。

## 実装順

依存順に実施した。各段階の検証が通ってから次へ進んだ。

1. `site/data/biosphere-scenarios.json` を作成し、JSON構文・件数・ID・値・関連時代IDを検証
2. `site/index.html` へ挿入のみの変更（既存行の削除・改名・移動・再インデントなし）
3. `site/r2.css` を作成し、全セレクタが `#r2-panel` / `#view-switch` 配下であることを検証
4. `site/r2.js` を作成し、R1保護の規律（後述）を機械的に検証
5. 実ブラウザでR2機能・アクセシビリティ・レスポンシブを検証
6. 実ブラウザでR1の回帰を検証
7. 文書とREADMEを更新

## R1保護の規律

R2のコードは次を守る。実装後に機械的に検査した。

- `site/app.js` の内部状態・関数・DOMマップを参照しない
- `showFatalError` / `guard` / `#error` / `#error-detail` / `#retry` / `#controls` に触れない
- `window` へ `error` / `unhandledrejection` のリスナーを追加しない
- 例外を外へ再throwしない（漏れるとR1のグローバルハンドラがR1を全面停止させる）
- R2専用のDOMマップ・状態・エラーラッチ・ガード関数を持つ
- タイマー・自動再生を作らない
- R1の再生停止は `#play` の `aria-pressed` を読み、再生中のときだけ合成クリックで行う
- `site/r2.css` の全セレクタを `#r2-panel` または `#view-switch` の配下に限定する

とくに、要素を `hidden` にしてもR1のタイマーは進み続ける。R2へ移る際は**隠す前に**停止する必要がある。R1の `visibilitychange` 監視はタブ単位であり、ページ内の hidden では発火しない。

## テスト順

1. JSON構文とデータ構造
2. JavaScript構文
3. R1保護の機械的検査（禁止参照のgrep、idの重複なし、CSSセレクタ範囲）
4. 変更禁止ファイルのハッシュ照合
5. R2機能（初期状態、16通りの組合せ、A=B、注記、用語）
6. R2のデータ異常12種でR1が無傷であること
7. R2のアクセシビリティ（キーボード、フォーカス、aria-live、動き軽減）
8. R2のレスポンシブ（360px／390px／デスクトップ／文字200%）
9. R1の回帰（6時代、再生、端の無効化）

## commit・push順

2コミットに分けた。実装と文書を独立して差し戻せるようにするためである。

1. `feat: add the R2 biosphere condition comparison view`
   対象：`site/index.html`、`site/r2.js`、`site/r2.css`、`site/data/biosphere-scenarios.json`
2. `docs: add R2 requirements and implementation order`
   対象：`README.md`、`docs/requirements/REQUIREMENTS_R2.md`、本書

push前に origin/main との差分、ステージング対象、`git diff --cached --check` を確認する。push後に `git status --short --branch`、`git log -2 --oneline`、`git rev-list --left-right --count HEAD...origin/main` が 0 / 0 であることを確認する。

## ロールバック方針

R2は追加と挿入のみで構成されており、R1のファイルを変更していない。

- 文書だけ戻す：コミット2を `git revert`
- 実装だけ戻す：コミット1を `git revert`。`site/index.html` の挿入が取り消され、R2の3ファイルが削除される。R1は元の状態に戻る
- 両方戻す：2つを `git revert`

`--force`、`reset --hard`、`clean`、rebase は使わない。

## 後続の設計文書をいつ作るか

今回は要件と実装順序の2件のみを作成した。次の文書は、必要になった時点でオーナーの承認を得てから作る。

| 文書 | 作る条件 |
|---|---|
| `docs/design/BIOSPHERE_MODEL_R2.md` | 変数を増やす、値を見直す、または出典を付けると決めたとき |
| `docs/design/BIOSPHERE_STORYBOARD_R2.md` | R2の画面構成を大きく変えるとき |
| `docs/planning/WBS_R2.md` / `BACKLOG_R2.md` | R2-P1（Unity移植）に着手するとき |
| `docs/reports/` のR2レビュー報告 | 人によるR2レビューを実施したとき |

文書を先に6件そろえるより、動く最小成果物とその記録を先に置く方針をとった。[AGENTS.md](../../AGENTS.md) の「新規実装はアジャイルな縦切りで、起動・操作・レビュー可能な最小成果物にする」に沿う。

関連：[R2要件](../requirements/REQUIREMENTS_R2.md)／[ロードマップ](ROADMAP.md)／[R1 WBS](WBS_R1.md)／[バックログ](BACKLOG.md)
