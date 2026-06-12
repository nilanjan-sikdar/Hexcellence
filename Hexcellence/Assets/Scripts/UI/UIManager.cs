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
    }

    // ───────────────────────── Event Handlers ───────────────────────────

    /// <summary>Handles score change events from ScoreManager.</summary>
    private void HandleScoreChanged(int newScore)
    {
        // Score is only shown at Game Over in the minimalist UI.
    }

    /// <summary>Handles piece count change events from GameManager.</summary>
    private void HandlePieceCountChanged(int current, int max)
    {
        // Piece counter is hidden in the minimalist UI.
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

    /// <summary>Shows the game over overlay with the final score.</summary>
    private void ShowGameOver(int finalScore)
    {
        if (finalScoreValueLabel != null)
            finalScoreValueLabel.text = finalScore.ToString();

        if (gameOverContainer != null)
            gameOverContainer.style.display = DisplayStyle.Flex;
    }

    /// <summary>Hides the game over overlay.</summary>
    private void HideGameOver()
    {
        if (gameOverContainer != null)
            gameOverContainer.style.display = DisplayStyle.None;
    }
}
