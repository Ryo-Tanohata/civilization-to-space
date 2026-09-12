using CivilizationToSpace.Sim;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 計算した天体を描く。天体ごとに GameObject を作らない。
    ///
    /// 数百個をひとつずつ GameObject にすると、合体のたびに生成と破棄が起き、
    /// スマートフォンでは描画より先にそちらで止まる。ひとつの球を使い回し、
    /// 種別ごとにまとめて描く（<see cref="Graphics.DrawMeshInstanced"/>）。
    /// 位置はシミュレーションが持ち、こちらは毎フレーム読むだけである。
    /// </summary>
    public sealed class SimulationView
    {
        /// <summary>1回に描ける数の上限。Unityの制限である。</summary>
        private const int BatchLimit = 1023;

        private static readonly Color PlanetesimalColor = new Color(0.42f, 0.40f, 0.38f, 1f);
        private static readonly Color EarthColor = new Color(0.85f, 0.42f, 0.20f, 1f);
        private static readonly Color ImpactorColor = new Color(0.70f, 0.32f, 0.42f, 1f);
        private static readonly Color DebrisColor = new Color(0.95f, 0.62f, 0.30f, 1f);
        private static readonly Color MoonColor = new Color(0.72f, 0.73f, 0.76f, 1f);

        private const int KindCount = 5;

        private Mesh sphere;
        private Material[] materials;
        private Matrix4x4[][] batches;
        private int[] used;

        /// <summary>描いた天体の数。直前のフレームの値。</summary>
        public int DrawnCount { get; private set; }

        public void Build()
        {
            sphere = BorrowSphereMesh();

            materials = new Material[KindCount];
            materials[(int)BodyKind.Planetesimal] = CreateMaterial(PlanetesimalColor, 0f);
            materials[(int)BodyKind.Earth] = CreateMaterial(EarthColor, 0.55f);
            materials[(int)BodyKind.Impactor] = CreateMaterial(ImpactorColor, 0.25f);
            materials[(int)BodyKind.Debris] = CreateMaterial(DebrisColor, 0.7f);
            materials[(int)BodyKind.Moon] = CreateMaterial(MoonColor, 0f);

            batches = new Matrix4x4[KindCount][];
            used = new int[KindCount];
            for (var i = 0; i < KindCount; i++)
            {
                batches[i] = new Matrix4x4[BatchLimit];
            }
        }

        public void Draw(AccretionSimulation simulation)
        {
            if (simulation == null || sphere == null || materials == null)
            {
                return;
            }

            for (var i = 0; i < KindCount; i++)
            {
                used[i] = 0;
            }

            var bodies = simulation.Bodies;
            var count = simulation.Count;
            DrawnCount = count;

            for (var i = 0; i < count; i++)
            {
                var kind = (int)bodies[i].Kind;
                if (kind < 0 || kind >= KindCount)
                {
                    continue;
                }

                if (used[kind] >= BatchLimit)
                {
                    // 上限に達したぶんは、いったん描いてから続きを積む。
                    Flush(kind);
                }

                // 球の元の直径は1。半径ぶんの2倍に伸ばす。
                var diameter = bodies[i].Radius * 2f;
                batches[kind][used[kind]] = Matrix4x4.TRS(
                    bodies[i].Position, Quaternion.identity, new Vector3(diameter, diameter, diameter));
                used[kind]++;
            }

            for (var kind = 0; kind < KindCount; kind++)
            {
                Flush(kind);
            }
        }

        public void Dispose()
        {
            if (materials == null)
            {
                return;
            }

            foreach (var material in materials)
            {
                if (material == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Object.Destroy(material);
                }
                else
                {
                    Object.DestroyImmediate(material);
                }
            }

            materials = null;
            batches = null;
        }

        private void Flush(int kind)
        {
            if (used[kind] <= 0 || materials[kind] == null)
            {
                return;
            }

            if (SystemInfo.supportsInstancing)
            {
                Graphics.DrawMeshInstanced(sphere, 0, materials[kind], batches[kind], used[kind]);
            }
            else
            {
                // まとめ描きに対応していない環境（古いWebGLなど）では1個ずつ描く。
                // 遅いが、何も映らないよりはよい。
                for (var i = 0; i < used[kind]; i++)
                {
                    Graphics.DrawMesh(sphere, batches[kind][i], materials[kind], 0);
                }
            }

            used[kind] = 0;
        }

        private static Material CreateMaterial(Color color, float glow)
        {
            // Shader.Find で組み立てない。実行時にしか組まない組み合わせは
            // ビルドで削られ、WebGLでは正しく描かれない（StandardMaterials の説明にある）。
            var material = StandardMaterials.CreateOpaque(glow > 0f);
            if (material == null)
            {
                return null;
            }

            material.color = color;
            material.SetFloat("_Glossiness", 0.15f);
            material.SetFloat("_Metallic", 0f);

            if (glow > 0f)
            {
                // 熱を持っている段階を、光っているように見せる。温度の計算ではない。
                material.SetColor("_EmissionColor", color * glow);
            }

            // まとめて描くために必要。切ると1個ずつの描画になり、一気に重くなる。
            material.enableInstancing = true;
            return material;
        }

        /// <summary>
        /// 組み込みの球メッシュを借りる。
        /// プリミティブを1つ作って、メッシュだけ受け取り、入れ物は捨てる。
        /// </summary>
        private static Mesh BorrowSphereMesh()
        {
            var temporary = GameObject.CreatePrimitive(PrimitiveType.Sphere);
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
