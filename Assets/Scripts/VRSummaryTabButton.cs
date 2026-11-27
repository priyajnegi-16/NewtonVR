using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Dedicated interactive tab button for Room 4 Experiment Summary console.
/// Supports Meta Quest Touch Plus controller rays, hand-tracking pinches, and desktop mouse clicks.
/// Implements standard Unity UI event interfaces (IPointerClickHandler, etc.) as well as 3D physics raycasts.
/// </summary>
public class VRSummaryTabButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Target tab index (0: RESULTS, 1: COMPARISON, 2: CONCLUSION)")]
    public int tabIndex = 0;

    [Tooltip("Tab display name for logging and diagnostics")]
    public string tabName = "RESULTS";

    private Button button;
    private BoxCollider col;

    void Awake()
    {
        button = GetComponent<Button>();
        col = GetComponent<BoxCollider>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnSelectTab);
        }
    }

    /// <summary>
    /// Executes tab switch with full diagnostic logging.
    /// </summary>
    public void OnSelectTab()
    {
        Debug.Log($"[Newton VR Interaction] Tab Button Selected: '{tabName}' (Index {tabIndex}) via Direct Call / Click");
        if (VRRoom4ObservationController.Instance != null)
        {
            VRRoom4ObservationController.Instance.SetSummaryTab(tabIndex);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[Newton VR Interaction] OnPointerClick on '{tabName}' (Index {tabIndex}) from source: {(eventData != null ? eventData.pointerEnter.name : "Unknown")}");
        OnSelectTab();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (VRRoom4ObservationController.Instance != null)
        {
            VRRoom4ObservationController.Instance.SetTabHover(tabIndex, true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (VRRoom4ObservationController.Instance != null)
        {
            VRRoom4ObservationController.Instance.SetTabHover(tabIndex, false);
        }
    }
}

