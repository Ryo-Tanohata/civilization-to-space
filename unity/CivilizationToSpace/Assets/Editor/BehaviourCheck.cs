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

        private static int startIndex;
        private static float startTime;
        private static Quaternion spinBefore;
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
                    startTime = Time.time;
                    WaitFor(() => Time.time - startTime >= 0.5f, 20f);
                    break;

                case 4:
                    Record("U-30 動きを減らす前は自転している",
                        spin != null && Quaternion.Angle(spinBefore, spin.localRotation) > 0.1f,
                        "回転差 " + (spin != null ? Quaternion.Angle(spinBefore, spin.localRotation) : -1f).ToString("F2") + "度");

                    app.SetReducedMotionForTesting(true);
                    spinBefore = spin != null ? spin.localRotation : Quaternion.identity;
                    startTime = Time.time;
                    WaitFor(() => Time.time - startTime >= 0.5f, 20f);
                    break;

                case 5:
                    Record("U-31 動きを減らすと自転が止まる",
                        spin != null && Quaternion.Angle(spinBefore, spin.localRotation) < 0.001f,
                        "回転差 " + (spin != null ? Quaternion.Angle(spinBefore, spin.localRotation) : -1f).ToString("F4") + "度");
                    app.SetReducedMotionForTesting(false);

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
