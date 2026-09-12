using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 月と、地球との行き来、月面の拠点。
    ///
    /// **これは予測でも計画でもない。** 仮想シナリオのひとつを象徴的に表す演出であり、
    /// 実際の軌道要素・距離・周期・推進・生命維持・資源収支を一切持たない。
    /// 距離も大きさも見やすさのために決めた値であり、実際の比ではない。
    ///
    /// 地球と同じく、月面も手続き的に描く。画像素材を持ち込まない。
    /// </summary>
    public sealed class MoonView : MonoBehaviour
    {
        /// <summary>月の半径。地球に対する実際の比（約0.27）より小さく、見やすさを優先する。</summary>
        private const float MoonRadius = 0.58f;

        /// <summary>地球の中心から月までの距離。実際の比（約60倍）では画面に収まらない。</summary>
        private const float OrbitRadius = 5.6f;

        /// <summary>月が地球のまわりを回る速さ（度／秒）。</summary>
        private const float OrbitDegreesPerSecond = 9f;

        /// <summary>月自身の自転（度／秒）。</summary>
        private const float SpinDegreesPerSecond = 4f;

        /// <summary>行き来する物体の持ち数。</summary>
        private const int TransferPoolSize = 6;

        /// <summary>月面に置く拠点の持ち数。</summary>
        private const int FacilityPoolSize = 14;

        /// <summary>
        /// 地球を回る拠点の軌道半径。地球の半径（2.2）より外、月の軌道（5.6）よりずっと内側に置く。
        /// 実際の高度の比ではない。低い軌道を回っていることだけを示す。
        ///
        /// 月の距離を約10分の1へ縮めているのと同じ理由で、ここも見やすさを優先している。
        /// 地表すれすれに置くと拠点が地球へ重なって見分けられないため、
        /// R1の人工衛星の輪（3.19）より外へ出し、背景の黒の上を通るようにした。
        /// </summary>
        private const float StationOrbitRadius = 3.5f;

        /// <summary>地球を回る拠点の速さ。月より速く回して、近くを回っていることを示す。</summary>
        private const float StationDegreesPerSecond = 26f;

        private static readonly Color32 Regolith = new Color32(0x8C, 0x88, 0x82, 0xFF);
        private static readonly Color32 Mare = new Color32(0x5A, 0x59, 0x58, 0xFF);
        private static readonly Color32 FacilityColor = new Color32(0xC8, 0xD2, 0xDC, 0xFF);
        private static readonly Color32 FacilityGlow = new Color32(0xFF, 0xD2, 0x93, 0xFF);
        private static readonly Color32 TransferColor = new Color32(0xE6, 0xEC, 0xF2, 0xFF);

        private Transform orbit;
        private Transform body;
        private Transform spin;
        private Material surfaceMaterial;
        private Material facilityMaterial;

        private GameObject[] transfers;
        private float[] transferOffsets;
        private GameObject[] facilities;

        private MotionSettings motion;
        private Vector3 earthCenter;

        private float transferAmount;
        private float facilityAmount;
        private float lightAmount;
        private float stationAmount;

        private Transform stationOrbit;
        private Transform station;
        private Material stationMaterial;

        /// <summary>月の中心。カメラの引きがこの位置を見る。</summary>
        public Vector3 MoonCenter
        {
            get { return body != null ? body.position : transform.position; }
        }

        /// <summary>地球と月の両方が画面へ入るために必要な半径。カメラがこれを見る。</summary>
        public static float FramedRadius
        {
            get { return OrbitRadius + MoonRadius * 2f; }
        }

        /// <summary>
        /// 月そのものを出すかどうか。形成過程で月が現れる前は出さない。
        /// 現れたあとは消さない。時代のあいだは画面の外にある。
        /// </summary>
        public void SetBodyVisible(bool visible)
        {
            if (orbit != null)
            {
                orbit.gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// 月が現れる度合い。0で見えず、1で本来の大きさ。
        /// 破片から集まってくる様子を、大きさの変化で表す。
        /// </summary>
        public void SetBodyScale(float scale)
        {
            if (body == null)
            {
                return;
            }

            var clamped = Mathf.Clamp01(scale);
            body.localScale = Vector3.one * clamped;
            if (orbit != null)
            {
                orbit.gameObject.SetActive(clamped > 0.001f);
            }
        }

        public void SetMotionSettings(MotionSettings settings)
        {
            motion = settings;
        }

        public void Build(Vector3 earth, HideFlags flags)
        {
            earthCenter = earth;
            transform.position = earth;

            var orbitObject = new GameObject("MoonOrbit");
            orbitObject.hideFlags = flags;
            orbitObject.transform.SetParent(transform, false);
            orbitObject.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            orbit = orbitObject.transform;

            var bodyObject = new GameObject("Moon");
            bodyObject.hideFlags = flags;
            bodyObject.transform.SetParent(orbit, false);
            bodyObject.transform.localPosition = new Vector3(OrbitRadius, 0f, 0f);
            body = bodyObject.transform;

            var spinObject = new GameObject("MoonSpin");
            spinObject.hideFlags = flags;
            spinObject.transform.SetParent(body, false);
            spin = spinObject.transform;

            surfaceMaterial = StandardMaterials.CreateOpaque(false);
            surfaceMaterial.hideFlags = flags;
            surfaceMaterial.SetFloat("_Glossiness", 0.04f);
            surfaceMaterial.SetFloat("_Metallic", 0f);
            surfaceMaterial.mainTexture = BakeSurface(flags);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "MoonSurface";
            sphere.hideFlags = flags;
            SafeDestroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(spin, false);
            sphere.transform.localScale = Vector3.one * (MoonRadius * 2f);
            sphere.GetComponent<Renderer>().sharedMaterial = surfaceMaterial;

            BuildFacilities(flags);
            BuildTransfers(flags);
            BuildStation(flags);

            Apply(null);
        }

        /// <summary>段階を写す。null を渡すと何も無い状態になる。</summary>
        public void Apply(MoonPhase phase)
        {
            transferAmount = phase != null ? (float)phase.Transfer : 0f;
            facilityAmount = phase != null ? (float)phase.Facility : 0f;
            lightAmount = phase != null ? (float)phase.SurfaceLights : 0f;
            stationAmount = phase != null ? (float)phase.OrbitStation : 0f;

            ApplyStation();

            var visibleTransfers = Mathf.RoundToInt(transferAmount * TransferPoolSize);
            for (var i = 0; i < transfers.Length; i++)
            {
                transfers[i].SetActive(i < visibleTransfers);
            }

            var visibleFacilities = Mathf.RoundToInt(facilityAmount * FacilityPoolSize);
            for (var i = 0; i < facilities.Length; i++)
            {
                facilities[i].SetActive(i < visibleFacilities);
            }

            facilityMaterial.SetColor("_EmissionColor", (Color)FacilityGlow * (lightAmount * 1.4f));
        }

        private void Update()
        {
            var reduced = motion != null && motion.Reduced;
            if (reduced)
            {
                return;
            }

            if (orbit != null)
            {
                orbit.Rotate(Vector3.up, OrbitDegreesPerSecond * Time.deltaTime, Space.Self);
            }

            if (spin != null)
            {
                spin.Rotate(Vector3.up, SpinDegreesPerSecond * Time.deltaTime, Space.Self);
            }

            if (stationOrbit != null && stationAmount > 0f)
            {
                stationOrbit.Rotate(Vector3.up, StationDegreesPerSecond * Time.deltaTime, Space.Self);
            }

            MoveTransfers();
        }

        /// <summary>
        /// 地球を回る拠点の見え方を、段階の値へ合わせる。
        /// 月面の施設とは別に、月へ向かう前から置かれる。
        /// </summary>
        private void ApplyStation()
        {
            if (station == null)
            {
                return;
            }

            var on = stationAmount > 0.01f;
            station.gameObject.SetActive(on);
            if (!on)
            {
                return;
            }

            // 段階が進むほど大きく見せる。上限は、内側の板の端が地球の表面へ触れない大きさに収める。
            // 板の端は中心から 0.47 の位置にあり、地球の表面までは 3.5-2.2=1.3 ある。
            station.localScale = Vector3.one * Mathf.Lerp(1f, 1.35f, stationAmount);

            if (stationMaterial != null)
            {
                stationMaterial.SetColor("_EmissionColor", (Color)FacilityGlow * (stationAmount * 1.1f));
            }
        }

        /// <summary>
        /// 行き来する物体を、地球と月のあいだで往復させる。
        /// 弧を描かせるのは、直線だと地球の裏を通り抜けて見えるためである。
        /// </summary>
        private void MoveTransfers()
        {
            if (transfers == null)
            {
                return;
            }

            // 行き来が多い段階ほど速く動かす。回数を表すものではない。
            var speed = 0.10f + transferAmount * 0.16f;
            var moon = MoonCenter;

            for (var i = 0; i < transfers.Length; i++)
            {
                if (!transfers[i].activeSelf)
                {
                    continue;
                }

                transferOffsets[i] += Time.deltaTime * speed;
                var cycle = Mathf.Repeat(transferOffsets[i], 2f);

                // 0〜1で地球から月へ、1〜2で月から地球へ。
                var forward = cycle < 1f;
                var t = forward ? cycle : 2f - cycle;

                var from = forward ? earthCenter : moon;
                var to = forward ? moon : earthCenter;

                var straight = Vector3.Lerp(from, to, t);
                var bulge = Mathf.Sin(t * Mathf.PI) * 1.5f;
                var side = Vector3.Cross((to - from).normalized, Vector3.up).normalized;

                transfers[i].transform.position = straight + side * (bulge * (i % 2 == 0 ? 1f : -1f))
                                                  + Vector3.up * (bulge * 0.35f);
            }
        }

        /// <summary>
        /// 地球を回る拠点を組む。進む向きへ伸びた胴体と、横木の両端に張った板でできている。
        ///
        /// 実在の宇宙ステーションの再現ではない。寸法・軌道高度・乗員数・電力を一切持たず、
        /// 「人が滞在する場所が地球の軌道にある」ことだけを示す象徴的な形である。
        /// 横木は軌道の半径の向き（内外）へ伸ばす。進む向きへ伸ばすと、
        /// どの角度から見ても細い線にしか見えない時があるためである。
        /// </summary>
        private void BuildStation(HideFlags flags)
        {
            var orbitObject = new GameObject("StationOrbit");
            orbitObject.hideFlags = flags;
            orbitObject.transform.SetParent(transform, false);
            // 月の軌道（12度）と別の傾きにして、二つが重なって見えないようにする。
            orbitObject.transform.localRotation = Quaternion.Euler(-26f, 0f, 8f);
            stationOrbit = orbitObject.transform;

            var stationObject = new GameObject("OrbitStation");
            stationObject.hideFlags = flags;
            stationObject.transform.SetParent(stationOrbit, false);
            stationObject.transform.localPosition = new Vector3(StationOrbitRadius, 0f, 0f);
            station = stationObject.transform;

            stationMaterial = StandardMaterials.CreateOpaque(true);
            stationMaterial.hideFlags = flags;
            stationMaterial.color = FacilityColor;
            stationMaterial.SetFloat("_Glossiness", 0.55f);
            stationMaterial.SetFloat("_Metallic", 0.7f);
            stationMaterial.EnableKeyword("_EMISSION");
            stationMaterial.SetColor("_EmissionColor", Color.black);

            // 胴体。進む向き（Z）へ寝かせる。円柱の長い軸はYなので、X軸まわりに90度倒す。
            AddStationPart(flags, "Hull", PrimitiveType.Cylinder,
                Vector3.zero, Quaternion.Euler(90f, 0f, 0f), new Vector3(0.10f, 0.26f, 0.10f));

            // 板をつなぐ横木。
            AddStationPart(flags, "Truss", PrimitiveType.Cube,
                Vector3.zero, Quaternion.identity, new Vector3(0.62f, 0.03f, 0.03f));

            // 両端の板。太陽電池の代わりで、発電量を表さない。
            for (var i = 0; i < 2; i++)
            {
                AddStationPart(flags, "Panel" + (i + 1), PrimitiveType.Cube,
                    new Vector3(i == 0 ? 0.30f : -0.30f, 0f, 0f), Quaternion.identity,
                    new Vector3(0.34f, 0.012f, 0.20f));
            }

            stationObject.SetActive(false);
        }

        /// <summary>拠点の部品をひとつ足す。当たり判定は要らないので外す。</summary>
        private void AddStationPart(
            HideFlags flags, string name, PrimitiveType shape,
            Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.hideFlags = flags;
            SafeDestroy(part.GetComponent<Collider>());
            part.transform.SetParent(station, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = stationMaterial;
        }

        private void BuildTransfers(HideFlags flags)
        {
            transfers = new GameObject[TransferPoolSize];
            transferOffsets = new float[TransferPoolSize];

            var material = StandardMaterials.CreateOpaque(true);
            material.hideFlags = flags;
            material.color = TransferColor;
            material.SetColor("_EmissionColor", (Color)TransferColor * 0.5f);

            for (var i = 0; i < TransferPoolSize; i++)
            {
                var item = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                item.name = "Transfer" + (i + 1);
                item.hideFlags = flags;
                SafeDestroy(item.GetComponent<Collider>());
                item.transform.SetParent(transform, false);
                item.transform.localScale = new Vector3(0.11f, 0.22f, 0.11f);
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                transfers[i] = item;
                transferOffsets[i] = i * (2f / TransferPoolSize);
            }
        }

        private void BuildFacilities(HideFlags flags)
        {
            facilities = new GameObject[FacilityPoolSize];

            facilityMaterial = StandardMaterials.CreateOpaque(true);
            facilityMaterial.hideFlags = flags;
            facilityMaterial.color = FacilityColor;
            facilityMaterial.SetFloat("_Glossiness", 0.2f);
            facilityMaterial.EnableKeyword("_EMISSION");
            facilityMaterial.SetColor("_EmissionColor", Color.black);

            // 拠点は月面の一帯へ寄せて置く。全面に散らすと、広がった様子が読めない。
            for (var i = 0; i < FacilityPoolSize; i++)
            {
                var angle = i * 137.5f * Mathf.Deg2Rad;
                var spread = Mathf.Sqrt((i + 0.5f) / FacilityPoolSize) * 0.9f;

                var latitude = Mathf.Cos(angle) * spread * 0.8f;
                var longitude = Mathf.Sin(angle) * spread * 1.1f;

                var direction = new Vector3(
                    Mathf.Cos(latitude) * Mathf.Cos(longitude),
                    Mathf.Sin(latitude),
                    Mathf.Cos(latitude) * Mathf.Sin(longitude)).normalized;

                var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name = "Facility" + (i + 1);
                item.hideFlags = flags;
                SafeDestroy(item.GetComponent<Collider>());
                item.transform.SetParent(spin, false);
                item.transform.localPosition = direction * (MoonRadius * 1.01f);
                item.transform.localRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                item.transform.localScale = new Vector3(0.10f, 0.055f, 0.10f);
                item.GetComponent<Renderer>().sharedMaterial = facilityMaterial;
                item.SetActive(false);

                facilities[i] = item;
            }
        }

        /// <summary>月面。灰色の地に、暗い海（マリア）とクレーターの明暗を置く。</summary>
        private static Texture2D BakeSurface(HideFlags flags)
        {
            const int Width = 256;
            const int Height = 128;

            var pixels = new Color32[Width * Height];
            for (var y = 0; y < Height; y++)
            {
                var latitude = ((y + 0.5f) / Height - 0.5f) * Mathf.PI;
                var cosLatitude = Mathf.Cos(latitude);
                var sinLatitude = Mathf.Sin(latitude);

                for (var x = 0; x < Width; x++)
                {
                    var longitude = (x + 0.5f) / Width * Mathf.PI * 2f;
                    var direction = new Vector3(
                        cosLatitude * Mathf.Cos(longitude), sinLatitude, cosLatitude * Mathf.Sin(longitude));

                    var mare = ValueNoise3D.Fractal(direction * 2.6f, 311, 3, 0.5f);
                    var crater = ValueNoise3D.Fractal(direction * 14f, 733, 3, 0.5f);

                    var color = Color.Lerp(
                        (Color)Regolith, (Color)Mare, Mathf.Clamp01((mare - 0.52f) * 3.2f));
                    color = Color.Lerp(color, color * 1.28f, Mathf.Clamp01((crater - 0.58f) * 2.6f));
                    color = Color.Lerp(color, color * 0.72f, Mathf.Clamp01((0.42f - crater) * 2.6f));

                    pixels[y * Width + x] = color;
                }
            }

            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true)
            {
                name = "MoonSurface",
                hideFlags = flags,
                wrapModeU = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            return texture;
        }

        private static void SafeDestroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
