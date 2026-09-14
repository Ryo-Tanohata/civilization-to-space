using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 太陽役の平行光。向きを時間で回し、昼夜の境目を地表の上で動かす。
    ///
    /// 光源が固定されていると、地球が自転していても明暗の境目が動かないため、
    /// 回っていることが読み取れない。ここで光の向きを動かし、EarthView 側で
    /// 地軸を傾けておくことで、季節によってどちらの極が照らされるかも入れ替わる。
    ///
    /// 公転のデフォルメについて:
    ///   本物の公転は必ず新月にあたる位相を通り、そこで地球は真っ暗になる。
    ///   このデモは時代ごとの地表を観察するためのものなので、
    ///   地表が読めなくなる時間帯ができるのは目的に反する。
    ///   そのため太陽の向きは、既定のカメラから地球へ向かう向きを軸とする
    ///   円錐の縁を一周させる。光は一周してまわり、季節も入れ替わるが、
    ///   照らされる割合は一定に保たれる。
    ///   本物と同じ位相まで出す場合は SwingDegrees を 90 にする。
    ///
    /// 色と強度はシーンの設定をそのまま使う。ここでは向きだけを扱う。
    /// シーンのアセットは書き換えない。AppRoot が再生中にだけ付ける。
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class SunLight : MonoBehaviour
    {
        /// <summary>公転の速さ（度／秒）。1周360秒。EarthView の自転45秒に対して8日／年にあたる。</summary>
        private const float OrbitDegreesPerSecond = 1f;

        /// <summary>光の向きが描く円錐の半頂角（度）。照らされる割合はこの値で決まる。</summary>
        private const float SwingDegrees = 55f;

        /// <summary>
        /// 円錐の軸。既定のカメラ位置から地球へ向かう向き。
        /// カメラを回すと位相が変わって見えるが、それは光ではなく見る側が動いたためである。
        /// </summary>
        private static readonly Vector3 ConeAxis = new Vector3(-0.3065f, 0f, 0.9519f);

        private MotionSettings motion;
        private float orbitAngle;

        /// <summary>動きの設定を渡す。渡さない場合は常に動く。</summary>
        public void SetMotionSettings(MotionSettings settings)
        {
            motion = settings;
        }

        /// <summary>
        /// 公転の角度（度）。0で従来の斜め上からの向きになり、そこから一周する。
        /// 点検ツールが季節を決め打ちで作れるように公開する。
        /// </summary>
        public float OrbitAngle
        {
            get { return orbitAngle; }
            set
            {
                orbitAngle = Mathf.Repeat(value, 360f);
                Apply();
            }
        }

        private void Awake()
        {
            Apply();
        }

        private void Update()
        {
            if (motion != null && motion.Reduced)
            {
                return;
            }

            orbitAngle = Mathf.Repeat(orbitAngle + OrbitDegreesPerSecond * Time.deltaTime, 360f);
            Apply();
        }

        /// <summary>いまの公転角から光の向きを決める。</summary>
        public void Apply()
        {
            var axis = ConeAxis.normalized;

            // 円錐の縁を作るための、軸に直交する向き。
            // 軸が真上を向いていても縮退しないよう、種になる向きを選び直す。
            var seed = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            var perpendicular = Vector3.ProjectOnPlane(seed, axis).normalized;

            var swing = SwingDegrees * Mathf.Deg2Rad;

            // 公転角0を、光が斜め上から差す向きにする。従来のシーンの見え方に近い位置から始まる。
            var start = axis * Mathf.Cos(swing) - perpendicular * Mathf.Sin(swing);
            var direction = Quaternion.AngleAxis(orbitAngle, axis) * start;

            // 平行光は transform.forward の向きへ進む。地球へ向かう向きをそのまま入れる。
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
