using System;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 地表から見た風景。空と地面と、影になって立つものだけで作る。
    ///
    /// **なぜ影（シルエット）で描くのか。**
    /// この作品は外部の図版も写真も使わず、絵をすべて計算で描いている。
    /// 生きものを色と陰影まで作り込むと、必ず「これは本当にその姿か」という問いが立つ。
    /// 化石から分かるのは骨格までで、皮膚の色も質感も分かっていない。
    /// 明るい空に黒い形だけを置けば、**大きさと形の傾向しか主張しない。**
    /// 言えることだけを言う形になり、この作品がこれまで守ってきた作法とも合う。
    ///
    /// **特定の種を表していない。** 首の長さ・胴の長さ・脚の高さの比を変えているだけで、
    /// 学名のある生きものを描き分けてはいない。数も分布も表していない。
    ///
    /// 空の色と植物の量は外から受け取る。場面ごとの専用コードを書かない。
    /// 時代が増えるたびに書き足すと、必ず破綻するためである。
    /// </summary>
    public sealed class SurfaceView : MonoBehaviour
    {
        /// <summary>地表の風景を決める値。時代データから作る。</summary>
        public struct Landscape
        {
            /// <summary>空の上側の色。</summary>
            public Color SkyHigh;

            /// <summary>空の地平線側の色。</summary>
            public Color SkyLow;

            /// <summary>植物の背の高さ（地面からの目安）。</summary>
            public float PlantHeight;

            /// <summary>針葉樹の本数。</summary>
            public int Conifers;

            /// <summary>シダの本数。低いところを埋める。</summary>
            public int Ferns;

            /// <summary>広葉樹の本数。</summary>
            public int Broadleaves;

            /// <summary>四つ足の生きものの数。</summary>
            public int Quadrupeds;

            /// <summary>二本足の生きものの数。</summary>
            public int Bipeds;

            /// <summary>置き方を決める種。同じ種なら同じ並びになる。</summary>
            public int Seed;
        }

        /// <summary>空の板までの距離。ここより手前にすべてを置く。</summary>
        private const float SkyDistance = 150f;

        /// <summary>ものを置く範囲。</summary>
        private const float NearZ = 16f;
        private const float FarZ = 165f;
        private const float HalfWidth = 65f;

        /// <summary>遠さで色を変える段の数。段を増やすほど滑らかになる。</summary>
        private const int DepthSteps = 8;

        private HideFlags createdFlags;
        private Material[] silhouettes;
        private Material skyMaterial;
        private Material groundMaterial;
        private Texture2D skyTexture;
        private Texture2D groundTexture;
        private Color hazeColor;
        private System.Random random;

        /// <summary>円錐の網。針葉樹に使う。Unityの基本形には円錐が無いので自分で作る。</summary>
        private static Mesh coneMesh;

        /// <summary>
        /// 円錐の網を作る。底面の半径0.5、高さ1、先が上。
        ///
        /// **球を積むと木に見えない。** 丸いものを重ねると雪だるまになる。
        /// 針葉樹の影が針葉樹に見えるのは、上へ細くなる三角の輪郭のためである。
        /// </summary>
        private static Mesh Cone()
        {
            if (coneMesh != null)
            {
                return coneMesh;
            }

            const int sides = 14;
            var vertices = new Vector3[sides + 2];
            var triangles = new int[sides * 6];

            vertices[0] = new Vector3(0f, 1f, 0f);
            vertices[sides + 1] = Vector3.zero;

            for (var i = 0; i < sides; i++)
            {
                var angle = i / (float)sides * Mathf.PI * 2f;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
            }

            for (var i = 0; i < sides; i++)
            {
                var a = i + 1;
                var b = (i + 1) % sides + 1;

                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = b;
                triangles[i * 3 + 2] = a;

                triangles[sides * 3 + i * 3] = sides + 1;
                triangles[sides * 3 + i * 3 + 1] = a;
                triangles[sides * 3 + i * 3 + 2] = b;
            }

            coneMesh = new Mesh();
            coneMesh.name = "SurfaceCone";
            coneMesh.vertices = vertices;
            coneMesh.triangles = triangles;
            coneMesh.RecalculateNormals();
            coneMesh.hideFlags = HideFlags.DontSave;
            return coneMesh;
        }

        /// <summary>この風景を見るカメラの置き場所。目の高さに合わせてある。</summary>
        public static Vector3 EyePosition
        {
            get { return new Vector3(0f, 1.7f, -9f); }
        }

        public void Build(Landscape land, HideFlags flags)
        {
            Clear();

            createdFlags = flags;
            hazeColor = land.SkyLow;
            random = new System.Random(land.Seed);

            BuildSky(land);
            BuildGround();

            Scatter(land.Conifers, BuildConifer, land.PlantHeight, 1.0f);
            Scatter(land.Ferns, BuildFern, land.PlantHeight * 0.32f, 0.9f);
            Scatter(land.Broadleaves, BuildBroadleaf, land.PlantHeight * 0.75f, 1.0f);
            Scatter(land.Quadrupeds, BuildQuadruped, land.PlantHeight * 0.55f, 1.0f, true);
            Scatter(land.Bipeds, BuildBiped, land.PlantHeight * 0.30f, 1.0f, true);
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            if (silhouettes != null)
            {
                foreach (var material in silhouettes)
                {
                    DestroyImmediate(material);
                }
            }

            DestroyImmediate(skyMaterial);
            DestroyImmediate(groundMaterial);
            DestroyImmediate(skyTexture);
            DestroyImmediate(groundTexture);
            silhouettes = null;
            skyMaterial = null;
            groundMaterial = null;
            skyTexture = null;
            groundTexture = null;
        }

        /// <summary>
        /// 空。縦の色の変化だけを焼いた絵を、遠くの板に貼る。
        ///
        /// 光の当たり方に左右されないよう、地色を黒にして発光だけで出す。
        /// 空は光を受ける面ではなく、光そのものだからである。
        /// </summary>
        private void BuildSky(Landscape land)
        {
            const int height = 128;
            skyTexture = new Texture2D(1, height, TextureFormat.RGBA32, false);
            skyTexture.hideFlags = createdFlags;
            skyTexture.wrapMode = TextureWrapMode.Clamp;

            for (var y = 0; y < height; y++)
            {
                // 地平線の近くだけ急に明るくする。空はどの時代でもそう見える。
                var t = y / (float)(height - 1);
                var color = Color.Lerp(land.SkyLow, land.SkyHigh, Mathf.Pow(t, 0.55f));
                skyTexture.SetPixel(0, y, color);
            }

            skyTexture.Apply();

            skyMaterial = StandardMaterials.CreateOpaque(true);
            skyMaterial.hideFlags = createdFlags;
            skyMaterial.color = Color.black;
            skyMaterial.SetTexture("_EmissionMap", skyTexture);
            skyMaterial.SetColor("_EmissionColor", Color.white);
            skyMaterial.SetFloat("_Blend", 0f);

            var sky = PrimitiveMeshes.Create(PrimitiveType.Quad, "Sky", createdFlags);
            sky.transform.SetParent(transform, false);
            sky.transform.localPosition = new Vector3(0f, 58f, SkyDistance);
            sky.transform.localScale = new Vector3(520f, 260f, 1f);
            sky.GetComponent<Renderer>().sharedMaterial = skyMaterial;
        }

        /// <summary>地面。手前は黒く置く。起伏は付けない。地平線を水平に保つためである。</summary>
        private void BuildGround()
        {
            var ground = PrimitiveMeshes.Create(PrimitiveType.Cube, "Ground", createdFlags);
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = new Vector3(0f, -2f, 80f);
            ground.transform.localScale = new Vector3(900f, 4f, 340f);
            ground.GetComponent<Renderer>().sharedMaterial = Silhouette(0);
        }

        /// <summary>
        /// 影に使う材質。**遠いものほど空の色へ溶かす。**
        ///
        /// すべてを同じ黒で塗ると、手前の木も奥の木も同じ色になり、
        /// 重なったところが一つの塊にしか見えない。遠さで色を変えると、
        /// 前後の層が分かれて奥行きが出る。実際の風景で遠くの山が薄く見えるのと同じである。
        ///
        /// 地色を黒、発光を色にしてある。光の当たり方に左右されず、指定した色がそのまま出る。
        /// </summary>
        private Material Silhouette(int step)
        {
            if (silhouettes == null)
            {
                silhouettes = new Material[DepthSteps];
            }

            var index = Mathf.Clamp(step, 0, DepthSteps - 1);
            if (silhouettes[index] == null)
            {
                var t = DepthSteps > 1 ? index / (float)(DepthSteps - 1) : 0f;

                // 完全に空の色まで溶かさない。溶かし切ると輪郭が消える。
                var tint = Color.Lerp(Color.black, hazeColor, t * 0.72f);

                var material = StandardMaterials.CreateOpaque(true);
                material.hideFlags = createdFlags;
                material.color = Color.black;
                material.SetColor("_EmissionColor", tint);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_Glossiness", 0f);
                material.SetFloat("_Metallic", 0f);
                silhouettes[index] = material;
            }

            return silhouettes[index];
        }

        /// <summary>置いた場所の遠さから、影の濃さを決めて貼る。</summary>
        private void ApplyDepth(GameObject item, float z)
        {
            var t = Mathf.InverseLerp(NearZ, FarZ, z);
            var material = Silhouette(Mathf.RoundToInt(t * (DepthSteps - 1)));
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterial = material;
            }
        }

        private void Scatter(int count, Func<float, GameObject> build, float height, float spread)
        {
            Scatter(count, build, height, spread, false);
        }

        /// <summary>
        /// 置く。<paramref name="profile"/> が真なら横向きに寄せる。
        ///
        /// **生きものの影は横から見ないと読めない。** 正面を向くと首も尾も脚も重なり、
        /// ただの塊になる。向きを散らすのは植物だけにする。
        /// </summary>
        private void Scatter(int count, Func<float, GameObject> build, float height, float spread, bool profile)
        {
            for (var i = 0; i < count; i++)
            {
                var z = Mathf.Lerp(NearZ, FarZ, (float)random.NextDouble());
                var x = (float)(random.NextDouble() * 2.0 - 1.0) * HalfWidth * spread;

                // 背の高さは1本ごとに散らす。揃えると並木に見える。
                var scale = height * (0.55f + (float)random.NextDouble() * 1.25f);

                var item = build(scale);
                item.transform.SetParent(transform, false);
                item.transform.localPosition = new Vector3(x, 0f, z);
                var turn = profile
                    ? (random.Next(2) == 0 ? 90f : 270f) + (float)(random.NextDouble() * 2.0 - 1.0) * 30f
                    : (float)random.NextDouble() * 360f;
                item.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
                ApplyDepth(item, z);
            }
        }

        private GameObject Piece(GameObject parent, PrimitiveType type, Vector3 position, Vector3 scale)
        {
            var piece = PrimitiveMeshes.Create(type, "Part", createdFlags);
            piece.transform.SetParent(parent.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;

            // 材質は置いたあとに ApplyDepth が貼り直す。ここでは手前の色を入れておく。
            piece.GetComponent<Renderer>().sharedMaterial = Silhouette(0);
            return piece;
        }

        /// <summary>円錐の部品。</summary>
        private GameObject ConePiece(GameObject parent, Vector3 position, Vector3 scale)
        {
            var piece = new GameObject("Cone", typeof(MeshFilter), typeof(MeshRenderer));
            piece.hideFlags = createdFlags;
            piece.transform.SetParent(parent.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<MeshFilter>().sharedMesh = Cone();
            piece.GetComponent<MeshRenderer>().sharedMaterial = Silhouette(0);
            return piece;
        }

        private GameObject Root(string name)
        {
            var root = new GameObject(name);
            root.hideFlags = createdFlags;
            return root;
        }

        /// <summary>針葉樹。細い幹と、上へ細くなる円錐を重ねる。</summary>
        private GameObject BuildConifer(float height)
        {
            var root = Root("Conifer");
            Piece(root, PrimitiveType.Cylinder, new Vector3(0f, height * 0.16f, 0f),
                new Vector3(height * 0.045f, height * 0.16f, height * 0.045f));

            // 下の段を広く、上の段を細くして重ねる。1つだけだと三角すぎる。
            ConePiece(root, new Vector3(0f, height * 0.20f, 0f),
                new Vector3(height * 0.52f, height * 0.50f, height * 0.52f));
            ConePiece(root, new Vector3(0f, height * 0.46f, 0f),
                new Vector3(height * 0.38f, height * 0.56f, height * 0.38f));
            return root;
        }

        /// <summary>
        /// シダ。短い幹と、上へ跳ねる葉の束。低いところを埋める。
        ///
        /// 葉を平たい球ひとつにすると円盤にしか見えない。
        /// 細長い葉を放射状に立てると、束ねた草に見える。
        /// </summary>
        private GameObject BuildFern(float height)
        {
            var root = Root("Fern");
            Piece(root, PrimitiveType.Cylinder, new Vector3(0f, height * 0.22f, 0f),
                new Vector3(height * 0.06f, height * 0.22f, height * 0.06f));

            const int fronds = 6;
            for (var i = 0; i < fronds; i++)
            {
                var angle = i / (float)fronds * 360f + (float)random.NextDouble() * 24f;
                var lean = 34f + (float)random.NextDouble() * 22f;
                var frond = Piece(root, PrimitiveType.Sphere,
                    Vector3.zero,
                    new Vector3(height * 0.13f, height * 0.62f, height * 0.13f));
                frond.transform.localRotation = Quaternion.Euler(lean, angle, 0f);
                frond.transform.localPosition =
                    frond.transform.localRotation * new Vector3(0f, height * 0.62f, 0f)
                    + new Vector3(0f, height * 0.4f, 0f);
            }

            return root;
        }

        /// <summary>
        /// 広葉樹。幹と、いびつな冠。
        ///
        /// 冠を球ひとつにすると円盤に見える。大きさの違う塊を寄せて輪郭を崩す。
        /// </summary>
        private GameObject BuildBroadleaf(float height)
        {
            var root = Root("Broadleaf");
            Piece(root, PrimitiveType.Cylinder, new Vector3(0f, height * 0.32f, 0f),
                new Vector3(height * 0.06f, height * 0.32f, height * 0.06f));

            for (var i = 0; i < 5; i++)
            {
                var r = height * (0.16f + (float)random.NextDouble() * 0.17f);
                var offset = new Vector3(
                    (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.26f,
                    height * (0.58f + (float)random.NextDouble() * 0.44f),
                    (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.26f);
                Piece(root, PrimitiveType.Sphere, offset, new Vector3(r, r * 1.05f, r));
            }

            return root;
        }

        /// <summary>
        /// 四つ足の生きもの。首と尾を長く取る。
        /// **特定の種ではない。** 長い首と長い尾という形の傾向だけを置いている。
        /// </summary>
        private GameObject BuildQuadruped(float height)
        {
            var root = Root("Quadruped");
            var body = height * 0.9f;
            var legs = height * 0.55f;

            Piece(root, PrimitiveType.Sphere, new Vector3(0f, legs + body * 0.28f, 0f),
                new Vector3(body * 0.42f, body * 0.46f, body));

            // 首は球を隙間なく重ねて弧にする。離すと数珠に見える。
            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                // まっすぐ立てると帆柱に見える。前へ倒しながら上げてS字にする。
                var y = legs + body * (0.36f + t * 0.62f);
                var z = body * (0.42f + t * 0.78f + Mathf.Sin(t * 3.1f) * 0.1f);
                var r = Mathf.Lerp(body * 0.21f, body * 0.09f, t);
                Piece(root, PrimitiveType.Sphere, new Vector3(0f, y, z), new Vector3(r, r, r));
            }

            Piece(root, PrimitiveType.Sphere, new Vector3(0f, legs + body * 0.99f, body * 1.28f),
                new Vector3(body * 0.12f, body * 0.11f, body * 0.22f));

            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                var r = Mathf.Lerp(body * 0.17f, body * 0.03f, t);
                Piece(root, PrimitiveType.Sphere,
                    new Vector3(0f, legs + body * (0.3f - t * 0.16f), -body * (0.45f + t * 0.72f)),
                    new Vector3(r, r, r));
            }

            for (var i = 0; i < 4; i++)
            {
                var front = i < 2;
                var side = (i % 2 == 0) ? 1f : -1f;
                Piece(root, PrimitiveType.Cylinder,
                    new Vector3(side * body * 0.22f, legs * 0.5f, front ? body * 0.3f : -body * 0.3f),
                    new Vector3(body * 0.1f, legs * 0.5f, body * 0.1f));
            }

            return root;
        }

        /// <summary>二本足の生きもの。胴を立て、尾で釣り合わせる。</summary>
        private GameObject BuildBiped(float height)
        {
            var root = Root("Biped");
            var legs = height * 0.45f;
            var body = height * 0.5f;

            Piece(root, PrimitiveType.Sphere, new Vector3(0f, legs + body * 0.4f, 0f),
                new Vector3(body * 0.5f, body * 0.6f, body * 0.85f));
            Piece(root, PrimitiveType.Sphere, new Vector3(0f, legs + body * 0.9f, body * 0.4f),
                new Vector3(body * 0.26f, body * 0.24f, body * 0.34f));

            for (var i = 0; i < 8; i++)
            {
                var t = i / 7f;
                var r = Mathf.Lerp(body * 0.2f, body * 0.04f, t);
                Piece(root, PrimitiveType.Sphere,
                    new Vector3(0f, legs + body * (0.36f - t * 0.1f), -body * (0.4f + t * 0.5f)),
                    new Vector3(r, r, r));
            }

            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? 1f : -1f;
                Piece(root, PrimitiveType.Cylinder, new Vector3(side * body * 0.18f, legs * 0.5f, 0f),
                    new Vector3(body * 0.13f, legs * 0.5f, body * 0.13f));
            }

            return root;
        }
    }
}
