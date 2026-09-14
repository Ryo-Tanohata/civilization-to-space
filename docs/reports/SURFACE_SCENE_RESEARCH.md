# 地表の場面をつくるための調査と、その反映

対象：`unity/CivilizationToSpace/Assets/Scripts/View/SurfaceView.cs` ほか
状態：試作。**実行時からは呼ばれておらず、公開ビルドの見え方は変わらない。**

## なぜ調べ直したか

最初の試作は、地表の見た目を調べずに作っていた。
この作品はこれまで、地球の形成も月の形成も衛星の順序も、必ず出典を取って
「何を表していないか」を書いてきた。地表だけ想像で作るのは釣り合わない。

調べた結果、**作ったものと事実が食い違う点が2つ見つかった。**

## 1. 中生代に草原は存在しない

草（イネ科）が広い面積を覆うようになるのは**中新世（約2300万〜530万年前）**である。
C4型の草による草原が各地で成立するのは後期中新世（約1100万〜500万年前）で、
急速に広がったのは**約700万〜600万年前**とされる。

恐竜が栄えた中生代（白亜紀の終わりは約6600万年前）は、**それよりずっと前**である。

- 出典：[Nature Scientific Reports「The contribution of fire to the late Miocene spread of grasslands in eastern Eurasia」](https://www.nature.com/articles/s41598-019-43094-w)

**最初の試作は一面を芝生の緑にしていた。誤りである。**
地面を裸の土と落ち葉の色（`#6E5C3E`）へ変えた。緑はシダと木だけが持つ。

白亜紀の植物としては、針葉樹・ソテツ・シダ・トクサに加え、
**被子植物（花の咲く植物）が約1億年前に現れ、急速に主要な植物になった**ことが
分かっている。いま置いているのは針葉樹・シダ・広葉樹の3種で、この範囲に収まる。

- 出典：[NPS「Cretaceous Period—145.0 to 66.0 MYA」](https://www.nps.gov/articles/000/cretaceous-period.htm)、
  [NPS「Plant Fossils」](https://www.nps.gov/subjects/fossils/plant-fossils.htm)、
  [NPS「Late Cretaceous Flora in an Ancient Fluvial Environment」](https://www.nps.gov/articles/aps-v8-i2-c2.htm)

## 2. 新石器時代の集落は、三角屋根の家が散らばった姿ではない

チャタルホユック（紀元前7100年ごろ〜5950年ごろ、トルコ）は、
**日干しレンガの長方形の家が背中合わせに隙間なく詰まり、家のあいだに道が無い。**
屋根は**平ら**で、人は屋根の上を歩き、天井の穴から梯子で家へ降りた。
屋根は松やビャクシンの梁で支えられていた。

- 出典：[UNESCO「Neolithic Site of Çatalhöyük」](https://whc.unesco.org/en/list/1405/)、
  [Çatalhöyük Research Project「Architecture」](http://www.catalhoyuk.com/site/architecture)

**最初の試作は、三角屋根の家をばらばらに散らしていた。誤りである。**
次の3点を直した。

| 直したこと | 内容 |
| --- | --- |
| 屋根 | 円錐をやめ、平らな屋根にした |
| 並べ方 | 散らすのをやめ、升目の上へ詰めて置く（`BuildingSpacing` で間隔を決める。1.05で背中合わせ） |
| 窓 | 高さで決めるのをやめ、データで持つ（`Windows`）。集落では入れない |

`BuildingSpacing` を2.1にすると道を挟んで並び、都市になる。
**段階ごとに別の形を作っていない。** 高さ・数・間隔・窓の有無という4つの数だけで、
集落から都市までを通す。作り分けると、段階が増えるたびに書き足すことになり破綻する。

## 3. 大きな四つ足の生きものの寸法

アパトサウルスは全長およそ22メートル、**肩の高さはおよそ4.5メートル**とされる。
マメンチサウルスは全長およそ18メートル、肩の高さおよそ3.4メートル。
最も長いものではアルゼンチノサウルスなどが全長50メートルに達する。

- 出典：[AMNH「Apatosaurus excelsus」](https://www.amnh.org/explore/ology/ology-cards/006-apatosaurus-excelsus)、
  [AMNH「Mamenchisaurus の大きさ」](https://amnh.org/exhibitions/sauropods-worlds-largest-dinosaurs/outside-mamenchisaurus/size)、
  [AMNH「Sauropods Guide」](https://www.amnh.org/explore/news-blogs/sauropod-identification-guide)

**長いが、肩はそれほど高くない。** 高さは持ち上げた首が作る。
最初の試作は脚を胴の高さの0.55倍取っており、竹馬に乗ったように浮いて見えた。
脚を0.30倍へ短くし、胴を長くした。

## 表していないこと

- **特定の種を表していない。** 首・胴・脚・尾の長さの比を変えているだけである
- **復元色ではない。** 色は読みやすさのための決めである。化石から分かるのは骨格までで、
  皮膚の色も質感も分かっていない
- 植物の種類も特定していない。針葉樹・シダ・広葉樹という3つの形しか持たない
- 生きものや建物の数・分布・密度は表していない
- チャタルホユック以外の集落の姿は調べていない。世界中の集落を代表するものではない
- 都市の姿は特定の都市に基づいていない

## まだ直していないところ

- 地面が平らで起伏が無い
- 地面の空きが画面の4割ほどあり、間延びしている
- 白亜紀に現れたとされる被子植物を、形の上で針葉樹と描き分けていない
