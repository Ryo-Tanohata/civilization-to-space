using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 地球が画面の左側に収まるようカメラを置き直す。
    ///
    /// カメラの位置を固定値で持つと、ウィンドウの縦横比が変わったときに地球がはみ出す。
    /// 画面の大きさから毎回、距離と横のずらし量を求め直す。
    ///
    /// カメラは回さない。向きは常に正面のままである。視点操作はS4で足す。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class EarthFraming : MonoBehaviour
    {
        /// <summary>地球の中心。原点に置く前提だが、変えられるようにしておく。</summary>
        public Vector3 Target { get; set; }

        /// <summary>地球の半径。EarthView と揃える。</summary>
        public float Radius { get; set; }

        /// <summary>左側の領域に対して、地球の直径が占める割合。</summary>
        private const float WidthShare = 0.72f;

        /// <summary>画面の高さに対して、地球の直径が占める割合の上限。見出しと操作帯を避ける。</summary>
        private const float HeightShare = 0.54f;

        /// <summary>説明パネルが極端に狭い画面を占めすぎないよう、左側の下限を決める。</summary>
        private const float MinimumLeftShare = 0.34f;

        private Camera view;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            view = GetComponent<Camera>();
            Radius = Radius <= 0f ? 2.2f : Radius;
        }

        private void LateUpdate()
        {
            // 描画先の大きさが変わったときだけ置き直す。毎フレーム動かす必要はない。
            int width;
            int height;
            ReadSize(out width, out height);

            if (width == lastWidth && height == lastHeight)
            {
                return;
            }

            lastWidth = width;
            lastHeight = height;
            Apply();
        }

        /// <summary>
        /// 描画先の大きさ。Screen ではなくカメラの値を見る。
        /// カメラが RenderTexture へ描くとき、Screen は実際の描画先と食い違うためである。
        /// </summary>
        private void ReadSize(out int width, out int height)
        {
            if (view == null)
            {
                view = GetComponent<Camera>();
            }

            width = view != null && view.pixelWidth > 0 ? view.pixelWidth : Screen.width;
            height = view != null && view.pixelHeight > 0 ? view.pixelHeight : Screen.height;
        }

        public void Apply()
        {
            if (view == null)
            {
                view = GetComponent<Camera>();
            }

            int pixelWidth;
            int pixelHeight;
            ReadSize(out pixelWidth, out pixelHeight);

            var width = Mathf.Max(1, pixelWidth);
            var height = Mathf.Max(1, pixelHeight);

            var leftShare = Mathf.Max(MinimumLeftShare, 1f - UiFactory.SidePanelWidthFraction);
            var leftPixels = width * leftShare;

            var diameterPixels = Mathf.Min(leftPixels * WidthShare, height * HeightShare);
            if (diameterPixels <= 0f)
            {
                return;
            }

            // 焦点距離（画素）。画面の高さと縦画角から決まる。
            var focal = height * 0.5f / Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // 球の輪郭は接線でできるため、見かけの半径は R/距離 ではなく asin(R/距離) で決まる。
            // 画面に占めたい直径から見かけの半角を出し、そこから距離を逆算する。
            var halfAngle = Mathf.Atan(diameterPixels * 0.5f / focal);
            var sine = Mathf.Sin(halfAngle);
            if (sine <= 0.0001f)
            {
                return;
            }

            var distanceToCenter = Radius / sine;

            // 光軸から外れた球の輪郭は、中心の射影よりさらに外側へずれる。
            // 左側の領域の中央へ輪郭の中心が来る方位角を二分法で求める。
            var targetCenter = leftPixels * 0.5f - width * 0.5f;
            var low = -Mathf.PI * 0.5f + halfAngle + 0.01f;
            var high = 0f;
            for (var i = 0; i < 24; i++)
            {
                var middle = (low + high) * 0.5f;
                var projected = focal * (Mathf.Tan(middle + halfAngle) + Mathf.Tan(middle - halfAngle)) * 0.5f;
                if (projected < targetCenter)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            var azimuth = (low + high) * 0.5f;

            // カメラは常に +z を向く。球はカメラから見て方位角 azimuth の方向にある。
            var direction = new Vector3(Mathf.Sin(azimuth), 0f, Mathf.Cos(azimuth));
            transform.rotation = Quaternion.identity;
            transform.position = Target - direction * distanceToCenter;
        }
    }
}
