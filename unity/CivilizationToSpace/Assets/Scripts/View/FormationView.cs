using CivilizationToSpace.Core;
using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 地球ができるまでの演出。ぶつかってくる小さな天体、大きな天体、散らばった破片。
    ///
    /// **これは仮説に基づく象徴表現である。** 月の成り立ちはジャイアントインパクト説を採っているが、
    /// 決着した事実ではない。質量・個数・速度・衝突エネルギー・角度を一切持たない。
    /// 大きさも速さも、画面で読み取れることだけを基準に決めた値である。
    /// </summary>
    public sealed class FormationView : MonoBehaviour
    {
        /// <summary>ぶつかってくる小さな天体の持ち数。</summary>
        private const int SwarmPoolSize = 22;

        /// <summary>散らばった破片の持ち数。</summary>
        private const int DebrisPoolSize = 40;

        /// <summary>小さな天体が現れる距離。地球の半径を1としたときの倍率。</summary>
        private const float SwarmStartRadius = 3.4f;

        /// <summary>破片の輪の半径。</summary>
        private const float DebrisRadius = 2.0f;

        private static readonly Color32 RockColor = new Color32(0x77, 0x6B, 0x60, 0xFF);
        private static readonly Color32 HotRockColor = new Color32(0xC8, 0x6A, 0x38, 0xFF);
        private static readonly Color32 DebrisColor = new Color32(0xA8, 0x92, 0x7C, 0xFF);

        private Transform swarmRoot;
        private Transform debrisRoot;
        private Transform impactorRoot;
        private GameObject impactor;

        private GameObject[] swarm;
        private float[] swarmProgress;
        private Vector3[] swarmDirections;
        private GameObject[] debris;

        private MotionSettings motion;
        private float earthRadius;

        private float swarmAmount;
        private float debrisAmount;
        private float impactorAmount;

        /// <summary>地球と衝突天体が画面へ入るために必要な半径。</summary>
        public float FramedRadius
        {
            get { return earthRadius * SwarmStartRadius * 1.1f; }
        }

        public void SetMotionSettings(MotionSettings settings)
        {
            motion = settings;
        }

        public void Build(float radius, HideFlags flags)
        {
            earthRadius = radius;

            swarmRoot = CreateRoot("Swarm", flags);
            debrisRoot = CreateRoot("Debris", flags);
            impactorRoot = CreateRoot("Impactor", flags);
            debrisRoot.localRotation = Quaternion.Euler(18f, 0f, 8f);

            BuildSwarm(flags);
            BuildDebris(flags);
            BuildImpactor(flags);

            Apply(null);
        }

        /// <summary>段階を写す。null を渡すと何も無い状態になる。</summary>
        public void Apply(FormationStage stage)
        {
            swarmAmount = stage != null ? (float)stage.Swarm : 0f;
            debrisAmount = stage != null ? (float)stage.Debris : 0f;
            impactorAmount = stage != null ? (float)stage.Impactor : 0f;

            var visibleSwarm = Mathf.RoundToInt(swarmAmount * SwarmPoolSize);
            for (var i = 0; i < swarm.Length; i++)
            {
                swarm[i].SetActive(i < visibleSwarm);
            }

            var visibleDebris = Mathf.RoundToInt(debrisAmount * DebrisPoolSize);
            for (var i = 0; i < debris.Length; i++)
            {
                debris[i].SetActive(i < visibleDebris);
            }

            impactor.SetActive(impactorAmount > 0.5f);
        }

        private void Update()
        {
            if (motion != null && motion.Reduced)
            {
                return;
            }

            if (debrisRoot != null)
            {
                debrisRoot.Rotate(Vector3.up, 26f * Time.deltaTime, Space.Self);
            }

            MoveSwarm();
            MoveImpactor();
        }

        /// <summary>
        /// 小さな天体を外から中心へ落とす。中心へ届いたら外から出し直す。
        /// 「ぶつかり続けている」ことだけを表す。回数を数えていない。
        /// </summary>
        private void MoveSwarm()
        {
            if (swarm == null)
            {
                return;
            }

            for (var i = 0; i < swarm.Length; i++)
            {
                if (!swarm[i].activeSelf)
                {
                    continue;
                }

                swarmProgress[i] += Time.deltaTime * (0.22f + (i % 5) * 0.05f);
                if (swarmProgress[i] >= 1f)
                {
                    swarmProgress[i] = 0f;
                    swarmDirections[i] = Random.onUnitSphere;
                }

                var distance = Mathf.Lerp(earthRadius * SwarmStartRadius, earthRadius * 0.95f, swarmProgress[i]);
                swarm[i].transform.localPosition = swarmDirections[i] * distance;
                swarm[i].transform.Rotate(Vector3.one, 90f * Time.deltaTime, Space.Self);
            }
        }

        /// <summary>大きな天体を斜めから近づける。ぶつかる瞬間は描かない。</summary>
        private void MoveImpactor()
        {
            if (impactor == null || !impactor.activeSelf)
            {
                return;
            }

            impactorRoot.Rotate(Vector3.up, 8f * Time.deltaTime, Space.Self);
            impactor.transform.Rotate(Vector3.one, 24f * Time.deltaTime, Space.Self);
        }

        private Transform CreateRoot(string name, HideFlags flags)
        {
            var root = new GameObject(name);
            root.hideFlags = flags;
            root.transform.SetParent(transform, false);
            return root.transform;
        }

        private void BuildSwarm(HideFlags flags)
        {
            swarm = new GameObject[SwarmPoolSize];
            swarmProgress = new float[SwarmPoolSize];
            swarmDirections = new Vector3[SwarmPoolSize];

            var material = CreateRockMaterial(RockColor, 0.25f, flags);

            for (var i = 0; i < SwarmPoolSize; i++)
            {
                var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name = "Planetesimal" + (i + 1);
                item.hideFlags = flags;
                SafeDestroy(item.GetComponent<Collider>());
                item.transform.SetParent(swarmRoot, false);

                var size = earthRadius * (0.05f + (i % 4) * 0.018f);
                item.transform.localScale = Vector3.one * size;
                item.transform.localRotation = Random.rotation;
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                swarm[i] = item;
                swarmProgress[i] = i / (float)SwarmPoolSize;
                swarmDirections[i] = Random.onUnitSphere;
            }
        }

        private void BuildDebris(HideFlags flags)
        {
            debris = new GameObject[DebrisPoolSize];
            var material = CreateRockMaterial(DebrisColor, 0.12f, flags);

            for (var i = 0; i < DebrisPoolSize; i++)
            {
                var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
                item.name = "Debris" + (i + 1);
                item.hideFlags = flags;
                SafeDestroy(item.GetComponent<Collider>());
                item.transform.SetParent(debrisRoot, false);

                // 輪として散らす。等間隔にすると人工物に見えるため、半径と高さをばらす。
                var angle = i * 137.5f * Mathf.Deg2Rad;
                var spread = 0.82f + (i % 7) * 0.055f;
                var lift = ((i % 5) - 2) * 0.06f;

                item.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * earthRadius * DebrisRadius * spread,
                    earthRadius * lift,
                    Mathf.Sin(angle) * earthRadius * DebrisRadius * spread);
                item.transform.localRotation = Random.rotation;
                item.transform.localScale = Vector3.one * (earthRadius * (0.028f + (i % 3) * 0.012f));
                item.GetComponent<Renderer>().sharedMaterial = material;
                item.SetActive(false);

                debris[i] = item;
            }
        }

        private void BuildImpactor(HideFlags flags)
        {
            var material = CreateRockMaterial(HotRockColor, 0.5f, flags);

            impactor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactor.name = "GiantImpactor";
            impactor.hideFlags = flags;
            SafeDestroy(impactor.GetComponent<Collider>());
            impactor.transform.SetParent(impactorRoot, false);

            // 地球より小さいが、ひと目で大きいと分かる大きさにする。
            impactor.transform.localScale = Vector3.one * (earthRadius * 1.05f);
            impactor.transform.localPosition = new Vector3(
                earthRadius * 2.3f, earthRadius * 0.85f, -earthRadius * 0.6f);
            impactor.GetComponent<Renderer>().sharedMaterial = material;
            impactor.SetActive(false);
        }

        private static Material CreateRockMaterial(Color color, float glow, HideFlags flags)
        {
            var material = new Material(Shader.Find("Standard"));
            material.hideFlags = flags;
            material.color = color;
            material.SetFloat("_Glossiness", 0.06f);
            material.SetFloat("_Metallic", 0f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * glow);
            return material;
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
