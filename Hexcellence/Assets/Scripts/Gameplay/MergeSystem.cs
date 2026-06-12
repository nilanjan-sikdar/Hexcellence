using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Executes piece placement onto the hex grid and handles number merging logic.
/// When a tile is placed on a cell with the same value, the values are summed.
/// </summary>
public class MergeSystem : MonoBehaviour
{
    #region Constants

    /// <summary>Default scale multiplier for the pop animation punch.</summary>
    private const float DEFAULT_PUNCH_SCALE = 1.25f;

    /// <summary>Default duration in seconds for the pop animation.</summary>
    private const float DEFAULT_PUNCH_DURATION = 0.25f;

    /// <summary>Base scale value for cells.</summary>
    private const float BASE_SCALE = 1.0f;

    #endregion

    #region Events

    /// <summary>
    /// Fired when a merge is completed on a cell.
    /// Parameters: the cell that was merged, and the new merged value.
    /// </summary>
    public static event Action<HexCell, int> OnMergeComplete;

    #endregion

    #region Public Methods

    /// <summary>
    /// Executes a 2-tile piece placement onto the grid.
    /// For each cell, if the cell is empty, the piece value is set directly.
    /// If the cell already contains the same number, the values are summed (merged).
    /// A scale pop animation is played on both cells after placement.
    /// </summary>
    /// <param name="cellA">The first hex cell to place on.</param>
    /// <param name="valueA">The number value of the first tile.</param>
    /// <param name="cellB">The second hex cell to place on.</param>
    /// <param name="valueB">The number value of the second tile.</param>
    public void ExecutePlacement(HexCell cellA, int valueA, HexCell cellB, int valueB)
    {
        PlaceOnCell(cellA, valueA);
        PlaceOnCell(cellB, valueB);

        StartCoroutine(ScalePopAnimation(cellA.transform, DEFAULT_PUNCH_SCALE, DEFAULT_PUNCH_DURATION));
        StartCoroutine(ScalePopAnimation(cellB.transform, DEFAULT_PUNCH_SCALE, DEFAULT_PUNCH_DURATION));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Places a value on a single cell, handling empty placement and merge logic.
    /// </summary>
    /// <param name="cell">The target hex cell.</param>
    /// <param name="pieceValue">The value of the piece tile being placed.</param>
    private void PlaceOnCell(HexCell cell, int pieceValue)
    {
        if (!cell.IsOccupied)
        {
            // Cell is empty — set the value directly
            cell.SetValue(pieceValue);
        }
        else if (cell.CurrentValue == pieceValue)
        {
            // Cell has the same number — merge by summing
            int mergedValue = cell.CurrentValue + pieceValue;
            cell.SetValue(mergedValue);
            OnMergeComplete?.Invoke(cell, mergedValue);
        }
    }

    /// <summary>
    /// Plays a scale punch animation on the target transform.
    /// Smoothly scales up to the punch scale then back to the base scale.
    /// </summary>
    /// <param name="target">The transform to animate.</param>
    /// <param name="punchScale">The peak scale multiplier.</param>
    /// <param name="duration">Total animation duration in seconds.</param>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator ScalePopAnimation(Transform target, float punchScale = DEFAULT_PUNCH_SCALE, float duration = DEFAULT_PUNCH_DURATION)
    {
        if (target == null)
        {
            yield break;
        }

        Vector3 originalScale = Vector3.one * BASE_SCALE;
        Vector3 punchTargetScale = Vector3.one * punchScale;
        float halfDuration = duration * 0.5f;
        float elapsed = 0f;

        // Scale up phase
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            // Smooth step for a polished ease-in-out feel
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(originalScale, punchTargetScale, smoothT);
            yield return null;
        }

        target.localScale = punchTargetScale;

        // Scale back down phase
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            target.localScale = Vector3.Lerp(punchTargetScale, originalScale, smoothT);
            yield return null;
        }

        target.localScale = originalScale;
    }

    #endregion
}

// Refresh
