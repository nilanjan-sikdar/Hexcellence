using UnityEngine;
using System;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all mouse/touch input for the hex merging game.
/// Detects taps (for rotation) vs drags (for piece placement).
/// Completely decoupled from game logic — communicates only via C# events.
/// </summary>
public class InputManager : MonoBehaviour
{
    // ───────────────────────── Serialized Fields ─────────────────────────

    [Header("Configuration")]
    [Tooltip("Y-axis offset applied to the piece during drag so it hovers above the finger.")]
    [SerializeField] private float dragYOffset = 1.5f;

    [Tooltip("Minimum pointer movement (in screen pixels) before a tap becomes a drag.")]
    [SerializeField] private float dragThreshold = 40f;

    [Tooltip("The camera used for screen-to-world conversion.")]
    [SerializeField] private Camera gameCamera;

    [Header("Layer Configuration")]
    [Tooltip("Layer mask for detecting the NumberPiece via raycast.")]
    [SerializeField] private LayerMask pieceLayerMask = ~0;

    // ───────────────────────── Private Fields ────────────────────────────

    private bool isPointerDown;
    private bool isDragging;
    private Vector3 pointerDownScreenPos;
    private bool inputEnabled = true;

    // ───────────────────────── Events ────────────────────────────────────



    /// <summary>Fired when a drag begins on the piece.</summary>
    public static event Action OnDragStarted;

    /// <summary>
    /// Fired every frame during a drag with the current world position (Y-offset applied).
    /// Parameter: world-space position where the piece should be rendered.
    /// </summary>
    public static event Action<Vector3> OnDragMoved;

    /// <summary>
    /// Fired when the drag ends (pointer released).
    /// Parameter: world-space position of the drop (without Y-offset, for grid snapping).
    /// </summary>
    public static event Action<Vector3> OnDragEnded;

    // ───────────────────────── Properties ────────────────────────────────

    /// <summary>Whether input is currently being processed.</summary>
    public bool InputEnabled
    {
        get => inputEnabled;
        set => inputEnabled = value;
    }

    // ───────────────────────── Unity Lifecycle ───────────────────────────

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
            
        // Force a reasonable threshold to prevent tapping from immediately turning into dragging on high-DPI screens
        if (dragThreshold < 40f) dragThreshold = 40f;
    }

    private void Update()
    {
        if (!inputEnabled) return;

        HandlePointerInput();
    }

    // ───────────────────────── Input Processing ─────────────────────────

    /// <summary>
    /// Processes mouse/touch input each frame.
    /// Distinguishes between taps (rotation) and drags (placement).
    /// </summary>
    private void HandlePointerInput()
    {
        if (Pointer.current == null) return;

        bool pointerPressedThisFrame = Pointer.current.press.wasPressedThisFrame;
        bool pointerIsPressed = Pointer.current.press.isPressed;
        bool pointerReleasedThisFrame = Pointer.current.press.wasReleasedThisFrame;
        Vector2 pointerPosition = Pointer.current.position.ReadValue();

        // Pointer down
        if (pointerPressedThisFrame)
        {
            isPointerDown = true;
            isDragging = false;
            pointerDownScreenPos = pointerPosition;
        }

        // While pointer held
        if (isPointerDown && pointerIsPressed)
        {
            float distance = Vector3.Distance(pointerPosition, pointerDownScreenPos);

            if (!isDragging && distance > dragThreshold)
            {
                // Crossed drag threshold — drag from anywhere
                isDragging = true;
                OnDragStarted?.Invoke();
            }

            if (isDragging)
            {
                Vector3 worldPos = ScreenToWorldPosition(pointerPosition);
                Vector3 dragPos = worldPos + Vector3.up * dragYOffset;
                OnDragMoved?.Invoke(dragPos);
            }
        }

        // Pointer up
        if (pointerReleasedThisFrame && isPointerDown)
        {
            if (isDragging)
            {
                // End drag — pass the piece's visual position (WITH Y-offset) so grid snapping matches what the user sees
                Vector3 worldPos = ScreenToWorldPosition(pointerPosition);
                Vector3 dropPos = worldPos + Vector3.up * dragYOffset;
                OnDragEnded?.Invoke(dropPos);
            }
            // Tap functionality has been moved to the UI button

            isPointerDown = false;
            isDragging = false;
        }
    }

    // ───────────────────────── Helper Methods ───────────────────────────

    /// <summary>
    /// Converts a screen-space position to world-space (Z = 0 plane).
    /// </summary>
    private Vector3 ScreenToWorldPosition(Vector3 screenPos)
    {
        screenPos.z = Mathf.Abs(gameCamera.transform.position.z);
        Vector3 worldPos = gameCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;
        return worldPos;
    }

    /// <summary>
    /// Checks if a world-space point overlaps with a piece collider.
    /// Uses Physics2D.OverlapPoint with the configured piece layer mask.
    /// </summary>
    private bool IsPointerOverPiece(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos, pieceLayerMask);
        if (hit != null)
        {
            // Check if the hit object has a NumberPiece (or is a child of one)
            return hit.GetComponentInParent<NumberPiece>() != null;
        }
        return false;
    }
}

// Refresh
