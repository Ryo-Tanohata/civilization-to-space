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

        private EarthFraming framing;
        private bool dragging;
        private Vector3 lastPointer;

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

            HandleDrag();
            HandleZoom();
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

            var height = Mathf.Max(1, Screen.height);
            var scale = DegreesPerScreenHeight / height;

            framing.Yaw -= delta.x * scale;
            framing.Pitch += delta.y * scale;
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
    }
}
