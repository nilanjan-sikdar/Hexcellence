using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Binds the UI Toolkit elements (from GameHUD.uxml) to game events.
/// Updates score, piece counter, and manages the game-over overlay visibility.
/// Connects the restart button to GameManager via events — no direct dependency.
/// </summary>
public class UIManager : MonoBehaviour
{
    // ───────────────────────── Serialized Fields ─────────────────────────

    [Header("References")]
    [Tooltip("The UIDocument component displaying the GameHUD.")]
    [SerializeField] private UIDocument uiDocument;

    [Tooltip("Reference to GameManager for restart functionality.")]
    [SerializeField] private GameManager gameManager;

    // ───────────────────────── Private Fields ────────────────────────────

    private Label scoreValueLabel;
    private Label pieceCounterLabel;
    private Label rotateHintLabel;
    private VisualElement gameOverContainer;
    private Label finalScoreValueLabel;
    private Button restartButton;

    // ───────────────────────── Unity Lifecycle ───────────────────────────

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        BindUIElements();
    }

    private void OnEnable()
    {
        ScoreManager.OnScoreChanged += HandleScoreChanged;
        GameManager.OnPieceCountChanged += HandlePieceCountChanged;
        GameManager.OnGameOver += HandleGameOver;
        GameManager.OnGameRestarted += HandleGameRestarted;
    }

    private void OnDisable()
    {
        ScoreManager.OnScoreChanged -= HandleScoreChanged;
        GameManager.OnPieceCountChanged -= HandlePieceCountChanged;
        GameManager.OnGameOver -= HandleGameOver;
        GameManager.OnGameRestarted -= HandleGameRestarted;

        if (restartButton != null)
            restartButton.clicked -= OnRestartButtonClicked;
    }

    // ───────────────────────── UI Binding ────────────────────────────────

    /// <summary>
    /// Queries all named elements from the UXML visual tree and caches references.
    /// </summary>
    private void BindUIElements()
    {
        if (uiDocument == null)
        {
            Debug.LogError("[UIManager] UIDocument is not assigned.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        scoreValueLabel = root.Q<Label>("score-value");
        pieceCounterLabel = root.Q<Label>("piece-counter");
        rotateHintLabel = root.Q<Label>("rotate-hint");
        gameOverContainer = root.Q<VisualElement>("game-over-container");
        finalScoreValueLabel = root.Q<Label>("final-score-value");
        restartButton = root.Q<Button>("restart-button");

        // Bind restart button
        if (restartButton != null)
        {
            restartButton.clicked += OnRestartButtonClicked;
        }

        // Ensure game over is hidden at start
        HideGameOver();

        // Set initial values
        UpdateScore(0);
        UpdatePieceCounter(0, 6);
    }

    // ───────────────────────── Event Handlers ───────────────────────────

    /// <summary>Handles score change events from ScoreManager.</summary>
    private void HandleScoreChanged(int newScore)
    {
        UpdateScore(newScore);
    }

    /// <summary>Handles piece count change events from GameManager.</summary>
    private void HandlePieceCountChanged(int current, int max)
    {
        UpdatePieceCounter(current, max);
    }

    /// <summary>Handles game over event from GameManager.</summary>
    private void HandleGameOver(int finalScore)
    {
        ShowGameOver(finalScore);
    }

    /// <summary>Handles game restart event from GameManager.</summary>
    private void HandleGameRestarted()
    {
        HideGameOver();
        UpdateScore(0);
        UpdatePieceCounter(0, 6);
    }

    /// <summary>Handles restart button click.</summary>
    private void OnRestartButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.RestartGame();
        }
    }

    // ───────────────────────── UI Updates ────────────────────────────────

    /// <summary>Updates the score display label.</summary>
    private void UpdateScore(int score)
    {
        if (scoreValueLabel != null)
            scoreValueLabel.text = score.ToString();
    }

    /// <summary>
    /// Updates the piece counter label.
    /// Shows "Piece X / Y" where X is the next piece number (current + 1).
    /// </summary>
    private void UpdatePieceCounter(int placed, int max)
    {
        if (pieceCounterLabel != null)
        {
            int nextPiece = Mathf.Min(placed + 1, max);
            pieceCounterLabel.text = $"Piece {nextPiece} / {max}";
        }
    }

    /// <summary>Shows the game over overlay with the final score.</summary>
    private void ShowGameOver(int finalScore)
    {
        if (finalScoreValueLabel != null)
            finalScoreValueLabel.text = finalScore.ToString();

        if (gameOverContainer != null)
            gameOverContainer.style.display = DisplayStyle.Flex;

        // Hide the rotate hint
        if (rotateHintLabel != null)
            rotateHintLabel.style.display = DisplayStyle.None;

        if (pieceCounterLabel != null)
            pieceCounterLabel.text = "Game Over!";
    }

    /// <summary>Hides the game over overlay.</summary>
    private void HideGameOver()
    {
        if (gameOverContainer != null)
            gameOverContainer.style.display = DisplayStyle.None;

        // Restore the rotate hint
        if (rotateHintLabel != null)
            rotateHintLabel.style.display = DisplayStyle.Flex;
    }
}
