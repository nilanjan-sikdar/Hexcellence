using UnityEngine;
using System;

/// <summary>
/// Calculates and tracks the player's score as the sum of all cell values on the board.
/// Automatically recalculates when any cell value changes.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    #region Events

    /// <summary>
    /// Fired when the score changes.
    /// Parameter: the new total score.
    /// </summary>
    public static event Action<int> OnScoreChanged;

    #endregion

    #region Serialized Fields

    [Header("References")]
    [Tooltip("Reference to the hex grid manager that holds all cells.")]
    [SerializeField] private HexGridManager gridManager;

    #endregion

    #region Private Fields

    /// <summary>The current calculated score.</summary>
    private int currentScore;

    #endregion

    #region Properties

    /// <summary>
    /// Gets the current total score.
    /// </summary>
    public int CurrentScore => currentScore;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Subscribes to cell value change events.
    /// </summary>
    private void OnEnable()
    {
        HexCell.OnCellValueChanged += HandleCellValueChanged;
    }

    /// <summary>
    /// Unsubscribes from cell value change events.
    /// </summary>
    private void OnDisable()
    {
        HexCell.OnCellValueChanged -= HandleCellValueChanged;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles a cell value change by recalculating the total score.
    /// </summary>
    /// <param name="cell">The cell whose value changed.</param>
    /// <param name="previousValue">The previous value of the cell.</param>
    /// <param name="newValue">The new value of the cell.</param>
    private void HandleCellValueChanged(HexCell cell, int previousValue, int newValue)
    {
        RecalculateScore();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Recalculates the score by summing all cell values on the grid.
    /// Fires <see cref="OnScoreChanged"/> if the score has changed.
    /// </summary>
    public void RecalculateScore()
    {
        if (gridManager == null)
        {
            Debug.LogWarning("[ScoreManager] GridManager reference is not assigned.");
            return;
        }

        int newScore = 0;

        foreach (var kvp in gridManager.Cells)
        {
            HexCell cell = kvp.Value;
            newScore += cell.CurrentValue;
        }

        if (newScore != currentScore)
        {
            currentScore = newScore;
            OnScoreChanged?.Invoke(currentScore);
        }
    }

    /// <summary>
    /// Resets the score to zero and fires the change event.
    /// </summary>
    public void ResetScore()
    {
        currentScore = 0;
        OnScoreChanged?.Invoke(currentScore);
    }

    #endregion
}

// Refresh
