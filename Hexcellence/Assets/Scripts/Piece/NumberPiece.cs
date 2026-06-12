using UnityEngine;
using TMPro;

/// <summary>
/// Represents the 2-tile number piece that the player drags and drops onto the hex grid.
/// Contains two hex tiles (A and B), each with a number value and visual representation.
/// Supports 6 rotation orientations corresponding to the 6 hex neighbor directions.
/// </summary>
public class NumberPiece : MonoBehaviour
{
    // ───────────────────────── Constants ─────────────────────────────────

    /// <summary>Pre-computed √3 constant for hex positioning.</summary>
    private const float Sqrt3 = 1.7320508075688772f;

    /// <summary>Total number of unique orientations for a hex piece.</summary>
    private const int TOTAL_ORIENTATIONS = 6;

    /// <summary>
    /// The six axial-coordinate direction vectors for hex neighbors.
    /// Order: East, NorthEast, NorthWest, West, SouthWest, SouthEast.
    /// Matches HexGridManager.NeighborDirections.
    /// </summary>
    private static readonly Vector2Int[] NeighborDirections =
    {
        new Vector2Int(+1,  0), // 0: East
        new Vector2Int(+1, -1), // 1: NorthEast
        new Vector2Int( 0, -1), // 2: NorthWest
        new Vector2Int(-1,  0), // 3: West
        new Vector2Int(-1, +1), // 4: SouthWest
        new Vector2Int( 0, +1), // 5: SouthEast
    };

    // ───────────────────────── Serialized Fields ─────────────────────────

    [Header("Tile References")]
    [Tooltip("The SpriteRenderer for tile A (the anchor tile).")]
    [SerializeField] private SpriteRenderer tileASpriteRenderer;

    [Tooltip("The SpriteRenderer for tile B (the offset tile).")]
    [SerializeField] private SpriteRenderer tileBSpriteRenderer;

    [Tooltip("TextMeshPro displaying the number on tile A.")]
    [SerializeField] private TextMeshPro tileAText;

    [Tooltip("TextMeshPro displaying the number on tile B.")]
    [SerializeField] private TextMeshPro tileBText;

    [Tooltip("The Transform of tile B, repositioned during rotation.")]
    [SerializeField] private Transform tileBTransform;

    [Header("Configuration")]
    [Tooltip("The outer radius of each hex in the piece, must match HexGridManager.OuterRadius.")]
    [SerializeField] private float outerRadius = 1f;

    // ───────────────────────── Private Fields ────────────────────────────

    private int valueA;
    private int valueB;
    private int rotationIndex;

    // ───────────────────────── Properties ────────────────────────────────

    /// <summary>The number value assigned to tile A (anchor).</summary>
    public int ValueA => valueA;

    /// <summary>The number value assigned to tile B (offset).</summary>
    public int ValueB => valueB;

    /// <summary>Current rotation index (0–5), mapping to NeighborDirections.</summary>
    public int RotationIndex => rotationIndex;

    /// <summary>The axial direction offset from tile A to tile B.</summary>
    public Vector2Int DirectionOffset => NeighborDirections[rotationIndex];

    // ───────────────────────── Initialization ───────────────────────────

    /// <summary>
    /// Initializes the piece with two number values, a starting rotation, the hex radius,
    /// and a color palette for tile coloring.
    /// </summary>
    /// <param name="valueA">Number value for tile A.</param>
    /// <param name="valueB">Number value for tile B.</param>
    /// <param name="startRotation">Initial rotation index (0–5).</param>
    /// <param name="hexOuterRadius">Outer radius of each hex, matching the grid.</param>
    /// <param name="palette">Color palette for number-based coloring.</param>
    public void Initialize(int valueA, int valueB, int startRotation, float hexOuterRadius, NumberColorPalette palette)
    {
        this.valueA = valueA;
        this.valueB = valueB;
        this.outerRadius = hexOuterRadius;
        rotationIndex = Mathf.Clamp(startRotation, 0, TOTAL_ORIENTATIONS - 1);

        // Set number text
        if (tileAText != null) tileAText.text = valueA.ToString();
        if (tileBText != null) tileBText.text = valueB.ToString();

        // Set tile colors from palette
        if (palette != null)
        {
            if (tileASpriteRenderer != null) tileASpriteRenderer.color = palette.GetColor(valueA);
            if (tileBSpriteRenderer != null) tileBSpriteRenderer.color = palette.GetColor(valueB);
        }

        // Position tile B based on rotation
        UpdateTileBPosition();

        gameObject.name = $"NumberPiece ({valueA}, {valueB})";
    }

    // ───────────────────────── Rotation ──────────────────────────────────

    /// <summary>
    /// Rotates the piece one step clockwise (increments rotationIndex by 1, mod 6).
    /// Repositions tile B visually.
    /// </summary>
    public void Rotate()
    {
        rotationIndex = (rotationIndex + 1) % TOTAL_ORIENTATIONS;
        UpdateTileBPosition();
    }

    /// <summary>
    /// Positions tile B relative to tile A (the anchor at local origin)
    /// based on the current rotation direction using pointy-top hex math.
    /// </summary>
    private void UpdateTileBPosition()
    {
        if (tileBTransform == null) return;

        Vector2Int dir = NeighborDirections[rotationIndex];
        float x = outerRadius * (Sqrt3 * dir.x + Sqrt3 * 0.5f * dir.y);
        float y = outerRadius * (1.5f * dir.y);

        tileBTransform.localPosition = new Vector3(x, y, 0f);
    }

    // ───────────────────────── Coordinate Queries ────────────────────────

    /// <summary>
    /// Given the axial coordinate where tile A would land (the anchor),
    /// returns both target coordinates: (coordA, coordB).
    /// </summary>
    /// <param name="anchorAxial">The grid coordinate where tile A is placed.</param>
    /// <param name="coordA">Output: coordinate for tile A (same as anchorAxial).</param>
    /// <param name="coordB">Output: coordinate for tile B (anchor + direction offset).</param>
    public void GetTargetCoords(Vector2Int anchorAxial, out Vector2Int coordA, out Vector2Int coordB)
    {
        coordA = anchorAxial;
        coordB = anchorAxial + NeighborDirections[rotationIndex];
    }

    /// <summary>
    /// Returns the world-space offset from tile A's position to tile B's position.
    /// Used by GameManager to compute tile B's snap position.
    /// </summary>
    public Vector3 GetTileBWorldOffset()
    {
        Vector2Int dir = NeighborDirections[rotationIndex];
        float x = outerRadius * (Sqrt3 * dir.x + Sqrt3 * 0.5f * dir.y);
        float y = outerRadius * (1.5f * dir.y);
        return new Vector3(x, y, 0f);
    }
}

// Refresh
