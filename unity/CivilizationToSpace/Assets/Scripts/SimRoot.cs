using CivilizationToSpace.Sim;
using CivilizationToSpace.View;
using UnityEngine;

namespace CivilizationToSpace
{
    /// <summary>
    /// 「地球ができるまで」を計算で動かす画面の起点。
    ///
    /// <see cref="AppRoot"/> とは別物である。あちらは用意した時代データを順に見せる。
    /// こちらはデータを読まず、重力の計算だけで形を決める。
    /// 両方を残しているのは、見せたいものが違うためである。
    /// </summary>
    public sealed class SimRoot : MonoBehaviour
    {
        [Tooltip("計算の条件。実行中に画面からも変えられます。")]
        [SerializeField]
        private SimSettings settings = new SimSettings();

        /// <summary>画面へ収める半径の下限。寄りすぎて何も見えなくなるのを防ぐ。</summary>
        private const float MinimumFrameRadius = 7f;

        /// <summary>この割合の質量が入る範囲を画面へ収める。遠くへ飛んだ1個に引きずられないため。</summary>
        private const float FrameMassFraction = 0.94f;

        private AccretionSimulation simulation;
        private SimulationView view;
        private SimulationHud hud;
        private SimCameraControl cameraControl;

        private float accumulator;

        /// <summary>
        /// 画面へ収める範囲を測るための入れ物。毎フレーム作り直すと、
        /// 捨てる処理が積み上がって端末が一定の間隔で引っかかる。使い回す。
        /// </summary>
        private readonly float[] histogram = new float[FrameBuckets];

        /// <summary>距離を何段階に区切って数えるか。</summary>
        private const int FrameBuckets = 24;

        public AccretionSimulation Simulation
        {
            get { return simulation; }
        }

        /// <summary>読込も組み立ても済んだか。点検ツールが待つために見る。</summary>
        public bool Ready { get; private set; }

        private void Start()
        {
            simulation = new AccretionSimulation(settings);

            view = new SimulationView();
            view.Build();

            var camera = Camera.main;
            if (camera != null)
            {
                cameraControl = camera.GetComponent<SimCameraControl>();
                if (cameraControl == null)
                {
                    cameraControl = camera.gameObject.AddComponent<SimCameraControl>();
                }
            }

            JapaneseFont.Get();
            Debug.Log("[UI] 日本語フォント: " + JapaneseFont.ResolvedName +
                      (JapaneseFont.FellBackToBuiltin ? "（内蔵へフォールバック・日本語が出ない可能性）" : string.Empty));

            hud = new GameObject("SimHud").AddComponent<SimulationHud>();
            hud.transform.SetParent(transform, false);
            hud.Build(camera, settings);
            hud.RestartRequested = OnRestartRequested;
            hud.ResetViewRequested = OnResetViewRequested;

            Ready = true;
        }

        private void OnDestroy()
        {
            if (view != null)
            {
                view.Dispose();
                view = null;
            }
        }

        private void Update()
        {
            if (simulation == null)
            {
                return;
            }

            Advance();
            Frame();

            view.Draw(simulation);
            hud.Refresh(simulation);
        }

        /// <summary>
        /// 決まった刻みで進める。フレームの長さで刻みを変えると、
        /// 端末の速さで結果が変わってしまう。速さの指定は「1フレームで何回進めるか」で効かせる。
        /// </summary>
        private void Advance()
        {
            if (hud != null && hud.Paused)
            {
                accumulator = 0f;
                return;
            }

            var step = Mathf.Max(0.001f, settings.StepSize);
            var speed = hud != null ? hud.Speed : 1f;

            accumulator += Time.deltaTime * speed;

            var steps = 0;
            var limit = Mathf.Max(1, settings.MaxStepsPerFrame);

            while (accumulator >= step && steps < limit)
            {
                simulation.Step(step);
                accumulator -= step;
                steps++;
            }

            // 追いつけないときは溜めずに捨てる。溜めると、重くなるほど加速して破綻する。
            if (accumulator > step * limit)
            {
                accumulator = 0f;
            }
        }

        /// <summary>
        /// 見せたい範囲を測ってカメラへ渡す。
        /// 重いものから順に入れていき、全体の質量の大半が入った時点の距離を半径とする。
        /// </summary>
        private void Frame()
        {
            if (cameraControl == null)
            {
                return;
            }

            var center = simulation.LargestIndex >= 0
                ? simulation.Bodies[simulation.LargestIndex].Position
                : simulation.CenterOfMass;

            cameraControl.Target = center;
            cameraControl.FrameRadius = Mathf.Max(MinimumFrameRadius, MassWeightedRadius(center));
        }

        private float MassWeightedRadius(Vector3 center)
        {
            var bodies = simulation.Bodies;
            var count = simulation.Count;
            var total = simulation.TotalLiveMass;

            if (count == 0 || total <= 0f)
            {
                return MinimumFrameRadius;
            }

            // 距離の順に並べ替えず、距離を段階に区切って質量を数える。
            // 毎フレームの並べ替えは、数百個でも端末によっては効いてくる。
            var farthest = 0f;

            for (var i = 0; i < count; i++)
            {
                var distance = Vector3.Distance(bodies[i].Position, center) + bodies[i].Radius;
                if (distance > farthest)
                {
                    farthest = distance;
                }
            }

            if (farthest <= 0f)
            {
                return MinimumFrameRadius;
            }

            for (var b = 0; b < FrameBuckets; b++)
            {
                histogram[b] = 0f;
            }

            for (var i = 0; i < count; i++)
            {
                var distance = Vector3.Distance(bodies[i].Position, center) + bodies[i].Radius;
                var bucket = Mathf.Clamp(
                    Mathf.FloorToInt(distance / farthest * (FrameBuckets - 1)), 0, FrameBuckets - 1);
                histogram[bucket] += bodies[i].Mass;
            }

            var wanted = total * FrameMassFraction;
            var running = 0f;
            var byMass = farthest;

            for (var b = 0; b < FrameBuckets; b++)
            {
                running += histogram[b];
                if (running >= wanted)
                {
                    byMass = (b + 1) / (float)FrameBuckets * farthest;
                    break;
                }
            }

            // 質量だけで決めると、地球が全体のほぼすべてを占めた時点で地球へ寄りきり、
            // 月が画面の外へ出る。重い上位2つは必ず入れる。終盤は地球と月、衝突の前は
            // 地球と衝突天体、破片のあいだは地球と最大の塊がこれにあたる。
            return Mathf.Max(byMass, TopTwoRadius(center));
        }

        /// <summary>最も重い2つが収まる半径。少し余白を足す。</summary>
        private float TopTwoRadius(Vector3 center)
        {
            var bodies = simulation.Bodies;
            var count = simulation.Count;

            var first = -1;
            var second = -1;
            var firstMass = -1f;
            var secondMass = -1f;

            for (var i = 0; i < count; i++)
            {
                if (bodies[i].Mass > firstMass)
                {
                    second = first;
                    secondMass = firstMass;
                    first = i;
                    firstMass = bodies[i].Mass;
                }
                else if (bodies[i].Mass > secondMass)
                {
                    second = i;
                    secondMass = bodies[i].Mass;
                }
            }

            var radius = 0f;
            radius = Mathf.Max(radius, DistanceWithMargin(first, center));
            radius = Mathf.Max(radius, DistanceWithMargin(second, center));

            return radius * 1.2f;
        }

        private float DistanceWithMargin(int index, Vector3 center)
        {
            if (index < 0 || index >= simulation.Count)
            {
                return 0f;
            }

            var bodies = simulation.Bodies;
            return Vector3.Distance(bodies[index].Position, center) + bodies[index].Radius * 1.6f;
        }

        private void OnRestartRequested(int seed)
        {
            accumulator = 0f;
            simulation.Restart(seed);
        }

        private void OnResetViewRequested()
        {
            if (cameraControl != null)
            {
                cameraControl.ResetView();
            }
        }
    }
}
