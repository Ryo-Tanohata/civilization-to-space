using System;
using System.Collections.Generic;
using System.Text;
using CivilizationToSpace.Core;
using CivilizationToSpace.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CivilizationToSpace.EditorTools
{
    /// <summary>
    /// Play Mode で再生・視点操作・動きの設定を順に確かめ、結果を一覧で出す。
    ///
    /// 検証件数を使い捨てスクリプトで作らず、再実行できる台帳としてリポジトリへ残す。
    /// 各行の判定条件はコードに書いてあり、あとから同じ結果を出せる。
    /// </summary>
    public static class BehaviourCheck
    {
        private const string PendingKey = "CivilizationToSpace.BehaviourCheck.Pending";
        private const string ExitKey = "CivilizationToSpace.BehaviourCheck.Exit";

        private static readonly List<string> Results = new List<string>();
        private static int failures;
        private static int step;

        /// <summary>
        /// 条件が満たされるまで待つ。実時間の固定待ちにすると、
        /// batchmodeではゲーム内時間が実時間より遅く進むため、待ち足りずに誤判定する。
        /// </summary>
        private static Func<bool> waitCondition;
        private static float waitDeadline;
        private static bool waitTimedOut;
        private static bool dissolveStarted;

        private static int startIndex;
        private static float startTime;
        private static Quaternion spinBefore;
        private static Quaternion sunBefore;
        private static Vector3 cameraBefore;

        [MenuItem("Tools/Civilization to Space/再生と視点の動作を点検", false, 302)]
        public static void Run()
        {
            Start(false);
        }

        public static void RunFromCommandLine()
        {
            Start(true);
        }

        private static void Start(bool exitWhenDone)
        {
            EditorSceneManager.OpenScene(DemoSceneBuilder.ScenePath);
            SessionState.SetString(AppRoot.CatalogPathOverrideKey, string.Empty);
            SessionState.SetBool(ExitKey, exitWhenDone);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void HookAfterDomainReload()
        {
            if (!SessionState.GetBool(PendingKey, false))
            {
                return;
            }

            Results.Clear();
            failures = 0;
            step = 0;
            waitCondition = null;
            waitDeadline = 0f;
            waitTimedOut = false;

            EditorApplication.update -= Observe;
            EditorApplication.update += Observe;
        }

        private static void Observe()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            if (waitCondition != null)
            {
                if (waitCondition())
                {
                    waitCondition = null;
                    waitTimedOut = false;
                }
                else if (Time.realtimeSinceStartup >= waitDeadline)
                {
                    waitCondition = null;
                    waitTimedOut = true;
                }
                else
                {
                    return;
                }
            }

            var app = UnityEngine.Object.FindAnyObjectByType<AppRoot>();
            if (app == null || app.LoadFailed)
            {
                Record("U-00 起動", false, "AppRoot が無いか、読込に失敗しました");
                Finish();
                return;
            }

            var timeline = app.Timeline;
            var playback = app.Playback;
            var camera = Camera.main;
            var framing = camera != null ? camera.GetComponent<EarthFraming>() : null;
            var earth = UnityEngine.Object.FindAnyObjectByType<EarthView>();
            var spin = earth != null ? earth.transform.Find("Spin") : null;
            var sunLight = UnityEngine.Object.FindAnyObjectByType<Light>();

            switch (step)
            {
                case 0:
                    Record("U-21 初期状態は停止・1x・最初の時代",
                        !playback.IsPlaying && Mathf.Approximately(playback.Speed, 1f) && timeline.Index == 0,
                        "再生=" + playback.IsPlaying + " 速度=" + playback.Speed + " 位置=" + timeline.Index);
                    startIndex = timeline.Index;
                    startTime = Time.time;
                    playback.Toggle();
                    Record("U-22 再生を開始できる", playback.IsPlaying, "再生=" + playback.IsPlaying);
                    WaitFor(() => timeline.Index != startIndex, 40f);
                    break;

                case 1:
                    // 形成過程の段階だけ長く取っている。どちらの長さを見るかは、
                    // 進み始めた位置が時代より手前かどうかで決まる。
                    var firstStep = startIndex < timeline.EraOffset
                        ? TimelinePlayback.FormationStepSeconds
                        : TimelinePlayback.BaseStepSeconds;
                    Record("U-23 1倍で1段階進む（形成過程は約8秒・時代は約4秒）",
                        !waitTimedOut && timeline.Index == startIndex + 1 &&
                        Mathf.Abs(Time.time - startTime - firstStep) < 1f,
                        "位置 " + startIndex + " → " + timeline.Index +
                        " / 経過 " + (Time.time - startTime).ToString("F2") + "秒" +
                        "（見込み " + firstStep.ToString("F0") + "秒）" +
                        (waitTimedOut ? "（時間切れ）" : string.Empty));
                    playback.SetSpeed(2f);
                    Record("U-24 速度を変えても再生は続く", playback.IsPlaying && Mathf.Approximately(playback.Speed, 2f),
                        "再生=" + playback.IsPlaying + " 速度=" + playback.Speed);
                    startIndex = timeline.Index;
                    startTime = Time.time;
                    WaitFor(() => timeline.Index != startIndex, 40f);
                    break;

                case 2:
                    var doubledStep = (startIndex < timeline.EraOffset
                        ? TimelinePlayback.FormationStepSeconds
                        : TimelinePlayback.BaseStepSeconds) * 0.5f;
                    Record("U-25 2倍でその半分の時間に1段階進む",
                        !waitTimedOut && timeline.Index == startIndex + 1 &&
                        Mathf.Abs(Time.time - startTime - doubledStep) < 0.8f,
                        "位置 " + startIndex + " → " + timeline.Index +
                        " / 経過 " + (Time.time - startTime).ToString("F2") + "秒" +
                        "（見込み " + doubledStep.ToString("F0") + "秒）" +
                        (waitTimedOut ? "（時間切れ）" : string.Empty));
                    timeline.Select(0);
                    Record("U-26 手で時代を変えると再生が止まる", !playback.IsPlaying && timeline.Index == 0,
                        "再生=" + playback.IsPlaying + " 位置=" + timeline.Index);
                    playback.SetSpeed(1f);

                    timeline.Select(timeline.Count - 1);
                    playback.Toggle();
                    Record("U-27 最後の時代から再生すると最初へ戻る", playback.IsPlaying && timeline.Index == 0,
                        "再生=" + playback.IsPlaying + " 位置=" + timeline.Index);
                    playback.RequestStop();
                    Record("U-28 停止を要求すると止まる", !playback.IsPlaying, "再生=" + playback.IsPlaying);

                    timeline.Select(timeline.Count - 2);
                    playback.Toggle();
                    WaitFor(() => !playback.IsPlaying, 40f);
                    break;

                case 3:
                    Record("U-29 最後の時代へ達すると自動で止まる",
                        !playback.IsPlaying && timeline.Index == timeline.Count - 1,
                        "再生=" + playback.IsPlaying + " 位置=" + timeline.Index);

                    spinBefore = spin != null ? spin.localRotation : Quaternion.identity;
                    sunBefore = sunLight != null ? sunLight.transform.rotation : Quaternion.identity;

                    // **決まった秒数だけ待ってはいけない。**
                    // 自転の1周ぶんだけ待つと、ちょうど元へ戻って差が0になり、
                    // 動いていても失敗と出る。自転の速さを変えるたびに待ち時間を
                    // 見直さずに済むよう、角度が付くまで待ち、
                    // 待てなかったときだけ失敗とする。
                    WaitFor(
                        () => spin != null && Quaternion.Angle(spinBefore, spin.localRotation) > 5f,
                        20f);
                    break;

                case 4:
                    Record("U-30 動きを減らす前は自転している",
                        !waitTimedOut && spin != null && Quaternion.Angle(spinBefore, spin.localRotation) > 0.1f,
                        "回転差 " + (spin != null ? Quaternion.Angle(spinBefore, spin.localRotation) : -1f).ToString("F2") + "度" +
                        (waitTimedOut ? "（時間切れ）" : string.Empty));

                    Record("U-37 動きを減らす前は太陽が季節で向きを変える",
                        sunLight != null && Quaternion.Angle(sunBefore, sunLight.transform.rotation) > 0.01f,
                        "回転差 " + (sunLight != null ? Quaternion.Angle(sunBefore, sunLight.transform.rotation) : -1f).ToString("F3") + "度");

                    app.SetReducedMotionForTesting(true);
                    spinBefore = spin != null ? spin.localRotation : Quaternion.identity;
                    sunBefore = sunLight != null ? sunLight.transform.rotation : Quaternion.identity;
                    startTime = Time.time;
                    WaitFor(() => Time.time - startTime >= 0.5f, 20f);
                    break;

                case 5:
                    Record("U-31 動きを減らすと自転が止まる",
                        spin != null && Quaternion.Angle(spinBefore, spin.localRotation) < 0.001f,
                        "回転差 " + (spin != null ? Quaternion.Angle(spinBefore, spin.localRotation) : -1f).ToString("F4") + "度");

                    Record("U-38 動きを減らすと季節も止まる",
                        sunLight != null && Quaternion.Angle(sunBefore, sunLight.transform.rotation) < 0.001f,
                        "回転差 " + (sunLight != null ? Quaternion.Angle(sunBefore, sunLight.transform.rotation) : -1f).ToString("F4") + "度");
                    app.SetReducedMotionForTesting(false);

                    // 季節の式そのものを確かめる。
                    //
                    // **画面の画素では確かめられない。** 夜側の街の明かり、溶岩、
                    // 大気のふちが混ざるため、光の当たり方だけを取り出せない。
                    // 実際、画素で南北の明るさを比べると季節が逆に見える。
                    // 向きと割合は式から直に測る。
                    var litLow = 1f;
                    var litHigh = 0f;
                    var latLow = 90f;
                    var latHigh = -90f;
                    for (var i = 0; i <= 720; i++)
                    {
                        var toward = -(AppRoot.SunRotation(i / 720f) * Vector3.forward);

                        var lit = (1f + Vector3.Dot(Vector3.back, toward)) * 0.5f;
                        litLow = Mathf.Min(litLow, lit);
                        litHigh = Mathf.Max(litHigh, lit);

                        var lat = Mathf.Asin(Mathf.Clamp(
                            Vector3.Dot(EarthView.AxisDirection, toward), -1f, 1f)) * Mathf.Rad2Deg;
                        latLow = Mathf.Min(latLow, lat);
                        latHigh = Mathf.Max(latHigh, lat);
                    }

                    Record("U-39 昼の割合は年を通して一定",
                        litHigh - litLow < 0.001f && Mathf.Abs(litLow - AppRoot.LitFraction) < 0.001f,
                        "昼 " + (litLow * 100f).ToString("F2") + "% 〜 " + (litHigh * 100f).ToString("F2") +
                        "%（狙い " + (AppRoot.LitFraction * 100f).ToString("F1") + "%）");

                    Record("U-40 太陽の正面へ来る緯度が地軸の傾きぶん振れる",
                        Mathf.Abs(latHigh - EarthView.AxialTiltDegrees) < 0.05f &&
                        Mathf.Abs(latLow + EarthView.AxialTiltDegrees) < 0.05f,
                        "緯度 " + latLow.ToString("F2") + "度 〜 " + latHigh.ToString("F2") +
                        "度（狙い ±" + EarthView.AxialTiltDegrees.ToString("F1") + "度）");

                    // 視点操作は再生を止めない
                    timeline.Select(0);
                    playback.Toggle();
                    cameraBefore = camera.transform.position;
                    framing.Yaw += 35f;
                    framing.Pitch += 20f;
                    framing.Apply();
                    Record("U-32 視点を回してもカメラが動き、再生は止まらない",
                        (camera.transform.position - cameraBefore).magnitude > 0.01f && playback.IsPlaying,
                        "移動量 " + (camera.transform.position - cameraBefore).magnitude.ToString("F3") +
                        " 再生=" + playback.IsPlaying);

                    cameraBefore = camera.transform.position;
                    framing.Zoom = 0.7f;
                    framing.Apply();
                    Record("U-33 寄りを変えると距離が縮む",
                        Vector3.Distance(camera.transform.position, framing.Target) <
                        Vector3.Distance(cameraBefore, framing.Target),
                        "距離 " + Vector3.Distance(cameraBefore, framing.Target).ToString("F2") + " → " +
                        Vector3.Distance(camera.transform.position, framing.Target).ToString("F2"));

                    framing.Pitch = 999f;
                    framing.Zoom = 99f;
                    framing.Apply();
                    Record("U-34 仰角と寄りは端で止まる",
                        Mathf.Approximately(framing.Pitch, EarthFraming.MaximumPitch) &&
                        Mathf.Approximately(framing.Zoom, EarthFraming.MaximumZoom),
                        "仰角=" + framing.Pitch + " 寄り=" + framing.Zoom);

                    // 指の動きと視点の向きの関係を確かめる。
                    // 画面の画素では確かめにくいので、割り当てを直に呼ぶ。
                    // Unityの画面座標は上へ行くほど y が大きいので、
                    // 指を下へ動かすと delta.y は負になる。
                    framing.ResetView();
                    var pitchBefore = framing.Pitch;
                    EarthCameraControl.ApplyDrag(framing, new Vector2(0f, -120f), 800);
                    Record("U-41 指を下へ動かすと北極を見下ろす向きになる",
                        framing.Pitch > pitchBefore + 1f,
                        "仰角 " + pitchBefore.ToString("F1") + "度 → " + framing.Pitch.ToString("F1") + "度");

                    framing.ResetView();
                    var yawBefore = framing.Yaw;
                    EarthCameraControl.ApplyDrag(framing, new Vector2(120f, 0f), 800);
                    Record("U-42 指を右へ動かすと地表が右へ流れる向きになる",
                        framing.Yaw > yawBefore + 1f,
                        "方位 " + yawBefore.ToString("F1") + "度 → " + framing.Yaw.ToString("F1") + "度");

                    framing.ResetView();
                    Record("U-35 視点をもどすと既定へ戻る",
                        Mathf.Approximately(framing.Yaw, 0f) && Mathf.Approximately(framing.Pitch, 0f) &&
                        Mathf.Approximately(framing.Zoom, 1f),
                        "向き=" + framing.Yaw + "/" + framing.Pitch + " 寄り=" + framing.Zoom);

                    playback.RequestStop();
                    cameraBefore = camera.transform.position;
                    timeline.Select(3);
                    Record("U-36 時代を切り替えてもカメラは動かない",
                        (camera.transform.position - cameraBefore).magnitude < 0.0001f,
                        "移動量 " + (camera.transform.position - cameraBefore).magnitude.ToString("F5"));

                    // 地表と宇宙の切り替え。宇宙の見せ物が残っていると、
                    // 地表の風景の中に地球が浮かぶことになる。
                    app.ToggleSurface();
                    Record("U-43 地表へ切り替えると地球が消える",
                        app.SurfaceMode && earth != null && !earth.gameObject.activeSelf,
                        "地表=" + app.SurfaceMode +
                        " 地球=" + (earth != null ? earth.gameObject.activeSelf.ToString() : "なし"));

                    app.ToggleSurface();
                    Record("U-44 宇宙へもどすと地球が出る",
                        !app.SurfaceMode && earth != null && earth.gameObject.activeSelf,
                        "地表=" + app.SurfaceMode +
                        " 地球=" + (earth != null ? earth.gameObject.activeSelf.ToString() : "なし"));

                    // **隕石が落ちたあとに首の長い四つ足を立たせない。**
                    // 形が1つしか無かったころ、恐竜の時代と氷期で同じ姿を使っており、
                    // 衝突の6400万年あとの氷期に首の長い大きな四つ足が立っていた。
                    // 出典: NPS「Mass Extinctions Through Geologic Time」
                    // https://www.nps.gov/subjects/fossils/mass-extinctions-through-geologic-time.htm
                    var dino = View.SurfaceCatalog.ForEra(4);
                    var ice = View.SurfaceCatalog.ForEra(6);
                    Record("U-45 衝突より後の時代に首の長い四つ足がいない",
                        ice.Quadrupeds == 0 || ice.CreatureNeck < dino.CreatureNeck * 0.5f,
                        "恐竜の首 " + dino.CreatureNeck.ToString("F2") +
                        " / 氷期の首 " + ice.CreatureNeck.ToString("F2"));

                    // **原因より先に結果を出さない。**
                    // 衝突の場面を最初から枯れた幹と暗い空で作っていたとき、
                    // 隕石がまだ空にあるのに地上はすでに死んでおり、
                    // 恐竜→隕石→氷期という順につながって見えなかった。
                    var impact = View.SurfaceCatalog.ForEra(5);
                    Record("U-46 衝突の場面は落ちる前に生きた世界から始まる",
                        impact.DiesOnImpact && impact.Conifers > 0 && impact.Quadrupeds > 0,
                        "枯れる=" + impact.DiesOnImpact +
                        " 木=" + impact.Conifers + " 生きもの=" + impact.Quadrupeds);

                    // **見えない場所に出来事を置かない。**
                    // ロケットは地表にしか出ない。すべての打ち上げが宇宙の既定の
                    // 時代にあったときは、切り替えない限り一度も見られなかった。
                    // 地球を離れたあとの時代は宇宙の既定でよいが、
                    // 少なくとも1つは、何もしなくても打ち上げが見えなければならない。
                    var shown = string.Empty;
                    for (var era = 0; era < 10; era++)
                    {
                        if (View.SurfaceCatalog.ForEra(era).ShowsRocket
                            && View.SurfaceCatalog.DefaultsToSurface(era))
                        {
                            shown += (shown.Length > 0 ? "," : string.Empty) + era;
                        }
                    }

                    Record("U-47 切り替えなくても打ち上げが見える時代がある",
                        shown.Length > 0,
                        shown.Length > 0 ? "地表が既定の時代 " + shown : "どの時代も宇宙が既定");

                    // **暗くなった理由を画面に出す。**
                    // 空が暗くなるだけでは、何におおわれたのかが読めない。
                    // 舞い上がった塵が降ってくるところと、冷えていく色を持たせる。
                    // 出典: Brugger ほか (2017, Geophysical Research Letters)
                    // 「Baby, it's cold outside」。世界の年平均気温が少なくとも26℃下がり、
                    // 年平均が氷点下の年が3年ほど続き、氷冠が広がった。
                    // Senel ほか (2023, Nature Geoscience)「Chicxulub impact winter
                    // sustained by fine silicate dust」。細かい塵は大気中に15年とどまり、
                    // 光合成は2年ちかく止まった。
                    Record("U-48 衝突のあとは塵が降り、冷えた色へ移る",
                        impact.AshFlakes > 0
                        && impact.WinterSkyLow.maxColorComponent > 0.001f
                        && impact.WinterGround.maxColorComponent > 0.001f,
                        "塵=" + impact.AshFlakes +
                        " 冷えた空=" + (impact.WinterSkyLow.maxColorComponent > 0.001f) +
                        " 冷えた地面=" + (impact.WinterGround.maxColorComponent > 0.001f));

                    // **星は実在の位置に置く。**
                    // 適当に撒くと、オリオン座もカシオペヤ座もどこにも無い空になる。
                    // 公開されている赤経・赤緯と突き合わせる。
                    var named = new[]
                    {
                        new[] { 101.287f, -16.716f },  // シリウス
                        new[] { 95.988f, -52.696f },   // カノープス
                        new[] { 78.634f, -8.202f },    // リゲル
                        new[] { 88.793f, 7.407f },     // ベテルギウス
                        new[] { 279.234f, 38.784f },   // ベガ
                        new[] { 37.955f, 89.264f },    // ポラリス
                    };

                    var worst = 0f;
                    foreach (var want in named)
                    {
                        var target = View.StarCatalog.Direction(want[0], want[1]);
                        var nearest = 180f;
                        var table = View.StarCatalog.Stars;
                        for (var at = 0; at < table.Length; at += View.StarCatalog.StarStride)
                        {
                            var have = View.StarCatalog.Direction(table[at], table[at + 1]);
                            var degrees = Vector3.Angle(target, have);
                            if (degrees < nearest)
                            {
                                nearest = degrees;
                            }
                        }

                        if (nearest > worst)
                        {
                            worst = nearest;
                        }
                    }

                    Record("U-49 宇宙の星空が実在の赤経・赤緯と合う",
                        View.StarCatalog.Stars.Length > 0 && worst < 0.01f,
                        "星 " + (View.StarCatalog.Stars.Length / View.StarCatalog.StarStride)
                        + "個 / 名のある6星のいちばん大きなずれ " + worst.ToString("F4") + "度");

                    // **星座の線は既定で出さない。**
                    // 空ぜんたいに線が走ると星座早見盤のようになり、
                    // 星空を見ている感じが薄れる。見たいときだけ出す。
                    Record("U-50 星座の線は最初は出ていない",
                        !app.ConstellationLines,
                        "線=" + app.ConstellationLines);

                    app.ToggleConstellationLines();
                    Record("U-51 押すと星座の線が出る",
                        app.ConstellationLines,
                        "線=" + app.ConstellationLines);

                    app.ToggleConstellationLines();
                    Record("U-52 もう一度押すと消える",
                        !app.ConstellationLines,
                        "線=" + app.ConstellationLines);

                    // **地表から見る時代は長く取る。**
                    // 昼夜が一巡するのに10秒、衝突も落ちて冷えるまでひと続き。
                    // 4秒だとどちらも途中で次の時代へ移ってしまう。
                    var spaceEra = -1;
                    var groundEra = -1;
                    for (var era = 0; era < 10 && (spaceEra < 0 || groundEra < 0); era++)
                    {
                        if (View.SurfaceCatalog.DefaultsToSurface(era))
                        {
                            if (groundEra < 0)
                            {
                                groundEra = era;
                            }
                        }
                        else if (spaceEra < 0)
                        {
                            spaceEra = era;
                        }
                    }

                    // 時代の位置は先頭からいくつ目か分からないので、探して合わせる。
                    var spaceSeconds = 0f;
                    var groundSeconds = 0f;
                    for (var at = 0; at < timeline.Count; at++)
                    {
                        timeline.Select(at);
                        if (!timeline.InEra)
                        {
                            continue;
                        }

                        if (timeline.CurrentEraIndex == spaceEra)
                        {
                            spaceSeconds = playback.StepSeconds;
                        }
                        else if (timeline.CurrentEraIndex == groundEra)
                        {
                            groundSeconds = playback.StepSeconds;
                        }
                    }

                    Record("U-53 地表から見る時代は宇宙の時代より長い",
                        groundSeconds > spaceSeconds + 0.5f,
                        "宇宙の時代 " + spaceSeconds.ToString("F1") + "秒 / 地表の時代 "
                        + groundSeconds.ToString("F1") + "秒");

                    // **場面の切り替わりは溶明でつなぐ。**
                    // 時代が移るたびに風景は丸ごと作り直され、カメラも飛ぶ。
                    // 前の絵を1枚控えて重ね、薄れさせることでつなげる。
                    //
                    // ここはエディタの実時間で測る。WebGLのヘッドレスは
                    // 描画が遅く、0.4秒の溶明を数こまでは捉えられない。
                    timeline.Select(0);
                    dissolveStarted = app.Dissolve != null && app.Dissolve.Running;
                    Record("U-54 場面が切り替わると溶明が始まる",
                        dissolveStarted,
                        "溶明=" + dissolveStarted);

                    WaitFor(() => app.Dissolve == null || !app.Dissolve.Running, 5f);
                    break;

                case 6:
                    Record("U-55 溶明はひとりでに終わる",
                        app.Dissolve != null && !app.Dissolve.Running,
                        "溶明=" + (app.Dissolve != null && app.Dissolve.Running));

                    Finish();
                    break;
            }

            step++;
        }

        /// <summary>条件が満たされるまで待つ。上限は実時間で切る。</summary>
        private static void WaitFor(Func<bool> condition, float timeoutSeconds)
        {
            waitCondition = condition;
            waitDeadline = Time.realtimeSinceStartup + timeoutSeconds;
            waitTimedOut = false;
        }

        private static void Record(string name, bool ok, string detail)
        {
            if (!ok)
            {
                failures++;
            }

            Results.Add(string.Format("{0,-44} {1}  {2}", name, ok ? "通過" : "失敗", detail));
        }

        private static void Finish()
        {
            EditorApplication.update -= Observe;
            SessionState.SetBool(PendingKey, false);

            var builder = new StringBuilder("[Behaviour] 動作の点検結果\n");
            foreach (var line in Results)
            {
                builder.Append(line).Append('\n');
            }

            builder.Append(string.Format("合計 {0}件 / 失敗 {1}件", Results.Count, failures));
            if (failures > 0)
            {
                Debug.LogError(builder.ToString());
            }
            else
            {
                Debug.Log(builder.ToString());
            }

            if (SessionState.GetBool(ExitKey, false))
            {
                EditorApplication.Exit(failures == 0 ? 0 : 1);
                return;
            }

            EditorApplication.isPlaying = false;
        }
    }
}
