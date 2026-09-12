using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 高さで膨らませた球。輪郭にも起伏が出る。
    ///
    /// Unityの標準の球は頂点が少なく、膨らませても形が出ない。
    /// 経度・緯度の格子で自前の球を作り、地表の高さで半径を動かす。
    ///
    /// 時代が変わるときは頂点の半径を補間する。全時代で頂点の並びが同じなので、
    /// 大陸がせり上がり、また沈む様子がそのまま動きになる。
    /// </summary>
    public sealed class PlanetMesh
    {
        /// <summary>経度方向の分割数。地表の絵の横幅に合わせすぎない程度に取る。</summary>
        private const int Columns = 192;

        /// <summary>緯度方向の分割数。</summary>
        private const int Rows = 96;

        /// <summary>
        /// 半径に対する起伏の大きさ。実際の地球の起伏は半径の0.1%ほどで、
        /// そのままでは見えない。読み取れるようにデフォルメして誇張する。
        /// </summary>
        private const float ReliefScale = 0.045f;

        private readonly Mesh mesh;
        private readonly Vector3[] directions;
        private readonly Vector3[] positions;
        private readonly float[] current;
        private readonly float[] from;
        private readonly float[] to;
        private readonly float radius;

        public PlanetMesh(float radius)
        {
            this.radius = radius;

            var vertexCount = (Columns + 1) * (Rows + 1);
            directions = new Vector3[vertexCount];
            positions = new Vector3[vertexCount];
            current = new float[vertexCount];
            from = new float[vertexCount];
            to = new float[vertexCount];

            var uv = new Vector2[vertexCount];
            var triangles = new int[Columns * Rows * 6];

            for (var row = 0; row <= Rows; row++)
            {
                var v = (float)row / Rows;
                var latitude = (v - 0.5f) * Mathf.PI;
                var cosLatitude = Mathf.Cos(latitude);
                var sinLatitude = Mathf.Sin(latitude);

                for (var column = 0; column <= Columns; column++)
                {
                    var u = (float)column / Columns;
                    var longitude = u * Mathf.PI * 2f;

                    var index = row * (Columns + 1) + column;
                    directions[index] = new Vector3(
                        cosLatitude * Mathf.Cos(longitude), sinLatitude, cosLatitude * Mathf.Sin(longitude));
                    positions[index] = directions[index] * radius;

                    // 経度0度で絵が繋がるよう、端の列を重ねて別のUVにする。
                    uv[index] = new Vector2(u, v);
                }
            }

            var t = 0;
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    var a = row * (Columns + 1) + column;
                    var b = a + 1;
                    var c = a + Columns + 1;
                    var d = c + 1;

                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;

                    triangles[t++] = b;
                    triangles[t++] = c;
                    triangles[t++] = d;
                }
            }

            mesh = new Mesh { name = "PlanetSurface" };
            mesh.indexFormat = vertexCount > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
        }

        public Mesh Mesh
        {
            get { return mesh; }
        }

        /// <summary>いまの高さを、次の時代の高さとして置き換える。補間せず即座に反映する。</summary>
        public void SetImmediate(float[] elevation)
        {
            Read(elevation, to);
            System.Array.Copy(to, from, to.Length);
            System.Array.Copy(to, current, to.Length);
            Push();
        }

        /// <summary>移り変わりを始める。いまの高さを起点にする。</summary>
        public void BeginTransition(float[] elevation)
        {
            System.Array.Copy(current, from, current.Length);
            Read(elevation, to);
        }

        /// <summary>移り変わりの途中。0で元の高さ、1で次の高さ。</summary>
        public void SetTransition(float t)
        {
            for (var i = 0; i < current.Length; i++)
            {
                current[i] = Mathf.Lerp(from[i], to[i], t);
            }

            Push();
        }

        /// <summary>地表の絵の高さを、頂点の並びへ拾い直す。</summary>
        private void Read(float[] elevation, float[] destination)
        {
            var width = PlanetSurfaceBaker.Width;
            var height = PlanetSurfaceBaker.Height;

            for (var row = 0; row <= Rows; row++)
            {
                var v = (float)row / Rows;
                var y = Mathf.Clamp(Mathf.RoundToInt(v * height - 0.5f), 0, height - 1);

                for (var column = 0; column <= Columns; column++)
                {
                    var u = (float)column / Columns;
                    var x = Mathf.Clamp(Mathf.RoundToInt(u * width - 0.5f), 0, width - 1) % width;
                    destination[row * (Columns + 1) + column] = elevation[y * width + x];
                }
            }
        }

        private void Push()
        {
            for (var i = 0; i < positions.Length; i++)
            {
                positions[i] = directions[i] * (radius * (1f + current[i] * ReliefScale));
            }

            mesh.vertices = positions;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}
