using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Central game orchestrator for Hexcellence. Manages game state, connects all subsystems
/// via event subscriptions, and drives the turn-based game loop.
///
/// State flow: Spawning → WaitingForInput → Dragging → Placing → (repeat or GameOver)
///
/// Does NOT contain business logic — delegates to PlacementValidator, MergeSystem,
/// PieceSpawner, NumberPool, and ScoreManager via serialized references and events.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ───────────────────────── Enums ─────────────────────────────────────

    /// <summary>Game state machine states.</summary>
    public enum GameState
    {
        Spawning,
        WaitingForInput,
        Dragging,
        Placing,
        GameOver
    }

    // ───────────────────────── Serialized Fields ─────────────────────────

    [Header("References")]
    [Tooltip("The hex grid manager.")]
    [SerializeField] private HexGridManager gridManager;

    [Tooltip("Spawns and manages number pieces.")]
    [SerializeField] private PieceSpawner pieceSpawner;

    [Tooltip("Validates piece placement on the grid.")]
    [SerializeField] private PlacementValidator placementValidator;

    [Tooltip("Handles merging logic when pieces are placed.")]
    [SerializeField] private MergeSystem mergeSystem;

    [Tooltip("Manages the growing number pool.")]
    [SerializeField] private NumberPool numberPool;

    [Tooltip("Manages the input.")]
    [SerializeField] private InputManager inputManager;

    [Header("Game Configuration")]
    [Tooltip("Maximum number of pieces before game over.")]
    [SerializeField, Min(1)] private int maxPieces = 6;

    // ───────────────────────── Private Fields ────────────────────────────

    private GameState currentState = GameState.Spawning;
    private int piecesPlaced;
    private NumberPiece currentPiece;

    /// <summary>Cells currently highlighted during drag hover.</summary>
    private readonly List<HexCell> highlightedCells = new List<HexCell>();

    // ───────────────────────── Properties ────────────────────────────────

    /// <summary>Current game state.</summary>
    public GameState CurrentState => currentState;

    /// <summary>How many pieces have been placed this game.</summary>
    public int PiecesPlaced => piecesPlaced;

    /// <summary>Maximum pieces allowed before game over.</summary>
    public int MaxPieces => maxPieces;

    // ───────────────────────── Events ────────────────────────────────────

    /// <summary>Fired when the game ends. Parameter: final score.</summary>
    public static event Action<int> OnGameOver;

    /// <summary>Fired when the game is restarted.</summary>
    public static event Action OnGameRestarted;

    /// <summary>Fired when the piece count changes. Params: (currentCount, maxCount).</summary>
    public static event Action<int, int> OnPieceCountChanged;

    // ───────────────────────── Unity Lifecycle ───────────────────────────

    private void OnEnable()
    {
        InputManager.OnDragStarted += HandleDragStarted;
        InputManager.OnDragMoved += HandleDragMoved;
        InputManager.OnDragEnded += HandleDragEnded;
        MergeSystem.OnMergeComplete += HandleMergeComplete;
    }

    private void OnDisable()
    {
        InputManager.OnDragStarted -= HandleDragStarted;
        InputManager.OnDragMoved -= HandleDragMoved;
        InputManager.OnDragEnded -= HandleDragEnded;
        MergeSystem.OnMergeComplete -= HandleMergeComplete;
    }

    private void Start()
    {
        StartNewGame();
    }

    // ───────────────────────── Game Flow ─────────────────────────────────

    /// <summary>
    /// Starts a fresh game: resets all systems and spawns the first piece.
    /// </summary>
    public void StartNewGame()
    {
        piecesPlaced = 0;
        currentState = GameState.Spawning;

        gridManager.ResetAllCellValues();
        numberPool.ResetPool();

        OnPieceCountChanged?.Invoke(piecesPlaced, maxPieces);
        SpawnNextPiece();
    }

    /// <summary>
    /// Restarts the entire game. Called by UI restart button.
    /// </summary>
    public void RestartGame()
    {
        // Clear any highlighted cells
        ClearHighlights();

        OnGameRestarted?.Invoke();
        StartNewGame();
    }

    /// <summary>
    /// Spawns the next piece and transitions to WaitingForInput state.
    /// </summary>
    private void SpawnNextPiece()
    {
        currentState = GameState.Spawning;

        pieceSpawner.SpawnNewPiece();
        currentPiece = pieceSpawner.CurrentPiece;

        if (inputManager != null)
            inputManager.InputEnabled = true;

        currentState = GameState.WaitingForInput;
    }

    /// <summary>
    /// Triggers game over state, disabling input and firing the event.
    /// </summary>
    private void TriggerGameOver()
    {
        currentState = GameState.GameOver;

        if (inputManager != null)
            inputManager.InputEnabled = false;

        int finalScore = gridManager.GetBoardScore();
        OnGameOver?.Invoke(finalScore);
    }

    // ───────────────────────── Input Handlers ───────────────────────────

    /// <summary>
    /// Rotates the current piece, invoked from UI or input.
    /// </summary>
    public void RotateCurrentPiece()
    {
        if (currentState != GameState.WaitingForInput) return;
        if (currentPiece == null) return;

        currentPiece.Rotate();
    }



    /// <summary>
    /// Handles drag start — transitions to Dragging state.
    /// </summary>
    private void HandleDragStarted()
    {
        if (currentState != GameState.WaitingForInput) return;

        currentState = GameState.Dragging;
    }

    /// <summary>
    /// Handles drag movement — moves the piece and shows hover highlights on grid cells.
    /// </summary>
    private void HandleDragMoved(Vector3 dragWorldPos)
    {
        if (currentState != GameState.Dragging) return;
        if (currentPiece == null) return;

        // Move the piece to follow the pointer
        currentPiece.transform.position = dragWorldPos;

        // Determine which grid cells the piece would land on
        // Tile A position is the piece's position (corrected without Y offset for grid query)
        Vector3 tileAWorldPos = dragWorldPos;
        Vector2Int anchorAxial = gridManager.WorldToAxial(tileAWorldPos);
        currentPiece.GetTargetCoords(anchorAxial, out Vector2Int coordA, out Vector2Int coordB);

        // Clear previous highlights
        ClearHighlights();

        // Check validity and highlight
        HexCell cellA = gridManager.GetCell(coordA);
        HexCell cellB = gridManager.GetCell(coordB);

        bool bothValid = placementValidator.IsQuickValid(coordA, currentPiece.ValueA, coordB, currentPiece.ValueB);

        if (bothValid && cellA != null && cellB != null)
        {
            cellA.Highlight();
            cellB.Highlight();
            highlightedCells.Add(cellA);
            highlightedCells.Add(cellB);
        }
        // Per game design: invalid tiles don't change color — so we do nothing for invalid
    }

    /// <summary>
    /// Handles drag end — validates and either places the piece or returns it to spawn.
    /// </summary>
    private void HandleDragEnded(Vector3 dropWorldPos)
    {
        if (currentState != GameState.Dragging) return;
        if (currentPiece == null) return;

        ClearHighlights();

        // Calculate target cells from the drop position
        Vector2Int anchorAxial = gridManager.WorldToAxial(dropWorldPos);
        currentPiece.GetTargetCoords(anchorAxial, out Vector2Int coordA, out Vector2Int coordB);

        // Validate placement
        bool isValid = placementValidator.IsQuickValid(coordA, currentPiece.ValueA, coordB, currentPiece.ValueB);

        if (isValid)
        {
            ExecutePlacement(coordA, coordB);
        }
        else
        {
            // Invalid drop — return piece to spawn
            pieceSpawner.ReturnPieceToSpawn();
            currentState = GameState.WaitingForInput;
        }
    }

    // ───────────────────────── Placement ─────────────────────────────────

    /// <summary>
    /// Executes a valid piece placement: merges values, destroys the piece,
    /// increments the counter, and either spawns the next piece or triggers game over.
    /// </summary>
    private void ExecutePlacement(Vector2Int coordA, Vector2Int coordB)
    {
        currentState = GameState.Placing;

        HexCell cellA = gridManager.GetCell(coordA);
        HexCell cellB = gridManager.GetCell(coordB);

        // Execute the merge/placement logic
        mergeSystem.ExecutePlacement(cellA, currentPiece.ValueA, cellB, currentPiece.ValueB);

        // Destroy the piece visual
        pieceSpawner.DestroyCurrentPiece();
        currentPiece = null;

        // Increment counter
        piecesPlaced++;
        OnPieceCountChanged?.Invoke(piecesPlaced, maxPieces);

        // Check for game over based on occupied grid cells
        if (gridManager.GetOccupiedCellCount() >= maxPieces)
        {
            TriggerGameOver();
        }
        else
        {
            SpawnNextPiece();
        }
    }

    // ───────────────────────── Merge Handler ────────────────────────────

    /// <summary>
    /// Handles merge completion — discovers new numbers in the pool.
    /// </summary>
    private void HandleMergeComplete(HexCell cell, int newValue)
    {
        if (numberPool != null)
        {
            numberPool.DiscoverNumber(newValue);
        }
    }

    // ───────────────────────── Utility ───────────────────────────────────

    /// <summary>
    /// Clears all currently highlighted grid cells.
    /// </summary>
    private void ClearHighlights()
    {
        foreach (HexCell cell in highlightedCells)
        {
            if (cell != null)
                cell.ResetHighlight();
        }
        highlightedCells.Clear();
    }
}

// Refresh
