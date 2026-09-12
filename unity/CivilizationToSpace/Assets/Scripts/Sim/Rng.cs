using UnityEngine;

namespace CivilizationToSpace.Sim
{
    /// <summary>
    /// 種を決めれば同じ結果になる乱数。xorshift32。
    ///
    /// <see cref="UnityEngine.Random"/> を使わないのは、あれが処理系全体で共有される
    /// 状態を持ち、他の処理が引くと結果が変わってしまうためである。
    /// 「同じ種なら同じ形成過程になる」と言えないと、見え方の相談ができない。
    /// </summary>
    public sealed class Rng
    {
        private uint state;

        public Rng(int seed)
        {
            state = unchecked((uint)seed);
            if (state == 0u)
            {
                state = 2463534242u;
            }
        }

        /// <summary>0以上1未満。</summary>
        public float Value()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        /// <summary>球面上の一様な向き。</summary>
        public Vector3 OnSphere()
        {
            var z = Range(-1f, 1f);
            var angle = Range(0f, Mathf.PI * 2f);
            var r = Mathf.Sqrt(Mathf.Max(0f, 1f - z * z));
            return new Vector3(r * Mathf.Cos(angle), r * Mathf.Sin(angle), z);
        }

        private uint NextUInt()
        {
            var x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }
    }
}
