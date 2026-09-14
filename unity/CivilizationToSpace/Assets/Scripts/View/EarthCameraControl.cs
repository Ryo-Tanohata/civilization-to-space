using UnityEngine;
using UnityEngine.EventSystems;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// ドラッグで視点を回し、ホイールで寄る。
    ///
    /// ブラウザモック仕様は「カメラは固定」と定めているが、3D空間では、
    /// ある程度の視点移動がないと球の形が読めない。R1-P1実装順序に逸脱3として記録済み。
    ///
    /// 仰角と距離は端で止める。裏返ったり、球の内側へ入り込んだりしないようにする。
    /// 視点の操作は再生を止めない。再生を止めるのは時代の移動だけという規則を保つ。
    /// UIの上で押し始めたドラッグは無視する。ボタンを押したつもりが視点が回るのを防ぐ。
    /// </summary>
    [RequireComponent(typeof(EarthFraming))]
    public sealed class EarthCameraControl : MonoBehaviour
    {
        /// <summary>画面の高さ1つ分のドラッグで何度回すか。</summary>
        private const float DegreesPerScreenHeight = 220f;

        /// <summary>ホイール1目盛りあたりの寄りの割合。</summary>
        private const float ZoomPerNotch = 0.12f;

        /// <summary>
        /// 2本指の間隔が画面の高さ1つ分変わったときに、どれだけ寄るか。
        /// ホイールと同じ感覚になるよう、指を広げると寄り、狭めると引く。
        /// </summary>
        private const float ZoomPerScreenHeightPinch = 1.6f;

        private EarthFraming framing;
        private bool dragging;
        private Vector3 lastPointer;

        private bool pinching;
        private float lastPinchDistance;

        private void Awake()
        {
            framing = GetComponent<EarthFraming>();
        }

        private void Update()
        {
            if (framing == null)
            {
                return;
            }

            // 2本指のときは、つまむ操作を優先する。
            // Unityは1本目の指をマウスとしても渡すため、先に指の状態を見ないと
            // つまみながら視点が回ってしまう。
            if (HandlePinch())
            {
                dragging = false;
                return;
            }

            HandleDrag();
            HandleZoom();
        }

        /// <summary>
        /// 2本指の間隔で寄り引きする。触った本数が2本未満になるまで、他の操作は行わない。
        /// つまみ始めが両方ともUIの上なら何もしない。
        /// </summary>
        /// <returns>つまむ操作を扱ったなら真。</returns>
        private bool HandlePinch()
        {
            if (Input.touchCount < 2)
            {
                pinching = false;
                return false;
            }

            var first = Input.GetTouch(0);
            var second = Input.GetTouch(1);
            var distance = Vector2.Distance(first.position, second.position);

            if (!pinching)
            {
                // 両方の指がUIの上から始まったときは、つまむ操作にしない。
                // ボタンを押したつもりが寄り引きするのを防ぐ。
                if (IsTouchOverUi(first) && IsTouchOverUi(second))
                {
                    return false;
                }

                // 指を置いた直後は間隔の差が取れないので、基準だけ覚えて次のフレームから動かす。
                pinching = true;
                lastPinchDistance = distance;
                return true;
            }

            var delta = distance - lastPinchDistance;
            lastPinchDistance = distance;

            if (Mathf.Approximately(delta, 0f))
            {
                return true;
            }

            var height = Mathf.Max(1, Screen.height);
            framing.Zoom *= 1f - (delta / height) * ZoomPerScreenHeightPinch;
            framing.Apply();
            return true;
        }

        private void HandleDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // UIの上から始まったドラッグは視点操作にしない。
                if (IsPointerOverUi())
                {
                    return;
                }

                dragging = true;
                lastPointer = Input.mousePosition;
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                dragging = false;
                return;
            }

            if (!dragging || !Input.GetMouseButton(0))
            {
                return;
            }

            var current = Input.mousePosition;
            var delta = current - lastPointer;
            lastPointer = current;

            if (delta.sqrMagnitude <= 0f)
            {
                return;
            }

            ApplyDrag(framing, delta, Screen.height);
        }

        /// <summary>
        /// 指の動きぶんだけ視点を回す。
        ///
        /// **指に地球がついてくる向きにする。**
        /// 以前は逆だった。指を下へすべらせるとカメラが下がり、地球は上を向いた。
        /// 指を右へすべらせると地表は左へ流れた。つかんで回している感じにならない。
        ///
        /// カメラは地球のまわりを回るので、地表を指と同じ向きへ動かすには、
        /// カメラを指と反対へ回す。指を下へ動かせばカメラは上がり、北極を見下ろす。
        /// 指を右へ動かせばカメラは左へ回り、地表は右へ流れる。
        ///
        /// <paramref name="delta"/> は画面座標での動き。Unityの画面座標は
        /// 上へ行くほどyが大きいので、指を下へ動かすと y は負になる。
        ///
        /// 入力の取り出しと分けてあるのは、点検ツールから向きを確かめられるようにするため。
        /// </summary>
        public static void ApplyDrag(EarthFraming framing, Vector2 delta, int screenHeight)
        {
            if (framing == null)
            {
                return;
            }

            var scale = DegreesPerScreenHeight / Mathf.Max(1, screenHeight);

            framing.Yaw += delta.x * scale;
            framing.Pitch -= delta.y * scale;
            framing.Apply();
        }

        private void HandleZoom()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f) || IsPointerOverUi())
            {
                return;
            }

            framing.Zoom *= 1f - scroll * ZoomPerNotch;
            framing.Apply();
        }

        private static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>その指がUIの上にあるか。指ごとに見分けるため、指の番号で問い合わせる。</summary>
        private static bool IsTouchOverUi(Touch touch)
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }
    }
}
