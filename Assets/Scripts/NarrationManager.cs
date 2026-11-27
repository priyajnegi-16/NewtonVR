using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages spoken voiceover narration across the Newton VR Simulation for Meta Quest 3S and Desktop.
/// - Clear, friendly instructor voice.
/// - Strictly non-overlapping playback with smooth crossfading.
/// - Station-based triggers (Welcome on START LAB entry, First Law on Lab 1 entry, etc.).
/// - Does not replay automatically every time the user moves around.
/// - Unobtrusive replay control (Desktop [P] key, VR panel button, or public API).
/// - Tracks completion of all 3 laws and plays the Conclusion narration once all 3 are verified.
/// - Respects headset pause/resume and comfortable volume controls.
/// </summary>
public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

    [Header("Narration Audio Clips")]
    public AudioClip welcomeClip;
    public AudioClip firstLawClip;
    public AudioClip secondLawClip;
    public AudioClip thirdLawClip;
    public AudioClip conclusionClip;

    [Header("Volume & Playback Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 0.85f;
    public float crossfadeDuration = 0.15f;

    [Header("Experiment Completion Status")]
    public bool firstLawCompleted = false;
    public bool secondLawCompleted = false;
    public bool thirdLawCompleted = false;

    public enum NarrationStation
    {
        Hub,
        Welcome,
        FirstLaw,
        SecondLaw,
        ThirdLaw,
        Conclusion
    }

    [Header("Live Narration State")]
    public NarrationStation currentStation = NarrationStation.Hub;
    public string currentNarrationTitle = "";
    public string currentNarrationSubtitles = "";

    // One-time playback flags
    private bool hasPlayedWelcome = false;
    private bool hasPlayedFirstLaw = false;
    private bool hasPlayedSecondLaw = false;
    private bool hasPlayedThirdLaw = false;
    private bool hasPlayedConclusion = false;

    // Headset Pause / Audio Source state
    private AudioSource audioSource;
    private Coroutine activeFadeRoutine = null;
    private bool wasPlayingBeforePause = false;

    // Player tracking
    private Transform playerTransform;

    // Station center coordinates
    private readonly Vector3 lab1Center = new Vector3(0f, 1.2f, -46f);
    private readonly Vector3 lab2Center = new Vector3(-46f, 1.2f, 0f);
    private readonly Vector3 lab3Center = new Vector3(46.6f, 1.2f, 0f);
    private readonly Vector3 lobbyCenter = new Vector3(0f, 1.2f, -10f);

    public bool IsPlaying => audioSource != null && audioSource.isPlaying;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("NarrationManager_System");
            go.AddComponent<NarrationManager>();
            Debug.Log("[Newton VR Narration] NarrationManager system auto-initialized successfully.");
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSource();
        LoadAudioClips();
    }

    void Start()
    {
        FindPlayerReference();
        EnsureStartButtonCollider();
    }

    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Stereo direct headset feed: crisp 2D audio so user hears narration equally in both ears
        audioSource.spatialBlend = 0.0f;
        audioSource.priority = 32; // Higher priority than sound effects
        audioSource.volume = masterVolume;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
    }

    private void LoadAudioClips()
    {
        // 1. Try Resources.Load
        if (welcomeClip == null) welcomeClip = Resources.Load<AudioClip>("Audio/Narration/Narration_Welcome");
        if (firstLawClip == null) firstLawClip = Resources.Load<AudioClip>("Audio/Narration/Narration_FirstLaw");
        if (secondLawClip == null) secondLawClip = Resources.Load<AudioClip>("Audio/Narration/Narration_SecondLaw");
        if (thirdLawClip == null) thirdLawClip = Resources.Load<AudioClip>("Audio/Narration/Narration_ThirdLaw");
        if (conclusionClip == null) conclusionClip = Resources.Load<AudioClip>("Audio/Narration/Narration_Conclusion");

        // 2. Editor AssetDatabase fallback
#if UNITY_EDITOR
        if (welcomeClip == null)
            welcomeClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Narration/Narration_Welcome.wav");
        if (firstLawClip == null)
            firstLawClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Narration/Narration_FirstLaw.wav");
        if (secondLawClip == null)
            secondLawClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Narration/Narration_SecondLaw.wav");
        if (thirdLawClip == null)
            thirdLawClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Narration/Narration_ThirdLaw.wav");
        if (conclusionClip == null)
            conclusionClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Narration/Narration_Conclusion.wav");
#endif

        // 3. Fallback runtime streaming from Assets folder if in standalone build
        if (welcomeClip == null) StartCoroutine(StreamClipFallback("Narration_Welcome.wav", clip => welcomeClip = clip));
        if (firstLawClip == null) StartCoroutine(StreamClipFallback("Narration_FirstLaw.wav", clip => firstLawClip = clip));
        if (secondLawClip == null) StartCoroutine(StreamClipFallback("Narration_SecondLaw.wav", clip => secondLawClip = clip));
        if (thirdLawClip == null) StartCoroutine(StreamClipFallback("Narration_ThirdLaw.wav", clip => thirdLawClip = clip));
        if (conclusionClip == null) StartCoroutine(StreamClipFallback("Narration_Conclusion.wav", clip => conclusionClip = clip));
    }

    private IEnumerator StreamClipFallback(string filename, Action<AudioClip> onLoaded)
    {
        string localPath = System.IO.Path.Combine(Application.dataPath, "Audio/Narration", filename);
        if (System.IO.File.Exists(localPath))
        {
            using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip("file://" + localPath, AudioType.WAV))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                    if (clip != null)
                    {
                        clip.name = System.IO.Path.GetFileNameWithoutExtension(filename);
                        onLoaded?.Invoke(clip);
                        Debug.Log($"[Newton VR Narration] Streamed clip {filename} successfully.");
                    }
                }
            }
        }
    }

    private void FindPlayerReference()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
        }
        else
        {
            Camera cam = Camera.main;
            if (cam != null) playerTransform = cam.transform;
        }
    }

    /// <summary>
    /// Guarantees that the START LAB button mesh in the central hub has an active collider for raycasts.
    /// </summary>
    public void EnsureStartButtonCollider()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var obj in allObjects)
        {
            string n = obj.name.ToLower();
            if (n.Equals("startbutton") || n.Equals("starttext"))
            {
                Collider col = obj.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider bc = obj.AddComponent<BoxCollider>();
                    if (n.Equals("startbutton")) bc.size = new Vector3(2.0f, 0.6f, 0.6f);
                    else bc.size = new Vector3(1.4f, 0.5f, 0.5f);
                    Debug.Log($"[Newton VR Narration] Attached interaction BoxCollider to hub button '{obj.name}'.");
                }
                else if (col is MeshCollider mc && !mc.convex)
                {
                    // Non-convex mesh colliders can fail raycasts in VR; replace with generous BoxCollider
                    Destroy(mc);
                    BoxCollider bc = obj.AddComponent<BoxCollider>();
                    if (n.Equals("startbutton")) bc.size = new Vector3(2.0f, 0.6f, 0.6f);
                    else bc.size = new Vector3(1.4f, 0.5f, 0.5f);
                    Debug.Log($"[Newton VR Narration] Replaced non-convex MeshCollider with BoxCollider on '{obj.name}'.");
                }
            }
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayerReference();
            if (playerTransform == null) return;
        }

        Vector3 pos = playerTransform.position;

        // Proximity station checks (auto-triggers only once per station)
        if (Vector3.Distance(pos, lab1Center) < 14f)
        {
            OnEnterLab1();
        }
        else if (Vector3.Distance(pos, lab2Center) < 15f)
        {
            OnEnterLab2();
        }
        else if (Vector3.Distance(pos, lab3Center) < 15f)
        {
            OnEnterLab3();
        }
        else if (Vector3.Distance(pos, lobbyCenter) < 12f)
        {
            OnEnterMainLobby();
        }

        // Desktop hotkey P: Replay current narration
        if (NewtonInput.GetKeyDown(KeyCode.P))
        {
            ReplayCurrentStation();
        }
    }

    #region Narration Entry Triggers

    /// <summary>
    /// Triggered after user enters the VR lab from the START LAB button in the hub.
    /// </summary>
    public void OnEnterFromStartLab()
    {
        currentStation = NarrationStation.Welcome;
        if (!hasPlayedWelcome)
        {
            PlayWelcome();
            hasPlayedWelcome = true;
        }
    }

    /// <summary>
    /// Triggered when user enters or teleports to Lab 1 (Newton's First Law).
    /// </summary>
    public void OnEnterLab1()
    {
        if (currentStation == NarrationStation.FirstLaw) return;
        currentStation = NarrationStation.FirstLaw;

        if (!hasPlayedFirstLaw)
        {
            PlayFirstLaw();
            hasPlayedFirstLaw = true;
        }
    }

    /// <summary>
    /// Triggered when user enters or teleports to Lab 2 (Newton's Second Law).
    /// </summary>
    public void OnEnterLab2()
    {
        if (currentStation == NarrationStation.SecondLaw) return;
        currentStation = NarrationStation.SecondLaw;

        if (!hasPlayedSecondLaw)
        {
            PlaySecondLaw();
            hasPlayedSecondLaw = true;
        }
    }

    /// <summary>
    /// Triggered when user enters or teleports to Lab 3 (Newton's Third Law).
    /// </summary>
    public void OnEnterLab3()
    {
        if (currentStation == NarrationStation.ThirdLaw) return;
        currentStation = NarrationStation.ThirdLaw;

        if (!hasPlayedThirdLaw)
        {
            PlayThirdLaw();
            hasPlayedThirdLaw = true;
        }
    }

    /// <summary>
    /// Triggered when user returns to Central Main Lobby.
    /// </summary>
    public void OnEnterMainLobby()
    {
        currentStation = NarrationStation.Hub;
    }

    #endregion

    #region Audio Playback & Smooth Crossfade

    public void PlayWelcome()
    {
        PlayClip(welcomeClip,
            "Welcome to Newton's Laws Lab",
            "Welcome to the Newton’s Laws Lab. Here we’ll explore and verify Newton’s three laws of motion through interactive experiments. Choose an experiment to begin.");
    }

    public void PlayFirstLaw()
    {
        PlayClip(firstLawClip,
            "Newton's First Law (Law of Inertia)",
            "Welcome to Newton’s First Law: the law of inertia. An object stays at rest, or keeps moving, unless an unbalanced external force acts on it. Watch the cart and ball as they move along the track. Raise the obstacle to see them stop. Leave it down to see them continue to the end of the track.");
    }

    public void PlaySecondLaw()
    {
        PlayClip(secondLawClip,
            "Newton's Second Law (F = m × a)",
            "Welcome to Newton’s Second Law. Acceleration depends on net force and mass: force equals mass times acceleration. Compare the carts at the same force, then compare the effect of changing the force. A greater force produces greater acceleration, while a greater mass produces less acceleration for the same force.");
    }

    public void PlayThirdLaw()
    {
        PlayClip(thirdLawClip,
            "Newton's Third Law (Action & Reaction)",
            "Welcome to Newton’s Third Law. When two objects interact, they exert equal and opposite forces on each other. Use the push controls and watch both carts respond. The forces are equal in size and opposite in direction.");
    }

    public void PlayConclusion()
    {
        currentStation = NarrationStation.Conclusion;
        PlayClip(conclusionClip,
            "Newton's Laws Lab — Conclusion",
            "You’ve now explored all three of Newton’s laws of motion. The experiments showed how objects respond to forces, how force and mass affect acceleration, and how interacting objects exert equal and opposite forces. You have verified Newton’s three laws. Well done.");
    }

    /// <summary>
    /// Replays the narration for whichever station/experiment the player is currently in.
    /// </summary>
    public void ReplayCurrentStation()
    {
        Debug.Log($"[Newton VR Narration] Replaying narration for station: {currentStation}");
        switch (currentStation)
        {
            case NarrationStation.FirstLaw:
                PlayFirstLaw();
                break;
            case NarrationStation.SecondLaw:
                PlaySecondLaw();
                break;
            case NarrationStation.ThirdLaw:
                PlayThirdLaw();
                break;
            case NarrationStation.Conclusion:
                PlayConclusion();
                break;
            case NarrationStation.Welcome:
            case NarrationStation.Hub:
            default:
                if (firstLawCompleted && secondLawCompleted && thirdLawCompleted)
                {
                    PlayConclusion();
                }
                else
                {
                    PlayWelcome();
                }
                break;
        }
    }

    /// <summary>
    /// Plays an audio clip smoothly, ending any currently playing clip first with zero overlap.
    /// </summary>
    private void PlayClip(AudioClip clip, string title, string subtitles)
    {
        currentNarrationTitle = title;
        currentNarrationSubtitles = subtitles;

        if (clip == null)
        {
            Debug.LogWarning($"[Newton VR Narration] Audio clip for '{title}' is not yet loaded.");
            return;
        }

        if (activeFadeRoutine != null)
        {
            StopCoroutine(activeFadeRoutine);
        }
        activeFadeRoutine = StartCoroutine(CrossfadeRoutine(clip));
    }

    private IEnumerator CrossfadeRoutine(AudioClip newClip)
    {
        if (audioSource.isPlaying)
        {
            // Smoothly fade out existing narration in crossfadeDuration
            float startVol = audioSource.volume;
            float t = 0f;
            while (t < crossfadeDuration)
            {
                t += Time.unscaledDeltaTime;
                audioSource.volume = Mathf.Lerp(startVol, 0f, t / crossfadeDuration);
                yield return null;
            }
            audioSource.Stop();
        }

        audioSource.clip = newClip;
        audioSource.volume = masterVolume;
        audioSource.Play();
        Debug.Log($"[Newton VR Narration] Playing narration: '{currentNarrationTitle}' ({newClip.length:F1}s)");
        activeFadeRoutine = null;
    }

    public void StopNarration()
    {
        if (activeFadeRoutine != null)
        {
            StopCoroutine(activeFadeRoutine);
            activeFadeRoutine = null;
        }
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        currentNarrationTitle = "";
        currentNarrationSubtitles = "";
    }

    #endregion

    #region Completion Tracking & Conclusion Trigger

    /// <summary>
    /// Notifies that one of Newton's Law experiments has reached its verified completion.
    /// </summary>
    /// <param name="lawNumber">1, 2, or 3</param>
    public void NotifyExperimentCompleted(int lawNumber)
    {
        bool newlyMarked = false;
        if (lawNumber == 1 && !firstLawCompleted)
        {
            firstLawCompleted = true;
            newlyMarked = true;
        }
        else if (lawNumber == 2 && !secondLawCompleted)
        {
            secondLawCompleted = true;
            newlyMarked = true;
        }
        else if (lawNumber == 3 && !thirdLawCompleted)
        {
            thirdLawCompleted = true;
            newlyMarked = true;
        }

        if (newlyMarked)
        {
            int count = (firstLawCompleted ? 1 : 0) + (secondLawCompleted ? 1 : 0) + (thirdLawCompleted ? 1 : 0);
            Debug.Log($"[Newton VR Narration] Experiment {lawNumber} verified complete! Progress: ({count}/3 completed)");

            // Check if all three experiments are complete
            if (firstLawCompleted && secondLawCompleted && thirdLawCompleted && !hasPlayedConclusion)
            {
                hasPlayedConclusion = true;
                StartCoroutine(PlayConclusionWithDelay(1.2f));
            }
        }
    }

    private IEnumerator PlayConclusionWithDelay(float delay)
    {
        // Wait for experiment impact / finish sound effect to settle
        yield return new WaitForSeconds(delay);

        Debug.Log("[Newton VR Narration] All 3 Newton's Law experiments completed! Commencing Conclusion narration.");
        PlayConclusion();
    }

    public int GetCompletedCount()
    {
        return (firstLawCompleted ? 1 : 0) + (secondLawCompleted ? 1 : 0) + (thirdLawCompleted ? 1 : 0);
    }

    #endregion

    #region Headset Pause / Resume Handling

    void OnApplicationPause(bool pauseStatus)
    {
        if (audioSource == null) return;
        if (pauseStatus)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                wasPlayingBeforePause = true;
            }
        }
        else
        {
            if (wasPlayingBeforePause)
            {
                audioSource.UnPause();
                wasPlayingBeforePause = false;
            }
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (audioSource == null) return;
        if (!hasFocus)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
                wasPlayingBeforePause = true;
            }
        }
        else
        {
            if (wasPlayingBeforePause)
            {
                audioSource.UnPause();
                wasPlayingBeforePause = false;
            }
        }
    }

    #endregion
}

