using UnityEngine;
using UnityEngine.UI;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 場面の切り替わりをなめらかにする。
    ///
    /// **切り替わりが唐突だった。** 時代が移るたびに、地表の風景は
    /// 丸ごと作り直され、カメラも一瞬で別の場所へ飛んでいた。
    /// 前の絵と次の絵のあいだに何も無いので、つながって見えない。
    ///
    /// **やり方は、前の絵を1枚撮って重ね、薄れさせるだけ。**
    /// 作り直す直前にカメラの絵を控えておき、それを画面いっぱいに貼る。
    /// 裏では新しい場面ができあがっている。貼った絵を薄くしていくと、
    /// 前の絵から次の絵へ溶けるように移る。
    ///
    /// 暗転を挟む手もあるが、昼の明るい風景では重く感じる。
    /// 溶明なら、明るさを保ったまま移り変わる。
    ///
    /// **実時間で薄れさせる。** 再生を止めていても、速度を変えていても、
    /// 切り替わりの見え方は同じにする。場面の中の時間ではなく、
    /// 見ている人の時間に属する動きだからである。
    /// </summary>
    public sealed class SceneDissolve : MonoBehaviour
    {
        /// <summary>
        /// 薄れきるまでの秒数。
        ///
        /// 短いと切り替わりに気づけず、長いと次の場面が出るのを待たされる。
        /// 1段階のいちばん短い長さ（8倍速で0.5秒）より短く取る。
        /// </summary>
        private const float Seconds = 0.42f;

        private RawImage image;
        private RenderTexture capture;
        private float remaining;

        /// <summary>いま溶明の途中か。</summary>
        public bool Running
        {
            get { return remaining > 0f; }
        }

        /// <summary>
        /// 画面に貼る板を用意する。<see cref="DemoHud"/> の画布の中に置き、
        /// 操作の帯より後ろへ回す。操作は常に見えていなければならない。
        /// </summary>
        public void Attach(RectTransform canvasRoot)
        {
            var host = new GameObject("SceneDissolve", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)host.transform;
            rect.SetParent(canvasRoot, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // いちばん後ろへ。ここより手前に操作の帯が並ぶ。
            rect.SetAsFirstSibling();

            image = host.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            image.enabled = false;
        }

        /// <summary>
        /// いまのカメラの絵を控えて、溶明を始める。
        /// **場面を作り直す直前に呼ぶ。** 呼んだあとで作り直す。
        /// </summary>
        public void Begin(Camera camera)
        {
            if (image == null || camera == null)
            {
                return;
            }

            // **すでに薄れている途中なら、撮り直さない。**
            // 視点の切り替えは、地球を消してから風景を作る二段構えになっている。
            // 途中で撮り直すと、何も無い一瞬を控えてしまい、前の絵が消える。
            if (remaining > 0f)
            {
                return;
            }

            var width = Mathf.Max(16, Screen.width);
            var height = Mathf.Max(16, Screen.height);

            // 画面の大きさが変わったら取り直す。使い回すと縦横が合わない。
            if (capture != null && (capture.width != width || capture.height != height))
            {
                Release();
            }

            if (capture == null)
            {
                capture = new RenderTexture(width, height, 24, RenderTextureFormat.Default);
                capture.hideFlags = HideFlags.DontSave;
                capture.Create();
            }

            var previous = camera.targetTexture;
            camera.targetTexture = capture;
            camera.Render();
            camera.targetTexture = previous;

            image.texture = capture;
            image.enabled = true;
            image.color = new Color(1f, 1f, 1f, 1f);
            remaining = Seconds;
        }

        private void Update()
        {
            if (remaining <= 0f || image == null)
            {
                return;
            }

            // 場面の時計ではなく実時間で進める。止めていても薄れる。
            remaining -= Time.unscaledDeltaTime;
            if (remaining <= 0f)
            {
                remaining = 0f;
                image.enabled = false;
                image.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            // 端をゆるめる。まっすぐ薄くすると、始まりと終わりが角張って見える。
            var t = 1f - remaining / Seconds;
            image.color = new Color(1f, 1f, 1f, 1f - Mathf.SmoothStep(0f, 1f, t));
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Release()
        {
            if (capture == null)
            {
                return;
            }

            if (image != null)
            {
                image.texture = null;
            }

            capture.Release();
            DestroyImmediate(capture);
            capture = null;
        }
    }
}
