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
    /// 時代の切り替えは、次の地表の絵を同じ材質へもう一組渡し、
    /// シェーダーの中で混ぜることで移り変わらせる。
    /// 以前は半透明の殻を上に重ねていたが、重ね終わって不透明の本体へ
    /// 差し替える1フレームだけ明るさが飛んでいた。1枚で混ぜれば、
    /// 混ぜ終わった姿と差し替えた姿が同じ式になり、飛びようがない。
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

        /// <summary>溶けた地表が自ら放つ色。割れ目から覗く溶岩にあたる。</summary>
        private static readonly Color MoltenColor = new Color(1f, 0.30f, 0.06f, 1f);

        /// <summary>
        /// 溶けているときの地色。焼いた絵に掛けて、岩の色を暗い赤へ寄せる。
        /// 発光だけを強めても、地色が岩のままだと全体が赤く見えない。
        /// </summary>
        private static readonly Color MoltenAlbedo = new Color(0.24f, 0.075f, 0.05f, 1f);

        /// <summary>いま溶けて見せている度合い。0で通常、1でもっとも赤い。</summary>
        private float molten;

        /// <summary>
        /// 溶岩の光をにじませる殻の大きさと強さ。
        /// 内側ほど強く、外へ行くほど弱くする。段を重ねることで、
        /// 輪郭の外側へ向かって光が薄れていくように見せる。
        /// </summary>
        private static readonly float[] GlowScales = { 1.030f, 1.090f, 1.180f };
        private static readonly float[] GlowWeights = { 0.85f, 0.50f, 0.28f };

        /// <summary>溶岩の光をにじませる殻。溶けていないときは消す。</summary>
        private GameObject[] glowShells;
        private Material[] glowMaterials;

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

        /// <summary>大気の色。移り変わりのあいだ、前の色から次の色へ寄せる。</summary>
        private Color atmosphereFrom = FallbackColor;
        private Color atmosphereTo = FallbackColor;

        /// <summary>
        /// 溶岩の光が輪郭の外へにじむ殻を組む。
        ///
        /// 地表に光る絵を貼るだけでは、光は球の内側で止まり、
        /// 溶けた岩が放つ明るさが外へ漏れているようには見えない。
        /// 少しずつ大きい球を重ね、外側ほど弱く光らせることで、
        /// 輪郭の外へ向かって薄れていく明るさを作る。
        ///
        /// 重ね方は加算にする。加算なら、重なったところが明るくなるだけで、
        /// 不透明度の扱いに左右されない。半透明が効かない環境でも同じに見える。
        /// </summary>
        private void BuildMagmaGlow()
        {
            glowShells = new GameObject[GlowScales.Length];
            glowMaterials = new Material[GlowScales.Length];

            for (var i = 0; i < GlowScales.Length; i++)
            {
                // 加算の専用シェーダー。混ぜ方はシェーダー側に固定してあるので、
                // 材質へ指定し直す必要がない。
                var material = StandardMaterials.CreateGlow();
                material.hideFlags = createdFlags;
                material.color = Color.white;
                material.SetColor("_EmissionColor", Color.black);
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 4 + i;

                glowMaterials[i] = material;

                var shell = new GameObject("MagmaGlow" + (i + 1), typeof(MeshFilter), typeof(MeshRenderer));
                shell.hideFlags = createdFlags;
                shell.transform.SetParent(spin, false);
                shell.transform.localScale = Vector3.one * GlowScales[i];
                shell.GetComponent<MeshFilter>().sharedMesh = planet.Mesh;
                shell.GetComponent<MeshRenderer>().sharedMaterial = material;
                shell.SetActive(false);

                glowShells[i] = shell;
            }
        }

        /// <summary>
        /// 地表を溶けた状態に見せる度合いを渡す。0で通常、1でもっとも赤い。
        ///
        /// 地球ができたばかりのころは全体が溶けていたとされ、
        /// 岩の色のままだと「もう固まった地球」に見えてしまう。
        /// 時代ごとに焼いた絵はそのまま使い、自ら光る色だけを赤へ寄せる。
        /// 絵を焼き直さないので、時代のデータには影響しない。
        /// </summary>
        public void SetMolten(float amount)
        {
            var next = Mathf.Clamp01(amount);
            if (Mathf.Approximately(next, molten))
            {
                return;
            }

            molten = next;
            ApplyMolten(currentMaterial);
            ApplyGlow();
        }

        /// <summary>にじみの強さを、いまの溶け具合へ合わせる。</summary>
        private void ApplyGlow()
        {
            if (glowShells == null)
            {
                return;
            }

            var on = molten > 0.01f;
            for (var i = 0; i < glowShells.Length; i++)
            {
                glowShells[i].SetActive(on);
                if (on)
                {
                    glowMaterials[i].SetColor(
                        "_EmissionColor", MoltenColor * (molten * GlowWeights[i] * 2.2f));
                }
            }
        }

        private void ApplyMolten(Material material)
        {
            if (material == null)
            {
                return;
            }

            // 通常は白。白のままだと発光の絵がそのまま出る。
            // 赤へ寄せるほど、発光の絵が溶岩の色に染まり、明るさも増す。
            material.SetColor("_EmissionColor", Color.Lerp(Color.white, MoltenColor * 2.2f, molten));

            // 地色にも掛ける。不透明度は移り変わりに使っているので、そこは触らない。
            var rgb = Color.Lerp(Color.white, MoltenAlbedo, molten);
            var current = material.color;
            material.color = new Color(rgb.r, rgb.g, rgb.b, current.a);
        }

        /// <summary>
        /// 地球の一部を描かないようにする。ぶつかって抉れた部分を表す。
        ///
        /// 球を小さくするだけでは「縮んだ」ようにしか見えず、壊れたことが伝わらない。
        /// 実際にその範囲を描かないことで、欠けた形を作る。
        /// </summary>
        /// <param name="worldCenter">削る球の中心（ワールド座標）。</param>
        /// <param name="radius">削る球の半径。0で削らない。</param>
        public void SetCut(Vector3 worldCenter, float radius)
        {
            var center = new Vector4(worldCenter.x, worldCenter.y, worldCenter.z, 0f);
            ApplyCut(currentMaterial, center, radius);
            ApplyCut(cloudMaterial, center, radius);
            ApplyCut(atmosphereMaterial, center, radius);

            if (glowMaterials == null)
            {
                return;
            }

            foreach (var glow in glowMaterials)
            {
                ApplyCut(glow, center, radius);
            }
        }

        private static void ApplyCut(Material material, Vector4 center, float radius)
        {
            if (material == null)
            {
                return;
            }

            material.SetVector("_CutCenter", center);
            material.SetFloat("_CutRadius", radius);
        }

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

            // 地表は1枚だけにする。次の時代の絵は同じ材質へもう一組の絵として渡し、
            // シェーダーの中で混ぜる。重ねる殻は作らない。
            currentMaterial = CreateSurfaceMaterial(false, 0, true);
            CreatePlanetShell(spin, "Surface", 1.000f, currentMaterial);

            BuildMagmaGlow();

            var cloudObject = new GameObject("CloudSpin");
            cloudObject.hideFlags = flags;
            cloudObject.transform.SetParent(transform, false);
            cloudSpin = cloudObject.transform;

            cloudMaterial = CreateSurfaceMaterial(true, 2, false);
            CreateSphere(cloudSpin, "Clouds", 1.050f, cloudMaterial);

            atmosphereMaterial = CreateBlended(
                FallbackColor,
                3,
                UnityEngine.Rendering.BlendMode.SrcAlpha,
                UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha,
                false);
            CreateSphere(transform, "Atmosphere", 1.075f, atmosphereMaterial);

            BuildSatellites();
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
                AssignNextSurface(currentMaterial, surface);
                currentMaterial.SetFloat("_Blend", 0f);
                planet.SetImmediate(surface.Elevation);
                transition = 1f;
            }
            else
            {
                // 途中で次の時代へ移ったときは、そこまで混ざっていたぶんを
                // いまの絵として確定させてから、新しい絵を次として渡す。
                // そうしないと、混ざりかけの姿を飛ばして別の絵から始めることになる。
                if (transition < 1f)
                {
                    Settle();
                }

                AssignNextSurface(currentMaterial, surface);
                currentMaterial.SetFloat("_Blend", 0f);

                // 大陸がせり上がり、また沈む様子を出すため、形も一緒に移り変わらせる。
                planet.BeginTransition(surface.Elevation);
                transition = 0f;
            }

            // 雲も地表と同じ要領で混ぜる。差し替えると雲の形がその場で飛ぶ。
            if (instant)
            {
                cloudMaterial.mainTexture = surface.Clouds;
            }

            cloudMaterial.SetTexture("_MainTexNext", surface.Clouds);
            cloudMaterial.SetFloat("_Blend", 0f);

            Color emissionColor;
            if (!ColorUtility.TryParseHtmlString(visual.EmissionColor, out emissionColor))
            {
                emissionColor = FallbackColor;
            }

            // 大気は薄くする。濃いと地表全体に膜がかかり、地形が読めなくなる。
            // 球の外側へはみ出した部分が輪郭の光として残ればよい。
            // 色も時代ごとに変わるので、移り変わりのあいだをかけて寄せる。
            atmosphereFrom = instant ? emissionColor : atmosphereTo;
            atmosphereTo = emissionColor;
            ApplyAtmosphere(instant ? 1f : 0f);

            hasSurface = true;
            ApplyMolten(currentMaterial);

            // にじみは、地表の「光る絵」をそのまま使う。
            // 溶岩のあるところだけが外へ漏れる形になる。
            if (glowMaterials != null)
            {
                foreach (var glow in glowMaterials)
                {
                    glow.SetTexture("_EmissionMap", surface.Emission);
                }
            }

            ApplyGlow();
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
            currentMaterial.SetFloat("_Blend", transition);
            cloudMaterial.SetFloat("_Blend", transition);
            ApplyAtmosphere(transition);
            planet.SetTransition(transition);

            if (transition >= 1f)
            {
                Settle();
            }
        }

        /// <summary>
        /// 混ぜ終わった次の地表を、そのまま今の地表にする。
        ///
        /// 混ぜ具合が1のとき、シェーダーは次の絵だけを出している。
        /// ここで次の絵を今の絵へ写し、混ぜ具合を0へ戻しても、
        /// 出てくる絵は同じである。見た目は変わらない。
        /// </summary>
        private void Settle()
        {
            currentMaterial.mainTexture = currentMaterial.GetTexture("_MainTexNext");
            currentMaterial.SetTexture("_EmissionMap", currentMaterial.GetTexture("_EmissionMapNext"));
            currentMaterial.SetTexture("_BumpMap", currentMaterial.GetTexture("_BumpMapNext"));
            currentMaterial.SetFloat("_Blend", 0f);

            cloudMaterial.mainTexture = cloudMaterial.GetTexture("_MainTexNext");
            cloudMaterial.SetFloat("_Blend", 0f);

            atmosphereFrom = atmosphereTo;
            ApplyAtmosphere(1f);
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

        /// <summary>
        /// 大気の色を、移り変わりの進み具合で決める。
        /// 時代ごとに色が変わるため、そのまま入れ替えると輪郭の光がその場で変わる。
        /// </summary>
        private void ApplyAtmosphere(float amount)
        {
            var color = Color.Lerp(atmosphereFrom, atmosphereTo, Mathf.Clamp01(amount));
            var tint = color;
            tint.a = 0.10f;
            atmosphereMaterial.color = tint;
            atmosphereMaterial.SetColor("_EmissionColor", color * 0.45f);
        }

        /// <summary>
        /// 移り変わる先の絵を渡す。シェーダーが今の絵とこれを混ぜる。
        /// 絵の組を2つ持たせるので、層を重ねずに移り変わらせられる。
        /// </summary>
        private static void AssignNextSurface(Material material, PlanetSurfaceBaker.Surface surface)
        {
            material.SetTexture("_MainTexNext", surface.Albedo);
            material.SetTexture("_EmissionMapNext", surface.Emission);
            material.SetTexture("_BumpMapNext", surface.Normal);
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
