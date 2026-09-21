# Civilization to Space Simulator

地球の形成から文明と宇宙居住までを、異なる条件で比較・考察する探索的・象徴的なシナリオシミュレーター。

## 現在の段階

フェーズ1：R1ブラウザモックのP0が動作します。`site/` をローカルHTTPサーバー経由で開くと、6時代を選択・再生できます。起動手順は[P0ブラウザモック実装報告](docs/reports/P0_BROWSER_MOCK_IMPLEMENTATION.md)にあります。

同じページに、R2「生命圏の比較」がブラウザ拡張として加わっています。画面上部の切替で、6時代の観察と、生命圏に関わる4つの比較条件のA/B比較を行き来できます。R2-P0は人によるブラウザレビューを完了しました（[R2-P0レビュー報告](docs/reports/R2_P0_HUMAN_REVIEW.md)）。

**Unityでの最小再現（R1-P1）が動作します。** `unity/CivilizationToSpace/` を Unity 6000.4.8f1 で開き、`EarthTimelineDemo` シーンを Play すると、同じ6時代データを読み込んだ象徴的な地球を、選択・自動再生・視点操作で見られます。起動手順と検証結果は[R1-P1実装報告](docs/reports/R1_P1_UNITY_IMPLEMENTATION.md)にあります。

**重力計算で地球の形成を再現するページがあります。** `site/formation.html` は、微惑星どうしの重力と合体を実際に計算して地球ができるまでを描きます。何を計算し何を計算していないかは[形成シミュレーションの報告](docs/reports/FORMATION_SIMULATION.md)にあります。

**同じ内容をブラウザでも動かせます。** `site/unity/` にWebGLビルドを置いているため、スマートフォンからも開けます。地球の形成4段階・6時代・月への展開5段階を通して見られ、指2本で寄り引きできます。追加した操作と、公開ビルドに残っていた不具合2件の対応は[WebGL版の対応報告](docs/reports/UNITY_WEBGL_FONT_FIX.md)にあります。

**地軸の傾きから季節が現れます。** 地球を23.4度傾け、太陽の向きを一年かけて地軸に対し上下に振ります。昼夜の境目が斜めに倒れ、極の白夜と極夜が入れ替わります。1年は40秒で、自転10回ぶんです。形成から月までを通して見ると季節がおよそ2回巡ります。公転そのものは表していません。一周させると1年に一度カメラから見て真っ暗になり、地表が読めなくなるためです。採否の判断と実測値は[季節の実装報告](docs/reports/EARTH_SEASONS_FROM_TILTED_AXIS.md)にあります。

**海と陸がはっきり分かれ、画面が明るくなりました。** 地球を囲む雲と大気の球が地表へ影を落としており、地表は太陽ではなく環境光だけで照らされていました。影を落とさないようにし、陸・浅い海・深い海を明るさで3段に分け、太陽の強さを釣り合う値へ下げています。明るさは2倍になり、昼と夜の差は保たれています。原因の切り分けと実測値は[明るさと海岸線の報告](docs/reports/EARTH_BRIGHTNESS_AND_SHORELINE.md)にあります。

**時代が6つから10つへ増えました。** 巨大生物の時代・衝突と暗い空・氷期のくり返し・人類の広がりを、森林と陸上生態系のあとへ足しています。**宇宙から見た地球には**恐竜そのものを描かず、氷の少なさ・緑の広さ・割れ始めた大陸で表しています。地表へ降りると生きものが立っています。決めごとは[地表の描き方](docs/design/SURFACE_SCENE_SPEC.md)にあります。

R1のブラウザモックレビューは[実施済み](docs/reports/R1_P0_HUMAN_REVIEW.md)です。人によるUnityレビューは未実施で、FR-04（Futureの2案切替）はブラウザ・Unityとも未実装です。したがってR1全体のDefinition of Doneは未達です。文書の仕様・優先度はレビュー用の初案のままです。

## 参照した資料

地球ができるまでの順序（円盤の塵 → 引力による衝突で微惑星へ → 微惑星どうしの衝突 → 惑星、そして巨大衝突と月の形成）は、次を参照しています。数値・形・色は象徴的な表現であり、出典から取った測定値ではありません。

- 丸山茂徳ほか・冥王代生命学研究グループによる、太陽系と地球の誕生から生命の誕生・進化までをたどる映像資料（平成26年度 文部科学省科学研究費補助金・新学術領域研究）
- [NASA Astrobiology「How did our Solar System form?」](https://astrobiology.nasa.gov/education/alp/how-did-our-solar-system-form/)
- [Lunar and Planetary Institute「Active Accretion」](https://www.lpi.usra.edu/education/orexlaunch/Active%20Accretion.pdf)

- [NASA Science「Moon Formation」](https://science.nasa.gov/moon/formation/)（クレジット：NASA）— 火星ほどの大きさの天体が若い地球へ衝突したとする記述
- [NASA Astrobiology「Tracking Formation of the Earth and Moon」](https://astrobiology.nasa.gov/news/tracking-formation-of-the-earth-and-moon/)・[Lunar and Planetary Institute「The Moon's Formation and Evolution」](https://www.lpi.usra.edu/education/explore/marvelMoon/background/moon-formation/) — 飛び散った物質が地球を巡る円盤になり、そこから月が集まったとする記述。集積は数百年ほどで、その大半は初めの100年ほど。地球側のマグマオーシャンが固まるまではおよそ1000年。Unityの画面で、地球が丸く戻るより月が集まるほうに時間をかけているのはこの関係によります（年数そのものは表していません）。
- [NTRS「Origin of the Moon, Impactor Theory」](https://ntrs.nasa.gov/api/citations/20210000977/downloads/Moon-ImpactTheory_Ahrens.pdf) — 定説とされる模型では、円盤の物質はぶつかってきた天体のマントルが主（6割超）で、原始地球からのぶんは2割ほど。砕けたかけら・輪・月を同じ岩の色にしているのはこの模型によります。ただし月の同位体組成が地球とほぼ同じである理由は決着しておらず、この点自体が論点として残っています。

宇宙へ出る順序（打ち上げ → 人工衛星 → 宇宙ステーション）は、次を参照しています。

- [NASA「65 Years Ago: Sputnik Ushers in the Space Age」](https://www.nasa.gov/history/65-years-ago-sputnik-ushers-in-the-space-age/) — 世界初の人工衛星スプートニク1号の打ち上げは1957年10月4日
- [NASA「50 Years Ago: Launch of Salyut, the World's First Space Station」](https://www.nasa.gov/missions/station/50-years-ago-launch-of-salyut-the-worlds-first-space-station/) — 世界初の宇宙ステーション サリュート1号の打ち上げは1971年4月19日

**人工衛星が先、宇宙ステーションが後**です（14年の差）。どちらもロケットで運び上げられています。Unityの画面でも、衛星は時代の側で先に上がり、拠点は月の段階に入ってから上がります。いずれも軌道上へ先に現れることはなく、必ず地表から機体が上がってから置かれます。高度・速度・打ち上げにかかる時間は表していません。

巨大生物の時代から人類までの4時代は、次を参照しています。

- [NASA Science「Deep Impact and the Mass Extinction of Species 65 Million Years Ago」](https://science.nasa.gov/earth/deep-impact-and-the-mass-extinction-of-species-65-million-years-ago/)・[NSF「A moment that changed Earth」](https://www.nsf.gov/science-matters/moment-changed-earth) — 約6600万年前、直径10〜15kmの小天体がユカタン半島へ衝突し、舞い上がった塵が数年から数十年にわたり日光をさえぎった。地表の温度は最大で28度ほど下がり、鳥を除く恐竜を含むおよそ76%の種が絶滅した
- [NOAA NCEI「Glacial-Interglacial Cycles」](https://www.ncei.noaa.gov/sites/default/files/2021-11/1%20Glacial-Interglacial%20Cycles-Final-OCT%202021.pdf) — 第四紀（約260万年前から現在）を通じて氷期と間氷期がくり返されている
- [Smithsonian Human Origins「Our species arose at least 300,000 years ago」](https://humanorigins.si.edu/research/whats-hot-human-origins/our-species-arose-least-300000-years-ago)・[「Introduction to Human Evolution」](https://humanorigins.si.edu/education/introduction-human-evolution) — 私たちの種は少なくとも約30万年前に現れ、食べ物を作って周囲を変えはじめたのはここ約1万2000年のこと

**衝突の冬と氷期は別の出来事です。** 衝突の直後に続いた暗い空は数年から数十年、氷期は6400万年あとの第四紀に始まります。画面では隣り合う時代として並びますが、そのあいだの隔たりは表していません。**宇宙から見た地球には、恐竜そのものを描いていません。** 宇宙から見た地球に生きものは写らないためで、氷の少なさ・緑の広さ・割れ始めた大陸で時代を表しています。**地表に降りたときは別です。** そこでは生きものを描いており、決めごとは[地表の描き方](docs/design/SURFACE_SCENE_SPEC.md)にあります。

映像そのものは引用していません。参考にしたのは順序と、何が何から生じたかという関係だけです。

画面に出る絵はすべて計算で描いており、外部の図版や写真は使っていません。公的機関の図版を載せる場合は、NASAの画像はクレジット（例：NASA、NASA/JPL-Caltech）を添えて、推薦・提携を示唆せずロゴも使わない条件で、JAXAの画像は利用規約に従い出典を明記して扱います。

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
  design/SURFACE_SCENE_SPEC.md
  design/BAKED_CREATURES.md
  reports/P0_BROWSER_MOCK_IMPLEMENTATION.md
  reports/R2_P0_HUMAN_REVIEW.md
  reports/R1_P0_HUMAN_REVIEW.md
  reports/R1_P1_UNITY_IMPLEMENTATION.md
  reports/EARTH_SEASONS_FROM_TILTED_AXIS.md
  reports/EARTH_BRIGHTNESS_AND_SHORELINE.md
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
- [季節の実装報告](docs/reports/EARTH_SEASONS_FROM_TILTED_AXIS.md)
- [地表の描き方（仕様）](docs/design/SURFACE_SCENE_SPEC.md)
- [獣を網目に焼いて地表へ置く（手順書）](docs/design/BAKED_CREATURES.md)
- [地表の調査報告](docs/reports/SURFACE_SCENE_RESEARCH.md)
- [明るさと海岸線の報告](docs/reports/EARTH_BRIGHTNESS_AND_SHORELINE.md)
