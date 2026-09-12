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
        /// <summary>段階ボタンを並べる行。横長なら1本、縦長なら2本。</summary>
        private RectTransform[] stageRows;

        /// <summary>段階ボタンの総数。行へ均等に割り振るために先に数える。</summary>
        private int stageCount;

        /// <summary>すでに何個置いたか。</summary>
        private int stagePlaced;

        /// <summary>長押しで出す説明。1つを使い回し、出す場所だけ変える。</summary>
        private RectTransform tooltipRoot;
        private Text tooltipTitle;
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

            compact = Screen.width < Screen.height * WideAspect;

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

            // 行へ均等に割り振るため、置く前に総数を数えておく。
            stageCount = (formation != null ? formation.Stages.Count : 0)
                         + timeline.EraCount
                         + (moon != null ? moon.Phases.Count : 0);
            stagePlaced = 0;

            // 形成過程は時代の手前に来る。時代ではないので色を分ける。
            if (formation != null)
            {
                for (var i = 0; i < formation.Stages.Count; i++)
                {
                    var target = i;
                    var stage = formation.Stages[i];
                    var button = UiFactory.CreateIconButton(
                        NextStageRow(), "Formation" + (i + 1), StageIcons.Formation(stage), FormationButtonColor);
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
                    NextStageRow(), "Era" + (i + 1), StageIcons.Era(era.Visual), ButtonColor);
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
                        NextStageRow(), "Moon" + (i + 1), StageIcons.Moon(phase), MoonButtonColor);
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

            SetButtonFace(
                descriptionButton,
                visible ? "説明を隠す" : "説明を出す",
                ControlIcons.Description(visible));

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
            var atEnd = timeline.Index >= last;

            SetButtonFace(
                playButton,
                playback.IsPlaying ? "停止" : (atEnd ? "最初から再生" : "再生"),
                playback.IsPlaying ? ControlIcons.Pause()
                    : (atEnd ? ControlIcons.Replay() : ControlIcons.Play()));

            // 速さは絵で表せないので、狭い画面でも倍率の数字だけは文字で出す。
            SetButtonFace(
                speedButton,
                compact ? FormatSpeed(playback.Speed) : "速度 " + FormatSpeed(playback.Speed),
                null);

            SetButtonFace(
                motionButton,
                motion.Reduced ? "動きを減らす：オン" : "動きを減らす：オフ",
                ControlIcons.Motion(motion.Reduced));
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

            if (compact)
            {
                playButton = CreateControlIcon(playbackRoot, "Play", ControlIcons.Play(), "再生");
                speedButton = UiFactory.CreateButton(playbackRoot, "Speed", 14, ButtonColor, TextColor);
                UiFactory.SetWidth(speedButton.gameObject, ControlIconSize, 0f);
                AttachControlInfo(speedButton, "再生の速さ");
            }
            else
            {
                playButton = UiFactory.CreateButton(playbackRoot, "Play", 15, ButtonColor, TextColor);
                UiFactory.SetWidth(playButton.gameObject, 116f, 0f);

                speedButton = UiFactory.CreateButton(playbackRoot, "Speed", 15, ButtonColor, TextColor);
                UiFactory.SetWidth(speedButton.gameObject, 96f, 0f);
            }

            var sliderHost = UiFactory.CreateRect(playbackRoot, "EraSliderHost");
            UiFactory.SetWidth(sliderHost.gameObject, 160f, 1f);
            eraSlider = UiFactory.CreateSlider(
                sliderHost, "EraSlider", 6, new Color(0.18f, 0.22f, 0.29f, 1f), AccentColor, TextColor);
            UiFactory.Stretch((RectTransform)eraSlider.transform, 4f, 12f, 4f, 12f);

            statusText = UiFactory.CreateText(
                playbackRoot, "Status", 13, DimTextColor, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.SetWidth(statusText.gameObject, 230f, 0f);

            if (compact)
            {
                motionButton = CreateControlIcon(
                    playbackRoot, "Motion", ControlIcons.Motion(false), "動きを減らす");
                resetViewButton = CreateControlIcon(
                    playbackRoot, "ResetView", ControlIcons.ResetView(), "視点をもどす");
                descriptionButton = CreateControlIcon(
                    playbackRoot, "Description", ControlIcons.Description(false), "説明を出す");
            }
            else
            {
                motionButton = UiFactory.CreateButton(playbackRoot, "Motion", 14, ButtonColor, TextColor);
                UiFactory.SetWidth(motionButton.gameObject, 150f, 0f);

                resetViewButton = UiFactory.CreateButton(playbackRoot, "ResetView", 14, ButtonColor, TextColor);
                resetViewButton.GetComponentInChildren<Text>().text = "視点をもどす";
                UiFactory.SetWidth(resetViewButton.gameObject, 118f, 0f);

                descriptionButton = UiFactory.CreateButton(playbackRoot, "Description", 14, ButtonColor, TextColor);
                UiFactory.SetWidth(descriptionButton.gameObject, 118f, 0f);
            }
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

            // 横長の画面では16個が1行に収まる。縦長では収まらないので折り返す。
            //
            // 以前は形成・時代・月の3つの群れごとに行を分けていたが、縦画面では3行になり、
            // そのぶん下の帯が高くなって地球の見える範囲を削っていた。
            // 群れの違いは絵の色と形で読み取れるので、行を分けてまで示す必要はない。
            // 2行に詰め、指で押せる大きさ（約44）を下回らない範囲で並べる。
            if (!compact)
            {
                stageRows = new[] { CreateStageRow(controlRoot, "StageRow") };
            }
            else
            {
                stageRows = new[]
                {
                    CreateStageRow(controlRoot, "StageRowTop"),
                    CreateStageRow(controlRoot, "StageRowBottom"),
                };
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
        /// <summary>
        /// 次の段階ボタンを置く行を返す。
        /// 前半を上の行、後半を下の行へ入れ、時代の並び順はそのまま保つ。
        /// </summary>
        private RectTransform NextStageRow()
        {
            if (stageRows == null || stageRows.Length == 0)
            {
                return controlRoot;
            }

            if (stageRows.Length == 1)
            {
                stagePlaced++;
                return stageRows[0];
            }

            var perRow = Mathf.CeilToInt(stageCount / (float)stageRows.Length);
            var row = Mathf.Clamp(stagePlaced / Mathf.Max(1, perRow), 0, stageRows.Length - 1);
            stagePlaced++;
            return stageRows[row];
        }

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

        /// <summary>
        /// 狭い画面（縦長）かどうか。段階ボタンの行数と、操作ボタンを絵にするかを決める。
        /// 起動時の向きで決め、あとから変えない。作り直すと押している最中の状態が飛ぶ。
        /// </summary>
        private bool compact;

        /// <summary>絵のボタンの大きさ。指で押せる下限（約44）を下回らせない。</summary>
        private const float ControlIconSize = 46f;

        /// <summary>操作ボタンを絵で作り、長押しで名前が出るようにする。</summary>
        private Button CreateControlIcon(RectTransform parent, string name, Sprite icon, string label)
        {
            var button = UiFactory.CreateIconButton(parent, name, icon, ButtonColor);
            AttachControlInfo(button, label);
            return button;
        }

        /// <summary>絵のボタンに名前を結びつける。押したときの動きは呼び出し側が足す。</summary>
        private void AttachControlInfo(Button button, string label)
        {
            var element = UiFactory.SetWidth(button.gameObject, ControlIconSize, 0f);
            element.preferredHeight = ControlIconSize;

            var info = button.gameObject.AddComponent<LongPressInfo>();
            info.Title = label;
            info.Body = string.Empty;
            info.Show = ShowTooltip;
            info.Hide = HideTooltip;
        }

        /// <summary>
        /// ボタンの見た目を更新する。文字のボタンなら文字を、絵のボタンなら絵と名前を変える。
        /// 呼ぶ側が両方を気にしなくて済むように、ここで吸収する。
        /// </summary>
        private static void SetButtonFace(Button button, string label, Sprite icon)
        {
            if (button == null)
            {
                return;
            }

            var info = button.GetComponent<LongPressInfo>();
            if (info != null)
            {
                info.Title = label;
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
                return;
            }

            if (icon == null)
            {
                return;
            }

            var images = button.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                // 背景ではなく、中の絵だけを差し替える。
                if (image.gameObject != button.gameObject)
                {
                    image.sprite = icon;
                    image.enabled = true;
                    return;
                }
            }
        }

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
            tooltipRoot.sizeDelta = new Vector2(200f, 0f);

            UiFactory.AddVerticalLayout(tooltipRoot, 10, 0f);
            UiFactory.AddContentHeight(tooltipRoot);

            // 名前だけを出す。以前は説明文も並べていたが、指で押さえている最中に
            // 3〜4行の文章が地球へ覆いかぶさり、かえって読み取りの邪魔になっていた。
            // 説明は「説明を出す」で右の面に出せるので、ここでは繰り返さない。
            tooltipTitle = UiFactory.CreateText(
                tooltipRoot, "Title", 16, TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);

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
