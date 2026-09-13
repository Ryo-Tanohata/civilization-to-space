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

        /// <summary>
        /// 段階を移ったときに、見た目が新しい値へ追いつくまでの時間（秒）。
        /// 1段階ぶんの時間（1倍速で4秒）より短くして、次へ進む前に落ち着くようにする。
        /// </summary>
        private const float BlendSeconds = 1.6f;

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

        /// <summary>
        /// ラグランジュ点 L4・L5 の位置。月から見て軌道上の前後60度にあたる。
        /// 地球・月・その点が正三角形をつくる、という関係だけを写している。
        /// </summary>
        private const float LagrangeDegrees = 60f;

        /// <summary>
        /// コロニーの円筒の長さ（半分）と太さ。
        ///
        /// **実物の比ではない。** オニール型の円筒は長さ数十kmで、月（直径3474km）の
        /// 1%ほどしかない。この画面では月の半径が0.58なので、実際の比なら0.003ほどになり
        /// 点にもならない。見える大きさまで拡げている。
        /// </summary>
        private const float ColonyHalfLength = 0.34f;

        private const float ColonyRadius = 0.075f;

        /// <summary>コロニーの自転（度／秒）。重力の代わりを自転で作る、という点を示す。</summary>
        private const float ColonySpinDegreesPerSecond = 42f;

        private static readonly Color32 Regolith = new Color32(0x8C, 0x88, 0x82, 0xFF);
        private static readonly Color32 Mare = new Color32(0x5A, 0x59, 0x58, 0xFF);
        private static readonly Color32 FacilityColor = new Color32(0xC8, 0xD2, 0xDC, 0xFF);
        private static readonly Color32 FacilityGlow = new Color32(0xFF, 0xD2, 0x93, 0xFF);
        private static readonly Color32 TransferColor = new Color32(0xE6, 0xEC, 0xF2, 0xFF);

        /// <summary>できたばかりの、まだ溶けている月の色。</summary>
        private static readonly Color32 MoltenColor = new Color32(0xFF, 0x4A, 0x10, 0xFF);

        /// <summary>いま溶けて見せている度合い。</summary>
        private float moltenAmount;

        private Transform orbit;
        private Transform body;
        private Transform spin;
        private Material surfaceMaterial;
        private Material facilityMaterial;

        private GameObject[] transfers;

        /// <summary>L4・L5 に置くコロニー。2つとも月と同じ枠にぶら下げ、一緒に回す。</summary>
        private Transform[] colonies;
        private Material colonyMaterial;
        private float colonyAmount;
        private float[] transferOffsets;
        private GameObject[] facilities;

        private MotionSettings motion;
        private Vector3 earthCenter;

        private float transferAmount;
        private float facilityAmount;
        private float lightAmount;
        private float stationAmount;

        /// <summary>目指す値。段階を移ると先にこちらが変わり、見た目があとから追いつく。</summary>
        private float targetTransfer;
        private float targetFacility;
        private float targetLight;
        private float targetStation;
        private float targetColony;

        /// <summary>持ち物の本来の大きさ。端数を大きさで表すときの基準にする。</summary>
        private Vector3[] transferScales;
        private Vector3[] facilityScales;

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

        /// <summary>
        /// 月を溶けた状態に見せる度合い。0で通常、1でもっとも赤い。
        ///
        /// 巨大衝突で飛び散った物質が集まってできた直後は、月もまだ熱かったとされる。
        /// 月面の絵はそのまま使い、自ら光る色だけを赤へ寄せる。
        /// </summary>
        public void SetMolten(float amount)
        {
            if (surfaceMaterial == null)
            {
                return;
            }

            var next = Mathf.Clamp01(amount);
            if (Mathf.Approximately(next, moltenAmount))
            {
                return;
            }

            moltenAmount = next;
            surfaceMaterial.SetColor("_EmissionColor", (Color)MoltenColor * (moltenAmount * 1.5f));
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

            // 溶けた月を見せるために発光を使う。既定は黒なので、通常の見え方は変わらない。
            surfaceMaterial = StandardMaterials.CreateOpaque(true);
            surfaceMaterial.hideFlags = flags;
            surfaceMaterial.SetFloat("_Glossiness", 0.04f);
            surfaceMaterial.SetFloat("_Metallic", 0f);
            surfaceMaterial.SetColor("_EmissionColor", Color.black);
            surfaceMaterial.mainTexture = BakeSurface(flags);

            var sphere = PrimitiveMeshes.Create(PrimitiveType.Sphere, "MoonSurface", flags);
            sphere.transform.SetParent(spin, false);
            sphere.transform.localScale = Vector3.one * (MoonRadius * 2f);
            sphere.GetComponent<Renderer>().sharedMaterial = surfaceMaterial;

            BuildFacilities(flags);
            BuildTransfers(flags);
            BuildStation(flags);
            BuildColonies(flags);

            Apply(null);
        }

        /// <summary>段階を写す。null を渡すと何も無い状態になる。</summary>
        /// <summary>
        /// 段階を写す。null を渡すと何も無い状態になる。
        ///
        /// ここでは目指す値を置くだけで、すぐには反映しない。
        /// 段階を移るたびに拠点や明かりが瞬時に増減すると、
        /// 時系列が地続きに見えず、別の絵へ切り替わったように見えるためである。
        /// 実際の見た目は <see cref="Update"/> が時間をかけて寄せていく。
        /// </summary>
        public void Apply(MoonPhase phase)
        {
            targetTransfer = phase != null ? (float)phase.Transfer : 0f;
            targetFacility = phase != null ? (float)phase.Facility : 0f;
            targetLight = phase != null ? (float)phase.SurfaceLights : 0f;
            targetStation = phase != null ? (float)phase.OrbitStation : 0f;
            targetColony = phase != null ? (float)phase.LagrangeColony : 0f;

            // 動きを減らしているときは、途中を見せずにその段階の姿にする。
            if (motion != null && motion.Reduced)
            {
                transferAmount = targetTransfer;
                facilityAmount = targetFacility;
                lightAmount = targetLight;
                stationAmount = targetStation;
                colonyAmount = targetColony;
            }

            ApplyAmounts();
        }

        /// <summary>いまの値を見た目へ写す。数は端数ぶんだけ大きさで表す。</summary>
        private void ApplyAmounts()
        {
            ApplyStation();
            ApplyColonies();

            ApplyPool(transfers, transferAmount, TransferPoolSize, transferScales);
            ApplyPool(facilities, facilityAmount, FacilityPoolSize, facilityScales);

            facilityMaterial.SetColor("_EmissionColor", (Color)FacilityGlow * (lightAmount * 1.4f));
        }

        /// <summary>
        /// 持ち数のうち、いくつを出すかを決める。
        ///
        /// 個数は整数なので、そのまま増減させると1個ずつ現れて目に付く。
        /// 端数は「いま生えかけの1個」の大きさで表し、少しずつ育つように見せる。
        /// </summary>
        private static void ApplyPool(GameObject[] pool, float amount, int size, Vector3[] baseScales)
        {
            var exact = Mathf.Clamp01(amount) * size;
            var whole = Mathf.FloorToInt(exact);
            var partial = exact - whole;

            for (var i = 0; i < pool.Length; i++)
            {
                if (i < whole)
                {
                    pool[i].SetActive(true);
                    pool[i].transform.localScale = baseScales[i];
                }
                else if (i == whole && partial > 0.02f)
                {
                    pool[i].SetActive(true);
                    pool[i].transform.localScale = baseScales[i] * partial;
                }
                else
                {
                    pool[i].SetActive(false);
                }
            }
        }

        /// <summary>
        /// コロニーの出し入れ。強さが上がるほど大きく見せる。
        /// 大きさは見せ方であって、寸法でも建設量でもない。
        /// </summary>
        private void ApplyColonies()
        {
            if (colonies == null)
            {
                return;
            }

            var visible = colonyAmount > 0.02f;
            var scale = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(colonyAmount));

            for (var i = 0; i < colonies.Length; i++)
            {
                if (colonies[i] == null)
                {
                    continue;
                }

                colonies[i].gameObject.SetActive(visible);
                colonies[i].localScale = Vector3.one * scale;
            }

            if (colonyMaterial != null)
            {
                colonyMaterial.SetColor("_EmissionColor", (Color)FacilityGlow * (colonyAmount * 0.5f));
            }
        }

        private void Update()
        {
            var reduced = motion != null && motion.Reduced;
            if (reduced)
            {
                return;
            }

            BlendAmounts();

            if (orbit != null)
            {
                orbit.Rotate(Vector3.up, OrbitDegreesPerSecond * SceneClock.Delta, Space.Self);
            }

            if (spin != null)
            {
                spin.Rotate(Vector3.up, SpinDegreesPerSecond * SceneClock.Delta, Space.Self);
            }

            if (stationOrbit != null && stationAmount > 0f)
            {
                stationOrbit.Rotate(Vector3.up, StationDegreesPerSecond * SceneClock.Delta, Space.Self);
            }

            // コロニーは自転させる。重力の代わりを自転で作る、という点がこの形の要だからである。
            if (colonies != null && colonyAmount > 0f)
            {
                var turn = ColonySpinDegreesPerSecond * SceneClock.Delta;
                for (var i = 0; i < colonies.Length; i++)
                {
                    if (colonies[i] != null)
                    {
                        colonies[i].Rotate(Vector3.up, turn, Space.Self);
                    }
                }
            }

            MoveTransfers();
        }

        /// <summary>
        /// 見た目の値を、目指す値へ少しずつ寄せる。
        /// 段階のあいだを地続きにして、時系列として読めるようにするためである。
        /// </summary>
        private void BlendAmounts()
        {
            var step = SceneClock.Delta / BlendSeconds;
            var changed = false;

            Approach(ref transferAmount, targetTransfer, step, ref changed);
            Approach(ref facilityAmount, targetFacility, step, ref changed);
            Approach(ref lightAmount, targetLight, step, ref changed);
            Approach(ref stationAmount, targetStation, step, ref changed);
            Approach(ref colonyAmount, targetColony, step, ref changed);

            if (changed)
            {
                ApplyAmounts();
            }
        }

        private static void Approach(ref float value, float target, float step, ref bool changed)
        {
            if (Mathf.Approximately(value, target))
            {
                return;
            }

            value = Mathf.MoveTowards(value, target, step);
            changed = true;
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

                transferOffsets[i] += SceneClock.Delta * speed;
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

        /// <summary>
        /// ラグランジュ点 L4・L5 に、回転する円筒形の居住地を置く。
        ///
        /// 月と同じ枠（MoonOrbit）にぶら下げる。枠ごと回るので、月との角度が60度に保たれる。
        /// **これは「そこに置ける」という仮想シナリオの絵であって、安定性を計算していない。**
        /// 計算で確かめる版は FormationSim シーン（SimRoot）にある。そちらでは、
        /// 月の質量比が 0.0385 を超えると留まらなくなることまで出る。
        /// </summary>
        private void BuildColonies(HideFlags flags)
        {
            colonyMaterial = StandardMaterials.CreateOpaque(true);
            colonyMaterial.hideFlags = flags;
            colonyMaterial.color = FacilityColor;
            colonyMaterial.SetFloat("_Glossiness", 0.6f);
            colonyMaterial.SetFloat("_Metallic", 0.5f);
            colonyMaterial.EnableKeyword("_EMISSION");
            colonyMaterial.SetColor("_EmissionColor", (Color)FacilityGlow * 0.35f);

            colonies = new Transform[2];

            for (var i = 0; i < colonies.Length; i++)
            {
                var sign = i == 0 ? 1f : -1f;

                var host = new GameObject(i == 0 ? "ColonyL4" : "ColonyL5");
                host.hideFlags = flags;
                host.transform.SetParent(orbit, false);
                host.transform.localPosition =
                    Quaternion.Euler(0f, sign * LagrangeDegrees, 0f) * new Vector3(OrbitRadius, 0f, 0f);
                host.SetActive(false);

                colonies[i] = host.transform;

                // 胴体。円柱の長い軸はYなので、そのまま軌道の面に垂直へ立てる。
                // 太陽を置いていないための決めで、実際のオニール型は軸を太陽へ向ける。
                AddColonyPart(host.transform, flags, "Hull", PrimitiveType.Cylinder,
                    Vector3.zero, Quaternion.identity,
                    new Vector3(ColonyRadius * 2f, ColonyHalfLength, ColonyRadius * 2f));

                // 両端の輪。円筒に見せるための縁である。
                for (var end = 0; end < 2; end++)
                {
                    AddColonyPart(host.transform, flags, "Rim" + (end + 1), PrimitiveType.Cylinder,
                        new Vector3(0f, end == 0 ? ColonyHalfLength : -ColonyHalfLength, 0f),
                        Quaternion.identity,
                        new Vector3(ColonyRadius * 2.5f, ColonyHalfLength * 0.06f, ColonyRadius * 2.5f));
                }

                // 外の鏡。日照を取り込む板の代わりで、面積も発電量も表さない。
                for (var mirror = 0; mirror < 2; mirror++)
                {
                    AddColonyPart(host.transform, flags, "Mirror" + (mirror + 1), PrimitiveType.Cube,
                        new Vector3(mirror == 0 ? ColonyRadius * 2.6f : -ColonyRadius * 2.6f, 0f, 0f),
                        Quaternion.Euler(0f, 0f, mirror == 0 ? 22f : -22f),
                        new Vector3(ColonyRadius * 2.2f, ColonyHalfLength * 1.1f, 0.012f));
                }
            }
        }

        private void AddColonyPart(
            Transform parent, HideFlags flags, string name, PrimitiveType shape,
            Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var part = PrimitiveMeshes.Create(shape, name, flags);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = colonyMaterial;
        }

        /// <summary>拠点の部品をひとつ足す。当たり判定は要らないので外す。</summary>
        private void AddStationPart(
            HideFlags flags, string name, PrimitiveType shape,
            Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var part = PrimitiveMeshes.Create(shape, name, flags);
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
            transferScales = new Vector3[TransferPoolSize];

            var material = StandardMaterials.CreateOpaque(true);
            material.hideFlags = flags;
            material.color = TransferColor;
            material.SetColor("_EmissionColor", (Color)TransferColor * 0.5f);

            for (var i = 0; i < TransferPoolSize; i++)
            {
                var item = PrimitiveMeshes.Create(PrimitiveType.Capsule, "Transfer" + (i + 1), flags);
                item.transform.SetParent(transform, false);
                item.transform.localScale = new Vector3(0.11f, 0.22f, 0.11f);
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                transfers[i] = item;
                transferOffsets[i] = i * (2f / TransferPoolSize);
                transferScales[i] = item.transform.localScale;
            }
        }

        private void BuildFacilities(HideFlags flags)
        {
            facilities = new GameObject[FacilityPoolSize];
            facilityScales = new Vector3[FacilityPoolSize];

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

                var item = PrimitiveMeshes.Create(PrimitiveType.Cube, "Facility" + (i + 1), flags);
                item.transform.SetParent(spin, false);
                item.transform.localPosition = direction * (MoonRadius * 1.01f);
                item.transform.localRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
                item.transform.localScale = new Vector3(0.10f, 0.055f, 0.10f);
                item.GetComponent<Renderer>().sharedMaterial = facilityMaterial;
                item.SetActive(false);

                facilities[i] = item;
                facilityScales[i] = item.transform.localScale;
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
    }
}
