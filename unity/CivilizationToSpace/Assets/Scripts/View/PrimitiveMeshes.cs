using System.Collections.Generic;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 組み込みの球・立方体などを、当たり判定を付けずに作る。
    ///
    /// **なぜ GameObject.CreatePrimitive を直接使わないのか。**
    /// CreatePrimitive は必ず当たり判定（Collider）を付けようとする。
    /// このビルドは stripEngineCode を有効にしており、当たり判定を誰も使わないため
    /// Physics のモジュールごと削られている。その結果、呼ぶたびに
    ///
    ///   Can't add component because class 'SphereCollider' doesn't exist!
    ///
    /// がコンソールへ出ていた。描画には影響しないが、1回の起動で30件ほど積もる。
    /// 数が多いと本物のエラーがその中に埋もれる。実際、この見落としで
    /// 検査の側がエラー無しと報告してしまっていた。
    ///
    /// ここではメッシュだけを借りて、MeshFilter と MeshRenderer を自分で付ける。
    /// 当たり判定を作らないので、上のエラーは出ない。
    ///
    /// メッシュは種類ごとに1つ覚えておく。組み込みのメッシュは共有資産であり、
    /// 借りるために作った入れ物を捨てても生き続ける。
    /// </summary>
    public static class PrimitiveMeshes
    {
        private static readonly Dictionary<PrimitiveType, Mesh> Cache =
            new Dictionary<PrimitiveType, Mesh>();

        /// <summary>種類に応じたメッシュを返す。</summary>
        public static Mesh Get(PrimitiveType type)
        {
            Mesh cached;
            if (Cache.TryGetValue(type, out cached) && cached != null)
            {
                return cached;
            }

            var mesh = BorrowFromPrimitive(type);
            Cache[type] = mesh;
            return mesh;
        }

        /// <summary>
        /// 当たり判定を持たない表示だけの入れ物を作る。
        /// 材質は呼び出し側で入れること。ここでは入れない。
        /// </summary>
        public static GameObject Create(PrimitiveType type, string name, HideFlags flags)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.hideFlags = flags;
            item.GetComponent<MeshFilter>().sharedMesh = Get(type);
            return item;
        }

        /// <summary>
        /// プリミティブを1つだけ作ってメッシュを受け取り、入れ物は捨てる。
        ///
        /// Resources.GetBuiltinResource で名前から引く手もあるが、
        /// そちらの球は半径1.0で、CreatePrimitive の球（半径0.5）と大きさが違う。
        /// 取り違えると、月も大気の殻もすべて倍の大きさで描かれる。
        /// 大きさを確実に合わせるため、実際にプリミティブを作って借りる。
        ///
        /// 当たり判定のエラーはここでも出るが、種類ごとに1回で済む。
        /// </summary>
        private static Mesh BorrowFromPrimitive(PrimitiveType type)
        {
            var temporary = GameObject.CreatePrimitive(type);
            var filter = temporary.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;

            if (Application.isPlaying)
            {
                Object.Destroy(temporary);
            }
            else
            {
                Object.DestroyImmediate(temporary);
            }

            return mesh;
        }
    }
}
