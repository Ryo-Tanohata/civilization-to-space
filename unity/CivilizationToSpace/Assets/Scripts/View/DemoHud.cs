using System;
using System.Collections.Generic;
using System.Globalization;
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

        /// <summary>形成過程のボタン。時代とも月とも分けて見せる。</summary>
        private static readonly Color FormationButtonColor = new Color(0.26f, 0.18f, 0.15f, 1f);
        private static readonly Color FormationButtonSelectedColor = new Color(0.52f, 0.33f, 0.24f, 1f);

        private EraTimeline timeline;
        private TimelinePlayback playback;
        private MotionSettings motion;
        private EarthFraming framing;
        private MoonExpansion moon;
        private EarthFormation formation;

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
        private RectTransform bottomStack;
        private RectTransform formationRow;
        private RectTransform eraRow;
        private RectTransform moonRow;

        /// <summary>長押しで出す説明。1つを使い回し、出す場所だけ変える。</summary>
        private RectTransform tooltipRoot;
        private Text tooltipTitle;
        private Text tooltipBody;
        private LongPressInfo tooltipOwner;
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

            // 下側は1本の積み重ねにする。帯ごとに位置を数値で決めていたため、
            // 段階ボタンが3行に増えると必ず重なる。
            BuildBottomStack(root);
            BuildErrorPanel(root);
            BuildTooltip(root);

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
            MoonExpansion moonExpansion,
            EarthFormation earthFormation)
        {
            timeline = eraTimeline;
            playback = timelinePlayback;
            motion = motionSettings;
            framing = earthFraming;
            moon = moonExpansion;
            formation = earthFormation;

            // 形成過程は時代の手前に来る。時代ではないので色を分ける。
            if (formation != null)
            {
                for (var i = 0; i < formation.Stages.Count; i++)
                {
                    var target = i;
                    var stage = formation.Stages[i];
                    var button = UiFactory.CreateIconButton(
                        formationRow, "Formation" + (i + 1), StageIcons.Formation(stage), FormationButtonColor);
                    AttachStageInfo(button, stage.DisplayName, stage.Summary,
                        delegate { timeline.Select(target); });
                    eraButtons.Add(button);
                }
            }

            for (var i = 0; i < timeline.EraCount; i++)
            {
                var target = timeline.EraOffset + i;
                var era = timeline.At(i);
                var button = UiFactory.CreateIconButton(
                    eraRow, "Era" + (i + 1), StageIcons.Era(era.Visual), ButtonColor);
                AttachStageInfo(button, era.DisplayName, era.Summary,
                    delegate { timeline.Select(target); });
                eraButtons.Add(button);
            }

            // 月への展開は時代ではない。色を変えて並べ、時代と同じものに見せない。
            if (moon != null)
            {
                for (var i = 0; i < moon.Phases.Count; i++)
                {
                    var target = timeline.EraOffset + timeline.EraCount + i;
                    var phase = moon.Phases[i];
                    var button = UiFactory.CreateIconButton(
                        moonRow, "Moon" + (i + 1), StageIcons.Moon(phase), MoonButtonColor);
                    AttachStageInfo(button, phase.DisplayName, phase.Summary,
                        delegate { timeline.Select(target); });
                    eraButtons.Add(button);
                }
            }

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

            var head = timeline.HeadIndex;
            var tail = timeline.TailIndex;

            if (head >= 0 && formation != null && head < formation.Stages.Count)
            {
                ShowFormationStage(formation.Stages[head]);
            }
            else if (tail >= 0 && moon != null && tail < moon.Phases.Count)
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
                var selected = i == timeline.Index;
                Color normal;
                Color chosen;

                if (i < timeline.EraOffset)
                {
                    normal = FormationButtonColor;
                    chosen = FormationButtonSelectedColor;
                }
                else if (i >= timeline.EraOffset + timeline.EraCount)
                {
                    normal = MoonButtonColor;
                    chosen = MoonButtonSelectedColor;
                }
                else
                {
                    normal = ButtonColor;
                    chosen = ButtonSelectedColor;
                }

                SetNormalColor(eraButtons[i], selected ? chosen : normal);
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

        /// <summary>
        /// 地球ができるまでの段階。仮説であることを、年代ラベルの位置と注意書きで示す。
        /// </summary>
        private void ShowFormationStage(FormationStage stage)
        {
            nameText.text = stage.DisplayName;
            rangeText.text = "地球ができるまで（仮説に基づく象徴表現）";
            summaryText.text = stage.Summary;
            tagsText.text = Join(stage.Tags, "　／　");
            qualityText.text = "データ品質状態：" + stage.Status;

            eventsText.text = stage.Caption;
            eventsText.gameObject.SetActive(stage.Caption.Length > 0);

            degradedText.gameObject.SetActive(false);

            futureText.text = formation.Disclaimer;
            futureText.gameObject.SetActive(true);
        }

        private string BuildStatus()
        {
            var position = Pad2(timeline.Index + 1) + " / " + Pad2(timeline.Count);
            if (!playback.IsPlaying)
            {
                return "停止中 ・ " + position;
            }

            return "再生中 " + FormatSpeed(playback.Speed) +
                   "（1時代あたり約" + FormatSeconds(playback.StepSeconds) + "秒）・ " + position;
        }

        private static string FormatSpeed(float speed)
        {
            return (Mathf.Approximately(speed, 0.5f) ? "0.5" : Mathf.RoundToInt(speed).ToString()) + "x";
        }

        /// <summary>
        /// 1段階あたりの秒数の表し方。8倍では0.5秒になり、
        /// 整数へ丸めると「約0秒」になってしまうため、1秒未満は小数で出す。
        /// </summary>
        private static string FormatSeconds(float seconds)
        {
            return seconds < 1f
                ? seconds.ToString("0.0", CultureInfo.InvariantCulture)
                : Mathf.RoundToInt(seconds).ToString(CultureInfo.InvariantCulture);
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
            // 幅を参照解像度から計算すると、縦長の画面では実際の横幅より広くなり、
            // 右の説明パネルの下へ文字が潜り込む。割合で決めて重ならないようにする。
            UiFactory.TopLeftColumn(header, UiFactory.SidePanelWidthFraction, 20f, 14f);
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

        /// <summary>
        /// 画面の下側。案内・再生の帯・段階ボタンを1本に積む。
        ///
        /// 帯ごとに下端からの距離を数値で決めていたが、段階ボタンが3行に増えると
        /// 必ず重なる。積み重ねにすれば、行が増えても上へ伸びるだけで重ならない。
        /// </summary>
        private void BuildBottomStack(RectTransform root)
        {
            bottomStack = UiFactory.BottomStack(UiFactory.CreateRect(root, "BottomStack"), 20f, 18f, 8f);

            BuildViewHint(bottomStack);
            BuildPlaybackBar(bottomStack);
            BuildStageArea(bottomStack);
        }

        /// <summary>再生・速度・動き・視点リセットとスライダーの帯。</summary>
        private void BuildPlaybackBar(RectTransform parent)
        {
            var bar = UiFactory.CreatePanel(parent, "PlaybackBar", BarColor);
            playbackRoot = bar.rectTransform;
            playbackRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
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
        private void BuildViewHint(RectTransform parent)
        {
            var hint = UiFactory.CreateText(
                parent, "ViewHint", 13, DimTextColor, TextAnchor.LowerLeft, FontStyle.Normal);
            hint.rectTransform.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            hint.text = "地球の上をドラッグ（指でなぞる）すると視点が回り、ホイールか指2本の間隔で寄ります。" +
                        "下の丸いボタンは長押しすると名前と説明が出ます。視点の操作では再生は止まりません。";
            viewHint = hint;
        }

        /// <summary>
        /// 段階ボタン。形成過程・時代・月への展開の3行に分ける。
        ///
        /// 1行に15個並べると、狭い画面では1個あたりが指より細くなり、
        /// 名前も1文字ずつ縦に折り返されて読めない。3行に分ければ1個を大きく取れる。
        /// もともと3つの群れなので、分けたほうが並びの意味とも合う。
        /// </summary>
        private void BuildStageArea(RectTransform parent)
        {
            var bar = UiFactory.CreatePanel(parent, "ControlBar", BarColor);
            controlRoot = bar.rectTransform;
            UiFactory.AddVerticalLayout(controlRoot, 8, 6f);

            // 横長の画面では15個が1行に収まる。縦長では収まらないので群れごとに分ける。
            // 分けるほど下側が高くなり、地球の見える範囲が狭くなるため、収まるなら1行にする。
            if (Screen.width >= Screen.height * WideAspect)
            {
                var single = CreateStageRow(controlRoot, "StageRow");
                formationRow = single;
                eraRow = single;
                moonRow = single;
            }
            else
            {
                formationRow = CreateStageRow(controlRoot, "FormationRow");
                eraRow = CreateStageRow(controlRoot, "EraRow");
                moonRow = CreateStageRow(controlRoot, "MoonRow");
            }

            var nav = UiFactory.CreateRect(controlRoot, "NavRow");
            var navLayout = nav.gameObject.AddComponent<HorizontalLayoutGroup>();
            navLayout.spacing = 8f;
            navLayout.childAlignment = TextAnchor.MiddleCenter;
            navLayout.childControlWidth = true;
            navLayout.childControlHeight = true;
            navLayout.childForceExpandWidth = false;
            navLayout.childForceExpandHeight = true;
            nav.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            previousButton = UiFactory.CreateButton(nav, "Previous", 15, ButtonColor, TextColor);
            previousButton.GetComponentInChildren<Text>().text = "前へ";
            UiFactory.SetWidth(previousButton.gameObject, 110f, 0f);

            nextButton = UiFactory.CreateButton(nav, "Next", 15, ButtonColor, TextColor);
            nextButton.GetComponentInChildren<Text>().text = "次へ";
            UiFactory.SetWidth(nextButton.gameObject, 110f, 0f);
        }

        /// <summary>段階ボタン1行ぶんの入れ物。中央に寄せ、幅は引き伸ばさない。</summary>
        private static RectTransform CreateStageRow(RectTransform parent, string name)
        {
            var row = UiFactory.CreateRect(parent, name);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return row;
        }

        /// <summary>段階ボタン1個の大きさ。指で押せる下限（約44）を下回らせない。</summary>
        private const float StageButtonSize = 52f;

        /// <summary>この縦横比より横長なら、段階ボタンを1行に並べる。</summary>
        private const float WideAspect = 1.3f;

        /// <summary>絵だけのボタンに、名前と説明と押したときの動きを結びつける。</summary>
        private void AttachStageInfo(Button button, string title, string body, Action activate)
        {
            var element = UiFactory.SetWidth(button.gameObject, StageButtonSize, 0f);
            element.preferredHeight = StageButtonSize;

            var info = button.gameObject.AddComponent<LongPressInfo>();
            info.Title = title;
            info.Body = body;
            info.Activate = activate;
            info.Show = ShowTooltip;
            info.Hide = HideTooltip;
        }

        /// <summary>
        /// 長押しで出す説明。1つを使い回し、押されたボタンの上へ移す。
        /// ボタンごとに持たせると、15個ぶんの文字が常に画面の外に積まれる。
        /// </summary>
        private void BuildTooltip(RectTransform root)
        {
            var panel = UiFactory.CreatePanel(root, "Tooltip", PanelColor);
            tooltipRoot = panel.rectTransform;
            tooltipRoot.anchorMin = new Vector2(0.5f, 0f);
            tooltipRoot.anchorMax = new Vector2(0.5f, 0f);
            tooltipRoot.pivot = new Vector2(0.5f, 0f);
            tooltipRoot.sizeDelta = new Vector2(340f, 0f);

            UiFactory.AddVerticalLayout(tooltipRoot, 12, 4f);
            UiFactory.AddContentHeight(tooltipRoot);

            tooltipTitle = UiFactory.CreateText(
                tooltipRoot, "Title", 16, TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
            tooltipBody = UiFactory.CreateText(
                tooltipRoot, "Body", 13, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal);

            tooltipRoot.gameObject.SetActive(false);
        }

        private void ShowTooltip(LongPressInfo info)
        {
            if (info == null || tooltipRoot == null)
            {
                return;
            }

            tooltipOwner = info;
            tooltipTitle.text = info.Title;
            tooltipBody.text = info.Body;
            tooltipRoot.gameObject.SetActive(true);

            // 文字を入れてから位置を決める。先に測ると前回の大きさのままになる。
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRoot);

            var canvasRect = tooltipRoot.parent as RectTransform;
            var button = info.transform as RectTransform;
            if (canvasRect == null || button == null)
            {
                return;
            }

            var top = button.TransformPoint(new Vector3(0f, button.rect.yMax, 0f));
            var local = canvasRect.InverseTransformPoint(top);

            // 画面の端では内側へ寄せる。はみ出すと読めない。
            var half = tooltipRoot.rect.width * 0.5f;
            var limit = canvasRect.rect.width * 0.5f - 8f;
            var x = limit > half ? Mathf.Clamp(local.x, -limit + half, limit - half) : 0f;

            tooltipRoot.anchoredPosition = new Vector2(x, local.y - canvasRect.rect.yMin + 10f);
        }

        private void HideTooltip(LongPressInfo info)
        {
            // 別のボタンが先に出し直していたら、古いほうの指示では消さない。
            if (tooltipOwner != null && info != null && tooltipOwner != info)
            {
                return;
            }

            tooltipOwner = null;

            if (tooltipRoot != null)
            {
                tooltipRoot.gameObject.SetActive(false);
            }
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
            if (FindAnyObjectByType<EventSystem>() != null)
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
