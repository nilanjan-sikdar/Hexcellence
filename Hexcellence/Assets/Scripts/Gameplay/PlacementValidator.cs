using UnityEngine;

/// <summary>
/// Validates whether a 2-tile piece can be placed at given positions on the hex grid.
/// Checks grid bounds and ensures cells are either empty or contain matching values.
/// </summary>
public class PlacementValidator : MonoBehaviour
{
    #region Nested Types

    /// <summary>
    /// Result codes for placement validation.
    /// </summary>
    public enum PlacementResult
    {
        /// <summary>The placement is valid for both tiles.</summary>
        Valid,

        /// <summary>One or both coordinates are outside the grid bounds.</summary>
        InvalidOutOfBounds,

        /// <summary>One or both cells are occupied with a non-matching value.</summary>
        InvalidOccupiedMismatch
    }

    /// <summary>
    /// Detailed validation result containing the overall result and per-cell validity.
    /// </summary>
    public struct ValidationResult
    {
        /// <summary>The overall placement result.</summary>
        public PlacementResult result;

        /// <summary>Whether cell A passed validation individually.</summary>
        public bool cellAValid;

        /// <summary>Whether cell B passed validation individually.</summary>
        public bool cellBValid;
    }

    #endregion

    #region Serialized Fields

    [Header("References")]
    [Tooltip("Reference to the hex grid manager for cell lookups.")]
    [SerializeField] private HexGridManager gridManager;

    #endregion

    #region Public Methods

    /// <summary>
    /// Validates whether a 2-tile piece can be placed at the specified coordinates.
    /// Each cell must exist in the grid and be either empty or contain the same value as the incoming tile.
    /// </summary>
    /// <param name="coordA">Grid coordinate for tile A.</param>
    /// <param name="valueA">Number value of tile A.</param>
    /// <param name="coordB">Grid coordinate for tile B.</param>
    /// <param name="valueB">Number value of tile B.</param>
    /// <returns>A <see cref="ValidationResult"/> with the overall result and per-cell validity.</returns>
    public ValidationResult Validate(Vector2Int coordA, int valueA, Vector2Int coordB, int valueB)
    {
        ValidationResult result = new ValidationResult
        {
            result = PlacementResult.Valid,
            cellAValid = true,
            cellBValid = true
        };

        bool cellAExists = gridManager.HasCell(coordA);
        bool cellBExists = gridManager.HasCell(coordB);

        // Check bounds first
        if (!cellAExists || !cellBExists)
        {
            result.result = PlacementResult.InvalidOutOfBounds;
            result.cellAValid = cellAExists;
            result.cellBValid = cellBExists;
            return result;
        }

        // Check cell A occupancy / value match
        bool cellAMatchValid = IsSingleCellValid(coordA, valueA);
        // Check cell B occupancy / value match
        bool cellBMatchValid = IsSingleCellValid(coordB, valueB);

        result.cellAValid = cellAMatchValid;
        result.cellBValid = cellBMatchValid;

        if (!cellAMatchValid || !cellBMatchValid)
        {
            result.result = PlacementResult.InvalidOccupiedMismatch;
        }

        return result;
    }

    /// <summary>
    /// Performs a quick boolean check for placement validity of a 2-tile piece.
    /// </summary>
    /// <param name="coordA">Grid coordinate for tile A.</param>
    /// <param name="valueA">Number value of tile A.</param>
    /// <param name="coordB">Grid coordinate for tile B.</param>
    /// <param name="valueB">Number value of tile B.</param>
    /// <returns><c>true</c> if the placement is valid; otherwise <c>false</c>.</returns>
    public bool IsQuickValid(Vector2Int coordA, int valueA, Vector2Int coordB, int valueB)
    {
        return IsSingleCellValid(coordA, valueA) && IsSingleCellValid(coordB, valueB);
    }

    /// <summary>
    /// Validates whether a single cell can accept the given value.
    /// The cell must exist in the grid and be either empty (CurrentValue == 0) or contain the same value.
    /// </summary>
    /// <param name="coord">The grid coordinate to validate.</param>
    /// <param name="value">The number value to place.</param>
    /// <returns><c>true</c> if the cell can accept the value; otherwise <c>false</c>.</returns>
    public bool IsSingleCellValid(Vector2Int coord, int value)
    {
        if (!gridManager.HasCell(coord))
        {
            return false;
        }

        HexCell cell = gridManager.Cells[coord];
        return cell.CurrentValue == 0 || cell.CurrentValue == value;
    }

    #endregion
}

// Refresh
