using System.Collections.Generic;
using System.Text;
using CivilizationToSpace.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 画面の文字と操作。時代の選択と、選んだ時代の説明を出す。
    ///
    /// 表示する文字列はすべて時代データ由来である。時代の名称・年代ラベル・解説をここへ書き写さない。
    /// 見出しや区切りの語だけを持つ。
    /// </summary>
    public sealed class DemoHud : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.11f, 0.88f);
        private static readonly Color BarColor = new Color(0.05f, 0.07f, 0.11f, 0.92f);
        private static readonly Color TextColor = new Color(0.90f, 0.93f, 0.96f, 1f);
        private static readonly Color DimTextColor = new Color(0.66f, 0.72f, 0.79f, 1f);
        private static readonly Color AccentColor = new Color(0.55f, 0.78f, 0.92f, 1f);
        private static readonly Color WarnColor = new Color(0.98f, 0.80f, 0.45f, 1f);
        private static readonly Color ButtonColor = new Color(0.13f, 0.18f, 0.25f, 1f);
        private static readonly Color ButtonSelectedColor = new Color(0.22f, 0.40f, 0.55f, 1f);

        private EraTimeline timeline;

        private Text indexText;
        private Text nameText;
        private Text rangeText;
        private Text summaryText;
        private Text tagsText;
        private Text statusText;
        private Text eventsText;
        private Text degradedText;
        private Text futureText;

        private Button previousButton;
        private Button nextButton;
        private readonly List<Button> eraButtons = new List<Button>();

        private RectTransform controlRoot;
        private RectTransform infoRoot;
        private RectTransform errorRoot;
        private Text errorText;

        public void Build(Camera camera, string catalogTitle, string disclaimer, string parameterNote)
        {
            EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("Hud", camera);
            var root = (RectTransform)canvas.transform;

            BuildHeader(root, catalogTitle, disclaimer, parameterNote);
            BuildInfoPanel(root);
            BuildControlBar(root);
            BuildErrorPanel(root);
        }

        /// <summary>データが読めたときに呼ぶ。操作を有効にし、最初の時代を表示する。</summary>
        public void Bind(EraTimeline eraTimeline)
        {
            timeline = eraTimeline;

            for (var i = 0; i < timeline.Count; i++)
            {
                var target = i;
                var button = UiFactory.CreateButton(
                    controlRoot, "Era" + (i + 1), 15, ButtonColor, TextColor);
                button.GetComponentInChildren<Text>().text = timeline.At(i).DisplayName;
                button.onClick.AddListener(delegate { timeline.Select(target); });
                eraButtons.Add(button);
            }

            previousButton.transform.SetAsFirstSibling();
            nextButton.transform.SetAsLastSibling();

            previousButton.onClick.AddListener(timeline.Previous);
            nextButton.onClick.AddListener(timeline.Next);

            timeline.Changed += Show;
            Show(timeline.Current);
        }

        /// <summary>全体エラー。操作を無効にし、見せてよい文言だけを出す。</summary>
        public void ShowError(string userMessage)
        {
            infoRoot.gameObject.SetActive(false);
            controlRoot.gameObject.SetActive(false);
            errorRoot.gameObject.SetActive(true);
            errorText.text = userMessage;
        }

        private void Show(EraData era)
        {
            indexText.text = Pad2(timeline.Index + 1) + " / " + Pad2(timeline.Count);
            nameText.text = era.DisplayName;
            rangeText.text = era.RangeLabel;
            summaryText.text = era.Summary;
            tagsText.text = Join(era.Tags, "　／　");
            statusText.text = "データ品質状態：" + era.Status;

            eventsText.text = BuildEvents(era.Events);
            eventsText.gameObject.SetActive(era.Events.Count > 0);

            degradedText.gameObject.SetActive(era.VisualDegraded);
            degradedText.text = "一部の視覚情報を読み取れなかったため、簡略表示にしています。";

            futureText.text = BuildFuture(era);
            futureText.gameObject.SetActive(futureText.text.Length > 0);

            previousButton.interactable = timeline.HasPrevious;
            nextButton.interactable = timeline.HasNext;

            // 選択中はボタンの通常色を変える。Image の色を直接触ると、
            // Button 自身の状態遷移が次の描画で上書きしてしまう。
            for (var i = 0; i < eraButtons.Count; i++)
            {
                SetNormalColor(eraButtons[i], i == timeline.Index ? ButtonSelectedColor : ButtonColor);
            }
        }

        private static string BuildEvents(IReadOnlyList<string> events)
        {
            if (events.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder("代表イベント");
            foreach (var item in events)
            {
                builder.Append('\n').Append("・").Append(item);
            }

            return builder.ToString();
        }

        /// <summary>Futureの注記と将来シナリオ候補。読み取り専用であり、選択させない。</summary>
        private static string BuildFuture(EraData era)
        {
            if (era.FutureNote.Length == 0 && era.Scenarios.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            if (era.FutureNote.Length > 0)
            {
                builder.Append(era.FutureNote);
            }

            if (era.Scenarios.Count > 0)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append("将来シナリオ候補（読み取り専用）");
                foreach (var scenario in era.Scenarios)
                {
                    builder.Append('\n').Append("・").Append(scenario.Name).Append("：").Append(scenario.Assumption);
                }
            }

            return builder.ToString();
        }

        private void BuildHeader(RectTransform root, string catalogTitle, string disclaimer, string parameterNote)
        {
            var header = UiFactory.CreateRect(root, "Header");
            UiFactory.TopLeft(header, 20f, 14f, UiFactory.ReferenceResolution.x * (1f - UiFactory.SidePanelWidthFraction) - 40f, 96f);
            UiFactory.AddVerticalLayout(header, 0, 4f);

            UiFactory.CreateText(header, "Title", 24, TextColor, TextAnchor.UpperLeft, FontStyle.Bold)
                .text = catalogTitle;
            UiFactory.CreateText(header, "Disclaimer", 13, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal)
                .text = disclaimer;

            if (parameterNote.Length > 0)
            {
                UiFactory.CreateText(header, "ParameterNote", 12, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal)
                    .text = parameterNote;
            }
        }

        private void BuildInfoPanel(RectTransform root)
        {
            var panel = UiFactory.CreatePanel(root, "InfoPanel", PanelColor);
            infoRoot = panel.rectTransform;
            UiFactory.RightColumn(infoRoot, UiFactory.SidePanelWidthFraction, 20f, 20f, 96f);

            var content = UiFactory.CreateRect(infoRoot, "Content");
            UiFactory.Stretch(content, 0f, 0f, 0f, 0f);
            UiFactory.AddVerticalLayout(content, 20, 8f);

            indexText = UiFactory.CreateText(content, "Index", 14, AccentColor, TextAnchor.UpperLeft, FontStyle.Bold);
            nameText = UiFactory.CreateText(content, "Name", 28, TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
            rangeText = UiFactory.CreateText(content, "Range", 15, AccentColor, TextAnchor.UpperLeft, FontStyle.Normal);
            summaryText = UiFactory.CreateText(content, "Summary", 15, TextColor, TextAnchor.UpperLeft, FontStyle.Normal);
            tagsText = UiFactory.CreateText(content, "Tags", 14, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal);
            eventsText = UiFactory.CreateText(content, "Events", 14, TextColor, TextAnchor.UpperLeft, FontStyle.Normal);
            futureText = UiFactory.CreateText(content, "Future", 14, WarnColor, TextAnchor.UpperLeft, FontStyle.Normal);
            degradedText = UiFactory.CreateText(content, "Degraded", 13, WarnColor, TextAnchor.UpperLeft, FontStyle.Normal);
            statusText = UiFactory.CreateText(content, "Status", 13, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal);
        }

        private void BuildControlBar(RectTransform root)
        {
            var bar = UiFactory.CreatePanel(root, "ControlBar", BarColor);
            controlRoot = bar.rectTransform;
            UiFactory.BottomBar(controlRoot, 60f, 24f, 20f);
            UiFactory.AddHorizontalLayout(controlRoot, 8, 8f);

            previousButton = UiFactory.CreateButton(controlRoot, "Previous", 15, ButtonColor, TextColor);
            previousButton.GetComponentInChildren<Text>().text = "前へ";
            UiFactory.SetWidth(previousButton.gameObject, 88f, 0f);

            nextButton = UiFactory.CreateButton(controlRoot, "Next", 15, ButtonColor, TextColor);
            nextButton.GetComponentInChildren<Text>().text = "次へ";
            UiFactory.SetWidth(nextButton.gameObject, 88f, 0f);
        }

        private void BuildErrorPanel(RectTransform root)
        {
            var panel = UiFactory.CreatePanel(root, "ErrorPanel", PanelColor);
            errorRoot = panel.rectTransform;
            UiFactory.RightColumn(errorRoot, UiFactory.SidePanelWidthFraction, 20f, 20f, 96f);

            var content = UiFactory.CreateRect(errorRoot, "Content");
            UiFactory.Stretch(content, 0f, 0f, 0f, 0f);
            UiFactory.AddVerticalLayout(content, 20, 10f);

            UiFactory.CreateText(content, "Heading", 20, WarnColor, TextAnchor.UpperLeft, FontStyle.Bold)
                .text = "時代データを表示できません";
            errorText = UiFactory.CreateText(content, "Message", 15, TextColor, TextAnchor.UpperLeft, FontStyle.Normal);

            errorRoot.gameObject.SetActive(false);
        }

        private static void SetNormalColor(Button button, Color color)
        {
            button.colors = UiFactory.MakeColors(color);
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.transform.SetParent(null);
        }

        private static string Pad2(int value)
        {
            return value < 10 ? "0" + value : value.ToString();
        }

        private static string Join(IReadOnlyList<string> items, string separator)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(separator);
                }

                builder.Append(items[i]);
            }

            return builder.ToString();
        }
    }
}
