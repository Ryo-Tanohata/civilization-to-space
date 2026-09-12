using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 押しっぱなしで説明を出すボタンの中身。
    ///
    /// 段階ボタンは絵だけで文字を持たない。名前と説明はここから出す。
    /// 指では長押し、マウスでは乗せるだけで出す。指とマウスで同じ待ち時間にすると、
    /// マウスでは遅すぎ、指では誤って出やすい。
    ///
    /// 長押しで説明を読んだときは、指を離しても段階を選び直さない。
    /// 「何の段階か確かめたいだけ」の操作で表示が変わると、確かめる前に画面が動いてしまう。
    /// </summary>
    public sealed class LongPressInfo : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>説明を出すまでの押しっぱなしの長さ。</summary>
        private const float HoldSeconds = 0.4f;

        /// <summary>指を離してから説明を消すまでの猶予。読む時間を残す。</summary>
        private const float LingerSeconds = 3f;

        /// <summary>見出し。ボタンが表す段階の名前。</summary>
        public string Title { get; set; }

        /// <summary>本文。段階の説明。</summary>
        public string Body { get; set; }

        /// <summary>説明を出すとき呼ばれる。出す場所はこのボタンの位置から決める。</summary>
        public Action<LongPressInfo> Show { get; set; }

        /// <summary>
        /// 説明を消すとき呼ばれる。どのボタンが消そうとしているかを渡す。
        /// 別のボタンが先に出し直していたら、受け手は無視できる。
        /// </summary>
        public Action<LongPressInfo> Hide { get; set; }

        /// <summary>短く押したとき呼ばれる。長押しのときは呼ばれない。</summary>
        public Action Activate { get; set; }

        private bool pressed;
        private float pressedAt;

        /// <summary>いまこのボタンが説明を出しているか。</summary>
        private bool open;

        /// <summary>長押しで出したので、この指離しでは選び直さない、という印。</summary>
        private bool suppressClick;

        private float hideAt;

        private void OnDisable()
        {
            pressed = false;
            hideAt = 0f;
            suppressClick = false;
            Close();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            pressedAt = Time.unscaledTime;
            hideAt = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;

            // 出したままにして読ませる。少し待ってから消す。
            if (suppressClick)
            {
                hideAt = Time.unscaledTime + LingerSeconds;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (suppressClick)
            {
                // 長押しは「確かめる」操作である。選び直さない。
                suppressClick = false;
                return;
            }

            if (Activate != null)
            {
                Activate();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // マウスの pointerId は負の値になる。指のときは乗せただけでは出さない。
            if (eventData == null || eventData.pointerId >= 0)
            {
                return;
            }

            hideAt = 0f;
            Open();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pressed = false;

            if (eventData != null && eventData.pointerId >= 0)
            {
                // 指では、離したときにも exit が来る。すぐ消さず猶予に任せる。
                return;
            }

            Close();
        }

        private void Update()
        {
            if (pressed && !suppressClick && Time.unscaledTime - pressedAt >= HoldSeconds)
            {
                suppressClick = true;
                Open();
                return;
            }

            if (hideAt > 0f && Time.unscaledTime >= hideAt)
            {
                hideAt = 0f;
                Close();
            }
        }

        private void Open()
        {
            open = true;
            if (Show != null)
            {
                Show(this);
            }
        }

        private void Close()
        {
            if (!open)
            {
                return;
            }

            open = false;

            if (Hide != null)
            {
                Hide(this);
            }
        }
    }
}
