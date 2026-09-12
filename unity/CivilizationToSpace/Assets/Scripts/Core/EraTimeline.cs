using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationToSpace.Core
{
    /// <summary>
    /// どの時代を選んでいるかだけを持つ状態。表示はすべてここから導く。
    /// 画面の部品ごとに別の時代を持たせない。
    ///
    /// S3では選択のみを扱う。時系列再生はS4で足す。
    /// static も Singleton も作らない。
    /// </summary>
    public sealed class EraTimeline
    {
        private readonly IReadOnlyList<EraData> eras;
        private int index;

        /// <summary>
        /// 時代の先に続く段階の数。月への展開がこれにあたる。
        /// 0なら時代だけで終わる。時代データ自体は増やさない。
        /// </summary>
        private int tailCount;

        public EraTimeline(IReadOnlyList<EraData> eras)
        {
            if (eras == null || eras.Count == 0)
            {
                throw new ArgumentException("eras must not be empty", "eras");
            }

            this.eras = eras;
            index = 0;
        }

        /// <summary>選択が変わったときだけ呼ばれる。同じ時代を選び直しても呼ばれない。</summary>
        public event Action<EraData> Changed;

        public int Index
        {
            get { return index; }
        }

        /// <summary>時代の数。月への展開は含まない。</summary>
        public int EraCount
        {
            get { return eras.Count; }
        }

        /// <summary>時代と、その先に続く段階を合わせた数。</summary>
        public int Count
        {
            get { return eras.Count + tailCount; }
        }

        /// <summary>時代の先に続く段階の数を決める。</summary>
        public void SetTailCount(int count)
        {
            tailCount = Mathf.Max(0, count);
            if (index > Count - 1)
            {
                Select(Count - 1);
            }
        }

        /// <summary>いま時代を指しているか。偽なら先に続く段階を指している。</summary>
        public bool InEra
        {
            get { return index < eras.Count; }
        }

        /// <summary>先に続く段階の位置。時代を指しているときは -1。</summary>
        public int TailIndex
        {
            get { return InEra ? -1 : index - eras.Count; }
        }

        /// <summary>いまの時代。先に続く段階を指しているときは最後の時代を返す。</summary>
        public EraData Current
        {
            get { return eras[Mathf.Min(index, eras.Count - 1)]; }
        }

        /// <summary>並び順どおりの位置で取り出す。ボタンの見出しを作るために使う。</summary>
        public EraData At(int position)
        {
            return eras[position];
        }

        public bool HasPrevious
        {
            get { return index > 0; }
        }

        public bool HasNext
        {
            get { return index < Count - 1; }
        }

        /// <summary>範囲外は端で止める。折り返さない。</summary>
        public void Select(int next)
        {
            if (next < 0)
            {
                next = 0;
            }
            else if (next > Count - 1)
            {
                next = Count - 1;
            }

            if (next == index)
            {
                return;
            }

            index = next;
            var handler = Changed;
            if (handler != null)
            {
                handler(Current);
            }
        }

        public void Previous()
        {
            Select(index - 1);
        }

        public void Next()
        {
            Select(index + 1);
        }
    }
}
