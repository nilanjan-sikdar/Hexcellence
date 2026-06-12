using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Dynamically generates, stores, and manages a pointy-topped hexagonal grid using
/// Axial Coordinates (Q, R). All cells are stored in a <c>Dictionary&lt;Vector2Int, HexCell&gt;</c>.
///
/// Provides world-space positioning, coordinate conversions (axial ↔ world),
/// neighbor queries, and hex distance calculations. The generated grid is centered
/// on this manager's Transform position.
///
/// Communicates grid lifecycle events via static C# Actions for decoupled architecture.
/// </summary>
public class HexGridManager : MonoBehaviour
{
    // ───────────────────────── Constants ─────────────────────────────────

    /// <summary>Pre-computed √3 constant for hex geometry calculations.</summary>
    private const float Sqrt3 = 1.7320508075688772f;

    /// <summary>
    /// The six axial-coordinate direction vectors for hex neighbors.
    /// Order: East, NorthEast, NorthWest, West, SouthWest, SouthEast.
    /// </summary>
    private static readonly Vector2Int[] NeighborDirections =
    {
        new Vector2Int(+1,  0), // East
        new Vector2Int(+1, -1), // NorthEast
        new Vector2Int( 0, -1), // NorthWest
        new Vector2Int(-1,  0), // West
        new Vector2Int(-1, +1), // SouthWest
        new Vector2Int( 0, +1), // SouthEast
    };

    // ───────────────────────── Serialized Fields ─────────────────────────

    [Header("Grid Dimensions")]
    [Tooltip("Number of columns (horizontal count) in the grid.")]
    [SerializeField, Min(1)] private int columns = 5;

    [Tooltip("Number of rows (vertical count) in the grid.")]
    [SerializeField, Min(1)] private int rows = 5;

    [Header("Hex Geometry")]
    [Tooltip("Outer radius of each hex — the distance from center to any vertex, in world units.")]
    [SerializeField, Min(0.01f)] private float outerRadius = 1f;

    [Header("References")]
    [Tooltip("The prefab to instantiate for each cell. Must have a HexCell component.")]
    [SerializeField] private HexCell cellPrefab;

    [Tooltip("Parent transform for instantiated cells. Defaults to this transform if left empty.")]
    [SerializeField] private Transform cellParent;

    // ───────────────────────── Private Fields ────────────────────────────

    /// <summary>Master dictionary mapping axial coordinates to their HexCell instances.</summary>
    private readonly Dictionary<Vector2Int, HexCell> cells = new Dictionary<Vector2Int, HexCell>();

    /// <summary>
    /// Offset applied to raw hex positions so the grid is centered on this transform.
    /// Computed once during generation from the bounding box of all raw positions.
    /// </summary>
    private Vector3 gridCenterOffset;

    // ───────────────────────── Properties ────────────────────────────────

    /// <summary>Outer radius (center → vertex) of each hex cell.</summary>
    public float OuterRadius => outerRadius;

    /// <summary>
    /// Inner radius (center → edge midpoint) of each hex cell.
    /// Derived as <c>OuterRadius × √3 / 2</c>.
    /// </summary>
    public float InnerRadius => outerRadius * Sqrt3 / 2f;

    /// <summary>Read-only view of the cell dictionary for external queries.</summary>
    public IReadOnlyDictionary<Vector2Int, HexCell> Cells => cells;

    /// <summary>Total number of cells currently in the grid.</summary>
    public int CellCount => cells.Count;

    /// <summary>Configured column count.</summary>
    public int Columns => columns;

    /// <summary>Configured row count.</summary>
    public int Rows => rows;

    // ───────────────────────── Events ────────────────────────────────────

    /// <summary>
    /// Raised after the grid has been fully generated and all cells are placed.
    /// Passes a reference to this manager.
    /// </summary>
    public static event Action<HexGridManager> OnGridGenerated;

    /// <summary>
    /// Raised immediately before the grid is cleared / destroyed.
    /// Subscribers should release any cell references they hold.
    /// </summary>
    public static event Action OnGridClearing;

    // ───────────────────────── Unity Lifecycle ───────────────────────────

    private void Awake()
    {
        if (cellParent == null)
            cellParent = transform;

        GenerateGrid();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GRID GENERATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tears down any existing grid, then generates a brand-new one using the current
    /// configuration (rows, columns, outerRadius). Safe to call at runtime to regenerate.
    /// </summary>
    public void GenerateGrid()
    {
        ClearGrid();
        ComputeGridCenterOffset();
        CreateCells();

        OnGridGenerated?.Invoke(this);
    }

    /// <summary>
    /// Destroys every cell GameObject in the dictionary and resets internal state.
    /// Raises <see cref="OnGridClearing"/> before destruction begins.
    /// </summary>
    public void ClearGrid()
    {
        OnGridClearing?.Invoke();

        foreach (KeyValuePair<Vector2Int, HexCell> kvp in cells)
        {
            if (kvp.Value != null && kvp.Value.gameObject != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(kvp.Value.gameObject);
                else
#endif
                    Destroy(kvp.Value.gameObject);
            }
        }

        cells.Clear();
    }

    /// <summary>
    /// Pre-computes the centering offset by finding the bounding-box center of all
    /// raw (un-offset) hex positions. This ensures the visual grid is centered on
    /// this manager's transform regardless of how axial coordinates are distributed.
    /// </summary>
    private void ComputeGridCenterOffset()
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                OffsetToAxial(col, row, out int q, out int r);
                Vector3 rawPos = AxialToRawPosition(q, r);

                if (rawPos.x < minX) minX = rawPos.x;
                if (rawPos.x > maxX) maxX = rawPos.x;
                if (rawPos.y < minY) minY = rawPos.y;
                if (rawPos.y > maxY) maxY = rawPos.y;
            }
        }

        gridCenterOffset = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
    }

    /// <summary>
    /// Instantiates a <see cref="HexCell"/> for every (col, row) position, converts it
    /// to axial coordinates, and positions the GameObject in world space.
    /// </summary>
    private void CreateCells()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                OffsetToAxial(col, row, out int q, out int r);

                Vector2Int axialCoord = new Vector2Int(q, r);
                Vector3 worldPos = AxialToWorldPosition(q, r);

                HexCell cell = Instantiate(cellPrefab, worldPos, Quaternion.identity, cellParent);
                cell.Initialize(q, r);

                cells.Add(axialCoord, cell);
            }
        }
    }

    /// <summary>
    /// Converts rectangular grid indices (col, row) to axial coordinates (q, r)
    /// using the "odd-r" offset layout for pointy-topped hexagons.
    /// Odd-numbered rows are shifted right by half a hex width.
    /// <para>Formula: <c>q = col − ⌊row / 2⌋</c>, <c>r = row</c>.</para>
    /// </summary>
    /// <param name="col">The column index (0-based, left to right).</param>
    /// <param name="row">The row index (0-based, bottom to top).</param>
    /// <param name="q">Output axial Q coordinate.</param>
    /// <param name="r">Output axial R coordinate.</param>
    private static void OffsetToAxial(int col, int row, out int q, out int r)
    {
        q = col - (row >> 1); // Integer division by 2 via bit-shift
        r = row;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  COORDINATE CONVERSIONS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Converts axial coordinates to a raw local position (without the centering offset).
    /// <para>Pointy-topped hex math:</para>
    /// <para><c>x = outerRadius × (√3 × q  +  √3/2 × r)</c></para>
    /// <para><c>y = outerRadius × (3/2 × r)</c></para>
    /// </summary>
    private Vector3 AxialToRawPosition(int q, int r)
    {
        float x = outerRadius * (Sqrt3 * q + Sqrt3 * 0.5f * r);
        float y = outerRadius * (1.5f * r);
        return new Vector3(x, y, 0f);
    }

    /// <summary>
    /// Converts axial coordinates (Q, R) to a world-space position, centered on this
    /// manager's Transform. This is the primary conversion used by all external systems.
    /// </summary>
    /// <param name="q">Axial Q coordinate.</param>
    /// <param name="r">Axial R coordinate.</param>
    /// <returns>World-space position of the hex center.</returns>
    public Vector3 AxialToWorldPosition(int q, int r)
    {
        return AxialToRawPosition(q, r) - gridCenterOffset + transform.position;
    }

    /// <summary>
    /// <see cref="AxialToWorldPosition(int,int)"/> overload accepting a Vector2Int.
    /// </summary>
    public Vector3 AxialToWorldPosition(Vector2Int axial)
    {
        return AxialToWorldPosition(axial.x, axial.y);
    }

    /// <summary>
    /// Converts a world-space position to the nearest axial coordinate using
    /// fractional cube-coordinate rounding for pixel-perfect accuracy.
    /// <para>Inverse pointy-top formulas:</para>
    /// <para><c>q = (√3/3 × x  −  1/3 × y) / outerRadius</c></para>
    /// <para><c>r = (2/3 × y) / outerRadius</c></para>
    /// </summary>
    /// <param name="worldPosition">The world-space position to convert.</param>
    /// <returns>The nearest axial coordinate (Q, R).</returns>
    public Vector2Int WorldToAxial(Vector3 worldPosition)
    {
        // Undo centering so we work in "raw" hex space.
        Vector3 rawPos = worldPosition + gridCenterOffset - transform.position;

        float q = (Sqrt3 / 3f * rawPos.x - 1f / 3f * rawPos.y) / outerRadius;
        float r = (2f / 3f * rawPos.y) / outerRadius;

        return CubeRound(q, r);
    }

    /// <summary>
    /// Rounds fractional axial coordinates to the nearest integer hex position
    /// using the cube-coordinate rounding algorithm.
    ///
    /// 1. Derive the implicit third cube coordinate: s = −q − r.
    /// 2. Round all three independently.
    /// 3. Reset the component with the largest rounding error so that q + r + s == 0.
    /// </summary>
    private static Vector2Int CubeRound(float q, float r)
    {
        float s = -q - r;

        int qi = Mathf.RoundToInt(q);
        int ri = Mathf.RoundToInt(r);
        int si = Mathf.RoundToInt(s);

        float qDiff = Mathf.Abs(qi - q);
        float rDiff = Mathf.Abs(ri - r);
        float sDiff = Mathf.Abs(si - s);

        if (qDiff > rDiff && qDiff > sDiff)
            qi = -ri - si;
        else if (rDiff > sDiff)
            ri = -qi - si;
        // else si would be corrected, but axial only needs (q, r).

        return new Vector2Int(qi, ri);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CELL QUERIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Retrieves the cell at the given axial coordinate, or <c>null</c> if none exists.
    /// </summary>
    public HexCell GetCell(Vector2Int axialCoord)
    {
        cells.TryGetValue(axialCoord, out HexCell cell);
        return cell;
    }

    /// <summary>
    /// Retrieves the cell at axial (Q, R), or <c>null</c> if none exists.
    /// </summary>
    public HexCell GetCell(int q, int r)
    {
        return GetCell(new Vector2Int(q, r));
    }

    /// <summary>
    /// Converts a world position to the nearest axial coordinate and returns
    /// the corresponding cell, or <c>null</c> if that coordinate is outside the grid.
    /// </summary>
    public HexCell GetCellAtWorldPosition(Vector3 worldPosition)
    {
        Vector2Int axial = WorldToAxial(worldPosition);
        return GetCell(axial);
    }

    /// <summary>
    /// Checks whether a cell exists at the given axial coordinate in the grid.
    /// </summary>
    public bool HasCell(Vector2Int axialCoord)
    {
        return cells.ContainsKey(axialCoord);
    }

    /// <summary>
    /// Returns all existing neighbor cells of the given axial coordinate.
    /// Only cells that are present in the grid dictionary are included.
    /// </summary>
    /// <param name="axialCoord">The center cell's axial coordinate.</param>
    /// <returns>A list of up to 6 neighboring <see cref="HexCell"/> instances.</returns>
    public List<HexCell> GetNeighbors(Vector2Int axialCoord)
    {
        List<HexCell> neighbors = new List<HexCell>(6);

        for (int i = 0; i < NeighborDirections.Length; i++)
        {
            Vector2Int neighborCoord = axialCoord + NeighborDirections[i];
            if (cells.TryGetValue(neighborCoord, out HexCell neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// <see cref="GetNeighbors(Vector2Int)"/> overload accepting (Q, R) directly.
    /// </summary>
    public List<HexCell> GetNeighbors(int q, int r)
    {
        return GetNeighbors(new Vector2Int(q, r));
    }

    /// <summary>
    /// Returns the axial direction vectors for all six hex neighbors.
    /// Useful for external systems (e.g., PieceController rotation).
    /// </summary>
    public static Vector2Int[] GetNeighborDirections()
    {
        return (Vector2Int[])NeighborDirections.Clone();
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  HEX MATH UTILITIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Computes the hexagonal distance (minimum number of hex steps) between
    /// two axial coordinates. Uses the cube coordinate distance formula:
    /// <c>dist = max(|Δq|, |Δr|, |Δs|)</c> where <c>s = −q − r</c>.
    /// </summary>
    public static int GetDistance(Vector2Int a, Vector2Int b)
    {
        int dq = a.x - b.x;
        int dr = a.y - b.y;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(dq + dr)) / 2;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GRID UTILITIES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resets all cell values to zero without destroying the grid.
    /// Used by GameManager during game restart.
    /// </summary>
    public void ResetAllCellValues()
    {
        foreach (KeyValuePair<Vector2Int, HexCell> kvp in cells)
        {
            kvp.Value.ClearValue();
        }
    }

    /// <summary>
    /// Calculates the sum of all cell values on the board.
    /// </summary>
    public int GetBoardScore()
    {
        int score = 0;
        foreach (KeyValuePair<Vector2Int, HexCell> kvp in cells)
        {
            score += kvp.Value.CurrentValue;
        }
        return score;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  EDITOR GIZMOS
    // ═══════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // At edit-time, draw a preview of where the grid would be.
        if (!Application.isPlaying)
        {
            DrawGridPreview();
            return;
        }

        // At play-time, draw actual cell outlines.
        if (cells == null || cells.Count == 0)
            return;

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.6f); // Semi-transparent yellow
        foreach (KeyValuePair<Vector2Int, HexCell> kvp in cells)
        {
            DrawHexOutline(kvp.Value.transform.position);
        }
    }

    /// <summary>
    /// Draws an edit-mode wireframe preview of the grid based on current serialized values.
    /// Allows designers to visualize the grid before entering Play mode.
    /// </summary>
    private void DrawGridPreview()
    {
        if (columns <= 0 || rows <= 0 || outerRadius <= 0f)
            return;

        // Temporarily compute the offset for preview.
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        var positions = new List<Vector3>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                OffsetToAxial(col, row, out int q, out int r);
                Vector3 rawPos = new Vector3(
                    outerRadius * (Sqrt3 * q + Sqrt3 * 0.5f * r),
                    outerRadius * (1.5f * r),
                    0f);

                positions.Add(rawPos);

                if (rawPos.x < minX) minX = rawPos.x;
                if (rawPos.x > maxX) maxX = rawPos.x;
                if (rawPos.y < minY) minY = rawPos.y;
                if (rawPos.y > maxY) maxY = rawPos.y;
            }
        }

        Vector3 previewOffset = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Gizmos.color = new Color(0.3f, 0.75f, 1f, 0.5f); // Light blue

        foreach (Vector3 rawPos in positions)
        {
            DrawHexOutline(rawPos - previewOffset + transform.position);
        }
    }

    /// <summary>
    /// Draws a wireframe hexagon outline at the given center position.
    /// Vertices start at −30° for pointy-topped orientation.
    /// </summary>
    private void DrawHexOutline(Vector3 center)
    {
        for (int i = 0; i < 6; i++)
        {
            // Pointy-topped: first vertex at −30° (i.e., 330°), then every 60°.
            float angle1 = (60f * i - 30f) * Mathf.Deg2Rad;
            float angle2 = (60f * (i + 1) - 30f) * Mathf.Deg2Rad;

            Vector3 v1 = center + new Vector3(
                outerRadius * Mathf.Cos(angle1),
                outerRadius * Mathf.Sin(angle1),
                0f);

            Vector3 v2 = center + new Vector3(
                outerRadius * Mathf.Cos(angle2),
                outerRadius * Mathf.Sin(angle2),
                0f);

            Gizmos.DrawLine(v1, v2);
        }
    }
#endif
}

// Refresh
