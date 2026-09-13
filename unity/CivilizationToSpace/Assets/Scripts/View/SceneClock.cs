using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 見せている場面だけの時計。
    ///
    /// 地球の自転、微惑星の周回、月の行き来など、場面の中で動くものは
    /// <c>Time.deltaTime</c> ではなくここの <see cref="Delta"/> を見る。
    ///
    /// **なぜ Time.timeScale を使わないのか。**
    /// コマ送りのために <c>Time.timeScale = 0</c> で全体を止めたところ、
    /// 画面は止まったが、そのあとボタンがまったく効かなくなった。
    /// 止めたことを解除する手立ても押せなくなるため、利用者から見れば
    /// 操作不能である。時間はUnity側を触らず、こちらの値だけで扱う。
    ///
    /// 止めているあいだは <see cref="Delta"/> が0を返し、
    /// <see cref="RequestStep"/> を呼んだ次の1回だけ決まった長さを返す。
    /// 操作や表示はUnityの時間で動き続けるので、押せなくなることはない。
    /// </summary>
    public static class SceneClock
    {
        /// <summary>コマ送り1回で進む長さ（秒）。</summary>
        public const float StepSeconds = 0.12f;

        private static bool paused;
        private static bool stepQueued;

        /// <summary>頼まれたコマ送りを、誰かが実際に受け取ったか。</summary>
        private static bool stepTaken;

        /// <summary>止めているかどうか。</summary>
        public static bool Paused
        {
            get { return paused; }
        }

        /// <summary>
        /// この1フレームで場面が進む長さ。
        /// 止めていなければ通常の経過時間、止めていれば0を返す。
        /// コマ送りを頼まれていれば、その1回だけ決まった長さを返す。
        /// </summary>
        public static float Delta
        {
            get
            {
                if (!paused)
                {
                    return Time.deltaTime;
                }

                if (!stepQueued)
                {
                    return 0f;
                }

                stepTaken = true;
                return StepSeconds;
            }
        }

        /// <summary>コマ送りを1回ぶん頼む。止めていなければ、まず止める。</summary>
        public static void RequestStep()
        {
            paused = true;
            stepQueued = true;
            stepTaken = false;
        }

        /// <summary>通常の進み方へ戻す。</summary>
        public static void Resume()
        {
            paused = false;
            stepQueued = false;
            stepTaken = false;
        }

        /// <summary>
        /// 1フレームの終わりに呼ぶ。受け取られたコマ送りを使い切る。
        ///
        /// まだ誰も受け取っていなければ残しておく。
        /// ボタンが押される順番と、場面を動かす側が時間を読む順番は
        /// フレーム内で前後しうるため、押した直後のフレームで
        /// 誰にも渡らないことがある。そのまま消すと、押しても進まない回が出る。
        /// </summary>
        public static void EndFrame()
        {
            if (stepTaken)
            {
                stepQueued = false;
                stepTaken = false;
            }
        }
    }
}
