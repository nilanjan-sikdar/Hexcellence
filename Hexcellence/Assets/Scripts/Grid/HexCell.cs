using UnityEngine;
using System;
using TMPro;

/// <summary>
/// Represents a single hexagonal cell within the grid.
/// Stores axial coordinates (Q, R), manages cell state (number value, occupancy),
/// displays the number via a child TextMeshPro, and provides visual highlight
/// feedback for drag-and-drop interactions.
/// Communicates via static C# events for full cross-system decoupling.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class HexCell : MonoBehaviour
{
    // ───────────────────────── Serialized Fields ─────────────────────────

    [Header("Visual Configuration")]
    [Tooltip("The default color of this cell when idle (set by CellVisualController at runtime).")]
    [SerializeField] private Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("The color applied when this cell is highlighted (e.g., valid drop target).")]
    [SerializeField] private Color highlightColor = new Color(0.95f, 0.85f, 0.2f, 1f);

    [Tooltip("The color applied when this cell is marked as an invalid target.")]
    [SerializeField] private Color invalidColor = new Color(0.95f, 0.35f, 0.35f, 1f);

    [Header("References")]
    [Tooltip("Child TextMeshPro component used to display the number value on this cell.")]
    [SerializeField] private TextMeshPro numberText;

    // ───────────────────────── Private Fields ────────────────────────────

    private int q;
    private int r;
    private int currentValue;
    private bool isHighlighted;
    private SpriteRenderer spriteRenderer;

    // ───────────────────────── Properties ────────────────────────────────

    /// <summary>The axial Q coordinate of this cell.</summary>
    public int Q => q;

    /// <summary>The axial R coordinate of this cell.</summary>
    public int R => r;

    /// <summary>The axial coordinates packed as a Vector2Int(Q, R).</summary>
    public Vector2Int AxialCoordinates => new Vector2Int(q, r);

    /// <summary>Cached reference to this cell's SpriteRenderer.</summary>
    public SpriteRenderer CellSpriteRenderer => spriteRenderer;

    /// <summary>True if this cell currently holds a number value greater than zero.</summary>
    public bool IsOccupied => currentValue > 0;

    /// <summary>The current number value stored in this cell. Zero means empty.</summary>
    public int CurrentValue => currentValue;

    /// <summary>Whether this cell is in a highlighted visual state.</summary>
    public bool IsHighlighted => isHighlighted;

    // ───────────────────────── Events ────────────────────────────────────

    /// <summary>
    /// Fired whenever a cell's number value changes.
    /// Parameters: (HexCell cell, int previousValue, int newValue).
    /// </summary>
    public static event Action<HexCell, int, int> OnCellValueChanged;

    /// <summary>
    /// Fired whenever a cell's highlight state changes.
    /// Parameters: (HexCell cell, bool isNowHighlighted).
    /// </summary>
    public static event Action<HexCell, bool> OnCellHighlightChanged;

    // ───────────────────────── Unity Lifecycle ───────────────────────────

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // ───────────────────────── Initialization ───────────────────────────

    /// <summary>
    /// Initializes this cell with the given axial coordinates and resets its state.
    /// Called by <see cref="HexGridManager"/> during grid generation.
    /// </summary>
    /// <param name="q">The axial Q coordinate.</param>
    /// <param name="r">The axial R coordinate.</param>
    public void Initialize(int q, int r)
    {
        this.q = q;
        this.r = r;
        isHighlighted = false;

        gameObject.name = $"HexCell [{q}, {r}]";

        // Guard against Awake not having run yet (e.g., editor instantiation).
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // This will fire OnCellValueChanged, allowing CellVisualController to color it correctly as an empty cell
        SetValue(0);

    }

    // ───────────────────────── State Management ─────────────────────────

    /// <summary>
    /// Sets the number value on this cell, updates the number display,
    /// and raises <see cref="OnCellValueChanged"/>.
    /// </summary>
    /// <param name="value">The new number value. Use 0 to mark empty.</param>
    public void SetValue(int value)
    {
        int previousValue = currentValue;
        currentValue = value;
        UpdateNumberDisplay();
        OnCellValueChanged?.Invoke(this, previousValue, value);
    }

    /// <summary>
    /// Convenience method to clear this cell (sets value to 0).
    /// </summary>
    public void ClearValue()
    {
        SetValue(0);
    }

    // ───────────────────────── Number Display ───────────────────────────

    /// <summary>
    /// Updates the child TextMeshPro component to reflect the current value.
    /// Shows the number when value > 0, hides it when empty.
    /// </summary>
    private void UpdateNumberDisplay()
    {
        if (numberText == null) return;

        if (currentValue == 0)
        {
            spriteRenderer.color = defaultColor;
            if (numberText != null) numberText.text = "";
        }
        else
        {
            numberText.gameObject.SetActive(true);
            numberText.text = currentValue.ToString();
        }
    }

    /// <summary>
    /// Directly sets the number display text and visibility.
    /// Used by external systems for preview/ghost display during drag.
    /// Does NOT change the cell's actual value.
    /// </summary>
    /// <param name="displayValue">The number to display, or 0 to hide.</param>
    public void SetNumberDisplayPreview(int displayValue)
    {
        if (numberText == null) return;

        if (displayValue > 0)
        {
            numberText.gameObject.SetActive(true);
            numberText.text = displayValue.ToString();
        }
        else
        {
            numberText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Resets the number display to match the actual current value.
    /// Called after a preview display is cleared.
    /// </summary>
    public void ClearNumberDisplayPreview()
    {
        UpdateNumberDisplay();
    }

    // ───────────────────────── Visual Feedback ──────────────────────────

    /// <summary>
    /// Highlights this cell with the configured <see cref="highlightColor"/>.
    /// Skips redundant calls if already highlighted.
    /// </summary>
    public void Highlight()
    {
        if (isHighlighted) return;

        isHighlighted = true;
        spriteRenderer.color = highlightColor;
        OnCellHighlightChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Highlights this cell with a custom color override.
    /// </summary>
    /// <param name="color">The highlight color to apply.</param>
    public void Highlight(Color color)
    {
        isHighlighted = true;
        spriteRenderer.color = color;
        OnCellHighlightChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Marks this cell with the configured <see cref="invalidColor"/>.
    /// Used to signal that a placement here is not valid.
    /// </summary>
    public void MarkInvalid()
    {
        isHighlighted = true;
        spriteRenderer.color = invalidColor;
        OnCellHighlightChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Resets this cell back to the default idle visual state.
    /// Skips redundant calls if already in default state.
    /// </summary>
    public void ResetHighlight()
    {
        if (!isHighlighted) return;

        isHighlighted = false;
        spriteRenderer.color = defaultColor;
        OnCellHighlightChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Updates the default resting color for this cell.
    /// Applies immediately only if the cell is not currently highlighted.
    /// Called by <see cref="CellVisualController"/> when the cell's number changes.
    /// </summary>
    /// <param name="color">The new default color.</param>
    public void SetDefaultColor(Color color)
    {
        defaultColor = color;
        if (!isHighlighted)
            spriteRenderer.color = defaultColor;
    }

    // ───────────────────────── Debug ─────────────────────────────────────

    public override string ToString()
    {
        return $"HexCell [{q}, {r}] Value={currentValue}";
    }
}

// Refresh
