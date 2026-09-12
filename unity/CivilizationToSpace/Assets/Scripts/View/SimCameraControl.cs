using UnityEngine;
using UnityEngine.EventSystems;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 形成の計算を見るためのカメラ。
    ///
    /// 注視点と「収めたい半径」を外から与えると、その大きさが画面に入る距離まで下がる。
    /// 雲が集まるにつれて収めたい半径は小さくなり、カメラは自然に寄っていく。
    /// 指でなぞると回り、指2本の間隔で寄れる。手で動かしても自動の寄りは効き続ける。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class SimCameraControl : MonoBehaviour
    {
        /// <summary>画面の高さ1つ分のドラッグで何度回すか。</summary>
        private const float DegreesPerScreenHeight = 200f;

        private const float ZoomPerNotch = 0.12f;
        private const float ZoomPerPinchScreenHeight = 1.6f;

        private const float MinimumPitch = -82f;
        private const float MaximumPitch = 82f;
        private const float MinimumZoom = 0.18f;
        private const float MaximumZoom = 4f;

        /// <summary>注視点と半径の追従の速さ。大きいほど機敏で、揺れやすい。</summary>
        private const float FollowSpeed = 2.4f;

        private Camera view;
        private float yaw = 24f;
        private float pitch = 26f;
        private float zoom = 1f;

        private Vector3 smoothedTarget;
        private float smoothedRadius = 30f;
        private bool initialised;

        private bool dragging;
        private Vector3 lastPointer;
        private bool pinching;
        private float lastPinchDistance;

        /// <summary>見たい場所。毎フレーム入れ替えてよい。</summary>
        public Vector3 Target { get; set; }

        /// <summary>画面へ収めたい半径。</summary>
        public float FrameRadius { get; set; }

        private void Awake()
        {
            view = GetComponent<Camera>();
            FrameRadius = 30f;
        }

        /// <summary>視点と寄りを最初の状態へ戻す。注視点と半径は戻さない。</summary>
        public void ResetView()
        {
            yaw = 24f;
            pitch = 26f;
            zoom = 1f;
        }

        private void LateUpdate()
        {
            HandleInput();
            Follow();
            Place();
        }

        private void HandleInput()
        {
            if (Input.touchCount >= 2)
            {
                dragging = false;
                HandlePinch();
                return;
            }

            pinching = false;
            HandleDrag();
            HandleWheel();
        }

        private void HandlePinch()
        {
            var first = Input.GetTouch(0);
            var second = Input.GetTouch(1);

            if (IsOverUi(first.fingerId) || IsOverUi(second.fingerId))
            {
                pinching = false;
                return;
            }

            var distance = Vector2.Distance(first.position, second.position);

            if (!pinching)
            {
                pinching = true;
                lastPinchDistance = distance;
                return;
            }

            var delta = distance - lastPinchDistance;
            lastPinchDistance = distance;

            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            var height = Mathf.Max(1, Screen.height);
            zoom = Mathf.Clamp(zoom * (1f - delta / height * ZoomPerPinchScreenHeight), MinimumZoom, MaximumZoom);
        }

        private void HandleDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (IsOverUi(PrimaryFingerId()))
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

            var scale = DegreesPerScreenHeight / Mathf.Max(1, Screen.height);
            yaw -= delta.x * scale;
            pitch = Mathf.Clamp(pitch + delta.y * scale, MinimumPitch, MaximumPitch);
        }

        private void HandleWheel()
        {
            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f) || IsOverUi(PrimaryFingerId()))
            {
                return;
            }

            zoom = Mathf.Clamp(zoom * (1f - scroll * ZoomPerNotch), MinimumZoom, MaximumZoom);
        }

        /// <summary>注視点と半径をゆっくり追う。いきなり合わせると画面が跳ねる。</summary>
        private void Follow()
        {
            var radius = Mathf.Max(0.5f, FrameRadius);

            if (!initialised)
            {
                smoothedTarget = Target;
                smoothedRadius = radius;
                initialised = true;
                return;
            }

            var t = 1f - Mathf.Exp(-FollowSpeed * Time.deltaTime);
            smoothedTarget = Vector3.Lerp(smoothedTarget, Target, t);
            smoothedRadius = Mathf.Lerp(smoothedRadius, radius, t);
        }

        private void Place()
        {
            if (view == null)
            {
                return;
            }

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var distance = DistanceFor(smoothedRadius * zoom);

            transform.rotation = rotation;
            transform.position = smoothedTarget - rotation * Vector3.forward * distance;

            // 近くと遠くの両方を写すため、描画の範囲も距離に合わせて動かす。
            view.nearClipPlane = Mathf.Max(0.05f, distance * 0.01f);
            view.farClipPlane = Mathf.Max(100f, distance * 12f);
        }

        /// <summary>半径 r が画面に収まる距離。縦と横の狭いほうに合わせる。</summary>
        private float DistanceFor(float radius)
        {
            var halfVertical = view.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * Mathf.Max(0.1f, view.aspect));
            var half = Mathf.Min(halfVertical, halfHorizontal);
            return radius / Mathf.Max(0.05f, Mathf.Tan(half));
        }

        private static int PrimaryFingerId()
        {
            return Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
        }

        private static bool IsOverUi(int fingerId)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            return fingerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }
    }
}
