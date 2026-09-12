using UnityEngine;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 地球が画面の左側に収まるようカメラを置く。視点の向きと寄りも受け取る。
    ///
    /// カメラの位置を固定値で持つと、ウィンドウの縦横比が変わったときに地球がはみ出す。
    /// 描画先の大きさから毎回、距離と見かけの位置を求め直す。
    ///
    /// 球の輪郭は接線でできるため、見かけの半径は 半径÷距離 ではなく asin(半径÷距離) で決まる。
    /// 単純な比で計算すると1割ほど小さく見積もり、実際にははみ出す。
    ///
    /// 時代を切り替えてもカメラは動かさない。動かすのは描画先の大きさが変わったときと、
    /// 利用者が視点を操作したときだけである。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class EarthFraming : MonoBehaviour
    {
        /// <summary>地球の中心。</summary>
        public Vector3 Target { get; set; }

        /// <summary>地球の半径。EarthView と揃える。</summary>
        public float Radius { get; set; }

        /// <summary>左右の向き（度）。0が正面。</summary>
        public float Yaw { get; set; }

        /// <summary>上下の向き（度）。端で止める。</summary>
        public float Pitch { get; set; }

        /// <summary>寄りの倍率。1が既定。小さいほど近づく。</summary>
        public float Zoom { get; set; }

        public const float MinimumPitch = -70f;
        public const float MaximumPitch = 70f;
        public const float MinimumZoom = 0.55f;
        public const float MaximumZoom = 2.4f;

        /// <summary>
        /// 月まで画面へ入れるときに、収めたい半径。
        /// 引きの倍率で下げると、画面の大きさによって収まり方が変わる。
        /// 収めたいものの大きさを変えるほうが確実である。
        /// </summary>
        public float WideRadius { get; set; }

        /// <summary>
        /// 説明を出しているかどうか。出していないときは地球を中央に置き、大きく見せる。
        /// </summary>
        public bool SidePanelVisible { get; set; }

        /// <summary>月まで画面へ入れる引き。通常より外まで下がれるようにする。</summary>
        public bool WideMode { get; set; }

        /// <summary>使える幅に対して、地球の直径が占める割合。</summary>
        private const float WidthShare = 0.72f;

        /// <summary>画面の高さに対して、地球の直径が占める割合の上限。操作帯を避ける。</summary>
        private const float HeightShare = 0.50f;

        /// <summary>説明を出していないときの割合。操作帯の上まで使い切る。</summary>
        private const float WideWidthShare = 0.92f;
        private const float WideHeightShare = 0.80f;

        /// <summary>説明パネルが極端に狭い画面を占めすぎないよう、左側の下限を決める。</summary>
        private const float MinimumLeftShare = 0.34f;

        private Camera view;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            view = GetComponent<Camera>();

            if (Radius <= 0f)
            {
                Radius = 2.2f;
            }

            if (Zoom <= 0f)
            {
                Zoom = 1f;
            }
        }

        /// <summary>視点を既定へ戻す。</summary>
        public void ResetView()
        {
            Yaw = 0f;
            Pitch = 0f;
            Zoom = 1f;
            Apply();
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

            Pitch = Mathf.Clamp(Pitch, MinimumPitch, MaximumPitch);
            Zoom = Mathf.Clamp(Zoom <= 0f ? 1f : Zoom, MinimumZoom, MaximumZoom);

            int pixelWidth;
            int pixelHeight;
            ReadSize(out pixelWidth, out pixelHeight);

            var width = Mathf.Max(1, pixelWidth);
            var height = Mathf.Max(1, pixelHeight);

            var leftShare = SidePanelVisible
                ? Mathf.Max(MinimumLeftShare, 1f - UiFactory.SidePanelWidthFraction)
                : 1f;
            var leftPixels = width * leftShare;

            var widthShare = SidePanelVisible ? WidthShare : WideWidthShare;
            var heightShare = SidePanelVisible ? HeightShare : WideHeightShare;
            var diameterPixels = Mathf.Min(leftPixels * widthShare, height * heightShare);
            if (diameterPixels <= 0f)
            {
                return;
            }

            // 焦点距離（画素）。画面の高さと縦画角から決まる。
            var focal = height * 0.5f / Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad);

            // 画面に占めたい直径から見かけの半角を出し、そこから距離を逆算する。
            var halfAngle = Mathf.Atan(diameterPixels * 0.5f / focal);
            var sine = Mathf.Sin(halfAngle);
            if (sine <= 0.0001f)
            {
                return;
            }

            // 月まで入れるときは、収めたい半径を大きく取る。
            var framed = WideMode && WideRadius > 0f ? WideRadius : Radius;
            var distance = framed / sine * Zoom;

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

            var azimuth = (low + high) * 0.5f * Mathf.Rad2Deg;

            // 地球から見たカメラの方向。既定では地球の手前にいる。
            var direction = Quaternion.Euler(Pitch, Yaw, 0f) * Vector3.back;
            transform.position = Target + direction * distance;

            // 地球へ正対させたあと、方位角のぶんだけ右へ振る。
            // カメラが右を向くと、地球は画面の左へ寄る。
            transform.rotation = Quaternion.LookRotation(-direction, Vector3.up)
                                 * Quaternion.AngleAxis(-azimuth, Vector3.up);
        }
    }
}
