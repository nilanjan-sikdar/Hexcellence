using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hooks up the UI Canvas button to the GameManager's Rotate logic.
/// </summary>
[RequireComponent(typeof(Button))]
public class CanvasRotateButton : MonoBehaviour
{
    private void Start()
    {
        // 1. Ensure Canvas has a worldCamera
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.worldCamera == null)
        {
            canvas.worldCamera = Camera.main;
        }

        // 2. Fix the EventSystem to use the new Input System (since the project uses it)
        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
        {
            var oldModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldModule != null)
            {
                Destroy(oldModule);
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        // 3. Bind the button
        Button btn = GetComponent<Button>();
        GameManager gm = FindObjectOfType<GameManager>();
        
        if (gm != null)
        {
            btn.onClick.AddListener(() => {
                Debug.Log("[CanvasRotateButton] Rotate button clicked!");
                gm.RotateCurrentPiece();
            });
        }
        else
        {
            Debug.LogError("[CanvasRotateButton] No GameManager found in the scene.");
        }
    }
}
