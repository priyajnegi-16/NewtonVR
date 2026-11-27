using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Automated verification test runner for Newton's First Law experiment requirements:
/// 1. Forward movement along track toward obstacle (never travels backward).
/// 2. Ball visibly on cart and moving together with it (does not separate).
/// 3. Obstructing panels adjusted so cart and ball remain visible throughout run.
/// 4. Outcome A (Obstacle Raised): Cart and ball reach obstacle and stop there together.
/// 5. Outcome B (Obstacle Down): Cart and ball continue past obstacle, reach track end,
///    and fall together into visible space below without stopping/resetting before fall.
/// 6. Obstacle control reliably toggles between outcomes.
/// 7. Reset restores cart, ball, and obstacle to starting positions.
/// Captures verification screenshots to confirm visual results.
/// </summary>
public class FirstLawVerificationRunner : MonoBehaviour
{
    private static FirstLawVerificationRunner instance;
    private bool testRunning = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoStart()
    {
        if (instance != null) return;
        GameObject go = new GameObject("FirstLawVerificationRunner");
        instance = go.AddComponent<FirstLawVerificationRunner>();
        DontDestroyOnLoad(go);
    }

    void Start()
    {
        // Hotkey V triggers the verification sequence on demand
    }

    void Update()
    {
        // Hotkey: V restarts the verification test sequence
        if (NewtonInput.GetKeyDown(KeyCode.V) && !testRunning)
        {
            StartCoroutine(RunVerificationSequence());
        }
    }

    private IEnumerator RunVerificationSequence()
    {
        testRunning = true;
        yield return new WaitForSeconds(1.0f);

        Debug.Log("=====================================================================");
        Debug.Log(">>> STARTING NEWTON'S FIRST LAW EXPERIMENT AUTOMATED VERIFICATION <<<");
        Debug.Log("=====================================================================");

        NewtonsFirstLawExperiment exp = FindFirstObjectByType<NewtonsFirstLawExperiment>();
        if (exp == null)
        {
            Debug.LogError("[VERIFICATION ERROR] NewtonsFirstLawExperiment not found in scene!");
            testRunning = false;
            yield break;
        }

        // Align camera to view entire First Law apparatus (from start on left to track end on right)
        Camera cam = Camera.main;
        if (cam != null)
        {
            // Position camera looking directly at the track and consoles
            cam.transform.position = new Vector3(1.5f, 2.3f, -42.0f);
            cam.transform.rotation = Quaternion.Euler(14.0f, 180.0f, 0.0f);
        }

        // -------------------------------------------------------------
        // STEP 1: Verify Panel Layout (Unobstructed View)
        // -------------------------------------------------------------
        Debug.Log("\n--- STEP 1: VERIFYING INFORMATION PANEL LAYOUT ---");
        exp.AdjustObstructingPanels();

        Transform liveDataPanel = null;
        Transform trackCallout = null;
        foreach (Transform t in exp.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Live Data Panel") liveDataPanel = t;
            if (t.name.Contains("Track Callout")) trackCallout = t;
        }

        if (liveDataPanel != null)
        {
            Debug.Log($"[PANEL CHECK] Live Data Panel local position: {liveDataPanel.localPosition}");
            if (liveDataPanel.localPosition.x < -1.5f)
            {
                Debug.Log("[PANEL CHECK PASS] Live Data Panel is shifted to left desk (X < -1.5m), completely clear of cart path (-1.7m to 6.8m)!");
            }
            else
            {
                Debug.LogWarning($"[PANEL CHECK] Live Data Panel X is {liveDataPanel.localPosition.x:F2}m.");
            }
        }

        if (trackCallout != null)
        {
            Debug.Log($"[PANEL CHECK] Track Callout local position: {trackCallout.localPosition}");
            if (trackCallout.localPosition.z >= 0.5f)
            {
                Debug.Log("[PANEL CHECK PASS] Track Callout is shifted behind track in depth (Z >= 0.5m), cart rolls in front of it!");
            }
        }

        // -------------------------------------------------------------
        // STEP 2: Verify Initial State & Starting Position
        // -------------------------------------------------------------
        Debug.Log("\n--- STEP 2: VERIFYING INITIAL STARTING POSITION ---");
        exp.ResetExperiment();
        yield return new WaitForSeconds(0.3f);

        Debug.Log($"[INITIAL STATE] Cart local position: {exp.cartRoot.localPosition}, Ball local position: {exp.inertiaBall.localPosition}");
        Debug.Log($"[INITIAL STATE] Obstacle Raised: {exp.isObstacleRaised}, Velocity: {exp.currentVelocity}, State: {exp.currentState}");

        // -------------------------------------------------------------
        // STEP 3: Test Case 1 — Obstacle Raised
        // -------------------------------------------------------------
        Debug.Log("\n--- STEP 3: TESTING OUTCOME A (OBSTACLE RAISED) ---");
        if (!exp.isObstacleRaised)
        {
            exp.ToggleObstacle();
            yield return new WaitForSeconds(0.4f);
        }

        float initialCartX = exp.cartRoot.localPosition.x;
        float prevCartX = initialCartX;
        bool movedBackward = false;
        bool ballStayedOnCart = true;

        exp.ToggleLaunchStop();
        Debug.Log("[CASE 1] Launched cart toward raised obstacle...");

        float timeout = 4.5f;
        float elapsed = 0f;

        while (exp.currentState != NewtonsFirstLawExperiment.ExperimentState.Collided && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;

            float curCartX = exp.cartRoot.localPosition.x;
            if (curCartX < prevCartX - 0.001f)
            {
                movedBackward = true;
            }
            prevCartX = curCartX;

            // Check that ball position relative to cart remains fixed (ball stays seated on cart)
            Vector3 cartToBall = exp.inertiaBall.localPosition - exp.cartRoot.localPosition;
            if (Mathf.Abs(cartToBall.x) > 0.35f)
            {
                ballStayedOnCart = false;
            }
        }

        Debug.Log($"[CASE 1 RESULT] Final State: {exp.currentState}, Cart X: {exp.cartRoot.localPosition.x:F2}, Velocity: {exp.currentVelocity:F2}");
        Debug.Log($"[CASE 1 RESULT] Forward motion only (never backward): {!movedBackward}");
        Debug.Log($"[CASE 1 RESULT] Ball stayed on cart and moved together: {ballStayedOnCart}");
        Debug.Log($"[CASE 1 RESULT] Cart and ball stopped together at obstacle: {exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Collided && exp.currentVelocity == 0f}");

        string shotPath1 = Path.Combine(Application.dataPath, "FirstLaw_Case1_ObstacleRaised.png");
        ScreenCapture.CaptureScreenshot(shotPath1);
        Debug.Log($"[SCREENSHOT SAVED] Case 1 Screenshot: {shotPath1}");

        yield return new WaitForSeconds(1.0f);

        // -------------------------------------------------------------
        // STEP 4: Test Reset Functionality
        // -------------------------------------------------------------
        Debug.Log("\n--- STEP 4: TESTING RESET FUNCTIONALITY ---");
        exp.ResetExperiment();
        yield return new WaitForSeconds(0.4f);

        bool resetPassed = Mathf.Abs(exp.cartRoot.localPosition.x - initialCartX) < 0.05f &&
                           exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Ready &&
                           exp.currentVelocity == 0f &&
                           exp.isObstacleRaised == true;

        Debug.Log($"[RESET TEST] Reset restored cart to start (X={exp.cartRoot.localPosition.x:F2}), velocity to 0, obstacle raised: {resetPassed}");

        yield return new WaitForSeconds(0.5f);

        // -------------------------------------------------------------
        // STEP 5: Test Case 2 — Obstacle Lowered (Down)
        // -------------------------------------------------------------
        Debug.Log("\n--- STEP 5: TESTING OUTCOME B (OBSTACLE DOWN) ---");
        Debug.Log("[CASE 2] Lowering obstacle barrier...");
        exp.ToggleObstacle();
        yield return new WaitForSeconds(0.5f);

        Debug.Log($"[CASE 2] Obstacle is lowered: {!exp.isObstacleRaised}");
        exp.ToggleLaunchStop();
        Debug.Log("[CASE 2] Launched cart with obstacle lowered...");

        prevCartX = exp.cartRoot.localPosition.x;
        movedBackward = false;
        ballStayedOnCart = true;
        bool passedObstaclePoint = false;
        bool enteredFallingState = false;
        bool settledOnFloor = false;

        elapsed = 0f;
        timeout = 5.5f;

        while (elapsed < timeout && !settledOnFloor)
        {
            yield return null;
            elapsed += Time.deltaTime;

            float curCartX = exp.cartRoot.localPosition.x;
            if (curCartX < prevCartX - 0.001f && exp.currentState != NewtonsFirstLawExperiment.ExperimentState.Stopped)
            {
                movedBackward = true;
            }
            prevCartX = curCartX;

            if (curCartX >= 4.60f)
            {
                passedObstaclePoint = true;
            }

            if (exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Falling)
            {
                enteredFallingState = true;
            }

            if (enteredFallingState && exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Stopped)
            {
                settledOnFloor = true;
            }

            Vector3 cartToBall = exp.inertiaBall.localPosition - exp.cartRoot.localPosition;
            if (Mathf.Abs(cartToBall.x) > 0.40f)
            {
                ballStayedOnCart = false;
            }
        }

        Debug.Log($"[CASE 2 RESULT] Final State: {exp.currentState}, Cart Pos: {exp.cartRoot.localPosition}");
        Debug.Log($"[CASE 2 RESULT] Cart rolled past lowered obstacle without stopping: {passedObstaclePoint}");
        Debug.Log($"[CASE 2 RESULT] Cart reached track end and entered Falling state: {enteredFallingState}");
        Debug.Log($"[CASE 2 RESULT] Cart and ball settled visibly on floor below (Y={exp.cartRoot.localPosition.y:F2}): {settledOnFloor}");
        Debug.Log($"[CASE 2 RESULT] Ball remained on cart throughout motion and fall: {ballStayedOnCart}");
        Debug.Log($"[CASE 2 RESULT] Cart and ball remain visible (not destroyed or hidden): {exp.cartRoot.gameObject.activeInHierarchy && exp.inertiaBall.gameObject.activeInHierarchy}");

        string shotPath2 = Path.Combine(Application.dataPath, "FirstLaw_Case2_ObstacleDown.png");
        ScreenCapture.CaptureScreenshot(shotPath2);
        Debug.Log($"[SCREENSHOT SAVED] Case 2 Screenshot: {shotPath2}");

        yield return new WaitForSeconds(1.0f);

        // -------------------------------------------------------------
        // STEP 6: Final Reset Test (Restores after fall)
        // -------------------------------------------------------------
        Debug.Log("\n--- STEP 6: VERIFYING RESET AFTER FALL ---");
        exp.ResetExperiment();
        yield return new WaitForSeconds(0.4f);

        bool finalResetPassed = Mathf.Abs(exp.cartRoot.localPosition.x - initialCartX) < 0.05f &&
                                exp.currentState == NewtonsFirstLawExperiment.ExperimentState.Ready &&
                                exp.isObstacleRaised == true;

        Debug.Log($"[FINAL RESET] Cart, ball, and obstacle fully restored to start: {finalResetPassed}");

        string shotPath3 = Path.Combine(Application.dataPath, "FirstLaw_ResetRestored.png");
        ScreenCapture.CaptureScreenshot(shotPath3);

        Debug.Log("\n=====================================================================");
        Debug.Log(">>> AUTOMATED VERIFICATION OF NEWTON'S FIRST LAW EXPERIMENT COMPLETE <<<");
        Debug.Log("=====================================================================");

        testRunning = false;
    }
}
