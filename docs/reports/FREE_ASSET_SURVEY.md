# 恐竜と建物に使える、無料の3D素材の調査

調べた日：2026年9月16日
対象：地表の場面の生きもの（恐竜・氷期の獣）と建物
いまの素材：植物は Poly Haven（CC0）。生きものと建物は球と直方体の組み立て。

## この作品にとっての「使える」条件

「無料」だけでは足りない。**この作品は素材ファイルを公開リポジトリに置いている**ため、
使うだけでなく**再配布**にあたる。ここが選別のいちばん大事な軸になる。

| 条件 | なぜ要るか |
| --- | --- |
| 再配布できる | 加工したファイルを `Assets/Resources/` に入れて GitHub で公開している |
| 改変できる | 面を減らし、絵を512へ落とし、板に焼く加工を必ず通す |
| クレジットを出せる | 出典を書く欄は作れる。**表示が要るライセンスでも構わない** |

**非商用（NC）のものは、結論として要らない。**
ご指摘のとおりこの公開は商用ではないので NC でも条件は満たす。しかし下に挙げるとおり
CC0 と CC BY だけで必要なものは揃う。NC を入れると、あとで作品を別の形にするとき
（展示・配布・受賞作品集への収録など）に一度きりの判断が足かせになる。
**選べるなら NC を選ばない**ほうがよい、というのが調べた上での見立てである。

---

## 1. 生きもの（恐竜・氷期の獣）

### 使えるもの

| 素材 | ライセンス | 中身 | 再配布 | 備考 |
| --- | --- | --- | --- | --- |
| [Gobkit Free Dinosaur Pack](https://gobkit.itch.io/gobkit-free-dinosaur-pack) | **CC0 1.0** | 恐竜10種・リグ+アニメ（待機/攻撃/死/歩き 24fps）・GLB・8.9MB | 可 | **いちばん近い**。ただし竜脚類が無い |
| [Poly Pizza](https://poly.pizza/search/Dinosaur) | **CC0** と **CC BY 3.0** の混在（約69%が CC BY） | Apatosaurus・Diplodocus・T-Rex・Triceratops ほか | 可 | **竜脚類（首の長い四つ足）がある**。モデルごとに表示を確認 |
| [OpenGameArt CC0 3D Animals/Creatures](https://opengameart.org/content/cc0-3d-animals-creatures) | **CC0** | Brontosaurus・Dromaeosaur・Dinosaur ほか | 可 | Brontosaurus は .blend。作者自身が「テクスチャに満足していない」と書いている |
| [Smithsonian Open Access](https://3d.si.edu/collections/openaccesshighlights) | **CC0**（ただし後述） | 実物の3Dスキャン2,000点超。恐竜は**骨格** | 可 | 写真計測なので重い。Blender で面を減らす前提 |
| [Sketchfab](https://sketchfab.com/tags/dinosaur) | **モデルごとに違う**（CC0 / CC BY / CC BY-NC / 有償） | 例：[Tyrant King - Tyrannosaurus](https://sketchfab.com/3d-models/tyrant-king-tyrannosaurus-6465a297fa784598adc49f6e0042d449)（CC BY・4096² の PBR） | 表示次第 | **写実に最も近い**が、1点ずつ確認が要る |

**Smithsonian には注意が要る。** すべてが CC0 ではない。
「CC0 の表示があるものは自由に使えるが、指定が無いものには利用条件が付く」と
[Open Access FAQ](https://www.si.edu/openaccess/faq) に書かれている。モデルのページで CC0 の印を見ること。

### 使えない／気をつけるもの

**[Quaternius](https://quaternius.com/) — 2026年8月28日にライセンスが変わっている。**

長く CC0 として知られ、検索でも今なお「CC0」と出てくるが、
[公式のライセンスページ](https://quaternius.com/license.html)は
**Quaternius Asset License (QAL) v1.0（最終更新 2026/08/28）** になっている。

> 3. Restrictions — a) Resell or redistribute the Assets themselves.
> You may not extract, repackage, sublicense, sell, or otherwise redistribute the Assets
> (in original or modified form) as a standalone asset, asset pack, stock file, template,
> or similar product … **This restriction applies regardless of how much the Assets have been modified.**

- 完成した作品（WebGL ビルド）に**組み込んで配るのは明確に許されている**
- **素材ファイルそのものを置き直すのは禁じられている**（改変後も）
- クレジットは不要

この作品はファイルをリポジトリに置いているので、**そのままでは条件に触れるおそれがある。**

ややこしいのは、**個別のパックのページ**（例：[Animated Dinosaur Pack](https://quaternius.com/packs/animateddinosaurs.html)）
には**いまも「CC0」と書かれている**ことである。サイト全体のライセンスページと食い違っている。
また [Poly Pizza 上の Quaternius のモデル](https://poly.pizza/m/wuerCFCWNR)は
「Public Domain (CC0)」として配られている。CC0 は一度与えると撤回できないとされるため、
CC0 として配られた版は CC0 のままと考えられるが、**どの版を CC0 として入手したかを
自分で示せるようにしておく**（ページの控えを残す）のが安全である。

**Free3D・CGTrader・TurboSquid の「無料」モデルは避ける。**
多くが「Personal Use Only」「Editorial Use Only」で、再配布どころか
公開作品への組み込みも認めていないものが混じる。1点ずつの確認に見合わない。

**Meshy（AI生成）は二段構えになっている。** ギャラリーの Public 素材は CC0 だが、
無料プランで自分が生成したモデルは CC BY 4.0（Meshy へのクレジットが要る）。
混同しやすい。

---

## 2. 建物

| 素材 | ライセンス | 中身 | 再配布 | 備考 |
| --- | --- | --- | --- | --- |
| [Kenney City Kit (Commercial)](https://kenney.nl/assets/city-kit-commercial) | **CC0** | 50種の変化 | 可 | 表示不要。素材の定番 |
| [Kenney City Kit (Roads)](https://kenney.nl/assets/city-kit-roads) | **CC0** | 90種の変化・道路・標識・信号 | 可 | 上と組み合わせる前提 |
| Kenney City Kit (Industrial) / (Suburban) | **CC0** | 工場・住宅地 | 可 | 時代8（集落）にも流用できる |
| [KayKit City Builder Bits](https://kaylousberg.itch.io/city-builder-bits) | **CC0** | 無料で32種以上・OBJ/FBX/GLTF・1024²の一枚絵 | 可 | 作者は「無改変の再販はしないで」と希望を添えている |
| [PLATEAU（国土交通省）](https://www.mlit.go.jp/plateau/) | **PDL1.0（CC BY 4.0 互換）** | 実在の日本の都市。東京23区は[FBX で配布](https://www.geospatial.jp/ckan/dataset/plateau-tokyo23ku-fbx-2020)。主要部は LOD2（テクスチャ付き） | 可（条件つき） | **出典表示が必須**。改変したことも明記が要る |
| [ambientCG](https://ambientcg.com/) | **CC0** | Facade011・Facade015 ほか PBR 素材2,000点超 | 可 | **いまの作りに最も合う**（後述） |
| [Poly Haven](https://polyhaven.com/textures) | **CC0** | テクスチャ・HDRI・モデル | 可 | すでに植物で使っている |

**PLATEAU の条件**（[サイトポリシー](https://www.mlit.go.jp/plateau/site-policy/)）：

- 出典を「出典：国土交通省 PLATEAUウェブサイト（URL）」の形で記す
- 改変した場合は**改変した旨を併記**し、国が作成したかのように見せない
- 一部データには測量法など個別の法的制限がかかる

商用利用も可、と明記されている。**この作品にとっての障害は法ではなく大きさ**で、
東京23区の FBX は Web に載せるには桁が違う。使うなら1街区だけ切り出すことになる。

---

## 3. もうひとつの問題 — 見た目が揃わない

ライセンスとは別に、**混ぜると絵が崩れる**という問題がある。

いまの植物は Poly Haven の**写真計測**（実物をスキャンしたもの）である。
上に挙げた恐竜・建物の多くは**低ポリゴンで平らな色**の素材で、並べると
そこだけ語彙が違って見える。Gobkit のパックは「unlit baked-color（陰影なしの一枚絵）」と
明記されており、写実の草の横に置くと玩具に見える。

取れる道は3つある。

1. **写実に寄せる** — Sketchfab の CC BY で PBR テクスチャ付きの恐竜を1点ずつ選び、
   建物は ambientCG の facade テクスチャを**いまの直方体に貼る**。
   モデルを増やさずに写実へ寄せられるので、容量も描画も増えない。**いちばん費用対効果が高い。**
2. **低ポリに寄せる** — Kenney + KayKit + Gobkit で全部そろえ、植物も低ポリに差し替える。
   統一感はいちばん出るが、これまでの Poly Haven の加工が無駄になる。
3. **いまの板に焼く方式を生きものにも広げる** — 恐竜は近く大きく映るので、板では破綻する。**採らない。**

---

## 4. おすすめ

| 用途 | 使うもの | ライセンス | クレジット |
| --- | --- | --- | --- |
| 建物（時代9・10の街） | **ambientCG の facade / concrete テクスチャ**をいまの直方体に貼る | CC0 | 不要（任意で記す） |
| 建物（時代8の集落） | **Kenney City Kit (Suburban)** から低い家を選ぶ、または土壁テクスチャ | CC0 | 不要 |
| 恐竜（竜脚類） | **Poly Pizza の Apatosaurus / Diplodocus** | CC0 または CC BY 3.0 | 必要なら出す |
| 恐竜（二足） | **Gobkit Free Dinosaur Pack**（T-Rex・Carnotaurus） | CC0 | 不要 |
| 氷期の獣 | **Smithsonian Open Access の骨格（CC0）を下敷きに、肉と毛皮を付ける** | CC0（印を確認） | 不要 |
| 骨格（衝突後の演出に使うなら） | **Smithsonian Open Access** | CC0（印を確認） | 不要 |

**完成形のマンモスは、条件を満たすものが見つからなかった。**
CC0 を名乗るものは Meshy（AI生成）くらいで、Sketchfab のものは有償か CC BY-NC だった。

**2026年9月21日の追記：骨格から肉付けする。**
完成形は無いが、**骨格ならある**ことをオーナーが確かめた。
Smithsonian Open Access の骨格（CC0）を下敷きにし、その上へ肉と毛皮を付ける。
[マンモスの試作](../prototypes/mammoth.html)は、高い肩・下がる背・丸い頭頂・小さな耳
という体つきから身と毛皮を組み立てたもので、この肉付けの先取りにあたる。
**使う骨格の個体と、その CC0 の印はまだ確認していない。**

---

## 5. クレジットの置き方

CC BY のものを入れる場合に備えて、置き場所を決めておく。

- `unity/CivilizationToSpace/Assets/Resources/Nature/SOURCES.md` — いまも植物の出どころを書いている
- 画面から読める場所（「説明を出す」の中）に1行足す
- `docs/reports/FREE_ASSET_SURVEY.md`（この文書）に一覧を残す

CC BY で必要なのは、**作者名・作品名・ライセンス名・元のURL・改変した旨**の5つである。
CC0 では不要だが、書いておいて損はない。

---

## 6. 追記 — 古代遺跡・時代ごとの街（2026年9月21日）

調べた日：2026年9月21日
対象：1〜5章で扱っていない**古代遺跡（ピラミッドなど）**と**時代ごとの街並み（古代・中世・近代）**
状態：**案。仕様の改訂が先に要る**（6.3）。素材の導入・ファイルの追加は行っていない。

### 6.1 古代遺跡

| 素材 | ライセンス | 中身 | 再配布 | 備考 |
| --- | --- | --- | --- | --- |
| **計算で作る四角錐 + [ambientCG](https://ambientcg.com/) の石材テクスチャ** | CC0 | ピラミッド・段状の基壇など単純な形 | 可 | **いちばん合う**。いまの「直方体に質感を貼る」作りと同じで、容量も描画も増えない |
| [Sketchfab](https://sketchfab.com/) の文化財スキャン（CC0） | **CC0** | 美術館・博物館による実物のスキャン | 可 | CC0 を付けられるのは**文化機関だけ**とされている（[2020年の発表](https://sketchfab.com/blogs/community/sketchfab-launches-public-domain-dedication-for-3d-cultural-heritage/)）。現在も同じ運用かは未確認 |
| Sketchfab の文化財スキャン（CC BY） | **CC BY** | 例：[The Ancient City of Histria](https://sketchfab.com/3d-models/the-ancient-city-of-histria-6d02bb7ae1194c7eaba876e1a8379204)（ローマ時代の都市遺跡・ドローン計測） | 表示次第 | 写実に最も近い。**写真計測で重い**ため、Blender で面を減らす前提 |
| Free3D・CGTrader・TurboSquid・Open3dModel の「無料」ピラミッド | モデルごとに違う | 多数 | 多くは不可 | 1章と同じ理由で**避ける** |

**ピラミッドは素材を探さないほうがよい。** 形が四角錐で足りるため、外から持ち込むと
容量と確認の手間だけが増える。質感だけを CC0 のテクスチャで足す。
ambientCG で使う石材（砂岩・石灰岩の類）の**具体的な素材名は未確認**である。

### 6.2 時代ごとの街

| 時代の姿 | 使うもの | ライセンス | 備考 |
| --- | --- | --- | --- |
| 古代の街 | 計算で作る低い箱 + 土壁・日干し煉瓦の類のテクスチャ | CC0 | 4章の「集落」と同じ作り方 |
| 中世の城・街 | [Kenney Castle Kit](https://kenney.nl/assets/castle-kit)（75点）・[Kenney Fantasy Town Kit](https://kenney.nl/assets/fantasy-town-kit)（160点） | **CC0** | 表示不要。**低ポリで平らな色**なので、3章の「見た目が揃わない」問題がそのまま出る |
| 近代〜現代の街 | 2章の Kenney City Kit・ambientCG の facade（既存の案のまま） | CC0 | 変更なし |
| 日本の実在の街 | 2章の PLATEAU（既存の案のまま） | PDL1.0 | 変更なし |

**中世は、Kenney をそのまま置くと浮く。** 取れる道は3章と同じで、
（a）形だけ Kenney から借りて ambientCG の石材を貼り直す、
（b）遠景に置いて小ささで差を目立たせない、のどちらか。**（a）を推す。**

### 6.3 先に決めること（仕様との衝突）

素材よりも先に、次の2点をオーナーが決める必要がある。

| # | 衝突 | 現在の決め | 決めること |
| --- | --- | --- | --- |
| 1 | **実在のものを描くか** | [地表の仕様](../design/SURFACE_SCENE_SPEC.md)は「特定の種を表さない」「形の語彙は8つ」。README は「恐竜そのものは描かない」 | 実在の恐竜・ピラミッドを描くなら、仕様とREADMEを改訂する |
| 2 | **古代〜近代の街をどこに置くか** | いまの10時代に居場所がない。[ロードマップ](../planning/ROADMAP.md)では文明はR3 | 時代を足すか、R3として切り出すか |

### 6.4 手戻りの少ない順序（案）

1. 6.3 の2点を決め、仕様を改訂する
2. ピラミッドを計算＋テクスチャで**1基だけ**置いて、写実の植物との並びを確かめる
3. 恐竜は4章のとおり Gobkit か Poly Pizza から**1体だけ**入れて、同じく並びを確かめる
4. 並びが成り立ったら、中世の街を（a）の方式で試す

### 6.5 調べていないこと

- **恐竜以外の動物**は、4章（マンモスは条件を満たすものが無い）から追加で調べていない
- ambientCG の石材の具体的な素材名
- Sketchfab の CC0 が現在も文化機関に限られるか
- 上の素材はどれも**ダウンロード・加工・Unity での表示を試していない**

---

## 6.6 既存文書との突き合わせ（2026年9月21日）

6.3 が挙げた「仕様との衝突」を、既存の文書と1件ずつ突き合わせた結果である。
**調べたところ、多くは衝突ではなかった。** 判断が要ったのは1点だけだった。

### 突き合わせた結果

| 6.3 の主張 | 突き合わせた結果 |
| --- | --- |
| READMEの「恐竜そのものは描かない」と衝突する | **衝突しない。** この記述は宇宙から見た地球についてのもので、理由も「宇宙から見た地球に生きものは写らないため」と書かれている。地表には及ばない。地表では既に四つ足・二足を描いている |
| 地表仕様の「特定の種を表さない」と衝突する | **ここだけが本当の争点だった**（下の決定を参照） |
| 地表仕様の「形の語彙は8つ」と衝突する | **衝突しない。** 仕様は自ら6つから8つへ増やしており、「1つで複数の場面を助けることを確かめてから足す」という条件つきである。さらにピラミッドは 6.1 のとおり計算で作る四角錐で足り、建物語彙の変種にあたる。語彙は増えない |
| 古代〜近代の街にいまの10時代で居場所がない | **R1の中にはない。R1の外にはある。** [ロードマップ](../planning/ROADMAP.md)のR3が「農業・都市・産業の追加」であり、[時代カタログ](../design/EARTH_ERA_CATALOG.md)の「将来追加する候補」にもAgriculture・Civilization・Industrialが名指しで載っている |

### 決まったこと（2026年9月21日・オーナー判断）

| # | 決定 | 反映先 |
| --- | --- | --- |
| 1 | **作れるものは作り、多いものは借りる。** ピラミッドのような単純な建造物はUnityの計算かBlenderで自作する。恐竜・動物・草木・街の建物は無料（CC0）の素材から採る | [地表の描き方](../design/SURFACE_SCENE_SPEC.md)の決めごと3を改訂した |
| 2 | **古代〜近代の街はR3で扱う。時代は足さない** | ロードマップとカタログの既定どおりであり、文書の改訂は要らない |

決定1により、6.1 の「ピラミッドは素材を探さないほうがよい」はそのまま採る。
6.2 の中世の街は、素材から採る側に入る。

**決定1は方針であって、実装ではない。** 恐竜もマンモスもいまだ自作のままである。

### R1に時代を足さない理由

足すと [R1要件](../requirements/REQUIREMENTS_R1.md) の広い範囲が変わる。
FR-01とFR-06の「10時代」、AC-01〜AC-09のスライダー段数・再生の終端・Unityとの一致、
そして「スコープ内」の記述である。R3として切り出せば、どれも動かさずに済む。

**なお、R1要件の「スコープ内」「前提条件」「AC-01」「AC-09」は、いまも「6時代」と
書かれたままで、本文の「10時代」と食い違っている。** これは6.3とは別の、既存の
書き落としである。直すには受入基準の改訂にあたるため、ここでは指摘のみとする。

### まだ残っていること

- 素材はどれも**ダウンロード・加工・WebGLでの表示を試していない**（6.5 のまま）
- ambientCGの石材の具体的な素材名、SketchfabのCC0が現在も文化機関に限られるかも未確認
- 恐竜・動物は時代ごとに当たりが違う。**マンモスは完成形が無く、骨格（CC0）から
  肉付けすると決まった**（4章の追記）。他の時代も、採れる素材があるかは個別に確かめる

---

## 7. 追記 — 生きものと人の素材（2026年9月21日）

調べた日：2026年9月21日
対象：衝突後の動物（氷期の獣をふくむ）と、人類
状態：**案。素材はまだ入れていない。** 7.1 は当日に配布元で確かめた。

### 7.1 ライセンスの前提 — Quaternius は CC0 ではなくなっている

**9月16日の調査で見つかったライセンス変更は、実在した。**
2026年9月21日に配布元で確かめた結果が下表である。
この作品は素材ファイルを公開リポジトリへ置くため、冒頭の条件どおり
**再配布を許すかどうかが選別の軸**になる。

| 配布元 | 2026年9月21日の状態 | 再配布 |
| --- | --- | --- |
| **[Quaternius](https://quaternius.com/license.html)** | **CC0 ではない。** 独自の Quaternius Asset License (QAL) になり、「これは CC0 ではない」「Quaternius が権利を持つ」と明記されている。使用・改変・商用利用は無償で許され、クレジットも不要 | **不可。** 「素材そのものを、無償・有償を問わず単体で再配布・再販してはならない」とある |
| [Kenney](https://kenney.nl/support) | **CC0 のまま。** 「asset ページの素材はすべて public domain (CC0)」と明記 | 可 |
| [Poly Haven](https://polyhaven.com/license) | CC0（3章のまま） | 可 |
| [Smithsonian Open Access](https://www.si.edu/openaccess) | CC0 の印が付いたものは CC0 | 可 |
| [MakeHuman](http://www.makehumancommunity.org/content/license_explanation.html) | ソフトは AGPL だが、**書き出したモデルは CC0** として扱える。ただし**公式かつ未改変の MakeHuman の書き出し機能で出したもの**に限る | 可（条件つき） |

**これは4章の結論に直接あたる。** 4章は「恐竜（二足）は Gobkit Free Dinosaur Pack、CC0」
「Poly Pizza の動物」を挙げているが、**Poly Pizza に並ぶ動物の多くは Quaternius 作である**。
QAL の版であれば、リポジトリへは置けない。**Gobkit の現在のライセンスは未確認である。**

**使うか捨てるかの分かれ目は「CC0 かどうか」ではなく「再配布できるか」である。**
QAL は使うだけなら自由だが、この作品の置き方とは噛み合わない。

### 7.2 衝突後の動物の層

**層。** 生きた姿は低ポリばかりで、写実なのは骨格スキャンである。
ケナガマンモスの骨格は
[Smithsonian が CC0 で公開](https://www.si.edu/object/3d/mammuthus-primigenius-blumbach:341c96cd-f967-4540-8ed1-d3fc56d31f12)
しており（*Mammuthus primigenius*）、Sketchfab からも入手できる。
4章で「マンモスは無い」としたのは**生きた姿の話**で、骨格ならある。

**道すじ。** 生きた獣は Poly Pizza にアニメーション付きで CC0 の牡鹿・狼・馬などがある。
ただし多くは Quaternius 作で、7.1 のとおり**いまは QAL であり再配布できない**。
使うなら、CC0 と表示されていた版の配布ページを控えておく必要がある。

**顔ぶれ。** 同じ Poly Pizza に象やサイ（Poly by Google 作）もあり、
象を牙と毛で作り替えればマンモス、サイからケサイが作れる。
**こちらのライセンスはモデルごとに未確認である。**

**試作で分かったこと。** [マンモスの試作](../prototypes/mammoth.html)は、
高い肩・下がる背・丸い頭頂・小さな耳という体つきから、身と毛皮を
形の組み合わせと揺らぎだけで組み立てている。**素材ファイルを使わずに、
牙・鼻・毛皮まで読み取れる姿になった。**
**マンモスは素材が要らない見込みがある。** 骨格を下敷きにするかどうかも、
この試作を地表へ置いて確かめてから決めるのがよい。

### 7.3 人類の層

**層。** 化石人類の実物スキャンは手に入りにくい。
Smithsonian の化石人類3Dは**他の博物館が所有する標本の複製**のため、
ダウンロードできない。

**道すじ。** 化石人類は、博物館が CC BY で出している復元頭骨（例：*Homo erectus*）を
1点ずつ使う形になる。
**人物の見た目は、この作品の「民族の優劣を描かない」方針に触れやすい。
肌や顔立ちを特定の集団に寄せない決めを、素材を選ぶより先に置くのが安全である。**

**顔ぶれ。** 動く人物は [Kenney Animated Characters](https://kenney.nl/assets/animated-characters-3) が
CC0 で、骨組みと待機・ジャンプ・走りの動きが付いている。
写実寄りなら MakeHuman で、書き出したモデルは CC0 として扱える（7.1 の条件つき）。

### 7.4 先に決めること

| # | 決めること | なぜ |
| --- | --- | --- |
| 1 | **人物の見た目の決めごと** | 「民族の優劣を描かない」に触れる。肌・顔立ちを特定の集団に寄せない旨を[地表の描き方](../design/SURFACE_SCENE_SPEC.md)へ先に書く |
| 2 | マンモスを素材から採るか、試作の自作で進むか | 試作が成立しているため、素材が要らない可能性がある |
| 3 | 恐竜を素材から採るか | 7.1 により 4章の候補が揺らいでいる。Gobkit の現在のライセンスを確かめてから決める |

### 7.5 葉の落ちた立木が無い

**氷期と衝突の両方で、葉の無い立木が要る。** 氷期は遠景の写真が
「まばらな裸の木」であり、手前もそれに合わせたい。衝突は葉が落ちたあとの
姿を出したい。いまある `imp_dead_trunk` は**横たわった幹**で、立たない。

| 当たった先 | 結果 |
| --- | --- |
| [Poly Haven](https://polyhaven.com/models/nature/trees) | [dead_tree_trunk](https://polyhaven.com/a/dead_tree_trunk)・[dead_tree_trunk_02](https://polyhaven.com/a/dead_tree_trunk_02)・[tree_stump_01](https://polyhaven.com/a/tree_stump_01) はいずれも**倒木か切り株**で、立木ではない。一覧はJavaScriptで組み立てられており、全点は確かめられていない |
| [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) | CC0・330点。tree / foliage の札は付いているが、**枯れた立木が含まれるかは未確認**。落とさないと分からない |

**低ポリを混ぜると絵柄が揃わない。** 3章と同じ問題が出る。
遠くに小さく置くか、形だけ借りて写真の質感を貼り直すかになる。

### 7.6 調べていないこと

- **Gobkit の現在のライセンス**（4章は CC0 としているが、未再確認）
- Poly Pizza の個々のモデルの現在のライセンス。作者ごとに違い、表示が古いことがある
- Sketchfab のマンモス骨格が Smithsonian のものと同一か
- 博物館が CC BY で出している復元頭骨の具体的な所在
- 上の素材はどれも**ダウンロード・加工・WebGL での表示を試していない**
