using System;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// 時系列の自動再生。判定は site/app.js と対応させる。
    ///
    /// - 1時代あたり 4秒 ÷ 速度
    /// - 最後の時代へ達したら自動的に止まる
    /// - 最後の時代で再生を始めると、最初の時代へ戻してから再生する
    /// - 手で時代を変えたら止まる
    /// - 速度を変えても再生状態は保ち、その時点から計時し直す
    ///
    /// 選べる速度だけはブラウザモックと一致しない。モックは 0.5/1/2 のままで、
    /// こちらは 4倍・8倍を足している。段階が15まで増え、通しで見ると
    /// 1倍では1分を超えるためである。上の判定そのものは変えていない。
    ///
    /// R2-P1はこのクラスの IsPlaying と RequestStop だけに依存する。
    /// それ以外を公開しない。static も Singleton も作らない。
    /// </summary>
    public sealed class TimelinePlayback
    {
        /// <summary>1倍のときの1時代あたりの秒数。</summary>
        public const float BaseStepSeconds = 4f;

        /// <summary>
        /// 1倍のときの、形成過程の1段階あたりの秒数。
        ///
        /// 時代は4秒だが、形成過程だけは長く取る。巨大衝突の段階では
        /// 「遠くから近づく → ぶつかる → 砕ける → 引力で丸く戻る」を
        /// ひと続きで見せる必要があり、4秒では近づいている途中で次の段階へ移る。
        /// 月の形成も、破片が集まって球になるまでに時間がかかる。
        ///
        /// 時代の4秒は site/app.js と対応させているが、
        /// 形成過程はUnityだけにある段階なので、ブラウザとの対応は崩れない。
        /// </summary>
        public const float FormationStepSeconds = 8f;

        /// <summary>
        /// 選べる速度。8倍では1段階0.5秒になり、通しで約8秒になる。
        /// これより速くすると、段階が切り替わったことを目で追えない。
        /// </summary>
        private static readonly float[] AllowedSpeeds = { 0.5f, 1f, 2f, 4f, 8f };
        private const float DefaultSpeed = 1f;

        private readonly EraTimeline timeline;

        private bool playing;
        private float speed = DefaultSpeed;
        private float elapsed;

        /// <summary>自分で進めている最中かどうか。手動の変更と区別するために持つ。</summary>
        private bool advancing;

        public TimelinePlayback(EraTimeline timeline)
        {
            if (timeline == null)
            {
                throw new ArgumentNullException("timeline");
            }

            this.timeline = timeline;

            // 経路を問わず、手で時代が変わったら止める。
            // ボタン・スライダー・前後移動のどれから来ても同じ扱いになる。
            this.timeline.Changed += OnTimelineChanged;
        }

        /// <summary>再生・停止・速度のいずれかが変わったときに呼ばれる。</summary>
        public event Action Changed;

        /// <summary>再生中かどうか。R2-P1が読む。</summary>
        public bool IsPlaying
        {
            get { return playing; }
        }

        public float Speed
        {
            get { return speed; }
        }

        /// <summary>現在の速度での1時代あたりの秒数。表示に使う。</summary>
        public float EraStepSeconds
        {
            get { return BaseStepSeconds / speed; }
        }

        /// <summary>
        /// いま指している段階を、次へ進めるまでの秒数。
        /// 形成過程のあいだだけ長く、時代へ入ると4秒へ戻る。
        /// </summary>
        public float StepSeconds
        {
            get
            {
                var seconds = timeline.HeadIndex >= 0 ? FormationStepSeconds : BaseStepSeconds;
                return seconds / speed;
            }
        }

        /// <summary>選べる速度。表示の順序もこの並びに従う。</summary>
        public static float[] Speeds
        {
            get { return (float[])AllowedSpeeds.Clone(); }
        }

        /// <summary>停止を要求する。すでに止まっていれば何もしない。R2-P1が呼ぶ。</summary>
        public void RequestStop()
        {
            if (!playing)
            {
                return;
            }

            playing = false;
            elapsed = 0f;
            Raise();
        }

        public void Toggle()
        {
            if (playing)
            {
                RequestStop();
                return;
            }

            // 最後の時代で再生を始めた場合は最初へ戻してから再生する。
            if (timeline.Index >= timeline.Count - 1)
            {
                advancing = true;
                timeline.Select(0);
                advancing = false;
            }

            playing = true;
            elapsed = 0f;
            Raise();
        }

        public void SetSpeed(float value)
        {
            var next = DefaultSpeed;
            foreach (var allowed in AllowedSpeeds)
            {
                if (Math.Abs(allowed - value) < 0.0001f)
                {
                    next = allowed;
                    break;
                }
            }

            if (Math.Abs(next - speed) < 0.0001f)
            {
                return;
            }

            speed = next;

            // 再生状態は保ち、変更時点から新しい間隔で計時し直す。
            elapsed = 0f;
            Raise();
        }

        /// <summary>次の速度へ回す。0.5x → 1x → 2x → 0.5x。</summary>
        public void CycleSpeed()
        {
            var index = 0;
            for (var i = 0; i < AllowedSpeeds.Length; i++)
            {
                if (Math.Abs(AllowedSpeeds[i] - speed) < 0.0001f)
                {
                    index = i;
                    break;
                }
            }

            SetSpeed(AllowedSpeeds[(index + 1) % AllowedSpeeds.Length]);
        }

        /// <summary>毎フレーム呼ぶ。経過が1歩分に達したら次の時代へ進める。</summary>
        public void Tick(float deltaTime)
        {
            if (!playing)
            {
                return;
            }

            elapsed += deltaTime;
            if (elapsed < StepSeconds)
            {
                return;
            }

            elapsed = 0f;
            Advance();
        }

        private void Advance()
        {
            var last = timeline.Count - 1;
            if (timeline.Index >= last)
            {
                RequestStop();
                return;
            }

            advancing = true;
            timeline.Next();
            advancing = false;

            // 最後の時代へ達したら自動再生を止める。
            if (timeline.Index >= last)
            {
                playing = false;
                Raise();
            }
        }

        private void OnTimelineChanged(EraData era)
        {
            if (advancing)
            {
                return;
            }

            RequestStop();
        }

        private void Raise()
        {
            var handler = Changed;
            if (handler != null)
            {
                handler();
            }
        }
    }
}
