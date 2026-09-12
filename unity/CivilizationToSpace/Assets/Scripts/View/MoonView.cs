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

            surfaceMaterial = new Material(Shader.Find("Standard"));
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

            Apply(null);
        }

        /// <summary>段階を写す。null を渡すと何も無い状態になる。</summary>
        public void Apply(MoonPhase phase)
        {
            transferAmount = phase != null ? (float)phase.Transfer : 0f;
            facilityAmount = phase != null ? (float)phase.Facility : 0f;
            lightAmount = phase != null ? (float)phase.SurfaceLights : 0f;

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

            MoveTransfers();
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

        private void BuildTransfers(HideFlags flags)
        {
            transfers = new GameObject[TransferPoolSize];
            transferOffsets = new float[TransferPoolSize];

            var material = new Material(Shader.Find("Standard"));
            material.hideFlags = flags;
            material.color = TransferColor;
            material.EnableKeyword("_EMISSION");
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

            facilityMaterial = new Material(Shader.Find("Standard"));
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
