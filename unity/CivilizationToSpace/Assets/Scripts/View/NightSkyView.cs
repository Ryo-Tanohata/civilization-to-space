using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 宇宙から見るときの星空。**星も星座も実在の位置に置く。**
    ///
    /// これまで宇宙の側は単色の背景で、星が1つも無かった。
    /// 地表の夜空にだけ星があり、宇宙へ出ると何も無いのは、
    /// 同じ空を見ている感じにならない。
    ///
    /// **位置は作り物にしない。** 適当に撒くと、オリオン座もカシオペヤ座も
    /// どこにも無い空になる。J2000分点の実視等級 4.5 等までの星を、
    /// 赤経・赤緯のとおりに置いている（<see cref="StarCatalog"/>）。
    ///
    /// **星ごとに小さな板を置き、1つのメッシュにまとめる。**
    /// 絵に焼くと、寄ったときに粒がにじんで四角くなる。板なら寄っても丸い。
    /// 1つにまとめれば描画は1回で済む。
    ///
    /// **実際の見え方は表していない。** 星の大きさは等級から決めた見た目の値で、
    /// 実際の視直径ではない（どの星も点にしか見えない）。星座の線は約束ごとである。
    /// 地球の自転や公転に合わせて空を回してもいない。
    /// </summary>
    public sealed class NightSkyView : MonoBehaviour
    {
        /// <summary>
        /// 星空の球の半径。
        ///
        /// 地球（半径2.2）と月の距離よりじゅうぶん外側に置き、
        /// カメラの遠い側の切り取り面より内側に収める。
        /// </summary>
        public const float SkyRadius = 520f;

        /// <summary>
        /// いちばん明るい星の板の大きさ（球の半径に対する比）。
        ///
        /// **実際の視直径ではない。** どの星も点にしか見えないので、
        /// 見て分かる大きさに広げている。
        /// </summary>
        private const float BrightestSize = 0.016f;

        /// <summary>いちばん暗い星の板の大きさ。</summary>
        private const float FaintestSize = 0.0048f;

        /// <summary>星座の線の太さ（球の半径に対する比）。</summary>
        private const float LineWidth = 0.0013f;

        /// <summary>
        /// 星座の線の濃さ。
        ///
        /// **星より弱くする。** 線のほうが目立つと、星座早見盤のようになり、
        /// 星空を見ている感じにならない。
        /// </summary>
        private const float LineBrightness = 0.055f;

        /// <summary>
        /// 星の明るさの倍率。
        ///
        /// 地表の夜空より強く出す。宇宙の側は地球が画面の中心にあり、
        /// 星は端のほうにしか出ないため、同じ明るさでは気づかれない。
        /// </summary>
        private const float Brightness = 2.4f;

        private HideFlags createdFlags;
        private Material starMaterial;
        private Material lineMaterial;
        private Texture2D dotTexture;
        private Mesh starMesh;
        private Mesh lineMesh;

        /// <summary>
        /// 動きを減らす設定。真のとき星を光らせない。
        /// **瞬きは動きである。** 減らすと決めたなら止める。
        /// </summary>
        public bool ReducedMotion { get; set; }

        /// <summary>光っていないときの明るさ。</summary>
        private const float TwinkleBase = 0.62f;

        /// <summary>光ったときに足す明るさ。加算なので1を超えてよい。</summary>
        private const float TwinkleDepth = 2.4f;

        /// <summary>光のとがり。大きいほど短く強く光る。</summary>
        private const float TwinkleSharp = 3.2f;

        public void Build(HideFlags flags)
        {
            Clear();
            createdFlags = flags;

            dotTexture = BuildDot();
            BuildStars();
            BuildLines();
            ApplyTwinkle();
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            DestroyImmediate(starMaterial);
            DestroyImmediate(lineMaterial);
            DestroyImmediate(dotTexture);
            DestroyImmediate(starMesh);
            DestroyImmediate(lineMesh);
            starMaterial = null;
            lineMaterial = null;
            dotTexture = null;
            starMesh = null;
            lineMesh = null;
        }

        private void OnDestroy()
        {
            Clear();
        }

        /// <summary>星の光り方を材質へ渡す。</summary>
        public void ApplyTwinkle()
        {
            if (starMaterial == null)
            {
                return;
            }

            starMaterial.SetFloat("_TwinkleBase", ReducedMotion ? 1f : TwinkleBase);
            starMaterial.SetFloat("_TwinkleDepth", ReducedMotion ? 0f : TwinkleDepth);
        }

        /// <summary>
        /// 星1つぶんの丸い絵。中心が濃く、外へ向かって消える。
        /// 四角い板をそのまま出すと、星が四角く見える。
        /// </summary>
        private Texture2D BuildDot()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.hideFlags = createdFlags;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - center) / center;
                    var dy = (y - center) / center;
                    var d = Mathf.Sqrt(dx * dx + dy * dy);

                    // 中心を強く、外へ落とす。
                    // **落とし方を急にしすぎない。** 3乗にしていたときは、
                    // 板が数画素しかないところへ縮むと中心だけが残り、
                    // 拾い上げの平均で消えて、ほとんどの星が見えなかった。
                    var v = Mathf.Clamp01(1f - d);
                    v = Mathf.Pow(v, 1.5f);
                    var level = (byte)Mathf.Clamp(v * 255f, 0f, 255f);
                    pixels[y * size + x] = new Color32(level, level, level, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private void BuildStars()
        {
            var data = StarCatalog.Stars;
            var count = data.Length / StarCatalog.StarStride;

            var vertices = new Vector3[count * 4];
            var uvs = new Vector2[count * 4];
            var colors = new Color32[count * 4];
            var triangles = new int[count * 6];

            // 位相は星ごとに1つ。同じ星の4つの角には同じ値を入れる。
            var random = new System.Random(20260915);

            for (var i = 0; i < count; i++)
            {
                var at = i * StarCatalog.StarStride;
                var ra = data[at];
                var dec = data[at + 1];
                var magnitude = data[at + 2];
                var bv = data[at + 3];

                var direction = StarCatalog.Direction(ra, dec);
                var center = direction * SkyRadius;

                // 板を空の内側へ向ける。中心（カメラのいる側）を向かせる。
                var right = Vector3.Normalize(Vector3.Cross(direction, Vector3.up));
                if (right.sqrMagnitude < 0.5f)
                {
                    right = Vector3.right;
                }

                var up = Vector3.Cross(right, direction);

                // 等級は小さいほど明るい。-1.5等から4.5等までを大きさへ移す。
                var t = Mathf.InverseLerp(StarCatalog.MagnitudeLimit, -1.5f, magnitude);
                var size = SkyRadius * Mathf.Lerp(FaintestSize, BrightestSize, t * t);

                // 明るさも等級から。暗い星まで同じ明るさにすると、
                // 空が均一な粒の海になり、星座が読めない。
                var level = Mathf.Lerp(0.55f, 1f, t) * Brightness;
                var tint = StarCatalog.ColorFor(bv) * level;
                var phase = (byte)random.Next(256);
                var color = new Color32(
                    (byte)Mathf.Clamp(tint.r * 255f, 0f, 255f),
                    (byte)Mathf.Clamp(tint.g * 255f, 0f, 255f),
                    (byte)Mathf.Clamp(tint.b * 255f, 0f, 255f),
                    phase);

                var v = i * 4;
                vertices[v + 0] = center - right * size - up * size;
                vertices[v + 1] = center + right * size - up * size;
                vertices[v + 2] = center + right * size + up * size;
                vertices[v + 3] = center - right * size + up * size;

                uvs[v + 0] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(1f, 1f);
                uvs[v + 3] = new Vector2(0f, 1f);

                colors[v + 0] = color;
                colors[v + 1] = color;
                colors[v + 2] = color;
                colors[v + 3] = color;

                var tri = i * 6;
                triangles[tri + 0] = v + 0;
                triangles[tri + 1] = v + 2;
                triangles[tri + 2] = v + 1;
                triangles[tri + 3] = v + 0;
                triangles[tri + 4] = v + 3;
                triangles[tri + 5] = v + 2;
            }

            starMesh = new Mesh();
            starMesh.hideFlags = createdFlags;
            starMesh.name = "Stars";

            // 星は千個ちかくあり、角の数が16ビットの上限を超えうる。
            starMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            starMesh.vertices = vertices;
            starMesh.uv = uvs;
            starMesh.colors32 = colors;
            starMesh.triangles = triangles;

            // 空は常に視界にある。切り捨ての判定で消えないよう、広めに取る。
            starMesh.bounds = new Bounds(Vector3.zero, Vector3.one * (SkyRadius * 3f));

            starMaterial = StandardMaterials.CreateStarField();
            starMaterial.hideFlags = createdFlags;
            starMaterial.color = Color.white;
            starMaterial.SetTexture("_EmissionMap", dotTexture);
            starMaterial.SetColor("_EmissionColor", Color.white);
            starMaterial.SetFloat("_TwinkleSpeed", 2.2f);
            starMaterial.SetFloat("_TwinkleSharp", TwinkleSharp);

            // **裏表を切り捨てない。**
            // 板は空の面に沿って置くので、どちら向きに組んだかで消えてしまう。
            // 加算の平らな板は、どちらから見ても同じに見えるので切り捨てなくてよい。
            starMaterial.SetFloat("_Cull", 0f);

            // 位相と色は頂点から取る。1枚の絵を使い回すため。
            starMaterial.SetFloat("_VertexPhase", 1f);

            Place("Stars", starMesh, starMaterial);
        }

        /// <summary>
        /// 星座の線。大円に沿って何段かに分け、球の面へ貼りつける。
        /// まっすぐ結ぶと球を突き抜けて、星から離れた位置に見える。
        /// </summary>
        private void BuildLines()
        {
            var data = StarCatalog.ConstellationLines;
            var count = data.Length / StarCatalog.LineStride;
            const int steps = 6;

            var vertices = new Vector3[count * steps * 4];
            var uvs = new Vector2[count * steps * 4];
            var colors = new Color32[count * steps * 4];
            var triangles = new int[count * steps * 6];

            var tint = new Color32(
                (byte)(LineBrightness * 255f),
                (byte)(LineBrightness * 255f * 1.06f),
                (byte)(LineBrightness * 255f * 1.25f),
                255);

            var piece = 0;
            for (var i = 0; i < count; i++)
            {
                var at = i * StarCatalog.LineStride;
                var from = StarCatalog.Direction(data[at], data[at + 1]);
                var to = StarCatalog.Direction(data[at + 2], data[at + 3]);

                for (var step = 0; step < steps; step++)
                {
                    var a = Vector3.Slerp(from, to, step / (float)steps) * SkyRadius;
                    var b = Vector3.Slerp(from, to, (step + 1) / (float)steps) * SkyRadius;

                    var along = (b - a).normalized;
                    var outward = ((a + b) * 0.5f).normalized;
                    var side = Vector3.Cross(along, outward) * (SkyRadius * LineWidth);

                    var v = piece * 4;
                    vertices[v + 0] = a - side;
                    vertices[v + 1] = a + side;
                    vertices[v + 2] = b + side;
                    vertices[v + 3] = b - side;

                    for (var k = 0; k < 4; k++)
                    {
                        uvs[v + k] = new Vector2(0.5f, 0.5f);
                        colors[v + k] = tint;
                    }

                    var tri = piece * 6;
                    triangles[tri + 0] = v + 0;
                    triangles[tri + 1] = v + 2;
                    triangles[tri + 2] = v + 1;
                    triangles[tri + 3] = v + 0;
                    triangles[tri + 4] = v + 3;
                    triangles[tri + 5] = v + 2;
                    piece++;
                }
            }

            lineMesh = new Mesh();
            lineMesh.hideFlags = createdFlags;
            lineMesh.name = "ConstellationLines";
            lineMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            lineMesh.vertices = vertices;
            lineMesh.uv = uvs;
            lineMesh.colors32 = colors;
            lineMesh.triangles = triangles;
            lineMesh.bounds = new Bounds(Vector3.zero, Vector3.one * (SkyRadius * 3f));

            // 線は揺れない。星と一緒に明滅すると、何の線か分からなくなる。
            lineMaterial = StandardMaterials.CreateStarField();
            lineMaterial.hideFlags = createdFlags;
            lineMaterial.color = Color.white;
            lineMaterial.SetTexture("_EmissionMap", Texture2D.whiteTexture);
            lineMaterial.SetColor("_EmissionColor", Color.white);
            lineMaterial.SetFloat("_VertexPhase", 1f);
            lineMaterial.SetFloat("_TwinkleBase", 1f);
            lineMaterial.SetFloat("_TwinkleDepth", 0f);
            lineMaterial.SetFloat("_Cull", 0f);

            Place("ConstellationLines", lineMesh, lineMaterial);
        }

        private void Place(string name, Mesh mesh, Material material)
        {
            var host = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            host.hideFlags = createdFlags;
            host.transform.SetParent(transform, false);
            host.GetComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = host.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
