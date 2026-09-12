using System;
using System.Text;
using CivilizationToSpace.Sim;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CivilizationToSpace.View
{
    /// <summary>
    /// 形成の計算を操作する画面。
    ///
    /// 出す文字は「段階の名前」と「測った数」だけにする。年代や規模を書かない。
    /// 条件を画面から変えられるようにしてあるのは、どの値がどこに効くかを
    /// 手で確かめられるようにするためである。
    /// </summary>
    public sealed class SimulationHud : MonoBehaviour
    {
        private static readonly Color PanelColor = new Color(0.05f, 0.07f, 0.11f, 0.88f);
        private static readonly Color BarColor = new Color(0.05f, 0.07f, 0.11f, 0.92f);
        private static readonly Color TextColor = new Color(0.90f, 0.93f, 0.96f, 1f);
        private static readonly Color DimTextColor = new Color(0.66f, 0.72f, 0.79f, 1f);
        private static readonly Color AccentColor = new Color(0.95f, 0.70f, 0.40f, 1f);
        private static readonly Color ButtonColor = new Color(0.13f, 0.18f, 0.25f, 1f);
        private static readonly Color TrackColor = new Color(0.18f, 0.22f, 0.29f, 1f);

        /// <summary>速さの選択肢。計算の刻みは変えず、1フレームで進める量を変える。</summary>
        private static readonly float[] Speeds = { 0.5f, 1f, 2f, 4f };

        private static readonly int[] ParticleChoices = { 120, 200, 280, 380, 520, 700 };
        private static readonly float[] EjectaChoices = { 0.006f, 0.010f, 0.017f, 0.026f, 0.040f };
        private static readonly float[] AimChoices = { 0f, 0.4f, 0.8f, 1.2f, 1.6f };

        private static readonly string[] AimLabels =
        {
            "正面から", "やや斜めから", "斜めから", "かすめて", "大きくかすめて"
        };

        /// <summary>破片が集まる力の倍率。1のままだと月にならないことを見せるため、1を残す。</summary>
        private static readonly float[] AttractionChoices = { 1f, 10f, 30f, 60f, 100f };

        private static readonly string[] AttractionLabels =
        {
            "1倍（そのまま）", "10倍", "30倍", "60倍", "100倍"
        };

        private SimSettings settings;

        private Text phaseText;
        private Text statusText;
        private Button playButton;
        private Button speedButton;
        private Button settingsButton;
        private RectTransform settingsRoot;

        private Text particleValue;
        private Text ejectaValue;
        private Text aimValue;
        private Text attractionValue;
        private Text seedValue;

        private int speedIndex = 1;
        private bool paused;
        private bool settingsVisible;

        private readonly StringBuilder builder = new StringBuilder();

        /// <summary>一時停止しているか。<see cref="SimRoot"/> が毎フレーム読む。</summary>
        public bool Paused
        {
            get { return paused; }
        }

        /// <summary>いま選ばれている速さ。</summary>
        public float Speed
        {
            get { return Speeds[Mathf.Clamp(speedIndex, 0, Speeds.Length - 1)]; }
        }

        /// <summary>「やり直す」が押されたときに呼ばれる。新しい種を渡す。</summary>
        public Action<int> RestartRequested;

        /// <summary>「視点をもどす」が押されたときに呼ばれる。</summary>
        public Action ResetViewRequested;

        public void Build(Camera camera, SimSettings simSettings)
        {
            settings = simSettings;
            EnsureEventSystem();

            var canvas = UiFactory.CreateCanvas("SimHud", camera);
            var root = (RectTransform)canvas.transform;

            BuildHeader(root);
            BuildSettingsPanel(root);
            BuildBottomBar(root);
            BuildViewHint(root);

            SetSettingsVisible(false);
            RefreshSpeedLabel();
            RefreshPlayLabel();
        }

        /// <summary>毎フレーム、測った数を書き換える。数は転記せず、その場の値だけを出す。</summary>
        public void Refresh(AccretionSimulation simulation)
        {
            if (simulation == null || phaseText == null)
            {
                return;
            }

            phaseText.text = PhaseLabel(simulation.Phase);

            builder.Length = 0;
            builder.Append("天体 ").Append(simulation.Count).Append("個");
            builder.Append("　合体 ").Append(simulation.MergeEvents).Append("回");
            builder.Append("　いちばん重い塊 ")
                .Append((simulation.TotalLiveMass > 0f
                    ? simulation.LargestMass / simulation.TotalLiveMass * 100f
                    : 0f).ToString("0"))
                .Append("%");
            builder.Append("　経過 ").Append(simulation.Time.ToString("0.0"));

            statusText.text = builder.ToString();
        }

        private static string PhaseLabel(SimPhase phase)
        {
            switch (phase)
            {
                case SimPhase.Accretion:
                    return "微惑星がぶつかりながら集まっている";
                case SimPhase.ProtoEarth:
                    return "ひとつの塊になった（原始地球）";
                case SimPhase.Impactor:
                    return "別の天体が近づいている";
                case SimPhase.Impact:
                    return "ぶつかった。破片が飛び散った";
                case SimPhase.MoonForming:
                    return "破片がまわりながら集まっている";
                case SimPhase.Settled:
                    return "地球と月になった";
                default:
                    return string.Empty;
            }
        }

        // ------------------------------------------------------------------
        // 組み立て
        // ------------------------------------------------------------------

        private void BuildHeader(RectTransform root)
        {
            var header = UiFactory.CreateRect(root, "Header");
            UiFactory.TopLeft(header, 20f, 14f, UiFactory.ReferenceResolution.x - 40f, 122f);
            UiFactory.AddVerticalLayout(header, 0, 4f);

            UiFactory.CreateText(header, "Title", 24, TextColor, TextAnchor.UpperLeft, FontStyle.Bold)
                .text = "地球ができるまで（重力の計算で動かす）";

            phaseText = UiFactory.CreateText(
                header, "Phase", 18, AccentColor, TextAnchor.UpperLeft, FontStyle.Bold);

            statusText = UiFactory.CreateText(
                header, "Status", 13, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal);

            UiFactory.CreateText(header, "Disclaimer", 12, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal)
                .text = "天体どうしの引力を計算して動かしています。ただし単位のない相対値であり、" +
                        "実際の質量・距離・年数ではありません。衝突で破片が飛び散る過程は計算しておらず、" +
                        "決めた量の破片を決めた範囲へ置く模型に置き換えています。";
        }

        private void BuildBottomBar(RectTransform root)
        {
            var bar = UiFactory.CreatePanel(root, "SimBar", BarColor);
            var barRect = bar.rectTransform;
            UiFactory.BottomBar(barRect, 60f, 20f, 20f);
            UiFactory.AddHorizontalLayout(barRect, 8, 8f);

            playButton = UiFactory.CreateButton(barRect, "Play", 15, ButtonColor, TextColor);
            UiFactory.SetWidth(playButton.gameObject, 128f, 0f);
            playButton.onClick.AddListener(TogglePause);

            speedButton = UiFactory.CreateButton(barRect, "Speed", 15, ButtonColor, TextColor);
            UiFactory.SetWidth(speedButton.gameObject, 108f, 0f);
            speedButton.onClick.AddListener(CycleSpeed);

            var spacer = UiFactory.CreateRect(barRect, "Spacer");
            UiFactory.SetWidth(spacer.gameObject, 0f, 1f);

            var resetView = UiFactory.CreateButton(barRect, "ResetView", 14, ButtonColor, TextColor);
            resetView.GetComponentInChildren<Text>().text = "視点をもどす";
            UiFactory.SetWidth(resetView.gameObject, 128f, 0f);
            resetView.onClick.AddListener(delegate
            {
                if (ResetViewRequested != null)
                {
                    ResetViewRequested();
                }
            });

            var restart = UiFactory.CreateButton(barRect, "Restart", 14, ButtonColor, TextColor);
            restart.GetComponentInChildren<Text>().text = "別の種でやり直す";
            UiFactory.SetWidth(restart.gameObject, 160f, 0f);
            restart.onClick.AddListener(delegate { Restart(settings.Seed + 1); });

            settingsButton = UiFactory.CreateButton(barRect, "Settings", 14, ButtonColor, TextColor);
            UiFactory.SetWidth(settingsButton.gameObject, 112f, 0f);
            settingsButton.onClick.AddListener(delegate { SetSettingsVisible(!settingsVisible); });
        }

        private void BuildSettingsPanel(RectTransform root)
        {
            var panel = UiFactory.CreatePanel(root, "SettingsPanel", PanelColor);
            settingsRoot = panel.rectTransform;
            UiFactory.RightColumn(settingsRoot, 0.42f, 20f, 118f, 74f);

            var content = UiFactory.CreateRect(settingsRoot, "Content");
            UiFactory.Stretch(content, 0f, 0f, 0f, 0f);
            UiFactory.AddVerticalLayout(content, 16, 6f);

            UiFactory.CreateText(content, "Heading", 17, TextColor, TextAnchor.UpperLeft, FontStyle.Bold)
                .text = "条件を変える";

            UiFactory.CreateText(content, "Note", 12, DimTextColor, TextAnchor.UpperLeft, FontStyle.Normal)
                .text = "変えたら下の「この条件でやり直す」を押します。";

            particleValue = AddSliderRow(
                content, "Particles", "微惑星の数", ParticleChoices.Length,
                IndexOf(ParticleChoices, settings.PlanetesimalCount),
                delegate(int index)
                {
                    settings.PlanetesimalCount = ParticleChoices[index];
                    particleValue.text = ParticleChoices[index] + "個";
                });
            particleValue.text = settings.PlanetesimalCount + "個";

            aimValue = AddSliderRow(
                content, "Aim", "衝突天体の当て方", AimChoices.Length,
                NearestIndex(AimChoices, settings.ImpactParameter),
                delegate(int index)
                {
                    settings.ImpactParameter = AimChoices[index];
                    aimValue.text = AimLabels[index];
                });
            aimValue.text = AimLabels[NearestIndex(AimChoices, settings.ImpactParameter)];

            ejectaValue = AddSliderRow(
                content, "Ejecta", "破片になる質量", EjectaChoices.Length,
                NearestIndex(EjectaChoices, settings.EjectaMassFraction),
                delegate(int index)
                {
                    settings.EjectaMassFraction = EjectaChoices[index];
                    ejectaValue.text = (EjectaChoices[index] * 100f).ToString("0.0") + "%";
                });
            ejectaValue.text = (settings.EjectaMassFraction * 100f).ToString("0.0") + "%";

            // 破片が集まる力は「時間を縮めるために強めている」箇所そのものである。
            // 隠さず画面へ出し、1倍に戻すと月にならないことを確かめられるようにする。
            attractionValue = AddSliderRow(
                content, "Attraction", "破片が集まる力", AttractionChoices.Length,
                NearestIndex(AttractionChoices, settings.DebrisAttraction),
                delegate(int index)
                {
                    settings.DebrisAttraction = AttractionChoices[index];
                    attractionValue.text = AttractionLabels[index];
                });
            attractionValue.text = AttractionLabels[NearestIndex(AttractionChoices, settings.DebrisAttraction)];

            var seedRow = UiFactory.CreateRect(content, "SeedRow");
            UiFactory.AddHorizontalLayout(seedRow, 0, 8f);
            seedRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            UiFactory.CreateText(seedRow, "SeedLabel", 13, DimTextColor, TextAnchor.MiddleLeft, FontStyle.Normal)
                .text = "種";
            seedValue = UiFactory.CreateText(
                seedRow, "SeedValue", 13, TextColor, TextAnchor.MiddleLeft, FontStyle.Normal);
            seedValue.text = settings.Seed.ToString();
            UiFactory.SetWidth(seedValue.gameObject, 0f, 1f);

            var apply = UiFactory.CreateButton(content, "Apply", 14, ButtonColor, TextColor);
            apply.GetComponentInChildren<Text>().text = "この条件でやり直す";
            apply.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
            apply.onClick.AddListener(delegate { Restart(settings.Seed); });
        }

        /// <summary>ひとつぶんの行。見出し・値・つまみを縦に並べる。</summary>
        private Text AddSliderRow(
            RectTransform parent, string name, string label, int steps, int initialIndex, Action<int> onChange)
        {
            var row = UiFactory.CreateRect(parent, name);
            UiFactory.AddVerticalLayout(row, 0, 2f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

            var head = UiFactory.CreateRect(row, "Head");
            UiFactory.AddHorizontalLayout(head, 0, 6f);
            head.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            UiFactory.CreateText(head, "Label", 13, DimTextColor, TextAnchor.MiddleLeft, FontStyle.Normal)
                .text = label;

            var value = UiFactory.CreateText(head, "Value", 13, TextColor, TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.SetWidth(value.gameObject, 0f, 1f);

            var sliderHost = UiFactory.CreateRect(row, "SliderHost");
            sliderHost.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            var slider = UiFactory.CreateSlider(sliderHost, "Slider", steps, TrackColor, AccentColor, TextColor);
            UiFactory.Stretch((RectTransform)slider.transform, 0f, 4f, 0f, 4f);
            slider.value = Mathf.Clamp(initialIndex, 0, steps - 1);
            slider.onValueChanged.AddListener(delegate(float raw) { onChange(Mathf.RoundToInt(raw)); });

            return value;
        }

        private void BuildViewHint(RectTransform root)
        {
            var hint = UiFactory.CreateText(
                root, "ViewHint", 13, DimTextColor, TextAnchor.LowerLeft, FontStyle.Normal);
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.zero;
            hint.rectTransform.pivot = Vector2.zero;
            hint.rectTransform.anchoredPosition = new Vector2(24f, 90f);
            hint.rectTransform.sizeDelta = new Vector2(700f, 40f);
            hint.text = Application.isMobilePlatform
                ? "画面を指でなぞると視点が回り、指2本の間隔を変えると寄ります。計算は止まりません。"
                : "ドラッグで視点が回り、ホイールで寄ります。計算は止まりません。";
        }

        // ------------------------------------------------------------------
        // 操作
        // ------------------------------------------------------------------

        private void TogglePause()
        {
            paused = !paused;
            RefreshPlayLabel();
        }

        private void CycleSpeed()
        {
            speedIndex = (speedIndex + 1) % Speeds.Length;
            RefreshSpeedLabel();
        }

        private void Restart(int seed)
        {
            settings.Seed = seed;
            if (seedValue != null)
            {
                seedValue.text = seed.ToString();
            }

            if (RestartRequested != null)
            {
                RestartRequested(seed);
            }
        }

        private void SetSettingsVisible(bool visible)
        {
            settingsVisible = visible;
            if (settingsRoot != null)
            {
                settingsRoot.gameObject.SetActive(visible);
            }

            if (settingsButton != null)
            {
                settingsButton.GetComponentInChildren<Text>().text = visible ? "設定を閉じる" : "条件を変える";
            }
        }

        private void RefreshPlayLabel()
        {
            if (playButton != null)
            {
                playButton.GetComponentInChildren<Text>().text = paused ? "再生" : "一時停止";
            }
        }

        private void RefreshSpeedLabel()
        {
            if (speedButton != null)
            {
                speedButton.GetComponentInChildren<Text>().text = "速さ ×" + Speed.ToString("0.#");
            }
        }

        private static int IndexOf(int[] choices, int value)
        {
            for (var i = 0; i < choices.Length; i++)
            {
                if (choices[i] == value)
                {
                    return i;
                }
            }

            return choices.Length / 2;
        }

        private static int NearestIndex(float[] choices, float value)
        {
            var best = 0;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < choices.Length; i++)
            {
                var distance = Mathf.Abs(choices[i] - value);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = i;
            }

            return best;
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
    }
}
