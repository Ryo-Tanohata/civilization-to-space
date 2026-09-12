using System;
using System.Collections.Generic;

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

        public int Count
        {
            get { return eras.Count; }
        }

        public EraData Current
        {
            get { return eras[index]; }
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
            get { return index < eras.Count - 1; }
        }

        /// <summary>範囲外は端で止める。折り返さない。</summary>
        public void Select(int next)
        {
            if (next < 0)
            {
                next = 0;
            }
            else if (next > eras.Count - 1)
            {
                next = eras.Count - 1;
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
