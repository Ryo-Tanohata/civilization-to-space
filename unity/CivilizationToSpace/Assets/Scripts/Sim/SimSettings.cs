using System;
using UnityEngine;

namespace CivilizationToSpace.Sim
{
    /// <summary>
    /// 計算の条件。すべて単位のない相対値である。
    ///
    /// **実際の質量・距離・年数ではない。** 地球の最終質量を1、その半径を
    /// <see cref="RadiusUnit"/> とした系で、見て分かる速さに時間を縮めてある。
    /// したがってここから年代も規模も読み取れない。読み取れるのは順序と、
    /// 「条件を変えると結果が変わる」という関係だけである。
    ///
    /// 画面から変えられるようにしてあるのは、どの値がどこに効くかを
    /// 手で確かめられるようにするためである。
    /// </summary>
    [Serializable]
    public sealed class SimSettings
    {
        [Header("はじまりの雲")]
        [Tooltip("同じ種なら同じ形成過程になります。")]
        public int Seed = 20260912;

        [Tooltip("最初に置く微惑星の数。多いほど重くなります。")]
        [Range(60, 900)]
        public int PlanetesimalCount = 380;

        [Tooltip("雲の広がり。地球半径の何倍かではなく、系の長さの単位です。")]
        public float CloudRadius = 26f;

        [Tooltip("雲の厚み。0にすると完全な平面になります。")]
        public float CloudThickness = 4.5f;

        [Tooltip("最初の回転。1に近いほど落ちてこず、回り続けます。")]
        [Range(0f, 1.2f)]
        public float SpinFraction = 0.52f;

        [Header("重力")]
        [Tooltip("重力の強さ。大きいほど速く集まります。")]
        public float Gravity = 150f;

        [Tooltip("重力をやわらげる長さ。近づきすぎたときに力が跳ね上がるのを防ぎます。")]
        public float Softening = 0.45f;

        [Tooltip("集積のあいだだけ効く抵抗。0にすると、残った数個が互いを回り続けて合体が終わりません。")]
        [Range(0f, 0.6f)]
        public float GasDrag = 0.16f;

        [Header("大きさ")]
        [Tooltip("質量の合計。地球1個ぶんを1とします。")]
        public float TotalMass = 1f;

        [Tooltip("質量1の天体の半径。半径は質量の3乗根に比例させます（密度を一定とみなす）。")]
        public float RadiusUnit = 2.2f;

        [Tooltip("触れたとみなす距離の倍率。1.0で半径の和ちょうどです。")]
        [Range(0.8f, 1.6f)]
        public float MergeSlack = 1.05f;

        [Header("原始地球とジャイアントインパクト")]
        [Tooltip("ひとつの塊がこの割合を超えたら、原始地球ができたとみなします。")]
        [Range(0.4f, 0.98f)]
        public float ProtoEarthMassFraction = 0.78f;

        [Tooltip("原始地球ができてから衝突天体が現れるまでの待ち時間。")]
        public float ImpactorDelay = 2.0f;

        [Tooltip("衝突天体の質量。原始地球に対する割合です。")]
        [Range(0.02f, 0.4f)]
        public float ImpactorMassFraction = 0.13f;

        [Tooltip("衝突天体が現れる距離。")]
        public float ImpactorStartDistance = 34f;

        [Tooltip("初速。1.0でその距離の脱出速度と同じです。")]
        [Range(0.3f, 1.6f)]
        public float ImpactorSpeedFactor = 1.0f;

        [Tooltip("狙いのずらし幅。0で正面衝突、大きいほどかすめる当たりになります。")]
        [Range(0f, 2f)]
        public float ImpactParameter = 0.8f;

        [Header("放出される破片")]
        [Tooltip("破片になる質量の割合。地球と衝突天体の合計に対する割合です。")]
        [Range(0.002f, 0.08f)]
        public float EjectaMassFraction = 0.022f;

        [Tooltip("破片の数。")]
        [Range(10, 240)]
        public int EjectaCount = 40;

        [Tooltip("破片を置く内側の半径。地球半径の何倍か。近すぎると潮汐に負けて月になりません。")]
        public float EjectaInnerRadius = 4.0f;

        [Tooltip("破片を置く外側の半径。地球半径の何倍か。")]
        public float EjectaOuterRadius = 6.0f;

        [Tooltip("破片を広げる角度。小さいほど早くひとつに集まります。")]
        [Range(30f, 360f)]
        public float EjectaArcDegrees = 120f;

        [Tooltip("破片の速さ。1.0でその半径の円軌道と同じです。")]
        [Range(0.6f, 1.4f)]
        public float EjectaSpeedFactor = 1.0f;

        [Tooltip("破片どうしの粘り。軌道を円くします。0にすると破片が地球へ落ちます。")]
        [Range(0f, 2f)]
        public float DiskViscosity = 0.8f;

        [Tooltip("破片どうしが集まる力の倍率。1のままだと、見ていられる時間では月になりません。")]
        [Range(1f, 120f)]
        public float DebrisAttraction = 60f;

        [Header("時間の刻み")]
        [Tooltip("1回の計算で進める時間。小さいほど正確で、重くなります。")]
        public float StepSize = 0.015f;

        [Tooltip("1フレームで計算する回数の上限。増やすと遅い端末で処理落ちします。")]
        [Range(1, 8)]
        public int MaxStepsPerFrame = 3;

        /// <summary>破片が1つにまとまったと判定するまでの猶予。</summary>
        public float SettleDelay = 3f;

        [Header("月の落ち着きとラグランジュ点")]
        [Tooltip("月ができた直後の軌道を落ち着かせる強さ。0にすると月が落ちるか飛び去ります。")]
        [Range(0f, 1.5f)]
        public float TidalCircularise = 0.35f;

        [Tooltip("月を運ぶ先。地球半径の何倍か。")]
        public float MoonTargetRadii = 12f;

        [Tooltip("ラグランジュ点にコロニーを置きます。")]
        public bool ColonyEnabled = true;

        [Tooltip("コロニーの質量。運動を乱さないよう、事実上の試験粒子にします。")]
        public float ColonyMassFraction = 1e-7f;

        [Tooltip("地球と月が落ち着いてからコロニーを置くまでの待ち時間。")]
        public float ColonyDelay = 1.5f;

        [Tooltip("この離心率より小さくなったら置きます。大きいまま置くと留まりません。")]
        [Range(0.001f, 0.1f)]
        public float ColonyMaxEccentricity = 0.005f;

        [Tooltip("落ち着くのを待つ上限。過ぎたら、落ち着いていなくても置きます。")]
        public float ColonySettleTimeout = 400f;

        public SimSettings Clone()
        {
            return (SimSettings)MemberwiseClone();
        }
    }
}
