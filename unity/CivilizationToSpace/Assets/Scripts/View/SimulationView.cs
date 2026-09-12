using CivilizationToSpace.Sim;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 計算した天体を描く。天体ごとに GameObject を作らない。
    ///
    /// 数百個をひとつずつ GameObject にすると、合体のたびに生成と破棄が起き、
    /// スマートフォンでは描画より先にそちらで止まる。ひとつの球を使い回し、
    /// 種別ごとにまとめて描く（<see cref="Graphics.RenderMeshInstanced"/>）。
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
        private static readonly Color ColonyColor = new Color(0.75f, 0.89f, 0.96f, 1f);

        private const int KindCount = 6;

        /// <summary>
        /// コロニーを描く大きさ。地球の半径に対する割合。
        ///
        /// **実物の割合ではない。** オニール型の円筒は長さ数十kmで、月（直径3474km）の
        /// 1%ほどしかない。そのまま描くと1画素も出ないため、見えるまで大きくしている。
        /// </summary>
        private const float ColonyDrawScale = 0.42f;

        /// <summary>
        /// 描画してよい範囲。
        ///
        /// <see cref="Graphics.RenderMeshInstanced"/> は範囲を自分で渡す必要がある。
        /// 既定のままだと原点付近の小さな箱とみなされ、外にある天体が消えてしまう。
        /// 系の広がり（最大でも半径40ほど）を十分に包む大きさにしてある。
        /// </summary>
        private static readonly Bounds WorldBounds = new Bounds(Vector3.zero, Vector3.one * 4000f);

        private Mesh sphere;
        private Mesh cylinder;

        /// <summary>コロニーの軸の向き。円盤の面に垂直にする。</summary>
        private Vector3 colonyAxis = Vector3.up;
        private Material[] materials;
        private Matrix4x4[][] batches;
        private int[] used;

        /// <summary>描いた天体の数。直前のフレームの値。</summary>
        public int DrawnCount { get; private set; }

        public void Build()
        {
            sphere = BorrowMesh(PrimitiveType.Sphere);
            cylinder = BorrowMesh(PrimitiveType.Cylinder);

            materials = new Material[KindCount];
            materials[(int)BodyKind.Planetesimal] = CreateMaterial(PlanetesimalColor, 0f);
            materials[(int)BodyKind.Earth] = CreateMaterial(EarthColor, 0.55f);
            materials[(int)BodyKind.Impactor] = CreateMaterial(ImpactorColor, 0.25f);
            materials[(int)BodyKind.Debris] = CreateMaterial(DebrisColor, 0.7f);
            materials[(int)BodyKind.Moon] = CreateMaterial(MoonColor, 0f);
            materials[(int)BodyKind.Colony] = CreateMaterial(ColonyColor, 0.85f);

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

            UpdateColonyAxis(simulation);

            var earth = simulation.EarthIndex;
            var earthRadius = earth >= 0 && earth < count ? bodies[earth].Radius : 1f;
            var colonyRotation = Quaternion.FromToRotation(Vector3.up, colonyAxis);

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

                if (bodies[i].Kind == BodyKind.Colony)
                {
                    // 円筒の元の高さは2、直径は1。見える大きさまで伸ばす。
                    var length = earthRadius * ColonyDrawScale;
                    var width = length * 0.34f;
                    batches[kind][used[kind]] = Matrix4x4.TRS(
                        bodies[i].Position, colonyRotation, new Vector3(width, length * 0.5f, width));
                }
                else
                {
                    // 球の元の直径は1。半径ぶんの2倍に伸ばす。
                    var diameter = bodies[i].Radius * 2f;
                    batches[kind][used[kind]] = Matrix4x4.TRS(
                        bodies[i].Position, Quaternion.identity, new Vector3(diameter, diameter, diameter));
                }

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

            // コロニーだけは球ではない。円筒で描く。
            var mesh = kind == (int)BodyKind.Colony ? cylinder : sphere;
            if (mesh == null)
            {
                used[kind] = 0;
                return;
            }

            // Unity 6 で Graphics.DrawMeshInstanced は非推奨になった。
            // 置き換え先の RenderMeshInstanced は描画の条件を RenderParams で渡す。
            var parameters = new RenderParams(materials[kind])
            {
                worldBounds = WorldBounds,
                receiveShadows = true,
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
            };

            if (SystemInfo.supportsInstancing)
            {
                Graphics.RenderMeshInstanced(parameters, mesh, 0, batches[kind], used[kind]);
            }
            else
            {
                // まとめ描きに対応していない環境（古いWebGLなど）では1個ずつ描く。
                // 遅いが、何も映らないよりはよい。
                for (var i = 0; i < used[kind]; i++)
                {
                    Graphics.RenderMesh(parameters, mesh, 0, batches[kind][i]);
                }
            }

            used[kind] = 0;
        }

        /// <summary>
        /// コロニーの軸を、円盤の面に垂直な向きに合わせる。
        /// 太陽を置いていないためこう決めている。実際のオニール型は軸を太陽へ向ける。
        /// </summary>
        private void UpdateColonyAxis(Sim.AccretionSimulation simulation)
        {
            var earth = simulation.EarthIndex;
            var moon = simulation.HeaviestOtherThanEarth();
            if (earth < 0 || moon < 0)
            {
                return;
            }

            var bodies = simulation.Bodies;
            var normal = Vector3.Cross(
                bodies[moon].Position - bodies[earth].Position,
                bodies[moon].Velocity - bodies[earth].Velocity);

            if (normal.sqrMagnitude > 1e-10f)
            {
                colonyAxis = normal.normalized;
            }
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
        /// 組み込みのメッシュを借りる。
        /// プリミティブを1つ作って、メッシュだけ受け取り、入れ物は捨てる。
        /// </summary>
        private static Mesh BorrowMesh(PrimitiveType type)
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
