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
| 氷期の獣 | **CC0 のマンモスは見つからなかった。** Poly Pizza の象を加工するか、いまの手組みを残す | — | — |
| 骨格（衝突後の演出に使うなら） | **Smithsonian Open Access** | CC0（印を確認） | 不要 |

**マンモスだけは、条件を満たすものが見つからなかった。**
CC0 を名乗るものは Meshy（AI生成）くらいで、Sketchfab のものは有償か CC BY-NC だった。
象を牙ごと作り替えるか、いまの手組みの獣を残すかになる。

---

## 5. クレジットの置き方

CC BY のものを入れる場合に備えて、置き場所を決めておく。

- `unity/CivilizationToSpace/Assets/Resources/Nature/SOURCES.md` — いまも植物の出どころを書いている
- 画面から読める場所（「説明を出す」の中）に1行足す
- `docs/reports/FREE_ASSET_SURVEY.md`（この文書）に一覧を残す

CC BY で必要なのは、**作者名・作品名・ライセンス名・元のURL・改変した旨**の5つである。
CC0 では不要だが、書いておいて損はない。
