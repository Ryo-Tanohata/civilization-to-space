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

        /// <summary>
        /// 視線の軸。地球から見て、既定のカメラがいる向き。
        ///
        /// <see cref="View.EarthFraming"/> が既定（向き0・寄り1）で
        /// <c>Vector3.back</c> を使うので、それに合わせる。
        /// 太陽の向きはこの軸を基準に決める。理由は <see cref="SunRotation"/> にある。
        /// </summary>
        private static readonly Vector3 ViewAxis = Vector3.back;

        /// <summary>
        /// 見えている円板のうち、昼が占める割合。年を通して変えない。
        ///
        /// **なぜ一定にするのか。**
        /// 太陽を本当に一周させると、カメラから見た地球は月と同じように満ち欠けし、
        /// 1年に一度は完全な夜の側だけが見える。40秒ごとに地球が真っ暗になり、
        /// 時代ごとの地表を見るための画面としては成り立たない。
        /// 計算で確かめたところ、明るい割合は0%から100%まで振れていた。
        ///
        /// 0.857は、これまでの固定の光と同じ明るさになる値である。
        /// 以前の Euler(28, -36, 0) を視線の軸へ射影すると 0.714 で、
        /// 昼の割合に直すと (1 + 0.714) / 2 = 0.857 にあたる。
        /// 同じ値にしておけば、**季節が付いたこと以外は見え方が変わらない。**
        /// 前の版で測った昼夜の差（昼129／夜10）もそのまま保てる。
        ///
        /// 下げるほど昼夜の境目が円板の内側へ寄り、季節の傾きは読みやすくなるが、
        /// 地表を見る面積が減る。0.70で試したときは、昼の見える割合が
        /// 実測で約80%から約53%まで落ちた。
        /// **この値は見え方で決めた。太陽との距離や位置を表すものではない。**
        /// </summary>
        public const float LitFraction = 0.857f;

        /// <summary>
        /// 1年の長さ（秒）。自転10回ぶんにしてある。
        ///
        /// **なぜ自転の10倍なのか。**
        /// この見せ方で動く光は「季節のうなずき」だけである。うなずきが自転より
        /// 速いと、季節ではなく照明が揺れているように見える。1年が1日の
        /// 数倍あって初めて、日が過ぎて季節が移る、という順に読める。
        ///
        /// はじめは1時代ぶん（4秒）にしていたが、そのためには自転を1周1秒まで
        /// 速める必要があり、目で追うには速すぎた。自転を落ち着かせると、
        /// 年もその比のぶんだけ伸びる。通しで見ると季節がおよそ2回巡る。
        ///
        /// **実際の1年を表す秒数ではない。** 再生の速度を変えても年の長さは変えない。
        /// 速度は段階の進み方を変えるものであり、天体の動きまで速めると、
        /// 8倍速で自転が1周1.25秒になって地表が読めなくなる。
        /// </summary>
        public const float YearSeconds = 40f;

        /// <summary>
        /// 太陽の強さ。
        ///
        /// **2.4から1.2へ下げた。**
        /// 2.4は、地球を囲む雲と大気の球が地表へ影を落としていたぶんを
        /// 補うための値だった（<see cref="View.EarthView"/> の CreateSphere にある）。
        /// 影を落とさないようにしたところ、そのままでは明るすぎて、
        /// Hadean と Snowball では円板の4割以上が白へ飽和した。
        ///
        /// 1.2にすると、白飛びは最大1.1%まで下がり、明るさは影があったころの
        /// 約2倍、昼と夜の差は12.8倍になる。前の版が狙っていた差（昼129／夜10、
        /// およそ12.9倍）とほぼ同じである。
        /// </summary>
        public const float SunIntensity = 1.2f;

        /// <summary>
        /// 環境光。どこからともなく当たる明るさで、夜側の暗さを決める。
        ///
        /// 以前は (0.16, 0.18, 0.23) で、夜側もかなり見えていた。
        /// 宇宙には空気が無く、光を回り込ませるものが無い。
        /// 落とすほど昼と夜の差がはっきりする。完全な0にしないのは、
        /// 夜側が真っ黒になると輪郭も地形も読めなくなるためである。
        /// </summary>
        public static readonly Color SpaceAmbient = new Color(0.040f, 0.048f, 0.066f, 1f);

        /// <summary>
        /// 年の進み具合から、太陽（平行光）の向きを作る。0で年の初め、1で一周ぶん。
        ///
        /// **何を表しているか。**
        /// 太陽の向きを、次の3つの向きへの成分に分けて組み立てる。
        ///   ・視線の軸（<see cref="ViewAxis"/>）……昼の割合を決める。年じゅう一定
        ///   ・地軸（<see cref="View.EarthView.AxisDirection"/>）……季節を決める。年で上下に振れる
        ///   ・残り（上の2つに直交する向き）……長さを1に保つぶん
        ///
        /// 地軸は視線の軸と直交させてあるので、この2つの成分は互いに干渉しない。
        /// 昼の割合を一定に保ったまま、太陽の正面へ来る緯度だけをちょうど
        /// ±<see cref="View.EarthView.AxialTiltDegrees"/> 度の範囲で振れる。
        ///
        /// **公転を一周させていない。往復である。**
        /// 一周させると1年に一度カメラから見て完全な夜になり、地表が読めなくなる。
        /// ここでは季節として現れる成分だけを取り出している。
        /// 実際の公転・公転面・離心率・歳差のいずれも表していない。
        /// </summary>
        public static Quaternion SunRotation(float yearPhase)
        {
            var axis = EarthView.AxisDirection;

            // 春分・秋分にあたる向き。視線の軸とも地軸とも直交する。
            var equinox = Vector3.Cross(ViewAxis, axis).normalized;

            // 視線の軸ぶん。昼の割合はこの成分だけで決まるので、一定にしておく。
            var towardView = LitFraction * 2f - 1f;

            // 地軸ぶん。太陽の正面へ来る緯度の正弦にあたる。
            var towardAxis = Mathf.Sin(2f * Mathf.PI * yearPhase) *
                             Mathf.Sin(EarthView.AxialTiltDegrees * Mathf.Deg2Rad);

            // 残りを春分・秋分の向きへ回し、全体の長さを1に保つ。
            var towardEquinox = Mathf.Sqrt(
                Mathf.Max(0f, 1f - towardView * towardView - towardAxis * towardAxis));

            var towardSun = ViewAxis * towardView + axis * towardAxis + equinox * towardEquinox;

            // 平行光は自分の正面へ光を飛ばす。太陽がある向きとは逆を向かせる。
            return Quaternion.LookRotation(-towardSun, Vector3.up);
        }

        /// <summary>
        /// 太陽と環境光を場面へ当てる。年の初めの向きにする。
        ///
        /// 場面の資産にも同じ値が入っているが、実行時にここで入れ直す。
        /// 値の正本をひとつにしておかないと、場面を作り直したときだけ
        /// 見え方が変わる、という食い違いが起きる。
        /// </summary>
        public static void ApplySunLight(Light sun)
        {
            ApplySunLight(sun, 0f);
        }

        /// <summary>太陽と環境光を、指定した年の進み具合で場面へ当てる。</summary>
        public static void ApplySunLight(Light sun, float yearPhase)
        {
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.transform.rotation = SunRotation(yearPhase);
                sun.intensity = SunIntensity;
                sun.color = Color.white;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = SpaceAmbient;
        }

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

        /// <summary>季節で向きを変える太陽。平行光でなければ持たない。</summary>
        private Light sun;

        /// <summary>年の進み具合。0で年の初め、1で一周ぶん。</summary>
        private float yearPhase;

        /// <summary>地表から見た風景。宇宙から見ているあいだは消してある。</summary>
        private SurfaceView surface;

        /// <summary>いま地表を見ているか。</summary>
        private bool surfaceMode;

        /// <summary>1日の進み具合。0で真夜中、0.5で正午。</summary>
        private float dayPhase = 0.35f;

        /// <summary>地表で見せる衝突の進み具合。落ちて、光って、また落ちる。</summary>
        private float impactPhase;

        /// <summary>地表で見るときのカメラの置き場所。揺らすときの基準にする。</summary>
        private Vector3 surfaceEye;

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

            sun = FindAnyObjectByType<Light>();
            if (sun != null && sun.type != LightType.Directional)
            {
                // 平行光でなければ動かさない。向きだけでは位置が決まらないためである。
                sun = null;
            }

            ApplySunLight(sun);

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

            var surfaceObject = new GameObject("Surface");
            surfaceObject.transform.SetParent(transform, false);
            surface = surfaceObject.AddComponent<SurfaceView>();
            surfaceObject.SetActive(false);

            hud.Build(camera, catalog.Title, catalog.Disclaimer, catalog.ParameterNote);
            hud.SurfaceToggleRequested = ToggleSurface;

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
            ApplyDefaultView();
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
        /// 年の進み具合を外から決める。点検・キャプチャの道具が使う。
        ///
        /// 季節の端（夏至・冬至）を狙って撮るために要る。止めているあいだは
        /// 年が進まないので、外から入れないと春分の姿しか撮れない。
        /// </summary>
        public void SetYearPhaseForTesting(float phase)
        {
            yearPhase = phase - Mathf.Floor(phase);
            if (sun != null)
            {
                sun.transform.rotation = SunRotation(yearPhase);
            }
        }

        /// <summary>
        /// 地表と宇宙を切り替える。
        ///
        /// 宇宙の側の見せ物（地球・月・形成過程）は消し、カメラの置き方も変える。
        /// 残したままだと、地表の風景の中に地球が浮かぶことになる。
        ///
        /// カメラの枠決め（<see cref="EarthFraming"/>）と視点操作も止める。
        /// 止めないと、次の描画で地球を画面へ収める位置へ引き戻される。
        /// </summary>
        public void ToggleSurface()
        {
            SetSurfaceMode(!surfaceMode);
        }

        /// <summary>いま地表を見ているか。点検ツールが読む。</summary>
        public bool SurfaceMode
        {
            get { return surfaceMode; }
        }

        private void SetSurfaceMode(bool enabled)
        {
            surfaceMode = enabled;

            if (hud != null)
            {
                hud.SurfaceMode = enabled;
            }

            if (earth != null)
            {
                earth.gameObject.SetActive(!enabled);
            }

            if (moon != null)
            {
                moon.gameObject.SetActive(!enabled);
            }

            if (formation != null)
            {
                formation.gameObject.SetActive(!enabled);
            }

            var camera = Camera.main;
            if (framing != null)
            {
                framing.enabled = !enabled;
            }

            if (camera != null)
            {
                var control = camera.GetComponent<EarthCameraControl>();
                if (control != null)
                {
                    control.enabled = !enabled;
                }
            }

            if (surface != null)
            {
                surface.gameObject.SetActive(enabled);
            }

            if (enabled)
            {
                BuildSurface();
            }
            else
            {
                // 宇宙へ戻すときは、光と回り込む明るさを宇宙のものへ入れ直す。
                ApplySunLight(sun, yearPhase);
                if (framing != null)
                {
                    framing.Apply();
                }
            }
        }

        /// <summary>いまの時代の風景を組み、カメラを置き直す。</summary>
        private void BuildSurface()
        {
            if (surface == null || timeline == null)
            {
                return;
            }

            var era = timeline.CurrentEraIndex;
            surface.Build(SurfaceCatalog.ForEra(era), HideFlags.None);
            surface.SetTimeOfDay(dayPhase, sun);

            // 時代を移ったら、落ちてくるものは最初から見せる。
            impactPhase = 0f;
            surface.SetImpact(impactPhase);

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 eye;
            float pitch;
            SurfaceCatalog.EyeForEra(era, out eye, out pitch);
            surfaceEye = eye;
            camera.transform.position = eye;
            camera.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

            // 空の板は置く範囲よりさらに遠い。切り取られないよう遠くまで映す。
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, SurfaceCatalog.ForEra(era).FarZ * 3f);
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

        /// <summary>
        /// 太陽を1年ぶん進める。
        ///
        /// 動きを減らしているあいだは止める。自転を止めておいて光だけが動くと、
        /// 地球が止まっているのに昼夜の境目だけが動くことになる。
        ///
        /// 場面の時計を見るので、停止・コマ送りにもそのまま従う。
        /// </summary>
        private void AdvanceYear()
        {
            if (sun == null || (motion != null && motion.Reduced))
            {
                return;
            }

            yearPhase += View.SceneClock.Delta / YearSeconds;

            // 0以上1未満に畳む。長く動かしても値が育たず、精度が落ちない。
            yearPhase -= Mathf.Floor(yearPhase);

            sun.transform.rotation = SunRotation(yearPhase);
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (surfaceMode)
            {
                AdvanceDay();
            }
            else
            {
                // 読込に失敗していても太陽は動かす。止めると、失敗の画面だけ
                // 光の当たり方が違うことになり、切り分けの妨げになる。
                AdvanceYear();
            }

            if (playback == null)
            {
                return;
            }

            // 自動再生も場面の時計に合わせる。コマ送り中に段階だけ進むと、
            // 絵が止まっているのに段が変わることになる。
            playback.Tick(View.SceneClock.Delta);

            // 月が破片から集まってくる様子を、大きさの変化で表す。
            // 破片の寄せ先も渡す。形成の側は月がどこにできるかを知らないので、
            // ここで結び付けないと、破片が集まる先と月の位置が食い違う。
            //
            // **段階の位置で値を切り替えないこと。**
            // 以前は「形成過程を指していれば FormationView の値、そうでなければ既定値」
            // としていた。形成過程から時代へ移った瞬間に、溶けた赤みが1フレームで0へ落ち、
            // 月も一息に本来の大きさへ飛んでいた。画面では、そこだけ急に明るさが変わる。
            //
            // FormationView は段階が null になっても目標値へ寄せ続けており、
            // 溶け具合は2.4秒、月の大きさは1.3秒かけて戻る。そのまま渡せばよい。
            if (moon != null && formation != null)
            {
                formation.MoonAnchor = moon.MoonCenter - EarthPosition;
                moon.SetBodyScale(formation.MoonEmergence);
            }

            // できたばかりの地球と月は溶けていた。段階が進むと冷えて通常の色へ戻る。
            // 地球と月で同じ値を使い、片方だけ冷えて見えないようにする。
            if (formation != null)
            {
                var molten = formation.MoltenAmount;
                if (earth != null)
                {
                    earth.SetMolten(molten);
                }

                if (moon != null)
                {
                    moon.SetMolten(molten);
                }

                // ぶつかって抉れた部分を地球へ伝える。抉っていないときは半径が0になる。
                if (earth != null)
                {
                    earth.SetCut(EarthPosition + formation.CutCenter, formation.CutRadius);
                }
            }

            // まだ焼いていない時代を1フレームに1つずつ用意する。
            // 再生中に初めて使う時代を焼くと、その瞬間だけ画面が止まる。
            if (earth != null && result != null && result.Ok)
            {
                earth.BakeNext(result.Catalog.Eras);
            }
        }

        /// <summary>
        /// 地表の1日を進める。
        ///
        /// 動きを減らしているあいだは止める。止めた画面で空だけが明るくなると、
        /// 止めたようには見えない。
        /// </summary>
        private void AdvanceDay()
        {
            if (surface == null)
            {
                return;
            }

            if (motion == null || !motion.Reduced)
            {
                dayPhase += View.SceneClock.Delta / SurfaceView.DaySeconds;
                dayPhase -= Mathf.Floor(dayPhase);

                var seconds = SurfaceCatalog.ForEra(timeline != null ? timeline.CurrentEraIndex : 0)
                    .ImpactSeconds;
                if (seconds > 0f)
                {
                    impactPhase += View.SceneClock.Delta / seconds;
                    impactPhase -= Mathf.Floor(impactPhase);
                }
            }

            surface.SetTimeOfDay(dayPhase, sun);
            surface.SetImpact(impactPhase);

            // **ぶつかった瞬間はカメラを揺らす。**
            // 音も振動も出せないので、揺れだけが「ぶつかった」ことを伝える。
            var camera = Camera.main;
            if (camera != null)
            {
                var shake = surface.Shake;
                if (shake > 0f)
                {
                    var t = Time.time * 47f;
                    var amount = shake * shake * 1.6f;
                    camera.transform.position = surfaceEye + new Vector3(
                        Mathf.Sin(t) * amount,
                        Mathf.Sin(t * 1.7f + 1.1f) * amount,
                        0f);
                }
                else
                {
                    camera.transform.position = surfaceEye;
                }
            }
        }

        private void OnEraChanged(EraData era)
        {
            // 手前・先の段階にいるあいだ、地球は端の時代の姿のまま保つ。
            earth.Apply(era.Visual, timeline.CurrentEraIndex);
            ApplyFormationStage();
            ApplyMoonPhase();
            ApplyFraming();

            ApplyDefaultView();
        }

        /// <summary>
        /// その段階の既定の視点にする。
        ///
        /// **時代を移るたびに既定へ戻す。** 手で切り替えたぶんは、その時代を
        /// 見ているあいだだけ保たれる。持ち越すと、地表が見どころの時代へ来ても
        /// 宇宙のままになり、何も起きていないように見える。
        ///
        /// 形成過程と月への展開は必ず宇宙から見せる。地球ができることも
        /// 月へ出ていくことも、地球全体が見えていないと分からない。
        /// </summary>
        private void ApplyDefaultView()
        {
            if (timeline == null || surface == null)
            {
                return;
            }

            var wantSurface = timeline.InEra
                             && SurfaceCatalog.DefaultsToSurface(timeline.CurrentEraIndex);

            if (wantSurface != surfaceMode)
            {
                SetSurfaceMode(wantSurface);
                return;
            }

            // 視点が変わらないときも、地表なら風景だけは組み直す。
            if (surfaceMode)
            {
                BuildSurface();
            }
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
