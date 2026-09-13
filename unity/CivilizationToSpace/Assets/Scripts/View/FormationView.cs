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

        /// <summary>集積の始まりに置く数。向かい合う2個から始める。</summary>
        private const int AccretionSeedCount = 2;

        /// <summary>
        /// 2個のままで見せる区間。集積の時間に対する割合。
        /// すぐに数を増やすと、向かい合って近づく2個がその他大勢に紛れてしまう。
        /// </summary>
        private const float AccretionSeedHold = 0.4f;

        /// <summary>最初の2個が進む速さ。ほかより遅くして、近づく様子を追えるようにする。</summary>
        private const float AccretionSeedSpeed = 0.22f;

        /// <summary>ぶつかって飛び出す大きな塊の半径。地球の半径を1としたときの倍率。</summary>
        private const float FragmentScale = 0.30f;

        /// <summary>大きな塊が飛びのく距離。地球の半径を1としたときの倍率。</summary>
        private const float FragmentTravel = 2.3f;

        /// <summary>飛びのく勢いが収まるまでの時間（秒）。</summary>
        private const float FragmentEaseSeconds = 0.65f;

        /// <summary>破片の輪の半径。</summary>
        private const float DebrisRadius = 2.0f;

        /// <summary>大きな天体が近づいてぶつかるまでの時間（秒）。</summary>
        private const float ImpactTravelSeconds = 2.4f;

        /// <summary>ぶつかったあと、破片が輪へ落ち着くまでの時間（秒）。</summary>
        private const float DebrisSpreadSeconds = 1.6f;

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

        /// <summary>ぶつかった衝撃で地球が揺れている時間（秒）。</summary>
        private const float ShakeSeconds = 0.9f;

        /// <summary>揺れの大きさ。地球の半径を1としたときの倍率。</summary>
        private const float ShakeAmplitude = 0.085f;

        /// <summary>揺れの速さ（1秒あたりの往復回数）。</summary>
        private const float ShakeFrequency = 7.5f;

        private static readonly Color32 RockColor = new Color32(0x77, 0x6B, 0x60, 0xFF);
        private static readonly Color32 HotRockColor = new Color32(0xC8, 0x6A, 0x38, 0xFF);
        private static readonly Color32 DebrisColor = new Color32(0xA8, 0x92, 0x7C, 0xFF);
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

        private GameObject[] debris;
        private Vector3[] debrisTargets;

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
            BuildImpactor(flags);
            BuildFragment(flags);
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
            var debrisAmount = next != null ? (float)next.Debris : 0f;
            var visibleDebris = Mathf.RoundToInt(debrisAmount * DebrisPoolSize);
            var burstStage = next != null && next.Impactor > 0.5d;
            for (var i = 0; i < debris.Length; i++)
            {
                debris[i].SetActive(i < visibleDebris && !burstStage);
            }

            // 大きな塊は、ぶつかる段階でだけ出す。次の段階では月として別に現れる。
            if (fragment != null)
            {
                fragment.SetActive(false);
            }

            impactor.SetActive(burstStage);
            if (burstStage)
            {
                impactor.transform.localPosition = impactorStart;
            }

            MoonEmergence = next != null ? (float)next.Moon : 1f;

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
                ApplyScale(Mathf.Clamp01(elapsed / AccretionSeconds));
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
            ShakeEarth();
            MoveFragment();
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

            earth.localScale = Vector3.one * Mathf.Lerp(scaleFrom, scaleTo, t);
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

                // 最初の2個は同じ速さで進める。速さが違うと、
                // 向かい合わせに置いても同じところで出会わない。
                var speed = i < AccretionSeedCount
                    ? AccretionSeedSpeed
                    : 0.30f + (i % 5) * 0.07f;
                swarmProgress[i] += SceneClock.Delta * speed;

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

            int visible;
            if (accreting)
            {
                var t = Mathf.Clamp01(elapsed / AccretionSeconds);

                // はじめの区間は2個のまま置き、出会うところを見せてから増やしていく。
                var ramp = Mathf.InverseLerp(AccretionSeedHold, 1f, t);
                visible = Mathf.RoundToInt(Mathf.Lerp(AccretionSeedCount, full, ramp));
            }
            else
            {
                visible = full;
            }

            for (var i = 0; i < swarm.Length; i++)
            {
                swarm[i].SetActive(i < visible);
            }
        }

        /// <summary>
        /// 微惑星ひとつの位置を、楕円軌道の上で求める。
        ///
        /// **この形にしている根拠。**
        /// 地球は、重力で引き寄せられた小天体が衝突と合体を繰り返して育った、
        /// というのが標準的な説明である。NASAの天体生物学の教材は
        /// 「planetesimals が質量を得るほど重力が強まり、まわりの大小の物体を引き寄せた」
        /// 「原始惑星が育つと、その強い重力が微惑星との高速の衝突を数多く生んだ」と述べ、
        /// 最後は月を生んだ巨大衝突で地球の成長が終わった、としている。
        /// 重力に引かれた軌道は円ではなく楕円になり、ぶつかるまでのあいだ
        /// 天体は地球のまわりを回りながら近づく。まっすぐ落とすと、その過程が消えてしまう。
        ///
        /// 参照した公的資料：
        ///   NASA Astrobiology「How did our Solar System form?」
        ///   https://astrobiology.nasa.gov/education/alp/how-did-our-solar-system-form/
        ///   Lunar and Planetary Institute「Active Accretion」
        ///   https://www.lpi.usra.edu/education/orexlaunch/Active%20Accretion.pdf
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
        /// 最初の2個だけは決め打ちにする。同じ面の上で向かい合わせに置き、
        /// 互いに引かれて楕円を描きながら近づく様子を、はっきり見せるためである。
        /// 3個目からは向きを散らし、あちこちから集まってくるようにする。
        /// </summary>
        private void ResetSwarmOrbit(int index)
        {
            if (index < AccretionSeedCount)
            {
                // 画面の手前側に寝かせた面。横から見て楕円と分かる向きにする。
                swarmAxisU[index] = new Vector3(1f, 0f, 0f);
                swarmAxisV[index] = new Vector3(0f, 0.32f, 0.95f).normalized;
                swarmEccentricity[index] = 0.5f;
                swarmPhase[index] = index * Mathf.PI;
                return;
            }

            var normal = Random.onUnitSphere;
            var u = Vector3.Cross(normal, Vector3.up);
            if (u.sqrMagnitude < 0.001f)
            {
                u = Vector3.Cross(normal, Vector3.forward);
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

            // 近づくほど速くする。等速だと、ぶつかる瞬間が分かりにくい。
            var eased = t * t;
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

        /// <summary>広がった破片を、輪の位置へ落ち着かせる。</summary>
        private void MoveDebris()
        {
            if (debris == null || !impactHappened)
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

            var material = CreateRockMaterial(RockColor, 0.25f, flags);

            for (var i = 0; i < SwarmPoolSize; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Cube, "Planetesimal" + (i + 1), flags);
                item.transform.SetParent(swarmRoot, false);

                // 最初の2個は、これから塊の芯になるので大きめにする。
                var size = i < AccretionSeedCount
                    ? earthRadius * 0.13f
                    : earthRadius * (0.05f + (i % 4) * 0.018f);
                item.transform.localScale = Vector3.one * size;
                item.transform.localRotation = Random.rotation;
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                swarm[i] = item;

                // 最初の2個は同じところから始め、同時に近づいて出会うようにする。
                swarmProgress[i] = i < AccretionSeedCount ? 0f : i / (float)SwarmPoolSize;
                ResetSwarmOrbit(i);
            }
        }

        private void BuildDebris(HideFlags flags)
        {
            debris = new GameObject[DebrisPoolSize];
            debrisTargets = new Vector3[DebrisPoolSize];
            var material = CreateRockMaterial(DebrisColor, 0.12f, flags);

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
                item.transform.localScale = Vector3.one * (earthRadius * (0.028f + (i % 3) * 0.012f));
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                debris[i] = item;
            }
        }

        private void BuildImpactor(HideFlags flags)
        {
            var material = CreateRockMaterial(HotRockColor, 0.5f, flags);

            impactorStart = new Vector3(
                earthRadius * 3.0f, earthRadius * 1.1f, -earthRadius * 0.8f);

            impactor = PrimitiveMeshes.Create(PrimitiveType.Sphere, "GiantImpactor", flags);
            impactor.transform.SetParent(impactorRoot, false);
            impactorScale = Vector3.one * (earthRadius * 1.0f);
            impactor.transform.localScale = impactorScale;
            impactor.transform.localPosition = impactorStart;
            impactor.GetComponent<Renderer>().sharedMaterial = material;
            impactor.SetActive(false);
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

            flashMaterial = StandardMaterials.CreateFadeEmissive();
            flashMaterial.hideFlags = flags;
            flashMaterial.SetFloat("_Mode", 3f);
            flashMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            flashMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            flashMaterial.SetInt("_ZWrite", 0);
            flashMaterial.EnableKeyword("_ALPHABLEND_ON");
            flashMaterial.EnableKeyword("_EMISSION");
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
