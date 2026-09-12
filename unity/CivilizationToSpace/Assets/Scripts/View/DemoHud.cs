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

        /// <summary>月への展開のボタン。時代と同じものに見せないため色を分ける。</summary>
        private static readonly Color MoonButtonColor = new Color(0.20f, 0.17f, 0.26f, 1f);
        private static readonly Color MoonButtonSelectedColor = new Color(0.38f, 0.32f, 0.50f, 1f);

        private EraTimeline timeline;
        private TimelinePlayback playback;
        private MotionSettings motion;
        private EarthFraming framing;
        private MoonExpansion moon;

        private Text indexText;
        private Text nameText;
        private Text rangeText;
        private Text summaryText;
        private Text tagsText;
        private Text qualityText;
        private Text eventsText;
        private Text degradedText;
        private Text futureText;

        private Button previousButton;
        private Button nextButton;
        private Button playButton;
        private Button speedButton;
        private Button motionButton;
        private Button resetViewButton;
        private Slider eraSlider;
        private Text statusText;
        private bool suppressSliderCallback;
        private readonly List<Button> eraButtons = new List<Button>();

        private RectTransform controlRoot;
        private RectTransform playbackRoot;
        private RectTransform headerRoot;
        private Text viewHint;
        private Button descriptionButton;

        /// <summary>説明を出しているかどうか。既定は出さない。</summary>
        private bool descriptionVisible;
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
            BuildPlaybackBar(root);
            BuildControlBar(root);
            BuildViewHint(root);
            BuildErrorPanel(root);

            // 部品がそろってから既定の表示にする。先に呼ぶと、
            // まだ作られていない部品に指定が効かない。
            SetDescriptionVisible(false);
        }

        /// <summary>データが読めたときに呼ぶ。操作を有効にし、最初の時代を表示する。</summary>
        public void Bind(
            EraTimeline eraTimeline,
            TimelinePlayback timelinePlayback,
            MotionSettings motionSettings,
            EarthFraming earthFraming,
            MoonExpansion moonExpansion)
        {
            timeline = eraTimeline;
            playback = timelinePlayback;
            motion = motionSettings;
            framing = earthFraming;
            moon = moonExpansion;

            for (var i = 0; i < timeline.EraCount; i++)
            {
                var target = i;
                var button = UiFactory.CreateButton(
                    controlRoot, "Era" + (i + 1), 15, ButtonColor, TextColor);
                button.GetComponentInChildren<Text>().text = timeline.At(i).DisplayName;
                button.onClick.AddListener(delegate { timeline.Select(target); });
                eraButtons.Add(button);
            }

            // 月への展開は時代ではない。色を変えて並べ、時代と同じものに見せない。
            if (moon != null)
            {
                for (var i = 0; i < moon.Phases.Count; i++)
                {
                    var target = timeline.EraCount + i;
                    var button = UiFactory.CreateButton(
                        controlRoot, "Moon" + (i + 1), 14, MoonButtonColor, TextColor);
                    button.GetComponentInChildren<Text>().text = moon.Phases[i].DisplayName;
                    button.onClick.AddListener(delegate { timeline.Select(target); });
                    eraButtons.Add(button);
                }
            }

            previousButton.transform.SetAsFirstSibling();
            nextButton.transform.SetAsLastSibling();

            previousButton.onClick.AddListener(timeline.Previous);
            nextButton.onClick.AddListener(timeline.Next);

            eraSlider.maxValue = timeline.Count - 1;
            eraSlider.onValueChanged.AddListener(OnSliderChanged);

            playButton.onClick.AddListener(playback.Toggle);
            speedButton.onClick.AddListener(playback.CycleSpeed);
            motionButton.onClick.AddListener(motion.Toggle);
            resetViewButton.onClick.AddListener(framing.ResetView);
            descriptionButton.onClick.AddListener(ToggleDescription);

            timeline.Changed += Show;
            playback.Changed += Refresh;
            motion.Changed += Refresh;

            Show(timeline.Current);
        }

        /// <summary>
        /// 説明の表示を切り替える。既定では出さず、地球を大きく見せる。
        /// 出したときはカメラの配置も切り替え、地球が説明と重ならないようにする。
        /// </summary>
        private void ToggleDescription()
        {
            SetDescriptionVisible(!descriptionVisible);
            Refresh();
        }

        private void SetDescriptionVisible(bool visible)
        {
            descriptionVisible = visible;
            infoRoot.gameObject.SetActive(visible);
            headerRoot.gameObject.SetActive(visible);

            // 視点操作の案内も文字なので、説明を出しているときだけにする。
            if (viewHint != null)
            {
                viewHint.gameObject.SetActive(visible);
            }

            if (descriptionButton != null)
            {
                descriptionButton.GetComponentInChildren<Text>().text = visible ? "説明を隠す" : "説明を出す";
            }

            if (framing != null)
            {
                framing.SidePanelVisible = visible;
                framing.Apply();
            }
        }

        private void OnSliderChanged(float value)
        {
            if (suppressSliderCallback || timeline == null)
            {
                return;
            }

            // 手で時代を動かすと再生は止まる。止めるのは TimelinePlayback 側が引き受ける。
            timeline.Select(Mathf.RoundToInt(value));
        }

        /// <summary>再生・速度・動きの設定が変わったときの表示更新。時代は変わっていない。</summary>
        private void Refresh()
        {
            if (timeline != null)
            {
                Show(timeline.Current);
            }
        }

        /// <summary>全体エラー。操作を無効にし、見せてよい文言だけを出す。</summary>
        public void ShowError(string userMessage)
        {
            infoRoot.gameObject.SetActive(false);
            headerRoot.gameObject.SetActive(true);
            controlRoot.gameObject.SetActive(false);
            playbackRoot.gameObject.SetActive(false);

            // 地球を描いていないので、視点操作の案内も出さない。
            viewHint.gameObject.SetActive(false);

            errorRoot.gameObject.SetActive(true);
            errorText.text = userMessage;
        }

        private void Show(EraData era)
        {
            indexText.text = Pad2(timeline.Index + 1) + " / " + Pad2(timeline.Count);

            var tail = timeline.TailIndex;
            if (tail >= 0 && moon != null && tail < moon.Phases.Count)
            {
                ShowMoonPhase(moon.Phases[tail]);
            }
            else
            {
                ShowEra(era);
            }

            previousButton.interactable = timeline.HasPrevious;
            nextButton.interactable = timeline.HasNext;

            suppressSliderCallback = true;
            eraSlider.value = timeline.Index;
            suppressSliderCallback = false;

            var last = timeline.Count - 1;
            playButton.GetComponentInChildren<Text>().text =
                playback.IsPlaying ? "停止" : (timeline.Index >= last ? "最初から再生" : "再生");
            speedButton.GetComponentInChildren<Text>().text = "速度 " + FormatSpeed(playback.Speed);
            motionButton.GetComponentInChildren<Text>().text =
                motion.Reduced ? "動きを減らす：オン" : "動きを減らす：オフ";
            SetNormalColor(motionButton, motion.Reduced ? ButtonSelectedColor : ButtonColor);

            statusText.text = BuildStatus();

            // 選択中はボタンの通常色を変える。Image の色を直接触ると、
            // Button 自身の状態遷移が次の描画で上書きしてしまう。
            for (var i = 0; i < eraButtons.Count; i++)
            {
                var isMoon = i >= timeline.EraCount;
                var selected = i == timeline.Index;
                SetNormalColor(eraButtons[i], selected
                    ? (isMoon ? MoonButtonSelectedColor : ButtonSelectedColor)
                    : (isMoon ? MoonButtonColor : ButtonColor));
            }
        }

        private void ShowEra(EraData era)
        {
            nameText.text = era.DisplayName;
            rangeText.text = era.RangeLabel;
            summaryText.text = era.Summary;
            tagsText.text = Join(era.Tags, "　／　");
            qualityText.text = "データ品質状態：" + era.Status;

            eventsText.text = BuildEvents(era.Events);
            eventsText.gameObject.SetActive(era.Events.Count > 0);

            degradedText.gameObject.SetActive(era.VisualDegraded);
            degradedText.text = "一部の視覚情報を読み取れなかったため、簡略表示にしています。";

            futureText.text = BuildFuture(era);
            futureText.gameObject.SetActive(futureText.text.Length > 0);
        }

        /// <summary>
        /// 月への展開の段階。時代ではなく仮想シナリオであることを、
        /// 年代ラベルの位置と注意書きで示す。
        /// </summary>
        private void ShowMoonPhase(MoonPhase phase)
        {
            nameText.text = phase.DisplayName;
            rangeText.text = "月への展開（仮想シナリオ）";
            summaryText.text = phase.Summary;
            tagsText.text = Join(phase.Tags, "　／　");
            qualityText.text = "データ品質状態：" + phase.Status;

            eventsText.text = phase.Caption;
            eventsText.gameObject.SetActive(phase.Caption.Length > 0);

            degradedText.gameObject.SetActive(false);

            futureText.text = moon.Disclaimer;
            futureText.gameObject.SetActive(true);
        }

        private string BuildStatus()
        {
            var position = Pad2(timeline.Index + 1) + " / " + Pad2(timeline.Count);
            if (!playback.IsPlaying)
            {
                return "停止中 ・ " + position;
            }

            var seconds = Mathf.RoundToInt(playback.StepSeconds);
            return "再生中 " + FormatSpeed(playback.Speed) +
                   "（1時代あたり約" + seconds + "秒）・ " + position;
        }

        private static string FormatSpeed(float speed)
        {
            return (Mathf.Approximately(speed, 0.5f) ? "0.5" : Mathf.RoundToInt(speed).ToString()) + "x";
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
            headerRoot = header;
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
            UiFactory.RightColumn(infoRoot, UiFactory.SidePanelWidthFraction, 20f, 20f, 148f);

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
            qualityText = UiFactory.CreateText(content, "Quality", 13, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal);
        }

        /// <summary>再生・速度・動き・視点リセットとスライダーの帯。</summary>
        private void BuildPlaybackBar(RectTransform root)
        {
            var bar = UiFactory.CreatePanel(root, "PlaybackBar", BarColor);
            playbackRoot = bar.rectTransform;
            UiFactory.BottomBar(playbackRoot, 52f, 24f, 88f);
            UiFactory.AddHorizontalLayout(playbackRoot, 8, 8f);

            playButton = UiFactory.CreateButton(playbackRoot, "Play", 15, ButtonColor, TextColor);
            UiFactory.SetWidth(playButton.gameObject, 116f, 0f);

            speedButton = UiFactory.CreateButton(playbackRoot, "Speed", 15, ButtonColor, TextColor);
            UiFactory.SetWidth(speedButton.gameObject, 96f, 0f);

            var sliderHost = UiFactory.CreateRect(playbackRoot, "EraSliderHost");
            UiFactory.SetWidth(sliderHost.gameObject, 160f, 1f);
            eraSlider = UiFactory.CreateSlider(
                sliderHost, "EraSlider", 6, new Color(0.18f, 0.22f, 0.29f, 1f), AccentColor, TextColor);
            UiFactory.Stretch((RectTransform)eraSlider.transform, 4f, 12f, 4f, 12f);

            statusText = UiFactory.CreateText(
                playbackRoot, "Status", 13, DimTextColor, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.SetWidth(statusText.gameObject, 230f, 0f);

            motionButton = UiFactory.CreateButton(playbackRoot, "Motion", 14, ButtonColor, TextColor);
            UiFactory.SetWidth(motionButton.gameObject, 150f, 0f);

            resetViewButton = UiFactory.CreateButton(playbackRoot, "ResetView", 14, ButtonColor, TextColor);
            resetViewButton.GetComponentInChildren<Text>().text = "視点をもどす";
            UiFactory.SetWidth(resetViewButton.gameObject, 118f, 0f);

            descriptionButton = UiFactory.CreateButton(playbackRoot, "Description", 14, ButtonColor, TextColor);
            UiFactory.SetWidth(descriptionButton.gameObject, 118f, 0f);
        }

        /// <summary>
        /// 視点操作の説明。3Dの操作は見ただけでは分からないため常設する。
        /// R1要件のAC-06が求める「操作名が認識できる」を、これで満たす。
        /// </summary>
        private void BuildViewHint(RectTransform root)
        {
            var hint = UiFactory.CreateText(
                root, "ViewHint", 13, DimTextColor, TextAnchor.LowerLeft, FontStyle.Normal);
            hint.rectTransform.anchorMin = new Vector2(0f, 0f);
            hint.rectTransform.anchorMax = new Vector2(0f, 0f);
            hint.rectTransform.pivot = new Vector2(0f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(26f, 146f);
            hint.rectTransform.sizeDelta = new Vector2(620f, 22f);
            hint.text = "地球の上をドラッグすると視点が回り、ホイールで寄ります。視点の操作では再生は止まりません。";
            viewHint = hint;
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
            UiFactory.RightColumn(errorRoot, UiFactory.SidePanelWidthFraction, 20f, 20f, 148f);

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
