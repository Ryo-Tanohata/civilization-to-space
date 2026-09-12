using System;
using UnityEngine;

namespace CivilizationToSpace.Sim
{
    /// <summary>
    /// 微惑星の集積からジャイアントインパクト、月の形成までを計算する。
    ///
    /// **何を計算しているか**
    /// 天体どうしの万有引力を毎ステップ総当たりで足し、速度と位置を更新する。
    /// 触れた対は運動量を保って合体させる。位置は台本ではなく、この積み重ねで決まる。
    /// 種を同じにすれば同じ結果になる。
    ///
    /// **何を計算していないか（重要）**
    /// 衝突で破片が飛び散る過程は計算していない。物質の流体としての振る舞いを
    /// 解いておらず、衝突の瞬間に「決めた質量の破片を、決めた範囲へ置く」という
    /// 手続きで置き換えている。放出量・広がり・速さは <see cref="SimSettings"/> の
    /// 値であって、計算から出た量ではない。
    /// その破片が周回しながらひとつに集まる過程は、また重力の計算に戻る。
    ///
    /// したがって本実装は「重力多体の計算」＋「衝突の扱いだけを定めた模型」であり、
    /// 地球形成の数値シミュレーション研究の再現ではない。年代・規模・回数を示さない。
    /// </summary>
    public sealed class AccretionSimulation
    {
        public struct Body
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 Acceleration;
            public float Mass;
            public float Radius;
            public BodyKind Kind;
            public bool Alive;
        }

        private struct Pair
        {
            public int A;
            public int B;
        }

        private readonly SimSettings settings;

        private Body[] bodies = new Body[1];
        private int[] mergedInto = new int[1];
        private Pair[] pending = new Pair[1];

        private int count;
        private int pendingCount;

        private Rng rng;
        private SimPhase phase;
        private float time;
        private float protoEarthAt;
        private float impactAt;
        private int mergeEvents;

        public AccretionSimulation(SimSettings simSettings)
        {
            settings = simSettings ?? new SimSettings();
            Restart(settings.Seed);
        }

        /// <summary>天体の配列。<see cref="Count"/> 個までが有効である。</summary>
        public Body[] Bodies
        {
            get { return bodies; }
        }

        public int Count
        {
            get { return count; }
        }

        public SimPhase Phase
        {
            get { return phase; }
        }

        /// <summary>開始からの経過。実時間ではなく、計算上の時間である。</summary>
        public float Time
        {
            get { return time; }
        }

        /// <summary>これまでに起きた合体の回数。</summary>
        public int MergeEvents
        {
            get { return mergeEvents; }
        }

        /// <summary>最も重い天体。1つも無いときは -1。</summary>
        public int LargestIndex { get; private set; }

        public float LargestMass { get; private set; }

        public float TotalLiveMass { get; private set; }

        /// <summary>系の重心。カメラの寄せ先に使う。</summary>
        public Vector3 CenterOfMass { get; private set; }

        /// <summary>系全体の動き。抵抗はこの動きとの差に対して効かせる。</summary>
        public Vector3 CenterVelocity { get; private set; }

        /// <summary>地球とみなしている天体。無いときは -1。</summary>
        public int EarthIndex { get; private set; }

        /// <summary>種を変えて最初からやり直す。</summary>
        public void Restart(int seed)
        {
            settings.Seed = seed;
            rng = new Rng(seed);
            phase = SimPhase.Accretion;
            time = 0f;
            protoEarthAt = 0f;
            impactAt = 0f;
            mergeEvents = 0;
            count = 0;

            var capacity = Mathf.Max(16, settings.PlanetesimalCount + settings.EjectaCount + 8);
            bodies = new Body[capacity];
            mergedInto = new int[capacity];
            pending = new Pair[capacity * 4];

            BuildCloud();
            Measure();
        }

        /// <summary>1ステップ進める。dt は小さいほど正確で、重い。</summary>
        public void Step(float dt)
        {
            if (count == 0 || dt <= 0f)
            {
                return;
            }

            Accumulate();
            Integrate(dt);
            ResolveMerges();
            Compact();
            Measure();

            time += dt;
            UpdatePhase();
        }

        /// <summary>質量から半径を出す。密度を一定とみなし、3乗根に比例させる。</summary>
        public float RadiusFor(float mass)
        {
            return settings.RadiusUnit * Mathf.Pow(Mathf.Max(mass, 1e-9f), 1f / 3f);
        }

        // ------------------------------------------------------------------
        // はじまりの雲
        // ------------------------------------------------------------------

        private void BuildCloud()
        {
            var n = Mathf.Max(1, settings.PlanetesimalCount);
            var each = settings.TotalMass / n;
            var radius = RadiusFor(each);

            for (var i = 0; i < n; i++)
            {
                // 面積あたりの個数をそろえるため、半径は平方根で引く。
                // そのまま一様に引くと中心に寄りすぎる。
                var r = settings.CloudRadius * Mathf.Sqrt(rng.Range(0.05f, 1f));
                var angle = rng.Range(0f, Mathf.PI * 2f);
                var height = rng.Range(-0.5f, 0.5f) * settings.CloudThickness;

                var position = new Vector3(Mathf.Cos(angle) * r, height, Mathf.Sin(angle) * r);

                // 内側にある質量だけが効くとみなして、円軌道の速さを出す。
                // 面密度が一定なら内側の質量は半径の2乗に比例する。
                var enclosed = settings.TotalMass * Mathf.Clamp01((r / settings.CloudRadius) * (r / settings.CloudRadius));
                var circular = Mathf.Sqrt(settings.Gravity * Mathf.Max(enclosed, each) / Mathf.Max(r, 0.01f));

                var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                var velocity = tangent * (circular * settings.SpinFraction);

                // ばらつきを少し入れる。完全にそろうと、いつまでも合体しない。
                velocity += rng.OnSphere() * (circular * 0.06f);

                Add(new Body
                {
                    Position = position,
                    Velocity = velocity,
                    Mass = each,
                    Radius = radius,
                    Kind = BodyKind.Planetesimal,
                    Alive = true
                });
            }
        }

        // ------------------------------------------------------------------
        // 重力
        // ------------------------------------------------------------------

        private void Accumulate()
        {
            pendingCount = 0;

            for (var i = 0; i < count; i++)
            {
                bodies[i].Acceleration = Vector3.zero;
            }

            var softening2 = settings.Softening * settings.Softening;
            var g = settings.Gravity;
            var slack = settings.MergeSlack;

            // 破片どうしのあいだだけ引力を強める。
            //
            // 破片は軽く、地球のそばでは潮汐に負けて集まれない。実際の月も、
            // 円盤が集まって月になるまでに何十回も回っている。その回数ぶんの時間を
            // 縮めるかわりに、破片どうしの引力を強めて同じ結末へ早く運んでいる。
            // **物理としては正しくない。** 何倍にしているかを画面から変えられるようにして、
            // 倍率を上げ下げすると集まり方が変わることを確かめられるようにしてある。
            // 力は互いに同じ大きさで向きが逆のままなので、全体の運動量は保たれる。
            var debrisBoost = Mathf.Max(1f, settings.DebrisAttraction);

            for (var i = 0; i < count; i++)
            {
                var pi = bodies[i].Position;
                var mi = bodies[i].Mass;
                var ri = bodies[i].Radius;
                var ai = bodies[i].Acceleration;

                for (var j = i + 1; j < count; j++)
                {
                    var dx = bodies[j].Position.x - pi.x;
                    var dy = bodies[j].Position.y - pi.y;
                    var dz = bodies[j].Position.z - pi.z;
                    var distance2 = dx * dx + dy * dy + dz * dz;

                    var touch = (ri + bodies[j].Radius) * slack;
                    if (distance2 <= touch * touch)
                    {
                        // 触れている対は力を足さない。めり込んだ瞬間に力が跳ねるのを防ぐ。
                        AddPending(i, j);
                        continue;
                    }

                    var inverse = 1f / Mathf.Sqrt(distance2 + softening2);
                    var scale = g * inverse * inverse * inverse;

                    if (bodies[i].Kind == BodyKind.Debris && bodies[j].Kind == BodyKind.Debris)
                    {
                        scale *= debrisBoost;
                    }

                    var toJ = scale * bodies[j].Mass;
                    ai.x += dx * toJ;
                    ai.y += dy * toJ;
                    ai.z += dz * toJ;

                    var toI = scale * mi;
                    bodies[j].Acceleration.x -= dx * toI;
                    bodies[j].Acceleration.y -= dy * toI;
                    bodies[j].Acceleration.z -= dz * toI;
                }

                bodies[i].Acceleration = ai;
            }
        }

        private void Integrate(float dt)
        {
            // 集積のあいだだけ抵抗を効かせる。
            //
            // 重力だけだと、最後に残った数個が互いを回る安定な組になり、
            // いつまでも合体しない（実測：既定の条件で天体3〜4個のまま300以上進んでも終わらない）。
            // 実際の集積も、原始惑星系円盤のガスが抵抗として効いて軌道が縮むことで進む。
            // その働きを、系全体の動きとの差を減らす力としてならしたものである。
            // 大きさは見て分かる速さに合わせた値であり、ガスの密度から求めた量ではない。
            var drag = phase == SimPhase.Accretion ? Mathf.Max(0f, settings.GasDrag) : 0f;

            // 衝突直後の円盤の粘りを、軌道を円くする向きの力としてならしたもの。
            //
            // 破片だけに効かせ、その破片自身が回っている面と向きは変えない。
            // 変えるのは「同じ半径の円軌道からのずれ」だけである。半径の近い破片の
            // 動きがそろい、追いつき追い越すところで触れて合体する。
            //
            // これが無いと破片は月にならない（実測：粘り0だと、どの配置でも破片の
            // 2〜3割が集まったところで止まり、十数個のまま散らばって残る）。
            // 逆に、平均の速度へ引き寄せる形にすると周回そのものが消え、
            // 破片が地球へ落ちて月がほとんど残らない（実測：質量比0.0002〜0.0038）。
            //
            // 実際の巨大衝突でできる円盤は大部分が蒸発しており、粘性で広がりながら
            // 集まっていくと考えられている。円くする働きの向きだけを写した項である。
            // 大きさは、見ていられる時間でひとつにまとまるように決めた値であり、
            // 粘性率から求めた量ではない。
            var viscosity = phase == SimPhase.Impact || phase == SimPhase.MoonForming
                ? Mathf.Max(0f, settings.DiskViscosity)
                : 0f;

            var earth = EarthIndex;
            var canCircularise = viscosity > 0f && earth >= 0 && earth < count;

            for (var i = 0; i < count; i++)
            {
                if (drag > 0f)
                {
                    bodies[i].Acceleration -= (bodies[i].Velocity - CenterVelocity) * drag;
                }

                if (canCircularise && bodies[i].Kind == BodyKind.Debris)
                {
                    bodies[i].Acceleration += Circularise(i, earth) * viscosity;
                }

                bodies[i].Velocity += bodies[i].Acceleration * dt;
                bodies[i].Position += bodies[i].Velocity * dt;
            }
        }

        /// <summary>
        /// その破片を、いまの半径・いまの回る向きのままの円軌道へ近づける向き。
        /// 面も回る向きも変えないため、衝突で決まった角運動量の向きは残る。
        /// </summary>
        private Vector3 Circularise(int index, int earth)
        {
            var offset = bodies[index].Position - bodies[earth].Position;
            var distance = offset.magnitude;
            if (distance < 1e-4f)
            {
                return Vector3.zero;
            }

            var relative = bodies[index].Velocity - bodies[earth].Velocity;
            var angular = Vector3.Cross(offset, relative);
            if (angular.sqrMagnitude < 1e-8f)
            {
                // まっすぐ落ちている破片には、回る向きが無い。触らない。
                return Vector3.zero;
            }

            var tangent = Vector3.Cross(angular.normalized, offset / distance);
            var circular = Mathf.Sqrt(settings.Gravity * bodies[earth].Mass / distance);
            var wanted = bodies[earth].Velocity + tangent * circular;

            return wanted - bodies[index].Velocity;
        }

        // ------------------------------------------------------------------
        // 合体
        // ------------------------------------------------------------------

        private void ResolveMerges()
        {
            if (pendingCount == 0)
            {
                return;
            }

            for (var i = 0; i < count; i++)
            {
                mergedInto[i] = -1;
            }

            for (var p = 0; p < pendingCount; p++)
            {
                var a = Resolve(pending[p].A);
                var b = Resolve(pending[p].B);

                if (a == b || !bodies[a].Alive || !bodies[b].Alive)
                {
                    continue;
                }

                if (bodies[b].Mass > bodies[a].Mass)
                {
                    var swap = a;
                    a = b;
                    b = swap;
                }

                if (phase == SimPhase.Impactor && IsGiantImpact(a, b))
                {
                    GiantImpact(a, b);
                    continue;
                }

                MergeInto(a, b);
            }

            pendingCount = 0;
        }

        private bool IsGiantImpact(int a, int b)
        {
            var hasImpactor = bodies[a].Kind == BodyKind.Impactor || bodies[b].Kind == BodyKind.Impactor;
            var hasEarth = bodies[a].Kind == BodyKind.Earth || bodies[b].Kind == BodyKind.Earth;
            return hasImpactor && hasEarth;
        }

        /// <summary>b を a へ合わせる。質量と運動量を保つ。</summary>
        private void MergeInto(int a, int b)
        {
            var total = bodies[a].Mass + bodies[b].Mass;
            if (total <= 0f)
            {
                return;
            }

            bodies[a].Position =
                (bodies[a].Position * bodies[a].Mass + bodies[b].Position * bodies[b].Mass) / total;
            bodies[a].Velocity =
                (bodies[a].Velocity * bodies[a].Mass + bodies[b].Velocity * bodies[b].Mass) / total;
            bodies[a].Mass = total;
            bodies[a].Radius = RadiusFor(total);

            // 地球に取り込まれたものは地球になる。それ以外は重いほうの種別を残す。
            if (bodies[b].Kind == BodyKind.Earth)
            {
                bodies[a].Kind = BodyKind.Earth;
            }

            bodies[b].Alive = false;
            mergedInto[b] = a;
            mergeEvents++;
        }

        private int Resolve(int index)
        {
            var guard = 0;
            while (mergedInto[index] >= 0 && guard++ < 64)
            {
                index = mergedInto[index];
            }

            return index;
        }

        private void Compact()
        {
            var write = 0;
            for (var i = 0; i < count; i++)
            {
                if (!bodies[i].Alive)
                {
                    continue;
                }

                if (write != i)
                {
                    bodies[write] = bodies[i];
                }

                write++;
            }

            count = write;
        }

        // ------------------------------------------------------------------
        // ジャイアントインパクト
        // ------------------------------------------------------------------

        /// <summary>
        /// 衝突の扱い。ここだけは計算ではなく模型である。
        ///
        /// 2つを合わせたうえで、決めた割合の質量を破片として周回軌道へ置く。
        /// 置く向きは、衝突の角運動量から決める。正面から当たれば回転が小さく、
        /// かすめて当たれば大きい。当て方を変えると円盤の向きが変わる、
        /// という関係だけは衝突の条件から出している。
        /// </summary>
        private void GiantImpact(int a, int b)
        {
            var earth = bodies[a].Kind == BodyKind.Earth ? a : b;
            var impactor = earth == a ? b : a;

            var earthMass = bodies[earth].Mass;
            var impactorMass = bodies[impactor].Mass;
            var total = earthMass + impactorMass;
            if (total <= 0f)
            {
                return;
            }

            var center = (bodies[earth].Position * earthMass + bodies[impactor].Position * impactorMass) / total;
            var centerVelocity = (bodies[earth].Velocity * earthMass + bodies[impactor].Velocity * impactorMass) / total;

            var relativePosition = bodies[impactor].Position - bodies[earth].Position;
            var relativeVelocity = bodies[impactor].Velocity - bodies[earth].Velocity;

            // 衝突の角運動量。破片を回す向きをここから決める。
            var spin = Vector3.Cross(relativePosition, relativeVelocity);
            var axis = spin.sqrMagnitude > 1e-8f ? spin.normalized : Vector3.up;

            var ejectaMass = total * Mathf.Clamp01(settings.EjectaMassFraction);
            var remaining = total - ejectaMass;

            bodies[earth].Position = center;
            bodies[earth].Velocity = centerVelocity;
            bodies[earth].Mass = remaining;
            bodies[earth].Radius = RadiusFor(remaining);
            bodies[earth].Kind = BodyKind.Earth;

            bodies[impactor].Alive = false;
            mergedInto[impactor] = earth;
            mergeEvents++;

            SpawnEjecta(earth, axis, relativePosition, ejectaMass);

            phase = SimPhase.Impact;
            impactAt = time;
        }

        private void SpawnEjecta(int earth, Vector3 axis, Vector3 impactDirection, float ejectaMass)
        {
            var pieces = Mathf.Max(1, settings.EjectaCount);
            var each = ejectaMass / pieces;
            var pieceRadius = RadiusFor(each);
            var earthRadius = bodies[earth].Radius;

            // 円盤の面内で使う2つの向き。当たった側を弧の中心にする。
            var forward = Vector3.ProjectOnPlane(impactDirection, axis);
            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.right, axis);
            }

            if (forward.sqrMagnitude < 1e-8f)
            {
                forward = Vector3.ProjectOnPlane(Vector3.forward, axis);
            }

            forward = forward.normalized;
            var side = Vector3.Cross(axis, forward);

            var arc = settings.EjectaArcDegrees * Mathf.Deg2Rad;
            var inner = Mathf.Min(settings.EjectaInnerRadius, settings.EjectaOuterRadius);
            var outer = Mathf.Max(settings.EjectaInnerRadius, settings.EjectaOuterRadius);

            for (var k = 0; k < pieces; k++)
            {
                var along = pieces == 1 ? 0.5f : k / (float)(pieces - 1);
                var angle = (along - 0.5f) * arc + rng.Range(-0.04f, 0.04f);

                var distance = Mathf.Lerp(inner, outer, rng.Value()) * earthRadius;
                var direction = forward * Mathf.Cos(angle) + side * Mathf.Sin(angle);
                var tangent = Vector3.Cross(axis, direction);

                var circular = Mathf.Sqrt(settings.Gravity * bodies[earth].Mass / Mathf.Max(distance, 0.01f));
                var speed = circular * settings.EjectaSpeedFactor * rng.Range(0.97f, 1.03f);

                // 完全な平面に置くと、すべてが同じ軌道を回って重ならない。少し浮かせる。
                var lift = axis * (rng.Range(-0.06f, 0.06f) * distance);

                Add(new Body
                {
                    Position = bodies[earth].Position + direction * distance + lift,
                    Velocity = bodies[earth].Velocity + tangent * speed,
                    Mass = each,
                    Radius = pieceRadius,
                    Kind = BodyKind.Debris,
                    Alive = true
                });
            }
        }

        private void SpawnImpactor()
        {
            if (LargestIndex < 0)
            {
                return;
            }

            var earth = LargestIndex;
            var mass = bodies[earth].Mass * Mathf.Max(0.01f, settings.ImpactorMassFraction);
            var radius = RadiusFor(mass);

            var direction = rng.OnSphere();
            if (direction.sqrMagnitude < 1e-6f)
            {
                direction = Vector3.right;
            }

            direction = direction.normalized;

            // 真正面ではなく、少しずらして狙う。かすめて当たると回転が残る。
            var reference = Mathf.Abs(direction.y) > 0.9f ? Vector3.forward : Vector3.up;
            var offsetDirection = Vector3.Cross(direction, reference).normalized;
            var offset = offsetDirection * (settings.ImpactParameter * (bodies[earth].Radius + radius));

            var position = bodies[earth].Position + direction * settings.ImpactorStartDistance;
            var aim = (bodies[earth].Position + offset - position).normalized;

            // その距離での脱出速度を基準にする。1.0なら、ほぼ放物線で落ちてくる。
            var escape = Mathf.Sqrt(
                2f * settings.Gravity * (bodies[earth].Mass + mass) /
                Mathf.Max(settings.ImpactorStartDistance, 0.01f));

            Add(new Body
            {
                Position = position,
                Velocity = bodies[earth].Velocity + aim * (escape * settings.ImpactorSpeedFactor),
                Mass = mass,
                Radius = radius,
                Kind = BodyKind.Impactor,
                Alive = true
            });
        }

        // ------------------------------------------------------------------
        // 段階
        // ------------------------------------------------------------------

        private void UpdatePhase()
        {
            switch (phase)
            {
                case SimPhase.Accretion:
                    if (LargestIndex >= 0 && TotalLiveMass > 0f &&
                        LargestMass >= TotalLiveMass * settings.ProtoEarthMassFraction)
                    {
                        bodies[LargestIndex].Kind = BodyKind.Earth;
                        phase = SimPhase.ProtoEarth;
                        protoEarthAt = time;
                    }

                    break;

                case SimPhase.ProtoEarth:
                    if (time - protoEarthAt >= settings.ImpactorDelay)
                    {
                        SpawnImpactor();
                        Measure();
                        phase = SimPhase.Impactor;
                    }

                    break;

                case SimPhase.Impactor:
                    // 衝突が起きると GiantImpact が段階を進める。ここでは待つだけ。
                    break;

                case SimPhase.Impact:
                    if (time - impactAt >= 0.5f)
                    {
                        phase = SimPhase.MoonForming;
                    }

                    break;

                case SimPhase.MoonForming:
                    // ひとつにまとまったら落ち着いたとみなす。まとまらないこともあるため、
                    // 十分に待ったら、そのときいちばん重いものを月と呼んで先へ進める。
                    if (time - impactAt >= settings.SettleDelay &&
                        (CountOtherThanEarth() <= 1 || time - impactAt >= settings.SettleDelay * 8f))
                    {
                        NameTheMoon();
                        phase = SimPhase.Settled;
                    }

                    break;
            }
        }

        private int CountOtherThanEarth()
        {
            var others = 0;
            for (var i = 0; i < count; i++)
            {
                if (bodies[i].Kind != BodyKind.Earth)
                {
                    others++;
                }
            }

            return others;
        }

        /// <summary>地球以外で最も重いものを月と呼ぶ。名前を付けるだけで、運動は変えない。</summary>
        private void NameTheMoon()
        {
            var best = -1;
            var bestMass = 0f;

            for (var i = 0; i < count; i++)
            {
                if (bodies[i].Kind == BodyKind.Earth || bodies[i].Mass <= bestMass)
                {
                    continue;
                }

                best = i;
                bestMass = bodies[i].Mass;
            }

            if (best >= 0)
            {
                bodies[best].Kind = BodyKind.Moon;
            }
        }

        private void Measure()
        {
            LargestIndex = -1;
            LargestMass = 0f;
            TotalLiveMass = 0f;

            EarthIndex = -1;

            var weighted = Vector3.zero;
            var weightedVelocity = Vector3.zero;

            for (var i = 0; i < count; i++)
            {
                var mass = bodies[i].Mass;
                TotalLiveMass += mass;
                weighted += bodies[i].Position * mass;
                weightedVelocity += bodies[i].Velocity * mass;

                if (bodies[i].Kind == BodyKind.Earth)
                {
                    EarthIndex = i;
                }

                if (mass <= LargestMass)
                {
                    continue;
                }

                LargestMass = mass;
                LargestIndex = i;
            }

            CenterOfMass = TotalLiveMass > 0f ? weighted / TotalLiveMass : Vector3.zero;
            CenterVelocity = TotalLiveMass > 0f ? weightedVelocity / TotalLiveMass : Vector3.zero;
        }

        // ------------------------------------------------------------------
        // 配列
        // ------------------------------------------------------------------

        private void Add(Body body)
        {
            if (count >= bodies.Length)
            {
                var size = Mathf.Max(16, bodies.Length * 2);
                Array.Resize(ref bodies, size);
                Array.Resize(ref mergedInto, size);
            }

            bodies[count] = body;
            mergedInto[count] = -1;
            count++;
        }

        private void AddPending(int a, int b)
        {
            if (pendingCount >= pending.Length)
            {
                Array.Resize(ref pending, Mathf.Max(16, pending.Length * 2));
            }

            pending[pendingCount].A = a;
            pending[pendingCount].B = b;
            pendingCount++;
        }
    }
}
