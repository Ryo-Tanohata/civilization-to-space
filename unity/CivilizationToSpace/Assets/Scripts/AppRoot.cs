using System.IO;
using System.Text;
using CivilizationToSpace.Core;
using CivilizationToSpace.View;
using UnityEngine;

namespace CivilizationToSpace
{
    /// <summary>
    /// R1-P1の起点。StreamingAssets のコピーを読み、地球と説明を組み立てる。
    ///
    /// S3では時代の選択までを扱う。時系列再生・自転・視点操作はS4で足す。
    /// static も Singleton も作らない。R2-P1はこのコンポーネントの参照だけに依存する。
    /// </summary>
    [ExecuteAlways]
    public sealed class AppRoot : MonoBehaviour
    {
        [Tooltip("Playしていないときも、シーンビューに地球を出します。出したものはシーンへ保存されません。")]
        [SerializeField]
        private bool showEarthWhileEditing = true;

        [Tooltip("編集中のプレビューに使う時代の位置。0が最初の時代です。")]
        [SerializeField]
        private int previewEraIndex;

        /// <summary>地球を置く位置。画面の左寄りに見えるよう、カメラを右へずらしてある。</summary>
        private static readonly Vector3 EarthPosition = Vector3.zero;

        /// <summary>編集中のプレビューに付ける名前。実行時の Earth と取り違えないようにする。</summary>
        private const string PreviewName = "Earth (編集中プレビュー・保存されません)";

        private CatalogLoadResult result;
        private EraTimeline timeline;
        private TimelinePlayback playback;
        private MotionSettings motion;
        private EarthView earth;
        private MoonView moon;
        private DemoHud hud;
        private MoonExpansionLoader.Result moonResult;
        private EarthFormationLoader.Result formationResult;
        private FormationView formation;
        private EarthFraming framing;

        /// <summary>検証済みカタログ。読込に失敗した場合は null。</summary>
        public EraCatalog Catalog
        {
            get { return result != null ? result.Catalog : null; }
        }

        /// <summary>時代の選択状態。読込に失敗した場合は null。</summary>
        public EraTimeline Timeline
        {
            get { return timeline; }
        }

        /// <summary>
        /// 自動再生。R2-P1はここから IsPlaying を読み、RequestStop を呼ぶ。
        /// それ以外に依存しない。
        /// </summary>
        public TimelinePlayback Playback
        {
            get { return playback; }
        }

        /// <summary>読込に失敗したかどうか。真のとき操作を無効化する。</summary>
        public bool LoadFailed
        {
            get { return result == null || !result.Ok; }
        }

        /// <summary>画面へ出してよい失敗の文言。成功時は空文字。</summary>
        public string FailureMessage
        {
            get { return result != null ? result.UserMessage : string.Empty; }
        }

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            // 場面の時計は static なので、前回の再生で止めたままだと
            // 次の起動でも止まったまま始まってしまう。必ず通常へ戻す。
            View.SceneClock.Resume();

            Load();
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            BuildView();
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                return;
            }

            BuildEditPreview();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ClearEditPreview();
        }

        private void OnValidate()
        {
            if (Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            // インスペクタで値を変えたら、プレビューを組み直す。
            BuildEditPreview();
        }

        /// <summary>
        /// Playしていないときのプレビュー。シーンビューで地球を見られるようにする。
        ///
        /// 生成物には DontSave を付ける。シーンへ保存されず、.unity の差分も増えない。
        /// UIは作らない。画面いっぱいのCanvasはシーンビューでは邪魔になるためである。
        /// </summary>
        private void BuildEditPreview()
        {
            ClearEditPreview();

            if (!showEarthWhileEditing)
            {
                return;
            }

            var loaded = EraCatalogLoader.LoadFromFile(ResolveCatalogPath());
            if (!loaded.Ok)
            {
                return;
            }

            var host = new GameObject(PreviewName);
            host.hideFlags = HideFlags.DontSave;
            host.transform.SetParent(transform, false);
            host.transform.position = EarthPosition;

            var preview = host.AddComponent<EarthView>();
            preview.Build(HideFlags.DontSave);

            var eras = loaded.Catalog.Eras;
            var index = Mathf.Clamp(previewEraIndex, 0, eras.Count - 1);
            preview.Apply(eras[index].Visual, index);
        }

        private void ClearEditPreview()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name != PreviewName)
                {
                    continue;
                }

                var view = child.GetComponent<EarthView>();
                if (view != null)
                {
                    view.Clear();
                }

                DestroyImmediate(child.gameObject);
            }
        }

        /// <summary>
        /// 検証のために読込先を差し替えるためのキー。エディタでのみ効く。
        /// 異常系を確かめるとき、リポジトリ内のJSONを書き換えず、一時フォルダの複製を読ませる。
        /// </summary>
        public const string CatalogPathOverrideKey = "CivilizationToSpace.CatalogPathOverride";

        /// <summary>
        /// 段階に応じてカメラの引きを変える。形成過程はぶつかってくる天体まで、
        /// 月への展開は月まで画面へ入れる。時代のあいだは地球だけを見る。
        /// </summary>
        private void ApplyFraming()
        {
            if (framing == null)
            {
                return;
            }

            // どの段階でも同じ引きにする。宇宙から地球と月を眺めている、という一つの視点を
            // 通して保つためである。段階ごとに引きを変えると、進めるたびに画面が飛び、
            // 地球へ寄ったり離れたりして、同じ場所を見ている感じが途切れる。
            //
            // 必要な広さは2つあり、広いほうに合わせる。
            //   ・月の軌道まで入る広さ（月が画面から出ないように）
            //   ・微惑星が現れる位置まで入る広さ（外から近づく様子が見えるように）
            var wideRadius = Mathf.Max(
                MoonView.FramedRadius,
                formation != null ? formation.FramedRadius : 0f);

            var wide = wideRadius > 0f;
            if (framing.WideMode == wide && Mathf.Approximately(framing.WideRadius, wideRadius))
            {
                return;
            }

            framing.WideMode = wide;
            framing.WideRadius = wideRadius;
            framing.Zoom = 1f;
            framing.Apply();
        }

        private static string ResolveMoonPath()
        {
            return Path.Combine(Application.streamingAssetsPath, MoonExpansionLoader.FileName);
        }

        private static string ResolveCatalogPath()
        {
#if UNITY_EDITOR
            var overridePath = UnityEditor.SessionState.GetString(CatalogPathOverrideKey, string.Empty);
            if (!string.IsNullOrEmpty(overridePath))
            {
                return overridePath;
            }
#endif
            return Path.Combine(Application.streamingAssetsPath, EraCatalogLoader.EraCatalogFileName);
        }

        private void Load()
        {
            // 絶対パスにはユーザー名が含まれる。本リポジトリはPublicであるため、パスを出力しない。
            var path = ResolveCatalogPath();
            result = EraCatalogLoader.LoadFromFile(path);

            if (LoadFailed)
            {
                Debug.LogError("[EraCatalog] 読込に失敗しました: " + FailureMessage);
                return;
            }

            timeline = new EraTimeline(result.Catalog.Eras);

            // 形成過程も月への展開も時代ではない。時代データを増やさず、
            // 時系列の手前と先へ段階として足す。
            formationResult = EarthFormationLoader.LoadFromFile(
                Path.Combine(Application.streamingAssetsPath, EarthFormationLoader.FileName));
            if (!formationResult.Ok)
            {
                Debug.LogWarning("[EarthFormation] 読み込めませんでした: " + formationResult.UserMessage);
            }

            moonResult = MoonExpansionLoader.LoadFromFile(ResolveMoonPath());
            if (!moonResult.Ok)
            {
                Debug.LogWarning("[MoonExpansion] 読み込めませんでした: " + moonResult.UserMessage);
            }

            timeline.SetOuterCounts(
                formationResult.Ok ? formationResult.Formation.Stages.Count : 0,
                moonResult.Ok ? moonResult.Expansion.Phases.Count : 0);

            playback = new TimelinePlayback(timeline);
            motion = new MotionSettings();
            Debug.Log(BuildSummary(result));
        }

        private void BuildView()
        {
            var camera = Camera.main;

            // 解決を先に走らせてから名前を読む。どのフォントが採られたかを報告へ残すため。
            JapaneseFont.Get();
            Debug.Log("[UI] 日本語フォント: " + JapaneseFont.ResolvedName +
                      (JapaneseFont.FellBackToBuiltin ? "（内蔵へフォールバック）" : string.Empty));

            hud = new GameObject("Hud").AddComponent<DemoHud>();
            hud.transform.SetParent(transform, false);

            if (LoadFailed)
            {
                hud.Build(camera, "Earth Through Time", string.Empty, string.Empty);
                hud.ShowError(FailureMessage);
                return;
            }

            var catalog = result.Catalog;

            var earthObject = new GameObject("Earth");
            earthObject.transform.SetParent(transform, false);
            earthObject.transform.position = EarthPosition;
            earth = earthObject.AddComponent<EarthView>();
            earth.SetMotionSettings(motion);
            earth.Build();

            framing = AttachFraming(camera);

            if (formationResult != null && formationResult.Ok)
            {
                var formationObject = new GameObject("Formation");
                formationObject.transform.SetParent(transform, false);
                formationObject.transform.position = EarthPosition;
                formation = formationObject.AddComponent<FormationView>();
                formation.SetMotionSettings(motion);
                formation.Build(earth.transform, EarthView.Radius, HideFlags.None);
            }

            if (moonResult != null && moonResult.Ok)
            {
                var moonObject = new GameObject("Moon");
                moonObject.transform.SetParent(transform, false);
                moon = moonObject.AddComponent<MoonView>();
                moon.SetMotionSettings(motion);
                moon.Build(EarthPosition, HideFlags.None);
            }

            hud.Build(camera, catalog.Title, catalog.Disclaimer, catalog.ParameterNote);

            timeline.Changed += OnEraChanged;
            hud.Bind(
                timeline,
                playback,
                motion,
                framing,
                moonResult != null && moonResult.Ok ? moonResult.Expansion : null,
                formationResult != null && formationResult.Ok ? formationResult.Formation : null);

            earth.Apply(timeline.Current.Visual, timeline.CurrentEraIndex);
            ApplyFormationStage();
            ApplyMoonPhase();
            ApplyFraming();
        }

        /// <summary>
        /// カメラの位置を画面の大きさから決め直させる。
        /// 固定値のままだと、ウィンドウの縦横比によって地球がはみ出す。
        /// </summary>
        private static EarthFraming AttachFraming(Camera camera)
        {
            if (camera == null)
            {
                return null;
            }

            var framing = camera.GetComponent<EarthFraming>();
            if (framing == null)
            {
                framing = camera.gameObject.AddComponent<EarthFraming>();
            }

            framing.Target = EarthPosition;
            framing.Radius = EarthView.Radius;
            framing.Zoom = 1f;
            framing.Apply();

            if (camera.GetComponent<EarthCameraControl>() == null)
            {
                camera.gameObject.AddComponent<EarthCameraControl>();
            }

            return framing;
        }

        /// <summary>
        /// 動きを減らす設定を外から切り替える。点検ツールが使う。
        /// 画面のトグルと同じ経路を通す。
        /// </summary>
        public void SetReducedMotionForTesting(bool reduced)
        {
            if (motion != null)
            {
                motion.Reduced = reduced;
            }
        }

        /// <summary>
        /// 1フレームの終わりに、コマ送りの1回ぶんを使い切る。
        /// 場面を動かす側がすべて読み終えたあとで消す必要があるため、
        /// Update ではなく LateUpdate で行う。
        /// </summary>
        private void LateUpdate()
        {
            View.SceneClock.EndFrame();
        }

        private void Update()
        {
            if (!Application.isPlaying || playback == null)
            {
                return;
            }

            // 自動再生も場面の時計に合わせる。コマ送り中に段階だけ進むと、
            // 絵が止まっているのに段が変わることになる。
            playback.Tick(View.SceneClock.Delta);

            // 月が破片から集まってくる様子を、大きさの変化で表す。
            if (moon != null && formation != null)
            {
                moon.SetBodyScale(timeline.HeadIndex >= 0 ? formation.MoonEmergence : 1f);
            }

            // まだ焼いていない時代を1フレームに1つずつ用意する。
            // 再生中に初めて使う時代を焼くと、その瞬間だけ画面が止まる。
            if (earth != null && result != null && result.Ok)
            {
                earth.BakeNext(result.Catalog.Eras);
            }
        }

        private void OnEraChanged(EraData era)
        {
            // 手前・先の段階にいるあいだ、地球は端の時代の姿のまま保つ。
            earth.Apply(era.Visual, timeline.CurrentEraIndex);
            ApplyFormationStage();
            ApplyMoonPhase();
            ApplyFraming();
        }

        /// <summary>
        /// 形成過程の段階を反映する。育ちかけの塊を表すため、地球そのものを小さくする。
        /// </summary>
        private void ApplyFormationStage()
        {
            if (formation == null || formationResult == null || !formationResult.Ok)
            {
                return;
            }

            var head = timeline.HeadIndex;
            var stages = formationResult.Formation.Stages;
            var stage = head >= 0 && head < stages.Count ? stages[head] : null;

            // 塊の大きさは FormationView が時間をかけて動かす。ここでは指示だけ出す。
            // 先頭の段階だけは、何も無いところから集まってくる様子を見せる。
            formation.Apply(stage, head == 0);
        }

        /// <summary>
        /// 月の段階を反映する。時代を見ているあいだは何も無い状態へ戻し、
        /// カメラも通常の引きへ戻す。
        /// </summary>
        private void ApplyMoonPhase()
        {
            if (moon == null || moonResult == null || !moonResult.Ok)
            {
                return;
            }

            var tail = timeline.TailIndex;
            var phases = moonResult.Expansion.Phases;
            moon.Apply(tail >= 0 && tail < phases.Count ? phases[tail] : null);
        }

        private static string BuildSummary(CatalogLoadResult loaded)
        {
            var catalog = loaded.Catalog;
            var builder = new StringBuilder();
            builder.Append("[EraCatalog] SHA-256 ").Append(loaded.Sha256).Append('\n');
            builder.Append("件数 ").Append(catalog.Eras.Count).Append('\n');
            builder.Append("見出し ").Append(catalog.Title).Append('\n');

            for (var i = 0; i < catalog.Eras.Count; i++)
            {
                var era = catalog.Eras[i];
                builder.Append(i + 1).Append('/').Append(catalog.Eras.Count).Append(' ')
                    .Append(era.Id).Append(" | ").Append(era.DisplayName)
                    .Append(" | ").Append(era.RangeLabel)
                    .Append(" | sortOrder ").Append(era.SortOrder)
                    .Append(" | ").Append(era.Status)
                    .Append(" | タグ ").Append(era.Tags.Count)
                    .Append(" | 代表イベント ").Append(era.Events.Count)
                    .Append(" | 将来シナリオ候補 ").Append(era.Scenarios.Count)
                    .Append(era.VisualDegraded ? " | 視覚値を簡略表示へ縮退" : string.Empty)
                    .Append('\n');
            }

            return builder.ToString().TrimEnd('\n');
        }
    }
}
