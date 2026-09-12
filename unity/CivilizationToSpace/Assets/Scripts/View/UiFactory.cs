using UnityEngine;
using UnityEngine.UI;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// uGUIをコードから組み立てるためのヘルパー。
    ///
    /// 画面構成をSceneのYAMLではなくコードに置く。差分が読め、生成し直せるためである。
    /// 位置は参照解像度1280x720の単位で指定し、画面端に寄せる要素は端へアンカーする。
    /// 画面比が変わっても端の要素が画面外へ出ないようにするためである。
    /// </summary>
    public static class UiFactory
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

        /// <summary>
        /// 右の説明パネルが画面の横幅に占める割合。
        /// 幅を固定値で持つと、狭い画面で画面外へ出たり、広い画面で余白が空きすぎたりする。
        /// カメラのフレーミング（EarthFraming）も同じ値を見て、地球を左側へ収める。
        /// </summary>
        public const float SidePanelWidthFraction = 0.41f;

        public static Canvas CreateCanvas(string name, Camera camera)
        {
            var go = new GameObject(
                name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image CreatePanel(Transform parent, string name, Color color)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text CreateText(
            Transform parent,
            string name,
            int fontSize,
            Color color,
            TextAnchor anchor,
            FontStyle style)
        {
            var rect = CreateRect(parent, name);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = JapaneseFont.Get();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            // 時代データ由来の文字列をマークアップとして解釈させない。
            text.supportRichText = false;

            return text;
        }

        public static Button CreateButton(Transform parent, string name, int fontSize, Color background, Color label)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();

            // Button の色遷移は targetGraphic の色に掛け算で効く。
            // 画像を白にしておき、実際の色は ColorBlock 側だけで決める。
            image.color = Color.white;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.colors = MakeColors(background);

            var text = CreateText(rect, "Label", fontSize, label, TextAnchor.MiddleCenter, FontStyle.Normal);
            Stretch(text.rectTransform, 6f, 4f, 6f, 4f);

            return button;
        }

        /// <summary>
        /// 基準色から各状態の色を作る。無効時をはっきり暗くする。
        /// 既定値のままだと、端で押せなくなっていることが見分けにくい。
        /// </summary>
        public static ColorBlock MakeColors(Color basis)
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = basis;
            colors.selectedColor = basis;
            colors.highlightedColor = Color.Lerp(basis, Color.white, 0.22f);
            colors.pressedColor = Color.Lerp(basis, Color.black, 0.25f);
            colors.disabledColor = new Color(basis.r * 0.45f, basis.g * 0.45f, basis.b * 0.45f, basis.a * 0.75f);
            // 選択の切替は即座に反映させる。フェードさせると、
            // どのボタンが選ばれているかが一瞬あいまいになる。
            colors.fadeDuration = 0f;
            return colors;
        }

        /// <summary>親いっぱいに広げる。余白は参照解像度の単位。</summary>
        public static RectTransform Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        /// <summary>左上を基準に置く。y は上端からの距離。</summary>
        public static RectTransform TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
        }

        /// <summary>
        /// 右端の列。幅は画面の横幅に対する割合で決める。
        /// 固定幅にすると、狭い画面では画面外へ出て、広い画面では余白が空きすぎる。
        /// </summary>
        public static RectTransform RightColumn(RectTransform rect, float widthFraction, float margin, float top, float bottom)
        {
            rect.anchorMin = new Vector2(1f - widthFraction, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(0f, bottom);
            rect.offsetMax = new Vector2(-margin, -top);
            return rect;
        }

        /// <summary>下端に横いっぱいの帯を作る。</summary>
        public static RectTransform BottomBar(RectTransform rect, float height, float side, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(side, bottom);
            rect.offsetMax = new Vector2(-side, bottom + height);
            return rect;
        }

        /// <summary>縦に積む並べ方。情報パネルの各行に使う。</summary>
        public static VerticalLayoutGroup AddVerticalLayout(RectTransform rect, int padding, float spacing)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        /// <summary>横に並べる。時代ボタンの帯に使う。</summary>
        public static HorizontalLayoutGroup AddHorizontalLayout(RectTransform rect, int padding, float spacing)
        {
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return layout;
        }

        /// <summary>
        /// 目盛りつきのスライダー。uGUIのSliderは背景・塗り・つまみを自前で組む必要がある。
        /// </summary>
        public static Slider CreateSlider(Transform parent, string name, int steps, Color track, Color fill, Color handle)
        {
            var rect = CreateRect(parent, name);
            var slider = rect.gameObject.AddComponent<Slider>();

            var background = CreatePanel(rect, "Track", track);
            background.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            background.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            background.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            background.rectTransform.offsetMin = new Vector2(0f, -3f);
            background.rectTransform.offsetMax = new Vector2(0f, 3f);

            var fillArea = CreateRect(rect, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.pivot = new Vector2(0.5f, 0.5f);
            fillArea.offsetMin = new Vector2(8f, -3f);
            fillArea.offsetMax = new Vector2(-8f, 3f);

            var fillImage = CreatePanel(fillArea, "Fill", fill);
            fillImage.rectTransform.anchorMin = Vector2.zero;
            fillImage.rectTransform.anchorMax = new Vector2(0f, 1f);
            fillImage.rectTransform.sizeDelta = new Vector2(16f, 0f);

            var handleArea = CreateRect(rect, "Handle Slide Area");
            handleArea.anchorMin = new Vector2(0f, 0f);
            handleArea.anchorMax = new Vector2(1f, 1f);
            handleArea.offsetMin = new Vector2(8f, 0f);
            handleArea.offsetMax = new Vector2(-8f, 0f);

            var handleImage = CreatePanel(handleArea, "Handle", handle);
            handleImage.raycastTarget = true;
            handleImage.rectTransform.anchorMin = new Vector2(0f, 0f);
            handleImage.rectTransform.anchorMax = new Vector2(0f, 1f);
            handleImage.rectTransform.sizeDelta = new Vector2(16f, 0f);

            slider.fillRect = fillImage.rectTransform;
            slider.handleRect = handleImage.rectTransform;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(1, steps - 1);
            slider.wholeNumbers = true;
            slider.colors = MakeColors(handle);

            return slider;
        }

        public static LayoutElement SetWidth(GameObject target, float preferred, float flexible)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.preferredWidth = preferred;
            element.flexibleWidth = flexible;
            return element;
        }
    }
}
