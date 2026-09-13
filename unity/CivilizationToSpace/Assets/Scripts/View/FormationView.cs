using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 地球ができるまでの演出。ぶつかってくる小さな天体、大きな天体、散らばった破片。
    ///
    /// **これは仮説に基づく象徴表現である。** 月の成り立ちはジャイアントインパクト説を採っているが、
    /// 決着した事実ではない。質量・個数・速度・衝突エネルギー・角度を一切持たない。
    /// 大きさも速さも、画面で読み取れることだけを基準に決めた値である。
    ///
    /// 段階に入ってからの時間で動かす。止まった絵を並べるのではなく、
    /// 集まる様子とぶつかる瞬間そのものを見せるためである。
    /// </summary>
    public sealed class FormationView : MonoBehaviour
    {
        /// <summary>ぶつかってくる小さな天体の持ち数。</summary>
        private const int SwarmPoolSize = 26;

        /// <summary>散らばった破片の持ち数。</summary>
        private const int DebrisPoolSize = 44;

        /// <summary>ぶつかった跡の光の持ち数。</summary>
        private const int FlashPoolSize = 10;

        /// <summary>小さな天体が現れる距離。地球の半径を1としたときの倍率。</summary>
        private const float SwarmStartRadius = 3.4f;

        /// <summary>
        /// 微惑星が地表へ届くまでに地球のまわりを回る回数。
        /// 少なすぎるとまっすぐ落ちて見え、多すぎると近づいていることが読めない。
        /// </summary>
        private const float SwarmTurns = 2.25f;

        /// <summary>集積の段階で、2個から全部まで増えきるまでの時間（秒）。</summary>
        private const float AccretionSeconds = 3.4f;

        /// <summary>
        /// 合体が進みきったときに残る数。
        /// ここまで減った2つがぶつかって、塊が育っていく。
        /// </summary>
        private const int AccretionEndCount = 2;

        /// <summary>粒だったころの大きさ。地球の半径を1としたときの倍率。</summary>
        private const float AccretionDustSize = 0.028f;

        /// <summary>微惑星まで育ったときの大きさ。地球の半径を1としたときの倍率。</summary>
        private const float AccretionBodySize = 0.16f;

        /// <summary>
        /// 数が減りきるまでの区間。集積の時間に対する割合。
        /// ここを過ぎると、残った微惑星どうしがぶつかる場面になる。
        /// </summary>
        private const float AccretionMergeSpan = 0.72f;

        /// <summary>塊が育ち始める時点。集積の時間に対する割合。</summary>
        private const float AccretionGrowFrom = 0.62f;

        /// <summary>軌道面の傾きの散らばり（度）。小さいほど円盤に近づく。</summary>
        private const float AccretionDiskTilt = 14f;

        /// <summary>溶け具合が新しい値へ移るまでの時間（秒）。</summary>
        private const float MoltenBlendSeconds = 2.4f;

        /// <summary>
        /// 段階を移ったときに、数や大きさが新しい値へ追いつくまでの時間（秒）。
        /// 1段階ぶん（1倍速で4秒）より短くして、次へ進む前に落ち着くようにする。
        /// </summary>
        private const float StageBlendSeconds = 1.3f;

        /// <summary>ぶつかって飛び出す大きな塊の半径。地球の半径を1としたときの倍率。</summary>
        private const float FragmentScale = 0.30f;

        /// <summary>大きな塊が飛びのく距離。地球の半径を1としたときの倍率。</summary>
        private const float FragmentTravel = 2.3f;

        /// <summary>飛びのく勢いが収まるまでの時間（秒）。</summary>
        private const float FragmentEaseSeconds = 0.65f;

        /// <summary>ぶつかって砕けたときに飛び散る塊の持ち数。</summary>
        private const int ShardPoolSize = 26;

        /// <summary>砕けてから、いちばん散らばるまでの時間（秒）。</summary>
        private const float ShatterOutSeconds = 0.8f;

        /// <summary>
        /// 砕けてから、球へ戻りきるまでの時間（秒）。
        ///
        /// 以前は2.8秒で、ぶつかった直後に元の丸い地球へ戻っていた。
        /// 地球ほどの大きさの、しかも溶けた天体は自らの引力で丸くなるが、
        /// 「欠けた → すぐ元通り」に見えるほど速くはない。
        /// 破片が回り込みながら戻る時間を取り、丸くなるまでを長くしている。
        /// **この秒数は見え方で決めた値であり、実際の時間ではない。**
        /// </summary>
        private const float ShatterSeconds = 4.0f;

        /// <summary>
        /// 散った塊が地球のまわりを回り込む角度の上限（度）。
        ///
        /// まっすぐ出てまっすぐ戻ると、跳ね返って落ちただけに見える。
        /// 飛び出した物質は地球を回る軌道に乗ったとされるため、
        /// 出るときも戻るときも、横へ回り込ませる。
        /// **軌道を解いているわけではなく、回り込んで見えるようにしているだけである。**
        /// </summary>
        private const float ShardSwingDegrees = 145f;

        /// <summary>
        /// 散った塊のうち、地球へ戻らず輪に残る割合。
        ///
        /// 飛び散った物質の多くは地球へ落ち戻り、残りが地球を巡る円盤になって
        /// そこから月が集まった、とされる。落ち戻る側を多数にしている。
        /// 出典: NASA Astrobiology "Tracking Formation of the Earth and Moon",
        /// LPI "The Moon's Formation and Evolution"。
        /// </summary>
        private const float ShardEscapeShare = 0.3f;

        /// <summary>
        /// いちばん崩れたときに、地球が縮む割合。
        /// 欠けで壊れ具合を表すようにしたので、縮みはごくわずかにとどめる。
        /// 大きく縮めると、壊れたのではなく遠ざかったように見える。
        /// </summary>
        private const float ShatterMinScale = 0.94f;

        /// <summary>抉れる範囲の半径。地球の半径を1としたときの倍率。</summary>
        private const float ShatterCutRadius = 1.05f;

        /// <summary>抉れる中心を、地球の中心からどれだけ衝突側へ寄せるか。</summary>
        private const float ShatterCutOffset = 0.95f;

        /// <summary>塊が散らばる距離。地球の半径を1としたときの倍率。</summary>
        private const float ShatterSpread = 1.35f;

        /// <summary>破片の輪の半径。</summary>
        private const float DebrisRadius = 2.0f;

        /// <summary>大きな天体が近づいてぶつかるまでの時間（秒）。</summary>
        private const float ImpactTravelSeconds = 3.8f;

        /// <summary>
        /// 大きな天体が現れる距離。画面に収めている半径を1としたときの倍率。
        ///
        /// 以前は地球の半径の3.3倍という、画面の中ほどの位置だった。
        /// そこにいきなり現れるため、近づいてきたのではなく湧いたように見えていた。
        ///
        /// 画面の広さから決めているのは、収めている半径だけが
        /// 「どこまでが画面の内か」を知っている値だからである。
        /// 地球の半径を基準にすると、画面の縦横比や説明欄の開閉で
        /// 画面の外から入るか、内側に湧くかが変わってしまう。
        ///
        /// 1.35倍は、収めている縁のすぐ外にあたる。これより遠くすると、
        /// 近づく時間の大半を画面の外で使い、見えている時間が短くなる。
        /// </summary>
        private const float ImpactorStartFrames = 1.35f;

        /// <summary>
        /// 大きな天体が現れる向き。ここから地球の中心へ向かって落ちてくる。
        /// 画面の右上の外から入り、手前側の地表へ当たるようにしている。
        /// 当たる場所が奥側だと、抉れた跡が地球の裏に隠れて見えない。
        /// </summary>
        private static readonly Vector3 ImpactorStartDirection =
            new Vector3(0.56f, 0.74f, -0.37f);

        /// <summary>
        /// 近づくにつれてどれだけ速くなるか。0で等速、1で最後だけ速い。
        ///
        /// 以前は1（速さが0から始まる）で、遠くではほとんど止まって見え、
        /// 画面へ入ってから一気に詰め寄っていた。
        /// ぶつかってきた天体は止まっていたところから落ちたのではなく、
        /// もともと太陽のまわりを回っていたとされるので、
        /// はじめから速さを持たせ、近づくぶんだけ速くする。
        /// </summary>
        private const float ImpactApproachCurve = 0.55f;

        /// <summary>ぶつかったあと、破片が輪へ落ち着くまでの時間（秒）。</summary>
        private const float DebrisSpreadSeconds = 1.6f;

        /// <summary>
        /// 破片が集まって月になるまでの時間（秒）。
        ///
        /// 以前は段階の切り替えと同じ1.3秒で、月がその場で膨らむだけだった。
        /// 月は地球を巡る破片の円盤から集まってできたとされ、
        /// 集積は数百年、その大半は初めの100年ほどとされる。
        /// ここでは「輪の破片が寄り集まって球になる」ことだけを、
        /// 段階のあいだをかけて見せる。**年数は表していない。**
        /// 出典: NASA Astrobiology "Tracking Formation of the Earth and Moon"。
        /// </summary>
        private const float MoonAccretionSeconds = 5.4f;

        /// <summary>
        /// 月へ吸い寄せられる破片の、出発をずらす幅。
        /// 0だと全部が一斉に動き、1だと最後の1個が終わりぎわに出発する。
        /// 一斉に消えると、集まったのではなく消えたように見える。
        /// </summary>
        private const float MoonGatherStagger = 0.55f;

        /// <summary>
        /// 月になる前に、破片がいったんまとまる塊の数。
        ///
        /// 破片が一つずつ月へ吸い込まれるだけでは、
        /// 「引力で結合しながら育った」のではなく「月が吸い取った」ように見える。
        /// 円盤の中では、まず破片どうしがくっついて大きな塊になり、
        /// その塊どうしがさらに合わさって月になった、とされる。
        /// </summary>
        private const int MoonClumpCount = 3;

        /// <summary>
        /// 集積のうち、破片どうしが塊になるまでに使う割合。
        /// 残りで、その塊どうしが一つに合わさる。
        /// </summary>
        private const float MoonClumpPhase = 0.58f;

        /// <summary>
        /// 塊ができているあいだに見せる、月の芯の大きさ。
        /// 0にすると月が何も無いところから現れることになり、
        /// 塊が合わさって月になった、という順が見えない。
        /// </summary>
        private const float MoonSeedScale = 0.22f;

        /// <summary>塊が次の大きさになるまでの時間（秒）。</summary>
        private const float GrowSeconds = 1.4f;

        /// <summary>ぶつかった跡の光が消えるまでの時間（秒）。集積の小さな光に使う。</summary>
        private const float FlashSeconds = 0.32f;

        /// <summary>
        /// 巨大衝突の光が消えるまでの時間（秒）。集積の光より長く残す。
        /// 一瞬で消えると、ぶつかったことに気づけないためである。
        /// </summary>
        private const float ImpactFlashSeconds = 0.7f;

        /// <summary>ぶつかった天体が地球へ沈み込んで見えなくなるまでの時間（秒）。</summary>
        private const float SinkSeconds = 0.36f;

        /// <summary>ぶつかってきた天体が砕けて散るかけらの持ち数。</summary>
        private const int ImpactorShardPoolSize = 22;

        /// <summary>
        /// ぶつかってきた天体のかけらのうち、地球へ落ち込まず輪に残る割合。
        ///
        /// 定説とされる模型では、円盤の物質はぶつかってきた天体のマントルが
        /// 主（6割超）で、原始地球からのぶんは2割ほどとされる。
        /// ここで残る側を半分にしているのは、そのかけらが月の材料になるまでの
        /// 筋道を目で追えるようにするためである。
        /// **実際には、ぶつかってきた天体の大半は地球と一体になったとされ、
        /// 円盤に残ったのは月2個分ほどとされる。この割合は質量比ではない。**
        /// **また、月の同位体組成が地球とほぼ同じである理由は決着しておらず、
        /// 「月は主に衝突天体から来た」という点自体が論点として残っている。**
        /// 出典: NASA Astrobiology "Tracking Formation of the Earth and Moon",
        /// LPI "The Moon's Formation and Evolution",
        /// NTRS "Origin of the Moon, Impactor Theory"。
        /// </summary>
        private const float ImpactorShardKeepShare = 0.5f;

        /// <summary>砕けたかけらが、元の天体の形から離れきるまでの時間（秒）。</summary>
        private const float ImpactorBreakSeconds = 0.9f;

        /// <summary>ぶつかった衝撃で地球が揺れている時間（秒）。</summary>
        private const float ShakeSeconds = 0.9f;

        /// <summary>揺れの大きさ。地球の半径を1としたときの倍率。</summary>
        private const float ShakeAmplitude = 0.085f;

        /// <summary>揺れの速さ（1秒あたりの往復回数）。</summary>
        private const float ShakeFrequency = 7.5f;

        private static readonly Color32 RockColor = new Color32(0x77, 0x6B, 0x60, 0xFF);
        private static readonly Color32 HotRockColor = new Color32(0xC8, 0x6A, 0x38, 0xFF);
        private static readonly Color FlashColor = new Color(1f, 0.72f, 0.38f, 1f);

        private Transform swarmRoot;
        private Transform debrisRoot;
        private Transform impactorRoot;
        private Transform flashRoot;
        private GameObject impactor;

        private GameObject[] swarm;
        private float[] swarmProgress;

        /// <summary>軌道面を表す直交する2本。位置は この2本の合成で決める。</summary>
        private Vector3[] swarmAxisU;
        private Vector3[] swarmAxisV;

        /// <summary>軌道のつぶれ具合。0で円、1に近いほど細長い楕円。</summary>
        private float[] swarmEccentricity;

        /// <summary>軌道上のどこから始めるか。</summary>
        private float[] swarmPhase;

        /// <summary>粒ごとの元の大きさ。集積では全体の大きさだけを動かす。</summary>
        private Vector3[] swarmSizes;

        private GameObject[] debris;
        private Vector3[] debrisTargets;

        /// <summary>破片ごとの元の大きさ。端数を大きさで表すときの基準。</summary>
        private Vector3[] debrisSizes;

        private GameObject[] flashes;
        private float[] flashLife;
        private float[] flashSize;

        /// <summary>それぞれの光の寿命。薄くしていく割合を出すのに使う。</summary>
        private float[] flashMaxLife;
        private Material flashMaterial;

        private MotionSettings motion;
        private Transform earth;
        private float earthRadius;

        private FormationStage stage;
        private float elapsed;

        /// <summary>いま集積の段階かどうか。数と大きさの増やし方を変える。</summary>
        private bool accreting;

        /// <summary>この段階で目指す溶け具合。</summary>
        private float moltenTarget;

        /// <summary>
        /// いま見せている量と、目指す量。
        /// 段階を移った瞬間に切り替えると、数が一度に変わって画が飛ぶ。
        /// 目指す量だけを先に置き、見せている量は時間をかけて寄せる。
        /// </summary>
        private float swarmShown;
        private float swarmTarget;
        private float debrisShown;
        private float debrisTarget;
        private float moonShown;
        private float moonTarget;

        /// <summary>ぶつかって破片を撒いている最中か。そのあいだは破片を自前で扱う。</summary>
        private bool debrisBurst;

        /// <summary>いま月が破片から集まっている最中か。そのあいだは破片を自前で扱う。</summary>
        private bool moonAccreting;

        /// <summary>この段階に入った時点の破片の量。月へ渡すぶんを数えるのに使う。</summary>
        private float debrisEntry;
        private float scaleFrom = 1f;
        private float scaleTo = 1f;

        private Vector3 impactorStart;
        private bool impactHappened;

        /// <summary>ぶつかった位置。沈み込みと揺れの向きに使う。</summary>
        private Vector3 impactPoint;

        /// <summary>ぶつかってからの経過秒。沈み込みと揺れの進み具合に使う。</summary>
        private float sinceImpact;

        /// <summary>ぶつかる前の衝突天体の大きさ。沈み込みで縮めるときの基準。</summary>
        private Vector3 impactorScale;

        /// <summary>ぶつかって飛び出す大きな塊。のちの月にあたる。</summary>
        private GameObject fragment;

        /// <summary>砕けた地球の塊。散らばってから、また集まって球へ戻る。</summary>
        private GameObject[] shards;
        private Vector3[] shardDirections;
        private Vector3[] shardSizes;

        /// <summary>塊が回り込む軸と、回り込む角度。まっすぐ出入りさせないために持つ。</summary>
        private Vector3[] shardAxes;
        private float[] shardSwings;

        /// <summary>その塊が地球へ戻らず輪に残るか。残るぶんが月のもとになる。</summary>
        private bool[] shardEscapes;

        /// <summary>
        /// 破片が寄り集まってできる塊。これが合わさって月になる。
        ///
        /// 破片と同じ立方体にしてある。角ばった形のほうが、
        /// この画面の他の破片と揃い、崩した絵として読みやすいためである。
        /// 「まだ丸くなっていないもの」と「丸くなった月」の違いは、
        /// 最後に月が球として現れるところで出る。
        /// </summary>
        private GameObject[] moonClumps;

        /// <summary>塊の元の大きさ。育ち具合を掛けるときの基準。</summary>
        private Vector3 moonClumpScale;

        /// <summary>
        /// ぶつかってきた天体が砕けたかけら。地球の塊とは別に持つ。
        /// 見た目も出どころも違い、こちらは月の材料になる側である。
        /// </summary>
        private GameObject[] impactorShards;

        /// <summary>かけらが、砕ける前の天体のどこにあったか。中心からの向きと距離。</summary>
        private Vector3[] impactorShardSeats;
        private Vector3[] impactorShardDirections;
        private Vector3[] impactorShardSizes;
        private Vector3[] impactorShardAxes;
        private float[] impactorShardSwings;

        /// <summary>そのかけらが輪に残るか。残らないものは地球へ落ち込む。</summary>
        private bool[] impactorShardKeeps;

        /// <summary>塊が飛び出した場所と向き。</summary>
        private Vector3 fragmentOrigin;
        private Vector3 fragmentDirection;

        /// <summary>地球と衝突天体が画面へ入るために必要な半径。</summary>
        public float FramedRadius
        {
            get { return earthRadius * SwarmStartRadius * 1.15f; }
        }

        /// <summary>月が現れる度合い。0で見えず、1で本来の大きさ。</summary>
        public float MoonEmergence { get; private set; }

        /// <summary>
        /// 月ができる場所（この見せ物の中の座標）。<see cref="AppRoot"/> が渡す。
        /// 破片をそこへ寄せ集めるために要る。渡されないうちは原点のままで、
        /// そのときは破片を寄せずに輪へ置いたままにする。
        /// </summary>
        public Vector3 MoonAnchor { get; set; }

        /// <summary>抉れている球の中心（この見せ物の中の座標）。</summary>
        public Vector3 CutCenter { get; private set; }

        /// <summary>抉れている球の半径。0で抉れていない。</summary>
        public float CutRadius { get; private set; }

        /// <summary>
        /// 地球と月がどれだけ溶けて見えるか。0で通常、1でもっとも赤い。
        ///
        /// できたばかりの地球は全体が溶けており、巨大衝突のあとも
        /// しばらくは地球も月も熱いままだったとされる。
        /// 段階ごとの値を <see cref="AppRoot"/> が読み、地球と月の両方へ渡す。
        /// </summary>
        public float MoltenAmount { get; private set; }

        public void SetMotionSettings(MotionSettings settings)
        {
            motion = settings;
        }

        public void Build(Transform earthTransform, float radius, HideFlags flags)
        {
            earth = earthTransform;
            earthRadius = radius;

            swarmRoot = CreateRoot("Swarm", flags);
            debrisRoot = CreateRoot("Debris", flags);
            impactorRoot = CreateRoot("Impactor", flags);
            flashRoot = CreateRoot("Flash", flags);
            debrisRoot.localRotation = Quaternion.Euler(18f, 0f, 8f);

            BuildSwarm(flags);
            BuildDebris(flags);
            BuildMoonClumps(flags);
            BuildImpactor(flags);
            BuildImpactorShards(flags);
            BuildFragment(flags);
            BuildShards(flags);
            BuildFlashes(flags);

            Apply(null, false);
        }

        /// <summary>段階を写す。null を渡すと何も無い状態になる。</summary>
        /// <param name="next">写す段階。</param>
        /// <param name="first">
        /// いちばん最初の段階かどうか。ここだけ、何も無いところから始めて
        /// 数と大きさを増やしていく。並び順で決めるので、識別子の綴りに依存しない。
        /// </param>
        public void Apply(FormationStage next, bool first)
        {
            accreting = next != null && first;

            // 集積の段階だけ、何も無いところから始める。
            // 以前は最初から0.45の大きさの地球が置いてあり、
            // 「集まってできた」のではなく「初めからあった」ように見えていた。
            scaleFrom = accreting
                ? 0f
                : (stage != null ? (float)stage.BodyScale : (next != null ? (float)next.BodyScale : 1f));
            scaleTo = next != null ? (float)next.BodyScale : 1f;

            // 撒いている最中の破片は debrisShown に数えていない。
            // そのまま次の段階へ移ると、いま出ている破片をいったん全部消してから
            // 出し直すことになり、輪が一瞬またたく。撒いた量を引き継いでおく。
            if (debrisBurst && stage != null)
            {
                debrisShown = (float)stage.Debris;
            }

            stage = next;
            elapsed = 0f;
            impactHappened = false;
            sinceImpact = 0f;

            // 前の段階の揺れと沈み込みを持ち越さない。
            if (earth != null)
            {
                earth.localPosition = Vector3.zero;
            }

            impactor.transform.localScale = impactorScale;

            ApplySwarmCount();

            // 破片は、ぶつかる前は出さない。ぶつかってから広がる。
            var burstStage = next != null && next.Impactor > 0.5d;
            debrisBurst = burstStage;
            debrisTarget = burstStage ? 0f : (next != null ? (float)next.Debris : 0f);
            if (burstStage)
            {
                debrisShown = 0f;
                for (var i = 0; i < debris.Length; i++)
                {
                    debris[i].SetActive(false);
                }
            }
            else
            {
                // 前の段階で月へ寄せた破片が、寄せた先に置き去りにならないようにする。
                for (var i = 0; i < debris.Length; i++)
                {
                    debris[i].transform.localPosition = debrisTargets[i];
                }
            }

            // 大きな塊は、ぶつかる段階でだけ出す。次の段階では月として別に現れる。
            if (fragment != null)
            {
                fragment.SetActive(false);
            }

            // 砕けたかけらも塊も持ち越さない。動きを減らしているときは
            // Update が早く返るため、ここで消しておかないと出たままになる。
            if (impactorShards != null)
            {
                for (var i = 0; i < impactorShards.Length; i++)
                {
                    impactorShards[i].SetActive(false);
                }
            }

            HideMoonClumps();

            impactor.SetActive(burstStage);
            if (burstStage)
            {
                impactor.transform.localPosition = impactorStart;
            }

            moonTarget = next != null ? (float)next.Moon : 1f;
            swarmTarget = next != null ? (float)next.Swarm : 0f;

            // 月が現れる段階では、破片が寄り集まって月になるところを見せる。
            // それまでの段階から続けて入ってきたときだけで、
            // 月ができたあとの段階へ戻ってきたときは、ふつうの寄せ方に任せる。
            moonAccreting = next != null && next.Moon > 0.5d && moonShown < 0.5f;
            debrisEntry = Mathf.Max(debrisShown, debrisTarget);

            // 動きを減らしているときは、途中を見せずにその段階の姿にする。
            if (motion != null && motion.Reduced)
            {
                moonAccreting = false;
                moonShown = moonTarget;
                swarmShown = swarmTarget;
                debrisShown = debrisTarget;
            }

            MoonEmergence = moonShown;

            // 溶け具合は段階の並びで決める。
            //   集積・原始地球・巨大衝突 … 溶けたまま
            //   月の形成 …………………… 冷えはじめる。ぶつかったあともしばらくは熱い
            //   時代へ入ったら ………… 通常へ戻す
            moltenTarget = next == null ? 0f : (next.Moon > 0.5d ? 0.55f : 1f);

            for (var i = 0; i < flashes.Length; i++)
            {
                flashes[i].SetActive(false);
                flashLife[i] = 0f;
            }

            // 動きを減らしているときは、その段階の落ち着いた姿をすぐ出す。
            if (motion != null && motion.Reduced)
            {
                Settle();
            }
        }

        private void Update()
        {
            if (motion != null && motion.Reduced)
            {
                ApplyScale(1f);

                // 揺れの途中で動きを減らしても、地球がずれたまま止まらないようにする。
                if (earth != null)
                {
                    earth.localPosition = Vector3.zero;
                }

                return;
            }

            elapsed += SceneClock.Delta;

            if (accreting)
            {
                // 集積の段階は、数と大きさを同じ時計で増やす。
                // 微惑星が増えるにつれて塊が育つ、という関係を目で追えるようにする。
                ApplySwarmCount();
                ApplyScale(AccretionGrowth());
            }
            else
            {
                ApplyScale(Mathf.Clamp01(elapsed / GrowSeconds));
            }

            if (debrisRoot != null)
            {
                debrisRoot.Rotate(Vector3.up, 22f * SceneClock.Delta, Space.Self);
            }

            MoveSwarm();
            MoveImpactor();

            // ぶつかったあとの時計は、沈み込みと揺れの両方が使う。
            // 沈み込みの中で進めると、沈み終わって隠れた時点で時計が止まり、
            // 揺れが終わらなくなる。
            if (impactHappened)
            {
                sinceImpact += SceneClock.Delta;
            }

            SinkImpactor();
            MoveImpactorShards();
            ShakeEarth();
            ShatterEarth();
            MoveFragment();
            BlendMolten();
            BlendStage();
            MoveDebris();
            UpdateFlashes();
        }

        /// <summary>塊の大きさを、前の段階から今の段階へ寄せていく。</summary>
        private void ApplyScale(float t)
        {
            if (earth == null)
            {
                return;
            }

            // 砕けているあいだは、そのぶん小さくする。散らばった塊が戻るにつれて元へ戻る。
            earth.localScale = Vector3.one * (Mathf.Lerp(scaleFrom, scaleTo, t) * ShatterScale());
        }

        /// <summary>
        /// 小さな天体を外から中心へ落とす。表面へ届いたら光を残して消え、外から出し直す。
        /// 「ぶつかりながら集まっている」ことだけを表す。回数を数えていない。
        /// </summary>
        private void MoveSwarm()
        {
            if (swarm == null)
            {
                return;
            }

            var surface = earthRadius * CurrentScale();

            for (var i = 0; i < swarm.Length; i++)
            {
                if (!swarm[i].activeSelf)
                {
                    continue;
                }

                swarmProgress[i] += SceneClock.Delta * (0.30f + (i % 5) * 0.07f);

                if (swarmProgress[i] >= 1f)
                {
                    // 表面へ届いた。跡の光を残して、別の軌道で出し直す。
                    SpawnFlash(SwarmPosition(i, 1f, surface), earthRadius * 0.12f);
                    swarmProgress[i] = 0f;
                    ResetSwarmOrbit(i);
                }

                swarm[i].transform.localPosition = SwarmPosition(i, swarmProgress[i], surface);
                swarm[i].transform.Rotate(Vector3.one, 120f * SceneClock.Delta, Space.Self);
            }
        }

        /// <summary>
        /// 段階を移ったときの数や大きさを、少しずつ新しい値へ寄せる。
        ///
        /// 段階の切れ目でいきなり数が変わると、そこだけ画が飛んで見える。
        /// 数は整数なので、そのまま増減させても1個ずつ現れて目に付く。
        /// 端数は「いま現れかけの1個」の大きさで表し、育つように見せる。
        /// </summary>
        private void BlendStage()
        {
            var step = SceneClock.Delta / StageBlendSeconds;

            if (moonAccreting)
            {
                // 破片が寄り集まって球になるまで。段階のあいだをかけて育てる。
                //
                // すでに見えているぶんより小さくはしない。前の段階から戻ってきて
                // 月が残っているときに、いったん縮んでから育て直すと画が飛ぶ。
                moonShown = Mathf.Max(moonShown, MoonGathering());
            }
            else
            {
                moonShown = Mathf.MoveTowards(moonShown, moonTarget, step);
            }

            MoonEmergence = moonShown;

            // 集積の段階は、数の増やし方をそちらが持っている。
            if (!accreting)
            {
                var before = swarmShown;
                swarmShown = Mathf.MoveTowards(swarmShown, swarmTarget, step);
                if (!Mathf.Approximately(before, swarmShown))
                {
                    ShowPool(swarm, swarmSizes, swarmShown, SwarmPoolSize);
                }
            }

            // ぶつかって撒いている最中と、月へ寄せている最中は、破片をそちらが持っている。
            if (!debrisBurst && !moonAccreting)
            {
                var before = debrisShown;
                debrisShown = Mathf.MoveTowards(debrisShown, debrisTarget, step);
                if (!Mathf.Approximately(before, debrisShown))
                {
                    ShowPool(debris, debrisSizes, debrisShown, DebrisPoolSize);
                }
            }
        }

        /// <summary>
        /// 持ち数のうち、いくつを出すかを決める。
        /// 端数は、いま現れかけの1個の大きさで表す。
        /// </summary>
        private static void ShowPool(GameObject[] pool, Vector3[] sizes, float amount, int size)
        {
            if (pool == null || sizes == null)
            {
                return;
            }

            var exact = Mathf.Clamp01(amount) * size;
            var whole = Mathf.FloorToInt(exact);
            var partial = exact - whole;

            for (var i = 0; i < pool.Length; i++)
            {
                if (i < whole)
                {
                    pool[i].SetActive(true);
                    pool[i].transform.localScale = sizes[i];
                }
                else if (i == whole && partial > 0.02f)
                {
                    pool[i].SetActive(true);
                    pool[i].transform.localScale = sizes[i] * partial;
                }
                else
                {
                    pool[i].SetActive(false);
                }
            }
        }

        /// <summary>
        /// 溶け具合を、段階の値へ少しずつ寄せる。
        /// 急に冷えると切り替わりに見えるため、時間をかけて色を移す。
        /// </summary>
        private void BlendMolten()
        {
            if (Mathf.Approximately(MoltenAmount, moltenTarget))
            {
                return;
            }

            MoltenAmount = Mathf.MoveTowards(
                MoltenAmount, moltenTarget, SceneClock.Delta / MoltenBlendSeconds);
        }

        /// <summary>
        /// いま出しておく微惑星の数を決める。
        ///
        /// 集積の段階では、向かい合う2個から始めて、時間とともに増やしていく。
        /// 最初から全部出すと「もともと大勢いた」ように見えてしまい、
        /// 少しずつ集まって育っていく過程が読み取れない。
        /// ほかの段階では、その段階の値のまま一定にする。
        /// </summary>
        private void ApplySwarmCount()
        {
            var amount = stage != null ? (float)stage.Swarm : 0f;
            var full = Mathf.RoundToInt(amount * SwarmPoolSize);

            if (!accreting)
            {
                for (var i = 0; i < swarm.Length; i++)
                {
                    swarm[i].SetActive(i < full);
                    swarm[i].transform.localScale = swarmSizes[i];
                }

                return;
            }

            // 数は多いところから減り、そのぶん一粒ずつが大きくなる。
            // 「たくさんの粒がぶつかって、より大きなかけらへ育つ」という順序にあたる。
            var t = Mathf.Clamp01(elapsed / AccretionSeconds);
            var merged = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / AccretionMergeSpan));

            var visible = Mathf.Max(
                AccretionEndCount, Mathf.RoundToInt(Mathf.Lerp(full, AccretionEndCount, merged)));
            var size = Mathf.Lerp(AccretionDustSize, AccretionBodySize, merged) * earthRadius;

            for (var i = 0; i < swarm.Length; i++)
            {
                var on = i < visible;
                swarm[i].SetActive(on);
                if (on)
                {
                    // 粒ごとの差は残したまま、全体の大きさだけを動かす。
                    swarm[i].transform.localScale = swarmSizes[i].normalized * (size * 1.732f);
                }
            }
        }

        /// <summary>
        /// 集積の段階で、中心の塊が育つ度合い。
        /// 粒が微惑星へまとまるまでは育てず、そのあとで大きくする。
        /// 先に育てると、粒が集まる前から惑星があるように見えてしまう。
        /// </summary>
        private float AccretionGrowth()
        {
            var t = Mathf.Clamp01(elapsed / AccretionSeconds);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(AccretionGrowFrom, 1f, t));
        }

        /// <summary>
        /// 微惑星ひとつの位置を、楕円軌道の上で求める。
        ///
        /// **この順序にしている根拠。**
        /// 順番は次のとおりで、ひとつ飛ばすと話が変わってしまう。
        ///   1. 円盤に多数の塵とかけらがある
        ///   2. 引力による衝突が頻繁に起こり、徐々に大きなかけらへ育って微惑星となる
        ///   3. その微惑星どうしが衝突を繰り返す
        ///   4. さらに大きな惑星へ育つ
        ///
        /// はじめから大きな天体を2つ置くと、2が抜けて「もともと大きかった」話になる。
        /// そのためここでは、多数の小さな粒から始め、数を減らしながら一粒を大きくし、
        /// 最後に残った塊どうしがぶつかって中心が育つ、という順で見せている。
        ///
        /// 参照した資料：
        ///   丸山茂徳ほか・冥王代生命学研究グループ
        ///   （平成26年度 文部科学省科学研究費補助金・新学術領域研究）による
        ///   太陽系と地球の誕生から生命の誕生・進化までの映像資料
        ///   NASA Astrobiology「How did our Solar System form?」
        ///   https://astrobiology.nasa.gov/education/alp/how-did-our-solar-system-form/
        ///   Lunar and Planetary Institute「Active Accretion」
        ///   https://www.lpi.usra.edu/education/orexlaunch/Active%20Accretion.pdf
        ///
        /// **数値はすべて見せ方である。** 粒の数・大きさ・軌道の傾き・周回数は
        /// 実際の個数でも寸法でも軌道要素でも、かかった時間でもない。
        ///
        /// **数値の意味。** 半長径・離心率・周回数は見やすさのために決めた値であり、
        /// 実際の軌道要素でも、衝突の頻度でも、かかった時間でもない。
        /// </summary>
        /// <param name="index">何番目の微惑星か。</param>
        /// <param name="progress">0で軌道の外側、1で地表。</param>
        /// <param name="surface">いまの地表までの距離。</param>
        private Vector3 SwarmPosition(int index, float progress, float surface)
        {
            // 半長径が縮んでいく。これが「だんだん近づく」ことにあたる。
            var apart = Mathf.Lerp(earthRadius * SwarmStartRadius, surface, progress);

            // 近づくほど円に近づける。最後まで細長いと、地表を素通りして見える。
            var eccentricity = swarmEccentricity[index] * (1f - progress);

            var angle = swarmPhase[index] + progress * SwarmTurns * Mathf.PI * 2f;

            // 焦点を地球に置いた楕円。angle=0 が最も遠い側になる。
            var radius = apart * (1f - eccentricity * eccentricity)
                         / (1f + eccentricity * Mathf.Cos(angle));

            return (swarmAxisU[index] * Mathf.Cos(angle) + swarmAxisV[index] * Mathf.Sin(angle)) * radius;
        }

        /// <summary>
        /// 微惑星に軌道面と形を割り当てる。
        ///
        /// 軌道面はばらばらではなく、ひとつの面の近くへ寄せる。
        /// もとになった塵とかけらは円盤状に広がっていたとされており、
        /// 球状に散らすと、その円盤が消えてしまう。
        /// </summary>
        private void ResetSwarmOrbit(int index)
        {
            // 真上からわずかに傾けた向きを軌道面の法線にする。傾きが小さいほど円盤に近い。
            var normal = Quaternion.Euler(
                Random.Range(-AccretionDiskTilt, AccretionDiskTilt),
                Random.Range(0f, 360f),
                Random.Range(-AccretionDiskTilt, AccretionDiskTilt)) * Vector3.up;

            var u = Vector3.Cross(normal, Vector3.forward);
            if (u.sqrMagnitude < 0.001f)
            {
                u = Vector3.Cross(normal, Vector3.right);
            }

            swarmAxisU[index] = u.normalized;
            swarmAxisV[index] = Vector3.Cross(normal, swarmAxisU[index]).normalized;
            swarmEccentricity[index] = Random.Range(0.25f, 0.55f);
            swarmPhase[index] = Random.Range(0f, Mathf.PI * 2f);
        }

        /// <summary>
        /// 大きな天体を近づけ、表面へ届いたところでぶつかったことにする。
        /// 破壊の描写も閃光の大きさも、規模を述べないよう控えめにする。
        /// </summary>
        private void MoveImpactor()
        {
            if (impactor == null || !impactor.activeSelf || impactHappened)
            {
                return;
            }

            var t = Mathf.Clamp01(elapsed / ImpactTravelSeconds);

            // はじめから速さを持たせ、近づくぶんだけ速くする。
            // 以前は t*t で、速さが0から始まっていた。遠くにいるあいだは
            // ほとんど動かず、画面へ入ってから一気に詰め寄るため、
            // 「遠くから近づいてきた」ではなく「急に現れた」に見えていた。
            var eased = t * (1f - ImpactApproachCurve) + t * t * ImpactApproachCurve;
            var contact = impactorStart.normalized * (earthRadius * CurrentScale() + earthRadius * 0.5f);
            impactor.transform.localPosition = Vector3.Lerp(impactorStart, contact, eased);
            impactor.transform.Rotate(Vector3.one, 40f * SceneClock.Delta, Space.Self);

            if (t < 1f)
            {
                return;
            }

            // ここが接触の瞬間。以前はすぐ消していたため、ぶつかった感じが出ていなかった。
            // 沈み込みと揺れを始め、光を強めに出す。
            impactHappened = true;
            impactPoint = contact;
            sinceImpact = 0f;

            // contact はぶつかる天体の「中心」であり、地表より自分の半径ぶん外にある。
            // 光をそこへ置くと、地球の横で光っているように見えてしまう。
            // 光と輪と破片は、実際に触れた地表の点から出す。
            var surfacePoint = impactorStart.normalized * (earthRadius * CurrentScale());

            SpawnFlash(surfacePoint, earthRadius * 0.55f, ImpactFlashSeconds);
            SpawnImpactRing(surfacePoint);
            BurstDebris(surfacePoint);
            LaunchFragment(surfacePoint);
        }

        /// <summary>
        /// ぶつかった天体を地球へ沈ませ、縮めて見えなくする。
        /// 一瞬で消すと「当たった」ではなく「消えた」に見えるため、短く見せる。
        ///
        /// 縮むのは、砕けたかけらが <see cref="MoveImpactorShards"/> で
        /// 外へ出ていくぶんである。球のまま小さくなるのではなく、
        /// 球からかけらが抜けていって残りが地球に埋まる、という順で見える。
        /// </summary>
        private void SinkImpactor()
        {
            if (impactor == null || !impactHappened || !impactor.activeSelf)
            {
                return;
            }

            var t = Mathf.Clamp01(sinceImpact / SinkSeconds);

            // 地表からさらに内側へ、自分の半径ぶんだけ潜らせる。
            var depth = earthRadius * 0.5f * t;
            impactor.transform.localPosition = impactPoint - impactPoint.normalized * depth;
            impactor.transform.localScale = impactorScale * (1f - t);
            impactor.transform.Rotate(Vector3.one, 90f * SceneClock.Delta, Space.Self);

            if (t >= 1f)
            {
                impactor.SetActive(false);
                impactor.transform.localScale = impactorScale;
            }
        }

        /// <summary>
        /// ぶつかってきた天体が砕けたかけらを動かす。
        ///
        /// **なぜ砕くのか。** これまで、ぶつかってきた天体は地球へ潜って
        /// そのまま消えていた。月は飛び散った物質が集まってできたとされ、
        /// その物質は主にぶつかってきた天体のものだったとされる。
        /// 潜って消えるだけでは、月の材料がどこから来たのかが画面に出てこない。
        ///
        /// かけらは、砕ける前の天体の形の中から出て外へ広がる。
        /// 半分は地球へ落ち込んで消え、残りは外の輪の高さまで出て、
        /// 終わりぎわに輪へ引き継ぐ。その輪が次の段階で月になる。
        ///
        /// **軌道は解いていない。** 散る向きも、残る割合も、
        /// 筋道が見えるようにこちらで決めた値である。
        /// </summary>
        private void MoveImpactorShards()
        {
            if (impactorShards == null)
            {
                return;
            }

            var active = impactHappened && sinceImpact < ShatterSeconds;
            if (!active)
            {
                for (var i = 0; i < impactorShards.Length; i++)
                {
                    if (impactorShards[i].activeSelf)
                    {
                        impactorShards[i].SetActive(false);
                    }
                }

                return;
            }

            var t = Mathf.Clamp01(sinceImpact / ShatterSeconds);

            // 割れて離れるのは速く、そのあとの散り方はゆっくり。
            var apart = Mathf.Clamp01(sinceImpact / ImpactorBreakSeconds);
            var spread = Mathf.SmoothStep(0f, 1f, apart);

            // 落ち込む側が地球へ吸い込まれるまでの進み具合。
            var fall = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.72f, t));

            // 終わりぎわに消して、同じ場所にある破片の輪へ引き継ぐ。
            var settle = Mathf.InverseLerp(0.78f, 1f, t);

            // 輪までの距離。かけらはぶつかった点から外へ向かって広がる。
            var ringReach = earthRadius * DebrisRadius;

            for (var i = 0; i < impactorShards.Length; i++)
            {
                impactorShards[i].SetActive(true);

                // 砕ける前の座席から始め、そこから外へ離れていく。
                var seat = impactPoint + impactorShardSeats[i];
                var keep = impactorShardKeeps[i];

                // 残る側は輪の高さへ、落ちる側はぶつかった点の近くへ引き戻す。
                var reach = keep
                    ? ringReach * spread
                    : earthRadius * ShatterSpread * spread * (1f - fall);

                var swing = Quaternion.AngleAxis(impactorShardSwings[i] * t, impactorShardAxes[i]);
                var offset = swing * (impactorShardDirections[i] * reach);
                impactorShards[i].transform.localPosition =
                    Vector3.Lerp(seat, impactPoint.normalized * (earthRadius * 0.9f), spread) + offset;

                impactorShards[i].transform.localScale =
                    impactorShardSizes[i] * (keep ? 1f - settle : 1f - fall);
                impactorShards[i].transform.Rotate(Vector3.one, 120f * SceneClock.Delta, Space.Self);
            }
        }

        /// <summary>
        /// ぶつかった衝撃で地球を揺らす。ぶつかった向きへ押されてから、
        /// だんだん収まる。動きを減らしているときは呼ばれない。
        /// </summary>
        private void ShakeEarth()
        {
            if (earth == null || !impactHappened)
            {
                return;
            }

            var t = sinceImpact / ShakeSeconds;
            if (t >= 1f)
            {
                earth.localPosition = Vector3.zero;
                return;
            }

            // 減衰する振動。最初が大きく、終わりへ向けて0に戻る。
            var damping = 1f - t;
            var wave = Mathf.Sin(sinceImpact * ShakeFrequency * Mathf.PI * 2f);
            var push = -impactPoint.normalized;
            earth.localPosition = push * (wave * damping * damping * earthRadius * ShakeAmplitude);
        }

        /// <summary>
        /// 砕けている度合いから、地球の大きさに掛ける割合を出す。
        /// ぶつかった直後がいちばん小さく、塊が戻るにつれて1へ戻る。
        /// </summary>
        private float ShatterScale()
        {
            if (!impactHappened)
            {
                return 1f;
            }

            var t = Mathf.Clamp01(sinceImpact / ShatterSeconds);
            var out1 = Mathf.Clamp01(sinceImpact / ShatterOutSeconds);

            // 崩れは速く、戻りはゆっくり。引力で引き戻されて丸くなる感じにする。
            var broken = out1 * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, t)));
            return Mathf.Lerp(1f, ShatterMinScale, broken);
        }

        /// <summary>
        /// ぶつかった衝撃で地球を砕く。
        ///
        /// **なぜ砕くのか。** 火星ほどの天体がぶつかった場面で、地球の側が
        /// まったく形を変えないのは不自然である。ぶつかった側だけが沈み、
        /// 地球は丸いまま、という絵になっていた。
        ///
        /// 実際の形を解いているわけではない。塊を外へ散らし、地球を縮め、
        /// そのあと塊を引き戻して地球を元へ戻す、という見せ方である。
        /// **崩れる量も戻る速さも、こちらで決めた値であり、物理の計算ではない。**
        ///
        /// **塊の道すじについて。** 以前は出た向きへまっすぐ出て、同じ道を戻っていた。
        /// 跳ね返って落ちただけに見えるうえ、飛び出した物質が地球のまわりを
        /// 巡ったという説明と噛み合わない。いまは回り込ませ、
        /// 多くは地球へ落ち戻り、一部は落ちずに外の輪へ残る。
        /// 残ったぶんが次の段階で月になる破片にあたる。
        /// 出典: NASA Astrobiology "Tracking Formation of the Earth and Moon",
        /// LPI "The Moon's Formation and Evolution"。
        /// **軌道を解いてはいない。楕円かどうかも、回る速さも表していない。**
        /// </summary>
        private void ShatterEarth()
        {
            if (shards == null)
            {
                return;
            }

            var active = impactHappened && sinceImpact < ShatterSeconds;
            if (!active)
            {
                CutRadius = 0f;
                for (var i = 0; i < shards.Length; i++)
                {
                    if (shards[i].activeSelf)
                    {
                        shards[i].SetActive(false);
                    }
                }

                return;
            }

            var t = Mathf.Clamp01(sinceImpact / ShatterSeconds);
            var surface = earthRadius * CurrentScale();

            // 外へ出るのは速く、戻るのはゆっくり。
            var outward = Mathf.Clamp01(sinceImpact / ShatterOutSeconds);
            var back = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 1f, t));
            var reach = outward * (1f - back);

            // 抉れた範囲。ぶつかった側へ寄せた球を、地球から差し引く。
            // 一気に開いて、戻りにつれて閉じる。閉じきると元の丸い地球になる。
            var opening = Mathf.Clamp01(sinceImpact / (ShatterOutSeconds * 0.5f)) * (1f - back);
            CutCenter = impactPoint.normalized * (earthRadius * ShatterCutOffset)
                        + impactPoint.normalized * (earthRadius * 0.35f * (1f - opening));
            CutRadius = earthRadius * ShatterCutRadius * opening;

            for (var i = 0; i < shards.Length; i++)
            {
                shards[i].SetActive(true);

                // 塊は抉れた側から出す。全方向へ均等に出すと、欠けた場所と結びつかない。
                var direction = (shardDirections[i] + impactPoint.normalized * 1.1f).normalized;

                // 落ちずに残る塊は、外の輪の高さまで出て、そこにとどまる。
                // 落ち戻る塊だけが縮んで消え、地球と一体になる。
                var escaping = shardEscapes[i];
                var distance = escaping
                    ? surface * 0.92f + earthRadius * (DebrisRadius - 0.92f) * outward
                    : surface * 0.92f + earthRadius * ShatterSpread * reach;

                // 回り込み。時間とともに角度が増えるだけで、戻らない。
                // 行きと帰りで同じ道を通らないので、周回しながら落ちるように見える。
                var swing = Quaternion.AngleAxis(shardSwings[i] * t, shardAxes[i]);
                shards[i].transform.localPosition = swing * (direction * distance);

                // 落ち戻る塊は、戻りきるところで消える。地球と一体になったことを表す。
                // 残る塊は終わりぎわに消し、同じ場所にある破片の輪へ引き継ぐ。
                // 輪はこのあと月の材料になるので、見た目を二重に持たない。
                var settle = Mathf.InverseLerp(0.78f, 1f, t);
                shards[i].transform.localScale =
                    shardSizes[i] * (escaping ? 1f - settle : 1f - back);
                shards[i].transform.Rotate(Vector3.one, 150f * SceneClock.Delta, Space.Self);
            }
        }

        /// <summary>
        /// ぶつかった衝撃で、大きな塊をひとつ弾き出す。
        ///
        /// これまでは細かい破片しか出しておらず、「砕けて散った」までは見えても、
        /// 「大きく二つに分かれた」ことが読み取れなかった。
        /// ジャイアントインパクト説では、飛び出した物質が集まって月になったとされる。
        /// その「もう一方の塊」を、はっきり見える大きさでひとつだけ出す。
        ///
        /// **これは仮説の象徴表現である。** 質量比・飛び出す速さ・角度・
        /// 集まるまでの時間を一切表さない。大きさも飛ぶ距離も見やすさで決めている。
        /// この塊が月そのものになる過程は描かず、次の段階で月として現れる。
        /// </summary>
        private void LaunchFragment(Vector3 surfacePoint)
        {
            if (fragment == null)
            {
                return;
            }

            fragmentOrigin = surfacePoint;

            // ぶつかった向きの横へ逃がす。まっすぐ跳ね返すと、当たって弾んだだけに見える。
            var outward = surfacePoint.normalized;
            var side = Vector3.Cross(outward, Vector3.up);
            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.Cross(outward, Vector3.forward);
            }

            fragmentDirection = (outward * 0.55f + side.normalized * 0.8f + Vector3.up * 0.25f).normalized;

            fragment.SetActive(true);
            fragment.transform.localPosition = surfacePoint;
            fragment.transform.localScale = Vector3.one * (earthRadius * FragmentScale);
        }

        /// <summary>弾き出した塊を、勢いを落としながら遠ざける。</summary>
        private void MoveFragment()
        {
            if (fragment == null || !impactHappened || !fragment.activeSelf)
            {
                return;
            }

            // 最初が速く、だんだん収まる。放り出されて離れていく感じにする。
            var eased = 1f - Mathf.Exp(-sinceImpact / FragmentEaseSeconds);
            fragment.transform.localPosition =
                fragmentOrigin + fragmentDirection * (earthRadius * FragmentTravel * eased);
            fragment.transform.Rotate(Vector3.one, 55f * SceneClock.Delta, Space.Self);
        }

        /// <summary>ぶつかった場所のまわりに、光をいくつか散らす。</summary>
        private void SpawnImpactRing(Vector3 origin)
        {
            var normal = origin.normalized;
            var side = Vector3.Cross(normal, Vector3.up);
            if (side.sqrMagnitude < 0.001f)
            {
                side = Vector3.Cross(normal, Vector3.forward);
            }

            side.Normalize();
            var other = Vector3.Cross(normal, side);

            for (var i = 0; i < 5; i++)
            {
                var angle = i * (Mathf.PI * 2f / 5f);
                var offset = (side * Mathf.Cos(angle) + other * Mathf.Sin(angle)) * (earthRadius * 0.42f);
                SpawnFlash(origin + offset, earthRadius * 0.26f, ImpactFlashSeconds * 0.75f);
            }
        }

        /// <summary>ぶつかった場所から破片を出し、輪へ広げる。</summary>
        private void BurstDebris(Vector3 origin)
        {
            var amount = stage != null ? (float)stage.Debris : 0f;
            var visible = Mathf.RoundToInt(amount * DebrisPoolSize);

            for (var i = 0; i < debris.Length; i++)
            {
                var on = i < visible;
                debris[i].SetActive(on);
                if (on)
                {
                    debris[i].transform.localPosition = debrisRoot.InverseTransformPoint(
                        transform.TransformPoint(origin));
                }
            }
        }

        /// <summary>
        /// 破片が月へ集まっていく度合い。0でまだ輪のまま、1で集まりきった状態。
        /// 月の大きさも、寄せる破片の数も、この一つの値から出す。
        /// 二つの時計で別々に進めると、月が育ちきったのに破片がまだ残る、
        /// といったずれが出る。
        /// </summary>
        private float MoonGathering()
        {
            var t = Mathf.Clamp01(elapsed / MoonAccretionSeconds);

            // 前半は、破片どうしが寄り集まっていくつかの塊になる時間。
            // 月そのものはまだ芯の大きさにとどめ、育つのは後半にする。
            var seed = Mathf.SmoothStep(0f, MoonSeedScale, Mathf.InverseLerp(0f, MoonClumpPhase, t));

            // 後半は、その塊どうしが一つに合わさって月の大きさへ育つ。
            // 少し重ねて始めるので、芯から育ちへの移りに切れ目が出ない。
            var merged = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(MoonClumpPhase * 0.85f, 1f, t));

            return Mathf.Max(seed, merged);
        }

        /// <summary>
        /// 輪の破片を月へ寄せ集める。
        ///
        /// 月は、ぶつかった衝撃で飛び散った物質が地球を巡る円盤になり、
        /// そこから集まってできたとされる。以前はその円盤をそのまま残したまま、
        /// 月だけが軌道上で膨らんでいた。破片が月の材料であることが見て取れないため、
        /// 輪から月へ向かって順に動かし、着いたところで見えなくする。
        /// 全部は寄せず、輪にはいくらか残す。
        /// 出典: NASA Astrobiology "Tracking Formation of the Earth and Moon"。
        /// **軌道も、集まるのにかかった年数も表していない。**
        /// </summary>
        private void GatherDebrisIntoMoon()
        {
            // 月の位置を渡されていないときは、寄せ先が分からない。
            // 輪をそのまま回しておくほうが、原点へ吸い込むより無害である。
            if (MoonAnchor.sqrMagnitude < 0.0001f)
            {
                return;
            }

            var t = Mathf.Clamp01(elapsed / MoonAccretionSeconds);
            var moon = debrisRoot.InverseTransformPoint(transform.TransformPoint(MoonAnchor));

            var stay = Mathf.RoundToInt(debrisTarget * DebrisPoolSize);
            var from = Mathf.Max(stay, Mathf.RoundToInt(debrisEntry * DebrisPoolSize));
            var feeding = Mathf.Max(1, from - stay);
            var perClump = Mathf.Max(1, Mathf.CeilToInt(feeding / (float)MoonClumpCount));

            // 前半は破片どうしが塊になる時間、後半はその塊どうしが合わさる時間。
            var joining = Mathf.Clamp01(t / MoonClumpPhase);
            var merging = Mathf.SmoothStep(
                0f, 1f, Mathf.InverseLerp(MoonClumpPhase * 0.85f, 1f, t));

            for (var i = 0; i < debris.Length; i++)
            {
                if (i >= from)
                {
                    debris[i].SetActive(false);
                    continue;
                }

                debris[i].SetActive(true);
                debris[i].transform.Rotate(Vector3.one, 60f * SceneClock.Delta, Space.Self);

                if (i < stay)
                {
                    // 輪に残る破片。月ができたあとも残る円盤にあたる。
                    debris[i].transform.localScale = debrisSizes[i];
                    debris[i].transform.localPosition = Vector3.Lerp(
                        debris[i].transform.localPosition, debrisTargets[i], 0.08f);
                    continue;
                }

                // 月へ渡る破片。いくつかの組に分け、組ごとに一つの塊へまとまる。
                // 塊の居場所は、その組の先頭の破片が輪で占めていた位置にする。
                var order = i - stay;
                var seat = debrisTargets[stay + (order / perClump) * perClump];

                // 塊へ寄っていく破片。順に出発させ、一度に消えないようにする。
                var within = (float)(order % perClump) / perClump;
                var pull = Mathf.SmoothStep(
                    0f, 1f, Mathf.InverseLerp(within * MoonGatherStagger, 1f, joining));

                // まっすぐ向かわせず、いったん外へ膨らませてから寄せる。
                // 直線で結ぶと、引かれたのではなく並べ替えたように見える。
                var toCore = seat - debrisTargets[i];
                var bulge = Vector3.Cross(toCore, Vector3.up);
                if (bulge.sqrMagnitude < 0.0001f)
                {
                    bulge = Vector3.Cross(toCore, Vector3.forward);
                }

                var arc = bulge.normalized * (earthRadius * 0.22f * Mathf.Sin(pull * Mathf.PI));
                debris[i].transform.localPosition = Vector3.Lerp(debrisTargets[i], seat, pull) + arc;

                // 着くにつれて小さくなる。塊に取り込まれたことを表す。
                debris[i].transform.localScale = debrisSizes[i] * (1f - pull);
            }

            MoveMoonClumps(moon, stay, perClump, joining, merging);

            // 集まりきったら、ふつうの寄せ方へ戻す。
            if (t >= 0.999f)
            {
                moonAccreting = false;
                debrisShown = debrisTarget;
                HideMoonClumps();
            }
        }

        /// <summary>塊を引っ込める。集まり終わったあとと、段階を移ったときに呼ぶ。</summary>
        private void HideMoonClumps()
        {
            if (moonClumps == null)
            {
                return;
            }

            for (var i = 0; i < moonClumps.Length; i++)
            {
                moonClumps[i].SetActive(false);
            }
        }

        /// <summary>
        /// 破片が寄り集まってできる塊を動かす。
        ///
        /// 前半は輪の上に居座り、破片を取り込みながら育つ。
        /// 後半は月のできる位置へ寄り、そこで小さくなって消える。
        /// 消えるぶんだけ月が育つので、塊が合わさって月になったように見える。
        ///
        /// **引力を解いているわけではない。** どれがどの塊になるかも、
        /// 合わさる順も、見て分かるようにこちらで決めた組み分けである。
        /// </summary>
        private void MoveMoonClumps(
            Vector3 moon, int stay, int perClump, float joining, float merging)
        {
            if (moonClumps == null)
            {
                return;
            }

            // 取り込んだぶんだけ育つ。丸い天体は体積で増えるので、
            // 見かけの半径は取り込んだ量の3乗根で伸びる。
            var grown = Mathf.Pow(Mathf.SmoothStep(0f, 1f, joining), 1f / 3f);

            for (var i = 0; i < moonClumps.Length; i++)
            {
                var index = stay + i * perClump;
                if (index >= debrisTargets.Length)
                {
                    moonClumps[i].SetActive(false);
                    continue;
                }

                moonClumps[i].SetActive(true);
                moonClumps[i].transform.localPosition =
                    Vector3.Lerp(debrisTargets[index], moon, merging);
                moonClumps[i].transform.localScale =
                    moonClumpScale * (grown * (1f - merging));
                moonClumps[i].transform.Rotate(Vector3.one, 35f * SceneClock.Delta, Space.Self);
            }
        }

        /// <summary>広がった破片を、輪の位置へ落ち着かせる。</summary>
        private void MoveDebris()
        {
            if (debris == null)
            {
                return;
            }

            if (moonAccreting)
            {
                GatherDebrisIntoMoon();
                return;
            }

            if (!impactHappened)
            {
                return;
            }

            var t = Mathf.Clamp01((elapsed - ImpactTravelSeconds) / DebrisSpreadSeconds);
            var eased = 1f - (1f - t) * (1f - t);

            for (var i = 0; i < debris.Length; i++)
            {
                if (!debris[i].activeSelf)
                {
                    continue;
                }

                debris[i].transform.localPosition =
                    Vector3.Lerp(debris[i].transform.localPosition, debrisTargets[i], eased * 0.12f);
                debris[i].transform.Rotate(Vector3.one, 60f * SceneClock.Delta, Space.Self);
            }
        }

        /// <summary>段階の落ち着いた姿にする。動きを減らしているときに使う。</summary>
        private void Settle()
        {
            impactHappened = true;
            sinceImpact = ShakeSeconds;   // 揺れを終わった状態にする
            impactor.SetActive(false);
            impactor.transform.localScale = impactorScale;
            if (earth != null)
            {
                earth.localPosition = Vector3.zero;
            }


            var amount = stage != null ? (float)stage.Debris : 0f;
            var visible = Mathf.RoundToInt(amount * DebrisPoolSize);
            for (var i = 0; i < debris.Length; i++)
            {
                debris[i].SetActive(i < visible);
                debris[i].transform.localPosition = debrisTargets[i];
            }

            ApplyScale(1f);
        }

        private float CurrentScale()
        {
            return earth != null ? earth.localScale.x : 1f;
        }

        private void SpawnFlash(Vector3 localPosition, float size)
        {
            SpawnFlash(localPosition, size, FlashSeconds);
        }

        /// <summary>
        /// 光をひとつ出す。寿命を渡せるようにしてあるのは、
        /// 集積の小さな光と、巨大衝突の光で、残る長さを変えたいためである。
        /// </summary>
        private void SpawnFlash(Vector3 localPosition, float size, float life)
        {
            for (var i = 0; i < flashes.Length; i++)
            {
                if (flashes[i].activeSelf)
                {
                    continue;
                }

                flashes[i].transform.localPosition = localPosition;
                flashSize[i] = size;
                flashes[i].transform.localScale = Vector3.one * size;
                flashes[i].SetActive(true);
                flashLife[i] = life;
                flashMaxLife[i] = life;
                return;
            }
        }

        /// <summary>ぶつかった跡の光を膨らませながら消す。</summary>
        private void UpdateFlashes()
        {
            for (var i = 0; i < flashes.Length; i++)
            {
                if (!flashes[i].activeSelf)
                {
                    continue;
                }

                flashLife[i] -= SceneClock.Delta;
                if (flashLife[i] <= 0f)
                {
                    flashes[i].SetActive(false);
                    continue;
                }

                // 膨らませながら薄くする。倍率を掛け続けると際限なく大きくなるため、
                // 生まれたときの大きさを基準に決める。
                var life = flashMaxLife[i] > 0f ? flashMaxLife[i] : FlashSeconds;
                var t = 1f - flashLife[i] / life;
                flashes[i].transform.localScale = Vector3.one * (flashSize[i] * (1f + t * 1.6f));

                var fade = 1f - t;
                var block = new MaterialPropertyBlock();
                block.SetColor("_EmissionColor", FlashColor * (fade * fade * 2.6f));
                block.SetColor("_Color", new Color(FlashColor.r, FlashColor.g, FlashColor.b, fade));
                flashes[i].GetComponent<Renderer>().SetPropertyBlock(block);
            }
        }

        private Transform CreateRoot(string name, HideFlags flags)
        {
            var root = new GameObject(name);
            root.hideFlags = flags;
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private void BuildSwarm(HideFlags flags)
        {
            swarm = new GameObject[SwarmPoolSize];
            swarmProgress = new float[SwarmPoolSize];
            swarmAxisU = new Vector3[SwarmPoolSize];
            swarmAxisV = new Vector3[SwarmPoolSize];
            swarmEccentricity = new float[SwarmPoolSize];
            swarmPhase = new float[SwarmPoolSize];
            swarmSizes = new Vector3[SwarmPoolSize];

            var material = CreateRockMaterial(RockColor, 0.25f, flags);

            for (var i = 0; i < SwarmPoolSize; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Cube, "Planetesimal" + (i + 1), flags);
                item.transform.SetParent(swarmRoot, false);

                var size = earthRadius * (0.05f + (i % 4) * 0.018f);
                item.transform.localScale = Vector3.one * size;
                item.transform.localRotation = Random.rotation;
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                swarm[i] = item;
                swarmSizes[i] = item.transform.localScale;
                swarmProgress[i] = i / (float)SwarmPoolSize;
                ResetSwarmOrbit(i);
            }
        }

        private void BuildDebris(HideFlags flags)
        {
            debris = new GameObject[DebrisPoolSize];
            debrisTargets = new Vector3[DebrisPoolSize];
            debrisSizes = new Vector3[DebrisPoolSize];
            // 輪の破片は、ぶつかってきた天体のかけらと同じ熱い岩の色にする。
            // 円盤の物質はぶつかってきた天体のものが主だったとされるためである
            // （<see cref="ImpactorShardKeepShare"/> に出典と、決着していない点を書いた）。
            // 冷えた灰色にしていたころは、砕けたかけらと輪と月が、
            // 見た目の上でつながっていなかった。
            var material = CreateRockMaterial(HotRockColor, 0.28f, flags);

            for (var i = 0; i < DebrisPoolSize; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Cube, "Debris" + (i + 1), flags);
                item.transform.SetParent(debrisRoot, false);

                // 輪として散らす。等間隔にすると人工物に見えるため、半径と高さをばらす。
                var angle = i * 137.5f * Mathf.Deg2Rad;
                var spread = 0.82f + (i % 7) * 0.055f;
                var lift = ((i % 5) - 2) * 0.06f;

                debrisTargets[i] = new Vector3(
                    Mathf.Cos(angle) * earthRadius * DebrisRadius * spread,
                    earthRadius * lift,
                    Mathf.Sin(angle) * earthRadius * DebrisRadius * spread);

                item.transform.localPosition = debrisTargets[i];
                item.transform.localRotation = Random.rotation;
                // 以前は0.028〜0.052で、画面では2〜4画素にしかならなかった。
                // 輪があることは分かっても、その一粒一粒が月へ寄っていく様子は読めない。
                // 砕けたかけら（0.11〜0.245）より細かくしているのは、
                // 大きな塊が砕けて散ったあとの、細かいほうを表すためである。
                item.transform.localScale = Vector3.one * (earthRadius * (0.058f + (i % 3) * 0.021f));
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                debris[i] = item;
                debrisSizes[i] = item.transform.localScale;
            }
        }

        private void BuildImpactor(HideFlags flags)
        {
            var material = CreateRockMaterial(HotRockColor, 0.5f, flags);

            impactorStart =
                ImpactorStartDirection.normalized * (FramedRadius * ImpactorStartFrames);

            impactor = PrimitiveMeshes.Create(PrimitiveType.Sphere, "GiantImpactor", flags);
            impactor.transform.SetParent(impactorRoot, false);
            // ぶつかってくる天体の大きさ。地球の直径のおよそ0.6にあたる。
            // 火星ほどの大きさの天体がぶつかったとされており、実際の比（約0.53）へ
            // 近いところで、画面で見て分かる大きさに寄せている。
            impactorScale = Vector3.one * (earthRadius * 1.2f);
            impactor.transform.localScale = impactorScale;
            impactor.transform.localPosition = impactorStart;
            impactor.GetComponent<Renderer>().sharedMaterial = material;
            impactor.SetActive(false);
        }

        /// <summary>
        /// 破片が寄り集まってできる塊を組む。輪の破片と同じ岩の見た目にする。
        ///
        /// 大きさは、この塊が <see cref="MoonClumpCount"/> 個ぶん合わさると
        /// ちょうど月になる量にしている。体積を等分するので、
        /// 一辺は月の直径を個数の3乗根で割ったものになる。
        /// 目分量で決めると、合わさった結果が月より大きかったり小さかったりする。
        /// </summary>
        private void BuildMoonClumps(HideFlags flags)
        {
            moonClumps = new GameObject[MoonClumpCount];
            var material = CreateRockMaterial(HotRockColor, 0.28f, flags);

            // 立方体なので、月の球と同じ体積にすると見た目が勝ちすぎる。
            // 月の直径を差し渡しの目安にし、そこから個数の3乗根で割る。
            var side = MoonView.BodyRadius * 2f / Mathf.Pow(MoonClumpCount, 1f / 3f);
            moonClumpScale = Vector3.one * side;

            for (var i = 0; i < MoonClumpCount; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Cube, "MoonClump" + (i + 1), flags);
                item.transform.SetParent(debrisRoot, false);
                item.transform.localRotation = Random.rotation;
                item.transform.localScale = moonClumpScale;
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);
                moonClumps[i] = item;
            }
        }

        /// <summary>
        /// ぶつかってきた天体が砕けたかけらを組む。
        ///
        /// 砕ける前の天体の形の中へ、あらかじめ座らせておく。
        /// ぶつかった瞬間に、その座席の位置でかけらを出し、そこから散らす。
        /// こうすると「球がその場で割れた」ように見える。
        /// 適当な位置から出すと、天体が消えて別の物が湧いたように見えてしまう。
        /// </summary>
        private void BuildImpactorShards(HideFlags flags)
        {
            impactorShards = new GameObject[ImpactorShardPoolSize];
            impactorShardSeats = new Vector3[ImpactorShardPoolSize];
            impactorShardDirections = new Vector3[ImpactorShardPoolSize];
            impactorShardSizes = new Vector3[ImpactorShardPoolSize];
            impactorShardAxes = new Vector3[ImpactorShardPoolSize];
            impactorShardSwings = new float[ImpactorShardPoolSize];
            impactorShardKeeps = new bool[ImpactorShardPoolSize];

            var material = CreateRockMaterial(HotRockColor, 0.5f, flags);

            // 輪に残すものを飛び飛びに選ぶ。続けて選ぶと片側だけが残る。
            var keepCount = Mathf.RoundToInt(ImpactorShardPoolSize * ImpactorShardKeepShare);
            var keepEvery = keepCount > 0 ? Mathf.Max(1, ImpactorShardPoolSize / keepCount) : 0;

            // 砕ける前の天体の半径。かけらはこの内側に座る。
            var bodyRadius = earthRadius * 0.6f;

            for (var i = 0; i < ImpactorShardPoolSize; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Cube, "ImpactorShard" + (i + 1), flags);
                item.transform.SetParent(impactorRoot, false);
                item.transform.localRotation = Random.rotation;
                item.transform.localScale =
                    Vector3.one * (earthRadius * (0.11f + (i % 4) * 0.045f));
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                impactorShards[i] = item;
                impactorShardSizes[i] = item.transform.localScale;

                // 球の中へ散らして座らせる。3乗根を掛けると、内側へ偏らずに詰まる。
                var seat = Random.onUnitSphere;
                impactorShardSeats[i] = seat * (bodyRadius * Mathf.Pow(Random.value, 1f / 3f));

                // 散る向きは、座っていた向きへ広がる。中心から外へ割れる形になる。
                impactorShardDirections[i] = seat;

                var axis = Vector3.Cross(seat, Random.onUnitSphere);
                impactorShardAxes[i] = axis.sqrMagnitude < 0.001f ? Vector3.up : axis.normalized;
                impactorShardSwings[i] =
                    ShardSwingDegrees * Random.Range(0.4f, 1f) * (i % 2 == 0 ? 1f : -1f);

                impactorShardKeeps[i] = keepEvery > 0 && i % keepEvery == 0;
            }
        }

        /// <summary>砕けた地球の塊を組む。地表と同じ岩の見た目にする。</summary>
        private void BuildShards(HideFlags flags)
        {
            shards = new GameObject[ShardPoolSize];
            shardDirections = new Vector3[ShardPoolSize];
            shardSizes = new Vector3[ShardPoolSize];
            shardAxes = new Vector3[ShardPoolSize];
            shardSwings = new float[ShardPoolSize];
            shardEscapes = new bool[ShardPoolSize];

            // 輪に残す塊の数。割合から出し、飛び飛びに選ぶ。
            // 続けて選ぶと片側だけが残り、輪ではなく塊の列に見える。
            var escapeCount = Mathf.RoundToInt(ShardPoolSize * ShardEscapeShare);
            var escapeEvery = escapeCount > 0 ? Mathf.Max(1, ShardPoolSize / escapeCount) : 0;

            var material = CreateRockMaterial(HotRockColor, 0.30f, flags);

            for (var i = 0; i < ShardPoolSize; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Cube, "Shard" + (i + 1), flags);
                item.transform.SetParent(impactorRoot, false);
                item.transform.localRotation = Random.rotation;
                item.transform.localScale =
                    Vector3.one * (earthRadius * (0.10f + (i % 4) * 0.035f));
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                shards[i] = item;
                shardSizes[i] = item.transform.localScale;

                // 全方向へ均等に散らす。偏ると片側だけ欠けたように見える。
                shardDirections[i] = Random.onUnitSphere;

                // 回り込む軸。出る向きと直交する側へ取り、その場で自転しないようにする。
                var axis = Vector3.Cross(shardDirections[i], Random.onUnitSphere);
                shardAxes[i] = axis.sqrMagnitude < 0.001f ? Vector3.up : axis.normalized;
                shardSwings[i] =
                    ShardSwingDegrees * Random.Range(0.35f, 1f) * (i % 2 == 0 ? 1f : -1f);

                shardEscapes[i] = escapeEvery > 0 && i % escapeEvery == 0;
            }
        }

        /// <summary>弾き出す大きな塊を組む。ぶつかる天体と同じ岩の見た目にする。</summary>
        private void BuildFragment(HideFlags flags)
        {
            fragment = PrimitiveMeshes.Create(PrimitiveType.Sphere, "ImpactFragment", flags);
            fragment.transform.SetParent(impactorRoot, false);
            fragment.transform.localScale = Vector3.one * (earthRadius * FragmentScale);
            fragment.GetComponent<Renderer>().sharedMaterial =
                CreateRockMaterial(HotRockColor, 0.35f, flags);
            fragment.SetActive(false);
        }

        private void BuildFlashes(HideFlags flags)
        {
            flashes = new GameObject[FlashPoolSize];
            flashLife = new float[FlashPoolSize];
            flashSize = new float[FlashPoolSize];
            flashMaxLife = new float[FlashPoolSize];

            // 加算の専用シェーダー。混ぜ方はシェーダー側に固定してある。
            flashMaterial = StandardMaterials.CreateGlow();
            flashMaterial.hideFlags = flags;
            flashMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            flashMaterial.color = FlashColor;
            flashMaterial.SetColor("_EmissionColor", FlashColor * 2.2f);

            for (var i = 0; i < FlashPoolSize; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Sphere, "Flash" + (i + 1), flags);
                item.transform.SetParent(flashRoot, false);
                item.GetComponent<Renderer>().sharedMaterial = flashMaterial;
                item.SetActive(false);
                flashes[i] = item;
            }
        }

        private static Material CreateRockMaterial(Color color, float glow, HideFlags flags)
        {
            var material = StandardMaterials.CreateOpaque(true);
            material.hideFlags = flags;
            material.color = color;
            material.SetFloat("_Glossiness", 0.06f);
            material.SetFloat("_Metallic", 0f);
            material.SetColor("_EmissionColor", color * glow);
            return material;
        }
    }
}
