using System;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 動きを減らす設定。
    ///
    /// ブラウザはCSSの prefers-reduced-motion で同じことをしているが、
    /// Unityには相当する標準の仕組みがない。画面のトグルで代替する。
    ///
    /// セッション内のメモリにだけ持つ。PlayerPrefs へ書かない。
    /// R2要件の R2-NFR-05 が永続保存を禁じている趣旨に合わせる。
    /// </summary>
    public sealed class MotionSettings
    {
        private bool reduced;

        public event Action Changed;

        /// <summary>真のとき、自転・衛星の周回・時代遷移の補間を止める。</summary>
        public bool Reduced
        {
            get { return reduced; }
            set
            {
                if (reduced == value)
                {
                    return;
                }

                reduced = value;
                var handler = Changed;
                if (handler != null)
                {
                    handler();
                }
            }
        }

        public void Toggle()
        {
            Reduced = !Reduced;
        }
    }
}
