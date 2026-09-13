using System.Collections.Generic;
using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 象徴的な地球。時代ごとに手続き的に描いた地表を球へ貼る。
    ///
    /// 一様な色の球では、自転していても動いて見えず、時代ごとの違いも色の差でしか読めない。
    /// 海陸の分布・植生・氷・火山・都市光を絵として持たせ、時代を切り替えると
    /// 大陸の位置と形、緑や氷の広がりが変わるようにしている。
    ///
    /// 時代の切り替えは、次の地表を重ねて濃くしていくことで移り変わらせる。
    /// 画素を毎フレーム合成すると重いため、球を2枚重ねて不透明度だけを動かす。
    ///
    /// ブラウザとの見た目の一致は求めない。色・半径・合成はすべて描画側の都合であり、
    /// 共通データではない。いずれの色も特定の時代を表さない。
    /// </summary>
    public sealed class EarthView : MonoBehaviour
    {
        private const float BaseRadius = 2.2f;

        /// <summary>地球の半径。カメラのフレーミングが同じ値を見る。</summary>
        public static float Radius
        {
            get { return BaseRadius; }
        }

        private static readonly Color FallbackColor = new Color(0.41f, 0.47f, 0.53f);

        /// <summary>自転の速さ（度／秒）。1周およそ45秒。</summary>
        private const float SpinDegreesPerSecond = 8f;

        /// <summary>雲を地表より少し速く流す（度／秒）。</summary>
        private const float CloudDegreesPerSecond = 11f;

        /// <summary>衛星の周回の速さ（度／秒）。</summary>
        private const float OrbitDegreesPerSecond = 14f;

        /// <summary>
        /// 時代を切り替えたときの移り変わりの長さ（秒）。
        /// 1段階ぶんの時間（1倍速で4秒）の半分ほどにして、
        /// 次へ進む前には落ち着き、かつ切り替わりが唐突にならないようにする。
        /// </summary>
        private const float TransitionSeconds = 2.2f;

        /// <summary>衛星の表示上限。データ側の値に上限は無いため、描画側で持ち数を決める。</summary>
        private const int SatellitePoolSize = 8;

        private Transform spin;
        private Transform cloudSpin;
        private Transform satelliteRing;

        private Material currentMaterial;
        private Material incomingMaterial;
        private Material cloudMaterial;
        private Material atmosphereMaterial;

        /// <summary>高さで膨らませた球。輪郭にも起伏が出る。</summary>
        private PlanetMesh planet;

        private GameObject[] satellites;

        private HideFlags createdFlags = HideFlags.None;
        private MotionSettings motion;

        /// <summary>焼いた地表の控え。時代ごとに1組だけ作る。</summary>
        private readonly Dictionary<int, PlanetSurfaceBaker.Surface> baked =
            new Dictionary<int, PlanetSurfaceBaker.Surface>();

        private float transition = 1f;
        private bool hasSurface;

        /// <summary>動きの設定を渡す。渡さない場合は常に動く。</summary>
        public void SetMotionSettings(MotionSettings settings)
        {
            motion = settings;
        }

        public void Build()
        {
            Build(HideFlags.None);
        }

        public void Build(HideFlags flags)
        {
            createdFlags = flags;

            var spinObject = new GameObject("Spin");
            spinObject.hideFlags = flags;
            spinObject.transform.SetParent(transform, false);
            spin = spinObject.transform;

            planet = new PlanetMesh(BaseRadius);
            planet.Mesh.hideFlags = flags;

            currentMaterial = CreateSurfaceMaterial(false, 0, true);
            CreatePlanetShell(spin, "Surface", 1.000f, currentMaterial);

            incomingMaterial = CreateSurfaceMaterial(true, 1, true);
            CreatePlanetShell(spin, "SurfaceNext", 1.003f, incomingMaterial);
            SetSurfaceAlpha(incomingMaterial, 0f);

            var cloudObject = new GameObject("CloudSpin");
            cloudObject.hideFlags = flags;
            cloudObject.transform.SetParent(transform, false);
            cloudSpin = cloudObject.transform;

            cloudMaterial = CreateSurfaceMaterial(true, 2, false);
            var clouds = CreateSphere(cloudSpin, "Clouds", 1.050f, cloudMaterial);

            atmosphereMaterial = CreateBlended(
                FallbackColor,
                3,
                UnityEngine.Rendering.BlendMode.SrcAlpha,
                UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha,
                false);
            var atmosphere = CreateSphere(transform, "Atmosphere", 1.075f, atmosphereMaterial);

            HideShellsWhereBlendingIsUnavailable(clouds, atmosphere);

            BuildSatellites();
        }

        /// <summary>
        /// 雲と大気の殻を、WebGLでは出さない。
        ///
        /// **これは回避策であって原因の修復ではない。**
        /// WebGLビルドでは、この2つの殻の半透明が効かず、不透明な球として描かれる。
        /// いちばん外側の大気が地表を完全に覆うため、地球が単色の球に見えていた。
        /// 実機のブラウザで、殻を外すと地表の絵が正しく出ることを確かめたうえで、
        /// 「単色の球」より「雲と大気が無い地球」の方が良いと判断して外している。
        ///
        /// 原因はStandardの半透明の枝がビルドに残らないことだと見ているが、
        /// 確定できていない。次の2つを試したが、どちらでも直らなかった。
        ///   1. 実行時に行き着くキーワードの組み合わせ（_ALPHABLEND_ON、_EMISSION、
        ///      _NORMALMAP の全組み合わせ）を雛形の材質として資産へ置く
        ///   2. 専用の半透明シェーダーへ移す（描き方は変わったが見え方を揃えられなかった）
        ///
        /// 本筋の直し方は、雲を地表の絵へ焼き込んでしまうことだと思われる。
        /// そうすれば半透明の殻そのものが要らなくなる。PlanetSurfaceBaker 側の仕事になる。
        ///
        /// Editorと他の環境では今までどおり出す。見え方は変えない。
        /// </summary>
        private static void HideShellsWhereBlendingIsUnavailable(GameObject clouds, GameObject atmosphere)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (clouds != null)
            {
                clouds.SetActive(false);
            }

            if (atmosphere != null)
            {
                atmosphere.SetActive(false);
            }
#endif
        }

        /// <summary>
        /// 時代の視覚値を写す。動きを減らしていなければ、少しかけて移り変わる。
        /// 衛星の個数だけは補間しない。個数は連続量ではないためである。
        /// </summary>
        public void Apply(EraVisual visual, int eraIndex)
        {
            var surface = GetOrBake(visual, eraIndex);
            var instant = !hasSurface || (motion != null && motion.Reduced);

            if (instant)
            {
                AssignSurface(currentMaterial, surface);
                SetSurfaceAlpha(incomingMaterial, 0f);
                planet.SetImmediate(surface.Elevation);
                transition = 1f;
            }
            else
            {
                AssignSurface(incomingMaterial, surface);
                SetSurfaceAlpha(incomingMaterial, 0f);

                // 大陸がせり上がり、また沈む様子を出すため、形も一緒に移り変わらせる。
                planet.BeginTransition(surface.Elevation);
                transition = 0f;
            }

            cloudMaterial.mainTexture = surface.Clouds;

            Color emissionColor;
            if (!ColorUtility.TryParseHtmlString(visual.EmissionColor, out emissionColor))
            {
                emissionColor = FallbackColor;
            }

            // 大気は薄くする。濃いと地表全体に膜がかかり、地形が読めなくなる。
            // 球の外側へはみ出した部分が輪郭の光として残ればよい。
            var atmosphere = emissionColor;
            atmosphere.a = 0.10f;
            atmosphereMaterial.color = atmosphere;
            atmosphereMaterial.SetColor("_EmissionColor", emissionColor * 0.45f);

            hasSurface = true;
            ApplySatellites(visual.SatelliteCount);
        }

        private void Update()
        {
            var reduced = motion != null && motion.Reduced;

            if (!reduced)
            {
                if (spin != null)
                {
                    spin.Rotate(Vector3.up, SpinDegreesPerSecond * SceneClock.Delta, Space.Self);
                }

                if (cloudSpin != null)
                {
                    cloudSpin.Rotate(Vector3.up, CloudDegreesPerSecond * SceneClock.Delta, Space.Self);
                }

                if (satelliteRing != null)
                {
                    satelliteRing.Rotate(Vector3.up, OrbitDegreesPerSecond * SceneClock.Delta, Space.Self);
                }
            }

            if (transition >= 1f)
            {
                return;
            }

            if (reduced)
            {
                // 途中で動きを減らした場合は、そこで打ち切って目的の見た目にする。
                transition = 1f;
                planet.SetTransition(1f);
                Settle();
                return;
            }

            transition = Mathf.Min(1f, transition + SceneClock.Delta / TransitionSeconds);
            SetSurfaceAlpha(incomingMaterial, transition);
            planet.SetTransition(transition);

            if (transition >= 1f)
            {
                Settle();
            }
        }

        /// <summary>重ねていた次の地表を、そのまま本体の地表にする。</summary>
        private void Settle()
        {
            currentMaterial.mainTexture = incomingMaterial.mainTexture;
            currentMaterial.SetTexture("_EmissionMap", incomingMaterial.GetTexture("_EmissionMap"));
            currentMaterial.SetTexture("_BumpMap", incomingMaterial.GetTexture("_BumpMap"));
            SetSurfaceAlpha(incomingMaterial, 0f);
        }

        /// <summary>
        /// 膨らませた球を1枚置く。本体と重ね合わせ用で同じメッシュを共有し、
        /// 大きさだけをわずかに変えて重なりを避ける。
        /// </summary>
        private void CreatePlanetShell(Transform parent, string name, float scale, Material material)
        {
            var shell = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            shell.hideFlags = createdFlags;
            shell.transform.SetParent(parent, false);
            shell.transform.localScale = Vector3.one * scale;
            shell.GetComponent<MeshFilter>().sharedMesh = planet.Mesh;
            shell.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private PlanetSurfaceBaker.Surface GetOrBake(EraVisual visual, int eraIndex)
        {
            PlanetSurfaceBaker.Surface surface;
            if (baked.TryGetValue(eraIndex, out surface))
            {
                return surface;
            }

            surface = PlanetSurfaceBaker.Bake(visual, eraIndex);
            surface.Albedo.hideFlags = createdFlags;
            surface.Emission.hideFlags = createdFlags;
            surface.Clouds.hideFlags = createdFlags;
            baked[eraIndex] = surface;
            return surface;
        }

        /// <summary>
        /// まだ焼いていない時代を1つだけ焼く。焼くものが無ければ false を返す。
        /// 再生中に初めて使う時代を焼くと、その瞬間だけ画面が止まるため、
        /// 1フレームに1時代ずつ先に用意しておく。
        /// </summary>
        public bool BakeNext(IReadOnlyList<EraData> eras)
        {
            for (var i = 0; i < eras.Count; i++)
            {
                if (baked.ContainsKey(i))
                {
                    continue;
                }

                GetOrBake(eras[i].Visual, i);
                return true;
            }

            return false;
        }

        private static void AssignSurface(Material material, PlanetSurfaceBaker.Surface surface)
        {
            material.mainTexture = surface.Albedo;
            material.SetTexture("_EmissionMap", surface.Emission);
            material.SetTexture("_BumpMap", surface.Normal);
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_BumpScale", 1f);
        }

        private static void SetSurfaceAlpha(Material material, float alpha)
        {
            var color = material.color;
            color.a = Mathf.Clamp01(alpha);
            material.color = color;
        }

        private void BuildSatellites()
        {
            satellites = new GameObject[SatellitePoolSize];

            var material = StandardMaterials.CreateOpaque(true);
            material.hideFlags = createdFlags;
            material.color = new Color(0.78f, 0.83f, 0.88f);
            material.SetColor("_EmissionColor", new Color(0.35f, 0.37f, 0.40f));

            var ring = new GameObject("Satellites");
            ring.hideFlags = createdFlags;
            ring.transform.SetParent(transform, false);
            ring.transform.localRotation = Quaternion.Euler(24f, 0f, 12f);
            satelliteRing = ring.transform;

            for (var i = 0; i < SatellitePoolSize; i++)
            {
                var angle = 360f / SatellitePoolSize * i;
                var satellite = PrimitiveMeshes.Create(PrimitiveType.Sphere, "Satellite" + (i + 1), createdFlags);
                satellite.transform.SetParent(ring.transform, false);
                satellite.transform.localPosition =
                    Quaternion.Euler(0f, angle, 0f) * new Vector3(BaseRadius * 1.45f, 0f, 0f);
                satellite.transform.localScale = Vector3.one * (BaseRadius * 0.055f);
                satellite.GetComponent<Renderer>().sharedMaterial = material;
                satellite.SetActive(false);
                satellites[i] = satellite;
            }
        }

        private void ApplySatellites(double count)
        {
            // データ側の個数に上限は無い。持ち数を超える分は描かない。
            var visible = count > SatellitePoolSize ? SatellitePoolSize : (int)count;
            for (var i = 0; i < satellites.Length; i++)
            {
                satellites[i].SetActive(i < visible);
            }
        }

        /// <summary>作った球を返す。呼び出し側が後から出し入れできるようにする。</summary>
        private GameObject CreateSphere(Transform parent, string name, float radiusScale, Material material)
        {
            var sphere = PrimitiveMeshes.Create(PrimitiveType.Sphere, name, createdFlags);
            sphere.transform.SetParent(parent != null ? parent : transform, false);
            sphere.transform.localScale = Vector3.one * (BaseRadius * 2f * radiusScale);
            sphere.GetComponent<Renderer>().sharedMaterial = material;
            return sphere;
        }

        /// <summary>
        /// 地表用の材質。焼いた絵を貼り、自ら光る量も絵で与える。
        /// 重ねる層は半透明にして、不透明度で移り変わらせる。
        /// </summary>
        private Material CreateSurfaceMaterial(bool transparent, int queueOffset, bool emissive)
        {
            var material = transparent
                ? CreateBlended(
                    Color.white,
                    queueOffset,
                    UnityEngine.Rendering.BlendMode.SrcAlpha,
                    UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha,
                    emissive)
                // 発光する地表の材質には、生成後に AssignSurface が
                // 凹凸の絵と _NORMALMAP を足す。雛形もそれに合わせて選ぶ。
                : emissive
                    ? StandardMaterials.CreateOpaqueSurface()
                    : StandardMaterials.CreateOpaque(false);

            material.hideFlags = createdFlags;
            material.SetFloat("_Glossiness", 0.08f);
            material.SetFloat("_Metallic", 0f);

            if (emissive)
            {
                // 発光マップを貼る材質だけ発光を有効にする。
                // マップを渡さずに有効にすると、既定の白い画像が使われて全面が光る。
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.white);
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }

            if (!transparent)
            {
                material.color = Color.white;
            }

            return material;
        }

        /// <param name="surface">
        /// 地表の層なら真。地表の層は生成後に発光と凹凸の絵を貼るため、
        /// 雛形もその組み合わせに合わせる必要がある。雲と大気は偽。
        /// </param>
        private Material CreateBlended(
            Color color,
            int queueOffset,
            UnityEngine.Rendering.BlendMode source,
            UnityEngine.Rendering.BlendMode destination,
            bool surface)
        {
            var material = surface
                ? StandardMaterials.CreateFadeSurface()
                : StandardMaterials.CreateFade();
            material.hideFlags = createdFlags;
            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)source);
            material.SetInt("_DstBlend", (int)destination);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + queueOffset;
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.color = color;
            return material;
        }

        /// <summary>
        /// 編集中は Destroy が次のフレームまで効かない。プレビューを組み直せるよう経路を分ける。
        /// </summary>
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

        /// <summary>生成した子と材質を片付ける。プレビューを作り直すときに呼ぶ。</summary>
        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                SafeDestroy(transform.GetChild(i).gameObject);
            }

            SafeDestroy(currentMaterial);
            SafeDestroy(incomingMaterial);
            SafeDestroy(cloudMaterial);
            SafeDestroy(atmosphereMaterial);

            if (planet != null)
            {
                SafeDestroy(planet.Mesh);
                planet = null;
            }

            foreach (var surface in baked.Values)
            {
                SafeDestroy(surface.Albedo);
                SafeDestroy(surface.Emission);
                SafeDestroy(surface.Clouds);
            }

            baked.Clear();
            satellites = null;
            spin = null;
            cloudSpin = null;
            satelliteRing = null;
            hasSurface = false;
        }
    }
}
