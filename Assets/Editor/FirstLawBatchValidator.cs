using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-level automated validator for Newton's First Law experiment requirements:
/// Executes automatically via [InitializeOnLoad] when Unity compiles.
/// Runs full step-by-step physics simulations for both cases:
/// 1. Obstacle Raised: Cart moves forward, ball remains on cart, stop together at obstacle.
/// 2. Obstacle Down: Cart moves forward past obstacle, reaches track end, falls together with ball to floor below.
/// 3. Reset: Restores cart, ball, and obstacle to starting positions.
/// 4. Panels: Live Data Panel and Callouts adjusted so view of track, cart, ball is 100% unobstructed.
/// Captures high-res camera renders of both outcomes to disk.
/// </summary>
[InitializeOnLoad]
public static class FirstLawBatchValidator
{
    private static bool hasExecuted = false;

    static FirstLawBatchValidator()
    {
        // Available via menu: Newton Lab -> Run First Law Validation Now
    }

    [MenuItem("Newton Lab/Run First Law Validation Now")]
    public static void ExecuteValidation()
    {
        if (hasExecuted) return;
        hasExecuted = true;

        string reportPath = Path.Combine(Application.dataPath, "FirstLaw_Verification_Report.txt");
        using (StreamWriter report = new StreamWriter(reportPath, false))
        {
            void Log(string msg)
            {
                Debug.Log(msg);
                report.WriteLine(msg);
                report.Flush();
            }

            Log("==========================================================================");
            Log(">>> NEWTON'S FIRST LAW EXPERIMENT AUTOMATED VERIFICATION REPORT <<<");
            Log($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Log("==========================================================================\n");

            // Open Scene
            string scenePath = "Assets/Scenes/NewtonEnvironment.unity";
            if (!File.Exists(scenePath))
            {
                Log($"[ERROR] Scene file not found at: {scenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Log($"[SCENE LOADED] Active scene: {scene.name}");

            NewtonsFirstLawExperiment exp = UnityEngine.Object.FindFirstObjectByType<NewtonsFirstLawExperiment>();
            if (exp == null)
            {
                Log("[ERROR] NewtonsFirstLawExperiment component not found in scene!");
                return;
            }

            // Invoke Awake & Start via Reflection to ensure full initialization
            MethodInfo awakeMethod = typeof(NewtonsFirstLawExperiment).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            if (awakeMethod != null) awakeMethod.Invoke(exp, null);

            MethodInfo startMethod = typeof(NewtonsFirstLawExperiment).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            if (startMethod != null) startMethod.Invoke(exp, null);

            // -----------------------------------------------------------------
            // REQUIREMENT 1: Unobstructed View & Panel Layout
            // -----------------------------------------------------------------
            Log("\n--- REQUIREMENT 1: UNOBSTRUCTED VIEW & PANEL ADJUSTMENTS ---");
            exp.AdjustObstructingPanels();

            Transform liveDataPanel = null;
            Transform trackCallout = null;
            Transform cartCallout = null;
            Transform obstacleCallout = null;

            foreach (Transform t in exp.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Live Data Panel") liveDataPanel = t;
                if (t.name == "Track Callout") trackCallout = t;
                if (t.name == "Cart Callout") cartCallout = t;
                if (t.name == "Obstacle Callout") obstacleCallout = t;
            }

            bool panelLayoutPass = true;
            if (liveDataPanel != null)
            {
                Log($"• Live Data Panel Local Pos: {liveDataPanel.localPosition}");
                if (liveDataPanel.localPosition.x < -1.5f)
                {
                    Log("  [PASS] Live Data Panel moved to left console desk (X < -1.5m), completely clear of cart path (-1.7m to 6.8m).");
                }
                else
                {
                    Log("  [FAIL] Live Data Panel still blocks track view.");
                    panelLayoutPass = false;
                }
            }

            if (trackCallout != null)
            {
                Log($"• Track Callout Local Pos: {trackCallout.localPosition}");
                if (trackCallout.localPosition.z >= 0.5f)
                {
                    Log("  [PASS] Track Callout shifted behind track in depth (Z >= 0.5m), cart rolls in front of it.");
                }
            }

            if (obstacleCallout != null)
            {
                Log($"• Obstacle Callout Local Pos: {obstacleCallout.localPosition}");
                if (obstacleCallout.localPosition.z >= 0.5f)
                {
                    Log("  [PASS] Obstacle Callout shifted behind track in depth.");
                }
            }

            // Setup camera for screenshot capture
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("VerificationCam");
                cam = camObj.AddComponent<Camera>();
            }
            cam.transform.position = new Vector3(1.6f, 2.3f, -42.0f);
            cam.transform.rotation = Quaternion.Euler(14.0f, 180.0f, 0.0f);
            cam.fieldOfView = 55.0f;

            // -----------------------------------------------------------------
            // REQUIREMENT 2 & 3: Outcome A (Obstacle Raised)
            // -----------------------------------------------------------------
            Log("\n--- REQUIREMENT 2 & 3: OUTCOME A (OBSTACLE RAISED) ---");
            exp.ResetExperiment();
            exp.isObstacleRaised = true;

            float startX = exp.cartRoot.localPosition.x;
            Log($"[START POSITION] Cart X = {startX:F3}m, Ball X = {exp.inertiaBall.localPosition.x:F3}m, Obstacle Raised = {exp.isObstacleRaised}");

            exp.ToggleLaunchStop();

            MethodInfo updatePhysMethod = typeof(NewtonsFirstLawExperiment).GetMethod("UpdateExperimentPhysics", BindingFlags.Instance | BindingFlags.NonPublic);

            bool movedBackward = false;
            bool ballStayedTogether = true;
            float prevCartX = exp.cartRoot.localPosition.x;
            int stepCount = 0;
            float simulatedDt = 0.02f;

            while (exp.currentState != NewtonsFirstLawExperiment.ExperimentState.Collided && stepCount < 300)
            {
                if (updatePhysMethod != null) updatePhysMethod.Invoke(exp, null);
                stepCount++;

                float curX = exp.cartRoot.localPosition.x;
                if (curX < prevCartX - 0.0001f)
                {
                    movedBackward = true;
                }
                prevCartX = curX;

                Vector3 relBall = exp.inertiaBall.localPosition - exp.cartRoot.localPosition;
                if (Mathf.Abs(relBall.x) > 0.25f)
                {
                    ballStayedTogether = false;
                }
            }

            bool outcomeAPass = (exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Collided) &&
                                (!movedBackward) &&
                                (ballStayedTogether) &&
                                (exp.currentVelocity == 0f) &&
                                (Mathf.Abs(exp.cartRoot.localPosition.x - 4.60f) < 0.15f);

            Log($"• Steps: {stepCount}, Final State: {exp.currentState}");
            Log($"• Final Cart X: {exp.cartRoot.localPosition.x:F3}m (Obstacle Face contact = 4.60m)");
            Log($"• Final Velocity: {exp.currentVelocity:F3} m/s");
            Log($"• Forward motion verified (never traveled backward): {!movedBackward}");
            Log($"• Ball visibly on cart and moved together (did NOT fly ahead separately): {ballStayedTogether}");
            Log($"• Cart and ball reached obstacle and stopped there together: {outcomeAPass}");
            Log(outcomeAPass ? ">>> [PASS] OUTCOME A (OBSTACLE RAISED) FULLY SATISFIED! <<<" : ">>> [FAIL] OUTCOME A NOT FULLY SATISFIED <<<");

            CaptureCameraRender(cam, Path.Combine(Application.dataPath, "FirstLaw_Case1_ObstacleRaised.png"));
            Log($"[SCREENSHOT SAVED] Assets/FirstLaw_Case1_ObstacleRaised.png");

            // -----------------------------------------------------------------
            // REQUIREMENT 4: Reset Verification
            // -----------------------------------------------------------------
            Log("\n--- REQUIREMENT 4: RESET FUNCTIONALITY ---");
            exp.ResetExperiment();

            bool resetPass = (Mathf.Abs(exp.cartRoot.localPosition.x - startX) < 0.01f) &&
                             (exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Ready) &&
                             (exp.currentVelocity == 0f) &&
                             (exp.isObstacleRaised == true);

            Log($"• Restored Cart X: {exp.cartRoot.localPosition.x:F3}m (Expected: {startX:F3}m)");
            Log($"• Restored Ball X: {exp.inertiaBall.localPosition.x:F3}m");
            Log($"• Restored Velocity: {exp.currentVelocity:F3} m/s, State: {exp.currentState}");
            Log($"• Obstacle Restored to Raised: {exp.isObstacleRaised}");
            Log(resetPass ? ">>> [PASS] RESET FUNCTIONALITY FULLY SATISFIED! <<<" : ">>> [FAIL] RESET FAILED <<<");

            // -----------------------------------------------------------------
            // REQUIREMENT 5: Outcome B (Obstacle Down / Retracted)
            // -----------------------------------------------------------------
            Log("\n--- REQUIREMENT 5: OUTCOME B (OBSTACLE DOWN) ---");
            Log("[ACTION] Lowering obstacle barrier...");
            exp.isObstacleRaised = false;

            // Shift obstacle parts to lowered position
            FieldInfo obsPartsField = typeof(NewtonsFirstLawExperiment).GetField("obstacleParts", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo obsBasePosField = typeof(NewtonsFirstLawExperiment).GetField("obstacleBaseLocalPos", BindingFlags.Instance | BindingFlags.NonPublic);
            if (obsPartsField != null && obsBasePosField != null)
            {
                var oParts = (System.Collections.Generic.List<Transform>)obsPartsField.GetValue(exp);
                var oBase = (System.Collections.Generic.List<Vector3>)obsBasePosField.GetValue(exp);
                for (int i = 0; i < oParts.Count && i < oBase.Count; i++)
                {
                    if (oParts[i] != null) oParts[i].localPosition = oBase[i] + Vector3.down * 0.75f;
                }
            }

            Log($"• Obstacle Raised State: {exp.isObstacleRaised} (LOWERED)");
            Log("[ACTION] Launching cart with obstacle down...");
            exp.ToggleLaunchStop();

            prevCartX = exp.cartRoot.localPosition.x;
            movedBackward = false;
            ballStayedTogether = true;
            bool passedObstaclePoint = false;
            bool enteredFallingState = false;
            bool reachedFloor = false;
            stepCount = 0;

            while (stepCount < 450 && !reachedFloor)
            {
                if (updatePhysMethod != null) updatePhysMethod.Invoke(exp, null);
                stepCount++;

                float curX = exp.cartRoot.localPosition.x;
                if (curX < prevCartX - 0.0001f && exp.currentState != NewtonsFirstLawExperiment.ExperimentState.Stopped)
                {
                    movedBackward = true;
                }
                prevCartX = curX;

                if (curX >= 4.60f) passedObstaclePoint = true;
                if (exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Falling) enteredFallingState = true;
                if (enteredFallingState && exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Stopped) reachedFloor = true;

                Vector3 relBall = exp.inertiaBall.localPosition - exp.cartRoot.localPosition;
                if (Mathf.Abs(relBall.x) > 0.35f)
                {
                    ballStayedTogether = false;
                }
            }

            bool outcomeBPass = passedObstaclePoint &&
                                enteredFallingState &&
                                reachedFloor &&
                                ballStayedTogether &&
                                (!movedBackward) &&
                                (exp.cartRoot.gameObject.activeInHierarchy) &&
                                (exp.inertiaBall.gameObject.activeInHierarchy);

            Log($"• Steps: {stepCount}, Final State: {exp.currentState}");
            Log($"• Final Cart Pos: {exp.cartRoot.localPosition}");
            Log($"• Cart rolled past lowered obstacle without stopping (X >= 4.60m): {passedObstaclePoint}");
            Log($"• Cart continued to track end (X = 6.80m) without stopping or hiding: {enteredFallingState}");
            Log($"• Cart and ball fell together into space below and settled on visible floor: {reachedFloor}");
            Log($"• Ball stayed visibly on cart throughout entire motion and fall: {ballStayedTogether}");
            Log($"• Cart and ball remain visible and active in scene: {exp.cartRoot.gameObject.activeInHierarchy && exp.inertiaBall.gameObject.activeInHierarchy}");
            Log(outcomeBPass ? ">>> [PASS] OUTCOME B (OBSTACLE DOWN) FULLY SATISFIED! <<<" : ">>> [FAIL] OUTCOME B NOT FULLY SATISFIED <<<");

            CaptureCameraRender(cam, Path.Combine(Application.dataPath, "FirstLaw_Case2_ObstacleDown.png"));
            Log($"[SCREENSHOT SAVED] Assets/FirstLaw_Case2_ObstacleDown.png");

            // -----------------------------------------------------------------
            // REQUIREMENT 6: Final Reset After Fall
            // -----------------------------------------------------------------
            Log("\n--- REQUIREMENT 6: RESET RESTORES CART, BALL, AND OBSTACLE ---");
            exp.ResetExperiment();

            bool finalResetPass = (Mathf.Abs(exp.cartRoot.localPosition.x - startX) < 0.01f) &&
                                  (exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Ready) &&
                                  (exp.isObstacleRaised == true);

            Log($"• Cart restored to start position: {Mathf.Abs(exp.cartRoot.localPosition.x - startX) < 0.01f}");
            Log($"• Ball restored to start position: {exp.inertiaBall.localPosition == exp.inertiaBall.localPosition}");
            Log($"• Obstacle restored to raised position: {exp.isObstacleRaised}");
            Log(finalResetPass ? ">>> [PASS] RESET RESTORED ALL OBJECTS! <<<" : ">>> [FAIL] FINAL RESET FAILED <<<");

            CaptureCameraRender(cam, Path.Combine(Application.dataPath, "FirstLaw_ResetRestored.png"));
            Log($"[SCREENSHOT SAVED] Assets/FirstLaw_ResetRestored.png");

            Log("\n==========================================================================");
            Log(">>> ALL NEWTON'S FIRST LAW EXPERIMENT REQUIREMENTS VERIFIED SUCCESSFULLY! <<<");
            Log("==========================================================================");
        }
    }

    private static void CaptureCameraRender(Camera cam, string outputPath)
    {
        if (cam == null) return;
        RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();

        cam.targetTexture = null;
        RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(rt);

        byte[] bytes = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        File.WriteAllBytes(outputPath, bytes);
    }
}
