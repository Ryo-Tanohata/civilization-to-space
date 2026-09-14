using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 地表から見た風景。空・地面・立っているものを、色と陰影のある形で置く。
    ///
    /// **デフォルメした実物として描く。影（シルエット）では描かない。**
    /// 形は単純な塊の組み合わせで、細部は作り込まない。
    /// 化石から分かるのは骨格までで、皮膚の色も質感も分かっていない。
    /// ここで置く色は読みやすさのための決めであり、**復元色ではない。**
    /// 特定の種も表していない。首の長さ・胴の長さ・脚の高さの比を変えているだけである。
    ///
    /// **遠いものほど空の色へ溶かす。** 同じ色で塗ると、重なったところが
    /// 一つの塊にしか見えない。遠さで色を薄めると前後の層が分かれる。
    ///
    /// 置くものの種類と数と色は外から受け取る。場面ごとの専用コードを書かない。
    /// 時代が増えるたびに書き足すと、必ず破綻するためである。
    /// </summary>
    public sealed class SurfaceView : MonoBehaviour
    {
        /// <summary>地表の風景を決める値。時代データから作る。</summary>
        public struct Landscape
        {
            /// <summary>空の上側と地平線側の色。</summary>
            public Color SkyHigh;
            public Color SkyLow;

            /// <summary>地面の色。</summary>
            public Color Ground;

            /// <summary>幹・葉・生きもの・建物・窓の色。</summary>
            public Color Trunk;
            public Color Foliage;
            public Color Creature;
            public Color Building;
            public Color Window;

            /// <summary>ものを置く範囲。寄り方に合わせて変える。</summary>
            public float NearZ;
            public float FarZ;
            public float HalfWidth;

            /// <summary>植物の背の高さ（地面からの目安、メートル相当）。</summary>
            public float PlantHeight;

            /// <summary>建物の高さの目安。</summary>
            public float BuildingHeight;

            /// <summary>
            /// 建物どうしの間隔。建物の幅に対する倍率。
            ///
            /// 1.05なら背中合わせに詰まり、2.5なら道を挟んで並ぶ。
            /// 新石器時代の集落（チャタルホユック）は道が無く、家が隙間なく
            /// 詰まっていたことが分かっている。散らして置くと別のものになる。
            /// </summary>
            public float BuildingSpacing;

            /// <summary>
            /// 建物に窓の帯を入れるか。
            ///
            /// **新石器時代の集落には入れない。** チャタルホユックの家は
            /// 屋根から梯子で出入りしており、壁に並ぶ窓という作りではない。
            /// 高さだけで決めると、少し高い家に窓の帯が出てしまう。
            /// </summary>
            public bool Windows;

            public int Conifers;
            public int Ferns;
            public int Broadleaves;
            public int Quadrupeds;
            public int Bipeds;
            public int Buildings;

            /// <summary>置き方を決める種。同じ種なら同じ並びになる。</summary>
            public int Seed;
        }

        /// <summary>遠さで色を変える段の数。</summary>
        private const int DepthSteps = 8;

        /// <summary>遠くのものを空の色へどれだけ寄せるか。1で完全に溶ける。</summary>
        private const float HazeStrength = 0.34f;

        private HideFlags createdFlags;
        private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        private Material skyMaterial;
        private Texture2D skyTexture;
        private Landscape current;
        private System.Random random;

        private static Mesh coneMesh;

        /// <summary>生きものが画面に収まる寄り方。背の高さを見るための位置。</summary>
        public static void AimAtCreatures(Camera camera)
        {
            camera.transform.position = new Vector3(0f, 8f, -38f);
            camera.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
            camera.fieldOfView = 52f;
        }

        /// <summary>街が画面に収まる寄り方。建物の並びを見るための位置。</summary>
        public static void AimAtSettlement(Camera camera)
        {
            camera.transform.position = new Vector3(0f, 95f, -300f);
            camera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            camera.fieldOfView = 52f;
        }

        public void Build(Landscape land, HideFlags flags)
        {
            Clear();

            createdFlags = flags;
            current = land;
            random = new System.Random(land.Seed);

            BuildSky(land);
            BuildGround(land);

            Scatter(land.Conifers, BuildConifer, land.PlantHeight, false);
            Scatter(land.Ferns, BuildFern, land.PlantHeight * 0.30f, false);
            Scatter(land.Broadleaves, BuildBroadleaf, land.PlantHeight * 0.75f, false);
            PlaceBuildings(land);
            // 生きものは手前寄りに置く。奥へ撒くと小さすぎて形が読めない。
            ScatterNear(land.Quadrupeds, BuildQuadruped, land.PlantHeight * 0.85f, 0.34f);
            ScatterNear(land.Bipeds, BuildBiped, land.PlantHeight * 0.40f, 0.26f);
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            foreach (var material in materials.Values)
            {
                DestroyImmediate(material);
            }

            materials.Clear();
            DestroyImmediate(skyMaterial);
            DestroyImmediate(skyTexture);
            skyMaterial = null;
            skyTexture = null;
        }

        /// <summary>
        /// 空。縦の色の変化だけを焼いた絵を、遠くの板に貼る。
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
                var t = y / (float)(height - 1);
                skyTexture.SetPixel(0, y, Color.Lerp(land.SkyLow, land.SkyHigh, Mathf.Pow(t, 0.6f)));
            }

            skyTexture.Apply();

            skyMaterial = StandardMaterials.CreateOpaque(true);
            skyMaterial.hideFlags = createdFlags;
            skyMaterial.color = Color.black;
            skyMaterial.SetTexture("_EmissionMap", skyTexture);
            skyMaterial.SetColor("_EmissionColor", Color.white);
            skyMaterial.SetFloat("_Blend", 0f);

            var distance = land.FarZ * 1.35f;
            var sky = PrimitiveMeshes.Create(PrimitiveType.Quad, "Sky", createdFlags);
            sky.transform.SetParent(transform, false);
            sky.transform.localPosition = new Vector3(0f, distance * 0.36f, distance);
            sky.transform.localScale = new Vector3(distance * 4f, distance * 1.9f, 1f);
            sky.GetComponent<Renderer>().sharedMaterial = skyMaterial;
        }

        /// <summary>
        /// 地面。帯に分けて、遠い帯ほど空の色へ寄せる。
        /// 1枚の板だと手前も地平線も同じ色になり、奥行きが消えるためである。
        /// </summary>
        private void BuildGround(Landscape land)
        {
            const int bands = DepthSteps;
            var end = land.FarZ * 1.3f;

            for (var i = 0; i < bands; i++)
            {
                var from = Mathf.Lerp(-land.FarZ * 0.2f, end, i / (float)bands);
                var to = Mathf.Lerp(-land.FarZ * 0.2f, end, (i + 1) / (float)bands);

                var band = PrimitiveMeshes.Create(PrimitiveType.Cube, "GroundBand", createdFlags);
                band.transform.SetParent(transform, false);
                band.transform.localPosition = new Vector3(0f, -1f, (from + to) * 0.5f);
                band.transform.localScale = new Vector3(land.HalfWidth * 9f, 2f, to - from);
                band.GetComponent<Renderer>().sharedMaterial = Tinted("Ground", land.Ground, i);
            }
        }

        /// <summary>
        /// 色と遠さから材質を作る。作った材質は使い回す。
        /// 遠いものほど空の色へ寄せることで、前後の層が分かれて見える。
        /// </summary>
        private Material Tinted(string key, Color baseColor, int step)
        {
            var index = Mathf.Clamp(step, 0, DepthSteps - 1);
            var id = key + "/" + index;

            Material material;
            if (materials.TryGetValue(id, out material) && material != null)
            {
                return material;
            }

            var t = DepthSteps > 1 ? index / (float)(DepthSteps - 1) : 0f;
            material = StandardMaterials.CreateOpaque(false);
            material.hideFlags = createdFlags;
            material.color = Color.Lerp(baseColor, current.SkyLow, t * HazeStrength);
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            materials[id] = material;
            return material;
        }

        private int StepFor(float z)
        {
            var t = Mathf.InverseLerp(current.NearZ, current.FarZ, z);
            return Mathf.RoundToInt(t * (DepthSteps - 1));
        }

        /// <summary>置いたあとに、遠さに合わせて色を貼り直す。</summary>
        private void ApplyDepth(GameObject item, float z)
        {
            var step = StepFor(z);
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                var key = renderer.gameObject.name;
                renderer.sharedMaterial = Tinted(key, ColorFor(key), step);
            }
        }

        private Color ColorFor(string key)
        {
            switch (key)
            {
                case "Trunk": return current.Trunk;
                case "Foliage": return current.Foliage;
                case "Creature": return current.Creature;
                case "Building": return current.Building;
                case "Window": return current.Window;
                default: return current.Ground;
            }
        }

        /// <summary>
        /// 置く。<paramref name="profile"/> が真なら横向きに寄せる。
        /// 生きものと建物は、正面を向くと形が重なって読めなくなる。
        /// </summary>
        private void Scatter(int count, Func<float, GameObject> build, float height, bool profile)
        {
            for (var i = 0; i < count; i++)
            {
                // 手前を薄く、奥を濃くする。同じ密度で撒くと手前が詰まりすぎる。
                var depth = Mathf.Pow((float)random.NextDouble(), 0.62f);
                var z = Mathf.Lerp(current.NearZ, current.FarZ, depth);
                var x = (float)(random.NextDouble() * 2.0 - 1.0) * current.HalfWidth;

                var scale = height * (0.62f + (float)random.NextDouble() * 0.95f);

                var item = build(scale);
                item.transform.SetParent(transform, false);
                item.transform.localPosition = new Vector3(x, 0f, z);

                var turn = profile
                    ? (random.Next(2) == 0 ? 90f : 270f) + (float)(random.NextDouble() * 2.0 - 1.0) * 26f
                    : (float)random.NextDouble() * 360f;
                item.transform.localRotation = Quaternion.Euler(0f, turn, 0f);

                ApplyDepth(item, z);
            }
        }

        /// <summary>
        /// 建物を並べる。散らさず、升目の上へ置いてから少しずらす。
        ///
        /// **人の建てたものは散らばらない。** 寄り集まって建つ。
        /// 新石器時代のチャタルホユックは家が背中合わせに詰まり、道が無かった
        /// （出典は docs/reports にある地表の報告）。ばらばらに撒くと、
        /// 集落にも都市にも見えない。
        /// </summary>
        private void PlaceBuildings(Landscape land)
        {
            if (land.Buildings <= 0)
            {
                return;
            }

            var pitch = Mathf.Max(1f, land.BuildingHeight * Mathf.Max(0.4f, land.BuildingSpacing));
            var columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(land.Buildings * 1.6f)));
            var rows = Mathf.Max(1, Mathf.CeilToInt(land.Buildings / (float)columns));
            var originX = -(columns - 1) * pitch * 0.5f;
            var originZ = Mathf.Lerp(land.NearZ, land.FarZ, 0.34f);

            var placed = 0;
            for (var row = 0; row < rows && placed < land.Buildings; row++)
            {
                for (var column = 0; column < columns && placed < land.Buildings; column++)
                {
                    placed++;

                    var jitter = pitch * 0.16f;
                    var x = originX + column * pitch
                        + (float)(random.NextDouble() * 2.0 - 1.0) * jitter;
                    var z = originZ + row * pitch
                        + (float)(random.NextDouble() * 2.0 - 1.0) * jitter;

                    var height = land.BuildingHeight * (0.7f + (float)random.NextDouble() * 0.8f);
                    var item = BuildStructure(height);
                    item.transform.SetParent(transform, false);
                    item.transform.localPosition = new Vector3(x, 0f, z);
                    item.transform.localRotation = Quaternion.Euler(0f,
                        (float)(random.NextDouble() * 2.0 - 1.0) * 8f, 0f);
                    ApplyDepth(item, z);
                }
            }
        }

        /// <summary>手前寄りに置く。<paramref name="reach"/> は置く範囲の割合。</summary>
        private void ScatterNear(int count, Func<float, GameObject> build, float height, float reach)
        {
            var far = Mathf.Lerp(current.NearZ, current.FarZ, reach);
            for (var i = 0; i < count; i++)
            {
                var z = Mathf.Lerp(current.NearZ, far, (float)random.NextDouble());
                var x = (float)(random.NextDouble() * 2.0 - 1.0) * current.HalfWidth * 0.55f;
                var scale = height * (0.7f + (float)random.NextDouble() * 0.6f);

                var item = build(scale);
                item.transform.SetParent(transform, false);
                item.transform.localPosition = new Vector3(x, 0f, z);
                item.transform.localRotation = Quaternion.Euler(0f,
                    (random.Next(2) == 0 ? 90f : 270f) + (float)(random.NextDouble() * 2.0 - 1.0) * 24f, 0f);
                ApplyDepth(item, z);
            }
        }

        private GameObject Piece(GameObject parent, PrimitiveType type, string role, Vector3 position, Vector3 scale)
        {
            var piece = PrimitiveMeshes.Create(type, role, createdFlags);
            piece.transform.SetParent(parent.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            return piece;
        }

        private GameObject ConePiece(GameObject parent, string role, Vector3 position, Vector3 scale)
        {
            var piece = new GameObject(role, typeof(MeshFilter), typeof(MeshRenderer));
            piece.hideFlags = createdFlags;
            piece.transform.SetParent(parent.transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            piece.GetComponent<MeshFilter>().sharedMesh = Cone();
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
            Piece(root, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, height * 0.16f, 0f),
                new Vector3(height * 0.05f, height * 0.16f, height * 0.05f));
            ConePiece(root, "Foliage", new Vector3(0f, height * 0.20f, 0f),
                new Vector3(height * 0.52f, height * 0.50f, height * 0.52f));
            ConePiece(root, "Foliage", new Vector3(0f, height * 0.46f, 0f),
                new Vector3(height * 0.38f, height * 0.56f, height * 0.38f));
            return root;
        }

        /// <summary>シダ。短い幹と、上へ跳ねる葉の束。低いところを埋める。</summary>
        private GameObject BuildFern(float height)
        {
            var root = Root("Fern");
            Piece(root, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, height * 0.22f, 0f),
                new Vector3(height * 0.06f, height * 0.22f, height * 0.06f));

            const int fronds = 6;
            for (var i = 0; i < fronds; i++)
            {
                var angle = i / (float)fronds * 360f + (float)random.NextDouble() * 24f;
                var lean = 34f + (float)random.NextDouble() * 22f;
                var frond = Piece(root, PrimitiveType.Sphere, "Foliage", Vector3.zero,
                    new Vector3(height * 0.13f, height * 0.62f, height * 0.13f));
                frond.transform.localRotation = Quaternion.Euler(lean, angle, 0f);
                frond.transform.localPosition =
                    frond.transform.localRotation * new Vector3(0f, height * 0.62f, 0f)
                    + new Vector3(0f, height * 0.4f, 0f);
            }

            return root;
        }

        /// <summary>広葉樹。幹と、いびつな冠。球ひとつにすると円盤に見える。</summary>
        private GameObject BuildBroadleaf(float height)
        {
            var root = Root("Broadleaf");
            Piece(root, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, height * 0.32f, 0f),
                new Vector3(height * 0.06f, height * 0.32f, height * 0.06f));

            for (var i = 0; i < 5; i++)
            {
                var r = height * (0.16f + (float)random.NextDouble() * 0.17f);
                Piece(root, PrimitiveType.Sphere, "Foliage", new Vector3(
                        (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.26f,
                        height * (0.58f + (float)random.NextDouble() * 0.44f),
                        (float)(random.NextDouble() * 2.0 - 1.0) * height * 0.26f),
                    new Vector3(r, r * 1.05f, r));
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
            // 脚を短く、胴を長く取る。脚が長いと竹馬に乗ったように浮いて見える。
            var body = height * 0.72f;
            var legs = height * 0.30f;

            Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, legs + body * 0.28f, 0f),
                new Vector3(body * 0.42f, body * 0.46f, body));

            // 首は球を隙間なく重ねて弧にする。離すと数珠に見える。
            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                var y = legs + body * (0.36f + t * 0.62f);
                var z = body * (0.42f + t * 0.78f + Mathf.Sin(t * 3.1f) * 0.1f);
                var r = Mathf.Lerp(body * 0.21f, body * 0.09f, t);
                Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, y, z), new Vector3(r, r, r));
            }

            Piece(root, PrimitiveType.Sphere, "Creature",
                new Vector3(0f, legs + body * 0.99f, body * 1.28f),
                new Vector3(body * 0.12f, body * 0.11f, body * 0.22f));

            for (var i = 0; i < 10; i++)
            {
                var t = i / 9f;
                var r = Mathf.Lerp(body * 0.17f, body * 0.03f, t);
                Piece(root, PrimitiveType.Sphere, "Creature",
                    new Vector3(0f, legs + body * (0.3f - t * 0.16f), -body * (0.45f + t * 0.72f)),
                    new Vector3(r, r, r));
            }

            for (var i = 0; i < 4; i++)
            {
                var front = i < 2;
                var side = (i % 2 == 0) ? 1f : -1f;
                Piece(root, PrimitiveType.Cylinder, "Creature",
                    new Vector3(side * body * 0.22f, legs * 0.5f, front ? body * 0.3f : -body * 0.3f),
                    new Vector3(body * 0.15f, legs * 0.5f, body * 0.15f));
            }

            return root;
        }

        /// <summary>二本足の生きもの。胴を立て、尾で釣り合わせる。</summary>
        private GameObject BuildBiped(float height)
        {
            var root = Root("Biped");
            var legs = height * 0.45f;
            var body = height * 0.5f;

            Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, legs + body * 0.4f, 0f),
                new Vector3(body * 0.5f, body * 0.6f, body * 0.85f));
            Piece(root, PrimitiveType.Sphere, "Creature", new Vector3(0f, legs + body * 0.9f, body * 0.4f),
                new Vector3(body * 0.26f, body * 0.24f, body * 0.34f));

            for (var i = 0; i < 8; i++)
            {
                var t = i / 7f;
                var r = Mathf.Lerp(body * 0.2f, body * 0.04f, t);
                Piece(root, PrimitiveType.Sphere, "Creature",
                    new Vector3(0f, legs + body * (0.36f - t * 0.1f), -body * (0.4f + t * 0.5f)),
                    new Vector3(r, r, r));
            }

            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? 1f : -1f;
                Piece(root, PrimitiveType.Cylinder, "Creature",
                    new Vector3(side * body * 0.18f, legs * 0.5f, 0f),
                    new Vector3(body * 0.13f, legs * 0.5f, body * 0.13f));
            }

            return root;
        }

        /// <summary>
        /// 人の建てたもの。高さと数だけで、集落から都市までを1つの作りで表す。
        ///
        /// 段階ごとに別の形を作らない。低くて小さいものを散らせば集落に、
        /// 高さを上げて数を増やせば都市になる。作り分けると、段階が増えるたびに
        /// 書き足すことになり、必ず破綻する。
        /// </summary>
        private GameObject BuildStructure(float height)
        {
            var root = Root("Structure");
            var width = height * (0.45f + (float)random.NextDouble() * 0.5f);
            var depth = width * (0.7f + (float)random.NextDouble() * 0.7f);

            Piece(root, PrimitiveType.Cube, "Building", new Vector3(0f, height * 0.5f, 0f),
                new Vector3(width, height, depth));

            // **屋根は平らにする。三角屋根にしない。**
            // 新石器時代のチャタルホユックは平らな屋根で、屋根から梯子で出入りし、
            // 家のあいだに道が無かった。三角屋根を載せると別の時代の姿になる。
            if (!current.Windows || height < current.BuildingHeight * 0.85f)
            {
                Piece(root, PrimitiveType.Cube, "Building",
                    new Vector3(0f, height * 1.02f, 0f),
                    new Vector3(width * 1.06f, height * 0.05f, depth * 1.06f));
                return root;
            }

            // 高いものには窓の帯を入れる。のっぺりした箱に高さを感じさせるため。
            var floors = Mathf.Clamp(Mathf.RoundToInt(height / (current.BuildingHeight * 0.16f)), 2, 9);
            for (var i = 0; i < floors; i++)
            {
                var y = height * (0.16f + 0.78f * i / Mathf.Max(1, floors - 1));
                Piece(root, PrimitiveType.Cube, "Window", new Vector3(0f, y, 0f),
                    new Vector3(width * 1.02f, height * 0.035f, depth * 1.02f));
            }

            return root;
        }

        /// <summary>
        /// 円錐の網。針葉樹と屋根に使う。
        /// Unityの基本形には円錐が無いので自分で作る。球を積むと雪だるまになる。
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
    }
}
